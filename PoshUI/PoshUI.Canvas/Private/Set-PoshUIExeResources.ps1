# Stamps an icon (and optionally requireAdministrator) into a COPY of PoshUI.exe - New-PoshUIApp -StampIcon.
#
# Any change to an exe's bytes voids its Authenticode signature, which is why this is opt-in and why
# New-PoshUIApp re-signs (or warns) afterwards. The default packaging path never calls this.
function script:Initialize-PoshUIExeResources {
    # Compiled on first use only: most imports never stamp an exe and should not pay for Add-Type.
    if ('PoshUI.ExeResources' -as [type]) { return }
    Add-Type -TypeDefinition @'
using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;

namespace PoshUI
{
    public static class ExeResources
    {
        const int RT_ICON = 3, RT_GROUP_ICON = 14, RT_MANIFEST = 24;
        const uint LOAD_LIBRARY_AS_DATAFILE = 0x2;

        [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Unicode)] static extern IntPtr BeginUpdateResource(string file, bool deleteExisting);
        [DllImport("kernel32.dll", SetLastError = true)] static extern bool UpdateResource(IntPtr h, IntPtr type, IntPtr name, ushort lang, byte[] data, uint size);
        [DllImport("kernel32.dll", SetLastError = true)] static extern bool EndUpdateResource(IntPtr h, bool discard);
        [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Unicode)] static extern IntPtr LoadLibraryEx(string file, IntPtr reserved, uint flags);
        [DllImport("kernel32.dll")] static extern bool FreeLibrary(IntPtr h);
        [DllImport("kernel32.dll", SetLastError = true)] static extern IntPtr FindResource(IntPtr h, IntPtr name, IntPtr type);
        [DllImport("kernel32.dll", SetLastError = true)] static extern IntPtr LoadResource(IntPtr h, IntPtr res);
        [DllImport("kernel32.dll", SetLastError = true)] static extern IntPtr LockResource(IntPtr data);
        [DllImport("kernel32.dll", SetLastError = true)] static extern uint SizeofResource(IntPtr h, IntPtr res);

        /// <summary>Writes every image of an .ico file as RT_ICON entries plus one RT_GROUP_ICON (id 1),
        /// which Explorer uses as the file's icon.</summary>
        public static void SetIcon(string exePath, byte[] ico)
        {
            if (ico.Length < 6 || BitConverter.ToUInt16(ico, 2) != 1) throw new InvalidDataException("Not an .ico file.");
            int count = BitConverter.ToUInt16(ico, 4);
            var group = new MemoryStream();
            var gw = new BinaryWriter(group);
            gw.Write((ushort)0); gw.Write((ushort)1); gw.Write((ushort)count);

            IntPtr h = BeginUpdateResource(exePath, false);
            if (h == IntPtr.Zero) throw new IOException("Cannot open the exe for resource updates (error " + Marshal.GetLastWin32Error() + ").");
            bool ok = false;
            try
            {
                for (int i = 0; i < count; i++)
                {
                    int e = 6 + i * 16;
                    int size = BitConverter.ToInt32(ico, e + 8), offset = BitConverter.ToInt32(ico, e + 12);
                    var image = new byte[size];
                    Buffer.BlockCopy(ico, offset, image, 0, size);
                    if (!UpdateResource(h, (IntPtr)RT_ICON, (IntPtr)(i + 1), 0, image, (uint)size))
                        throw new IOException("Writing icon image " + (i + 1) + " failed (error " + Marshal.GetLastWin32Error() + ").");
                    gw.Write(ico, e, 12);              // width, height, colors, reserved, planes, bitcount, bytes
                    gw.Write((ushort)(i + 1));         // resource id instead of the file offset
                }
                var g = group.ToArray();
                if (!UpdateResource(h, (IntPtr)RT_GROUP_ICON, (IntPtr)1, 0, g, (uint)g.Length))
                    throw new IOException("Writing the icon group failed (error " + Marshal.GetLastWin32Error() + ").");
                ok = true;
            }
            finally { if (!EndUpdateResource(h, !ok) && ok) throw new IOException("Saving the resources failed (error " + Marshal.GetLastWin32Error() + ")."); }
        }

        /// <summary>Reads the embedded application manifest (RT_MANIFEST id 1), or null.</summary>
        public static string GetManifest(string exePath)
        {
            IntPtr lib = LoadLibraryEx(exePath, IntPtr.Zero, LOAD_LIBRARY_AS_DATAFILE);
            if (lib == IntPtr.Zero) return null;
            try
            {
                IntPtr res = FindResource(lib, (IntPtr)1, (IntPtr)RT_MANIFEST);
                if (res == IntPtr.Zero) return null;
                uint size = SizeofResource(lib, res);
                IntPtr ptr = LockResource(LoadResource(lib, res));
                var bytes = new byte[size];
                Marshal.Copy(ptr, bytes, 0, (int)size);
                return Encoding.UTF8.GetString(bytes).TrimStart((char)0xFEFF);
            }
            finally { FreeLibrary(lib); }
        }

        public static void SetManifest(string exePath, string manifest)
        {
            var data = new UTF8Encoding(false).GetBytes(manifest);
            IntPtr h = BeginUpdateResource(exePath, false);
            if (h == IntPtr.Zero) throw new IOException("Cannot open the exe for resource updates (error " + Marshal.GetLastWin32Error() + ").");
            bool ok = UpdateResource(h, (IntPtr)RT_MANIFEST, (IntPtr)1, 0, data, (uint)data.Length);
            if (!EndUpdateResource(h, !ok) || !ok) throw new IOException("Writing the manifest failed (error " + Marshal.GetLastWin32Error() + ").");
        }
    }
}
'@
}

# PNG bytes -> a one-image .ico (PNG-in-ICO, which Windows reads natively). Width/height from the PNG
# header; 256 and larger are written as 0, the ICO format's way of spelling 256.
function script:ConvertTo-PoshUIIco {
    param([Parameter(Mandatory)][byte[]]$Png)
    if ($Png.Length -lt 24 -or $Png[1] -ne 0x50 -or $Png[2] -ne 0x4E -or $Png[3] -ne 0x47) { throw 'The icon is not a PNG file.' }
    $w = ([int]$Png[16] -shl 24) -bor ([int]$Png[17] -shl 16) -bor ([int]$Png[18] -shl 8) -bor [int]$Png[19]
    $h = ([int]$Png[20] -shl 24) -bor ([int]$Png[21] -shl 16) -bor ([int]$Png[22] -shl 8) -bor [int]$Png[23]
    $ms = New-Object System.IO.MemoryStream
    $bw = New-Object System.IO.BinaryWriter $ms
    $bw.Write([uint16]0); $bw.Write([uint16]1); $bw.Write([uint16]1)
    $bw.Write([byte]$(if ($w -ge 256) { 0 } else { $w })); $bw.Write([byte]$(if ($h -ge 256) { 0 } else { $h }))
    $bw.Write([byte]0); $bw.Write([byte]0); $bw.Write([uint16]1); $bw.Write([uint16]32)
    $bw.Write([int]$Png.Length); $bw.Write([int]22); $bw.Write($Png)
    $bw.Flush(); $ms.ToArray()
}

function script:Set-PoshUIExeResources {
    param([Parameter(Mandatory)][string]$ExePath, [string]$IconPath, [switch]$RequireAdmin)
    Initialize-PoshUIExeResources

    if ($IconPath) {
        $bytes = [System.IO.File]::ReadAllBytes($IconPath)
        if ($IconPath -like '*.png') { $bytes = ConvertTo-PoshUIIco -Png $bytes }
        [PoshUI.ExeResources]::SetIcon($ExePath, $bytes)
    }
    if ($RequireAdmin) {
        $manifest = [PoshUI.ExeResources]::GetManifest($ExePath)
        if (-not $manifest) { throw 'The exe has no embedded manifest to mark as requiring administrator.' }
        if ($manifest -notmatch 'requestedExecutionLevel') {
            $trust = '<trustInfo xmlns="urn:schemas-microsoft-com:asm.v2"><security><requestedPrivileges>' +
                     '<requestedExecutionLevel level="requireAdministrator" uiAccess="false"/>' +
                     '</requestedPrivileges></security></trustInfo>'
            $manifest = $manifest -replace '</assembly>\s*$', "$trust</assembly>"
        }
        else {
            $manifest = $manifest -replace 'level="[^"]*"', 'level="requireAdministrator"'
        }
        [PoshUI.ExeResources]::SetManifest($ExePath, $manifest)
    }
}

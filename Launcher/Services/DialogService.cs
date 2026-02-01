// Copyright (c) 2025 A Solution IT LLC. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;
using Microsoft.Win32;

namespace Launcher.Services
{
    public class DialogService : IDialogService
    {
        public string ShowFolderBrowserDialog(string description, string initialPath)
        {
            // Get the window handle for proper dialog ownership
            IntPtr hwnd = IntPtr.Zero;
            try
            {
                if (Application.Current?.MainWindow != null)
                {
                    hwnd = new WindowInteropHelper(Application.Current.MainWindow).Handle;
                }
            }
            catch
            {
                // If we can't get the window handle, continue with IntPtr.Zero
            }

            // Use modern Vista-style folder browser via COM interop (IFileDialog)
            try
            {
                var dialog = (IFileOpenDialog)new FileOpenDialog();
                
                // Set options for folder selection
                dialog.GetOptions(out uint options);
                options |= FOS_PICKFOLDERS | FOS_FORCEFILESYSTEM;
                dialog.SetOptions(options);
                
                // Set title
                if (!string.IsNullOrEmpty(description))
                {
                    dialog.SetTitle(description);
                }
                
                // Set initial folder
                if (!string.IsNullOrEmpty(initialPath) && Directory.Exists(initialPath))
                {
                    IShellItem item = GetShellItemFromPath(initialPath);
                    if (item != null)
                    {
                        dialog.SetFolder(item);
                        Marshal.ReleaseComObject(item);
                    }
                }
                
                // Show dialog with proper parent window
                uint hr = dialog.Show(hwnd);
                
                if (hr == 0) // S_OK
                {
                    dialog.GetResult(out IShellItem resultItem);
                    resultItem.GetDisplayName(SIGDN_FILESYSPATH, out string path);
                    Marshal.ReleaseComObject(resultItem);
                    Marshal.ReleaseComObject(dialog);
                    LoggingService.Info($"Folder selected: {path}", component: "DialogService");
                    return path;
                }
                else if (hr == 0x800704C7) // ERROR_CANCELLED
                {
                    Marshal.ReleaseComObject(dialog);
                    LoggingService.Info("Folder selection cancelled by user", component: "DialogService");
                    return null;
                }
                else
                {
                    Marshal.ReleaseComObject(dialog);
                    LoggingService.Warn($"Folder dialog returned HRESULT: 0x{hr:X8}", component: "DialogService");
                    return null;
                }
            }
            catch (Exception ex)
            {
                LoggingService.Error($"Error showing folder browser dialog: {ex.Message}", ex, component: "DialogService");
            }

            return null; // User cancelled or error
        }
        
        private IShellItem GetShellItemFromPath(string path)
        {
            try
            {
                Guid guid = new Guid("43826d1e-e718-42ee-bc55-a1e261c37bfe"); // IShellItem
                SHCreateItemFromParsingName(path, IntPtr.Zero, ref guid, out IShellItem item);
                return item;
            }
            catch
            {
                return null;
            }
        }
        
        // COM Interop definitions for Vista-style folder browser
        private const uint FOS_PICKFOLDERS = 0x00000020;
        private const uint FOS_FORCEFILESYSTEM = 0x00000040;
        private const uint SIGDN_FILESYSPATH = 0x80058000;

        [ComImport, Guid("DC1C5A9C-E88A-4dde-A5A1-60F82A20AEF7")]
        private class FileOpenDialog { }

        [ComImport, Guid("42f85136-db7e-439c-85f1-e4075d135fc8"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        private interface IFileOpenDialog
        {
            [PreserveSig] uint Show(IntPtr parent);
            void SetFileTypes(uint cFileTypes, IntPtr rgFilterSpec);
            void SetFileTypeIndex(uint iFileType);
            void GetFileTypeIndex(out uint piFileType);
            void Advise(IntPtr pfde, out uint pdwCookie);
            void Unadvise(uint dwCookie);
            void SetOptions(uint fos);
            void GetOptions(out uint pfos);
            void SetDefaultFolder(IShellItem psi);
            void SetFolder(IShellItem psi);
            void GetFolder(out IShellItem ppsi);
            void GetCurrentSelection(out IShellItem ppsi);
            void SetFileName([MarshalAs(UnmanagedType.LPWStr)] string pszName);
            void GetFileName([MarshalAs(UnmanagedType.LPWStr)] out string pszName);
            void SetTitle([MarshalAs(UnmanagedType.LPWStr)] string pszTitle);
            void SetOkButtonLabel([MarshalAs(UnmanagedType.LPWStr)] string pszText);
            void SetFileNameLabel([MarshalAs(UnmanagedType.LPWStr)] string pszLabel);
            void GetResult(out IShellItem ppsi);
            void AddPlace(IShellItem psi, int alignment);
            void SetDefaultExtension([MarshalAs(UnmanagedType.LPWStr)] string pszDefaultExtension);
            void Close(int hr);
            void SetClientGuid(ref Guid guid);
            void ClearClientData();
            void SetFilter(IntPtr pFilter);
            void GetResults(out IntPtr ppenum);
            void GetSelectedItems(out IntPtr ppsai);
        }

        [ComImport, Guid("43826D1E-E718-42EE-BC55-A1E261C37BFE"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        private interface IShellItem
        {
            void BindToHandler(IntPtr pbc, [MarshalAs(UnmanagedType.LPStruct)] Guid bhid, [MarshalAs(UnmanagedType.LPStruct)] Guid riid, out IntPtr ppv);
            void GetParent(out IShellItem ppsi);
            void GetDisplayName(uint sigdnName, [MarshalAs(UnmanagedType.LPWStr)] out string ppszName);
            void GetAttributes(uint sfgaoMask, out uint psfgaoAttribs);
            void Compare(IShellItem psi, uint hint, out int piOrder);
        }

        [DllImport("shell32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        private static extern int SHCreateItemFromParsingName(
            [MarshalAs(UnmanagedType.LPWStr)] string path,
            IntPtr pbc,
            ref Guid riid,
            [MarshalAs(UnmanagedType.Interface)] out IShellItem shellItem);

        public string ShowOpenFileDialog(string initialPath, string initialFileName, string filter = null, string title = null)
        {
            var dialog = new OpenFileDialog
            {
                InitialDirectory = initialPath,
                FileName = initialFileName,
                CheckFileExists = true,
                CheckPathExists = true
            };

            // Apply filter if provided
            if (!string.IsNullOrEmpty(filter))
            {
                // Check if filter is in simple format (*.ext or *.ext1;*.ext2) or full format (Name|*.ext|Name2|*.ext2)
                if (filter.Contains("|"))
                {
                    // Already in Windows dialog format
                    dialog.Filter = filter;
                }
                else
                {
                    // Simple format - convert to Windows dialog format
                    // Examples: "*.ps1" -> "PowerShell Files (*.ps1)|*.ps1"
                    //           "*.log;*.txt" -> "Log Files (*.log;*.txt)|*.log;*.txt"
                    string displayName = filter.Replace("*.", "").Replace(";", ", ").ToUpper() + " Files";
                    dialog.Filter = $"{displayName} ({filter})|{filter}|All Files (*.*)|*.*";
                }
            }

            // Apply title if provided
            if (!string.IsNullOrEmpty(title))
            {
                dialog.Title = title;
            }

            if (dialog.ShowDialog() == true)
            {
                return dialog.FileName;
            }
            return null; // User cancelled
        }
    }
}
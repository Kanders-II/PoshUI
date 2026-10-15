// Copyright (c) 2025 Kanders-II. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using Launcher.Services;

namespace Launcher.AppMode
{
    /// <summary>
    /// Creates and removes the shortcuts an app's settings ask for (<c>--create-shortcuts</c> /
    /// <c>--remove-shortcuts</c>). Each one points at the launcher exe, starts in the app folder, and carries
    /// the app's icon - so a shortcut looks like the app even when the exe itself is the plain, signed
    /// PoshUI.exe under a new name. Never done implicitly: an installer or the operator asks for it.
    /// </summary>
    internal static class AppShortcuts
    {
        public static List<string> Create(AppConfig config, string exePath, bool allUsers)
        {
            var made = new List<string>();
            if (config.Shortcuts == null) return made;
            var icon = ShortcutIcon(config);

            object shell = Activator.CreateInstance(Type.GetTypeFromProgID("WScript.Shell"));
            foreach (var s in config.Shortcuts)
            {
                if (s == null) continue;
                var path = LinkPath(config, s, allUsers);
                Directory.CreateDirectory(Path.GetDirectoryName(path));

                object link = Call(shell, "CreateShortcut", path);
                Set(link, "TargetPath", exePath);
                Set(link, "WorkingDirectory", config.BaseDirectory);
                Set(link, "Description", config.DisplayName);
                if (!string.IsNullOrEmpty(s.Arguments)) Set(link, "Arguments", s.Arguments);
                if (icon != null) Set(link, "IconLocation", icon + ",0");
                Call(link, "Save");
                made.Add(path);
                LoggingService.Info("Shortcut created: " + path, component: "AppShortcuts");
            }
            return made;
        }

        public static List<string> Remove(AppConfig config, bool allUsers)
        {
            var gone = new List<string>();
            if (config.Shortcuts == null) return gone;
            foreach (var s in config.Shortcuts)
            {
                if (s == null) continue;
                var path = LinkPath(config, s, allUsers);
                if (!File.Exists(path)) continue;
                File.Delete(path);
                gone.Add(path);
                // An app's own Start-menu folder goes too once it is empty; a shared folder that still holds
                // something else is left alone.
                if (!string.IsNullOrEmpty(s.Folder))
                {
                    var dir = Path.GetDirectoryName(path);
                    try { if (Directory.Exists(dir) && Directory.GetFileSystemEntries(dir).Length == 0) Directory.Delete(dir); } catch { }
                }
                LoggingService.Info("Shortcut removed: " + path, component: "AppShortcuts");
            }
            return gone;
        }

        private static string LinkPath(AppConfig config, AppShortcut s, bool allUsers)
        {
            bool desktop = string.Equals((s.Location ?? "").Trim(), "Desktop", StringComparison.OrdinalIgnoreCase);
            var root = Environment.GetFolderPath(desktop
                ? (allUsers ? Environment.SpecialFolder.CommonDesktopDirectory : Environment.SpecialFolder.DesktopDirectory)
                : (allUsers ? Environment.SpecialFolder.CommonPrograms : Environment.SpecialFolder.Programs));
            if (!desktop && !string.IsNullOrEmpty(s.Folder)) root = Path.Combine(root, s.Folder);
            var name = string.IsNullOrWhiteSpace(s.Name) ? config.DisplayName : s.Name;
            return Path.Combine(root, name + ".lnk");
        }

        /// <summary>The icon file a shortcut can point at. A shortcut takes .ico/.exe/.dll, not .png, so a
        /// PNG is wrapped as an ICO (Windows reads PNG-in-ICO natively) in the user's PoshUI folder.</summary>
        private static string ShortcutIcon(AppConfig config)
        {
            var src = config.IconPath;
            if (src == null || !File.Exists(src)) return null;
            if (src.EndsWith(".ico", StringComparison.OrdinalIgnoreCase)) return src;

            var dir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "PoshUI", "icons");
            Directory.CreateDirectory(dir);
            var ico = Path.Combine(dir, Path.GetFileNameWithoutExtension(config.SourcePath) + ".ico");
            File.WriteAllBytes(ico, PngToIco(File.ReadAllBytes(src)));
            return ico;
        }

        /// <summary>Wraps PNG bytes as a single-image ICO. Width/height come from the PNG header; 256 and
        /// larger are written as 0, which is how the ICO format spells 256.</summary>
        internal static byte[] PngToIco(byte[] png)
        {
            if (png.Length < 24 || png[1] != 'P' || png[2] != 'N' || png[3] != 'G')
                throw new InvalidDataException("The icon is not a PNG file.");
            int w = (png[16] << 24) | (png[17] << 16) | (png[18] << 8) | png[19];
            int h = (png[20] << 24) | (png[21] << 16) | (png[22] << 8) | png[23];
            using (var ms = new MemoryStream())
            using (var bw = new BinaryWriter(ms))
            {
                bw.Write((short)0); bw.Write((short)1); bw.Write((short)1);       // ICONDIR: reserved, type=icon, count
                bw.Write((byte)(w >= 256 ? 0 : w)); bw.Write((byte)(h >= 256 ? 0 : h));
                bw.Write((byte)0); bw.Write((byte)0);                               // palette, reserved
                bw.Write((short)1); bw.Write((short)32);                            // planes, bits per pixel
                bw.Write(png.Length); bw.Write(6 + 16);                             // size, offset
                bw.Write(png);
                return ms.ToArray();
            }
        }

        private static object Call(object target, string method, params object[] args)
        {
            return target.GetType().InvokeMember(method, BindingFlags.InvokeMethod, null, target, args);
        }

        private static void Set(object target, string property, object value)
        {
            target.GetType().InvokeMember(property, BindingFlags.SetProperty, null, target, new[] { value });
        }
    }
}

// Copyright (c) 2025 Kanders-II. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using System.Text;

namespace Launcher.AppMode
{
    /// <summary>One shortcut an app asks for (see <see cref="AppConfig.Shortcuts"/>).</summary>
    [DataContract]
    public class AppShortcut
    {
        /// <summary>Desktop or StartMenu.</summary>
        [DataMember(Name = "Location")] public string Location { get; set; }
        /// <summary>The shortcut's name, without ".lnk". Defaults to the app's Name.</summary>
        [DataMember(Name = "Name")] public string Name { get; set; }
        /// <summary>Start menu only: a folder to put it in, e.g. "EITaaS".</summary>
        [DataMember(Name = "Folder")] public string Folder { get; set; }
        /// <summary>Extra arguments the shortcut passes to the app.</summary>
        [DataMember(Name = "Arguments")] public string Arguments { get; set; }
    }

    /// <summary>
    /// An app's launcher settings - <c>&lt;ExeName&gt;.app.json</c> beside the exe, or the file named by
    /// <c>--app</c>. Turns PoshUI.exe into that app's launcher: which script runs, whether it needs admin,
    /// its icon and its shortcuts.
    ///
    /// Every path is relative to the config's folder and must stay inside it. The config decides which
    /// script runs with the launcher's rights, so a path that escapes the app folder (rooted, UNC, or
    /// climbing with "..") is refused outright rather than resolved.
    /// </summary>
    [DataContract]
    public class AppConfig
    {
        [DataMember(Name = "Name")] public string Name { get; set; }
        [DataMember(Name = "Script")] public string Script { get; set; }
        [DataMember(Name = "Arguments")] public List<string> Arguments { get; set; }
        [DataMember(Name = "RequireAdmin")] public bool RequireAdmin { get; set; }
        [DataMember(Name = "Icon")] public string Icon { get; set; }
        [DataMember(Name = "LogDirectory")] public string LogDirectory { get; set; }
        [DataMember(Name = "Shortcuts")] public List<AppShortcut> Shortcuts { get; set; }

        /// <summary>The folder every relative path resolves against.</summary>
        public string BaseDirectory { get; private set; }
        /// <summary>Where the config came from (or the script, for <c>--app script.ps1</c>).</summary>
        public string SourcePath { get; private set; }

        public string ScriptPath { get { return Resolve(Script); } }
        public string IconPath { get { return string.IsNullOrWhiteSpace(Icon) ? null : Resolve(Icon); } }
        public string LogPath { get { return string.IsNullOrWhiteSpace(LogDirectory) ? null : Resolve(LogDirectory); } }
        public string DisplayName { get { return string.IsNullOrWhiteSpace(Name) ? Path.GetFileNameWithoutExtension(Script ?? "App") : Name; } }

        /// <summary>The config beside an exe: <c>ESSB.exe</c> -> <c>ESSB.app.json</c>.</summary>
        public static string ConfigPathFor(string exePath)
        {
            return Path.Combine(Path.GetDirectoryName(exePath), Path.GetFileNameWithoutExtension(exePath) + ".app.json");
        }

        /// <summary>Loads a config, or - for a <c>.ps1</c> passed to <c>--app</c> - a minimal one that just
        /// runs that script. Throws <see cref="InvalidDataException"/> with an operator-readable message.</summary>
        public static AppConfig Load(string path)
        {
            var full = Path.GetFullPath(path);
            if (!File.Exists(full)) throw new InvalidDataException("The app settings file was not found: " + full);

            AppConfig cfg;
            if (full.EndsWith(".ps1", StringComparison.OrdinalIgnoreCase))
            {
                cfg = new AppConfig { Script = Path.GetFileName(full) };
            }
            else
            {
                // BOM-tolerant: Windows PowerShell's -Encoding UTF8 writes one and DataContractJsonSerializer
                // rejects it, so a hand-saved config would otherwise fail with an unreadable parse error.
                var text = File.ReadAllText(full).TrimStart((char)0xFEFF);
                try
                {
                    using (var ms = new MemoryStream(Encoding.UTF8.GetBytes(text)))
                        cfg = (AppConfig)new DataContractJsonSerializer(typeof(AppConfig)).ReadObject(ms);
                }
                catch (Exception ex)
                {
                    throw new InvalidDataException("The app settings file is not valid JSON (" + Path.GetFileName(full) + "): " + ex.Message);
                }
                if (cfg == null) throw new InvalidDataException("The app settings file is empty: " + full);
            }

            cfg.SourcePath = full;
            cfg.BaseDirectory = Path.GetDirectoryName(full);
            cfg.Validate();
            return cfg;
        }

        private void Validate()
        {
            if (string.IsNullOrWhiteSpace(Script))
                throw new InvalidDataException("The app settings do not name a Script to run.");
            if (!Script.EndsWith(".ps1", StringComparison.OrdinalIgnoreCase))
                throw new InvalidDataException("Script must be a .ps1 file: " + Script);

            var script = ScriptPath;   // throws if it escapes the folder
            if (!File.Exists(script)) throw new InvalidDataException("The app's script was not found: " + script);

            if (!string.IsNullOrWhiteSpace(Icon))
            {
                var icon = IconPath;
                var ext = Path.GetExtension(icon).ToLowerInvariant();
                if (ext != ".ico" && ext != ".png") throw new InvalidDataException("Icon must be a .ico or .png file: " + Icon);
                // A missing icon is not worth refusing to start over; it is logged by the launcher.
            }
            if (!string.IsNullOrWhiteSpace(LogDirectory)) { var unused = LogPath; }

            if (Shortcuts != null)
                foreach (var s in Shortcuts)
                {
                    if (s == null) continue;
                    var loc = (s.Location ?? "").Trim();
                    if (!loc.Equals("Desktop", StringComparison.OrdinalIgnoreCase) && !loc.Equals("StartMenu", StringComparison.OrdinalIgnoreCase))
                        throw new InvalidDataException("Shortcut Location must be Desktop or StartMenu, not '" + s.Location + "'.");
                    CheckPlainName(s.Name, "Shortcut Name");
                    if (!string.IsNullOrEmpty(s.Folder))
                        foreach (var part in s.Folder.Split('\\', '/')) CheckPlainName(part, "Shortcut Folder");
                }
        }

        /// <summary>Resolves a config path against <see cref="BaseDirectory"/>, refusing anything that would
        /// land outside it.</summary>
        public string Resolve(string relative)
        {
            if (string.IsNullOrWhiteSpace(relative)) throw new InvalidDataException("An empty path was given in the app settings.");
            var r = relative.Trim();
            if (Path.IsPathRooted(r) || r.StartsWith("\\\\", StringComparison.Ordinal) || r.IndexOf(':') >= 0)
                throw new InvalidDataException("Paths in the app settings must be relative to the app folder: " + relative);
            foreach (var part in r.Split('\\', '/'))
                if (part == "..") throw new InvalidDataException("Paths in the app settings may not climb out of the app folder: " + relative);

            var baseDir = Path.GetFullPath(BaseDirectory).TrimEnd('\\') + "\\";
            var full = Path.GetFullPath(Path.Combine(baseDir, r));
            if (!full.StartsWith(baseDir, StringComparison.OrdinalIgnoreCase))
                throw new InvalidDataException("Paths in the app settings must stay inside the app folder: " + relative);
            return full;
        }

        private static void CheckPlainName(string value, string what)
        {
            if (string.IsNullOrEmpty(value)) return;
            if (value.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0 || value == "." || value == "..")
                throw new InvalidDataException(what + " contains characters a file name cannot: " + value);
        }
    }
}

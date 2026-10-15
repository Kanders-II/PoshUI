// Copyright (c) 2025 Kanders-II. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Management.Automation;
using System.Management.Automation.Runspaces;
using System.Security.Principal;
using System.Text.RegularExpressions;
using System.Threading;
using System.Windows;
using Launcher.Services;

namespace Launcher.AppMode
{
    /// <summary>
    /// App mode: PoshUI.exe as an app's launcher. A copy named after the app (ESSB.exe) with its settings
    /// beside it (ESSB.app.json) - or PoshUI.exe given <c>--app &lt;settings-or-script&gt;</c> - runs the app's
    /// script inside this process: no powershell.exe, no console window, and the script's windows open here
    /// rather than in a second PoshUI.exe.
    ///
    /// Exit codes: the script's own (<c>exit N</c>), 1 when it fails with an error, 2 for unusable settings.
    /// </summary>
    internal static class AppLauncher
    {
        private const int ExitBadConfig = 2;

        /// <summary>Set POSHUI_APP_NONINTERACTIVE=1 (tests, unattended runs) to log instead of showing
        /// message boxes.</summary>
        private static bool NonInteractive { get { return Environment.GetEnvironmentVariable("POSHUI_APP_NONINTERACTIVE") == "1"; } }

        /// <summary>Returns true when this launch is app mode and has been handled; false leaves the launch to
        /// PoshUI's normal startup, untouched.</summary>
        public static bool TryRun(App app, string[] args)
        {
            string exePath = Process.GetCurrentProcess().MainModule.FileName;
            string configPath = null;
            bool debug = false, createShortcuts = false, removeShortcuts = false, allUsers = false;
            var scriptArgs = new List<string>();

            for (int i = 0; i < args.Length; i++)
            {
                var a = args[i];
                if (a == "--app" && i + 1 < args.Length) { configPath = args[++i]; continue; }
                if (a == "--debug") { debug = true; continue; }
                if (a == "--create-shortcuts") { createShortcuts = true; continue; }
                if (a == "--remove-shortcuts") { removeShortcuts = true; continue; }
                if (a == "--all-users") { allUsers = true; continue; }
                scriptArgs.Add(a);
            }

            if (configPath == null)
            {
                // A settings file beside the exe makes this copy that app's launcher - but never for a launch
                // that hands PoshUI a definition to show (Show-PoshUICanvas starting the engine).
                if (scriptArgs.Any(s => !s.StartsWith("-") && s.EndsWith(".json", StringComparison.OrdinalIgnoreCase))) return false;
                var beside = AppConfig.ConfigPathFor(exePath);
                if (!File.Exists(beside)) return false;
                configPath = beside;
            }

            AppConfig config;
            try { config = AppConfig.Load(configPath); }
            catch (Exception ex)
            {
                try { LoggingService.Initialize(debug); LoggingService.Error("App settings rejected: " + ex.Message); }
                catch { /* nowhere writable to log to - the message box below still tells the user */ }
                Tell(ex.Message + "\n\nSettings: " + Path.GetFullPath(configPath), "Cannot start", MessageBoxImage.Error);
                app.Shutdown(ExitBadConfig);
                return true;
            }

            // The app's log folder, or "logs" beside the exe - unless that cannot be written (an install under
            // Program Files is read-only for users), in which case the user's own profile.
            var logDir = config.LogPath ?? Path.Combine(Path.GetDirectoryName(exePath), "logs");
            if (!CanWrite(logDir))
                logDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "PoshUI", SafeFileName(config.DisplayName), "logs");
            try { Directory.CreateDirectory(logDir); LoggingService.SetLogDirectory(logDir); } catch { }
            LoggingService.Initialize(debug);
            AuditLogger.Initialize();
            LoggingService.Info("App mode: " + config.DisplayName + " (" + config.SourcePath + ")", component: "AppLauncher");
            if (config.IconPath != null && !File.Exists(config.IconPath))
                LoggingService.Warn("App icon not found: " + config.IconPath, component: "AppLauncher");

            if (createShortcuts || removeShortcuts)
            {
                if (allUsers && !IsAdmin()) { Tell("Shortcuts for all users need administrator rights. Run this from an elevated prompt.", config.DisplayName, MessageBoxImage.Warning); app.Shutdown(1); return true; }
                int code = 0;
                try
                {
                    var done = createShortcuts ? AppShortcuts.Create(config, exePath, allUsers) : AppShortcuts.Remove(config, allUsers);
                    Tell((createShortcuts ? "Shortcuts created:\n" : "Shortcuts removed:\n") + (done.Count == 0 ? "(none)" : string.Join("\n", done)), config.DisplayName, MessageBoxImage.Information);
                }
                catch (Exception ex)
                {
                    LoggingService.Error("Shortcut change failed", ex);
                    Tell("The shortcuts could not be changed: " + ex.Message, config.DisplayName, MessageBoxImage.Error);
                    code = 1;
                }
                app.Shutdown(code);
                return true;
            }

            if (config.RequireAdmin && !IsAdmin())
            {
                Elevate(exePath, args, config);
                app.Shutdown(0);
                return true;
            }

            Run(app, config, exePath, scriptArgs);
            return true;
        }

        private static void Run(App app, AppConfig config, string exePath, List<string> extraArgs)
        {
            // The process belongs to the script now: it ends when the script does, not when a window closes.
            app.ShutdownMode = ShutdownMode.OnExplicitShutdown;
            Launcher.Controls.CanvasBridge.ProcessOwnedByApp = true;

            var logFile = Path.Combine(LoggingService.LogFolder, SafeFileName(config.DisplayName) + ".log");
            var host = new AppHost(config, exePath, logFile);
            var allArgs = new List<string>(config.Arguments ?? new List<string>());
            allArgs.AddRange(extraArgs);

            var thread = new Thread(() =>
            {
                int exitCode;
                string failure = null;
                var started = DateTime.Now;
                AuditLogger.LogScriptExecutionStart(config.ScriptPath);
                try { exitCode = RunScript(config, host, allArgs); }
                catch (Exception ex)
                {
                    var err = (ex as RuntimeException)?.ErrorRecord?.ToString() ?? ex.Message;
                    host.UI.WriteErrorLine(err);
                    AuditLogger.LogExecutionError(config.ScriptPath, ex);
                    failure = err;
                    exitCode = 1;
                }
                AuditLogger.LogScriptExecutionComplete(config.ScriptPath, exitCode, DateTime.Now - started);
                LoggingService.Info("App script finished, exit code " + exitCode, component: "AppLauncher");

                app.Dispatcher.BeginInvoke((Action)(() =>
                {
                    // A script that fails before any window appears would otherwise look like "nothing
                    // happened". Show what it said - this is where a fatal startup check becomes visible.
                    if (exitCode != 0 && !host.WindowShown)
                    {
                        var text = host.RecentOutput;
                        if (string.IsNullOrWhiteSpace(text)) text = failure ?? ("The app stopped with exit code " + exitCode + ".");
                        Tell(text + "\n\nLog: " + logFile, config.DisplayName + " could not start", MessageBoxImage.Error);
                    }
                    app.Shutdown(exitCode);
                }));

                // Something in the script (a COM apartment, a stray foreground thread) can refuse to unwind;
                // never leave an invisible process behind. Background, so a clean exit makes it moot.
                var watchdog = new Thread(() => { Thread.Sleep(8000); LoggingService.Shutdown(); Environment.Exit(exitCode); }) { IsBackground = true };
                watchdog.Start();
            });
            thread.SetApartmentState(ApartmentState.STA);
            thread.IsBackground = true;
            thread.Name = "PoshUIAppScript";
            thread.Start();
        }

        private static int RunScript(AppConfig config, AppHost host, List<string> args)
        {
            // Same effective policy as the "powershell.exe -ExecutionPolicy Bypass -File" launch this replaces;
            // a policy enforced by Group Policy still takes precedence, as it does there.
            var iss = InitialSessionState.CreateDefault();
            iss.LanguageMode = PSLanguageMode.FullLanguage;
            iss.ExecutionPolicy = Microsoft.PowerShell.ExecutionPolicy.Bypass;

            Environment.CurrentDirectory = config.BaseDirectory;
            using (var rs = RunspaceFactory.CreateRunspace(host, iss))
            {
                rs.ApartmentState = ApartmentState.STA;
                rs.ThreadOptions = PSThreadOptions.UseCurrentThread;
                rs.Open();
                rs.SessionStateProxy.SetVariable("__PoshUIAppHost", host);
                rs.SessionStateProxy.Path.SetLocation(WildcardPattern.Escape(config.BaseDirectory));

                using (var ps = PowerShell.Create())
                {
                    ps.Runspace = rs;
                    // Every stream goes through Out-Default so it reaches the host (and the log): a hosted
                    // pipeline otherwise keeps errors and Write-Host in collections nobody reads.
                    // `exit N` inside the script sets $LASTEXITCODE - reset first so a value from before the
                    // script cannot leak into its exit code.
                    var script = "$global:LASTEXITCODE = 0\n" +
                                 "& " + Quote(config.ScriptPath) + (args.Count > 0 ? " " + string.Join(" ", args.Select(ToToken)) : "") + " *>&1 | Out-Default\n" +
                                 "$global:LASTEXITCODE";
                    ps.AddScript(script);
                    var results = ps.Invoke();
                    var last = results.LastOrDefault();
                    int code;
                    if (last != null && int.TryParse(last.ToString(), out code)) return code;
                    return 0;
                }
            }
        }

        /// <summary>One command-line argument as PowerShell source: parameter names stay bare so they bind
        /// (-Live, -AssumeVersion, -Theme:Light), everything else becomes a single-quoted literal so nothing
        /// in it is ever evaluated.</summary>
        internal static string ToToken(string arg)
        {
            if (arg == null) return "''";
            if (Regex.IsMatch(arg, @"^-[A-Za-z_][A-Za-z0-9_]*$")) return arg;
            var m = Regex.Match(arg, @"^(-[A-Za-z_][A-Za-z0-9_]*):(.*)$");
            if (m.Success) return m.Groups[1].Value + ":" + Quote(m.Groups[2].Value);
            return Quote(arg);
        }

        private static string Quote(string s) { return "'" + (s ?? "").Replace("'", "''") + "'"; }

        private static bool IsAdmin()
        {
            using (var id = WindowsIdentity.GetCurrent())
                return new WindowsPrincipal(id).IsInRole(WindowsBuiltInRole.Administrator);
        }

        private static void Elevate(string exePath, string[] args, AppConfig config)
        {
            var psi = new ProcessStartInfo(exePath, string.Join(" ", args.Select(QuoteArg)))
            {
                UseShellExecute = true,
                Verb = "runas",
                WorkingDirectory = config.BaseDirectory
            };
            try
            {
                Process.Start(psi);
                LoggingService.Info("Relaunched as administrator", component: "AppLauncher");
            }
            catch (Win32Exception ex) when (ex.NativeErrorCode == 1223)
            {
                LoggingService.Info("Administrator prompt cancelled - not starting", component: "AppLauncher");
            }
            catch (Exception ex)
            {
                LoggingService.Error("Could not relaunch as administrator", ex);
                Tell(config.DisplayName + " needs administrator rights and could not ask for them: " + ex.Message, "Cannot start", MessageBoxImage.Error);
            }
        }

        /// <summary>Windows command-line quoting for the relaunch (CommandLineToArgvW rules).</summary>
        internal static string QuoteArg(string a)
        {
            if (string.IsNullOrEmpty(a)) return "\"\"";
            if (a.IndexOfAny(new[] { ' ', '\t', '"' }) < 0) return a;
            var sb = new System.Text.StringBuilder("\"");
            int slashes = 0;
            foreach (var ch in a)
            {
                if (ch == '\\') { slashes++; continue; }
                if (ch == '"') { sb.Append('\\', slashes * 2 + 1); slashes = 0; sb.Append('"'); continue; }
                sb.Append('\\', slashes); slashes = 0; sb.Append(ch);
            }
            sb.Append('\\', slashes * 2);
            return sb.Append('"').ToString();
        }

        private static bool CanWrite(string dir)
        {
            try
            {
                Directory.CreateDirectory(dir);
                var probe = Path.Combine(dir, ".write-test-" + Guid.NewGuid().ToString("N"));
                File.WriteAllText(probe, "");
                File.Delete(probe);
                return true;
            }
            catch { return false; }
        }

        private static string SafeFileName(string name)
        {
            foreach (var c in Path.GetInvalidFileNameChars()) name = name.Replace(c, '_');
            return string.IsNullOrWhiteSpace(name) ? "App" : name;
        }

        private static void Tell(string text, string caption, MessageBoxImage icon)
        {
            LoggingService.Info("[message] " + caption + ": " + text.Replace("\n", " | "), component: "AppLauncher");
            if (NonInteractive) return;
            MessageBox.Show(text, caption, MessageBoxButton.OK, icon);
        }
    }
}

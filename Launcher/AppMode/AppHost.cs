// Copyright (c) 2025 Kanders-II. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Management.Automation;
using System.Management.Automation.Host;
using System.Security;
using System.Text;
using System.Threading;
using System.Windows;
using Launcher.Services;

namespace Launcher.AppMode
{
    /// <summary>
    /// The PowerShell host an app's script runs under when PoshUI.exe is its launcher.
    ///
    /// A script started by double-click has no console, so anything it prints would vanish - including the
    /// one message that matters most, a fatal "cannot start" before any window exists. This host writes
    /// every line to the app's log and keeps the recent ones, so the launcher can show them when the script
    /// fails before a window appears. Interactive prompts (Read-Host, credential prompts) fail loudly rather
    /// than hanging a process nobody can see.
    ///
    /// It is also the hand-off point for windows: the script is exposed as <c>$__PoshUIAppHost</c>, and
    /// Show-PoshUICanvas calls <see cref="ShowCanvas"/> instead of starting a second PoshUI.exe.
    /// </summary>
    public sealed class AppHost : PSHost
    {
        private readonly Guid _id = Guid.NewGuid();
        private readonly AppHostUI _ui;
        private readonly AppConfig _config;

        public AppHost(AppConfig config, string exePath, string logFile)
        {
            _config = config;
            ExePath = exePath;
            _ui = new AppHostUI(logFile);
        }

        /// <summary>The launcher exe - what a script should relaunch (e.g. a resume-after-reboot task).</summary>
        public string ExePath { get; private set; }
        /// <summary>The app folder (the config's folder).</summary>
        public string AppDirectory { get { return _config.BaseDirectory; } }
        /// <summary>True once any window has been shown. Before that, a failure is shown in a message box.</summary>
        public bool WindowShown { get; private set; }
        /// <summary>The most recent output, for the failure message box.</summary>
        public string RecentOutput { get { return _ui.Recent(); } }

        public override string Name { get { return "PoshUI"; } }
        public override Version Version { get { return typeof(AppHost).Assembly.GetName().Version; } }
        public override Guid InstanceId { get { return _id; } }
        public override PSHostUserInterface UI { get { return _ui; } }
        public override CultureInfo CurrentCulture { get { return Thread.CurrentThread.CurrentCulture; } }
        public override CultureInfo CurrentUICulture { get { return Thread.CurrentThread.CurrentUICulture; } }

        /// <summary>Only reached by an <c>exit</c> at the very top of the launcher's own command; an
        /// <c>exit N</c> inside the app's script surfaces as $LASTEXITCODE instead (see AppLauncher).</summary>
        public override void SetShouldExit(int exitCode) { ShouldExitCode = exitCode; }
        public int? ShouldExitCode { get; private set; }

        public override void EnterNestedPrompt() { throw new NotSupportedException("Nested prompts are not available when PoshUI is the launcher."); }
        public override void ExitNestedPrompt() { throw new NotSupportedException("Nested prompts are not available when PoshUI is the launcher."); }
        public override void NotifyBeginApplication() { }
        public override void NotifyEndApplication() { }

        /// <summary>
        /// Shows a canvas definition in THIS process and blocks the calling (script) thread until its window
        /// closes. The definition file and its <c>.result.json</c> are handled exactly as when
        /// Show-PoshUICanvas starts PoshUI.exe - this only replaces the process launch.
        /// </summary>
        public void ShowCanvas(string definitionPath)
        {
            if (string.IsNullOrWhiteSpace(definitionPath) || !File.Exists(definitionPath))
                throw new FileNotFoundException("Canvas definition not found.", definitionPath);

            var app = Application.Current as App;
            if (app == null) throw new InvalidOperationException("The PoshUI application is not running.");

            var closed = new ManualResetEventSlim(false);
            app.Dispatcher.Invoke((Action)(() =>
            {
                var window = app.ShowMainWindow(definitionPath, true, _config.IconPath);
                if (window == null) { closed.Set(); return; }
                WindowShown = true;
                window.Closed += (s, e) => closed.Set();
            }));
            closed.Wait();
        }
    }

    internal sealed class AppHostUI : PSHostUserInterface
    {
        private const int Keep = 40;
        private readonly string _logFile;
        private readonly object _lock = new object();
        private readonly Queue<string> _recent = new Queue<string>();
        private readonly StringBuilder _partial = new StringBuilder();
        private readonly AppHostRawUI _raw = new AppHostRawUI();

        public AppHostUI(string logFile) { _logFile = logFile; }

        public override PSHostRawUserInterface RawUI { get { return _raw; } }

        public string Recent() { lock (_lock) return string.Join(Environment.NewLine, _recent.ToArray()); }

        // Write() without a newline accumulates: Write-Host -NoNewline and the formatter both use it.
        public override void Write(string value) { lock (_lock) _partial.Append(value); }
        public override void Write(ConsoleColor foregroundColor, ConsoleColor backgroundColor, string value) { Write(value); }
        public override void WriteLine(string value) { Emit("", value); }
        public override void WriteErrorLine(string value) { Emit("ERROR: ", value); }
        public override void WriteWarningLine(string message) { Emit("WARNING: ", message); }
        public override void WriteVerboseLine(string message) { Emit("VERBOSE: ", message); }
        public override void WriteDebugLine(string message) { Emit("DEBUG: ", message); }
        public override void WriteProgress(long sourceId, ProgressRecord record) { }

        private void Emit(string prefix, string value)
        {
            string line;
            lock (_lock)
            {
                line = prefix + _partial + (value ?? "");
                _partial.Clear();
                foreach (var l in line.Replace("\r\n", "\n").Split('\n'))
                {
                    _recent.Enqueue(l);
                    while (_recent.Count > Keep) _recent.Dequeue();
                }
                if (_logFile != null)
                {
                    try { File.AppendAllText(_logFile, "[" + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + "] " + line + Environment.NewLine, new UTF8Encoding(false)); }
                    catch { }
                }
            }
            LoggingService.Info("[app] " + line, component: "AppHost");
        }

        private static Exception NoPrompt(string what)
        {
            return new PSInvalidOperationException(what + " is not available when PoshUI is the launcher: there is no console to type into. Collect input with a canvas control instead.");
        }

        public override string ReadLine() { throw NoPrompt("Read-Host"); }
        public override SecureString ReadLineAsSecureString() { throw NoPrompt("Read-Host"); }
        public override Dictionary<string, PSObject> Prompt(string caption, string message, System.Collections.ObjectModel.Collection<FieldDescription> descriptions) { throw NoPrompt("Prompting for input"); }
        public override int PromptForChoice(string caption, string message, System.Collections.ObjectModel.Collection<ChoiceDescription> choices, int defaultChoice) { throw NoPrompt("A choice prompt"); }
        public override PSCredential PromptForCredential(string caption, string message, string userName, string targetName) { throw NoPrompt("Get-Credential"); }
        public override PSCredential PromptForCredential(string caption, string message, string userName, string targetName, PSCredentialTypes allowedCredentialTypes, PSCredentialUIOptions options) { throw NoPrompt("Get-Credential"); }
    }

    /// <summary>A console-shaped stub: scripts that read or set window title, colours or size keep working
    /// (the values are simply remembered), and anything needing real key input fails clearly.</summary>
    internal sealed class AppHostRawUI : PSHostRawUserInterface
    {
        public override ConsoleColor BackgroundColor { get; set; } = ConsoleColor.Black;
        public override ConsoleColor ForegroundColor { get; set; } = ConsoleColor.Gray;
        public override System.Management.Automation.Host.Size BufferSize { get; set; } = new System.Management.Automation.Host.Size(200, 3000);
        public override Coordinates CursorPosition { get; set; }
        public override int CursorSize { get; set; } = 25;
        public override bool KeyAvailable { get { return false; } }
        public override System.Management.Automation.Host.Size MaxPhysicalWindowSize { get { return new System.Management.Automation.Host.Size(200, 80); } }
        public override System.Management.Automation.Host.Size MaxWindowSize { get { return new System.Management.Automation.Host.Size(200, 80); } }
        public override Coordinates WindowPosition { get; set; }
        public override System.Management.Automation.Host.Size WindowSize { get; set; } = new System.Management.Automation.Host.Size(120, 40);
        public override string WindowTitle { get; set; } = "";
        public override void FlushInputBuffer() { }
        public override BufferCell[,] GetBufferContents(Rectangle rectangle) { return new BufferCell[0, 0]; }
        public override KeyInfo ReadKey(ReadKeyOptions options) { throw new PSInvalidOperationException("Reading a key is not available when PoshUI is the launcher."); }
        public override void ScrollBufferContents(Rectangle source, Coordinates destination, Rectangle clip, BufferCell fill) { }
        public override void SetBufferContents(Coordinates origin, BufferCell[,] contents) { }
        public override void SetBufferContents(Rectangle rectangle, BufferCell fill) { }
    }
}

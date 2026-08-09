// Copyright (c) 2025 Kanders-II. All rights reserved.
// Licensed under the MIT License.
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Management.Automation;
using System.Management.Automation.Runspaces;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Threading;
using Launcher.Services;

namespace Launcher.Controls
{
    /// <summary>
    /// In-process runtime for a Canvas app (.NET 4.8 / WPF). Hosts one stateful runspace, registers
    /// controls by Name, and exposes the Set-UICanvasValue / Set-UICanvasProperty / Get-UICanvasValue /
    /// Show-UICanvasPage / Submit-UICanvas cmdlets to in-app Action/OnChange scriptblocks. UI mutations
    /// marshal onto the WPF Dispatcher; scriptblocks run off-thread, serialized.
    /// </summary>
    public class CanvasBridge
    {
        private readonly Dispatcher _ui;
        private readonly string _definitionPath;
        private readonly Dictionary<string, CanvasControl> _byName = new Dictionary<string, CanvasControl>(StringComparer.OrdinalIgnoreCase);
        // Values survive page navigation (controls are rebuilt per page); restored on Register, captured on leave.
        private readonly Dictionary<string, object> _values = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);
        private readonly SemaphoreSlim _gate = new SemaphoreSlim(1, 1);
        private readonly List<DispatcherTimer> _timers = new List<DispatcherTimer>();
        private readonly List<Window> _windows = new List<Window>();   // open secondary windows (Show/Close-UICanvasWindow)
        private readonly List<Tuple<System.Windows.Input.Key, System.Windows.Input.ModifierKeys, string>> _shortcuts = new List<Tuple<System.Windows.Input.Key, System.Windows.Input.ModifierKeys, string>>();
        // ── Reactive state ───────────────────────────────────────────────────────────
        private readonly Dictionary<string, object> _state = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<string, List<CanvasControl>> _bindKey = new Dictionary<string, List<CanvasControl>>(StringComparer.OrdinalIgnoreCase);
        private readonly List<Tuple<CanvasControl, string, List<string>>> _bindTemplate = new List<Tuple<CanvasControl, string, List<string>>>();
        private readonly List<Tuple<string, string>> _stateWatchers = new List<Tuple<string, string>>();
        private readonly Dictionary<string, List<CanvasControl>> _bindItems = new Dictionary<string, List<CanvasControl>>(StringComparer.OrdinalIgnoreCase);
        private bool _inBindPush;
        private Runspace _runspace;
        // Strict diagnostics: set env POSHUI_CANVAS_DIAG=1 to also TOAST silent failures (always logged either way).
        private readonly bool _strict = string.Equals(Environment.GetEnvironmentVariable("POSHUI_CANVAS_DIAG"), "1", StringComparison.Ordinal);

        /// <summary>Surfaces a would-be-silent failure: always to the engine log; toasted too in strict mode.</summary>
        private void Diag(string msg)
        {
            LoggingService.Warn("[diag] " + msg, component: "CanvasBridge");
            if (_strict) { try { Toast(msg, "warning", 4500); } catch { } }
        }

        /// <summary>Set by the host: navigate to a canvas page by name/index.</summary>
        public Action<string> PageNavigator;
        /// <summary>Set by the host: called after Submit writes its result (e.g. close the window).</summary>
        public Action Submitter;
        /// <summary>Set by the host: enable/disable the no-backtracking navigation lock.</summary>
        public Action<bool> NavLocker;

        // Also seeded into the Workflow executor's runspace (StartWorkflow) so engine-run step bodies get the
        // same Set-UICanvasValue/Get-UICanvasValue functions and existing canvas scripts run unchanged.
        internal const string Bootstrap = @"
function Set-UICanvasValue { param([Parameter(Mandatory,Position=0)][string]$Name,[Parameter(Position=1)]$Value,[switch]$Quiet) $__PoshUICanvasBridge.SetValue($Name,$Value,[bool]$Quiet) }
function Set-UICanvasProperty { param([Parameter(Mandatory,Position=0)][string]$Name,[Parameter(Mandatory,Position=1)][string]$Property,[Parameter(Position=2)]$Value,[switch]$Quiet) $__PoshUICanvasBridge.SetProperty($Name,$Property,$Value,[bool]$Quiet) }
function Get-UICanvasValue { param([Parameter(Mandatory,Position=0)][string]$Name) $__PoshUICanvasBridge.GetValue($Name) }
function Show-UICanvasPage { param([Parameter(Mandatory,Position=0)]$Page) $__PoshUICanvasBridge.NavigateToPage([string]$Page) }
function Submit-UICanvas { $__PoshUICanvasBridge.Submit() }
function Lock-UICanvasNavigation { $__PoshUICanvasBridge.SetNavLock($true) }
function Unlock-UICanvasNavigation { $__PoshUICanvasBridge.SetNavLock($false) }
function Select-UICanvasFolder { param([string]$Description) $__PoshUICanvasBridge.SelectFolder($Description) }
function Select-UICanvasFile { param([string]$Title,[string]$Filter) $__PoshUICanvasBridge.SelectFile($Title,$Filter) }
function Set-UICanvasAnimate { param([Parameter(Mandatory,Position=0)][string]$Name,[Parameter(Mandatory,Position=1)][string]$Property,[Parameter(Position=2)][double]$To,$From,[double]$Duration=250,[string]$Easing='CubicOut') $__PoshUICanvasBridge.Animate($Name,$Property,$To,$From,$Duration,$Easing) }
function Start-UICanvasAsync { param([Parameter(Mandatory,Position=0)][scriptblock]$Script) $__PoshUICanvasBridge.RunAsync($Script.ToString()) }
function Show-UICanvasToast { param([Parameter(Mandatory,Position=0)][string]$Message,[string]$Severity='info',[int]$Duration=3000) $__PoshUICanvasBridge.Toast($Message,$Severity,$Duration) }
function Show-UICanvasDialog { param([Parameter(Mandatory,Position=0)][string]$Message,[string]$Title='Confirm',[switch]$Prompt,[string]$DefaultValue='',[string]$OkLabel='OK',[string]$CancelLabel='Cancel') $__PoshUICanvasBridge.Dialog($Message,$Title,[bool]$Prompt,$DefaultValue,$OkLabel,$CancelLabel) }
function Show-UICanvasFlyout { param([string]$Target,[string[]]$Items,[string]$Title,[string]$Message,[ValidateSet('Bottom','Top','Left','Right','Mouse')][string]$Placement='Bottom') $__PoshUICanvasBridge.Flyout($Target,$Placement,$Title,$Message,[string[]]$Items) }
function Show-UICanvasWindow { param([Parameter(Mandatory,Position=0)][string]$Name) $__PoshUICanvasBridge.OpenWindow($Name) }
function Close-UICanvasWindow { $__PoshUICanvasBridge.CloseWindow() }
function Set-UICanvasState { param([Parameter(Mandatory,Position=0)][string]$Name,[Parameter(Position=1)]$Value) $__PoshUICanvasBridge.SetState($Name,$Value) }
function Get-UICanvasState { param([Parameter(Position=0)][string]$Name) $__PoshUICanvasBridge.GetState($Name) }
$InformationPreference = 'Continue'
";

        public CanvasBridge(Dispatcher ui, string definitionPath)
        {
            _ui = ui;
            _definitionPath = definitionPath;
            try
            {
                _runspace = RunspaceFactory.CreateRunspace();
                _runspace.ApartmentState = ApartmentState.STA;
                _runspace.ThreadOptions = PSThreadOptions.ReuseThread;
                _runspace.Open();
                _runspace.SessionStateProxy.SetVariable("__PoshUICanvasBridge", this);
                using (var ps = PowerShell.Create())
                {
                    ps.Runspace = _runspace;
                    ps.AddScript(Bootstrap).Invoke();
                }
            }
            catch (Exception ex)
            {
                LoggingService.Error("Failed to init canvas runspace: " + ex.Message, component: "CanvasBridge");
            }

            // Test probe: POSHUI_CANVAS_PROBE=<path> dumps the registered controls (name + value summary) a
            // moment after load, so the smoke harness can assert specific controls actually rendered.
            string probe = Environment.GetEnvironmentVariable("POSHUI_CANVAS_PROBE");
            if (!string.IsNullOrEmpty(probe))
            {
                var t = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(1500) };
                t.Tick += (s, e) => { t.Stop(); try { WriteProbe(probe); } catch { } };
                t.Start();
                _timers.Add(t);
            }
        }

        /// <summary>Writes a JSON snapshot of every registered control (name → value summary) for the test harness.</summary>
        private void WriteProbe(string path)
        {
            var sb = new StringBuilder();
            sb.Append("{");
            bool first = true;
            foreach (var kv in _byName)
            {
                string summary;
                var el = kv.Value.Element;
                var dgp = el as DataGrid ?? FindItemsControl(el) as DataGrid;   // grid may be wrapped (empty-state overlay)
                if (dgp != null) summary = "rows=" + (dgp.Items != null ? dgp.Items.Count : 0);
                else if (kv.Value.GetValue != null) { var v = kv.Value.GetValue(); summary = v == null ? "" : Str(v); if (summary.Length > 40) summary = summary.Substring(0, 40); }
                else summary = el != null ? el.GetType().Name : "";
                if (!first) sb.Append(",");
                first = false;
                sb.Append("\"").Append(kv.Key.Replace("\\", "\\\\").Replace("\"", "\\\"")).Append("\":\"").Append((summary ?? "").Replace("\\", "\\\\").Replace("\"", "\\\"").Replace("\n", " ").Replace("\r", " ")).Append("\"");
            }
            sb.Append("}");
            System.IO.File.WriteAllText(path, sb.ToString());
            LoggingService.Info("Canvas probe written: " + _byName.Count + " controls -> " + path, component: "CanvasBridge");
        }

        /// <summary>Registers a control tree (top-level + nested children) by Name, and starts any
        /// live-value refresh timer.</summary>
        public void Register(CanvasControl cc)
        {
            if (cc == null) return;
            if (!string.IsNullOrEmpty(cc.Name))
            {
                _byName[cc.Name] = cc;
                // Restore a value the user entered on a previous visit to this page.
                object saved;
                if (cc.SetValue != null && _values.TryGetValue(cc.Name, out saved) && saved != null)
                    cc.SetValue(saved);
                // If navigation is locked, keep rail/back buttons (named "nav_*") greyed on rebuilt pages too.
                if (_navLocked && cc.Element != null && cc.Name.StartsWith("nav_", StringComparison.OrdinalIgnoreCase))
                    cc.Element.IsEnabled = false;
            }
            // Reactive state: seed from StateInit, register watchers, and wire -Bind controls.
            if (cc.Def != null)
            {
                if (string.Equals(cc.Def.Type, "StateInit", StringComparison.OrdinalIgnoreCase)) SeedState(PropStr(cc, "StateJson"));
                else if (string.Equals(cc.Def.Type, "StateWatcher", StringComparison.OrdinalIgnoreCase))
                {
                    string wk = PropStr(cc, "Key");
                    if (!string.IsNullOrEmpty(wk)) _stateWatchers.Add(Tuple.Create(wk, cc.Name));
                }
                string bind = PropStr(cc, "Bind");
                if (!string.IsNullOrEmpty(bind))
                {
                    if (bind.IndexOf('{') >= 0) RegisterTemplate(cc, bind);   // "Count is {count}"
                    else RegisterKeyBind(cc, bind);                            // bare key (one-way + two-way)
                }
                string bindItems = PropStr(cc, "BindItems");
                if (!string.IsNullOrEmpty(bindItems)) RegisterItemsBind(cc, bindItems);   // collection -> ItemsSource
            }
            // Keyboard shortcut: parse its gesture once and remember it (the action runs via RunEvent on its name).
            if (cc.Def != null && string.Equals(cc.Def.Type, "Shortcut", StringComparison.OrdinalIgnoreCase))
            {
                string g = PropStr(cc, "Key");
                if (!string.IsNullOrEmpty(g))
                {
                    try
                    {
                        var kg = (System.Windows.Input.KeyGesture)new System.Windows.Input.KeyGestureConverter().ConvertFromString(g);
                        if (kg != null) _shortcuts.Add(new Tuple<System.Windows.Input.Key, System.Windows.Input.ModifierKeys, string>(kg.Key, kg.Modifiers, cc.Name));
                    }
                    catch { Diag("Add-UICanvasShortcut: '" + g + "' is not a valid key gesture."); }
                }
            }
            StartRefresh(cc);
            StartClock(cc);
            StartWorkflow(cc);
            foreach (var child in cc.Children) Register(child);
        }

        // ── Engine-native Workflow control ─────────────────────────────────────────
        /// <summary>Hosts a WorkflowExecutor over a Type="Workflow" control's WorkflowViewModel: runs each step on
        /// the executor's own runspace (off the serialized gate, so the UI/elapsed never freeze), snapshots the
        /// canvas form values as the run's variables, and exposes $wf.GetValue/$wf.SetValue back to the canvas.</summary>
        private void StartWorkflow(CanvasControl cc)
        {
            if (cc == null || cc.Workflow == null) return;
            var vm = cc.Workflow;
            string wfName = cc.Name ?? "";
            string stateFile = PropStr(cc, "StateFile");
            var expected = cc.WorkflowExpectedSeconds;

            Action start = () =>
            {
                if (vm.IsExecuting || vm.IsCompleted) return;
                // Stamp run-start ticks + clear end so an elapsed Clock (Clock=<wf>_start) ticks (no-op if absent).
                SetValue(wfName + "_start", DateTime.UtcNow.Ticks, true);
                SetValue(wfName + "_end", "", true);
                if (string.Equals(PropStr(cc, "LockNavigation"), "True", StringComparison.OrdinalIgnoreCase))
                    SetNavLock(true);   // block backtracking to config pages once the deploy is underway
                var wizardResults = CollectValues();   // every form field becomes a $var in each step
                // Seed the executor's runspace with the bridge cmdlets so step bodies (Set-UICanvasValue / their
                // Set-<name>Progress / WLog helpers) drive canvas controls exactly as in the bridge runspace.
                var seed = new Dictionary<string, object> { { "__PoshUICanvasBridge", this } };
                var exec = new Launcher.Services.WorkflowExecutor(
                    vm, wizardResults,
                    onRebootRequested: reason => { var sub = Submitter; if (sub != null) _ui.Invoke(sub); },
                    getCanvasValue: name => GetValue(name),
                    setCanvasValue: (name, value) => SetValue(name, value),
                    stateFilePath: string.IsNullOrEmpty(stateFile) ? null : stateFile,
                    runspaceVariables: seed,
                    runspacePreamble: Bootstrap);
                // Called on the UI thread: ExecuteAsync yields at its awaits (Task.Run per step), so the window
                // stays responsive and the mirror + step SetValue calls land live — no gate, no freeze.
                var ignore = exec.ExecuteAsync(System.Threading.CancellationToken.None);
            };

            // Defer wiring until the whole tree is registered so the named step cards + run button resolve.
            _ui.BeginInvoke(System.Windows.Threading.DispatcherPriority.Background, (Action)(() =>
            {
                WireWorkflowMirror(vm, wfName, expected);
                if (!string.IsNullOrEmpty(stateFile))
                    Launcher.Services.WorkflowExecutor.LoadState(stateFile, vm);   // resume visuals via the mirror
                CanvasControl runCc;
                if (_byName.TryGetValue(wfName + "_run", out runCc) && runCc != null && runCc.Element is System.Windows.Controls.Primitives.ButtonBase btn)
                    btn.Click += (s, e) => start();
                if (string.Equals(PropStr(cc, "AutoStart"), "True", StringComparison.OrdinalIgnoreCase))
                    start();
            }));
        }

        /// <summary>Mirrors the executor's per-task state onto the PS-rendered named cards (<wf>_stat{i} / <wf>_icon{i} /
        /// <wf>_status) and the weighted gauge/pct (<wf>_gauge / <wf>_pct) — reproducing the legacy composed-action
        /// visuals exactly, but driven by the engine. On completion it also freezes the elapsed clock (<wf>_end).</summary>
        private void WireWorkflowMirror(Launcher.ViewModels.WorkflowViewModel vm, string wfName, List<int> expected)
        {
            if (vm == null || string.IsNullOrEmpty(wfName)) return;
            int n = vm.Tasks.Count;
            var cum = new double[n + 1]; double total = 0;
            if (expected != null && expected.Count == n) { for (int i = 0; i < n; i++) cum[i + 1] = cum[i] + Math.Max(0, expected[i]); total = cum[n]; }
            string SPIN = ((char)0xE72C).ToString(), CHECK = ((char)0x2713).ToString(), CROSS = ((char)0xE711).ToString();

            for (int i = 0; i < n; i++)
            {
                int idx = i;
                var task = vm.Tasks[i];
                task.PropertyChanged += (s, e) =>
                {
                    // Sub-progress: $wf.UpdateProgress drives the gauge smoothly. Only when the step actually reports
                    // (ProgressPercent>0) so it never fights a step that drives the gauge itself (e.g. Set-depProgress).
                    if (e.PropertyName == "ProgressPercent")
                    {
                        if (task.Status == Launcher.ViewModels.WorkflowTaskStatus.Running && task.ProgressPercent > 0)
                        {
                            double frac = task.ProgressPercent / 100.0;
                            int g = total > 0 ? (int)(100 * (cum[idx] + (cum[idx + 1] - cum[idx]) * frac) / total) : (n > 0 ? (int)(100.0 * (idx + frac) / n) : 0);
                            SetProperty(wfName + "_gauge", "Value", g, true); SetValue(wfName + "_pct", g + "%", true);
                        }
                        return;
                    }
                    if (e.PropertyName != "Status") return;
                    string stat = wfName + "_stat" + idx, icon = wfName + "_icon" + idx, status = wfName + "_status";
                    switch (task.Status)
                    {
                        case Launcher.ViewModels.WorkflowTaskStatus.Running:
                            SetValue(stat, "RUNNING", true); SetProperty(stat, "Foreground", "#FBBF24", true);
                            SetValue(icon, SPIN, true); SetProperty(icon, "Foreground", "#FBBF24", true); SetProperty(icon, "Spin", true, true);
                            SetValue(status, "In Progress", true); SetProperty(status, "Foreground", "#93C5FD", true);
                            break;
                        case Launcher.ViewModels.WorkflowTaskStatus.Completed:
                        case Launcher.ViewModels.WorkflowTaskStatus.Skipped:
                            SetProperty(icon, "Spin", false, true); SetValue(icon, CHECK, true); SetProperty(icon, "Foreground", "#34D399", true);
                            SetValue(stat, task.Status == Launcher.ViewModels.WorkflowTaskStatus.Skipped ? "SKIPPED" : "DONE", true);
                            SetProperty(stat, "Foreground", "#34D399", true);
                            int pct = total > 0 ? (int)(100 * cum[idx + 1] / total) : (n > 0 ? (int)(100.0 * (idx + 1) / n) : 0);
                            SetProperty(wfName + "_gauge", "Value", pct, true); SetValue(wfName + "_pct", pct + "%", true);
                            break;
                        case Launcher.ViewModels.WorkflowTaskStatus.Failed:
                            SetProperty(icon, "Spin", false, true); SetValue(icon, CROSS, true); SetProperty(icon, "Foreground", "#F87171", true);
                            SetValue(stat, "FAILED", true); SetProperty(stat, "Foreground", "#F87171", true);
                            SetValue(status, "Failed", true); SetProperty(status, "Foreground", "#F87171", true);
                            break;
                    }
                };
            }
            vm.PropertyChanged += (s, e) =>
            {
                if (e.PropertyName == "IsCompleted" && vm.IsCompleted)
                {
                    SetValue(wfName + "_status", "Complete", true); SetProperty(wfName + "_status", "Foreground", "#34D399", true);
                    SetProperty(wfName + "_gauge", "Value", 100, true); SetValue(wfName + "_pct", "100%", true);
                    SetValue(wfName + "_end", DateTime.UtcNow.Ticks, true);   // freeze the elapsed clock
                }
            };
        }

        private static string PropStr(CanvasControl cc, string key)
        {
            object v;
            if (cc.Def != null && cc.Def.Properties != null && cc.Def.Properties.TryGetValue(key, out v) && v != null) return v.ToString();
            return null;
        }

        /// <summary>Window-level key handler: runs a matching shortcut's action. Wired by CanvasView.</summary>
        public void HandleKey(System.Windows.Input.KeyEventArgs e)
        {
            var mods = System.Windows.Input.Keyboard.Modifiers;
            var key = e.Key == System.Windows.Input.Key.System ? e.SystemKey : e.Key;
            foreach (var sc in _shortcuts)
                if (sc.Item1 == key && sc.Item2 == mods)
                {
                    e.Handled = true;
                    RunEvent(sc.Item3, "Clicked");
                    return;
                }
        }

        /// <summary>Snapshots current on-screen control values into the persistent store (called before
        /// navigating away, and before Submit) so they survive page rebuilds.</summary>
        public void CaptureValues()
        {
            foreach (var kv in _byName)
                if (kv.Value.GetValue != null)
                {
                    var v = kv.Value.GetValue();
                    if (v != null) _values[kv.Key] = v;
                }
        }

        // ── Event execution (Action / OnChange) ───────────────────────────────────
        public void RunEvent(string name, string eventName)
        {
            CanvasControl cc;
            if (!_byName.TryGetValue(name ?? "", out cc) || cc.Def == null) return;
            bool clicked = string.Equals(eventName, "Clicked", StringComparison.OrdinalIgnoreCase);
            string script = clicked ? cc.Def.Action : cc.Def.OnChange;
            if (string.IsNullOrWhiteSpace(script)) return;
            // Disable the button while its click action runs (busy state; prevents double-fire).
            RunScriptAsync(script, name + "." + eventName, clicked ? cc : null);
        }

        private void RunScriptAsync(string script, string label, CanvasControl busyControl = null)
        {
            if (_disposed) return;
            if (busyControl != null && busyControl.Element != null)
                _ui.Invoke((Action)(() => { busyControl.Element.IsEnabled = false; }));
            Task.Run(() =>
            {
                if (!_gate.Wait(0)) { _gate.Wait(); }   // serialize: one pipeline at a time
                try
                {
                    using (var ps = PowerShell.Create())
                    {
                        ps.Runspace = _runspace;
                        ps.AddScript(script);
                        ps.Streams.Error.DataAdded += (s, e) =>
                        {
                            var col = s as PSDataCollection<ErrorRecord>;
                            if (col != null && e.Index < col.Count)
                                LoggingService.Warn("[" + label + "] " + col[e.Index], component: "CanvasBridge");
                        };
                        ps.Invoke();
                        LoggingService.Info("Canvas action ran: " + label, component: "CanvasBridge");
                    }
                }
                catch (Exception ex) { LoggingService.Error("Canvas action '" + label + "' failed: " + ex.Message, component: "CanvasBridge"); }
                finally
                {
                    _gate.Release();
                    if (busyControl != null && busyControl.Element != null)
                        _ui.BeginInvoke((Action)(() => { busyControl.Element.IsEnabled = true; }));   // re-enable after action
                }
            });
        }

        private bool _disposed;

        /// <summary>Stops live-refresh timers and disposes the runspace. Call when the window closes.</summary>
        public void Shutdown()
        {
            if (_disposed) return;
            _disposed = true;
            try { foreach (var t in _timers) t.Stop(); _timers.Clear(); } catch { }
            try { if (_runspace != null) { _runspace.Close(); _runspace.Dispose(); _runspace = null; } } catch { }
            LoggingService.Info("CanvasBridge shut down (timers stopped, runspace disposed).", component: "CanvasBridge");
        }

        // ── Bridge methods called from in-app scriptblocks ────────────────────────
        public void SetValue(string name, object value, bool quiet = false)
        {
            _ui.Invoke((Action)(() =>
            {
                CanvasControl cc;
                if (_byName.TryGetValue(name ?? "", out cc)) { if (cc.SetValue != null) cc.SetValue(value); else { if (!quiet) Diag("Set-UICanvasValue: control '" + name + "' has no settable value."); } }
                else if (!quiet) Diag("Set-UICanvasValue: no control named '" + name + "'.");
            }));
        }

        public object GetValue(string name)
        {
            return _ui.Invoke((Func<object>)(() =>
            {
                CanvasControl cc;
                if (_byName.TryGetValue(name ?? "", out cc) && cc.GetValue != null) return cc.GetValue();
                // Not live on the current page: fall back to the persistent cross-page store. CaptureValues()
                // snapshots each page's control values into _values before navigating away, so a field entered
                // on an earlier page (e.g. an admin password on a Config page) is still readable from a later
                // page's action — otherwise the read returns null and silently drops the value.
                object stored;
                if (name != null && _values.TryGetValue(name, out stored)) return stored;
                // Reading a not-yet-rendered / cross-page control and getting null is a legitimate defensive
                // pattern (e.g. a live summary that reads fields from later pages) — so no diagnostic here.
                return null;
            }));
        }

        public void SetProperty(string name, string property, object value, bool quiet = false)
        {
            _ui.Invoke((Action)(() =>
            {
                CanvasControl cc;
                if (!_byName.TryGetValue(name ?? "", out cc) || cc.Element == null) { if (!quiet) Diag("Set-UICanvasProperty: no control named '" + name + "'."); return; }
                var el = cc.Element;
                switch ((property ?? "").ToLowerInvariant())
                {
                    case "visible": el.Visibility = AsBool(value) ? Visibility.Visible : Visibility.Collapsed; break;
                    case "enabled": el.IsEnabled = AsBool(value); break;
                    case "value":
                        // Progress bars tween smoothly to the new value instead of jumping.
                        if (el is ProgressBar pgb) AnimateDouble(pgb, ProgressBar.ValueProperty, AsDbl(value, 0), null, 350, "CubicOut");
                        else if (cc.SetValue != null) cc.SetValue(value);
                        break;
                    case "label":
                    case "text":
                    case "content":
                        if (el is TextBlock tbl) tbl.Text = Str(value);
                        else if (el is ContentControl cctl) cctl.Content = value;
                        else if (cc.SetValue != null) cc.SetValue(value);
                        break;
                    case "appendline":
                        if (el is TextBox console) { console.AppendText(Str(value) + Environment.NewLine); console.ScrollToEnd(); }
                        break;
                    case "clear": if (el is TextBox cb) cb.Clear(); break;
                    case "x": Canvas.SetLeft(el, AsDbl(value, 0)); break;
                    case "y": Canvas.SetTop(el, AsDbl(value, 0)); break;
                    case "width": el.Width = AsDbl(value, el.Width); break;
                    case "height": el.Height = AsDbl(value, el.Height); break;
                    case "opacity": el.Opacity = AsDbl(value, 1); break;
                    case "background": { var c = el as Control; if (c != null) c.Background = Brush(Str(value)); var b = el as Border; if (b != null) b.Background = Brush(Str(value)); break; }
                    case "color":
                    case "foreground": { var c = el as Control; if (c != null) c.Foreground = Brush(Str(value)); var t = el as TextBlock; if (t != null) t.Foreground = Brush(Str(value)); var bd = el as Border; if (bd != null && bd.Child is TextBlock bt) bt.Foreground = Brush(Str(value)); break; }
                    case "source":
                    case "image":
                    case "imagesource":
                        { var im = el as Image; if (im != null) { var bs = CanvasControlFactory.MakeImageSource(Str(value)); if (bs != null) im.Source = bs; } break; }
                    case "backgroundimage":
                        { var bd2 = el as Border; var bs2 = CanvasControlFactory.MakeImageSource(Str(value)); if (bd2 != null && bs2 != null) bd2.Background = new System.Windows.Media.ImageBrush(bs2) { Stretch = Stretch.UniformToFill }; break; }
                    case "spin":
                        {
                            // Continuous rotation for "processing/busy" indicators (any icon or image).
                            var rt = el.RenderTransform as System.Windows.Media.RotateTransform;
                            if (AsBool(value))
                            {
                                if (rt == null) { rt = new System.Windows.Media.RotateTransform(); el.RenderTransform = rt; el.RenderTransformOrigin = new System.Windows.Point(0.5, 0.5); }
                                var anim = new System.Windows.Media.Animation.DoubleAnimation(0, 360, new System.Windows.Duration(System.TimeSpan.FromSeconds(1)))
                                { RepeatBehavior = System.Windows.Media.Animation.RepeatBehavior.Forever };
                                rt.BeginAnimation(System.Windows.Media.RotateTransform.AngleProperty, anim);
                            }
                            else if (rt != null) { rt.BeginAnimation(System.Windows.Media.RotateTransform.AngleProperty, null); rt.Angle = 0; }
                            break;
                        }
                    default:
                        {
                            // Escape-hatch fallback: set any public CLR property by name (e.g. ItemsSource on a
                            // raw XAML DataGrid spliced in via Add-UICanvasXaml). Case-insensitive; best-effort convert.
                            try
                            {
                                var prop = el.GetType().GetProperty(property,
                                    System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.IgnoreCase);
                                if (prop != null && prop.CanWrite)
                                {
                                    object v = value;
                                    // PowerShell objects don't expose CLR properties to WPF binding; coerce a
                                    // PSObject collection into a DataView so {Binding Col} resolves (e.g. a DataGrid).
                                    if (string.Equals(property, "itemssource", StringComparison.OrdinalIgnoreCase)) v = CoerceItemsSource(value);
                                    else if (value != null && !prop.PropertyType.IsAssignableFrom(value.GetType()))
                                    {
                                        try { v = System.Convert.ChangeType(value, prop.PropertyType); } catch { v = value; }
                                    }
                                    prop.SetValue(el, v);
                                }
                                else Diag("Set-UICanvasProperty: '" + property + "' is not a settable property on " + el.GetType().Name + " ('" + name + "').");
                            }
                            catch (Exception ex) { Diag("Set-UICanvasProperty '" + property + "' on '" + name + "' failed: " + ex.Message); }
                            break;
                        }
                }
            }));
        }

        /// <summary>Turns a PowerShell object collection into a DataView so WPF {Binding Col} resolves (rows
        /// become DataRowView). Non-PSObject collections pass straight through unchanged.</summary>
        private static object CoerceItemsSource(object value)
        {
            if (value == null) return value;
            // The runspace may hand us the whole collection wrapped in a PSObject — unwrap to its BaseObject.
            var wrap = value as System.Management.Automation.PSObject;
            if (wrap != null) value = wrap.BaseObject;
            if (value is string) return value;
            var en = value as System.Collections.IEnumerable;
            if (en == null) return value;

            // Collect rows, unwrapping any per-row PSObject. Rows may be IDictionary (PS hashtable OR the
            // Dictionary<string,object> that MiniJson emits) or [pscustomobject]/CLR objects.
            var rows = new System.Collections.Generic.List<object>();
            foreach (var o in en)
            {
                if (o == null) continue;
                var w = o as System.Management.Automation.PSObject;
                rows.Add(w != null ? w.BaseObject : o);
            }
            if (rows.Count == 0) return value;

            // Scalars (string / number / bool) are plain items for a ComboBox/ListBox — return them as-is. A
            // string's PSObject surfaces a 'Length' property, so without this it would be treated as a 1-column
            // row and wrapped in a DataView (each item rendering as "System.Data.DataRowView").
            var firstScalar = rows[0];
            if (firstScalar is string || firstScalar is System.ValueType) return rows;

            // Columns from the first row. IDictionary -> its keys (NOT AsPSObject: that would surface the
            // Dictionary's CLR members Comparer/Count/Keys/Values instead of the entries).
            var cols = new System.Collections.Generic.List<string>();
            var first = rows[0];
            var fdict = first as System.Collections.IDictionary;
            if (fdict != null) { foreach (var k in fdict.Keys) cols.Add(Str(k)); }
            else { foreach (var p in System.Management.Automation.PSObject.AsPSObject(first).Properties) cols.Add(p.Name); }
            if (cols.Count == 0) return value;   // primitives (strings/ints) — leave for the control's own templating

            var dt = new System.Data.DataTable();
            foreach (var cn in cols) dt.Columns.Add(cn, typeof(object));
            foreach (var r in rows)
            {
                var row = dt.NewRow();
                var d = r as System.Collections.IDictionary;
                var ps = d == null ? System.Management.Automation.PSObject.AsPSObject(r) : null;
                foreach (var cn in cols)
                {
                    object cv = null;
                    if (d != null) { if (d.Contains(cn)) cv = d[cn]; }
                    else { var pp = ps.Properties[cn]; if (pp != null) { try { cv = pp.Value; } catch { } } }
                    row[cn] = cv ?? (object)System.DBNull.Value;
                }
                dt.Rows.Add(row);
            }
            return dt.DefaultView;
        }

        // ── Motion ──────────────────────────────────────────────────────────────────
        /// <summary>Animates a double property (Opacity/Width/Height/X/Y) on a named control with easing.</summary>
        public void Animate(string name, string property, object to, object from, double durationMs, string easing)
        {
            _ui.Invoke((Action)(() =>
            {
                CanvasControl cc;
                if (!_byName.TryGetValue(name ?? "", out cc) || cc.Element == null) { Diag("Set-UICanvasAnimate: no control named '" + name + "'."); return; }
                System.Windows.DependencyProperty dp; bool isCanvasPos;
                if (!ResolveAnimatable(property, out dp, out isCanvasPos)) { Diag("Set-UICanvasAnimate: property '" + property + "' is not animatable (use Opacity/Width/Height/X/Y)."); return; }
                AnimateDouble(cc.Element, dp, AsDbl(to, 0), from == null ? (double?)null : AsDbl(from, 0), durationMs, easing);
            }));
        }

        private static bool ResolveAnimatable(string property, out System.Windows.DependencyProperty dp, out bool isCanvasPos)
        {
            isCanvasPos = false; dp = null;
            switch ((property ?? "").ToLowerInvariant())
            {
                case "opacity": dp = UIElement.OpacityProperty; return true;
                case "width": dp = FrameworkElement.WidthProperty; return true;
                case "height": dp = FrameworkElement.HeightProperty; return true;
                case "x": case "left": dp = Canvas.LeftProperty; isCanvasPos = true; return true;
                case "y": case "top": dp = Canvas.TopProperty; isCanvasPos = true; return true;
                default: return false;
            }
        }

        private static void AnimateDouble(FrameworkElement el, System.Windows.DependencyProperty dp, double to, double? from, double durationMs, string easing)
        {
            var anim = new System.Windows.Media.Animation.DoubleAnimation
            {
                To = to,
                Duration = new System.Windows.Duration(System.TimeSpan.FromMilliseconds(durationMs <= 0 ? 1 : durationMs)),
                EasingFunction = Ease(easing)
            };
            if (from.HasValue) anim.From = from.Value;
            el.BeginAnimation(dp, anim);
        }

        private static System.Windows.Media.Animation.IEasingFunction Ease(string name)
        {
            var mode = System.Windows.Media.Animation.EasingMode.EaseOut;
            string n = (name ?? "").ToLowerInvariant();
            if (n.EndsWith("in")) mode = System.Windows.Media.Animation.EasingMode.EaseIn;
            else if (n.EndsWith("inout")) mode = System.Windows.Media.Animation.EasingMode.EaseInOut;
            if (n.StartsWith("linear") || n == "") return null;
            if (n.StartsWith("back")) return new System.Windows.Media.Animation.BackEase { EasingMode = mode };
            if (n.StartsWith("bounce")) return new System.Windows.Media.Animation.BounceEase { EasingMode = mode };
            if (n.StartsWith("elastic")) return new System.Windows.Media.Animation.ElasticEase { EasingMode = mode };
            if (n.StartsWith("quad")) return new System.Windows.Media.Animation.QuadraticEase { EasingMode = mode };
            if (n.StartsWith("sine")) return new System.Windows.Media.Animation.SineEase { EasingMode = mode };
            return new System.Windows.Media.Animation.CubicEase { EasingMode = mode };   // default
        }

        // ── Async (non-blocking) ─────────────────────────────────────────────────────
        /// <summary>Runs a scriptblock on its OWN runspace + thread (not the gated UI runspace) so long work
        /// doesn't freeze the UI. The injected runtime cmdlets still marshal updates back to the UI thread.</summary>
        public void RunAsync(string script)
        {
            Task.Run(() =>
            {
                Runspace rs = null;
                try
                {
                    rs = RunspaceFactory.CreateRunspace();
                    rs.ApartmentState = ApartmentState.STA;
                    rs.ThreadOptions = PSThreadOptions.ReuseThread;   // runspace owns its own STA thread (we're on an MTA Task thread)
                    rs.Open();
                    rs.SessionStateProxy.SetVariable("__PoshUICanvasBridge", this);
                    using (var ps = PowerShell.Create())
                    {
                        ps.Runspace = rs;
                        ps.AddScript(Bootstrap + "\n" + script);
                        ps.Invoke();
                        if (ps.HadErrors)
                            foreach (var er in ps.Streams.Error) Diag("Start-UICanvasAsync error: " + er.ToString());
                    }
                }
                catch (Exception ex) { Diag("Start-UICanvasAsync failed: " + ex.Message); }
                finally { try { if (rs != null) { rs.Close(); rs.Dispose(); } } catch { } }
            });
        }

        // ── Toasts & dialogs ─────────────────────────────────────────────────────────
        /// <summary>Transient notification that fades in over the window, waits, and fades out.</summary>
        public void Toast(string message, string severity, int durationMs)
        {
            _ui.BeginInvoke((Action)(() =>
            {
                var win = System.Windows.Application.Current != null ? System.Windows.Application.Current.MainWindow : null;
                var root = win != null ? win.Content as Panel : null;
                if (root == null) return;
                string bg, fg; ToastColors(severity, out bg, out fg);
                var border = new Border
                {
                    Background = Brush(bg),
                    CornerRadius = new CornerRadius(8),
                    Padding = new Thickness(18, 12, 18, 12),
                    Margin = new Thickness(0, 0, 0, 36),
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Bottom,
                    Opacity = 0,
                    Effect = new System.Windows.Media.Effects.DropShadowEffect { BlurRadius = 18, ShadowDepth = 2, Opacity = 0.5, Color = System.Windows.Media.Colors.Black }
                };
                border.Child = new TextBlock { Text = message ?? "", Foreground = Brush(fg), FontSize = 13, FontWeight = FontWeights.SemiBold };
                var g = root as Grid;
                if (g != null) { Grid.SetRowSpan(border, Math.Max(1, g.RowDefinitions.Count)); Grid.SetColumnSpan(border, Math.Max(1, g.ColumnDefinitions.Count)); }
                Panel.SetZIndex(border, 99999);
                root.Children.Add(border);
                border.BeginAnimation(UIElement.OpacityProperty, new System.Windows.Media.Animation.DoubleAnimation(0, 1, new Duration(TimeSpan.FromMilliseconds(180))));
                var timer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(durationMs <= 0 ? 3000 : durationMs) };
                timer.Tick += (s, e) =>
                {
                    timer.Stop();
                    var fout = new System.Windows.Media.Animation.DoubleAnimation(1, 0, new Duration(TimeSpan.FromMilliseconds(220)));
                    fout.Completed += (s2, e2) => { try { root.Children.Remove(border); } catch { } };
                    border.BeginAnimation(UIElement.OpacityProperty, fout);
                };
                timer.Start();
            }));
        }

        private static void ToastColors(string severity, out string bg, out string fg)
        {
            switch ((severity ?? "").ToLowerInvariant())
            {
                case "success": case "ok": bg = "#14322A"; fg = "#34D399"; break;
                case "warning": bg = "#3A2E14"; fg = "#FBBF24"; break;
                case "error": case "danger": bg = "#3A1E1E"; fg = "#F87171"; break;
                default: bg = "#1E2942"; fg = "#93C5FD"; break;
            }
        }

        /// <summary>Modal confirm/prompt dialog. Returns the entered text (prompt+OK), $true (confirm+OK), or $null (cancel).</summary>
        public object Dialog(string message, string title, bool prompt, string defaultValue, string okLabel, string cancelLabel)
        {
            return _ui.Invoke((Func<object>)(() =>
            {
                var owner = System.Windows.Application.Current != null ? System.Windows.Application.Current.MainWindow : null;
                var dlg = new Window
                {
                    Title = title ?? "Confirm",
                    Width = 440,
                    SizeToContent = SizeToContent.Height,
                    WindowStartupLocation = WindowStartupLocation.CenterOwner,
                    ResizeMode = ResizeMode.NoResize,
                    ShowInTaskbar = false,
                    Owner = owner,
                    Background = Brush("#141A2E")
                };
                var panel = new StackPanel { Margin = new Thickness(22) };
                panel.Children.Add(new TextBlock { Text = message ?? "", Foreground = Brush("#E5E7EB"), TextWrapping = TextWrapping.Wrap, FontSize = 13, Margin = new Thickness(0, 0, 0, 16) });
                TextBox input = null;
                if (prompt)
                {
                    input = new TextBox { Text = defaultValue ?? "", Padding = new Thickness(7, 5, 7, 5), Margin = new Thickness(0, 0, 0, 16), MinHeight = 30 };
                    input.SetResourceReference(Control.BackgroundProperty, "TextBoxBackgroundBrush");
                    input.SetResourceReference(Control.ForegroundProperty, "BodyForegroundBrush");
                    input.SetResourceReference(Control.BorderBrushProperty, "BorderBrush");
                    panel.Children.Add(input);
                }
                var bar = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Right };
                var ok = new Button { Content = okLabel ?? "OK", MinWidth = 84, Margin = new Thickness(0, 0, 8, 0), Padding = new Thickness(14, 6, 14, 6), IsDefault = true };
                var cancel = new Button { Content = cancelLabel ?? "Cancel", MinWidth = 84, Padding = new Thickness(14, 6, 14, 6), IsCancel = true };
                var okStyle = System.Windows.Application.Current.TryFindResource("CanvasPrimaryButtonStyle") as Style; if (okStyle != null) ok.Style = okStyle;
                var caStyle = System.Windows.Application.Current.TryFindResource("CanvasSecondaryButtonStyle") as Style; if (caStyle != null) cancel.Style = caStyle;
                object result = null;
                ok.Click += (s, e) => { result = prompt ? (object)(input != null ? input.Text : "") : (object)true; dlg.DialogResult = true; };
                cancel.Click += (s, e) => { result = null; dlg.DialogResult = false; };
                bar.Children.Add(ok); bar.Children.Add(cancel);
                panel.Children.Add(bar);
                dlg.Content = panel;
                dlg.ShowDialog();
                return result;
            }));
        }

        /// <summary>Lightweight anchored flyout: an optional title/message plus clickable items, anchored to a named
        /// control (dismisses on click-away). Returns the chosen item, or $null if dismissed. Synchronous.</summary>
        public object Flyout(string targetName, string placement, string title, string message, string[] items)
        {
            return _ui.Invoke((Func<object>)(() =>
            {
                UIElement target = null;
                CanvasControl cc;
                if (!string.IsNullOrEmpty(targetName) && _byName.TryGetValue(targetName, out cc)) target = cc.Element as UIElement;

                var stack = new StackPanel();
                if (!string.IsNullOrEmpty(title))
                {
                    var t = new TextBlock { Text = title, FontWeight = FontWeights.SemiBold, FontSize = 13, Margin = new Thickness(10, 8, 10, 4) };
                    t.SetResourceReference(TextBlock.ForegroundProperty, "BodyForegroundBrush");
                    stack.Children.Add(t);
                }
                if (!string.IsNullOrEmpty(message))
                {
                    var m = new TextBlock { Text = message, TextWrapping = TextWrapping.Wrap, FontSize = 12.5, Margin = new Thickness(10, 2, 10, 8), MaxWidth = 280 };
                    m.SetResourceReference(TextBlock.ForegroundProperty, "SecondaryForegroundBrush");
                    stack.Children.Add(m);
                }

                var card = new Border { CornerRadius = new CornerRadius(8), BorderThickness = new Thickness(1), Padding = new Thickness(4), MinWidth = 180 };
                card.SetResourceReference(Border.BackgroundProperty, "CardBackgroundBrush");
                card.SetResourceReference(Border.BorderBrushProperty, "BorderBrush");
                card.Effect = new System.Windows.Media.Effects.DropShadowEffect { BlurRadius = 18, ShadowDepth = 3, Opacity = 0.5, Color = Colors.Black };
                card.Child = stack;

                var popup = new System.Windows.Controls.Primitives.Popup
                {
                    AllowsTransparency = true,
                    StaysOpen = false,
                    PlacementTarget = target,
                    Placement = target != null ? ParsePlacement(placement) : System.Windows.Controls.Primitives.PlacementMode.Mouse,
                    VerticalOffset = 4,
                    Child = card
                };

                object result = null;
                var frame = new DispatcherFrame();
                if (items != null)
                    foreach (var it in items)
                    {
                        if (it == null) continue;
                        var captured = it;
                        var btn = new Button { Content = it, HorizontalContentAlignment = HorizontalAlignment.Left, Padding = new Thickness(10, 7, 16, 7), Margin = new Thickness(2, 1, 2, 1), BorderThickness = new Thickness(0), Background = System.Windows.Media.Brushes.Transparent, Cursor = System.Windows.Input.Cursors.Hand, MinWidth = 170 };
                        btn.SetResourceReference(Control.ForegroundProperty, "BodyForegroundBrush");
                        btn.MouseEnter += (s, e) => btn.Background = Brush("#1B2740");
                        btn.MouseLeave += (s, e) => btn.Background = System.Windows.Media.Brushes.Transparent;
                        btn.Click += (s, e) => { result = captured; popup.IsOpen = false; };
                        stack.Children.Add(btn);
                    }

                popup.Closed += (s, e) => frame.Continue = false;
                popup.IsOpen = true;
                Dispatcher.PushFrame(frame);   // pump until dismissed -> synchronous return
                return result;
            }));
        }

        private static System.Windows.Controls.Primitives.PlacementMode ParsePlacement(string p)
        {
            switch ((p ?? "bottom").ToLowerInvariant())
            {
                case "top": return System.Windows.Controls.Primitives.PlacementMode.Top;
                case "left": return System.Windows.Controls.Primitives.PlacementMode.Left;
                case "right": return System.Windows.Controls.Primitives.PlacementMode.Right;
                case "mouse": return System.Windows.Controls.Primitives.PlacementMode.Mouse;
                default: return System.Windows.Controls.Primitives.PlacementMode.Bottom;
            }
        }

        /// <summary>Opens a secondary window from a WindowTemplate control (defined via New-UICanvasWindow): builds
        /// its captured content through the factory, registers it (so actions/state/console work), and shows it.</summary>
        public void OpenWindow(string name)
        {
            _ui.Invoke((Action)(() =>
            {
                CanvasControl tmpl;
                if (!_byName.TryGetValue(name ?? "", out tmpl) || tmpl.Def == null) { Diag("Show-UICanvasWindow: no window named '" + name + "'."); return; }
                var root = DeserializeControl(PropStr(tmpl, "ContentJson"));
                if (root == null) return;
                CanvasControl cc;
                try { cc = CanvasControlFactory.Build(root, (n, ev) => RunEvent(n, ev)); }
                catch (Exception ex) { Diag("Show-UICanvasWindow build failed: " + ex.Message); return; }
                RegisterTree(cc);

                bool noScroll = Truthy(PropStr(tmpl, "NoScroll"));
                object body;
                if (noScroll) { body = cc.Element; }
                else
                {
                    var sv = new ScrollViewer { Content = cc.Element, VerticalScrollBarVisibility = ScrollBarVisibility.Auto, HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled };
                    CanvasControlFactory.ApplyCanvasScroll(sv);   // thin, auto-hiding, semi-transparent scrollbar
                    body = sv;
                }
                bool modal = Truthy(PropStr(tmpl, "Modal"));
                var win = new Window
                {
                    Title = string.IsNullOrEmpty(PropStr(tmpl, "Title")) ? "PoshUI" : PropStr(tmpl, "Title"),
                    Width = AsDbl(PropStr(tmpl, "Width"), 640),
                    Height = AsDbl(PropStr(tmpl, "Height"), 480),
                    MinWidth = AsDbl(PropStr(tmpl, "MinWidth"), 320),
                    MinHeight = AsDbl(PropStr(tmpl, "MinHeight"), 200),
                    Owner = System.Windows.Application.Current != null ? System.Windows.Application.Current.MainWindow : null,
                    ShowInTaskbar = false,
                    ResizeMode = Truthy(PropStr(tmpl, "Resizable"), true) ? ResizeMode.CanResize : ResizeMode.NoResize,
                    Topmost = Truthy(PropStr(tmpl, "Topmost")),
                    Content = (System.Windows.UIElement)body
                };
                win.SetResourceReference(Control.BackgroundProperty, "ContentBackgroundBrush");

                // Startup position
                switch ((PropStr(tmpl, "Position") ?? "CenterOwner").ToLowerInvariant())
                {
                    case "centerscreen": win.WindowStartupLocation = WindowStartupLocation.CenterScreen; break;
                    case "manual": win.WindowStartupLocation = WindowStartupLocation.Manual; win.Left = AsDbl(PropStr(tmpl, "X"), 100); win.Top = AsDbl(PropStr(tmpl, "Y"), 100); break;
                    default: win.WindowStartupLocation = WindowStartupLocation.CenterOwner; break;
                }
                // Window icon
                string iconPath = PropStr(tmpl, "Icon");
                if (!string.IsNullOrEmpty(iconPath) && System.IO.File.Exists(iconPath))
                    try { win.Icon = new System.Windows.Media.Imaging.BitmapImage(new Uri(iconPath, UriKind.Absolute)); } catch { }
                // Chromeless: no OS title bar; keep drag/resize via WindowChrome (author supplies a close control).
                if (Truthy(PropStr(tmpl, "HideTitleBar")))
                {
                    win.WindowStyle = WindowStyle.None;
                    System.Windows.Shell.WindowChrome.SetWindowChrome(win, new System.Windows.Shell.WindowChrome
                    { CaptionHeight = 36, ResizeBorderThickness = new Thickness(6), GlassFrameThickness = new Thickness(0), CornerRadius = new CornerRadius(0) });
                }

                // Tint the OS title bar (Win11 DWM) to match the theme, when a colour is given and not chromeless.
                string tbColor = PropStr(tmpl, "TitleBarColor");
                if (!string.IsNullOrEmpty(tbColor) && !Truthy(PropStr(tmpl, "HideTitleBar")))
                {
                    string tbText = PropStr(tmpl, "TitleBarText");
                    win.SourceInitialized += (s, e) => ApplyCaptionColor(win, tbColor, tbText);
                }

                _windows.Add(win);
                win.Closed += (s, e) => { _windows.Remove(win); UnregisterTree(cc); };
                if (modal) win.ShowDialog(); else win.Show();
            }));
        }

        [System.Runtime.InteropServices.DllImport("dwmapi.dll")]
        private static extern int DwmSetWindowAttribute(IntPtr hwnd, int attr, ref int value, int size);

        /// <summary>Sets the OS caption (and optional text) colour on Windows 11 via DWM. No-op on older Windows.</summary>
        private static void ApplyCaptionColor(Window win, string captionHex, string textHex)
        {
            try
            {
                var hwnd = new System.Windows.Interop.WindowInteropHelper(win).Handle;
                if (hwnd == IntPtr.Zero) return;
                int cap = ColorRef(captionHex);
                if (cap >= 0) { int v = cap; DwmSetWindowAttribute(hwnd, 35 /*DWMWA_CAPTION_COLOR*/, ref v, sizeof(int)); }
                int txt = ColorRef(textHex);
                if (txt >= 0) { int v = txt; DwmSetWindowAttribute(hwnd, 36 /*DWMWA_TEXT_COLOR*/, ref v, sizeof(int)); }
            }
            catch { }
        }

        /// <summary>#RRGGBB -> Win32 COLORREF (0x00BBGGRR), or -1 if not a valid hex colour.</summary>
        private static int ColorRef(string hex)
        {
            if (string.IsNullOrEmpty(hex)) return -1;
            var c = hex.TrimStart('#');
            if (c.Length != 6) return -1;
            try
            {
                int r = Convert.ToInt32(c.Substring(0, 2), 16);
                int g = Convert.ToInt32(c.Substring(2, 2), 16);
                int b = Convert.ToInt32(c.Substring(4, 2), 16);
                return (b << 16) | (g << 8) | r;
            }
            catch { return -1; }
        }

        /// <summary>Closes the most-recently-opened secondary window (from a Close-UICanvasWindow action).</summary>
        public void CloseWindow()
        {
            _ui.Invoke((Action)(() =>
            {
                if (_windows.Count == 0) return;
                var win = _windows[_windows.Count - 1];
                try { win.Close(); } catch { }
            }));
        }

        /// <summary>Loose truthiness for serialized bool props ("True"/"1"/"true").</summary>
        private static bool Truthy(string s, bool dflt = false)
        {
            if (string.IsNullOrEmpty(s)) return dflt;
            return string.Equals(s, "True", StringComparison.OrdinalIgnoreCase) || s == "1";
        }

        private static Launcher.Models.UIControlJson DeserializeControl(string json)
        {
            if (string.IsNullOrWhiteSpace(json)) return null;
            try
            {
                var settings = new System.Runtime.Serialization.Json.DataContractJsonSerializerSettings { UseSimpleDictionaryFormat = true };
                var ser = new System.Runtime.Serialization.Json.DataContractJsonSerializer(typeof(Launcher.Models.UIControlJson), settings);
                using (var ms = new System.IO.MemoryStream(System.Text.Encoding.UTF8.GetBytes(json)))
                    return (Launcher.Models.UIControlJson)ser.ReadObject(ms);
            }
            catch { return null; }
        }

        private void RegisterTree(CanvasControl cc)
        {
            if (cc == null) return;
            Register(cc);
            if (cc.Children != null) foreach (var ch in cc.Children) RegisterTree(ch);
        }

        private void UnregisterTree(CanvasControl cc)
        {
            if (cc == null) return;
            if (!string.IsNullOrEmpty(cc.Name)) _byName.Remove(cc.Name);
            if (cc.Children != null) foreach (var ch in cc.Children) UnregisterTree(ch);
        }

        // ── Reactive state engine ────────────────────────────────────────────────────
        /// <summary>Set a state key; fans out to every bound control + template + watcher. From an action (bg thread).</summary>
        public void SetState(string name, object value) { _ui.Invoke((Action)(() => SetStateCore(name ?? "", value, null))); }
        public object GetState(string name) { return _ui.Invoke((Func<object>)(() => { object v; return _state.TryGetValue(name ?? "", out v) ? v : null; })); }

        private void SeedState(string json)
        {
            if (string.IsNullOrWhiteSpace(json)) return;
            try
            {
                var d = CanvasControlFactory.MiniJson.Parse(json) as System.Collections.IDictionary;
                if (d != null) foreach (System.Collections.DictionaryEntry e in d) _state[Str(e.Key)] = e.Value;
            }
            catch { Diag("New-UICanvasState: could not parse initial state."); }
        }

        private void RegisterKeyBind(CanvasControl cc, string key)
        {
            List<CanvasControl> list;
            if (!_bindKey.TryGetValue(key, out list)) { list = new List<CanvasControl>(); _bindKey[key] = list; }
            list.Add(cc);
            object v;
            if (_state.TryGetValue(key, out v) && cc.SetValue != null) { _inBindPush = true; try { cc.SetValue(v); } finally { _inBindPush = false; } }
            HookTwoWay(cc, key);
        }

        private void RegisterItemsBind(CanvasControl cc, string key)
        {
            List<CanvasControl> list;
            if (!_bindItems.TryGetValue(key, out list)) { list = new List<CanvasControl>(); _bindItems[key] = list; }
            list.Add(cc);
            object v;
            if (_state.TryGetValue(key, out v)) SetItemsSource(cc, v);
        }

        private static void SetItemsSource(CanvasControl cc, object value)
        {
            var ic = cc.Element as ItemsControl ?? FindItemsControl(cc.Element);
            if (ic != null) ic.ItemsSource = CoerceItemsSource(value) as System.Collections.IEnumerable;
        }

        /// <summary>Finds the first ItemsControl in the LOGICAL subtree (built at construction, so it works before
        /// the element is rendered) — the grid may be wrapped, e.g. with an empty-state overlay.</summary>
        private static ItemsControl FindItemsControl(System.Windows.DependencyObject root)
        {
            if (root == null) return null;
            foreach (var child in System.Windows.LogicalTreeHelper.GetChildren(root))
            {
                var dep = child as System.Windows.DependencyObject; if (dep == null) continue;
                var ic = dep as ItemsControl; if (ic != null) return ic;
                var deep = FindItemsControl(dep); if (deep != null) return deep;
            }
            return null;
        }

        private void RegisterTemplate(CanvasControl cc, string template)
        {
            var keys = new List<string>();
            foreach (System.Text.RegularExpressions.Match m in System.Text.RegularExpressions.Regex.Matches(template, @"\{(\w+)\}")) keys.Add(m.Groups[1].Value);
            _bindTemplate.Add(Tuple.Create(cc, template, keys));
            if (cc.SetValue != null) cc.SetValue(RenderTemplate(template));
        }

        private string RenderTemplate(string template)
        {
            return System.Text.RegularExpressions.Regex.Replace(template, @"\{(\w+)\}", m => { object v; return _state.TryGetValue(m.Groups[1].Value, out v) ? Str(v) : ""; });
        }

        private void HookTwoWay(CanvasControl cc, string key)
        {
            var el = cc.Element;
            if (el is TextBox tb) tb.TextChanged += (s, e) => OnControlChanged(key, cc);
            else if (el is ComboBox cmb) cmb.SelectionChanged += (s, e) => OnControlChanged(key, cc);
            else if (el is CheckBox chk) { chk.Checked += (s, e) => OnControlChanged(key, cc); chk.Unchecked += (s, e) => OnControlChanged(key, cc); }
            else if (el is Slider sl) sl.ValueChanged += (s, e) => OnControlChanged(key, cc);
            else if (el is System.Windows.Controls.Primitives.ToggleButton tg) { tg.Checked += (s, e) => OnControlChanged(key, cc); tg.Unchecked += (s, e) => OnControlChanged(key, cc); }
            else if (el is ListBox lb) lb.SelectionChanged += (s, e) => OnControlChanged(key, cc);
            else if (el is DatePicker dp) dp.SelectedDateChanged += (s, e) => OnControlChanged(key, cc);
        }

        private void OnControlChanged(string key, CanvasControl cc)
        {
            if (_inBindPush || cc.GetValue == null) return;
            SetStateCore(key, cc.GetValue(), cc);
        }

        private void SetStateCore(string key, object value, CanvasControl source)
        {
            _state[key] = value;
            List<CanvasControl> list;
            if (_bindKey.TryGetValue(key, out list))
            {
                _inBindPush = true;
                try { foreach (var b in list) if (!ReferenceEquals(b, source) && b.SetValue != null) b.SetValue(value); }
                finally { _inBindPush = false; }
            }
            foreach (var t in _bindTemplate)
            {
                bool refs = false;
                foreach (var k in t.Item3) if (string.Equals(k, key, StringComparison.OrdinalIgnoreCase)) { refs = true; break; }
                if (refs && t.Item1.SetValue != null) t.Item1.SetValue(RenderTemplate(t.Item2));
            }
            List<CanvasControl> ilist;
            if (_bindItems.TryGetValue(key, out ilist))
                foreach (var b in ilist) SetItemsSource(b, value);   // collection bound -> rebuild rows (auto add/remove)
            foreach (var w in _stateWatchers) if (string.Equals(w.Item1, key, StringComparison.OrdinalIgnoreCase)) RunEvent(w.Item2, "Clicked");
        }

        public void NavigateToPage(string page)
        {
            var nav = PageNavigator;
            if (nav != null) _ui.Invoke((Action)(() => { CaptureValues(); nav(page); }));
        }

        /// <summary>True when running in a minimal environment (e.g. a preinstallation/recovery OS) where the
        /// Win32 shell (comdlg32/shell32) that backs the native folder/file dialogs is unavailable — so we must
        /// use the WPF-drawn picker instead.</summary>
        private static bool IsMinimalShellEnvironment()
        {
            try
            {
                if (string.Equals(Environment.GetEnvironmentVariable("SystemDrive"), "X:", StringComparison.OrdinalIgnoreCase))
                    return true;
                using (var k = Microsoft.Win32.Registry.LocalMachine.OpenSubKey(@"SYSTEM\CurrentControlSet\Control\MiniNT"))
                    if (k != null) return true;
            }
            catch { }
            return false;
        }

        /// <summary>Opens a folder-browser dialog (on the UI thread) and returns the chosen path, or null.
        /// Uses the native shell dialog on a full OS; falls back to a WPF-drawn browser when the shell is unavailable.</summary>
        public string SelectFolder(string description)
        {
            return (string)_ui.Invoke((Func<string>)(() =>
            {
                if (!IsMinimalShellEnvironment())
                {
                    try
                    {
                        using (var dlg = new System.Windows.Forms.FolderBrowserDialog())
                        {
                            dlg.Description = string.IsNullOrEmpty(description) ? "Select a folder" : description;
                            dlg.ShowNewFolderButton = false;
                            return dlg.ShowDialog() == System.Windows.Forms.DialogResult.OK ? dlg.SelectedPath : null;
                        }
                    }
                    catch (Exception ex) { LoggingService.Warn("SelectFolder native dialog failed, using WPF picker: " + ex.Message, component: "CanvasBridge"); }
                }
                return WpfBrowse(true, description, null);
            }));
        }

        /// <summary>Opens an open-file dialog (on the UI thread) and returns the chosen path, or null.
        /// Uses the native shell dialog on a full OS; falls back to a WPF-drawn browser when the shell is unavailable.</summary>
        public string SelectFile(string title, string filter)
        {
            return (string)_ui.Invoke((Func<string>)(() =>
            {
                if (IsMinimalShellEnvironment()) return WpfBrowse(false, title, filter);
                try
                {
                    var dlg = new Microsoft.Win32.OpenFileDialog
                    {
                        Title = string.IsNullOrEmpty(title) ? "Select a file" : title,
                        Filter = string.IsNullOrEmpty(filter) ? "All files (*.*)|*.*" : filter,
                        CheckFileExists = true
                    };
                    return dlg.ShowDialog() == true ? dlg.FileName : null;
                }
                catch (Exception ex) { LoggingService.Warn("SelectFile native dialog failed, using WPF picker: " + ex.Message, component: "CanvasBridge"); return WpfBrowse(false, title, filter); }
            }));
        }

        // ── WPF-drawn file/folder picker (shell-free; works in minimal environments) ─────────
        private sealed class BrowseEntry
        {
            public string Path;
            public bool IsDir;
            public bool IsUp;
            public string Display;
            public override string ToString() { return Display; }
        }

        /// <summary>Parses a Win32-style filter ("desc|*.wim;*.esd|All|*.*") into an extension set.
        /// Returns null when any pattern matches all files (*.* / *).</summary>
        private static HashSet<string> ParseFilterExts(string filter)
        {
            if (string.IsNullOrEmpty(filter)) return null;
            var parts = filter.Split('|');
            var set = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            for (int i = 1; i < parts.Length; i += 2)
            {
                foreach (var patRaw in parts[i].Split(';'))
                {
                    var pat = patRaw.Trim();
                    if (pat == "*.*" || pat == "*" || pat.Length == 0) return null;
                    int dot = pat.LastIndexOf('.');
                    if (dot >= 0) set.Add(pat.Substring(dot));
                }
            }
            return set.Count > 0 ? set : null;
        }

        /// <summary>Must be called on the UI thread. Shows a modal, dark-themed WPF browser over the file
        /// system (drives → folders → optionally files) and returns the chosen path, or null if cancelled.</summary>
        private string WpfBrowse(bool foldersOnly, string title, string filter)
        {
            try
            {
                var exts = ParseFilterExts(filter);
                Func<System.Windows.Media.Color, System.Windows.Media.Brush> B =
                    c => new System.Windows.Media.SolidColorBrush(c);
                Func<string, System.Windows.Media.Brush> Hex =
                    s => B((System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString(s));

                var win = new Window
                {
                    Title = string.IsNullOrEmpty(title) ? (foldersOnly ? "Select a folder" : "Select a file") : title,
                    Width = 660,
                    Height = 540,
                    WindowStartupLocation = WindowStartupLocation.CenterOwner,
                    Background = Hex("#0A0E1A"),
                    Foreground = Hex("#E5E7EB"),
                    ShowInTaskbar = false
                };
                try { var o = System.Windows.Application.Current != null ? System.Windows.Application.Current.MainWindow : null; if (o != null && o != win) win.Owner = o; } catch { }

                var root = new DockPanel { Margin = new Thickness(12) };

                var pathBox = new TextBox { Foreground = Hex("#E5E7EB"), Background = Hex("#141A2E"), BorderBrush = Hex("#1E2942"), Padding = new Thickness(6, 3, 6, 3), VerticalContentAlignment = VerticalAlignment.Center };
                var upBtn = new Button { Content = "Up", Width = 60, Margin = new Thickness(0, 0, 8, 0) };
                var top = new DockPanel { Margin = new Thickness(0, 0, 0, 8) };
                DockPanel.SetDock(top, Dock.Top);
                DockPanel.SetDock(upBtn, Dock.Left);
                top.Children.Add(upBtn);
                top.Children.Add(pathBox);

                var selLabel = new TextBlock { Foreground = Hex("#94A3B8"), VerticalAlignment = VerticalAlignment.Center, TextTrimming = TextTrimming.CharacterEllipsis };
                var okBtn = new Button { Content = "Select", Width = 90, Margin = new Thickness(8, 0, 0, 0), IsDefault = true };
                var cancelBtn = new Button { Content = "Cancel", Width = 90, Margin = new Thickness(8, 0, 0, 0), IsCancel = true };
                var bottom = new DockPanel { Margin = new Thickness(0, 8, 0, 0) };
                DockPanel.SetDock(bottom, Dock.Bottom);
                DockPanel.SetDock(cancelBtn, Dock.Right);
                DockPanel.SetDock(okBtn, Dock.Right);
                bottom.Children.Add(cancelBtn);
                bottom.Children.Add(okBtn);
                bottom.Children.Add(selLabel);

                var list = new ListBox { Background = Hex("#0E1424"), Foreground = Hex("#E5E7EB"), BorderBrush = Hex("#1E2942") };

                root.Children.Add(top);
                root.Children.Add(bottom);
                root.Children.Add(list);
                win.Content = root;

                string current = null;         // null => drive list
                string chosen = null;

                Action<string> load = null;
                load = dir =>
                {
                    list.Items.Clear();
                    current = dir;
                    pathBox.Text = dir ?? "";
                    if (string.IsNullOrEmpty(dir))
                    {
                        foreach (var dv in System.IO.DriveInfo.GetDrives())
                        {
                            try { if (!dv.IsReady) continue; } catch { continue; }
                            list.Items.Add(new BrowseEntry { Path = dv.RootDirectory.FullName, IsDir = true, Display = "[" + dv.Name.TrimEnd('\\') + "]  " + dv.RootDirectory.FullName });
                        }
                        return;
                    }
                    list.Items.Add(new BrowseEntry { Path = null, IsUp = true, IsDir = true, Display = ".." });
                    try
                    {
                        foreach (var d in System.IO.Directory.GetDirectories(dir))
                            list.Items.Add(new BrowseEntry { Path = d, IsDir = true, Display = "[DIR]  " + System.IO.Path.GetFileName(d) });
                    }
                    catch (Exception ex) { LoggingService.Warn("browse dirs: " + ex.Message, component: "CanvasBridge"); }
                    if (!foldersOnly)
                    {
                        try
                        {
                            foreach (var f in System.IO.Directory.GetFiles(dir))
                            {
                                if (exts != null && !exts.Contains(System.IO.Path.GetExtension(f))) continue;
                                list.Items.Add(new BrowseEntry { Path = f, IsDir = false, Display = "        " + System.IO.Path.GetFileName(f) });
                            }
                        }
                        catch (Exception ex) { LoggingService.Warn("browse files: " + ex.Message, component: "CanvasBridge"); }
                    }
                };

                Action<string> navUp = dir =>
                {
                    if (string.IsNullOrEmpty(dir)) return;
                    try
                    {
                        var parent = System.IO.Directory.GetParent(dir);
                        load(parent != null ? parent.FullName : null);   // null => drive list
                    }
                    catch { load(null); }
                };

                upBtn.Click += (s, e) => navUp(current);

                list.MouseDoubleClick += (s, e) =>
                {
                    var en = list.SelectedItem as BrowseEntry;
                    if (en == null) return;
                    if (en.IsUp) { navUp(current); return; }
                    if (en.IsDir) { load(en.Path); return; }
                    chosen = en.Path; win.DialogResult = true;   // double-click a file selects it
                };

                list.SelectionChanged += (s, e) =>
                {
                    var en = list.SelectedItem as BrowseEntry;
                    if (en == null || en.IsUp) { selLabel.Text = foldersOnly ? (current ?? "") : ""; return; }
                    selLabel.Text = en.Path ?? "";
                };

                pathBox.KeyDown += (s, e) =>
                {
                    if (e.Key != System.Windows.Input.Key.Enter) return;
                    var p = pathBox.Text != null ? pathBox.Text.Trim() : "";
                    if (System.IO.Directory.Exists(p)) load(p);
                    else if (!foldersOnly && System.IO.File.Exists(p)) { chosen = p; win.DialogResult = true; }
                };

                okBtn.Click += (s, e) =>
                {
                    var en = list.SelectedItem as BrowseEntry;
                    if (foldersOnly)
                    {
                        // A selected sub-folder wins; otherwise the folder currently shown.
                        if (en != null && en.IsDir && !en.IsUp && en.Path != null) chosen = en.Path;
                        else if (!string.IsNullOrEmpty(current)) chosen = current;
                        else { return; }   // still at the drive list, nothing to select
                        win.DialogResult = true;
                    }
                    else
                    {
                        if (en != null && !en.IsDir && en.Path != null) { chosen = en.Path; win.DialogResult = true; }
                    }
                };

                load(null);   // start at the drive list
                return (win.ShowDialog() == true) ? chosen : null;
            }
            catch (Exception ex) { LoggingService.Warn("WpfBrowse failed: " + ex.Message, component: "CanvasBridge"); return null; }
        }

        /// <summary>Enables/disables the no-backtracking lock and greys out rail buttons (named "nav_*").</summary>
        private bool _navLocked;

        public void SetNavLock(bool on)
        {
            _navLocked = on;
            _ui.Invoke((Action)(() =>
            {
                var f = NavLocker;
                if (f != null) f(on);
                // Visually disable nav-rail buttons (convention: names start with "nav_") so back-nav looks blocked.
                foreach (var kv in _byName)
                    if (kv.Key.StartsWith("nav_", StringComparison.OrdinalIgnoreCase) && kv.Value.Element != null)
                        kv.Value.Element.IsEnabled = !on;
            }));
        }

        public void Submit()
        {
            try
            {
                var values = CollectValues();
                ProtectSecrets(values);   // DPAPI-protect password-typed fields so they aren't plaintext at rest
                if (!string.IsNullOrEmpty(_definitionPath))
                {
                    var resultPath = System.IO.Path.ChangeExtension(_definitionPath, ".result.json");
                    System.IO.File.WriteAllText(resultPath, ToJson(values));
                    LoggingService.Info("Canvas submitted: " + values.Count + " values -> " + resultPath, component: "CanvasBridge");
                }
            }
            catch (Exception ex) { LoggingService.Error("Submit failed: " + ex.Message, component: "CanvasBridge"); }
            // BeginInvoke (not Invoke): don't block the runspace thread on the close, or disposing the runspace
            // during window-close (Shutdown) would deadlock against this in-flight action.
            var done = Submitter;
            if (done != null) _ui.BeginInvoke((Action)(() => done()));
        }

        // Shared entropy for canvas password DPAPI protection (must match the module's unprotect).
        private static readonly byte[] SecretEntropy = System.Text.Encoding.UTF8.GetBytes("PoshUI_Canvas_Secret_v1");
        private const string SecretPrefix = "PoshUISecure:";

        /// <summary>Replaces Password-typed control values with a DPAPI-protected (CurrentUser) marker token so
        /// they are never written to result.json in plaintext. The module unprotects them to a SecureString.</summary>
        private void ProtectSecrets(Dictionary<string, object> values)
        {
            var pwNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            _ui.Invoke((Action)(() =>
            {
                foreach (var kv in _byName)
                    if (kv.Value.Def != null && string.Equals(kv.Value.Def.Type, "Password", StringComparison.OrdinalIgnoreCase))
                        pwNames.Add(kv.Key);
            }));
            foreach (var name in pwNames)
            {
                object v;
                if (!values.TryGetValue(name, out v) || v == null) continue;
                string s = v.ToString();
                if (s.Length == 0) continue;
                try
                {
                    var prot = System.Security.Cryptography.ProtectedData.Protect(
                        System.Text.Encoding.UTF8.GetBytes(s), SecretEntropy,
                        System.Security.Cryptography.DataProtectionScope.CurrentUser);
                    values[name] = SecretPrefix + System.Convert.ToBase64String(prot);
                }
                catch (Exception ex)
                {
                    values[name] = SecretPrefix;   // fail closed — never leak the plaintext
                    LoggingService.Warn("Password protect failed for '" + name + "': " + ex.Message, component: "CanvasBridge");
                }
            }
        }

        public Dictionary<string, object> CollectValues()
        {
            return (Dictionary<string, object>)_ui.Invoke((Func<Dictionary<string, object>>)(() =>
            {
                // Merge values from every visited page (persistent store) with the current page's live controls.
                var d = new Dictionary<string, object>(_values, StringComparer.OrdinalIgnoreCase);
                foreach (var kv in _byName)
                    if (kv.Value.GetValue != null)
                    {
                        var v = kv.Value.GetValue();
                        if (v != null) d[kv.Key] = v;
                    }
                return d;
            }));
        }

        // ── Live value refresh ─────────────────────────────────────────────────────
        private void StartRefresh(CanvasControl cc)
        {
            if (cc.Def == null || cc.Def.Properties == null || cc.SetValue == null) return;
            object scriptObj, intervalObj;
            if (!cc.Def.Properties.TryGetValue("ValueScript", out scriptObj)) return;
            cc.Def.Properties.TryGetValue("RefreshInterval", out intervalObj);
            string script = Str(scriptObj);
            int interval = (int)AsDbl(intervalObj, 0);
            if (string.IsNullOrWhiteSpace(script) || interval <= 0) return;

            var target = cc;
            Action tick = () =>
            {
                Task.Run(() =>
                {
                    if (!_gate.Wait(0)) return;   // skip tick if busy
                    object result = null;
                    try
                    {
                        using (var ps = PowerShell.Create())
                        {
                            ps.Runspace = _runspace;
                            ps.AddScript(script);
                            var outp = ps.Invoke();
                            if (outp.Count > 0 && outp[outp.Count - 1] != null) result = outp[outp.Count - 1].BaseObject;
                        }
                    }
                    catch { }
                    finally { _gate.Release(); }
                    var r = result;
                    _ui.BeginInvoke((Action)(() => { if (target.SetValue != null) target.SetValue(r); }));
                });
            };
            var timer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(interval) };
            timer.Tick += (s, e) => tick();
            timer.Start();
            _timers.Add(timer);
            // Evaluate once immediately: without this, a freshly-navigated-to page shows the control's
            // static placeholder text (e.g. "-") for the full RefreshInterval before the first tick fires,
            // which reads as a multi-second lag (Target Node / Review summary going stale-then-populating).
            tick();
        }

        /// <summary>Self-ticking elapsed clock. A control with a `Clock` property (name of a control holding the
        /// run-start UTC ticks) updates itself as mm:ss on a UI-thread DispatcherTimer — in pure C#, with NO
        /// runspace call — so it keeps ticking even while a long step holds the runspace gate (unlike -Refresh,
        /// whose ValueScript can't run while the runspace is busy). Freezes once the optional `ClockStop`
        /// control (end ticks) is set. This is the engine-driven, decoupled timing the original workflow used.</summary>
        private void StartClock(CanvasControl cc)
        {
            if (cc.Def == null || cc.Def.Properties == null || cc.SetValue == null) return;
            object clockObj;
            if (!cc.Def.Properties.TryGetValue("Clock", out clockObj)) return;
            string startName = Str(clockObj);
            if (string.IsNullOrWhiteSpace(startName)) return;
            object stopObj; cc.Def.Properties.TryGetValue("ClockStop", out stopObj);
            string stopName = Str(stopObj);
            var target = cc;

            Action tick = () =>
            {
                // Runs on the UI thread (DispatcherTimer). No _gate, no runspace — ticks even during a busy step.
                long startTicks = ReadTicks(startName);
                if (startTicks <= 0) return;                       // run hasn't started yet
                long endTicks = string.IsNullOrEmpty(stopName) ? 0 : ReadTicks(stopName);
                long span = ((endTicks > startTicks) ? endTicks : DateTime.UtcNow.Ticks) - startTicks;
                if (span < 0) span = 0;
                var ts = TimeSpan.FromTicks(span);
                target.SetValue(string.Format("{0:00}:{1:00}", (int)ts.TotalMinutes, ts.Seconds));
            };
            var timer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(500) };
            timer.Tick += (s, e) => tick();
            timer.Start();
            _timers.Add(timer);
            tick();
        }

        /// <summary>Reads another control's value as UTC ticks (the workflow stamps its start/end there).</summary>
        private long ReadTicks(string name)
        {
            CanvasControl c;
            if (string.IsNullOrEmpty(name) || !_byName.TryGetValue(name, out c) || c == null || c.GetValue == null) return 0;
            var v = c.GetValue();
            if (v == null) return 0;
            long t;
            return long.TryParse(Str(v), NumberStyles.Any, CultureInfo.InvariantCulture, out t) ? t : 0;
        }

        public void Dispose()
        {
            foreach (var t in _timers) { try { t.Stop(); } catch { } }
            _timers.Clear();
            try { if (_runspace != null) _runspace.Dispose(); } catch { }
        }

        // ── helpers ────────────────────────────────────────────────────────────────
        private static string Str(object v) { return v == null ? "" : v.ToString(); }
        private static bool AsBool(object v) { if (v is bool b) return b; bool r; return v != null && bool.TryParse(v.ToString(), out r) && r; }
        private static double AsDbl(object v, double dflt) { if (v == null) return dflt; if (v is double d) return d; double p; return double.TryParse(v.ToString(), NumberStyles.Any, CultureInfo.InvariantCulture, out p) ? p : dflt; }
        private static Brush Brush(string hex) { if (string.IsNullOrEmpty(hex)) return null; try { return (Brush)new BrushConverter().ConvertFromString(hex.StartsWith("#") ? hex : "#" + hex); } catch { return null; } }

        private static string ToJson(Dictionary<string, object> d)
        {
            var sb = new StringBuilder("{");
            bool first = true;
            foreach (var kv in d)
            {
                if (!first) sb.Append(",");
                first = false;
                sb.Append("\"").Append(kv.Key.Replace("\"", "\\\"")).Append("\":");
                var v = kv.Value;
                if (v == null) sb.Append("null");
                else if (v is bool) sb.Append(((bool)v) ? "true" : "false");
                else if (v is int || v is long || v is double || v is float || v is decimal) sb.Append(Convert.ToString(v, CultureInfo.InvariantCulture));
                else sb.Append("\"").Append(v.ToString().Replace("\\", "\\\\").Replace("\"", "\\\"")).Append("\"");
            }
            sb.Append("}");
            return sb.ToString();
        }
    }
}

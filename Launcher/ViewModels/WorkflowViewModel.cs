// Copyright (c) 2025 A Solution IT LLC. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Windows.Input;
using System.Windows.Media;
using Launcher.Services;

namespace Launcher.ViewModels
{
    /// <summary>
    /// Represents the execution status of a workflow task.
    /// </summary>
    public enum WorkflowTaskStatus
    {
        NotStarted = 0,
        Running = 1,
        Completed = 2,
        Failed = 3,
        PendingReboot = 4,
        AwaitingApproval = 5,
        Skipped = 6
    }

    /// <summary>
    /// Represents the type of workflow task.
    /// </summary>
    public enum WorkflowTaskType
    {
        Normal = 0,
        ApprovalGate = 1
    }

    /// <summary>
    /// ViewModel for a single workflow task.
    /// </summary>
    public class WorkflowTaskViewModel : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        private string _name;
        private string _title;
        private string _description;
        private int _order;
        private string _icon;
        private WorkflowTaskType _taskType;
        private WorkflowTaskStatus _status;
        private int _progressPercent;
        private string _progressMessage;
        private string _errorMessage;
        private DateTime? _startTime;
        private DateTime? _endTime;
        private TimeSpan _duration;
        private bool _isExpanded;
        private bool _isIndeterminate = true;
        private bool _hasReportedProgress;

        // Approval gate properties
        private string _approvalMessage;
        private string _approveButtonText = "Approve";
        private string _rejectButtonText = "Reject";
        private bool _requireReason;
        private int _timeoutMinutes;
        private string _rejectionReason;
        private string _scriptBlockString;
        private string _scriptPath;
        private Dictionary<string, object> _arguments;
        private string _onError = "Stop";

        // Advanced task properties
        private int _retryCount = 0;
        private int _retryDelaySeconds = 5;
        private int _timeoutSeconds = 0;
        private string _skipCondition;
        private string _skipReason;
        private string _group;
        private string _rollbackScriptPath;
        private string _rollbackScriptBlock;

        public string Name
        {
            get { return _name; }
            set { _name = value; OnPropertyChanged("Name"); }
        }

        public string Title
        {
            get { return _title; }
            set { _title = value; OnPropertyChanged("Title"); }
        }

        public string Description
        {
            get { return _description; }
            set { _description = value; OnPropertyChanged("Description"); OnPropertyChanged("HasDescription"); }
        }

        public bool HasDescription { get { return !string.IsNullOrEmpty(_description); } }

        public int Order
        {
            get { return _order; }
            set { _order = value; OnPropertyChanged("Order"); }
        }

        public string Icon
        {
            get { return _icon; }
            set { _icon = value; OnPropertyChanged("Icon"); OnPropertyChanged("HasIcon"); }
        }

        public bool HasIcon { get { return !string.IsNullOrEmpty(_icon); } }

        public WorkflowTaskType TaskType
        {
            get { return _taskType; }
            set { _taskType = value; OnPropertyChanged("TaskType"); OnPropertyChanged("IsApprovalGate"); }
        }

        public bool IsApprovalGate { get { return _taskType == WorkflowTaskType.ApprovalGate; } }

        public string ScriptBlockString
        {
            get { return _scriptBlockString; }
            set { _scriptBlockString = value; OnPropertyChanged("ScriptBlockString"); }
        }

        public string ScriptPath
        {
            get { return _scriptPath; }
            set { _scriptPath = value; OnPropertyChanged("ScriptPath"); OnPropertyChanged("HasScriptPath"); }
        }

        public bool HasScriptPath { get { return !string.IsNullOrEmpty(_scriptPath); } }

        public Dictionary<string, object> Arguments
        {
            get { return _arguments; }
            set { _arguments = value; OnPropertyChanged("Arguments"); }
        }

        public WorkflowTaskStatus Status
        {
            get { return _status; }
            set
            {
                if (_status == value) return;
                _status = value;
                OnPropertyChanged("Status");
                OnPropertyChanged("StatusIcon");
                OnPropertyChanged("StatusColor");
                OnPropertyChanged("StatusText");
                OnPropertyChanged("StatusBadgeBackground");
                OnPropertyChanged("StatusBadgeForeground");
                OnPropertyChanged("StatusBorderColor");
                OnPropertyChanged("IsRunning");
                OnPropertyChanged("IsCompleted");
                OnPropertyChanged("IsFailed");
                OnPropertyChanged("IsWaiting");
                OnPropertyChanged("IsAwaitingApproval");
                OnPropertyChanged("ShowProgressBar");
            }
        }

        public string StatusIcon
        {
            get
            {
                switch (_status)
                {
                    case WorkflowTaskStatus.NotStarted: return "\uE8B5";
                    case WorkflowTaskStatus.Running: return "\uE768";
                    case WorkflowTaskStatus.Completed: return "\uE73E";
                    case WorkflowTaskStatus.Failed: return "\uE711";
                    case WorkflowTaskStatus.PendingReboot: return "\uE777";
                    case WorkflowTaskStatus.AwaitingApproval: return "\uE8E8";
                    case WorkflowTaskStatus.Skipped: return "\uE72B";
                    default: return "\uE8B5";
                }
            }
        }

        public Brush StatusColor
        {
            get
            {
                switch (_status)
                {
                    case WorkflowTaskStatus.NotStarted: return Brushes.Gray;
                    case WorkflowTaskStatus.Running: return Brushes.DodgerBlue;
                    case WorkflowTaskStatus.Completed: return Brushes.Green;
                    case WorkflowTaskStatus.Failed: return Brushes.Red;
                    case WorkflowTaskStatus.PendingReboot: return Brushes.Orange;
                    case WorkflowTaskStatus.AwaitingApproval: return Brushes.Purple;
                    case WorkflowTaskStatus.Skipped: return Brushes.Gray;
                    default: return Brushes.Gray;
                }
            }
        }

        public bool IsRunning { get { return _status == WorkflowTaskStatus.Running; } }
        public bool IsCompleted { get { return _status == WorkflowTaskStatus.Completed; } }
        public bool IsFailed { get { return _status == WorkflowTaskStatus.Failed; } }
        public bool IsWaiting { get { return _status == WorkflowTaskStatus.AwaitingApproval || _status == WorkflowTaskStatus.PendingReboot; } }
        public bool ShowProgressBar { get { return _status == WorkflowTaskStatus.Running; } }

        public int ProgressPercent
        {
            get { return _progressPercent; }
            set
            {
                _progressPercent = Math.Max(0, Math.Min(100, value));
                // When progress is explicitly set, switch to determinate mode
                if (value > 0)
                {
                    _hasReportedProgress = true;
                    _isIndeterminate = false;
                    OnPropertyChanged("IsIndeterminate");
                }
                OnPropertyChanged("ProgressPercent");
            }
        }

        /// <summary>
        /// Gets whether the progress bar should show indeterminate (animated) mode.
        /// True when task is running but hasn't reported explicit progress.
        /// </summary>
        public bool IsIndeterminate
        {
            get { return _isIndeterminate && _status == WorkflowTaskStatus.Running && !_hasReportedProgress; }
            set { _isIndeterminate = value; OnPropertyChanged("IsIndeterminate"); }
        }

        /// <summary>
        /// Resets progress state for a new task execution or retry attempt.
        /// </summary>
        public void ResetProgress()
        {
            _progressPercent = 0;
            _progressMessage = "";
            _errorMessage = "";
            _hasReportedProgress = false;
            _isIndeterminate = true;
            _status = WorkflowTaskStatus.NotStarted;
            OutputLines.Clear();
            OnPropertyChanged("ProgressPercent");
            OnPropertyChanged("ProgressMessage");
            OnPropertyChanged("ErrorMessage");
            OnPropertyChanged("IsIndeterminate");
            OnPropertyChanged("Status");
            OnPropertyChanged("HasOutput");
        }

        public string ProgressMessage
        {
            get { return _progressMessage; }
            set { _progressMessage = value; OnPropertyChanged("ProgressMessage"); }
        }

        public string ErrorMessage
        {
            get { return _errorMessage; }
            set { _errorMessage = value; OnPropertyChanged("ErrorMessage"); OnPropertyChanged("HasError"); }
        }

        public bool HasError { get { return !string.IsNullOrEmpty(_errorMessage); } }

        public DateTime? StartTime
        {
            get { return _startTime; }
            set { _startTime = value; OnPropertyChanged("StartTime"); UpdateDuration(); }
        }

        public DateTime? EndTime
        {
            get { return _endTime; }
            set { _endTime = value; OnPropertyChanged("EndTime"); UpdateDuration(); }
        }

        public TimeSpan Duration
        {
            get { return _duration; }
            private set { _duration = value; OnPropertyChanged("Duration"); OnPropertyChanged("DurationText"); }
        }

        public string DurationText
        {
            get
            {
                if (_duration == TimeSpan.Zero) return "";
                if (_duration.TotalHours >= 1)
                    return string.Format("{0}:{1:D2}:{2:D2}", (int)_duration.TotalHours, _duration.Minutes, _duration.Seconds);
                if (_duration.TotalMinutes >= 1)
                    return string.Format("{0}:{1:D2}", (int)_duration.TotalMinutes, _duration.Seconds);
                return string.Format("{0}s", _duration.Seconds);
            }
        }

        public bool IsExpanded
        {
            get { return _isExpanded; }
            set { _isExpanded = value; OnPropertyChanged("IsExpanded"); }
        }

        // Approval gate properties
        public string ApprovalMessage
        {
            get { return _approvalMessage; }
            set { _approvalMessage = value; OnPropertyChanged("ApprovalMessage"); }
        }

        public string ApproveButtonText
        {
            get { return _approveButtonText; }
            set { _approveButtonText = value; OnPropertyChanged("ApproveButtonText"); }
        }

        public string RejectButtonText
        {
            get { return _rejectButtonText; }
            set { _rejectButtonText = value; OnPropertyChanged("RejectButtonText"); }
        }

        public bool RequireReason
        {
            get { return _requireReason; }
            set { _requireReason = value; OnPropertyChanged("RequireReason"); }
        }

        public int TimeoutMinutes
        {
            get { return _timeoutMinutes; }
            set { _timeoutMinutes = value; OnPropertyChanged("TimeoutMinutes"); OnPropertyChanged("HasTimeout"); }
        }

        public bool HasTimeout { get { return _timeoutMinutes > 0; } }

        public string OnError
        {
            get { return _onError; }
            set { _onError = value; OnPropertyChanged("OnError"); OnPropertyChanged("ContinueOnError"); }
        }

        public bool ContinueOnError { get { return string.Equals(_onError, "Continue", StringComparison.OrdinalIgnoreCase); } }

        public string RejectionReason
        {
            get { return _rejectionReason; }
            set { _rejectionReason = value; OnPropertyChanged("RejectionReason"); }
        }

        public bool IsAwaitingApproval { get { return _status == WorkflowTaskStatus.AwaitingApproval; } }

        #region Advanced Task Properties

        /// <summary>
        /// Number of times to retry the task if it fails. Default is 0 (no retry).
        /// </summary>
        public int RetryCount
        {
            get { return _retryCount; }
            set { _retryCount = value; OnPropertyChanged("RetryCount"); OnPropertyChanged("HasRetry"); }
        }

        public bool HasRetry { get { return _retryCount > 0; } }

        /// <summary>
        /// Delay in seconds between retry attempts. Default is 5 seconds.
        /// </summary>
        public int RetryDelaySeconds
        {
            get { return _retryDelaySeconds; }
            set { _retryDelaySeconds = value; OnPropertyChanged("RetryDelaySeconds"); }
        }

        /// <summary>
        /// Maximum time in seconds the task can run before timing out. 0 means no timeout.
        /// </summary>
        public int TimeoutSeconds
        {
            get { return _timeoutSeconds; }
            set { _timeoutSeconds = value; OnPropertyChanged("TimeoutSeconds"); OnPropertyChanged("HasTaskTimeout"); }
        }

        public bool HasTaskTimeout { get { return _timeoutSeconds > 0; } }

        /// <summary>
        /// PowerShell expression that, if true, causes the task to be skipped.
        /// Can reference wizard results and workflow data.
        /// Example: "$ServerType -eq 'Production'" or "$WorkflowData['AlreadyInstalled'] -eq $true"
        /// </summary>
        public string SkipCondition
        {
            get { return _skipCondition; }
            set { _skipCondition = value; OnPropertyChanged("SkipCondition"); OnPropertyChanged("HasSkipCondition"); }
        }

        public bool HasSkipCondition { get { return !string.IsNullOrEmpty(_skipCondition); } }

        /// <summary>
        /// Reason shown when task is skipped (either by condition or script request).
        /// </summary>
        public string SkipReason
        {
            get { return _skipReason; }
            set { _skipReason = value; OnPropertyChanged("SkipReason"); }
        }

        /// <summary>
        /// Group/phase name for organizing tasks visually.
        /// Tasks with the same group are shown together with a group header.
        /// </summary>
        public string Group
        {
            get { return _group; }
            set { _group = value; OnPropertyChanged("Group"); OnPropertyChanged("HasGroup"); }
        }

        public bool HasGroup { get { return !string.IsNullOrEmpty(_group); } }

        /// <summary>
        /// Path to a PowerShell script to execute if this task fails and rollback is requested.
        /// </summary>
        public string RollbackScriptPath
        {
            get { return _rollbackScriptPath; }
            set { _rollbackScriptPath = value; OnPropertyChanged("RollbackScriptPath"); OnPropertyChanged("HasRollback"); }
        }

        /// <summary>
        /// Inline PowerShell script to execute if this task fails and rollback is requested.
        /// </summary>
        public string RollbackScriptBlock
        {
            get { return _rollbackScriptBlock; }
            set { _rollbackScriptBlock = value; OnPropertyChanged("RollbackScriptBlock"); OnPropertyChanged("HasRollback"); }
        }

        public bool HasRollback { get { return !string.IsNullOrEmpty(_rollbackScriptPath) || !string.IsNullOrEmpty(_rollbackScriptBlock); } }

        #endregion

        public string StatusText
        {
            get
            {
                switch (_status)
                {
                    case WorkflowTaskStatus.NotStarted: return "";
                    case WorkflowTaskStatus.Running: return "Running";
                    case WorkflowTaskStatus.Completed: return "Done";
                    case WorkflowTaskStatus.Failed: return "Failed";
                    case WorkflowTaskStatus.PendingReboot: return "Reboot";
                    case WorkflowTaskStatus.AwaitingApproval: return "Waiting";
                    case WorkflowTaskStatus.Skipped: return "Skipped";
                    default: return "";
                }
            }
        }

        public Brush StatusBadgeBackground
        {
            get
            {
                switch (_status)
                {
                    case WorkflowTaskStatus.Running: return new SolidColorBrush(Color.FromRgb(0, 100, 150));
                    case WorkflowTaskStatus.Completed: return new SolidColorBrush(Color.FromRgb(0, 100, 80));
                    case WorkflowTaskStatus.Failed: return new SolidColorBrush(Color.FromRgb(100, 30, 30));
                    case WorkflowTaskStatus.AwaitingApproval: return new SolidColorBrush(Color.FromRgb(80, 60, 100));
                    default: return Brushes.Transparent;
                }
            }
        }

        public Brush StatusBadgeForeground
        {
            get
            {
                switch (_status)
                {
                    case WorkflowTaskStatus.Running: return new SolidColorBrush(Color.FromRgb(0, 200, 255));
                    case WorkflowTaskStatus.Completed: return new SolidColorBrush(Color.FromRgb(0, 212, 170));
                    case WorkflowTaskStatus.Failed: return new SolidColorBrush(Color.FromRgb(255, 100, 100));
                    case WorkflowTaskStatus.AwaitingApproval: return new SolidColorBrush(Color.FromRgb(180, 130, 220));
                    default: return Brushes.Gray;
                }
            }
        }

        public Brush StatusBorderColor
        {
            get
            {
                switch (_status)
                {
                    case WorkflowTaskStatus.Running: return new SolidColorBrush(Color.FromRgb(0, 212, 170));
                    default: return new SolidColorBrush(Color.FromArgb(50, 255, 255, 255));
                }
            }
        }

        public string TimestampText
        {
            get
            {
                if (_startTime.HasValue)
                {
                    return _startTime.Value.ToString("h:mm:ss tt");
                }
                return "";
            }
        }

        // Output lines for real-time streaming
        public ObservableCollection<string> OutputLines { get; private set; }

        public bool HasOutput { get { return OutputLines.Count > 0; } }

        // Commands for approval gates
        private ICommand _approveCommand;
        private ICommand _rejectCommand;

        public ICommand ApproveCommand
        {
            get { return _approveCommand; }
            set { _approveCommand = value; OnPropertyChanged("ApproveCommand"); }
        }

        public ICommand RejectCommand
        {
            get { return _rejectCommand; }
            set { _rejectCommand = value; OnPropertyChanged("RejectCommand"); }
        }

        public ICommand ToggleExpandCommand { get; set; }

        public WorkflowTaskViewModel()
        {
            OutputLines = new ObservableCollection<string>();
            ToggleExpandCommand = new RelayCommand(_ => IsExpanded = !IsExpanded);
        }

        private void UpdateDuration()
        {
            if (_startTime.HasValue)
            {
                var endTime = _endTime ?? DateTime.Now;
                Duration = endTime - _startTime.Value;
            }
        }

        public void AddOutputLine(string level, string message)
        {
            OutputLines.Add(string.Format("> {0}", message));
            OnPropertyChanged("HasOutput");

            // Write to file log
            WorkflowLogService.LogOutput(_name, level, message);
        }

        protected void OnPropertyChanged(string name)
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(name));
            }
        }
    }

    /// <summary>
    /// ViewModel for the Workflow step view, managing a collection of tasks.
    /// </summary>
    public class WorkflowViewModel : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        private string _title;
        private string _description;
        private int _currentTaskIndex = -1;
        private bool _isExecuting;
        private bool _isCompleted;
        private bool _hasFailed;
        private string _errorAction = "Stop";
        private bool _isRebootPending;
        private string _rebootReason;
        private string _scriptPath;
        private DateTime? _startedAt;
        private System.Windows.Threading.DispatcherTimer _elapsedTimer;
        private Dictionary<string, object> _wizardResults;

        public string Title
        {
            get { return _title; }
            set { _title = value; OnPropertyChanged("Title"); }
        }

        public string Description
        {
            get { return _description; }
            set { _description = value; OnPropertyChanged("Description"); }
        }

        public ObservableCollection<WorkflowTaskViewModel> Tasks { get; private set; }

        public int CurrentTaskIndex
        {
            get { return _currentTaskIndex; }
            set
            {
                _currentTaskIndex = value;
                OnPropertyChanged("CurrentTaskIndex");
                OnPropertyChanged("CurrentTask");
                OnPropertyChanged("OverallProgress");
                OnPropertyChanged("ProgressText");
            }
        }

        public WorkflowTaskViewModel CurrentTask
        {
            get
            {
                if (_currentTaskIndex >= 0 && _currentTaskIndex < Tasks.Count)
                    return Tasks[_currentTaskIndex];
                return null;
            }
        }

        public bool IsExecuting
        {
            get { return _isExecuting; }
            set
            {
                _isExecuting = value;
                OnPropertyChanged("IsExecuting");
                OnPropertyChanged("CanStart");

                if (value && !_startedAt.HasValue)
                {
                    _startedAt = DateTime.Now;
                    StartElapsedTimer();
                }
                else if (!value)
                {
                    StopElapsedTimer();
                }
            }
        }

        public DateTime? StartedAt
        {
            get { return _startedAt; }
            set
            {
                _startedAt = value;
                OnPropertyChanged("StartedAt");
                OnPropertyChanged("ElapsedTimeText");
            }
        }

        public string ElapsedTimeText
        {
            get
            {
                if (!_startedAt.HasValue)
                    return "00:00:00";
                var elapsed = DateTime.Now - _startedAt.Value;
                return elapsed.ToString(@"hh\:mm\:ss");
            }
        }

        private void StartElapsedTimer()
        {
            _elapsedTimer = new System.Windows.Threading.DispatcherTimer();
            _elapsedTimer.Interval = TimeSpan.FromSeconds(1);
            _elapsedTimer.Tick += (s, e) => OnPropertyChanged("ElapsedTimeText");
            _elapsedTimer.Start();
        }

        private void StopElapsedTimer()
        {
            if (_elapsedTimer != null)
            {
                _elapsedTimer.Stop();
                _elapsedTimer = null;
            }
        }

        public bool IsCompleted
        {
            get { return _isCompleted; }
            set { _isCompleted = value; OnPropertyChanged("IsCompleted"); }
        }

        public bool HasFailed
        {
            get { return _hasFailed; }
            set { _hasFailed = value; OnPropertyChanged("HasFailed"); }
        }

        public bool CanStart { get { return !_isExecuting && !_isCompleted && Tasks.Count > 0; } }

        public string ErrorAction
        {
            get { return _errorAction; }
            set { _errorAction = value; OnPropertyChanged("ErrorAction"); }
        }

        public double OverallProgress
        {
            get
            {
                if (Tasks.Count == 0) return 0;
                int completed = Tasks.Count(t => t.Status == WorkflowTaskStatus.Completed || t.Status == WorkflowTaskStatus.Skipped);
                double currentProgress = 0;
                if (CurrentTask != null && CurrentTask.Status == WorkflowTaskStatus.Running)
                {
                    currentProgress = CurrentTask.ProgressPercent / 100.0;
                }
                return ((completed + currentProgress) / Tasks.Count) * 100;
            }
        }

        public string ProgressText
        {
            get
            {
                return string.Format("{0}%", (int)OverallProgress);
            }
        }

        /// <summary>
        /// Gets the count of completed tasks.
        /// </summary>
        public int CompletedTaskCount
        {
            get
            {
                return Tasks.Count(t => t.Status == WorkflowTaskStatus.Completed || t.Status == WorkflowTaskStatus.Skipped);
            }
        }

        // Reboot properties
        public bool IsRebootPending
        {
            get { return _isRebootPending; }
            set { _isRebootPending = value; OnPropertyChanged("IsRebootPending"); }
        }

        public string RebootReason
        {
            get { return _rebootReason; }
            set { _rebootReason = value; OnPropertyChanged("RebootReason"); }
        }

        public string ScriptPath
        {
            get { return _scriptPath; }
            set { _scriptPath = value; OnPropertyChanged("ScriptPath"); }
        }

        /// <summary>
        /// Wizard results from previous steps that will be passed to workflow tasks as variables.
        /// </summary>
        public Dictionary<string, object> WizardResults
        {
            get { return _wizardResults ?? (_wizardResults = new Dictionary<string, object>()); }
            set { _wizardResults = value; OnPropertyChanged("WizardResults"); }
        }

        /// <summary>
        /// Sets wizard results from form data collected in previous steps.
        /// </summary>
        public void SetWizardResults(Dictionary<string, object> formData)
        {
            _wizardResults = formData ?? new Dictionary<string, object>();
            LoggingService.Info($"Workflow received {_wizardResults.Count} wizard parameters", component: "WorkflowViewModel");
        }

        /// <summary>
        /// Gets a wizard value by parameter name.
        /// </summary>
        public object GetWizardValue(string parameterName)
        {
            if (_wizardResults != null && _wizardResults.TryGetValue(parameterName, out object value))
            {
                return value;
            }
            return null;
        }

        // Commands
        public ICommand StartCommand { get; set; }
        public ICommand CancelCommand { get; set; }
        public ICommand ViewLogsCommand { get; set; }
        public ICommand CloseCommand { get; set; }
        public ICommand RebootNowCommand { get; set; }
        public ICommand SimulateRebootCommand { get; set; }

        public WorkflowViewModel()
        {
            Tasks = new ObservableCollection<WorkflowTaskViewModel>();
            ViewLogsCommand = new RelayCommand(_ => OpenLogFile());
            SimulateRebootCommand = new RelayCommand(_ => ExecuteSimulateReboot());
            RebootNowCommand = new RelayCommand(_ => ExecuteRebootNow());
        }

        public WorkflowViewModel(string title, string description)
        {
            _title = title;
            _description = description;
            Tasks = new ObservableCollection<WorkflowTaskViewModel>();
            ViewLogsCommand = new RelayCommand(_ => OpenLogFile());
            SimulateRebootCommand = new RelayCommand(_ => ExecuteSimulateReboot());
            RebootNowCommand = new RelayCommand(_ => ExecuteRebootNow());
            
            // Note: Log service is initialized later via InitializeLogging() when script path is available
        }

        /// <summary>
        /// Initializes the workflow log service with script path for proper log location.
        /// </summary>
        /// <param name="scriptPath">Path to the script - logs will be in script folder/Logs by default.</param>
        /// <param name="customLogPath">Optional custom log file path to override default location.</param>
        /// <param name="previousLogFilePath">Optional path to previous log file to restore content from (resume scenario).</param>
        public void InitializeLogging(string scriptPath = null, string customLogPath = null, string previousLogFilePath = null)
        {
            WorkflowLogService.Initialize(_title, scriptPath, customLogPath);
            
            // If resuming, append previous log content to the new log file
            if (!string.IsNullOrEmpty(previousLogFilePath) && System.IO.File.Exists(previousLogFilePath))
            {
                try
                {
                    var previousContent = System.IO.File.ReadAllText(previousLogFilePath);
                    if (!string.IsNullOrEmpty(previousContent))
                    {
                        // Append previous log content to new log file with separator
                        WorkflowLogService.LogOutput("Resume", "INFO", "=== Continuing from previous run ===");
                        WorkflowLogService.LogOutput("Resume", "INFO", string.Format("Previous log: {0}", previousLogFilePath));
                        LoggingService.Info($"Resuming workflow - previous log at: {previousLogFilePath}", component: "WorkflowViewModel");
                    }
                }
                catch (System.Exception ex)
                {
                    LoggingService.Info($"Could not access previous log file: {ex.Message}", component: "WorkflowViewModel");
                }
            }
        }

        private void ExecuteSimulateReboot()
        {
            LoggingService.Info("Simulate reboot (close only) requested", component: "WorkflowViewModel");
            WorkflowLogService.LogRebootRequest(_rebootReason ?? "Simulated reboot");
            
            // Close the window - state is already saved by the PowerShell module
            System.Windows.Application.Current?.MainWindow?.Close();
        }

        private void ExecuteRebootNow()
        {
            LoggingService.Info("Reboot Now requested", component: "WorkflowViewModel");
            WorkflowLogService.LogRebootRequest(_rebootReason ?? "System reboot");
            
            try
            {
                // Initiate system restart
                var psi = new System.Diagnostics.ProcessStartInfo
                {
                    FileName = "shutdown.exe",
                    Arguments = "/r /t 5 /c \"PoshUI Workflow: Reboot required to continue.\"",
                    UseShellExecute = false,
                    CreateNoWindow = true
                };
                System.Diagnostics.Process.Start(psi);
                
                // Close the application
                System.Windows.Application.Current?.MainWindow?.Close();
            }
            catch (Exception ex)
            {
                LoggingService.Error($"Failed to initiate reboot: {ex.Message}", component: "WorkflowViewModel");
                System.Windows.MessageBox.Show(
                    $"Failed to initiate reboot: {ex.Message}\n\nPlease restart the system manually.",
                    "Reboot Error",
                    System.Windows.MessageBoxButton.OK,
                    System.Windows.MessageBoxImage.Error);
            }
        }

        public void AddTask(WorkflowTaskViewModel task)
        {
            Tasks.Add(task);
            OnPropertyChanged("OverallProgress");
            OnPropertyChanged("ProgressText");
            OnPropertyChanged("CanStart");
        }

        public void UpdateProgress()
        {
            OnPropertyChanged("OverallProgress");
            OnPropertyChanged("ProgressText");
            OnPropertyChanged("CompletedTaskCount");
        }

        private void OpenLogFile()
        {
            LoggingService.Info("View logs requested", component: "WorkflowViewModel");

            // Open the log file if it exists
            if (WorkflowLogService.IsInitialized && !string.IsNullOrEmpty(WorkflowLogService.LogFilePath))
            {
                try
                {
                    if (System.IO.File.Exists(WorkflowLogService.LogFilePath))
                    {
                        System.Diagnostics.Process.Start("notepad.exe", WorkflowLogService.LogFilePath);
                    }
                }
                catch (Exception ex)
                {
                    LoggingService.Error($"Failed to open log file: {ex.Message}", component: "WorkflowViewModel");
                }
            }
        }

        /// <summary>
        /// Logs task start to file.
        /// </summary>
        public void LogTaskStart(WorkflowTaskViewModel task)
        {
            WorkflowLogService.LogTaskStart(task.Name, task.Title);
        }

        /// <summary>
        /// Logs task completion to file.
        /// </summary>
        public void LogTaskComplete(WorkflowTaskViewModel task)
        {
            WorkflowLogService.LogTaskComplete(task.Name, task.Title, task.Duration);
        }

        /// <summary>
        /// Logs task failure to file.
        /// </summary>
        public void LogTaskFailed(WorkflowTaskViewModel task, string errorMessage)
        {
            WorkflowLogService.LogTaskFailed(task.Name, task.Title, errorMessage);
        }

        /// <summary>
        /// Logs workflow completion to file.
        /// </summary>
        public void LogWorkflowComplete()
        {
            int completed = Tasks.Count(t => t.Status == WorkflowTaskStatus.Completed);
            bool success = !_hasFailed && completed == Tasks.Count;
            WorkflowLogService.LogWorkflowComplete(success, completed, Tasks.Count);
        }

        /// <summary>
        /// Gets the current log file path.
        /// </summary>
        public string GetLogFilePath()
        {
            return WorkflowLogService.LogFilePath;
        }

        protected void OnPropertyChanged(string name)
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(name));
            }
        }
    }
}

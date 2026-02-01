// Copyright (c) 2025 A Solution IT LLC. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
using System;
using System.Collections.Generic;
using System.Management.Automation;
using System.Management.Automation.Runspaces;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using Launcher.ViewModels;

namespace Launcher.Services
{
    /// <summary>
    /// Provides a context object for workflow tasks to report progress and request actions.
    /// This is exposed to PowerShell scripts as $PoshUIWorkflow.
    /// </summary>
    public class WorkflowContext
    {
        private readonly WorkflowTaskViewModel _currentTask;
        private readonly WorkflowExecutor _executor;
        private readonly Action<int, string> _progressCallback;
        private readonly Action<string, string> _outputCallback;

        private bool _manualProgressMode;
        private int _autoProgressValue;
        private int _outputCount;

        public WorkflowContext(
            WorkflowTaskViewModel currentTask,
            WorkflowExecutor executor,
            Action<int, string> progressCallback,
            Action<string, string> outputCallback)
        {
            _currentTask = currentTask;
            _executor = executor;
            _progressCallback = progressCallback;
            _outputCallback = outputCallback;
            _manualProgressMode = false;
            _autoProgressValue = 0;
            _outputCount = 0;
        }

        public string CurrentTaskName { get { return _currentTask != null ? _currentTask.Name : null; } }
        public string CurrentTaskTitle { get { return _currentTask != null ? _currentTask.Title : null; } }

        /// <summary>
        /// Gets the current task index (0-based).
        /// </summary>
        public int CurrentTaskIndex { get { return _executor != null ? _executor.CurrentTaskIndex : -1; } }

        /// <summary>
        /// Gets the total number of tasks in the workflow.
        /// </summary>
        public int TotalTaskCount { get { return _executor != null ? _executor.TotalTaskCount : 0; } }

        /// <summary>
        /// Stores a value in the shared workflow data store for access by subsequent tasks.
        /// </summary>
        /// <param name="key">The key to store the value under.</param>
        /// <param name="value">The value to store.</param>
        public void SetData(string key, object value)
        {
            if (_executor != null)
            {
                _executor.SetWorkflowData(key, value);
            }
        }

        /// <summary>
        /// Retrieves a value from the shared workflow data store.
        /// </summary>
        /// <param name="key">The key to retrieve.</param>
        /// <returns>The stored value, or null if not found.</returns>
        public object GetData(string key)
        {
            if (_executor != null)
            {
                return _executor.GetWorkflowData(key);
            }
            return null;
        }

        /// <summary>
        /// Checks if a key exists in the shared workflow data store.
        /// </summary>
        /// <param name="key">The key to check.</param>
        /// <returns>True if the key exists, false otherwise.</returns>
        public bool HasData(string key)
        {
            if (_executor != null)
            {
                return _executor.HasWorkflowData(key);
            }
            return false;
        }

        /// <summary>
        /// Gets all keys in the shared workflow data store.
        /// </summary>
        public IEnumerable<string> GetDataKeys()
        {
            if (_executor != null)
            {
                return _executor.GetWorkflowDataKeys();
            }
            return new string[0];
        }

        /// <summary>
        /// Requests to skip the current task. Only works if task has CanSkip enabled.
        /// </summary>
        /// <param name="reason">The reason for skipping.</param>
        public void SkipTask(string reason)
        {
            if (_executor != null)
            {
                _executor.RequestSkipTask(reason ?? "Skipped by script");
            }
        }

        /// <summary>
        /// Gets whether manual progress mode is active (UpdateProgress was called).
        /// </summary>
        public bool IsManualProgressMode { get { return _manualProgressMode; } }

        public void UpdateProgress(int percent, string message)
        {
            _manualProgressMode = true;
            if (_progressCallback != null)
            {
                _progressCallback(percent, message ?? "");
            }
        }

        public void SetStatus(string message)
        {
            if (_progressCallback != null && _currentTask != null)
            {
                _progressCallback(_currentTask.ProgressPercent, message);
            }
        }

        public void WriteOutput(string message, string level)
        {
            if (_outputCallback != null)
            {
                _outputCallback(level ?? "OUTPUT", message);
            }
            
            // Auto-progress: increment on each output if not in manual mode
            if (!_manualProgressMode && _progressCallback != null)
            {
                _outputCount++;
                // Progress from 5% to 90% based on output count (cap at 90, complete sets 100)
                _autoProgressValue = Math.Min(90, 5 + (_outputCount * 10));
                _progressCallback(_autoProgressValue, message);
            }
        }
        
        /// <summary>
        /// Called by executor to finalize progress to 100% when task completes.
        /// </summary>
        internal void FinalizeProgress()
        {
            if (_progressCallback != null)
            {
                _progressCallback(100, "Complete");
            }
        }

        public void RequestReboot(string reason)
        {
            if (_executor != null)
            {
                _executor.RequestReboot(reason ?? "Reboot required to continue");
            }
        }

        public object GetWizardValue(string parameterName)
        {
            if (_executor != null)
            {
                return _executor.GetWizardValue(parameterName);
            }
            return null;
        }
    }

    /// <summary>
    /// Executes workflow tasks sequentially with progress tracking and error handling.
    /// </summary>
    public class WorkflowExecutor : IDisposable
    {
        private readonly WorkflowViewModel _workflow;
        private readonly Dictionary<string, object> _wizardResults;
        private readonly Action<WorkflowTaskViewModel> _onTaskStarted;
        private readonly Action<WorkflowTaskViewModel> _onTaskCompleted;
        private readonly Action<WorkflowTaskViewModel, Exception> _onTaskFailed;
        private readonly Action<string> _onRebootRequested;
        private readonly string _scriptPath;

        private Runspace _runspace;
        private PowerShell _powerShell;
        private CancellationTokenSource _cancellationTokenSource;
        private bool _rebootRequested;
        private string _rebootReason;
        private bool _disposed;

        // Shared workflow data store for inter-task communication
        private readonly Dictionary<string, object> _workflowData = new Dictionary<string, object>();

        // Task skip request
        private bool _skipRequested;
        private string _skipReason;

        // Current task tracking for context
        private int _currentTaskIndex = -1;

        public bool IsExecuting { get; private set; }
        public bool IsCancelled { get { return _cancellationTokenSource != null && _cancellationTokenSource.IsCancellationRequested; } }
        public bool RebootRequested { get { return _rebootRequested; } }
        public string RebootReason { get { return _rebootReason; } }

        /// <summary>
        /// Gets the current task index (0-based).
        /// </summary>
        public int CurrentTaskIndex { get { return _currentTaskIndex; } }

        /// <summary>
        /// Gets the total number of tasks.
        /// </summary>
        public int TotalTaskCount { get { return _workflow != null ? _workflow.Tasks.Count : 0; } }

        public WorkflowExecutor(
            WorkflowViewModel workflow,
            Dictionary<string, object> wizardResults,
            Action<WorkflowTaskViewModel> onTaskStarted = null,
            Action<WorkflowTaskViewModel> onTaskCompleted = null,
            Action<WorkflowTaskViewModel, Exception> onTaskFailed = null,
            Action<string> onRebootRequested = null,
            string scriptPath = null)
        {
            if (workflow == null) throw new ArgumentNullException("workflow");
            _workflow = workflow;
            _wizardResults = wizardResults ?? new Dictionary<string, object>();
            _onTaskStarted = onTaskStarted;
            _onTaskCompleted = onTaskCompleted;
            _onTaskFailed = onTaskFailed;
            _onRebootRequested = onRebootRequested;
            _scriptPath = scriptPath;
        }

        #region Shared Workflow Data Store

        /// <summary>
        /// Sets a value in the shared workflow data store.
        /// </summary>
        public void SetWorkflowData(string key, object value)
        {
            if (string.IsNullOrEmpty(key)) return;
            _workflowData[key] = value;
            LoggingService.Info(string.Format("Workflow data set: {0} = {1}", key, value), component: "WorkflowExecutor");
        }

        /// <summary>
        /// Gets a value from the shared workflow data store.
        /// </summary>
        public object GetWorkflowData(string key)
        {
            if (string.IsNullOrEmpty(key)) return null;
            object value;
            if (_workflowData.TryGetValue(key, out value))
            {
                return value;
            }
            return null;
        }

        /// <summary>
        /// Checks if a key exists in the workflow data store.
        /// </summary>
        public bool HasWorkflowData(string key)
        {
            return !string.IsNullOrEmpty(key) && _workflowData.ContainsKey(key);
        }

        /// <summary>
        /// Gets all keys in the workflow data store.
        /// </summary>
        public IEnumerable<string> GetWorkflowDataKeys()
        {
            return _workflowData.Keys;
        }

        #endregion

        /// <summary>
        /// Requests to skip the current task.
        /// </summary>
        public void RequestSkipTask(string reason)
        {
            _skipRequested = true;
            _skipReason = reason;
            LoggingService.Info(string.Format("Skip requested: {0}", reason), component: "WorkflowExecutor");
        }

        public async Task ExecuteAsync(CancellationToken cancellationToken)
        {
            if (IsExecuting)
            {
                throw new InvalidOperationException("Workflow is already executing");
            }

            IsExecuting = true;
            _workflow.IsExecuting = true;
            _cancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);

            try
            {
                InitializeRunspace();

                for (int i = 0; i < _workflow.Tasks.Count; i++)
                {
                    _currentTaskIndex = i;

                    if (_cancellationTokenSource.IsCancellationRequested)
                    {
                        LoggingService.Info("Workflow execution cancelled", component: "WorkflowExecutor");
                        break;
                    }

                    if (_rebootRequested)
                    {
                        LoggingService.Info(string.Format("Reboot requested, stopping at task {0}", i), component: "WorkflowExecutor");
                        break;
                    }

                    var task = _workflow.Tasks[i];
                    _workflow.CurrentTaskIndex = i;

                    if (task.Status == WorkflowTaskStatus.Completed || task.Status == WorkflowTaskStatus.Skipped)
                    {
                        LoggingService.Info(string.Format("Skipping already completed task: {0}", task.Name), component: "WorkflowExecutor");
                        continue;
                    }

                    // Check skip condition before executing
                    if (ShouldSkipTask(task))
                    {
                        var app = Application.Current;
                        if (app != null)
                        {
                            app.Dispatcher.Invoke(() =>
                            {
                                task.Status = WorkflowTaskStatus.Skipped;
                                task.ProgressMessage = string.Format("Skipped: {0}", task.SkipReason ?? "Condition not met");
                                task.EndTime = DateTime.Now;
                            });
                        }
                        LoggingService.Info(string.Format("Task '{0}' skipped: {1}", task.Name, task.SkipReason ?? "Condition not met"), component: "WorkflowExecutor");
                        WorkflowLogService.LogOutput(task.Name, "SKIP", task.SkipReason ?? "Condition not met");
                        continue;
                    }

                    // Reset skip flag for this task
                    _skipRequested = false;
                    _skipReason = null;

                    // Execute with retry support
                    int retryCount = task.RetryCount > 0 ? task.RetryCount : 0;
                    int attempt = 0;
                    Exception lastException = null;

                    while (attempt <= retryCount)
                    {
                        try
                        {
                            if (attempt > 0)
                            {
                                LoggingService.Info(string.Format("Retrying task '{0}' (attempt {1}/{2})", task.Name, attempt + 1, retryCount + 1), component: "WorkflowExecutor");
                                var app = Application.Current;
                                if (app != null)
                                {
                                    app.Dispatcher.Invoke(() =>
                                    {
                                        task.ProgressMessage = string.Format("Retrying (attempt {0}/{1})...", attempt + 1, retryCount + 1);
                                        task.ResetProgress();
                                    });
                                }

                                // Wait before retry if delay specified
                                if (task.RetryDelaySeconds > 0)
                                {
                                    await Task.Delay(task.RetryDelaySeconds * 1000, _cancellationTokenSource.Token);
                                }
                            }

                            await ExecuteTaskWithTimeoutAsync(task);

                            // Check if skip was requested during execution
                            if (_skipRequested)
                            {
                                var app = Application.Current;
                                if (app != null)
                                {
                                    app.Dispatcher.Invoke(() =>
                                    {
                                        task.Status = WorkflowTaskStatus.Skipped;
                                        task.ProgressMessage = string.Format("Skipped: {0}", _skipReason ?? "Skipped by script");
                                        task.EndTime = DateTime.Now;
                                    });
                                }
                                LoggingService.Info(string.Format("Task '{0}' skipped during execution: {1}", task.Name, _skipReason), component: "WorkflowExecutor");
                            }

                            // Task succeeded, break out of retry loop
                            break;
                        }
                        catch (OperationCanceledException)
                        {
                            if (_rebootRequested)
                            {
                                LoggingService.Info(string.Format("Task '{0}' stopped for reboot", task.Name), component: "WorkflowExecutor");
                                break;
                            }
                            else if (_skipRequested)
                            {
                                // Skip was requested via cancellation
                                break;
                            }
                            else
                            {
                                task.Status = WorkflowTaskStatus.Failed;
                                task.ErrorMessage = "Task was cancelled";
                                throw;
                            }
                        }
                        catch (TimeoutException tex)
                        {
                            lastException = tex;
                            attempt++;

                            if (attempt > retryCount)
                            {
                                task.Status = WorkflowTaskStatus.Failed;
                                task.ErrorMessage = string.Format("Task timed out after {0} seconds", task.TimeoutSeconds);
                                task.EndTime = DateTime.Now;

                                WorkflowLogService.LogTaskFailed(task.Name, task.Title, task.ErrorMessage);

                                if (_onTaskFailed != null)
                                {
                                    _onTaskFailed(task, tex);
                                }

                                bool continueOnError = task.ContinueOnError || _workflow.ErrorAction == "Continue";
                                if (!continueOnError)
                                {
                                    LoggingService.Error(string.Format("Task '{0}' timed out, stopping workflow", task.Name), tex, component: "WorkflowExecutor");
                                    _workflow.HasFailed = true;
                                    throw;
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            lastException = ex;
                            attempt++;

                            if (attempt > retryCount)
                            {
                                task.Status = WorkflowTaskStatus.Failed;
                                task.ErrorMessage = ex.Message;
                                task.EndTime = DateTime.Now;

                                // Log to file
                                WorkflowLogService.LogTaskFailed(task.Name, task.Title, ex.Message);

                                if (_onTaskFailed != null)
                                {
                                    _onTaskFailed(task, ex);
                                }

                                // Check task-level OnError first, then workflow-level
                                bool continueOnError = task.ContinueOnError || _workflow.ErrorAction == "Continue";

                                if (!continueOnError)
                                {
                                    LoggingService.Error(string.Format("Task '{0}' failed after {1} attempts, stopping workflow", task.Name, attempt), ex, component: "WorkflowExecutor");
                                    _workflow.HasFailed = true;
                                    throw;
                                }
                                else
                                {
                                    LoggingService.Warn(string.Format("Task '{0}' failed after {1} attempts, continuing (OnError=Continue)", task.Name, attempt), component: "WorkflowExecutor");
                                }
                            }
                            else
                            {
                                LoggingService.Warn(string.Format("Task '{0}' failed (attempt {1}/{2}), will retry: {3}", task.Name, attempt, retryCount + 1, ex.Message), component: "WorkflowExecutor");
                            }
                        }
                    }

                    if (_rebootRequested)
                    {
                        break;
                    }
                }

                bool allCompleted = true;
                foreach (var task in _workflow.Tasks)
                {
                    if (task.Status != WorkflowTaskStatus.Completed && task.Status != WorkflowTaskStatus.Skipped)
                    {
                        allCompleted = false;
                        break;
                    }
                }

                if (allCompleted)
                {
                    _workflow.IsCompleted = true;
                    LoggingService.Info("Workflow execution completed successfully", component: "WorkflowExecutor");

                    // Log workflow completion to file
                    int completedCount = 0;
                    foreach (var t in _workflow.Tasks)
                    {
                        if (t.Status == WorkflowTaskStatus.Completed) completedCount++;
                    }
                    WorkflowLogService.LogWorkflowComplete(!_workflow.HasFailed, completedCount, _workflow.Tasks.Count);

                    // Clear saved state file on successful completion
                    ClearWorkflowState();
                }
                else if (_rebootRequested)
                {
                    if (_onRebootRequested != null)
                    {
                        _onRebootRequested(_rebootReason);
                    }
                }
            }
            finally
            {
                IsExecuting = false;
                _workflow.IsExecuting = false;
                _currentTaskIndex = -1;
                CleanupRunspace();
            }
        }

        /// <summary>
        /// Checks if a task should be skipped based on its skip condition.
        /// </summary>
        private bool ShouldSkipTask(WorkflowTaskViewModel task)
        {
            // If no skip condition, don't skip
            if (string.IsNullOrEmpty(task.SkipCondition))
            {
                return false;
            }

            try
            {
                // Create a temporary PowerShell to evaluate the condition
                using (var ps = PowerShell.Create())
                {
                    ps.Runspace = _runspace;

                    // Set up variables for the condition
                    foreach (var kvp in _wizardResults)
                    {
                        ps.Runspace.SessionStateProxy.SetVariable(kvp.Key, kvp.Value);
                    }

                    // Also provide access to workflow data
                    ps.Runspace.SessionStateProxy.SetVariable("WorkflowData", _workflowData);

                    // Evaluate the condition
                    ps.AddScript(string.Format("$result = {0}; $result", task.SkipCondition));
                    var results = ps.Invoke();

                    if (results.Count > 0 && results[0] != null)
                    {
                        var result = results[0].BaseObject;
                        if (result is bool)
                        {
                            return (bool)result;
                        }
                        // Treat non-null/non-empty as true
                        return !string.IsNullOrEmpty(result.ToString());
                    }
                }
            }
            catch (Exception ex)
            {
                LoggingService.Warn(string.Format("Failed to evaluate skip condition for task '{0}': {1}", task.Name, ex.Message), component: "WorkflowExecutor");
            }

            return false;
        }

        /// <summary>
        /// Executes a task with optional timeout.
        /// </summary>
        private async Task ExecuteTaskWithTimeoutAsync(WorkflowTaskViewModel task)
        {
            if (task.TimeoutSeconds > 0)
            {
                using (var timeoutCts = new CancellationTokenSource())
                {
                    var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(_cancellationTokenSource.Token, timeoutCts.Token);

                    var taskExecution = ExecuteTaskAsync(task);
                    var timeoutTask = Task.Delay(task.TimeoutSeconds * 1000, linkedCts.Token);

                    var completedTask = await Task.WhenAny(taskExecution, timeoutTask);

                    if (completedTask == timeoutTask && !taskExecution.IsCompleted)
                    {
                        // Timeout occurred
                        _powerShell.Stop();
                        throw new TimeoutException(string.Format("Task '{0}' exceeded timeout of {1} seconds", task.Name, task.TimeoutSeconds));
                    }

                    // Cancel the timeout task if execution completed first
                    timeoutCts.Cancel();

                    // Propagate any exception from the task
                    await taskExecution;
                }
            }
            else
            {
                await ExecuteTaskAsync(task);
            }
        }

        private async Task ExecuteTaskAsync(WorkflowTaskViewModel task)
        {
            LoggingService.Info(string.Format("Starting task: {0} ({1})", task.Name, task.Title), component: "WorkflowExecutor");

            // Log to file
            WorkflowLogService.LogTaskStart(task.Name, task.Title);

            task.Status = WorkflowTaskStatus.Running;
            task.StartTime = DateTime.Now;
            task.ProgressPercent = 0;
            task.ProgressMessage = "Starting...";

            if (_onTaskStarted != null)
            {
                _onTaskStarted(task);
            }

            if (task.TaskType == WorkflowTaskType.ApprovalGate)
            {
                await ExecuteApprovalGateAsync(task);
            }
            else
            {
                await ExecuteNormalTaskAsync(task);
            }
        }

        private async Task ExecuteNormalTaskAsync(WorkflowTaskViewModel task)
        {
            var context = new WorkflowContext(
                task,
                this,
                (percent, message) => UpdateTaskProgress(task, percent, message),
                (level, message) => AddTaskOutput(task, level, message)
            );

            await Task.Run(() =>
            {
                try
                {
                    _powerShell.Commands.Clear();

                    _powerShell.Runspace.SessionStateProxy.SetVariable("PoshUIWorkflow", context);

                    foreach (var kvp in _wizardResults)
                    {
                        _powerShell.Runspace.SessionStateProxy.SetVariable(kvp.Key, kvp.Value);
                    }

                    if (task.Arguments != null)
                    {
                        foreach (var kvp in task.Arguments)
                        {
                            _powerShell.Runspace.SessionStateProxy.SetVariable(kvp.Key, kvp.Value);
                        }
                    }

                    string script;
                    if (!string.IsNullOrEmpty(task.ScriptPath))
                    {
                        script = string.Format(". '{0}'", task.ScriptPath);
                        LoggingService.Info(string.Format("Task '{0}' using dot-sourced script: {1}", task.Name, task.ScriptPath), component: "WorkflowExecutor");
                    }
                    else if (!string.IsNullOrEmpty(task.ScriptBlockString))
                    {
                        script = task.ScriptBlockString;
                    }
                    else
                    {
                        script = @"
                            $PoshUIWorkflow.UpdateProgress(50, 'Running...')
                            Start-Sleep -Milliseconds 500
                            $PoshUIWorkflow.UpdateProgress(100, 'Complete')
                        ";
                    }

                    _powerShell.AddScript(script);

                    _powerShell.Streams.Information.DataAdded += (s, e) =>
                    {
                        var info = _powerShell.Streams.Information[e.Index];
                        var app = Application.Current;
                        if (app != null)
                        {
                            app.Dispatcher.Invoke(() =>
                                AddTaskOutput(task, "INFO", info.MessageData != null ? info.MessageData.ToString() : ""));
                        }
                    };

                    _powerShell.Streams.Warning.DataAdded += (s, e) =>
                    {
                        var warn = _powerShell.Streams.Warning[e.Index];
                        var app = Application.Current;
                        if (app != null)
                        {
                            app.Dispatcher.Invoke(() =>
                                AddTaskOutput(task, "WARN", warn.Message));
                        }
                    };

                    _powerShell.Streams.Error.DataAdded += (s, e) =>
                    {
                        var err = _powerShell.Streams.Error[e.Index];
                        var app = Application.Current;
                        if (app != null)
                        {
                            app.Dispatcher.Invoke(() =>
                                AddTaskOutput(task, "ERR", err.Exception != null ? err.Exception.Message : err.ToString()));
                        }
                    };

                    var results = _powerShell.Invoke();

                    if (_powerShell.HadErrors)
                    {
                        var errorMessage = string.Join(Environment.NewLine,
                            _powerShell.Streams.Error);
                        throw new Exception(string.Format("Task failed: {0}", errorMessage));
                    }

                    foreach (var result in results)
                    {
                        if (result != null)
                        {
                            var app = Application.Current;
                            if (app != null)
                            {
                                app.Dispatcher.Invoke(() =>
                                    AddTaskOutput(task, "OUTPUT", result.ToString()));
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    LoggingService.Error(string.Format("Task '{0}' execution error", task.Name), ex, component: "WorkflowExecutor");
                    throw;
                }
            }, _cancellationTokenSource.Token);

            if (!_rebootRequested)
            {
                var app = Application.Current;
                if (app != null)
                {
                    app.Dispatcher.Invoke(() =>
                    {
                        task.Status = WorkflowTaskStatus.Completed;
                        task.EndTime = DateTime.Now;
                        task.ProgressPercent = 100;
                        task.ProgressMessage = "Completed";
                        _workflow.UpdateProgress();

                        // Log to file
                        WorkflowLogService.LogTaskComplete(task.Name, task.Title, task.Duration);

                        if (_onTaskCompleted != null)
                        {
                            _onTaskCompleted(task);
                        }
                    });
                }
            }
        }

        private async Task ExecuteApprovalGateAsync(WorkflowTaskViewModel task)
        {
            task.Status = WorkflowTaskStatus.AwaitingApproval;
            task.ProgressMessage = "Waiting for approval...";

            var tcs = new TaskCompletionSource<bool>();

            var app = Application.Current;
            if (app != null)
            {
                app.Dispatcher.Invoke(() =>
                {
                    task.ApproveCommand = new RelayCommand(_ =>
                    {
                        task.Status = WorkflowTaskStatus.Completed;
                        task.EndTime = DateTime.Now;
                        task.ProgressMessage = "Approved";

                        // Log to file
                        WorkflowLogService.LogApproval(task.Name, task.Title, "Approved", task.RejectionReason);
                        WorkflowLogService.LogTaskComplete(task.Name, task.Title, task.Duration);

                        tcs.TrySetResult(true);
                        if (_onTaskCompleted != null)
                        {
                            _onTaskCompleted(task);
                        }
                    });

                    task.RejectCommand = new RelayCommand(_ =>
                    {
                        task.Status = WorkflowTaskStatus.Failed;
                        task.EndTime = DateTime.Now;
                        task.ErrorMessage = string.Format("Rejected: {0}", task.RejectionReason);

                        // Log to file
                        WorkflowLogService.LogApproval(task.Name, task.Title, "Rejected", task.RejectionReason);

                        tcs.TrySetResult(false);
                    });
                });
            }

            var result = await tcs.Task;

            if (!result)
            {
                throw new Exception(string.Format("Approval gate rejected: {0}", task.RejectionReason));
            }

            _workflow.UpdateProgress();
        }

        private void UpdateTaskProgress(WorkflowTaskViewModel task, int percent, string message)
        {
            var app = Application.Current;
            if (app != null)
            {
                app.Dispatcher.Invoke(() =>
                {
                    task.ProgressPercent = percent;
                    if (!string.IsNullOrEmpty(message))
                    {
                        task.ProgressMessage = message;
                    }
                    _workflow.UpdateProgress();
                });
            }
        }

        private void AddTaskOutput(WorkflowTaskViewModel task, string level, string message)
        {
            var app = Application.Current;
            if (app != null)
            {
                app.Dispatcher.Invoke(() =>
                {
                    task.AddOutputLine(level, message);
                });
            }
        }

        public void RequestReboot(string reason)
        {
            _rebootRequested = true;
            _rebootReason = reason;

            var currentTask = _workflow.CurrentTask;
            if (currentTask != null)
            {
                var app = Application.Current;
                if (app != null)
                {
                    app.Dispatcher.Invoke(() =>
                    {
                        currentTask.Status = WorkflowTaskStatus.Completed;
                        currentTask.ProgressMessage = string.Format("Completed (reboot: {0})", reason);
                        currentTask.ProgressPercent = 100;
                        currentTask.EndTime = DateTime.Now;
                    });
                }
            }

            if (_cancellationTokenSource != null)
            {
                _cancellationTokenSource.Cancel();
            }

            var app2 = Application.Current;
            if (app2 != null)
            {
                app2.Dispatcher.Invoke(() =>
                {
                    _workflow.IsRebootPending = true;
                    _workflow.RebootReason = reason;
                    _workflow.ScriptPath = _scriptPath;
                });
            }

            // Save workflow state for resume after reboot
            SaveWorkflowState();

            LoggingService.Info(string.Format("Reboot requested: {0}, cancelling execution", reason), component: "WorkflowExecutor");
        }

        private void SaveWorkflowState()
        {
            try
            {
                var stateDir = System.IO.Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                    "PoshUI");
                
                if (!System.IO.Directory.Exists(stateDir))
                {
                    System.IO.Directory.CreateDirectory(stateDir);
                }

                var statePath = System.IO.Path.Combine(stateDir, "PoshUI_Workflow_State.json");

                // Build state JSON manually for PowerShell compatibility
                var sb = new System.Text.StringBuilder();
                sb.AppendLine("{");
                sb.AppendLine(string.Format("  \"Id\": \"{0}\",", EscapeJson(_workflow.Title ?? "Workflow")));
                sb.AppendLine(string.Format("  \"CurrentTaskIndex\": {0},", _workflow.CurrentTaskIndex + 1));
                sb.AppendLine(string.Format("  \"RebootCount\": 0,"));
                sb.AppendLine(string.Format("  \"StartTime\": \"{0}\",", DateTime.Now.ToString("yyyy-MM-ddTHH:mm:ss")));
                sb.AppendLine(string.Format("  \"LastSaveTime\": \"{0}\",", DateTime.Now.ToString("yyyy-MM-ddTHH:mm:ss")));
                sb.AppendLine(string.Format("  \"ScriptPath\": \"{0}\",", EscapeJson(_scriptPath ?? "")));
                sb.AppendLine(string.Format("  \"LogFilePath\": \"{0}\",", EscapeJson(WorkflowLogService.LogFilePath ?? "")));
                sb.AppendLine(string.Format("  \"SavedBy\": \"{0}\",", EscapeJson(Environment.UserName)));
                sb.AppendLine(string.Format("  \"ComputerName\": \"{0}\",", EscapeJson(Environment.MachineName)));
                
                // Tasks array
                sb.AppendLine("  \"Tasks\": [");
                for (int i = 0; i < _workflow.Tasks.Count; i++)
                {
                    var task = _workflow.Tasks[i];
                    var startTime = task.StartTime.HasValue ? task.StartTime.Value.ToString("yyyy-MM-ddTHH:mm:ss") : "";
                    var endTime = task.EndTime.HasValue ? task.EndTime.Value.ToString("yyyy-MM-ddTHH:mm:ss") : "";
                    
                    sb.AppendLine("    {");
                    sb.AppendLine(string.Format("      \"Name\": \"{0}\",", EscapeJson(task.Name ?? "")));
                    sb.AppendLine(string.Format("      \"Title\": \"{0}\",", EscapeJson(task.Title ?? "")));
                    sb.AppendLine(string.Format("      \"Status\": \"{0}\",", task.Status.ToString()));
                    sb.AppendLine(string.Format("      \"ProgressPercent\": {0},", task.ProgressPercent));
                    sb.AppendLine(string.Format("      \"ProgressMessage\": \"{0}\",", EscapeJson(task.ProgressMessage ?? "")));
                    sb.AppendLine(string.Format("      \"StartTime\": \"{0}\",", startTime));
                    sb.AppendLine(string.Format("      \"EndTime\": \"{0}\",", endTime));
                    
                    // Save OutputLines for resume
                    sb.AppendLine("      \"OutputLines\": [");
                    for (int j = 0; j < task.OutputLines.Count; j++)
                    {
                        sb.Append(string.Format("        \"{0}\"", EscapeJson(task.OutputLines[j])));
                        if (j < task.OutputLines.Count - 1) sb.Append(",");
                        sb.AppendLine();
                    }
                    sb.AppendLine("      ]");
                    sb.Append("    }");
                    if (i < _workflow.Tasks.Count - 1) sb.Append(",");
                    sb.AppendLine();
                }
                sb.AppendLine("  ],");
                
                // WizardResults
                sb.AppendLine("  \"WizardResults\": {");
                int wIdx = 0;
                foreach (var kvp in _wizardResults)
                {
                    var valueStr = kvp.Value != null ? EscapeJson(kvp.Value.ToString()) : "";
                    sb.Append(string.Format("    \"{0}\": \"{1}\"", EscapeJson(kvp.Key), valueStr));
                    if (wIdx < _wizardResults.Count - 1) sb.Append(",");
                    sb.AppendLine();
                    wIdx++;
                }
                sb.AppendLine("  },");

                // WorkflowData (shared data between tasks)
                sb.AppendLine("  \"WorkflowData\": {");
                int dIdx = 0;
                foreach (var kvp in _workflowData)
                {
                    var valueStr = kvp.Value != null ? EscapeJson(kvp.Value.ToString()) : "";
                    sb.Append(string.Format("    \"{0}\": \"{1}\"", EscapeJson(kvp.Key), valueStr));
                    if (dIdx < _workflowData.Count - 1) sb.Append(",");
                    sb.AppendLine();
                    dIdx++;
                }
                sb.AppendLine("  }");
                sb.AppendLine("}");

                System.IO.File.WriteAllText(statePath, sb.ToString(), System.Text.Encoding.UTF8);

                LoggingService.Info(string.Format("Workflow state saved to: {0}", statePath), component: "WorkflowExecutor");
                WorkflowLogService.LogRebootRequest(_rebootReason ?? "Reboot requested");
            }
            catch (Exception ex)
            {
                LoggingService.Error(string.Format("Failed to save workflow state: {0}", ex.Message), ex, component: "WorkflowExecutor");
            }
        }

        private static string EscapeJson(string s)
        {
            if (string.IsNullOrEmpty(s)) return "";
            return s.Replace("\\", "\\\\").Replace("\"", "\\\"").Replace("\n", "\\n").Replace("\r", "\\r").Replace("\t", "\\t");
        }

        private void ClearWorkflowState()
        {
            try
            {
                // Clear from LOCALAPPDATA
                var localPath = System.IO.Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                    "PoshUI", "PoshUI_Workflow_State.json");
                
                if (System.IO.File.Exists(localPath))
                {
                    System.IO.File.Delete(localPath);
                    LoggingService.Info(string.Format("Cleared workflow state file: {0}", localPath), component: "WorkflowExecutor");
                }

                // Also clear from PROGRAMDATA if exists
                var programDataPath = System.IO.Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData),
                    "PoshUI", "PoshUI_Workflow_State.json");
                
                if (System.IO.File.Exists(programDataPath))
                {
                    System.IO.File.Delete(programDataPath);
                    LoggingService.Info(string.Format("Cleared workflow state file: {0}", programDataPath), component: "WorkflowExecutor");
                }
            }
            catch (Exception ex)
            {
                LoggingService.Info(string.Format("Failed to clear workflow state file: {0}", ex.Message), component: "WorkflowExecutor");
            }
        }

        public object GetWizardValue(string parameterName)
        {
            object value;
            if (_wizardResults.TryGetValue(parameterName, out value))
            {
                return value;
            }
            return null;
        }

        public void Cancel()
        {
            if (_cancellationTokenSource != null)
            {
                _cancellationTokenSource.Cancel();
            }
            if (_powerShell != null)
            {
                _powerShell.Stop();
            }
        }

        private void InitializeRunspace()
        {
            var iss = InitialSessionState.CreateDefault2();
            _runspace = RunspaceFactory.CreateRunspace(iss);
            _runspace.Open();
            _powerShell = PowerShell.Create();
            _powerShell.Runspace = _runspace;
        }

        private void CleanupRunspace()
        {
            if (_powerShell != null)
            {
                _powerShell.Dispose();
                _powerShell = null;
            }
            if (_runspace != null)
            {
                _runspace.Close();
                _runspace.Dispose();
                _runspace = null;
            }
        }

        public void Dispose()
        {
            if (!_disposed)
            {
                if (_cancellationTokenSource != null)
                {
                    _cancellationTokenSource.Cancel();
                    _cancellationTokenSource.Dispose();
                }
                CleanupRunspace();
                _disposed = true;
            }
        }
    }
}

// Copyright (c) 2025 A Solution IT LLC. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
using System;
using System.IO;
using System.Text;

namespace Launcher.Services
{
    /// <summary>
    /// Provides file logging for workflow task execution.
    /// Logs are written to a file in real-time as tasks execute.
    /// </summary>
    public static class WorkflowLogService
    {
        private static string _logFilePath;
        private static readonly object _lock = new object();
        private static bool _isInitialized;
        private static string _workflowTitle;
        private static DateTime _startTime;

        /// <summary>
        /// Gets the current log file path.
        /// </summary>
        public static string LogFilePath => _logFilePath;

        /// <summary>
        /// Gets whether the log service is initialized.
        /// </summary>
        public static bool IsInitialized => _isInitialized;

        /// <summary>
        /// Initializes the workflow log service with a new log file.
        /// </summary>
        /// <param name="workflowTitle">Title of the workflow for the log header.</param>
        /// <param name="scriptPath">Optional script path - logs will be created in the script's folder by default.</param>
        /// <param name="customLogPath">Optional custom log file path. If specified, overrides script folder location.</param>
        public static void Initialize(string workflowTitle, string scriptPath = null, string customLogPath = null)
        {
            lock (_lock)
            {
                _workflowTitle = workflowTitle ?? "Workflow";
                _startTime = DateTime.Now;

                var timestamp = _startTime.ToString("yyyyMMdd_HHmmss");
                var safeTitle = SanitizeFileName(_workflowTitle);

                // Determine log file path (priority: customLogPath > scriptPath folder > LOCALAPPDATA fallback)
                if (!string.IsNullOrEmpty(customLogPath))
                {
                    // Custom path specified - use it directly
                    _logFilePath = customLogPath;
                }
                else if (!string.IsNullOrEmpty(scriptPath))
                {
                    // Use script folder as default location
                    var scriptDir = Path.GetDirectoryName(scriptPath);
                    if (!string.IsNullOrEmpty(scriptDir) && Directory.Exists(scriptDir))
                    {
                        var logDir = Path.Combine(scriptDir, "Logs");
                        if (!Directory.Exists(logDir))
                        {
                            try
                            {
                                Directory.CreateDirectory(logDir);
                            }
                            catch
                            {
                                // Fall back to LOCALAPPDATA if can't create in script folder
                                logDir = Path.Combine(
                                    Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                                    "PoshUI", "Logs");
                                if (!Directory.Exists(logDir))
                                {
                                    Directory.CreateDirectory(logDir);
                                }
                            }
                        }
                        _logFilePath = Path.Combine(logDir, $"Workflow_{safeTitle}_{timestamp}.log");
                    }
                    else
                    {
                        // Script dir doesn't exist, fall back to LOCALAPPDATA
                        var logDir = Path.Combine(
                            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                            "PoshUI", "Logs");
                        if (!Directory.Exists(logDir))
                        {
                            Directory.CreateDirectory(logDir);
                        }
                        _logFilePath = Path.Combine(logDir, $"Workflow_{safeTitle}_{timestamp}.log");
                    }
                }
                else
                {
                    // Fallback: %LOCALAPPDATA%\PoshUI\Logs\
                    var logDir = Path.Combine(
                        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                        "PoshUI", "Logs");

                    if (!Directory.Exists(logDir))
                    {
                        Directory.CreateDirectory(logDir);
                    }

                    _logFilePath = Path.Combine(logDir, $"Workflow_{safeTitle}_{timestamp}.log");
                }

                // Ensure directory exists
                var dir = Path.GetDirectoryName(_logFilePath);
                if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
                {
                    Directory.CreateDirectory(dir);
                }

                _isInitialized = true;

                // Write header
                WriteHeader();
            }
        }

        /// <summary>
        /// Writes the log file header.
        /// </summary>
        private static void WriteHeader()
        {
            var sb = new StringBuilder();
            sb.AppendLine("================================================================================");
            sb.AppendLine($"  WORKFLOW LOG: {_workflowTitle}");
            sb.AppendLine("================================================================================");
            sb.AppendLine($"  Started:  {_startTime:yyyy-MM-dd HH:mm:ss}");
            sb.AppendLine($"  Computer: {Environment.MachineName}");
            sb.AppendLine($"  User:     {Environment.UserName}");
            sb.AppendLine("================================================================================");
            sb.AppendLine();

            WriteRaw(sb.ToString());
        }

        /// <summary>
        /// Logs a task start event.
        /// </summary>
        public static void LogTaskStart(string taskName, string taskTitle)
        {
            if (!_isInitialized) return;

            var sb = new StringBuilder();
            sb.AppendLine();
            sb.AppendLine($"[{DateTime.Now:HH:mm:ss}] ────────────────────────────────────────────────────────");
            sb.AppendLine($"[{DateTime.Now:HH:mm:ss}] TASK START: {taskTitle}");
            sb.AppendLine($"[{DateTime.Now:HH:mm:ss}] ────────────────────────────────────────────────────────");

            WriteRaw(sb.ToString());
        }

        /// <summary>
        /// Logs a task completion event.
        /// </summary>
        public static void LogTaskComplete(string taskName, string taskTitle, TimeSpan duration)
        {
            if (!_isInitialized) return;

            var sb = new StringBuilder();
            sb.AppendLine($"[{DateTime.Now:HH:mm:ss}] TASK COMPLETE: {taskTitle} (Duration: {FormatDuration(duration)})");
            sb.AppendLine($"[{DateTime.Now:HH:mm:ss}] ────────────────────────────────────────────────────────");

            WriteRaw(sb.ToString());
        }

        /// <summary>
        /// Logs a task failure event.
        /// </summary>
        public static void LogTaskFailed(string taskName, string taskTitle, string errorMessage)
        {
            if (!_isInitialized) return;

            var sb = new StringBuilder();
            sb.AppendLine($"[{DateTime.Now:HH:mm:ss}] TASK FAILED: {taskTitle}");
            sb.AppendLine($"[{DateTime.Now:HH:mm:ss}] ERROR: {errorMessage}");
            sb.AppendLine($"[{DateTime.Now:HH:mm:ss}] ────────────────────────────────────────────────────────");

            WriteRaw(sb.ToString());
        }

        /// <summary>
        /// Logs an approval gate event.
        /// </summary>
        public static void LogApproval(string taskName, string taskTitle, string action, string reason = null)
        {
            if (!_isInitialized) return;

            var sb = new StringBuilder();
            sb.AppendLine($"[{DateTime.Now:HH:mm:ss}] APPROVAL: {taskTitle} - {action}");
            if (!string.IsNullOrEmpty(reason))
            {
                sb.AppendLine($"[{DateTime.Now:HH:mm:ss}] REASON: {reason}");
            }

            WriteRaw(sb.ToString());
        }

        /// <summary>
        /// Logs a progress update.
        /// </summary>
        public static void LogProgress(string taskName, int percent, string message)
        {
            if (!_isInitialized) return;

            WriteRaw($"[{DateTime.Now:HH:mm:ss}] [{percent,3}%] {message}\r\n");
        }

        /// <summary>
        /// Logs an output message from a task.
        /// </summary>
        public static void LogOutput(string taskName, string level, string message)
        {
            if (!_isInitialized) return;

            var levelTag = level?.ToUpper() ?? "INFO";
            WriteRaw($"[{DateTime.Now:HH:mm:ss}] [{levelTag,-5}] {message}\r\n");
        }

        /// <summary>
        /// Logs workflow completion.
        /// </summary>
        public static void LogWorkflowComplete(bool success, int tasksCompleted, int totalTasks)
        {
            if (!_isInitialized) return;

            var duration = DateTime.Now - _startTime;
            var status = success ? "COMPLETED SUCCESSFULLY" : "COMPLETED WITH ERRORS";

            var sb = new StringBuilder();
            sb.AppendLine();
            sb.AppendLine("================================================================================");
            sb.AppendLine($"  WORKFLOW {status}");
            sb.AppendLine("================================================================================");
            sb.AppendLine($"  Finished: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
            sb.AppendLine($"  Duration: {FormatDuration(duration)}");
            sb.AppendLine($"  Tasks:    {tasksCompleted}/{totalTasks} completed");
            sb.AppendLine("================================================================================");

            WriteRaw(sb.ToString());
        }

        /// <summary>
        /// Logs a reboot request.
        /// </summary>
        public static void LogRebootRequest(string reason)
        {
            if (!_isInitialized) return;

            var sb = new StringBuilder();
            sb.AppendLine();
            sb.AppendLine($"[{DateTime.Now:HH:mm:ss}] *** REBOOT REQUESTED ***");
            sb.AppendLine($"[{DateTime.Now:HH:mm:ss}] Reason: {reason}");
            sb.AppendLine($"[{DateTime.Now:HH:mm:ss}] Workflow state saved for resume after reboot.");

            WriteRaw(sb.ToString());
        }

        /// <summary>
        /// Logs a custom message.
        /// </summary>
        public static void Log(string level, string message)
        {
            if (!_isInitialized) return;

            var levelTag = level?.ToUpper() ?? "INFO";
            WriteRaw($"[{DateTime.Now:HH:mm:ss}] [{levelTag,-5}] {message}\r\n");
        }

        /// <summary>
        /// Writes raw text to the log file.
        /// </summary>
        private static void WriteRaw(string text)
        {
            if (string.IsNullOrEmpty(_logFilePath)) return;

            lock (_lock)
            {
                try
                {
                    File.AppendAllText(_logFilePath, text, Encoding.UTF8);
                }
                catch (Exception ex)
                {
                    // Silently fail - don't crash workflow due to logging issues
                    System.Diagnostics.Debug.WriteLine($"WorkflowLogService: Failed to write log: {ex.Message}");
                }
            }
        }

        /// <summary>
        /// Formats a duration for display.
        /// </summary>
        private static string FormatDuration(TimeSpan duration)
        {
            if (duration.TotalHours >= 1)
                return $"{(int)duration.TotalHours}h {duration.Minutes}m {duration.Seconds}s";
            if (duration.TotalMinutes >= 1)
                return $"{(int)duration.TotalMinutes}m {duration.Seconds}s";
            return $"{duration.Seconds}.{duration.Milliseconds / 100}s";
        }

        /// <summary>
        /// Sanitizes a string for use in a filename.
        /// </summary>
        private static string SanitizeFileName(string name)
        {
            if (string.IsNullOrEmpty(name)) return "Workflow";

            var invalid = Path.GetInvalidFileNameChars();
            var sb = new StringBuilder();
            foreach (var c in name)
            {
                if (Array.IndexOf(invalid, c) < 0 && c != ' ')
                    sb.Append(c);
                else if (c == ' ')
                    sb.Append('_');
            }
            return sb.Length > 0 ? sb.ToString() : "Workflow";
        }

        /// <summary>
        /// Closes the log service and releases resources.
        /// </summary>
        public static void Close()
        {
            lock (_lock)
            {
                _isInitialized = false;
                _logFilePath = null;
            }
        }
    }
}

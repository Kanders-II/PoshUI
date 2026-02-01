// Copyright (c) 2025 A Solution IT LLC. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
using System;
using System.Diagnostics;
using System.Security.Principal;

namespace Launcher.Services
{
    /// <summary>
    /// Provides centralized audit logging to Windows Event Log for security-relevant events.
    /// </summary>
    public static class AuditLogger
    {
        private const string EventSource = "PoshUI";
        private const string EventLogName = "Application";
        private static bool _isInitialized = false;
        private static readonly object _lock = new object();

        // Event IDs
        public const int EventIdScriptLoad = 1000;
        public const int EventIdScriptExecute = 1001;
        public const int EventIdExecutionError = 1002;
        public const int EventIdSecurityViolation = 1003;
        public const int EventIdPathValidationFailure = 1004;
        public const int EventIdInputValidationFailure = 1005;

        /// <summary>
        /// Initializes the event source. Must be called once at application startup.
        /// Requires administrator privileges to create the event source.
        /// </summary>
        public static void Initialize()
        {
            lock (_lock)
            {
                if (_isInitialized) return;

                try
                {
                    // Check if event source exists
                    if (!EventLog.SourceExists(EventSource))
                    {
                        // Attempt to create the event source (requires admin privileges)
                        EventLog.CreateEventSource(EventSource, EventLogName);
                        LoggingService.Info($"Event source '{EventSource}' created successfully.");
                    }
                    _isInitialized = true;
                }
                catch (UnauthorizedAccessException)
                {
                    // Not running as admin - log warning but don't fail
                    LoggingService.Warn($"Unable to create event source '{EventSource}'. Application must run as administrator once to create event source. Audit logging will use fallback.");
                    _isInitialized = false;
                }
                catch (Exception ex)
                {
                    LoggingService.Error($"Failed to initialize audit logger", ex);
                    _isInitialized = false;
                }
            }
        }

        /// <summary>
        /// Logs a script load event to the Windows Event Log.
        /// </summary>
        public static void LogScriptLoad(string scriptPath, string hash)
        {
            string user = GetCurrentUser();
            string message = $"Script loaded successfully\n" +
                           $"Path: {scriptPath}\n" +
                           $"SHA256: {hash}\n" +
                           $"User: {user}\n" +
                           $"Machine: {Environment.MachineName}\n" +
                           $"Time: {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} UTC";

            WriteEvent(EventIdScriptLoad, EventLogEntryType.Information, message);
            LoggingService.Info($"[AUDIT] Script loaded: {scriptPath} by {user}");
        }

        /// <summary>
        /// Logs the start of script execution.
        /// </summary>
        public static void LogScriptExecutionStart(string scriptPath)
        {
            string user = GetCurrentUser();
            string message = $"Script execution started\n" +
                           $"Path: {scriptPath}\n" +
                           $"User: {user}\n" +
                           $"Machine: {Environment.MachineName}\n" +
                           $"Time: {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} UTC";

            WriteEvent(EventIdScriptExecute, EventLogEntryType.Information, message);
            LoggingService.Info($"[AUDIT] Script execution started: {scriptPath} by {user}");
        }

        /// <summary>
        /// Logs script execution completion with exit code.
        /// </summary>
        public static void LogScriptExecutionComplete(string scriptPath, int exitCode, TimeSpan duration)
        {
            string user = GetCurrentUser();
            string message = $"Script execution completed\n" +
                           $"Path: {scriptPath}\n" +
                           $"Exit Code: {exitCode}\n" +
                           $"Duration: {duration.TotalSeconds:F2} seconds\n" +
                           $"User: {user}\n" +
                           $"Machine: {Environment.MachineName}\n" +
                           $"Time: {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} UTC";

            EventLogEntryType type = exitCode == 0 ? EventLogEntryType.Information : EventLogEntryType.Warning;
            WriteEvent(EventIdScriptExecute, type, message);
            LoggingService.Info($"[AUDIT] Script execution completed: {scriptPath}, exit code {exitCode}");
        }

        /// <summary>
        /// Logs a security violation.
        /// </summary>
        public static void LogSecurityViolation(string violationType, string details)
        {
            string user = GetCurrentUser();
            string message = $"SECURITY VIOLATION DETECTED\n" +
                           $"Type: {violationType}\n" +
                           $"Details: {details}\n" +
                           $"User: {user}\n" +
                           $"Machine: {Environment.MachineName}\n" +
                           $"Time: {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} UTC";

            WriteEvent(EventIdSecurityViolation, EventLogEntryType.Warning, message);
            LoggingService.Warn($"[SECURITY] Violation detected: {violationType} - {details}");
        }

        /// <summary>
        /// Logs a path validation failure.
        /// </summary>
        public static void LogPathValidationFailure(string attemptedPath, string reason)
        {
            string user = GetCurrentUser();
            string message = $"Path validation failed\n" +
                           $"Attempted Path: {attemptedPath}\n" +
                           $"Reason: {reason}\n" +
                           $"User: {user}\n" +
                           $"Machine: {Environment.MachineName}\n" +
                           $"Time: {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} UTC";

            WriteEvent(EventIdPathValidationFailure, EventLogEntryType.Warning, message);
            LoggingService.Warn($"[SECURITY] Path validation failed: {attemptedPath} - {reason}");
        }

        /// <summary>
        /// Logs an input validation failure.
        /// </summary>
        public static void LogInputValidationFailure(string inputName, string reason)
        {
            string user = GetCurrentUser();
            string message = $"Input validation failed\n" +
                           $"Input: {inputName}\n" +
                           $"Reason: {reason}\n" +
                           $"User: {user}\n" +
                           $"Machine: {Environment.MachineName}\n" +
                           $"Time: {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} UTC";

            WriteEvent(EventIdInputValidationFailure, EventLogEntryType.Warning, message);
            LoggingService.Warn($"[SECURITY] Input validation failed: {inputName} - {reason}");
        }

        /// <summary>
        /// Logs an execution error.
        /// </summary>
        public static void LogExecutionError(string scriptPath, Exception exception)
        {
            string user = GetCurrentUser();
            string message = $"Script execution error\n" +
                           $"Path: {scriptPath}\n" +
                           $"Error: {exception.Message}\n" +
                           $"Stack Trace: {exception.StackTrace}\n" +
                           $"User: {user}\n" +
                           $"Machine: {Environment.MachineName}\n" +
                           $"Time: {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} UTC";

            WriteEvent(EventIdExecutionError, EventLogEntryType.Error, message);
            LoggingService.Error($"[AUDIT] Execution error in {scriptPath}", exception);
        }

        private static void WriteEvent(int eventId, EventLogEntryType type, string message)
        {
            try
            {
                if (_isInitialized)
                {
                    EventLog.WriteEntry(EventSource, message, type, eventId);
                }
                else
                {
                    // Fallback: write to file-based log
                    LoggingService.Info($"[AUDIT FALLBACK] EventID={eventId}, Type={type}, Message={message}");
                }
            }
            catch (Exception ex)
            {
                LoggingService.Error("Failed to write audit log event", ex);
            }
        }

        private static string GetCurrentUser()
        {
            try
            {
                return WindowsIdentity.GetCurrent()?.Name ?? Environment.UserName;
            }
            catch
            {
                return Environment.UserName;
            }
        }
    }
}

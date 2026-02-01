// Copyright (c) 2025 A Solution IT LLC. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
using System;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Collections.Generic;

namespace Launcher.Services
{
    public class LoggingService
    {
        private static readonly string LogDirectory = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "logs");
        private static readonly Dictionary<string, StreamWriter> _logWriters = new Dictionary<string, StreamWriter>();
        private static bool _debugEnabled = false;
        private static bool _initialized = false;
        private const long MaxLogSize = 10 * 1024 * 1024; // 10MB
        private const int MaxBackups = 5;

        public static void Initialize(bool debugEnabled = false)
        {
            if (_initialized) return;
            
            _debugEnabled = debugEnabled;
            
            // Ensure log directory exists
            if (!Directory.Exists(LogDirectory))
            {
                Directory.CreateDirectory(LogDirectory);
            }

            // Initialize main log file
            InitializeLogFile("main", "PoshUI.log");
            
            // Command log is always enabled
            InitializeLogFile("commands", "CommandLog.txt");
            
            // Initialize debug-specific log files if debug mode is enabled
            if (_debugEnabled)
            {
                InitializeLogFile("debug", "Debug.log");
                InitializeLogFile("validation", "Validation.log");
                InitializeLogFile("ui", "UI.log");
            }
            
            _initialized = true;
        }

        private static void InitializeLogFile(string category, string fileName)
        {
            var filePath = Path.Combine(LogDirectory, fileName);
            
            try
            {
                if (File.Exists(filePath))
                {
                    FileInfo fileInfo = new FileInfo(filePath);
                    if (fileInfo.Length > MaxLogSize)
                    {
                        RotateLogs(filePath);
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Log rotation failed for {fileName}: {ex.Message}");
            }

            var streamWriter = new StreamWriter(filePath, true, new UTF8Encoding(false));
            _logWriters[category] = streamWriter;
        }

        private static void RotateLogs(string filePath)
        {
            string directory = Path.GetDirectoryName(filePath);
            string fileName = Path.GetFileNameWithoutExtension(filePath);
            string extension = Path.GetExtension(filePath);

            // 1. Delete the oldest backup if it exists
            string oldestBackup = Path.Combine(directory, $"{fileName}.{MaxBackups}{extension}");
            if (File.Exists(oldestBackup))
            {
                File.Delete(oldestBackup);
            }

            // 2. Shift existing backups (e.g., log.1 -> log.2)
            for (int i = MaxBackups - 1; i >= 1; i--)
            {
                string source = Path.Combine(directory, $"{fileName}.{i}{extension}");
                string target = Path.Combine(directory, $"{fileName}.{i + 1}{extension}");
                if (File.Exists(source))
                {
                    File.Move(source, target);
                }
            }

            // 3. Move current log to .1
            string firstBackup = Path.Combine(directory, $"{fileName}.1{extension}");
            File.Move(filePath, firstBackup);
        }

        public static void Trace(string message, string component = null, string file = null)
        {
            LogMessage(TraceEventType.Verbose, message, component, file);
        }

        public static void Debug(string message, string component = null, string file = null)
        {
            LogMessage(TraceEventType.Information, message, component, file);
        }

        public static void Info(string message, string component = null, string file = null)
        {
            LogMessage(TraceEventType.Information, message, component, file);
        }

        public static void Warn(string message, string component = null, string file = null)
        {
            LogMessage(TraceEventType.Warning, message, component, file);
        }

        public static void Error(string message, Exception ex = null, string component = null, string file = null)
        {
            if (ex != null)
            {
                message = $"{message} Exception: {ex}";
            }
            LogMessage(TraceEventType.Error, message, component, file);
        }

        public static void Fatal(string message, Exception ex = null, string component = null, string file = null)
        {
            if (ex != null)
            {
                message = $"{message} Exception: {ex}";
            }
            LogMessage(TraceEventType.Critical, message, component, file);
        }

        private static void LogMessage(TraceEventType eventType, string message, string component = null, string file = null, string category = "main")
        {
            try
            {
                // Skip debug/trace messages in production mode
                if (!_debugEnabled && (eventType == TraceEventType.Verbose || eventType == TraceEventType.Information))
                {
                    // Only log Info+ in production mode
                    if (eventType == TraceEventType.Information && category == "main")
                    {
                        // Allow Info messages in main log
                    }
                    else if (eventType == TraceEventType.Verbose)
                    {
                        return; // Skip trace messages in production
                    }
                }

                // Sanitize message for CMTrace (no line breaks)
                message = message?.Replace("\r", " ").Replace("\n", " ");

                // CMTrace type: 1=Info, 2=Warning, 3=Error
                int cmType = 1;
                switch (eventType)
                {
                    case TraceEventType.Warning:
                        cmType = 2;
                        break;
                    case TraceEventType.Error:
                    case TraceEventType.Critical:
                        cmType = 3;
                        break;
                    default:
                        cmType = 1;
                        break;
                }
                
                string logComponent = component ?? "PoshUI";
                string context = System.Security.Principal.WindowsIdentity.GetCurrent().Name;
                int threadId = System.Threading.Thread.CurrentThread.ManagedThreadId;
                string logFile = file ?? string.Empty;
                string logLine = $"<![LOG[{message}]LOG]!>" +
                    $"<time=\"{DateTime.Now:HH:mm:ss.ffffff}\" " +
                    $"date=\"{DateTime.Now:M-d-yyyy}\" " +
                    $"component=\"{logComponent}\" " +
                    $"context=\"{context}\" " +
                    $"type=\"{cmType}\" " +
                    $"thread=\"{threadId}\" " +
                    $"file=\"{logFile}\">";

                // Write to appropriate log file
                if (_logWriters.ContainsKey(category))
                {
                    _logWriters[category].WriteLine(logLine);
                    _logWriters[category].Flush();
                }
                else if (_logWriters.ContainsKey("main"))
                {
                    _logWriters["main"].WriteLine(logLine);
                    _logWriters["main"].Flush();
                }
            }
            catch (Exception ex)
            {
                // If logging fails, write to debug output as last resort
                System.Diagnostics.Debug.WriteLine($"Logging failed: {ex.Message}");
            }
        }

        // Specialized logging methods for different categories
        public static void LogCommand(string message, string component = null)
        {
            LogMessage(TraceEventType.Information, $"[CMD] {message}", component, null, "commands");
        }

        public static void LogValidation(string message, string component = null)
        {
            if (_debugEnabled)
            {
                LogMessage(TraceEventType.Information, $"[VAL] {message}", component, null, "validation");
            }
        }

        public static void LogUI(string message, string component = null)
        {
            if (_debugEnabled)
            {
                LogMessage(TraceEventType.Information, $"[UI] {message}", component, null, "ui");
            }
        }

        public static void LogDebug(string message, string component = null)
        {
            if (_debugEnabled)
            {
                LogMessage(TraceEventType.Verbose, $"[DEBUG] {message}", component, null, "debug");
            }
        }

        public static void Shutdown()
        {
            try
            {
                foreach (var writer in _logWriters.Values)
                {
                    writer?.Flush();
                    writer?.Close();
                }
                _logWriters.Clear();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error shutting down logging: {ex.Message}");
            }
        }
    }
} 
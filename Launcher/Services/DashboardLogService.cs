// Copyright (c) 2025 A Solution IT LLC. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Text;
using System.Windows;

namespace Launcher.Services
{
    /// <summary>
    /// Log entry for dashboard logging
    /// </summary>
    public class DashboardLogEntry : INotifyPropertyChanged
    {
        public DateTime Timestamp { get; set; }
        public string Level { get; set; } // Info, Warning, Error
        public string Source { get; set; } // Card name or component
        public string Message { get; set; }
        
        public string FormattedTime => Timestamp.ToString("HH:mm:ss");
        public string FormattedEntry => $"[{FormattedTime}] [{Level}] {Source}: {Message}";

        public event PropertyChangedEventHandler PropertyChanged;
    }

    /// <summary>
    /// Dashboard logging service - provides observable log entries for UI and file logging
    /// </summary>
    public class DashboardLogService : INotifyPropertyChanged
    {
        private static DashboardLogService _instance;
        private static readonly object _lock = new object();
        
        private ObservableCollection<DashboardLogEntry> _logEntries;
        private StreamWriter _fileWriter;
        private string _logFilePath;
        private bool _isEnabled;
        private bool _isPanelVisible;
        private string _scriptName;
        private string _scriptRoot;
        private int _errorCount;
        private int _warningCount;

        public event PropertyChangedEventHandler PropertyChanged;

        public static DashboardLogService Instance
        {
            get
            {
                if (_instance == null)
                {
                    lock (_lock)
                    {
                        if (_instance == null)
                        {
                            _instance = new DashboardLogService();
                        }
                    }
                }
                return _instance;
            }
        }

        private DashboardLogService()
        {
            _logEntries = new ObservableCollection<DashboardLogEntry>();
            _isEnabled = false;
            _isPanelVisible = false;
        }

        public ObservableCollection<DashboardLogEntry> LogEntries => _logEntries;

        public bool IsEnabled
        {
            get => _isEnabled;
            set
            {
                if (_isEnabled != value)
                {
                    _isEnabled = value;
                    OnPropertyChanged(nameof(IsEnabled));
                }
            }
        }

        public bool IsPanelVisible
        {
            get => _isPanelVisible;
            set
            {
                if (_isPanelVisible != value)
                {
                    _isPanelVisible = value;
                    OnPropertyChanged(nameof(IsPanelVisible));
                }
            }
        }

        public int ErrorCount
        {
            get => _errorCount;
            private set
            {
                if (_errorCount != value)
                {
                    _errorCount = value;
                    OnPropertyChanged(nameof(ErrorCount));
                    OnPropertyChanged(nameof(HasErrors));
                }
            }
        }

        public int WarningCount
        {
            get => _warningCount;
            private set
            {
                if (_warningCount != value)
                {
                    _warningCount = value;
                    OnPropertyChanged(nameof(WarningCount));
                }
            }
        }

        public bool HasErrors => _errorCount > 0;

        public string LogFilePath => _logFilePath;

        /// <summary>
        /// Initialize dashboard logging for a script
        /// </summary>
        /// <param name="scriptPath">Full path to the script being executed</param>
        public void Initialize(string scriptPath)
        {
            if (string.IsNullOrEmpty(scriptPath)) 
            {
                System.Diagnostics.Debug.WriteLine("DashboardLogService.Initialize: scriptPath is null or empty");
                return;
            }

            try
            {
                _scriptRoot = Path.GetDirectoryName(scriptPath);
                _scriptName = Path.GetFileNameWithoutExtension(scriptPath);
                
                System.Diagnostics.Debug.WriteLine($"DashboardLogService.Initialize: Processing script '{_scriptName}' in '{_scriptRoot}'");
                
                // Create log file: ScriptName_YYYYMMDD_HHmmss.log
                string timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
                string logFileName = $"{_scriptName}_{timestamp}.log";
                _logFilePath = Path.Combine(_scriptRoot, logFileName);

                System.Diagnostics.Debug.WriteLine($"DashboardLogService.Initialize: Creating log file at '{_logFilePath}'");

                // Create/open log file
                _fileWriter = new StreamWriter(_logFilePath, false, new UTF8Encoding(false));
                _fileWriter.AutoFlush = true;

                // Write header
                _fileWriter.WriteLine($"# PoshUI Dashboard Log");
                _fileWriter.WriteLine($"# Script: {_scriptName}");
                _fileWriter.WriteLine($"# Started: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
                _fileWriter.WriteLine($"# ============================================");
                _fileWriter.WriteLine();

                _isEnabled = true;
                
                System.Diagnostics.Debug.WriteLine($"DashboardLogService.Initialize: Logging initialized successfully for '{_logFilePath}'");
                
                // Clear any previous entries
                Application.Current?.Dispatcher?.Invoke(() =>
                {
                    _logEntries.Clear();
                    ErrorCount = 0;
                    WarningCount = 0;
                });

                Info("Dashboard", $"Logging initialized: {logFileName}");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to initialize dashboard logging: {ex.Message}");
            }
        }

        /// <summary>
        /// Log an info message
        /// </summary>
        public void Info(string source, string message)
        {
            Log("Info", source, message);
        }

        /// <summary>
        /// Log a warning message
        /// </summary>
        public void Warning(string source, string message)
        {
            Log("Warning", source, message);
            Application.Current?.Dispatcher?.Invoke(() => WarningCount++);
        }

        /// <summary>
        /// Log an error message
        /// </summary>
        public void Error(string source, string message)
        {
            Log("Error", source, message);
            Application.Current?.Dispatcher?.Invoke(() =>
            {
                ErrorCount++;
                // Auto-expand panel on error
                IsPanelVisible = true;
            });
        }

        /// <summary>
        /// Log a message from a refresh script
        /// </summary>
        public void LogRefresh(string cardName, string message, bool isError = false)
        {
            if (isError)
                Error(cardName, $"Refresh: {message}");
            else
                Info(cardName, $"Refresh: {message}");
        }

        private void Log(string level, string source, string message)
        {
            if (!_isEnabled) return;

            var entry = new DashboardLogEntry
            {
                Timestamp = DateTime.Now,
                Level = level,
                Source = source ?? "System",
                Message = message
            };

            // Add to UI collection on UI thread
            Application.Current?.Dispatcher?.Invoke(() =>
            {
                _logEntries.Add(entry);
                
                // Keep max 1000 entries to prevent memory issues
                while (_logEntries.Count > 1000)
                {
                    _logEntries.RemoveAt(0);
                }
            });

            // Write to file
            WriteToFile(entry);
        }

        private void WriteToFile(DashboardLogEntry entry)
        {
            try
            {
                if (_fileWriter != null)
                {
                    string line = $"[{entry.Timestamp:yyyy-MM-dd HH:mm:ss.fff}] [{entry.Level,-7}] [{entry.Source}] {entry.Message}";
                    _fileWriter.WriteLine(line);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to write log entry: {ex.Message}");
            }
        }

        /// <summary>
        /// Clear all log entries
        /// </summary>
        public void Clear()
        {
            Application.Current?.Dispatcher?.Invoke(() =>
            {
                _logEntries.Clear();
                ErrorCount = 0;
                WarningCount = 0;
            });

            if (_fileWriter != null)
            {
                _fileWriter.WriteLine();
                _fileWriter.WriteLine($"# Log cleared at {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
                _fileWriter.WriteLine();
            }
        }

        /// <summary>
        /// Toggle panel visibility
        /// </summary>
        public void TogglePanel()
        {
            IsPanelVisible = !IsPanelVisible;
        }

        /// <summary>
        /// Shutdown logging
        /// </summary>
        public void Shutdown()
        {
            try
            {
                if (_fileWriter != null)
                {
                    _fileWriter.WriteLine();
                    _fileWriter.WriteLine($"# ============================================");
                    _fileWriter.WriteLine($"# Ended: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
                    _fileWriter.WriteLine($"# Errors: {_errorCount}, Warnings: {_warningCount}");
                    _fileWriter.Flush();
                    _fileWriter.Close();
                    _fileWriter = null;
                }
                
                _isEnabled = false;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error shutting down dashboard logging: {ex.Message}");
            }
        }

        protected void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}

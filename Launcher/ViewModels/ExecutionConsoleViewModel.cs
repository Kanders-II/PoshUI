// Copyright (c) 2025 A Solution IT LLC. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;

namespace Launcher.ViewModels
{
    public class ConsoleLine
    {
        public DateTime Timestamp { get; set; }
        public string Level { get; set; }
        public string Message { get; set; }
        public Brush Foreground { get; set; }
        public string Text => $"[{Timestamp:HH:mm:ss}] [{Level}] {Message}";
    }

    public class ExecutionConsoleViewModel : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        public ObservableCollection<ConsoleLine> Lines { get; } = new ObservableCollection<ConsoleLine>();

        private bool _isRunning;
        public bool IsRunning
        {
            get => _isRunning;
            set 
            { 
                _isRunning = value; 
                OnPropertyChanged(nameof(IsRunning)); 
                _cancelCommand?.RaiseCanExecuteChanged();
                _closeCommand?.RaiseCanExecuteChanged();
            }
        }

        private string _statusText = "Running...";
        public string StatusText
        {
            get => _statusText;
            set { _statusText = value; OnPropertyChanged(nameof(StatusText)); }
        }

        private string _logFilePath;
        public string LogFilePath
        {
            get => _logFilePath;
            set 
            { 
                _logFilePath = value; 
                OnPropertyChanged(nameof(LogFilePath)); 
                _openLogRelay?.RaiseCanExecuteChanged();
            }
        }

        public ICommand CancelCommand { get; }
        public ICommand OpenLogCommand { get; }
        public ICommand CopyCommand { get; }
        public ICommand ClearCommand { get; }
        public ICommand CloseCommand { get; }

        private readonly RelayCommand _cancelCommand;
        private readonly RelayCommand _openLogRelay;
        private readonly RelayCommand _copyRelay;
        private readonly RelayCommand _clearCommand;
        private readonly RelayCommand _closeCommand;

        private readonly Action _onCancel;
        private readonly ICommand _closeCommandAction;

        public ExecutionConsoleViewModel(Action onCancel, ICommand closeCommand = null)
        {
            _onCancel = onCancel;
            _closeCommandAction = closeCommand;
            IsRunning = true;

            _cancelCommand = new RelayCommand(_ => _onCancel?.Invoke(), _ => IsRunning);
            _openLogRelay = new RelayCommand(_ => OpenLog(), _ => !string.IsNullOrEmpty(LogFilePath));
            _copyRelay = new RelayCommand(_ => CopyAll());
            _clearCommand = new RelayCommand(_ => Lines.Clear(), _ => Lines.Any());
            _closeCommand = new RelayCommand(_ => 
            {
                // Route close action through provided command, or fallback to direct close
                if (_closeCommandAction != null)
                {
                    _closeCommandAction.Execute(null);
                }
                else
                {
                    Application.Current.MainWindow?.Close();
                }
            }, _ => !IsRunning);

            CancelCommand = _cancelCommand;
            OpenLogCommand = _openLogRelay;
            CopyCommand = _copyRelay;
            ClearCommand = _clearCommand;
            CloseCommand = _closeCommand;

            Lines.CollectionChanged += (s, e) => _clearCommand?.RaiseCanExecuteChanged();
        }

        public void AddLine(DateTime ts, string level, string message)
        {
            var brush = GetBrushForLevel(level);
            var line = new ConsoleLine { Timestamp = ts, Level = level, Message = message, Foreground = brush };
            Lines.Add(line);
        }

        public void MarkCompleted(string status)
        {
            IsRunning = false;
            StatusText = status;
            _cancelCommand?.RaiseCanExecuteChanged();
            _closeCommand?.RaiseCanExecuteChanged();
        }

        private void OpenLog()
        {
            try
            {
                if (!string.IsNullOrEmpty(LogFilePath))
                {
                    Process.Start(new ProcessStartInfo
                    {
                        FileName = "notepad.exe",
                        Arguments = $"\"{LogFilePath}\"",
                        UseShellExecute = false
                    });
                }
            }
            catch { /* ignore */ }
        }

        private void CopyAll()
        {
            try
            {
                var sb = new StringBuilder();
                foreach (var l in Lines)
                    sb.AppendLine(l.Text);
                Clipboard.SetText(sb.ToString());
            }
            catch { /* ignore */ }
        }

        private static Brush GetBrushForLevel(string level)
        {
            switch ((level ?? "").ToUpperInvariant())
            {
                case "ERR": return Brushes.Red;
                case "WARN": return Brushes.Orange;
                case "VERBOSE": return Brushes.SteelBlue;
                case "DEBUG": return Brushes.SlateGray;
                case "PROGRESS": return Brushes.Green;
                case "HOST": return GetDefaultTextBrush();
                case "OUTPUT": return GetDefaultTextBrush();
                default: return GetDefaultTextBrush();
            }
        }

        private static Brush GetDefaultTextBrush()
        {
            // Return null so XAML data triggers can handle theme-aware colors
            // This allows the UI to use DynamicResource bindings for proper theme switching
            return null;
        }

        private void OnPropertyChanged(string name) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}

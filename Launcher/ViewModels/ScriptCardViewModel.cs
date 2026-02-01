// Copyright (c) 2025 A Solution IT LLC. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;
using Launcher.Services;

namespace Launcher.ViewModels
{
    /// <summary>
    /// Represents the execution state of a script card.
    /// </summary>
    public enum CardExecutionState
    {
        Ready,
        Configuring,
        Running,
        Success,
        Error,
        Cancelled
    }

    /// <summary>
    /// Represents a parameter control definition for a script card.
    /// </summary>
    public class ScriptCardParameter : INotifyPropertyChanged
    {
        private string _name;
        private string _label;
        private string _type;
        private bool _mandatory;
        private object _default;
        private string _helpText;
        private List<string> _choices;
        private double? _min;
        private double? _max;
        private double? _step;
        private string _validationPattern;
        private object _value;
        private string _pathType;

        public string Name { get => _name; set { _name = value; OnPropertyChanged(); } }
        public string Label { get => _label; set { _label = value; OnPropertyChanged(); } }
        public string Type { get => _type; set { _type = value; OnPropertyChanged(); } }
        public bool Mandatory { get => _mandatory; set { _mandatory = value; OnPropertyChanged(); } }
        public object Default { get => _default; set { _default = value; OnPropertyChanged(); } }
        public string HelpText { get => _helpText; set { _helpText = value; OnPropertyChanged(); } }
        public List<string> Choices { get => _choices; set { _choices = value; OnPropertyChanged(); } }
        public double? Min { get => _min; set { _min = value; OnPropertyChanged(); } }
        public double? Max { get => _max; set { _max = value; OnPropertyChanged(); } }
        public double? Step { get => _step; set { _step = value; OnPropertyChanged(); } }
        public string ValidationPattern { get => _validationPattern; set { _validationPattern = value; OnPropertyChanged(); } }
        public string PathType { get => _pathType; set { _pathType = value; OnPropertyChanged(); } }

        public object Value
        {
            get 
            {
                // For toggles/checkboxes, ensure we return a boolean
                if (IsToggle || IsCheckbox)
                {
                    var val = _value ?? _default;
                    if (val == null) return false;
                    if (val is bool b) return b;
                    if (val is string s && bool.TryParse(s, out bool result)) return result;
                    return false;
                }
                return _value ?? _default;
            }
            set
            {
                // Always update _value, not _default
                if (!Equals(_value, value))
                {
                    _value = value;
                    OnPropertyChanged();
                }
            }
        }

        // UI helper properties
        public bool IsTextBox => Type == "TextBox";
        public bool IsPassword => Type == "Password";
        public bool IsCheckbox => Type == "Checkbox";
        public bool IsToggle => Type == "Toggle";
        public bool IsDropdown => Type == "Dropdown";
        public bool IsNumeric => Type == "Numeric";
        public bool IsDate => Type == "Date";
        public bool IsFilePath => Type == "FilePath";
        public bool IsFolderPath => Type == "FolderPath";
        public bool HasChoices => Choices != null && Choices.Count > 0;

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }

    /// <summary>
    /// ViewModel for an executable script card in CardGrid view mode.
    /// </summary>
    public class ScriptCardViewModel : INotifyPropertyChanged
    {
        private string _name;
        private string _title;
        private string _description;
        private string _iconGlyph;
        private string _category;
        private string _tags;
        private CardExecutionState _state = CardExecutionState.Ready;
        private string _scriptPath;
        private string _scriptBlock;
        private string _scriptSource;
        private string _outputText = "";
        private double _progress;
        private string _errorMessage;
        private CancellationTokenSource _cancellationTokenSource;

        public string Name { get => _name; set { _name = value; OnPropertyChanged(); } }
        public string Title { get => _title; set { _title = value; OnPropertyChanged(); } }
        public string Description { get => _description; set { _description = value; OnPropertyChanged(); } }
        public string IconGlyph { get => _iconGlyph; set { _iconGlyph = value; OnPropertyChanged(); } }
        public string Category { get => _category; set { _category = value; OnPropertyChanged(); } }
        public string Tags { get => _tags; set { _tags = value; OnPropertyChanged(); } }

        public CardExecutionState State
        {
            get => _state;
            set
            {
                _state = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(IsReady));
                OnPropertyChanged(nameof(IsConfiguring));
                OnPropertyChanged(nameof(IsRunning));
                OnPropertyChanged(nameof(IsSuccess));
                OnPropertyChanged(nameof(IsError));
                OnPropertyChanged(nameof(StateIcon));
                OnPropertyChanged(nameof(CanRun));
                OnPropertyChanged(nameof(ActionLabel));
            }
        }

        // State convenience properties for UI binding
        public bool IsReady => State == CardExecutionState.Ready;
        public bool IsConfiguring => State == CardExecutionState.Configuring;
        public bool IsRunning => State == CardExecutionState.Running;
        public bool IsSuccess => State == CardExecutionState.Success;
        public bool IsError => State == CardExecutionState.Error;
        public bool CanRun => State != CardExecutionState.Running;

        public string StateIcon
        {
            get
            {
                switch (State)
                {
                    case CardExecutionState.Ready:
                        return "\uE768";       // Play
                    case CardExecutionState.Configuring:
                        return "\uE713"; // Settings
                    case CardExecutionState.Running:
                        return "\uE895";     // Sync (spinner)
                    case CardExecutionState.Success:
                        return "\uE73E";     // Checkmark
                    case CardExecutionState.Error:
                        return "\uE711";       // X
                    case CardExecutionState.Cancelled:
                        return "\uE711";
                    default:
                        return "\uE768";
                }
            }
        }

        public string ActionLabel
        {
            get
            {
                switch (State)
                {
                    case CardExecutionState.Ready:
                        return "Run";
                    case CardExecutionState.Success:
                        return "Run Again";
                    case CardExecutionState.Error:
                        return "Retry";
                    case CardExecutionState.Running:
                        return "Running...";
                    default:
                        return "Run";
                }
            }
        }

        public string ScriptPath { get => _scriptPath; set { _scriptPath = value; OnPropertyChanged(); } }
        public string ScriptBlock { get => _scriptBlock; set { _scriptBlock = value; OnPropertyChanged(); } }
        public string ScriptSource { get => _scriptSource; set { _scriptSource = value; OnPropertyChanged(); } }

        public string OutputText
        {
            get => _outputText;
            set { _outputText = value; OnPropertyChanged(); OnPropertyChanged(nameof(HasOutput)); }
        }

        public bool HasOutput => !string.IsNullOrEmpty(_outputText);

        public double Progress
        {
            get => _progress;
            set { _progress = value; OnPropertyChanged(); }
        }

        public string ErrorMessage
        {
            get => _errorMessage;
            set { _errorMessage = value; OnPropertyChanged(); OnPropertyChanged(nameof(HasError)); }
        }

        public bool HasError => !string.IsNullOrEmpty(_errorMessage);

        // Parameters for the script (populated from auto-discovery)
        public ObservableCollection<ScriptCardParameter> Parameters { get; } = new ObservableCollection<ScriptCardParameter>();

        public bool HasParameters => Parameters.Count > 0;

        // Commands
        public ICommand OpenDialogCommand { get; }
        public ICommand ExecuteCommand { get; }
        public ICommand CancelCommand { get; }
        public ICommand ResetCommand { get; }
        public ICommand ClearOutputCommand { get; }

        // Reference to parent for dialog management
        private readonly Action<ScriptCardViewModel> _openDialogAction;

        public ScriptCardViewModel(Action<ScriptCardViewModel> openDialogAction = null)
        {
            _openDialogAction = openDialogAction;
            
            OpenDialogCommand = new RelayCommand(_ => OpenDialog(), _ => CanRun);
            ExecuteCommand = new RelayCommand(async _ => await ExecuteScriptAsync(), _ => CanRun);
            CancelCommand = new RelayCommand(_ => CancelExecution(), _ => IsRunning);
            ResetCommand = new RelayCommand(_ => Reset());
            ClearOutputCommand = new RelayCommand(_ => ClearOutput());
        }

        private void OpenDialog()
        {
            LoggingService.Info($"Opening dialog for script card: {Name}", component: "ScriptCardViewModel");
            if (_openDialogAction != null)
            {
                _openDialogAction(this);
            }
        }

        public async Task ExecuteScriptAsync()
        {
            State = CardExecutionState.Running;
            OutputText = "";
            ErrorMessage = "";
            Progress = 0;

            _cancellationTokenSource = new CancellationTokenSource();
            var token = _cancellationTokenSource.Token;

            try
            {
                LoggingService.Info($"Executing script card: {Name}", component: "ScriptCardViewModel");

                // Initialize automatic logging for dashboard mode
                if (ScriptSource == "File" && !string.IsNullOrEmpty(ScriptPath))
                {
                    try
                    {
                        LoggingService.Debug($"Attempting to initialize logging for: {ScriptPath}", component: "ScriptCardViewModel");
                        Services.DashboardLogService.Instance.Initialize(ScriptPath);
                        Services.DashboardLogService.Instance.Info(Name, $"Starting execution of script card '{Name}'");
                        Services.DashboardLogService.Instance.Info(Name, $"Script path: {ScriptPath}");
                        LoggingService.Debug($"Logging initialized successfully for: {ScriptPath}", component: "ScriptCardViewModel");
                        
                        // Log parameters
                        if (Parameters.Count > 0)
                        {
                            Services.DashboardLogService.Instance.Info(Name, "Parameters:");
                            foreach (var param in Parameters)
                            {
                                if (param.Value != null)
                                {
                                    Services.DashboardLogService.Instance.Info(Name, $"  {param.Name}: {param.Value}");
                                }
                            }
                        }
                    }
                    catch (Exception logEx)
                    {
                        LoggingService.Warn($"Failed to initialize automatic logging: {logEx.Message}", component: "ScriptCardViewModel");
                    }
                }
                else
                {
                    LoggingService.Debug($"Logging not initialized - ScriptSource: {ScriptSource}, ScriptPath: {(string.IsNullOrEmpty(ScriptPath) ? "NULL or EMPTY" : ScriptPath)}", component: "ScriptCardViewModel");
                }

                // Build parameter dictionary with proper type conversion
                var parameters = new Dictionary<string, object>();
                foreach (var param in Parameters)
                {
                    if (param.Value != null)
                    {
                        // Convert to proper .NET types
                        object convertedValue = ConvertParameterValue(param.Value, param.Type);
                        parameters[param.Name] = convertedValue;
                        LoggingService.Debug($"  Parameter: {param.Name} = {convertedValue} (Type: {convertedValue?.GetType().Name ?? "null"})", component: "ScriptCardViewModel");
                    }
                }

                // Execute using ScriptCardRunner
                await Task.Run(async () =>
                {
                    using (var runner = new ScriptCardRunner())
                    {
                        runner.OutputReceived += (s, output) =>
                        {
                            if (System.Windows.Application.Current != null && System.Windows.Application.Current.Dispatcher != null)
                            {
                                System.Windows.Application.Current.Dispatcher.Invoke(() =>
                                {
                                    OutputText += output + Environment.NewLine;
                                });
                            }
                            
                            // Log output to dashboard log (filter out empty lines and common noise)
                            try
                            {
                                if (!string.IsNullOrWhiteSpace(output) && 
                                    !output.Trim().Equals("True", StringComparison.OrdinalIgnoreCase) &&
                                    !output.Trim().Equals("False", StringComparison.OrdinalIgnoreCase))
                                {
                                    Services.DashboardLogService.Instance.Info(Name, output.Trim());
                                }
                            }
                            catch (Exception logEx)
                            {
                                // Don't log logging failures to avoid infinite loops
                                System.Diagnostics.Debug.WriteLine($"Failed to log output: {logEx.Message}");
                            }
                        };

                        runner.ProgressChanged += (s, pct) =>
                        {
                            if (System.Windows.Application.Current != null && System.Windows.Application.Current.Dispatcher != null)
                            {
                                System.Windows.Application.Current.Dispatcher.Invoke(() =>
                                {
                                    Progress = pct;
                                });
                            }
                        };

                        if (ScriptSource == "File")
                        {
                            // For file-based scripts, use direct invocation with proper parameter passing
                            await runner.ExecuteFileAsync(ScriptPath, parameters, token);
                        }
                        else
                        {
                            // For ScriptBlock, use the existing method
                            await runner.ExecuteAsync(ScriptBlock, parameters, token);
                        }
                    }
                });

                State = CardExecutionState.Success;
                Progress = 100;
                LoggingService.Info($"Script card '{Name}' completed successfully", component: "ScriptCardViewModel");
                
                // Log successful execution
                try
                {
                    Services.DashboardLogService.Instance.Info(Name, "Script execution completed successfully");
                    Services.DashboardLogService.Instance.Info(Name, $"Execution time: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
                }
                catch (Exception logEx)
                {
                    LoggingService.Warn($"Failed to log success: {logEx.Message}", component: "ScriptCardViewModel");
                }
            }
            catch (OperationCanceledException)
            {
                State = CardExecutionState.Cancelled;
                OutputText += Environment.NewLine + "--- Execution cancelled ---" + Environment.NewLine;
                LoggingService.Warn($"Script card '{Name}' was cancelled", component: "ScriptCardViewModel");
                
                // Log cancellation
                try
                {
                    Services.DashboardLogService.Instance.Warning(Name, "Script execution was cancelled by user");
                }
                catch (Exception logEx)
                {
                    LoggingService.Warn($"Failed to log cancellation: {logEx.Message}", component: "ScriptCardViewModel");
                }
            }
            catch (Exception ex)
            {
                State = CardExecutionState.Error;
                ErrorMessage = ex.Message;
                OutputText += Environment.NewLine + $"--- ERROR: {ex.Message} ---" + Environment.NewLine;
                LoggingService.Error($"Script card '{Name}' failed: {ex.Message}", component: "ScriptCardViewModel");
                
                // Log error
                try
                {
                    Services.DashboardLogService.Instance.Error(Name, $"Script execution failed: {ex.Message}");
                    Services.DashboardLogService.Instance.Error(Name, $"Full error: {ex.ToString()}");
                }
                catch (Exception logEx)
                {
                    LoggingService.Warn($"Failed to log error: {logEx.Message}", component: "ScriptCardViewModel");
                }
            }
        }

        private void CancelExecution()
        {
            LoggingService.Info($"Cancelling execution for script card: {Name}", component: "ScriptCardViewModel");
            if (_cancellationTokenSource != null)
            {
                _cancellationTokenSource.Cancel();
            }
        }

        private void Reset()
        {
            State = CardExecutionState.Ready;
            OutputText = "";
            ErrorMessage = "";
            Progress = 0;
            LoggingService.Debug($"Reset script card: {Name}", component: "ScriptCardViewModel");
        }

        private void ClearOutput()
        {
            OutputText = "";
        }

        /// <summary>
        /// Converts parameter values to proper .NET types for PowerShell.
        /// </summary>
        private object ConvertParameterValue(object value, string paramType)
        {
            if (value == null) return null;

            // Handle numeric types - ensure integers are passed as int, not double
            if (paramType == "Numeric" && value is double doubleVal)
            {
                // If it's a whole number, convert to int
                if (doubleVal == Math.Floor(doubleVal) && doubleVal >= int.MinValue && doubleVal <= int.MaxValue)
                {
                    return (int)doubleVal;
                }
                return doubleVal;
            }

            // Handle string representations of numbers
            if (paramType == "Numeric" && value is string strVal)
            {
                int parsedInt;
                double parsedDouble;
                if (int.TryParse(strVal, out parsedInt))
                    return parsedInt;
                if (double.TryParse(strVal, out parsedDouble))
                    return parsedDouble;
            }

            // Handle boolean/switch parameters
            if (paramType == "Toggle" || paramType == "Checkbox")
            {
                if (value is bool boolVal)
                    return boolVal;
                if (value is string boolStr)
                    return boolStr.Equals("true", StringComparison.OrdinalIgnoreCase);
            }

            return value;
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}



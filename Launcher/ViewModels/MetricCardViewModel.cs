// Copyright (c) 2025 A Solution IT LLC. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Management.Automation;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;
using System.Windows.Media;
using Launcher.Services;

namespace Launcher.ViewModels
{
    /// <summary>
    /// ViewModel for displaying KPI-style metric cards in CardGrid
    /// </summary>
    public class MetricCardViewModel : INotifyPropertyChanged
    {
        private string _title = string.Empty;
        private double _value;
        private string _unit = string.Empty;
        private string _format = "N0";
        private string _trend = string.Empty;
        private double _trendValue;
        private Brush _foreground = Brushes.White;
        private Brush _accentBrush = new SolidColorBrush(Color.FromRgb(0, 120, 212)); // Windows blue
        private string _icon = string.Empty;
        private string _description = string.Empty;
        private string _target = string.Empty;
        private double _targetValue;
        private double _minValue;
        private double _maxValue = 100;
        private bool _showProgressBar;
        private bool _showTrend;
        private bool _showTarget;
        private string _category = "General";
        private string _refreshScript;
        private bool _isRefreshing;
        private double? _previousValue;
        private bool _autoCalculateTrend = true;

        public event PropertyChangedEventHandler PropertyChanged;

        public MetricCardViewModel()
        {
            RefreshCommand = new RelayCommand(ExecuteRefresh);
            OpenDetailsCommand = new RelayCommand(ExecuteOpenDetails);
        }

        #region Properties

        /// <summary>
        /// Main title of the metric
        /// </summary>
        public string Title
        {
            get => _title;
            set
            {
                if (_title != value)
                {
                    _title = value;
                    OnPropertyChanged(nameof(Title));
                }
            }
        }

        /// <summary>
        /// Category for filtering
        /// </summary>
        public string Category
        {
            get => _category;
            set { _category = value; OnPropertyChanged(nameof(Category)); }
        }

        /// <summary>
        /// Primary metric value
        /// </summary>
        public double Value
        {
            get => _value;
            set
            {
                if (_value != value)
                {
                    _value = value;
                    OnPropertyChanged(nameof(Value));
                    OnPropertyChanged(nameof(FormattedValue));
                    OnPropertyChanged(nameof(ProgressPercentage));
                    OnPropertyChanged(nameof(TargetProgressPercentage));
                }
            }
        }

        /// <summary>
        /// Unit displayed after the value (%, MB, GB, etc.)
        /// </summary>
        public string Unit
        {
            get => _unit;
            set
            {
                if (_unit != value)
                {
                    _unit = value;
                    OnPropertyChanged(nameof(Unit));
                    OnPropertyChanged(nameof(FormattedValue));
                }
            }
        }

        /// <summary>
        /// .NET format string for the value (N0, N2, P0, etc.)
        /// </summary>
        public string Format
        {
            get => _format;
            set
            {
                if (_format != value)
                {
                    _format = value;
                    OnPropertyChanged(nameof(Format));
                    OnPropertyChanged(nameof(FormattedValue));
                }
            }
        }

        /// <summary>
        /// Formatted display value with unit
        /// </summary>
        public string FormattedValue
        {
            get
            {
                var formattedValue = Value.ToString(Format, CultureInfo.CurrentCulture);
                return string.IsNullOrEmpty(Unit) ? formattedValue : $"{formattedValue} {Unit}";
            }
        }

        /// <summary>
        /// Trend indicator (Up, Down, Neutral, or custom text).
        /// When set manually, auto-calculation of trend on refresh is disabled.
        /// </summary>
        public string Trend
        {
            get => _trend;
            set
            {
                if (_trend != value)
                {
                    _trend = value;
                    OnPropertyChanged(nameof(Trend));
                    OnPropertyChanged(nameof(TrendColor));
                }
            }
        }

        /// <summary>
        /// Whether to auto-calculate trend direction on refresh.
        /// Defaults to true. Set to false to use manually specified Trend values.
        /// </summary>
        public bool AutoCalculateTrend
        {
            get => _autoCalculateTrend;
            set
            {
                if (_autoCalculateTrend != value)
                {
                    _autoCalculateTrend = value;
                    OnPropertyChanged(nameof(AutoCalculateTrend));
                }
            }
        }

        /// <summary>
        /// Numeric value for trend calculation
        /// </summary>
        public double TrendValue
        {
            get => _trendValue;
            set
            {
                if (_trendValue != value)
                {
                    _trendValue = value;
                    OnPropertyChanged(nameof(TrendValue));
                    OnPropertyChanged(nameof(FormattedTrendValue));
                }
            }
        }

        /// <summary>
        /// Formatted trend value with sign
        /// </summary>
        public string FormattedTrendValue
        {
            get
            {
                var sign = TrendValue >= 0 ? "+" : "";
                return $"{sign}{TrendValue:N1}";
            }
        }

        /// <summary>
        /// Color based on trend direction
        /// </summary>
        public Brush TrendColor
        {
            get
            {
                switch (Trend?.ToLowerInvariant())
                {
                    case "up":
                    case "↑":
                        return new SolidColorBrush(Color.FromRgb(0, 153, 0));   // Green
                    case "down":
                    case "↓":
                        return new SolidColorBrush(Color.FromRgb(204, 0, 0));   // Red
                    case "neutral":
                    case "stable":
                    case "→":
                        return new SolidColorBrush(Color.FromRgb(255, 128, 0)); // Orange
                    default:
                        return Brushes.Gray;
                }
            }
        }

        /// <summary>
        /// Icon glyph (Segoe MDL2 or Fluent icon)
        /// </summary>
        public string Icon
        {
            get => _icon;
            set
            {
                if (_icon != value)
                {
                    _icon = value;
                    OnPropertyChanged(nameof(Icon));
                }
            }
        }

        /// <summary>
        /// Optional description below the value
        /// </summary>
        public string Description
        {
            get => _description;
            set
            {
                if (_description != value)
                {
                    _description = value;
                    OnPropertyChanged(nameof(Description));
                    OnPropertyChanged(nameof(HasDescription));
                }
            }
        }

        /// <summary>
        /// Whether the card has a description to display
        /// </summary>
        public bool HasDescription => !string.IsNullOrEmpty(Description);

        /// <summary>
        /// Target label (e.g., "Goal", "Target")
        /// </summary>
        public string Target
        {
            get => _target;
            set
            {
                if (_target != value)
                {
                    _target = value;
                    OnPropertyChanged(nameof(Target));
                }
            }
        }

        /// <summary>
        /// Target value for comparison
        /// </summary>
        public double TargetValue
        {
            get => _targetValue;
            set
            {
                if (_targetValue != value)
                {
                    _targetValue = value;
                    OnPropertyChanged(nameof(TargetValue));
                    OnPropertyChanged(nameof(FormattedTargetValue));
                    OnPropertyChanged(nameof(TargetProgressPercentage));
                }
            }
        }

        /// <summary>
        /// Formatted target value
        /// </summary>
        public string FormattedTargetValue
        {
            get => TargetValue.ToString(Format, CultureInfo.CurrentCulture);
        }

        /// <summary>
        /// Minimum value for progress bar
        /// </summary>
        public double MinValue
        {
            get => _minValue;
            set
            {
                if (_minValue != value)
                {
                    _minValue = value;
                    OnPropertyChanged(nameof(MinValue));
                    OnPropertyChanged(nameof(ProgressPercentage));
                }
            }
        }

        /// <summary>
        /// Maximum value for progress bar
        /// </summary>
        public double MaxValue
        {
            get => _maxValue;
            set
            {
                if (_maxValue != value)
                {
                    _maxValue = value;
                    OnPropertyChanged(nameof(MaxValue));
                    OnPropertyChanged(nameof(ProgressPercentage));
                }
            }
        }

        /// <summary>
        /// Progress percentage (0-100)
        /// </summary>
        public double ProgressPercentage
        {
            get
            {
                if (MaxValue <= MinValue) return 0;
                return Math.Max(0, Math.Min(100, (Value - MinValue) / (MaxValue - MinValue) * 100));
            }
        }

        /// <summary>
        /// Progress towards target percentage
        /// </summary>
        public double TargetProgressPercentage
        {
            get
            {
                if (TargetValue <= 0) return 0;
                return Math.Max(0, Math.Min(100, (Value / TargetValue) * 100));
            }
        }

        /// <summary>
        /// Accent color for the card
        /// </summary>
        public Brush AccentBrush
        {
            get => _accentBrush;
            set
            {
                if (_accentBrush != value)
                {
                    _accentBrush = value;
                    OnPropertyChanged(nameof(AccentBrush));
                }
            }
        }

        /// <summary>
        /// Show/hide progress bar
        /// </summary>
        public bool ShowProgressBar
        {
            get => _showProgressBar;
            set
            {
                if (_showProgressBar != value)
                {
                    _showProgressBar = value;
                    OnPropertyChanged(nameof(ShowProgressBar));
                }
            }
        }

        /// <summary>
        /// Show/hide trend indicator
        /// </summary>
        public bool ShowTrend
        {
            get => _showTrend;
            set
            {
                if (_showTrend != value)
                {
                    _showTrend = value;
                    OnPropertyChanged(nameof(ShowTrend));
                }
            }
        }

        /// <summary>
        /// Show/hide target comparison
        /// </summary>
        public bool ShowTarget
        {
            get => _showTarget;
            set
            {
                if (_showTarget != value)
                {
                    _showTarget = value;
                    OnPropertyChanged(nameof(ShowTarget));
                }
            }
        }

        #endregion

        #region Commands

        public ICommand RefreshCommand { get; }
        public ICommand OpenDetailsCommand { get; }

        #endregion

        #region Command Implementations

        private async void ExecuteRefresh(object parameter)
        {
            if (string.IsNullOrEmpty(_refreshScript) || _isRefreshing)
                return;

            _isRefreshing = true;
            OnPropertyChanged(nameof(IsRefreshing));

            try
            {
                var result = await ExecuteRefreshScriptAsync(_refreshScript);
                if (result != null)
                {
                    UpdateFromRefreshResult(result);
                }
            }
            catch (Exception ex)
            {
                LoggingService.Error($"MetricCard refresh failed: {ex.Message}", component: "MetricCardViewModel");
            }
            finally
            {
                _isRefreshing = false;
                OnPropertyChanged(nameof(IsRefreshing));
            }
        }

        private async Task<object> ExecuteRefreshScriptAsync(string script)
        {
            return await Task.Run(() =>
            {
                using (var runner = new ScriptCardRunner())
                {
                    object lastResult = null;

                    runner.OutputReceived += (s, output) =>
                    {
                        // Capture the last non-empty output (string or numeric)
                        if (!string.IsNullOrWhiteSpace(output))
                        {
                            // Try to parse as numeric first
                            double numericValue;
                            if (double.TryParse(output.Trim(), out numericValue))
                            {
                                lastResult = numericValue;
                            }
                            else
                            {
                                // Keep as string if not numeric
                                lastResult = output.Trim();
                            }
                        }
                    };

                    runner.ExecuteAsync(script, null, CancellationToken.None).Wait();
                    return lastResult;
                }
            });
        }

        private void UpdateFromRefreshResult(object result)
        {
            if (Application.Current != null && Application.Current.Dispatcher != null)
            {
                Application.Current.Dispatcher.Invoke(() =>
                {
                    // Handle both numeric and string values
                    if (result is string strVal && !string.IsNullOrEmpty(strVal))
                    {
                        // For string values, we can't calculate trends or use numeric formatting
                        // Set the value to 0 and update description to show the string
                        Value = 0;
                        Description = strVal;
                        ShowTrend = false;
                        ShowTarget = false;
                        ShowProgressBar = false;
                    }
                    else
                    {
                        double newValue = Value;

                        if (result is double dVal)
                        {
                            newValue = dVal;
                        }
                        else if (result is int iVal)
                        {
                            newValue = iVal;
                        }
                        else
                        {
                            double parsed;
                            if (double.TryParse(result?.ToString(), out parsed))
                            {
                                newValue = parsed;
                            }
                        }

                        // Auto-calculate trend comparing new value to current displayed value
                        // Use Value (current) as the baseline, not _previousValue which lags behind
                        if (_autoCalculateTrend)
                        {
                            double oldValue = Value;
                            double diff = newValue - oldValue;
                            double threshold = Math.Abs(oldValue) * 0.005; // 0.5% threshold for "stable"
                            
                            // Only show trend after first refresh (when we have a baseline)
                            if (_previousValue.HasValue || oldValue != 0)
                            {
                                if (diff > threshold)
                                {
                                    Trend = "up";
                                    TrendValue = Math.Round(diff, 2);
                                    ShowTrend = true;
                                }
                                else if (diff < -threshold)
                                {
                                    Trend = "down";
                                    TrendValue = Math.Round(diff, 2);
                                    ShowTrend = true;
                                }
                                else
                                {
                                    Trend = "stable";
                                    TrendValue = 0;
                                    ShowTrend = true;
                                }
                            }
                            
                            // Mark that we've had at least one refresh
                            _previousValue = oldValue;
                        }

                        Value = newValue;
                    }
                });
            }
        }

        private void ExecuteOpenDetails(object parameter)
        {
            // Open detailed view or dialog
            // Implementation depends on requirements
        }

        #endregion

        #region Properties for Refresh

        /// <summary>
        /// PowerShell script to execute for refreshing the metric value
        /// </summary>
        public string RefreshScript
        {
            get => _refreshScript;
            set
            {
                if (_refreshScript != value)
                {
                    _refreshScript = value;
                    OnPropertyChanged(nameof(RefreshScript));
                    OnPropertyChanged(nameof(CanRefresh));
                }
            }
        }

        /// <summary>
        /// Indicates if refresh is currently in progress
        /// </summary>
        public bool IsRefreshing => _isRefreshing;

        /// <summary>
        /// Indicates if the card can be refreshed (has a refresh script)
        /// </summary>
        public bool CanRefresh => !string.IsNullOrEmpty(_refreshScript);

        #endregion

        #region Helper Methods

        /// <summary>
        /// Update metric from PowerShell output
        /// </summary>
        public void UpdateFromPowerShellOutput(object output)
        {
            if (output is PSObject psObj)
            {
                // Try to extract common properties
                if (psObj.Properties["Value"] != null && psObj.Properties["Value"].Value is double val) Value = val;
                if (psObj.Properties["Title"] != null && psObj.Properties["Title"].Value is string title) Title = title;
                if (psObj.Properties["Unit"] != null && psObj.Properties["Unit"].Value is string unit) Unit = unit;
                if (psObj.Properties["Trend"] != null && psObj.Properties["Trend"].Value is string trend) Trend = trend;
                if (psObj.Properties["TrendValue"] != null && psObj.Properties["TrendValue"].Value is double trendVal) TrendValue = trendVal;
                if (psObj.Properties["Description"] != null && psObj.Properties["Description"].Value is string desc) Description = desc;
                if (psObj.Properties["Target"] != null && psObj.Properties["Target"].Value is double target) TargetValue = target;
            }
            else if (output is double doubleValue)
            {
                Value = doubleValue;
            }
            else if (output is int intValue)
            {
                Value = intValue;
            }
        }

        /// <summary>
        /// Set color based on value ranges
        /// </summary>
        public void SetStatusColor(double warningThreshold = 70, double criticalThreshold = 90)
        {
            if (Value >= criticalThreshold)
            {
                AccentBrush = new SolidColorBrush(Color.FromRgb(204, 0, 0)); // Red
            }
            else if (Value >= warningThreshold)
            {
                AccentBrush = new SolidColorBrush(Color.FromRgb(255, 128, 0)); // Orange
            }
            else
            {
                AccentBrush = new SolidColorBrush(Color.FromRgb(0, 153, 0)); // Green
            }
        }

        #endregion

        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}


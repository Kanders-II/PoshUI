// Copyright (c) 2025 A Solution IT LLC. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Management.Automation;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Threading;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;
using System.Windows.Media;
using Launcher.Services;

namespace Launcher.ViewModels
{
    /// <summary>
    /// ViewModel for displaying chart/graph cards in CardGrid with custom WPF rendering
    /// Supports Line, Bar, Area, and Pie charts with multiple series, custom colors, and dynamic updates
    /// </summary>
    public class GraphCardViewModel : INotifyPropertyChanged
    {
        private string _title = string.Empty;
        private string _description = string.Empty;
        private object _data;
        private string _chartType = "Bar";
        private bool _showLegend = true;
        private bool _showTooltip = true;
        private Brush _foreground = Brushes.White;
        private Brush _accentBrush = new SolidColorBrush(Color.FromRgb(0, 120, 212)); // Windows blue
        private string _icon = string.Empty;
        private string _category = "General";
        private string _refreshScript;
        private bool _isRefreshing;
        private double[] _chartValues;
        private string[] _chartLabels;
        private string _chartColor = "#4CAF50"; // Default green for line/area charts
        private static int _chartColorIndex = 0; // Static counter for auto-assigning colors
        private static readonly string[] ChartColorPalette = { "#4CAF50", "#2196F3", "#FF9800", "#E91E63", "#9C27B0", "#00BCD4", "#FFC107", "#795548" };

        public event PropertyChangedEventHandler PropertyChanged;

        public GraphCardViewModel()
        {
            // Chart data will be set when data is available
            DataPoints = new ObservableCollection<object>();
            
            // Auto-assign a unique color to each chart from the palette
            _chartColor = ChartColorPalette[_chartColorIndex % ChartColorPalette.Length];
            _chartColorIndex++;
            
            RefreshCommand = new RelayCommand(ExecuteRefresh);
            ExportCommand = new RelayCommand((_) => ExecuteExport());
            OpenDetailsCommand = new RelayCommand((_) => ExecuteOpenDetails());
        }
        
        /// <summary>
        /// The color used for line/area charts (hex string like "#4CAF50")
        /// </summary>
        public string ChartColor
        {
            get => _chartColor;
            set
            {
                if (_chartColor != value)
                {
                    _chartColor = value;
                    OnPropertyChanged(nameof(ChartColor));
                    OnPropertyChanged(nameof(ChartColorBrush));
                }
            }
        }
        
        /// <summary>
        /// Brush version of ChartColor for XAML binding
        /// </summary>
        public Brush ChartColorBrush
        {
            get
            {
                try
                {
                    return new SolidColorBrush((Color)ColorConverter.ConvertFromString(_chartColor));
                }
                catch
                {
                    return new SolidColorBrush(Color.FromRgb(76, 175, 80)); // Default green
                }
            }
        }
        
        private void AddSampleData()
        {
            // Only add sample data if no actual data loaded after sufficient time
            var sampleValues = new[] { 10.0, 20.0, 30.0, 25.0 };
            var sampleLabels = new[] { "A", "B", "C", "D" };
            CreateSeries(sampleValues, sampleLabels);
        }

        #region Properties

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

        public string Description
        {
            get => _description;
            set
            {
                if (_description != value)
                {
                    _description = value;
                    OnPropertyChanged(nameof(Description));
                }
            }
        }

        public object Data
        {
            get => _data;
            set
            {
                if (_data != value)
                {
                    _data = value;
                    OnPropertyChanged(nameof(Data));
                    // Load data immediately - timing is handled by CardGridViewModel
                    if (value != null)
                    {
                        System.Diagnostics.Debug.WriteLine($"GraphCard Data setter: type={value.GetType().Name}, title={Title}");
                        try
                        {
                            LoadDataInternal(value);
                            System.Diagnostics.Debug.WriteLine($"GraphCard Data setter: after LoadDataInternal, ChartValues={ChartValues?.Length ?? 0}");
                        }
                        catch (Exception ex)
                        {
                            System.Diagnostics.Debug.WriteLine($"Error in LoadData: {ex.Message}\n{ex.StackTrace}");
                        }
                    }
                }
            }
        }

        public string ChartType
        {
            get => _chartType;
            set
            {
                if (_chartType != value)
                {
                    _chartType = value;
                    OnPropertyChanged(nameof(ChartType));
                    // WPF will automatically re-evaluate IsPieChart and IsCartesianChart
                    UpdateChartType();
                }
            }
        }

        public bool ShowLegend
        {
            get => _showLegend;
            set
            {
                if (_showLegend != value)
                {
                    _showLegend = value;
                    OnPropertyChanged(nameof(ShowLegend));
                }
            }
        }

        public bool ShowTooltip
        {
            get => _showTooltip;
            set
            {
                if (_showTooltip != value)
                {
                    _showTooltip = value;
                    OnPropertyChanged(nameof(ShowTooltip));
                }
            }
        }

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

        public string Category
        {
            get => _category;
            set
            {
                if (_category != value)
                {
                    _category = value;
                    OnPropertyChanged(nameof(Category));
                }
            }
        }

        public double[] ChartValues
        {
            get => _chartValues;
            private set
            {
                if (_chartValues != value)
                {
                    _chartValues = value;
                    OnPropertyChanged(nameof(ChartValues));
                }
            }
        }

        public string[] ChartLabels
        {
            get => _chartLabels;
            private set
            {
                if (_chartLabels != value)
                {
                    _chartLabels = value;
                    OnPropertyChanged(nameof(ChartLabels));
                }
            }
        }

        public ObservableCollection<object> DataPoints { get; set; } = new ObservableCollection<object>();
        
        /// <summary>
        /// Line chart data points with individual colors
        /// </summary>
        public ObservableCollection<LinePoint> LinePoints { get; } = new ObservableCollection<LinePoint>();
        
        public class LinePoint
        {
            public double Value { get; set; }
            public string Label { get; set; } = "";
            public string Color { get; set; } = "#4CAF50"; // Default green
            public int Index { get; set; }
        }
        
        /// <summary>
        /// Simple bar chart data for backward compatibility
        /// </summary>
        public ObservableCollection<ChartBar> ChartBars { get; } = new ObservableCollection<ChartBar>();
        
        /// <summary>
        /// Maximum value in chart data, used for Y-axis scaling
        /// </summary>
        private double _maxValue = 100;
        public double MaxValue
        {
            get => _maxValue;
            private set
            {
                if (_maxValue != value)
                {
                    _maxValue = value;
                    OnPropertyChanged(nameof(MaxValue));
                    OnPropertyChanged(nameof(YAxisLabels));
                }
            }
        }
        
        /// <summary>
        /// Y-axis labels for chart display (5 labels from 0 to max)
        /// </summary>
        public string[] YAxisLabels
        {
            get
            {
                return new[]
                {
                    MaxValue.ToString("N0"),
                    (MaxValue * 0.75).ToString("N0"),
                    (MaxValue * 0.5).ToString("N0"),
                    (MaxValue * 0.25).ToString("N0"),
                    "0"
                };
            }
        }
        
        public class ChartBar
        {
            public double Height { get; set; }
            public double ScaledHeight { get; set; } // Pre-calculated height for display (0-120)
            public string Label { get; set; } = "";
            public string Value { get; set; } = "";
            public double NumericValue { get; set; }
            public string Color { get; set; } = "#2196F3"; // Default blue
        }
        
        /// <summary>
        /// Pie chart data with labels, values, and percentages
        /// </summary>
        private ObservableCollection<PieSliceData> _pieChartData = new ObservableCollection<PieSliceData>();
        public ObservableCollection<PieSliceData> PieChartData
        {
            get => _pieChartData;
            private set
            {
                if (_pieChartData != value)
                {
                    _pieChartData = value;
                    OnPropertyChanged(nameof(PieChartData));
                }
            }
        }
        
        public class PieSliceData
        {
            public string Label { get; set; } = "";
            public double Value { get; set; }
            public string Percentage { get; set; } = "";
            public string Color { get; set; } = "#2196F3";
        }

        public ICommand RefreshCommand { get; }
        public ICommand ExportCommand { get; }
        public ICommand OpenDetailsCommand { get; }

        // Helper properties for XAML visibility bindings
        private bool _hasData = false;
        public bool HasData 
        { 
            get => _hasData; 
            private set 
            { 
                if (_hasData != value) 
                { 
                    _hasData = value; 
                    OnPropertyChanged(nameof(HasData));
                } 
            } 
        }
        
        public bool IsPieChart => HasData && ChartType.Equals("Pie", StringComparison.OrdinalIgnoreCase);
        public bool IsCartesianChart => HasData && !ChartType.Equals("Pie", StringComparison.OrdinalIgnoreCase);

        #endregion

        #region Refresh Support

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

        public bool IsRefreshing => _isRefreshing;
        public bool CanRefresh => !string.IsNullOrEmpty(_refreshScript);

        private async void ExecuteRefresh(object parameter)
        {
            if (string.IsNullOrEmpty(RefreshScript)) return;

            _isRefreshing = true;
            OnPropertyChanged(nameof(IsRefreshing));

            try
            {
                var result = await ExecuteRefreshScriptAsync(RefreshScript);
                UpdateFromRefreshResult(result);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error refreshing graph: {ex.Message}");
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
                using (var ps = System.Management.Automation.PowerShell.Create())
                {
                    ps.AddScript(script);
                    var result = ps.Invoke();
                    
                    if (result.Count > 0)
                    {
                        var tuples = new List<Tuple<string, double>>();
                        
                        foreach (var item in result)
                        {
                            string label = null;
                            double? value = null;
                            
                            if (item is PSObject psObj)
                            {
                                // Check if BaseObject is a Hashtable (from @{ Label=...; Value=... })
                                if (psObj.BaseObject is System.Collections.Hashtable ht)
                                {
                                    label = ht["Label"]?.ToString();
                                    var valObj = ht["Value"];
                                    if (valObj != null && double.TryParse(valObj.ToString(), out double v))
                                        value = v;
                                }
                                // Check if it has Label/Value properties directly
                                else if (psObj.Properties["Label"] != null && psObj.Properties["Value"] != null)
                                {
                                    label = psObj.Properties["Label"]?.Value?.ToString();
                                    var valObj = psObj.Properties["Value"]?.Value;
                                    if (valObj != null && double.TryParse(valObj.ToString(), out double v))
                                        value = v;
                                }
                            }
                            
                            if (label != null && value.HasValue)
                            {
                                tuples.Add(Tuple.Create(label, value.Value));
                            }
                        }
                        
                        if (tuples.Count > 0)
                        {
                            return tuples.ToArray();
                        }
                        
                        // Fallback
                        return result[0].BaseObject;
                    }
                    return null;
                }
            });
        }

        private void UpdateFromRefreshResult(object result)
        {
            if (result == null) return;
            
            // Ensure we run on UI thread
            if (Application.Current?.Dispatcher != null && !Application.Current.Dispatcher.CheckAccess())
            {
                Application.Current.Dispatcher.Invoke(() => UpdateFromRefreshResult(result));
                return;
            }
            
            try
            {
                // If result is an array of Tuple<string, double> from ExecuteRefreshScriptAsync
                if (result is Tuple<string, double>[] tupleArray && tupleArray.Length > 0)
                {
                    var values = tupleArray.Select(t => t.Item2).ToList();
                    var labels = tupleArray.Select(t => t.Item1).ToList();
                    
                    // Directly update chart data
                    ChartBars.Clear();
                    ChartValues = values.ToArray();
                    ChartLabels = labels.ToArray();
                    
                    // Color palette for charts (same as CreateSeries)
                    string[] chartColors = { "#2196F3", "#4CAF50", "#FF9800", "#E91E63", "#9C27B0", "#00BCD4", "#FFC107", "#795548" };
                    
                    // Update bar chart data
                    double maxValue = values.Max();
                    for (int i = 0; i < values.Count; i++)
                    {
                        ChartBars.Add(new ChartBar
                        {
                            Label = labels[i],
                            Value = values[i].ToString("N1"),
                            NumericValue = values[i],
                            ScaledHeight = maxValue > 0 ? (values[i] / maxValue * 150) : 0,
                            Color = chartColors[i % chartColors.Length]
                        });
                    }
                    
                    OnPropertyChanged(nameof(ChartValues));
                    OnPropertyChanged(nameof(ChartLabels));
                    OnPropertyChanged(nameof(ChartBars));
                    OnPropertyChanged(nameof(HasData));
                    System.Diagnostics.Debug.WriteLine($"GraphCard refresh complete: {values.Count} data points");
                    return;
                }
                
                // Fallback to default Data setter
                Data = result;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error in UpdateFromRefreshResult: {ex.Message}");
                Data = result;
            }
        }

        #endregion

        #region Methods
        
        private void LoadData(object data)
        {
            if (data == null)
            {
                System.Diagnostics.Debug.WriteLine("LoadData: data is null, skipping");
                return;
            }

            try
            {
                System.Diagnostics.Debug.WriteLine($"LoadData called with data type: {data.GetType().Name}");
                
                // Always defer data processing with a small delay to ensure UI controls are fully initialized
                if (Application.Current != null && Application.Current.Dispatcher != null)
                {
                    // Use a timer to delay data loading slightly to ensure chart controls are ready
                    var timer = new DispatcherTimer
                    {
                        Interval = TimeSpan.FromMilliseconds(100)
                    };
                    timer.Tick += (s, e) =>
                    {
                        timer.Stop();
                        try
                        {
                            LoadDataInternal(data);
                        }
                        catch (Exception ex)
                        {
                            System.Diagnostics.Debug.WriteLine($"Error in deferred LoadData: {ex.Message}\n{ex.StackTrace}");
                        }
                    };
                    timer.Start();
                    return;
                }
                
                // Fallback if no dispatcher available
                LoadDataInternal(data);
            }
            catch (Exception ex)
            {
                // Log error but don't crash
                System.Diagnostics.Debug.WriteLine($"Error loading chart data: {ex.Message}\n{ex.StackTrace}");
            }
        }
        
        private void LoadDataInternal(object data)
        {
            if (data == null) return;
            
            // Ensure we're on UI thread
            if (Application.Current != null && Application.Current.Dispatcher != null && !Application.Current.Dispatcher.CheckAccess())
            {
                Application.Current.Dispatcher.Invoke(() => LoadDataInternal(data));
                return;
            }
            
            try
            {
                ChartBars.Clear();
                DataPoints.Clear();

                System.Diagnostics.Debug.WriteLine($"Processing data type: {data.GetType().Name}");

                // Handle JSON string from PowerShell
                if (data is string jsonString)
                {
                    // Try to parse as JSON array
                    if (jsonString.TrimStart().StartsWith("[") || jsonString.TrimStart().StartsWith("{"))
                    {
                        LoadFromJson(jsonString);
                    }
                    else
                    {
                        // Try CSV
                        LoadFromCsv(jsonString);
                    }
                }
                // Handle PowerShell array
                else if (data is object[] dataArray)
                {
                    LoadFromArray(dataArray);
                }
                // Handle PSObject collection
                else if (data is System.Collections.IEnumerable enumerable && !(data is string))
                {
                    var list = new List<object>();
                    foreach (var item in enumerable)
                    {
                        list.Add(item);
                    }
                    LoadFromArray(list.ToArray());
                }
                
                // If no data was loaded, add sample data
                if (ChartBars.Count == 0 && (ChartValues == null || ChartValues.Length == 0))
                {
                    AddSampleData();
                }
            }
            catch (Exception ex)
            {
                // Log error but don't crash
                System.Diagnostics.Debug.WriteLine($"Error loading chart data: {ex.Message}\n{ex.StackTrace}");
            }
        }
        
        /// <summary>
        /// Simple JSON parser for arrays of objects with Label/Value properties
        /// </summary>
        private void LoadFromJson(string jsonString)
        {
            try
            {
                System.Diagnostics.Debug.WriteLine($"LoadFromJson: Input length={jsonString?.Length ?? 0}");
                
                if (string.IsNullOrEmpty(jsonString))
                {
                    System.Diagnostics.Debug.WriteLine("LoadFromJson: Empty input");
                    return;
                }
                
                var values = new List<double>();
                var labels = new List<string>();
                
                // Simple regex-based JSON parsing for arrays of objects
                // Pattern: [{"Label":"...","Value":...}, ...]
                var pattern = @"\{[^}]*""(?:Label|label)""\s*:\s*""([^""]*)""[^}]*""(?:Value|value)""\s*:\s*([0-9.]+)[^}]*\}";
                var matches = Regex.Matches(jsonString, pattern);
                
                foreach (Match match in matches)
                {
                    if (match.Groups.Count >= 3)
                    {
                        var label = match.Groups[1].Value;
                        double value;
                        if (double.TryParse(match.Groups[2].Value, out value))
                        {
                            labels.Add(label);
                            values.Add(value);
                        }
                    }
                }
                
                // If regex didn't work, try a simpler approach - look for Label/Value pairs
                if (values.Count == 0)
                {
                    // Try parsing as simple array: [{"Label":"A","Value":10}, ...]
                    var labelPattern = @"""Label""\s*:\s*""([^""]*)""";
                    var valuePattern = @"""Value""\s*:\s*([0-9.]+)";
                    
                    var labelMatches = Regex.Matches(jsonString, labelPattern, RegexOptions.IgnoreCase);
                    var valueMatches = Regex.Matches(jsonString, valuePattern, RegexOptions.IgnoreCase);
                    
                    int count = Math.Min(labelMatches.Count, valueMatches.Count);
                    for (int i = 0; i < count; i++)
                    {
                        var label = labelMatches[i].Groups[1].Value;
                        double value;
                        if (double.TryParse(valueMatches[i].Groups[1].Value, out value))
                        {
                            labels.Add(label);
                            values.Add(value);
                        }
                    }
                }
                
                if (values.Count > 0)
                {
                    CreateSeries(values.ToArray(), labels.ToArray());
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"LoadFromJson ERROR: {ex.Message}");
            }
        }

        private void LoadFromCsv(string csv)
        {
            var lines = csv.Split('\n');
            if (lines.Length < 2) return;

            var headers = lines[0].Split(',');
            var values = new List<double>();
            var labels = new List<string>();

            for (int i = 1; i < lines.Length; i++)
            {
                var parts = lines[i].Split(',');
                if (parts.Length > 1)
                {
                    double value;
                    if (double.TryParse(parts[1], out value))
                    {
                        values.Add(value);
                        labels.Add(parts[0] ?? $"Item{i}");
                    }
                }
            }

            if (values.Count > 0)
            {
                CreateSeries(values.ToArray(), labels.ToArray());
            }
        }

        private void LoadFromArray(object[] array)
        {
            if (array.Length == 0) return;

            var values = new List<double>();
            var labels = new List<string>();

            foreach (var item in array)
            {
                if (item is PSObject psObj)
                {
                    // Try to find numeric properties
                    var numericProp = psObj.Properties.FirstOrDefault(p => p.Value is double || p.Value is int);
                    var labelProp = psObj.Properties.FirstOrDefault(p => p.Name.Equals("Name", StringComparison.OrdinalIgnoreCase) ||
                                                                       p.Name.Equals("Label", StringComparison.OrdinalIgnoreCase) ||
                                                                       p.Name.Equals("X", StringComparison.OrdinalIgnoreCase));

                    if (numericProp != null)
                    {
                        double value;
                        if (double.TryParse(numericProp.Value?.ToString(), out value))
                        {
                            values.Add(value);
                            labels.Add(labelProp?.Value?.ToString() ?? values.Count.ToString());
                        }
                    }
                }
                else
                {
                    // Handle anonymous objects
                    var props = item.GetType().GetProperties();
                    var numericProp = props.FirstOrDefault(p => p.PropertyType == typeof(double) || p.PropertyType == typeof(int));
                    var labelProp = props.FirstOrDefault(p => p.Name.Equals("Label", StringComparison.OrdinalIgnoreCase) ||
                                                           p.Name.Equals("Name", StringComparison.OrdinalIgnoreCase));
                    
                    if (numericProp != null)
                    {
                        var value = Convert.ToDouble(numericProp.GetValue(item));
                        values.Add(value);
                        labels.Add(labelProp?.GetValue(item)?.ToString() ?? props.FirstOrDefault()?.Name ?? values.Count.ToString());
                    }
                }
            }

            if (values.Count > 0)
            {
                CreateSeries(values.ToArray(), labels.ToArray());
            }
        }

        private void CreateSeries(double[] values, string[] labels)
        {
            try
            {
                if (values == null || labels == null || values.Length == 0 || labels.Length == 0)
                {
                    System.Diagnostics.Debug.WriteLine("CreateSeries: Invalid data - values or labels are null/empty");
                    return;
                }
                
                System.Diagnostics.Debug.WriteLine($"CreateSeries: ChartType={ChartType}, Values={values.Length}, Labels={labels.Length}");
                
                // Ensure application is initialized
                if (Application.Current == null)
                {
                    System.Diagnostics.Debug.WriteLine("CreateSeries: Application.Current is null - deferring");
                    return;
                }
                
                // Ensure we're on the UI thread for collection updates
                if (Application.Current.Dispatcher != null && !Application.Current.Dispatcher.CheckAccess())
                {
                    Application.Current.Dispatcher.BeginInvoke(new Action(() => CreateSeries(values, labels)), 
                        DispatcherPriority.Loaded);
                    return;
                }
                
                // Update DataPoints for backward compatibility
                DataPoints.Clear();
                for (int i = 0; i < Math.Min(values.Length, labels.Length); i++)
                {
                    DataPoints.Add(new { Label = labels[i], Value = values[i] });
                }
                
                // Set chart data arrays
                ChartValues = values;
                ChartLabels = labels;
                
                // Calculate max value for Y-axis scaling
                double actualMax = values.Length > 0 ? values.Max() : 100;
                if (actualMax <= 100) MaxValue = 100;
                else if (actualMax <= 200) MaxValue = 200;
                else if (actualMax <= 500) MaxValue = 500;
                else if (actualMax <= 1000) MaxValue = 1000;
                else if (actualMax <= 2000) MaxValue = 2000;
                else MaxValue = Math.Ceiling(actualMax / 1000) * 1000;
                
                // Color palette for charts (professional, accessible colors)
                string[] chartColors = { "#2196F3", "#4CAF50", "#FF9800", "#E91E63", "#9C27B0", "#00BCD4", "#FFC107", "#795548" };
                
                // Update LinePoints with colors
                LinePoints.Clear();
                for (int i = 0; i < Math.Min(values.Length, labels.Length); i++)
                {
                    LinePoints.Add(new LinePoint
                    {
                        Value = values[i],
                        Label = labels[i] ?? $"Item {i + 1}",
                        Color = chartColors[i % chartColors.Length],
                        Index = i
                    });
                }
                
                // Update ChartBars with pre-calculated ScaledHeight and colors
                ChartBars.Clear();
                double chartHeight = 120; // Max bar height in pixels
                for (int i = 0; i < Math.Min(values.Length, labels.Length); i++)
                {
                    double scaledHeight = actualMax > 0 ? (values[i] / actualMax) * chartHeight : 0;
                    ChartBars.Add(new ChartBar { 
                        Height = values[i], 
                        ScaledHeight = scaledHeight,
                        NumericValue = values[i],
                        Label = labels[i] ?? $"Item {i + 1}", 
                        Value = values[i].ToString("N0"),
                        Color = chartColors[i % chartColors.Length]
                    });
                }
                
                // Always create pie chart data with percentages (needed for legend display)
                if (values.Length > 0)
                {
                    // Use same color palette for consistency
                    double total = values.Sum();
                    var pieData = new ObservableCollection<PieSliceData>();
                    
                    for (int i = 0; i < values.Length; i++)
                    {
                        double percentage = total > 0 ? (values[i] / total) * 100 : 0;
                        pieData.Add(new PieSliceData
                        {
                            Label = i < labels.Length ? labels[i] : $"Item {i + 1}",
                            Value = values[i],
                            Percentage = $"{percentage:F0}%",
                            Color = chartColors[i % chartColors.Length]
                        });
                    }
                    
                    PieChartData = pieData;
                }
                
                OnPropertyChanged(nameof(DataPoints));
                
                // Mark that we have data
                if (values.Length > 0)
                {
                    HasData = true;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error in CreateSeries: {ex.Message}\n{ex.StackTrace}");
                // Don't crash - just log the error
            }
        }
        
        private void UpdateChartType()
        {
            // Re-create series when chart type changes
            if (DataPoints.Count > 0)
            {
                var values = DataPoints.Cast<dynamic>().Select(x => (double)x.Value).ToArray();
                var labels = DataPoints.Cast<dynamic>().Select(x => (string)x.Label).ToArray();
                CreateSeries(values, labels);
            }
        }

        private void ExecuteExport()
        {
            // TODO: Implement chart export functionality (PNG, SVG, etc.)
            System.Diagnostics.Debug.WriteLine("Export chart - not yet implemented");
        }

        private void ExecuteOpenDetails()
        {
            // TODO: Implement chart details view
            System.Diagnostics.Debug.WriteLine("Open chart details - not yet implemented");
        }

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        #endregion
    }
}



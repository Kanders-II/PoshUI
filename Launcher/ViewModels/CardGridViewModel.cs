// Copyright (c) 2025 A Solution IT LLC. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.Serialization.Json;
using System.Text;
using System.IO;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Management.Automation;
using Launcher.Services;
using Launcher.Views;

namespace Launcher.ViewModels
{
    /// <summary>
    /// ViewModel for CardGrid view mode - displays script cards in a responsive grid.
    /// </summary>
    public class CardGridViewModel : INotifyPropertyChanged
    {
        private string _title;
        private string _description;
        private int _gridColumns = 3;
        private string _selectedCategory;

        public string Title { get => _title; set { _title = value; OnPropertyChanged(); } }
        public string Description { get => _description; set { _description = value; OnPropertyChanged(); } }
        
        public int GridColumns 
        { 
            get => _gridColumns; 
            set { _gridColumns = Math.Max(1, Math.Min(6, value)); OnPropertyChanged(); } 
        }

        public string SelectedCategory
        {
            get => _selectedCategory;
            set
            {
                _selectedCategory = value;
                OnPropertyChanged();
                FilterCards();
            }
        }

        /// <summary>
        /// All script cards in this step.
        /// </summary>
        public ObservableCollection<ScriptCardViewModel> ScriptCards { get; } = new ObservableCollection<ScriptCardViewModel>();

        /// <summary>
        /// All cards (any type) for mixed-type CardGrid.
        /// </summary>
        public ObservableCollection<object> AllCards { get; } = new ObservableCollection<object>();

        /// <summary>
        /// Filtered cards based on selected category.
        /// </summary>
        public ObservableCollection<ScriptCardViewModel> FilteredCards { get; } = new ObservableCollection<ScriptCardViewModel>();

        /// <summary>
        /// Filtered cards (any type) for mixed-type CardGrid.
        /// </summary>
        public ObservableCollection<object> FilteredAllCards { get; } = new ObservableCollection<object>();

        /// <summary>
        /// Available categories extracted from cards.
        /// </summary>
        public ObservableCollection<string> Categories { get; } = new ObservableCollection<string>();

        /// <summary>
        /// Command to refresh all cards that have refresh scripts.
        /// </summary>
        public ICommand RefreshAllCommand { get; }

        /// <summary>
        /// Indicates if any refresh operation is in progress.
        /// </summary>
        private bool _isRefreshing;
        public bool IsRefreshing
        {
            get => _isRefreshing;
            private set
            {
                if (_isRefreshing != value)
                {
                    _isRefreshing = value;
                    OnPropertyChanged(nameof(IsRefreshing));
                }
            }
        }

        public CardGridViewModel()
        {
            Categories.Add("All");
            SelectedCategory = "All";
            RefreshAllCommand = new RelayCommand(ExecuteRefreshAll, CanExecuteRefreshAll);
        }
        
        private bool CanExecuteRefreshAll(object parameter)
        {
            return !IsRefreshing;
        }

        /// <summary>
        /// Executes refresh on all cards that have refresh scripts.
        /// </summary>
        private async void ExecuteRefreshAll(object parameter)
        {
            LoggingService.Info("Refreshing all cards...", component: "CardGridViewModel");
            
            IsRefreshing = true;
            
            try
            {
                var refreshTasks = new List<Task>();
                
                foreach (var card in FilteredAllCards)
                {
                    if (card is MetricCardViewModel mc && mc.CanRefresh)
                    {
                        mc.RefreshCommand.Execute(null);
                    }
                    else if (card is GraphCardViewModel gc && gc.CanRefresh)
                    {
                        gc.RefreshCommand.Execute(null);
                    }
                    else if (card is DataGridCardViewModel dc && dc.CanRefresh)
                    {
                        dc.RefreshCommand.Execute(null);
                    }
                }
                
                // Wait a moment for async operations to complete
                await Task.Delay(2000);
            }
            finally
            {
                IsRefreshing = false;
                LoggingService.Info("Refresh completed", component: "CardGridViewModel");
            }
        }
        
        /// <summary>
        /// Loads cards from step Controls collection (for Add-WizardBanner, Add-WizardVisualizationCard, etc.)
        /// </summary>
        public void LoadCardsFromControls(System.Collections.IList controls)
        {
            LoggingService.Info($"*** LoadCardsFromControls CALLED with {controls?.Count ?? 0} controls", component: "CardGridViewModel");
            
            if (controls == null || controls.Count == 0)
            {
                LoggingService.Warn("No controls provided for CardGrid", component: "CardGridViewModel");
                return;
            }

            try
            {
                LoggingService.Info($"Loading {controls.Count} cards from Controls", component: "CardGridViewModel");
                
                foreach (var controlObj in controls)
                {
                    // Use dynamic to access PowerShell object properties
                    dynamic control = controlObj;
                    
                    string controlType = control.Type?.ToString() ?? "Unknown";
                    string controlName = control.Name?.ToString() ?? "Unnamed";
                    
                    LoggingService.Info($"Processing control: Type={controlType}, Name={controlName}", component: "CardGridViewModel");
                    
                    // Helper function to get property value
                    Func<string, string> GetPropValue = (key) =>
                    {
                        try
                        {
                            if (control.Properties != null && control.Properties.ContainsKey(key))
                            {
                                return control.Properties[key]?.ToString() ?? "";
                            }
                        }
                        catch { }
                        return "";
                    };
                    
                    // Convert control properties to CardData
                    var cardData = new CardData
                    {
                        Type = controlType,
                        Name = controlName,
                        Title = GetPropValue("CardTitle") != "" ? GetPropValue("CardTitle") : control.Label?.ToString() ?? "",
                        Description = GetPropValue("CardDescription"),
                        Icon = GetPropValue("Icon"),
                        Category = GetPropValue("Category") != "" ? GetPropValue("Category") : "General"
                    };
                    
                    // Add type-specific properties
                    if (control.Properties != null)
                    {
                        foreach (var propKey in control.Properties.Keys)
                        {
                            string key = propKey.ToString();
                            var propValue = control.Properties[propKey];
                            
                            switch (key)
                            {
                                case "Value":
                                    double val;
                                    if (double.TryParse(propValue?.ToString(), out val))
                                        cardData.Value = val;
                                    break;
                                case "Unit":
                                    cardData.Unit = propValue?.ToString();
                                    break;
                                case "Format":
                                    cardData.Format = propValue?.ToString();
                                    break;
                                case "Trend":
                                    cardData.Trend = propValue?.ToString();
                                    break;
                                case "TrendValue":
                                    double trendVal;
                                    if (double.TryParse(propValue?.ToString(), out trendVal))
                                        cardData.TrendValue = trendVal;
                                    break;
                                case "Target":
                                    double target;
                                    if (double.TryParse(propValue?.ToString(), out target))
                                        cardData.Target = target;
                                    break;
                                case "MinValue":
                                    double minVal;
                                    if (double.TryParse(propValue?.ToString(), out minVal))
                                        cardData.MinValue = minVal;
                                    break;
                                case "MaxValue":
                                    double maxVal;
                                    if (double.TryParse(propValue?.ToString(), out maxVal))
                                        cardData.MaxValue = maxVal;
                                    break;
                                case "ShowProgressBar":
                                    bool showProg;
                                    if (bool.TryParse(propValue?.ToString(), out showProg))
                                        cardData.ShowProgressBar = showProg;
                                    break;
                                case "ShowTrend":
                                    bool showTrend;
                                    if (bool.TryParse(propValue?.ToString(), out showTrend))
                                        cardData.ShowTrend = showTrend;
                                    break;
                                case "ShowTarget":
                                    bool showTarget;
                                    if (bool.TryParse(propValue?.ToString(), out showTarget))
                                        cardData.ShowTarget = showTarget;
                                    break;
                                case "ChartType":
                                    cardData.ChartType = propValue?.ToString();
                                    break;
                                case "Data":
                                    cardData.Data = propValue;
                                    break;
                                case "RefreshScript":
                                    cardData.RefreshScript = propValue?.ToString();
                                    break;
                                case "Content":
                                case "CardContent":
                                    cardData.Content = propValue?.ToString();
                                    break;
                                case "ImagePath":
                                    cardData.ImagePath = propValue?.ToString();
                                    break;
                                case "ImageUrl":
                                    cardData.ImageUrl = propValue?.ToString();
                                    break;
                                case "Width":
                                    double width;
                                    if (double.TryParse(propValue?.ToString(), out width))
                                        cardData.Width = width;
                                    break;
                                case "Height":
                                    double height;
                                    if (double.TryParse(propValue?.ToString(), out height))
                                        cardData.Height = height;
                                    break;
                                case "ImageHeight":
                                    double imgHeight;
                                    if (double.TryParse(propValue?.ToString(), out imgHeight))
                                        cardData.ImageHeight = imgHeight;
                                    break;
                                case "BackgroundColor":
                                    cardData.BackgroundColor = propValue?.ToString();
                                    break;
                                case "TextColor":
                                    cardData.TextColor = propValue?.ToString();
                                    break;
                                case "BorderColor":
                                    cardData.BorderColor = propValue?.ToString();
                                    break;
                                case "LinkUrl":
                                    cardData.LinkUrl = propValue?.ToString();
                                    break;
                                case "ContentAlignment":
                                    cardData.ContentAlignment = propValue?.ToString();
                                    break;
                                case "ImageAlignment":
                                    cardData.ImageAlignment = propValue?.ToString();
                                    break;
                                // Banner-specific properties
                                case "BannerTitle":
                                    cardData.Title = propValue?.ToString();
                                    break;
                                case "BannerSubtitle":
                                    cardData.Description = propValue?.ToString();
                                    break;
                                case "BannerIcon":
                                    cardData.Icon = propValue?.ToString();
                                    break;
                                case "TitleColor":
                                    cardData.TitleColor = propValue?.ToString();
                                    break;
                                case "SubtitleColor":
                                    cardData.SubtitleColor = propValue?.ToString();
                                    break;
                                case "DescriptionColor":
                                    cardData.DescriptionColor = propValue?.ToString();
                                    break;
                                case "BackgroundImagePath":
                                    cardData.BackgroundImagePath = propValue?.ToString();
                                    break;
                                case "IconPath":
                                    cardData.IconPath = propValue?.ToString();
                                    break;
                                case "IconPosition":
                                    cardData.IconPosition = propValue?.ToString();
                                    break;
                                case "IconSize":
                                    int iconSize;
                                    if (int.TryParse(propValue?.ToString(), out iconSize))
                                        cardData.IconSize = iconSize;
                                    break;
                                case "IconColor":
                                    cardData.IconColor = propValue?.ToString();
                                    break;
                                case "IconAnimation":
                                    cardData.IconAnimation = propValue?.ToString();
                                    break;
                                case "OverlayImagePath":
                                    cardData.OverlayImagePath = propValue?.ToString();
                                    break;
                                case "OverlayImageOpacity":
                                    double overlayOpacity;
                                    if (double.TryParse(propValue?.ToString(), out overlayOpacity))
                                        cardData.OverlayImageOpacity = overlayOpacity;
                                    break;
                                case "OverlayPosition":
                                    cardData.OverlayPosition = propValue?.ToString();
                                    break;
                                case "OverlayImageSize":
                                    int overlaySize;
                                    if (int.TryParse(propValue?.ToString(), out overlaySize))
                                        cardData.OverlayImageSize = overlaySize;
                                    break;
                                case "TitleFontSize":
                                    cardData.TitleFontSize = propValue?.ToString();
                                    break;
                                case "SubtitleFontSize":
                                    cardData.SubtitleFontSize = propValue?.ToString();
                                    break;
                                case "DescriptionFontSize":
                                    cardData.DescriptionFontSize = propValue?.ToString();
                                    break;
                                case "TitleFontWeight":
                                    cardData.TitleFontWeight = propValue?.ToString();
                                    break;
                                case "SubtitleFontWeight":
                                    cardData.SubtitleFontWeight = propValue?.ToString();
                                    break;
                                case "FontFamily":
                                    cardData.FontFamily = propValue?.ToString();
                                    break;
                                case "TitleAllCaps":
                                    bool titleAllCaps;
                                    if (bool.TryParse(propValue?.ToString(), out titleAllCaps))
                                        cardData.TitleAllCaps = titleAllCaps;
                                    break;
                                case "GradientStart":
                                    cardData.GradientStart = propValue?.ToString();
                                    break;
                                case "GradientEnd":
                                    cardData.GradientEnd = propValue?.ToString();
                                    break;
                                case "GradientAngle":
                                    double gradAngle;
                                    if (double.TryParse(propValue?.ToString(), out gradAngle))
                                        cardData.GradientAngle = gradAngle;
                                    break;
                                case "ShadowIntensity":
                                    cardData.ShadowIntensity = propValue?.ToString();
                                    break;
                                case "ProgressValue":
                                    int progVal;
                                    if (int.TryParse(propValue?.ToString(), out progVal))
                                        cardData.ProgressValue = progVal;
                                    break;
                                case "ProgressLabel":
                                    cardData.ProgressLabel = propValue?.ToString();
                                    break;
                                case "ProgressColor":
                                    cardData.ProgressColor = propValue?.ToString();
                                    break;
                                case "ProgressBackgroundColor":
                                    cardData.ProgressBackgroundColor = propValue?.ToString();
                                    break;
                                case "BadgeText":
                                    cardData.BadgeText = propValue?.ToString();
                                    break;
                                case "BadgeColor":
                                    cardData.BadgeColor = propValue?.ToString();
                                    break;
                                case "BadgePosition":
                                    cardData.BadgePosition = propValue?.ToString();
                                    break;
                                case "ButtonText":
                                    cardData.ButtonText = propValue?.ToString();
                                    break;
                                case "ButtonColor":
                                    cardData.ButtonColor = propValue?.ToString();
                                    break;
                                case "CarouselSlidesJson":
                                    cardData.CarouselSlidesJson = propValue?.ToString();
                                    break;
                                case "AutoRotate":
                                    bool autoRotate;
                                    if (bool.TryParse(propValue?.ToString(), out autoRotate))
                                        cardData.AutoRotate = autoRotate;
                                    break;
                                case "RotateInterval":
                                    int rotateInt;
                                    if (int.TryParse(propValue?.ToString(), out rotateInt))
                                        cardData.RotateInterval = rotateInt;
                                    break;
                                // ScriptCard properties
                                case "ScriptBlock":
                                    cardData.ScriptBlock = propValue?.ToString();
                                    break;
                                case "ScriptPath":
                                    cardData.ScriptPath = propValue?.ToString();
                                    break;
                                case "ScriptSource":
                                    cardData.ScriptSource = propValue?.ToString();
                                    break;
                                case "ParameterControls":
                                    cardData.ParameterControls = propValue?.ToString();
                                    break;
                                case "DefaultParameters":
                                    cardData.DefaultParameters = propValue?.ToString();
                                    break;
                            }
                        }
                    }
                    
                    // Create the appropriate ViewModel
                    object cardVm = CreateCardByType(cardData);
                    LoggingService.Info($"*** CREATED ViewModel: {cardVm.GetType().Name}, Title={cardData.Title}, Type={cardData.Type}", component: "CardGridViewModel");
                    AllCards.Add(cardVm);
                    LoggingService.Info($"*** ADDED to AllCards. Total AllCards count: {AllCards.Count}", component: "CardGridViewModel");
                    
                    // Extract category
                    if (!string.IsNullOrEmpty(cardData.Category) && !Categories.Contains(cardData.Category))
                    {
                        Categories.Add(cardData.Category);
                    }
                }
                
                LoggingService.Info($"Loaded {AllCards.Count} cards from Controls", component: "CardGridViewModel");
                
                // Call FilterCards synchronously first to populate collections
                FilterCards();
                
                // Then schedule a UI refresh to ensure bindings are updated
                if (Application.Current != null && Application.Current.Dispatcher != null)
                {
                    Application.Current.Dispatcher.BeginInvoke(System.Windows.Threading.DispatcherPriority.Render, new Action(() =>
                    {
                        OnPropertyChanged(nameof(AllCards));
                        OnPropertyChanged(nameof(FilteredAllCards));
                        OnPropertyChanged(nameof(Categories));
                    }));
                }
            }
            catch (Exception ex)
            {
                LoggingService.Error($"Failed to load cards from Controls: {ex.Message}", ex, component: "CardGridViewModel");
            }
        }

        /// <summary>
        /// Parses script cards from JSON data (from UIScriptCards attribute).
        /// </summary>
        public void LoadCardsFromJson(string json)
        {
            LoggingService.Info("*** LoadCardsFromJson CALLED", component: "CardGridViewModel");

            if (string.IsNullOrEmpty(json))
            {
                LoggingService.Warn("No JSON data provided for CardGrid", component: "CardGridViewModel");
                return;
            }

            try
            {
                LoggingService.Info($"*** Loading cards from JSON ({json.Length} chars): {json.Substring(0, Math.Min(200, json.Length))}...", component: "CardGridViewModel");
                
                var cardDataList = DeserializeJson<List<CardData>>(json);
                
                if (cardDataList == null)
                {
                    LoggingService.Warn("Failed to deserialize card data", component: "CardGridViewModel");
                    return;
                }

                foreach (var cardData in cardDataList)
                {
                    LoggingService.Info($"Processing card: Type={cardData.Type}, Title={cardData.Title}, Category={cardData.Category}", component: "CardGridViewModel");
                    object cardVm = CreateCardByType(cardData);
                    LoggingService.Info($"Created ViewModel: {cardVm.GetType().Name}", component: "CardGridViewModel");
                    AllCards.Add(cardVm);

                    // Also add to ScriptCards if it's a ScriptCard for backward compatibility
                    if (cardVm is ScriptCardViewModel scriptCard)
                    {
                        ScriptCards.Add(scriptCard);
                    }

                    // Extract category
                    if (!string.IsNullOrEmpty(cardData.Category) && !Categories.Contains(cardData.Category))
                    {
                        Categories.Add(cardData.Category);
                    }
                }

                LoggingService.Info($"Loaded {AllCards.Count} cards ({ScriptCards.Count} script cards)", component: "CardGridViewModel");
                
                // Call FilterCards synchronously first to populate collections
                FilterCards();
                
                // Then schedule a UI refresh to ensure bindings are updated
                if (Application.Current != null && Application.Current.Dispatcher != null)
                {
                    Application.Current.Dispatcher.BeginInvoke(System.Windows.Threading.DispatcherPriority.Render, new Action(() =>
                    {
                        OnPropertyChanged(nameof(AllCards));
                        OnPropertyChanged(nameof(FilteredAllCards));
                        OnPropertyChanged(nameof(Categories));
                    }));
                }
            }
            catch (Exception ex)
            {
                LoggingService.Error($"Failed to parse card JSON: {ex.Message}", component: "CardGridViewModel");
            }
        }

        /// <summary>
        /// Creates the appropriate card ViewModel based on the Type property.
        /// </summary>
        private object CreateCardByType(CardData data)
        {
            var cardType = (data.Type ?? "scriptcard").ToLowerInvariant();
            
            switch (cardType)
            {
                case "metriccard":
                    return CreateMetricCardViewModel(data);
                case "graphcard":
                    return CreateGraphCardViewModel(data);
                case "datagridcard":
                    return CreateDataGridCardViewModel(data);
                case "infocard":
                case "info":
                    return CreateInfoCardViewModel(data);
                case "banner":
                    return CreateBannerViewModel(data);
                case "scriptcard":
                default:
                    return CreateCardViewModel(data);
            }
        }

        /// <summary>
        /// Creates a MetricCardViewModel from card data.
        /// </summary>
        private MetricCardViewModel CreateMetricCardViewModel(CardData data)
        {
            var vm = new MetricCardViewModel
            {
                Title = data.Title ?? "Metric",
                Value = data.Value ?? 0,
                Unit = data.Unit ?? string.Empty,
                Description = data.Description ?? string.Empty,
                Category = data.Category ?? "General",
                Icon = ConvertIconToGlyph(data.Icon)
            };

            // Parse metric-specific properties
            if (data.Value.HasValue) vm.Value = data.Value.Value;
            if (!string.IsNullOrEmpty(data.Unit)) vm.Unit = data.Unit;
            if (!string.IsNullOrEmpty(data.Format)) vm.Format = data.Format;
            if (!string.IsNullOrEmpty(data.Trend)) vm.Trend = data.Trend;
            if (data.TrendValue.HasValue) vm.TrendValue = data.TrendValue.Value;
            if (data.Target.HasValue)
            {
                vm.Target = data.Target.Value.ToString();
                vm.TargetValue = data.Target.Value;
            }
            if (data.MinValue.HasValue) vm.MinValue = data.MinValue.Value;
            if (data.MaxValue.HasValue) vm.MaxValue = data.MaxValue.Value;
            vm.ShowProgressBar = data.ShowProgressBar ?? (data.Target.HasValue);
            vm.ShowTrend = data.ShowTrend ?? (!string.IsNullOrEmpty(data.Trend));
            vm.ShowTarget = data.ShowTarget ?? (data.Target.HasValue);
            
            // Set refresh script if provided
            if (!string.IsNullOrEmpty(data.RefreshScript))
            {
                vm.RefreshScript = data.RefreshScript;
            }

            LoggingService.Debug($"Created MetricCard: {vm.Title} = {vm.Value}{vm.Unit}, CanRefresh={vm.CanRefresh}", component: "CardGridViewModel");
            return vm;
        }

        /// <summary>
        /// Creates a GraphCardViewModel from card data.
        /// </summary>
        private GraphCardViewModel CreateGraphCardViewModel(CardData data)
        {
            var vm = new GraphCardViewModel
            {
                Title = data.Title ?? "Graph",
                Description = data.Description ?? "",
                ChartType = data.ChartType ?? "Line",
                Icon = ConvertIconToGlyph(data.Icon),
                Category = data.Category ?? "General",
                ShowLegend = data.ShowLegend ?? true,
                ShowTooltip = data.ShowTooltip ?? true
            };

            // Set refresh script if provided (before setting data)
            if (!string.IsNullOrEmpty(data.RefreshScript))
            {
                vm.RefreshScript = data.RefreshScript;
            }

            // Store data to set later - DO NOT set Data property during construction
            if (data.Data != null)
            {
                try
                {
                    object dataToSet = null;
                    // If data is a JSON string, parse it
                    if (data.Data is string dataString)
                    {
                        var trimmed = dataString.Trim();
                        LoggingService.Debug($"Chart data is string, length={dataString.Length}", component: "CardGridViewModel");
                        
                        // Try to parse as JSON array or object
                        if (trimmed.StartsWith("[") || trimmed.StartsWith("{"))
                        {
                            try
                            {
                                dataToSet = trimmed; // Store as string, GraphCardViewModel will parse it
                                LoggingService.Debug($"Storing JSON string for later parsing", component: "CardGridViewModel");
                            }
                            catch (Exception parseEx)
                            {
                                LoggingService.Error($"JSON parse error: {parseEx.Message}", component: "CardGridViewModel");
                                dataToSet = data.Data;
                            }
                        }
                        else
                        {
                            dataToSet = data.Data;
                        }
                    }
                    else
                    {
                        LoggingService.Debug($"Chart data is type: {data.Data.GetType().Name}", component: "CardGridViewModel");
                        dataToSet = data.Data;
                    }
                    
                    // Defer Data assignment until ViewModel is bound to UI
                    if (Application.Current != null && Application.Current.Dispatcher != null)
                    {
                        var capturedData = dataToSet;
                        Application.Current.Dispatcher.BeginInvoke(new Action(() =>
                        {
                            try
                            {
                                LoggingService.Debug($"Setting chart data, type={capturedData?.GetType().Name ?? "null"}", component: "CardGridViewModel");
                                vm.Data = capturedData;
                            }
                            catch (Exception ex)
                            {
                                LoggingService.Error($"Error setting chart data after UI ready: {ex.Message}", component: "CardGridViewModel");
                            }
                        }), System.Windows.Threading.DispatcherPriority.Loaded);
                    }
                }
                catch (Exception ex)
                {
                    LoggingService.Error($"Error preparing chart data: {ex.Message}", component: "CardGridViewModel");
                }
            }

            LoggingService.Debug($"Created GraphCard: {vm.Title} ({vm.ChartType}), CanRefresh={vm.CanRefresh}", component: "CardGridViewModel");
            return vm;
        }

        /// <summary>
        /// Creates a DataGridCardViewModel from card data.
        /// </summary>
        private DataGridCardViewModel CreateDataGridCardViewModel(CardData data)
        {
            var vm = new DataGridCardViewModel
            {
                Title = data.Title ?? "Data Grid",
                Description = data.Description ?? "",
                Icon = ConvertIconToGlyph(data.Icon),
                Category = data.Category ?? "General",
                AllowSort = data.AllowSort ?? true,
                AllowFilter = data.AllowFilter ?? true,
                AllowExport = data.AllowExport ?? true
            };

            // Load grid data if provided
            if (data.Data != null)
            {
                var dataType = data.Data.GetType();
                var dataStrForPreview = data.Data.ToString() ?? "";
                var dataPreview = dataStrForPreview.Length > 100 ? dataStrForPreview.Substring(0, 100) : dataStrForPreview;
                LoggingService.Info($"DataGridCard: Loading data for '{data.Title}', type={dataType.Name}, fullName={dataType.FullName}, preview={dataPreview}", component: "CardGridViewModel");
                
                // If Data is a string that looks like JSON, try to parse it first
                if (data.Data is string dataStr)
                {
                    var trimmed = dataStr.Trim();
                    if (trimmed.StartsWith("[") || trimmed.StartsWith("{"))
                    {
                        LoggingService.Debug($"DataGridCard: Data is JSON string, parsing...", component: "CardGridViewModel");
                        // Try to deserialize the JSON string to get the actual data
                        try
                        {
                            // Use PowerShell to parse JSON (more reliable for complex objects)
                            using (var ps = System.Management.Automation.PowerShell.Create())
                            {
                                // Pass JSON string as a parameter to avoid escaping issues
                                ps.AddCommand("ConvertFrom-Json").AddParameter("InputObject", trimmed);
                                var results = ps.Invoke();
                                if (results != null && results.Count > 0)
                                {
                                    // If results contain a single array wrapper, unwrap it
                                    if (results.Count == 1 && results[0].BaseObject is System.Collections.IEnumerable enumerable && !(results[0].BaseObject is string))
                                    {
                                        var items = new List<PSObject>();
                                        foreach (var item in enumerable)
                                        {
                                            if (item is PSObject psItem)
                                                items.Add(psItem);
                                            else
                                                items.Add(PSObject.AsPSObject(item));
                                        }
                                        if (items.Count > 0)
                                        {
                                            LoggingService.Debug($"DataGridCard: Unwrapped array to {items.Count} PSObjects", component: "CardGridViewModel");
                                            vm.LoadFromPowerShellOutput(items.ToArray());
                                        }
                                        else
                                        {
                                            vm.LoadFromPowerShellOutput(data.Data);
                                        }
                                    }
                                    else
                                    {
                                        LoggingService.Debug($"DataGridCard: Parsed JSON to {results.Count} PSObjects", component: "CardGridViewModel");
                                        vm.LoadFromPowerShellOutput(results.ToArray());
                                    }
                                }
                                else
                                {
                                    vm.LoadFromPowerShellOutput(data.Data);
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            LoggingService.Warn($"DataGridCard: Failed to parse JSON string, using as-is: {ex.Message}", component: "CardGridViewModel");
                            vm.LoadFromPowerShellOutput(data.Data);
                        }
                    }
                    else
                    {
                        vm.LoadFromPowerShellOutput(data.Data);
                    }
                }
                else
                {
                    vm.LoadFromPowerShellOutput(data.Data);
                }
            }
            else
            {
                LoggingService.Warn($"DataGridCard: No data provided for {data.Title}", component: "CardGridViewModel");
            }
            
            // Set refresh script if provided
            if (!string.IsNullOrEmpty(data.RefreshScript))
            {
                vm.RefreshScript = data.RefreshScript;
            }

            LoggingService.Debug($"Created DataGridCard: {vm.Title} ({vm.Items.Count} rows), CanRefresh={vm.CanRefresh}", component: "CardGridViewModel");
            return vm;
        }

        /// <summary>
        /// Creates an InfoCardViewModel from card data.
        /// </summary>
        private InfoCardViewModel CreateInfoCardViewModel(CardData data)
        {
            var vm = new InfoCardViewModel
            {
                Title = data.Title ?? "Information",
                Description = data.Description ?? string.Empty,
                Content = data.Content ?? string.Empty,
                Category = data.Category ?? "General",
                Icon = ConvertIconToGlyph(data.Icon)
            };

            // Set optional properties if provided
            if (!string.IsNullOrEmpty(data.ImagePath)) vm.ImagePath = data.ImagePath;
            if (!string.IsNullOrEmpty(data.ImageUrl)) vm.ImageUrl = data.ImageUrl;
            if (data.Width.HasValue) vm.Width = data.Width.Value;
            if (data.Height.HasValue) vm.Height = data.Height.Value;
            if (data.ImageHeight.HasValue) vm.ImageHeight = data.ImageHeight.Value;
            if (!string.IsNullOrEmpty(data.BackgroundColor)) vm.BackgroundColor = data.BackgroundColor;
            if (!string.IsNullOrEmpty(data.TextColor)) vm.TextColor = data.TextColor;
            if (!string.IsNullOrEmpty(data.BorderColor)) vm.BorderColor = data.BorderColor;
            if (!string.IsNullOrEmpty(data.LinkUrl)) vm.LinkUrl = data.LinkUrl;
            if (!string.IsNullOrEmpty(data.ContentAlignment)) vm.ContentAlignment = data.ContentAlignment;
            if (!string.IsNullOrEmpty(data.ImageAlignment)) vm.ImageAlignment = data.ImageAlignment;

            LoggingService.Debug(string.Format("Created InfoCard: {0}, HasImage={1}, HasContent={2}", vm.Title, vm.HasImage, vm.HasContent), component: "CardGridViewModel");
            return vm;
        }

        /// <summary>
        /// Creates a BannerViewModel from card data with comprehensive property support.
        /// </summary>
        private BannerViewModel CreateBannerViewModel(CardData data)
        {
            LoggingService.Info(string.Format("*** CreateBannerViewModel CALLED: Title='{0}', IconPosition='{1}', Height={2}, BgColor='{3}'", data.Title, data.IconPosition, data.Height, data.BackgroundColor), component: "CardGridViewModel");
            
            // Banner uses properties directly from CardData (deserialized from JSON)
            var vm = new BannerViewModel
            {
                // Core properties
                Title = !string.IsNullOrEmpty(data.Title) ? data.Title : "Banner",
                Subtitle = data.Description ?? "",
                Category = data.Category ?? "General",
                
                // Layout & Sizing
                Height = data.Height ?? 180,
                Width = data.Width ?? 700,
                FullWidth = data.FullWidth ?? (data.Width.HasValue && data.Width.Value > 1000),
                Layout = data.Layout ?? "Left",
                ContentAlignment = data.ContentAlignment ?? "Left",
                VerticalAlignment = data.VerticalAlignment ?? "Center",
                Padding = data.Padding ?? "32,24",
                CornerRadius = data.CornerRadius ?? 12,
                
                // Typography
                TitleFontSize = data.TitleFontSize ?? "32",
                SubtitleFontSize = data.SubtitleFontSize ?? "16",
                DescriptionFontSize = data.DescriptionFontSize ?? "14",
                TitleFontWeight = data.TitleFontWeight ?? "Bold",
                SubtitleFontWeight = data.SubtitleFontWeight ?? "Normal",
                DescriptionFontWeight = data.DescriptionFontWeight ?? "Normal",
                FontFamily = data.FontFamily ?? "Segoe UI",
                TitleColor = data.TitleColor ?? "#FFFFFF",
                SubtitleColor = data.SubtitleColor ?? "#B0B0B0",
                DescriptionColor = data.DescriptionColor ?? "#909090",
                TitleAllCaps = data.TitleAllCaps ?? false,
                TitleLetterSpacing = data.TitleLetterSpacing ?? 0,
                LineHeight = data.LineHeight ?? 1.4,
                
                // Background & Visual Effects
                BackgroundColor = data.BackgroundColor ?? "#2D2D30",
                BackgroundImagePath = data.BackgroundImagePath ?? "",
                BackgroundImageOpacity = data.BackgroundImageOpacity ?? 0.3,
                BackgroundImageStretch = data.BackgroundImageStretch ?? "Uniform",
                GradientStart = data.GradientStart ?? "",
                GradientEnd = data.GradientEnd ?? "",
                GradientAngle = data.GradientAngle ?? 90,
                BorderColor = data.BorderColor ?? "Transparent",
                BorderThickness = data.BorderThickness ?? 0,
                ShadowIntensity = data.ShadowIntensity ?? "Medium",
                Opacity = data.Opacity ?? 1.0,
                
                // Icon & Image
                IconGlyph = ConvertIconToGlyph(data.Icon),
                IconPath = data.IconPath ?? "",
                IconSize = data.IconSize ?? 64,
                IconPosition = data.IconPosition ?? "Right",
                IconColor = data.IconColor ?? "#40FFFFFF",
                IconAnimation = data.IconAnimation ?? "None",
                OverlayImagePath = data.OverlayImagePath ?? "",
                OverlayImageOpacity = data.OverlayImageOpacity ?? 0.5,
                OverlayPosition = data.OverlayPosition ?? "Right",
                OverlayImageSize = data.OverlayImageSize ?? 120,
                
                // Interactive Elements
                Clickable = data.Clickable ?? false,
                ClickAction = data.ClickAction ?? "",
                LinkUrl = data.LinkUrl ?? "",
                HoverEffect = data.HoverEffect ?? "None",
                ButtonText = data.ButtonText ?? "",
                ButtonIcon = data.ButtonIcon ?? "",
                ButtonColor = data.ButtonColor ?? "#0078D4",
                ButtonTextColor = data.ButtonTextColor ?? "#FFFFFF",
                ShowCloseButton = data.ShowCloseButton ?? false,
                
                // Badge
                BadgeText = data.BadgeText ?? "",
                BadgeColor = data.BadgeColor ?? "#FF5722",
                BadgeTextColor = data.BadgeTextColor ?? "#FFFFFF",
                BadgePosition = data.BadgePosition ?? "TopRight",
                
                // Progress
                ProgressValue = data.ProgressValue ?? -1,
                ProgressLabel = data.ProgressLabel ?? "",
                ProgressColor = data.ProgressColor ?? "#0078D4",
                ProgressBackgroundColor = data.ProgressBackgroundColor ?? "#40FFFFFF",
                
                // Responsive
                Responsive = data.Responsive ?? true,
                SmallTitleFontSize = data.SmallTitleFontSize ?? "24",
                SmallSubtitleFontSize = data.SmallSubtitleFontSize ?? "14",
                SmallHeight = data.SmallHeight ?? 140,
                SmallIconSize = data.SmallIconSize ?? 48,
                ResponsiveBreakpoint = data.ResponsiveBreakpoint ?? 500,
                
                // Animation
                EntranceAnimation = data.EntranceAnimation ?? "None",
                AnimationDuration = data.AnimationDuration ?? 300,
                
                // Carousel
                AutoRotate = data.AutoRotate ?? false,
                RotateInterval = data.RotateInterval ?? 3000,
                NavigationStyle = data.NavigationStyle ?? "Dots"
            };

            // Handle carousel slides - deserialize from JSON string
            if (!string.IsNullOrEmpty(data.CarouselSlidesJson))
            {
                try
                {
                    LoggingService.Info($"Deserializing carousel slides from JSON: {data.CarouselSlidesJson}", component: "CardGridViewModel");
                    // Use DataContractJsonSerializer for .NET 4.8 compatibility
                    var serializer = new DataContractJsonSerializer(typeof(List<BannerSlide>));
                    using (var ms = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(data.CarouselSlidesJson)))
                    {
                        var slides = serializer.ReadObject(ms) as List<BannerSlide>;
                        if (slides != null)
                        {
                            vm.CarouselItems = slides;
                            LoggingService.Info($"Successfully deserialized {slides.Count} carousel slides", component: "CardGridViewModel");
                            foreach (var slide in slides)
                            {
                                LoggingService.Info($"Carousel slide: Title='{slide.Title}', LinkUrl='{slide.LinkUrl}', Clickable={slide.Clickable}, HasLink={slide.HasLink}", component: "CardGridViewModel");
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    LoggingService.Error("Failed to deserialize carousel slides: " + ex.Message, component: "CardGridViewModel");
                }
            }

            // Initialize commands for interactive elements
            vm.NextSlideCommand = new RelayCommand(_ => vm.NextSlide(), _ => vm.IsCarousel);
            vm.PreviousSlideCommand = new RelayCommand(_ => vm.PreviousSlide(), _ => vm.IsCarousel);
            vm.BannerClickCommand = new RelayCommand(_ => OnBannerClick(vm), _ => vm.Clickable || !string.IsNullOrEmpty(vm.LinkUrl));
            vm.ButtonClickCommand = new RelayCommand(_ => OnButtonClick(vm), _ => vm.HasButton);
            vm.CloseCommand = new RelayCommand(_ => { /* Close banner - to be implemented */ }, _ => vm.ShowCloseButton);

            // Start carousel timer if needed
            if (vm.AutoRotate && vm.IsCarousel)
            {
                vm.StartCarouselTimer();
            }

            LoggingService.Info(string.Format("*** BANNER CREATED: Title='{0}', IconPos='{1}', Height={2}, BgColor='{3}', IsCarousel={4}, Clickable={5}", vm.Title, vm.IconPosition, data.Height, vm.BackgroundColor, vm.IsCarousel, vm.Clickable), component: "CardGridViewModel");
            return vm;
        }

        private ScriptCardViewModel CreateCardViewModel(CardData data)
        {
            var vm = new ScriptCardViewModel(OpenCardDialog)
            {
                Name = data.Name,
                Title = data.Title,
                Description = data.Description,
                IconGlyph = ConvertIconToGlyph(data.Icon),
                Category = data.Category,
                Tags = data.Tags,
                ScriptSource = data.ScriptSource,
                ScriptPath = data.ScriptPath,
                ScriptBlock = data.ScriptBlock
            };

            // Parse parameters
            if (!string.IsNullOrEmpty(data.ParameterControls))
            {
                try
                {
                    var paramList = DeserializeJson<List<ScriptCardParameterData>>(data.ParameterControls);

                    if (paramList != null)
                    {
                        foreach (var paramData in paramList)
                        {
                            var param = new ScriptCardParameter
                            {
                                Name = paramData.Name,
                                Label = paramData.Label ?? paramData.Name,
                                Type = paramData.Type ?? "TextBox",
                                Mandatory = paramData.Mandatory,
                                Default = paramData.Default,
                                HelpText = paramData.HelpText,
                                Choices = paramData.Choices,
                                Min = paramData.Min,
                                Max = paramData.Max,
                                Step = paramData.Step,
                                ValidationPattern = paramData.Validation,
                                PathType = paramData.PathType
                            };
                            vm.Parameters.Add(param);
                        }
                    }
                }
                catch (Exception ex)
                {
                    LoggingService.Warn($"Failed to parse parameters for card {data.Name}: {ex.Message}", component: "CardGridViewModel");
                }
            }

            return vm;
        }

        private string ConvertIconToGlyph(string icon)
        {
            if (string.IsNullOrEmpty(icon))
                return "\uE8A5"; // Default icon

            // Check if it's already a glyph (starts with unicode escape)
            if (icon.StartsWith("&#x") && icon.EndsWith(";"))
            {
                // Convert &#xE77B; format to actual unicode character
                var hex = icon.Substring(3, icon.Length - 4);
                int codePoint;
                if (int.TryParse(hex, System.Globalization.NumberStyles.HexNumber, null, out codePoint))
                {
                    return char.ConvertFromUtf32(codePoint);
                }
            }
            
            // Return as-is (might be emoji or already converted)
            return icon;
        }

        private void FilterCards()
        {
            FilteredCards.Clear();
            FilteredAllCards.Clear();

            foreach (var card in ScriptCards)
            {
                if (SelectedCategory == "All" || card.Category == SelectedCategory)
                {
                    FilteredCards.Add(card);
                }
            }

            foreach (var card in AllCards)
            {
                var category = GetCardCategory(card);
                LoggingService.Debug($"FilterCards: Processing card type={card.GetType().Name}, category={category}, SelectedCategory={SelectedCategory}", component: "CardGridViewModel");
                if (SelectedCategory == "All" || category == SelectedCategory)
                {
                    FilteredAllCards.Add(card);
                    LoggingService.Debug($"FilterCards: Added {card.GetType().Name} to FilteredAllCards", component: "CardGridViewModel");
                }
            }

            LoggingService.Info($"FilterCards: Total AllCards={AllCards.Count}, FilteredAllCards={FilteredAllCards.Count} (category: {SelectedCategory})", component: "CardGridViewModel");
            
            // Notify UI that collections have changed
            OnPropertyChanged(nameof(FilteredAllCards));
            OnPropertyChanged(nameof(FilteredCards));
        }

        private string GetCardCategory(object card)
        {
            if (card is ScriptCardViewModel sc)
                return sc.Category ?? "General";
            if (card is GraphCardViewModel gc)
                return gc.Category ?? "General";
            if (card is MetricCardViewModel mc)
                return mc.Category ?? "General";
            if (card is DataGridCardViewModel dc)
                return dc.Category ?? "General";
            if (card is InfoCardViewModel ic)
                return ic.Category ?? "General";
            if (card is BannerViewModel bv)
                return bv.Category ?? "General";
            return "General";
        }

        private void OpenCardDialog(ScriptCardViewModel card)
        {
            LoggingService.Info($"Opening dialog for card: {card.Name}", component: "CardGridViewModel");

            var dialog = new ScriptCardDialog(card)
            {
                Owner = Application.Current.MainWindow
            };

            dialog.ShowDialog();
        }

        /// <summary>
        /// Handles banner click action.
        /// </summary>
        private void OnBannerClick(BannerViewModel banner)
        {
            LoggingService.Info($"Banner clicked: {banner.Title}", component: "CardGridViewModel");

            // Handle URL if specified
            if (!string.IsNullOrEmpty(banner.LinkUrl))
            {
                try
                {
                    System.Diagnostics.Process.Start(banner.LinkUrl);
                }
                catch (Exception ex)
                {
                    LoggingService.Error($"Failed to open URL: {ex.Message}", component: "CardGridViewModel");
                }
            }
            // Handle click action script if specified
            else if (!string.IsNullOrEmpty(banner.ClickAction))
            {
                try
                {
                    using (var ps = System.Management.Automation.PowerShell.Create())
                    {
                        ps.AddScript(banner.ClickAction);
                        ps.Invoke();
                    }
                }
                catch (Exception ex)
                {
                    LoggingService.Error($"Failed to execute click action: {ex.Message}", component: "CardGridViewModel");
                }
            }
        }

        /// <summary>
        /// Handles banner button click action.
        /// </summary>
        private void OnButtonClick(BannerViewModel banner)
        {
            LoggingService.Info($"Banner button clicked: {banner.Title} - {banner.ButtonText}", component: "CardGridViewModel");

            // For now, same behavior as banner click
            OnBannerClick(banner);
        }

        /// <summary>
        /// Deserializes JSON using DataContractJsonSerializer (built-in .NET Framework).
        /// </summary>
        private static T DeserializeJson<T>(string json)
        {
            if (string.IsNullOrEmpty(json))
                return default(T);

            try
            {
                var serializer = new DataContractJsonSerializer(typeof(T));
                using (var ms = new MemoryStream(Encoding.UTF8.GetBytes(json)))
                {
                    return (T)serializer.ReadObject(ms);
                }
            }
            catch (Exception ex)
            {
                LoggingService.Error($"JSON deserialization failed: {ex.Message}", component: "CardGridViewModel");
                return default(T);
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }

    /// <summary>
    /// Data transfer object for deserializing script card JSON.
    /// </summary>
    public class ScriptCardData
    {
        public string Name { get; set; }
        public string Title { get; set; }
        public string Description { get; set; }
        public string Icon { get; set; }
        public string Category { get; set; }
        public string Tags { get; set; }
        public string ScriptSource { get; set; }
        public string ScriptPath { get; set; }
        public string ScriptBlock { get; set; }
        public string ParameterControls { get; set; }
        public string DefaultParameters { get; set; }
    }

    /// <summary>
    /// Unified data transfer object for all card types.
    /// </summary>
    public class CardData
    {
        // Common properties
        public string Type { get; set; }
        public string Name { get; set; }
        public string Title { get; set; }
        public string Description { get; set; }
        public string Icon { get; set; }
        public string Category { get; set; }
        public string Tags { get; set; }
        public object Data { get; set; }

        // ScriptCard properties
        public string ScriptSource { get; set; }
        public string ScriptPath { get; set; }
        public string ScriptBlock { get; set; }
        public string ParameterControls { get; set; }
        public string DefaultParameters { get; set; }

        // MetricCard properties
        public double? Value { get; set; }
        public string Unit { get; set; }
        public string Format { get; set; }
        public string Trend { get; set; }
        public double? TrendValue { get; set; }
        public double? Target { get; set; }
        public double? MinValue { get; set; }
        public double? MaxValue { get; set; }
        public bool? ShowProgressBar { get; set; }
        public bool? ShowTrend { get; set; }
        public bool? ShowTarget { get; set; }

        // GraphCard properties
        public string ChartType { get; set; }
        public bool? ShowLegend { get; set; }
        public bool? ShowTooltip { get; set; }

        // DataGridCard properties
        public bool? AllowSort { get; set; }
        public bool? AllowFilter { get; set; }
        public bool? AllowExport { get; set; }

        // InfoCard properties
        public string Content { get; set; }
        public string ImagePath { get; set; }
        public string ImageUrl { get; set; }
        public double? Width { get; set; }
        public double? Height { get; set; }
        public double? ImageHeight { get; set; }
        public string BackgroundColor { get; set; }
        public string TextColor { get; set; }
        public string BorderColor { get; set; }
        public string LinkUrl { get; set; }
        public string ContentAlignment { get; set; }
        public string ImageAlignment { get; set; }

        // Refresh support - PowerShell script to re-fetch data
        public string RefreshScript { get; set; }

        // Banner-specific properties (layout)
        public int? CornerRadius { get; set; }
        public bool? FullWidth { get; set; }
        public string Layout { get; set; }
        public string VerticalAlignment { get; set; }
        public string Padding { get; set; }

        // Banner-specific properties (icon/image)
        public string IconPath { get; set; }
        public string IconPosition { get; set; }
        public int? IconSize { get; set; }
        public string IconColor { get; set; }
        public string IconAnimation { get; set; }
        public string OverlayImagePath { get; set; }
        public double? OverlayImageOpacity { get; set; }
        public string OverlayPosition { get; set; }
        public int? OverlayImageSize { get; set; }
        public string TitleFontSize { get; set; }
        public string SubtitleFontSize { get; set; }
        public string DescriptionFontSize { get; set; }
        public string TitleFontWeight { get; set; }
        public string SubtitleFontWeight { get; set; }
        public string DescriptionFontWeight { get; set; }
        public string FontFamily { get; set; }
        public string TitleColor { get; set; }
        public string SubtitleColor { get; set; }
        public string DescriptionColor { get; set; }
        public bool? TitleAllCaps { get; set; }
        public double? TitleLetterSpacing { get; set; }
        public double? LineHeight { get; set; }
        public string BackgroundImagePath { get; set; }
        public double? BackgroundImageOpacity { get; set; }
        public string BackgroundImageStretch { get; set; }
        public string GradientStart { get; set; }
        public string GradientEnd { get; set; }
        public double? GradientAngle { get; set; }
        public int? BorderThickness { get; set; }
        public string ShadowIntensity { get; set; }
        public double? Opacity { get; set; }
        public bool? Clickable { get; set; }
        public string ClickAction { get; set; }
        public string HoverEffect { get; set; }
        public string ButtonText { get; set; }
        public string ButtonIcon { get; set; }
        public string ButtonColor { get; set; }
        public string ButtonTextColor { get; set; }
        public bool? ShowCloseButton { get; set; }
        public string BadgeText { get; set; }
        public string BadgeColor { get; set; }
        public string BadgeTextColor { get; set; }
        public string BadgePosition { get; set; }
        public int? ProgressValue { get; set; }
        public string ProgressLabel { get; set; }
        public string ProgressColor { get; set; }
        public string ProgressBackgroundColor { get; set; }
        public bool? Responsive { get; set; }
        public string SmallTitleFontSize { get; set; }
        public string SmallSubtitleFontSize { get; set; }
        public double? SmallHeight { get; set; }
        public int? SmallIconSize { get; set; }
        public int? ResponsiveBreakpoint { get; set; }
        public string EntranceAnimation { get; set; }
        public int? AnimationDuration { get; set; }
        public string CarouselSlidesJson { get; set; }
        public bool? AutoRotate { get; set; }
        public int? RotateInterval { get; set; }
        public string NavigationStyle { get; set; }

        // Generic properties dictionary for extended features (e.g., banner enhancements)
        public Dictionary<string, object> Properties { get; set; }
    }

    /// <summary>
    /// Data transfer object for deserializing parameter control JSON.
    /// </summary>
    public class ScriptCardParameterData
    {
        public string Name { get; set; }
        public string Label { get; set; }
        public string Type { get; set; }
        public bool Mandatory { get; set; }
        public object Default { get; set; }
        public string HelpText { get; set; }
        public List<string> Choices { get; set; }
        public double? Min { get; set; }
        public double? Max { get; set; }
        public double? Step { get; set; }
        public string Validation { get; set; }
        public string PathType { get; set; }
        public string Filter { get; set; }
    }
}



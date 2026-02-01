// Copyright (c) 2025 A Solution IT LLC. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using System.Windows.Data;
using System.Collections.Generic;
using System.Text;
using System.IO;
using Microsoft.Win32;
using System.Management.Automation;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;
using Launcher.Services;

namespace Launcher.ViewModels
{
    /// <summary>
    /// ViewModel for displaying sortable data tables with export functionality in CardGrid
    /// </summary>
    public class DataGridCardViewModel : INotifyPropertyChanged
    {
        private string _title = string.Empty;
        private string _description = string.Empty;
        private string _icon = string.Empty;
        private string _category = "General";
        private ObservableCollection<object> _items = new ObservableCollection<object>();
        private CollectionViewSource _itemsView;
        private System.Data.DataTable _dataTable;
        private string _sortColumn = string.Empty;
        private ListSortDirection _sortDirection = ListSortDirection.Ascending;
        private bool _allowSort = true;
        private bool _allowExport = true;
        private bool _allowFilter = true;
        private string _filterText = string.Empty;
        private int _rowCount;
        private int _columnCount;
        private string _refreshScript;
        private bool _isRefreshing;
        private bool _hasData;

        public event PropertyChangedEventHandler PropertyChanged;

        public DataGridCardViewModel()
        {
            _itemsView = new CollectionViewSource();
            _itemsView.Source = Items;
            _itemsView.View.CollectionChanged += (s, e) => UpdateCounts();
            _itemsView.View.Filter = FilterItems;

            RefreshCommand = new RelayCommand(ExecuteRefresh);
            ExportCsvCommand = new RelayCommand(ExecuteExportCsv);
            ExportTxtCommand = new RelayCommand(ExecuteExportTxt);
            ClearFilterCommand = new RelayCommand(ExecuteClearFilter);
            SortCommand = new RelayCommand(ExecuteSort);
        }

        #region Properties

        /// <summary>
        /// Category for filtering
        /// </summary>
        public string Category
        {
            get => _category;
            set { _category = value; OnPropertyChanged(nameof(Category)); }
        }

        /// <summary>
        /// Grid title
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
        /// Grid description
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
                }
            }
        }

        /// <summary>
        /// Icon glyph
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
        /// Data items collection
        /// </summary>
        public ObservableCollection<object> Items
        {
            get => _items;
            set
            {
                if (_items != value)
                {
                    _items = value ?? new ObservableCollection<object>();
                    _itemsView.Source = _items;
                    OnPropertyChanged(nameof(Items));
                    OnPropertyChanged(nameof(ItemsView));
                    UpdateCounts();
                }
            }
        }

        /// <summary>
        /// Collection view with sorting and filtering
        /// </summary>
        public ICollectionView ItemsView => _itemsView.View;
        
        /// <summary>
        /// DataTable for DataGrid binding (returns DefaultView for sorting support)
        /// </summary>
        public System.Data.DataView DataSource => _dataTable?.DefaultView;
        
        /// <summary>
        /// Indicates if the DataGrid has data to display
        /// </summary>
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

        /// <summary>
        /// Currently sorted column
        /// </summary>
        public string SortColumn
        {
            get => _sortColumn;
            set
            {
                if (_sortColumn != value)
                {
                    _sortColumn = value;
                    OnPropertyChanged(nameof(SortColumn));
                }
            }
        }

        /// <summary>
        /// Sort direction
        /// </summary>
        public ListSortDirection SortDirection
        {
            get => _sortDirection;
            set
            {
                if (_sortDirection != value)
                {
                    _sortDirection = value;
                    OnPropertyChanged(nameof(SortDirection));
                }
            }
        }

        /// <summary>
        /// Allow sorting
        /// </summary>
        public bool AllowSort
        {
            get => _allowSort;
            set
            {
                if (_allowSort != value)
                {
                    _allowSort = value;
                    OnPropertyChanged(nameof(AllowSort));
                }
            }
        }

        /// <summary>
        /// Allow export
        /// </summary>
        public bool AllowExport
        {
            get => _allowExport;
            set
            {
                if (_allowExport != value)
                {
                    _allowExport = value;
                    OnPropertyChanged(nameof(AllowExport));
                }
            }
        }

        /// <summary>
        /// Allow filtering
        /// </summary>
        public bool AllowFilter
        {
            get => _allowFilter;
            set
            {
                if (_allowFilter != value)
                {
                    _allowFilter = value;
                    OnPropertyChanged(nameof(AllowFilter));
                }
            }
        }

        /// <summary>
        /// Filter text for searching
        /// </summary>
        public string FilterText
        {
            get => _filterText;
            set
            {
                if (_filterText != value)
                {
                    _filterText = value;
                    OnPropertyChanged(nameof(FilterText));
                    _itemsView.View.Refresh();
                }
            }
        }

        /// <summary>
        /// Number of rows
        /// </summary>
        public int RowCount
        {
            get => _rowCount;
            set
            {
                if (_rowCount != value)
                {
                    _rowCount = value;
                    OnPropertyChanged(nameof(RowCount));
                }
            }
        }

        /// <summary>
        /// Number of columns
        /// </summary>
        public int ColumnCount
        {
            get => _columnCount;
            set
            {
                if (_columnCount != value)
                {
                    _columnCount = value;
                    OnPropertyChanged(nameof(ColumnCount));
                }
            }
        }

        /// <summary>
        /// PowerShell script to execute for refreshing the grid data
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

        #region Commands

        public ICommand RefreshCommand { get; }
        public ICommand ExportCsvCommand { get; }
        public ICommand ExportTxtCommand { get; }
        public ICommand ClearFilterCommand { get; }
        public ICommand SortCommand { get; }

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
                    if (Application.Current != null && Application.Current.Dispatcher != null)
                    {
                        Application.Current.Dispatcher.Invoke(() =>
                        {
                            // Clear existing data and reload
                            if (_dataTable != null)
                            {
                                _dataTable.Clear();
                            }
                            _dataTable = null;
                            Items.Clear();
                            
                            // Parse new data - result should be JSON array or PowerShell objects
                            if (result is string jsonData)
                            {
                                LoadFromPowerShellOutput(jsonData);
                            }
                            else
                            {
                                LoadFromPowerShellOutput(result);
                            }
                        });
                    }
                }
            }
            catch (Exception ex)
            {
                LoggingService.Error($"DataGridCard refresh failed: {ex.Message}", component: "DataGridCardViewModel");
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
                try
                {
                    using (var ps = System.Management.Automation.PowerShell.Create())
                    {
                        ps.AddScript(script);
                        var results = ps.Invoke();
                        
                        if (results != null && results.Count > 0)
                        {
                            // Convert Collection<PSObject> to PSObject[] for proper handling
                            return results.ToArray();
                        }
                    }
                }
                catch (Exception ex)
                {
                    LoggingService.Error($"DataGrid refresh script error: {ex.Message}", component: "DataGridCardViewModel");
                }
                return null;
            });
        }

        private void ExecuteExportCsv(object parameter)
        {
            var saveDialog = new SaveFileDialog
            {
                Filter = "CSV files (*.csv)|*.csv|All files (*.*)|*.*",
                DefaultExt = "csv",
                FileName = $"{Title}_Export_{DateTime.Now:yyyyMMdd_HHmmss}.csv"
            };

            if (saveDialog.ShowDialog() == true)
            {
                ExportToCsv(saveDialog.FileName);
            }
        }

        private void ExecuteExportTxt(object parameter)
        {
            var saveDialog = new SaveFileDialog
            {
                Filter = "Text files (*.txt)|*.txt|All files (*.*)|*.*",
                DefaultExt = "txt",
                FileName = $"{Title}_Export_{DateTime.Now:yyyyMMdd_HHmmss}.txt"
            };

            if (saveDialog.ShowDialog() == true)
            {
                ExportToTxt(saveDialog.FileName);
            }
        }

        private void ExecuteClearFilter(object parameter)
        {
            FilterText = string.Empty;
        }

        private void ExecuteSort(object parameter)
        {
            var columnName = parameter?.ToString();
            if (string.IsNullOrEmpty(columnName) || !AllowSort) return;

            if (SortColumn == columnName)
            {
                // Toggle direction
                SortDirection = SortDirection == ListSortDirection.Ascending 
                    ? ListSortDirection.Descending 
                    : ListSortDirection.Ascending;
            }
            else
            {
                SortColumn = columnName;
                SortDirection = ListSortDirection.Ascending;
            }

            // Apply sort to DataTable if available
            if (_dataTable != null)
            {
                var sortDir = SortDirection == ListSortDirection.Ascending ? "ASC" : "DESC";
                _dataTable.DefaultView.Sort = $"[{columnName}] {sortDir}";
                OnPropertyChanged(nameof(DataSource)); // Refresh binding
            }
            else
            {
                // Fallback to ItemsView sorting
                _itemsView.View.SortDescriptions.Clear();
                _itemsView.View.SortDescriptions.Add(new SortDescription(columnName, SortDirection));
                _itemsView.View.Refresh();
            }
        }
        
        /// <summary>
        /// Sort by column name - can be called from DataGrid Sorting event
        /// </summary>
        public void SortByColumn(string columnName)
        {
            ExecuteSort(columnName);
        }

        public void SortByColumn(string columnName, ListSortDirection direction)
        {
            if (string.IsNullOrEmpty(columnName) || !AllowSort) return;

            SortColumn = columnName;
            SortDirection = direction;

            // Apply sort to DataTable if available
            if (_dataTable != null)
            {
                try
                {
                    var sortDir = SortDirection == ListSortDirection.Ascending ? "ASC" : "DESC";
                    // Use brackets to escape column names with special characters
                    _dataTable.DefaultView.Sort = $"[{columnName}] {sortDir}";
                    
                    // Force UI refresh by getting the default view and refreshing it
                    var view = System.Windows.Data.CollectionViewSource.GetDefaultView(_dataTable.DefaultView);
                    if (view != null) view.Refresh();
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Sort error: {ex.Message}");
                    // Try without brackets for simple column names
                    try
                    {
                        var sortDir = SortDirection == ListSortDirection.Ascending ? "ASC" : "DESC";
                        _dataTable.DefaultView.Sort = $"{columnName} {sortDir}";
                        var view = System.Windows.Data.CollectionViewSource.GetDefaultView(_dataTable.DefaultView);
                        if (view != null) view.Refresh();
                    }
                    catch { /* Sorting failed */ }
                }
            }
            else
            {
                // Fallback to ItemsView sorting
                _itemsView.View.SortDescriptions.Clear();
                _itemsView.View.SortDescriptions.Add(new SortDescription(columnName, SortDirection));
                _itemsView.View.Refresh();
            }
        }

        #endregion

        #region Data Loading Methods

        /// <summary>
        /// Load data from PowerShell output
        /// </summary>
        public void LoadFromPowerShellOutput(object output)
        {
            Items.Clear();
            HasData = false;
            _dataTable = null;
            
            var outputType = output?.GetType()?.Name ?? "null";
            var outputPreview = output?.ToString();
            var preview = outputPreview != null && outputPreview.Length > 100 ? outputPreview.Substring(0, 100) : outputPreview ?? "null";
            LoggingService.Info($"DataGridCard LoadFromPowerShellOutput: type={outputType}, value preview={preview}", component: "DataGridCardViewModel");

            if (output == null)
            {
                LoggingService.Warn("DataGridCard: output is null", component: "DataGridCardViewModel");
                OnPropertyChanged(nameof(DataSource));
                UpdateCounts();
                return;
            }

            // Handle JSON string (from PowerShell ConvertTo-Json)
            if (output is string js)
            {
                var trimmed = js.Trim();
                var startsWithBracket = trimmed.StartsWith("[");
                var startsWithBrace = trimmed.StartsWith("{");
                LoggingService.Debug($"DataGridCard: output is string, length={js.Length}, starts with [={startsWithBracket}, starts with {{={startsWithBrace}", component: "DataGridCardViewModel");
                
                if (trimmed.StartsWith("[") || trimmed.StartsWith("{"))
                {
                    try
                    {
                        // Try to parse JSON array and convert to DataTable
                        LoadFromJsonString(trimmed);
                        return;
                    }
                    catch (Exception ex)
                    {
                        LoggingService.Error($"Failed to parse JSON string: {ex.Message}", component: "DataGridCardViewModel");
                        // Fall through to other handlers
                    }
                }
            }

            // Handle Dictionary array (from JSON deserialization)
            if (output is Dictionary<string, object>[] dictArray)
            {
                LoggingService.Debug($"DataGridCard: output is Dictionary<string,object>[], count={dictArray.Length}", component: "DataGridCardViewModel");
                ConvertDictionariesToDataTable(dictArray);
                return;
            }
            
            // Handle List of Dictionaries
            if (output is List<Dictionary<string, object>> dictList)
            {
                LoggingService.Debug($"DataGridCard: output is List<Dictionary>, count={dictList.Count}", component: "DataGridCardViewModel");
                ConvertDictionariesToDataTable(dictList.ToArray());
                return;
            }

            // Handle PowerShell objects
            if (output is PSObject[] psObjects)
            {
                LoggingService.Debug($"DataGridCard: output is PSObject[], count={psObjects.Length}", component: "DataGridCardViewModel");
                ConvertPsObjectsToDataTable(psObjects);
                return;
            }
            
            if (output is object[] objects)
            {
                var firstType = objects.Length > 0 ? objects[0]?.GetType().Name : "empty";
                LoggingService.Debug($"DataGridCard: output is object[], count={objects.Length}, first type={firstType}", component: "DataGridCardViewModel");
                ConvertObjectsToDataTable(objects);
                return;
            }
            
            if (output is System.Collections.IEnumerable enumerable && !(output is string))
            {
                var list = new List<object>();
                foreach (var obj in enumerable)
                {
                    list.Add(obj);
                }
                var firstType = list.Count > 0 ? list[0]?.GetType().Name : "empty";
                LoggingService.Debug($"DataGridCard: output is IEnumerable, count={list.Count}, first type={firstType}", component: "DataGridCardViewModel");
                
                if (list.Count > 0)
                {
                    // Check if all items are dictionaries
                    if (list.All(item => item is Dictionary<string, object>))
                    {
                        LoggingService.Debug($"DataGridCard: All items are Dictionary, converting", component: "DataGridCardViewModel");
                        ConvertDictionariesToDataTable(list.Cast<Dictionary<string, object>>().ToArray());
                        return;
                    }
                    ConvertObjectsToDataTable(list.ToArray());
                    return;
                }
            }
            
            LoggingService.Warn($"DataGridCard: Could not determine output type: {output.GetType().FullName}", component: "DataGridCardViewModel");

            // Fallback: add as single item
            if (output != null)
            {
                Items.Add(output);
                HasData = true;
            }

            UpdateCounts();
        }

        /// <summary>
        /// Parse JSON string and convert to DataTable
        /// </summary>
        private void LoadFromJsonString(string jsonString)
        {
            // For .NET 4.8, we'll use a simpler approach: convert JSON to PSObjects
            // This works well with PowerShell's ConvertFrom-Json output
            try
            {
                using (var ps = System.Management.Automation.PowerShell.Create())
                {
                    // Use AddCommand with parameter to avoid escaping issues
                    ps.AddCommand("ConvertFrom-Json").AddParameter("InputObject", jsonString);
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
                                ConvertPsObjectsToDataTable(items.ToArray());
                                return;
                            }
                        }
                        ConvertPsObjectsToDataTable(results.ToArray());
                        return;
                    }
                }
            }
            catch (Exception ex)
            {
                LoggingService.Error($"Error parsing JSON in DataGridCard: {ex.Message}", component: "DataGridCardViewModel");
            }
            
            // If parsing failed, try using DataContractJsonSerializer
            try
            {
                // Try to deserialize as array of dictionaries
                var serializer = new System.Runtime.Serialization.Json.DataContractJsonSerializer(typeof(List<Dictionary<string, object>>));
                using (var ms = new System.IO.MemoryStream(System.Text.Encoding.UTF8.GetBytes(jsonString)))
                {
                    var dictList = serializer.ReadObject(ms) as List<Dictionary<string, object>>;
                    if (dictList != null && dictList.Count > 0)
                    {
                        ConvertDictionariesToDataTable(dictList.ToArray());
                        return;
                    }
                }
            }
            catch (Exception ex2)
            {
                LoggingService.Error($"Error parsing JSON with DataContractJsonSerializer: {ex2.Message}", component: "DataGridCardViewModel");
            }
            
            // Fallback: add as string
            Items.Add(jsonString);
            HasData = true;
            UpdateCounts();
        }
        
        /// <summary>
        /// Convert dictionary array to DataTable
        /// </summary>
        private void ConvertDictionariesToDataTable(Dictionary<string, object>[] dictArray)
        {
            if (dictArray == null || dictArray.Length == 0)
            {
                LoggingService.Warn("DataGridCard: dictArray is null or empty", component: "DataGridCardViewModel");
                return;
            }

            LoggingService.Info($"DataGridCard: Converting {dictArray.Length} dictionaries to DataTable", component: "DataGridCardViewModel");
            _dataTable = new System.Data.DataTable();
            bool columnsCreated = false;

            foreach (var dict in dictArray)
            {
                if (!columnsCreated)
                {
                    foreach (var key in dict.Keys)
                    {
                        _dataTable.Columns.Add(key, typeof(string));
                    }
                    columnsCreated = true;
                    LoggingService.Debug($"DataGridCard: Created {_dataTable.Columns.Count} columns", component: "DataGridCardViewModel");
                }

                var row = _dataTable.NewRow();
                foreach (var kvp in dict)
                {
                    row[kvp.Key] = kvp.Value?.ToString() ?? string.Empty;
                }
                _dataTable.Rows.Add(row);
            }

            LoggingService.Info($"DataGridCard: Created DataTable with {_dataTable.Rows.Count} rows, {_dataTable.Columns.Count} columns", component: "DataGridCardViewModel");
            
            HasData = _dataTable.Rows.Count > 0;
            
            // Ensure property change happens on UI thread
            if (System.Windows.Application.Current != null && System.Windows.Application.Current.Dispatcher != null)
            {
                System.Windows.Application.Current.Dispatcher.Invoke(() =>
                {
                    OnPropertyChanged(nameof(DataSource));
                    RowCount = _dataTable.Rows.Count;
                    ColumnCount = _dataTable.Columns.Count;
                });
            }
            else
            {
                OnPropertyChanged(nameof(DataSource));
                RowCount = _dataTable.Rows.Count;
                ColumnCount = _dataTable.Columns.Count;
            }
        }

        /// <summary>
        /// Convert PSObject array to DataTable
        /// </summary>
        private void ConvertPsObjectsToDataTable(PSObject[] psObjects)
        {
            if (psObjects == null || psObjects.Length == 0)
            {
                LoggingService.Warn("DataGridCard: psObjects is null or empty", component: "DataGridCardViewModel");
                return;
            }

            LoggingService.Info($"DataGridCard: Converting {psObjects.Length} PSObjects to DataTable", component: "DataGridCardViewModel");
            _dataTable = new System.Data.DataTable();
            bool columnsCreated = false;
            var columnTypes = new Dictionary<string, Type>(StringComparer.OrdinalIgnoreCase);

            foreach (var psObj in psObjects)
            {
                if (!columnsCreated)
                {
                    foreach (var prop in psObj.Properties)
                    {
                        Type dataType = typeof(string);
                        if (prop.Value != null)
                        {
                            // Check for numeric types - PowerShell can use various numeric types
                            if (IsNumericType(prop.Value))
                                dataType = typeof(double);
                            else if (prop.Value is bool)
                                dataType = typeof(bool);
                            else if (prop.Value is DateTime)
                                dataType = typeof(DateTime);
                            // Check if string value looks numeric
                            else if (prop.Value is string strVal && double.TryParse(strVal, NumberStyles.Any, CultureInfo.InvariantCulture, out _))
                                dataType = typeof(double);
                        }

                        columnTypes[prop.Name] = dataType;
                        _dataTable.Columns.Add(prop.Name, dataType);
                    }
                    columnsCreated = true;
                }

                var row = _dataTable.NewRow();
                foreach (var prop in psObj.Properties)
                {
                    if (columnTypes.ContainsKey(prop.Name))
                    {
                        var targetType = columnTypes[prop.Name];
                        object value = prop.Value ?? DBNull.Value;

                        if (value != DBNull.Value && targetType == typeof(double))
                        {
                            // Use TryGetDouble to handle PSObject unwrapping and all numeric types
                            double dblVal;
                            if (TryGetDouble(value, out dblVal))
                                row[prop.Name] = dblVal;
                            else
                                row[prop.Name] = DBNull.Value;
                        }
                        else if (value != DBNull.Value && targetType == typeof(bool))
                        {
                            if (value is bool b)
                                row[prop.Name] = b;
                            else
                            {
                                bool parsed;
                                if (bool.TryParse(value.ToString(), out parsed))
                                    row[prop.Name] = parsed;
                                else
                                    row[prop.Name] = DBNull.Value;
                            }
                        }
                        else
                        {
                            row[prop.Name] = value?.ToString() ?? "";
                        }
                    }
                }
                _dataTable.Rows.Add(row);
            }

            HasData = _dataTable.Rows.Count > 0;
            
            OnPropertyChanged(nameof(DataSource));
            RowCount = _dataTable.Rows.Count;
            ColumnCount = _dataTable.Columns.Count;
        }

        /// <summary>
        /// Convert object array to DataTable
        /// </summary>
        private void ConvertObjectsToDataTable(object[] objects)
        {
            if (objects == null || objects.Length == 0) return;

            _dataTable = new System.Data.DataTable();
            bool columnsCreated = false;
            var columnTypes = new Dictionary<string, Type>(StringComparer.OrdinalIgnoreCase);

            foreach (var obj in objects)
            {
                if (!columnsCreated)
                {
                    var props = obj.GetType().GetProperties();
                    foreach (var prop in props)
                    {
                        columnTypes[prop.Name] = prop.PropertyType;
                        _dataTable.Columns.Add(prop.Name, prop.PropertyType);
                    }
                    columnsCreated = true;
                }

                var row = _dataTable.NewRow();
                var props2 = obj.GetType().GetProperties();
                foreach (var prop in props2)
                {
                    if (columnTypes.ContainsKey(prop.Name))
                    {
                        var value = prop.GetValue(obj);
                        row[prop.Name] = value ?? DBNull.Value;
                    }
                }
                _dataTable.Rows.Add(row);
            }

            LoggingService.Info($"DataGridCard: Created DataTable from objects with {_dataTable.Rows.Count} rows, {_dataTable.Columns.Count} columns", component: "DataGridCardViewModel");
            
            // Ensure property change happens on UI thread
            if (System.Windows.Application.Current != null && System.Windows.Application.Current.Dispatcher != null)
            {
                System.Windows.Application.Current.Dispatcher.Invoke(() =>
                {
                    HasData = _dataTable.Rows.Count > 0;
                    OnPropertyChanged(nameof(DataSource));
                    RowCount = _dataTable.Rows.Count;
                    ColumnCount = _dataTable.Columns.Count;
                });
            }
            else
            {
                HasData = _dataTable.Rows.Count > 0;
                OnPropertyChanged(nameof(DataSource));
                RowCount = _dataTable.Rows.Count;
                ColumnCount = _dataTable.Columns.Count;
            }
        }

        /// <summary>
        /// Check if a value is a numeric type (unwraps PSObject if needed)
        /// </summary>
        private static bool IsNumericType(object value)
        {
            // Unwrap PSObject to get actual value
            var actualValue = value is PSObject psObj ? psObj.BaseObject : value;
            return actualValue is double || actualValue is int || actualValue is long || actualValue is float ||
                   actualValue is decimal || actualValue is short || actualValue is uint || actualValue is ulong ||
                   actualValue is byte || actualValue is sbyte || actualValue is ushort;
        }

        /// <summary>
        /// Unwrap PSObject and convert to double if possible
        /// </summary>
        private static bool TryGetDouble(object value, out double result)
        {
            result = 0;
            if (value == null || value == DBNull.Value) return false;
            
            // Unwrap PSObject
            var actualValue = value is PSObject psObj ? psObj.BaseObject : value;
            
            if (actualValue is double d) { result = d; return true; }
            if (actualValue is int i) { result = i; return true; }
            if (actualValue is long l) { result = l; return true; }
            if (actualValue is float f) { result = f; return true; }
            if (actualValue is decimal dec) { result = (double)dec; return true; }
            if (actualValue is short s) { result = s; return true; }
            if (actualValue is uint ui) { result = ui; return true; }
            if (actualValue is ulong ul) { result = ul; return true; }
            if (actualValue is byte b) { result = b; return true; }
            if (actualValue is sbyte sb) { result = sb; return true; }
            if (actualValue is ushort us) { result = us; return true; }
            
            // Try parsing string
            return double.TryParse(actualValue?.ToString(), NumberStyles.Any, CultureInfo.InvariantCulture, out result);
        }

        /// <summary>
        /// Load data from CSV content
        /// </summary>
        public void LoadFromCsv(string csvContent)
        {
            Items.Clear();
            
            var lines = csvContent.Split('\n');
            var nonEmptyLines = new List<string>();
            foreach (var line in lines)
            {
                if (!string.IsNullOrWhiteSpace(line))
                    nonEmptyLines.Add(line);
            }
            
            if (nonEmptyLines.Count == 0) return;

            // Parse header
            var headers = nonEmptyLines[0].Split(',');
            for (int i = 1; i < nonEmptyLines.Count; i++)
            {
                var values = nonEmptyLines[i].Split(',');
                var obj = new PSObject();
                
                for (int j = 0; j < Math.Min(headers.Length, values.Length); j++)
                {
                    obj.Properties.Add(new PSNoteProperty(headers[j].Trim(), values[j].Trim()));
                }
                
                Items.Add(obj);
            }

            ColumnCount = headers.Length;
        }

        #endregion

        #region Export Methods

        /// <summary>
        /// Export to CSV file
        /// </summary>
        private void ExportToCsv(string filePath)
        {
            try
            {
                var sb = new StringBuilder();
                var viewItems = _itemsView.View.Cast<object>().ToList();

                if (viewItems.Count == 0) return;

                // Get headers from first item
                var firstItem = viewItems[0];
                var properties = GetProperties(firstItem);
                
                // Write header
                sb.AppendLine(string.Join(",", properties.Select(p => EscapeCsvValue(p))));

                // Write rows
                foreach (var item in viewItems)
                {
                    var values = properties.Select(p => 
                    {
                        var value = GetPropertyValue(item, p);
                        return EscapeCsvValue(value?.ToString() ?? string.Empty);
                    });
                    sb.AppendLine(string.Join(",", values));
                }

                File.WriteAllText(filePath, sb.ToString(), Encoding.UTF8);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error exporting CSV: {ex.Message}", "Export Error", 
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// Export to TXT file (tab-delimited)
        /// </summary>
        private void ExportToTxt(string filePath)
        {
            try
            {
                var sb = new StringBuilder();
                var viewItems = _itemsView.View.Cast<object>().ToList();

                if (viewItems.Count == 0) return;

                // Get headers from first item
                var firstItem = viewItems[0];
                var properties = GetProperties(firstItem);

                // Calculate column widths
                var columnWidths = new int[properties.Length];
                for (int i = 0; i < properties.Length; i++)
                {
                    columnWidths[i] = Math.Max(properties[i].Length, 20);
                    foreach (var item in viewItems)
                    {
                        var value = GetPropertyValue(item, properties[i])?.ToString() ?? string.Empty;
                        columnWidths[i] = Math.Max(columnWidths[i], value.Length);
                    }
                    columnWidths[i] = Math.Min(columnWidths[i], 50); // Max width
                }

                // Write header
                var header = string.Join(" | ", properties.Select((p, i) => p.PadRight(columnWidths[i])));
                sb.AppendLine(header);
                sb.AppendLine(new string('-', header.Length));

                // Write rows
                foreach (var item in viewItems)
                {
                    var values = properties.Select((p, i) =>
                    {
                        var value = GetPropertyValue(item, p)?.ToString() ?? string.Empty;
                        return value.PadRight(columnWidths[i]);
                    });
                    sb.AppendLine(string.Join(" | ", values));
                }

                // Add summary
                sb.AppendLine();
                sb.AppendLine($"Total Rows: {viewItems.Count}");
                sb.AppendLine($"Export Date: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");

                File.WriteAllText(filePath, sb.ToString(), Encoding.UTF8);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error exporting TXT: {ex.Message}", "Export Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        #endregion

        #region Helper Methods

        /// <summary>
        /// Filter items based on filter text
        /// </summary>
        private bool FilterItems(object item)
        {
            if (string.IsNullOrEmpty(FilterText)) return true;

            var properties = GetProperties(item);
            return properties.Any(p => 
            {
                var value = GetPropertyValue(item, p)?.ToString();
                if (value != null)
                {
                    return value.IndexOf(FilterText, StringComparison.OrdinalIgnoreCase) >= 0;
                }
                return false;
            });
        }

        /// <summary>
        /// Get property names from object
        /// </summary>
        private string[] GetProperties(object item)
        {
            if (item is PSObject psObj)
            {
                return psObj.Properties.Select(p => p.Name).ToArray();
            }
            return item.GetType().GetProperties().Select(p => p.Name).ToArray();
        }

        /// <summary>
        /// Get property value from object
        /// </summary>
        private object GetPropertyValue(object item, string propertyName)
        {
            if (item is PSObject psObj)
            {
                if (psObj.Properties[propertyName] != null)
                    return psObj.Properties[propertyName].Value;
                return null;
            }
            var prop = item.GetType().GetProperty(propertyName);
            if (prop != null)
                return prop.GetValue(item);
            return null;
        }

        /// <summary>
        /// Escape CSV value
        /// </summary>
        private string EscapeCsvValue(string value)
        {
            if (value.Contains(",") || value.Contains("\"") || value.Contains("\n"))
            {
                return $"\"{value.Replace("\"", "\"\"")}\"";
            }
            return value;
        }

        /// <summary>
        /// Update row and column counts
        /// </summary>
        private void UpdateCounts()
        {
            RowCount = _itemsView.View.Cast<object>().Count();
            if (Items.Count > 0)
            {
                ColumnCount = GetProperties(Items[0]).Length;
            }
        }

        #endregion

        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}



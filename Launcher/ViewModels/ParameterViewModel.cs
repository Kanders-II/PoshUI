// Copyright (c) 2025 A Solution IT LLC. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Windows.Controls;
using System.Windows.Input;
using Launcher.Services;
using System.Management.Automation;
using System.Linq;
using Microsoft.Win32;
using Win32 = Microsoft.Win32;
using WinForms = System.Windows.Forms;
using System.IO;
using System.Security;
using System.Collections.Generic;
using System.Collections;

namespace Launcher.ViewModels
{
    // Helper class for checkbox items in multi-select ListBox
    public class CheckableItem : INotifyPropertyChanged
    {
        private bool _isChecked;
        public string Value { get; set; }
        
        public bool IsChecked
        {
            get => _isChecked;
            set
            {
                Services.LoggingService.Info($"CheckableItem.IsChecked SETTER called: Value='{this.Value}', OldChecked={_isChecked}, NewChecked={value}", component: "CheckableItem");
                if (_isChecked != value)
                {
                    _isChecked = value;
                    Services.LoggingService.Info($"CheckableItem raising PropertyChanged for '{this.Value}'", component: "CheckableItem");
                    OnPropertyChanged(nameof(IsChecked));
                }
                else
                {
                    Services.LoggingService.Warn($"CheckableItem.IsChecked NO CHANGE for '{this.Value}' (already {value})", component: "CheckableItem");
                }
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged(string propertyName) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    public class OptionItem : INotifyPropertyChanged
    {
        private bool _isSelected;
        public string Value { get; set; }

        public bool IsSelected
        {
            get => _isSelected;
            set
            {
                if (_isSelected != value)
                {
                    Services.LoggingService.Info($"OptionItem.IsSelected SETTER called: Value='{Value}', IsSelected={value}", component: "OptionItem");
                    _isSelected = value;
                    OnPropertyChanged(nameof(IsSelected));
                }
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }

    public class ParameterViewModel : INotifyPropertyChanged
    {
        private string _name;
        private string _label;
        private bool _isMandatory;
        private Type _parameterType;
        private string _validationPattern;
        private double? _controlWidth;
        private bool _isSwitch;
        private Services.PathSelectorType _pathType;
        private string _pathFilter;
        private string _dialogTitle;
        private ObservableCollection<string> _choices;
        private bool _isListBox;
        private bool _isMultiSelect;
        private ObservableCollection<string> _selectedItems;
        private ObservableCollection<CheckableItem> _checkableItems;
        private bool _isNumeric;
        private double? _numericMinimum;
        private double? _numericMaximum;
        private double? _numericStep;
        private bool _numericAllowDecimal;
        private string _numericFormat;
        private double? _numericValue;
        private bool _isDate;
        private DateTime? _dateMinimum;
        private DateTime? _dateMaximum;
        private string _dateFormat;
        private DateTime? _dateValue;
        private bool _isOptionGroup;
        private bool _optionGroupHorizontalLayout;
        private bool _isMultiLineText;
        private int? _multiLineRows;
        private ObservableCollection<OptionItem> _optionItems;
        private ICommand _incrementNumericCommand;
        private ICommand _decrementNumericCommand;
        private bool _isUpdatingOptionItems;

        private object _value;
        private bool? _boolValue;
        private SecureString _secureValue;

        // Backing field for lazy initialization
        private ICommand _browsePathCommand;
        
        // Phase 2: Track if parameter is dynamic
        private bool _isDynamic;

        private readonly IDialogService _dialogService;
        private readonly MainWindowViewModel _mainWindowViewModel;

        public string Name => _name;
        public string Label { get => _label; private set { _label = value; OnPropertyChanged(nameof(Label)); } }
        public bool IsMandatory { get => _isMandatory; private set { _isMandatory = value; OnPropertyChanged(nameof(IsMandatory)); } }
        public Type ParameterType { get => _parameterType; private set { _parameterType = value; OnPropertyChanged(nameof(ParameterType)); } }
        public string ValidationPattern { get => _validationPattern; private set { _validationPattern = value; OnPropertyChanged(nameof(ValidationPattern)); } }
        public double? ControlWidth { get => _controlWidth; set { _controlWidth = value; OnPropertyChanged(nameof(ControlWidth)); } }
        public bool IsSwitch => _isSwitch;
        public Services.PathSelectorType PathType => _pathType;
        public bool IsListBox => _isListBox;
        public bool IsMultiSelect => _isMultiSelect;
        public bool IsNumeric => _isNumeric;
        public double? NumericMinimum => _numericMinimum;
        public double? NumericMaximum => _numericMaximum;
        public double NumericStep => _numericStep ?? (_numericAllowDecimal ? 0.1 : 1);
        public bool NumericAllowDecimal => _numericAllowDecimal;
        public double? NumericValue
        {
            get => _numericValue;
            set
            {
                if (!_isNumeric)
                {
                    return;
                }

                double? clamped = ClampNumericValue(value);

                if (_numericValue != clamped)
                {
                    _numericValue = clamped;
                    _value = clamped.HasValue
                        ? clamped.Value.ToString(CultureInfo.InvariantCulture)
                        : null;
                    LoggingService.Debug($"Numeric value changed for '{Name}': {_value}", component: "ParameterViewModel");
                    OnPropertyChanged(nameof(NumericValue));
                    OnPropertyChanged(nameof(Value));
                    _mainWindowViewModel?.UpdateParameterValueAndValidation(Name, _value);
                }
            }
        }

        public bool IsDate => _isDate;
        public DateTime? DateMinimum => _dateMinimum;
        public DateTime? DateMaximum => _dateMaximum;
        public string DateDisplayFormat => string.IsNullOrWhiteSpace(_dateFormat) ? "yyyy-MM-dd" : _dateFormat;
        public DateTime? DateValue
        {
            get => _dateValue;
            set
            {
                if (!_isDate)
                {
                    return;
                }

                DateTime? clamped = ClampDateValue(value);

                if (_dateValue != clamped)
                {
                    _dateValue = clamped;
                    _value = clamped.HasValue
                        ? clamped.Value.ToString(DateDisplayFormat, CultureInfo.InvariantCulture)
                        : null;
                    LoggingService.Debug($"Date value changed for '{Name}': {_value}", component: "ParameterViewModel");
                    OnPropertyChanged(nameof(DateValue));
                    OnPropertyChanged(nameof(Value));
                    _mainWindowViewModel?.UpdateParameterValueAndValidation(Name, _value);
                }
            }
        }

        public bool IsOptionGroup => _isOptionGroup;
        public Orientation OptionGroupOrientation => _optionGroupHorizontalLayout ? Orientation.Horizontal : Orientation.Vertical;

        public bool IsMultiLineText => _isMultiLineText;
        public int MultiLineRows => _multiLineRows.HasValue && _multiLineRows > 0 ? _multiLineRows.Value : 4;
        
        // Indicates this parameter should be masked as a password
        public bool IsPassword => ParameterType == typeof(SecureString);
        
        // For multi-select ListBox
        public ObservableCollection<string> SelectedItems 
        { 
            get 
            {
                if (_selectedItems == null)
                {
                    _selectedItems = new ObservableCollection<string>();
                    _selectedItems.CollectionChanged += SelectedItems_CollectionChanged;
                }
                return _selectedItems;
            } 
            set 
            { 
                if (_selectedItems != null)
                {
                    _selectedItems.CollectionChanged -= SelectedItems_CollectionChanged;
                }
                
                _selectedItems = value;
                
                if (_selectedItems != null)
                {
                    _selectedItems.CollectionChanged += SelectedItems_CollectionChanged;
                }
                
                OnPropertyChanged(nameof(SelectedItems));
                UpdateValueFromSelectedItems();
            } 
        }
        
        private void SelectedItems_CollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            UpdateValueFromSelectedItems();
        }
        
        private void UpdateValueFromSelectedItems()
        {
            if (_isMultiSelect && _selectedItems != null && _selectedItems.Count > 0)
            {
                var newValue = string.Join(",", _selectedItems);
                if (_value as string != newValue)
                {
                    _value = newValue;
                    OnPropertyChanged(nameof(Value));
                    LoggingService.Debug($"Multi-select value updated for '{Name}': {newValue}", component: "ParameterViewModel");
                }
            }
            else if (_isMultiSelect)
            {
                _value = string.Empty;
                OnPropertyChanged(nameof(Value));
            }
        }

        public ObservableCollection<OptionItem> OptionItems
        {
            get => _optionItems;
            private set
            {
                _optionItems = value;
                OnPropertyChanged(nameof(OptionItems));
            }
        }

        public ICommand IncrementNumericCommand
        {
            get
            {
                if (_incrementNumericCommand == null)
                {
                    _incrementNumericCommand = new RelayCommand(_ => AdjustNumericValue(1), _ => _isNumeric);
                }
                return _incrementNumericCommand;
            }
        }

        public ICommand DecrementNumericCommand
        {
            get
            {
                if (_decrementNumericCommand == null)
                {
                    _decrementNumericCommand = new RelayCommand(_ => AdjustNumericValue(-1), _ => _isNumeric);
                }
                return _decrementNumericCommand;
            }
        }

        public ObservableCollection<string> Choices
        {
            get
            {
                LoggingService.Trace($"Choices getter for '{Name}' returning count: {_choices?.Count ?? 0}", component: "ParameterViewModel");
                if (_choices != null && _choices.Count > 0)
                {
                    LoggingService.Trace($"Choices items: [{string.Join(", ", _choices)}]", component: "ParameterViewModel");
                }
                return _choices;
            }
            private set
            {
                LoggingService.Trace($"Choices setter for '{Name}' called with count: {value?.Count ?? 0}", component: "ParameterViewModel");

                if (value != null && value.Count > 0)
                {
                    LoggingService.Trace($"Setting Choices items: [{string.Join(", ", value)}]", component: "ParameterViewModel");
                }

                _choices = value;
                OnPropertyChanged(nameof(Choices));

                if (_isMultiSelect && _choices != null)
                {
                    InitializeCheckableItems();
                }

                if (_isOptionGroup && _choices != null)
                {
                    InitializeOptionItems();
                }

                LoggingService.Trace($"Choices setter for '{Name}' completed. _choices count is now: {_choices?.Count ?? 0}", component: "ParameterViewModel");
            }
        }
        
        public bool IsDynamic
        {
            get => _isDynamic;
            set
            {
                if (_isDynamic != value)
                {
                    _isDynamic = value;
                    OnPropertyChanged(nameof(IsDynamic));
                }
            }
        }

        // For multi-select with checkboxes
        public ObservableCollection<CheckableItem> CheckableItems
        {
            get
            {
                if (_checkableItems == null && _isMultiSelect && _choices != null && _choices.Count > 0)
                {
                    LoggingService.Warn($"CheckableItems was null for '{Name}' - initializing now (lazy)", component: "ParameterViewModel");
                    InitializeCheckableItems();
                }
                return _checkableItems;
            }
            private set
            {
                _checkableItems = value;
                OnPropertyChanged(nameof(CheckableItems));
            }
        }

        private void InitializeCheckableItems()
        {
            if (_checkableItems != null)
            {
                LoggingService.Warn($"CheckableItems already initialized for '{Name}' - skipping", component: "ParameterViewModel");
                return;
            }

            LoggingService.Debug($"InitializeCheckableItems called for '{Name}'. IsMultiSelect={_isMultiSelect}, Choices count={_choices?.Count ?? 0}, Current Value='{_value}'", component: "ParameterViewModel");

            if (_choices == null || _choices.Count == 0)
            {
                LoggingService.Warn($"Cannot initialize CheckableItems for '{Name}' - no choices available", component: "ParameterViewModel");
                return;
            }

            var selectedValues = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            if (!string.IsNullOrEmpty(_value as string))
            {
                var valueParts = (_value as string).Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
                foreach (var part in valueParts)
                {
                    selectedValues.Add(part.Trim());
                }
                LoggingService.Debug($"Restoring {selectedValues.Count} selected values for '{Name}': {string.Join(", ", selectedValues)}", component: "ParameterViewModel");
            }

            var items = new ObservableCollection<CheckableItem>();
            foreach (var choice in _choices)
            {
                bool isChecked = selectedValues.Contains(choice);
                var item = new CheckableItem { Value = choice, IsChecked = isChecked };
                item.PropertyChanged += CheckableItem_PropertyChanged;
                items.Add(item);
                LoggingService.Trace($"Added checkable item: {choice} (Checked={isChecked})", component: "ParameterViewModel");
            }

            CheckableItems = items;
            LoggingService.Info($"CheckableItems initialized for '{Name}': {items.Count} items, {selectedValues.Count} pre-selected", component: "ParameterViewModel");

            if (_isMultiSelect)
            {
                var arrayValue = selectedValues.ToArray();
                var displayValue = string.Join(",", selectedValues);
                LoggingService.Debug($"Updating FormData for '{Name}' after CheckableItems init: '{displayValue}'", component: "ParameterViewModel");
                _mainWindowViewModel?.UpdateParameterValueAndValidation(Name, arrayValue);
            }
        }
        
        private void CheckableItem_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(CheckableItem.IsChecked))
            {
                var item = sender as CheckableItem;
                LoggingService.Info($"CheckableItem changed for '{Name}': Item='{item?.Value}', IsChecked={item?.IsChecked}", component: "ParameterViewModel");
                UpdateValueFromCheckableItems();
            }
        }
        
        private void UpdateValueFromCheckableItems()
        {
            if (_checkableItems != null)
            {
                var selected = _checkableItems.Where(item => item.IsChecked).Select(item => item.Value).ToList();
                
                // Store as array for multi-select ListBox
                var arrayValue = selected.ToArray();
                var displayValue = string.Join(",", selected); // For display/logging only
                
                // Always update even if going from empty to empty, to ensure FormData is initialized
                _value = displayValue; // Keep Value property as string for display
                OnPropertyChanged(nameof(Value));
                
                if (selected.Count == 0)
                {
                    LoggingService.Warn($"Multi-select '{Name}': NO items selected (empty value)", component: "ParameterViewModel");
                }
                else
                {
                    LoggingService.Debug($"Multi-select '{Name}': {selected.Count} item(s) selected: {displayValue}", component: "ParameterViewModel");
                }
                
                // Update FormData with array value and trigger validation in real-time
                _mainWindowViewModel?.UpdateParameterValueAndValidation(Name, arrayValue);
            }
        }

        public string Value
        {
            get
            {
                if (ParameterType == typeof(bool) || IsSwitch)
                {
                    // For checkboxes, Value is the string representation of BoolValue
                    return _boolValue.HasValue ? _boolValue.Value.ToString() : "False";
                }
                if (_isNumeric)
                {
                    if (!_numericValue.HasValue)
                        return null;
                        
                    // Apply format if specified
                    if (!string.IsNullOrEmpty(_numericFormat))
                    {
                        try
                        {
                            return _numericValue.Value.ToString(_numericFormat, CultureInfo.CurrentCulture);
                        }
                        catch
                        {
                            // Fallback to plain formatting if format string is invalid
                            return _numericValue.Value.ToString(CultureInfo.InvariantCulture);
                        }
                    }
                    
                    return _numericValue.Value.ToString(CultureInfo.InvariantCulture);
                }
                if (_isDate)
                {
                    return _dateValue.HasValue
                        ? _dateValue.Value.ToString(DateDisplayFormat, CultureInfo.InvariantCulture)
                        : null;
                }
                return _value as string;
            }
            set
            {
                if (ParameterType == typeof(bool) || IsSwitch)
                {
                    // For checkboxes, parse and set BoolValue directly
                    bool parsed = false;
                    if (value != null && bool.TryParse(value, out parsed))
                    {
                        if (_boolValue != parsed)
                        {
                            LoggingService.Debug($"[DIAG] Value SETTER for checkbox '{Name}': Old='{_boolValue}', New='{parsed}'", component: "ParameterViewModel");
                            _boolValue = parsed;
                            OnPropertyChanged(nameof(BoolValue));
                            OnPropertyChanged(nameof(Value));
                        }
                    }
                    return;
                }

                if (_isNumeric)
                {
                    if (string.IsNullOrWhiteSpace(value))
                    {
                        NumericValue = null;
                    }
                    else
                    {
                        // Try parsing with current culture first (handles formatted input like $1,500.00 or 15%)
                        if (double.TryParse(value, NumberStyles.Any, CultureInfo.CurrentCulture, out double numericParsed))
                        {
                            NumericValue = numericParsed;
                        }
                        // Fallback to invariant culture
                        else if (double.TryParse(value, NumberStyles.Any, CultureInfo.InvariantCulture, out numericParsed))
                        {
                            NumericValue = numericParsed;
                        }
                    }
                    return;
                }

                if (_isDate)
                {
                    if (string.IsNullOrWhiteSpace(value))
                    {
                        DateValue = null;
                    }
                    else if (DateTime.TryParse(value, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out DateTime parsedDate) ||
                             DateTime.TryParseExact(value, DateDisplayFormat, CultureInfo.InvariantCulture, DateTimeStyles.AssumeLocal, out parsedDate))
                    {
                        DateValue = parsedDate;
                    }
                    return;
                }

                if (_value as string != value)
                {
                    LoggingService.Debug($"[DIAG] Value SETTER for '{Name}': Old='{_value}', New='{value}'", component: "ParameterViewModel");
                    _value = value;
                    OnPropertyChanged(nameof(Value));
                    
                    // If this is a multi-select ListBox and CheckableItems exist, refresh them (legacy checkbox UI)
                    if (_isMultiSelect && _checkableItems != null && _choices != null)
                    {
                        LoggingService.Debug($"Value changed for multi-select '{Name}' - refreshing CheckableItems", component: "ParameterViewModel");
                        RefreshCheckableItemsFromValue();
                    }
                    
                    // If this is a multi-select ListBox, sync SelectedItems from Value (new ListBox UI)
                    if (_isMultiSelect && _isListBox && !string.IsNullOrWhiteSpace(value))
                    {
                        var values = value.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries)
                                          .Select(v => v.Trim())
                                          .ToList();
                        
                        // Only update if different to avoid circular updates
                        if (!values.SequenceEqual(SelectedItems))
                        {
                            SelectedItems.Clear();
                            foreach (var val in values)
                            {
                                SelectedItems.Add(val);
                            }
                            LoggingService.Debug($"Synced SelectedItems for '{Name}' from Value: {SelectedItems.Count} items", component: "ParameterViewModel");
                        }
                    }

                    if (_isOptionGroup)
                    {
                        RefreshOptionItemsFromValue();
                    }
                    
                    // Phase 2: Always notify of value changes for dependency refresh
                    _mainWindowViewModel?.UpdateParameterValueAndValidation(Name, _value);
                }
            }
        }
        
        private void RefreshCheckableItemsFromValue()
        {
            if (_checkableItems == null || _choices == null) return;
            
            // Parse the comma-separated value
            var selectedValues = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            if (!string.IsNullOrEmpty(_value as string))
            {
                var valueParts = (_value as string).Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
                foreach (var part in valueParts)
                {
                    selectedValues.Add(part.Trim());
                }
            }
            
            // Update IsChecked for each item (without triggering PropertyChanged events)
            foreach (var item in _checkableItems)
            {
                item.PropertyChanged -= CheckableItem_PropertyChanged;
                item.IsChecked = selectedValues.Contains(item.Value);
                item.PropertyChanged += CheckableItem_PropertyChanged;
            }
            
            LoggingService.Debug($"Refreshed CheckableItems for '{Name}': {selectedValues.Count} items checked", component: "ParameterViewModel");
        }

        public bool? BoolValue
        {
            get => _boolValue;
            set
            {
                if (_boolValue != value)
                {
                    LoggingService.Debug($"[DIAG] BoolValue SETTER for '{Name}': Old='{_boolValue}', New='{value}'", component: "ParameterViewModel");
                    _boolValue = value;
                    OnPropertyChanged(nameof(BoolValue));
                    OnPropertyChanged(nameof(Value));
                }
            }
        }

        public SecureString SecureValue
        {
            get => _secureValue;
            set
            {
                if (!IsPassword)
                {
                    LoggingService.Warn($"Attempted to set SecureValue for non-SecureString parameter '{Name}'. Ignored.", component: "ParameterViewModel");
                    return;
                }
                LoggingService.Info($">>> PVMS SECURE SETTER ('{Name}') called. Current internal length: {_secureValue?.Length ?? 0}", component: "ParameterViewModel");
                if (_secureValue != value)
                {
                    _secureValue = value;
                    _value = "****";
                    OnPropertyChanged(nameof(SecureValue));
                    LoggingService.Info($">>> PVMS SECURE SETTER ('{Name}') finished. Internal length is now: {_secureValue?.Length ?? 0}, _value is '****'", component: "ParameterViewModel");
                }
                else
                {
                    LoggingService.Info($">>> PVMS SECURE SETTER ('{Name}') skipped - value unchanged.", component: "ParameterViewModel");
                }
            }
        }

        // Lazy initialized command property
        public ICommand BrowsePathCommand
        {
            get
            {
                if (_browsePathCommand == null)
                {
                    LoggingService.Trace($"Lazily creating BrowsePathCommand for {Name}...", component: "ParameterViewModel");
                    if (_pathType == Services.PathSelectorType.File || _pathType == Services.PathSelectorType.Folder)
                    {
                        _browsePathCommand = new RelayCommand(ExecuteBrowsePath, CanExecuteBrowsePath);
                        LoggingService.Trace($"BrowsePathCommand created for {Name} with PathType={_pathType}", component: "ParameterViewModel");
                    }
                }
                return _browsePathCommand;
            }
        }

        public ParameterViewModel(ParameterInfo info, MainWindowViewModel mainVm, object existingValue, IDialogService dialogService)
        {
            try
            {
                LoggingService.Info($"Creating ParameterViewModel for parameter '{info.Name}'", component: "ParameterViewModel");
                _dialogService = dialogService ?? throw new ArgumentNullException(nameof(dialogService));
                _mainWindowViewModel = mainVm;
                _name = info.Name;
                _label = info.Label ?? info.Name;
                _isMandatory = info.IsMandatory;
                _parameterType = info.ParameterType;
                _validationPattern = info.ValidationPattern;
                _controlWidth = info.ControlWidth;
                _isSwitch = info.IsSwitch;
                _pathType = info.PathType;
                _pathFilter = info.PathFilter;
                _dialogTitle = info.DialogTitle;
                _isListBox = info.IsListBox;
                _isMultiSelect = info.IsMultiSelect;
                _isNumeric = info.IsNumeric;
                _numericMinimum = info.NumericMinimum;
                _numericMaximum = info.NumericMaximum;
                _numericStep = info.NumericStep;
                _numericAllowDecimal = info.NumericAllowDecimal;
                _numericFormat = info.NumericFormat;
                _isDate = info.IsDate;
                _dateMinimum = info.DateMinimum;
                _dateMaximum = info.DateMaximum;
                _dateFormat = info.DateFormat;
                _isOptionGroup = info.IsOptionGroup;
                _optionGroupHorizontalLayout = info.OptionGroupHorizontalLayout;
                _isMultiLineText = info.IsMultiLineText;
                _multiLineRows = info.MultiLineRows;
                _isDynamic = info.IsDynamic; // Phase 2: Track dynamic parameters
                
                object defaultValue = info.DefaultValue;
                
                double? numericDefault = null;
                if (_isNumeric && defaultValue != null && TryConvertToDouble(defaultValue, out double numericDefaultVal))
                {
                    numericDefault = numericDefaultVal;
                }

                DateTime? dateDefault = null;
                if (_isDate && defaultValue != null && TryConvertToDateTime(defaultValue, out DateTime dateDefaultVal))
                {
                    dateDefault = dateDefaultVal;
                }

                // Handle existing value FIRST (before initializing CheckableItems)
                if (existingValue != null)
                {
                    LoggingService.Trace($"Setting existing value for '{Name}': Type={existingValue.GetType().Name}, Value='{existingValue}'", component: "ParameterViewModel");
                    if (_isNumeric && TryConvertToDouble(existingValue, out double numericExisting))
                    {
                        NumericValue = numericExisting;
                    }
                    else if (_isDate && TryConvertToDateTime(existingValue, out DateTime dateExisting))
                    {
                        DateValue = dateExisting;
                    }
                    else if (ParameterType == typeof(bool) || IsSwitch)
                    {
                        if (existingValue is bool boolVal)
                        {
                            BoolValue = boolVal;
                        }
                        else if (existingValue is string strVal)
                        {
                            if (bool.TryParse(strVal, out bool parsedBool))
                                BoolValue = parsedBool;
                        }
                        else
                        {
                            try
                            {
                                BoolValue = Convert.ToBoolean(existingValue);
                            }
                            catch
                            {
                                BoolValue = false; // fallback
                            }
                        }
                    }
                    else if (existingValue is SecureString secureVal)
                    {
                        SecureValue = secureVal;
                    }
                    else if (existingValue is IEnumerable enumerableValue && !(existingValue is string))
                    {
                        var collectedValues = new List<string>();
                        foreach (var element in enumerableValue)
                        {
                            if (element == null)
                            {
                                continue;
                            }

                            var elementString = element.ToString();
                            if (!string.IsNullOrWhiteSpace(elementString))
                            {
                                collectedValues.Add(elementString);
                            }
                        }

                        var joined = string.Join(",", collectedValues);
                        LoggingService.Debug($"Initializing multi-value parameter '{Name}' with values: {joined}", component: "ParameterViewModel");
                        _value = joined;
                        OnPropertyChanged(nameof(Value));

                        // For checkbox-based multi-select (legacy)
                        if (_isMultiSelect && _checkableItems != null)
                        {
                            RefreshCheckableItemsFromValue();
                        }
                        
                        // For ListBox-based multi-select (new UI) - populate SelectedItems collection
                        if (_isMultiSelect && _isListBox)
                        {
                            SelectedItems.Clear();
                            foreach (var val in collectedValues)
                            {
                                SelectedItems.Add(val);
                            }
                            LoggingService.Debug($"Populated SelectedItems for '{Name}' with {SelectedItems.Count} items", component: "ParameterViewModel");
                        }
                    }
                    else
                    {
                        Value = existingValue.ToString();
                    }
                }
                else
                {
                    LoggingService.Trace($"No existing value provided for '{Name}'", component: "ParameterViewModel");
                    if (_isNumeric && numericDefault.HasValue)
                    {
                        NumericValue = numericDefault;
                    }
                    else if (_isDate && dateDefault.HasValue)
                    {
                        DateValue = dateDefault;
                    }
                    else if (ParameterType == typeof(bool) || IsSwitch)
                    {
                        BoolValue = false; // Ensure default is false
                    }
                    else if (defaultValue is IEnumerable enumerableDefault && !(defaultValue is string))
                    {
                        var collectedDefaults = new List<string>();
                        foreach (var element in enumerableDefault)
                        {
                            if (element == null)
                            {
                                continue;
                            }

                            var elementString = element.ToString();
                            if (!string.IsNullOrWhiteSpace(elementString))
                            {
                                collectedDefaults.Add(elementString);
                            }
                        }

                        if (collectedDefaults.Count > 0)
                        {
                            var joinedDefaults = string.Join(",", collectedDefaults);
                            LoggingService.Debug($"Initializing default multi-value parameter '{Name}' with values: {joinedDefaults}", component: "ParameterViewModel");
                            Value = joinedDefaults;
                        }
                        else
                        {
                            Value = string.Empty;
                        }
                    }
                    else if (defaultValue != null)
                    {
                        // For dynamic parameters without choices, don't set Value from defaultValue
                        // as it might contain script text that should not be displayed
                        if (!_isDynamic || (info.ValidateSetChoices != null && info.ValidateSetChoices.Any()))
                        {
                            Value = defaultValue.ToString();
                        }
                        else
                        {
                            LoggingService.Trace($"Skipping defaultValue for dynamic parameter '{Name}' - waiting for data source execution", component: "ParameterViewModel");
                        }
                    }
                }

                // NOW initialize Choices (after existingValue is loaded into _value)
                if (info.ValidateSetChoices != null && info.ValidateSetChoices.Any())
                {
                    LoggingService.Trace($"Setting up choices for '{Name}': {string.Join(", ", info.ValidateSetChoices)}", component: "ParameterViewModel");
                    Choices = new ObservableCollection<string>(info.ValidateSetChoices);
                    LoggingService.Info($"After setting Choices for '{Name}': CheckableItems count={_checkableItems?.Count ?? 0}", component: "ParameterViewModel");
                    
                    // For multi-select, eagerly ensure CheckableItems are initialized
                    // This must happen BEFORE validation runs, so we can't rely on lazy initialization
                    if (_isMultiSelect && _checkableItems == null)
                    {
                        LoggingService.Debug($"Eagerly initializing CheckableItems for '{Name}' in constructor", component: "ParameterViewModel");
                        _ = CheckableItems; // Trigger the getter which will lazy-initialize
                    }
                }

                LoggingService.Info($"ParameterViewModel created successfully for '{Name}'", component: "ParameterViewModel");
            }
            catch (Exception ex)
            {
                LoggingService.Error($"Error creating ParameterViewModel for '{info?.Name ?? "unknown"}'", ex, component: "ParameterViewModel");
                throw; // Re-throw to let caller handle
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        private bool CanExecuteBrowsePath(object parameter)
        {
            return true; // Always enabled if command exists
        }

        private void ExecuteBrowsePath(object parameter)
        {
            try
            {
                string result = null;
                string initialPath = string.IsNullOrEmpty(Value) ? Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments) : Value;

                if (_pathType == Services.PathSelectorType.File)
                {
                    string title = !string.IsNullOrEmpty(_dialogTitle) ? _dialogTitle : "Select File";
                    result = _dialogService.ShowOpenFileDialog(initialPath, null, _pathFilter, title);
                }
                else if (_pathType == Services.PathSelectorType.Folder)
                {
                    string description = !string.IsNullOrEmpty(_dialogTitle) ? _dialogTitle : "Select Folder";
                    result = _dialogService.ShowFolderBrowserDialog(description, initialPath);
                }

                if (!string.IsNullOrEmpty(result))
                {
                    Value = result;
                }
            }
            catch (Exception ex)
            {
                LoggingService.Error($"Error browsing for path", ex, component: "ParameterViewModel");
            }
        }

        private void AdjustNumericValue(int direction)
        {
            if (!_isNumeric)
            {
                return;
            }

            double step = NumericStep;
            if (step <= 0)
            {
                step = _numericAllowDecimal ? 0.1 : 1;
            }

            double current = _numericValue ?? (_numericMinimum ?? 0);
            double newValue = current + (step * direction);

            NumericValue = newValue;
        }

        private void InitializeOptionItems()
        {
            if (!_isOptionGroup)
            {
                return;
            }

            if (_optionItems != null)
            {
                foreach (var existing in _optionItems)
                {
                    existing.PropertyChanged -= OptionItem_PropertyChanged;
                }
            }

            if (_choices == null || _choices.Count == 0)
            {
                OptionItems = null;
                return;
            }

            var items = new ObservableCollection<OptionItem>();
            foreach (var choice in _choices)
            {
                var item = new OptionItem { Value = choice };
                item.PropertyChanged += OptionItem_PropertyChanged;
                items.Add(item);
            }

            OptionItems = items;
            RefreshOptionItemsFromValue();
        }

        private void RefreshOptionItemsFromValue()
        {
            if (!_isOptionGroup || _optionItems == null)
            {
                return;
            }

            _isUpdatingOptionItems = true;
            try
            {
                string currentValue = _value as string;
                foreach (var item in _optionItems)
                {
                    bool shouldSelect = string.Equals(item.Value, currentValue, StringComparison.OrdinalIgnoreCase);
                    if (item.IsSelected != shouldSelect)
                    {
                        item.IsSelected = shouldSelect;
                    }
                }
            }
            finally
            {
                _isUpdatingOptionItems = false;
            }
        }

        private void OptionItem_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (_isUpdatingOptionItems || e.PropertyName != nameof(OptionItem.IsSelected))
            {
                return;
            }

            if (sender is OptionItem item && item.IsSelected)
            {
                _isUpdatingOptionItems = true;
                try
                {
                    foreach (var option in _optionItems)
                    {
                        if (!ReferenceEquals(option, item) && option.IsSelected)
                        {
                            option.IsSelected = false;
                        }
                    }

                    Value = item.Value;
                    _mainWindowViewModel?.UpdateParameterValueAndValidation(Name, item.Value);
                }
                finally
                {
                    _isUpdatingOptionItems = false;
                }
            }
        }

        private double? ClampNumericValue(double? value)
        {
            if (!_isNumeric || !value.HasValue)
            {
                return value;
            }

            double clamped = value.Value;

            if (_numericMinimum.HasValue && clamped < _numericMinimum.Value)
            {
                clamped = _numericMinimum.Value;
            }

            if (_numericMaximum.HasValue && clamped > _numericMaximum.Value)
            {
                clamped = _numericMaximum.Value;
            }

            if (!_numericAllowDecimal)
            {
                clamped = Math.Round(clamped);
            }

            return clamped;
        }

        private DateTime? ClampDateValue(DateTime? value)
        {
            if (!_isDate || !value.HasValue)
            {
                return value;
            }

            DateTime clamped = value.Value;

            if (_dateMinimum.HasValue && clamped < _dateMinimum.Value)
            {
                clamped = _dateMinimum.Value;
            }

            if (_dateMaximum.HasValue && clamped > _dateMaximum.Value)
            {
                clamped = _dateMaximum.Value;
            }

            return clamped;
        }

        private bool TryConvertToDouble(object value, out double numericValue)
        {
            numericValue = 0;

            if (value == null)
            {
                return false;
            }

            switch (value)
            {
                case double d:
                    numericValue = d;
                    return true;
                case float f:
                    numericValue = f;
                    return true;
                case int i:
                    numericValue = i;
                    return true;
                case long l:
                    numericValue = l;
                    return true;
                case decimal dec:
                    numericValue = (double)dec;
                    return true;
            }

            return double.TryParse(value.ToString(), NumberStyles.Any, CultureInfo.InvariantCulture, out numericValue);
        }

        private bool TryConvertToDateTime(object value, out DateTime dateValue)
        {
            dateValue = default;

            if (value == null)
            {
                return false;
            }

            if (value is DateTime dt)
            {
                dateValue = dt;
                return true;
            }

            string raw = value.ToString();

            if (DateTime.TryParse(raw, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out DateTime parsed))
            {
                dateValue = parsed;
                return true;
            }

            // Try format specified for the parameter, then fallback to ISO date
            string[] formats = string.IsNullOrWhiteSpace(DateDisplayFormat)
                ? new[] { "yyyy-MM-dd", "yyyy-MM-ddTHH:mm:ss", "yyyy-MM-ddTHH:mm:ssZ" }
                : new[] { DateDisplayFormat, "yyyy-MM-dd", "yyyy-MM-ddTHH:mm:ss", "yyyy-MM-ddTHH:mm:ssZ" };

            return DateTime.TryParseExact(raw, formats, CultureInfo.InvariantCulture, DateTimeStyles.AssumeLocal, out dateValue);
        }
    }
}
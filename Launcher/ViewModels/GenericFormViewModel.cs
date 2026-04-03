// Copyright (c) 2025 Kanders-II. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using Launcher.Services;

namespace Launcher.ViewModels
{
    /// <summary>
    /// Represents a group of parameters sharing the same category, for Settings UI display.
    /// </summary>
    public class ParameterCategoryGroup : INotifyPropertyChanged
    {
        private string _categoryName;
        private ObservableCollection<ParameterViewModel> _parameters;
        private ObservableCollection<ParameterViewModel> _buttonParameters;
        private int _requestedGridColumns;

        public string CategoryName
        {
            get => _categoryName;
            set { _categoryName = value; OnPropertyChanged(); }
        }

        public ObservableCollection<ParameterViewModel> Parameters
        {
            get => _parameters;
            set { _parameters = value; OnPropertyChanged(); }
        }

        /// <summary>
        /// Buttons that belong to this category group (displayed inside the category card).
        /// </summary>
        public ObservableCollection<ParameterViewModel> ButtonParameters
        {
            get => _buttonParameters;
            set
            {
                if (_buttonParameters != null)
                {
                    _buttonParameters.CollectionChanged -= ButtonParameters_CollectionChanged;
                }
                _buttonParameters = value;
                if (_buttonParameters != null)
                {
                    _buttonParameters.CollectionChanged += ButtonParameters_CollectionChanged;
                }
                OnPropertyChanged();
                OnPropertyChanged(nameof(HasButtons));
            }
        }

        private void ButtonParameters_CollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            OnPropertyChanged(nameof(HasButtons));
        }

        /// <summary>
        /// Whether this category has any buttons to display.
        /// </summary>
        public bool HasButtons => _buttonParameters != null && _buttonParameters.Count > 0;

        /// <summary>
        /// Requested number of grid columns for this category group (propagated from GenericFormViewModel).
        /// </summary>
        public int RequestedGridColumns
        {
            get => _requestedGridColumns;
            set { _requestedGridColumns = value; OnPropertyChanged(); }
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }

    /// <summary>
    /// Represents a tab containing category groups, for Freeform tab-based layout.
    /// </summary>
    public class TabGroup : INotifyPropertyChanged
    {
        private string _tabName;
        private ObservableCollection<ParameterCategoryGroup> _categoryGroups = new ObservableCollection<ParameterCategoryGroup>();
        private ObservableCollection<ParameterViewModel> _buttonParameters = new ObservableCollection<ParameterViewModel>();

        public string TabName
        {
            get => _tabName;
            set { _tabName = value; OnPropertyChanged(); }
        }

        public ObservableCollection<ParameterCategoryGroup> CategoryGroups
        {
            get => _categoryGroups;
            set { _categoryGroups = value; OnPropertyChanged(); }
        }

        /// <summary>
        /// Button parameters assigned to this tab, rendered at the bottom of the tab content.
        /// </summary>
        public ObservableCollection<ParameterViewModel> ButtonParameters
        {
            get => _buttonParameters;
            set { _buttonParameters = value; OnPropertyChanged(); }
        }

        /// <summary>
        /// True when this tab has at least one button parameter.
        /// </summary>
        public bool HasButtons => _buttonParameters != null && _buttonParameters.Count > 0;

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }

    public class GenericFormViewModel : INotifyPropertyChanged
    {
        private string _title;
        private string _description;
        private bool _isFreeformGrid;
        private string _searchText = string.Empty;
        private ObservableCollection<ParameterViewModel> _parameters = new ObservableCollection<ParameterViewModel>();
        private ObservableCollection<CardViewModel> _additionalCards = new ObservableCollection<CardViewModel>();
        private ObservableCollection<BannerViewModel> _banners = new ObservableCollection<BannerViewModel>();
        private ObservableCollection<ParameterCategoryGroup> _categoryGroups = new ObservableCollection<ParameterCategoryGroup>();
        private ObservableCollection<ParameterViewModel> _buttonParameters = new ObservableCollection<ParameterViewModel>();
        private ObservableCollection<TabGroup> _tabGroups = new ObservableCollection<TabGroup>();
        private List<string> _tabNames = new List<string>();

        public string Title
        {
            get => _title;
            set { _title = value; OnPropertyChanged(); }
        }

        public string Description
        {
            get => _description;
            set { _description = value; OnPropertyChanged(); }
        }

        public ObservableCollection<ParameterViewModel> Parameters
        {
            get => _parameters;
            set { _parameters = value; OnPropertyChanged(); }
        }

        public ObservableCollection<CardViewModel> AdditionalCards
        {
            get => _additionalCards;
            set { _additionalCards = value; OnPropertyChanged(); }
        }

        public ObservableCollection<BannerViewModel> Banners
        {
            get => _banners;
            set { _banners = value; OnPropertyChanged(); }
        }

        /// <summary>
        /// True when this form should use a WPF Grid layout (Freeform pages with Row/Column properties).
        /// </summary>
        public bool IsFreeformGrid
        {
            get => _isFreeformGrid;
            set { _isFreeformGrid = value; OnPropertyChanged(); }
        }

        private int _requestedGridColumns;
        /// <summary>
        /// Requested number of grid columns from branding (GridColumns parameter). 0 = auto-detect.
        /// </summary>
        public int RequestedGridColumns
        {
            get => _requestedGridColumns;
            set { _requestedGridColumns = value; OnPropertyChanged(); }
        }

        /// <summary>
        /// Number of columns needed for the grid layout (max Column + 1 from all parameters).
        /// </summary>
        public int GridColumnCount
        {
            get
            {
                if (!_isFreeformGrid || _parameters == null || _parameters.Count == 0) return 1;
                int maxCol = _parameters.Where(p => p.GridColumn >= 0).Select(p => p.GridColumn).DefaultIfEmpty(0).Max();
                return maxCol + 1;
            }
        }

        /// <summary>
        /// Number of rows needed for the grid layout (max Row + 1 from all parameters).
        /// </summary>
        public int GridRowCount
        {
            get
            {
                if (!_isFreeformGrid || _parameters == null || _parameters.Count == 0) return 1;
                int maxRow = _parameters.Where(p => p.GridRow >= 0).Select(p => p.GridRow).DefaultIfEmpty(0).Max();
                return maxRow + 1;
            }
        }

        /// <summary>
        /// Search/filter text for the Settings UI. Rebuilds category groups on change.
        /// </summary>
        public string SearchText
        {
            get => _searchText;
            set
            {
                if (_searchText != value)
                {
                    _searchText = value;
                    OnPropertyChanged();
                    RebuildCategoryGroups();
                }
            }
        }

        /// <summary>
        /// Parameters grouped by Category for Settings UI display.
        /// </summary>
        public ObservableCollection<ParameterCategoryGroup> CategoryGroups
        {
            get => _categoryGroups;
            private set { _categoryGroups = value; OnPropertyChanged(); }
        }

        /// <summary>
        /// True when parameters have at least 2 distinct categories, enabling the categorized layout.
        /// </summary>
        public bool HasCategories
        {
            get
            {
                if (_parameters == null || _parameters.Count == 0) return false;
                var nonButtons = _parameters.Where(p => !p.IsButton);
                return nonButtons.Select(p => p.Category ?? "General").Distinct(StringComparer.OrdinalIgnoreCase).Count() > 1;
            }
        }

        /// <summary>
        /// Button parameters separated from form fields, rendered in the action bar.
        /// </summary>
        public ObservableCollection<ParameterViewModel> ButtonParameters
        {
            get => _buttonParameters;
            private set { _buttonParameters = value; OnPropertyChanged(); }
        }

        /// <summary>
        /// Tab groups for Freeform tab-based layout. Each tab contains its own category groups.
        /// </summary>
        public ObservableCollection<TabGroup> TabGroups
        {
            get => _tabGroups;
            private set { _tabGroups = value; OnPropertyChanged(); }
        }

        /// <summary>
        /// True when a TabControl has been defined, enabling tab-based layout.
        /// </summary>
        public bool HasTabs => _tabNames != null && _tabNames.Count > 0;

        /// <summary>
        /// Sets the tab names from a TabControl definition.
        /// </summary>
        public void SetTabNames(List<string> tabNames)
        {
            _tabNames = tabNames ?? new List<string>();
            OnPropertyChanged(nameof(HasTabs));
        }

        public GenericFormViewModel(string title, string description = "")
        {
            LoggingService.Trace($"Creating GenericFormViewModel with title: '{title}', description: '{description}'");
            _title = title;
            _description = description;
            LoggingService.Trace($"GenericFormViewModel created with title: '{_title}', description: '{_description}'");
        }

        /// <summary>
        /// Rebuilds CategoryGroups from Parameters, applying search filter.
        /// Excludes button parameters (they go to ButtonParameters instead).
        /// Call this after parameters are loaded or when SearchText changes.
        /// </summary>
        public void RebuildCategoryGroups()
        {
            var nonButtons = _parameters.Where(p => !p.IsButton).ToList();
            var buttons = _parameters.Where(p => p.IsButton).ToList();
            LoggingService.Info($"RebuildCategoryGroups: Total={_parameters.Count}, NonButtons={nonButtons.Count}, Buttons={buttons.Count}, TabNames={string.Join(",", _tabNames)}", component: "GenericFormViewModel");

            // Clear global button list - will be populated later with uncategorized buttons
            _buttonParameters.Clear();

            // Filter non-button parameters
            IEnumerable<ParameterViewModel> filtered = nonButtons;

            if (!string.IsNullOrWhiteSpace(_searchText))
            {
                var search = _searchText.Trim();
                filtered = filtered.Where(p =>
                    (p.Label != null && p.Label.IndexOf(search, StringComparison.OrdinalIgnoreCase) >= 0) ||
                    (p.Name != null && p.Name.IndexOf(search, StringComparison.OrdinalIgnoreCase) >= 0) ||
                    (p.HelpText != null && p.HelpText.IndexOf(search, StringComparison.OrdinalIgnoreCase) >= 0) ||
                    (p.Category != null && p.Category.IndexOf(search, StringComparison.OrdinalIgnoreCase) >= 0));
            }

            var groupsList = filtered
                .GroupBy(p => p.Category ?? "General", StringComparer.OrdinalIgnoreCase)
                .OrderBy(g => g.Key, StringComparer.OrdinalIgnoreCase)
                .Select(g => new ParameterCategoryGroup
                {
                    CategoryName = g.Key,
                    Parameters = new ObservableCollection<ParameterViewModel>(g),
                    RequestedGridColumns = _requestedGridColumns,
                    ButtonParameters = new ObservableCollection<ParameterViewModel>()
                })
                .ToList();

            // Distribute buttons to their matching category groups
            foreach (var btn in buttons)
            {
                if (!string.IsNullOrEmpty(btn.ButtonCategory))
                {
                    var matchingCat = groupsList.FirstOrDefault(cg =>
                        cg.CategoryName.Equals(btn.ButtonCategory, StringComparison.OrdinalIgnoreCase));
                    if (matchingCat != null)
                    {
                        matchingCat.ButtonParameters.Add(btn);
                    }
                    else
                    {
                        // Category doesn't exist - add to global button list
                        _buttonParameters.Add(btn);
                    }
                }
                else
                {
                    // No category - add to global button list
                    _buttonParameters.Add(btn);
                }
            }

            _categoryGroups.Clear();
            foreach (var group in groupsList)
            {
                _categoryGroups.Add(group);
            }

            OnPropertyChanged(nameof(CategoryGroups));
            OnPropertyChanged(nameof(HasCategories));

            // Also rebuild tab groups if tabs are defined
            if (HasTabs)
                RebuildTabGroups();
        }

        /// <summary>
        /// Rebuilds TabGroups by distributing parameters into tabs.
        /// Non-button parameters are grouped by Category within each tab.
        /// Button parameters are placed into each tab's ButtonParameters collection.
        /// Parameters without a Tab assignment default to the first tab.
        /// </summary>
        public void RebuildTabGroups()
        {
            var nonButtons = _parameters.Where(p => !p.IsButton).ToList();
            var buttons = _parameters.Where(p => p.IsButton).ToList();
            string firstTab = _tabNames.Count > 0 ? _tabNames[0] : "General";

            _tabGroups.Clear();
            foreach (var tabName in _tabNames)
            {
                // Non-button parameters assigned to this tab, or to the first tab if unassigned
                var tabParams = nonButtons.Where(p =>
                {
                    var pTab = p.Tab;
                    if (string.IsNullOrEmpty(pTab))
                        return tabName.Equals(firstTab, StringComparison.OrdinalIgnoreCase);
                    return pTab.Equals(tabName, StringComparison.OrdinalIgnoreCase);
                });

                var catGroupsList = tabParams
                    .GroupBy(p => p.Category ?? "General", StringComparer.OrdinalIgnoreCase)
                    .OrderBy(g => g.Key, StringComparer.OrdinalIgnoreCase)
                    .Select(g => new ParameterCategoryGroup
                    {
                        CategoryName = g.Key,
                        Parameters = new ObservableCollection<ParameterViewModel>(g),
                        RequestedGridColumns = _requestedGridColumns,
                        ButtonParameters = new ObservableCollection<ParameterViewModel>()
                    })
                    .ToList();

                // Button parameters assigned to this tab, or to the first tab if unassigned
                var tabButtons = buttons.Where(p =>
                {
                    var pTab = p.Tab;
                    if (string.IsNullOrEmpty(pTab))
                        return tabName.Equals(firstTab, StringComparison.OrdinalIgnoreCase);
                    return pTab.Equals(tabName, StringComparison.OrdinalIgnoreCase);
                }).ToList();

                // Distribute buttons to category groups based on ButtonCategory
                var tabLevelButtons = new List<ParameterViewModel>();
                foreach (var btn in tabButtons)
                {
                    if (!string.IsNullOrEmpty(btn.ButtonCategory))
                    {
                        // Find matching category group
                        var matchingCat = catGroupsList.FirstOrDefault(cg =>
                            cg.CategoryName.Equals(btn.ButtonCategory, StringComparison.OrdinalIgnoreCase));
                        if (matchingCat != null)
                        {
                            matchingCat.ButtonParameters.Add(btn);
                        }
                        else
                        {
                            // Category doesn't exist - add to tab-level buttons
                            tabLevelButtons.Add(btn);
                        }
                    }
                    else
                    {
                        // No category specified - add to tab-level buttons
                        tabLevelButtons.Add(btn);
                    }
                }

                var tabGroup = new TabGroup { TabName = tabName };
                foreach (var cg in catGroupsList)
                    tabGroup.CategoryGroups.Add(cg);
                foreach (var btn in tabLevelButtons)
                    tabGroup.ButtonParameters.Add(btn);
                _tabGroups.Add(tabGroup);
            }

            OnPropertyChanged(nameof(TabGroups));
            OnPropertyChanged(nameof(HasTabs));
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
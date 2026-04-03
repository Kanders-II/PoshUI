// Copyright (c) 2025 Kanders-II. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows.Input;
using System.Windows.Media;
using Launcher.Services;

namespace Launcher.ViewModels
{
    /// <summary>
    /// ViewModel for displaying status indicator cards with colored status dots in Dashboard mode.
    /// Shows a list of items each with a name, status text, and colored status indicator.
    /// </summary>
    public class StatusIndicatorCardViewModel : INotifyPropertyChanged
    {
        private string _title = string.Empty;
        private string _description = string.Empty;
        private string _icon = string.Empty;
        private string _category = "General";
        private string _refreshScript;
        private bool _isRefreshing;
        private Brush _accentBrush = new SolidColorBrush(Color.FromRgb(0, 120, 212));
        private ObservableCollection<StatusItem> _items = new ObservableCollection<StatusItem>();

        public event PropertyChangedEventHandler PropertyChanged;

        public StatusIndicatorCardViewModel()
        {
            RefreshCommand = new RelayCommand(_ => ExecuteRefresh());
        }

        #region Properties

        public string Name { get; set; }

        public string Title
        {
            get => _title;
            set { if (_title != value) { _title = value; OnPropertyChanged(); } }
        }

        public string Description
        {
            get => _description;
            set { if (_description != value) { _description = value; OnPropertyChanged(); } }
        }

        public string Icon
        {
            get => _icon;
            set { if (_icon != value) { _icon = value; OnPropertyChanged(); } }
        }

        public string Category
        {
            get => _category;
            set { if (_category != value) { _category = value; OnPropertyChanged(); } }
        }

        public Brush AccentBrush
        {
            get => _accentBrush;
            set { _accentBrush = value; OnPropertyChanged(); }
        }

        public ObservableCollection<StatusItem> Items
        {
            get => _items;
            set { _items = value; OnPropertyChanged(); }
        }

        public string RefreshScript
        {
            get => _refreshScript;
            set
            {
                if (_refreshScript != value)
                {
                    _refreshScript = value;
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(CanRefresh));
                }
            }
        }

        public bool CanRefresh => !string.IsNullOrEmpty(_refreshScript);

        public bool IsRefreshing
        {
            get => _isRefreshing;
            set { if (_isRefreshing != value) { _isRefreshing = value; OnPropertyChanged(); } }
        }

        #endregion

        #region Commands

        public ICommand RefreshCommand { get; }

        #endregion

        #region Command Implementations

        private async void ExecuteRefresh()
        {
            if (IsRefreshing || string.IsNullOrEmpty(RefreshScript)) return;

            IsRefreshing = true;
            try
            {
                await Task.Run(() =>
                {
                    LoggingService.Debug($"StatusIndicatorCard '{Title}' refresh triggered", component: "StatusIndicatorCard");
                });
            }
            catch (Exception ex)
            {
                LoggingService.Error($"StatusIndicatorCard refresh error: {ex.Message}", component: "StatusIndicatorCard");
            }
            finally
            {
                IsRefreshing = false;
            }
        }

        #endregion

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }

    /// <summary>
    /// Represents a single status item with a name, status text, and status color.
    /// </summary>
    public class StatusItem : INotifyPropertyChanged
    {
        private string _label = string.Empty;
        private string _status = string.Empty;
        private Brush _statusColor = Brushes.Gray;

        public event PropertyChangedEventHandler PropertyChanged;

        public string Label
        {
            get => _label;
            set { if (_label != value) { _label = value; OnPropertyChanged(); } }
        }

        public string Status
        {
            get => _status;
            set { if (_status != value) { _status = value; OnPropertyChanged(); } }
        }

        public Brush StatusColor
        {
            get => _statusColor;
            set { _statusColor = value; OnPropertyChanged(); }
        }

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        /// <summary>
        /// Creates a StatusItem with automatic color based on common status strings.
        /// </summary>
        public static StatusItem Create(string label, string status)
        {
            var item = new StatusItem { Label = label, Status = status };
            string lower = (status ?? "").ToLowerInvariant().Trim();

            if (lower == "online" || lower == "running" || lower == "healthy" || lower == "ok" || lower == "active" || lower == "up" || lower == "connected" || lower == "success")
                item.StatusColor = new SolidColorBrush(Color.FromRgb(0, 153, 0));       // Green
            else if (lower == "warning" || lower == "degraded" || lower == "slow" || lower == "pending" || lower == "starting")
                item.StatusColor = new SolidColorBrush(Color.FromRgb(255, 185, 0));      // Amber
            else if (lower == "offline" || lower == "stopped" || lower == "error" || lower == "critical" || lower == "down" || lower == "failed" || lower == "disconnected")
                item.StatusColor = new SolidColorBrush(Color.FromRgb(204, 0, 0));        // Red
            else if (lower == "maintenance" || lower == "disabled" || lower == "unknown")
                item.StatusColor = new SolidColorBrush(Color.FromRgb(128, 128, 128));    // Gray
            else
                item.StatusColor = new SolidColorBrush(Color.FromRgb(0, 120, 212));      // Blue default

            return item;
        }
    }
}

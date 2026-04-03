// Copyright (c) 2025 Kanders-II. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using System.Windows.Media;
using Launcher.Services;

namespace Launcher.ViewModels
{
    /// <summary>
    /// ViewModel for displaying information cards with text, pictures, and canvas support in Dashboard mode
    /// </summary>
    public class InfoCardViewModel : INotifyPropertyChanged
    {
        private string _title = string.Empty;
        private string _description = string.Empty;
        private string _content = string.Empty;
        private string _subtitle = string.Empty;
        private string _imagePath = string.Empty;
        private string _imageUrl = string.Empty;
        private string _icon = string.Empty;
        private string _iconPath = string.Empty;
        private string _category = "General";
        private string _style = "Info";
        private string _accentColor = string.Empty;
        private string _backgroundColor = string.Empty;
        private string _textColor = string.Empty;
        private string _borderColor = string.Empty;
        private string _buttonText = string.Empty;
        private string _buttonScript = string.Empty;
        private double _width = 350;
        private double _height = 250;
        private string _contentAlignment = "Left";
        private string _imageAlignment = "Top";
        private double _imageHeight = 120;
        private bool _showBorder = true;
        private bool _isCollapsible = false;
        private bool _isExpanded = true;
        private string _markdown = string.Empty;
        private string _htmlContent = string.Empty;
        private Brush _accentBrush = new SolidColorBrush(Color.FromRgb(0, 120, 212));

        public Brush AccentBrush { get => _accentBrush; set { _accentBrush = value; OnPropertyChanged(); } }

        public event PropertyChangedEventHandler PropertyChanged;

        public InfoCardViewModel()
        {
            OpenLinkCommand = new RelayCommand(ExecuteOpenLink, CanExecuteOpenLink);
            ToggleCollapseCommand = new RelayCommand(ExecuteToggleCollapse);
            ExecuteButtonCommand = new RelayCommand(ExecuteButton, CanExecuteButton);
        }

        #region Properties

        /// <summary>
        /// Title of the info card
        /// </summary>
        public string Title
        {
            get { return _title; }
            set { _title = value; OnPropertyChanged(); }
        }

        /// <summary>
        /// Category for filtering
        /// </summary>
        public string Category
        {
            get { return _category; }
            set { _category = value; OnPropertyChanged(); }
        }

        /// <summary>
        /// Short description or subtitle
        /// </summary>
        public string Description
        {
            get { return _description; }
            set { _description = value; OnPropertyChanged(); }
        }

        /// <summary>
        /// Subtitle displayed below the title
        /// </summary>
        public string Subtitle
        {
            get { return _subtitle; }
            set { _subtitle = value; OnPropertyChanged(); OnPropertyChanged("HasSubtitle"); }
        }

        /// <summary>
        /// Card style: Info, Success, Warning, Error, Hero
        /// </summary>
        public string Style
        {
            get { return _style; }
            set
            {
                _style = value;
                OnPropertyChanged();
                OnPropertyChanged("IsHeroStyle");
                OnPropertyChanged("IsInfoStyle");
                OnPropertyChanged("IsSuccessStyle");
                OnPropertyChanged("IsWarningStyle");
                OnPropertyChanged("IsErrorStyle");
                UpdateAccentBrushFromStyle();
            }
        }

        /// <summary>
        /// Custom accent color override (hex). If not set, derived from Style.
        /// </summary>
        public string AccentColor
        {
            get { return _accentColor; }
            set { _accentColor = value; OnPropertyChanged(); UpdateAccentBrushFromStyle(); }
        }

        /// <summary>
        /// Whether the card content can be collapsed
        /// </summary>
        public bool IsCollapsible
        {
            get { return _isCollapsible; }
            set { _isCollapsible = value; OnPropertyChanged(); }
        }

        /// <summary>
        /// Whether the card content is currently expanded (visible)
        /// </summary>
        public bool IsExpanded
        {
            get { return _isExpanded; }
            set { _isExpanded = value; OnPropertyChanged(); }
        }

        /// <summary>
        /// Text for an optional action button in the card footer
        /// </summary>
        public string ButtonText
        {
            get { return _buttonText; }
            set { _buttonText = value; OnPropertyChanged(); OnPropertyChanged("HasButton"); }
        }

        /// <summary>
        /// PowerShell script to execute when the button is clicked
        /// </summary>
        public string ButtonScript
        {
            get { return _buttonScript; }
            set { _buttonScript = value; OnPropertyChanged(); }
        }

        /// <summary>
        /// Main text content to display
        /// </summary>
        public string Content
        {
            get { return _content; }
            set 
            { 
                _content = value; 
                OnPropertyChanged(); 
                OnPropertyChanged("HasContent"); 
            }
        }

        /// <summary>
        /// Path to a local image file
        /// </summary>
        public string ImagePath
        {
            get { return _imagePath; }
            set { _imagePath = value; OnPropertyChanged(); OnPropertyChanged("HasImage"); OnPropertyChanged("ImageSource"); }
        }

        /// <summary>
        /// URL to a remote image
        /// </summary>
        public string ImageUrl
        {
            get { return _imageUrl; }
            set { _imageUrl = value; OnPropertyChanged(); OnPropertyChanged("HasImage"); OnPropertyChanged("ImageSource"); }
        }

        /// <summary>
        /// Returns the image source (path or URL)
        /// </summary>
        public string ImageSource
        {
            get { return !string.IsNullOrEmpty(ImagePath) ? ImagePath : ImageUrl; }
        }

        /// <summary>
        /// Fluent icon glyph code
        /// </summary>
        public string Icon
        {
            get { return _icon; }
            set { _icon = value; OnPropertyChanged(); OnPropertyChanged("HasIcon"); }
        }

        /// <summary>
        /// Path to a PNG icon file displayed next to the title (32x32px).
        /// When set, the PNG image is shown instead of the Segoe MDL2 glyph.
        /// </summary>
        public string IconPath
        {
            get { return _iconPath; }
            set { _iconPath = value; OnPropertyChanged(); OnPropertyChanged("HasIconPath"); }
        }

        /// <summary>
        /// Custom background color (hex or named color)
        /// </summary>
        public string BackgroundColor
        {
            get { return _backgroundColor; }
            set { _backgroundColor = value; OnPropertyChanged(); OnPropertyChanged("BackgroundBrush"); }
        }

        /// <summary>
        /// Custom text color (hex or named color)
        /// </summary>
        public string TextColor
        {
            get { return _textColor; }
            set { _textColor = value; OnPropertyChanged(); OnPropertyChanged("TextBrush"); }
        }

        /// <summary>
        /// Custom border color (hex or named color)
        /// </summary>
        public string BorderColor
        {
            get { return _borderColor; }
            set { _borderColor = value; OnPropertyChanged(); OnPropertyChanged("BorderBrush"); }
        }

        /// <summary>
        /// Card width
        /// </summary>
        public double Width
        {
            get { return _width; }
            set { _width = value; OnPropertyChanged(); }
        }

        /// <summary>
        /// Card height
        /// </summary>
        public double Height
        {
            get { return _height; }
            set { _height = value; OnPropertyChanged(); }
        }

        /// <summary>
        /// Content text alignment (Left, Center, Right)
        /// </summary>
        public string ContentAlignment
        {
            get { return _contentAlignment; }
            set { _contentAlignment = value; OnPropertyChanged(); }
        }

        /// <summary>
        /// Image position relative to content (Top, Bottom, Left, Right)
        /// </summary>
        public string ImageAlignment
        {
            get { return _imageAlignment; }
            set { _imageAlignment = value; OnPropertyChanged(); }
        }

        /// <summary>
        /// Height of the image area
        /// </summary>
        public double ImageHeight
        {
            get { return _imageHeight; }
            set { _imageHeight = value; OnPropertyChanged(); }
        }

        /// <summary>
        /// Whether to show card border
        /// </summary>
        public bool ShowBorder
        {
            get { return _showBorder; }
            set { _showBorder = value; OnPropertyChanged(); }
        }

        /// <summary>
        /// Markdown content (for rich text display)
        /// </summary>
        public string Markdown
        {
            get { return _markdown; }
            set { _markdown = value; OnPropertyChanged(); OnPropertyChanged("HasMarkdown"); }
        }

        /// <summary>
        /// HTML content (for web-style display)
        /// </summary>
        public string HtmlContent
        {
            get { return _htmlContent; }
            set { _htmlContent = value; OnPropertyChanged(); OnPropertyChanged("HasHtml"); }
        }

        /// <summary>
        /// Link URL to open when clicked
        /// </summary>
        public string LinkUrl { get; set; } = string.Empty;

        #endregion

        #region Computed Properties

        public bool HasContent { get { return !string.IsNullOrWhiteSpace(Content); } }
        public bool HasImage { get { return !string.IsNullOrWhiteSpace(ImagePath) || !string.IsNullOrWhiteSpace(ImageUrl); } }
        public bool HasIcon { get { return !string.IsNullOrWhiteSpace(Icon); } }
        public bool HasIconPath { get { return !string.IsNullOrWhiteSpace(IconPath); } }
        public bool HasMarkdown { get { return !string.IsNullOrWhiteSpace(Markdown); } }
        public bool HasHtml { get { return !string.IsNullOrWhiteSpace(HtmlContent); } }
        public bool HasLink { get { return !string.IsNullOrWhiteSpace(LinkUrl); } }
        public bool HasSubtitle { get { return !string.IsNullOrWhiteSpace(Subtitle); } }
        public bool HasButton { get { return !string.IsNullOrWhiteSpace(ButtonText); } }
        public bool HasFooter { get { return HasLink || HasButton; } }
        public bool IsHeroStyle { get { return string.Equals(Style, "Hero", StringComparison.OrdinalIgnoreCase); } }
        public bool IsInfoStyle { get { return string.Equals(Style, "Info", StringComparison.OrdinalIgnoreCase); } }
        public bool IsSuccessStyle { get { return string.Equals(Style, "Success", StringComparison.OrdinalIgnoreCase); } }
        public bool IsWarningStyle { get { return string.Equals(Style, "Warning", StringComparison.OrdinalIgnoreCase); } }
        public bool IsErrorStyle { get { return string.Equals(Style, "Error", StringComparison.OrdinalIgnoreCase); } }

        public Brush BackgroundBrush
        {
            get
            {
                if (string.IsNullOrWhiteSpace(BackgroundColor)) return null;
                try
                {
                    return new BrushConverter().ConvertFromString(BackgroundColor) as Brush;
                }
                catch { return null; }
            }
        }

        public Brush TextBrush
        {
            get
            {
                if (string.IsNullOrWhiteSpace(TextColor)) return null;
                try
                {
                    return new BrushConverter().ConvertFromString(TextColor) as Brush;
                }
                catch { return null; }
            }
        }

        public Brush BorderBrush
        {
            get
            {
                if (string.IsNullOrWhiteSpace(BorderColor)) return null;
                try
                {
                    return new BrushConverter().ConvertFromString(BorderColor) as Brush;
                }
                catch { return null; }
            }
        }

        #endregion

        #region Commands

        public ICommand OpenLinkCommand { get; private set; }
        public ICommand ToggleCollapseCommand { get; private set; }
        public ICommand ExecuteButtonCommand { get; private set; }

        private void ExecuteToggleCollapse(object parameter)
        {
            IsExpanded = !IsExpanded;
        }

        private bool CanExecuteButton(object parameter)
        {
            return HasButton;
        }

        private void ExecuteButton(object parameter)
        {
            if (string.IsNullOrWhiteSpace(ButtonScript) && !string.IsNullOrWhiteSpace(LinkUrl))
            {
                ExecuteOpenLink(parameter);
                return;
            }
            // Button script execution is handled by the parent CardGridViewModel
            LoggingService.Info(string.Format("InfoCard button clicked: {0}", Title), component: "InfoCardViewModel");
        }

        private bool CanExecuteOpenLink(object parameter)
        {
            return HasLink;
        }

        private void ExecuteOpenLink(object parameter)
        {
            if (string.IsNullOrWhiteSpace(LinkUrl)) return;
            
            try
            {
                System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                {
                    FileName = LinkUrl,
                    UseShellExecute = true
                });
            }
            catch (Exception ex)
            {
                LoggingService.Error(string.Format("Failed to open link: {0}", LinkUrl), ex, component: "InfoCardViewModel");
            }
        }

        #endregion

        protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            var handler = PropertyChanged;
            if (handler != null)
            {
                handler(this, new PropertyChangedEventArgs(propertyName));
            }
        }

        /// <summary>
        /// Updates the AccentBrush based on the Style or AccentColor property.
        /// </summary>
        private void UpdateAccentBrushFromStyle()
        {
            // Custom accent color takes priority
            if (!string.IsNullOrWhiteSpace(AccentColor))
            {
                try
                {
                    AccentBrush = new BrushConverter().ConvertFromString(AccentColor) as Brush;
                    return;
                }
                catch { }
            }

            // Default colors by style
            switch ((Style ?? "Info").ToLowerInvariant())
            {
                case "success":
                    AccentBrush = new SolidColorBrush(Color.FromRgb(16, 124, 16)); // #107C10
                    break;
                case "warning":
                    AccentBrush = new SolidColorBrush(Color.FromRgb(255, 165, 0)); // #FFA500
                    break;
                case "error":
                    AccentBrush = new SolidColorBrush(Color.FromRgb(220, 38, 38)); // #DC2626
                    break;
                case "hero":
                    AccentBrush = new SolidColorBrush(Color.FromRgb(0, 120, 212)); // #0078D4
                    break;
                case "info":
                default:
                    AccentBrush = new SolidColorBrush(Color.FromRgb(0, 120, 212)); // #0078D4
                    break;
            }
        }
    }
}

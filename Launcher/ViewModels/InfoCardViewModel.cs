// Copyright (c) 2025 A Solution IT LLC. All rights reserved.
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
        private string _imagePath = string.Empty;
        private string _imageUrl = string.Empty;
        private string _icon = string.Empty;
        private string _category = "General";
        private string _backgroundColor = string.Empty;
        private string _textColor = string.Empty;
        private string _borderColor = string.Empty;
        private double _width = 350;
        private double _height = 250;
        private string _contentAlignment = "Left";
        private string _imageAlignment = "Top";
        private double _imageHeight = 120;
        private bool _showBorder = true;
        private string _markdown = string.Empty;
        private string _htmlContent = string.Empty;

        public event PropertyChangedEventHandler PropertyChanged;

        public InfoCardViewModel()
        {
            OpenLinkCommand = new RelayCommand(ExecuteOpenLink, CanExecuteOpenLink);
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
        /// Main text content to display
        /// </summary>
        public string Content
        {
            get { return _content; }
            set { _content = value; OnPropertyChanged(); OnPropertyChanged("HasContent"); }
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
        public bool HasMarkdown { get { return !string.IsNullOrWhiteSpace(Markdown); } }
        public bool HasHtml { get { return !string.IsNullOrWhiteSpace(HtmlContent); } }
        public bool HasLink { get { return !string.IsNullOrWhiteSpace(LinkUrl); } }

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
    }
}

// Copyright (c) 2025 A Solution IT LLC. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Windows;
using System.Windows.Input;
using System.Windows.Threading;
using Launcher.Services;

namespace Launcher.ViewModels
{
    public class BannerViewModel : System.ComponentModel.INotifyPropertyChanged
    {
        private DispatcherTimer _carouselTimer;
        private int _currentSlideIndex = 0;
        private bool _isPaused = false;
        
        // ═══════════════════════════════════════════════════════════════════════════════
        // Core Properties
        // ═══════════════════════════════════════════════════════════════════════════════
        public string Title { get; set; } = string.Empty;
        public string Subtitle { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Category { get; set; } = "General";

        public BannerViewModel()
        {
            NextSlideCommand = new RelayCommand(_ => NextSlide());
            PreviousSlideCommand = new RelayCommand(_ => PreviousSlide());
            OpenSlideLinkCommand = new RelayCommand(param => OpenSlideLink(param?.ToString()));
        }

        /// <summary>
        /// Opens a URL in the default browser
        /// </summary>
        private void OpenSlideLink(string url)
        {
            if (string.IsNullOrEmpty(url))
            {
                LoggingService.Info("OpenSlideLink called with empty URL", component: "BannerViewModel");
                return;
            }
            
            LoggingService.Info($"Opening URL: {url}", component: "BannerViewModel");
            try
            {
                System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                {
                    FileName = url,
                    UseShellExecute = true
                });
                LoggingService.Info($"Successfully opened URL: {url}", component: "BannerViewModel");
            }
            catch (Exception ex)
            {
                LoggingService.Error($"Failed to open URL '{url}': {ex.Message}", component: "BannerViewModel");
            }
        }

        // ═══════════════════════════════════════════════════════════════════════════════
        // Layout & Sizing Options
        // ═══════════════════════════════════════════════════════════════════════════════
        public double Height { get; set; } = 180;
        public double Width { get; set; } = 700;
        public double MinHeight { get; set; } = 120;
        public double MaxHeight { get; set; } = 400;
        public string Layout { get; set; } = "Left";  // Left, Center, Right
        public string ContentAlignment { get; set; } = "Left";  // Left, Center, Right
        public string VerticalAlignment { get; set; } = "Center";  // Top, Center, Bottom
        public string Padding { get; set; } = "32,24";  // Horizontal,Vertical
        public int CornerRadius { get; set; } = 12;
        public bool FullWidth { get; set; } = false;  // Stretch to full width

        // ═══════════════════════════════════════════════════════════════════════════════
        // Typography Enhancements
        // ═══════════════════════════════════════════════════════════════════════════════
        public string TitleFontSize { get; set; } = "32";
        public string SubtitleFontSize { get; set; } = "16";
        public string DescriptionFontSize { get; set; } = "14";
        public string TitleFontWeight { get; set; } = "Bold";  // Normal, Medium, SemiBold, Bold, ExtraBold
        public string SubtitleFontWeight { get; set; } = "Normal";
        public string DescriptionFontWeight { get; set; } = "Normal";
        public string FontFamily { get; set; } = "Segoe UI";
        public string TitleColor { get; set; } = "#FFFFFF";
        public string SubtitleColor { get; set; } = "#B0B0B0";
        public string DescriptionColor { get; set; } = "#909090";
        public bool TitleAllCaps { get; set; } = false;
        public double TitleLetterSpacing { get; set; } = 0;
        public double LineHeight { get; set; } = 1.4;

        // ═══════════════════════════════════════════════════════════════════════════════
        // Background & Visual Effects
        // ═══════════════════════════════════════════════════════════════════════════════
        public string BackgroundColor { get; set; } = "#2D2D30";
        public string BackgroundImagePath { get; set; } = string.Empty;
        public double BackgroundImageOpacity { get; set; } = 0.3;
        public string BackgroundImageStretch { get; set; } = "Uniform";  // Fill, Uniform, UniformToFill, None
        public string GradientStart { get; set; } = string.Empty;
        public string GradientEnd { get; set; } = string.Empty;
        public double GradientAngle { get; set; } = 90;  // 0-360 degrees
        public string BorderColor { get; set; } = "Transparent";
        public int BorderThickness { get; set; } = 0;
        public string ShadowIntensity { get; set; } = "Medium";  // None, Light, Medium, Heavy
        public double Opacity { get; set; } = 1.0;

        // ═══════════════════════════════════════════════════════════════════════════════
        // Icon & Image Options
        // ═══════════════════════════════════════════════════════════════════════════════
        public string IconGlyph { get; set; } = string.Empty;
        public string IconPath { get; set; } = string.Empty;
        public int IconSize { get; set; } = 64;
        public string IconPosition { get; set; } = "Right";  // Left, Right, Top, Bottom, Background
        public string IconColor { get; set; } = "#40FFFFFF";
        public string IconAnimation { get; set; } = "None";  // None, Pulse, Rotate, Bounce
        public string OverlayImagePath { get; set; } = string.Empty;
        public double OverlayImageOpacity { get; set; } = 0.5;
        public string OverlayPosition { get; set; } = "Right";  // Left, Right, Center
        public int OverlayImageSize { get; set; } = 120;

        // ═══════════════════════════════════════════════════════════════════════════════
        // Interactive Elements
        // ═══════════════════════════════════════════════════════════════════════════════
        public bool Clickable { get; set; } = false;
        public string ClickAction { get; set; } = string.Empty;  // ScriptBlock as string
        public string LinkUrl { get; set; } = string.Empty;
        public string HoverEffect { get; set; } = "None";  // None, Lift, Glow, Zoom, Darken
        public string ButtonText { get; set; } = string.Empty;
        public string ButtonIcon { get; set; } = string.Empty;
        public string ButtonColor { get; set; } = "#0078D4";
        public string ButtonTextColor { get; set; } = "#FFFFFF";
        public bool ShowCloseButton { get; set; } = false;
        public string Cursor { get; set; } = "Arrow";  // Arrow, Hand

        // ═══════════════════════════════════════════════════════════════════════════════
        // Badge/Label
        // ═══════════════════════════════════════════════════════════════════════════════
        public string BadgeText { get; set; } = string.Empty;
        public string BadgeColor { get; set; } = "#FF5722";
        public string BadgeTextColor { get; set; } = "#FFFFFF";
        public string BadgePosition { get; set; } = "TopRight";  // TopLeft, TopRight, BottomLeft, BottomRight

        // ═══════════════════════════════════════════════════════════════════════════════
        // Progress Indicator
        // ═══════════════════════════════════════════════════════════════════════════════
        public int ProgressValue { get; set; } = -1;  // -1 means no progress bar
        public string ProgressLabel { get; set; } = string.Empty;
        public string ProgressColor { get; set; } = "#0078D4";
        public string ProgressBackgroundColor { get; set; } = "#40FFFFFF";

        // ═══════════════════════════════════════════════════════════════════════════════
        // Responsive Design
        // ═══════════════════════════════════════════════════════════════════════════════
        public bool Responsive { get; set; } = true;
        public string SmallTitleFontSize { get; set; } = "24";  // Font size for narrow widths
        public string SmallSubtitleFontSize { get; set; } = "14";
        public double SmallHeight { get; set; } = 140;
        public int SmallIconSize { get; set; } = 48;
        public int ResponsiveBreakpoint { get; set; } = 500;  // Width threshold for small mode

        private double _actualWidth = 700;
        /// <summary>
        /// Tracks the actual rendered width for responsive calculations
        /// </summary>
        public double ActualWidth
        {
            get { return _actualWidth; }
            set
            {
                if (Math.Abs(_actualWidth - value) > 1)
                {
                    _actualWidth = value;
                    OnPropertyChanged(nameof(ActualWidth));
                    OnPropertyChanged(nameof(IsSmallMode));
                    OnPropertyChanged(nameof(ResponsiveTitleFontSize));
                    OnPropertyChanged(nameof(ResponsiveSubtitleFontSize));
                    OnPropertyChanged(nameof(ResponsiveHeight));
                    OnPropertyChanged(nameof(ResponsiveIconSize));
                }
            }
        }

        /// <summary>
        /// Returns true when banner width is below the responsive breakpoint
        /// </summary>
        public bool IsSmallMode
        {
            get { return Responsive && _actualWidth < ResponsiveBreakpoint; }
        }

        /// <summary>
        /// Returns the appropriate title font size based on current width
        /// </summary>
        public string ResponsiveTitleFontSize
        {
            get { return IsSmallMode ? SmallTitleFontSize : TitleFontSize; }
        }

        /// <summary>
        /// Returns the appropriate subtitle font size based on current width
        /// </summary>
        public string ResponsiveSubtitleFontSize
        {
            get { return IsSmallMode ? SmallSubtitleFontSize : SubtitleFontSize; }
        }

        /// <summary>
        /// Returns the appropriate height based on current width
        /// </summary>
        public double ResponsiveHeight
        {
            get { return IsSmallMode ? SmallHeight : Height; }
        }

        /// <summary>
        /// Returns the appropriate icon size based on current width
        /// </summary>
        public int ResponsiveIconSize
        {
            get { return IsSmallMode ? SmallIconSize : IconSize; }
        }

        // ═══════════════════════════════════════════════════════════════════════════════
        // Animation
        // ═══════════════════════════════════════════════════════════════════════════════
        public string EntranceAnimation { get; set; } = "None";  // None, FadeIn, SlideIn, ZoomIn
        public int AnimationDuration { get; set; } = 300;  // milliseconds

        // ═══════════════════════════════════════════════════════════════════════════════
        // Computed Properties for XAML Binding
        // ═══════════════════════════════════════════════════════════════════════════════
        public bool HasGradient 
        { 
            get { return !string.IsNullOrEmpty(GradientStart) && !string.IsNullOrEmpty(GradientEnd); }
        }
        
        public bool HasIcon 
        { 
            get { return !string.IsNullOrEmpty(IconGlyph) || !string.IsNullOrEmpty(IconPath); }
        }
        
        public bool HasBadge 
        { 
            get { return !string.IsNullOrEmpty(BadgeText); }
        }
        
        public bool HasButton 
        { 
            get { return !string.IsNullOrEmpty(ButtonText); }
        }
        
        public bool HasProgress 
        { 
            get { return ProgressValue >= 0; }
        }
        
        public bool HasDescription 
        { 
            get { return !string.IsNullOrEmpty(Description); }
        }
        
        public bool HasOverlayImage
        {
            get { return !string.IsNullOrEmpty(OverlayImagePath); }
        }

        public bool IsOverlayLeft
        {
            get { return OverlayPosition == "Left"; }
        }

        public bool IsOverlayRight
        {
            get { return OverlayPosition == "Right" || string.IsNullOrEmpty(OverlayPosition); }
        }

        public bool IsOverlayCenter
        {
            get { return OverlayPosition == "Center"; }
        }

        public bool IsIconLeft 
        { 
            get { return IconPosition == "Left"; }
        }
        
        public bool IsIconRight 
        { 
            get { return IconPosition == "Right"; }
        }
        
        public bool IsIconTop 
        { 
            get { return IconPosition == "Top"; }
        }
        
        public bool IsIconBottom 
        { 
            get { return IconPosition == "Bottom"; }
        }
        
        public bool IsIconBackground 
        { 
            get { return IconPosition == "Background"; }
        }
        
        public string DisplayTitle 
        { 
            get { return TitleAllCaps ? (Title?.ToUpperInvariant() ?? string.Empty) : Title; }
        }
        
        public bool ShowShadow 
        { 
            get { return ShadowIntensity != "None"; }
        }
        
        // Shadow properties based on intensity (using traditional switch instead of expression)
        public double ShadowDepth 
        { 
            get 
            { 
                switch (ShadowIntensity)
                {
                    case "Light": return 2;
                    case "Medium": return 3;
                    case "Heavy": return 5;
                    default: return 0;
                }
            }
        }
        
        public double ShadowBlurRadius 
        { 
            get 
            { 
                switch (ShadowIntensity)
                {
                    case "Light": return 8;
                    case "Medium": return 12;
                    case "Heavy": return 20;
                    default: return 0;
                }
            }
        }
        
        public double ShadowOpacity 
        { 
            get 
            { 
                switch (ShadowIntensity)
                {
                    case "Light": return 0.15;
                    case "Medium": return 0.2;
                    case "Heavy": return 0.35;
                    default: return 0;
                }
            }
        }

        // Cursor for interactive banners
        public Cursor ActualCursor 
        { 
            get { return (Clickable || !string.IsNullOrEmpty(LinkUrl)) ? Cursors.Hand : Cursors.Arrow; }
        }

        // ═══════════════════════════════════════════════════════════════════════════════
        // Carousel Properties
        // ═══════════════════════════════════════════════════════════════════════════════
        public List<BannerSlide> CarouselItems { get; set; } = new List<BannerSlide>();
        
        public int CurrentSlideIndex 
        { 
            get { return _currentSlideIndex; }
            set 
            {
                if (_currentSlideIndex != value)
                {
                    _currentSlideIndex = value;
                    OnPropertyChanged(nameof(CurrentSlideIndex));
                    OnPropertyChanged(nameof(CurrentSlide));
                }
            }
        }
        
        public bool AutoRotate { get; set; } = false;
        public int RotateInterval { get; set; } = 3000;
        public string NavigationStyle { get; set; } = "Dots";
        public bool IsCarousel 
        { 
            get { return CarouselItems != null && CarouselItems.Count > 1; }
        }
        
        public BannerSlide CurrentSlide 
        { 
            get 
            { 
                return CarouselItems != null && CarouselItems.Count > 0 && CurrentSlideIndex < CarouselItems.Count 
                    ? CarouselItems[CurrentSlideIndex] 
                    : null; 
            }
        }

        /// <summary>
        /// Starts the carousel auto-rotation timer
        /// </summary>
        public void StartCarouselTimer()
        {
            if (!AutoRotate || !IsCarousel) return;
            
            if (_carouselTimer != null)
            {
                _carouselTimer.Stop();
            }
            
            _carouselTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromMilliseconds(RotateInterval)
            };
            _carouselTimer.Tick += (s, e) =>
            {
                if (!_isPaused && IsCarousel)
                {
                    CurrentSlideIndex = (CurrentSlideIndex + 1) % CarouselItems.Count;
                }
            };
            _carouselTimer.Start();
        }

        /// <summary>
        /// Pauses/resumes the carousel (for hover pause)
        /// </summary>
        public void PauseCarousel(bool pause)
        {
            _isPaused = pause;
        }

        /// <summary>
        /// Navigates to a specific slide
        /// </summary>
        public void GoToSlide(int index)
        {
            if (CarouselItems != null && index >= 0 && index < CarouselItems.Count)
            {
                CurrentSlideIndex = index;
            }
        }

        /// <summary>
        /// Navigates to next slide
        /// </summary>
        public void NextSlide()
        {
            if (IsCarousel)
            {
                CurrentSlideIndex = (CurrentSlideIndex + 1) % CarouselItems.Count;
            }
        }

        /// <summary>
        /// Navigates to previous slide
        /// </summary>
        public void PreviousSlide()
        {
            if (IsCarousel)
            {
                CurrentSlideIndex = (CurrentSlideIndex - 1 + CarouselItems.Count) % CarouselItems.Count;
            }
        }

        // ═══════════════════════════════════════════════════════════════════════════════
        // Commands for Interactive Elements
        // ═══════════════════════════════════════════════════════════════════════════════
        public ICommand BannerClickCommand { get; set; }
        public ICommand ButtonClickCommand { get; set; }
        public ICommand CloseCommand { get; set; }
        public ICommand NextSlideCommand { get; set; }
        public ICommand PreviousSlideCommand { get; set; }
        public ICommand OpenSlideLinkCommand { get; set; }

        // ═══════════════════════════════════════════════════════════════════════════════
        // INotifyPropertyChanged Implementation
        // ═══════════════════════════════════════════════════════════════════════════════
        public event System.ComponentModel.PropertyChangedEventHandler PropertyChanged;
        
        protected virtual void OnPropertyChanged(string propertyName)
        {
            var handler = PropertyChanged;
            if (handler != null)
            {
                handler(this, new System.ComponentModel.PropertyChangedEventArgs(propertyName));
            }
        }
    }

    /// <summary>
    /// Represents a single slide in a banner carousel
    /// </summary>
    [DataContract]
    public class BannerSlide
    {
        [DataMember]
        public string Title { get; set; } = string.Empty;
        [DataMember]
        public string Subtitle { get; set; } = string.Empty;
        [DataMember]
        public string Icon { get; set; } = string.Empty;
        [DataMember]
        public string BackgroundColor { get; set; } = "#2D2D30";
        [DataMember]
        public string BackgroundImagePath { get; set; } = string.Empty;
        [DataMember]
        public double BackgroundImageOpacity { get; set; } = 0.3;
        [DataMember]
        public string BackgroundImageStretch { get; set; } = "Uniform";
        [DataMember]
        public string IconColor { get; set; } = "#40FFFFFF";
        [DataMember]
        public string IconPosition { get; set; } = "Right";
        [DataMember]
        public int IconSize { get; set; } = 64;
        [DataMember]
        public string TitleFontSize { get; set; } = "32";
        [DataMember]
        public string SubtitleFontSize { get; set; } = "16";
        [DataMember]
        public string TitleFontWeight { get; set; } = "Bold";
        [DataMember]
        public int Height { get; set; } = 180;
        [DataMember]
        public int CornerRadius { get; set; } = 12;
        [DataMember]
        public string LinkUrl { get; set; } = string.Empty;
        [DataMember]
        public bool Clickable { get; set; } = false;

        /// <summary>
        /// Returns true if this slide has a clickable link
        /// </summary>
        public bool HasLink
        {
            get { return Clickable && !string.IsNullOrEmpty(LinkUrl); }
        }

        /// <summary>
        /// Converts the Icon string (e.g., "&#xE8B1;") to actual Unicode character for display
        /// </summary>
        public string IconGlyph
        {
            get
            {
                if (string.IsNullOrEmpty(Icon))
                    return string.Empty;

                // Convert &#xE8B1; format to actual unicode character
                if (Icon.StartsWith("&#x") && Icon.EndsWith(";"))
                {
                    var hex = Icon.Substring(3, Icon.Length - 4);
                    int codePoint;
                    if (int.TryParse(hex, System.Globalization.NumberStyles.HexNumber, null, out codePoint))
                    {
                        return char.ConvertFromUtf32(codePoint);
                    }
                }

                // Return as-is (might be emoji or already converted)
                return Icon;
            }
        }
    }
}

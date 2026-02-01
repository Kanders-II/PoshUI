// Copyright (c) 2025 A Solution IT LLC. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;

namespace Launcher.ViewModels
{
    public class CardViewModel : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string name = null) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));

        public string Title { get; set; }
        public string Content { get; set; }
        
        // Image support
        public string ImagePath { get; set; }
        public double ImageHeight { get; set; } = 120;
        public double ImageOpacity { get; set; } = 1.0;
        
        // Styling
        public string BackgroundColor { get; set; } = "#2D2D30";
        public string GradientStart { get; set; }
        public string GradientEnd { get; set; }
        public string TitleColor { get; set; } = "#FFFFFF";
        public string ContentColor { get; set; } = "#B0B0B0";
        public int CornerRadius { get; set; } = 8;
        
        // Link/Clickable support
        public string LinkUrl { get; set; }
        public string LinkText { get; set; }
        public bool IsClickable => !string.IsNullOrEmpty(LinkUrl);
        
        // Icon support
        public string IconPath { get; set; }
        public string IconGlyph { get; set; }
        public int IconSize { get; set; } = 32;
        
        // Carousel support - slides within the card
        public ObservableCollection<CarouselSlide> CarouselSlides { get; set; }
        public bool HasCarousel => CarouselSlides != null && CarouselSlides.Count > 0;
        
        private int _currentSlideIndex;
        public int CurrentSlideIndex
        {
            get => _currentSlideIndex;
            set { _currentSlideIndex = value; OnPropertyChanged(); OnPropertyChanged(nameof(CurrentSlide)); }
        }
        
        public CarouselSlide CurrentSlide => HasCarousel && CurrentSlideIndex < CarouselSlides.Count 
            ? CarouselSlides[CurrentSlideIndex] : null;
        
        // Commands for carousel navigation
        public ICommand NextSlideCommand { get; set; }
        public ICommand PreviousSlideCommand { get; set; }
        public ICommand OpenLinkCommand { get; set; }
    }
    
    public class CarouselSlide
    {
        public string Title { get; set; }
        public string Content { get; set; }
        public string ImagePath { get; set; }
        public string LinkUrl { get; set; }
    }
}
// Copyright (c) 2025 A Solution IT LLC. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
using System.ComponentModel;
using System.Windows.Input;

namespace Launcher.ViewModels
{
    /// <summary>
    /// Represents a single step item in the wizard navigation
    /// </summary>
    public class StepItem : INotifyPropertyChanged
    {
        public int StepNumber { get; set; }
        public string Title { get; set; }
        
        /// <summary>
        /// Command to navigate to this step (used in dashboard mode for sidebar icon clicks)
        /// </summary>
        public ICommand NavigateCommand { get; set; }
        
        private bool _isCompleted;
        public bool IsCompleted
        {
            get => _isCompleted;
            set
            {
                if (_isCompleted == value) return;
                _isCompleted = value;
                OnPropertyChanged(nameof(IsCompleted));
            }
        }
        
        private bool _isCurrent;
        public bool IsCurrent
        {
            get => _isCurrent;
            set
            {
                if (_isCurrent == value) return;
                _isCurrent = value;
                OnPropertyChanged(nameof(IsCurrent));
            }
        }
        
        private bool _isValid;
        public bool IsValid
        {
            get => _isValid;
            set
            {
                if (_isValid == value) return;
                _isValid = value;
                OnPropertyChanged(nameof(IsValid));
            }
        }
        
        public string IconGlyph { get; set; }  // Fluent icon glyph for this step
        
        /// <summary>
        /// Returns true if IconGlyph is set (for XAML binding)
        /// </summary>
        public bool HasIconGlyph => !string.IsNullOrEmpty(IconGlyph);
        
        public bool ShowConnector { get; set; }
        public string Tag { get; set; }

        public event PropertyChangedEventHandler PropertyChanged;
        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}

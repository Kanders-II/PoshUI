// Copyright (c) 2025 A Solution IT LLC. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;
using System.ComponentModel;

namespace Launcher.Controls
{
    public partial class StepperControl : UserControl
    {
        public static readonly DependencyProperty CurrentStepProperty =
            DependencyProperty.Register("CurrentStep", typeof(int), typeof(StepperControl),
                new PropertyMetadata(1, OnCurrentStepChanged));

        public static readonly DependencyProperty StepsProperty =
            DependencyProperty.Register("Steps", typeof(ObservableCollection<StepItem>), typeof(StepperControl),
                new PropertyMetadata(new ObservableCollection<StepItem>()));

        public int CurrentStep
        {
            get { return (int)GetValue(CurrentStepProperty); }
            set { SetValue(CurrentStepProperty, value); }
        }

        public ObservableCollection<StepItem> Steps
        {
            get { return (ObservableCollection<StepItem>)GetValue(StepsProperty); }
            set { SetValue(StepsProperty, value); }
        }

        public StepperControl()
        {
            InitializeComponent();
        }

        private static void OnCurrentStepChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var control = (StepperControl)d;
            var newStep = (int)e.NewValue;

            foreach (var step in control.Steps)
            {
                step.IsCompleted = step.StepNumber < newStep;
                step.IsCurrent = step.StepNumber == newStep;
            }
        }
    }

    public class StepItem : INotifyPropertyChanged
    {
        public int StepNumber { get; set; }
        public string Title { get; set; }
        public bool IsCompleted { get; set; }
        public bool ShowConnector { get; set; }
        public string Tag { get; set; }

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

        public event PropertyChangedEventHandler PropertyChanged;
        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
} 
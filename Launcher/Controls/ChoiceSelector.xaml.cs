// Copyright (c) 2025 A Solution IT LLC. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using Launcher.Services;

namespace Launcher.Controls
{
    public partial class ChoiceSelector : UserControl
    {
        public static readonly DependencyProperty ItemsSourceProperty = 
            DependencyProperty.Register("ItemsSource", typeof(IEnumerable<string>), typeof(ChoiceSelector), 
                new PropertyMetadata(null, OnItemsSourceChanged));

        public static readonly DependencyProperty SelectedItemProperty = 
            DependencyProperty.Register("SelectedItem", typeof(string), typeof(ChoiceSelector), 
                new PropertyMetadata(null, OnSelectedItemChanged));

        public IEnumerable<string> ItemsSource
        {
            get { return (IEnumerable<string>)GetValue(ItemsSourceProperty); }
            set { SetValue(ItemsSourceProperty, value); }
        }

        public string SelectedItem
        {
            get { return (string)GetValue(SelectedItemProperty); }
            set { SetValue(SelectedItemProperty, value); }
        }

        public ChoiceSelector()
        {
            InitializeComponent();
            LoggingService.Info("ChoiceSelector control initialized");
        }

        private static void OnItemsSourceChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var control = (ChoiceSelector)d;
            var newItems = e.NewValue as IEnumerable<string>;
            
            LoggingService.Info($"ItemsSource changed: {(newItems != null ? string.Join(", ", newItems) : "null")}");
            
            control.UpdateComboBoxItems();
        }

        private static void OnSelectedItemChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var control = (ChoiceSelector)d;
            var newValue = e.NewValue as string;
            
            LoggingService.Info($"SelectedItem changed to: {newValue ?? "null"}");
            
            if (control.ChoicesComboBox.SelectedItem?.ToString() != newValue)
            {
                control.ChoicesComboBox.SelectedItem = newValue;
            }
        }

        private void UpdateComboBoxItems()
        {
            ChoicesComboBox.Items.Clear();
            
            if (ItemsSource != null)
            {
                foreach (var item in ItemsSource)
                {
                    ChoicesComboBox.Items.Add(item);
                    LoggingService.Info($"Added item to ComboBox: {item}");
                }
                
                // Select the first item if nothing is selected
                if (ChoicesComboBox.SelectedItem == null && ChoicesComboBox.Items.Count > 0)
                {
                    ChoicesComboBox.SelectedIndex = 0;
                }
            }
        }

        private void ChoicesComboBox_Loaded(object sender, RoutedEventArgs e)
        {
            UpdateComboBoxItems();
        }

        private void ChoicesComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (ChoicesComboBox.SelectedItem != null)
            {
                SelectedItem = ChoicesComboBox.SelectedItem.ToString();
                LoggingService.Info($"ComboBox selection changed to: {SelectedItem}");
            }
        }
    }
} 
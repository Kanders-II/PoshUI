// Copyright (c) 2025 A Solution IT LLC. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
using System;
using System.Collections;
using System.Collections.Specialized;
using System.Windows;
using System.Windows.Controls;
using Launcher.Services;

namespace Launcher.Helpers
{
    /// <summary>
    /// Attached property helper for binding to ListBox SelectedItems (multi-select)
    /// </summary>
    public static class ListBoxHelper
    {
        public static readonly DependencyProperty SelectedItemsProperty =
            DependencyProperty.RegisterAttached(
                "SelectedItems",
                typeof(IList),
                typeof(ListBoxHelper),
                new FrameworkPropertyMetadata(null, OnSelectedItemsChanged));

        public static IList GetSelectedItems(DependencyObject obj)
        {
            return (IList)obj.GetValue(SelectedItemsProperty);
        }

        public static void SetSelectedItems(DependencyObject obj, IList value)
        {
            obj.SetValue(SelectedItemsProperty, value);
        }

        private static void OnSelectedItemsChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is ListBox listBox)
            {
                listBox.SelectionChanged -= ListBox_SelectionChanged;

                if (e.NewValue is IList selectedItems)
                {
                    // Sync the ListBox selection with the bound collection
                    listBox.SelectedItems.Clear();
                    foreach (var item in selectedItems)
                    {
                        if (listBox.Items.Contains(item))
                        {
                            listBox.SelectedItems.Add(item);
                        }
                    }

                    // Subscribe to collection changes
                    if (selectedItems is INotifyCollectionChanged notifyCollection)
                    {
                        notifyCollection.CollectionChanged -= SelectedItems_CollectionChanged;
                        notifyCollection.CollectionChanged += SelectedItems_CollectionChanged;
                    }
                }

                listBox.SelectionChanged += ListBox_SelectionChanged;
            }
        }

        private static void ListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var listBox = (ListBox)sender;
            var selectedItems = GetSelectedItems(listBox);

            if (selectedItems != null)
            {
                // Update the bound collection with current ListBox selection
                selectedItems.Clear();
                foreach (var item in listBox.SelectedItems)
                {
                    selectedItems.Add(item);
                }

                LoggingService.Trace($"ListBox selection changed: {listBox.SelectedItems.Count} items selected", component: "ListBoxHelper");
            }
        }

        private static void SelectedItems_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            // Handle external changes to the collection if needed
            LoggingService.Trace("Bound SelectedItems collection changed externally", component: "ListBoxHelper");
        }
    }
}

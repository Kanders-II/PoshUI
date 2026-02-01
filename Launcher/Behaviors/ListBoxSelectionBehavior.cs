// Copyright (c) 2025 A Solution IT LLC. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
using System;
using System.Collections;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;

namespace Launcher.Behaviors
{
    /// <summary>
    /// Attached behavior to enable two-way binding for ListBox.SelectedItems
    /// </summary>
    public static class ListBoxSelectionBehavior
    {
        private static bool _isUpdating = false;
        
        // Map collections to their associated ListBoxes (weak references to avoid memory leaks)
        private static readonly ConditionalWeakTable<ObservableCollection<string>, ListBox> _collectionToListBox 
            = new ConditionalWeakTable<ObservableCollection<string>, ListBox>();

        public static readonly DependencyProperty SelectedItemsProperty =
            DependencyProperty.RegisterAttached(
                "SelectedItems",
                typeof(ObservableCollection<string>),
                typeof(ListBoxSelectionBehavior),
                new PropertyMetadata(null, OnSelectedItemsChanged));

        public static ObservableCollection<string> GetSelectedItems(DependencyObject obj)
        {
            return (ObservableCollection<string>)obj.GetValue(SelectedItemsProperty);
        }

        public static void SetSelectedItems(DependencyObject obj, ObservableCollection<string> value)
        {
            obj.SetValue(SelectedItemsProperty, value);
        }

        private static void OnSelectedItemsChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is ListBox listBox)
            {
                // Detach old handlers
                listBox.SelectionChanged -= ListBox_SelectionChanged;
                listBox.Loaded -= ListBox_Loaded;

                if (e.OldValue is ObservableCollection<string> oldCollection)
                {
                    oldCollection.CollectionChanged -= Collection_CollectionChanged;
                }

                // Attach new handlers (or re-attach if it's the same collection but a new ListBox)
                if (e.NewValue is ObservableCollection<string> newCollection)
                {
                    listBox.SelectionChanged += ListBox_SelectionChanged;
                    newCollection.CollectionChanged += Collection_CollectionChanged;
                    
                    // Add Loaded handler to force re-sync when ListBox is loaded
                    // This handles cases where WPF doesn't fire OnSelectedItemsChanged due to same collection instance
                    listBox.Loaded += ListBox_Loaded;
                    
                    // Always update the mapping, even if it's the same collection
                    // Because the ListBox is a NEW instance (recreated from DataTemplate when page is cached/restored)
                    try
                    {
                        _collectionToListBox.Remove(newCollection);
                    }
                    catch { }
                    
                    _collectionToListBox.Add(newCollection, listBox);

                    // Initialize ListBox selection from ViewModel
                    SyncListBoxFromViewModel(listBox, newCollection);
                }
            }
        }
        
        private static void ListBox_Loaded(object sender, RoutedEventArgs e)
        {
            if (sender is ListBox listBox)
            {
                var collection = GetSelectedItems(listBox);
                if (collection != null)
                {
                    // Update the mapping in case it was lost
                    try
                    {
                        _collectionToListBox.Remove(collection);
                    }
                    catch { }
                    _collectionToListBox.Add(collection, listBox);
                    
                    // Force sync
                    SyncListBoxFromViewModel(listBox, collection);
                }
            }
        }

        private static void ListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (_isUpdating) return;

            if (sender is ListBox listBox)
            {
                // CRITICAL FIX: Don't sync if all items are being removed during page navigation
                // When navigating away, WPF clears all selections which would wipe out our saved state
                if (e.RemovedItems.Count > 0 && e.AddedItems.Count == 0 && listBox.SelectedItems.Count == 0)
                {
                    return;
                }
                
                var collection = GetSelectedItems(listBox);
                if (collection != null)
                {
                    _isUpdating = true;
                    try
                    {
                        // Sync ViewModel collection from ListBox selection
                        collection.Clear();
                        foreach (var item in listBox.SelectedItems)
                        {
                            if (item is string strItem)
                            {
                                collection.Add(strItem);
                            }
                        }
                    }
                    finally
                    {
                        _isUpdating = false;
                    }
                }
            }
        }

        private static void Collection_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            // This handler is triggered when ViewModel's collection changes programmatically
            // (e.g., when restoring cached page data or when ViewModel updates the collection)
            if (sender is ObservableCollection<string> collection)
            {
                // Find the associated ListBox and sync it
                if (_collectionToListBox.TryGetValue(collection, out ListBox listBox))
                {
                    // Sync immediately on UI thread (we're already on UI thread when collection changes)
                    if (listBox.Dispatcher.CheckAccess())
                    {
                        SyncListBoxFromViewModel(listBox, collection);
                    }
                    else
                    {
                        listBox.Dispatcher.Invoke(new System.Action(() =>
                        {
                            SyncListBoxFromViewModel(listBox, collection);
                        }));
                    }
                }
            }
        }

        private static void SyncListBoxFromViewModel(ListBox listBox, ObservableCollection<string> collection)
        {
            if (_isUpdating) return;

            _isUpdating = true;
            try
            {
                listBox.SelectedItems.Clear();
                foreach (var item in collection)
                {
                    if (listBox.Items.Contains(item))
                    {
                        listBox.SelectedItems.Add(item);
                    }
                }
            }
            finally
            {
                _isUpdating = false;
            }
        }
    }
}

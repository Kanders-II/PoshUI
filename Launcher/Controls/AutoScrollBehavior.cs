// Copyright (c) 2025 A Solution IT LLC. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
using System;
using System.Collections;
using System.Collections.Specialized;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;

namespace Launcher.Controls
{
    public static class AutoScrollBehavior
    {
        private static readonly ConditionalWeakTable<INotifyCollectionChanged, ListBox> CollectionToListBox = new ConditionalWeakTable<INotifyCollectionChanged, ListBox>();

        public static readonly DependencyProperty EnableAutoScrollProperty =
            DependencyProperty.RegisterAttached(
                "EnableAutoScroll",
                typeof(bool),
                typeof(AutoScrollBehavior),
                new PropertyMetadata(false, OnEnableAutoScrollChanged));

        public static void SetEnableAutoScroll(DependencyObject element, bool value) => element.SetValue(EnableAutoScrollProperty, value);
        public static bool GetEnableAutoScroll(DependencyObject element) => (bool)element.GetValue(EnableAutoScrollProperty);

        private static void OnEnableAutoScrollChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var listBox = d as ListBox;
            if (listBox == null) return;

            if ((bool)e.NewValue)
            {
                listBox.Loaded += ListBox_Loaded;
                listBox.Unloaded += ListBox_Unloaded;
                HookCollectionChanged(listBox);
            }
            else
            {
                listBox.Loaded -= ListBox_Loaded;
                listBox.Unloaded -= ListBox_Unloaded;
                UnhookCollectionChanged(listBox);
            }
        }

        private static void ListBox_Loaded(object sender, RoutedEventArgs e)
        {
            var lb = sender as ListBox;
            HookCollectionChanged(lb);
            ScrollToEnd(lb);
        }

        private static void ListBox_Unloaded(object sender, RoutedEventArgs e)
        {
            var lb = sender as ListBox;
            UnhookCollectionChanged(lb);
        }

        private static void HookCollectionChanged(ListBox lb)
        {
            var items = lb?.ItemsSource as INotifyCollectionChanged;
            if (items != null)
            {
                items.CollectionChanged -= Items_CollectionChanged;
                items.CollectionChanged += Items_CollectionChanged;
                try { CollectionToListBox.Remove(items); } catch { }
                CollectionToListBox.Add(items, lb);
            }
        }

        private static void UnhookCollectionChanged(ListBox lb)
        {
            var items = lb?.ItemsSource as INotifyCollectionChanged;
            if (items != null)
            {
                items.CollectionChanged -= Items_CollectionChanged;
                try { CollectionToListBox.Remove(items); } catch { }
            }
        }

        private static void Items_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            if (e.Action == NotifyCollectionChangedAction.Add)
            {
                if (sender is INotifyCollectionChanged coll && CollectionToListBox.TryGetValue(coll, out var lb))
                {
                    lb.Dispatcher.BeginInvoke(new Action(() => ScrollToEnd(lb)));
                }
            }
        }

        private static void ScrollToEnd(ListBox lb)
        {
            if (lb == null || lb.Items.Count == 0) return;
            var last = lb.Items[lb.Items.Count - 1];
            lb.ScrollIntoView(last);
        }
    }
}

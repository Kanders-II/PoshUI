// Copyright (c) 2025 A Solution IT LLC. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
using System;
using System.Windows;
using System.Windows.Controls;
using System.IO;
using Launcher.ViewModels;
using System.ComponentModel; // For CancelEventArgs
using Launcher.Services; // Theme change notifications
using System.Runtime.InteropServices;
using System.Windows.Interop;

namespace Launcher
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        // P/Invoke for Windows 11 rounded corners
        [DllImport("dwmapi.dll")]
        private static extern int DwmSetWindowAttribute(IntPtr hwnd, int attr, ref int attrValue, int attrSize);

        private const int DWMWA_WINDOW_CORNER_PREFERENCE = 33;
        private const int DWMWCP_ROUND = 2;

        public MainWindow()
        {
            InitializeComponent();
            this.SourceInitialized += MainWindow_SourceInitialized;
            this.Loaded += MainWindow_Loaded;
            this.Closing += MainWindow_Closing;
        }

        private void MainWindow_SourceInitialized(object sender, EventArgs e)
        {
            // Apply Windows 11 native rounded corners
            try
            {
                var hwnd = new WindowInteropHelper(this).Handle;
                int preference = DWMWCP_ROUND;
                DwmSetWindowAttribute(hwnd, DWMWA_WINDOW_CORNER_PREFERENCE, ref preference, sizeof(int));
            }
            catch
            {
                // Silently fail on Windows 10 or if API is unavailable
            }
        }

        private void NextButton_Click(object sender, RoutedEventArgs e)
        {
            var viewModel = DataContext as MainWindowViewModel;
            if (viewModel?.NextCommand?.CanExecute(null) ?? false)
            {
                viewModel.NextCommand.Execute(null);
            }
        }

        private void PrevButton_Click(object sender, RoutedEventArgs e)
        {
            var viewModel = DataContext as MainWindowViewModel;
            if (viewModel?.PreviousCommand?.CanExecute(null) ?? false)
            {
                viewModel.PreviousCommand.Execute(null);
            }
        }

        // Handler for navigating hyperlinks
        private void Hyperlink_RequestNavigate(object sender, System.Windows.Navigation.RequestNavigateEventArgs e)
        {
            try
            {
                System.Diagnostics.Process.Start(e.Uri.AbsoluteUri);
                e.Handled = true;
            }
            catch (Exception ex)
            {
                // Log or handle the error appropriately (e.g., show a message box)
                MessageBox.Show($"Could not open link: {ex.Message}", "Navigation Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            // Initialize
            
            // Subscribe to theme change events from ViewModel
            if (DataContext is MainWindowViewModel viewModel)
            {
                viewModel.ThemeChanged += ViewModel_ThemeChanged;
                
                // Subscribe to PropertyChanged to control sidebar visibility directly
                viewModel.PropertyChanged += ViewModel_PropertyChanged;
                
                // Initial sync of sidebar visibility (in case property was set before subscription)
                if (viewModel.IsSidebarHidden)
                {
                    SidebarColumn.Width = new GridLength(0);
                    SidebarBorder.Width = 0;
                    SidebarBorder.Visibility = Visibility.Collapsed;
                }
                LoggingService.Info($"[SIDEBAR] Initial sync: IsSidebarHidden={viewModel.IsSidebarHidden}, Column={SidebarColumn.Width}, Width={SidebarBorder.Width}, Visibility={SidebarBorder.Visibility}", component: "MainWindow");
            }
            
            // Sync theme toggle button state on window load
            SyncThemeToggleState();
        }
        
        private void ViewModel_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(MainWindowViewModel.IsSidebarHidden))
            {
                if (DataContext is MainWindowViewModel viewModel)
                {
                    // Directly control sidebar AND column width from code-behind
                    if (viewModel.IsSidebarHidden)
                    {
                        LoggingService.Info($"[SIDEBAR] Hiding sidebar: setting Column and Border Width=0, Visibility=Collapsed", component: "MainWindow");
                        SidebarColumn.Width = new GridLength(0);
                        SidebarBorder.Width = 0;
                        SidebarBorder.Visibility = Visibility.Collapsed;
                    }
                    else
                    {
                        LoggingService.Info($"[SIDEBAR] Showing sidebar: restoring Column=Auto, clearing Border Width, Visibility=Visible", component: "MainWindow");
                        SidebarColumn.Width = GridLength.Auto;
                        SidebarBorder.ClearValue(FrameworkElement.WidthProperty);
                        SidebarBorder.Visibility = Visibility.Visible;
                    }
                    LoggingService.Info($"[SIDEBAR] After: Column={SidebarColumn.Width}, Border Width={SidebarBorder.Width}, Visibility={SidebarBorder.Visibility}", component: "MainWindow");
                }
            }
        }

        private void ListBox_Loaded(object sender, RoutedEventArgs e)
        {
            if (sender is ListBox listBox)
            {
                // Check if the behavior is applied
                var selectedItems = Behaviors.ListBoxSelectionBehavior.GetSelectedItems(listBox);
                if (selectedItems != null)
                {
                    LoggingService.Debug($"ListBox '{listBox.Name}' loaded with {selectedItems.Count} selected items", component: "MainWindow");
                }
            }
        }

        // Title bar drag to move window
        private void TitleBar_MouseLeftButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (e.ClickCount == 2)
            {
                // Double-click to maximize/restore
                MaximizeButton_Click(sender, e);
            }
            else if (e.LeftButton == System.Windows.Input.MouseButtonState.Pressed)
            {
                this.DragMove();
            }
        }

        // Minimize button
        private void MinimizeButton_Click(object sender, RoutedEventArgs e)
        {
            this.WindowState = WindowState.Minimized;
        }

        // Maximize/Restore button
        private void MaximizeButton_Click(object sender, RoutedEventArgs e)
        {
            if (this.WindowState == WindowState.Maximized)
            {
                this.WindowState = WindowState.Normal;
                if (MaximizeIcon != null)
                {
                    MaximizeIcon.Text = "\uE922";  // Maximize icon
                }
            }
            else
            {
                this.WindowState = WindowState.Maximized;
                if (MaximizeIcon != null)
                {
                    MaximizeIcon.Text = "\uE923";  // Restore icon
                }
            }
        }

        // Close button
        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private bool IsDarkThemeActive()
        {
            foreach (var dict in Application.Current.Resources.MergedDictionaries)
            {
                var src = dict.Source;
                if (src != null && src.OriginalString != null && src.OriginalString.EndsWith("/Assets/DarkTheme.xaml", StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }
            return false;
        }

        private void ApplyTheme(string themePath)
        {
            // Remove existing theme dictionaries (light or dark) and add the requested one
            var merged = Application.Current.Resources.MergedDictionaries;
            for (int i = merged.Count - 1; i >= 0; i--)
            {
                var md = merged[i];
                var src = md.Source;
                if (src != null)
                {
                    var s = src.OriginalString ?? string.Empty;
                    if (s.EndsWith("/Assets/Fluent.xaml", StringComparison.OrdinalIgnoreCase) ||
                        s.EndsWith("/Assets/DarkTheme.xaml", StringComparison.OrdinalIgnoreCase))
                    {
                        merged.RemoveAt(i);
                    }
                }
            }

            // Build absolute pack URI to ensure reliable resolution
            var packUri = new Uri($"pack://application:,,,{themePath}", UriKind.Absolute);
            var newDict = new ResourceDictionary { Source = packUri };
            // Insert at the beginning to give it highest precedence
            merged.Insert(0, newDict);

            // Notify custom controls to refresh their theme-dependent visuals
            ThemeManager.NotifyThemeChanged();
        }

        private void ThemeToggle_Checked(object sender, RoutedEventArgs e)
        {
            // Switch to dark theme
            ApplyTheme("/Assets/DarkTheme.xaml");
        }

        private void ThemeToggle_Unchecked(object sender, RoutedEventArgs e)
        {
            // Switch to light theme
            ApplyTheme("/Assets/Fluent.xaml");
        }

        private void SyncThemeToggleState()
        {
            // Temporarily detach event handlers to prevent recursion
            TitleBarThemeToggle.Checked -= ThemeToggle_Checked;
            TitleBarThemeToggle.Unchecked -= ThemeToggle_Unchecked;
            
            try
            {
                // Set IsChecked to match the currently active theme
                TitleBarThemeToggle.IsChecked = IsDarkThemeActive();
            }
            finally
            {
                // Reattach event handlers
                TitleBarThemeToggle.Checked += ThemeToggle_Checked;
                TitleBarThemeToggle.Unchecked += ThemeToggle_Unchecked;
            }
        }

        private void ViewModel_ThemeChanged(object sender, EventArgs e)
        {
            // Sync the theme toggle button state when theme changes from ViewModel
            SyncThemeToggleState();
        }

        private void MainWindow_Closing(object sender, CancelEventArgs e)
        {
            var viewModel = DataContext as MainWindowViewModel;
            if (viewModel != null)
            {
                string message;
                if (!viewModel.CanClose(out message))
                {
                    if (!string.IsNullOrEmpty(message))
                    {
                        Views.MessageDialog.ShowWarning(message, "Validation Error", this);
                    }
                    e.Cancel = true;
                }
            }
        }

        private void ExportButton_Click(object sender, RoutedEventArgs e)
        {
            // Show context menu when export button is clicked
            var button = sender as Button;
            if (button?.ContextMenu != null)
            {
                button.ContextMenu.PlacementTarget = button;
                button.ContextMenu.IsOpen = true;
            }
        }

        private void DataGrid_Sorting(object sender, DataGridSortingEventArgs e)
        {
            // Allow default sorting behavior
            // This event handler is here to ensure the XAML reference is valid
            // The DataGrid will handle sorting automatically
        }

        private void ToggleSidebarButton_Click(object sender, RoutedEventArgs e)
        {
            var viewModel = DataContext as MainWindowViewModel;
            if (viewModel != null)
            {
                viewModel.IsSidebarCollapsed = !viewModel.IsSidebarCollapsed;
                
                // Animate sidebar width
                if (viewModel.IsSidebarCollapsed)
                {
                    SidebarColumn.Width = new GridLength(60);
                }
                else
                {
                    SidebarColumn.Width = new GridLength(280);
                }
            }
        }
    }
}
// Copyright (c) 2025 Kanders-II. All rights reserved.
// Licensed under the MIT License.
using System;
using System.Collections.Specialized;
using System.Windows;
using Launcher.ViewModels;

namespace Launcher.Views
{
    /// <summary>
    /// Code-behind for the script execution flyout window.
    /// </summary>
    public partial class FlyoutWindow : Window
    {
        public FlyoutWindow()
        {
            InitializeComponent();
            Loaded += OnLoaded;
        }

        public FlyoutWindow(FlyoutViewModel viewModel) : this()
        {
            DataContext = viewModel;
            viewModel.CloseRequested += (s, e) => Close();
        }

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            // Auto-scroll console output to bottom
            if (DataContext is FlyoutViewModel vm)
            {
                ((INotifyCollectionChanged)vm.OutputLines).CollectionChanged += (s, args) =>
                {
                    if (ConsoleOutput.Items.Count > 0)
                    {
                        ConsoleOutput.ScrollIntoView(ConsoleOutput.Items[ConsoleOutput.Items.Count - 1]);
                    }
                };

                // Bind the RenderedDocument to the RichTextBox manually
                // (FlowDocument can't be bound via normal XAML binding)
                vm.PropertyChanged += (s, args) =>
                {
                    if (args.PropertyName == nameof(FlyoutViewModel.RenderedDocument) && vm.RenderedDocument != null)
                    {
                        Dispatcher.Invoke(() =>
                        {
                            MarkdownViewer.Document = vm.RenderedDocument;
                        });
                    }
                };
            }
        }
    }
}

// Copyright (c) 2025 Kanders-II. All rights reserved.
// Licensed under the MIT License.
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows.Documents;
using System.Windows.Input;
using Launcher.Services;

namespace Launcher.ViewModels
{
    /// <summary>
    /// ViewModel for the script execution flyout window.
    /// Supports live console output during execution and Markdown-rendered results.
    /// </summary>
    public class FlyoutViewModel : INotifyPropertyChanged
    {
        private string _title = "Script Output";
        private bool _isRunning;
        private bool _showMarkdownResult;
        private string _markdownResult;
        private FlowDocument _renderedDocument;
        private string _statusText = "Ready";

        public event PropertyChangedEventHandler PropertyChanged;

        public FlyoutViewModel()
        {
            OutputLines = new ObservableCollection<string>();
            CloseCommand = new RelayCommand(_ => CloseRequested?.Invoke(this, EventArgs.Empty));
            CopyOutputCommand = new RelayCommand(_ => CopyOutput(), _ => OutputLines.Count > 0);
        }

        /// <summary>
        /// Title displayed in the flyout title bar.
        /// </summary>
        public string Title
        {
            get => _title;
            set { _title = value; OnPropertyChanged(nameof(Title)); }
        }

        /// <summary>
        /// Whether a script is currently executing.
        /// </summary>
        public bool IsRunning
        {
            get => _isRunning;
            set
            {
                _isRunning = value;
                OnPropertyChanged(nameof(IsRunning));
                StatusText = value ? "Running..." : "Completed";
            }
        }

        /// <summary>
        /// Whether to show the Markdown results pane after execution.
        /// </summary>
        public bool ShowMarkdownResult
        {
            get => _showMarkdownResult;
            set { _showMarkdownResult = value; OnPropertyChanged(nameof(ShowMarkdownResult)); }
        }

        /// <summary>
        /// Raw Markdown text for the results pane.
        /// Setting this also renders it to a FlowDocument.
        /// </summary>
        public string MarkdownResult
        {
            get => _markdownResult;
            set
            {
                _markdownResult = value;
                OnPropertyChanged(nameof(MarkdownResult));
                if (!string.IsNullOrEmpty(value))
                {
                    try
                    {
                        RenderedDocument = MarkdownParser.Parse(value);
                        ShowMarkdownResult = true;
                    }
                    catch (Exception ex)
                    {
                        LoggingService.Error($"Failed to parse Markdown: {ex.Message}", ex, component: "FlyoutViewModel");
                    }
                }
            }
        }

        /// <summary>
        /// Rendered FlowDocument from Markdown for the RichTextBox.
        /// </summary>
        public FlowDocument RenderedDocument
        {
            get => _renderedDocument;
            set { _renderedDocument = value; OnPropertyChanged(nameof(RenderedDocument)); }
        }

        /// <summary>
        /// Status bar text.
        /// </summary>
        public string StatusText
        {
            get => _statusText;
            set { _statusText = value; OnPropertyChanged(nameof(StatusText)); }
        }

        /// <summary>
        /// Live console output lines from script execution.
        /// </summary>
        public ObservableCollection<string> OutputLines { get; }

        public ICommand CloseCommand { get; }
        public ICommand CopyOutputCommand { get; }

        /// <summary>
        /// Event raised when close is requested.
        /// </summary>
        public event EventHandler CloseRequested;

        /// <summary>
        /// Appends a line to the live console output (thread-safe via dispatcher).
        /// </summary>
        public void AppendOutput(string line)
        {
            if (System.Windows.Application.Current?.Dispatcher != null)
            {
                System.Windows.Application.Current.Dispatcher.Invoke(() =>
                {
                    OutputLines.Add(line);
                });
            }
            else
            {
                OutputLines.Add(line);
            }
        }

        /// <summary>
        /// Clears the console output.
        /// </summary>
        public void ClearOutput()
        {
            if (System.Windows.Application.Current?.Dispatcher != null)
            {
                System.Windows.Application.Current.Dispatcher.Invoke(() => OutputLines.Clear());
            }
            else
            {
                OutputLines.Clear();
            }
        }

        private void CopyOutput()
        {
            try
            {
                var text = string.Join(Environment.NewLine, OutputLines);
                if (!string.IsNullOrEmpty(_markdownResult))
                {
                    text += Environment.NewLine + Environment.NewLine + "--- Results ---" + Environment.NewLine + _markdownResult;
                }
                System.Windows.Clipboard.SetText(text);
                StatusText = "Copied to clipboard";
            }
            catch (Exception ex)
            {
                LoggingService.Error($"Failed to copy output: {ex.Message}", component: "FlyoutViewModel");
            }
        }

        protected void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}

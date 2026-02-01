// Copyright (c) 2025 A Solution IT LLC. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
namespace Launcher.Services
{
    public interface IDialogService
    {
        /// <summary>
        /// Shows a folder browser dialog.
        /// </summary>
        /// <param name="description">The description to show in the dialog.</param>
        /// <param name="initialPath">The initially selected path.</param>
        /// <returns>The selected path, or null if the dialog was cancelled.</returns>
        string ShowFolderBrowserDialog(string description, string initialPath);

        /// <summary>
        /// Shows an open file dialog.
        /// </summary>
        /// <param name="initialPath">The initial directory.</param>
        /// <param name="initialFileName">The initial file name.</param>
        /// <param name="filter">File filter string (e.g., "Text Files|*.txt|All Files|*.*")</param>
        /// <param name="title">Dialog title</param>
        /// <returns>The selected file path, or null if the dialog was cancelled.</returns>
        string ShowOpenFileDialog(string initialPath, string initialFileName, string filter = null, string title = null);
        // Add more methods for other dialogs if needed (e.g., SaveFileDialog, MessageBox)
    }
}
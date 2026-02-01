// Copyright (c) 2025 A Solution IT LLC. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
using System.Collections.Generic;
using System.Windows;

namespace Launcher
{
    public class UIHost
    {
        public Dictionary<string, object> ShowDialog(object metadata)
        {
            var window = new MainWindow();
            window.DataContext = new ViewModels.MainWindowViewModel();

            if (window.ShowDialog() == true)
            {
                // Return collected data
                return ((ViewModels.MainWindowViewModel)window.DataContext).FormData;
            }

            return null;
        }
    }
} 
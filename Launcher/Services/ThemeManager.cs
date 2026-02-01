// Copyright (c) 2025 A Solution IT LLC. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
using System;

namespace Launcher.Services
{
    public static class ThemeManager
    {
        public static event EventHandler ThemeChanged;

        public static void NotifyThemeChanged()
        {
            ThemeChanged?.Invoke(null, EventArgs.Empty);
        }
    }
}

// Copyright (c) 2025 A Solution IT LLC. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
using System;
using System.Globalization;
using System.IO;
using System.Windows.Data;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace Launcher.Converters
{
    /// <summary>
    /// Converts a file path string into a BitmapImage.
    /// Returns null if the path is invalid or the file doesn't exist.
    /// </summary>
    public class FilePathToImageSourceConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is string filePath && !string.IsNullOrEmpty(filePath))
            {
                Launcher.Services.LoggingService.Info($"FilePathToImageSourceConverter: Attempting to load '{filePath}'", component: "ImageConverter");
                try
                {
                    if (File.Exists(filePath))
                    {
                        Launcher.Services.LoggingService.Info($"FilePathToImageSourceConverter: File EXISTS at '{filePath}'", component: "ImageConverter");
                        // Load the image from the file path
                        BitmapImage bitmap = new BitmapImage();
                        bitmap.BeginInit();
                        bitmap.UriSource = new Uri(filePath, UriKind.Absolute);
                        bitmap.CacheOption = BitmapCacheOption.OnLoad; // Load fully into memory
                        bitmap.EndInit();
                        bitmap.Freeze(); // Freeze for performance and thread safety
                        return bitmap;
                    }
                    else
                    {
#if DEBUG
                        System.Diagnostics.Debug.WriteLine($"FilePathToImageSourceConverter: File not found at '{filePath}'");
#endif
                        return null; // Or return a default fallback image source
                    }
                }
                catch (Exception ex)
                {
#if DEBUG
                    System.Diagnostics.Debug.WriteLine($"FilePathToImageSourceConverter: Error loading image from '{filePath}'. Error: {ex.Message}");
#endif
                    return null; // Or return a default fallback image source
                }
            }
            
            return null; // Input value is not a valid string path
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotSupportedException("Cannot convert ImageSource back to file path.");
        }
    }
} 
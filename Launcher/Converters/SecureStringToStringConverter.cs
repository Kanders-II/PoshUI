// Copyright (c) 2025 A Solution IT LLC. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
using System;
using System.Globalization;
using System.Runtime.InteropServices;
using System.Security;
using System.Windows.Data;
using Launcher.Services;

namespace Launcher.Converters
{
    public class SecureStringToStringConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            // Convert SecureString (ViewModel) to string (Control DP)
            if (value is SecureString secureString)
            {
                IntPtr valuePtr = IntPtr.Zero;
                try
                {
                    // Decrypt SecureString to unmanaged memory
                    valuePtr = Marshal.SecureStringToGlobalAllocUnicode(secureString);
                    // Marshal the unmanaged string to a managed string
                    string insecureString = Marshal.PtrToStringUni(valuePtr);
                    LoggingService.Trace("SecureStringToStringConverter: Converted SecureString (ViewModel) to string (Control DP)");
                    return insecureString;
                }
                catch (Exception ex)
                {
                    LoggingService.Error("Error converting SecureString to string in converter.", ex);
                    return string.Empty;
                }
                finally
                {
                    // Zero out and free the unmanaged memory
                    if (valuePtr != IntPtr.Zero)
                    {
                        Marshal.ZeroFreeGlobalAllocUnicode(valuePtr);
                    }
                }
            }
            LoggingService.Trace("SecureStringToStringConverter: Convert called with non-SecureString or null value.");
            return string.Empty; // Default if input is not a SecureString
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            // Convert string (Control DP) back to SecureString (ViewModel)
            if (value is string insecureString)
            {
                SecureString secureString = new SecureString();
                try
                {
                    foreach (char c in insecureString)
                    {
                        secureString.AppendChar(c);
                    }
                    secureString.MakeReadOnly(); // Good practice
                    LoggingService.Trace("SecureStringToStringConverter: Converted string (Control DP) back to SecureString (ViewModel)");
                    return secureString;
                }
                catch (Exception ex)
                {
                    LoggingService.Error("Error converting string back to SecureString in converter.", ex);
                    // Clear the secure string on error
                    secureString.Dispose(); 
                    return new SecureString(); // Return empty SecureString on error
                }
            }
            LoggingService.Trace("SecureStringToStringConverter: ConvertBack called with non-string or null value.");
            return new SecureString(); // Default if input is not a string
        }
    }
} 
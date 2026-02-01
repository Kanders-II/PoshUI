// Copyright (c) 2025 A Solution IT LLC. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
using System;
using System.Windows;
using System.IO;
using System.Diagnostics;
using Launcher.Services;
using Launcher.ViewModels;

namespace Launcher
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;
            this.DispatcherUnhandledException += App_DispatcherUnhandledException;
            
            try
            {
                base.OnStartup(e);

                // Check .NET Framework version before anything else
                if (!ValidateDotNetFrameworkVersion())
                {
                    MessageBox.Show(
                        "This application requires .NET Framework 4.8 or later.\n\n" +
                        "Please install .NET Framework 4.8 from:\n" +
                        "https://dotnet.microsoft.com/download/dotnet-framework/net48",
                        "Missing Required Framework",
                        MessageBoxButton.OK,
                        MessageBoxImage.Error);
                    Shutdown(1);
                    return;
                }

                // Parse command line arguments first to configure logging
                var (scriptPath, debugEnabled, allowUnrestricted) = ParseCommandLineArgs(e.Args);

                // Initialize logging with debug settings
                LoggingService.Initialize(debugEnabled);

                // Initialize audit logging
                AuditLogger.Initialize();

                LoggingService.Info("Application starting");
                LoggingService.Info($"Current directory: {Environment.CurrentDirectory}");
                LoggingService.Info($"Debug mode: {debugEnabled}");

                if (!string.IsNullOrEmpty(scriptPath))
                {
                    LoggingService.Info($"Script path provided: {scriptPath}");
                    
                    // Resolve relative path if needed
                    if (!Path.IsPathRooted(scriptPath))
                    {
                        string oldPath = scriptPath;
                        scriptPath = Path.GetFullPath(Path.Combine(Environment.CurrentDirectory, scriptPath));
                        LoggingService.Info($"Resolved relative path: {oldPath} -> {scriptPath}");
                    }
                    
                    // Validate script path for security
                    if (!SecurityValidator.ValidateScriptPath(scriptPath, out string validationError))
                    {
                        LoggingService.Error($"Script path validation failed: {validationError}");
                        MessageBox.Show($"Security validation failed: {validationError}", "Security Error", MessageBoxButton.OK, MessageBoxImage.Error);
                        Shutdown(1);
                        return;
                    }
                }
                else
                {
                    LoggingService.Info("No script path provided");
                }

                // Create main window
                LoggingService.Info("Creating main window");
                ShowMainWindow(scriptPath, allowUnrestricted);
                
                LoggingService.Info("Startup completed successfully");
            }
            catch (Exception ex)
            {
                LoggingService.Fatal("Application startup error", ex);
                MessageBox.Show($"Application startup error: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        
        protected override void OnExit(ExitEventArgs e)
        {
            LoggingService.Info("Application exiting");
            LoggingService.Shutdown();
            base.OnExit(e);
        }
        
        private void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            var ex = e.ExceptionObject as Exception;
            LoggingService.Fatal("Unhandled exception", ex);
            try {
                File.AppendAllText("logs/GlobalUnhandledException.log", $"[AppDomain] {DateTime.Now}: {ex?.ToString() ?? e.ExceptionObject.ToString()}\n");
            } catch {}
        }

        private void App_DispatcherUnhandledException(object sender, System.Windows.Threading.DispatcherUnhandledExceptionEventArgs e)
        {
            LoggingService.Fatal("Dispatcher unhandled exception", e.Exception);
            try {
                File.AppendAllText("logs/GlobalUnhandledException.log", $"[Dispatcher] {DateTime.Now}: {e.Exception}\n");
            } catch {}
            MessageBox.Show($"A fatal error occurred:\n{e.Exception}", "Unhandled Exception", MessageBoxButton.OK, MessageBoxImage.Error);
            e.Handled = true;
        }

        private void ShowMainWindow(string scriptPath = null, bool allowUnrestricted = false)
        {
            try
            {
                LoggingService.Info($"Creating main window with script: {scriptPath}");

                MainWindowViewModel viewModel = new MainWindowViewModel
                {
                    AllowUnrestrictedExecution = allowUnrestricted
                };
                
                if (!string.IsNullOrEmpty(scriptPath))
                {
                    viewModel.LoadScriptCommand.Execute(scriptPath);
                }
                
                MainWindow mainWindow = new MainWindow
                {
                    DataContext = viewModel
                };
                
                // Subscribe to window close event to write results
                mainWindow.Closed += (s, args) =>
                {
                    try
                    {
                        // Check if Module API mode (indicated by temp file path pattern)
                        if (!string.IsNullOrEmpty(scriptPath) && scriptPath.Contains("PoshWizard_") && scriptPath.EndsWith(".ps1"))
                        {
                            // Get result file path (same directory as temp script, .result.json extension)
                            string resultPath = Path.ChangeExtension(scriptPath, ".result.json");
                            
                            // Check if ViewModel has execution results
                            if (viewModel.LastExecutionResult != null)
                            {
                                LoggingService.Info($"Writing Module API results to: {resultPath}");
                                File.WriteAllText(resultPath, viewModel.LastExecutionResult);
                            }
                            else
                            {
                                LoggingService.Warn("No execution result available to write");
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        LoggingService.Error("Error writing result file", ex);
                    }
                };
                
                // Make sure window is visible
                mainWindow.WindowState = System.Windows.WindowState.Normal;
                mainWindow.ShowInTaskbar = true;
                mainWindow.Topmost = true;
                mainWindow.Show();
                
                // Force window to be active and visible
                mainWindow.Activate();
                mainWindow.Focus();

                LoggingService.Info("Main window created and shown");
            }
            catch (Exception ex)
            {
                LoggingService.Error("Error showing main window", ex);
                MessageBox.Show($"Error showing main window: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private (string scriptPath, bool debugEnabled, bool allowUnrestricted) ParseCommandLineArgs(string[] args)
        {
            string scriptPath = null;
            bool debugEnabled = false;
            bool allowUnrestricted = true; // Default to Full Language Mode for internal use

            for (int i = 0; i < args.Length; i++)
            {
                string arg = args[i];
                
                if (arg == "--debug" || arg == "-d")
                {
                    debugEnabled = true;
                }
                else if (arg.StartsWith("--debug") || arg.StartsWith("-d"))
                {
                    // Handle --debug=true or similar
                    debugEnabled = true;
                }
                else if (arg == "--unrestricted" || arg == "-u")
                {
                    allowUnrestricted = true;
                    LoggingService.Warn("Unrestricted execution mode requested via command line flag");
                }
                else if (!arg.StartsWith("-") && scriptPath == null)
                {
                    // First non-flag argument is the script path
                    scriptPath = arg;
                }
            }
            
            // Security: Validate unrestricted mode request
            // DISABLED: Signature verification removed to allow unrestricted execution for internal scripts
            if (allowUnrestricted && !string.IsNullOrEmpty(scriptPath))
            {
                LoggingService.Info($"Unrestricted mode enabled for script: {scriptPath}");
                AuditLogger.LogSecurityViolation(
                    "Unrestricted Mode Granted",
                    $"Script: {scriptPath}\nWarning: Running with full system privileges (signature check bypassed)"
                );
            }
            
            /* COMMENTED OUT - Signature verification disabled for internal use
            if (allowUnrestricted && !string.IsNullOrEmpty(scriptPath))
            {
                // Check if script is signed by a trusted publisher
                bool isSignatureValid = SignatureValidator.VerifySignature(scriptPath, out string errorMessage);
                
                if (!isSignatureValid)
                {
                    LoggingService.Warn($"Unrestricted mode requested but script is not properly signed: {errorMessage}");
                    LoggingService.Warn("Script will use custom security validation.");
                    AuditLogger.LogSecurityViolation(
                        "Unrestricted Mode Denied",
                        $"Script: {scriptPath}\nReason: {errorMessage}"
                    );
                    allowUnrestricted = false;
                }
                else
                {
                    LoggingService.Info($"Unrestricted mode approved: Script signature verified");
                    AuditLogger.LogSecurityViolation(
                        "Unrestricted Mode Granted",
                        $"Script: {scriptPath}\nSignature: Valid\nWarning: Running with full system privileges"
                    );
                }
            }
            */

            return (scriptPath, debugEnabled, allowUnrestricted);
        }

        /// <summary>
        /// Validates that .NET Framework 4.8 or later is installed.
        /// </summary>
        private bool ValidateDotNetFrameworkVersion()
        {
            try
            {
                // Check CLR version (.NET Framework 4.8 uses CLR 4.0)
                var clrVersion = Environment.Version;

                // .NET Framework 4.8 reports as CLR 4.0.30319.xxxxx
                // We need at least CLR 4.0.30319.42000 (which is .NET 4.6+)
                // For .NET 4.8 specifically, we check the release key in registry

                if (clrVersion.Major < 4)
                {
                    return false;
                }

                // Additional check: Read .NET Framework release from registry
                // .NET Framework 4.8 = release key 528040 or higher
                using (var ndpKey = Microsoft.Win32.Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\NET Framework Setup\NDP\v4\Full\"))
                {
                    if (ndpKey != null)
                    {
                        var releaseKey = ndpKey.GetValue("Release");
                        if (releaseKey != null && (int)releaseKey >= 528040)
                        {
                            return true; // .NET Framework 4.8 or later
                        }
                    }
                }

                // If we can't determine version from registry, check CLR version
                // CLR 4.0.30319.42000+ is .NET Framework 4.6+
                return clrVersion.Major >= 4 && clrVersion.Build >= 30319;
            }
            catch
            {
                // If version check fails, assume it's not installed
                return false;
            }
        }
    }
} 
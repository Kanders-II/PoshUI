// Copyright (c) 2025 A Solution IT LLC. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Management.Automation;
using System.Management.Automation.Runspaces;
using System.Threading;
using System.Threading.Tasks;

namespace Launcher.Services
{
    /// <summary>
    /// Executes PowerShell scripts in an isolated runspace for script card execution.
    /// Provides output streaming, progress reporting, and cancellation support.
    /// </summary>
    public class ScriptCardRunner : IDisposable
    {
        private Runspace _runspace;
        private PowerShell _powerShell;
        private bool _disposed;

        /// <summary>
        /// Raised when output is received from the script.
        /// </summary>
        public event EventHandler<string> OutputReceived;

        /// <summary>
        /// Raised when progress percentage changes.
        /// </summary>
        public event EventHandler<double> ProgressChanged;

        public ScriptCardRunner()
        {
            LoggingService.Debug("Creating ScriptCardRunner with isolated runspace", component: "ScriptCardRunner");
            
            var initialState = InitialSessionState.CreateDefault();
            _runspace = RunspaceFactory.CreateRunspace(initialState);
            _runspace.Open();
        }

        /// <summary>
        /// Executes a PowerShell script with the specified parameters.
        /// </summary>
        /// <param name="script">The script to execute (can be a dot-source command or inline script).</param>
        /// <param name="parameters">Dictionary of parameter names and values.</param>
        /// <param name="cancellationToken">Token to cancel execution.</param>
        public async Task ExecuteAsync(
            string script,
            Dictionary<string, object> parameters,
            CancellationToken cancellationToken)
        {
            if (_disposed)
                throw new ObjectDisposedException(nameof(ScriptCardRunner));

            LoggingService.Info($"Executing script in isolated runspace", component: "ScriptCardRunner");
            var scriptPreview = script != null && script.Length > 100 ? script.Substring(0, 100) : script;
            LoggingService.Debug($"Script: {scriptPreview}...", component: "ScriptCardRunner");

            _powerShell = PowerShell.Create();
            _powerShell.Runspace = _runspace;

            // Add the script
            _powerShell.AddScript(script);

            // Add parameters
            if (parameters != null)
            {
                foreach (var kvp in parameters)
                {
                    if (kvp.Value != null)
                    {
                        _powerShell.AddParameter(kvp.Key, kvp.Value);
                        LoggingService.Trace($"Added parameter: {kvp.Key}", component: "ScriptCardRunner");
                    }
                }
            }

            // Subscribe to output streams
            _powerShell.Streams.Information.DataAdded += (s, e) =>
            {
                var record = _powerShell.Streams.Information[e.Index];
                var message = record.MessageData != null ? record.MessageData.ToString() : "";
                if (!string.IsNullOrEmpty(message))
                {
                    OutputReceived?.Invoke(this, message);
                }
            };

            _powerShell.Streams.Warning.DataAdded += (s, e) =>
            {
                var record = _powerShell.Streams.Warning[e.Index];
                OutputReceived?.Invoke(this, $"⚠️ WARNING: {record.Message}");
            };

            _powerShell.Streams.Error.DataAdded += (s, e) =>
            {
                var record = _powerShell.Streams.Error[e.Index];
                var errorMessage = record.Exception != null ? record.Exception.Message : record.ToString();
                OutputReceived?.Invoke(this, $"❌ ERROR: {errorMessage}");
            };

            _powerShell.Streams.Verbose.DataAdded += (s, e) =>
            {
                var record = _powerShell.Streams.Verbose[e.Index];
                OutputReceived?.Invoke(this, $"VERBOSE: {record.Message}");
            };

            _powerShell.Streams.Debug.DataAdded += (s, e) =>
            {
                var record = _powerShell.Streams.Debug[e.Index];
                OutputReceived?.Invoke(this, $"DEBUG: {record.Message}");
            };

            _powerShell.Streams.Progress.DataAdded += (s, e) =>
            {
                var record = _powerShell.Streams.Progress[e.Index];
                if (record.PercentComplete >= 0 && record.PercentComplete <= 100)
                {
                    ProgressChanged?.Invoke(this, record.PercentComplete);
                }
                
                var statusMessage = !string.IsNullOrEmpty(record.StatusDescription)
                    ? $"[{record.PercentComplete}%] {record.StatusDescription}"
                    : $"[{record.PercentComplete}%] {record.Activity}";
                    
                if (!string.IsNullOrEmpty(statusMessage))
                {
                    OutputReceived?.Invoke(this, statusMessage);
                }
            };

            // Execute with cancellation support
            var tcs = new TaskCompletionSource<bool>();

            // Register cancellation
            using (var registration = cancellationToken.Register(() =>
            {
                LoggingService.Warn("Cancellation requested - stopping PowerShell execution", component: "ScriptCardRunner");
                try
                {
                    if (_powerShell != null)
                    {
                        _powerShell.Stop();
                    }
                }
                catch (Exception ex)
                {
                    LoggingService.Error($"Error stopping PowerShell: {ex.Message}", component: "ScriptCardRunner");
                }
            }))
            {
                try
                {
                    // Begin async invocation
                    var asyncResult = _powerShell.BeginInvoke();

                    // Wait for completion on background thread
                    await Task.Run(() =>
                    {
                        asyncResult.AsyncWaitHandle.WaitOne();
                    }, cancellationToken);

                    // Get results
                    var results = _powerShell.EndInvoke(asyncResult);

                    // Process output objects
                    foreach (var result in results)
                    {
                        if (result != null)
                        {
                            string output;
                            // Handle formatting objects (Format-Table, Format-List, etc.)
                            if (result is System.Management.Automation.PSObject psObj && 
                                psObj.BaseObject != null &&
                                psObj.BaseObject.GetType().FullName != null &&
                                psObj.BaseObject.GetType().FullName.StartsWith("Microsoft.PowerShell.Commands.Internal.Format."))
                            {
                                // Skip formatting objects - they're handled by Out-String in the script
                                continue;
                            }
                            else
                            {
                                output = result.ToString();
                            }
                            
                            if (!string.IsNullOrEmpty(output))
                            {
                                OutputReceived?.Invoke(this, output);
                            }
                        }
                    }

                    // Check for errors
                    if (_powerShell.HadErrors && _powerShell.Streams.Error.Count > 0)
                    {
                        var firstError = _powerShell.Streams.Error[0];
                        var errorMessage = firstError.Exception != null ? firstError.Exception.Message : firstError.ToString();
                        LoggingService.Error($"Script execution had errors: {errorMessage}", component: "ScriptCardRunner");
                        throw new Exception(errorMessage);
                    }

                    LoggingService.Info("Script execution completed successfully", component: "ScriptCardRunner");
                }
                catch (OperationCanceledException)
                {
                    LoggingService.Warn("Script execution was cancelled", component: "ScriptCardRunner");
                    throw;
                }
                catch (Exception ex)
                {
                    LoggingService.Error($"Script execution failed: {ex.Message}", component: "ScriptCardRunner");
                    throw;
                }
            }
        }

        /// <summary>
        /// Executes a PowerShell script file with the specified parameters.
        /// Uses AddCommand for proper parameter binding.
        /// </summary>
        public async Task ExecuteFileAsync(
            string scriptPath,
            Dictionary<string, object> parameters,
            CancellationToken cancellationToken)
        {
            if (_disposed)
                throw new ObjectDisposedException(nameof(ScriptCardRunner));

            LoggingService.Info($"Executing script file: {scriptPath}", component: "ScriptCardRunner");

            _powerShell = PowerShell.Create();
            _powerShell.Runspace = _runspace;

            // Use AddCommand to invoke the script file with proper parameter binding
            _powerShell.AddCommand(scriptPath);

            // Add parameters
            if (parameters != null)
            {
                foreach (var kvp in parameters)
                {
                    if (kvp.Value != null)
                    {
                        _powerShell.AddParameter(kvp.Key, kvp.Value);
                        LoggingService.Trace($"Added parameter: {kvp.Key} = {kvp.Value}", component: "ScriptCardRunner");
                    }
                }
            }

            // Subscribe to output streams (same as ExecuteAsync)
            _powerShell.Streams.Information.DataAdded += (s, e) =>
            {
                var record = _powerShell.Streams.Information[e.Index];
                var message = record.MessageData != null ? record.MessageData.ToString() : "";
                if (!string.IsNullOrEmpty(message))
                {
                    OutputReceived?.Invoke(this, message);
                }
            };

            _powerShell.Streams.Warning.DataAdded += (s, e) =>
            {
                var record = _powerShell.Streams.Warning[e.Index];
                OutputReceived?.Invoke(this, $"⚠️ WARNING: {record.Message}");
            };

            _powerShell.Streams.Error.DataAdded += (s, e) =>
            {
                var record = _powerShell.Streams.Error[e.Index];
                var errorMessage = record.Exception != null ? record.Exception.Message : record.ToString();
                OutputReceived?.Invoke(this, $"❌ ERROR: {errorMessage}");
            };

            _powerShell.Streams.Verbose.DataAdded += (s, e) =>
            {
                var record = _powerShell.Streams.Verbose[e.Index];
                OutputReceived?.Invoke(this, $"VERBOSE: {record.Message}");
            };

            _powerShell.Streams.Debug.DataAdded += (s, e) =>
            {
                var record = _powerShell.Streams.Debug[e.Index];
                OutputReceived?.Invoke(this, $"DEBUG: {record.Message}");
            };

            _powerShell.Streams.Progress.DataAdded += (s, e) =>
            {
                var record = _powerShell.Streams.Progress[e.Index];
                if (record.PercentComplete >= 0 && record.PercentComplete <= 100)
                {
                    ProgressChanged?.Invoke(this, record.PercentComplete);
                }
                
                var statusMessage = !string.IsNullOrEmpty(record.StatusDescription)
                    ? $"[{record.PercentComplete}%] {record.StatusDescription}"
                    : $"[{record.PercentComplete}%] {record.Activity}";
                    
                if (!string.IsNullOrEmpty(statusMessage))
                {
                    OutputReceived?.Invoke(this, statusMessage);
                }
            };

            // Execute with cancellation support
            var tcs = new TaskCompletionSource<bool>();

            using (var registration = cancellationToken.Register(() =>
            {
                LoggingService.Warn("Cancellation requested - stopping PowerShell execution", component: "ScriptCardRunner");
                try
                {
                    if (_powerShell != null)
                    {
                        _powerShell.Stop();
                    }
                }
                catch (Exception ex)
                {
                    LoggingService.Error($"Error stopping PowerShell: {ex.Message}", component: "ScriptCardRunner");
                }
            }))
            {
                try
                {
                    var asyncResult = _powerShell.BeginInvoke();
                    
                    await Task.Run(() =>
                    {
                        asyncResult.AsyncWaitHandle.WaitOne();
                    }, cancellationToken);

                    var results = _powerShell.EndInvoke(asyncResult);

                    foreach (var result in results)
                    {
                        if (result != null)
                        {
                            string output;
                            if (result is System.Management.Automation.PSObject psObj && 
                                psObj.BaseObject != null &&
                                psObj.BaseObject.GetType().FullName != null &&
                                psObj.BaseObject.GetType().FullName.StartsWith("Microsoft.PowerShell.Commands.Internal.Format."))
                            {
                                continue;
                            }
                            else
                            {
                                output = result.ToString();
                            }
                            
                            if (!string.IsNullOrEmpty(output))
                            {
                                OutputReceived?.Invoke(this, output);
                            }
                        }
                    }

                    if (_powerShell.HadErrors && _powerShell.Streams.Error.Count > 0)
                    {
                        var firstError = _powerShell.Streams.Error[0];
                        var errorMessage = firstError.Exception != null ? firstError.Exception.Message : firstError.ToString();
                        LoggingService.Error($"Script execution had errors: {errorMessage}", component: "ScriptCardRunner");
                        throw new Exception(errorMessage);
                    }

                    LoggingService.Info("Script file execution completed successfully", component: "ScriptCardRunner");
                }
                catch (OperationCanceledException)
                {
                    LoggingService.Warn("Script execution was cancelled", component: "ScriptCardRunner");
                    throw;
                }
                catch (Exception ex)
                {
                    LoggingService.Error($"Script execution failed: {ex.Message}", component: "ScriptCardRunner");
                    throw;
                }
            }
        }

        public void Dispose()
        {
            if (_disposed)
                return;

            _disposed = true;

            try
            {
                if (_powerShell != null)
                {
                    _powerShell.Dispose();
                }
                if (_runspace != null)
                {
                    _runspace.Close();
                    _runspace.Dispose();
                }
                LoggingService.Debug("ScriptCardRunner disposed", component: "ScriptCardRunner");
            }
            catch (Exception ex)
            {
                LoggingService.Error($"Error disposing ScriptCardRunner: {ex.Message}", component: "ScriptCardRunner");
            }
        }
    }
}



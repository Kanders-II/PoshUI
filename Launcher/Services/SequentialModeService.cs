// Copyright (c) 2025 A Solution IT LLC. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Pipes;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Runtime.Serialization.Json;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;
using Launcher.Models;
using Launcher.ViewModels;
using System.Collections;
using System.Management.Automation;
using System.Management.Automation.Runspaces;

namespace Launcher.Services
{
    public class SequentialModeService : IDisposable
    {
        private NamedPipeServerStream _pipeServer;
        private readonly string _pipeName;
        private readonly Dictionary<string, WizardSession> _sessions;
        private CancellationTokenSource _cancellationTokenSource;
        private Task _listenerTask;
        private X509Certificate2 _sessionCertificate;
        private string _sessionSecret;
        private MainWindowViewModel _mainViewModel;
        private MainWindow _mainWindow;
        private readonly SemaphoreSlim _sendLock = new SemaphoreSlim(1, 1);

        public string PipeName => _pipeName;
        public string SessionSecret => _sessionSecret;
        public X509Certificate2 Certificate => _sessionCertificate;

        public SequentialModeService()
        {
            _pipeName = $"PoshUI_{Process.GetCurrentProcess().Id}_{Guid.NewGuid():N}";
            _sessions = new Dictionary<string, WizardSession>();
            _cancellationTokenSource = new CancellationTokenSource();
        }

        public string Start()
        {
            try
            {
                // Generate session security
                _sessionCertificate = SecurityService.GenerateSessionCertificate();
                _sessionSecret = SecurityService.GenerateSessionToken();

                LoggingService.Info($"Generated session certificate: {SecurityService.GetCertificateFingerprint(_sessionCertificate)}", "SequentialMode");
                LoggingService.Info($"Session secret generated (length: {_sessionSecret.Length})", "SequentialMode");

                // Create named pipe server
                _pipeServer = new NamedPipeServerStream(
                    _pipeName,
                    PipeDirection.InOut,
                    1, // maxNumberOfServerInstances
                    PipeTransmissionMode.Message,
                    PipeOptions.Asynchronous);

                LoggingService.Info($"Sequential Mode started - Pipe: {_pipeName}", "SequentialMode");
                
                _listenerTask = Task.Run(() => ListenForConnections(_cancellationTokenSource.Token));
                
                return _pipeName;
            }
            catch (Exception ex)
            {
                LoggingService.Error("Failed to start Sequential Mode", ex, "SequentialMode");
                throw;
            }
        }

        // Robust payload deserialization that supports both string and already-deserialized objects
        private T DeserializePayload<T>(object payload)
        {
            try
            {
                if (payload == null)
                    return default(T);
                    
                string json;
                if (payload is string s)
                {
                    json = s;
                }
                else
                {
                    // If payload is already an object, serialize it first
                    using (var ms = new MemoryStream())
                    {
                        var ser = new DataContractJsonSerializer(payload.GetType());
                        ser.WriteObject(ms, payload);
                        json = Encoding.UTF8.GetString(ms.ToArray());
                    }
                }
                
                // Deserialize to target type
                using (var ms = new MemoryStream(Encoding.UTF8.GetBytes(json)))
                {
                    var serializer = new DataContractJsonSerializer(typeof(T));
                    return (T)serializer.ReadObject(ms);
                }
            }
            catch (Exception ex)
            {
                LoggingService.Error($"Failed to deserialize payload to {typeof(T).Name}", ex, "SequentialMode");
                throw;
            }
        }

        private async Task ListenForConnections(CancellationToken cancellationToken)
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                try
                {
                    LoggingService.Info("Waiting for PowerShell module connection...", "SequentialMode");
                    await _pipeServer.WaitForConnectionAsync(cancellationToken);
                    LoggingService.Info("PowerShell module connected", "SequentialMode");

                    await HandleClientCommunication(cancellationToken);
                }
                catch (OperationCanceledException)
                {
                    LoggingService.Info("Sequential mode cancelled", "SequentialMode");
                    break;
                }
                catch (Exception ex)
                {
                    LoggingService.Error("Error in pipe listener", ex, "SequentialMode");
                }
                finally
                {
                    if (_pipeServer.IsConnected)
                    {
                        _pipeServer.Disconnect();
                    }
                }
            }
        }

        private async Task HandleClientCommunication(CancellationToken cancellationToken)
        {
            var buffer = new byte[4096];
            var messageBuilder = new StringBuilder();

            while (_pipeServer.IsConnected && !cancellationToken.IsCancellationRequested)
            {
                try
                {
                    var bytesRead = await _pipeServer.ReadAsync(buffer, 0, buffer.Length, cancellationToken);
                    
                    if (bytesRead == 0)
                    {
                        LoggingService.Info("Client disconnected", "SequentialMode");
                        break;
                    }

                    var messageData = Encoding.UTF8.GetString(buffer, 0, bytesRead);
                    messageBuilder.Append(messageData);

                    // Process only complete messages (newline-delimited), preserve any partial remainder
                    var fullMessage = messageBuilder.ToString();
                    var lastNewlineIndex = fullMessage.LastIndexOf('\n');
                    if (lastNewlineIndex >= 0)
                    {
                        var upToLast = fullMessage.Substring(0, lastNewlineIndex);
                        var messages = upToLast.Split(new char[] { '\n' }, StringSplitOptions.RemoveEmptyEntries);

                        foreach (var msg in messages)
                        {
                            if (!string.IsNullOrWhiteSpace(msg))
                            {
                                await ProcessMessage(msg.Trim());
                            }
                        }

                        // Keep any partial message content after the last newline
                        messageBuilder.Clear();
                        if (lastNewlineIndex + 1 < fullMessage.Length)
                        {
                            messageBuilder.Append(fullMessage.Substring(lastNewlineIndex + 1));
                        }
                    }
                }
                catch (OperationCanceledException)
                {
                    break;
                }
                catch (Exception ex)
                {
                    LoggingService.Error("Error reading from pipe", ex, "SequentialMode");
                    break;
                }
            }
        }

        private async Task ProcessMessage(string messageJson)
        {
            try
            {
                LoggingService.Debug($"Processing message: {messageJson.Substring(0, Math.Min(100, messageJson.Length))}...", "SequentialMode");
                
                // Deserialize using built-in DataContractJsonSerializer
                UIMessage message;
                using (var ms = new MemoryStream(Encoding.UTF8.GetBytes(messageJson)))
                {
                    var serializer = new DataContractJsonSerializer(typeof(UIMessage));
                    message = (UIMessage)serializer.ReadObject(ms);
                }
                
                // Validate message security
                if (!ValidateMessage(message))
                {
                    await SendErrorResponse(message?.MessageId, "Message validation failed");
                    return;
                }

                // Process based on message type
                await HandleMessage(message);
            }
            catch (ArgumentException ex)
            {
                LoggingService.Error("Failed to deserialize message", ex, "SequentialMode");
                await SendErrorResponse(null, "Invalid message format");
            }
            catch (InvalidOperationException ex)
            {
                LoggingService.Error("Failed to deserialize message", ex, "SequentialMode");
                await SendErrorResponse(null, "Invalid message format");
            }
            catch (Exception ex)
            {
                LoggingService.Error("Error processing message", ex, "SequentialMode");
                await SendErrorResponse(null, ex.Message);
            }
        }

        private bool ValidateMessage(UIMessage message)
        {
            if (message == null)
            {
                LoggingService.Warn("Received null message", "SequentialMode");
                return false;
            }

            // Validate auth token
            if (message.AuthToken != _sessionSecret)
            {
                LoggingService.Warn($"Invalid auth token in message {message.MessageId}", "SequentialMode");
                return false;
            }

            // Validate process ownership
            if (!SecurityService.ValidateProcessOwnership(message.ClientProcessId))
            {
                LoggingService.Warn($"Process ownership validation failed for PID {message.ClientProcessId}", "SequentialMode");
                return false;
            }

            // Validate message signature if present
            if (!string.IsNullOrEmpty(message.MessageHash))
            {
                if (!SecurityService.VerifyMessageSignature(message, _sessionCertificate))
                {
                    LoggingService.Warn($"Message signature validation failed for {message.MessageId}", "SequentialMode");
                    return false;
                }
            }

            return true;
        }

        private async Task HandleMessage(UIMessage message)
        {
            LoggingService.Info($"Handling message: {message.Type} ({message.MessageId})", "SequentialMode");

            switch (message.Type)
            {
                case UIMessageType.CreateSession:
                    await HandleCreateSession(message);
                    break;
                    
                case UIMessageType.AddStep:
                    await HandleAddStep(message);
                    break;
                    
                case UIMessageType.ExecuteStep:
                    await HandleExecuteStep(message);
                    break;
                    
                case UIMessageType.SetVariable:
                    await HandleSetVariable(message);
                    break;
                    
                case UIMessageType.GetVariable:
                    await HandleGetVariable(message);
                    break;
                    
                case UIMessageType.CloseSession:
                    await HandleCloseSession(message);
                    break;
                    
                default:
                    LoggingService.Warn($"Unknown message type: {message.Type}", "SequentialMode");
                    await SendErrorResponse(message.MessageId, $"Unknown message type: {message.Type}");
                    break;
            }
        }

        private async Task HandleCreateSession(UIMessage message)
        {
            try
            {
                var payload = DeserializePayload<CreateSessionPayload>(message.Payload);
                var sessionId = Guid.NewGuid().ToString();
                
                LoggingService.Info($"Creating session {sessionId}: {payload.SessionName}", "SequentialMode");

                // Create wizard session
                var session = new WizardSession
                {
                    SessionId = sessionId,
                    Name = payload.SessionName,
                    ScriptPath = payload.ScriptPath,
                    DebugMode = payload.DebugMode,
                    CreatedAt = DateTime.UtcNow,
                    Variables = payload.InitialParameters ?? new Dictionary<string, object>()
                };

                _sessions[sessionId] = session;

                // Create UI on main thread if needed
                if (Application.Current != null)
                {
                    await Application.Current.Dispatcher.InvokeAsync(() =>
                    {
                        CreateMainWindow(session);
                    });
                }

                // Send success response
                var response = new UIMessage
                {
                    MessageId = message.MessageId,  // Use the same MessageId as the request
                    SessionId = sessionId,
                    Type = UIMessageType.SessionCreated,
                    Payload = new SessionCreatedPayload
                    {
                        SessionId = sessionId,
                        PipeName = _pipeName,
                        Success = true,
                        Message = "Session created successfully",
                        Certificate = Convert.ToBase64String(_sessionCertificate.RawData)
                    }
                };

                await SendMessage(response);
                LoggingService.Info($"Session {sessionId} created successfully", "SequentialMode");
            }
            catch (Exception ex)
            {
                LoggingService.Error("Failed to create session", ex, "SequentialMode");
                await SendErrorResponse(message.MessageId, ex.Message);
            }
        }

        private Type GetTypeFromString(string typeName)
        {
            if (string.IsNullOrEmpty(typeName))
                return typeof(string);

            switch (typeName.ToLower())
            {
                case "string":
                    return typeof(string);
                case "bool":
                case "boolean":
                    return typeof(bool);
                case "int":
                case "integer":
                    return typeof(int);
                case "double":
                case "decimal":
                    return typeof(double);
                case "securestring":
                    return typeof(System.Security.SecureString);
                case "path":
                    return typeof(string);
                default:
                    return typeof(string);
            }
        }

        private void CreateMainWindow(WizardSession session)
        {
            try
            {
                LoggingService.Info($"Creating main window for session {session.SessionId}", "SequentialMode");

                _mainViewModel = new MainWindowViewModel();
                _mainWindow = new MainWindow
                {
                    DataContext = _mainViewModel
                };

                // Configure for sequential mode
                _mainWindow.WindowState = WindowState.Normal;
                _mainWindow.ShowInTaskbar = true;
                _mainWindow.Show();
                _mainWindow.Activate();

                LoggingService.Info("Main window created for sequential mode", "SequentialMode");
            }
            catch (Exception ex)
            {
                LoggingService.Error("Failed to create main window for sequential mode", ex, "SequentialMode");
                throw;
            }
        }

        private async Task HandleAddStep(UIMessage message)
        {
            try
            {
                var payload = DeserializePayload<AddStepPayload>(message.Payload);
                
                if (!_sessions.TryGetValue(message.SessionId, out var session))
                {
                    await SendErrorResponse(message.MessageId, "Session not found");
                    return;
                }

                LoggingService.Info($"Adding step '{payload.Title}' to session {session.SessionId}", "SequentialMode");

                // Create DynamicWizardStep for session storage
                var dynamicStep = new DynamicWizardStep
                {
                    StepId = payload.StepId,
                    Title = payload.Title,
                    PageType = payload.PageType ?? "GenericForm",
                    Description = payload.Description,
                    Position = payload.Position ?? session.Steps.Count,
                    Parameters = payload.Parameters ?? new List<ParameterDefinition>(),
                    ExecutionScript = payload.ExecutionScript,
                    Properties = payload.Properties ?? new Dictionary<string, object>()
                };

                // Add to session
                session.Steps.Add(dynamicStep);

                // Create WizardStep for UI
                var wizardStep = new WizardStep
                {
                    Title = payload.Title,
                    PageType = payload.PageType ?? "GenericForm",
                    Description = payload.Description,
                    Order = payload.Position ?? session.Steps.Count,
                    Parameters = payload.Parameters?.Select(p => new ParameterInfo
                    {
                        Name = p.Name,
                        ParameterType = GetTypeFromString(p.Type),
                        Label = p.Label,
                        ValidationPattern = p.ValidationPattern,
                        IsMandatory = p.Mandatory
                    }).ToList() ?? new List<ParameterInfo>()
                };

                // Add to main window view model if available
                if (_mainViewModel != null)
                {
                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        _mainViewModel.AddStepDynamic(wizardStep);
                    });
                }
                
                var response = new UIMessage
                {
                    SessionId = message.SessionId,
                    Type = UIMessageType.StepStarted,
                    Payload = new { StepId = payload.StepId, Success = true },
                    MessageId = message.MessageId
                };

                await SendMessage(response);
            }
            catch (Exception ex)
            {
                LoggingService.Error("Failed to add step", ex, "SequentialMode");
                await SendErrorResponse(message.MessageId, ex.Message);
            }
        }

        private async Task HandleExecuteStep(UIMessage message)
        {
            try
            {
                var payload = DeserializePayload<ExecuteStepPayload>(message.Payload);

                if (!_sessions.TryGetValue(message.SessionId, out var session))
                {
                    await SendErrorResponse(message.MessageId, "Session not found");
                    return;
                }

                var step = session.Steps.FirstOrDefault(s => s.StepId == payload.StepId);
                if (step == null)
                {
                    await SendErrorResponse(message.MessageId, $"Step not found: {payload.StepId}");
                    return;
                }

                LoggingService.Info($"ExecuteStep: StepId={payload.StepId}, StepNumber={payload.StepNumber}, WaitForCompletion={payload.WaitForCompletion}", "SequentialMode");

                // Update UI to highlight current step
                if (_mainViewModel != null && Application.Current != null)
                {
                    await Application.Current.Dispatcher.InvokeAsync(() =>
                    {
                        try { _mainViewModel.CurrentStep = payload.StepNumber > 0 ? payload.StepNumber : 1; }
                        catch (Exception uiEx) { LoggingService.Warn($"Failed to update UI current step: {uiEx.Message}", "SequentialMode"); }
                    });
                }

                // Notify that the step has started
                var startedMsg = new UIMessage
                {
                    SessionId = message.SessionId,
                    Type = UIMessageType.StepStarted,
                    Payload = new StepResultPayload
                    {
                        StepId = step.StepId,
                        StepNumber = payload.StepNumber,
                        Success = true,
                        Message = "Step started",
                        StartTime = DateTime.UtcNow
                    },
                    // Use the original request MessageId so the client can correlate this ack
                    MessageId = message.MessageId
                };
                await SendMessage(startedMsg);

                var requestMessageId = message.MessageId;
                var stepStart = DateTime.UtcNow;

                // Execute the step script asynchronously
                _ = Task.Run(async () =>
                {
                    try
                    {
                        Dictionary<string, object> outputData = null;
                        bool success = true;
                        string resultMessage = "Completed";

                        using (var runspace = RunspaceFactory.CreateRunspace())
                        {
                            runspace.Open();
                            using (var ps = PowerShell.Create())
                            {
                                ps.Runspace = runspace;

                                // Preferences to ensure non-silent streams
                                ps.AddScript("$InformationPreference='Continue'; $VerbosePreference='Continue'; $DebugPreference='Continue';");

                                // Add the step script content
                                var scriptText = step.ExecutionScript ?? string.Empty;
                                ps.AddScript(scriptText, useLocalScope: false);

                                // Build StepData hashtable
                                var stepData = new Hashtable();
                                if (payload.StepParameters != null)
                                {
                                    foreach (var kv in payload.StepParameters)
                                    {
                                        stepData[kv.Key] = kv.Value;
                                    }
                                }

                                // Build WizardContext hashtable
                                var wizardVars = new Hashtable();
                                foreach (var kv in session.Variables)
                                {
                                    wizardVars[kv.Key] = kv.Value;
                                }
                                var wizardContext = new Hashtable
                                {
                                    { "SessionId", session.SessionId },
                                    { "Name", session.Name },
                                    { "CreatedAt", session.CreatedAt },
                                    { "Variables", wizardVars }
                                };

                                // Pass named parameters expected by script's param()
                                ps.AddParameter("StepData", stepData);
                                ps.AddParameter("WizardContext", wizardContext);

                                // Wire up streams to ConsoleOutput messages
                                ps.Streams.Information.DataAdded += (s, e) =>
                                {
                                    try
                                    {
                                        var rec = ps.Streams.Information[e.Index];
                                        var text = rec?.MessageData?.ToString() ?? rec?.ToString();
                                        if (!string.IsNullOrEmpty(text))
                                        {
                                            var co = new UIMessage
                                            {
                                                SessionId = message.SessionId,
                                                Type = UIMessageType.ConsoleOutput,
                                                Payload = new ConsoleOutputPayload { Level = "INFO", Message = text, Source = "PowerShell" }
                                            };
                                            var _ = SendMessage(co);
                                        }
                                    }
                                    catch { }
                                };
                                ps.Streams.Warning.DataAdded += (s, e) =>
                                {
                                    try
                                    {
                                        var text = ps.Streams.Warning[e.Index].ToString();
                                        var co = new UIMessage
                                        {
                                            SessionId = message.SessionId,
                                            Type = UIMessageType.ConsoleOutput,
                                            Payload = new ConsoleOutputPayload { Level = "WARNING", Message = text, Source = "PowerShell" }
                                        };
                                        var _ = SendMessage(co);
                                    }
                                    catch { }
                                };
                                ps.Streams.Verbose.DataAdded += (s, e) =>
                                {
                                    try
                                    {
                                        var text = ps.Streams.Verbose[e.Index].ToString();
                                        var co = new UIMessage
                                        {
                                            SessionId = message.SessionId,
                                            Type = UIMessageType.ConsoleOutput,
                                            Payload = new ConsoleOutputPayload { Level = "VERBOSE", Message = text, Source = "PowerShell" }
                                        };
                                        var _ = SendMessage(co);
                                    }
                                    catch { }
                                };
                                ps.Streams.Debug.DataAdded += (s, e) =>
                                {
                                    try
                                    {
                                        var text = ps.Streams.Debug[e.Index].ToString();
                                        var co = new UIMessage
                                        {
                                            SessionId = message.SessionId,
                                            Type = UIMessageType.ConsoleOutput,
                                            Payload = new ConsoleOutputPayload { Level = "DEBUG", Message = text, Source = "PowerShell" }
                                        };
                                        var _ = SendMessage(co);
                                    }
                                    catch { }
                                };
                                ps.Streams.Error.DataAdded += (s, e) =>
                                {
                                    try
                                    {
                                        var text = ps.Streams.Error[e.Index].ToString();
                                        var co = new UIMessage
                                        {
                                            SessionId = message.SessionId,
                                            Type = UIMessageType.ConsoleOutput,
                                            Payload = new ConsoleOutputPayload { Level = "ERROR", Message = text, Source = "PowerShell" }
                                        };
                                        var _ = SendMessage(co);
                                    }
                                    catch { }
                                };

                                var output = new PSDataCollection<PSObject>();
                                output.DataAdded += (s, e) =>
                                {
                                    try
                                    {
                                        var o = output[e.Index];
                                        if (o != null)
                                        {
                                            var co = new UIMessage
                                            {
                                                SessionId = message.SessionId,
                                                Type = UIMessageType.ConsoleOutput,
                                                Payload = new ConsoleOutputPayload { Level = "OUTPUT", Message = o.ToString(), Source = "PowerShell" }
                                            };
                                            var _ = SendMessage(co);
                                        }
                                    }
                                    catch { }
                                };

                                var asyncResult = ps.BeginInvoke<PSObject, PSObject>(null, output);
                                var results = ps.EndInvoke(asyncResult);

                                // Determine success based on error stream
                                if (ps.Streams.Error != null && ps.Streams.Error.Count > 0)
                                {
                                    success = false;
                                    resultMessage = string.Join(Environment.NewLine, ps.Streams.Error.Select(er => er.ToString()));
                                }

                                // Extract output dictionary if provided
                                if (results != null && results.Count > 0)
                                {
                                    var last = results[results.Count - 1];
                                    outputData = ConvertOutputToDictionary(last);
                                }

                                // Update session variables with any returned data
                                if (outputData != null)
                                {
                                    foreach (var kv in outputData)
                                    {
                                        session.Variables[kv.Key] = kv.Value;
                                    }
                                }
                            }
                        }

                        var end = DateTime.UtcNow;
                        var resultPayload = new StepResultPayload
                        {
                            StepId = step.StepId,
                            StepNumber = payload.StepNumber,
                            Success = success,
                            OutputData = outputData,
                            Message = resultMessage,
                            StartTime = stepStart,
                            EndTime = end,
                            Duration = end - stepStart
                        };

                        // Reflect completion in UI by moving selection forward
                        if (_mainViewModel != null && Application.Current != null)
                        {
                            try
                            {
                                await Application.Current.Dispatcher.InvokeAsync(() =>
                                {
                                    try { _mainViewModel.CurrentStep = payload.StepNumber + 1; } catch { }
                                });
                            }
                            catch { }
                        }

                        var completionMessage = new UIMessage
                        {
                            SessionId = message.SessionId,
                            Type = success ? UIMessageType.StepCompleted : UIMessageType.StepFailed,
                            Payload = resultPayload,
                            MessageId = requestMessageId
                        };
                        await SendMessage(completionMessage);

                        // Notify UI that dynamic step execution finished to reset running state and refresh navigation
                        if (_mainViewModel != null && Application.Current != null)
                        {
                            try
                            {
                                await Application.Current.Dispatcher.InvokeAsync(() =>
                                {
                                    try
                                    {
                                        LoggingService.Debug($"Invoking OnDynamicStepExecutionFinished for step {payload.StepNumber}, success={success}", "SequentialMode");
                                        _mainViewModel.OnDynamicStepExecutionFinished(payload.StepNumber, success);
                                    }
                                    catch (Exception uiEx)
                                    {
                                        LoggingService.Warn($"OnDynamicStepExecutionFinished threw: {uiEx.Message}", "SequentialMode");
                                    }
                                });
                            }
                            catch (Exception dispEx)
                            {
                                LoggingService.Warn($"Dispatcher invoke failed for OnDynamicStepExecutionFinished: {dispEx.Message}", "SequentialMode");
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        LoggingService.Error("Unhandled error during step execution", ex, "SequentialMode");
                        var end = DateTime.UtcNow;
                        var failPayload = new StepResultPayload
                        {
                            StepId = step.StepId,
                            StepNumber = payload.StepNumber,
                            Success = false,
                            OutputData = null,
                            Message = ex.Message,
                            StartTime = stepStart,
                            EndTime = end,
                            Duration = end - stepStart
                        };
                        var failMessage = new UIMessage
                        {
                            SessionId = message.SessionId,
                            Type = UIMessageType.StepFailed,
                            Payload = failPayload,
                            MessageId = requestMessageId
                        };
                        await SendMessage(failMessage);

                        // Ensure UI is updated on failure as well
                        if (_mainViewModel != null && Application.Current != null)
                        {
                            try
                            {
                                await Application.Current.Dispatcher.InvokeAsync(() =>
                                {
                                    try
                                    {
                                        LoggingService.Debug($"Invoking OnDynamicStepExecutionFinished for step {payload.StepNumber}, success=false (failure path)", "SequentialMode");
                                        _mainViewModel.OnDynamicStepExecutionFinished(payload.StepNumber, false);
                                    }
                                    catch (Exception uiEx)
                                    {
                                        LoggingService.Warn($"OnDynamicStepExecutionFinished threw (failure path): {uiEx.Message}", "SequentialMode");
                                    }
                                });
                            }
                            catch (Exception dispEx)
                            {
                                LoggingService.Warn($"Dispatcher invoke failed for OnDynamicStepExecutionFinished (failure path): {dispEx.Message}", "SequentialMode");
                            }
                        }
                    }
                });
            }
            catch (Exception ex)
            {
                LoggingService.Error("Failed to handle ExecuteStep", ex, "SequentialMode");
                await SendErrorResponse(message.MessageId, ex.Message);
            }
        }

        // Helper to convert PowerShell output to a dictionary (supports Hashtable and PSCustomObject)
        private static Dictionary<string, object> ConvertOutputToDictionary(PSObject obj)
        {
            if (obj == null) return null;
            try
            {
                var baseObj = obj.BaseObject;
                var idict = baseObj as IDictionary;
                if (idict != null)
                {
                    var dict = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);
                    foreach (DictionaryEntry de in idict)
                    {
                        var key = de.Key != null ? de.Key.ToString() : null;
                        if (!string.IsNullOrEmpty(key)) dict[key] = de.Value;
                    }
                    return dict;
                }

                // Try reading PSObject properties
                if (obj.Properties != null && obj.Properties.Any())
                {
                    var dict = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);
                    foreach (var p in obj.Properties)
                    {
                        if (!string.IsNullOrEmpty(p.Name)) dict[p.Name] = p.Value;
                    }
                    return dict;
                }
            }
            catch { }
            return null;
        }

        private async Task HandleSetVariable(UIMessage message)
        {
            try
            {
                var payload = DeserializePayload<VariablePayload>(message.Payload);
                
                if (_sessions.TryGetValue(message.SessionId, out var session))
                {
                    session.Variables[payload.Name] = payload.Value;
                    LoggingService.Debug($"Variable set: {payload.Name} = {payload.Value}", "SequentialMode");
                    
                    await SendMessage(new UIMessage
                    {
                        SessionId = message.SessionId,
                        Type = UIMessageType.SetVariable,
                        Payload = new { Success = true },
                        MessageId = message.MessageId
                    });
                }
                else
                {
                    await SendErrorResponse(message.MessageId, "Session not found");
                }
            }
            catch (Exception ex)
            {
                LoggingService.Error("Failed to set variable", ex, "SequentialMode");
                await SendErrorResponse(message.MessageId, ex.Message);
            }
        }

        private async Task HandleGetVariable(UIMessage message)
        {
            try
            {
                var payload = DeserializePayload<VariablePayload>(message.Payload);
                
                if (_sessions.TryGetValue(message.SessionId, out var session))
                {
                    var value = session.Variables.TryGetValue(payload.Name, out var val) ? val : null;
                    
                    await SendMessage(new UIMessage
                    {
                        SessionId = message.SessionId,
                        Type = UIMessageType.GetVariable,
                        Payload = new VariablePayload { Name = payload.Name, Value = value },
                        MessageId = message.MessageId
                    });
                }
                else
                {
                    await SendErrorResponse(message.MessageId, "Session not found");
                }
            }
            catch (Exception ex)
            {
                LoggingService.Error("Failed to get variable", ex, "SequentialMode");
                await SendErrorResponse(message.MessageId, ex.Message);
            }
        }

        private async Task HandleCloseSession(UIMessage message)
        {
            try
            {
                if (_sessions.TryGetValue(message.SessionId, out var session))
                {
                    LoggingService.Info($"Closing session {session.SessionId}", "SequentialMode");
                    _sessions.Remove(message.SessionId);
                    
                    // Close main window if this was the last session
                    if (_sessions.Count == 0 && _mainWindow != null)
                    {
                        await Application.Current.Dispatcher.InvokeAsync(() =>
                        {
                            _mainWindow.Close();
                        });
                    }
                }
                
                await SendMessage(new UIMessage
                {
                    SessionId = message.SessionId,
                    Type = UIMessageType.CloseSession,
                    Payload = new { Success = true },
                    MessageId = message.MessageId
                });
            }
            catch (Exception ex)
            {
                LoggingService.Error("Failed to close session", ex, "SequentialMode");
                await SendErrorResponse(message.MessageId, ex.Message);
            }
        }

        private async Task SendMessage(UIMessage message)
        {
            try
            {
                // Sign the message
                SecurityService.SignMessage(message, _sessionCertificate, _sessionSecret);
                
                // Serialize with Type as a string for PowerShell client compatibility using built-in serializer
                string json;
                using (var ms = new MemoryStream())
                {
                    var wireMessage = new
                    {
                        MessageId = message.MessageId,
                        SessionId = message.SessionId,
                        Type = message.Type.ToString(),
                        Timestamp = message.Timestamp,
                        Payload = message.Payload,
                        Error = message.Error,
                        AuthToken = message.AuthToken,
                        ClientProcessId = message.ClientProcessId,
                        MessageHash = message.MessageHash
                    };
                    var serializer = new DataContractJsonSerializer(wireMessage.GetType());
                    serializer.WriteObject(ms, wireMessage);
                    json = Encoding.UTF8.GetString(ms.ToArray()) + "\n";
                }
                var bytes = Encoding.UTF8.GetBytes(json);
                
                await _sendLock.WaitAsync();
                try
                {
                    await _pipeServer.WriteAsync(bytes, 0, bytes.Length);
                    await _pipeServer.FlushAsync();
                }
                finally
                {
                    _sendLock.Release();
                }
                
                LoggingService.Debug($"Sent message: {message.Type} ({message.MessageId})", "SequentialMode");
            }
            catch (Exception ex)
            {
                LoggingService.Error($"Failed to send message {message.MessageId}", ex, "SequentialMode");
            }
        }

        private async Task SendErrorResponse(string messageId, string error)
        {
            var response = new UIMessage
            {
                Type = UIMessageType.SessionError,
                Error = error
            };
            
            if (!string.IsNullOrEmpty(messageId))
            {
                response.MessageId = messageId;
            }
            
            await SendMessage(response);
        }

        public void Dispose()
        {
            try
            {
                _cancellationTokenSource?.Cancel();
                _listenerTask?.Wait(TimeSpan.FromSeconds(5));
                _pipeServer?.Dispose();
                _sessionCertificate?.Dispose();
                _cancellationTokenSource?.Dispose();
                
                LoggingService.Info("Sequential mode service disposed", "SequentialMode");
            }
            catch (Exception ex)
            {
                LoggingService.Error("Error disposing sequential mode service", ex, "SequentialMode");
            }
        }
    }

    public class WizardSession
    {
        public string SessionId { get; set; }
        public string Name { get; set; }
        public string ScriptPath { get; set; }
        public bool DebugMode { get; set; }
        public DateTime CreatedAt { get; set; }
        public Dictionary<string, object> Variables { get; set; } = new Dictionary<string, object>();
        public List<DynamicWizardStep> Steps { get; set; } = new List<DynamicWizardStep>();
    }

    public class DynamicWizardStep
    {
        public string StepId { get; set; }
        public string Title { get; set; }
        public string PageType { get; set; }
        public string Description { get; set; }
        public int Position { get; set; }
        public List<ParameterDefinition> Parameters { get; set; }
        public string ExecutionScript { get; set; }
        public Dictionary<string, object> Properties { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}

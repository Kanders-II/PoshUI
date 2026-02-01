// Copyright (c) 2025 A Solution IT LLC. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
using System;
using System.Collections.Generic;

namespace Launcher.Models
{
    [Serializable]
    public class UIMessage
    {
        public string MessageId { get; set; } = Guid.NewGuid().ToString();
        public string SessionId { get; set; }
        public UIMessageType Type { get; set; }
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
        public object Payload { get; set; }
        public string Error { get; set; }
        public string AuthToken { get; set; }
        public int ClientProcessId { get; set; }
        public string MessageHash { get; set; }
    }

    public enum UIMessageType
    {
        // Session Management
        CreateSession,
        SessionCreated,
        SessionError,
        
        // Step Management
        AddStep,
        InsertStep,
        RemoveStep,
        ModifyStep,
        GetStepInfo,
        
        // Step Execution
        ExecuteStep,
        StepStarted,
        StepProgress,
        StepCompleted,
        StepFailed,
        
        // Console Operations
        ShowExecutionConsole,
        HideExecutionConsole,
        ConsoleOutput,
        UpdateProgress,
        
        // State Management
        SetVariable,
        GetVariable,
        GetAllVariables,
        
        // Control Flow
        PauseExecution,
        ResumeExecution,
        CancelSession,
        CloseSession,
        
        // Parameter Management
        AddParameter,
        RemoveParameter,
        UpdateParameter,
        
        // UI Control
        ShowStep,
        HideStep,
        EnableStep,
        DisableStep,
        RefreshUI
    }

    [Serializable]
    public class CreateSessionPayload
    {
        public string ScriptPath { get; set; }
        public string SessionName { get; set; }
        public Dictionary<string, object> InitialParameters { get; set; }
        public bool DebugMode { get; set; }
        public string LogPath { get; set; }
        public string ExecutionMode { get; set; } = "Sequential";
    }

    [Serializable]
    public class SessionCreatedPayload
    {
        public string SessionId { get; set; }
        public string PipeName { get; set; }
        public bool Success { get; set; }
        public string Message { get; set; }
        public string Certificate { get; set; } // Base64 encoded certificate
    }

    [Serializable]
    public class AddStepPayload
    {
        public string Title { get; set; }
        public string PageType { get; set; } = "GenericForm";
        public string Description { get; set; }
        public int? Position { get; set; } // null = append to end
        public List<ParameterDefinition> Parameters { get; set; }
        public string ExecutionScript { get; set; }
        public Dictionary<string, object> Properties { get; set; }
        public string StepId { get; set; } = Guid.NewGuid().ToString();
    }

    [Serializable]
    public class ParameterDefinition
    {
        public string Name { get; set; }
        public string Type { get; set; } // "String", "Bool", "ValidateSet", "SecureString", "Path", etc.
        public string Label { get; set; }
        public bool Mandatory { get; set; }
        public object DefaultValue { get; set; }
        public string ValidationPattern { get; set; }
        public string[] ValidateSetChoices { get; set; }
        public string HelpText { get; set; }
        public string PathType { get; set; } // "File", "Folder" for Path type
        public string FileFilter { get; set; } // For Path type
        public string CsvPath { get; set; } // For CsvDropdown type
        public string ValueColumn { get; set; } // For CsvDropdown type
        public string DisplayColumn { get; set; } // For CsvDropdown type
        public Dictionary<string, object> TypeSpecificProperties { get; set; }
    }

    [Serializable]
    public class ExecuteStepPayload
    {
        public string StepId { get; set; }
        public int StepNumber { get; set; }
        public Dictionary<string, object> StepParameters { get; set; }
        public bool SkipUI { get; set; }
        public bool WaitForCompletion { get; set; } = true;
    }

    [Serializable]
    public class StepResultPayload
    {
        public string StepId { get; set; }
        public int StepNumber { get; set; }
        public bool Success { get; set; }
        public Dictionary<string, object> OutputData { get; set; }
        public string Message { get; set; }
        public TimeSpan Duration { get; set; }
        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }
    }

    [Serializable]
    public class ConsoleOutputPayload
    {
        public string Level { get; set; } // "INFO", "ERROR", "WARNING", "DEBUG", "VERBOSE"
        public string Message { get; set; }
        public DateTime Timestamp { get; set; } = DateTime.Now;
        public string Source { get; set; } // "PowerShell", "System", "User"
    }

    [Serializable]
    public class ProgressUpdatePayload
    {
        public int Percent { get; set; }
        public string Status { get; set; }
        public string CurrentOperation { get; set; }
        public bool IsIndeterminate { get; set; }
    }

    [Serializable]
    public class VariablePayload
    {
        public string Name { get; set; }
        public object Value { get; set; }
        public string Type { get; set; }
    }
}

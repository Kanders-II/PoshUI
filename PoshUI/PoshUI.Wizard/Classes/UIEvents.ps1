# UIEvents.ps1 - Event system for UI components

<#
.SYNOPSIS
Event system for PoshUI components.

.DESCRIPTION
Provides publish-subscribe pattern for UI events, allowing components
to communicate without tight coupling. Supports event filtering,
priority handling, and async execution.

.NOTES
Company: A Solution IT LLC
Version: 2.0.0
#>

class UIEvents {
    # Event registry: EventName -> Array of handlers
    static [hashtable]$EventHandlers = @{}
    static [hashtable]$EventHistory = @{}
    static [bool]$EnableHistory = $false
    
    # Subscribe to an event
    static [void] Subscribe([string]$EventName, [scriptblock]$Handler) {
        [UIEvents]::Subscribe($EventName, $Handler, 0)
    }
    
    # Subscribe with priority (higher priority = executed first)
    static [void] Subscribe([string]$EventName, [scriptblock]$Handler, [int]$Priority) {
        if ([string]::IsNullOrWhiteSpace($EventName)) {
            throw "Event name cannot be empty"
        }
        
        if ($null -eq $Handler) {
            throw "Handler cannot be null"
        }
        
        # Initialize event handler list if needed
        if (-not [UIEvents]::EventHandlers.ContainsKey($EventName)) {
            [UIEvents]::EventHandlers[$EventName] = @()
        }
        
        # Create handler entry
        $handlerEntry = @{
            Handler = $Handler
            Priority = $Priority
            SubscribedAt = Get-Date
        }
        
        # Add and sort by priority (descending)
        [UIEvents]::EventHandlers[$EventName] += $handlerEntry
        [UIEvents]::EventHandlers[$EventName] = [UIEvents]::EventHandlers[$EventName] | 
            Sort-Object -Property Priority -Descending
    }
    
    # Unsubscribe from an event
    static [void] Unsubscribe([string]$EventName, [scriptblock]$Handler) {
        if (-not [UIEvents]::EventHandlers.ContainsKey($EventName)) {
            return
        }
        
        # Remove matching handlers
        [UIEvents]::EventHandlers[$EventName] = [UIEvents]::EventHandlers[$EventName] | 
            Where-Object { $_.Handler.ToString() -ne $Handler.ToString() }
        
        # Clean up empty event lists
        if ([UIEvents]::EventHandlers[$EventName].Count -eq 0) {
            [UIEvents]::EventHandlers.Remove($EventName)
        }
    }
    
    # Unsubscribe all handlers for an event
    static [void] UnsubscribeAll([string]$EventName) {
        if ([UIEvents]::EventHandlers.ContainsKey($EventName)) {
            [UIEvents]::EventHandlers.Remove($EventName)
        }
    }
    
    # Publish an event with data
    static [void] Publish([string]$EventName, [object]$Data) {
        if ([string]::IsNullOrWhiteSpace($EventName)) {
            throw "Event name cannot be empty"
        }
        
        # Record event in history if enabled
        if ([UIEvents]::EnableHistory) {
            if (-not [UIEvents]::EventHistory.ContainsKey($EventName)) {
                [UIEvents]::EventHistory[$EventName] = @()
            }
            [UIEvents]::EventHistory[$EventName] += @{
                Timestamp = Get-Date
                Data = $Data
            }
        }
        
        # Execute handlers if any exist
        if (-not [UIEvents]::EventHandlers.ContainsKey($EventName)) {
            return
        }
        
        # Execute each handler in priority order
        foreach ($handlerEntry in [UIEvents]::EventHandlers[$EventName]) {
            try {
                $eventArgs = @{
                    EventName = $EventName
                    Data = $Data
                    Timestamp = Get-Date
                }
                
                # Invoke handler with event args
                & $handlerEntry.Handler $eventArgs
            }
            catch {
                Write-Warning "Error executing handler for event '$EventName': $($_.Exception.Message)"
            }
        }
    }
    
    # Publish event asynchronously (fire and forget)
    static [void] PublishAsync([string]$EventName, [object]$Data) {
        # Use runspace for async execution
        $runspace = [runspacefactory]::CreateRunspace()
        $runspace.Open()
        
        $powershell = [powershell]::Create()
        $powershell.Runspace = $runspace
        
        [void]$powershell.AddScript({
            param($EventName, $Data)
            [UIEvents]::Publish($EventName, $Data)
        }).AddArgument($EventName).AddArgument($Data)
        
        [void]$powershell.BeginInvoke()
    }
    
    # Get list of registered events
    static [string[]] GetRegisteredEvents() {
        return [UIEvents]::EventHandlers.Keys
    }
    
    # Get handler count for an event
    static [int] GetHandlerCount([string]$EventName) {
        if (-not [UIEvents]::EventHandlers.ContainsKey($EventName)) {
            return 0
        }
        return [UIEvents]::EventHandlers[$EventName].Count
    }
    
    # Clear all event handlers
    static [void] ClearAll() {
        [UIEvents]::EventHandlers.Clear()
        [UIEvents]::EventHistory.Clear()
    }
    
    # Enable/disable event history
    static [void] SetHistoryEnabled([bool]$Enabled) {
        [UIEvents]::EnableHistory = $Enabled
        if (-not $Enabled) {
            [UIEvents]::EventHistory.Clear()
        }
    }
    
    # Get event history for a specific event
    static [array] GetEventHistory([string]$EventName) {
        if ([UIEvents]::EventHistory.ContainsKey($EventName)) {
            return [UIEvents]::EventHistory[$EventName]
        }
        return @()
    }
}

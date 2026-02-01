# Progress Reporting Guide

Understanding how to report progress in workflow tasks is essential for creating responsive, professional UIs. This guide explains the two progress reporting mechanisms and when to use each.

## Quick Decision Tree

```
Do you know the exact percentage complete for each step?
│
├─ YES: Use UpdateProgress()
│   └─ Explicitly set progress: 10%, 50%, 100%
│   └─ More precise, requires more code
│
└─ NO: Use WriteOutput()
    └─ Progress auto-advances with each call
    └─ Simpler, less code needed
```

## Method 1: Auto-Progress with WriteOutput()

The simplest approach. Progress automatically advances as you report status updates.

### How It Works

Each call to `WriteOutput()` automatically increments the progress bar (5% → 15% → 25% → ... → 90% → 100%).

```powershell
Add-UIWorkflowTask -Step 'Execution' -Name 'Configure' -Title 'Configuring System' `
    -ScriptBlock {
        # Progress: 0%
        $PoshUIWorkflow.WriteOutput("Step 1: Checking prerequisites...", "INFO")
        # Progress: 15%
        
        Start-Sleep -Seconds 2
        $PoshUIWorkflow.WriteOutput("Step 2: Installing components...", "INFO")
        # Progress: 25%
        
        Start-Sleep -Seconds 2
        $PoshUIWorkflow.WriteOutput("Step 3: Configuring services...", "INFO")
        # Progress: 35%
        
        Start-Sleep -Seconds 2
        $PoshUIWorkflow.WriteOutput("Step 4: Finalizing...", "INFO")
        # Progress: 45%
        
        # Task completes automatically at 100%
    }
```

### When to Use WriteOutput()

- ✅ Simple sequential tasks
- ✅ Don't know exact step durations
- ✅ Want minimal code
- ✅ Each step takes roughly equal time

### Parameters

```powershell
$PoshUIWorkflow.WriteOutput("Message text", "Level")
```

| Parameter | Values | Description |
|-----------|--------|-------------|
| Message | string | Text to display in console and log |
| Level | "INFO", "WARN", "ERROR" | Severity level for logging |

### Example: Installation Task

```powershell
Add-UIWorkflowTask -Step 'Execution' -Name 'Install' -Title 'Installing Software' `
    -ScriptBlock {
        $PoshUIWorkflow.WriteOutput("Downloading installer...", "INFO")
        # Simulate download
        Start-Sleep -Seconds 2
        
        $PoshUIWorkflow.WriteOutput("Verifying checksum...", "INFO")
        # Simulate verification
        Start-Sleep -Seconds 1
        
        $PoshUIWorkflow.WriteOutput("Running installer...", "INFO")
        # Run installer
        Start-Sleep -Seconds 3
        
        $PoshUIWorkflow.WriteOutput("Installation complete", "INFO")
        # Progress: 100% (auto)
    }
```

## Method 2: Manual Progress with UpdateProgress()

Explicit control over progress percentage. Use when you know step durations or need precise control.

### How It Works

You explicitly set the progress percentage (0-100) at key points.

```powershell
Add-UIWorkflowTask -Step 'Execution' -Name 'Deploy' -Title 'Deploying Application' `
    -ScriptBlock {
        # Progress: 0%
        $PoshUIWorkflow.UpdateProgress(10, "Validating configuration...")
        # Do validation work
        
        # Progress: 10%
        $PoshUIWorkflow.UpdateProgress(40, "Downloading files...")
        # Do download work
        
        # Progress: 40%
        $PoshUIWorkflow.UpdateProgress(70, "Installing application...")
        # Do installation work
        
        # Progress: 70%
        $PoshUIWorkflow.UpdateProgress(100, "Deployment complete")
        # Progress: 100%
    }
```

### When to Use UpdateProgress()

- ✅ Know step durations
- ✅ Some steps take much longer than others
- ✅ Need precise progress tracking
- ✅ Want to show accurate completion estimates

### Parameters

```powershell
$PoshUIWorkflow.UpdateProgress(Percent, "Message")
```

| Parameter | Type | Range | Description |
|-----------|------|-------|-------------|
| Percent | int | 0-100 | Progress percentage |
| Message | string | - | Status message to display |

### Example: Multi-Phase Deployment

```powershell
Add-UIWorkflowTask -Step 'Execution' -Name 'Deploy' -Title 'Multi-Phase Deployment' `
    -ScriptBlock {
        # Phase 1: Preparation (10%)
        $PoshUIWorkflow.UpdateProgress(5, "Preparing environment...")
        Start-Sleep -Seconds 1
        
        # Phase 2: Download (40%)
        $PoshUIWorkflow.UpdateProgress(15, "Downloading files...")
        Start-Sleep -Seconds 3
        $PoshUIWorkflow.UpdateProgress(30, "Verifying files...")
        Start-Sleep -Seconds 2
        
        # Phase 3: Installation (70%)
        $PoshUIWorkflow.UpdateProgress(50, "Installing components...")
        Start-Sleep -Seconds 3
        $PoshUIWorkflow.UpdateProgress(65, "Configuring services...")
        Start-Sleep -Seconds 2
        
        # Phase 4: Finalization (100%)
        $PoshUIWorkflow.UpdateProgress(85, "Running post-install checks...")
        Start-Sleep -Seconds 1
        $PoshUIWorkflow.UpdateProgress(100, "Deployment complete")
    }
```

## Critical Rule: Don't Mix Methods

**Never mix `WriteOutput()` and `UpdateProgress()` in the same task.**

### What Happens If You Mix

```powershell
# WRONG - Don't do this!
Add-UIWorkflowTask -Step 'Execution' -Name 'BadTask' -ScriptBlock {
    $PoshUIWorkflow.WriteOutput("Step 1", "INFO")
    # Progress: 15% (auto)
    
    $PoshUIWorkflow.UpdateProgress(50, "Manual control")
    # Switches to manual mode - auto-progress DISABLED
    
    $PoshUIWorkflow.WriteOutput("Step 2", "INFO")
    # Progress: 50% (stuck - no auto-increment)
    # This is confusing and looks broken!
}
```

### Why This Matters

When you call `UpdateProgress()`, it switches the task into **manual progress mode**. After that, `WriteOutput()` calls no longer auto-increment the progress bar. The progress gets stuck at whatever you last set it to.

## Comparison Table

| Aspect | WriteOutput() | UpdateProgress() |
|--------|---------------|------------------|
| **Simplicity** | Very simple | More code |
| **Precision** | Less precise | Very precise |
| **Best for** | Simple tasks | Complex tasks |
| **Progress increment** | Automatic | Manual |
| **Code lines** | Fewer | More |
| **Learning curve** | Easy | Moderate |

## Real-World Examples

### Example 1: Simple Service Restart (WriteOutput)

```powershell
Add-UIWorkflowTask -Step 'Execution' -Name 'RestartService' -Title 'Restarting Service' `
    -ScriptBlock {
        $PoshUIWorkflow.WriteOutput("Stopping service...", "INFO")
        Stop-Service -Name 'MyService' -Force
        
        $PoshUIWorkflow.WriteOutput("Waiting for service to stop...", "INFO")
        Start-Sleep -Seconds 2
        
        $PoshUIWorkflow.WriteOutput("Starting service...", "INFO")
        Start-Service -Name 'MyService'
        
        $PoshUIWorkflow.WriteOutput("Service restarted successfully", "INFO")
    }
```

### Example 2: Large File Copy (UpdateProgress)

```powershell
Add-UIWorkflowTask -Step 'Execution' -Name 'CopyFiles' -Title 'Copying Large Files' `
    -ScriptBlock {
        $sourceDir = "D:\SourceData"
        $destDir = "E:\BackupData"
        $files = Get-ChildItem -Path $sourceDir -File
        $totalFiles = $files.Count
        
        $PoshUIWorkflow.UpdateProgress(5, "Starting copy operation...")
        
        $copied = 0
        foreach ($file in $files) {
            Copy-Item -Path $file.FullName -Destination $destDir -Force
            $copied++
            
            $percent = [math]::Min(95, 5 + (($copied / $totalFiles) * 90))
            $PoshUIWorkflow.UpdateProgress($percent, "Copied $copied of $totalFiles files")
        }
        
        $PoshUIWorkflow.UpdateProgress(100, "Copy operation complete")
    }
```

### Example 3: Complex Deployment (UpdateProgress)

```powershell
Add-UIWorkflowTask -Step 'Execution' -Name 'Deploy' -Title 'Application Deployment' `
    -ScriptBlock {
        # Validation phase
        $PoshUIWorkflow.UpdateProgress(5, "Validating prerequisites...")
        if (-not (Test-Path "C:\AppData")) {
            throw "Required directory not found"
        }
        
        # Backup phase
        $PoshUIWorkflow.UpdateProgress(20, "Creating backup...")
        Copy-Item -Path "C:\AppData" -Destination "C:\AppData.backup" -Recurse
        
        # Download phase
        $PoshUIWorkflow.UpdateProgress(40, "Downloading application...")
        Invoke-WebRequest -Uri "https://example.com/app.zip" -OutFile "C:\app.zip"
        
        # Extract phase
        $PoshUIWorkflow.UpdateProgress(60, "Extracting files...")
        Expand-Archive -Path "C:\app.zip" -DestinationPath "C:\AppData" -Force
        
        # Configuration phase
        $PoshUIWorkflow.UpdateProgress(80, "Configuring application...")
        & "C:\AppData\configure.ps1"
        
        # Verification phase
        $PoshUIWorkflow.UpdateProgress(95, "Verifying installation...")
        if (-not (Test-Path "C:\AppData\app.exe")) {
            throw "Installation verification failed"
        }
        
        $PoshUIWorkflow.UpdateProgress(100, "Deployment successful")
    }
```

## Best Practices

1. **Choose one method per task** - Don't mix WriteOutput() and UpdateProgress()
2. **Use meaningful messages** - Help users understand what's happening
3. **Update frequently** - Show progress at least every few seconds
4. **Be realistic** - Don't jump from 0% to 90% instantly
5. **End at 100%** - Always complete with 100% progress
6. **Log important events** - Use "WARN" or "ERROR" levels for issues

## Troubleshooting

### Progress bar not moving
- Check that you're calling WriteOutput() or UpdateProgress()
- Verify you're not mixing both methods
- Ensure the task is actually executing

### Progress jumps around
- You're mixing WriteOutput() and UpdateProgress()
- Solution: Pick one method and stick with it

### Progress gets stuck at 50%
- You called UpdateProgress() once and then used WriteOutput()
- Solution: Use only UpdateProgress() for the rest of the task

Next: [Working with Tasks](./tasks.md) | [Data Passing Patterns](./data-passing.md)

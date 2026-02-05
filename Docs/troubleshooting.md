# Troubleshooting Guide

Common issues and solutions for PoshUI development.

## Cards Not Showing in Dashboard

### Symptom
You add cards with `Add-UIMetricCard` or other card cmdlets, but they don't appear in the dashboard.

### Diagnostic Steps

1. **Verify the step name matches:**
   ```powershell
   # Check what steps exist
   Get-PoshUIDashboard
   
   # Look for your step name in the output
   ```

2. **Verify cards were added:**
   ```powershell
   # Check if cards are in the step
   Get-PoshUIDashboard -StepName 'YourStepName' -IncludeProperties
   ```

3. **Check for errors:**
   ```powershell
   # Run with verbose output
   Add-UIMetricCard -Step 'Dashboard' -Name 'Test' -Title 'Test' -Value 100 -Verbose
   ```

### Common Causes

**Wrong Step Name:**
```powershell
# ❌ Wrong - step name doesn't match
Add-UIStep -Name 'Dashboard' -Title 'Dashboard' -Order 1
Add-UIMetricCard -Step 'DashBoard' -Name 'CPU' -Title 'CPU' -Value 50  # Typo!

# ✅ Correct - names match exactly
Add-UIStep -Name 'Dashboard' -Title 'Dashboard' -Order 1
Add-UIMetricCard -Step 'Dashboard' -Name 'CPU' -Title 'CPU' -Value 50
```

**Step Not Created:**
```powershell
# ❌ Wrong - forgot to create step
New-PoshUIDashboard -Title 'Dashboard'
Add-UIMetricCard -Step 'Dashboard' -Name 'CPU' -Title 'CPU' -Value 50

# ✅ Correct - create step first
New-PoshUIDashboard -Title 'Dashboard'
Add-UIStep -Name 'Dashboard' -Title 'Dashboard' -Order 1
Add-UIMetricCard -Step 'Dashboard' -Name 'CPU' -Title 'CPU' -Value 50
```

**Dashboard Not Initialized:**
```powershell
# ❌ Wrong - forgot to initialize
Add-UIMetricCard -Step 'Dashboard' -Name 'CPU' -Title 'CPU' -Value 50

# ✅ Correct - initialize first
New-PoshUIDashboard -Title 'Dashboard'
Add-UIStep -Name 'Dashboard' -Title 'Dashboard' -Order 1
Add-UIMetricCard -Step 'Dashboard' -Name 'CPU' -Title 'CPU' -Value 50
```

---

## ScriptBlock Errors

### Symptom
Cards with ScriptBlock values show errors or don't update.

### Diagnostic Steps

1. **Enable verbose output:**
   ```powershell
   Add-UIMetricCard -Step 'Dashboard' -Name 'Test' -Title 'Test' `
       -Value { Get-SomeCommand } -Verbose
   ```

2. **Test ScriptBlock independently:**
   ```powershell
   # Test your ScriptBlock outside the card
   $testScript = { (Get-CimInstance Win32_Processor).LoadPercentage }
   & $testScript  # Should return a number
   ```

3. **Check error details:**
   ```powershell
   # Errors are now detailed with suggestions
   Add-UIMetricCard -Step 'Dashboard' -Name 'Bad' -Title 'Bad' `
       -Value { Get-NonExistentCommand } -ErrorAction SilentlyContinue
   
   # Check $Error[0] for full details
   $Error[0].Exception.Message
   ```

### Common Causes

**Command Not Found:**
```powershell
# ❌ Wrong - typo in command
Add-UIMetricCard -Step 'Dashboard' -Name 'CPU' -Title 'CPU' `
    -Value { (Get-CimInstanc Win32_Processor).LoadPercentage }

# ✅ Correct - command spelled correctly
Add-UIMetricCard -Step 'Dashboard' -Name 'CPU' -Title 'CPU' `
    -Value { (Get-CimInstance Win32_Processor).LoadPercentage }
```

**Wrong Data Type Returned:**
```powershell
# ❌ Wrong - returns object, not number
Add-UIMetricCard -Step 'Dashboard' -Name 'CPU' -Title 'CPU' `
    -Value { Get-CimInstance Win32_Processor } -Unit '%'

# ✅ Correct - returns number
Add-UIMetricCard -Step 'Dashboard' -Name 'CPU' -Title 'CPU' `
    -Value { (Get-CimInstance Win32_Processor).LoadPercentage } -Unit '%'
```

**ScriptBlock Returns Null:**
```powershell
# ❌ Wrong - property doesn't exist
Add-UIMetricCard -Step 'Dashboard' -Name 'Test' -Title 'Test' `
    -Value { (Get-Process)[0].NonExistentProperty }

# ✅ Correct - use valid property
Add-UIMetricCard -Step 'Dashboard' -Name 'Test' -Title 'Test' `
    -Value { (Get-Process)[0].CPU }
```

### Error Message Format

New error messages include:
- **Context:** Which cmdlet failed
- **Suggestions:** How to fix the issue
- **Error Details:** Exact PowerShell error
- **Test Command:** How to test the ScriptBlock independently

Example:
```
[Add-UIMetricCard] Failed to execute Value ScriptBlock for MetricCard 'CPU'
Suggestions:
  - Check that the ScriptBlock syntax is valid
  - Ensure all cmdlets/functions used in the ScriptBlock are available
  - Test the ScriptBlock independently: & { (Get-CimInstance Win32_Processor).LoadPercentage }
  - Error details: The term 'Get-CimInstanc' is not recognized...
```

---

## Module Import Issues

### Symptom
`Import-Module` fails or cmdlets are not available.

### Diagnostic Steps

1. **Check module path:**
   ```powershell
   $modulePath = 'D:\PoshUI\PoshUI.Dashboard\PoshUI.Dashboard.psd1'
   Test-Path $modulePath  # Should return True
   ```

2. **Test module manifest:**
   ```powershell
   Test-ModuleManifest -Path $modulePath
   ```

3. **Check for errors:**
   ```powershell
   Import-Module $modulePath -Force -Verbose
   ```

### Common Causes

**Wrong Path:**
```powershell
# ❌ Wrong - relative path from wrong location
Import-Module '.\PoshUI.Dashboard\PoshUI.Dashboard.psd1'

# ✅ Correct - use Join-Path with $PSScriptRoot
$modulePath = Join-Path $PSScriptRoot 'PoshUI\PoshUI.Dashboard\PoshUI.Dashboard.psd1'
Import-Module $modulePath -Force
```

**Files Blocked:**
```powershell
# After downloading, unblock all files
Get-ChildItem -Path 'D:\PoshUI' -Recurse | Unblock-File
```

**Wrong PowerShell Version:**
```powershell
# Check PowerShell version
$PSVersionTable.PSVersion

# PoshUI requires Windows PowerShell 5.1
```

---

## Theme Not Applying

### Symptom
Dashboard appears in wrong theme (Light/Dark).

### Diagnostic Steps

1. **Check theme parameter:**
   ```powershell
   New-PoshUIDashboard -Title 'Dashboard' -Theme 'Dark'
   ```

2. **Verify Windows theme setting:**
   - Settings → Personalization → Colors → Choose your mode

### Solutions

**Use Auto Theme:**
```powershell
# Automatically matches Windows theme
New-PoshUIDashboard -Title 'Dashboard' -Theme 'Auto'
```

**Force Specific Theme:**
```powershell
# Always use dark theme
New-PoshUIDashboard -Title 'Dashboard' -Theme 'Dark'

# Always use light theme
New-PoshUIDashboard -Title 'Dashboard' -Theme 'Light'
```

---

## Performance Issues

### Symptom
Dashboard is slow to load or refresh.

### Diagnostic Steps

1. **Check ScriptBlock execution time:**
   ```powershell
   Measure-Command {
       & { Get-Process | Sort-Object CPU -Descending | Select-Object -First 100 }
   }
   ```

2. **Count data items:**
   ```powershell
   $data = Get-Process
   Write-Host "Returning $($data.Count) items"
   ```

### Solutions

**Limit Data Size:**
```powershell
# ❌ Slow - returns too much data
Add-UITableCard -Step 'Dashboard' -Name 'Processes' -Title 'Processes' `
    -Data { Get-Process }

# ✅ Fast - limit to top 20
Add-UITableCard -Step 'Dashboard' -Name 'Processes' -Title 'Processes' `
    -Data { Get-Process | Select-Object -First 20 Name, CPU, WorkingSet }
```

**Optimize Queries:**
```powershell
# ❌ Slow - multiple WMI queries
Add-UIMetricCard -Step 'Dashboard' -Name 'CPU' -Title 'CPU' `
    -Value { (Get-CimInstance Win32_Processor).LoadPercentage }
Add-UIMetricCard -Step 'Dashboard' -Name 'Memory' -Title 'Memory' `
    -Value { (Get-CimInstance Win32_OperatingSystem).FreePhysicalMemory }

# ✅ Fast - query once, reuse
$os = Get-CimInstance Win32_OperatingSystem
$cpu = Get-CimInstance Win32_Processor

Add-UIMetricCard -Step 'Dashboard' -Name 'CPU' -Title 'CPU' `
    -Value $cpu.LoadPercentage
Add-UIMetricCard -Step 'Dashboard' -Name 'Memory' -Title 'Memory' `
    -Value ($os.FreePhysicalMemory / 1MB)
```

**Use Static Data for Initial Display:**
```powershell
# Fast initial load, then refresh
Add-UIMetricCard -Step 'Dashboard' -Name 'CPU' -Title 'CPU' `
    -Value 0 `
    -RefreshScript { (Get-CimInstance Win32_Processor).LoadPercentage }
```

---

## Chart Data Not Displaying

### Symptom
Chart card shows but no data appears.

### Diagnostic Steps

1. **Verify data format:**
   ```powershell
   $data = @(
       @{ Label = 'A'; Value = 10 }
       @{ Label = 'B'; Value = 20 }
   )
   $data | Format-Table  # Should show Label and Value columns
   ```

2. **Test ScriptBlock:**
   ```powershell
   $chartData = & {
       Get-Process | Select-Object -First 5 | ForEach-Object {
           @{ Label = $_.Name; Value = $_.CPU }
       }
   }
   $chartData | Format-Table
   ```

### Common Causes

**Wrong Data Format:**
```powershell
# ❌ Wrong - missing Label/Value properties
Add-UIChartCard -Step 'Dashboard' -Name 'Chart' -Title 'Chart' `
    -ChartType 'Bar' -Data { Get-Process | Select-Object -First 5 }

# ✅ Correct - proper Label/Value format
Add-UIChartCard -Step 'Dashboard' -Name 'Chart' -Title 'Chart' `
    -ChartType 'Bar' `
    -Data {
        Get-Process | Select-Object -First 5 | ForEach-Object {
            @{ Label = $_.Name; Value = $_.CPU }
        }
    }
```

**Empty Data:**
```powershell
# ❌ Wrong - filter returns no results
Add-UIChartCard -Step 'Dashboard' -Name 'Chart' -Title 'Chart' `
    -ChartType 'Bar' `
    -Data { Get-Process | Where-Object Name -eq 'NonExistent' }

# ✅ Correct - verify data exists
Add-UIChartCard -Step 'Dashboard' -Name 'Chart' -Title 'Chart' `
    -ChartType 'Bar' `
    -Data {
        $procs = Get-Process | Select-Object -First 5
        if ($procs) {
            $procs | ForEach-Object { @{ Label = $_.Name; Value = $_.CPU } }
        } else {
            @( @{ Label = 'No Data'; Value = 0 } )
        }
    }
```

---

## BannerStyle Preset Not Working

### Symptom
Banner doesn't look like expected preset style.

### Diagnostic Steps

1. **Verify BannerStyle parameter:**
   ```powershell
   Add-UIBanner -Step 'Dashboard' -Title 'Test' -BannerStyle 'Gradient' -Verbose
   ```

2. **Check for conflicting parameters:**
   ```powershell
   # BannerConfig overrides preset defaults
   Add-UIBanner -Step 'Dashboard' -Title 'Test' `
       -BannerStyle 'Gradient' `
       -BannerConfig @{ Height = 300 }  # Overrides preset height
   ```

### Solutions

**Use Preset Without Overrides:**
```powershell
# Simple preset usage
Add-UIBanner -Step 'Dashboard' -Title 'Welcome' -BannerStyle 'Hero'
```

**Customize Preset:**
```powershell
# Start with preset, customize specific properties
Add-UIBanner -Step 'Dashboard' -Title 'Welcome' `
    -BannerStyle 'Hero' `
    -BannerConfig @{
        Height = 350
        TitleFontSize = 52
        BackgroundColor = '#1E1E1E'
    }
```

**Available Presets:**
- `Default` - Standard theme colors, 180px height
- `Gradient` - Blue gradient, 200px height
- `Image` - Optimized for background images, 220px height
- `Minimal` - Compact design, 120px height
- `Hero` - Large centered banner, 300px height
- `Accent` - Green accent color, 160px height

---

## Get-PoshUIDashboard Returns Nothing

### Symptom
`Get-PoshUIDashboard` returns no output.

### Cause
Dashboard not initialized or wrong module context.

### Solution

```powershell
# ❌ Wrong - Get-PoshUIDashboard called before initialization
Get-PoshUIDashboard

# ✅ Correct - initialize dashboard first
New-PoshUIDashboard -Title 'Dashboard'
Add-UIStep -Name 'Dashboard' -Title 'Dashboard' -Order 1
Get-PoshUIDashboard  # Now returns dashboard definition
```

---

## Getting Help

### Enable Verbose Output
```powershell
# See detailed execution information
Add-UIMetricCard -Step 'Dashboard' -Name 'Test' -Title 'Test' -Value 100 -Verbose
```

### Check Error Details
```powershell
# View last error with full details
$Error[0] | Format-List * -Force
```

### Export Dashboard for Inspection
```powershell
# Save dashboard definition as JSON
Get-PoshUIDashboard -AsJson | Out-File dashboard.json

# Review the JSON to see what was actually created
Get-Content dashboard.json | ConvertFrom-Json | Format-List
```

### Test in Isolation
```powershell
# Create minimal test case
New-PoshUIDashboard -Title 'Test'
Add-UIStep -Name 'Test' -Title 'Test' -Order 1
Add-UIMetricCard -Step 'Test' -Name 'Simple' -Title 'Simple' -Value 100
Show-PoshUIDashboard
```

---

## Common Error Messages

### "No UI initialized"
**Cause:** Forgot to call `New-PoshUIDashboard`  
**Solution:** Call `New-PoshUIDashboard -Title 'YourTitle'` first

### "Step 'X' not found"
**Cause:** Step name doesn't exist or typo  
**Solution:** Verify step name with `Get-PoshUIDashboard`

### "Failed to execute Value ScriptBlock"
**Cause:** Error in ScriptBlock code  
**Solution:** Test ScriptBlock independently, check error suggestions

### "The term 'X' is not recognized"
**Cause:** Cmdlet not available or typo  
**Solution:** Verify cmdlet name, check if module is loaded

---

## Still Having Issues?

1. **Check Examples:** Review working examples in `PoshUI\Examples\`
2. **Enable Verbose:** Use `-Verbose` on all cmdlets
3. **Test Components:** Test ScriptBlocks and data independently
4. **Simplify:** Create minimal reproduction case
5. **Check Logs:** Review `$env:LOCALAPPDATA\PoshUI\Logs\PoshUI.log`

For additional help, see:
- [Dashboard Cards Reference](./dashboard-cards-reference.md)
- [Cmdlet Reference](./cmdlet-reference.md)
- [Examples](./examples/demo-dashboard.md)

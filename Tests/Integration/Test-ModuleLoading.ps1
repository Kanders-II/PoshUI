<#
.SYNOPSIS
Tests PoshUI module loading and cmdlet export.

.DESCRIPTION
Verifies that the PoshUI modules (Wizard, Dashboard, Workflow) load correctly and export all expected cmdlets.
This is the primary integration test for module functionality.

.EXAMPLE
& .\Test-ModuleLoading.ps1
#>

param(
    [string]$WizardModulePath = (Join-Path $PSScriptRoot '..\..\PoshUI\PoshUI.Wizard\PoshUI.Wizard.psd1'),
    [string]$DashboardModulePath = (Join-Path $PSScriptRoot '..\..\PoshUI\PoshUI.Dashboard\PoshUI.Dashboard.psd1'),
    [string]$WorkflowModulePath = (Join-Path $PSScriptRoot '..\..\PoshUI\PoshUI.Workflow\PoshUI.Workflow.psd1')
)

$testsPassed = 0
$testsFailed = 0

Write-Host "`n========================================" -ForegroundColor Cyan
Write-Host "  PoshUI Module Loading Tests" -ForegroundColor Cyan
Write-Host "========================================`n" -ForegroundColor Cyan

# Test 1: PoshUI.Wizard module file exists
Write-Host "Test 1: PoshUI.Wizard module file exists..." -ForegroundColor Yellow
if (Test-Path $WizardModulePath) {
    Write-Host "  PASS: Module file found at $WizardModulePath" -ForegroundColor Green
    $testsPassed++
} else {
    Write-Host "  FAIL: Module file not found at $WizardModulePath" -ForegroundColor Red
    $testsFailed++
}

# Test 2: PoshUI.Dashboard module file exists
Write-Host "`nTest 2: PoshUI.Dashboard module file exists..." -ForegroundColor Yellow
if (Test-Path $DashboardModulePath) {
    Write-Host "  PASS: Module file found at $DashboardModulePath" -ForegroundColor Green
    $testsPassed++
} else {
    Write-Host "  FAIL: Module file not found at $DashboardModulePath" -ForegroundColor Red
    $testsFailed++
}

# Test 3: PoshUI.Workflow module file exists
Write-Host "`nTest 3: PoshUI.Workflow module file exists..." -ForegroundColor Yellow
if (Test-Path $WorkflowModulePath) {
    Write-Host "  PASS: Module file found at $WorkflowModulePath" -ForegroundColor Green
    $testsPassed++
} else {
    Write-Host "  FAIL: Module file not found at $WorkflowModulePath" -ForegroundColor Red
    $testsFailed++
}

# Test 4: PoshUI.Wizard imports successfully
Write-Host "`nTest 4: PoshUI.Wizard imports successfully..." -ForegroundColor Yellow
try {
    Import-Module $WizardModulePath -Force -ErrorAction Stop
    Write-Host "  PASS: PoshUI.Wizard imported successfully" -ForegroundColor Green
    $testsPassed++
} catch {
    Write-Host "  FAIL: PoshUI.Wizard import failed: $_" -ForegroundColor Red
    $testsFailed++
}

# Test 5: PoshUI.Dashboard imports successfully
Write-Host "`nTest 5: PoshUI.Dashboard imports successfully..." -ForegroundColor Yellow
try {
    Import-Module $DashboardModulePath -Force -ErrorAction Stop
    Write-Host "  PASS: PoshUI.Dashboard imported successfully" -ForegroundColor Green
    $testsPassed++
} catch {
    Write-Host "  FAIL: PoshUI.Dashboard import failed: $_" -ForegroundColor Red
    $testsFailed++
}

# Test 6: PoshUI.Workflow imports successfully
Write-Host "`nTest 6: PoshUI.Workflow imports successfully..." -ForegroundColor Yellow
try {
    Import-Module $WorkflowModulePath -Force -ErrorAction Stop
    Write-Host "  PASS: PoshUI.Workflow imported successfully" -ForegroundColor Green
    $testsPassed++
} catch {
    Write-Host "  FAIL: PoshUI.Workflow import failed: $_" -ForegroundColor Red
    $testsFailed++
}

# Test 7: Verify PoshUI.Wizard cmdlets are exported
Write-Host "`nTest 7: Verify PoshUI.Wizard cmdlets are exported..." -ForegroundColor Yellow
$wizardCommands = Get-Command -Module PoshUI.Wizard -ErrorAction SilentlyContinue
if ($wizardCommands.Count -gt 0) {
    Write-Host "  PASS: $($wizardCommands.Count) cmdlets exported from PoshUI.Wizard" -ForegroundColor Green
    $testsPassed++
} else {
    Write-Host "  FAIL: No cmdlets exported from PoshUI.Wizard" -ForegroundColor Red
    $testsFailed++
}

# Test 8: Verify PoshUI.Dashboard cmdlets are exported
Write-Host "`nTest 8: Verify PoshUI.Dashboard cmdlets are exported..." -ForegroundColor Yellow
$dashboardCommands = Get-Command -Module PoshUI.Dashboard -ErrorAction SilentlyContinue
if ($dashboardCommands.Count -gt 0) {
    Write-Host "  PASS: $($dashboardCommands.Count) cmdlets exported from PoshUI.Dashboard" -ForegroundColor Green
    $testsPassed++
} else {
    Write-Host "  FAIL: No cmdlets exported from PoshUI.Dashboard" -ForegroundColor Red
    $testsFailed++
}

# Test 9: Verify PoshUI.Workflow cmdlets are exported
Write-Host "`nTest 9: Verify PoshUI.Workflow cmdlets are exported..." -ForegroundColor Yellow
$workflowCommands = Get-Command -Module PoshUI.Workflow -ErrorAction SilentlyContinue
if ($workflowCommands.Count -gt 0) {
    Write-Host "  PASS: $($workflowCommands.Count) cmdlets exported from PoshUI.Workflow" -ForegroundColor Green
    $testsPassed++
} else {
    Write-Host "  FAIL: No cmdlets exported from PoshUI.Workflow" -ForegroundColor Red
    $testsFailed++
}

# Test 10: Verify core Wizard cmdlets exist
Write-Host "`nTest 10: Verify core Wizard cmdlets exist..." -ForegroundColor Yellow
$coreWizardCmdlets = @('New-PoshUIWizard', 'Add-UIStep', 'Add-UITextBox', 'Show-PoshUIWizard')
$missingCmdlets = @()

foreach ($cmdlet in $coreWizardCmdlets) {
    if (-not (Get-Command $cmdlet -ErrorAction SilentlyContinue)) {
        $missingCmdlets += $cmdlet
    }
}

if ($missingCmdlets.Count -eq 0) {
    Write-Host "  PASS: All core Wizard cmdlets present" -ForegroundColor Green
    $testsPassed++
} else {
    Write-Host "  FAIL: Missing cmdlets: $($missingCmdlets -join ', ')" -ForegroundColor Red
    $testsFailed++
}

# Test 11: Verify core Dashboard cmdlets exist
Write-Host "`nTest 11: Verify core Dashboard cmdlets exist..." -ForegroundColor Yellow
$coreDashboardCmdlets = @('New-PoshUIDashboard', 'Add-UIStep', 'Add-UIBanner', 'Show-PoshUIDashboard')
$missingCmdlets = @()

foreach ($cmdlet in $coreDashboardCmdlets) {
    if (-not (Get-Command $cmdlet -ErrorAction SilentlyContinue)) {
        $missingCmdlets += $cmdlet
    }
}

if ($missingCmdlets.Count -eq 0) {
    Write-Host "  PASS: All core Dashboard cmdlets present" -ForegroundColor Green
    $testsPassed++
} else {
    Write-Host "  FAIL: Missing cmdlets: $($missingCmdlets -join ', ')" -ForegroundColor Red
    $testsFailed++
}

# Test 12: Verify core Workflow cmdlets exist
Write-Host "`nTest 12: Verify core Workflow cmdlets exist..." -ForegroundColor Yellow
$coreWorkflowCmdlets = @('New-PoshUIWorkflow', 'Add-UIStep', 'Add-UIWorkflowTask', 'Show-PoshUIWorkflow')
$missingCmdlets = @()

foreach ($cmdlet in $coreWorkflowCmdlets) {
    if (-not (Get-Command $cmdlet -ErrorAction SilentlyContinue)) {
        $missingCmdlets += $cmdlet
    }
}

if ($missingCmdlets.Count -eq 0) {
    Write-Host "  PASS: All core Workflow cmdlets present" -ForegroundColor Green
    $testsPassed++
} else {
    Write-Host "  FAIL: Missing cmdlets: $($missingCmdlets -join ', ')" -ForegroundColor Red
    $testsFailed++
}

# Summary
Write-Host "`n========================================" -ForegroundColor Cyan
Write-Host "  Test Summary" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host "  Passed: $testsPassed" -ForegroundColor Green
Write-Host "  Failed: $testsFailed" -ForegroundColor $(if ($testsFailed -gt 0) { 'Red' } else { 'Green' })
Write-Host "========================================`n" -ForegroundColor Cyan

if ($testsFailed -gt 0) {
    exit 1
}

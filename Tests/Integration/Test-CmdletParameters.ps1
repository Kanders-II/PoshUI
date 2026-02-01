<#
.SYNOPSIS
Tests that all PoshUI cmdlets accept correct parameters.

.DESCRIPTION
Validates parameter definitions, mandatory flags, and parameter sets for all PoshUI module cmdlets.

.EXAMPLE
& .\Test-CmdletParameters.ps1
#>

param(
    [string]$WizardModulePath = (Join-Path $PSScriptRoot '..\..\PoshUI\PoshUI.Wizard\PoshUI.Wizard.psd1'),
    [string]$DashboardModulePath = (Join-Path $PSScriptRoot '..\..\PoshUI\PoshUI.Dashboard\PoshUI.Dashboard.psd1'),
    [string]$WorkflowModulePath = (Join-Path $PSScriptRoot '..\..\PoshUI\PoshUI.Workflow\PoshUI.Workflow.psd1')
)

$testsPassed = 0
$testsFailed = 0

Write-Host "`n========================================" -ForegroundColor Cyan
Write-Host "  PoshUI Cmdlet Parameter Tests" -ForegroundColor Cyan
Write-Host "========================================`n" -ForegroundColor Cyan

# Import modules
try {
    Import-Module $WizardModulePath -Force -ErrorAction Stop
    Import-Module $DashboardModulePath -Force -ErrorAction Stop
    Import-Module $WorkflowModulePath -Force -ErrorAction Stop
} catch {
    Write-Host "FAIL: Could not import modules: $_" -ForegroundColor Red
    exit 1
}

# Test 1: New-PoshUIWizard parameters
Write-Host "Test 1: New-PoshUIWizard parameters..." -ForegroundColor Yellow
$cmd = Get-Command New-PoshUIWizard -ErrorAction SilentlyContinue
if ($cmd) {
    $params = $cmd.Parameters.Keys
    $requiredParams = @('Title')
    
    $missingRequired = $requiredParams | Where-Object { $_ -notin $params }
    
    if ($missingRequired.Count -eq 0) {
        Write-Host "  PASS: All required parameters present" -ForegroundColor Green
        $testsPassed++
    } else {
        Write-Host "  FAIL: Missing required parameters: $($missingRequired -join ', ')" -ForegroundColor Red
        $testsFailed++
    }
} else {
    Write-Host "  FAIL: New-PoshUIWizard cmdlet not found" -ForegroundColor Red
    $testsFailed++
}

# Test 2: Add-UIStep parameters
Write-Host "`nTest 2: Add-UIStep parameters..." -ForegroundColor Yellow
$cmd = Get-Command Add-UIStep -ErrorAction SilentlyContinue
if ($cmd) {
    $params = $cmd.Parameters.Keys
    $requiredParams = @('Name', 'Title', 'Order')
    
    $missingRequired = $requiredParams | Where-Object { $_ -notin $params }
    
    if ($missingRequired.Count -eq 0) {
        Write-Host "  PASS: All required parameters present" -ForegroundColor Green
        $testsPassed++
    } else {
        Write-Host "  FAIL: Missing required parameters: $($missingRequired -join ', ')" -ForegroundColor Red
        $testsFailed++
    }
} else {
    Write-Host "  FAIL: Add-UIStep cmdlet not found" -ForegroundColor Red
    $testsFailed++
}

# Test 3: Add-UITextBox parameters
Write-Host "`nTest 3: Add-UITextBox parameters..." -ForegroundColor Yellow
$cmd = Get-Command Add-UITextBox -ErrorAction SilentlyContinue
if ($cmd) {
    $params = $cmd.Parameters.Keys
    $requiredParams = @('Step', 'Name', 'Label')
    
    $missingRequired = $requiredParams | Where-Object { $_ -notin $params }
    
    if ($missingRequired.Count -eq 0) {
        Write-Host "  PASS: All required parameters present" -ForegroundColor Green
        $testsPassed++
    } else {
        Write-Host "  FAIL: Missing required parameters: $($missingRequired -join ', ')" -ForegroundColor Red
        $testsFailed++
    }
} else {
    Write-Host "  FAIL: Add-UITextBox cmdlet not found" -ForegroundColor Red
    $testsFailed++
}

# Test 4: New-PoshUIDashboard parameters
Write-Host "`nTest 4: New-PoshUIDashboard parameters..." -ForegroundColor Yellow
$cmd = Get-Command New-PoshUIDashboard -ErrorAction SilentlyContinue
if ($cmd) {
    $params = $cmd.Parameters.Keys
    $requiredParams = @('Title')
    
    $missingRequired = $requiredParams | Where-Object { $_ -notin $params }
    
    if ($missingRequired.Count -eq 0) {
        Write-Host "  PASS: All required parameters present" -ForegroundColor Green
        $testsPassed++
    } else {
        Write-Host "  FAIL: Missing required parameters: $($missingRequired -join ', ')" -ForegroundColor Red
        $testsFailed++
    }
} else {
    Write-Host "  FAIL: New-PoshUIDashboard cmdlet not found" -ForegroundColor Red
    $testsFailed++
}

# Test 5: New-PoshUIWorkflow parameters
Write-Host "`nTest 5: New-PoshUIWorkflow parameters..." -ForegroundColor Yellow
$cmd = Get-Command New-PoshUIWorkflow -ErrorAction SilentlyContinue
if ($cmd) {
    $params = $cmd.Parameters.Keys
    $requiredParams = @('Title')
    
    $missingRequired = $requiredParams | Where-Object { $_ -notin $params }
    
    if ($missingRequired.Count -eq 0) {
        Write-Host "  PASS: All required parameters present" -ForegroundColor Green
        $testsPassed++
    } else {
        Write-Host "  FAIL: Missing required parameters: $($missingRequired -join ', ')" -ForegroundColor Red
        $testsFailed++
    }
} else {
    Write-Host "  FAIL: New-PoshUIWorkflow cmdlet not found" -ForegroundColor Red
    $testsFailed++
}

# Test 6: Add-UIWorkflowTask parameters
Write-Host "`nTest 6: Add-UIWorkflowTask parameters..." -ForegroundColor Yellow
$cmd = Get-Command Add-UIWorkflowTask -ErrorAction SilentlyContinue
if ($cmd) {
    $params = $cmd.Parameters.Keys
    $requiredParams = @('Step', 'Name', 'Title', 'Order')
    
    $missingRequired = $requiredParams | Where-Object { $_ -notin $params }
    
    if ($missingRequired.Count -eq 0) {
        Write-Host "  PASS: All required parameters present" -ForegroundColor Green
        $testsPassed++
    } else {
        Write-Host "  FAIL: Missing required parameters: $($missingRequired -join ', ')" -ForegroundColor Red
        $testsFailed++
    }
} else {
    Write-Host "  FAIL: Add-UIWorkflowTask cmdlet not found" -ForegroundColor Red
    $testsFailed++
}

# Test 7: Parameter validation attributes
Write-Host "`nTest 7: Add-UITextBox Step parameter is mandatory..." -ForegroundColor Yellow
$cmd = Get-Command Add-UITextBox -ErrorAction SilentlyContinue
if ($cmd) {
    $stepParam = $cmd.Parameters['Step']
    $isMandatory = $stepParam.Attributes | Where-Object { $_.TypeId.Name -eq 'ParameterAttribute' -and $_.Mandatory }
    
    if ($isMandatory) {
        Write-Host "  PASS: Step parameter is marked as mandatory" -ForegroundColor Green
        $testsPassed++
    } else {
        Write-Host "  FAIL: Step parameter should be mandatory" -ForegroundColor Red
        $testsFailed++
    }
} else {
    Write-Host "  FAIL: Add-UITextBox cmdlet not found" -ForegroundColor Red
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

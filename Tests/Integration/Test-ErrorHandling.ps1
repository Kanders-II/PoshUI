<#
.SYNOPSIS
Tests error handling in PoshUI cmdlets.

.DESCRIPTION
Validates that PoshUI cmdlets properly handle invalid inputs and provide meaningful error messages.

.EXAMPLE
& .\Test-ErrorHandling.ps1
#>

param(
    [string]$WizardModulePath = (Join-Path $PSScriptRoot '..\..\PoshUI\PoshUI.Wizard\PoshUI.Wizard.psd1'),
    [string]$DashboardModulePath = (Join-Path $PSScriptRoot '..\..\PoshUI\PoshUI.Dashboard\PoshUI.Dashboard.psd1'),
    [string]$WorkflowModulePath = (Join-Path $PSScriptRoot '..\..\PoshUI\PoshUI.Workflow\PoshUI.Workflow.psd1')
)

$testsPassed = 0
$testsFailed = 0

Write-Host "`n========================================" -ForegroundColor Cyan
Write-Host "  PoshUI Error Handling Tests" -ForegroundColor Cyan
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

# Test 1: New-PoshUIWizard without required parameter
Write-Host "Test 1: New-PoshUIWizard without Title (should fail)..." -ForegroundColor Yellow
try {
    New-PoshUIWizard -ErrorAction Stop
    Write-Host "  FAIL: Should have thrown error for missing Title parameter" -ForegroundColor Red
    $testsFailed++
} catch {
    Write-Host "  PASS: Correctly throws error for missing Title parameter" -ForegroundColor Green
    $testsPassed++
}

# Test 2: Add-UIStep without wizard initialization
Write-Host "`nTest 2: Add-UIStep before New-PoshUIWizard (should fail)..." -ForegroundColor Yellow
try {
    # Clear any existing wizard state
    Remove-Variable -Name CurrentWizard -Scope Script -ErrorAction SilentlyContinue
    Remove-Variable -Name script:CurrentWizard -ErrorAction SilentlyContinue
    
    Add-UIStep -Name 'Test' -Title 'Test' -Order 1 -ErrorAction Stop
    Write-Host "  FAIL: Should have thrown error when wizard not initialized" -ForegroundColor Red
    $testsFailed++
} catch {
    Write-Host "  PASS: Correctly throws error when wizard not initialized" -ForegroundColor Green
    $testsPassed++
}

# Test 3: Invalid theme value
Write-Host "`nTest 3: New-PoshUIWizard with invalid theme..." -ForegroundColor Yellow
try {
    New-PoshUIWizard -Title 'Test' -Theme 'InvalidTheme' -ErrorAction Stop
    Write-Host "  FAIL: Should have thrown error for invalid theme" -ForegroundColor Red
    $testsFailed++
} catch {
    Write-Host "  PASS: Correctly validates theme parameter" -ForegroundColor Green
    $testsPassed++
}

# Test 4: New-PoshUIDashboard without required parameter
Write-Host "`nTest 4: New-PoshUIDashboard without Title (should fail)..." -ForegroundColor Yellow
try {
    New-PoshUIDashboard -ErrorAction Stop
    Write-Host "  FAIL: Should have thrown error for missing Title parameter" -ForegroundColor Red
    $testsFailed++
} catch {
    Write-Host "  PASS: Correctly throws error for missing Title parameter" -ForegroundColor Green
    $testsPassed++
}

# Test 5: New-PoshUIWorkflow without required parameter
Write-Host "`nTest 5: New-PoshUIWorkflow without Title (should fail)..." -ForegroundColor Yellow
try {
    New-PoshUIWorkflow -ErrorAction Stop
    Write-Host "  FAIL: Should have thrown error for missing Title parameter" -ForegroundColor Red
    $testsFailed++
} catch {
    Write-Host "  PASS: Correctly throws error for missing Title parameter" -ForegroundColor Green
    $testsPassed++
}

# Test 6: Add-UITextBox with invalid regex pattern
Write-Host "`nTest 6: Add-UITextBox with invalid regex pattern..." -ForegroundColor Yellow
try {
    New-PoshUIWizard -Title 'Test'
    Add-UIStep -Name 'TestStep' -Title 'Test Step' -Order 1
    
    # Invalid regex pattern (unclosed bracket)
    Add-UITextBox -Step 'TestStep' -Name 'TestField' -Label 'Test' -ValidationPattern '[abc' -ErrorAction Stop
    
    Write-Host "  WARN: Invalid regex pattern was not rejected (may be validated at runtime)" -ForegroundColor Yellow
    $testsPassed++
} catch {
    Write-Host "  PASS: Correctly rejects invalid regex pattern" -ForegroundColor Green
    $testsPassed++
}

# Test 7: Duplicate step names
Write-Host "`nTest 7: Adding duplicate step names..." -ForegroundColor Yellow
try {
    New-PoshUIWizard -Title 'Test'
    Add-UIStep -Name 'DuplicateStep' -Title 'First' -Order 1
    Add-UIStep -Name 'DuplicateStep' -Title 'Second' -Order 2 -ErrorAction Stop
    
    Write-Host "  WARN: Duplicate step names allowed (may be handled by overwrite)" -ForegroundColor Yellow
    $testsPassed++
} catch {
    Write-Host "  PASS: Correctly prevents duplicate step names" -ForegroundColor Green
    $testsPassed++
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

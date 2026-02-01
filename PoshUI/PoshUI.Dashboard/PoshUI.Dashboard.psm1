#Requires -Version 5.1

# PoshUI.Dashboard PowerShell Module
# Provides native Verb-Noun API for creating interactive Dashboard UIs

# Cache module root for downstream scripts
$script:ModuleRoot = $PSScriptRoot

# Resolve file collections explicitly to avoid path parsing issues
$publicFolder  = Join-Path $PSScriptRoot 'Public'
$privateFolder = Join-Path $PSScriptRoot 'Private'
$classesFolder = Join-Path $PSScriptRoot 'Classes'

# Get public function definition files and class scripts
$PublicFunctions = @(Get-ChildItem -Path $publicFolder -Filter '*.ps1' -Recurse -ErrorAction SilentlyContinue)
$Classes         = @(Get-ChildItem -Path $classesFolder -Filter '*.ps1' -ErrorAction SilentlyContinue)

# Load classes in dependency order
$ClassOrder = @('UITemplate.ps1', 'UIStepType.ps1', 'UIControl.ps1', 'UIStep.ps1', 'UIDefinition.ps1', 'UIFactory.ps1', 'UIEvents.ps1')
foreach ($className in $ClassOrder) {
    $classFile = $Classes | Where-Object { $_.Name -eq $className }
    if ($classFile) {
        try {
            Write-Verbose "Loading class: $($classFile.FullName)"
            . $classFile.FullName
        }
        catch {
            Write-Error -Message "Failed to import class $($classFile.FullName): $_"
        }
    }
}

## Load security functions first (critical for module operation)
$securityFolder = Join-Path $privateFolder 'Security'
if (Test-Path $securityFolder) {
    $securityScripts = @(Get-ChildItem -Path $securityFolder -Filter '*.ps1' -ErrorAction SilentlyContinue)
    foreach ($secScript in $securityScripts) {
        try {
            Write-Verbose "Loading security function: $($secScript.FullName)"
            . $secScript.FullName
        }
        catch {
            Write-Error -Message "Failed to load security function $($secScript.FullName): $_"
            throw
        }
    }
    Write-Verbose "Loaded $($securityScripts.Count) security functions"
}

## Load state management functions
$stateManagementFolder = Join-Path $privateFolder 'StateManagement'
if (Test-Path $stateManagementFolder) {
    $stateScripts = @(Get-ChildItem -Path $stateManagementFolder -Filter '*.ps1' -ErrorAction SilentlyContinue)
    foreach ($stateScript in $stateScripts) {
        try {
            Write-Verbose "Loading state management function: $($stateScript.FullName)"
            . $stateScript.FullName
        }
        catch {
            Write-Error -Message "Failed to load state management function $($stateScript.FullName): $_"
        }
    }
    Write-Verbose "Loaded $($stateScripts.Count) state management functions"
}

## Load private helper scripts
$privateScripts = @(
    'Initialize-UIContext.ps1',
    'Serialize-UIDefinition.ps1',
    'ArgumentCompleters.ps1',
    'Test-UIDataSource.ps1',
    'Test-UIScriptBlockParameters.ps1',
    'Measure-UIDataSource.ps1',
    'Get-ScriptParameters.ps1',
    'ValidationHelpers.ps1'
)

foreach ($scriptName in $privateScripts) {
    $scriptPath = Join-Path $privateFolder $scriptName
    if (Test-Path $scriptPath) {
        try {
            Write-Verbose "Dot-sourcing private helper script: $scriptPath"
            . $scriptPath
        }
        catch {
            Write-Error -Message "Failed to dot-source private helper script '$scriptPath': $($_.Exception.Message)"
            throw
        }
    }
}

# Load public functions
foreach ($import in $PublicFunctions) {
    try {
        Write-Verbose "Loading public function: $($import.FullName)"
        . $import.FullName
    }
    catch {
        Write-Error -Message "Failed to import public function $($import.FullName): $_"
    }
}

# Module-level variables
$script:CurrentWizard = $null
$script:ModuleRoot = $PSScriptRoot

# Auto-cleanup: Remove stale sessions from previous crashed instances
Write-Verbose "Performing auto-cleanup of stale PoshUI sessions..."
try {
    $registryPath = "HKCU:\Software\PoshUI\Sessions"
    if (Test-Path $registryPath) {
        $staleHours = 24
        $sessionKeys = Get-ChildItem -Path $registryPath -ErrorAction SilentlyContinue
        $staleSessions = 0

        foreach ($key in $sessionKeys) {
            try {
                if ($key.LastWriteTime -lt (Get-Date).AddHours(-$staleHours)) {
                    Remove-Item -Path $key.PSPath -Recurse -Force -ErrorAction SilentlyContinue
                    $staleSessions++
                }
            }
            catch {
                # Silently continue if we can't clean a session
            }
        }

        if ($staleSessions -gt 0) {
            Write-Verbose "Cleaned up $staleSessions stale session(s)"
        }
    }
}
catch {
    Write-Verbose "Auto-cleanup skipped: $($_.Exception.Message)"
}

# Initialize module
Write-Verbose "PoshUI.Dashboard module loaded. Functions available: $($PublicFunctions.Count)"

# Module cleanup when removed
$MyInvocation.MyCommand.ScriptBlock.Module.OnRemove = {
    Write-Verbose "Cleaning up PoshUI.Dashboard module"
    $script:CurrentWizard = $null
    $script:ModuleRoot = $null
}

# Export public functions
$FunctionNames = $PublicFunctions | ForEach-Object { [System.IO.Path]::GetFileNameWithoutExtension($_.Name) }
$ValidationHelpers = @('Test-UIDataSource', 'Test-UIScriptBlockParameters', 'Measure-UIDataSource', 'Test-UIStepExists', 'Test-UIControlNameUnique', 'Test-UIParameterValidation', 'Test-ValidateSetValue', 'Test-ScriptBlockOrValue', 'Get-UIValidationErrorMessage')
$SecurityHelpers = @('Invoke-PoshUIExe')
$TypeSpecificCardlets = @('Add-UIMetricCard', 'Add-UIChartCard', 'Add-UITableCard')
$AllExportedFunctions = $FunctionNames + $ValidationHelpers + $SecurityHelpers + $TypeSpecificCardlets

Export-ModuleMember -Function $AllExportedFunctions

# Note: Legacy aliases removed - product not yet released

# Argument completers
$StepCompleter = {
    param($commandName, $parameterName, $wordToComplete, $commandAst, $fakeBoundParameters)
    
    if ($script:CurrentWizard) {
        $script:CurrentWizard.Steps | Where-Object { $_.Name -like "$wordToComplete*" } | ForEach-Object {
            [System.Management.Automation.CompletionResult]::new(
                $_.Name,
                $_.Name,
                'ParameterValue',
                "Step: $($_.Name)"
            )
        }
    }
}

$controlFunctions = @(
    'Add-UICard', 'Add-UIScriptCard', 'Add-UIVisualizationCard', 'Add-UIBanner',
    'Add-UIMetricCard', 'Add-UIChartCard', 'Add-UITableCard'
)

foreach ($func in $controlFunctions) {
    Register-ArgumentCompleter -CommandName $func -ParameterName 'Step' -ScriptBlock $StepCompleter
}

Write-Verbose "PoshUI.Dashboard module initialization complete"

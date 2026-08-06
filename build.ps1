<#
.SYNOPSIS
    PoshUI CI/CD pipeline orchestrator (PowerShell-native). Runs the same build/test/security/lint steps
    locally and in CI.
.DESCRIPTION
    Tasks:
      Clean            - remove build + artifact output.
      Build            - dotnet build the Launcher engine (PoshUI.exe).
      Analyze          - PSScriptAnalyzer lint of the module, tests and examples (fails on Error severity).
      Test             - Pester suite (Tests/PoshUI) -> NUnit XML in artifacts.
      Security         - security-focused Pester tests -> NUnit XML in artifacts.
      ValidateExamples - headless parse + BOM + definition-build of every example (no GUI).
      CI               - Build -> Analyze -> Test -> Security -> ValidateExamples (the full gate).
    Run under PowerShell 7 (pwsh) so Pester 5 is available; ValidateExamples shells out to Windows
    PowerShell 5.1 for runtime fidelity.
.PARAMETER Task
    Which task to run (default CI).
.PARAMETER Configuration
    Debug or Release (default Release).
.EXAMPLE
    pwsh ./build.ps1                       # full CI gate
.EXAMPLE
    pwsh ./build.ps1 -Task Test
.EXAMPLE
    pwsh ./build.ps1 -Task Build -Configuration Debug
#>
[CmdletBinding()]
param(
    [ValidateSet('Clean', 'Build', 'Analyze', 'Test', 'Security', 'ValidateExamples', 'CI')]
    [string]$Task = 'CI',
    [ValidateSet('Debug', 'Release')]
    [string]$Configuration = 'Release',
    [switch]$InstallDependencies
)

$ErrorActionPreference = 'Stop'
# Never block on an interactive prompt (a couple of legacy cleanup tests would otherwise hang a foreground run).
$ConfirmPreference = 'None'
$ProgressPreference = 'SilentlyContinue'
$root = $PSScriptRoot
$artifacts = Join-Path $root 'artifacts'
$csproj = Join-Path $root 'Launcher\Launcher.csproj'
$modulePath = Join-Path $root 'PoshUI\PoshUI.Canvas'
$testsDir = Join-Path $root 'Tests\PoshUI'
$script:failures = [System.Collections.Generic.List[string]]::new()

function Write-Section($name) { Write-Host ''; Write-Host "=== $name ===" -ForegroundColor Cyan }
function Fail($name, $msg) { $script:failures.Add("$name : $msg"); Write-Host "[FAIL] $name — $msg" -ForegroundColor Red }
function Pass($name) { Write-Host "[ OK ] $name" -ForegroundColor Green }

function Initialize-Tooling {
    [void][System.IO.Directory]::CreateDirectory($artifacts)
    if ($InstallDependencies -or $env:CI) {
        foreach ($m in @(@{ N = 'Pester'; V = '5.5.0' }, @{ N = 'PSScriptAnalyzer'; V = '1.21.0' })) {
            if (-not (Get-Module $m.N -ListAvailable | Where-Object { $_.Version -ge [version]$m.V })) {
                Write-Host "Installing $($m.N) >= $($m.V)..."
                Install-Module $m.N -MinimumVersion $m.V -Force -Scope CurrentUser -SkipPublisherCheck -AllowClobber
            }
        }
    }
}

function Invoke-CleanTask {
    Write-Section 'Clean'
    foreach ($d in @($artifacts, (Join-Path $root 'Launcher\bin'), (Join-Path $root 'Launcher\obj'))) {
        if (Test-Path $d) { Remove-Item $d -Recurse -Force -ErrorAction SilentlyContinue }
    }
    Pass 'Clean'
}

function Invoke-BuildTask {
    Write-Section "Build ($Configuration)"
    if (-not (Get-Command dotnet -ErrorAction SilentlyContinue)) { Fail 'Build' 'dotnet SDK not found'; return }
    # Stop a running engine so the DLL isn't locked.
    Get-Process PoshUI -ErrorAction SilentlyContinue | Stop-Process -Force -ErrorAction SilentlyContinue
    $log = Join-Path $artifacts 'build.log'
    & dotnet build $csproj -c $Configuration -v minimal 2>&1 | Tee-Object -FilePath $log
    if ($LASTEXITCODE -ne 0) { Fail 'Build' "dotnet build exited $LASTEXITCODE (see $log)" } else { Pass 'Build' }
}

function Invoke-AnalyzeTask {
    Write-Section 'Analyze (PSScriptAnalyzer)'
    if (-not (Get-Module PSScriptAnalyzer -ListAvailable)) { Fail 'Analyze' 'PSScriptAnalyzer not installed (run with -InstallDependencies)'; return }
    Import-Module PSScriptAnalyzer -Force
    $targets = @($modulePath, (Join-Path $root 'PoshUI\Examples'), $testsDir, (Join-Path $root 'build.ps1'))
    $findings = foreach ($t in $targets) {
        if (Test-Path $t) {
            Invoke-ScriptAnalyzer -Path $t -Recurse -Severity Error -ErrorAction SilentlyContinue -ExcludeRule `
                PSAvoidUsingWriteHost, PSUseShouldProcessForStateChangingFunctions, `
                PSAvoidUsingConvertToSecureStringWithPlainText   # test fixtures build known SecureStrings; module uses DPAPI
        }
    }
    $findings | Export-Clixml (Join-Path $artifacts 'analyzer.xml') -ErrorAction SilentlyContinue
    $errors = @($findings | Where-Object Severity -eq 'Error')
    if ($errors.Count -gt 0) {
        $errors | ForEach-Object { Write-Host ("  {0}:{1} {2}" -f (Split-Path $_.ScriptName -Leaf), $_.Line, $_.RuleName) -ForegroundColor Yellow }
        Fail 'Analyze' "$($errors.Count) error-severity finding(s)"
    }
    else { Pass 'Analyze (0 errors)' }
}

function Invoke-PesterFolder($name, [string[]]$paths, $outFile) {
    if (-not (Get-Module Pester -ListAvailable | Where-Object { $_.Version -ge [version]'5.0' })) { Fail $name 'Pester 5 not installed (run with -InstallDependencies)'; return }
    # Guard against a vacuous PASS: if the test path is absent (e.g. the folder is gitignored, so a fresh CI
    # clone has no tests) Pester discovers 0 tests, FailedCount is 0, and this task would report success while
    # having verified nothing. Fail loudly instead.
    $missing = @($paths | Where-Object { -not (Test-Path $_) })
    if ($missing.Count -gt 0) { Fail $name ("test path(s) not found: " + ($missing -join ', ')); return }
    Import-Module Pester -MinimumVersion 5.0 -Force
    $cfg = New-PesterConfiguration
    $cfg.Run.Path = $paths
    $cfg.Run.PassThru = $true
    $cfg.Output.Verbosity = 'Normal'
    $cfg.TestResult.Enabled = $true
    $cfg.TestResult.OutputFormat = 'NUnitXml'
    $cfg.TestResult.OutputPath = $outFile
    $r = Invoke-Pester -Configuration $cfg
    if ($r.FailedCount -gt 0) { Fail $name "$($r.FailedCount) failed / $($r.PassedCount) passed" }
    elseif ($r.TotalCount -eq 0) { Fail $name 'no tests were discovered - refusing to report success' }
    else { Pass "$name ($($r.PassedCount) passed)" }
}

function Invoke-TestTask {
    Write-Section 'Test (Pester)'
    Invoke-PesterFolder 'Test' @($testsDir) (Join-Path $artifacts 'pester.xml')
}

function Invoke-SecurityTask {
    Write-Section 'Security (Pester)'
    $sec = @(Get-ChildItem $testsDir -Filter '*Security*.Tests.ps1' -File | Select-Object -ExpandProperty FullName)
    if ($sec.Count -eq 0) { Fail 'Security' 'no security tests found'; return }
    Invoke-PesterFolder 'Security' $sec (Join-Path $artifacts 'security.xml')
}

function Invoke-ValidateExamplesTask {
    Write-Section 'ValidateExamples (headless)'
    $validator = Join-Path $root 'Tests\Test-CanvasExamples.ps1'
    # Run in Windows PowerShell 5.1 for runtime fidelity; fall back to current host if unavailable.
    $ps = (Get-Command powershell.exe -ErrorAction SilentlyContinue)?.Source
    if ($ps) { & $ps -NoProfile -ExecutionPolicy Bypass -File $validator } else { & $validator }
    if ($LASTEXITCODE -ne 0) { Fail 'ValidateExamples' "validator exited $LASTEXITCODE" } else { Pass 'ValidateExamples' }
}

# ── Dispatch ─────────────────────────────────────────────────────────────────
Initialize-Tooling
switch ($Task) {
    'Clean' { Invoke-CleanTask }
    'Build' { Invoke-BuildTask }
    'Analyze' { Invoke-AnalyzeTask }
    'Test' { Invoke-TestTask }
    'Security' { Invoke-SecurityTask }
    'ValidateExamples' { Invoke-ValidateExamplesTask }
    'CI' {
        Invoke-BuildTask
        Invoke-AnalyzeTask
        Invoke-TestTask
        Invoke-SecurityTask
        Invoke-ValidateExamplesTask
    }
}

Write-Host ''
if ($script:failures.Count -gt 0) {
    Write-Host "PIPELINE FAILED ($($script:failures.Count)):" -ForegroundColor Red
    $script:failures | ForEach-Object { Write-Host "  - $_" -ForegroundColor Red }
    exit 1
}
Write-Host "PIPELINE PASSED ($Task)" -ForegroundColor Green
exit 0

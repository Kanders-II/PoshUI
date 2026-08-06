function Show-PoshUICanvas {
    <#
    .SYNOPSIS
    Serializes the current canvas to JSON and launches the WPF engine (PoshUI.exe) to render it.
    .DESCRIPTION
    Writes the definition to a temp .json and runs PoshUI.exe with it. Returns the collected
    values once the window closes (when the engine emits a result file).
    #>
    [CmdletBinding()]
    param(
        [switch]$AppDebug,
        [switch]$NoWait,
        # Dry-run: serialize + validate the definition and return a summary WITHOUT launching the engine.
        # Also enabled by env POSHUI_CANVAS_NOLAUNCH=1 (used by CI to validate examples headlessly).
        [switch]$Validate
    )

    if (-not $Global:_PoshUICanvas) { throw "No canvas active. Call New-PoshUICanvas first." }
    $ui = $Global:_PoshUICanvas.Definition

    if ($Validate -or $env:POSHUI_CANVAS_NOLAUNCH -eq '1') {
        $json = $ui | ConvertTo-Json -Depth 32
        $parsed = $json | ConvertFrom-Json    # throws if the definition isn't valid JSON
        $tempDir = Join-Path ([System.IO.Path]::GetTempPath()) 'PoshUI'
        [void][System.IO.Directory]::CreateDirectory($tempDir)
        $jsonPath = Join-Path $tempDir ("validate_{0}.json" -f ([guid]::NewGuid().ToString('N')))
        [System.IO.File]::WriteAllText($jsonPath, $json, (New-Object System.Text.UTF8Encoding $false))
        return [pscustomobject]@{
            Validated  = $true
            JsonPath   = $jsonPath
            Pages      = @($ui.Steps).Count
            Bytes      = [System.Text.Encoding]::UTF8.GetByteCount($json)
            Definition = $parsed
        }
    }

    # Resolve PoshUI.exe (same search order as the Wizard module's Initialize-UIContext).
    $parentRoot    = Split-Path $script:ModuleRoot -Parent      # ...\PoshUI
    $workspaceRoot = Split-Path $parentRoot -Parent             # repo root
    $candidates = @(
        (Join-Path $parentRoot 'bin\PoshUI.exe'),
        (Join-Path $script:ModuleRoot 'bin\PoshUI.exe'),
        (Join-Path $workspaceRoot 'Launcher\bin\Release\PoshUI.exe'),
        (Join-Path $workspaceRoot 'Launcher\bin\Debug\PoshUI.exe')
    )
    $exe = $candidates | Where-Object { Test-Path $_ } | Select-Object -First 1
    if (-not $exe) { throw "PoshUI.exe not found. Build the Launcher or place it in PoshUI\bin\. Searched:`n$($candidates -join "`n")" }

    $tempDir = Join-Path ([System.IO.Path]::GetTempPath()) 'PoshUI'
    [void][System.IO.Directory]::CreateDirectory($tempDir)
    $jsonPath = Join-Path $tempDir ("canvas_{0}.json" -f ([guid]::NewGuid().ToString('N')))
    $ui | ConvertTo-Json -Depth 32 | Set-Content -Path $jsonPath -Encoding UTF8

    $argList = @("`"$jsonPath`"")
    if ($AppDebug) { $argList += '--debug' }

    Write-Verbose "Launching $exe $jsonPath"
    $proc = Start-Process -FilePath $exe -ArgumentList $argList -PassThru
    if ($NoWait) { return $proc }
    $proc.WaitForExit()

    # The engine writes collected values next to the definition as <name>.result.json (Phase 3).
    $resultPath = [System.IO.Path]::ChangeExtension($jsonPath, '.result.json')
    if (Test-Path $resultPath) {
        try {
            $result = Get-Content $resultPath -Raw | ConvertFrom-Json
            # Password fields are DPAPI-protected by the engine (prefix 'PoshUISecure:'); unprotect to SecureString.
            $result = Convert-UICanvasSecrets $result
            Remove-Item $resultPath -Force -ErrorAction SilentlyContinue   # don't leave the result on disk
            return $result
        }
        catch { }
    }
    return $null
}

function script:Convert-UICanvasSecrets {
    param($obj)
    if ($null -eq $obj) { return $obj }
    try { Add-Type -AssemblyName System.Security -ErrorAction Stop } catch { }   # WinPS 5.1 doesn't auto-load it
    $entropy = [System.Text.Encoding]::UTF8.GetBytes('PoshUI_Canvas_Secret_v1')
    foreach ($p in @($obj.PSObject.Properties)) {
        $v = $p.Value
        if ($v -is [string] -and $v.StartsWith('PoshUISecure:')) {
            $ss = New-Object System.Security.SecureString
            try {
                $b64 = $v.Substring('PoshUISecure:'.Length)
                if ($b64) {
                    $bytes = [System.Security.Cryptography.ProtectedData]::Unprotect(
                        [Convert]::FromBase64String($b64), $entropy,
                        [System.Security.Cryptography.DataProtectionScope]::CurrentUser)
                    $plain = [System.Text.Encoding]::UTF8.GetString($bytes)
                    foreach ($ch in $plain.ToCharArray()) { $ss.AppendChar($ch) }
                    [Array]::Clear($bytes, 0, $bytes.Length)
                }
            }
            catch { Write-Verbose "unprotect '$($p.Name)' failed: $($_.Exception.Message)" }
            $ss.MakeReadOnly()
            $p.Value = $ss
        }
    }
    $obj
}

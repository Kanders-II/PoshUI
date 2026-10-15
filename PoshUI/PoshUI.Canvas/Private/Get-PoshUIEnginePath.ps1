# Finds the PoshUI.exe engine this module belongs with. One search order for every caller, so
# Show-PoshUICanvas (which launches it) and New-PoshUIApp (which copies it) can never disagree.
function script:Get-PoshUIEnginePath {
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
    $exe
}

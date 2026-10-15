function New-PoshUIApp {
    <#
    .SYNOPSIS
    Makes an app's own launcher: a copy of PoshUI.exe named after the app, with its settings beside it.

    .DESCRIPTION
    Writes <Name>.exe, <Name>.exe.config and <Name>.app.json into the app folder. Double-clicking
    <Name>.exe then runs the app's script inside PoshUI - no powershell.exe, no console window - with
    the settings deciding which script, whether it needs administrator rights, its icon, and the
    shortcuts '<Name>.exe --create-shortcuts' makes.

    By default the exe is PoshUI.exe copied byte for byte. A signature covers a file's contents, not
    its name, so a signed PoshUI.exe stays signed and app builds never need signing. The app's icon
    shows on its shortcuts and its window; only the exe file itself keeps PoshUI's icon.

    -StampIcon also writes the icon into the exe (and the UAC shield, for admin apps). That changes the
    exe's bytes and voids the signature, so pair it with -Sign.

    .PARAMETER Config
    The app settings (.app.json). Its paths are relative to -Destination, the app folder.
    .PARAMETER Destination
    The app folder: where the script lives and where the launcher is written.
    .PARAMETER Name
    The exe name, without .exe. Defaults to the settings file's name ('ESSB.app.json' -> 'ESSB').
    .EXAMPLE
    New-PoshUIApp -Config .\ESSB.app.json -Destination D:\Out\ESSB
    .EXAMPLE
    New-PoshUIApp -Config .\ESSB.app.json -Destination D:\Out\ESSB -StampIcon -Sign
    #>
    [CmdletBinding()]
    param(
        [Parameter(Mandatory)][string]$Config,
        [Parameter(Mandatory)][string]$Destination,
        [string]$Name,
        [switch]$StampIcon,
        [switch]$Sign,
        # The signing script. Defaults to the repo's Scripts\Sign-PoshUI.ps1 when it can be found.
        [string]$SignScript
    )

    $ErrorActionPreference = 'Stop'
    $Config = (Resolve-Path -LiteralPath $Config).Path
    $Destination = (Resolve-Path -LiteralPath $Destination).Path
    if (-not $Name) { $Name = [IO.Path]::GetFileName($Config) -replace '\.app\.json$', '' -replace '\.json$', '' }
    if ($Name -match '[\\/:*?"<>|]') { throw "'$Name' cannot be used as a file name." }

    # Read the settings the way the launcher will, and check them against the app folder now - a
    # mistake should fail the build, not the operator's double-click.
    $raw = [IO.File]::ReadAllText($Config).TrimStart([char]0xFEFF)
    $settings = $raw | ConvertFrom-Json
    if (-not $settings.Script) { throw "$Config does not name a Script." }
    foreach ($key in 'Script', 'Icon', 'LogDirectory') {
        $v = [string]$settings.$key
        if (-not $v) { continue }
        if ([IO.Path]::IsPathRooted($v) -or $v -match ':' -or ($v -split '[\\/]') -contains '..') {
            throw "$key in $Config must be a path inside the app folder, not '$v'."
        }
    }
    $scriptPath = Join-Path $Destination $settings.Script
    if (-not (Test-Path -LiteralPath $scriptPath)) { throw "The app's script is not in the app folder: $scriptPath" }
    $iconPath = if ($settings.Icon) { Join-Path $Destination $settings.Icon }
    if ($iconPath -and -not (Test-Path -LiteralPath $iconPath)) { Write-Warning "The app's icon was not found: $iconPath" ; $iconPath = $null }

    $engine = Get-PoshUIEnginePath
    $exe = Join-Path $Destination "$Name.exe"
    Copy-Item -LiteralPath $engine -Destination $exe -Force
    Copy-Item -LiteralPath "$engine.config" -Destination "$exe.config" -Force
    $appJson = Join-Path $Destination "$Name.app.json"
    if ($Config -ne $appJson) { [IO.File]::WriteAllText($appJson, $raw, (New-Object Text.UTF8Encoding $false)) }

    $mode = 'Copied'
    if ($StampIcon) {
        Set-PoshUIExeResources -ExePath $exe -IconPath $iconPath -RequireAdmin:([bool]$settings.RequireAdmin)
        $mode = 'Stamped'
        if ($Sign) {
            if (-not $SignScript) {
                $repoScript = Join-Path (Split-Path (Split-Path $script:ModuleRoot -Parent) -Parent) 'Scripts\Sign-PoshUI.ps1'
                if (Test-Path $repoScript) { $SignScript = $repoScript }
            }
            if (-not $SignScript -or -not (Test-Path $SignScript)) { throw 'No signing script found - pass -SignScript.' }
            & $SignScript -ExePath $exe
        }
    }
    elseif ((Get-FileHash -LiteralPath $exe -Algorithm SHA256).Hash -ne (Get-FileHash -LiteralPath $engine -Algorithm SHA256).Hash) {
        throw "The copied exe does not match PoshUI.exe - the copy is damaged: $exe"
    }

    $sig = Get-AuthenticodeSignature -LiteralPath $exe
    if ($sig.Status -ne 'Valid') {
        if ($mode -eq 'Stamped') { Write-Warning "$Name.exe was changed by -StampIcon and is not signed. Re-run with -Sign, or SmartScreen and application control may block it." }
        else { Write-Warning "$Name.exe is unsigned because the PoshUI.exe it was copied from is unsigned ($($sig.Status)). Package from a signed PoshUI release." }
    }

    [pscustomobject]@{
        Name      = $Name
        Exe       = $exe
        Settings  = $appJson
        Mode      = $mode
        Signature = [string]$sig.Status
    }
}

# Authoring-time stubs for the in-app runtime cmdlets. When a canvas Action/OnChange runs inside
# the live app, the engine injects real implementations (bound to the UI bridge) that shadow these.
# Called at authoring time (outside the app) they simply warn.

function Set-UICanvasValue {
    [CmdletBinding()] param([Parameter(Mandatory, Position = 0)][string]$Name, [Parameter(Position = 1)]$Value, [switch]$Quiet)
    Write-Warning "Set-UICanvasValue runs only inside a canvas action (in the running app)."
}
function Set-UICanvasProperty {
    [CmdletBinding()] param([Parameter(Mandatory, Position = 0)][string]$Name, [Parameter(Mandatory, Position = 1)][string]$Property, [Parameter(Position = 2)]$Value, [switch]$Quiet)
    Write-Warning "Set-UICanvasProperty runs only inside a canvas action (in the running app)."
}
function Get-UICanvasValue {
    [CmdletBinding()] param([Parameter(Mandatory, Position = 0)][string]$Name)
    Write-Warning "Get-UICanvasValue runs only inside a canvas action (in the running app)."
}
function Show-UICanvasPage {
    [CmdletBinding()] param([Parameter(Mandatory, Position = 0)]$Page)
    Write-Warning "Show-UICanvasPage runs only inside a canvas action (in the running app)."
}
function Submit-UICanvas {
    [CmdletBinding()] param()
    Write-Warning "Submit-UICanvas runs only inside a canvas action (in the running app)."
}
function Lock-UICanvasNavigation {
    [CmdletBinding()] param()
    Write-Warning "Lock-UICanvasNavigation runs only inside a canvas action (in the running app)."
}
function Unlock-UICanvasNavigation {
    [CmdletBinding()] param()
    Write-Warning "Unlock-UICanvasNavigation runs only inside a canvas action (in the running app)."
}
function Select-UICanvasFolder {
    [CmdletBinding()] param([string]$Description)
    Write-Warning "Select-UICanvasFolder runs only inside a canvas action (in the running app)."
}
function Select-UICanvasFile {
    [CmdletBinding()] param([string]$Title, [string]$Filter)
    Write-Warning "Select-UICanvasFile runs only inside a canvas action (in the running app)."
}
function Set-UICanvasAnimate {
    [CmdletBinding()] param([Parameter(Mandatory, Position = 0)][string]$Name, [Parameter(Mandatory, Position = 1)][string]$Property, [Parameter(Position = 2)][double]$To, $From, [double]$Duration = 250, [string]$Easing = 'CubicOut')
    Write-Warning "Set-UICanvasAnimate runs only inside a canvas action (in the running app)."
}
function Start-UICanvasAsync {
    [CmdletBinding()] param([Parameter(Mandatory, Position = 0)][scriptblock]$Script)
    Write-Warning "Start-UICanvasAsync runs only inside a canvas action (in the running app)."
}
function Show-UICanvasToast {
    [CmdletBinding()] param([Parameter(Mandatory, Position = 0)][string]$Message, [string]$Severity = 'info', [int]$Duration = 3000)
    Write-Warning "Show-UICanvasToast runs only inside a canvas action (in the running app)."
}
function Show-UICanvasDialog {
    [CmdletBinding()] param([Parameter(Mandatory, Position = 0)][string]$Message, [string]$Title = 'Confirm', [switch]$Prompt, [string]$DefaultValue, [string]$OkLabel = 'OK', [string]$CancelLabel = 'Cancel')
    Write-Warning "Show-UICanvasDialog runs only inside a canvas action (in the running app)."
}
function Show-UICanvasFlyout {
    [CmdletBinding()] param([string]$Target, [string[]]$Items, [string]$Title, [string]$Message, [ValidateSet('Bottom', 'Top', 'Left', 'Right', 'Mouse')][string]$Placement = 'Bottom')
    Write-Warning "Show-UICanvasFlyout runs only inside a canvas action (in the running app)."
}
function Show-UICanvasWindow {
    [CmdletBinding()] param([Parameter(Mandatory, Position = 0)][string]$Name)
    Write-Warning "Show-UICanvasWindow runs only inside a canvas action (in the running app)."
}
function Close-UICanvasWindow {
    [CmdletBinding()] param()
    Write-Warning "Close-UICanvasWindow runs only inside a canvas action (in the running app)."
}
function Set-UICanvasState {
    [CmdletBinding()] param([Parameter(Mandatory, Position = 0)][string]$Name, [Parameter(Position = 1)]$Value)
    Write-Warning "Set-UICanvasState runs only inside a canvas action (in the running app)."
}
function Get-UICanvasState {
    [CmdletBinding()] param([Parameter(Position = 0)][string]$Name)
    Write-Warning "Get-UICanvasState runs only inside a canvas action (in the running app)."
}

# Serialize-UIDefinition.ps1 - JSON serialization for UI definitions

function Serialize-UIDefinition {
    <#
    .SYNOPSIS
    Serializes a UIDefinition to JSON for passing to the executable.
    
    .PARAMETER Definition
    The UIDefinition object to serialize.
    
    .OUTPUTS
    JSON string representation of the definition.
    #>
    [CmdletBinding()]
    param(
        [Parameter(Mandatory = $true)]
        [object]$Definition
    )
    
    try {
        $json = $Definition | ConvertTo-Json -Depth 10 -Compress
        return $json
    }
    catch {
        Write-Error "Failed to serialize UI definition: $($_.Exception.Message)"
        throw
    }
}

function Set-SecureFileACL {
    <#
    .SYNOPSIS
        Sets restrictive ACL on a file or directory (current user only).
    
    .DESCRIPTION
        Configures Windows ACL to allow only the current user full control,
        disabling inheritance and removing all other access rules.
        
        This prevents other users (including administrators) from reading
        or modifying the protected file.
    
    .PARAMETER Path
        Path to the file or directory to protect.
    
    .PARAMETER IsDirectory
        If specified, treats the path as a directory and sets appropriate
        inheritance flags for child items.
    
    .EXAMPLE
        Set-SecureFileACL -Path "C:\Users\...\state.dat"
    
    .EXAMPLE
        Set-SecureFileACL -Path "C:\Users\...\PoshUI" -IsDirectory
    #>
    [CmdletBinding()]
    param(
        [Parameter(Mandatory = $true)]
        [string]$Path,
        
        [Parameter()]
        [switch]$IsDirectory
    )
    
    try {
        if (-not (Test-Path $Path)) {
            Write-Warning "Cannot set ACL: Path does not exist: $Path"
            return
        }
        
        $acl = Get-Acl $Path
        
        # Disable inheritance and remove inherited rules
        $acl.SetAccessRuleProtection($true, $false)
        
        # Remove all existing access rules
        $acl.Access | ForEach-Object {
            $acl.RemoveAccessRule($_) | Out-Null
        }
        
        # Get current user identity
        $identity = [System.Security.Principal.WindowsIdentity]::GetCurrent()
        
        # Create full control rule for current user
        if ($IsDirectory) {
            # Directory: Allow inheritance to child items
            $rule = New-Object System.Security.AccessControl.FileSystemAccessRule(
                $identity.Name,
                [System.Security.AccessControl.FileSystemRights]::FullControl,
                ([System.Security.AccessControl.InheritanceFlags]::ContainerInherit -bor [System.Security.AccessControl.InheritanceFlags]::ObjectInherit),
                [System.Security.AccessControl.PropagationFlags]::None,
                [System.Security.AccessControl.AccessControlType]::Allow
            )
        }
        else {
            # File: No inheritance needed
            $rule = New-Object System.Security.AccessControl.FileSystemAccessRule(
                $identity.Name,
                [System.Security.AccessControl.FileSystemRights]::FullControl,
                [System.Security.AccessControl.InheritanceFlags]::None,
                [System.Security.AccessControl.PropagationFlags]::None,
                [System.Security.AccessControl.AccessControlType]::Allow
            )
        }
        
        $acl.AddAccessRule($rule)
        Set-Acl -Path $Path -AclObject $acl
        
        Write-Verbose "Set restrictive ACL on: $Path"
    }
    catch {
        Write-Warning "Could not set ACL on $Path : $_"
        # Continue anyway - encryption is the primary protection
    }
}

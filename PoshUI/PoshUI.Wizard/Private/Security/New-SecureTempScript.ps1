function New-SecureTempScript {
    <#
    .SYNOPSIS
        Creates a secure temporary PowerShell script file.
    
    .DESCRIPTION
        Creates a temporary script file with:
        - Cryptographically secure random filename
        - Restricted file permissions (current user only)
        - Proper encoding (UTF8 with BOM for PowerShell)
        
        Security measures:
        - Random filename prevents race conditions
        - ACLs prevent unauthorized access
        - Dedicated temp directory for isolation
    
    .PARAMETER ScriptContent
        The PowerShell script content to write
    
    .OUTPUTS
        [string] Full path to the created temporary script file
    
    .EXAMPLE
        $path = New-SecureTempScript -ScriptContent "Write-Host 'Hello'"
        # Returns: C:\Users\...\AppData\Local\Temp\PoshUI\A7F3B2C9D4E8F1A2.ps1
    #>
    [CmdletBinding()]
    [OutputType([string])]
    param(
        [Parameter(Mandatory)]
        [AllowEmptyString()]
        [string]$ScriptContent
    )
    
    try {
        # Create dedicated temp directory for PoshUI
        $tempBase = [System.IO.Path]::GetTempPath()
        $tempDir = Join-Path $tempBase 'PoshUI'

        if (-not (Test-Path $tempDir)) {
            Write-Verbose "Creating PoshUI temp directory: $tempDir"
            $dir = New-Item -Path $tempDir -ItemType Directory -Force -ErrorAction Stop
            
            # Set restrictive ACL on directory (current user only)
            try {
                $acl = Get-Acl $dir.FullName
                $acl.SetAccessRuleProtection($true, $false)  # Disable inheritance
                
                $identity = [System.Security.Principal.WindowsIdentity]::GetCurrent()
                $rule = New-Object System.Security.AccessControl.FileSystemAccessRule(
                    $identity.Name,
                    [System.Security.AccessControl.FileSystemRights]::FullControl,
                    [System.Security.AccessControl.InheritanceFlags]'ContainerInherit,ObjectInherit',
                    [System.Security.AccessControl.PropagationFlags]::None,
                    [System.Security.AccessControl.AccessControlType]::Allow
                )
                $acl.AddAccessRule($rule)
                Set-Acl -Path $dir.FullName -AclObject $acl
                
                Write-Verbose "Set restrictive ACL on temp directory"
            } catch {
                Write-Warning "Could not set ACL on temp directory: $_"
                # Continue anyway - file-level ACL is more critical
            }
        }
        
        # Generate cryptographically secure random filename
        $randomBytes = New-Object byte[] 16
        $rng = [System.Security.Cryptography.RandomNumberGenerator]::Create()
        $rng.GetBytes($randomBytes)
        $rng.Dispose()
        
        $randomHex = [System.BitConverter]::ToString($randomBytes).Replace('-', '')
        $filename = "PoshUI_$randomHex.ps1"
        $scriptPath = Join-Path $tempDir $filename
        
        Write-Verbose "Generated secure temp path: $scriptPath"
        
        # Write script content with UTF8 BOM (required for PowerShell script parsing)
        $utf8BOM = New-Object System.Text.UTF8Encoding $true
        [System.IO.File]::WriteAllText($scriptPath, $ScriptContent, $utf8BOM)
        
        # Set restrictive ACL on file (current user only)
        try {
            $acl = Get-Acl $scriptPath
            $acl.SetAccessRuleProtection($true, $false)  # Disable inheritance
            
            $identity = [System.Security.Principal.WindowsIdentity]::GetCurrent()
            $rule = New-Object System.Security.AccessControl.FileSystemAccessRule(
                $identity.Name,
                [System.Security.AccessControl.FileSystemRights]::FullControl,
                [System.Security.AccessControl.InheritanceFlags]::None,
                [System.Security.AccessControl.PropagationFlags]::None,
                [System.Security.AccessControl.AccessControlType]::Allow
            )
            $acl.AddAccessRule($rule)
            Set-Acl -Path $scriptPath -AclObject $acl
            
            Write-Verbose "Set restrictive ACL on temp script file"
        } catch {
            Write-Warning "Could not set ACL on temp script file: $_"
            # Continue - file is created, just with inherited permissions
        }
        
        # Verify file was created
        if (-not (Test-Path $scriptPath)) {
            throw "Failed to create temp script file at: $scriptPath"
        }
        
        Write-Verbose "Successfully created secure temp script: $scriptPath"
        return $scriptPath
        
    } catch {
        Write-Error "Failed to create secure temp script: $_"
        throw
    }
}


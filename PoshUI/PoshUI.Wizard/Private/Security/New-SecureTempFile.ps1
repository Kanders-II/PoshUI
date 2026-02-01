function New-SecureTempFile {
    <#
    .SYNOPSIS
        Creates a secure temporary file with any extension.
    
    .DESCRIPTION
        Creates a temporary file with:
        - Cryptographically secure random filename
        - Restricted file permissions (current user only)
        - Proper encoding (UTF8 with BOM for PowerShell scripts, UTF8 for JSON)
        
        Security measures:
        - Random filename prevents race conditions
        - ACLs prevent unauthorized access
        - Dedicated temp directory for isolation
    
    .PARAMETER Content
        The file content to write
    
    .PARAMETER Extension
        File extension (default: .json)
    
    .OUTPUTS
        [string] Full path to the created temporary file
    
    .EXAMPLE
        $path = New-SecureTempFile -Content '{"key":"value"}' -Extension '.json'
        # Returns: C:\Users\...\AppData\Local\Temp\PoshUI\A7F3B2C9D4E8F1A2.json
    #>
    [CmdletBinding()]
    [OutputType([string])]
    param(
        [Parameter(Mandatory)]
        [AllowEmptyString()]
        [string]$Content,
        
        [Parameter()]
        [string]$Extension = '.json'
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
        $filename = "PoshUI_$randomHex$Extension"
        $filePath = Join-Path $tempDir $filename
        
        Write-Verbose "Generated secure temp path: $filePath"
        
        # Write content with appropriate encoding
        if ($Extension -eq '.ps1') {
            # PowerShell scripts need UTF8 BOM
            $utf8BOM = New-Object System.Text.UTF8Encoding $true
            [System.IO.File]::WriteAllText($filePath, $Content, $utf8BOM)
        } else {
            # JSON and other files use UTF8 without BOM
            $utf8 = New-Object System.Text.UTF8Encoding $false
            [System.IO.File]::WriteAllText($filePath, $Content, $utf8)
        }
        
        # Set restrictive ACL on file (current user only) - MANDATORY for security
        try {
            $acl = Get-Acl $filePath
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
            Set-Acl -Path $filePath -AclObject $acl

            Write-Verbose "Set restrictive ACL on temp file"
        } catch {
            # ACL setting is mandatory for security - fail if unable to set
            Remove-Item -Path $filePath -Force -ErrorAction SilentlyContinue
            throw "Failed to set secure ACL on temp file: $_"
        }
        
        # Verify file was created
        if (-not (Test-Path $filePath)) {
            throw "Failed to create temp file at: $filePath"
        }
        
        Write-Verbose "Successfully created secure temp file: $filePath"
        return $filePath
        
    } catch {
        Write-Error "Failed to create secure temp file: $_"
        throw
    }
}

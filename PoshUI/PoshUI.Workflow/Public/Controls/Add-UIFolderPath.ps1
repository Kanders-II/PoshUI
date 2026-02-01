function Add-UIFolderPath {
    <#
    .SYNOPSIS
    Adds a folder path selector control to the current UI step.
    
    .DESCRIPTION
    Creates a text input with a browse button that allows users to select a folder path.
    The control includes a "..." button that opens a folder picker dialog.
    
    .PARAMETER Name
    Unique name for the control. This becomes the PowerShell parameter name.
    
    .PARAMETER Label
    Display label shown above the control.
    
    .PARAMETER Default
    Optional default folder path value.
    
    .PARAMETER Mandatory
    Whether this field is required. Default is $false.
    
    .PARAMETER HelpText
    Optional help text displayed as a tooltip.
    
    .EXAMPLE
    Add-UIFolderPath -Name "DataPath" -Label "Data Folder" -DefaultValue "C:\SQLData"
    
    Adds a folder path selector with a default value.
    
    .EXAMPLE
    Add-UIFolderPath -Name "BackupPath" -Label "Backup Location" -Mandatory
    
    Adds a required folder path selector.
    
    .OUTPUTS
    UIControl object representing the folder path selector.
    
    .NOTES
    This function requires that a UI step has been added first using Add-WizardStep.
    Generates a [WizardPathSelector('Folder')] attribute in the output script.
    #>
    [CmdletBinding()]
    param(
        [Parameter(Mandatory = $true, Position = 0)]
        [ValidateNotNullOrEmpty()]
        [string]$Step,
        
        [Parameter(Mandatory = $true, Position = 1)]
        [ValidateNotNullOrEmpty()]
        [string]$Name,
        
        [Parameter(Mandatory = $true, Position = 2)]
        [ValidateNotNullOrEmpty()]
        [string]$Label,
        
        [Parameter()]
        [string]$Default,
        
        [Parameter()]
        [switch]$Mandatory,
        
        [Parameter()]
        [string]$HelpText
    )
    
    begin {
        Write-Verbose "Adding folder path selector: $Name ($Label) to step: $Step"
        
        if (-not $script:CurrentWorkflow) {
            throw "No UI initialized. Call New-PoshUI first."
        }
        
        if (-not $script:CurrentWorkflow.HasStep($Step)) {
            throw "Step '$Step' does not exist. Add the step first using Add-UIStep."
        }
    }
    
    process {
        try {
            $wizardStep = $script:CurrentWorkflow.GetStep($Step)
            
            $control = [UIControl]::new($Name, $Label, 'FolderPath')
            $control.Default = $Default
            $control.Mandatory = $Mandatory.IsPresent
            $control.HelpText = $HelpText
            
            $wizardStep.AddControl($control)
            
            Write-Verbose "Successfully added folder path selector: $Name"
            return $control
        }
        catch {
            Write-Error "Failed to add folder path selector '$Name': $($_.Exception.Message)"
            throw
        }
    }
    
    end {
        Write-Verbose "Add-UIFolderPath completed for: $Name"
    }
}

# Backward compatibility alias
Set-Alias -Name 'Add-WizardFolderPath' -Value 'Add-UIFolderPath'

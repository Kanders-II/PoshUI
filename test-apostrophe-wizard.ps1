Import-Module .\PoshUI\PoshUI.Wizard\PoshUI.Wizard.psd1 -Force

New-PoshUIWizard -Title "Apostrophe Test" -Description "Testing apostrophe rendering"

Add-UIStep -Name "Test" -Title "Test" -Order 1

Add-UICard -Step "Test" -Title "Test Card" -Type Info -Content @"
What You'll Explore:
- Test item 1
- Test item 2
"@

Show-PoshUIWizard -ScriptBody {
    Write-Host "Test complete"
}

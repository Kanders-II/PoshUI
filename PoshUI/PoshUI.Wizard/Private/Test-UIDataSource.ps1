function Test-UIDataSource {
    <#
    .SYNOPSIS
        Validates a UI data source (script block or CSV file) before runtime.

    .DESCRIPTION
        Tests whether a data source returns valid results and meets performance expectations.
        Validates script blocks return proper arrays of strings and CSV files exist with correct columns.

    .PARAMETER ScriptBlock
        The script block to test. Should return an array of strings.

    .PARAMETER CsvPath
        Path to a CSV file to test.

    .PARAMETER CsvColumn
        The column name to extract from the CSV file.

    .PARAMETER CsvFilter
        Optional filter script block to apply to CSV rows.

    .PARAMETER Parameters
        Hashtable of parameters to pass to the script block (for testing dependencies).

    .PARAMETER WarnIfSlowMs
        Execution time threshold in milliseconds to trigger a warning. Default: 1000

    .PARAMETER WarnIfLargeResults
        Result count threshold to trigger a warning. Default: 1000

    .EXAMPLE
        Test-UIDataSource -ScriptBlock { Get-Service | Select-Object -ExpandProperty Name }

        Tests a script block that returns service names.

    .EXAMPLE
        Test-UIDataSource -CsvPath "./data/servers.csv" -CsvColumn "ServerName"

        Tests a CSV file exists and contains a ServerName column.

    .EXAMPLE
        $params = @{ Environment = "Production" }
        Test-UIDataSource -ScriptBlock { param($Environment) Get-Content ".\$Environment.txt" } -Parameters $params

        Tests a script block with parameters.
    #>
    [CmdletBinding(DefaultParameterSetName = 'ScriptBlock')]
    param(
        [Parameter(Mandatory, ParameterSetName = 'ScriptBlock')]
        [ScriptBlock]$ScriptBlock,
        
        [Parameter(Mandatory, ParameterSetName = 'Csv')]
        [string]$CsvPath,
        
        [Parameter(Mandatory, ParameterSetName = 'Csv')]
        [string]$CsvColumn,
        
        [Parameter(ParameterSetName = 'Csv')]
        [ScriptBlock]$CsvFilter,
        
        [Parameter(ParameterSetName = 'ScriptBlock')]
        [hashtable]$Parameters,
        
        [int]$WarnIfSlowMs = 1000,
        
        [int]$WarnIfLargeResults = 1000
    )
    
    $result = [PSCustomObject]@{
        IsValid = $false
        Warnings = [System.Collections.Generic.List[string]]::new()
        Errors = [System.Collections.Generic.List[string]]::new()
        Results = @()
        ExecutionTimeMs = 0
        ResultCount = 0
    }
    
    try {
        $stopwatch = [System.Diagnostics.Stopwatch]::StartNew()
        
        if ($PSCmdlet.ParameterSetName -eq 'ScriptBlock') {
            # Test script block
            Write-Verbose "Testing script block data source..."
            
            try {
                if ($Parameters) {
                    $results = & $ScriptBlock @Parameters
                } else {
                    $results = & $ScriptBlock
                }
                
                $stopwatch.Stop()
                $result.ExecutionTimeMs = $stopwatch.ElapsedMilliseconds
                
                # Validate results
                if ($null -eq $results) {
                    $result.Warnings.Add("Script block returned null. Consider returning an empty array instead.")
                    $results = @()
                }
                
                # Convert to array if single item
                if ($results -isnot [Array]) {
                    $results = @($results)
                }
                
                # Convert all to strings
                $stringResults = $results | ForEach-Object {
                    if ($null -ne $_) { $_.ToString() }
                } | Where-Object { -not [string]::IsNullOrWhiteSpace($_) }
                
                $result.Results = $stringResults
                $result.ResultCount = $stringResults.Count
                
                # Check for empty results
                if ($result.ResultCount -eq 0) {
                    $result.Warnings.Add("Script block returned no results.")
                }
                
                # Check for whitespace-only values
                $emptyCount = ($results | Where-Object { [string]::IsNullOrWhiteSpace($_) }).Count
                if ($emptyCount -gt 0) {
                    $result.Warnings.Add("Script block returned $emptyCount empty or whitespace-only values (filtered out).")
                }
                
                $result.IsValid = $true
                
            } catch {
                $result.Errors.Add("Script block execution failed: $($_.Exception.Message)")
                $result.IsValid = $false
            }
            
        } else {
            # Test CSV file
            Write-Verbose "Testing CSV data source..."
            
            # Check file exists
            if (-not (Test-Path -Path $CsvPath)) {
                $result.Errors.Add("CSV file not found: $CsvPath")
                $result.IsValid = $false
                return $result
            }
            
            try {
                $csvData = Import-Csv -Path $CsvPath -ErrorAction Stop
                $stopwatch.Stop()
                $result.ExecutionTimeMs = $stopwatch.ElapsedMilliseconds
                
                if ($csvData.Count -eq 0) {
                    $result.Warnings.Add("CSV file is empty or contains only headers.")
                    $result.IsValid = $true
                    return $result
                }
                
                # Check if column exists
                $firstRow = $csvData | Select-Object -First 1
                $availableColumns = $firstRow.PSObject.Properties.Name
                
                if ($CsvColumn -notin $availableColumns) {
                    $result.Errors.Add("Column '$CsvColumn' not found in CSV file. Available columns: $($availableColumns -join ', ')")
                    $result.IsValid = $false
                    return $result
                }
                
                # Apply filter if specified
                if ($CsvFilter) {
                    $csvData = $csvData | Where-Object $CsvFilter
                }
                
                # Extract column values
                $columnValues = $csvData | Select-Object -ExpandProperty $CsvColumn | Where-Object { -not [string]::IsNullOrWhiteSpace($_) }
                
                $result.Results = $columnValues
                $result.ResultCount = $columnValues.Count
                
                if ($result.ResultCount -eq 0) {
                    if ($CsvFilter) {
                        $result.Warnings.Add("No results after applying filter. Check your filter logic.")
                    } else {
                        $result.Warnings.Add("Column '$CsvColumn' contains no non-empty values.")
                    }
                }
                
                $result.IsValid = $true
                
            } catch {
                $result.Errors.Add("CSV import failed: $($_.Exception.Message)")
                $result.IsValid = $false
            }
        }
        
        # Performance warnings
        if ($result.ExecutionTimeMs -gt $WarnIfSlowMs) {
            $result.Warnings.Add("Execution time ($($result.ExecutionTimeMs)ms) exceeds threshold ($WarnIfSlowMs ms). Consider optimization.")
        }
        
        if ($result.ResultCount -gt $WarnIfLargeResults) {
            $result.Warnings.Add("Result count ($($result.ResultCount)) exceeds threshold ($WarnIfLargeResults). This may impact UI performance.")
        }
        
    } catch {
        $result.Errors.Add("Unexpected error: $($_.Exception.Message)")
        $result.IsValid = $false
    }
    
    # Display results with color coding
    if ($result.IsValid) {
        Write-Host "[OK] " -ForegroundColor Green -NoNewline
        Write-Host "Data source validation PASSED" -ForegroundColor Green
    } else {
        Write-Host "[FAIL] " -ForegroundColor Red -NoNewline
        Write-Host "Data source validation FAILED" -ForegroundColor Red
    }
    
    Write-Host "  Results: $($result.ResultCount) items"
    Write-Host "  Execution time: $($result.ExecutionTimeMs)ms"
    
    if ($result.Warnings.Count -gt 0) {
        Write-Host ""
        Write-Host "Warnings:" -ForegroundColor Yellow
        foreach ($warning in $result.Warnings) {
            Write-Host "  [WARN] $warning" -ForegroundColor Yellow
        }
    }
    
    if ($result.Errors.Count -gt 0) {
        Write-Host ""
        Write-Host "Errors:" -ForegroundColor Red
        foreach ($errorMessage in $result.Errors) {
            Write-Host "  [ERR] $errorMessage" -ForegroundColor Red
        }
    }
    
    return $result
}


function Measure-UIDataSource {
    <#
    .SYNOPSIS
        Measures performance of a UI data source over multiple iterations.

    .DESCRIPTION
        Runs a data source multiple times to calculate average, minimum, and maximum
        execution times. Provides performance insights and optimization suggestions.

    .PARAMETER ScriptBlock
        The script block to measure.

    .PARAMETER CsvPath
        Path to a CSV file to measure.

    .PARAMETER CsvColumn
        The column name to extract from the CSV file.

    .PARAMETER CsvFilter
        Optional filter script block to apply to CSV rows.

    .PARAMETER Parameters
        Hashtable of parameters to pass to the script block.

    .PARAMETER Iterations
        Number of times to run the data source. Default: 10

    .PARAMETER WarnIfAverageMs
        Average execution time threshold in milliseconds. Default: 1000

    .PARAMETER WarnIfMaxMs
        Maximum execution time threshold in milliseconds. Default: 2000

    .EXAMPLE
        Measure-UIDataSource -ScriptBlock { Get-Service | Select-Object -ExpandProperty Name } -Iterations 5

        Measures performance of the script block over 5 iterations.

    .EXAMPLE
        Measure-UIDataSource -CsvPath "./data/servers.csv" -CsvColumn "ServerName" -Iterations 20

        Measures CSV import performance over 20 iterations.
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
        
        [ValidateRange(1, 1000)]
        [int]$Iterations = 10,
        
        [int]$WarnIfAverageMs = 1000,
        
        [int]$WarnIfMaxMs = 2000
    )
    
    $timings = [System.Collections.Generic.List[long]]::new()
    $resultCounts = [System.Collections.Generic.List[int]]::new()
    $errors = [System.Collections.Generic.List[string]]::new()
    
    Write-Host "Running performance measurement over $Iterations iterations..." -ForegroundColor Cyan
    Write-Host ""
    
    for ($i = 1; $i -le $Iterations; $i++) {
        Write-Progress -Activity "Measuring data source performance" -Status "Iteration $i of $Iterations" -PercentComplete (($i / $Iterations) * 100)
        
        $stopwatch = [System.Diagnostics.Stopwatch]::StartNew()
        
        try {
            if ($PSCmdlet.ParameterSetName -eq 'ScriptBlock') {
                if ($Parameters) {
                    $results = & $ScriptBlock @Parameters
                } else {
                    $results = & $ScriptBlock
                }
            } else {
                $csvData = Import-Csv -Path $CsvPath
                if ($CsvFilter) {
                    $csvData = $csvData | Where-Object $CsvFilter
                }
                $results = $csvData | Select-Object -ExpandProperty $CsvColumn
            }
            
            $stopwatch.Stop()
            $timings.Add($stopwatch.ElapsedMilliseconds)
            
            # Count results
            if ($null -eq $results) {
                $resultCounts.Add(0)
            } elseif ($results -is [Array]) {
                $resultCounts.Add($results.Count)
            } else {
                $resultCounts.Add(1)
            }
            
        } catch {
            $stopwatch.Stop()
            $errors.Add("Iteration $i failed: $($_.Exception.Message)")
            $timings.Add($stopwatch.ElapsedMilliseconds)
            $resultCounts.Add(0)
        }
    }
    
    Write-Progress -Activity "Measuring data source performance" -Completed
    
    # Calculate statistics
    $avgTime = ($timings | Measure-Object -Average).Average
    $minTime = ($timings | Measure-Object -Minimum).Minimum
    $maxTime = ($timings | Measure-Object -Maximum).Maximum
    $avgResults = ($resultCounts | Measure-Object -Average).Average
    
    $result = [PSCustomObject]@{
        Iterations = $Iterations
        AverageTimeMs = [math]::Round($avgTime, 2)
        MinTimeMs = $minTime
        MaxTimeMs = $maxTime
        AverageResultCount = [math]::Round($avgResults, 0)
        Timings = $timings.ToArray()
        ResultCounts = $resultCounts.ToArray()
        Errors = $errors.ToArray()
        SuccessRate = [math]::Round((($Iterations - $errors.Count) / $Iterations) * 100, 2)
    }
    
    # Display results
    Write-Host "Performance Measurement Results" -ForegroundColor Cyan
    Write-Host ("=" * 50)
    Write-Host "Iterations:         $($result.Iterations)"
    Write-Host "Success Rate:       $($result.SuccessRate)%"
    Write-Host ""
    Write-Host "Timing Statistics:" -ForegroundColor White
    Write-Host "  Average:          $($result.AverageTimeMs)ms"
    Write-Host "  Minimum:          $($result.MinTimeMs)ms"
    Write-Host "  Maximum:          $($result.MaxTimeMs)ms"
    Write-Host ""
    Write-Host "Result Statistics:" -ForegroundColor White
    Write-Host "  Average Count:    $($result.AverageResultCount) items"
    Write-Host ""
    
    # Performance assessment
    $warnings = [System.Collections.Generic.List[string]]::new()
    
    if ($result.AverageTimeMs -gt $WarnIfAverageMs) {
        $warnings.Add("Average execution time ($($result.AverageTimeMs)ms) exceeds threshold ($WarnIfAverageMs ms)")
    }
    
    if ($result.MaxTimeMs -gt $WarnIfMaxMs) {
        $warnings.Add("Maximum execution time ($($result.MaxTimeMs)ms) exceeds threshold ($WarnIfMaxMs ms)")
    }
    
    if ($result.SuccessRate -lt 100) {
        $warnings.Add("$($errors.Count) out of $Iterations iterations failed")
    }
    
    if ($warnings.Count -gt 0) {
        Write-Host "Warnings:" -ForegroundColor Yellow
        foreach ($warning in $warnings) {
            Write-Host "  [WARN] $warning" -ForegroundColor Yellow
        }
        Write-Host ""
    } else {
        Write-Host "[OK] Performance is within acceptable thresholds" -ForegroundColor Green
        Write-Host ""
    }
    
    # Optimization suggestions
    if ($result.AverageTimeMs -gt $WarnIfAverageMs -or $result.MaxTimeMs -gt $WarnIfMaxMs) {
        Write-Host "Optimization Suggestions:" -ForegroundColor Cyan
        Write-Host "  - Cache results if the data doesn't change frequently"
        Write-Host "  - Filter data earlier in the pipeline"
        Write-Host "  - Consider using -CacheDuration parameter on WizardDataSource attribute"
        Write-Host "  - For CSV files, ensure the file is not too large"
        Write-Host "  - For script blocks, avoid expensive operations like network calls"
        Write-Host ""
    }
    
    if ($errors.Count -gt 0) {
        Write-Host "Errors encountered:" -ForegroundColor Red
        foreach ($errorMsg in $errors) {
            Write-Host "  [ERR] $errorMsg" -ForegroundColor Red
        }
        Write-Host ""
    }
    
    return $result
}


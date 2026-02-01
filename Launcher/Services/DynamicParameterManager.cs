// Copyright (c) 2025 A Solution IT LLC. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Management.Automation;
using System.Management.Automation.Runspaces;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Launcher.Attributes;

namespace Launcher.Services
{
    /// <summary>
    /// Manages dynamic parameter execution, dependencies, and caching.
    /// </summary>
    public class DynamicParameterManager
    {
        private readonly Dictionary<string, DynamicParameterInfo> _parameters;
        private readonly Runspace _runspace;

        /// <summary>
        /// Execution options for dynamic parameters (timeout, result limits, etc.)
        /// </summary>
        public DynamicParameterExecutionOptions ExecutionOptions { get; set; }

        public DynamicParameterManager(Runspace runspace)
        {
            _runspace = runspace ?? throw new ArgumentNullException(nameof(runspace));
            _parameters = new Dictionary<string, DynamicParameterInfo>(StringComparer.OrdinalIgnoreCase);
            ExecutionOptions = DynamicParameterExecutionOptions.Default;
        }

        /// <summary>
        /// Registers a dynamic parameter from a UIDataSourceAttribute.
        /// </summary>
        public void RegisterParameter(string paramName, UIDataSourceAttribute attribute)
        {
            if (string.IsNullOrEmpty(paramName))
                throw new ArgumentNullException(nameof(paramName));
            if (attribute == null)
                throw new ArgumentNullException(nameof(attribute));

            attribute.Validate();

            var info = new DynamicParameterInfo
            {
                Name = paramName,
                Attribute = attribute,
                DependsOn = attribute.DependsOn?.ToList() ?? new List<string>()
            };

            _parameters[paramName] = info;

            LoggingService.Info($"Registered dynamic parameter: {paramName}, Dependencies: [{string.Join(", ", info.DependsOn)}]", component: "DynamicParameterManager");
        }

        /// <summary>
        /// Executes a dynamic parameter's data source and returns the choices.
        /// </summary>
        public string[] ExecuteDataSource(string paramName, Dictionary<string, object> parameterValues = null)
        {
            if (!_parameters.ContainsKey(paramName))
            {
                LoggingService.Warn($"Parameter '{paramName}' is not registered as dynamic", component: "DynamicParameterManager");
                return new string[0];
            }

            var info = _parameters[paramName];
            var attr = info.Attribute;

            var stopwatch = Stopwatch.StartNew();

            try
            {
                string[] results;

                if (attr.IsScriptBlockSource)
                {
                    results = ExecuteScriptBlock(paramName, attr, parameterValues);
                }
                else if (attr.IsCsvSource)
                {
                    results = ExecuteCsvSource(paramName, attr);
                }
                else
                {
                    throw new InvalidOperationException($"Parameter '{paramName}' has invalid data source configuration");
                }

                stopwatch.Stop();

                // Validate and process results
                results = ValidateAndProcessResults(paramName, results, stopwatch.ElapsedMilliseconds);

                // Log performance if enabled or if execution was slow
                if (ExecutionOptions.EnablePerformanceLogging || stopwatch.ElapsedMilliseconds > ExecutionOptions.ProgressThresholdMs)
                {
                    LoggingService.Info($"Executed data source for '{paramName}': {results.Length} items in {stopwatch.ElapsedMilliseconds}ms", component: "DynamicParameterManager");
                }

                return results;
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                // Unwrap AggregateException to get the actual error
                var actualException = ex is AggregateException aex && aex.InnerException != null 
                    ? aex.InnerException 
                    : ex;
                LoggingService.Error($"Failed to execute data source for '{paramName}' after {stopwatch.ElapsedMilliseconds}ms: {actualException.Message}", component: "DynamicParameterManager");

                return new[] { $"Error: {actualException.Message}" };
            }
        }

        /// <summary>
        /// Executes a script block with timeout and returns the results as a string array.
        /// </summary>
        private string[] ExecuteScriptBlock(string paramName, UIDataSourceAttribute attr, Dictionary<string, object> parameterValues)
        {
            using (var cts = new CancellationTokenSource(TimeSpan.FromSeconds(ExecutionOptions.TimeoutSeconds)))
            {
                var task = Task.Run(() =>
                {
                    try
                    {
                        using (var ps = PowerShell.Create())
                        {
                            ps.Runspace = _runspace;

                            // Add the script block - pass directly without wrapping in braces
                            ps.AddScript(attr.ScriptBlock.ToString());

                            // Add parameters if there are dependencies
                            if (attr.HasDependencies && parameterValues != null)
                            {
                                var psParams = new Dictionary<string, object>();
                                foreach (var dep in attr.DependsOn)
                                {
                                    if (parameterValues.ContainsKey(dep))
                                    {
                                        psParams[dep] = parameterValues[dep];
                                        LoggingService.Debug($"  Passing dependency '{dep}' = '{parameterValues[dep]}'", component: "DynamicParameterManager");
                                    }
                                    else
                                    {
                                        LoggingService.Warn($"  Dependency '{dep}' not found in parameter values", component: "DynamicParameterManager");
                                    }
                                }

                                ps.AddParameters(psParams);
                            }

                            // Execute and collect results
                            var results = ps.Invoke();

                            // Check for errors
                            if (ps.HadErrors)
                            {
                                var errorMessages = ps.Streams.Error.Select(e => e.ToString()).ToList();
                                var sb = new StringBuilder();
                                sb.AppendLine($"Script block execution failed for parameter '{paramName}'.");
                                sb.AppendLine();
                                sb.AppendLine("Script content:");
                                sb.AppendLine($"  {attr.ScriptBlock.ToString().Replace(Environment.NewLine, Environment.NewLine + "  ")}");
                                sb.AppendLine();
                                
                                if (attr.HasDependencies && parameterValues != null)
                                {
                                    sb.AppendLine("Parameters passed:");
                                    foreach (var dep in attr.DependsOn)
                                    {
                                        if (parameterValues.ContainsKey(dep))
                                        {
                                            sb.AppendLine($"  -{dep}: {parameterValues[dep]}");
                                        }
                                        else
                                        {
                                            sb.AppendLine($"  -{dep}: (not provided)");
                                        }
                                    }
                                    sb.AppendLine();
                                }
                                
                                sb.AppendLine("Error details:");
                                sb.AppendLine($"  {string.Join(Environment.NewLine + "  ", errorMessages)}");
                                sb.AppendLine();
                                sb.AppendLine("Suggestions:");
                                sb.AppendLine("  - Test the script block in a PowerShell console with the same parameters");
                                sb.AppendLine("  - Verify all parameter names match the script block's param() block");
                                sb.AppendLine("  - Check that required cmdlets/modules are available");
                                
                                throw new InvalidOperationException(sb.ToString());
                            }

                            // Convert results to string array
                            return ConvertToStringArray(results);
                        }
                    }
                    catch (Exception ex)
                    {
                        LoggingService.Error($"Script block execution error for '{paramName}': {ex.Message}", component: "DynamicParameterManager");
                        throw;
                    }
                }, cts.Token);

                try
                {
                    if (!task.Wait(TimeSpan.FromSeconds(ExecutionOptions.TimeoutSeconds + 1)))
                    {
                        throw new TimeoutException($"Script block execution timed out after {ExecutionOptions.TimeoutSeconds} seconds. Consider optimizing the script or increasing the timeout.");
                    }

                    return task.Result;
                }
                catch (AggregateException aex) when (aex.InnerException is OperationCanceledException)
                {
                    throw new TimeoutException($"Script block execution was cancelled after {ExecutionOptions.TimeoutSeconds} seconds.");
                }
            }
        }

        /// <summary>
        /// Executes a CSV-based data source with detailed error reporting.
        /// </summary>
        private string[] ExecuteCsvSource(string paramName, UIDataSourceAttribute attr)
        {
            string specifiedPath = attr.CsvPath;
            string resolvedPath = null;
            string currentDirectory = Directory.GetCurrentDirectory();
            string scriptDirectory = _runspace != null ? _runspace.SessionStateProxy.Path.CurrentFileSystemLocation.Path : null;

            try
            {
                // Try to resolve the path
                if (Path.IsPathRooted(specifiedPath))
                {
                    resolvedPath = specifiedPath;
                }
                else if (!string.IsNullOrEmpty(scriptDirectory))
                {
                    resolvedPath = Path.Combine(scriptDirectory, specifiedPath);
                }
                else
                {
                    resolvedPath = Path.Combine(currentDirectory, specifiedPath);
                }

                // Check if file exists
                if (!File.Exists(resolvedPath))
                {
                    var sb = new StringBuilder();
                    sb.AppendLine($"CSV file not found for parameter '{paramName}'.");
                    sb.AppendLine();
                    sb.AppendLine("Paths checked:");
                    sb.AppendLine($"  Specified path: {specifiedPath}");
                    sb.AppendLine($"  Resolved path: {resolvedPath}");
                    if (!string.IsNullOrEmpty(scriptDirectory))
                        sb.AppendLine($"  Script directory: {scriptDirectory}");
                    sb.AppendLine($"  Current directory: {currentDirectory}");
                    sb.AppendLine();
                    sb.AppendLine("Suggestions:");
                    sb.AppendLine($"  - Verify the file exists at the specified location");
                    sb.AppendLine($"  - Use an absolute path for the CSV file");
                    sb.AppendLine($"  - Ensure the path is relative to the script directory");
                    
                    throw new FileNotFoundException(sb.ToString());
                }

                using (var ps = PowerShell.Create())
                {
                    ps.Runspace = _runspace;

                    // Build script to import CSV and extract column
                    string script = $"Import-Csv -Path '{resolvedPath}'";

                    // Add filter if specified
                    if (attr.CsvFilter != null)
                    {
                        script += $" | Where-Object {{ {attr.CsvFilter} }}";
                    }

                    // Extract the specified column
                    script += $" | Select-Object -ExpandProperty '{attr.CsvColumn}'";

                    ps.AddScript(script);

                    // Execute
                    var results = ps.Invoke();

                    // Check for errors
                    if (ps.HadErrors)
                    {
                        var errorMessages = ps.Streams.Error.Select(e => e.ToString()).ToList();
                        var firstError = ps.Streams.Error.FirstOrDefault();
                        
                        // Check for common CSV errors
                        var sb = new StringBuilder();
                        sb.AppendLine($"CSV import failed for parameter '{paramName}'.");
                        sb.AppendLine();
                        sb.AppendLine($"File: {resolvedPath}");
                        sb.AppendLine($"Column: {attr.CsvColumn}");
                        if (attr.CsvFilter != null)
                            sb.AppendLine($"Filter: {attr.CsvFilter.ToString()}");
                        sb.AppendLine();
                        sb.AppendLine("Error details:");
                        
                        // Check if it's a missing column error
                        if (errorMessages.Any(e => e.Contains("does not contain a property") || e.Contains("Property")))
                        {
                            sb.AppendLine($"  The column '{attr.CsvColumn}' was not found in the CSV file.");
                            sb.AppendLine();
                            
                            // Try to get available columns
                            try
                            {
                                using (var ps2 = PowerShell.Create())
                                {
                                    ps2.Runspace = _runspace;
                                    ps2.AddScript($"(Import-Csv -Path '{resolvedPath}' | Select-Object -First 1).PSObject.Properties.Name");
                                    var columnResults = ps2.Invoke();
                                    if (columnResults.Any())
                                    {
                                        var columns = columnResults.Select(c => c.ToString()).ToList();
                                        sb.AppendLine($"  Available columns: {string.Join(", ", columns)}");
                                        sb.AppendLine($"  Note: Column names are case-sensitive");
                                    }
                                }
                            }
                            catch
                            {
                                // Ignore errors when trying to get column names
                            }
                        }
                        else if (File.ReadAllText(resolvedPath).Trim().Length == 0)
                        {
                            sb.AppendLine($"  The CSV file is empty.");
                        }
                        else
                        {
                            sb.AppendLine($"  {string.Join(Environment.NewLine + "  ", errorMessages)}");
                        }
                        
                        throw new InvalidOperationException(sb.ToString());
                    }

                    var resultArray = ConvertToStringArray(results);
                    
                    // Warn if no results after filter
                    if (resultArray.Length == 0)
                    {
                        LoggingService.Warn($"CSV import for parameter '{paramName}' returned no values after applying filter: {attr.CsvFilter}", component: "DynamicParameterManager");
                    }
                    
                    return resultArray;
                }
            }
            catch (Exception ex)
            {
                LoggingService.Error($"CSV execution error for '{paramName}': {ex.Message}", component: "DynamicParameterManager");
                throw;
            }
        }

        /// <summary>
        /// Converts PowerShell results to a string array.
        /// </summary>
        private string[] ConvertToStringArray(IEnumerable<PSObject> results)
        {
            if (results == null)
            {
                LoggingService.Warn("ConvertToStringArray received null results, returning empty array", component: "DynamicParameterManager");
                return new string[0];
            }

            var list = new List<string>();

            foreach (var item in results)
            {
                if (item == null) continue;

                // If it's a PSObject, get the base object
                var baseObject = item.BaseObject;

                if (baseObject is string str)
                {
                    if (!string.IsNullOrWhiteSpace(str))
                    {
                        list.Add(str);
                    }
                }
                else if (baseObject is IEnumerable enumerable && !(baseObject is string))
                {
                    // Handle arrays/collections
                    foreach (var subItem in enumerable)
                    {
                        if (subItem != null)
                        {
                            var strValue = subItem.ToString();
                            if (!string.IsNullOrWhiteSpace(strValue))
                            {
                                list.Add(strValue);
                            }
                        }
                    }
                }
                else if (baseObject != null)
                {
                    var strValue = baseObject.ToString();
                    if (!string.IsNullOrWhiteSpace(strValue))
                    {
                        list.Add(strValue);
                    }
                }
            }

            return list.ToArray();
        }

        /// <summary>
        /// Validates and processes results according to execution options.
        /// </summary>
        private string[] ValidateAndProcessResults(string paramName, string[] results, long executionTimeMs)
        {
            // Handle null or empty results
            if (results == null || results.Length == 0)
            {
                LoggingService.Warn($"Data source for '{paramName}' returned no results", component: "DynamicParameterManager");
                return new string[0];
            }

            // Log empty/whitespace values
            int emptyCount = results.Count(r => string.IsNullOrWhiteSpace(r));
            if (emptyCount > 0)
            {
                LoggingService.Warn($"Data source for '{paramName}' contains {emptyCount} empty/whitespace values", component: "DynamicParameterManager");
            }

            // Truncate to MaxResults if necessary
            if (results.Length > ExecutionOptions.MaxResults)
            {
                LoggingService.Warn($"Data source for '{paramName}' returned {results.Length} results, truncating to {ExecutionOptions.MaxResults}", component: "DynamicParameterManager");
                results = results.Take(ExecutionOptions.MaxResults).ToArray();
            }

            // Log statistics in debug mode
            if (ExecutionOptions.EnablePerformanceLogging)
            {
                LoggingService.Debug($"Result statistics for '{paramName}': {results.Length} items, {executionTimeMs}ms execution time", component: "DynamicParameterManager");
            }

            return results;
        }

        /// <summary>
        /// Returns the list of parameters that depend on the given parameter.
        /// </summary>
        public List<string> GetDependentParameters(string paramName)
        {
            return _parameters.Values
                .Where(p => p.DependsOn.Contains(paramName, StringComparer.OrdinalIgnoreCase))
                .Select(p => p.Name)
                .ToList();
        }

        /// <summary>
        /// Checks if a parameter has dependencies.
        /// </summary>
        public bool HasDependencies(string paramName)
        {
            return _parameters.ContainsKey(paramName) && _parameters[paramName].DependsOn.Count > 0;
        }

        /// <summary>
        /// Gets all registered dynamic parameters.
        /// </summary>
        public IEnumerable<string> GetAllParameters()
        {
            return _parameters.Keys;
        }

        /// <summary>
        /// Builds the execution order for parameters based on dependencies.
        /// Returns parameters in topological order (dependencies first).
        /// </summary>
        public List<string> GetExecutionOrder()
        {
            var order = new List<string>();
            var visited = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var visiting = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            foreach (var paramName in _parameters.Keys)
            {
                if (!visited.Contains(paramName))
                {
                    Visit(paramName, visited, visiting, order);
                }
            }

            return order;
        }

        /// <summary>
        /// Depth-first search for topological sort.
        /// Throws if circular dependency is detected.
        /// </summary>
        private void Visit(string paramName, HashSet<string> visited, HashSet<string> visiting, List<string> order)
        {
            if (visiting.Contains(paramName))
            {
                throw new InvalidOperationException($"Circular dependency detected involving parameter '{paramName}'");
            }

            if (visited.Contains(paramName))
            {
                return;
            }

            visiting.Add(paramName);

            // Visit dependencies first
            if (_parameters.ContainsKey(paramName))
            {
                foreach (var dep in _parameters[paramName].DependsOn)
                {
                    // Only visit if the dependency is also a dynamic parameter
                    if (_parameters.ContainsKey(dep))
                    {
                        Visit(dep, visited, visiting, order);
                    }
                }
            }

            visiting.Remove(paramName);
            visited.Add(paramName);
            order.Add(paramName);
        }

        private class DynamicParameterInfo
        {
            public string Name { get; set; }
            public UIDataSourceAttribute Attribute { get; set; }
            public List<string> DependsOn { get; set; }
        }

    }
}

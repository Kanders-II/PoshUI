// Copyright (c) 2025 A Solution IT LLC. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
using System;
using System.Management.Automation;

namespace Launcher.Attributes
{
    /// <summary>
    /// Specifies a dynamic data source for populating parameter choices.
    /// Supports PowerShell script blocks, CSV files, and parameter dependencies.
    /// </summary>
    [AttributeUsage(AttributeTargets.Parameter, AllowMultiple = false)]
    public class UIDataSourceAttribute : Attribute
    {
        /// <summary>
        /// PowerShell script block to execute for generating choices.
        /// The script block can return any enumerable collection.
        /// </summary>
        public ScriptBlock ScriptBlock { get; set; }

        /// <summary>
        /// Path to a CSV file to use as a data source.
        /// </summary>
        public string CsvPath { get; set; }

        /// <summary>
        /// Column name in the CSV file to extract values from.
        /// </summary>
        public string CsvColumn { get; set; }

        /// <summary>
        /// Script block to filter CSV rows before extracting values.
        /// Receives each row as $_ and should return $true to include it.
        /// </summary>
        public ScriptBlock CsvFilter { get; set; }

        /// <summary>
        /// Names of parameters this data source depends on.
        /// The script block will be re-executed when any dependency changes.
        /// </summary>
        public string[] DependsOn { get; set; }

        /// <summary>
        /// Whether to execute the script block asynchronously.
        /// Shows a loading indicator while executing.
        /// </summary>
        public bool Async { get; set; }

        /// <summary>
        /// Whether to show a refresh button that allows re-executing the script block.
        /// </summary>
        public bool ShowRefreshButton { get; set; }

        public UIDataSourceAttribute()
        {
            DependsOn = new string[0];
            Async = false;
            ShowRefreshButton = false;
        }

        /// <summary>
        /// Constructor for simple script block data sources.
        /// </summary>
        /// <param name="scriptBlock">Script block to execute</param>
        public UIDataSourceAttribute(ScriptBlock scriptBlock)
        {
            ScriptBlock = scriptBlock;
            DependsOn = new string[0];
        }

        /// <summary>
        /// Constructor for CSV-based data sources.
        /// </summary>
        /// <param name="csvPath">Path to CSV file</param>
        /// <param name="csvColumn">Column name to extract</param>
        public UIDataSourceAttribute(string csvPath, string csvColumn)
        {
            CsvPath = csvPath;
            CsvColumn = csvColumn;
            DependsOn = new string[0];
        }

        /// <summary>
        /// Validates that the attribute configuration is correct.
        /// </summary>
        public void Validate()
        {
            // Must have either ScriptBlock or CSV configuration
            bool hasScriptBlock = ScriptBlock != null;
            bool hasCsv = !string.IsNullOrEmpty(CsvPath) && !string.IsNullOrEmpty(CsvColumn);

            if (!hasScriptBlock && !hasCsv)
            {
                throw new InvalidOperationException(
                    "UIDataSource must specify either a ScriptBlock or CSV configuration (CsvPath and CsvColumn).");
            }

            if (hasScriptBlock && hasCsv)
            {
                throw new InvalidOperationException(
                    "UIDataSource cannot have both ScriptBlock and CSV configuration. Choose one.");
            }

            // CsvFilter only makes sense with CSV
            if (CsvFilter != null && !hasCsv)
            {
                throw new InvalidOperationException(
                    "CsvFilter can only be used with CSV data sources (requires CsvPath and CsvColumn).");
            }
        }

        /// <summary>
        /// Returns true if this data source has dependencies.
        /// </summary>
        public bool HasDependencies => DependsOn != null && DependsOn.Length > 0;

        /// <summary>
        /// Returns true if this is a CSV-based data source.
        /// </summary>
        public bool IsCsvSource => !string.IsNullOrEmpty(CsvPath) && !string.IsNullOrEmpty(CsvColumn);

        /// <summary>
        /// Returns true if this is a script block-based data source.
        /// </summary>
        public bool IsScriptBlockSource => ScriptBlock != null;
    }
}

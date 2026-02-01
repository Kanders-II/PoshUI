// Copyright (c) 2025 A Solution IT LLC. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
using System;

namespace Launcher.Services
{
    /// <summary>
    /// Configuration options for dynamic parameter execution to prevent hangs and resource exhaustion.
    /// </summary>
    public class DynamicParameterExecutionOptions
    {
        /// <summary>
        /// Maximum execution time for script blocks in seconds. Default: 30
        /// </summary>
        public int TimeoutSeconds { get; set; } = 30;

        /// <summary>
        /// Maximum number of results to return from a data source. Default: 1000
        /// </summary>
        public int MaxResults { get; set; } = 1000;

        /// <summary>
        /// Minimum execution time in milliseconds before showing progress indicator. Default: 500
        /// </summary>
        public int ProgressThresholdMs { get; set; } = 500;

        /// <summary>
        /// Whether to show progress indicator for slow operations. Default: true
        /// </summary>
        public bool ShowProgressIndicator { get; set; } = true;

        /// <summary>
        /// Enable detailed performance logging (debug mode only). Default: false
        /// </summary>
        public bool EnablePerformanceLogging { get; set; } = false;

        /// <summary>
        /// Creates default options with recommended settings.
        /// </summary>
        public static DynamicParameterExecutionOptions Default => new DynamicParameterExecutionOptions();

        /// <summary>
        /// Creates options for fast-running data sources with shorter timeout.
        /// </summary>
        public static DynamicParameterExecutionOptions Fast => new DynamicParameterExecutionOptions
        {
            TimeoutSeconds = 10,
            MaxResults = 500,
            ProgressThresholdMs = 250
        };

        /// <summary>
        /// Creates options for slow-running data sources with longer timeout.
        /// </summary>
        public static DynamicParameterExecutionOptions Slow => new DynamicParameterExecutionOptions
        {
            TimeoutSeconds = 60,
            MaxResults = 2000,
            ProgressThresholdMs = 1000
        };

        /// <summary>
        /// Validates the options and throws if any are invalid.
        /// </summary>
        public void Validate()
        {
            if (TimeoutSeconds <= 0)
                throw new ArgumentException("TimeoutSeconds must be greater than 0", nameof(TimeoutSeconds));

            if (MaxResults <= 0)
                throw new ArgumentException("MaxResults must be greater than 0", nameof(MaxResults));

            if (ProgressThresholdMs < 0)
                throw new ArgumentException("ProgressThresholdMs cannot be negative", nameof(ProgressThresholdMs));
        }
    }
}

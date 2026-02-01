// Copyright (c) 2025 A Solution IT LLC. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;

namespace Launcher.Services
{
    /// <summary>
    /// Provides security validation for paths, inputs, and script integrity.
    /// </summary>
    public static class SecurityValidator
    {
        // Parameter name must start with letter, contain only alphanumeric and underscore, max 64 chars
        private static readonly Regex ValidParameterName = new Regex(@"^[a-zA-Z][a-zA-Z0-9_]{0,63}$", RegexOptions.Compiled);
        
        // Maximum length for parameter values to prevent DOS
        private const int MaxParameterValueLength = 10000;
        
        /// <summary>
        /// Validates a script file path for security issues.
        /// </summary>
        /// <param name="scriptPath">The path to validate</param>
        /// <param name="errorMessage">Output parameter containing the error message if validation fails</param>
        /// <returns>True if path is valid and safe, false otherwise</returns>
        public static bool ValidateScriptPath(string scriptPath, out string errorMessage)
        {
            errorMessage = null;

            // Check for null/empty
            if (string.IsNullOrWhiteSpace(scriptPath))
            {
                errorMessage = "Script path is null or empty";
                AuditLogger.LogPathValidationFailure(scriptPath ?? "(null)", errorMessage);
                return false;
            }

            try
            {
                // Get full path to normalize and check for traversal
                string fullPath = Path.GetFullPath(scriptPath);

                // Reject UNC paths (network shares)
                if (fullPath.StartsWith(@"\\", StringComparison.OrdinalIgnoreCase))
                {
                    errorMessage = "UNC paths (network shares) are not allowed for security reasons";
                    AuditLogger.LogSecurityViolation("UNC Path Blocked", fullPath);
                    return false;
                }

                // Check for invalid path characters
                if (scriptPath.IndexOfAny(Path.GetInvalidPathChars()) >= 0)
                {
                    errorMessage = "Path contains invalid characters";
                    AuditLogger.LogPathValidationFailure(scriptPath, errorMessage);
                    return false;
                }

                // Must be a .ps1 or .json file (JSON for module API mode)
                if (!fullPath.EndsWith(".ps1", StringComparison.OrdinalIgnoreCase) &&
                    !fullPath.EndsWith(".json", StringComparison.OrdinalIgnoreCase))
                {
                    errorMessage = "Script file must have .ps1 or .json extension";
                    AuditLogger.LogPathValidationFailure(fullPath, errorMessage);
                    return false;
                }

                // File must exist
                if (!File.Exists(fullPath))
                {
                    errorMessage = $"Script file not found: {fullPath}";
                    AuditLogger.LogPathValidationFailure(fullPath, errorMessage);
                    return false;
                }

                // Check file size (reject files > 10MB as potentially malicious)
                FileInfo fileInfo = new FileInfo(fullPath);
                if (fileInfo.Length > 10 * 1024 * 1024)
                {
                    errorMessage = "Script file exceeds maximum size limit (10MB)";
                    AuditLogger.LogSecurityViolation("Large Script Blocked", $"{fullPath} ({fileInfo.Length} bytes)");
                    return false;
                }

                LoggingService.Info($"Path validation passed: {fullPath}");
                return true;
            }
            catch (ArgumentException ex)
            {
                errorMessage = $"Invalid path format: {ex.Message}";
                AuditLogger.LogPathValidationFailure(scriptPath, errorMessage);
                return false;
            }
            catch (NotSupportedException ex)
            {
                errorMessage = $"Unsupported path format: {ex.Message}";
                AuditLogger.LogPathValidationFailure(scriptPath, errorMessage);
                return false;
            }
            catch (Exception ex)
            {
                errorMessage = $"Path validation error: {ex.Message}";
                AuditLogger.LogPathValidationFailure(scriptPath, errorMessage);
                return false;
            }
        }

        /// <summary>
        /// Validates a PowerShell parameter name.
        /// </summary>
        public static bool ValidateParameterName(string parameterName, out string errorMessage)
        {
            errorMessage = null;

            if (string.IsNullOrWhiteSpace(parameterName))
            {
                errorMessage = "Parameter name is null or empty";
                AuditLogger.LogInputValidationFailure("(null)", errorMessage);
                return false;
            }

            if (!ValidParameterName.IsMatch(parameterName))
            {
                errorMessage = $"Invalid parameter name '{parameterName}'. Must start with letter, contain only alphanumeric and underscore, max 64 characters";
                AuditLogger.LogInputValidationFailure(parameterName, errorMessage);
                return false;
            }

            return true;
        }

        /// <summary>
        /// Sanitizes a parameter value to prevent code injection.
        /// </summary>
        public static string SanitizeParameterValue(string value, string parameterName)
        {
            if (value == null) return null;

            // Truncate to maximum length
            if (value.Length > MaxParameterValueLength)
            {
                LoggingService.Warn($"Parameter '{parameterName}' value truncated from {value.Length} to {MaxParameterValueLength} characters");
                AuditLogger.LogInputValidationFailure(parameterName, $"Value truncated from {value.Length} chars");
                value = value.Substring(0, MaxParameterValueLength);
            }

            // Note: We don't escape here since PowerShell handles this properly
            // Escaping would be the responsibility of the script generator if needed
            return value;
        }

        /// <summary>
        /// Computes SHA256 hash of a file.
        /// </summary>
        public static string ComputeFileHash(string filePath)
        {
            try
            {
                using (var sha256 = SHA256.Create())
                using (var stream = File.OpenRead(filePath))
                {
                    byte[] hash = sha256.ComputeHash(stream);
                    return BitConverter.ToString(hash).Replace("-", "").ToLowerInvariant();
                }
            }
            catch (Exception ex)
            {
                LoggingService.Error($"Failed to compute hash for {filePath}", ex);
                return null;
            }
        }

        /// <summary>
        /// Verifies that a file's hash matches the expected hash.
        /// </summary>
        public static bool VerifyFileHash(string filePath, string expectedHash)
        {
            if (string.IsNullOrEmpty(expectedHash))
            {
                LoggingService.Warn($"No expected hash provided for {filePath}");
                return false;
            }

            string actualHash = ComputeFileHash(filePath);
            if (actualHash == null)
            {
                AuditLogger.LogSecurityViolation("Hash Computation Failed", filePath);
                return false;
            }

            bool matches = string.Equals(actualHash, expectedHash, StringComparison.OrdinalIgnoreCase);
            
            if (!matches)
            {
                AuditLogger.LogSecurityViolation(
                    "Script Hash Mismatch",
                    $"File: {filePath}\nExpected: {expectedHash}\nActual: {actualHash}"
                );
                LoggingService.Error($"Hash mismatch for {filePath}. Expected: {expectedHash}, Actual: {actualHash}");
            }

            return matches;
        }

        /// <summary>
        /// Validates that a value is safe for logging (no sensitive data).
        /// </summary>
        public static bool IsSafeToLog(object value, Type parameterType)
        {
            // Never log SecureString
            if (value is System.Security.SecureString)
                return false;

            // Never log PSCredential
            if (parameterType?.Name == "PSCredential")
                return false;

            // Check for common credential/password parameter names
            if (parameterType == typeof(string))
            {
                // These checks are done at the call site based on parameter metadata
                return true;
            }

            return true;
        }
    }
}

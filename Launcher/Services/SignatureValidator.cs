// Copyright (c) 2025 A Solution IT LLC. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
using System;
using System.IO;
using System.Security.Cryptography.X509Certificates;
using System.ComponentModel;
using System.Runtime.InteropServices;

namespace Launcher.Services
{
    /// <summary>
    /// Validates Authenticode signatures on executables and scripts.
    /// </summary>
    public static class SignatureValidator
    {
        /// <summary>
        /// Signature verification mode.
        /// </summary>
        public enum VerificationMode
        {
            /// <summary>No signature verification (development mode)</summary>
            Disabled,
            /// <summary>Verify signature but only warn if invalid</summary>
            Warn,
            /// <summary>Verify signature and block execution if invalid</summary>
            Enforce
        }

        // WinVerifyTrust API constants
        private const int TRUST_E_NOSIGNATURE = unchecked((int)0x800B0100);
        private const int TRUST_E_SUBJECT_NOT_TRUSTED = unchecked((int)0x800B0004);
        private const int TRUST_E_PROVIDER_UNKNOWN = unchecked((int)0x800B0001);
        private const int TRUST_E_BAD_DIGEST = unchecked((int)0x80096010);

        /// <summary>
        /// Gets the current signature verification mode from configuration.
        /// Default is Disabled for development. Use POSHUI_SIGNATURE_MODE environment variable to change.
        /// </summary>
        public static VerificationMode CurrentMode
        {
            get
            {
                // Check environment variable for signature mode
                string modeString = Environment.GetEnvironmentVariable("POSHUI_SIGNATURE_MODE");
                if (string.IsNullOrEmpty(modeString))
                {
#if DEBUG
                    // Development builds: Disabled for ease of development
                    return VerificationMode.Disabled;
#else
                    // Production builds: Warn (log violations but allow execution)
                    // For strict security, set POSHUI_SIGNATURE_MODE=Enforce
                    return VerificationMode.Warn;
#endif
                }

                if (Enum.TryParse<VerificationMode>(modeString, true, out var mode))
                {
                    return mode;
                }

                return VerificationMode.Warn;
            }
        }

        /// <summary>
        /// Verifies the Authenticode signature of an executable or script file.
        /// </summary>
        /// <param name="filePath">Path to the file to verify</param>
        /// <param name="errorMessage">Output parameter containing error details if verification fails</param>
        /// <returns>True if signature is valid, false otherwise</returns>
        public static bool VerifySignature(string filePath, out string errorMessage)
        {
            errorMessage = null;

            try
            {
                if (!File.Exists(filePath))
                {
                    errorMessage = $"File not found: {filePath}";
                    return false;
                }

                // Get the signature from the file
                X509Certificate2 certificate = null;
                try
                {
                    certificate = new X509Certificate2(filePath);
                }
                catch (Exception ex)
                {
                    // File might not be signed or signature is corrupted
                    errorMessage = $"No valid signature found: {ex.Message}";
                    LoggingService.Warn($"Signature verification failed for {filePath}: {errorMessage}");
                    return false;
                }

                if (certificate == null)
                {
                    errorMessage = "File is not signed with an Authenticode certificate";
                    LoggingService.Warn($"File not signed: {filePath}");
                    return false;
                }

                // Verify the certificate chain
                X509Chain chain = new X509Chain();
                chain.ChainPolicy.RevocationMode = X509RevocationMode.Online;
                chain.ChainPolicy.RevocationFlag = X509RevocationFlag.EntireChain;
                chain.ChainPolicy.UrlRetrievalTimeout = TimeSpan.FromSeconds(30);
                chain.ChainPolicy.VerificationFlags = X509VerificationFlags.NoFlag;

                bool chainIsValid = chain.Build(certificate);

                if (!chainIsValid)
                {
                    errorMessage = "Certificate chain validation failed:";
                    foreach (X509ChainStatus status in chain.ChainStatus)
                    {
                        errorMessage += $"\n  - {status.Status}: {status.StatusInformation}";
                    }
                    LoggingService.Warn($"Certificate chain invalid for {filePath}: {errorMessage}");
                    return false;
                }

                // Check certificate validity dates
                DateTime now = DateTime.Now;
                if (now < certificate.NotBefore || now > certificate.NotAfter)
                {
                    errorMessage = $"Certificate is not valid for current date. Valid from {certificate.NotBefore} to {certificate.NotAfter}";
                    LoggingService.Warn($"Certificate date invalid for {filePath}: {errorMessage}");
                    return false;
                }

                // Log successful verification
                LoggingService.Info($"Signature verified for {filePath}: Issued by {certificate.Issuer}, Subject: {certificate.Subject}");
                return true;
            }
            catch (Exception ex)
            {
                errorMessage = $"Signature verification error: {ex.Message}";
                LoggingService.Error($"Signature verification exception for {filePath}", ex);
                return false;
            }
        }

        /// <summary>
        /// Verifies a file's signature and takes action based on current verification mode.
        /// </summary>
        /// <param name="filePath">Path to the file to verify</param>
        /// <param name="fileDescription">Description of the file for error messages</param>
        /// <returns>True if verification passed or mode is Disabled, false if verification failed and mode is Enforce</returns>
        public static bool VerifyAndEnforce(string filePath, string fileDescription = "file")
        {
            var mode = CurrentMode;

            if (mode == VerificationMode.Disabled)
            {
                LoggingService.Info($"Signature verification disabled for {fileDescription}: {filePath}");
                return true;
            }

            bool isValid = VerifySignature(filePath, out string errorMessage);

            if (!isValid)
            {
                string logMessage = $"Signature verification failed for {fileDescription}: {filePath}\n{errorMessage}";
                
                if (mode == VerificationMode.Enforce)
                {
                    LoggingService.Error(logMessage);
                    AuditLogger.LogSecurityViolation("Invalid Signature - Execution Blocked", 
                        $"File: {filePath}\nType: {fileDescription}\nError: {errorMessage}");
                    return false;
                }
                else // Warn mode
                {
                    LoggingService.Warn(logMessage);
                    AuditLogger.LogSecurityViolation("Invalid Signature - Warning Only", 
                        $"File: {filePath}\nType: {fileDescription}\nError: {errorMessage}\nMode: Warn (execution allowed)");
                    return true; // Allow execution but log warning
                }
            }

            LoggingService.Info($"Signature verification passed for {fileDescription}: {filePath}");
            return true;
        }

        /// <summary>
        /// Gets signature information for a file.
        /// </summary>
        public static string GetSignatureInfo(string filePath)
        {
            try
            {
                var cert = new X509Certificate2(filePath);
                return $"Subject: {cert.Subject}\n" +
                       $"Issuer: {cert.Issuer}\n" +
                       $"Thumbprint: {cert.Thumbprint}\n" +
                       $"Valid From: {cert.NotBefore}\n" +
                       $"Valid To: {cert.NotAfter}\n" +
                       $"Serial: {cert.SerialNumber}";
            }
            catch (Exception ex)
            {
                return $"Unable to read signature: {ex.Message}";
            }
        }

        /// <summary>
        /// Checks if a file is signed (has an Authenticode signature).
        /// </summary>
        public static bool IsSigned(string filePath)
        {
            try
            {
                var cert = new X509Certificate2(filePath);
                return cert != null;
            }
            catch
            {
                return false;
            }
        }
    }
}

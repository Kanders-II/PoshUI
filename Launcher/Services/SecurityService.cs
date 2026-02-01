// Copyright (c) 2025 A Solution IT LLC. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
using System;
using System.Diagnostics;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Security.Principal;
using System.Text;
using System.Runtime.Serialization.Json;
using System.IO;
using Launcher.Models;

namespace Launcher.Services
{
    public static class SecurityService
    {
        // Simple JSON serialization helper for message hashing
        private static string SerializeMessageForHashing(dynamic message)
        {
            return $"{{\"SessionId\":\"{message.SessionId}\",\"Type\":\"{message.Type}\",\"Timestamp\":\"{message.Timestamp:O}\",\"Payload\":{(message.Payload != null ? $"\"{message.Payload}\"" : "null")},\"Error\":{(message.Error != null ? $"\"{message.Error}\"" : "null")},\"AuthToken\":\"{message.AuthToken}\",\"ClientProcessId\":{message.ClientProcessId}}}";
        }
        
        public static X509Certificate2 GenerateSessionCertificate()
        {
            try
            {
                using (var rsa = RSA.Create(2048))
                {
                    var request = new CertificateRequest(
                        "CN=PoshUI-Session", 
                        rsa, 
                        HashAlgorithmName.SHA256, 
                        RSASignaturePadding.Pkcs1);
                        
                    request.CertificateExtensions.Add(
                        new X509KeyUsageExtension(
                            X509KeyUsageFlags.DigitalSignature | X509KeyUsageFlags.KeyEncipherment,
                            false));
                            
                    // Short-lived certificate (1 hour)
                    var certificate = request.CreateSelfSigned(DateTime.Now, DateTime.Now.AddHours(1));
                    
                    LoggingService.Info("Session certificate generated successfully", "Security");
                    return certificate;
                }
            }
            catch (Exception ex)
            {
                LoggingService.Error("Failed to generate session certificate", ex, "Security");
                throw;
            }
        }
        
        public static string GenerateSessionToken()
        {
            using (var rng = RandomNumberGenerator.Create())
            {
                var bytes = new byte[32];
                rng.GetBytes(bytes);
                return Convert.ToBase64String(bytes);
            }
        }
        
        public static bool ValidateProcessOwnership(int processId)
        {
            try
            {
                var targetProcess = Process.GetProcessById(processId);
                var currentProcess = Process.GetCurrentProcess();
                
                // Both processes must be running as the same user
                var currentUser = WindowsIdentity.GetCurrent().User;
                var targetUser = GetProcessUser(targetProcess);
                
                bool isValid = currentUser?.Value == targetUser?.Value;
                
                LoggingService.Debug($"Process ownership validation for PID {processId}: {isValid}", "Security");
                return isValid;
            }
            catch (Exception ex)
            {
                LoggingService.Warn($"Process validation failed for PID {processId}: {ex.Message}", "Security");
                return false;
            }
        }
        
        public static SecurityIdentifier GetProcessUser(Process process)
        {
            try
            {
                // Use WMI for accurate process owner retrieval
                using (var searcher = new System.Management.ManagementObjectSearcher(
                    $"SELECT * FROM Win32_Process WHERE ProcessId = {process.Id}"))
                {
                    foreach (System.Management.ManagementObject obj in searcher.Get())
                    {
                        // Invoke GetOwner method
                        var outParams = obj.InvokeMethod("GetOwner", null, null);

                        if (outParams != null)
                        {
                            string domain = outParams["Domain"]?.ToString();
                            string user = outParams["User"]?.ToString();

                            if (!string.IsNullOrEmpty(user) && !string.IsNullOrEmpty(domain))
                            {
                                string accountName = $"{domain}\\{user}";
                                var account = new System.Security.Principal.NTAccount(accountName);
                                return (SecurityIdentifier)account.Translate(typeof(SecurityIdentifier));
                            }
                        }
                    }
                }

                // Fallback: If WMI fails, return null
                return null;
            }
            catch (Exception ex)
            {
                LoggingService.Debug($"Failed to get process user via WMI for PID {process.Id}: {ex.Message}", "Security");
                return null;
            }
        }
        
        public static string HashMessage(string message, string secret)
        {
            using (var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(secret)))
            {
                var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(message));
                return Convert.ToBase64String(hash);
            }
        }
        
        public static bool VerifyMessageHash(string message, string hash, string secret)
        {
            var expectedHash = HashMessage(message, secret);
            return expectedHash == hash;
        }
        
        public static UIMessage SignMessage(UIMessage message, X509Certificate2 certificate, string sessionSecret)
        {
            try
            {
                // Create hash of message content without the hash field
                var messageForHashing = new UIMessage
                {
                    MessageId = message.MessageId,
                    SessionId = message.SessionId,
                    Type = message.Type,
                    Timestamp = message.Timestamp,
                    Payload = message.Payload,
                    Error = message.Error,
                    AuthToken = message.AuthToken,
                    ClientProcessId = message.ClientProcessId
                };
                
                var json = SerializeMessageForHashing(messageForHashing);
                
                // Sign with certificate
                using (var rsa = certificate.GetRSAPrivateKey())
                {
                    var signature = rsa.SignData(Encoding.UTF8.GetBytes(json), HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
                    message.MessageHash = Convert.ToBase64String(signature);
                }
                
                LoggingService.Debug($"Message {message.MessageId} signed successfully", "Security");
                return message;
            }
            catch (Exception ex)
            {
                LoggingService.Error($"Failed to sign message {message.MessageId}", ex, "Security");
                throw;
            }
        }
        
        public static bool VerifyMessageSignature(UIMessage message, X509Certificate2 certificate)
        {
            try
            {
                if (string.IsNullOrEmpty(message.MessageHash))
                {
                    LoggingService.Warn($"Message {message.MessageId} has no signature", "Security");
                    return false;
                }

                // Recreate message without hash for verification
                var messageForVerification = new UIMessage
                {
                    MessageId = message.MessageId,
                    SessionId = message.SessionId,
                    Type = message.Type,
                    Timestamp = message.Timestamp,
                    Payload = message.Payload,
                    Error = message.Error,
                    AuthToken = message.AuthToken,
                    ClientProcessId = message.ClientProcessId
                };
                
                var json = SerializeMessageForHashing(messageForVerification);
                var signature = Convert.FromBase64String(message.MessageHash);
                
                // Verify with certificate
                using (var rsa = certificate.GetRSAPublicKey())
                {
                    bool isValid = rsa.VerifyData(Encoding.UTF8.GetBytes(json), signature, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
                    
                    LoggingService.Debug($"Message {message.MessageId} signature verification: {isValid}", "Security");
                    return isValid;
                }
            }
            catch (Exception ex)
            {
                LoggingService.Error($"Failed to verify message {message.MessageId} signature", ex, "Security");
                return false;
            }
        }
        
        public static byte[] EncryptData(byte[] data, X509Certificate2 certificate)
        {
            try
            {
                using (var rsa = certificate.GetRSAPublicKey())
                {
                    return rsa.Encrypt(data, RSAEncryptionPadding.OaepSHA256);
                }
            }
            catch (Exception ex)
            {
                LoggingService.Error("Failed to encrypt data", ex, "Security");
                throw;
            }
        }
        
        public static byte[] DecryptData(byte[] encryptedData, X509Certificate2 certificate)
        {
            try
            {
                using (var rsa = certificate.GetRSAPrivateKey())
                {
                    return rsa.Decrypt(encryptedData, RSAEncryptionPadding.OaepSHA256);
                }
            }
            catch (Exception ex)
            {
                LoggingService.Error("Failed to decrypt data", ex, "Security");
                throw;
            }
        }
        
        public static string GetCertificateFingerprint(X509Certificate2 certificate)
        {
            return certificate.Thumbprint;
        }
        
        public static bool IsValidSessionCertificate(X509Certificate2 certificate)
        {
            try
            {
                // Check if certificate is expired
                if (DateTime.Now > certificate.NotAfter || DateTime.Now < certificate.NotBefore)
                {
                    LoggingService.Warn("Certificate is expired or not yet valid", "Security");
                    return false;
                }
                
                // Check subject
                if (!certificate.Subject.Contains("PoshUI-Session"))
                {
                    LoggingService.Warn("Certificate subject is invalid", "Security");
                    return false;
                }
                
                return true;
            }
            catch (Exception ex)
            {
                LoggingService.Error("Failed to validate certificate", ex, "Security");
                return false;
            }
        }
    }
}

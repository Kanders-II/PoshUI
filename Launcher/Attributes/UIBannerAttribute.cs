// Copyright (c) 2025 A Solution IT LLC. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System;

namespace Launcher.Attributes
{
    /// <summary>
    /// Attribute that contains JSON-serialized banner definition for wizard steps.
    /// Applied to placeholder parameters in generated UI scripts.
    /// </summary>
    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field, AllowMultiple = false)]
    public class UIBannerAttribute : Attribute
    {
        /// <summary>
        /// JSON string containing banner definition (Base64 encoded).
        /// </summary>
        public string BannerJson { get; }

        /// <summary>
        /// Creates a new UIBannerAttribute with the specified JSON data.
        /// </summary>
        /// <param name="bannerJson">JSON banner definition (may be Base64 encoded with 'BASE64:' prefix).</param>
        public UIBannerAttribute(string bannerJson)
        {
            BannerJson = bannerJson ?? string.Empty;
        }
    }
}

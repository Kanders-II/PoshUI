// Copyright (c) 2025 A Solution IT LLC. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System;

namespace Launcher.Attributes
{
    /// <summary>
    /// Attribute that contains JSON-serialized script card definitions for CardGrid view mode.
    /// Applied to placeholder parameters in generated UI scripts.
    /// </summary>
    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field, AllowMultiple = false)]
    public class UIScriptCardsAttribute : Attribute
    {
        /// <summary>
        /// JSON string containing an array of script card definitions.
        /// </summary>
        public string CardsJson { get; }

        /// <summary>
        /// Creates a new UIScriptCardsAttribute with the specified JSON data.
        /// </summary>
        /// <param name="cardsJson">JSON array of script card definitions.</param>
        public UIScriptCardsAttribute(string cardsJson)
        {
            CardsJson = cardsJson ?? string.Empty;
        }
    }
}



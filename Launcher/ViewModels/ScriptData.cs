// Copyright (c) 2025 A Solution IT LLC. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
using System.Collections.Generic;
using Launcher.Services; // Assuming WizardStep is in Launcher.Services

namespace Launcher.ViewModels
{
    public class ScriptData
    {
        public List<WizardStep> WizardSteps { get; set; } = new List<WizardStep>();
        public WizardBranding Branding { get; set; }
        public string SidebarHeaderIconOrientation { get; set; }
        public string ScriptBody { get; set; }
    }
}
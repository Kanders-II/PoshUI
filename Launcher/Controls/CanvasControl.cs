// Copyright (c) 2025 Kanders-II. All rights reserved.
// Licensed under the MIT License.
using System;
using System.Collections.Generic;
using System.Windows;
using Launcher.Models;

namespace Launcher.Controls
{
    /// <summary>A built canvas control: its WPF element plus value get/set hooks and its definition.
    /// The CanvasBridge registers these by Name so in-app scriptblocks can read/update them.</summary>
    public class CanvasControl
    {
        public FrameworkElement Element;
        public string Name;
        public UIControlJson Def;
        public Func<object> GetValue;
        public Action<object> SetValue;
        public readonly List<CanvasControl> Children = new List<CanvasControl>();

        // ── Engine-native Workflow control (Type="Workflow", headless) ──
        // BuildWorkflow builds this WorkflowViewModel from the step list but renders nothing; the module (-Engine)
        // renders the visible step cards/header/run button. The bridge's StartWorkflow hosts a WorkflowExecutor
        // over the VM (its own runspace) and mirrors task state onto the named cards. ExpectedSeconds drive the
        // weighted gauge % at step boundaries.
        public Launcher.ViewModels.WorkflowViewModel Workflow;
        public System.Collections.Generic.List<int> WorkflowExpectedSeconds;
    }
}

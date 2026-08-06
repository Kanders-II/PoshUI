// Copyright (c) 2025 Kanders-II. All rights reserved.
// Licensed under the MIT License.
using System.Collections.Generic;
using Launcher.Models;
using Launcher.Services;

namespace Launcher.ViewModels
{
    /// <summary>View-model for a free-form Canvas page. Carries the raw control definitions; the
    /// CanvasView builds the WPF controls via <see cref="Launcher.Controls.CanvasControlFactory"/>.</summary>
    public class CanvasViewModel
    {
        public string Title { get; private set; }
        public List<UIControlJson> Controls { get; private set; }
        public Launcher.Controls.CanvasBridge Bridge { get; private set; }

        // Page-layout root: null/empty/"Canvas" = absolute free-form; Dock/Grid/VStack/HStack/Wrap = responsive.
        public string PageLayout { get; private set; }
        public int PageColumns { get; private set; }
        public string PageColumnWidths { get; private set; }
        public double PageSpacing { get; private set; }
        public string PagePadding { get; private set; }

        /// <summary>True when the page should reflow in a layout panel rather than an absolute Canvas.</summary>
        public bool IsLayoutRoot =>
            !string.IsNullOrEmpty(PageLayout) && !PageLayout.Equals("Canvas", System.StringComparison.OrdinalIgnoreCase);

        public CanvasViewModel(WizardStep step, Launcher.Controls.CanvasBridge bridge)
        {
            Title = step != null ? step.Title : null;
            Controls = (step != null ? step.Controls as List<UIControlJson> : null) ?? new List<UIControlJson>();
            Bridge = bridge;
            if (step != null)
            {
                PageLayout = step.PageLayout;
                PageColumns = step.PageColumns;
                PageColumnWidths = step.PageColumnWidths;
                PageSpacing = step.PageSpacing;
                PagePadding = step.PagePadding;
            }
        }
    }
}

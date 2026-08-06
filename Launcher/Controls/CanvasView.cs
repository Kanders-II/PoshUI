// Copyright (c) 2025 Kanders-II. All rights reserved.
// Licensed under the MIT License.
using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using Launcher.Models;
using Launcher.ViewModels;

namespace Launcher.Controls
{
    /// <summary>Hosts a Canvas page. Two modes:
    /// <list type="bullet">
    /// <item><b>Absolute</b> (default): free-form X/Y controls on a scrollable <see cref="Canvas"/>.</item>
    /// <item><b>Layout root</b> (page declares -Layout Dock/Grid/VStack/HStack/Wrap): controls reflow
    /// responsively in a layout panel that fills the window.</item>
    /// </list>
    /// Both build controls via <see cref="CanvasControlFactory"/>. Code-only UserControl (no XAML).</summary>
    public class CanvasView : UserControl
    {
        private readonly Canvas _surface = new Canvas();
        private readonly ScrollViewer _scroller;
        private CanvasViewModel _builtVm;

        public CanvasView()
        {
            _scroller = new ScrollViewer
            {
                HorizontalScrollBarVisibility = ScrollBarVisibility.Auto,
                VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
                Content = _surface
            };
            CanvasControlFactory.ApplyCanvasScroll(_scroller);
            HorizontalAlignment = HorizontalAlignment.Stretch;
            VerticalAlignment = VerticalAlignment.Stretch;
            DataContextChanged += (s, e) => Rebuild();
            Loaded += (s, e) => { Rebuild(); FitWindow(); WireShortcuts(); };
        }

        private bool _shortcutsWired;
        /// <summary>Forwards window key presses to the bridge so Add-UICanvasShortcut gestures fire their actions.</summary>
        private void WireShortcuts()
        {
            if (_shortcutsWired) return;
            var win = System.Windows.Window.GetWindow(this);
            var bridge = (DataContext as CanvasViewModel)?.Bridge;
            if (win == null || bridge == null) return;
            _shortcutsWired = true;
            System.Windows.Input.KeyEventHandler h = (s, e) => bridge.HandleKey(e);
            win.PreviewKeyDown += h;
            Unloaded += (s, e) => { try { win.PreviewKeyDown -= h; } catch { } };
        }

        private void Rebuild()
        {
            var vm = DataContext as CanvasViewModel;
            if (vm == null || vm.Controls == null) return;
            if (ReferenceEquals(_builtVm, vm)) return;   // build once per page (avoids duplicate registration/timers)
            _builtVm = vm;

            var bridge = vm.Bridge;
            Action<string, string> onEvent = bridge != null ? (Action<string, string>)((n, ev) => bridge.RunEvent(n, ev)) : null;

            if (vm.IsLayoutRoot)
                BuildLayoutRoot(vm, bridge, onEvent);
            else
                BuildAbsolute(vm, bridge, onEvent);

            FadeIn();   // subtle page-appear motion
        }

        /// <summary>Fades + lifts the page in when it's shown (built once per page).</summary>
        private void FadeIn()
        {
            var fade = new System.Windows.Media.Animation.DoubleAnimation(0, 1,
                new System.Windows.Duration(System.TimeSpan.FromMilliseconds(220)))
            { EasingFunction = new System.Windows.Media.Animation.CubicEase { EasingMode = System.Windows.Media.Animation.EasingMode.EaseOut } };
            BeginAnimation(OpacityProperty, fade);
        }

        /// <summary>Layout-root mode: synthesize a page-level container and let the factory's layout engine
        /// (Dock/Grid/Stack/Wrap) reflow the page's controls to fill the window.</summary>
        private void BuildLayoutRoot(CanvasViewModel vm, CanvasBridge bridge, Action<string, string> onEvent)
        {
            var props = new Dictionary<string, object> { { "Layout", vm.PageLayout } };
            if (vm.PageColumns > 0) props["Columns"] = vm.PageColumns;
            if (!string.IsNullOrEmpty(vm.PageColumnWidths)) props["ColumnWidths"] = vm.PageColumnWidths;
            if (vm.PageSpacing > 0) props["Spacing"] = vm.PageSpacing;
            if (!string.IsNullOrEmpty(vm.PagePadding)) props["Padding"] = vm.PagePadding;

            var pageContainer = new UIControlJson { Type = "Panel", Children = vm.Controls, Properties = props };
            CanvasControl cc;
            try { cc = CanvasControlFactory.Build(pageContainer, onEvent); }
            catch { return; }

            var root = cc.Element;
            root.HorizontalAlignment = HorizontalAlignment.Stretch;
            root.VerticalAlignment = VerticalAlignment.Stretch;

            // Make tall content scrollable. For a Dock root, scroll only the fill child so docked edges
            // (e.g. a nav rail) stay pinned; otherwise scroll the whole page.
            var dock = (root as Border)?.Child as DockPanel ?? root as DockPanel;
            if (dock != null && dock.Children.Count > 0)
            {
                int last = dock.Children.Count - 1;
                var fill = dock.Children[last] as FrameworkElement;
                if (fill != null)
                {
                    dock.Children.RemoveAt(last);
                    dock.Children.Add(WrapScroll(fill));
                }
                Content = root;
            }
            else
            {
                Content = WrapScroll(root);
            }
            if (bridge != null) RegisterTree(cc, bridge);
        }

        private static ScrollViewer WrapScroll(FrameworkElement content)
        {
            var sv = new ScrollViewer
            {
                VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
                HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled,  // keep horizontal reflow
                Content = content
            };
            CanvasControlFactory.ApplyCanvasScroll(sv);
            return sv;
        }

        /// <summary>Absolute mode: each control positioned by its own X/Y on a scrollable surface.</summary>
        private void BuildAbsolute(CanvasViewModel vm, CanvasBridge bridge, Action<string, string> onEvent)
        {
            Content = _scroller;
            _surface.Children.Clear();

            double w = 400, h = 300;
            foreach (var c in vm.Controls)
            {
                try
                {
                    var cc = CanvasControlFactory.Build(c, onEvent);
                    _surface.Children.Add(cc.Element);
                    if (bridge != null) RegisterTree(cc, bridge);
                    w = Math.Max(w, c.X + (c.Width > 0 ? c.Width : 200) + 24);
                    h = Math.Max(h, c.Y + (c.Height > 0 ? c.Height : 40) + 24);
                }
                catch { /* one bad control shouldn't break the page */ }
            }
            _surface.Width = w;
            _surface.Height = h;
        }

        /// <summary>Registers a control and all its descendants with the bridge so Set-UICanvas*/events
        /// can address nested controls by Name.</summary>
        private static void RegisterTree(CanvasControl cc, CanvasBridge bridge)
        {
            if (cc == null) return;
            bridge.Register(cc);
            if (cc.Children != null)
                foreach (var child in cc.Children) RegisterTree(child, bridge);
        }

        /// <summary>Grows the host window to fit absolute-mode content (capped to the screen), so wide
        /// dashboards/consoles aren't clipped behind a too-small default window.</summary>
        private void FitWindow()
        {
            var vm = DataContext as CanvasViewModel;
            if (vm != null && vm.IsLayoutRoot) return;   // layout roots fill the existing window
            var win = Window.GetWindow(this);
            if (win == null || win.WindowState == WindowState.Maximized) return;
            var wa = SystemParameters.WorkArea;
            double wantW = Math.Min(_surface.Width + 80, wa.Width);
            double wantH = Math.Min(_surface.Height + 140, wa.Height);
            if (wantW > win.Width) win.Width = wantW;
            if (wantH > win.Height) win.Height = wantH;
            if (win.Left + win.Width > wa.Right) win.Left = Math.Max(wa.Left, wa.Right - win.Width);
            if (win.Top + win.Height > wa.Bottom) win.Top = Math.Max(wa.Top, wa.Bottom - win.Height);
        }
    }
}

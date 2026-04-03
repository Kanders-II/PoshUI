// Copyright (c) 2025 Kanders-II. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;
using Launcher.ViewModels;

namespace Launcher.Controls
{
    /// <summary>
    /// A ContentControl that plays a fade-out / fade-in + slide animation when
    /// content changes. Uses the standard WPF content model (single internal
    /// ContentPresenter via the default template) so DynamicResource resolution
    /// and implicit DataTemplate matching work exactly as with a plain ContentControl.
    /// Respects the global AnimationsEnabled flag.
    /// </summary>
    public class TransitionContentControl : ContentControl
    {
        private bool _isFirstContent = true;
        private bool _isAnimating;

        // Track navigation direction for slide direction
        public static readonly DependencyProperty NavigationDirectionProperty =
            DependencyProperty.Register("NavigationDirection", typeof(NavigationDirection),
                typeof(TransitionContentControl), new PropertyMetadata(NavigationDirection.Forward));

        public NavigationDirection NavigationDirection
        {
            get { return (NavigationDirection)GetValue(NavigationDirectionProperty); }
            set { SetValue(NavigationDirectionProperty, value); }
        }

        public TransitionContentControl()
        {
            // Ensure we have a RenderTransform for slide animations
            this.RenderTransform = new TranslateTransform(0, 0);
        }

        protected override void OnContentChanged(object oldContent, object newContent)
        {
            base.OnContentChanged(oldContent, newContent);

            // Skip animation for the very first content load
            if (_isFirstContent)
            {
                _isFirstContent = false;
                return;
            }

            // If animations are disabled, nothing to do — base already swapped content
            if (!MainWindowViewModel.AnimationsEnabled)
                return;

            if (newContent == null)
                return;

            // If we're mid-animation, cancel it and finalize
            if (_isAnimating)
            {
                this.BeginAnimation(OpacityProperty, null);
                GetTranslateTransform().BeginAnimation(TranslateTransform.XProperty, null);
                this.Opacity = 1;
                GetTranslateTransform().X = 0;
                _isAnimating = false;
            }

            // Phase 1: Fade out + slide out the OLD content (which base already replaced,
            // so we need to temporarily put old content back, animate out, then swap)
            // 
            // Simpler approach: animate the control itself.
            // Since base.OnContentChanged already set the new content, we do a
            // fade-in + slide-in animation on the whole control.
            _isAnimating = true;

            bool isForward = NavigationDirection == NavigationDirection.Forward;
            double slideOffset = isForward ? 40.0 : -40.0;

            var fadeDuration = TimeSpan.FromMilliseconds(350);
            var slideDuration = TimeSpan.FromMilliseconds(400);
            var easing = new CubicEase { EasingMode = EasingMode.EaseOut };

            // Start from invisible + offset
            this.Opacity = 0;
            GetTranslateTransform().X = slideOffset;

            // Fade in
            var fadeIn = new DoubleAnimation(0, 1, fadeDuration) { EasingFunction = easing };
            fadeIn.Completed += (s, e) =>
            {
                _isAnimating = false;
                // Clear the animation so the property can be set directly again
                this.BeginAnimation(OpacityProperty, null);
                this.Opacity = 1;
            };

            // Slide in
            var slideIn = new DoubleAnimation(slideOffset, 0, slideDuration) { EasingFunction = easing };
            slideIn.Completed += (s, e) =>
            {
                GetTranslateTransform().BeginAnimation(TranslateTransform.XProperty, null);
                GetTranslateTransform().X = 0;
            };

            this.BeginAnimation(OpacityProperty, fadeIn);
            GetTranslateTransform().BeginAnimation(TranslateTransform.XProperty, slideIn);
        }

        private TranslateTransform GetTranslateTransform()
        {
            var tt = this.RenderTransform as TranslateTransform;
            if (tt == null)
            {
                tt = new TranslateTransform(0, 0);
                this.RenderTransform = tt;
            }
            return tt;
        }
    }

    public enum NavigationDirection
    {
        Forward,
        Backward
    }
}

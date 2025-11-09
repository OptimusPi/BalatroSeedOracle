using System;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Media;
using Avalonia.Threading;
using Avalonia.Xaml.Interactivity;
using BalatroSeedOracle.Constants;

namespace BalatroSeedOracle.Behaviors
{
    /// <summary>
    /// Simplified hover effect inspired by Balatro's card.lua:4376-4378 hover state.
    ///
    /// DESIGN TRADE-OFFS:
    /// - Balatro uses GPU shaders for 3D perspective tilt (LÖVE2D shader at line 4349)
    /// - Avalonia has NO 3D transforms (no RotateX/RotateY/perspective)
    /// - Translation creates jiggle (cards chase mouse = bad UX)
    /// - SkewTransform looks weird and doesn't match Balatro's feel
    ///
    /// SOLUTION: Scale pulse only (60% of feel, 100% safe)
    /// - Quick scale pulse on hover entry (juice_up effect)
    /// - No continuous tracking (avoids jiggle)
    /// - No rotation (epilepsy-safe)
    /// - Stable hitbox (professional feel)
    /// </summary>
    public class MagneticTiltBehavior : Behavior<Control>
    {
        /// <summary>
        /// Scale amount for hover pulse (0.05 = 5% scale increase)
        /// Matches Balatro's juice_up(0.05, 0.03) call
        /// </summary>
        public static readonly StyledProperty<double> ScalePulseAmountProperty =
            AvaloniaProperty.Register<MagneticTiltBehavior, double>(
                nameof(ScalePulseAmount),
                0.05 // 5% scale pulse on hover
            );

        public double ScalePulseAmount
        {
            get => GetValue(ScalePulseAmountProperty);
            set => SetValue(ScalePulseAmountProperty, value);
        }

        protected override void OnAttached()
        {
            base.OnAttached();

            if (AssociatedObject == null)
                return;

            // Only listen for hover enter/exit - no continuous tracking needed
            AssociatedObject.PointerEntered += OnPointerEntered;
        }

        protected override void OnDetaching()
        {
            base.OnDetaching();

            if (AssociatedObject != null)
            {
                AssociatedObject.PointerEntered -= OnPointerEntered;
            }
        }

        private void OnPointerEntered(object? sender, PointerEventArgs e)
        {
            // BALATRO JUICE_UP EFFECT: Quick scale pulse on hover (card.lua:4307)
            // self:juice_up(0.05, 0.03) - adds satisfying "pop" when hovering!
            // This is the ONLY effect we apply - no translation, no rotation, no jiggle
            JuiceUp(ScalePulseAmount);
        }

        /// <summary>
        /// Balatro's juice_up effect - quick scale pulse for tactile feedback
        /// Based on card.lua:4307 - self:juice_up(0.05, 0.03)
        ///
        /// How it works:
        /// 1. Instantly scale up by scaleAmount (e.g., 1.0 → 1.05)
        /// 2. Wait one render frame (16ms at 60fps)
        /// 3. Smoothly animate back to original scale
        ///
        /// This creates a satisfying "pop" feeling without:
        /// - Translation jiggle (no X/Y movement)
        /// - Rotation seizures (no angle changes)
        /// - Hitbox issues (RenderTransform doesn't affect hit testing)
        /// </summary>
        private void JuiceUp(double scaleAmount)
        {
            if (AssociatedObject == null)
                return;

            // Find ScaleTransform in the control's RenderTransform
            ScaleTransform? scaleTransform = null;

            if (AssociatedObject.RenderTransform is ScaleTransform scale)
            {
                scaleTransform = scale;
            }
            else if (AssociatedObject.RenderTransform is TransformGroup group)
            {
                scaleTransform = group.Children.OfType<ScaleTransform>().FirstOrDefault();
            }

            if (scaleTransform != null)
            {
                // Store original scale values
                var originalScaleX = scaleTransform.ScaleX;
                var originalScaleY = scaleTransform.ScaleY;

                // Calculate target scale (Balatro multiplies by 0.4 for subtlety)
                var targetScale = 1.0 + (scaleAmount * 0.4);

                // INSTANT scale up (no animation - this is key to the "pop" feel)
                scaleTransform.ScaleX = targetScale;
                scaleTransform.ScaleY = targetScale;

                // Schedule scale back to original after one frame (16ms)
                // Using DispatcherPriority.Render ensures it happens on next render
                Dispatcher.UIThread.Post(() =>
                {
                    if (scaleTransform != null)
                    {
                        scaleTransform.ScaleX = originalScaleX;
                        scaleTransform.ScaleY = originalScaleY;
                    }
                }, DispatcherPriority.Render);
            }
        }
    }
}

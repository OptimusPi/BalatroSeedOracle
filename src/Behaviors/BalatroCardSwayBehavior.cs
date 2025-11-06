using System;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Rendering.Composition;
using Avalonia.Threading;
using Avalonia.Xaml.Interactivity;
using BalatroSeedOracle.Constants;

namespace BalatroSeedOracle.Behaviors
{
    /// <summary>
    /// Balatro-style card sway animation - EXACTLY like the real game!
    /// Uses ambient tilt (breathing motion) based on cos/sin waves
    /// </summary>
    public class BalatroCardSwayBehavior : Behavior<Control>
    {
        private DispatcherTimer? _animationTimer;
        private DateTime _startTime;
        private double _cardId; // Unique ID for timing variation

        /// <summary>
        /// Ambient tilt strength (0.2 = Balatro default)
        /// </summary>
        public static readonly StyledProperty<double> AmbientTiltProperty =
            AvaloniaProperty.Register<BalatroCardSwayBehavior, double>(
                nameof(AmbientTilt),
                UIConstants.CardAmbientTiltRadians
            );

        public double AmbientTilt
        {
            get => GetValue(AmbientTiltProperty);
            set => SetValue(AmbientTiltProperty, value);
        }

        protected override void OnAttached()
        {
            base.OnAttached();

            if (AssociatedObject == null)
                return;

            // Generate unique card ID for timing variation (like Balatro)
            _cardId = new Random().NextDouble() * 100;
            _startTime = DateTime.Now;

            // Set up render transform if not already set
            // If there's already a TransformGroup (from ResponsiveCard setup), we'll use that
            // Otherwise create a new RotateTransform
            if (AssociatedObject.RenderTransform == null)
            {
                AssociatedObject.RenderTransformOrigin = new RelativePoint(
                    0.5,
                    0.5,
                    RelativeUnit.Relative
                );
                AssociatedObject.RenderTransform = new RotateTransform();
            }

            // Start animation timer (60 FPS like Balatro)
            _animationTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromMilliseconds(UIConstants.AnimationFrameRateMs), // ~60 FPS
            };
            _animationTimer.Tick += OnAnimationTick;
            _animationTimer.Start();
        }

        protected override void OnDetaching()
        {
            base.OnDetaching();

            _animationTimer?.Stop();
            _animationTimer = null;
        }

        private void OnAnimationTick(object? sender, EventArgs e)
        {
            if (AssociatedObject == null)
                return;

            // CRITICAL FIX: STOP ambient sway when card is being hovered
            // This prevents the seizure-inducing flicker when hovering between cards
            // Walk up visual tree to find ResponsiveCard parent
            var parent = AssociatedObject.Parent;
            while (parent != null)
            {
                if (parent is Components.ResponsiveCard card)
                {
                    if (card.IsHovering)
                    {
                        // IMMEDIATELY reset rotation to 0 when hovering starts
                        // This prevents jiggle caused by rotation moving hitbox edge
                        ResetRotation();
                        return; // Stop animation when hovering
                    }
                    break;
                }
                parent = parent.Parent;
            }

            // Find the RotateTransform - either standalone or in a TransformGroup
            RotateTransform? rotateTransform = null;

            if (AssociatedObject.RenderTransform is RotateTransform rotate)
            {
                rotateTransform = rotate;
            }
            else if (AssociatedObject.RenderTransform is TransformGroup group)
            {
                // Look for RotateTransform in the group (should be second child in ResponsiveCard)
                rotateTransform = group.Children.OfType<RotateTransform>().FirstOrDefault();
            }

            if (rotateTransform == null)
                return;

            // Calculate elapsed time (like G.TIMERS.REAL in Balatro)
            var elapsedSeconds = (DateTime.Now - _startTime).TotalSeconds;

            // Balatro's ambient tilt formula from card.lua:4380
            // local tilt_angle = G.TIMERS.REAL*(1.56 + (self.ID/1.14212)%1) + self.ID/1.35122
            var tilt_angle = elapsedSeconds * (1.56 + (_cardId / 1.14212) % 1) + _cardId / 1.35122;

            // Tilt amount based on cos wave (creates breathing effect) from card.lua:4383
            // self.tilt_var.amt = self.ambient_tilt*(0.5+math.cos(tilt_angle))*tilt_factor
            var tilt_amt =
                AmbientTilt * (0.5 + Math.Cos(tilt_angle)) * UIConstants.CardTiltFactorRadians;

            // Apply rotation (convert to degrees)
            // Balatro rotates in radians, we need degrees
            rotateTransform.Angle = tilt_amt * UIConstants.CardRotationToDegrees; // Scale for visibility
        }

        /// <summary>
        /// Immediately reset rotation to 0 to prevent flicker/jiggle
        /// </summary>
        private void ResetRotation()
        {
            if (AssociatedObject == null)
                return;

            RotateTransform? rotateTransform = null;

            if (AssociatedObject.RenderTransform is RotateTransform rotate)
            {
                rotateTransform = rotate;
            }
            else if (AssociatedObject.RenderTransform is TransformGroup group)
            {
                rotateTransform = group.Children.OfType<RotateTransform>().FirstOrDefault();
            }

            if (rotateTransform != null)
            {
                rotateTransform.Angle = 0;
            }
        }
    }
}

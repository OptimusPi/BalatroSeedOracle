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

            // Find the TranslateTransform - either standalone or in a TransformGroup
            TranslateTransform? translateTransform = null;

            if (AssociatedObject.RenderTransform is TranslateTransform translate)
            {
                translateTransform = translate;
            }
            else if (AssociatedObject.RenderTransform is TransformGroup group)
            {
                // Look for TranslateTransform in the group (should be second child in ResponsiveCard)
                translateTransform = group.Children.OfType<TranslateTransform>().FirstOrDefault();
            }

            if (translateTransform == null)
                return;

            // Find ResponsiveCard parent to check hover state
            var parent = AssociatedObject.Parent;
            Components.ResponsiveCard? card = null;
            while (parent != null)
            {
                if (parent is Components.ResponsiveCard responsiveCard)
                {
                    card = responsiveCard;
                    break;
                }
                parent = parent.Parent;
            }

            if (card != null && card.IsHovering)
            {
                // When hovering, let MagneticTiltBehavior handle the lean
                // Don't interfere with the magnetic lean effect
                return;
            }

            // AMBIENT MODE - Breathing motion when not hovering
            // Calculate elapsed time (like G.TIMERS.REAL in Balatro)
            var elapsedSeconds = (DateTime.Now - _startTime).TotalSeconds;

            // Balatro's ambient tilt formula from card.lua:4380-4383
            // Creates circular breathing motion using cos/sin
            var tilt_angle = elapsedSeconds * (1.56 + (_cardId / 1.14212) % 1) + _cardId / 1.35122;

            // Calculate X and Y offsets for subtle circular sway (like breathing)
            // Using cos/sin creates circular motion
            var sway_x = AmbientTilt * Math.Cos(tilt_angle) * UIConstants.CardTiltFactorRadians * 2;
            var sway_y = AmbientTilt * Math.Sin(tilt_angle) * UIConstants.CardTiltFactorRadians * 2;

            // Apply translation (creates subtle circular breathing motion)
            translateTransform.X = sway_x;
            translateTransform.Y = sway_y;
        }

    }
}

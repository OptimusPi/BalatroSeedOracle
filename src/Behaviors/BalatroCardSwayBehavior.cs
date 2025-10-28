using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Rendering.Composition;
using Avalonia.Threading;
using Avalonia.Xaml.Interactivity;

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
            AvaloniaProperty.Register<BalatroCardSwayBehavior, double>(nameof(AmbientTilt), 0.2);

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

            // Set up render transform
            AssociatedObject.RenderTransformOrigin = new RelativePoint(
                0.5,
                0.5,
                RelativeUnit.Relative
            );
            AssociatedObject.RenderTransform = new RotateTransform();

            // Start animation timer (60 FPS like Balatro)
            _animationTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromMilliseconds(16.67), // ~60 FPS
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
            if (AssociatedObject?.RenderTransform is not RotateTransform rotateTransform)
                return;

            // Calculate elapsed time (like G.TIMERS.REAL in Balatro)
            var elapsedSeconds = (DateTime.Now - _startTime).TotalSeconds;

            // Balatro's ambient tilt formula:
            // local tilt_angle = G.TIMERS.REAL*(1.56 + (self.ID/1.14212)%1) + self.ID/1.35122
            var tilt_angle = elapsedSeconds * (1.56 + (_cardId / 1.14212) % 1) + _cardId / 1.35122;

            // Tilt amount based on cos wave (creates breathing effect)
            // self.tilt_var.amt = self.ambient_tilt*(0.5+math.cos(tilt_angle))*tilt_factor
            var tilt_factor = 0.3;
            var tilt_amt = AmbientTilt * (0.5 + Math.Cos(tilt_angle)) * tilt_factor;

            // Apply rotation (convert to degrees)
            // Balatro rotates in radians, we need degrees
            rotateTransform.Angle = tilt_amt * 10; // Scale for visibility
        }
    }
}

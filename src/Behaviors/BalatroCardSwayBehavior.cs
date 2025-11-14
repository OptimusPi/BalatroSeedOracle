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
        private bool _isHovering; // Track if mouse is over the card

        /// <summary>
        /// Ambient tilt strength (0.2 = Balatro default)
        /// </summary>
        public static readonly StyledProperty<double> AmbientTiltProperty =
            AvaloniaProperty.Register<BalatroCardSwayBehavior, double>(
                nameof(AmbientTilt),
                UIConstants.CardAmbientTiltMultiplier
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
            // If there's already a TransformGroup, we'll use that
            // Otherwise create a new TranslateTransform (NOT RotateTransform!)
            if (AssociatedObject.RenderTransform == null)
            {
                AssociatedObject.RenderTransformOrigin = new RelativePoint(
                    0.5,
                    0.5,
                    RelativeUnit.Relative
                );
                AssociatedObject.RenderTransform = new TranslateTransform();
            }

            // Listen for hover events to pause sway when hovering
            AssociatedObject.PointerEntered += OnPointerEntered;
            AssociatedObject.PointerExited += OnPointerExited;

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

            if (AssociatedObject != null)
            {
                AssociatedObject.PointerEntered -= OnPointerEntered;
                AssociatedObject.PointerExited -= OnPointerExited;
            }

            _animationTimer?.Stop();
            _animationTimer = null;
        }

        private void OnPointerEntered(object? sender, Avalonia.Input.PointerEventArgs e)
        {
            _isHovering = true;
        }

        private void OnPointerExited(object? sender, Avalonia.Input.PointerEventArgs e)
        {
            _isHovering = false;
        }

        private void OnAnimationTick(object? sender, EventArgs e)
        {
            if (AssociatedObject == null)
                return;

            // STOP sway when hovering - let magnetic tilt take over!
            if (_isHovering)
                return;

            // Find the RotateTransform - either standalone or in a TransformGroup
            RotateTransform? rotateTransform = null;

            if (AssociatedObject.RenderTransform is RotateTransform rotate)
            {
                rotateTransform = rotate;
            }
            else if (AssociatedObject.RenderTransform is TransformGroup group)
            {
                // Look for RotateTransform in the group
                rotateTransform = group.Children.OfType<RotateTransform>().FirstOrDefault();
            }

            if (rotateTransform == null)
                return;

            // AMBIENT MODE - Breathing motion when not hovering
            // Calculate elapsed time (like G.TIMERS.REAL in Balatro)
            var elapsedSeconds = (DateTime.Now - _startTime).TotalSeconds;

            // Balatro's ambient tilt formula from card.lua:4380-4383
            // Creates circular breathing motion using cos/sin
            var tilt_angle = elapsedSeconds * (1.56 + (_cardId / 1.14212) % 1) + _cardId / 1.35122;

            // Calculate rotation angle for subtle sway (breathing effect)
            // Use cos to create smooth back-and-forth tilt
            var swayAngle = AmbientTilt * Math.Cos(tilt_angle) * 10; // 10 degrees max sway

            // Apply rotation (creates subtle breathing sway)
            rotateTransform.Angle = swayAngle;
        }
    }
}

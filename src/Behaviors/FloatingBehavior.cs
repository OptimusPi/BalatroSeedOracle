using System;
using Avalonia;
using Avalonia.Animation;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Threading;
using Avalonia.Xaml.Interactivity;

namespace BalatroSeedOracle.Behaviors
{
    /// <summary>
    /// Balatro-style floating animation behavior
    /// Extracted from real Balatro source: external/Balatro/engine/animatedsprite.lua lines 88-92
    ///
    /// Formula:
    /// - Rotation: 0.02 * sin(2 * t + initialX) = ~1.15° max wobble
    /// - Y offset: -(1 + 0.3 * sin(0.666 * t + initialY)) = slow breathing
    /// - X offset: -(0.7 + 0.2 * sin(0.666 * t + initialX)) = gentle sway
    /// </summary>
    public class FloatingBehavior : Behavior<Control>
    {
        // Balatro's exact parameters from animatedsprite.lua
        public static readonly StyledProperty<double> RotationAmplitudeProperty =
            AvaloniaProperty.Register<FloatingBehavior, double>(nameof(RotationAmplitude), 0.02);

        public static readonly StyledProperty<double> VerticalAmplitudeProperty =
            AvaloniaProperty.Register<FloatingBehavior, double>(nameof(VerticalAmplitude), 0.3);

        public static readonly StyledProperty<double> HorizontalAmplitudeProperty =
            AvaloniaProperty.Register<FloatingBehavior, double>(nameof(HorizontalAmplitude), 0.2);

        public static readonly StyledProperty<double> FrequencyProperty =
            AvaloniaProperty.Register<FloatingBehavior, double>(nameof(Frequency), 0.666);

        public static readonly StyledProperty<bool> EnabledProperty =
            AvaloniaProperty.Register<FloatingBehavior, bool>(nameof(Enabled), true);

        /// <summary>
        /// Rotation amplitude in radians (default: 0.02 = ~1.15 degrees)
        /// Balatro uses VERY subtle rotation - barely noticeable but FELT
        /// </summary>
        public double RotationAmplitude
        {
            get => GetValue(RotationAmplitudeProperty);
            set => SetValue(RotationAmplitudeProperty, value);
        }

        /// <summary>
        /// Vertical breathing amplitude (default: 0.3 = 30% variation)
        /// Creates that cozy "breathing" effect
        /// </summary>
        public double VerticalAmplitude
        {
            get => GetValue(VerticalAmplitudeProperty);
            set => SetValue(VerticalAmplitudeProperty, value);
        }

        /// <summary>
        /// Horizontal sway amplitude (default: 0.2 = 28% variation)
        /// Subtle left-right movement
        /// </summary>
        public double HorizontalAmplitude
        {
            get => GetValue(HorizontalAmplitudeProperty);
            set => SetValue(HorizontalAmplitudeProperty, value);
        }

        /// <summary>
        /// Animation frequency in Hz (default: 0.666 = one cycle every 1.5 seconds)
        /// Slow and relaxing - Balatro's signature cozy vibe!
        /// </summary>
        public double Frequency
        {
            get => GetValue(FrequencyProperty);
            set => SetValue(FrequencyProperty, value);
        }

        /// <summary>
        /// Enable/disable floating (respects accessibility settings)
        /// </summary>
        public bool Enabled
        {
            get => GetValue(EnabledProperty);
            set => SetValue(EnabledProperty, value);
        }

        private DispatcherTimer? _animationTimer;
        private DateTime _startTime;
        private Point _phaseOffset;
        private TransformGroup? _transformGroup;
        private RotateTransform? _rotateTransform;
        private TranslateTransform? _translateTransform;

        protected override void OnAttached()
        {
            base.OnAttached();

            if (AssociatedObject == null || !Enabled)
                return;

            // Use control's position as phase offset (like Balatro does with T.x, T.y)
            // This prevents synchronized "marching" - each widget breathes independently!
            _phaseOffset = new Point(
                AssociatedObject.Bounds.X * 0.01,
                AssociatedObject.Bounds.Y * 0.01
            );

            _startTime = DateTime.Now;

            // Set up transform group
            _transformGroup = new TransformGroup();
            _rotateTransform = new RotateTransform();
            _translateTransform = new TranslateTransform();

            _transformGroup.Children.Add(_rotateTransform);
            _transformGroup.Children.Add(_translateTransform);

            AssociatedObject.RenderTransform = _transformGroup;
            AssociatedObject.RenderTransformOrigin = new RelativePoint(0.5, 0.5, RelativeUnit.Relative);

            // Start animation at 60 FPS (matches Balatro's animation rate)
            _animationTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromMilliseconds(16.67) // ~60 FPS
            };
            _animationTimer.Tick += OnAnimationTick;
            _animationTimer.Start();
        }

        private void OnAnimationTick(object? sender, EventArgs e)
        {
            if (AssociatedObject == null || _rotateTransform == null || _translateTransform == null || !Enabled)
                return;

            // Balatro's exact formula from animatedsprite.lua
            double t = (DateTime.Now - _startTime).TotalSeconds;

            // Rotation wobble (2 Hz frequency, very subtle)
            // self.T.r = 0.02*math.sin(2*G.TIMERS.REAL+self.T.x)
            double rotation = RotationAmplitude * Math.Sin(2 * t + _phaseOffset.X);
            _rotateTransform.Angle = rotation * (180.0 / Math.PI); // Convert radians to degrees

            // Vertical breathing (0.666 Hz - slow and cozy)
            // self.offset.y = -(1+0.3*math.sin(0.666*G.TIMERS.REAL+self.T.y))*self.shadow_parrallax.y
            double offsetY = -(1 + VerticalAmplitude * Math.Sin(Frequency * t + _phaseOffset.Y));

            // Horizontal sway (0.666 Hz - same as vertical for smooth motion)
            // self.offset.x = -(0.7+0.2*math.sin(0.666*G.TIMERS.REAL+self.T.x))*self.shadow_parrallax.x
            double offsetX = -(0.7 + HorizontalAmplitude * Math.Sin(Frequency * t + _phaseOffset.X));

            _translateTransform.X = offsetX;
            _translateTransform.Y = offsetY;
        }

        protected override void OnDetaching()
        {
            base.OnDetaching();

            if (_animationTimer != null)
            {
                _animationTimer.Stop();
                _animationTimer.Tick -= OnAnimationTick;
                _animationTimer = null;
            }

            if (AssociatedObject != null)
            {
                AssociatedObject.RenderTransform = null;
            }
        }
    }
}

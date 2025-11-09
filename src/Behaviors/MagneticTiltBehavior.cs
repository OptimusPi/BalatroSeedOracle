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
    /// Magnetic 3D tilt effect that follows the mouse cursor on hover.
    /// Matches Balatro's card.lua:4376-4378 hover state behavior.
    /// This behavior ONLY runs when the card is being hovered.
    /// </summary>
    public class MagneticTiltBehavior : Behavior<Control>
    {
        private DispatcherTimer? _tiltTimer;
        private Point? _lastPointerPosition;
        private bool _isEnabled;

        /// <summary>
        /// Tilt factor (0.3 = Balatro default)
        /// Maps to: self.tilt_var.amt = math.abs(self.hover_offset.y + self.hover_offset.x - 1)*tilt_factor
        /// </summary>
        public static readonly StyledProperty<double> TiltFactorProperty =
            AvaloniaProperty.Register<MagneticTiltBehavior, double>(
                nameof(TiltFactor),
                UIConstants.CardTiltFactorRadians
            );

        public double TiltFactor
        {
            get => GetValue(TiltFactorProperty);
            set => SetValue(TiltFactorProperty, value);
        }

        /// <summary>
        /// Maximum tilt angle in degrees
        /// </summary>
        public static readonly StyledProperty<double> MaxTiltAngleProperty =
            AvaloniaProperty.Register<MagneticTiltBehavior, double>(
                nameof(MaxTiltAngle),
                15.0 // Max 15 degrees tilt
            );

        public double MaxTiltAngle
        {
            get => GetValue(MaxTiltAngleProperty);
            set => SetValue(MaxTiltAngleProperty, value);
        }

        protected override void OnAttached()
        {
            base.OnAttached();

            if (AssociatedObject == null)
                return;

            // Track pointer position for tilt calculation
            AssociatedObject.PointerMoved += OnPointerMoved;
            AssociatedObject.PointerEntered += OnPointerEntered;
            AssociatedObject.PointerExited += OnPointerExited;

            // Start tilt update timer (60 FPS for smooth magnetic tracking)
            _tiltTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromMilliseconds(UIConstants.AnimationFrameRateMs)
            };
            _tiltTimer.Tick += UpdateMagneticTilt;
            // Don't start timer yet - only start when hovering
        }

        protected override void OnDetaching()
        {
            base.OnDetaching();

            if (AssociatedObject != null)
            {
                AssociatedObject.PointerMoved -= OnPointerMoved;
                AssociatedObject.PointerEntered -= OnPointerEntered;
                AssociatedObject.PointerExited -= OnPointerExited;
            }

            _tiltTimer?.Stop();
            _tiltTimer = null;
        }

        private void OnPointerEntered(object? sender, PointerEventArgs e)
        {
            _isEnabled = true;
            _lastPointerPosition = e.GetPosition(AssociatedObject);
            _tiltTimer?.Start();

            // BALATRO JUICE_UP EFFECT: Quick scale pulse on hover (card.lua:4307)
            // self:juice_up(0.05, 0.03) - adds satisfying "pop" when hovering!
            JuiceUp(0.05);
        }

        /// <summary>
        /// Balatro's juice_up effect - quick scale pulse for tactile feedback
        /// </summary>
        private void JuiceUp(double scaleAmount)
        {
            if (AssociatedObject == null)
                return;

            // Find ScaleTransform
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
                // Quick scale pulse: 1.0 → 1.05 → 1.0
                var originalScaleX = scaleTransform.ScaleX;
                var originalScaleY = scaleTransform.ScaleY;
                var targetScale = 1.0 + (scaleAmount * 0.4); // Balatro uses scale*0.4

                // Pulse up
                scaleTransform.ScaleX = targetScale;
                scaleTransform.ScaleY = targetScale;

                // Pulse back down after 50ms
                Dispatcher.UIThread.Post(() =>
                {
                    scaleTransform.ScaleX = originalScaleX;
                    scaleTransform.ScaleY = originalScaleY;
                }, DispatcherPriority.Render);
            }
        }

        private void OnPointerExited(object? sender, PointerEventArgs e)
        {
            _isEnabled = false;
            _tiltTimer?.Stop();

            // Smoothly reset tilt to 0 when mouse leaves
            ResetTilt();
        }

        private void OnPointerMoved(object? sender, PointerEventArgs e)
        {
            if (_isEnabled && AssociatedObject != null)
            {
                _lastPointerPosition = e.GetPosition(AssociatedObject);
            }
        }

        private void UpdateMagneticTilt(object? sender, EventArgs e)
        {
            if (AssociatedObject == null || _lastPointerPosition == null || !_isEnabled)
                return;

            // Check if card is still hovering by checking ResponsiveCard parent
            var parent = AssociatedObject.Parent;
            bool isHovering = false;
            while (parent != null)
            {
                if (parent is Components.ResponsiveCard card)
                {
                    isHovering = card.IsHovering;
                    break;
                }
                parent = parent.Parent;
            }

            if (!isHovering)
            {
                _isEnabled = false;
                _tiltTimer?.Stop();
                return;
            }

            // Get card bounds
            var cardWidth = AssociatedObject.Bounds.Width;
            var cardHeight = AssociatedObject.Bounds.Height;

            if (cardWidth <= 0 || cardHeight <= 0)
                return;

            // Calculate pointer position relative to card center (normalized -1 to 1)
            var cardCenterX = cardWidth / 2;
            var cardCenterY = cardHeight / 2;

            var offsetX = (_lastPointerPosition.Value.X - cardCenterX) / cardCenterX;
            var offsetY = (_lastPointerPosition.Value.Y - cardCenterY) / cardCenterY;

            // Clamp offsets to [-1, 1] range
            offsetX = Math.Clamp(offsetX, -1.0, 1.0);
            offsetY = Math.Clamp(offsetY, -1.0, 1.0);

            // BALATRO FIX: Use TRANSLATION (lean toward mouse) instead of rotation!
            // This creates the 3D perspective illusion WITHOUT rotating the collider
            // The card moves/leans toward the mouse position

            // Calculate lean distance based on mouse offset
            // Balatro uses hover_offset to calculate tilt_var which affects rendering position
            var maxLeanDistance = MaxTiltAngle; // Reuse MaxTiltAngle as max lean distance in pixels

            // Lean the card toward the mouse (translate X/Y based on mouse position)
            var leanX = offsetX * maxLeanDistance * TiltFactor;
            var leanY = offsetY * maxLeanDistance * TiltFactor;

            // Find TranslateTransform and apply the magnetic lean
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

            if (translateTransform != null)
            {
                translateTransform.X = leanX;
                translateTransform.Y = leanY;
            }
        }

        private void ResetTilt()
        {
            if (AssociatedObject == null)
                return;

            // Find TranslateTransform and reset to 0
            TranslateTransform? translateTransform = null;

            if (AssociatedObject.RenderTransform is TranslateTransform translate)
            {
                translateTransform = translate;
            }
            else if (AssociatedObject.RenderTransform is TransformGroup group)
            {
                translateTransform = group.Children.OfType<TranslateTransform>().FirstOrDefault();
            }

            if (translateTransform != null)
            {
                // Smoothly animate back to 0 (the ambient sway will take over)
                translateTransform.X = 0;
                translateTransform.Y = 0;
            }
        }
    }
}

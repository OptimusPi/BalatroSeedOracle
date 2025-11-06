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

            // Calculate hover_offset like Balatro does
            // In Balatro: hover_offset is the distance from card center (0-1 range normalized)
            // self.tilt_var.amt = math.abs(self.hover_offset.y + self.hover_offset.x - 1)*tilt_factor
            var hoverOffsetSum = Math.Abs(offsetY + offsetX);
            var tiltAmount = hoverOffsetSum * TiltFactor;

            // Calculate rotation angle toward mouse
            // The card tilts in the direction of the mouse position
            var angle = Math.Atan2(offsetY, offsetX) * (180.0 / Math.PI);

            // Apply tilt amount as a scale on the angle
            var finalAngle = angle * tiltAmount;

            // Clamp to max tilt angle
            finalAngle = Math.Clamp(finalAngle, -MaxTiltAngle, MaxTiltAngle);

            // Find RotateTransform and apply the magnetic tilt
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

            if (rotateTransform != null)
            {
                rotateTransform.Angle = finalAngle;
            }
        }

        private void ResetTilt()
        {
            if (AssociatedObject == null)
                return;

            // Find RotateTransform and reset to 0
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
                // Smoothly animate back to 0 (the ambient sway will take over)
                rotateTransform.Angle = 0;
            }
        }
    }
}

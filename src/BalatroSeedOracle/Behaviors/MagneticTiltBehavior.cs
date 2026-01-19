using System;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Media;
using Avalonia.Threading;
using Avalonia.Xaml.Interactivity;

namespace BalatroSeedOracle.Behaviors
{
    /// <summary>
    /// REAL Balatro magnetic tilt behavior - EXACTLY like the game!
    /// Based on external/Balatro/card.lua:4371-4383 hover.is state
    ///
    /// self.tilt_var.mx = G.CONTROLLER.cursor_position.x
    /// self.tilt_var.my = G.CONTROLLER.cursor_position.y
    /// self.tilt_var.amt = math.abs(hover_offset) * tilt_factor
    /// </summary>
    public class MagneticTiltBehavior : Behavior<Control>
    {
        private DispatcherTimer? _tiltTimer;
        private Point? _lastPointerPosition;
        private bool _isHovering;

        protected override void OnAttached()
        {
            base.OnAttached();

            if (AssociatedObject == null)
                return;

            // Track pointer events for magnetic tilt
            AssociatedObject.PointerEntered += OnPointerEntered;
            AssociatedObject.PointerExited += OnPointerExited;
            AssociatedObject.PointerMoved += OnPointerMoved;

            // High-frequency timer for smooth magnetic tracking (60 FPS)
            _tiltTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(16) };
            _tiltTimer.Tick += UpdateMagneticTilt;
            _tiltTimer.Start();
        }

        protected override void OnDetaching()
        {
            base.OnDetaching();

            if (AssociatedObject != null)
            {
                AssociatedObject.PointerEntered -= OnPointerEntered;
                AssociatedObject.PointerExited -= OnPointerExited;
                AssociatedObject.PointerMoved -= OnPointerMoved;
            }

            _tiltTimer?.Stop();
            _tiltTimer = null;
        }

        private void OnPointerEntered(object? sender, PointerEventArgs e)
        {
            _isHovering = true;
            _lastPointerPosition = e.GetPosition(AssociatedObject);
        }

        private void OnPointerExited(object? sender, PointerEventArgs e)
        {
            _isHovering = false;
            _lastPointerPosition = null;
        }

        private void OnPointerMoved(object? sender, PointerEventArgs e)
        {
            if (_isHovering)
            {
                _lastPointerPosition = e.GetPosition(AssociatedObject);
            }
        }

        private void UpdateMagneticTilt(object? sender, EventArgs e)
        {
            if (AssociatedObject == null)
                return;

            // Get card dimensions first
            var cardWidth = AssociatedObject.Bounds.Width;
            var cardHeight = AssociatedObject.Bounds.Height;
            if (cardWidth <= 0 || cardHeight <= 0)
                return;

            // Get or create MatrixTransform for TRUE 3D-like magnetic tilt
            MatrixTransform? matrixTransform = null;
            if (AssociatedObject.RenderTransform is MatrixTransform existing)
            {
                matrixTransform = existing;
            }
            else if (AssociatedObject.RenderTransform is TransformGroup group)
            {
                matrixTransform = group.Children.OfType<MatrixTransform>().FirstOrDefault();
                if (matrixTransform == null)
                {
                    matrixTransform = new MatrixTransform();
                    group.Children.Add(matrixTransform);
                }
            }
            else
            {
                matrixTransform = new MatrixTransform();
                AssociatedObject.RenderTransform = matrixTransform;
                AssociatedObject.RenderTransformOrigin = new RelativePoint(
                    0.5,
                    0.5,
                    RelativeUnit.Relative
                );
            }

            // If not hovering, reset to identity matrix
            if (!_isHovering || _lastPointerPosition == null)
            {
                matrixTransform.Matrix = Matrix.Identity;
                return;
            }

            // Calculate mouse position relative to card center (normalized -1 to 1)
            var cardCenter = new Point(cardWidth / 2, cardHeight / 2);
            var offsetX = (_lastPointerPosition.Value.X - cardCenter.X) / (cardWidth / 2);
            var offsetY = (_lastPointerPosition.Value.Y - cardCenter.Y) / (cardHeight / 2);

            // Clamp to reasonable values
            offsetX = Math.Clamp(offsetX, -1.0, 1.0);
            offsetY = Math.Clamp(offsetY, -1.0, 1.0);

            // Balatro magnetic tilt: Card tilts TOWARD cursor
            // Using simple skew - edges tilt in direction of mouse

            var tiltStrength = 0.12;

            // Direct skew mapping:
            // offsetY (up/down) → M12 (horizontal skew)
            // offsetX (left/right) → M21 (vertical skew)
            //
            // Negative signs make card tilt TOWARD mouse position

            var m = new Matrix(
                1.0,
                -offsetY * tiltStrength, // M11, M12
                -offsetX * tiltStrength,
                1.0, // M21, M22
                0,
                0
            );

            matrixTransform.Matrix = m;
        }
    }
}

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

        protected override void OnAttached()
        {
            base.OnAttached();

            if (AssociatedObject == null) return;

            // Track pointer movement for magnetic tilt
            AssociatedObject.PointerMoved += OnPointerMoved;

            // High-frequency timer for smooth magnetic tracking (60 FPS)
            _tiltTimer = new DispatcherTimer 
            { 
                Interval = TimeSpan.FromMilliseconds(16) 
            };
            _tiltTimer.Tick += UpdateMagneticTilt;
            _tiltTimer.Start();
        }

        protected override void OnDetaching()
        {
            base.OnDetaching();

            if (AssociatedObject != null)
            {
                AssociatedObject.PointerMoved -= OnPointerMoved;
            }

            _tiltTimer?.Stop();
            _tiltTimer = null;
        }

        private void OnPointerMoved(object? sender, PointerEventArgs e)
        {
            _lastPointerPosition = e.GetPosition(AssociatedObject);
        }

        private void UpdateMagneticTilt(object? sender, EventArgs e)
        {
            if (AssociatedObject == null || _lastPointerPosition == null) return;

            // Get card dimensions
            var cardWidth = AssociatedObject.Bounds.Width;
            var cardHeight = AssociatedObject.Bounds.Height;
            
            if (cardWidth <= 0 || cardHeight <= 0) return;

            // Calculate mouse position relative to card center (normalized -1 to 1)
            var cardCenter = new Point(cardWidth / 2, cardHeight / 2);
            var offsetX = (_lastPointerPosition.Value.X - cardCenter.X) / (cardWidth / 2);
            var offsetY = (_lastPointerPosition.Value.Y - cardCenter.Y) / (cardHeight / 2);

            // Balatro's magnetic tilt calculation
            // Based on: self.tilt_var.amt = math.abs(hover_offset) * tilt_factor
            var tiltFactor = 0.3; // Balatro's default tilt sensitivity
            var hoverOffset = Math.Abs(offsetX) + Math.Abs(offsetY);
            var tiltAmount = hoverOffset * tiltFactor;

            // Calculate rotation angle toward mouse position
            var angle = Math.Atan2(offsetY, offsetX) * (180 / Math.PI);

            // Apply magnetic tilt to RotateTransform
            if (AssociatedObject.RenderTransform is TransformGroup group)
            {
                var rotateTransform = group.Children.OfType<RotateTransform>().FirstOrDefault();
                if (rotateTransform != null)
                {
                    rotateTransform.Angle = angle * tiltAmount;
                }
            }
            else if (AssociatedObject.RenderTransform is RotateTransform rotate)
            {
                rotate.Angle = angle * tiltAmount;
            }
        }
    }
}

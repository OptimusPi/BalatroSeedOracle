using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Xaml.Interactivity;
using Avalonia.VisualTree;

namespace BalatroSeedOracle.Behaviors
{
    /// <summary>
    /// Makes any Control draggable via pointer events.
    /// MVVM-compliant: position is bound to properties, no direct UI manipulation.
    /// </summary>
    public class DraggableWidgetBehavior : Behavior<Control>
    {
        private bool _isDragging;
        private Point _dragStartPoint;
        private Point _pointerPressedPoint;
        private double _originalLeft;
        private double _originalTop;

        /// <summary>
        /// Dependency property for X position (Canvas.Left or Margin.Left binding)
        /// </summary>
        public static readonly StyledProperty<double> XProperty =
            AvaloniaProperty.Register<DraggableWidgetBehavior, double>(nameof(X));

        public double X
        {
            get => GetValue(XProperty);
            set => SetValue(XProperty, value);
        }

        /// <summary>
        /// Dependency property for Y position (Canvas.Top or Margin.Top binding)
        /// </summary>
        public static readonly StyledProperty<double> YProperty =
            AvaloniaProperty.Register<DraggableWidgetBehavior, double>(nameof(Y));

        public double Y
        {
            get => GetValue(YProperty);
            set => SetValue(YProperty, value);
        }

        protected override void OnAttached()
        {
            base.OnAttached();

            if (AssociatedObject == null) return;

            // Listen for property changes to update Margin when X or Y changes
            XProperty.Changed.AddClassHandler<DraggableWidgetBehavior>((sender, args) =>
            {
                if (sender == this)
                    UpdatePosition();
            });
            YProperty.Changed.AddClassHandler<DraggableWidgetBehavior>((sender, args) =>
            {
                if (sender == this)
                    UpdatePosition();
            });

            // Apply initial position from ViewModel binding
            UpdatePosition();

            // Wire up pointer events
            AssociatedObject.PointerPressed += OnPointerPressed;
            AssociatedObject.PointerMoved += OnPointerMoved;
            AssociatedObject.PointerReleased += OnPointerReleased;
            AssociatedObject.PointerCaptureLost += OnPointerCaptureLost;
        }

        private void UpdatePosition()
        {
            if (AssociatedObject != null)
            {
                AssociatedObject.Margin = new Thickness(X, Y, 0, 0);
            }
        }

        protected override void OnDetaching()
        {
            base.OnDetaching();

            if (AssociatedObject == null) return;

            // Clean up events
            AssociatedObject.PointerPressed -= OnPointerPressed;
            AssociatedObject.PointerMoved -= OnPointerMoved;
            AssociatedObject.PointerReleased -= OnPointerReleased;
            AssociatedObject.PointerCaptureLost -= OnPointerCaptureLost;
        }

        private void OnPointerPressed(object? sender, PointerPressedEventArgs e)
        {
            if (AssociatedObject == null) return;

            var props = e.GetCurrentPoint(AssociatedObject).Properties;
            if (!props.IsLeftButtonPressed) return;

            // Check if click is on a draggable area (not buttons, textboxes, etc.)
            var clickedElement = e.Source as Control;
            while (clickedElement != null)
            {
                // Don't drag if clicking interactive controls
                if (clickedElement is Button || clickedElement is TextBox || clickedElement is Slider)
                    return;

                clickedElement = clickedElement.Parent as Control;
            }

            // Reset drag state on new press
            _isDragging = false;

            // Store press position - DON'T start dragging yet (wait for movement)
            var parent = AssociatedObject.Parent as Visual;
            if (parent != null)
            {
                _pointerPressedPoint = e.GetPosition(parent);
            }
            else
            {
                _pointerPressedPoint = e.GetPosition(null);
            }

            _originalLeft = X;
            _originalTop = Y;

            // DON'T set e.Handled = true yet - let the widget's click handler run first
        }

        private void OnPointerMoved(object? sender, PointerEventArgs e)
        {
            if (AssociatedObject == null) return;

            // CRITICAL: Only process if left button is ACTUALLY PRESSED (not just hovering!)
            var props = e.GetCurrentPoint(AssociatedObject).Properties;
            if (!props.IsLeftButtonPressed)
            {
                _isDragging = false;
                return;
            }

            // Get current position
            var parent = AssociatedObject.Parent as Visual;
            Point currentPoint = parent != null ? e.GetPosition(parent) : e.GetPosition(null);

            // If not dragging yet, check if we've moved far enough to start
            if (!_isDragging)
            {
                var distance = Math.Abs(currentPoint.X - _pointerPressedPoint.X) + Math.Abs(currentPoint.Y - _pointerPressedPoint.Y);

                // Only start dragging if moved more than 20 pixels
                if (distance > 20)
                {
                    _isDragging = true;
                    _dragStartPoint = _pointerPressedPoint;
                    e.Pointer.Capture(AssociatedObject);
                }
                else
                {
                    return; // Not enough movement yet - let click handlers work
                }
            }

            // Calculate delta from the drag start position
            var deltaX = currentPoint.X - _dragStartPoint.X;
            var deltaY = currentPoint.Y - _dragStartPoint.Y;

            // Apply delta to original position for proper dragging
            X = _originalLeft + deltaX;
            Y = _originalTop + deltaY;

            e.Handled = true;
        }

        private void OnPointerReleased(object? sender, PointerReleasedEventArgs e)
        {
            if (!_isDragging) return;

            _isDragging = false;
            e.Pointer.Capture(null);
            e.Handled = true;
        }

        private void OnPointerCaptureLost(object? sender, PointerCaptureLostEventArgs e)
        {
            // Safety: ensure drag stops if pointer capture is lost
            _isDragging = false;
        }
    }
}

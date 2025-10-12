using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Xaml.Interactivity;

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

            // Get parent for consistent coordinate system
            var parent = AssociatedObject.Parent as Visual;
            if (parent == null) return;

            _isDragging = true;
            _dragStartPoint = e.GetPosition(parent); // Parent coordinates instead of screen
            _originalLeft = X;
            _originalTop = Y;

            e.Pointer.Capture(AssociatedObject);
            e.Handled = true;
        }

        private void OnPointerMoved(object? sender, PointerEventArgs e)
        {
            if (!_isDragging || AssociatedObject == null) return;

            // Get the parent visual to calculate relative position
            var parent = AssociatedObject.Parent as Visual;
            if (parent == null) return;

            var currentPoint = e.GetPosition(parent);

            // Direct positioning - no delta calculation needed
            // This fixes the 0.5x speed issue caused by coordinate system mismatch
            X = currentPoint.X - (_dragStartPoint.X - _originalLeft);
            Y = currentPoint.Y - (_dragStartPoint.Y - _originalTop);

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

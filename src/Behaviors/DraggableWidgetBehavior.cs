using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.VisualTree;
using Avalonia.Xaml.Interactivity;
using BalatroSeedOracle.Helpers;
using BalatroSeedOracle.Services;

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

        // Tracks whether the initial press was on the configured drag handle
        private bool _pressOriginIsOnHandle;

        // Helper to clamp position within parent bounds so widgets cannot be dragged off-screen
        private (double X, double Y) ClampPosition(double x, double y)
        {
            if (AssociatedObject == null)
                return (x, y);

            var parentVisual = AssociatedObject.Parent as Visual;
            if (parentVisual == null)
                return (x, y);

            var parentBounds = parentVisual.Bounds;
            var selfBounds = AssociatedObject.Bounds;

            // If bounds are not available yet, avoid clamping to prevent jumpy behavior
            if (
                parentBounds.Width <= 0
                || parentBounds.Height <= 0
                || selfBounds.Width <= 0
                || selfBounds.Height <= 0
            )
                return (Math.Max(0, x), Math.Max(0, y));

            var maxX = Math.Max(0, parentBounds.Width - selfBounds.Width);
            var maxY = Math.Max(0, parentBounds.Height - selfBounds.Height);

            var clampedX = Math.Clamp(x, 0, maxX);
            var clampedY = Math.Clamp(y, 0, maxY);

            return (clampedX, clampedY);
        }

        /// <summary>
        /// Dependency property for X position (Canvas.Left or Margin.Left binding)
        /// </summary>
        public static readonly StyledProperty<double> XProperty = AvaloniaProperty.Register<
            DraggableWidgetBehavior,
            double
        >(nameof(X));

        public double X
        {
            get => GetValue(XProperty);
            set => SetValue(XProperty, value);
        }

        /// <summary>
        /// Dependency property for Y position (Canvas.Top or Margin.Top binding)
        /// </summary>
        public static readonly StyledProperty<double> YProperty = AvaloniaProperty.Register<
            DraggableWidgetBehavior,
            double
        >(nameof(Y));

        public double Y
        {
            get => GetValue(YProperty);
            set => SetValue(YProperty, value);
        }

        /// <summary>
        /// CSS class name that indicates draggable area (e.g., "widget-header")
        /// If set, ONLY elements with this class can be dragged
        /// If empty/null, allows drag from anywhere (entire control)
        /// </summary>
        public static readonly StyledProperty<string?> DragHandleClassProperty =
            AvaloniaProperty.Register<DraggableWidgetBehavior, string?>(
                nameof(DragHandleClass),
                null
            );

        public string? DragHandleClass
        {
            get => GetValue(DragHandleClassProperty);
            set => SetValue(DragHandleClassProperty, value);
        }

        protected override void OnAttached()
        {
            base.OnAttached();

            if (AssociatedObject == null)
                return;

            // Listen for property changes to update Margin when X or Y changes
            XProperty.Changed.AddClassHandler<DraggableWidgetBehavior>(
                (sender, args) =>
                {
                    if (sender == this)
                        UpdatePosition();
                }
            );
            YProperty.Changed.AddClassHandler<DraggableWidgetBehavior>(
                (sender, args) =>
                {
                    if (sender == this)
                        UpdatePosition();
                }
            );

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

            if (AssociatedObject == null)
                return;

            // Clean up events
            AssociatedObject.PointerPressed -= OnPointerPressed;
            AssociatedObject.PointerMoved -= OnPointerMoved;
            AssociatedObject.PointerReleased -= OnPointerReleased;
            AssociatedObject.PointerCaptureLost -= OnPointerCaptureLost;
        }

        private void OnPointerPressed(object? sender, PointerPressedEventArgs e)
        {
            if (AssociatedObject == null)
                return;

            var props = e.GetCurrentPoint(AssociatedObject).Properties;
            if (!props.IsLeftButtonPressed)
                return;

            // CRITICAL: Always reset ALL drag state on new press to prevent stale values
            _isDragging = false;
            _pointerPressedPoint = new Point(double.NaN, double.NaN);
            _dragStartPoint = new Point(double.NaN, double.NaN);
            _pressOriginIsOnHandle = false;

            Console.WriteLine($"[DragBehavior] PointerPressed at widget position ({X}, {Y})");

            // DON'T call BringToFront here - it changes ZIndex which causes pointer capture loss!
            // We'll bring to front when drag completes instead

            // PROPER MVVM: Check if clicking the configured drag handle class
            // Default is "widget-header" but can be customized via XAML property
            if (!string.IsNullOrEmpty(DragHandleClass))
            {
                var clickedElement = e.Source as Control;
                bool isOnDragHandle = false;

                while (clickedElement != null)
                {
                    if (clickedElement.Classes.Contains(DragHandleClass))
                    {
                        isOnDragHandle = true;
                        break;
                    }
                    clickedElement = clickedElement.Parent as Control;
                }

                // If not clicking the drag handle, clear state and exit (prevents ZOOP!)
                if (!isOnDragHandle)
                {
                    Console.WriteLine($"[DragBehavior] Not on drag handle, ignoring");
                    return;
                }
                else
                {
                    _pressOriginIsOnHandle = true;
                    Console.WriteLine($"[DragBehavior] On drag handle, ready to drag");
                }
            }

            // Store press position - DON'T start dragging yet (wait for movement)
            var parent = AssociatedObject?.Parent as Visual;
            if (parent != null)
            {
                _pointerPressedPoint = e.GetPosition(parent);
                Console.WriteLine($"[DragBehavior] Press point: ({_pointerPressedPoint.X}, {_pointerPressedPoint.Y})");
            }
            else
            {
                _pointerPressedPoint = e.GetPosition(null);
            }

            // DON'T set e.Handled = true yet - let the widget's click handler run first
        }

        private void OnPointerMoved(object? sender, PointerEventArgs e)
        {
            if (AssociatedObject == null)
                return;

            // CRITICAL: Only process if left button is ACTUALLY PRESSED (not just hovering!)
            var props = e.GetCurrentPoint(AssociatedObject).Properties;
            if (!props.IsLeftButtonPressed)
            {
                _isDragging = false;
                return;
            }

            // If pointer press point is invalid (NaN), exit immediately
            // This happens when OnPointerPressed detected a click on non-draggable area
            if (double.IsNaN(_pointerPressedPoint.X) || double.IsNaN(_pointerPressedPoint.Y))
            {
                _isDragging = false;
                return;
            }

            // Additional safety gate: if a drag handle class is set, only allow drag
            // when the initial press originated on the drag handle. This prevents
            // starting a drag from slider/content areas that may swallow PointerPressed.
            if (!string.IsNullOrEmpty(DragHandleClass) && !_pressOriginIsOnHandle)
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
                var distance =
                    Math.Abs(currentPoint.X - _pointerPressedPoint.X)
                    + Math.Abs(currentPoint.Y - _pointerPressedPoint.Y);

                // Only start dragging if moved more than 20 pixels
                if (distance > 20)
                {
                    _isDragging = true;
                    e.Pointer.Capture(AssociatedObject);
                    
                    Console.WriteLine($"[DragBehavior] Starting drag from ({X}, {Y})");

                    // DON'T call BringToFront here - it causes pointer capture loss!
                    // We'll bring to front when drag completes instead

                    // Calculate the initial offset from the widget's top-left corner to the mouse position
                    // This allows us to maintain the relative position during drag
                    _dragStartPoint = new Point(
                        _pointerPressedPoint.X - X,
                        _pointerPressedPoint.Y - Y
                    );
                }
                else
                {
                    return; // Not enough movement yet - let click handlers work
                }
            }

            // Use the initial click offset to maintain relative position during drag
            var newX = currentPoint.X - _dragStartPoint.X;
            var newY = currentPoint.Y - _dragStartPoint.Y;
            var clamped = ClampPosition(newX, newY);
            X = clamped.X;
            Y = clamped.Y;

            e.Handled = true;
        }

        private void OnPointerReleased(object? sender, PointerReleasedEventArgs e)
        {
            if (_isDragging)
            {
                _isDragging = false;
                e.Pointer.Capture(null);

                // Use collision-aware grid snapping for ALL widgets (minimized and expanded)
                if (AssociatedObject?.DataContext is ViewModels.BaseWidgetViewModel vm)
                {
                    // NOW bring widget to front after drag completes (won't cause capture loss)
                    vm.BringToFront();

                    try
                    {
                        var positionService = ServiceHelper.GetService<WidgetPositionService>();
                        if (positionService != null)
                        {
                            // Calculate widget's top-left position (consistent reference point)
                            // This ensures grid snapping is based on widget position, not mouse position
                            var widgetX = X;
                            var widgetY = Y;

                            // Get parent bounds for dynamic exclusion zones
                            var parentWidth = 1200.0; // Default fallback
                            var parentHeight = 700.0; // Default fallback

                            var parentVisual = AssociatedObject.Parent as Visual;
                            if (parentVisual != null)
                            {
                                parentWidth = parentVisual.Bounds.Width;
                                parentHeight = parentVisual.Bounds.Height;
                            }

                            // Different positioning behavior for minimized vs expanded widgets
                            if (vm.IsMinimized)
                            {
                                // Minimized widgets: Use grid snapping with collision avoidance
                                var (newX, newY) = positionService.SnapToGridWithCollisionAvoidance(
                                    widgetX,
                                    widgetY,
                                    vm,
                                    parentWidth,
                                    parentHeight
                                );
                                var clamped = ClampPosition(newX, newY);
                                X = clamped.X;
                                Y = clamped.Y;
                            }
                            else
                            {
                                // Expanded widgets: Use free positioning (just clamp to screen bounds)
                                var clamped = ClampPosition(widgetX, widgetY);
                                X = clamped.X;
                                Y = clamped.Y;
                            }
                        }
                        else
                        {
                            // Fallback to simple grid snapping if service not available
                            // Note: Using new 90px grid spacing instead of old 20px
                            const double gridSpacing = 90.0;
                            const double gridOriginX = 20.0;
                            const double gridOriginY = 20.0;

                            // Snap to grid using consistent reference point
                            var offsetX = X - gridOriginX;
                            var offsetY = Y - gridOriginY;
                            var snappedOffsetX = Math.Round(offsetX / gridSpacing) * gridSpacing;
                            var snappedOffsetY = Math.Round(offsetY / gridSpacing) * gridSpacing;
                            var snappedX = snappedOffsetX + gridOriginX;
                            var snappedY = snappedOffsetY + gridOriginY;

                            var clamped = ClampPosition(snappedX, snappedY);
                            X = clamped.X;
                            Y = clamped.Y;
                        }
                    }
                    catch
                    {
                        // Fallback to simple grid snapping if service access fails
                        const double gridSpacing = 90.0;
                        const double gridOriginX = 20.0;
                        const double gridOriginY = 20.0;

                        // Snap to grid using consistent reference point
                        var offsetX = X - gridOriginX;
                        var offsetY = Y - gridOriginY;
                        var snappedOffsetX = Math.Round(offsetX / gridSpacing) * gridSpacing;
                        var snappedOffsetY = Math.Round(offsetY / gridSpacing) * gridSpacing;
                        var snappedX = snappedOffsetX + gridOriginX;
                        var snappedY = snappedOffsetY + gridOriginY;

                        var clamped = ClampPosition(snappedX, snappedY);
                        X = clamped.X;
                        Y = clamped.Y;
                    }
                }

                e.Handled = true;
            }

            // Always clear stored press state to prevent stale data from causing jumps
            _pointerPressedPoint = new Point(double.NaN, double.NaN);
            _dragStartPoint = new Point(double.NaN, double.NaN);
            _pressOriginIsOnHandle = false;
        }

        private void OnPointerCaptureLost(object? sender, PointerCaptureLostEventArgs e)
        {
            // Safety: ensure drag stops if pointer capture is lost
            Console.WriteLine($"[DragBehavior] POINTER CAPTURE LOST at ({X}, {Y})");
            _isDragging = false;
            
            // CRITICAL: Clean up all drag state to prevent stuck behavior
            _pointerPressedPoint = new Point(double.NaN, double.NaN);
            _dragStartPoint = new Point(double.NaN, double.NaN);
            _pressOriginIsOnHandle = false;
        }
    }
}

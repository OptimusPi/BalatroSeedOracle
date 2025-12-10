using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Presenters;
using Avalonia.Input;
using Avalonia.Markup.Xaml;
using BalatroSeedOracle.ViewModels;

namespace BalatroSeedOracle.Components
{
    /// <summary>
    /// BaseWidget - Shared draggable widget component with minimize/maximize/close buttons
    /// ALL widgets should inherit from or use this component for consistency!
    /// Usage: <BaseWidget><StackPanel>...content...</StackPanel></BaseWidget>
    /// </summary>
    public partial class BaseWidget : ContentControl
    {
        // Drag state
        private bool _isDragging = false;
        private Point _dragStartScreenPoint;
        private Thickness _originalMargin;

        public BaseWidget()
        {
            InitializeComponent();

            // CRITICAL: Inherit DataContext from parent control when loaded
            this.AttachedToVisualTree += (s, e) =>
            {
                // Find parent UserControl and inherit its DataContext
                if (this.Parent is Control parent && parent.DataContext is BaseWidgetViewModel)
                {
                    this.DataContext = parent.DataContext;
                }

                // Wire up ContentPresenter to display our Content property
                var contentPresenter = this.FindControl<ContentPresenter>("PART_ContentPresenter");
                if (contentPresenter != null && this.Content != null)
                {
                    contentPresenter.Content = this.Content;
                    // Clear the ContentControl's content to avoid duplicate rendering
                    // (ContentPresenter will handle the display)
                }
            };
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }

        #region Drag Functionality (Header Only!)

        public void OnHeaderPointerPressed(object? sender, PointerPressedEventArgs e)
        {
            // Only start dragging on left button
            var props = e.GetCurrentPoint(this).Properties;
            if (!props.IsLeftButtonPressed)
                return;

            // Get the clicked element to ensure we're clicking the header, not a button
            var clickedElement = e.Source as Control;

            // Walk up to see if we clicked on a button (window controls)
            while (clickedElement != null)
            {
                // Don't drag if clicking on window control buttons
                if (
                    clickedElement is Button
                    && clickedElement.Classes.Contains("widget-minimize-btn")
                )
                {
                    return;
                }

                clickedElement = clickedElement.Parent as Control;
            }

            _isDragging = true;

            // Store screen coordinates (null = screen space)
            _dragStartScreenPoint = e.GetPosition(null);

            // Store original margin before drag starts
            var expandedView = this.FindControl<Border>("ExpandedView");
            _originalMargin = expandedView?.Margin ?? new Thickness(0);

            e.Pointer.Capture(this.FindControl<Border>("ExpandedView"));
            e.Handled = true;
        }

        public void OnHeaderPointerMoved(object? sender, PointerEventArgs e)
        {
            if (!_isDragging)
                return;

            // Get current screen position
            var currentScreenPoint = e.GetPosition(null);

            // Calculate delta from original position
            var delta = currentScreenPoint - _dragStartScreenPoint;

            // Apply delta to original margin
            var newMargin = new Thickness(
                _originalMargin.Left + delta.X,
                _originalMargin.Top + delta.Y,
                0,
                0
            );

            // Update actual margin for visual feedback with simple clamping to parent bounds
            var expandedView = this.FindControl<Border>("ExpandedView");

            if (expandedView != null)
            {
                var parentVisual = expandedView.Parent as Visual;
                if (parentVisual != null)
                {
                    var parentBounds = parentVisual.Bounds;
                    var selfBounds = expandedView.Bounds;

                    // Fallbacks if bounds aren't available yet
                    var maxX =
                        parentBounds.Width > 0 && selfBounds.Width > 0
                            ? Math.Max(0, parentBounds.Width - selfBounds.Width)
                            : double.MaxValue;
                    var maxY =
                        parentBounds.Height > 0 && selfBounds.Height > 0
                            ? Math.Max(0, parentBounds.Height - selfBounds.Height)
                            : double.MaxValue;

                    var clampedLeft = Math.Clamp(newMargin.Left, 0, maxX);
                    var clampedTop = Math.Clamp(newMargin.Top, 0, maxY);
                    newMargin = new Thickness(clampedLeft, clampedTop, 0, 0);
                }

                expandedView.Margin = newMargin;
            }

            // Also update ViewModel position if it exists
            if (DataContext is BaseWidgetViewModel vm)
            {
                vm.PositionX = newMargin.Left;
                vm.PositionY = newMargin.Top;
            }

            e.Handled = true;
        }

        public void OnHeaderPointerReleased(object? sender, PointerReleasedEventArgs e)
        {
            if (_isDragging)
            {
                _isDragging = false;
                e.Pointer.Capture(null);

                // SNAP TO GRID when drag ends!
                const double gridSize = 20.0; // Match the starting X position
                var expandedView = this.FindControl<Border>("ExpandedView");
                if (expandedView != null)
                {
                    var currentMargin = expandedView.Margin;

                    // Snap to nearest grid position
                    var snappedX = Math.Round(currentMargin.Left / gridSize) * gridSize;
                    var snappedY = Math.Round(currentMargin.Top / gridSize) * gridSize;

                    var snappedMargin = new Thickness(snappedX, snappedY, 0, 0);
                    expandedView.Margin = snappedMargin;

                    // Update ViewModel with snapped position
                    if (DataContext is BaseWidgetViewModel vm)
                    {
                        vm.PositionX = snappedX;
                        vm.PositionY = snappedY;
                    }
                }

                e.Handled = true;
            }
        }

        public void OnHeaderPointerCaptureLost(object? sender, PointerCaptureLostEventArgs e)
        {
            // Safety: ensure drag stops if pointer capture is lost for any reason
            _isDragging = false;
        }

        #endregion
    }
}

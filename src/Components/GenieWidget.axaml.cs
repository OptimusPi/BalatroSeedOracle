using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Markup.Xaml;
using BalatroSeedOracle.Services;
using BalatroSeedOracle.ViewModels;

namespace BalatroSeedOracle.Components
{
    /// <summary>
    /// GenieWidget - AI-powered filter generation widget
    /// MOVABLE, MINIMIZABLE, MAGICAL! 🧞✨
    /// </summary>
    public partial class GenieWidget : UserControl
    {
        public GenieWidgetViewModel? ViewModel { get; }

        // Drag state
        private bool _isDragging = false;

        public GenieWidget()
        {
            // Check feature flag - hide widget if disabled
            if (!FeatureFlagsService.Instance.IsEnabled(FeatureFlagsService.GENIE_ENABLED))
            {
                IsVisible = false;
                return;
            }

            ViewModel = new GenieWidgetViewModel();
            DataContext = ViewModel;

            InitializeComponent();

            this.DetachedFromVisualTree += OnDetachedFromVisualTree;
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }

        #region Drag Functionality

        private Point _dragStartScreenPoint;
        private Thickness _originalMargin;

        public void OnWidgetPointerPressed(object? sender, PointerPressedEventArgs e)
        {
            var props = e.GetCurrentPoint(this).Properties;
            if (!props.IsLeftButtonPressed)
                return;

            var clickedElement = e.Source as Control;
            var isHeader = false;

            while (clickedElement != null)
            {
                if (clickedElement.Name == "MinimizedView" || clickedElement.Classes.Contains("genie-header"))
                {
                    isHeader = true;
                    break;
                }

                // Don't drag if clicking on interactive controls
                if (clickedElement is Button || clickedElement is TextBox || clickedElement is Expander)
                {
                    return;
                }

                clickedElement = clickedElement.Parent as Control;
            }

            if (isHeader)
            {
                _isDragging = true;

                // Store screen coordinates (null = screen space)
                _dragStartScreenPoint = e.GetPosition(null);

                // Store original margin before drag starts
                var minimizedView = this.FindControl<Grid>("MinimizedView");
                var expandedView = this.FindControl<Border>("ExpandedView");
                _originalMargin = (minimizedView?.Margin ?? expandedView?.Margin) ?? new Thickness(0);

                e.Pointer.Capture(this);
                e.Handled = true;
            }
        }

        public void OnWidgetPointerMoved(object? sender, PointerEventArgs e)
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

            // Update ViewModel position
            if (ViewModel != null)
            {
                ViewModel.PositionX = newMargin.Left;
                ViewModel.PositionY = newMargin.Top;
            }

            // Update visual margin
            var minimizedView = this.FindControl<Grid>("MinimizedView");
            var expandedView = this.FindControl<Border>("ExpandedView");

            if (minimizedView != null)
                minimizedView.Margin = newMargin;
            if (expandedView != null)
                expandedView.Margin = newMargin;

            e.Handled = true;
        }

        public void OnWidgetPointerReleased(object? sender, PointerReleasedEventArgs e)
        {
            if (_isDragging)
            {
                _isDragging = false;
                e.Pointer.Capture(null);
                e.Handled = true;
            }
        }

        #endregion

        private void OnDetachedFromVisualTree(object? sender, EventArgs e)
        {
            // Cleanup if needed
        }
    }
}

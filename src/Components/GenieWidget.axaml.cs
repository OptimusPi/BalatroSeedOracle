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
        private Point _dragStartPoint;

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
                _dragStartPoint = e.GetPosition(this);
                e.Pointer.Capture(this);
                e.Handled = true;
            }
        }

        public void OnWidgetPointerMoved(object? sender, PointerEventArgs e)
        {
            if (!_isDragging)
                return;

            var parent = this.Parent as Control;
            if (parent == null) return;

            var currentPoint = e.GetPosition(parent);
            var delta = currentPoint - _dragStartPoint;

            ViewModel.PositionX = delta.X;
            ViewModel.PositionY = delta.Y;

            var minimizedView = this.FindControl<Grid>("MinimizedView");
            var expandedView = this.FindControl<Border>("ExpandedView");

            var newMargin = new Thickness(delta.X, delta.Y, 0, 0);
            if (minimizedView != null)
                minimizedView.Margin = newMargin;
            if (expandedView != null)
                expandedView.Margin = newMargin;

            e.Handled = true;
        }

        public void OnWidgetPointerReleased(object? sender, PointerReleasedEventArgs e)
        {
            if (!_isDragging)
                return;

            _isDragging = false;
            e.Pointer.Capture(null);
            e.Handled = true;
        }

        #endregion

        private void OnDetachedFromVisualTree(object? sender, EventArgs e)
        {
            // Cleanup if needed
        }
    }
}

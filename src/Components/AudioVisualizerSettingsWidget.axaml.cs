using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Markup.Xaml;
using BalatroSeedOracle.Helpers;
using BalatroSeedOracle.ViewModels;

namespace BalatroSeedOracle.Components
{
    /// <summary>
    /// AudioVisualizerSettingsWidget - A movable, minimizable widget for audio visualizer settings
    /// Following MVVM pattern - all logic is in AudioVisualizerSettingsWidgetViewModel
    /// </summary>
    public partial class AudioVisualizerSettingsWidget : UserControl
    {
        public AudioVisualizerSettingsWidgetViewModel ViewModel { get; }

        // Drag state
        private bool _isDragging = false;

        public AudioVisualizerSettingsWidget()
        {
            InitializeComponent();

            // Initialize ViewModel (creates it lazily - only when widget is actually used)
            ViewModel = new AudioVisualizerSettingsWidgetViewModel();
            DataContext = ViewModel;

            // Initialize ViewModel after XAML is loaded
            ViewModel.Initialize();

            // Wire up cleanup
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
            // Only start dragging on left button
            var props = e.GetCurrentPoint(this).Properties;
            if (!props.IsLeftButtonPressed)
                return;

            // Get the header border to check if we're clicking on it
            var clickedElement = e.Source as Control;
            var isHeader = false;

            // Walk up to see if we clicked on the header
            while (clickedElement != null)
            {
                if (clickedElement.Name == "MinimizedView" || clickedElement.Classes.Contains("widget-header"))
                {
                    isHeader = true;
                    break;
                }

                // Don't drag if clicking on interactive controls
                if (clickedElement is Button || clickedElement is Slider || clickedElement is ComboBox || clickedElement is CheckBox)
                {
                    return;
                }

                clickedElement = clickedElement.Parent as Control;
            }

            // Only start drag if clicking header/minimized view
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

            // Update position through ViewModel
            ViewModel.PositionX = newMargin.Left;
            ViewModel.PositionY = newMargin.Top;

            // Update actual margin for visual feedback
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

        #region Lifecycle

        private void OnDetachedFromVisualTree(object? sender, EventArgs e)
        {
            ViewModel.Dispose();
        }

        #endregion
    }
}

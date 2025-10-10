using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Markup.Xaml;
using Avalonia.VisualTree;
using BalatroSeedOracle.Helpers;
using BalatroSeedOracle.Services;
using BalatroSeedOracle.ViewModels;
using BalatroSeedOracle.Views;
using BalatroSeedOracle.Views.Modals;

namespace BalatroSeedOracle.Components
{
    /// <summary>
    /// Code-behind for DayLatroWidget
    /// Minimal code following MVVM pattern - all logic is in DayLatroWidgetViewModel
    /// </summary>
    public partial class DayLatroWidget : UserControl
    {
        public DayLatroWidgetViewModel? ViewModel { get; }

        // Drag state
        private bool _isDragging = false;
        private Point _dragStartPoint;

        public DayLatroWidget()
        {
            // Check feature flag - hide widget if disabled
            if (!FeatureFlagsService.Instance.IsEnabled(FeatureFlagsService.DAYLATRO_ENABLED))
            {
                IsVisible = false;
                return;
            }

            // Initialize ViewModel with dependency injection
            ViewModel = new DayLatroWidgetViewModel(
                DaylatroHighScoreService.Instance,
                App.GetService<UserProfileService>() ?? new UserProfileService()
            );

            // Set DataContext for bindings
            DataContext = ViewModel;

            // Wire up ViewModel events that require view interaction
            ViewModel.AnalyzeSeedRequested += OnAnalyzeSeedRequested;

            InitializeComponent();

            // Initialize ViewModel after XAML is loaded
            ViewModel.Initialize();

            // Wire up cleanup
            this.DetachedFromVisualTree += OnDetachedFromVisualTree;
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }

        /// <summary>
        /// Handle analyze seed request from ViewModel
        /// This is view-specific logic that navigates to a modal
        /// </summary>
        private void OnAnalyzeSeedRequested(object? sender, string seed)
        {
            try
            {
                // Walk up visual tree to find main menu to show modal
                var parent = this.Parent;
                BalatroMainMenu? mainMenu = null;
                while (parent != null && mainMenu == null)
                {
                    if (parent is BalatroMainMenu mm)
                        mainMenu = mm;
                    parent = (parent as Control)?.Parent;
                }

                if (mainMenu == null)
                {
                    DebugLogger.LogError("DayLatroWidget", "Could not find BalatroMainMenu in visual tree");
                    return;
                }

                var analyzeModal = new AnalyzeModal();
                analyzeModal.SetSeedAndAnalyze(seed);

                var stdModal = new StandardModal("ANALYZE");
                stdModal.SetContent(analyzeModal);
                stdModal.BackClicked += (s, _) => mainMenu.HideModalContent();
                mainMenu.ShowModalContent(stdModal, "SEED ANALYZER");
            }
            catch (Exception ex)
            {
                DebugLogger.LogError("DayLatroWidget", $"Error opening analyzer: {ex.Message}");
            }
        }

        /// <summary>
        /// Cleanup when control is unloaded
        /// </summary>
        private void OnDetachedFromVisualTree(object? sender, EventArgs e)
        {
            if (ViewModel != null)
            {
                ViewModel.AnalyzeSeedRequested -= OnAnalyzeSeedRequested;
                ViewModel.Dispose();
            }
        }

        #region Drag Functionality

        public void OnWidgetPointerPressed(object? sender, PointerPressedEventArgs e)
        {
            var props = e.GetCurrentPoint(this).Properties;
            if (!props.IsLeftButtonPressed)
                return;

            var clickedElement = e.Source as Control;
            var isHeader = false;

            // Walk up visual tree to see if we clicked on the header or minimized view
            while (clickedElement != null)
            {
                if (clickedElement.Name == "MinimizedView" || clickedElement.Classes.Contains("widget-header"))
                {
                    isHeader = true;
                    break;
                }

                // Don't drag if clicking on interactive controls (buttons, textboxes, etc.)
                if (clickedElement is Button button && button.Command != null)
                {
                    return;
                }
                if (clickedElement is TextBox || clickedElement is ScrollViewer)
                {
                    return;
                }

                clickedElement = clickedElement.Parent as Control;
            }

            if (isHeader)
            {
                _isDragging = true;
                _dragStartPoint = e.GetPosition(this.Parent as Visual);
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

            if (ViewModel != null)
            {
                ViewModel.PositionX = delta.X;
                ViewModel.PositionY = delta.Y;
            }

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
    }
}

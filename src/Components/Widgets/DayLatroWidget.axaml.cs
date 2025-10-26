using System;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using BalatroSeedOracle.Helpers;
using BalatroSeedOracle.Services;
using BalatroSeedOracle.ViewModels;
using BalatroSeedOracle.Views;
using BalatroSeedOracle.Views.Modals;

namespace BalatroSeedOracle.Components
{
    /// <summary>
    /// Code-behind for DayLatroWidget
    /// MVVM pattern - ALL business logic in ViewModel, drag handled by DraggableWidgetBehavior
    /// </summary>
    public partial class DayLatroWidget : UserControl
    {
        public DayLatroWidgetViewModel? ViewModel { get; }

        // Track click vs drag for minimized icon
        private Avalonia.Point _iconPressedPosition;

        public DayLatroWidget()
        {
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

            // Update ZIndex when IsMinimized changes - now handled by XAML binding to WidgetZIndex
            // ViewModel.PropertyChanged += (s, e) => { ... }; // REMOVED - use XAML binding instead

            // Set initial ZIndex - now handled by XAML binding to WidgetZIndex
            // this.ZIndex = ViewModel.IsMinimized ? 1 : 100; // REMOVED - use XAML binding instead

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

        /// <summary>
        /// Track pointer pressed position to detect drag vs click
        /// </summary>
        private void OnMinimizedIconPressed(object? sender, Avalonia.Input.PointerPressedEventArgs e)
        {
            _iconPressedPosition = e.GetPosition((Control)sender!);
        }

        /// <summary>
        /// On release: if no drag happened, expand the widget
        /// </summary>
        private void OnMinimizedIconReleased(object? sender, Avalonia.Input.PointerReleasedEventArgs e)
        {
            var releasePosition = e.GetPosition((Control)sender!);
            var distance = Math.Abs(releasePosition.X - _iconPressedPosition.X) + Math.Abs(releasePosition.Y - _iconPressedPosition.Y);

            // If pointer moved less than 20 pixels, treat as click (not drag)
            if (distance < 20 && ViewModel != null)
            {
                ViewModel.ExpandCommand.Execute(null);
            }
        }
    }
}

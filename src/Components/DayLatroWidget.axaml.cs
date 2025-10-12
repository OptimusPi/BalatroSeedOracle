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
    }
}

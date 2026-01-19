using System;
using System.Threading.Tasks;
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
    /// MVVM pattern - ALL business logic in ViewModel, drag handled by DraggableWidgetBehavior
    /// </summary>
    public partial class DayLatroWidget : BaseWidgetControl
    {
        public DayLatroWidgetViewModel? ViewModel { get; }

        public DayLatroWidget()
        {
            // Initialize ViewModel with dependency injection
            ViewModel = new DayLatroWidgetViewModel(
                DaylatroHighScoreService.Instance,
                App.GetService<UserProfileService>()
                    ?? throw new InvalidOperationException("UserProfileService not available")
            );

            // Set DataContext for bindings
            DataContext = ViewModel;

            // Wire up ViewModel events that require view interaction
            ViewModel.AnalyzeSeedRequested += OnAnalyzeSeedRequested;
            ViewModel.CopyToClipboardRequested += async (s, text) => await CopyToClipboardAsync(text);

            InitializeComponent();

            // Update ZIndex when IsMinimized changes - now handled by XAML binding to WidgetZIndex

            // Set initial ZIndex - now handled by XAML binding to WidgetZIndex

            // Initialize ViewModel after XAML is loaded
            ViewModel.Initialize();

            // Wire up cleanup
            this.DetachedFromVisualTree += OnDetachedFromVisualTree;
        }

        public async Task CopyToClipboardAsync(string text)
        {
            try
            {
                var topLevel = TopLevel.GetTopLevel(this);
                if (topLevel?.Clipboard != null)
                {
                    await topLevel.Clipboard.SetTextAsync(text);
                    DebugLogger.Log("DayLatroWidget", $"Copied to clipboard: {text}");
                }
            }
            catch (Exception ex)
            {
                DebugLogger.LogError("DayLatroWidget", $"Failed to copy to clipboard: {ex.Message}");
            }
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

                var platformServices = ServiceHelper.GetService<IPlatformServices>();
                if (platformServices?.SupportsAnalyzer == true)
                {
                    var analyzeModal = new AnalyzeModal();
                    analyzeModal.SetSeedAndAnalyze(seed);

                    var stdModal = new StandardModal("ANALYZE");
                    stdModal.SetContent(analyzeModal);
                    stdModal.BackClicked += (s, _) => mainMenu.HideModalContent();
                    mainMenu.ShowModalContent(stdModal, "SEED ANALYZER");
                }
                else
                {
                    // Analyzer not available in browser
                    return;
                }
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
                ViewModel.CopyToClipboardRequested -= OnAnalyzeSeedRequested;
                ViewModel.Dispose();
            }
        }

        // Event handlers inherited from BaseWidgetControl:
        // - OnMinimizedIconPressed
        // - OnMinimizedIconReleased
    }
}

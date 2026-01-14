using System;
using System.Linq;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
using BalatroSeedOracle.Helpers;
using BalatroSeedOracle.Services;
using BalatroSeedOracle.ViewModels;

namespace BalatroSeedOracle.Views.Modals
{
    public partial class SearchModal : UserControl
    {
        public SearchModalViewModel ViewModel { get; }

        public event EventHandler? CloseRequested;

        public SearchModal()
        {
            var searchManager = ServiceHelper.GetRequiredService<SearchManager>();
            var userProfileService = ServiceHelper.GetRequiredService<UserProfileService>();
            var appDataStore = ServiceHelper.GetRequiredService<BalatroSeedOracle.Services.Storage.IAppDataStore>();
            var platformServices = ServiceHelper.GetRequiredService<IPlatformServices>();
            ViewModel = new SearchModalViewModel(searchManager, userProfileService, appDataStore, platformServices);
            DataContext = ViewModel;

            ViewModel.CloseRequested += (s, e) => CloseRequested?.Invoke(this, e);
            ViewModel.MinimizeToDesktopRequested += OnMinimizeToDesktopRequested;
            ViewModel.CopyToClipboardRequested += async (s, text) => await CopyToClipboardAsync(text);

            InitializeComponent();
            WireUpComponentEvents();
        }

        public async Task CopyToClipboardAsync(string text)
        {
            try
            {
                var topLevel = TopLevel.GetTopLevel(this);
                if (topLevel?.Clipboard != null)
                {
                    await topLevel.Clipboard.SetTextAsync(text);
                    DebugLogger.Log("SearchModal", $"Copied to clipboard: {text}");
                }
            }
            catch (Exception ex)
            {
                DebugLogger.LogError("SearchModal", $"Failed to copy to clipboard: {ex.Message}");
            }
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }

        protected override void OnUnloaded(Avalonia.Interactivity.RoutedEventArgs e)
        {
            base.OnUnloaded(e);
            ViewModel?.Dispose();
        }

        /// <summary>
        /// Minimal adapter: wire component events to ViewModel commands
        /// This is proper MVVM - components communicate via events, we forward to ViewModel
        /// </summary>
        private void WireUpComponentEvents()
        {
            // CREATE NEW FILTER button callback (FilterSelectorControl is created dynamically by ViewModel)
            ViewModel.SetNewFilterRequestedCallback(OpenFiltersModal);

            // EDIT FILTER button callback - opens FiltersModal with current filter loaded
            ViewModel.SetEditFilterRequestedCallback(EditCurrentFilter);

            // DeckAndStakeSelector is no longer in SearchModal XAML - deck/stake selection
            // is now handled via SettingsTab with bindings. This code is obsolete.
        }

        /// <summary>
        /// Tab click handler - PROPER MVVM: Updates ViewModel instead of directly manipulating UI
        /// </summary>
        // Tab click wiring removed: native TabControl handles selection

        /// <summary>
        /// Opens the FiltersModal via ViewModel's MainMenu reference
        /// </summary>
        private void OpenFiltersModal()
        {
            try
            {
                DebugLogger.Log("SearchModal", "OpenFiltersModal called");
                
                if (ViewModel.MainMenu != null)
                {
                    DebugLogger.Log("SearchModal", "Calling MainMenu.ShowFiltersModal()");
                    ViewModel.MainMenu.ShowFiltersModal();
                    DebugLogger.Log("SearchModal", "MainMenu.ShowFiltersModal() completed");
                }
                else
                {
                    DebugLogger.LogError(
                        "SearchModal",
                        "ViewModel.MainMenu is NULL! Can't open FiltersModal"
                    );
                }
            }
            catch (Exception ex)
            {
                DebugLogger.LogError(
                    "SearchModal",
                    $"Error opening FiltersModal: {ex.Message}"
                );
            }
        }

        /// <summary>
        /// Edits the current filter - closes SearchModal and opens FiltersModal with the filter loaded
        /// </summary>
        private void EditCurrentFilter(string? filterPath)
        {
            try
            {
                if (string.IsNullOrEmpty(filterPath))
                {
                    DebugLogger.LogError(
                        "SearchModal",
                        "Cannot edit filter: filterPath is null or empty"
                    );
                    return;
                }

                if (ViewModel.MainMenu == null)
                {
                    DebugLogger.LogError(
                        "SearchModal",
                        "ViewModel.MainMenu is NULL! Can't open FiltersModal"
                    );
                    return;
                }

                DebugLogger.Log("SearchModal", $"EditCurrentFilter called with path: {filterPath}");

                // Convert filter path to filter ID (remove extension and directory)
                // Example: "JsonFilters/MyFilter.json" -> "MyFilter"
                var filterId = System.IO.Path.GetFileNameWithoutExtension(filterPath);

                // Close SearchModal
                CloseRequested?.Invoke(this, EventArgs.Empty);

                // Open FiltersModal with the filter loaded
                _ = ViewModel.MainMenu.ShowFiltersModalDirectAsync(filterId);

                DebugLogger.Log("SearchModal", $"Opened FiltersModal with filter: {filterId}");
            }
            catch (Exception ex)
            {
                DebugLogger.LogError("SearchModal", $"Error editing current filter: {ex.Message}");
            }
        }

        /// <summary>
        /// Handle minimize to desktop request - creates SearchDesktopIcon and closes modal
        /// </summary>
        private void OnMinimizeToDesktopRequested(
            object? sender,
            (string searchId, string? configPath, string filterName) args
        )
        {
            try
            {
                DebugLogger.Log(
                    "SearchModal",
                    $"OnMinimizeToDesktopRequested: SearchID={args.searchId}, Filter={args.filterName}"
                );

                if (ViewModel.MainMenu == null)
                {
                    DebugLogger.LogError(
                        "SearchModal",
                        "ViewModel.MainMenu is NULL! Can't create desktop icon"
                    );
                    return;
                }

                // Create the SearchDesktopIcon widget on the main menu
                ViewModel.MainMenu.ShowSearchDesktopIcon(args.searchId, args.configPath);

                // Close the search modal
                CloseRequested?.Invoke(this, EventArgs.Empty);

                DebugLogger.Log("SearchModal", "Search minimized to desktop successfully");
            }
            catch (Exception ex)
            {
                DebugLogger.LogError(
                    "SearchModal",
                    $"Error minimizing search to desktop: {ex.Message}"
                );
            }
        }
    }
}

using System;
using System.Linq;
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
        private Components.BalatroTabControl? _tabHeader;
        private bool _suppressHeaderSync = false;

        public event EventHandler? CloseRequested;

        public SearchModal()
        {
            var searchManager = ServiceHelper.GetRequiredService<SearchManager>();
            ViewModel = new SearchModalViewModel(searchManager);
            DataContext = ViewModel;

            ViewModel.CloseRequested += (s, e) => CloseRequested?.Invoke(this, e);
            ViewModel.MinimizeToDesktopRequested += OnMinimizeToDesktopRequested;

            InitializeComponent();
            WireUpComponentEvents();
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
            // Balatro-style tab header setup
            _tabHeader = this.FindControl<Components.BalatroTabControl>("TabHeader");
            if (_tabHeader != null)
            {
                // Initialize tab titles from ViewModel
                var titles = ViewModel.TabItems.Select(t => t.Header).ToArray();
                _tabHeader.SetTabs(titles);

                // Sync header when user clicks a tab button
                _tabHeader.TabChanged += (s, tabIndex) =>
                {
                    _suppressHeaderSync = true;
                    ViewModel.SelectedTabIndex = tabIndex;
                    ViewModel.UpdateTabVisibility(tabIndex);
                    _suppressHeaderSync = false;
                };

                // Sync header when ViewModel changes tab programmatically (e.g., after search)
                ViewModel.PropertyChanged += (s, e) =>
                {
                    if (
                        !_suppressHeaderSync
                        && e.PropertyName == nameof(ViewModel.SelectedTabIndex)
                    )
                    {
                        _tabHeader.SwitchToTab(ViewModel.SelectedTabIndex);
                    }
                };
            }

            // CREATE NEW FILTER button callback (FilterSelectorControl is created dynamically by ViewModel)
            ViewModel.SetNewFilterRequestedCallback(OpenFiltersModal);

            // DeckAndStakeSelector → ViewModel properties
            var deckStakeSelector = this.FindControl<Components.DeckAndStakeSelector>(
                "DeckStakeSelector"
            );
            if (deckStakeSelector != null)
            {
                deckStakeSelector.SelectionChanged += (s, selection) =>
                {
                    var deckNames = new[]
                    {
                        "Red",
                        "Blue",
                        "Yellow",
                        "Green",
                        "Black",
                        "Magic",
                        "Nebula",
                        "Ghost",
                        "Abandoned",
                        "Checkered",
                        "Zodiac",
                        "Painted",
                        "Anaglyph",
                        "Plasma",
                        "Erratic",
                    };
                    var stakeNames = new[]
                    {
                        "White",
                        "Red",
                        "Green",
                        "Black",
                        "Blue",
                        "Purple",
                        "Orange",
                        "Gold",
                    };

                    if (selection.deckIndex >= 0 && selection.deckIndex < deckNames.Length)
                        ViewModel.DeckSelection = deckNames[selection.deckIndex];

                    if (selection.stakeIndex >= 0 && selection.stakeIndex < stakeNames.Length)
                        ViewModel.StakeSelection = stakeNames[selection.stakeIndex];
                };

                // When the user clicks Select, jump to the Search tab
                deckStakeSelector.DeckSelected += (s, _) =>
                {
                    ViewModel.SelectedTabIndex = 1; // Search tab (indices shifted)
                    ViewModel.UpdateTabVisibility(1);
                    _tabHeader?.SwitchToTab(1);
                };
            }
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
                if (ViewModel.MainMenu != null)
                {
                    ViewModel.MainMenu.ShowFiltersModal();
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
                DebugLogger.LogError("SearchModal", $"Error opening FiltersModal: {ex.Message}");
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

using System;
using System.Linq;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Avalonia.Interactivity;
using Avalonia.Media;
using BalatroSeedOracle.ViewModels;
using BalatroSeedOracle.Services;
using BalatroSeedOracle.Helpers;

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
                    if (!_suppressHeaderSync && e.PropertyName == nameof(ViewModel.SelectedTabIndex))
                    {
                        _tabHeader.SwitchToTab(ViewModel.SelectedTabIndex);
                    }
                };
            }

            // FilterSelectorControl → ViewModel.LoadFilterCommand
            var filterSelector = this.FindControl<Components.FilterSelectorControl>("FilterSelector");
            if (filterSelector != null)
            {
                // CRITICAL: Set SearchModal mode to show SELECT button
                filterSelector.IsInSearchModal = true;

                // List click: Show preview only (don't advance)
                filterSelector.FilterSelected += async (s, path) =>
                {
                    DebugLogger.Log("SearchModal", $"Filter clicked in list! Path: {path}");
                    await ViewModel.LoadFilterAsync(path);
                    DebugLogger.Log("SearchModal", "Filter loaded and displayed for preview - staying on Select Filter tab");
                };

                // Confirmed load: fired by Select button, then advance to Search
                filterSelector.FilterConfirmed += async (s, path) =>
                {
                    DebugLogger.Log("SearchModal", $"SELECT THIS FILTER button clicked! Path: {path}");
                    await ViewModel.LoadFilterAsync(path);
                    DebugLogger.Log("SearchModal", "Filter confirmed, auto-advancing to Search tab");

                    // Auto-advance directly to Search tab (tab 2)
                    ViewModel.SelectedTabIndex = 2;
                    ViewModel.UpdateTabVisibility(2);
                    _tabHeader?.SwitchToTab(2);
                };

                // Create new filter: Open FiltersModal
                filterSelector.NewFilterRequested += (s, e) =>
                {
                    DebugLogger.Log("SearchModal", "CREATE NEW FILTER button clicked!");
                    OpenFiltersModal();
                };
            }

            // DeckAndStakeSelector → ViewModel properties
            var deckStakeSelector = this.FindControl<Components.DeckAndStakeSelector>("DeckStakeSelector");
            if (deckStakeSelector != null)
            {
                deckStakeSelector.SelectionChanged += (s, selection) =>
                {
                    var deckNames = new[] { "Red", "Blue", "Yellow", "Green", "Black", "Magic", "Nebula", "Ghost",
                                           "Abandoned", "Checkered", "Zodiac", "Painted", "Anaglyph", "Plasma", "Erratic" };
                    var stakeNames = new[] { "White", "Red", "Green", "Black", "Blue", "Purple", "Orange", "Gold" };

                    if (selection.deckIndex >= 0 && selection.deckIndex < deckNames.Length)
                        ViewModel.DeckSelection = deckNames[selection.deckIndex];

                    if (selection.stakeIndex >= 0 && selection.stakeIndex < stakeNames.Length)
                        ViewModel.StakeSelection = stakeNames[selection.stakeIndex];
                };

                // When the user clicks Select, jump to the Search tab
                deckStakeSelector.DeckSelected += (s, _) =>
                {
                    ViewModel.SelectedTabIndex = 2; // Search tab
                    ViewModel.UpdateTabVisibility(2);
                    _tabHeader?.SwitchToTab(2);
                };
            }
        }

        /// <summary>
        /// Tab click handler - PROPER MVVM: Updates ViewModel instead of directly manipulating UI
        /// </summary>
        // Tab click wiring removed: native TabControl handles selection

        /// <summary>
        /// Opens the FiltersModal by navigating up to BalatroMainMenu
        /// </summary>
        private void OpenFiltersModal()
        {
            try
            {
                // Walk up visual tree to find BalatroMainMenu
                var parent = this.Parent;
                while (parent != null)
                {
                    if (parent is BalatroMainMenu mainMenu)
                    {
                        DebugLogger.Log("SearchModal", "Found BalatroMainMenu, calling ShowFiltersModal()");
                        mainMenu.ShowFiltersModal();
                        return;
                    }
                    parent = (parent as Control)?.Parent;
                }

                DebugLogger.LogError("SearchModal", "Could not find BalatroMainMenu in visual tree!");
            }
            catch (Exception ex)
            {
                DebugLogger.LogError("SearchModal", $"Error opening FiltersModal: {ex.Message}");
            }
        }
    }
}

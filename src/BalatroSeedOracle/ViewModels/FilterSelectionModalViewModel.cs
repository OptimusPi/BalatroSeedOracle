using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using BalatroSeedOracle.Helpers;
using BalatroSeedOracle.Models;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace BalatroSeedOracle.ViewModels
{
    public partial class FilterSelectionModalViewModel : ObservableObject, IModalBackNavigable
    {
        // Configuration passed in constructor
        public bool EnableSearch { get; }
        public bool EnableEdit { get; }
        public bool EnableCopy { get; }
        public bool EnableDelete { get; }
        public bool EnableAnalyze { get; }

        // DEBUG: Add comprehensive logging

        // Child ViewModel for paginated filter list
        public PaginatedFilterBrowserViewModel FilterList { get; }

        // Selected filter details
        [ObservableProperty]
        private FilterBrowserItem? _selectedFilter;

        // Active tab index
        [ObservableProperty]
        private int _activeTabIndex = 0;

        // Details panel visibility
        public bool ShowDetailsPanel => SelectedFilter != null;
        public bool ShowPlaceholder => SelectedFilter == null;

        // Tab content visibility
        public bool ShowFilterTab => ActiveTabIndex == 0 && ShowDetailsPanel;
        public bool ShowScoreTab => ActiveTabIndex == 1 && ShowDetailsPanel;
        public bool ShowDeckTab => ActiveTabIndex == 2 && ShowDetailsPanel;

        // Filter details for display
        public string FilterName => SelectedFilter?.Name ?? "";
        public string FilterAuthor => SelectedFilter?.Author ?? "";
        public string FilterDescription => SelectedFilter?.Description ?? "";
        public string CreatedDate => SelectedFilter?.DateCreated.ToString("MMM yyyy") ?? "";

        // Deck details for Preferred Deck tab
        public string SelectedDeckName => SelectedFilter?.DeckName ?? "Red";
        public string SelectedStakeName => SelectedFilter?.StakeName ?? "White";

        public Avalonia.Media.IImage? DeckCardImage
        {
            get
            {
                var deckName = SelectedDeckName;
                var stakeName = SelectedStakeName;
                return Services.SpriteService.Instance.GetDeckWithStakeSticker(deckName, stakeName);
            }
        }

        public string SelectedDeckDescription
        {
            get
            {
                if (SelectedFilter == null)
                    return "";
                var deckName = SelectedFilter.DeckName;
                if (Models.BalatroData.DeckDescriptions.TryGetValue(deckName, out var description))
                {
                    return description;
                }
                return "";
            }
        }

        // Deck/Stake indices for DeckAndStakeSelector component
        [ObservableProperty]
        private int _selectedDeckIndex = 0;

        [ObservableProperty]
        private int _selectedStakeIndex = 0;

        // Item counts for preview
        public int MustHaveCount => SelectedFilter?.MustCount ?? 0;
        public int ShouldHaveCount => SelectedFilter?.ShouldCount ?? 0;
        public int MustNotCount => SelectedFilter?.MustNotCount ?? 0;

        // Placeholder text when nothing selected
        public string PlaceholderText =>
            EnableEdit || EnableCopy ? "Please select a filter or CREATE NEW" : "Please select a filter";

        // Result to return when modal closes
        public FilterSelectionResult Result { get; private set; } = new() { Cancelled = true };

        // Events
        public event EventHandler? ModalCloseRequested;
        public event EventHandler<string>? DeleteConfirmationRequested;

        public FilterSelectionModalViewModel(
            bool enableSearch = false,
            bool enableEdit = false,
            bool enableCopy = false,
            bool enableDelete = false,
            bool enableAnalyze = false
        )
        {
            EnableSearch = enableSearch;
            EnableEdit = enableEdit;
            EnableCopy = enableCopy;
            EnableDelete = enableDelete;
            EnableAnalyze = enableAnalyze;

            DebugLogger.Log(
                "FilterSelectionModalVM",
                $"🔵 CONSTRUCTOR: EnableSearch={EnableSearch}, EnableEdit={EnableEdit}, EnableCopy={EnableCopy}"
            );

            // Create child ViewModel for filter list
            FilterList = new PaginatedFilterBrowserViewModel();

            // Subscribe to filter selection changes
            FilterList.PropertyChanged += (s, e) =>
            {
                if (e.PropertyName == nameof(FilterList.SelectedFilter))
                {
                    SelectedFilter = FilterList.SelectedFilter;
                }
            };
        }

        partial void OnSelectedFilterChanged(FilterBrowserItem? value)
        {
            OnPropertyChanged(nameof(ShowDetailsPanel));
            OnPropertyChanged(nameof(ShowPlaceholder));
            OnPropertyChanged(nameof(FilterName));
            OnPropertyChanged(nameof(FilterAuthor));
            OnPropertyChanged(nameof(FilterDescription));
            OnPropertyChanged(nameof(CreatedDate));
            OnPropertyChanged(nameof(SelectedDeckName));
            OnPropertyChanged(nameof(SelectedStakeName));
            OnPropertyChanged(nameof(DeckCardImage));
            OnPropertyChanged(nameof(SelectedDeckDescription));
            OnPropertyChanged(nameof(MustHaveCount));
            OnPropertyChanged(nameof(ShouldHaveCount));
            OnPropertyChanged(nameof(MustNotCount));
            OnPropertyChanged(nameof(ShowFilterTab));
            OnPropertyChanged(nameof(ShowScoreTab));
            OnPropertyChanged(nameof(ShowDeckTab));

            // Update deck/stake indices for DeckSpinner display
            if (value != null)
            {
                // Map deck name to index
                var decks = new[]
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
                SelectedDeckIndex = Array.FindIndex(
                    decks,
                    d => d.Equals(value.DeckName, StringComparison.OrdinalIgnoreCase)
                );
                if (SelectedDeckIndex < 0)
                    SelectedDeckIndex = 0;

                // Map stake name to index
                var stakes = new[] { "white", "red", "green", "black", "blue", "purple", "orange", "gold" };
                SelectedStakeIndex = Array.FindIndex(
                    stakes,
                    s => s.Equals(value.StakeName, StringComparison.OrdinalIgnoreCase)
                );
                if (SelectedStakeIndex < 0)
                    SelectedStakeIndex = 0;
            }
        }

        partial void OnActiveTabIndexChanged(int value)
        {
            OnPropertyChanged(nameof(ShowFilterTab));
            OnPropertyChanged(nameof(ShowScoreTab));
            OnPropertyChanged(nameof(ShowDeckTab));
        }

        [RelayCommand]
        private void Search()
        {
            DebugLogger.Log("FilterSelectionModal", "🔵 Search() called");

            if (SelectedFilter == null)
            {
                DebugLogger.Log("FilterSelectionModal", "❌ SelectedFilter is null");
                return;
            }

            DebugLogger.Log(
                "FilterSelectionModal",
                $"✅ SelectedFilter: {SelectedFilter.Name}, ID: {SelectedFilter.FilterId}"
            );

            if (SelectedFilter.IsCreateNew)
            {
                // Cannot search with blank filter
                DebugLogger.Log("FilterSelectionModal", "Cannot search with CREATE NEW filter");
                throw new ArgumentException("Cannot search with CREATE NEW filter");
            }

            Result = new FilterSelectionResult
            {
                Cancelled = false,
                Action = FilterAction.Search,
                FilterId = SelectedFilter.FilterId,
            };

            DebugLogger.Log(
                "FilterSelectionModal",
                $"🚀 Invoking ModalCloseRequested with FilterId: {SelectedFilter.FilterId}"
            );
            ModalCloseRequested?.Invoke(this, EventArgs.Empty);
        }

        [RelayCommand]
        private void Edit()
        {
            if (SelectedFilter == null)
                return;

            Result = new FilterSelectionResult
            {
                Cancelled = false,
                Action = SelectedFilter.IsCreateNew ? FilterAction.CreateNew : FilterAction.Edit,
                FilterId = SelectedFilter.IsCreateNew ? null : SelectedFilter.FilterId,
            };

            ModalCloseRequested?.Invoke(this, EventArgs.Empty);
        }

        [RelayCommand]
        private void CreateNew()
        {
            DebugLogger.Log(
                "FilterSelectionModalViewModel",
                "CreateNew command called - CREATE NEW FILTER button clicked!"
            );

            // Create new filter directly (called from placeholder button)
            Result = new FilterSelectionResult
            {
                Cancelled = false,
                Action = FilterAction.CreateNew,
                FilterId = null,
            };

            DebugLogger.Log("FilterSelectionModalViewModel", "Invoking ModalCloseRequested for CreateNew");
            ModalCloseRequested?.Invoke(this, EventArgs.Empty);
        }

        [RelayCommand]
        private void Copy()
        {
            if (SelectedFilter == null || SelectedFilter.IsCreateNew)
                return;

            Result = new FilterSelectionResult
            {
                Cancelled = false,
                Action = FilterAction.Copy,
                FilterId = SelectedFilter.FilterId,
            };

            ModalCloseRequested?.Invoke(this, EventArgs.Empty);
        }

        [RelayCommand]
        private void Delete()
        {
            if (SelectedFilter == null || SelectedFilter.IsCreateNew)
                return;

            // Request confirmation from View (code-behind will show dialog)
            DeleteConfirmationRequested?.Invoke(this, SelectedFilter.Name);
        }

        /// <summary>
        /// Called by View after user confirms delete.
        /// Performs the deletion and refreshes the UI without closing the modal.
        /// </summary>
        public async Task ConfirmDeleteAsync()
        {
            try
            {
                if (SelectedFilter == null || SelectedFilter.IsCreateNew)
                    return;

                var filterIdToDelete = SelectedFilter.FilterId;
                var filterNameToDelete = SelectedFilter.Name;

                DebugLogger.Log(
                    "FilterSelectionModalVM",
                    $"Starting delete for filter: {filterNameToDelete} ({filterIdToDelete})"
                );

                // Get FilterService to perform the deletion
                var filterService = Helpers.ServiceHelper.GetRequiredService<Services.IFilterService>();
                var filtersDir = AppPaths.FiltersDir;
                var filterPath = System.IO.Path.Combine(filtersDir, $"{filterIdToDelete}.json");

                // Perform deletion (this also removes from cache)
                var deleted = await filterService.DeleteFilterAsync(filterPath);

                if (!deleted)
                {
                    DebugLogger.LogError("FilterSelectionModalVM", $"Failed to delete filter: {filterIdToDelete}");
                    return;
                }

                DebugLogger.Log("FilterSelectionModalVM", $"Filter deleted successfully: {filterIdToDelete}");

                // CRITICAL: Clear the selected filter FIRST to avoid showing stale data
                SelectedFilter = null;
                FilterList.SelectedFilter = null;

                // Refresh the filter list to reflect the deletion (reloads from cache/disk)
                FilterList.RefreshFilters();

                DebugLogger.Log(
                    "FilterSelectionModalVM",
                    $"Filter list refreshed - {FilterList.CurrentPageFilters.Count} filters on current page"
                );

                // Auto-select the first filter if any remain, otherwise show placeholder
                if (FilterList.CurrentPageFilters.Count > 0)
                {
                    // Use the SelectFilterCommand to properly trigger selection
                    var firstFilterViewModel = FilterList.CurrentPageFilters[0];
                    await FilterList.SelectFilterCommand.ExecuteAsync(firstFilterViewModel);

                    DebugLogger.Log(
                        "FilterSelectionModalVM",
                        $"Auto-selected first filter: {firstFilterViewModel.DisplayText}"
                    );
                }
                else
                {
                    DebugLogger.Log("FilterSelectionModalVM", "No filters remaining - showing placeholder");
                }

                // Modal stays open so user can continue managing filters
            }
            catch (Exception ex)
            {
                DebugLogger.LogError("FilterSelectionModalViewModel", $"ConfirmDeleteAsync failed: {ex.Message}");
                throw;
            }
        }

        [RelayCommand]
        private void Analyze()
        {
            if (SelectedFilter == null)
                return;

            if (SelectedFilter.IsCreateNew)
            {
                // Cannot analyze blank filter
                DebugLogger.Log("FilterSelectionModal", "Cannot analyze CREATE NEW filter");
                return;
            }

            Result = new FilterSelectionResult
            {
                Cancelled = false,
                Action = FilterAction.Analyze,
                FilterId = SelectedFilter.FilterId,
            };

            ModalCloseRequested?.Invoke(this, EventArgs.Empty);
        }

        [RelayCommand]
        private void Back()
        {
            // If a filter is selected, go back to placeholder view instead of closing
            if (TryGoBack())
            {
                return; // Stayed in modal, just reset view
            }

            // Only close if we're already on the placeholder page
            Result = new FilterSelectionResult { Cancelled = true, Action = FilterAction.Cancelled };

            ModalCloseRequested?.Invoke(this, EventArgs.Empty);
        }

        /// <summary>
        /// Implements IModalBackNavigable - Navigate back through internal state
        /// </summary>
        public bool TryGoBack()
        {
            // Priority 1: If viewing filter details, go back to initial page (placeholder)
            if (SelectedFilter != null)
            {
                // Clear selection to return to "Please select or create new" page
                SelectedFilter = null;
                FilterList.SelectedFilter = null;

                // Reset to first tab when returning to initial page
                if (ActiveTabIndex > 0)
                {
                    ActiveTabIndex = 0;
                }

                return true; // We handled the back navigation
            }

            // Priority 2: If on initial page with tabs visible, navigate tabs
            if (ActiveTabIndex > 0)
            {
                ActiveTabIndex = ActiveTabIndex - 1;
                return true; // We handled the back navigation
            }

            // Priority 3: On initial page, first tab - allow modal to close
            return false; // Let the modal close
        }
    }
}

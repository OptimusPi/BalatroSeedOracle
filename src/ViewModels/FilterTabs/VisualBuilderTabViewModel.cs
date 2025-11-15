using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;
using Avalonia.Media;
using Avalonia.Threading;
using BalatroSeedOracle.Controls;
using BalatroSeedOracle.Helpers;
using BalatroSeedOracle.Models;
using BalatroSeedOracle.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace BalatroSeedOracle.ViewModels.FilterTabs
{
    /// <summary>
    /// UI state enum for Phase 2 state system
    /// Controls which parts of the UI are visible and how they're displayed
    /// </summary>
    public enum EditingState
    {
        Default, // Normal view - all sections visible at normal size
        DragActive, // User is dragging - show overlays
        ClauseEdit, // Editing OR/AND clause - expand to full column
        ScoreEdit, // Editing score list - expand to full column
    }

    public partial class VisualBuilderTabViewModel : ObservableObject
    {
        private readonly FiltersModalViewModel? _parentViewModel;

        // Auto-save debouncing
        private CancellationTokenSource? _autoSaveCts;
        private const int AutoSaveDebounceMs = 500;

        [ObservableProperty]
        private string _autoSaveStatus = "";

        [ObservableProperty]
        private bool _isAutoSaving = false;

        [ObservableProperty]
        private string _searchFilter = "";

        [ObservableProperty]
        private bool _isLoading = true;

        // Phase 2: UI State Management
        [ObservableProperty]
        private EditingState _currentEditingState = EditingState.Default;

        // Computed properties for state-based visibility
        public bool IsDefaultState => CurrentEditingState == EditingState.Default;
        public bool IsDragActiveState => CurrentEditingState == EditingState.DragActive;
        public bool IsClauseEditState => CurrentEditingState == EditingState.ClauseEdit;
        public bool IsScoreEditState => CurrentEditingState == EditingState.ScoreEdit;

        // Computed properties for layout control
        public bool ShouldShowItemShelf =>
            CurrentEditingState == EditingState.Default
            || CurrentEditingState == EditingState.DragActive;
        public bool ShouldExpandClauses => CurrentEditingState == EditingState.ClauseEdit;
        public bool ShouldExpandScoreList => CurrentEditingState == EditingState.ScoreEdit;
        public bool ShouldCollapseClauses => CurrentEditingState == EditingState.ScoreEdit;
        public bool ShouldCollapseScoreList => CurrentEditingState == EditingState.ClauseEdit;

        // Track which clause is being edited
        [ObservableProperty]
        private string _editingClauseType = ""; // "Or" or "And"

        // Computed visibility for zones during clause editing
        public bool IsEditingOrClause => EditingClauseType == "Or";
        public bool IsEditingAndClause => EditingClauseType == "And";
        public bool ShouldHideAndTray => OrTrayItems.Count > 0 && EditingClauseType == "Or";
        public bool ShouldHideOrTray => AndTrayItems.Count > 0 && EditingClauseType == "And";
        public bool ShouldHideShouldZone => CurrentEditingState == EditingState.ClauseEdit;

        // Drop zones - always expanded for simplicity
        [ObservableProperty]
        private bool _isMustExpanded = true;

        [ObservableProperty]
        private bool _isShouldExpanded = true;

        [ObservableProperty]
        private bool _isCantExpanded = true;

        [ObservableProperty]
        private bool _isDragging = false; // Track if user is dragging a card

        // Hover state tracking for drop-zones (used to show indicators only when hovering over collapsed zones)
        [ObservableProperty]
        private bool _isMustHovered = false;

        [ObservableProperty]
        private bool _isShouldHovered = false;

        [ObservableProperty]
        private bool _isCantHovered = false;

        // Carousel pagination - show arrows when there are multiple pages
        // TODO: Implement actual pagination logic - for now always false (arrows hidden)
        public bool MustHasMultiplePages => false;
        public bool ShouldHasMultiplePages => false;
        public bool BannedHasMultiplePages => false;

        // Expose parent's FilterName for display
        public string FilterName => _parentViewModel?.FilterName ?? "New Filter";

        // Expose parent's SelectedDeck for flip animation
        public string SelectedDeck => _parentViewModel?.SelectedDeck ?? "Red";

        // Available items
        public ObservableCollection<FilterItem> AllJokers { get; }
        public ObservableCollection<FilterItem> AllTags { get; }
        public ObservableCollection<FilterItem> AllVouchers { get; }
        public ObservableCollection<FilterItem> AllTarots { get; }
        public ObservableCollection<FilterItem> AllPlanets { get; }
        public ObservableCollection<FilterItem> AllSpectrals { get; }
        public ObservableCollection<FilterItem> AllBosses { get; }
        public ObservableCollection<FilterItem> AllWildcards { get; }
        public ObservableCollection<FilterItem> AllStandardCards { get; }

        // Filtered items (based on search)
        public ObservableCollection<FilterItem> FilteredJokers { get; }
        public ObservableCollection<FilterItem> FilteredTags { get; }
        public ObservableCollection<FilterItem> FilteredVouchers { get; }
        public ObservableCollection<FilterItem> FilteredTarots { get; }
        public ObservableCollection<FilterItem> FilteredPlanets { get; }
        public ObservableCollection<FilterItem> FilteredSpectrals { get; }
        public ObservableCollection<FilterItem> FilteredBosses { get; }
        public ObservableCollection<FilterItem> FilteredWildcards { get; }
        public ObservableCollection<FilterItem> FilteredStandardCards { get; }

        public ObservableCollection<FilterItem> FilteredItems { get; }

        // Main category selection (6 main categories)
        [ObservableProperty]
        private string _selectedMainCategory = "Joker";

        // Subcategory tracking within each main category
        [ObservableProperty]
        private string _selectedCategory = "Legendary";

        partial void OnSelectedCategoryChanged(string value)
        {
            // Notify button images to update when category changes
            OnPropertyChanged(nameof(EditionBaseImage));
            OnPropertyChanged(nameof(TenOfSpadesImage));
        }

        // Grouped items for the new UI
        public class ItemGroup : ObservableObject
        {
            public string GroupName { get; set; } = "";
            public ObservableCollection<FilterItem> Items { get; set; } = new();

            // Vouchers: 8 wide (580px), Others: 5 wide (380px)
            public double ShelfMaxWidth => GroupName == "Vouchers" ? 580 : 380;
        }

        [ObservableProperty]
        private ObservableCollection<ItemGroup> _groupedItems = new();

        // Helper button visibility properties
        [ObservableProperty]
        private bool _isJokerCategorySelected = true;

        [ObservableProperty]
        private bool _isConsumableCategorySelected = false;

        [ObservableProperty]
        private bool _isSkipTagCategorySelected = false;

        [ObservableProperty]
        private bool _isBossCategorySelected = false;

        [ObservableProperty]
        private bool _isVoucherCategorySelected = false;

        [ObservableProperty]
        private bool _isStandardCardCategorySelected = false;

        // Phase 3: Edition/Sticker/Seal selectors (apply to ALL items in shelf)
        [ObservableProperty]
        private string _selectedEdition = "None"; // None, Foil, Holo, Polychrome, Negative

        [ObservableProperty]
        private bool _stickerPerishable = false;

        [ObservableProperty]
        private bool _stickerEternal = false;

        [ObservableProperty]
        private bool _stickerRental = false;

        [ObservableProperty]
        private string _selectedSeal = "None"; // None, Purple, Gold, Red, Blue (for StandardCards only)

        // Button icon images
        public IImage? DebuffedIconImage => Services.SpriteService.Instance.GetEditionImage("debuffed");

        // Base image for edition buttons - 10 of Spades for Standard Cards, joker for Jokers
        public IImage? EditionBaseImage => SelectedCategory == "StandardCard"
            ? TenOfSpadesImage
            : Services.SpriteService.Instance.GetJokerImage("Joker");

        // 10 of Spades card image for seal/edition buttons
        public IImage? TenOfSpadesImage => Services.SpriteService.Instance.GetPlayingCardImage("Spades", "10");

        // Legacy properties (kept for compatibility)
        [ObservableProperty]
        private string _currentEdition = "None";

        [ObservableProperty]
        private string _currentEnhancement = "None";

        [ObservableProperty]
        private string _currentSeal = "None";

        // Preferred Deck/Stake selection for previews
        [ObservableProperty]
        private int _selectedDeckIndex = 0;

        [ObservableProperty]
        private int _selectedStakeIndex = 0;

        // Computed properties for button visibility based on category
        public bool ShowEditionButtons =>
            SelectedMainCategory == "Joker" || SelectedMainCategory == "StandardCard";
        public bool ShowStickerButtons => SelectedMainCategory == "Joker";
        public bool ShowSealButtons => SelectedMainCategory == "StandardCard";
        public bool ShowEnhancementButtons => SelectedMainCategory == "StandardCard";

        // Notify property changes when category changes
        partial void OnSelectedMainCategoryChanged(string value)
        {
            OnPropertyChanged(nameof(ShowEditionButtons));
            OnPropertyChanged(nameof(ShowStickerButtons));
            OnPropertyChanged(nameof(ShowSealButtons));
            OnPropertyChanged(nameof(ShowEnhancementButtons));

            // Reset edition/sticker/seal state when switching categories
            // Cards should start fresh with no enhancements
            SelectedEdition = "None";
            SelectedSeal = "None";
            StickerPerishable = false;
            StickerEternal = false;
            StickerRental = false;

            DebugLogger.Log(
                "VisualBuilderTab",
                $"Category changed to: {value} - reset all editions/stickers/seals"
            );
        }

        // Display name for current category
        public string CurrentCategoryDisplay =>
            SelectedCategory switch
            {
                "Favorites" => "Favorites",
                "Legendary" => "Legendary",
                "Rare" => "Rare",
                "Uncommon" => "Uncommon",
                "Common" => "Common",
                "Voucher" => "Voucher",
                "Tarot" => "Tarot",
                "Planet" => "Planet",
                "Spectral" => "Spectral",
                "PlayingCards" => "Playing Cards",
                "Tag" => "Tag",
                "Boss" => "Boss",
                _ => SelectedCategory,
            };

        // Category view models for proper data template binding
        public ObservableCollection<CategoryViewModel> Categories { get; }

        public class CategoryViewModel
        {
            public string Name { get; set; } = "";
            public string DisplayName { get; set; } = "";
            public ObservableCollection<FilterItem> Items { get; set; } = new();
        }

        // Unified Operator Tray - single tray that toggles between OR and AND modes
        public FilterOperatorItem UnifiedOperator { get; }

        // Selected items - these should sync with parent
        public ObservableCollection<FilterItem> SelectedMust { get; }
        public ObservableCollection<FilterItem> SelectedShould { get; }

        // Operator trays for Configure Score tab
        public ObservableCollection<FilterItem> OrTrayItems { get; }
        public ObservableCollection<FilterItem> AndTrayItems { get; }

        // Item configurations
        public Dictionary<string, ItemConfig> ItemConfigs { get; }

        public VisualBuilderTabViewModel(FiltersModalViewModel? parentViewModel = null)
        {
            _parentViewModel = parentViewModel;

            // Subscribe to parent's property changes to update FilterName
            if (_parentViewModel != null)
            {
                _parentViewModel.PropertyChanged += (s, e) =>
                {
                    if (e.PropertyName == nameof(FiltersModalViewModel.FilterName))
                    {
                        OnPropertyChanged(nameof(FilterName));
                    }
                };
            }

            // Initialize collections
            AllJokers = new ObservableCollection<FilterItem>();
            AllTags = new ObservableCollection<FilterItem>();
            AllVouchers = new ObservableCollection<FilterItem>();
            AllTarots = new ObservableCollection<FilterItem>();
            AllPlanets = new ObservableCollection<FilterItem>();
            AllSpectrals = new ObservableCollection<FilterItem>();
            AllBosses = new ObservableCollection<FilterItem>();
            AllWildcards = new ObservableCollection<FilterItem>();
            AllStandardCards = new ObservableCollection<FilterItem>();

            FilteredJokers = new ObservableCollection<FilterItem>();
            FilteredTags = new ObservableCollection<FilterItem>();
            FilteredVouchers = new ObservableCollection<FilterItem>();
            FilteredTarots = new ObservableCollection<FilterItem>();
            FilteredPlanets = new ObservableCollection<FilterItem>();
            FilteredSpectrals = new ObservableCollection<FilterItem>();
            FilteredBosses = new ObservableCollection<FilterItem>();
            FilteredWildcards = new ObservableCollection<FilterItem>();
            FilteredStandardCards = new ObservableCollection<FilterItem>();

            FilteredItems = new ObservableCollection<FilterItem>();
            GroupedItems = new ObservableCollection<ItemGroup>();

            // Initialize categories with proper data template approach
            Categories = new ObservableCollection<CategoryViewModel>
            {
                new()
                {
                    Name = "Legendary",
                    DisplayName = "Legendary",
                    Items = FilteredJokers,
                },
                new()
                {
                    Name = "Rare",
                    DisplayName = "Rare",
                    Items = FilteredJokers,
                },
                new()
                {
                    Name = "Uncommon",
                    DisplayName = "Uncommon",
                    Items = FilteredJokers,
                },
                new()
                {
                    Name = "Common",
                    DisplayName = "Common",
                    Items = FilteredJokers,
                },
                new()
                {
                    Name = "Voucher",
                    DisplayName = "Voucher",
                    Items = FilteredVouchers,
                },
                new()
                {
                    Name = "Tarot",
                    DisplayName = "Tarot",
                    Items = FilteredTarots,
                },
                new()
                {
                    Name = "Planet",
                    DisplayName = "Planet",
                    Items = FilteredPlanets,
                },
                new()
                {
                    Name = "Spectral",
                    DisplayName = "Spectral",
                    Items = FilteredSpectrals,
                },
                new()
                {
                    Name = "Tag",
                    DisplayName = "Tag",
                    Items = FilteredTags,
                },
                new()
                {
                    Name = "Boss",
                    DisplayName = "Boss",
                    Items = FilteredBosses,
                },
            };

            // Initialize Unified Operator Tray starting in OR mode
            UnifiedOperator = new FilterOperatorItem("OR")
            {
                DisplayName = "OR",
                Type = "Operator",
                Category = "Operator",
            };

            SelectedMust = new ObservableCollection<FilterItem>();
            SelectedShould = new ObservableCollection<FilterItem>();

            // Initialize operator trays
            OrTrayItems = new ObservableCollection<FilterItem>();
            AndTrayItems = new ObservableCollection<FilterItem>();

            ItemConfigs = new Dictionary<string, ItemConfig>();

            // Subscribe to collection changes for auto-save
            SelectedMust.CollectionChanged += OnZoneCollectionChanged;
            SelectedShould.CollectionChanged += OnZoneCollectionChanged;
            OrTrayItems.CollectionChanged += OnZoneCollectionChanged;
            AndTrayItems.CollectionChanged += OnZoneCollectionChanged;

            // Initialize commands
            AddToMustCommand = new RelayCommand<FilterItem>(AddToMust);
            AddToShouldCommand = new RelayCommand<FilterItem>(AddToShould);

            RemoveFromMustCommand = new RelayCommand<FilterItem>(RemoveFromMust);
            RemoveFromShouldCommand = new RelayCommand<FilterItem>(RemoveFromShould);

            AddToOrTrayCommand = new RelayCommand<FilterItem>(AddToOrTray);
            AddToAndTrayCommand = new RelayCommand<FilterItem>(AddToAndTray);
            RemoveFromOrTrayCommand = new RelayCommand<FilterItem>(RemoveFromOrTray);
            RemoveFromAndTrayCommand = new RelayCommand<FilterItem>(RemoveFromAndTray);

            CommitOrClauseCommand = new RelayCommand(CommitOrClause);
            CommitAndClauseCommand = new RelayCommand(CommitAndClause);

            ToggleOperatorCommand = new RelayCommand(ToggleOperator);
            ClearTrayCommand = new RelayCommand(ClearTray);

            // NOTE: SetEditionCommand and SetStickerCommand are auto-generated via [RelayCommand] attributes on their methods

            // Simple property change handling
            PropertyChanged += (s, e) =>
            {
                if (e.PropertyName == nameof(SelectedCategory))
                {
                    RefreshFilteredItems();
                }
                else if (e.PropertyName == nameof(SearchFilter))
                {
                    // When search text changes, rebuild filtered collections from All*
                    ApplyFilter();
                }
            };

            // Initialize data asynchronously without blocking UI
            // Fire and forget is OK here - data will populate when ready
            _ = Task.Run(LoadGameDataAsync);
        }

        public void SetCategory(string category)
        {
            // Auto-clear search when switching tabs for clean navigation
            SearchFilter = "";
            SelectedMainCategory = category;

            // Update visibility flags
            IsJokerCategorySelected = category == "Joker";
            IsConsumableCategorySelected = category == "Consumable";
            IsSkipTagCategorySelected = category == "SkipTag";
            IsBossCategorySelected = category == "Boss";
            IsVoucherCategorySelected = category == "Voucher";
            IsStandardCardCategorySelected = category == "StandardCard";

            // Rebuild grouped items
            RebuildGroupedItems();

            OnPropertyChanged(nameof(CurrentCategoryDisplay));

            // Ensure collections are populated when switching categories
            if (FilteredJokers.Count == 0)
            {
                ApplyFilter();
            }
        }

        private void RebuildGroupedItems()
        {
            GroupedItems.Clear();

            switch (SelectedMainCategory)
            {
                case "Favorites":
                    // Section 1: Favorite Items (user's frequently used items)
                    var favoriteItems = AllJokers.Where(j => j.IsFavorite == true).ToList();
                    AddGroup("Favorite Items", favoriteItems);

                    // Section 2: Wildcards (ALL wildcards in one place)
                    AddGroup("Wildcards", FilteredWildcards);
                    break;

                case "Joker":
                    // Add groups: Legendary, Rare, Uncommon, Common
                    AddGroup("Legendary Jokers", FilteredJokers.Where(j => j.Type == "SoulJoker"));
                    AddGroup(
                        "Rare Jokers",
                        FilteredJokers.Where(j => j.Type == "Joker" && j.Category == "Rare")
                    );
                    AddGroup(
                        "Uncommon Jokers",
                        FilteredJokers.Where(j => j.Type == "Joker" && j.Category == "Uncommon")
                    );
                    AddGroup(
                        "Common Jokers",
                        FilteredJokers.Where(j => j.Type == "Joker" && j.Category == "Common")
                    );
                    break;

                case "Consumable":
                    AddGroup("Tarot Cards", FilteredTarots);
                    AddGroup("Planet Cards", FilteredPlanets);
                    AddGroup("Spectral Cards", FilteredSpectrals);
                    break;

                case "SkipTag":
                    // For now, just show all tags (ante filtering can be added later)
                    AddGroup("Skip Tags - Any Ante", FilteredTags);
                    break;

                case "Boss":
                    // For now, show all bosses (regular/finisher split can be added later)
                    AddGroup("Boss Blinds", FilteredBosses);
                    break;

                case "Voucher":
                    // Vouchers are already sorted in AllVouchers (and FilteredVouchers) with the correct display order
                    // Base vouchers appear first, then their upgrades directly below in the grid
                    AddGroup("Vouchers", FilteredVouchers);
                    break;

                case "StandardCard":
                    // Standard playing cards organized by suit and enhancement
                    AddGroup("Hearts", FilteredStandardCards.Where(c => c.Category == "Hearts"));
                    AddGroup("Spades", FilteredStandardCards.Where(c => c.Category == "Spades"));
                    AddGroup(
                        "Diamonds",
                        FilteredStandardCards.Where(c => c.Category == "Diamonds")
                    );
                    AddGroup("Clubs", FilteredStandardCards.Where(c => c.Category == "Clubs"));
                    AddGroup("Mult Cards", FilteredStandardCards.Where(c => c.Category == "Mult"));
                    AddGroup(
                        "Bonus Cards",
                        FilteredStandardCards.Where(c => c.Category == "Bonus")
                    );
                    AddGroup(
                        "Glass Cards",
                        FilteredStandardCards.Where(c => c.Category == "Glass")
                    );
                    AddGroup("Gold Cards", FilteredStandardCards.Where(c => c.Category == "Gold"));
                    AddGroup(
                        "Steel Cards",
                        FilteredStandardCards.Where(c => c.Category == "Steel")
                    );
                    AddGroup("Stone Card", FilteredStandardCards.Where(c => c.Category == "Stone"));
                    break;
            }
        }

        private void AddGroup(string groupName, IEnumerable<FilterItem> items)
        {
            var group = new ItemGroup
            {
                GroupName = groupName,
                Items = new ObservableCollection<FilterItem>(items.ToList()),
            };
            GroupedItems.Add(group);
        }

        private void RefreshFilteredItems()
        {
            // CROSS-CATEGORY SEARCH - If searching, search ALL items regardless of category
            if (!string.IsNullOrEmpty(SearchFilter))
            {
                // Clear and rebuild GroupedItems with search results
                GroupedItems.Clear();

                // Search across ALL categories when filter is active
                var searchResults = new List<FilterItem>();

                var allCollections = new[]
                {
                    AllJokers,
                    AllVouchers,
                    AllTarots,
                    AllPlanets,
                    AllSpectrals,
                    AllTags,
                    AllBosses,
                };

                foreach (var collection in allCollections)
                {
                    foreach (var item in collection)
                    {
                        if (
                            item.Name?.Contains(SearchFilter, StringComparison.OrdinalIgnoreCase)
                                == true
                            || item.DisplayName?.Contains(
                                SearchFilter,
                                StringComparison.OrdinalIgnoreCase
                            ) == true
                        )
                        {
                            searchResults.Add(item);
                        }
                    }
                }

                // Display all search results in a single "SEARCH RESULTS" group
                AddGroup("SEARCH RESULTS", searchResults);
                return;
            }

            // CATEGORY-SPECIFIC DISPLAY when no search filter
            var sourceCollection = SelectedCategory switch
            {
                "Favorites" => FilteredJokers.Where(j => j.IsFavorite == true),
                "Legendary" => FilteredJokers.Where(j => j.Type == "SoulJoker"),
                "Rare" => FilteredJokers.Where(j => j.Type == "Joker" && j.Category == "Rare"),
                "Uncommon" => FilteredJokers.Where(j =>
                    j.Type == "Joker" && j.Category == "Uncommon"
                ),
                "Common" => FilteredJokers.Where(j => j.Type == "Joker" && j.Category == "Common"),
                "Voucher" => FilteredVouchers,
                "Tarot" => FilteredTarots,
                "Planet" => FilteredPlanets,
                "Spectral" => FilteredSpectrals,
                "Tag" => FilteredTags,
                "Boss" => FilteredBosses,
                _ => FilteredJokers.Where(j => j.Type == "Joker"),
            };

            DebugLogger.Log(
                "VisualBuilderTab",
                $"RefreshFilteredItems: SelectedCategory={SelectedCategory}, sourceCollection count={sourceCollection.Count()}"
            );
            foreach (var item in sourceCollection.Take(3))
            {
                DebugLogger.Log(
                    "VisualBuilderTab",
                    $"  - {item.Name}: Type={item.Type}, Category={item.Category}"
                );
            }

            FilteredItems.Clear();
            foreach (var item in sourceCollection)
            {
                FilteredItems.Add(item);
            }
            DebugLogger.Log(
                "VisualBuilderTab",
                $"FilteredItems populated: {FilteredItems.Count} items"
            );

            // Also rebuild grouped items after filtering
            RebuildGroupedItems();
        }

        #region Commands

        public ICommand AddToMustCommand { get; }
        public ICommand AddToShouldCommand { get; }

        public ICommand RemoveFromMustCommand { get; }
        public ICommand RemoveFromShouldCommand { get; }

        public ICommand AddToOrTrayCommand { get; }
        public ICommand AddToAndTrayCommand { get; }
        public ICommand RemoveFromOrTrayCommand { get; }
        public ICommand RemoveFromAndTrayCommand { get; }

        // Commit clause commands
        public ICommand CommitOrClauseCommand { get; }
        public ICommand CommitAndClauseCommand { get; }

        // Unified Operator commands
        public ICommand ToggleOperatorCommand { get; }
        public ICommand ClearTrayCommand { get; }

        // Edition commands (SetEditionCommand is auto-generated from the [RelayCommand] attribute)

        #endregion

        #region Command Implementations

        private void AddToMust(FilterItem? item)
        {
            if (item == null)
            {
                Helpers.DebugLogger.Log("AddToMust", "Item is null, returning");
                return;
            }

            Helpers.DebugLogger.Log(
                "AddToMust",
                $"Adding item: Name={item.Name}, Type={item.Type}, Category={item.Category}, ItemImage={item.ItemImage != null}, DisplayName={item.DisplayName}, ItemType={item.GetType().Name}"
            );

            // Set IsInvertedFilter based on IsDebuffed state
            // Debuffed items (red X) in MUST zone = "Must NOT have this"
            item.IsInvertedFilter = item.IsDebuffed;

            DebugLogger.Log(
                "AddToMust",
                $"IsDebuffed={item.IsDebuffed}, IsInvertedFilter={item.IsInvertedFilter}"
            );

            // ALLOW DUPLICATES: Same item can be added multiple times with different configs
            SelectedMust.Add(item);

            Helpers.DebugLogger.Log(
                "AddToMust",
                $"SelectedMust count after add: {SelectedMust.Count}"
            );

            // Log all items in collection for debugging
            for (int i = 0; i < SelectedMust.Count; i++)
            {
                var existingItem = SelectedMust[i];
                Helpers.DebugLogger.Log(
                    "AddToMust",
                    $"  [{i}] {existingItem.Name} (Type={existingItem.GetType().Name}, Image={existingItem.ItemImage != null}, Display={existingItem.DisplayName})"
                );
            }

            // Force UI refresh
            OnPropertyChanged(nameof(SelectedMust));

            // Sync with parent ViewModel if available
            if (_parentViewModel != null)
            {
                // Special handling for operators
                if (item is FilterOperatorItem operatorItem)
                {
                    // If this is the unified operator, use it as-is with its children
                    if (operatorItem == UnifiedOperator)
                    {
                        DebugLogger.Log(
                            "VisualBuilderTab",
                            $"Adding unified operator ({operatorItem.OperatorType}) with {operatorItem.Children.Count} children"
                        );
                    }

                    SyncOperatorToParent(operatorItem, "Must");
                }
                else
                {
                    var itemKey = _parentViewModel.GenerateNextItemKey();
                    var itemConfig = new ItemConfig
                    {
                        ItemKey = itemKey,
                        ItemType = item.Type,
                        ItemName = item.Name,
                    };

                    // Phase 3: Apply currently selected edition/stickers/seal
                    ApplyEditionStickersSeal(itemConfig, item);

                    _parentViewModel.ItemConfigs[itemKey] = itemConfig;
                    _parentViewModel.SelectedMust.Add(itemKey);
                }
            }

            DebugLogger.Log("VisualBuilderTab", $"Added {item.Name} to MUST");

            // Trigger auto-sync to JSON Editor
            NotifyJsonEditorOfChanges();
        }

        private void AddToShould(FilterItem? item)
        {
            if (item == null)
            {
                Helpers.DebugLogger.Log("AddToShould", "Item is null, returning");
                return;
            }

            Helpers.DebugLogger.Log(
                "AddToShould",
                $"Adding item: Name={item.Name}, Type={item.Type}, Category={item.Category}, ItemImage={item.ItemImage != null}, DisplayName={item.DisplayName}"
            );

            // ALLOW DUPLICATES: Same item can be added multiple times with different configs
            SelectedShould.Add(item);

            Helpers.DebugLogger.Log(
                "AddToShould",
                $"SelectedShould count after add: {SelectedShould.Count}"
            );

            // Log all items in collection for debugging
            for (int i = 0; i < SelectedShould.Count; i++)
            {
                var existingItem = SelectedShould[i];
                Helpers.DebugLogger.Log(
                    "AddToShould",
                    $"  [{i}] {existingItem.Name} (Image={existingItem.ItemImage != null}, Display={existingItem.DisplayName})"
                );
            }

            // Force UI refresh
            OnPropertyChanged(nameof(SelectedShould));

            // Sync with parent ViewModel if available
            if (_parentViewModel != null)
            {
                // Special handling for operators
                if (item is FilterOperatorItem operatorItem)
                {
                    // If this is the unified operator, use it as-is with its children
                    if (operatorItem == UnifiedOperator)
                    {
                        DebugLogger.Log(
                            "VisualBuilderTab",
                            $"Adding unified operator ({operatorItem.OperatorType}) with {operatorItem.Children.Count} children"
                        );
                    }

                    SyncOperatorToParent(operatorItem, "Should");
                }
                else
                {
                    var itemKey = _parentViewModel.GenerateNextItemKey();
                    var itemConfig = new ItemConfig
                    {
                        ItemKey = itemKey,
                        ItemType = item.Type,
                        ItemName = item.Name,
                    };

                    // Phase 3: Apply currently selected edition/stickers/seal
                    ApplyEditionStickersSeal(itemConfig, item);

                    _parentViewModel.ItemConfigs[itemKey] = itemConfig;
                    _parentViewModel.SelectedShould.Add(itemKey);
                }
            }

            DebugLogger.Log("VisualBuilderTab", $"Added {item.Name} to SHOULD");

            // Trigger auto-sync to JSON Editor
            NotifyJsonEditorOfChanges();
        }

        private void RemoveFromMust(FilterItem? item)
        {
            if (item == null)
                return;

            SelectedMust.Remove(item);

            // Sync with parent ViewModel - remove from parent collections
            if (_parentViewModel != null)
            {
                RemoveItemFromParent(item, _parentViewModel.SelectedMust);
            }

            DebugLogger.Log("VisualBuilderTab", $"Removed {item.Name} from MUST");

            // Trigger auto-sync to JSON Editor
            NotifyJsonEditorOfChanges();
        }

        private void RemoveFromShould(FilterItem? item)
        {
            if (item == null)
                return;

            SelectedShould.Remove(item);

            // Sync with parent ViewModel - remove from parent collections
            if (_parentViewModel != null)
            {
                RemoveItemFromParent(item, _parentViewModel.SelectedShould);
            }

            DebugLogger.Log("VisualBuilderTab", $"Removed {item.Name} from SHOULD");

            // Trigger auto-sync to JSON Editor
            NotifyJsonEditorOfChanges();
        }

        private void AddToOrTray(FilterItem? item)
        {
            if (item == null)
                return;

            // Enter clause editing mode on first item
            if (OrTrayItems.Count == 0)
            {
                CurrentEditingState = EditingState.ClauseEdit;
                EditingClauseType = "Or";
                DebugLogger.Log("VisualBuilderTab", "Entered OR clause editing mode");
            }

            // Add to OR tray
            OrTrayItems.Add(item);

            // Create ItemConfig and apply current edition/sticker/seal settings
            if (_parentViewModel != null)
            {
                var itemKey = _parentViewModel.GenerateNextItemKey();
                var itemConfig = new ItemConfig
                {
                    ItemKey = itemKey,
                    ItemType = item.Type,
                    ItemName = item.Name,
                };

                ApplyEditionStickersSeal(itemConfig, item);
                _parentViewModel.ItemConfigs[itemKey] = itemConfig;
            }

            DebugLogger.Log("VisualBuilderTab", $"Added {item.Name} to OR tray");
            OnPropertyChanged(nameof(ShouldHideAndTray));
            OnPropertyChanged(nameof(ShouldHideShouldZone));
        }

        private void AddToAndTray(FilterItem? item)
        {
            if (item == null)
                return;

            // Enter clause editing mode on first item
            if (AndTrayItems.Count == 0)
            {
                CurrentEditingState = EditingState.ClauseEdit;
                EditingClauseType = "And";
                DebugLogger.Log("VisualBuilderTab", "Entered AND clause editing mode");
            }

            // Add to AND tray
            AndTrayItems.Add(item);

            // Create ItemConfig and apply current edition/sticker/seal settings
            if (_parentViewModel != null)
            {
                var itemKey = _parentViewModel.GenerateNextItemKey();
                var itemConfig = new ItemConfig
                {
                    ItemKey = itemKey,
                    ItemType = item.Type,
                    ItemName = item.Name,
                };

                ApplyEditionStickersSeal(itemConfig, item);
                _parentViewModel.ItemConfigs[itemKey] = itemConfig;
            }

            DebugLogger.Log("VisualBuilderTab", $"Added {item.Name} to AND tray");
            OnPropertyChanged(nameof(ShouldHideOrTray));
            OnPropertyChanged(nameof(ShouldHideShouldZone));
        }

        private void RemoveFromOrTray(FilterItem? item)
        {
            if (item == null)
                return;

            OrTrayItems.Remove(item);

            // Also remove from SelectedShould
            RemoveFromShould(item);

            DebugLogger.Log("VisualBuilderTab", $"Removed {item.Name} from OR tray");
        }

        private void RemoveFromAndTray(FilterItem? item)
        {
            if (item == null)
                return;

            AndTrayItems.Remove(item);

            // Also remove from SelectedShould
            RemoveFromShould(item);

            DebugLogger.Log("VisualBuilderTab", $"Removed {item.Name} from AND tray");
        }

        /// <summary>
        /// Commits OR clause to SHOULD list as a grouped clause with Children
        /// </summary>
        private void CommitOrClause()
        {
            if (OrTrayItems.Count == 0)
            {
                DebugLogger.Log("VisualBuilderTab", "Cannot commit empty OR clause");
                return;
            }

            // Create grouped ItemConfig with OperatorType="Or" and Children
            var groupedConfig = new ItemConfig
            {
                ItemKey = $"or_clause_{Guid.NewGuid():N}",
                ItemType = "Clause",
                ItemName = $"OR ({OrTrayItems.Count} items)",
                OperatorType = "Or",
                Mode = "Max",
                Children = new List<ItemConfig>(),
            };

            // Add each item in OR tray as a child
            if (_parentViewModel != null)
            {
                foreach (var item in OrTrayItems.ToList())
                {
                    // Find the ItemConfig for this item
                    var existingConfig = _parentViewModel.ItemConfigs.FirstOrDefault(kvp =>
                        kvp.Value.ItemName == item.Name && kvp.Value.ItemType == item.Type
                    );

                    if (existingConfig.Value != null)
                    {
                        groupedConfig.Children.Add(existingConfig.Value);
                    }
                }
            }

            // Create FilterOperatorItem for UI display
            var operatorItem = new FilterOperatorItem("OR")
            {
                DisplayName = $"OR ({OrTrayItems.Count} items)",
            };

            // Add tray items as children to the operator item
            foreach (var item in OrTrayItems.ToList())
            {
                operatorItem.Children.Add(item);
            }

            // Add to local SelectedShould for UI binding
            SelectedShould.Add(operatorItem);

            // Add grouped config to parent for persistence
            if (_parentViewModel != null)
            {
                _parentViewModel.ItemConfigs[groupedConfig.ItemKey] = groupedConfig;
                _parentViewModel.SelectedShould.Add(groupedConfig.ItemKey);
            }

            // Clear OR tray
            OrTrayItems.Clear();

            // Exit clause editing mode
            CurrentEditingState = EditingState.Default;
            EditingClauseType = "";

            DebugLogger.Log(
                "VisualBuilderTab",
                $"Committed OR clause with {groupedConfig.Children.Count} children to SHOULD"
            );

            NotifyJsonEditorOfChanges();
        }

        /// <summary>
        /// Commits AND clause to SHOULD list as a grouped clause with Children
        /// </summary>
        private void CommitAndClause()
        {
            if (AndTrayItems.Count == 0)
            {
                DebugLogger.Log("VisualBuilderTab", "Cannot commit empty AND clause");
                return;
            }

            // Create grouped ItemConfig with OperatorType="And" and Children
            var groupedConfig = new ItemConfig
            {
                ItemKey = $"and_clause_{Guid.NewGuid():N}",
                ItemType = "Clause",
                ItemName = $"AND ({AndTrayItems.Count} items)",
                OperatorType = "And",
                Children = new List<ItemConfig>(),
            };

            // Add each item in AND tray as a child
            if (_parentViewModel != null)
            {
                foreach (var item in AndTrayItems.ToList())
                {
                    // Find the ItemConfig for this item
                    var existingConfig = _parentViewModel.ItemConfigs.FirstOrDefault(kvp =>
                        kvp.Value.ItemName == item.Name && kvp.Value.ItemType == item.Type
                    );

                    if (existingConfig.Value != null)
                    {
                        groupedConfig.Children.Add(existingConfig.Value);
                    }
                }
            }

            // Create FilterOperatorItem for UI display
            var operatorItem = new FilterOperatorItem("AND")
            {
                DisplayName = $"AND ({AndTrayItems.Count} items)",
            };

            // Add tray items as children to the operator item
            foreach (var item in AndTrayItems.ToList())
            {
                operatorItem.Children.Add(item);
            }

            // Add to local SelectedShould for UI binding
            SelectedShould.Add(operatorItem);

            // Add grouped config to parent for persistence
            if (_parentViewModel != null)
            {
                _parentViewModel.ItemConfigs[groupedConfig.ItemKey] = groupedConfig;
                _parentViewModel.SelectedShould.Add(groupedConfig.ItemKey);
            }

            // Clear AND tray
            AndTrayItems.Clear();

            // Exit clause editing mode
            CurrentEditingState = EditingState.Default;
            EditingClauseType = "";

            DebugLogger.Log(
                "VisualBuilderTab",
                $"Committed AND clause with {groupedConfig.Children.Count} children to SHOULD"
            );

            NotifyJsonEditorOfChanges();
        }

        /// <summary>
        /// Toggles the unified operator between OR and AND modes
        /// </summary>
        private void ToggleOperator()
        {
            // Cycle through three operator types: OR → AND → BannedItems → OR
            UnifiedOperator.OperatorType = UnifiedOperator.OperatorType switch
            {
                "OR" => "AND",
                "AND" => "BannedItems",
                "BannedItems" => "OR",
                _ => "OR" // Default fallback
            };
            DebugLogger.Log(
                "VisualBuilderTab",
                $"Toggled operator to {UnifiedOperator.OperatorType} mode"
            );
        }

        /// <summary>
        /// Clears all items from the unified tray
        /// </summary>
        private void ClearTray()
        {
            UnifiedOperator.Children.Clear();
            DebugLogger.Log("VisualBuilderTab", "Cleared unified tray");
        }

        /// <summary>
        /// Removes an item from the parent's collections and ItemConfigs
        /// </summary>
        private void RemoveItemFromParent(
            FilterItem item,
            ObservableCollection<string> parentCollection
        )
        {
            if (_parentViewModel == null)
                return;

            // Find the item key in parent's ItemConfigs
            var itemKey = _parentViewModel
                .ItemConfigs.FirstOrDefault(kvp =>
                    kvp.Value.ItemName == item.Name && kvp.Value.ItemType == item.Type
                )
                .Key;

            if (!string.IsNullOrEmpty(itemKey))
            {
                // Remove from parent's collection
                parentCollection.Remove(itemKey);

                // Remove from parent's ItemConfigs
                _parentViewModel.ItemConfigs.Remove(itemKey);

                DebugLogger.Log("VisualBuilderTab", $"Removed {item.Name} from parent collection");
            }
        }

        /// <summary>
        /// Syncs a FilterOperatorItem to the parent's collections as a single operator with nested children.
        /// Creates a single ItemConfig with OperatorType and Children properties.
        /// </summary>
        private void SyncOperatorToParent(FilterOperatorItem operatorItem, string targetZone)
        {
            if (_parentViewModel == null)
                return;

            DebugLogger.Log(
                "VisualBuilderTab",
                $"Syncing {operatorItem.OperatorType} operator with {operatorItem.Children.Count} children to {targetZone}"
            );

            // Create a single ItemConfig representing the operator with its children
            var itemKey = _parentViewModel.GenerateNextItemKey();
            var operatorConfig = new ItemConfig
            {
                ItemKey = itemKey,
                ItemType = "Operator",
                ItemName = operatorItem.OperatorType,
                OperatorType = operatorItem.OperatorType,
                Mode = operatorItem.OperatorType == "OR" ? "Max" : null,
                Children = new List<ItemConfig>(),
            };

            // Convert each child FilterItem to an ItemConfig
            foreach (var child in operatorItem.Children)
            {
                var childConfig = new ItemConfig
                {
                    ItemType = child.Type,
                    ItemName = child.Name,
                    // Preserve any additional properties if needed
                };
                operatorConfig.Children.Add(childConfig);
            }

            // Store the operator config
            _parentViewModel.ItemConfigs[itemKey] = operatorConfig;

            // Add to the zone where the user dropped it
            var targetCollection = targetZone switch
            {
                "Must" => _parentViewModel.SelectedMust,
                "Should" => _parentViewModel.SelectedShould,
                // MustNot removed - use IsInvertedFilter flag instead
                _ => null,
            };

            if (targetCollection != null)
            {
                targetCollection.Add(itemKey);
            }
        }

        /// <summary>
        /// Notifies the JSON Editor tab to auto-update when Visual Builder changes
        /// </summary>
        private void NotifyJsonEditorOfChanges()
        {
            if (_parentViewModel?.JsonEditorTab is JsonEditorTabViewModel jsonEditorVm)
            {
                // Trigger the JSON generation automatically
                jsonEditorVm.AutoGenerateFromVisual();
            }
        }

        #endregion

        #region Helper Methods

        private async Task LoadGameDataAsync()
        {
            try
            {
                await Task.Run(() => LoadGameData());

                // Apply filter and update loading state on UI thread
                await Avalonia.Threading.Dispatcher.UIThread.InvokeAsync(() =>
                {
                    ApplyFilter();
                    IsLoading = false;
                });
            }
            catch (Exception ex)
            {
                // CRITICAL-002 FIX: Handle exceptions gracefully instead of crashing app
                DebugLogger.LogError(
                    "VisualBuilderTab",
                    $"Failed to load game data: {ex.Message}\n{ex.StackTrace}"
                );

                // Even on error, clear loading state and show empty state
                await Avalonia.Threading.Dispatcher.UIThread.InvokeAsync(() =>
                {
                    IsLoading = false;
                    // Set empty collections so UI doesn't crash
                    GroupedItems.Clear();
                });

                // Don't rethrow - gracefully degrade to empty state
                // User can still use other features of the modal
            }
        }

        private void LoadGameData()
        {
            // Ensure UI thread safety for collection initialization
            if (!Avalonia.Threading.Dispatcher.UIThread.CheckAccess())
            {
                Avalonia.Threading.Dispatcher.UIThread.Post(LoadGameData);
                return;
            }

            // Load from BalatroData with sprites
            try
            {
                var spriteService = SpriteService.Instance;

                // Load ALL WILDCARDS FIRST - they will ONLY appear in Specials category
                var allWildcards = new[]
                {
                    ("Wildcard_Joker", "Any Joker", "Joker"),
                    ("Wildcard_JokerCommon", "Any Common Joker", "Joker"),
                    ("Wildcard_JokerUncommon", "Any Uncommon Joker", "Joker"),
                    ("Wildcard_JokerRare", "Any Rare Joker", "Joker"),
                    ("Wildcard_JokerLegendary", "Any Legendary Joker", "SoulJoker"),
                    ("Wildcard_Tarot", "Any Tarot", "Tarot"),
                    ("Wildcard_Planet", "Any Planet", "Planet"),
                    ("Wildcard_Spectral", "Any Spectral", "Spectral"),
                };
                foreach (var (name, displayName, type) in allWildcards)
                {
                    AllWildcards.Add(
                        new FilterItem
                        {
                            Name = name,
                            Type = type,
                            Category = type switch // Fixed for CategoryGroupedLayoutBehavior
                            {
                                "Joker" or "SoulJoker" => "Jokers",
                                "SmallBlindTag" or "BigBlindTag" => "Tags",
                                "Voucher" => "Vouchers",
                                "Boss" => "Bosses",
                                _ => "Consumables", // Tarot, Planet, Spectral
                            },
                            DisplayName = displayName,
                            ItemImage = type switch
                            {
                                "Joker" or "SoulJoker" => spriteService.GetJokerImage(name),
                                "Tarot" => spriteService.GetTarotImage(name),
                                "Planet" => spriteService.GetPlanetCardImage(name),
                                "Spectral" => spriteService.GetSpectralImage(name),
                                _ => null,
                            },
                        }
                    );
                }

                // Load Soul Jokers SECOND (after wildcards)
                var legendaryJokers = new[] { "Triboulet", "Yorick", "Chicot", "Perkeo", "Canio" };
                foreach (var legendaryName in legendaryJokers)
                {
                    var item = new FilterItem
                    {
                        Name = legendaryName,
                        Type = "SoulJoker",
                        Category = "Jokers", // Fixed for CategoryGroupedLayoutBehavior
                        DisplayName = BalatroData.GetDisplayNameFromSprite(legendaryName),
                        ItemImage = spriteService.GetJokerImage(legendaryName),
                    };
                    AllJokers.Add(item);
                    DebugLogger.Log(
                        "VisualBuilderTab",
                        $"Loaded legendary {legendaryName}: Image={item.ItemImage != null}, ItemKey={item.ItemKey}"
                    );
                }

                // Load Regular Jokers from BalatroData
                if (BalatroData.Jokers?.Keys != null)
                {
                    foreach (var jokerName in BalatroData.Jokers.Keys)
                    {
                        // Skip if already added from legendaries
                        if (AllJokers.Any(j => j.Name == jokerName))
                            continue;

                        // Determine actual rarity from BalatroData.JokersByRarity
                        string rarity = "Common"; // Default
                        foreach (var rarityKvp in BalatroData.JokersByRarity)
                        {
                            if (rarityKvp.Value.Contains(jokerName.ToLower()))
                            {
                                rarity = rarityKvp.Key;
                                break;
                            }
                        }

                        var item = new FilterItem
                        {
                            Name = jokerName,
                            Type = "Joker",
                            Category = rarity, // Use actual rarity so shelf filtering works (Common/Uncommon/Rare)
                            DisplayName = BalatroData.GetDisplayNameFromSprite(jokerName),
                            ItemImage = spriteService.GetJokerImage(jokerName),
                        };
                        DebugLogger.Log(
                            "VisualBuilderTab",
                            $"Created regular joker: {jokerName} - Type={item.Type}, Category={item.Category}, ItemImage={item.ItemImage != null}"
                        );
                        AllJokers.Add(item);
                    }
                }

                // Load Tags from BalatroData
                if (BalatroData.Tags?.Keys != null)
                {
                    foreach (var tagName in BalatroData.Tags.Keys)
                    {
                        var item = new FilterItem
                        {
                            Name = tagName,
                            Type = "SmallBlindTag",
                            Category = "Tags", // Added for CategoryGroupedLayoutBehavior
                            DisplayName = BalatroData.GetDisplayNameFromSprite(tagName),
                            ItemImage = spriteService.GetTagImage(tagName),
                        };
                        AllTags.Add(item);
                    }
                }

                // Load Vouchers from BalatroData
                if (BalatroData.Vouchers?.Keys != null)
                {
                    // Custom display order: base vouchers in rows 1,3,5,7 and upgrades in rows 2,4,6,8
                    // Assuming 5 columns per row for grid layout
                    var voucherDisplayOrder = new List<string>
                    {
                        // Row 1: Base vouchers
                        "Overstock", "ClearanceSale", "Hone", "RerollSurplus", "CrystalBall",
                        // Row 2: Upgrades (directly below base versions)
                        "OverstockPlus", "Liquidation", "GlowUp", "RerollGlut", "OmenGlobe",
                        // Row 3: Base vouchers
                        "Telescope", "Grabber", "Wasteful", "TarotMerchant", "PlanetMerchant",
                        // Row 4: Upgrades
                        "Observatory", "NachoTong", "Recyclomancy", "TarotTycoon", "PlanetTycoon",
                        // Row 5: Base vouchers
                        "SeedMoney", "Blank", "MagicTrick", "Hieroglyph", "DirectorsCut",
                        // Row 6: Upgrades
                        "MoneyTree", "Antimatter", "Illusion", "Petroglyph", "Retcon",
                        // Row 7-8: Final pair
                        "PaintBrush", "Palette",
                    };

                    var tempVouchers = new List<FilterItem>();
                    foreach (var voucherName in BalatroData.Vouchers.Keys)
                    {
                        var item = new FilterItem
                        {
                            Name = voucherName,
                            Type = "Voucher",
                            Category = "Vouchers", // Added for CategoryGroupedLayoutBehavior
                            DisplayName = BalatroData.GetDisplayNameFromSprite(voucherName),
                            ItemImage = spriteService.GetVoucherImage(voucherName),
                        };
                        tempVouchers.Add(item);
                    }

                    // Sort by custom display order
                    DebugLogger.Log("VoucherOrdering", $"Sorting {tempVouchers.Count} vouchers...");
                    var sortedVouchers = tempVouchers
                        .OrderBy(v =>
                        {
                            var index = voucherDisplayOrder.IndexOf(v.Name);
                            DebugLogger.Log("VoucherOrdering", $"  {v.Name} -> index {index}");
                            return index == -1 ? int.MaxValue : index;
                        })
                        .ToList();

                    DebugLogger.Log("VoucherOrdering", "Final sorted order:");
                    for (int i = 0; i < sortedVouchers.Count; i++)
                    {
                        DebugLogger.Log("VoucherOrdering", $"  [{i}] {sortedVouchers[i].Name}");
                    }

                    foreach (var voucher in sortedVouchers)
                    {
                        AllVouchers.Add(voucher);
                    }
                }

                // Load Tarots from BalatroData
                if (BalatroData.TarotCards?.Keys != null)
                {
                    foreach (var tarotName in BalatroData.TarotCards.Keys)
                    {
                        // Skip old wildcard entries - we have Wildcard_ prefixed ones now
                        if (tarotName == "any" || tarotName == "*")
                            continue;

                        var item = new FilterItem
                        {
                            Name = tarotName,
                            Type = "Tarot",
                            DisplayName = BalatroData.GetDisplayNameFromSprite(tarotName),
                            ItemImage = spriteService.GetTarotImage(tarotName),
                        };
                        AllTarots.Add(item);
                    }
                }

                // Load Planets from BalatroData
                try
                {
                    if (BalatroData.PlanetCards?.Keys != null)
                    {
                        DebugLogger.Log(
                            "VisualBuilderTab",
                            $"Loading {BalatroData.PlanetCards.Keys.Count} planets"
                        );
                        foreach (var planetName in BalatroData.PlanetCards.Keys)
                        {
                            try
                            {
                                // Skip duplicate "Any Planet" wildcards
                                if (
                                    planetName == "any"
                                    || planetName == "*"
                                    || planetName == "anyplanet"
                                )
                                    continue;

                                var item = new FilterItem
                                {
                                    Name = planetName,
                                    Type = "Planet",
                                    DisplayName = BalatroData.GetDisplayNameFromSprite(planetName),
                                    ItemImage = spriteService.GetPlanetCardImage(planetName),
                                };
                                AllPlanets.Add(item);
                            }
                            catch (Exception ex)
                            {
                                DebugLogger.LogError(
                                    "VisualBuilderTab",
                                    $"Error loading planet {planetName}: {ex.Message}"
                                );
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    DebugLogger.LogError(
                        "VisualBuilderTab",
                        $"Error loading planets: {ex.Message}"
                    );
                }

                // Load Spectrals from BalatroData
                if (BalatroData.SpectralCards?.Keys != null)
                {
                    foreach (var spectralName in BalatroData.SpectralCards.Keys)
                    {
                        // Skip old wildcard entries - we have Wildcard_ prefixed ones now
                        if (spectralName == "any" || spectralName == "*")
                            continue;

                        var item = new FilterItem
                        {
                            Name = spectralName,
                            Type = "Spectral",
                            DisplayName = BalatroData.GetDisplayNameFromSprite(spectralName),
                            ItemImage = spriteService.GetSpectralImage(spectralName),
                        };
                        AllSpectrals.Add(item);
                    }
                }

                // Load Boss Blinds from BalatroData
                try
                {
                    if (BalatroData.BossBlinds?.Keys != null)
                    {
                        foreach (var bossName in BalatroData.BossBlinds.Keys)
                        {
                            try
                            {
                                var item = new FilterItem
                                {
                                    Name = bossName,
                                    Type = "Boss",
                                    Category = "Bosses", // Added for CategoryGroupedLayoutBehavior
                                    DisplayName = BalatroData.GetDisplayNameFromSprite(bossName),
                                    ItemImage = spriteService.GetBossImage(bossName),
                                };
                                AllBosses.Add(item);
                            }
                            catch (Exception ex)
                            {
                                DebugLogger.LogError(
                                    "VisualBuilderTab",
                                    $"Error loading boss {bossName}: {ex.Message}"
                                );
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    DebugLogger.LogError("VisualBuilderTab", $"Error loading bosses: {ex.Message}");
                }

                // Load Standard Playing Cards (52 base cards + enhanced variants)
                try
                {
                    var suits = new[] { "Hearts", "Spades", "Diamonds", "Clubs" };
                    var ranks = new[]
                    {
                        "2",
                        "3",
                        "4",
                        "5",
                        "6",
                        "7",
                        "8",
                        "9",
                        "10",
                        "Jack",
                        "Queen",
                        "King",
                        "Ace",
                    };

                    // Generate base 52 cards (Type A - Normal cards with no enhancement)
                    foreach (var suit in suits)
                    {
                        foreach (var rank in ranks)
                        {
                            var displayName =
                                rank == "Ace" ? $"Ace of {suit}" : $"{rank} of {suit}";
                            var item = new FilterItem
                            {
                                Name = $"{rank}_{suit}",
                                Type = "StandardCard",
                                Category = suit,
                                DisplayName = displayName,
                                Rank = rank,
                                Suit = suit,
                                Enhancement = null,
                                ItemImage = spriteService.GetPlayingCardImage(suit, rank),
                            };
                            AllStandardCards.Add(item);
                        }
                    }

                    // Generate enhanced variants (Type B1 and B2)
                    var enhancements = new[] { "Mult", "Bonus", "Glass", "Gold", "Steel" };
                    foreach (var enhancement in enhancements)
                    {
                        foreach (var rank in ranks)
                        {
                            var suit = "Hearts"; // Use Hearts as default suit for enhanced cards
                            var displayName = $"{enhancement} {rank}";
                            if (rank == "Ace")
                                displayName = $"{enhancement} Ace";

                            var item = new FilterItem
                            {
                                Name = $"{enhancement}_{rank}_{suit}",
                                Type = "StandardCard",
                                Category = enhancement,
                                DisplayName = displayName,
                                Rank = rank,
                                Suit = suit,
                                Enhancement = enhancement,
                                ItemImage = spriteService.GetPlayingCardImage(
                                    suit,
                                    rank,
                                    enhancement
                                ),
                            };
                            AllStandardCards.Add(item);
                        }
                    }

                    // Add Stone card (special case with no rank/suit)
                    var stoneCard = new FilterItem
                    {
                        Name = "Stone",
                        Type = "StandardCard",
                        Category = "Stone",
                        DisplayName = "Stone Card",
                        Rank = null,
                        Suit = null,
                        Enhancement = "Stone",
                        ItemImage = spriteService.GetPlayingCardImage("Hearts", "Ace", "Stone"),
                    };
                    AllStandardCards.Add(stoneCard);

                    DebugLogger.Log(
                        "VisualBuilderTab",
                        $"Loaded {AllStandardCards.Count} standard playing cards"
                    );
                }
                catch (Exception ex)
                {
                    DebugLogger.LogError(
                        "VisualBuilderTab",
                        $"Error loading standard cards: {ex.Message}"
                    );
                }

                // Mark favorite items AFTER all items are loaded with their proper categories
                // This ensures favorited items appear in BOTH Favorites AND their original category
                var favoritesService = ServiceHelper.GetService<FavoritesService>();
                var favoriteNames = favoritesService?.GetFavoriteItems() ?? new List<string>();

                foreach (var favoriteName in favoriteNames)
                {
                    // Find the joker in AllJokers and mark it as favorite
                    var joker = AllJokers.FirstOrDefault(j =>
                        j.Name.Equals(favoriteName, StringComparison.OrdinalIgnoreCase)
                    );
                    if (joker != null)
                    {
                        joker.IsFavorite = true;
                        DebugLogger.Log(
                            "VisualBuilderTab",
                            $"Marked {favoriteName} as favorite (Category={joker.Category})"
                        );
                    }
                }

                DebugLogger.Log(
                    "VisualBuilderTab",
                    $"Loaded {AllJokers.Count} jokers, {AllTags.Count} tags, {AllVouchers.Count} vouchers, {AllTarots.Count} tarots, {AllPlanets.Count} planets, {AllSpectrals.Count} spectrals, {AllBosses.Count} bosses, {AllStandardCards.Count} standard cards with images"
                );
                DebugLogger.Log(
                    "VisualBuilderTab",
                    $"Joker types: {string.Join(", ", AllJokers.Take(5).Select(j => $"{j.Name}:{j.Type}:{j.Category}"))}"
                );
                DebugLogger.Log(
                    "VisualBuilderTab",
                    $"FilteredJokers after ApplyFilter: {FilteredJokers.Count}"
                );
            }
            catch (Exception ex)
            {
                DebugLogger.LogError("VisualBuilderTab", $"Error loading data: {ex.Message}");

                // Fallback to sample data
                var spriteService = SpriteService.Instance;
                AllJokers.Add(
                    new FilterItem
                    {
                        Name = "Joker",
                        Type = "Joker",
                        ItemImage = spriteService.GetJokerImage("Joker"),
                    }
                );
                AllTags.Add(
                    new FilterItem
                    {
                        Name = "Negative Tag",
                        Type = "SmallBlindTag",
                        ItemImage = spriteService.GetTagImage("Negative Tag"),
                    }
                );
                AllVouchers.Add(
                    new FilterItem
                    {
                        Name = "Overstock",
                        Type = "Voucher",
                        ItemImage = spriteService.GetVoucherImage("Overstock"),
                    }
                );
            }

            // Ensure filtered lists and UI are refreshed after data loads
            ApplyFilter();
        }

        private string FormatDisplayName(string name)
        {
            // Convert snake_case to Title Case for display
            if (string.IsNullOrEmpty(name))
                return name;

            // Replace underscores with spaces and capitalize each word
            var words = name.Replace('_', ' ').Split(' ');
            for (int i = 0; i < words.Length; i++)
            {
                if (words[i].Length > 0)
                {
                    words[i] = char.ToUpper(words[i][0]) + words[i].Substring(1).ToLower();
                }
            }
            return string.Join(" ", words);
        }

        // Copied from original FiltersModal
        private List<string> GeneratePlayingCardsList()
        {
            var cards = new List<string>();
            var suits = new[] { "Hearts", "Diamonds", "Clubs", "Spades" };
            var ranks = new[]
            {
                "Ace",
                "2",
                "3",
                "4",
                "5",
                "6",
                "7",
                "8",
                "9",
                "10",
                "Jack",
                "Queen",
                "King",
            };

            foreach (var suit in suits)
            {
                foreach (var rank in ranks)
                {
                    cards.Add($"{rank} of {suit}");
                }
            }

            return cards;
        }

        private void ApplyFilter()
        {
            // Ensure UI thread safety for all collection modifications
            if (!Avalonia.Threading.Dispatcher.UIThread.CheckAccess())
            {
                Avalonia.Threading.Dispatcher.UIThread.Post(ApplyFilter);
                return;
            }

            FilteredJokers.Clear();
            FilteredTags.Clear();
            FilteredVouchers.Clear();
            FilteredTarots.Clear();
            FilteredPlanets.Clear();
            FilteredSpectrals.Clear();
            FilteredBosses.Clear();
            FilteredWildcards.Clear();
            FilteredStandardCards.Clear();

            var filter = SearchFilter.ToLowerInvariant();

            DebugLogger.Log(
                "VisualBuilderTab",
                $"ApplyFilter called - AllJokers count: {AllJokers.Count}, filter: '{filter}'"
            );

            foreach (var joker in AllJokers)
            {
                if (
                    string.IsNullOrEmpty(filter)
                    || joker.Name.ToLowerInvariant().Contains(filter)
                    || joker.DisplayName.ToLowerInvariant().Contains(filter)
                )
                {
                    FilteredJokers.Add(joker);
                }
            }

            DebugLogger.Log(
                "VisualBuilderTab",
                $"FilteredJokers after ApplyFilter: {FilteredJokers.Count}"
            );

            foreach (var tag in AllTags)
            {
                if (
                    string.IsNullOrEmpty(filter)
                    || tag.Name.ToLowerInvariant().Contains(filter)
                    || tag.DisplayName.ToLowerInvariant().Contains(filter)
                )
                {
                    FilteredTags.Add(tag);
                }
            }

            foreach (var voucher in AllVouchers)
            {
                if (
                    string.IsNullOrEmpty(filter)
                    || voucher.Name.ToLowerInvariant().Contains(filter)
                    || voucher.DisplayName.ToLowerInvariant().Contains(filter)
                )
                {
                    FilteredVouchers.Add(voucher);
                }
            }

            foreach (var tarot in AllTarots)
            {
                if (
                    string.IsNullOrEmpty(filter)
                    || tarot.Name.ToLowerInvariant().Contains(filter)
                    || tarot.DisplayName.ToLowerInvariant().Contains(filter)
                )
                {
                    FilteredTarots.Add(tarot);
                }
            }

            foreach (var planet in AllPlanets)
            {
                if (
                    string.IsNullOrEmpty(filter)
                    || planet.Name.ToLowerInvariant().Contains(filter)
                    || planet.DisplayName.ToLowerInvariant().Contains(filter)
                )
                {
                    FilteredPlanets.Add(planet);
                }
            }

            foreach (var spectral in AllSpectrals)
            {
                if (
                    string.IsNullOrEmpty(filter)
                    || spectral.Name.ToLowerInvariant().Contains(filter)
                    || spectral.DisplayName.ToLowerInvariant().Contains(filter)
                )
                {
                    FilteredSpectrals.Add(spectral);
                }
            }

            foreach (var boss in AllBosses)
            {
                if (
                    string.IsNullOrEmpty(filter)
                    || boss.Name.ToLowerInvariant().Contains(filter)
                    || boss.DisplayName.ToLowerInvariant().Contains(filter)
                )
                {
                    FilteredBosses.Add(boss);
                }
            }

            foreach (var wildcard in AllWildcards)
            {
                if (
                    string.IsNullOrEmpty(filter)
                    || wildcard.Name.ToLowerInvariant().Contains(filter)
                    || wildcard.DisplayName.ToLowerInvariant().Contains(filter)
                )
                {
                    FilteredWildcards.Add(wildcard);
                }
            }

            foreach (var card in AllStandardCards)
            {
                if (
                    string.IsNullOrEmpty(filter)
                    || card.Name.ToLowerInvariant().Contains(filter)
                    || card.DisplayName.ToLowerInvariant().Contains(filter)
                )
                {
                    FilteredStandardCards.Add(card);
                }
            }

            // Update unified FilteredItems for current category
            RefreshFilteredItems();
        }

        public void UpdateItemConfig(string itemKey, ItemConfig config)
        {
            ItemConfigs[itemKey] = config;

            // Sync with parent ViewModel if available
            if (_parentViewModel != null && _parentViewModel.ItemConfigs.ContainsKey(itemKey))
            {
                _parentViewModel.ItemConfigs[itemKey] = config;
            }

            DebugLogger.Log("VisualBuilderTab", $"Updated config for item: {itemKey}");
        }

        public void RemoveItem(FilterItem item)
        {
            // Find which zone the item is in and get its index BEFORE removing
            string? sourceZone = null;
            string? itemKeyToRemove = null;
            int itemIndex = -1;

            // Check MUST zone
            itemIndex = SelectedMust.IndexOf(item);
            if (itemIndex >= 0)
            {
                sourceZone = "MUST";
                if (_parentViewModel != null && itemIndex < _parentViewModel.SelectedMust.Count)
                {
                    itemKeyToRemove = _parentViewModel.SelectedMust[itemIndex];
                }
                SelectedMust.RemoveAt(itemIndex);
            }
            // Check SHOULD zone
            else
            {
                itemIndex = SelectedShould.IndexOf(item);
                if (itemIndex >= 0)
                {
                    sourceZone = "SHOULD";
                    if (
                        _parentViewModel != null
                        && itemIndex < _parentViewModel.SelectedShould.Count
                    )
                    {
                        itemKeyToRemove = _parentViewModel.SelectedShould[itemIndex];
                    }
                    SelectedShould.RemoveAt(itemIndex);
                }
            }

            // Remove associated config and sync with parent
            if (!string.IsNullOrEmpty(itemKeyToRemove) && _parentViewModel != null)
            {
                ItemConfigs.Remove(itemKeyToRemove);
                _parentViewModel.ItemConfigs.Remove(itemKeyToRemove);

                // Remove from parent's zone collections (only the specific zone)
                if (sourceZone == "MUST")
                {
                    _parentViewModel.SelectedMust.Remove(itemKeyToRemove);
                }
                else if (sourceZone == "SHOULD")
                {
                    _parentViewModel.SelectedShould.Remove(itemKeyToRemove);
                }
            }

            DebugLogger.Log(
                "VisualBuilderTab",
                $"Removed item: {item.Name} from {sourceZone ?? "UNKNOWN"} zone"
            );
        }

        #endregion

        #region Auto-Save Functionality

        /// <summary>
        /// Handles collection changes in drop zones and triggers debounced auto-save
        /// </summary>
        private void OnZoneCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
        {
            // Trigger debounced auto-save
            TriggerAutoSave();
        }

        /// <summary>
        /// Triggers a debounced auto-save operation.
        /// Cancels any pending save and schedules a new one after the debounce delay.
        /// </summary>
        private void TriggerAutoSave()
        {
            // Cancel any pending auto-save
            _autoSaveCts?.Cancel();
            _autoSaveCts?.Dispose();
            _autoSaveCts = new CancellationTokenSource();

            var token = _autoSaveCts.Token;

            // Schedule auto-save after debounce delay
            Task.Run(async () =>
            {
                try
                {
                    await Task.Delay(AutoSaveDebounceMs, token);

                    // If not cancelled, perform the save
                    if (!token.IsCancellationRequested)
                    {
                        await PerformAutoSave();
                    }
                }
                catch (TaskCanceledException)
                {
                    // Expected when debounce is cancelled
                }
                catch (Exception ex)
                {
                    DebugLogger.LogError("VisualBuilderTab", $"Auto-save error: {ex.Message}");
                }
            });
        }

        /// <summary>
        /// Performs the actual auto-save operation by calling the parent's SaveFilterTab logic
        /// </summary>
        private async Task PerformAutoSave()
        {
            if (_parentViewModel == null)
            {
                DebugLogger.Log("VisualBuilderTab", "Auto-save skipped: no parent ViewModel");
                return;
            }

            try
            {
                // Update UI to show saving status
                await Dispatcher.UIThread.InvokeAsync(() =>
                {
                    IsAutoSaving = true;
                    AutoSaveStatus = "Auto-saving...";
                });

                // Get the filter name from parent
                var filterName = _parentViewModel.FilterName;

                // Skip auto-save if filter name is empty (unsaved filter)
                if (string.IsNullOrWhiteSpace(filterName))
                {
                    await Dispatcher.UIThread.InvokeAsync(() =>
                    {
                        IsAutoSaving = false;
                        AutoSaveStatus = "";
                    });
                    DebugLogger.Log("VisualBuilderTab", "Auto-save skipped: filter name is empty");
                    return;
                }

                // Build config from current state using parent's method
                var config = _parentViewModel.BuildConfigFromCurrentState();

                // Get configuration service
                var configService = ServiceHelper.GetService<IConfigurationService>();
                var filterService = ServiceHelper.GetService<IFilterService>();

                if (configService == null || filterService == null)
                {
                    DebugLogger.LogError(
                        "VisualBuilderTab",
                        "Auto-save failed: services not available"
                    );
                    return;
                }

                // Generate filter file path (normalized with underscores)
                var filePath = filterService.GenerateFilterFileName(filterName);

                // Check if there's an old file with spaces instead of underscores and delete it
                if (filterName.Contains(' '))
                {
                    var oldFilePath = Path.Combine(
                        configService.GetFiltersDirectory(),
                        $"{filterName}.json"
                    );
                    if (File.Exists(oldFilePath) && oldFilePath != filePath)
                    {
                        try
                        {
                            File.Delete(oldFilePath);
                            DebugLogger.Log(
                                "VisualBuilderTab",
                                $"Deleted old filter file with spaces: {oldFilePath}"
                            );
                        }
                        catch (Exception ex)
                        {
                            DebugLogger.LogError(
                                "VisualBuilderTab",
                                $"Failed to delete old filter file: {ex.Message}"
                            );
                        }
                    }
                }

                // Save the filter
                var success = await configService.SaveFilterAsync(filePath, config);

                // Update UI with result
                await Dispatcher.UIThread.InvokeAsync(() =>
                {
                    IsAutoSaving = false;
                    if (success)
                    {
                        AutoSaveStatus = "Auto-saved";
                        DebugLogger.Log("VisualBuilderTab", $"Auto-saved filter: {filterName}");

                        // Clear the status after 2 seconds
                        Task.Delay(2000)
                            .ContinueWith(_ =>
                            {
                                Dispatcher.UIThread.InvokeAsync(() =>
                                {
                                    AutoSaveStatus = "";
                                });
                            });
                    }
                    else
                    {
                        AutoSaveStatus = "Auto-save failed";
                        DebugLogger.LogError("VisualBuilderTab", "Auto-save failed");
                    }
                });
            }
            catch (Exception ex)
            {
                DebugLogger.LogError("VisualBuilderTab", $"Auto-save exception: {ex.Message}");

                await Dispatcher.UIThread.InvokeAsync(() =>
                {
                    IsAutoSaving = false;
                    AutoSaveStatus = "Auto-save error";
                });
            }
        }

        #endregion

        #region Phase 2: State Transition Methods

        /// <summary>
        /// Automatically invoked when CurrentEditingState changes (via [ObservableProperty])
        /// Notifies all computed state properties to update bindings
        /// </summary>
        partial void OnCurrentEditingStateChanged(EditingState value)
        {
            OnPropertyChanged(nameof(IsDefaultState));
            OnPropertyChanged(nameof(IsDragActiveState));
            OnPropertyChanged(nameof(IsClauseEditState));
            OnPropertyChanged(nameof(IsScoreEditState));
            OnPropertyChanged(nameof(ShouldShowItemShelf));
            OnPropertyChanged(nameof(ShouldExpandClauses));
            OnPropertyChanged(nameof(ShouldExpandScoreList));
            OnPropertyChanged(nameof(ShouldCollapseClauses));
            OnPropertyChanged(nameof(ShouldCollapseScoreList));

            DebugLogger.Log("VisualBuilderTab", $"State changed to: {value}");
        }

        /// <summary>
        /// Transitions to Default state - normal view with all sections visible
        /// </summary>
        [RelayCommand]
        public void EnterDefaultState()
        {
            CurrentEditingState = EditingState.Default;
            IsDragging = false;
        }

        /// <summary>
        /// Transitions to DragActive state - user is dragging an item
        /// </summary>
        public void EnterDragActiveState()
        {
            CurrentEditingState = EditingState.DragActive;
            IsDragging = true;
        }

        /// <summary>
        /// Transitions to ClauseEdit state - expand OR/AND clause to full column
        /// </summary>
        [RelayCommand]
        public void EnterClauseEditState()
        {
            CurrentEditingState = EditingState.ClauseEdit;
        }

        /// <summary>
        /// Transitions to ScoreEdit state - expand score list to full column
        /// </summary>
        [RelayCommand]
        public void EnterScoreEditState()
        {
            CurrentEditingState = EditingState.ScoreEdit;
        }

        /// <summary>
        /// Exits any editing state and returns to Default
        /// </summary>
        [RelayCommand]
        public void ExitEditingState()
        {
            CurrentEditingState = EditingState.Default;
        }

        #endregion

        #region Phase 3: Edition/Sticker/Seal Commands

        /// <summary>
        /// Sets the edition for ALL palette items AND future drops
        /// Does NOT modify items already in drop zones
        /// </summary>
        [RelayCommand]
        public void SetEdition(string edition)
        {
            // Set the selection for future drops
            SelectedEdition = edition;

            // Apply to ALL palette items (but NOT drop zone items)
            foreach (var item in AllJokers.Concat(AllTags).Concat(AllVouchers)
                .Concat(AllTarots).Concat(AllPlanets).Concat(AllSpectrals)
                .Concat(AllBosses).Concat(AllWildcards).Concat(AllStandardCards))
            {
                item.Edition = edition == "None" ? null : edition;
            }

            // Also apply to filtered items (they're references to the same objects but just in case)
            foreach (var item in FilteredJokers.Concat(FilteredTags).Concat(FilteredVouchers)
                .Concat(FilteredTarots).Concat(FilteredPlanets).Concat(FilteredSpectrals)
                .Concat(FilteredBosses).Concat(FilteredWildcards).Concat(FilteredStandardCards))
            {
                item.Edition = edition == "None" ? null : edition;
            }

            DebugLogger.Log("SetEdition", $"Edition '{edition}' applied to all palette items");
        }

        /// <summary>
        /// Toggles Perishable sticker for all items in drop zones AND future items
        /// </summary>
        [RelayCommand]
        public void ToggleStickerPerishable()
        {
            StickerPerishable = !StickerPerishable;

            // Mutual exclusivity: Turn off Eternal when Perishable is enabled
            if (StickerPerishable && StickerEternal)
            {
                StickerEternal = false;
                DebugLogger.Log("VisualBuilderTab", "Turned off Eternal (mutual exclusivity)");
            }

            // Apply to all existing items
            ApplyStickersToAllItems();

            DebugLogger.Log(
                "VisualBuilderTab",
                $"Perishable sticker: {StickerPerishable} - applied to all items"
            );
        }

        /// <summary>
        /// Toggles Eternal sticker for all items in drop zones AND future items (respects CanBeEternal logic)
        /// </summary>
        [RelayCommand]
        public void ToggleStickerEternal()
        {
            StickerEternal = !StickerEternal;

            // Mutual exclusivity: Turn off Perishable when Eternal is enabled
            if (StickerEternal && StickerPerishable)
            {
                StickerPerishable = false;
                DebugLogger.Log("VisualBuilderTab", "Turned off Perishable (mutual exclusivity)");
            }

            // Apply to all existing items
            ApplyStickersToAllItems();

            DebugLogger.Log(
                "VisualBuilderTab",
                $"Eternal sticker: {StickerEternal} - applied to all items"
            );
        }

        /// <summary>
        /// Toggles Rental sticker for all items in drop zones AND future items
        /// </summary>
        [RelayCommand]
        public void ToggleStickerRental()
        {
            StickerRental = !StickerRental;

            // Apply to all existing items
            ApplyStickersToAllItems();

            DebugLogger.Log(
                "VisualBuilderTab",
                $"Rental sticker: {StickerRental} - applied to all items"
            );
        }

        /// <summary>
        /// Toggles the Debuffed state (inverted filter logic) on all items in the shelf.
        /// When enabled, items dropped into MUST zone will have IsInvertedFilter=true (MUST-NOT logic).
        /// </summary>
        [RelayCommand]
        public void ToggleDebuffed()
        {
            // Toggle debuffed state on all items in shelf
            foreach (var group in GroupedItems)
            {
                foreach (var item in group.Items)
                {
                    item.IsDebuffed = !item.IsDebuffed; // Toggle red X overlay
                }
            }

            DebugLogger.Log(
                "VisualBuilderTab",
                $"Toggled Debuffed state on all shelf items"
            );
        }

        /// <summary>
        /// Sets the seal for ALL StandardCard palette items AND future drops
        /// Does NOT modify items already in drop zones
        /// </summary>
        [RelayCommand]
        public void SetSeal(string seal)
        {
            // Don't trigger animation if seal didn't actually change
            if (SelectedSeal == seal)
            {
                DebugLogger.Log(
                    "VisualBuilderTab",
                    $"Seal already set to '{seal}' - skipping"
                );
                return;
            }

            // Set the selection for future drops
            SelectedSeal = seal;

            // Apply to ALL StandardCard palette items (but NOT drop zone items)
            foreach (var item in AllStandardCards)
            {
                item.Seal = seal == "None" ? null : seal;
            }

            // Also apply to filtered StandardCards
            foreach (var item in FilteredStandardCards)
            {
                item.Seal = seal == "None" ? null : seal;
            }

            DebugLogger.Log("SetSeal", $"Seal '{seal}' applied to all StandardCard palette items");
        }

        /// <summary>
        /// Checks if an item can have the Eternal sticker based on CanBeEternal logic
        /// Jokers, Consumables, and Vouchers can be eternal; Tags cannot
        /// </summary>
        public bool CanItemBeEternal(FilterItem item)
        {
            return item.Type switch
            {
                "Joker" or "SoulJoker" => true,
                "Tarot" or "Planet" or "Spectral" => true,
                "Voucher" => true,
                "SmallBlindTag" or "BigBlindTag" => false,
                "Boss" => false,
                "StandardCard" => true,
                _ => false,
            };
        }

        /// <summary>
        /// Applies stickers to all items in shelf AND drop zones
        /// Respects Eternal restrictions for specific jokers and Soul Jokers
        /// </summary>
        private void ApplyStickersToAllItems()
        {
            // Jokers that CANNOT be Eternal (from Balatro game logic)
            var eternalRestrictedJokers = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                "Cavendish",
                "DietCola",
                "GrosMichel",
                "IceCream",
                "InvisibleJoker",
                "Luchador",
                "MrBones",
                "Popcorn",
                "Ramen",
                "Seltzer",
                "TurtleBean",
                // Soul Jokers also cannot be Eternal
                "Perkeo",
                "Triboulet",
                "Yorick",
                "Chicot",
                "Canio",
            };

            // Apply to ALL items in the shelf (DIRECT property update - no ItemConfigs needed!)
            foreach (var group in GroupedItems)
            {
                foreach (var item in group.Items)
                {
                    // CRITICAL FIX: Update item.Stickers directly to trigger image binding update
                    var stickers = new List<string>();

                    // Perishable and Eternal are mutually exclusive
                    if (StickerPerishable)
                    {
                        stickers.Add("perishable");
                    }
                    else if (
                        StickerEternal
                        && CanItemBeEternal(item)
                        && !eternalRestrictedJokers.Contains(item.Name)
                    )
                    {
                        stickers.Add("eternal");
                    }

                    if (StickerRental)
                    {
                        stickers.Add("rental");
                    }

                    item.Stickers = stickers.Count > 0 ? stickers : null;

                    // Also update ItemConfig if it exists (for when item gets dropped to zones)
                    if (
                        _parentViewModel != null
                        && _parentViewModel.ItemConfigs.TryGetValue(item.ItemKey, out var config)
                    )
                    {
                        config.Stickers = item.Stickers;
                    }
                }
            }

            // Helper buttons only affect shelf items, NOT drop zones
            DebugLogger.Log(
                "VisualBuilderTab",
                $"Stickers applied to shelf items only"
            );
        }

        /// <summary>
        /// Helper method to apply sticker logic with Eternal restrictions
        /// </summary>
        private void ApplyStickerLogic(
            ItemConfig config,
            string itemName,
            HashSet<string> eternalRestrictedJokers
        )
        {
            // Build sticker list based on current toggles
            var stickers = new List<string>();

            // Perishable and Eternal are mutually exclusive
            if (StickerPerishable)
            {
                stickers.Add("perishable");
            }
            else if (StickerEternal)
            {
                // Check if item type can be eternal
                bool canTypeBeEternal = config.ItemType switch
                {
                    "Joker" or "SoulJoker" => true,
                    "Tarot" or "Planet" or "Spectral" => true,
                    "Voucher" => true,
                    "StandardCard" => true,
                    "SmallBlindTag" or "BigBlindTag" => false,
                    "Boss" => false,
                    _ => false,
                };

                // Check if specific joker is restricted from Eternal
                bool isRestrictedJoker =
                    (config.ItemType == "Joker" || config.ItemType == "SoulJoker")
                    && eternalRestrictedJokers.Contains(itemName);

                if (canTypeBeEternal && !isRestrictedJoker)
                {
                    stickers.Add("eternal");
                }
            }

            // Rental can combine with Eternal but not Perishable
            if (StickerRental && !StickerPerishable)
            {
                stickers.Add("rental");
            }

            // Apply stickers (or clear if none selected)
            config.Stickers = stickers.Any() ? stickers : null;
        }

        /// <summary>
        /// Applies the currently selected edition, stickers, and seal to an ItemConfig
        /// Called when an item is added to a drop zone
        /// Respects Eternal restrictions for specific jokers
        /// </summary>
        private void ApplyEditionStickersSeal(ItemConfig config, FilterItem item)
        {
            // Apply Edition (if not None)
            if (SelectedEdition != "None")
            {
                config.Edition = SelectedEdition.ToLower();
                item.Edition = config.Edition; // CRITICAL: Update item to trigger EditionImage binding
            }

            // Apply Stickers with Eternal restrictions
            var eternalRestrictedJokers = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                "Cavendish",
                "DietCola",
                "GrosMichel",
                "IceCream",
                "InvisibleJoker",
                "Luchador",
                "MrBones",
                "Popcorn",
                "Ramen",
                "Seltzer",
                "TurtleBean",
                // Soul Jokers also cannot be Eternal
                "Perkeo",
                "Triboulet",
                "Yorick",
                "Chicot",
                "Canio",
            };

            var stickers = new List<string>();

            // Perishable and Eternal are mutually exclusive
            if (StickerPerishable)
            {
                stickers.Add("perishable");
            }
            else if (StickerEternal && CanItemBeEternal(item))
            {
                // Check if specific joker is restricted from Eternal
                bool isRestrictedJoker =
                    (item.Type == "Joker" || item.Type == "SoulJoker")
                    && eternalRestrictedJokers.Contains(item.Name);

                if (!isRestrictedJoker)
                {
                    stickers.Add("eternal");
                }
            }

            // Rental can combine with Eternal but not Perishable
            if (StickerRental && !StickerPerishable)
            {
                stickers.Add("rental");
            }

            if (stickers.Any())
            {
                config.Stickers = stickers;
                item.Stickers = config.Stickers; // CRITICAL: Update item to trigger sticker image bindings
            }

            // Apply Seal (for StandardCards only)
            if (item.Type == "StandardCard" && SelectedSeal != "None")
            {
                config.Seal = SelectedSeal;
            }

            DebugLogger.Log(
                "VisualBuilderTab",
                $"Applied edition={config.Edition}, stickers=[{string.Join(",", config.Stickers ?? new List<string>())}], seal={config.Seal} to {item.Name}"
            );
        }

        #endregion

        #region Load/Save Sync

        /// <summary>
        /// Syncs Visual Builder collections from parent's ItemConfigs and Selected* collections
        /// CRITICAL for loading filters into Visual Builder tab!
        /// </summary>
        public void LoadFromParentCollections()
        {
            if (_parentViewModel == null)
            {
                DebugLogger.Log("VisualBuilderTab", "❌ Cannot load - parent view model is null!");
                return;
            }

            DebugLogger.Log("VisualBuilderTab", $"🔄 Loading from parent collections - Must: {_parentViewModel.SelectedMust.Count}, Should: {_parentViewModel.SelectedShould.Count}");

            // Clear current visual builder state
            SelectedMust.Clear();
            SelectedShould.Clear();

            // Load MUST items
            foreach (var itemKey in _parentViewModel.SelectedMust)
            {
                if (_parentViewModel.ItemConfigs.TryGetValue(itemKey, out var config))
                {
                    var filterItem = CreateFilterItemFromConfig(config);
                    if (filterItem != null)
                    {
                        SelectedMust.Add(filterItem);
                        DebugLogger.Log("VisualBuilderTab", $"Loaded MUST item: {filterItem.Name}");
                    }
                }
            }

            // Load SHOULD items
            foreach (var itemKey in _parentViewModel.SelectedShould)
            {
                if (_parentViewModel.ItemConfigs.TryGetValue(itemKey, out var config))
                {
                    var filterItem = CreateFilterItemFromConfig(config);
                    if (filterItem != null)
                    {
                        SelectedShould.Add(filterItem);
                        DebugLogger.Log(
                            "VisualBuilderTab",
                            $"Loaded SHOULD item: {filterItem.Name}"
                        );
                    }
                }
            }

            // MUST-NOT removed - items with IsInvertedFilter=true in Must collection are treated as MUST-NOT

            DebugLogger.Log(
                "VisualBuilderTab",
                $"✅ Finished loading - Now have {SelectedMust.Count} MUST, {SelectedShould.Count} SHOULD items in VisualBuilderTab"
            );
        }

        private FilterItem? CreateFilterItemFromConfig(ItemConfig config)
        {
            var ss = Services.SpriteService.Instance;

            // Determine category from ItemType
            string category = config.ItemType switch
            {
                "Joker" or "SoulJoker" => "Joker",
                "Tarot" or "TarotCard" => "Consumable",
                "Spectral" or "SpectralCard" => "Consumable",
                "Planet" or "PlanetCard" => "Consumable",
                "Voucher" => "Voucher",
                "SmallBlindTag" or "BigBlindTag" or "Tag" => "Tag",
                "Boss" or "BossBlind" => "Boss",
                "PlayingCard" or "StandardCard" => "StandardCard",
                _ => config.ItemType,
            };

            var filterItem = new FilterItem
            {
                Name = config.ItemName,
                DisplayName = config.ItemName,
                Type = config.ItemType,
                Category = category,
                ItemKey = config.ItemKey,
                Edition = config.Edition,
                Stickers = config.Stickers,
                Antes = config.Antes?.ToArray(),
            };

            // Get sprite image
            if (config.ItemType == "Joker" || config.ItemType == "SoulJoker")
            {
                filterItem.ItemImage = ss.GetJokerImage(config.ItemName);
            }
            else if (config.ItemType == "Voucher")
            {
                filterItem.ItemImage = ss.GetVoucherImage(config.ItemName);
            }
            else if (config.ItemType.Contains("Tarot"))
            {
                filterItem.ItemImage = ss.GetTarotImage(config.ItemName);
            }
            else if (config.ItemType.Contains("Spectral"))
            {
                filterItem.ItemImage = ss.GetSpectralImage(config.ItemName);
            }
            else if (config.ItemType.Contains("Planet"))
            {
                filterItem.ItemImage = ss.GetPlanetCardImage(config.ItemName);
            }
            else if (config.ItemType.Contains("Tag"))
            {
                filterItem.ItemImage = ss.GetTagImage(config.ItemName);
            }
            else if (config.ItemType == "Boss")
            {
                filterItem.ItemImage = ss.GetBossImage(config.ItemName);
            }
            else if (config.ItemType == "PlayingCard" || config.ItemType == "StandardCard")
            {
                // TODO: Handle playing cards properly
                filterItem.ItemImage = ss.GetSpecialImage("BlankCard");
            }

            return filterItem;
        }

        #endregion
    }
}

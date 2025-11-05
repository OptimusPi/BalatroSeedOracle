using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;
using Avalonia.Threading;
using BalatroSeedOracle.Controls;
using BalatroSeedOracle.Helpers;
using BalatroSeedOracle.Models;
using BalatroSeedOracle.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace BalatroSeedOracle.ViewModels.FilterTabs
{
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

        // Expose parent's FilterName for display
        public string FilterName => _parentViewModel?.FilterName ?? "New Filter";

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

        // Grouped items for the new UI
        public class ItemGroup : ObservableObject
        {
            public string GroupName { get; set; } = "";
            public ObservableCollection<FilterItem> Items { get; set; } = new();
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

        // Edition/Enhancement/Seal toggles
        [ObservableProperty]
        private string _currentEdition = "None";

        [ObservableProperty]
        private string _currentEnhancement = "None";

        [ObservableProperty]
        private string _currentSeal = "None";

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

        // Operator Tray - permanent OR and AND operators at the top of the shelf
        public FilterOperatorItem TrayOrOperator { get; }
        public FilterOperatorItem TrayAndOperator { get; }

        // Selected items - these should sync with parent
        public ObservableCollection<FilterItem> SelectedMust { get; }
        public ObservableCollection<FilterItem> SelectedShould { get; }
        public ObservableCollection<FilterItem> SelectedMustNot { get; }

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

            // Initialize Operator Tray with permanent OR and AND operators
            TrayOrOperator = new FilterOperatorItem("OR")
            {
                DisplayName = "OR",
                Type = "Operator",
                Category = "Operator",
            };
            TrayAndOperator = new FilterOperatorItem("AND")
            {
                DisplayName = "AND",
                Type = "Operator",
                Category = "Operator",
            };

            SelectedMust = new ObservableCollection<FilterItem>();
            SelectedShould = new ObservableCollection<FilterItem>();
            SelectedMustNot = new ObservableCollection<FilterItem>();

            // Initialize operator trays
            OrTrayItems = new ObservableCollection<FilterItem>();
            AndTrayItems = new ObservableCollection<FilterItem>();

            ItemConfigs = new Dictionary<string, ItemConfig>();

            // Subscribe to collection changes for auto-save
            SelectedMust.CollectionChanged += OnZoneCollectionChanged;
            SelectedShould.CollectionChanged += OnZoneCollectionChanged;
            SelectedMustNot.CollectionChanged += OnZoneCollectionChanged;
            OrTrayItems.CollectionChanged += OnZoneCollectionChanged;
            AndTrayItems.CollectionChanged += OnZoneCollectionChanged;

            // Initialize commands
            AddToMustCommand = new RelayCommand<FilterItem>(AddToMust);
            AddToShouldCommand = new RelayCommand<FilterItem>(AddToShould);
            AddToMustNotCommand = new RelayCommand<FilterItem>(AddToMustNot);

            RemoveFromMustCommand = new RelayCommand<FilterItem>(RemoveFromMust);
            RemoveFromShouldCommand = new RelayCommand<FilterItem>(RemoveFromShould);
            RemoveFromMustNotCommand = new RelayCommand<FilterItem>(RemoveFromMustNot);

            AddToOrTrayCommand = new RelayCommand<FilterItem>(AddToOrTray);
            AddToAndTrayCommand = new RelayCommand<FilterItem>(AddToAndTray);
            RemoveFromOrTrayCommand = new RelayCommand<FilterItem>(RemoveFromOrTray);
            RemoveFromAndTrayCommand = new RelayCommand<FilterItem>(RemoveFromAndTray);

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
                    // Organize vouchers to match sprite sheet layout: 8 columns wide with base/upgrade rows
                    var voucherPairs = GetVoucherPairs();
                    var organizedVouchers = new List<FilterItem>();

                    // First 8 pairs (row 0 bases, then row 1 upgrades)
                    var firstSet = voucherPairs.Take(8).ToList();

                    // Add all 8 base vouchers from row 0
                    foreach (var (baseName, _) in firstSet)
                    {
                        var baseVoucher = FilteredVouchers.FirstOrDefault(v =>
                            v.Name.Equals(baseName, StringComparison.OrdinalIgnoreCase));
                        if (baseVoucher != null)
                            organizedVouchers.Add(baseVoucher);
                    }

                    // Add all 8 upgrade vouchers from row 1
                    foreach (var (_, upgradeName) in firstSet)
                    {
                        var upgradeVoucher = FilteredVouchers.FirstOrDefault(v =>
                            v.Name.Equals(upgradeName, StringComparison.OrdinalIgnoreCase));
                        if (upgradeVoucher != null)
                            organizedVouchers.Add(upgradeVoucher);
                    }

                    // Second 8 pairs (row 2 bases, then row 3 upgrades)
                    var secondSet = voucherPairs.Skip(8).Take(8).ToList();

                    // Add all 8 base vouchers from row 2
                    foreach (var (baseName, _) in secondSet)
                    {
                        var baseVoucher = FilteredVouchers.FirstOrDefault(v =>
                            v.Name.Equals(baseName, StringComparison.OrdinalIgnoreCase));
                        if (baseVoucher != null)
                            organizedVouchers.Add(baseVoucher);
                    }

                    // Add all 8 upgrade vouchers from row 3
                    foreach (var (_, upgradeName) in secondSet)
                    {
                        var upgradeVoucher = FilteredVouchers.FirstOrDefault(v =>
                            v.Name.Equals(upgradeName, StringComparison.OrdinalIgnoreCase));
                        if (upgradeVoucher != null)
                            organizedVouchers.Add(upgradeVoucher);
                    }

                    // Add any remaining vouchers that weren't in the pairs
                    var remainingVouchers = FilteredVouchers.Except(organizedVouchers);
                    organizedVouchers.AddRange(remainingVouchers);

                    AddGroup("Vouchers", organizedVouchers);
                    break;

                case "StandardCard":
                    // Standard playing cards organized by suit and enhancement
                    AddGroup("Hearts", FilteredStandardCards.Where(c => c.Category == "Hearts"));
                    AddGroup("Spades", FilteredStandardCards.Where(c => c.Category == "Spades"));
                    AddGroup("Diamonds", FilteredStandardCards.Where(c => c.Category == "Diamonds"));
                    AddGroup("Clubs", FilteredStandardCards.Where(c => c.Category == "Clubs"));
                    AddGroup("Mult Cards", FilteredStandardCards.Where(c => c.Category == "Mult"));
                    AddGroup("Bonus Cards", FilteredStandardCards.Where(c => c.Category == "Bonus"));
                    AddGroup("Glass Cards", FilteredStandardCards.Where(c => c.Category == "Glass"));
                    AddGroup("Gold Cards", FilteredStandardCards.Where(c => c.Category == "Gold"));
                    AddGroup("Steel Cards", FilteredStandardCards.Where(c => c.Category == "Steel"));
                    AddGroup("Stone Card", FilteredStandardCards.Where(c => c.Category == "Stone"));
                    break;
            }
        }

        private void AddGroup(string groupName, IEnumerable<FilterItem> items)
        {
            var group = new ItemGroup
            {
                GroupName = groupName,
                Items = new ObservableCollection<FilterItem>(items),
            };
            GroupedItems.Add(group);
        }

        /// <summary>
        /// Get vouchers organized into pairs (base + upgrade) matching sprite sheet layout.
        /// Returns 8-column pairs where each pair has base voucher followed by upgrade voucher.
        /// </summary>
        private List<(string baseName, string upgradeName)> GetVoucherPairs()
        {
            return new List<(string, string)>
            {
                // Row 0 -> Row 1 pairs (8 pairs)
                ("overstock", "overstockplus"),
                ("tarotmerchant", "tarottycoon"),
                ("planetmerchant", "planettycoon"),
                ("clearancesale", "liquidation"),
                ("hone", "glowup"),
                ("grabber", "nachotong"),
                ("wasteful", "recyclomancy"),
                ("blank", "antimatter"),

                // Row 2 -> Row 3 pairs (8 pairs)
                ("rerollsurplus", "rerollglut"),
                ("seedmoney", "moneytree"),
                ("crystalball", "omenglobe"),
                ("telescope", "observatory"),
                ("magictrick", "illusion"),
                ("hieroglyph", "petroglyph"),
                ("directorscut", "retcon"),
                ("paintbrush", "palette"),
            };
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

            FilteredItems.Clear();
            foreach (var item in sourceCollection)
            {
                FilteredItems.Add(item);
            }

            // Also rebuild grouped items after filtering
            RebuildGroupedItems();
        }

        #region Commands

        public ICommand AddToMustCommand { get; }
        public ICommand AddToShouldCommand { get; }
        public ICommand AddToMustNotCommand { get; }

        public ICommand RemoveFromMustCommand { get; }
        public ICommand RemoveFromShouldCommand { get; }
        public ICommand RemoveFromMustNotCommand { get; }

        public ICommand AddToOrTrayCommand { get; }
        public ICommand AddToAndTrayCommand { get; }
        public ICommand RemoveFromOrTrayCommand { get; }
        public ICommand RemoveFromAndTrayCommand { get; }

        #endregion

        #region Command Implementations

        private void AddToMust(FilterItem? item)
        {
            if (item == null) return;

            // ALLOW DUPLICATES: Same item can be added multiple times with different configs
            SelectedMust.Add(item);

            // Sync with parent ViewModel if available
            if (_parentViewModel != null)
            {
                // Special handling for operators
                if (item is FilterOperatorItem operatorItem)
                {
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
            if (item == null) return;

            // ALLOW DUPLICATES: Same item can be added multiple times with different configs
            SelectedShould.Add(item);

            // Sync with parent ViewModel if available
            if (_parentViewModel != null)
            {
                // Special handling for operators
                if (item is FilterOperatorItem operatorItem)
                {
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
                    _parentViewModel.ItemConfigs[itemKey] = itemConfig;
                    _parentViewModel.SelectedShould.Add(itemKey);
                }
            }

            DebugLogger.Log("VisualBuilderTab", $"Added {item.Name} to SHOULD");

            // Trigger auto-sync to JSON Editor
            NotifyJsonEditorOfChanges();
        }

        private void AddToMustNot(FilterItem? item)
        {
            if (item == null) return;

            // ALLOW DUPLICATES: Same item can be added multiple times with different configs
            SelectedMustNot.Add(item);

            // Sync with parent ViewModel if available
            if (_parentViewModel != null)
            {
                // Special handling for operators
                if (item is FilterOperatorItem operatorItem)
                {
                    SyncOperatorToParent(operatorItem, "MustNot");
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
                    _parentViewModel.ItemConfigs[itemKey] = itemConfig;
                    _parentViewModel.SelectedMustNot.Add(itemKey);
                }
            }

            DebugLogger.Log("VisualBuilderTab", $"Added {item.Name} to MUST NOT");

            // Trigger auto-sync to JSON Editor
            NotifyJsonEditorOfChanges();
        }

        private void RemoveFromMust(FilterItem? item)
        {
            if (item == null) return;

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
            if (item == null) return;

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

        private void RemoveFromMustNot(FilterItem? item)
        {
            if (item == null) return;

            SelectedMustNot.Remove(item);

            // Sync with parent ViewModel - remove from parent collections
            if (_parentViewModel != null)
            {
                RemoveItemFromParent(item, _parentViewModel.SelectedMustNot);
            }

            DebugLogger.Log("VisualBuilderTab", $"Removed {item.Name} from MUST NOT");

            // Trigger auto-sync to JSON Editor
            NotifyJsonEditorOfChanges();
        }

        private void AddToOrTray(FilterItem? item)
        {
            if (item == null) return;

            // Add to OR tray - these are still SHOULD items, just visually grouped
            OrTrayItems.Add(item);

            // Also add to SelectedShould to maintain compatibility with existing logic
            if (!SelectedShould.Contains(item))
            {
                AddToShould(item);
            }

            DebugLogger.Log("VisualBuilderTab", $"Added {item.Name} to OR tray");
        }

        private void AddToAndTray(FilterItem? item)
        {
            if (item == null) return;

            // Add to AND tray - these are still SHOULD items, just visually grouped
            AndTrayItems.Add(item);

            // Also add to SelectedShould to maintain compatibility with existing logic
            if (!SelectedShould.Contains(item))
            {
                AddToShould(item);
            }

            DebugLogger.Log("VisualBuilderTab", $"Added {item.Name} to AND tray");
        }

        private void RemoveFromOrTray(FilterItem? item)
        {
            if (item == null) return;

            OrTrayItems.Remove(item);

            // Also remove from SelectedShould
            RemoveFromShould(item);

            DebugLogger.Log("VisualBuilderTab", $"Removed {item.Name} from OR tray");
        }

        private void RemoveFromAndTray(FilterItem? item)
        {
            if (item == null) return;

            AndTrayItems.Remove(item);

            // Also remove from SelectedShould
            RemoveFromShould(item);

            DebugLogger.Log("VisualBuilderTab", $"Removed {item.Name} from AND tray");
        }

        /// <summary>
        /// Removes an item from the parent's collections and ItemConfigs
        /// </summary>
        private void RemoveItemFromParent(FilterItem item, ObservableCollection<string> parentCollection)
        {
            if (_parentViewModel == null)
                return;

            // Find the item key in parent's ItemConfigs
            var itemKey = _parentViewModel.ItemConfigs.FirstOrDefault(kvp =>
                kvp.Value.ItemName == item.Name &&
                kvp.Value.ItemType == item.Type).Key;

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
                "MustNot" => _parentViewModel.SelectedMustNot,
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
            catch
            {
                // Even on error, clear loading state
                await Avalonia.Threading.Dispatcher.UIThread.InvokeAsync(() =>
                {
                    IsLoading = false;
                });
                throw;
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
                            Category = "Wildcard",
                            DisplayName = displayName,
                            ItemImage = type switch
                            {
                                "Joker" or "SoulJoker" => spriteService.GetJokerImage(name),
                                "Tarot" => spriteService.GetTarotImage(name),
                                "Planet" => spriteService.GetPlanetCardImage(name),
                                "Spectral" => spriteService.GetSpectralImage(name),
                                _ => null
                            }
                        }
                    );
                }

                // Load favorites (from FavoritesService)
                var favoritesService = ServiceHelper.GetService<FavoritesService>();
                var favoriteNames = favoritesService?.GetFavoriteItems() ?? new List<string>();

                foreach (var favoriteName in favoriteNames)
                {
                    var item = new FilterItem
                    {
                        Name = favoriteName,
                        Type = "Joker",
                        Category = "Favorite",
                        IsFavorite = true,
                        DisplayName = BalatroData.GetDisplayNameFromSprite(favoriteName),
                        ItemImage = spriteService.GetJokerImage(favoriteName),
                    };
                    AllJokers.Add(item);
                }

                // Load Soul Jokers SECOND (after wildcards)
                var legendaryJokers = new[] { "Triboulet", "Yorick", "Chicot", "Perkeo", "Canio" };
                foreach (var legendaryName in legendaryJokers)
                {
                    var item = new FilterItem
                    {
                        Name = legendaryName,
                        Type = "SoulJoker",
                        Category = "Legendary",
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
                        // Skip if already added from favorites or legendaries
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
                            Category = rarity, // Actual rarity from BalatroData
                            DisplayName = BalatroData.GetDisplayNameFromSprite(jokerName),
                            ItemImage = spriteService.GetJokerImage(jokerName),
                        };
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
                            DisplayName = BalatroData.GetDisplayNameFromSprite(tagName),
                            ItemImage = spriteService.GetTagImage(tagName),
                        };
                        AllTags.Add(item);
                    }
                }

                // Load Vouchers from BalatroData
                if (BalatroData.Vouchers?.Keys != null)
                {
                    foreach (var voucherName in BalatroData.Vouchers.Keys)
                    {
                        var item = new FilterItem
                        {
                            Name = voucherName,
                            Type = "Voucher",
                            DisplayName = BalatroData.GetDisplayNameFromSprite(voucherName),
                            ItemImage = spriteService.GetVoucherImage(voucherName),
                        };
                        AllVouchers.Add(item);
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
                    var ranks = new[] { "2", "3", "4", "5", "6", "7", "8", "9", "10", "Jack", "Queen", "King", "Ace" };

                    // Generate base 52 cards (Type A - Normal cards with no enhancement)
                    foreach (var suit in suits)
                    {
                        foreach (var rank in ranks)
                        {
                            var displayName = rank == "Ace" ? $"Ace of {suit}" : $"{rank} of {suit}";
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
                                ItemImage = spriteService.GetPlayingCardImage(suit, rank, enhancement),
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

                    DebugLogger.Log("VisualBuilderTab", $"Loaded {AllStandardCards.Count} standard playing cards");
                }
                catch (Exception ex)
                {
                    DebugLogger.LogError("VisualBuilderTab", $"Error loading standard cards: {ex.Message}");
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

            DebugLogger.Log("VisualBuilderTab", $"ApplyFilter called - AllJokers count: {AllJokers.Count}, filter: '{filter}'");

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

            DebugLogger.Log("VisualBuilderTab", $"FilteredJokers after ApplyFilter: {FilteredJokers.Count}");

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
                    if (_parentViewModel != null && itemIndex < _parentViewModel.SelectedShould.Count)
                    {
                        itemKeyToRemove = _parentViewModel.SelectedShould[itemIndex];
                    }
                    SelectedShould.RemoveAt(itemIndex);
                }
                // Check MUSTNOT zone
                else
                {
                    itemIndex = SelectedMustNot.IndexOf(item);
                    if (itemIndex >= 0)
                    {
                        sourceZone = "MUSTNOT";
                        if (_parentViewModel != null && itemIndex < _parentViewModel.SelectedMustNot.Count)
                        {
                            itemKeyToRemove = _parentViewModel.SelectedMustNot[itemIndex];
                        }
                        SelectedMustNot.RemoveAt(itemIndex);
                    }
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
                else if (sourceZone == "MUSTNOT")
                {
                    _parentViewModel.SelectedMustNot.Remove(itemKeyToRemove);
                }
            }

            DebugLogger.Log("VisualBuilderTab", $"Removed item: {item.Name} from {sourceZone ?? "UNKNOWN"} zone");
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
                    DebugLogger.LogError("VisualBuilderTab", "Auto-save failed: services not available");
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
                            DebugLogger.Log("VisualBuilderTab", $"Deleted old filter file with spaces: {oldFilePath}");
                        }
                        catch (Exception ex)
                        {
                            DebugLogger.LogError("VisualBuilderTab", $"Failed to delete old filter file: {ex.Message}");
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
                        Task.Delay(2000).ContinueWith(_ =>
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
    }
}

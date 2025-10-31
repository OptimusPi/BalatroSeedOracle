using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
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

        [ObservableProperty]
        private string _searchFilter = "";

        [ObservableProperty]
        private bool _isLoading = true;

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

        // Filtered items (based on search)
        public ObservableCollection<FilterItem> FilteredJokers { get; }
        public ObservableCollection<FilterItem> FilteredTags { get; }
        public ObservableCollection<FilterItem> FilteredVouchers { get; }
        public ObservableCollection<FilterItem> FilteredTarots { get; }
        public ObservableCollection<FilterItem> FilteredPlanets { get; }
        public ObservableCollection<FilterItem> FilteredSpectrals { get; }
        public ObservableCollection<FilterItem> FilteredBosses { get; }

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

        // Scroll-to-view trigger property
        private string? _scrollToGroupName;
        public string? ScrollToGroupName
        {
            get => _scrollToGroupName;
            private set
            {
                if (_scrollToGroupName != value)
                {
                    _scrollToGroupName = value;
                    OnPropertyChanged(nameof(ScrollToGroupName));
                }
            }
        }

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

        // Selected items - these should sync with parent
        public ObservableCollection<FilterItem> SelectedMust { get; }
        public ObservableCollection<FilterItem> SelectedShould { get; }
        public ObservableCollection<FilterItem> SelectedMustNot { get; }

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

            FilteredJokers = new ObservableCollection<FilterItem>();
            FilteredTags = new ObservableCollection<FilterItem>();
            FilteredVouchers = new ObservableCollection<FilterItem>();
            FilteredTarots = new ObservableCollection<FilterItem>();
            FilteredPlanets = new ObservableCollection<FilterItem>();
            FilteredSpectrals = new ObservableCollection<FilterItem>();
            FilteredBosses = new ObservableCollection<FilterItem>();

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

            SelectedMust = new ObservableCollection<FilterItem>();
            SelectedShould = new ObservableCollection<FilterItem>();
            SelectedMustNot = new ObservableCollection<FilterItem>();

            ItemConfigs = new Dictionary<string, ItemConfig>();

            // Initialize commands
            AddToMustCommand = new RelayCommand<FilterItem>(AddToMust);
            AddToShouldCommand = new RelayCommand<FilterItem>(AddToShould);
            AddToMustNotCommand = new RelayCommand<FilterItem>(AddToMustNot);

            RemoveFromMustCommand = new RelayCommand<FilterItem>(RemoveFromMust);
            RemoveFromShouldCommand = new RelayCommand<FilterItem>(RemoveFromShould);
            RemoveFromMustNotCommand = new RelayCommand<FilterItem>(RemoveFromMustNot);

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
            _ = Task.Run(LoadSampleDataAsync);
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
                    AddGroup("FAVORITE ITEMS", favoriteItems);

                    // Section 2: Filter Operators (OR/AND)
                    var operators = new List<FilterItem>
                    {
                        new FilterOperatorItem("OR")
                        {
                            DisplayName = "OR",
                            Type = "Operator",
                            Category = "Operator",
                        },
                        new FilterOperatorItem("AND")
                        {
                            DisplayName = "AND",
                            Type = "Operator",
                            Category = "Operator",
                        },
                    };
                    AddGroup("FILTER OPERATORS", operators);

                    // Section 3: Wildcards
                    var wildcards = AllJokers.Where(j => j.Name.StartsWith("Wildcard_")).ToList();
                    wildcards.AddRange(AllTarots.Where(t => t.Name.StartsWith("Wildcard_")));
                    wildcards.AddRange(AllPlanets.Where(p => p.Name.StartsWith("Wildcard_")));
                    wildcards.AddRange(AllSpectrals.Where(s => s.Name.StartsWith("Wildcard_")));
                    AddGroup("WILDCARDS", wildcards);
                    break;

                case "Joker":
                    // Add groups: Legendary, Rare, Uncommon, Common
                    AddGroup("LEGENDARY JOKERS", FilteredJokers.Where(j => j.Type == "SoulJoker"));
                    AddGroup(
                        "RARE JOKERS",
                        FilteredJokers.Where(j => j.Type == "Joker" && j.Category == "Rare")
                    );
                    AddGroup(
                        "UNCOMMON JOKERS",
                        FilteredJokers.Where(j => j.Type == "Joker" && j.Category == "Uncommon")
                    );
                    AddGroup(
                        "COMMON JOKERS",
                        FilteredJokers.Where(j => j.Type == "Joker" && j.Category == "Common")
                    );
                    break;

                case "Consumable":
                    AddGroup("TAROT CARDS", FilteredTarots);
                    AddGroup("PLANET CARDS", FilteredPlanets);
                    AddGroup("SPECTRAL CARDS", FilteredSpectrals);
                    break;

                case "SkipTag":
                    // For now, just show all tags (ante filtering can be added later)
                    AddGroup("SKIP TAGS - ANY ANTE", FilteredTags);
                    break;

                case "Boss":
                    // For now, show all bosses (regular/finisher split can be added later)
                    AddGroup("BOSS BLINDS", FilteredBosses);
                    break;

                case "Voucher":
                    // For now, show all vouchers (base/upgrade split can be added later)
                    AddGroup("VOUCHERS", FilteredVouchers);
                    break;

                case "StandardCard":
                    // Playing cards logic (to be implemented later)
                    AddGroup("PLAYING CARDS", new List<FilterItem>());
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

        #region Scroll-to-View Commands

        // Joker Commands
        [RelayCommand]
        private void ScrollToLegendary()
        {
            DebugLogger.Log("VisualBuilderTab", "Scroll to Legendary requested");
            ScrollToGroupName = "LEGENDARY JOKERS";
        }

        [RelayCommand]
        private void ScrollToRare()
        {
            DebugLogger.Log("VisualBuilderTab", "Scroll to Rare requested");
            ScrollToGroupName = "RARE JOKERS";
        }

        [RelayCommand]
        private void ScrollToUncommon()
        {
            DebugLogger.Log("VisualBuilderTab", "Scroll to Uncommon requested");
            ScrollToGroupName = "UNCOMMON JOKERS";
        }

        [RelayCommand]
        private void ScrollToCommon()
        {
            DebugLogger.Log("VisualBuilderTab", "Scroll to Common requested");
            ScrollToGroupName = "COMMON JOKERS";
        }

        // Consumable Commands
        [RelayCommand]
        private void ScrollToTarot()
        {
            DebugLogger.Log("VisualBuilderTab", "Scroll to Tarot requested");
            ScrollToGroupName = "TAROT CARDS";
        }

        [RelayCommand]
        private void ScrollToPlanet()
        {
            DebugLogger.Log("VisualBuilderTab", "Scroll to Planet requested");
            ScrollToGroupName = "PLANET CARDS";
        }

        [RelayCommand]
        private void ScrollToSpectral()
        {
            DebugLogger.Log("VisualBuilderTab", "Scroll to Spectral requested");
            ScrollToGroupName = "SPECTRAL CARDS";
        }

        // Skip Tag Commands
        [RelayCommand]
        private void ScrollToAnyAnte()
        {
            DebugLogger.Log("VisualBuilderTab", "Scroll to Any Ante tags requested");
            ScrollToGroupName = "SKIP TAGS - ANY ANTE";
        }

        [RelayCommand]
        private void ScrollToAnte2Plus()
        {
            DebugLogger.Log("VisualBuilderTab", "Scroll to Ante 2+ tags requested");
            ScrollToGroupName = "SKIP TAGS - ANTE 2+";
        }

        // Boss Commands
        [RelayCommand]
        private void ScrollToRegularBoss()
        {
            DebugLogger.Log("VisualBuilderTab", "Scroll to Regular Boss requested");
            ScrollToGroupName = "BOSS BLINDS";
        }

        [RelayCommand]
        private void ScrollToFinisherBoss()
        {
            DebugLogger.Log("VisualBuilderTab", "Scroll to Finisher Boss requested");
            ScrollToGroupName = "FINISHER BOSS BLINDS";
        }

        // Voucher Commands
        [RelayCommand]
        private void ScrollToBaseVoucher()
        {
            DebugLogger.Log("VisualBuilderTab", "Scroll to Base Voucher requested");
            ScrollToGroupName = "BASE VOUCHERS";
        }

        [RelayCommand]
        private void ScrollToUpgradeVoucher()
        {
            DebugLogger.Log("VisualBuilderTab", "Scroll to Upgrade Voucher requested");
            ScrollToGroupName = "UPGRADE VOUCHERS";
        }

        // Standard Card Commands
        [RelayCommand]
        private void ScrollToSpade()
        {
            DebugLogger.Log("VisualBuilderTab", "Scroll to Spades requested");
            ScrollToGroupName = "SPADES";
        }

        [RelayCommand]
        private void ScrollToClub()
        {
            DebugLogger.Log("VisualBuilderTab", "Scroll to Clubs requested");
            ScrollToGroupName = "CLUBS";
        }

        [RelayCommand]
        private void ScrollToHeart()
        {
            DebugLogger.Log("VisualBuilderTab", "Scroll to Hearts requested");
            ScrollToGroupName = "HEARTS";
        }

        [RelayCommand]
        private void ScrollToDiamond()
        {
            DebugLogger.Log("VisualBuilderTab", "Scroll to Diamonds requested");
            ScrollToGroupName = "DIAMONDS";
        }

        [RelayCommand]
        private void ScrollToEnhanced()
        {
            DebugLogger.Log("VisualBuilderTab", "Scroll to Enhanced requested");
            ScrollToGroupName = "ENHANCED CARDS";
        }

        #endregion

        #region Commands

        public ICommand AddToMustCommand { get; }
        public ICommand AddToShouldCommand { get; }
        public ICommand AddToMustNotCommand { get; }

        public ICommand RemoveFromMustCommand { get; }
        public ICommand RemoveFromShouldCommand { get; }
        public ICommand RemoveFromMustNotCommand { get; }

        #endregion

        #region Command Implementations

        private void AddToMust(FilterItem? item)
        {
            // ALLOW DUPLICATES: Remove the name check so same item can be added multiple times with different configs
            if (item != null)
            {
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
        }

        private void AddToShould(FilterItem? item)
        {
            // ALLOW DUPLICATES: Remove the name check so same item can be added multiple times with different configs
            if (item != null)
            {
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
        }

        private void AddToMustNot(FilterItem? item)
        {
            // ALLOW DUPLICATES: Remove the name check so same item can be added multiple times with different configs
            if (item != null)
            {
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
        }

        private void RemoveFromMust(FilterItem? item)
        {
            if (item != null)
            {
                SelectedMust.Remove(item);
                DebugLogger.Log("VisualBuilderTab", $"Removed {item.Name} from MUST");

                // Trigger auto-sync to JSON Editor
                NotifyJsonEditorOfChanges();
            }
        }

        private void RemoveFromShould(FilterItem? item)
        {
            if (item != null)
            {
                SelectedShould.Remove(item);
                DebugLogger.Log("VisualBuilderTab", $"Removed {item.Name} from SHOULD");

                // Trigger auto-sync to JSON Editor
                NotifyJsonEditorOfChanges();
            }
        }

        private void RemoveFromMustNot(FilterItem? item)
        {
            if (item != null)
            {
                SelectedMustNot.Remove(item);
                DebugLogger.Log("VisualBuilderTab", $"Removed {item.Name} from MUST NOT");

                // Trigger auto-sync to JSON Editor
                NotifyJsonEditorOfChanges();
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

            // Add to the appropriate collection based on target zone
            // OR operators typically go to Should, but respect the targetZone
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

        private async Task LoadSampleDataAsync()
        {
            try
            {
                await Task.Run(() => LoadSampleData());

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

        private void LoadSampleData()
        {
            // Ensure UI thread safety for collection initialization
            if (!Avalonia.Threading.Dispatcher.UIThread.CheckAccess())
            {
                Avalonia.Threading.Dispatcher.UIThread.Post(LoadSampleData);
                return;
            }

            // Load from BalatroData with sprites
            try
            {
                var spriteService = SpriteService.Instance;

                // Load Favorites
                var favorites = FavoritesService.Instance.GetFavoriteItems();
                foreach (var fav in favorites)
                {
                    var item = new FilterItem
                    {
                        Name = fav,
                        Type = "Joker",
                        DisplayName = FormatDisplayName(fav),
                        ItemImage = spriteService.GetJokerImage(fav),
                    };
                    AllJokers.Add(item);
                }

                // Load favorites FIRST (from FavoritesService)
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

                // Add wildcard joker entries FIRST (at the very top)
                var wildcardJokers = new[]
                {
                    ("Wildcard_Joker", "Any Joker", "Joker", "Common"),
                    ("Wildcard_JokerCommon", "Any Common", "Joker", "Common"),
                    ("Wildcard_JokerUncommon", "Any Uncommon", "Joker", "Uncommon"),
                    ("Wildcard_JokerRare", "Any Rare", "Joker", "Rare"),
                    ("Wildcard_JokerLegendary", "Any Legendary", "SoulJoker", "Legendary"),
                };
                foreach (var (name, displayName, type, category) in wildcardJokers)
                {
                    AllJokers.Add(
                        new FilterItem
                        {
                            Name = name,
                            Type = type,
                            Category = category,
                            DisplayName = displayName,
                            ItemImage = spriteService.GetJokerImage(name), // Use wildcard name to get mystery sprite
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
                // Add wildcard tarot entry FIRST (at the very top)
                AllTarots.Add(
                    new FilterItem
                    {
                        Name = "Wildcard_Tarot",
                        Type = "Tarot",
                        DisplayName = "Any Tarot",
                        ItemImage = spriteService.GetTarotImage("Wildcard_Tarot"), // Will use anytarot base + mystery overlay
                    }
                );

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
                    // Add wildcard planet entry FIRST (at the very top)
                    AllPlanets.Add(
                        new FilterItem
                        {
                            Name = "Wildcard_Planet",
                            Type = "Planet",
                            DisplayName = "Any Planet",
                            ItemImage = spriteService.GetPlanetCardImage("Pluto"), // Planets don't have wildcard sprites, use Pluto
                        }
                    );

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
                // Add wildcard spectral entry FIRST (at the very top)
                AllSpectrals.Add(
                    new FilterItem
                    {
                        Name = "Wildcard_Spectral",
                        Type = "Spectral",
                        DisplayName = "Any Spectral",
                        ItemImage = spriteService.GetSpectralImage("Familiar"), // Spectrals don't have wildcard sprites, use Familiar
                    }
                );

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

                // Note: Playing cards would need special handling for suits/ranks
                // Skipping for now as they're more complex

                DebugLogger.Log(
                    "VisualBuilderTab",
                    $"Loaded {AllJokers.Count} jokers, {AllTags.Count} tags, {AllVouchers.Count} vouchers, {AllTarots.Count} tarots, {AllPlanets.Count} planets, {AllSpectrals.Count} spectrals, {AllBosses.Count} bosses with images"
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

            var filter = SearchFilter.ToLowerInvariant();

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
            // Remove from all zones
            SelectedMust.Remove(item);
            SelectedShould.Remove(item);
            SelectedMustNot.Remove(item);

            // Find and remove associated config
            var itemKey = ItemConfigs.Keys.FirstOrDefault(k =>
                ItemConfigs[k].ItemName == item.Name && ItemConfigs[k].ItemType == item.Type
            );

            if (!string.IsNullOrEmpty(itemKey))
            {
                ItemConfigs.Remove(itemKey);

                // Sync with parent ViewModel
                if (_parentViewModel != null)
                {
                    _parentViewModel.SelectedMust.Remove(itemKey);
                    _parentViewModel.SelectedShould.Remove(itemKey);
                    _parentViewModel.SelectedMustNot.Remove(itemKey);
                    _parentViewModel.ItemConfigs.Remove(itemKey);
                }
            }

            DebugLogger.Log("VisualBuilderTab", $"Removed item: {item.Name}");
        }

        #endregion
    }
}

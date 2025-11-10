using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
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
    /// <summary>
    /// ViewModel for Configure Filter tab - handles MUST and MUST NOT zones only.
    /// SHOULD items moved to Configure Score tab.
    /// </summary>
    public partial class ConfigureFilterTabViewModel : ObservableObject
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

        // Drop zones - only MUST and MUST NOT (no SHOULD)
        [ObservableProperty]
        private bool _isMustExpanded = true;

        [ObservableProperty]
        private bool _isMustNotExpanded = true;

        [ObservableProperty]
        private bool _isDragging = false;

        // Hover state tracking for drop-zones
        [ObservableProperty]
        private bool _isMustHovered = false;

        [ObservableProperty]
        private bool _isMustNotHovered = false;

        // Expose parent's FilterName for display
        public string FilterName => _parentViewModel?.FilterName ?? "New Filter";

        // Expose parent's SelectedDeck for flip animation
        public string SelectedDeck => _parentViewModel?.SelectedDeck ?? "Red";

        // Available items (reuse same collections structure as VisualBuilderTab)
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

        // Main category selection
        [ObservableProperty]
        private string _selectedMainCategory = "Joker";

        // Grouped items for the UI
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

        // Edition/Sticker/Seal selectors (apply to ALL items in shelf)
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

        // Flip Animation Trigger - incremented whenever edition/sticker/seal changes
        [ObservableProperty]
        private int _flipAnimationTrigger = 0;

        // Computed properties for button visibility based on category
        public bool ShowEditionButtons =>
            SelectedMainCategory == "Joker" || SelectedMainCategory == "StandardCard";
        public bool ShowStickerButtons => SelectedMainCategory == "Joker";
        public bool ShowSealButtons => SelectedMainCategory == "StandardCard";
        public bool ShowEnhancementButtons => SelectedMainCategory == "StandardCard";

        // Card flip animation only for Jokers (including Soul Jokers) and Standard Cards
        public bool SupportsFlipAnimation =>
            SelectedMainCategory == "Joker" || SelectedMainCategory == "StandardCard";

        // Operator Tray - permanent OR and AND operators
        public FilterOperatorItem TrayOrOperator { get; }
        public FilterOperatorItem TrayAndOperator { get; }

        // Selected items - only MUST and MUST NOT (no SHOULD)
        public ObservableCollection<FilterItem> SelectedMust { get; }
        public ObservableCollection<FilterItem> SelectedMustNot { get; }

        // Item configurations
        public Dictionary<string, ItemConfig> ItemConfigs { get; }

        public ConfigureFilterTabViewModel(FiltersModalViewModel? parentViewModel = null)
        {
            _parentViewModel = parentViewModel;

            // Subscribe to parent's property changes
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

            // Initialize Operator Tray
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
            SelectedMustNot = new ObservableCollection<FilterItem>();

            ItemConfigs = new Dictionary<string, ItemConfig>();

            // Subscribe to collection changes for auto-save
            SelectedMust.CollectionChanged += OnZoneCollectionChanged;
            SelectedMustNot.CollectionChanged += OnZoneCollectionChanged;

            // Initialize commands
            AddToMustCommand = new RelayCommand<FilterItem>(AddToMust);
            AddToMustNotCommand = new RelayCommand<FilterItem>(AddToMustNot);
            RemoveFromMustCommand = new RelayCommand<FilterItem>(RemoveFromMust);
            RemoveFromMustNotCommand = new RelayCommand<FilterItem>(RemoveFromMustNot);

            // Edition/Seal/Sticker commands
            SetEditionCommand = new RelayCommand<string>(SetEdition);
            SetSealCommand = new RelayCommand<string>(SetSeal);
            ToggleStickerPerishableCommand = new RelayCommand(ToggleStickerPerishable);
            ToggleStickerEternalCommand = new RelayCommand(ToggleStickerEternal);
            ToggleStickerRentalCommand = new RelayCommand(ToggleStickerRental);

            // Simple property change handling
            PropertyChanged += (s, e) =>
            {
                if (e.PropertyName == nameof(SearchFilter))
                {
                    ApplyFilter();
                }
            };

            // Initialize data asynchronously
            _ = Task.Run(LoadGameDataAsync);
        }

        public void SetCategory(string category)
        {
            SearchFilter = "";
            SelectedMainCategory = category;

            IsJokerCategorySelected = category == "Joker";
            IsConsumableCategorySelected = category == "Consumable";
            IsSkipTagCategorySelected = category == "SkipTag";
            IsBossCategorySelected = category == "Boss";
            IsVoucherCategorySelected = category == "Voucher";
            IsStandardCardCategorySelected = category == "StandardCard";

            // Notify property changes for button visibility
            OnPropertyChanged(nameof(ShowEditionButtons));
            OnPropertyChanged(nameof(ShowStickerButtons));
            OnPropertyChanged(nameof(ShowSealButtons));
            OnPropertyChanged(nameof(ShowEnhancementButtons));
            OnPropertyChanged(nameof(SupportsFlipAnimation));

            // Reset edition/sticker/seal state when switching categories
            SelectedEdition = "None";
            SelectedSeal = "None";
            StickerPerishable = false;
            StickerEternal = false;
            StickerRental = false;

            RebuildGroupedItems();

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
                    var favoriteItems = AllJokers.Where(j => j.IsFavorite == true).ToList();
                    AddGroup("Favorite Items", favoriteItems);
                    AddGroup("Wildcards", FilteredWildcards);
                    break;

                case "Joker":
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
                    AddGroup("Skip Tags - Any Ante", FilteredTags);
                    break;

                case "Boss":
                    AddGroup("Boss Blinds", FilteredBosses);
                    break;

                case "Voucher":
                    var voucherPairs = GetVoucherPairs();
                    var organizedVouchers = new List<FilterItem>();

                    var firstSet = voucherPairs.Take(8).ToList();
                    foreach (var (baseName, _) in firstSet)
                    {
                        var baseVoucher = FilteredVouchers.FirstOrDefault(v =>
                            v.Name.Equals(baseName, StringComparison.OrdinalIgnoreCase)
                        );
                        if (baseVoucher != null)
                            organizedVouchers.Add(baseVoucher);
                    }
                    foreach (var (_, upgradeName) in firstSet)
                    {
                        var upgradeVoucher = FilteredVouchers.FirstOrDefault(v =>
                            v.Name.Equals(upgradeName, StringComparison.OrdinalIgnoreCase)
                        );
                        if (upgradeVoucher != null)
                            organizedVouchers.Add(upgradeVoucher);
                    }

                    var secondSet = voucherPairs.Skip(8).Take(8).ToList();
                    foreach (var (baseName, _) in secondSet)
                    {
                        var baseVoucher = FilteredVouchers.FirstOrDefault(v =>
                            v.Name.Equals(baseName, StringComparison.OrdinalIgnoreCase)
                        );
                        if (baseVoucher != null)
                            organizedVouchers.Add(baseVoucher);
                    }
                    foreach (var (_, upgradeName) in secondSet)
                    {
                        var upgradeVoucher = FilteredVouchers.FirstOrDefault(v =>
                            v.Name.Equals(upgradeName, StringComparison.OrdinalIgnoreCase)
                        );
                        if (upgradeVoucher != null)
                            organizedVouchers.Add(upgradeVoucher);
                    }

                    var remainingVouchers = FilteredVouchers.Except(organizedVouchers);
                    organizedVouchers.AddRange(remainingVouchers);
                    AddGroup("Vouchers", organizedVouchers);
                    break;

                case "StandardCard":
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
                Items = new ObservableCollection<FilterItem>(items),
            };
            GroupedItems.Add(group);
        }

        private List<(string baseName, string upgradeName)> GetVoucherPairs()
        {
            return new List<(string, string)>
            {
                ("overstock", "overstockplus"),
                ("tarotmerchant", "tarottycoon"),
                ("planetmerchant", "planettycoon"),
                ("clearancesale", "liquidation"),
                ("hone", "glowup"),
                ("grabber", "nachotong"),
                ("wasteful", "recyclomancy"),
                ("blank", "antimatter"),
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

        #region Commands

        public ICommand AddToMustCommand { get; }
        public ICommand AddToMustNotCommand { get; }
        public ICommand RemoveFromMustCommand { get; }
        public ICommand RemoveFromMustNotCommand { get; }
        public ICommand SetEditionCommand { get; }
        public ICommand SetSealCommand { get; }
        public ICommand ToggleStickerPerishableCommand { get; }
        public ICommand ToggleStickerEternalCommand { get; }
        public ICommand ToggleStickerRentalCommand { get; }

        #endregion

        #region Command Implementations

        private void AddToMust(FilterItem? item)
        {
            if (item == null)
                return;

            SelectedMust.Add(item);

            if (_parentViewModel != null)
            {
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

            DebugLogger.Log("ConfigureFilterTab", $"Added {item.Name} to MUST");
            NotifyJsonEditorOfChanges();
        }

        private void AddToMustNot(FilterItem? item)
        {
            if (item == null)
                return;

            SelectedMustNot.Add(item);

            if (_parentViewModel != null)
            {
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

            DebugLogger.Log("ConfigureFilterTab", $"Added {item.Name} to MUST NOT");
            NotifyJsonEditorOfChanges();
        }

        private void RemoveFromMust(FilterItem? item)
        {
            if (item == null)
                return;
            SelectedMust.Remove(item);
            if (_parentViewModel != null)
            {
                RemoveItemFromParent(item, _parentViewModel.SelectedMust);
            }
            DebugLogger.Log("ConfigureFilterTab", $"Removed {item.Name} from MUST");
            NotifyJsonEditorOfChanges();
        }

        private void RemoveFromMustNot(FilterItem? item)
        {
            if (item == null)
                return;
            SelectedMustNot.Remove(item);
            if (_parentViewModel != null)
            {
                RemoveItemFromParent(item, _parentViewModel.SelectedMustNot);
            }
            DebugLogger.Log("ConfigureFilterTab", $"Removed {item.Name} from MUST NOT");
            NotifyJsonEditorOfChanges();
        }

        private void RemoveItemFromParent(
            FilterItem item,
            ObservableCollection<string> parentCollection
        )
        {
            if (_parentViewModel == null)
                return;

            var itemKey = _parentViewModel
                .ItemConfigs.FirstOrDefault(kvp =>
                    kvp.Value.ItemName == item.Name && kvp.Value.ItemType == item.Type
                )
                .Key;

            if (!string.IsNullOrEmpty(itemKey))
            {
                parentCollection.Remove(itemKey);
                _parentViewModel.ItemConfigs.Remove(itemKey);
                DebugLogger.Log(
                    "ConfigureFilterTab",
                    $"Removed {item.Name} from parent collection"
                );
            }
        }

        private void SyncOperatorToParent(FilterOperatorItem operatorItem, string targetZone)
        {
            if (_parentViewModel == null)
                return;

            DebugLogger.Log(
                "ConfigureFilterTab",
                $"Syncing {operatorItem.OperatorType} operator to {targetZone}"
            );

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

            foreach (var child in operatorItem.Children)
            {
                var childConfig = new ItemConfig { ItemType = child.Type, ItemName = child.Name };
                operatorConfig.Children.Add(childConfig);
            }

            _parentViewModel.ItemConfigs[itemKey] = operatorConfig;

            var targetCollection = targetZone switch
            {
                "Must" => _parentViewModel.SelectedMust,
                "MustNot" => _parentViewModel.SelectedMustNot,
                _ => null,
            };

            if (targetCollection != null)
            {
                targetCollection.Add(itemKey);
            }
        }

        private void NotifyJsonEditorOfChanges()
        {
            if (_parentViewModel?.JsonEditorTab is JsonEditorTabViewModel jsonEditorVm)
            {
                jsonEditorVm.AutoGenerateFromVisual();
            }
        }

        private void SetEdition(string? edition)
        {
            if (string.IsNullOrEmpty(edition))
                return;

            SelectedEdition = edition;

            // Only trigger flip animation for Jokers (including Soul Jokers) and Standard Cards
            if (SupportsFlipAnimation)
            {
                FlipAnimationTrigger++; // Trigger awesome flip animation! 🃏
            }

            DebugLogger.Log("ConfigureFilterTab", $"Edition set to: {edition}");
        }

        private void SetSeal(string? seal)
        {
            if (string.IsNullOrEmpty(seal))
                return;

            SelectedSeal = seal;

            // Only trigger flip animation for Standard Cards (seals only apply to standard cards)
            if (SupportsFlipAnimation && SelectedMainCategory == "StandardCard")
            {
                FlipAnimationTrigger++; // Trigger awesome flip animation! 🃏
            }

            DebugLogger.Log("ConfigureFilterTab", $"Seal set to: {seal}");
        }

        private void ToggleStickerPerishable()
        {
            StickerPerishable = !StickerPerishable;

            // Only trigger flip animation for Jokers (stickers only apply to Jokers)
            if (SupportsFlipAnimation && SelectedMainCategory == "Joker")
            {
                FlipAnimationTrigger++; // Trigger awesome flip animation! 🃏
            }

            DebugLogger.Log(
                "ConfigureFilterTab",
                $"Perishable sticker toggled: {StickerPerishable}"
            );
        }

        private void ToggleStickerEternal()
        {
            StickerEternal = !StickerEternal;

            // Only trigger flip animation for Jokers (stickers only apply to Jokers)
            if (SupportsFlipAnimation && SelectedMainCategory == "Joker")
            {
                FlipAnimationTrigger++; // Trigger awesome flip animation! 🃏
            }

            DebugLogger.Log("ConfigureFilterTab", $"Eternal sticker toggled: {StickerEternal}");
        }

        private void ToggleStickerRental()
        {
            StickerRental = !StickerRental;

            // Only trigger flip animation for Jokers (stickers only apply to Jokers)
            if (SupportsFlipAnimation && SelectedMainCategory == "Joker")
            {
                FlipAnimationTrigger++; // Trigger awesome flip animation! 🃏
            }

            DebugLogger.Log("ConfigureFilterTab", $"Rental sticker toggled: {StickerRental}");
        }

        #endregion

        #region Helper Methods

        /// <summary>
        /// Update an item's configuration (called from popup dialog)
        /// </summary>
        public void UpdateItemConfig(string itemKey, ItemConfig config)
        {
            if (ItemConfigs.ContainsKey(itemKey))
            {
                ItemConfigs[itemKey] = config;
                DebugLogger.Log("ConfigureFilterTab", $"Updated config for item: {itemKey}");

                // Also update parent's ItemConfigs if available
                if (_parentViewModel != null && _parentViewModel.ItemConfigs.ContainsKey(itemKey))
                {
                    _parentViewModel.ItemConfigs[itemKey] = config;
                }

                // Trigger auto-sync to JSON Editor
                NotifyJsonEditorOfChanges();
            }
        }

        private async Task LoadGameDataAsync()
        {
            try
            {
                await Task.Run(() => LoadGameData());

                await Avalonia.Threading.Dispatcher.UIThread.InvokeAsync(() =>
                {
                    ApplyFilter();
                    IsLoading = false;
                });
            }
            catch
            {
                await Avalonia.Threading.Dispatcher.UIThread.InvokeAsync(() =>
                {
                    IsLoading = false;
                });
                throw;
            }
        }

        private void LoadGameData()
        {
            // Identical to VisualBuilderTabViewModel - load all game items
            if (!Avalonia.Threading.Dispatcher.UIThread.CheckAccess())
            {
                Avalonia.Threading.Dispatcher.UIThread.Post(LoadGameData);
                return;
            }

            try
            {
                var spriteService = SpriteService.Instance;

                // Load wildcards
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
                                _ => null,
                            },
                        }
                    );
                }

                // Load legendaries
                var legendaryJokers = new[] { "Triboulet", "Yorick", "Chicot", "Perkeo", "Canio" };
                foreach (var legendaryName in legendaryJokers)
                {
                    AllJokers.Add(
                        new FilterItem
                        {
                            Name = legendaryName,
                            Type = "SoulJoker",
                            Category = "Legendary",
                            DisplayName = BalatroData.GetDisplayNameFromSprite(legendaryName),
                            ItemImage = spriteService.GetJokerImage(legendaryName),
                        }
                    );
                }

                // Load regular jokers
                if (BalatroData.Jokers?.Keys != null)
                {
                    foreach (var jokerName in BalatroData.Jokers.Keys)
                    {
                        if (AllJokers.Any(j => j.Name == jokerName))
                            continue;

                        string rarity = "Common";
                        foreach (var rarityKvp in BalatroData.JokersByRarity)
                        {
                            if (rarityKvp.Value.Contains(jokerName.ToLower()))
                            {
                                rarity = rarityKvp.Key;
                                break;
                            }
                        }

                        AllJokers.Add(
                            new FilterItem
                            {
                                Name = jokerName,
                                Type = "Joker",
                                Category = rarity,
                                DisplayName = BalatroData.GetDisplayNameFromSprite(jokerName),
                                ItemImage = spriteService.GetJokerImage(jokerName),
                            }
                        );
                    }
                }

                // Load tags
                if (BalatroData.Tags?.Keys != null)
                {
                    foreach (var tagName in BalatroData.Tags.Keys)
                    {
                        AllTags.Add(
                            new FilterItem
                            {
                                Name = tagName,
                                Type = "SmallBlindTag",
                                DisplayName = BalatroData.GetDisplayNameFromSprite(tagName),
                                ItemImage = spriteService.GetTagImage(tagName),
                            }
                        );
                    }
                }

                // Load vouchers
                if (BalatroData.Vouchers?.Keys != null)
                {
                    foreach (var voucherName in BalatroData.Vouchers.Keys)
                    {
                        AllVouchers.Add(
                            new FilterItem
                            {
                                Name = voucherName,
                                Type = "Voucher",
                                DisplayName = BalatroData.GetDisplayNameFromSprite(voucherName),
                                ItemImage = spriteService.GetVoucherImage(voucherName),
                            }
                        );
                    }
                }

                // Load tarots
                if (BalatroData.TarotCards?.Keys != null)
                {
                    foreach (var tarotName in BalatroData.TarotCards.Keys)
                    {
                        if (tarotName == "any" || tarotName == "*")
                            continue;
                        AllTarots.Add(
                            new FilterItem
                            {
                                Name = tarotName,
                                Type = "Tarot",
                                DisplayName = BalatroData.GetDisplayNameFromSprite(tarotName),
                                ItemImage = spriteService.GetTarotImage(tarotName),
                            }
                        );
                    }
                }

                // Load planets
                if (BalatroData.PlanetCards?.Keys != null)
                {
                    foreach (var planetName in BalatroData.PlanetCards.Keys)
                    {
                        if (planetName == "any" || planetName == "*" || planetName == "anyplanet")
                            continue;
                        AllPlanets.Add(
                            new FilterItem
                            {
                                Name = planetName,
                                Type = "Planet",
                                DisplayName = BalatroData.GetDisplayNameFromSprite(planetName),
                                ItemImage = spriteService.GetPlanetCardImage(planetName),
                            }
                        );
                    }
                }

                // Load spectrals
                if (BalatroData.SpectralCards?.Keys != null)
                {
                    foreach (var spectralName in BalatroData.SpectralCards.Keys)
                    {
                        if (spectralName == "any" || spectralName == "*")
                            continue;
                        AllSpectrals.Add(
                            new FilterItem
                            {
                                Name = spectralName,
                                Type = "Spectral",
                                DisplayName = BalatroData.GetDisplayNameFromSprite(spectralName),
                                ItemImage = spriteService.GetSpectralImage(spectralName),
                            }
                        );
                    }
                }

                // Load bosses
                if (BalatroData.BossBlinds?.Keys != null)
                {
                    foreach (var bossName in BalatroData.BossBlinds.Keys)
                    {
                        AllBosses.Add(
                            new FilterItem
                            {
                                Name = bossName,
                                Type = "Boss",
                                DisplayName = BalatroData.GetDisplayNameFromSprite(bossName),
                                ItemImage = spriteService.GetBossImage(bossName),
                            }
                        );
                    }
                }

                DebugLogger.Log(
                    "ConfigureFilterTab",
                    $"Loaded {AllJokers.Count} jokers, {AllTags.Count} tags, {AllVouchers.Count} vouchers"
                );
            }
            catch (Exception ex)
            {
                DebugLogger.LogError("ConfigureFilterTab", $"Error loading data: {ex.Message}");
            }

            ApplyFilter();
        }

        private void ApplyFilter()
        {
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

            RebuildGroupedItems();
        }

        public void RemoveItem(FilterItem item)
        {
            string? sourceZone = null;
            string? itemKeyToRemove = null;
            int itemIndex = -1;

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
            else
            {
                itemIndex = SelectedMustNot.IndexOf(item);
                if (itemIndex >= 0)
                {
                    sourceZone = "MUSTNOT";
                    if (
                        _parentViewModel != null
                        && itemIndex < _parentViewModel.SelectedMustNot.Count
                    )
                    {
                        itemKeyToRemove = _parentViewModel.SelectedMustNot[itemIndex];
                    }
                    SelectedMustNot.RemoveAt(itemIndex);
                }
            }

            if (!string.IsNullOrEmpty(itemKeyToRemove) && _parentViewModel != null)
            {
                ItemConfigs.Remove(itemKeyToRemove);
                _parentViewModel.ItemConfigs.Remove(itemKeyToRemove);

                if (sourceZone == "MUST")
                {
                    _parentViewModel.SelectedMust.Remove(itemKeyToRemove);
                }
                else if (sourceZone == "MUSTNOT")
                {
                    _parentViewModel.SelectedMustNot.Remove(itemKeyToRemove);
                }
            }

            DebugLogger.Log(
                "ConfigureFilterTab",
                $"Removed item: {item.Name} from {sourceZone ?? "UNKNOWN"} zone"
            );
        }

        #endregion

        #region Auto-Save Functionality

        private void OnZoneCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
        {
            TriggerAutoSave();
        }

        private void TriggerAutoSave()
        {
            _autoSaveCts?.Cancel();
            _autoSaveCts?.Dispose();
            _autoSaveCts = new CancellationTokenSource();

            var token = _autoSaveCts.Token;

            Task.Run(async () =>
            {
                try
                {
                    await Task.Delay(AutoSaveDebounceMs, token);
                    if (!token.IsCancellationRequested)
                    {
                        await PerformAutoSave();
                    }
                }
                catch (TaskCanceledException) { }
                catch (Exception ex)
                {
                    DebugLogger.LogError("ConfigureFilterTab", $"Auto-save error: {ex.Message}");
                }
            });
        }

        private async Task PerformAutoSave()
        {
            if (_parentViewModel == null)
                return;

            try
            {
                await Dispatcher.UIThread.InvokeAsync(() =>
                {
                    IsAutoSaving = true;
                    AutoSaveStatus = "Auto-saving...";
                });

                var filterName = _parentViewModel.FilterName;

                if (string.IsNullOrWhiteSpace(filterName))
                {
                    await Dispatcher.UIThread.InvokeAsync(() =>
                    {
                        IsAutoSaving = false;
                        AutoSaveStatus = "";
                    });
                    return;
                }

                var config = _parentViewModel.BuildConfigFromCurrentState();
                var configService = ServiceHelper.GetService<IConfigurationService>();

                if (configService == null)
                    return;

                var filePath = System.IO.Path.Combine(
                    configService.GetFiltersDirectory(),
                    $"{filterName.Replace(" ", "_")}.json"
                );
                var success = await configService.SaveFilterAsync(filePath, config);

                await Dispatcher.UIThread.InvokeAsync(() =>
                {
                    IsAutoSaving = false;
                    if (success)
                    {
                        AutoSaveStatus = "Auto-saved";
                        Task.Delay(2000)
                            .ContinueWith(_ =>
                            {
                                Dispatcher.UIThread.InvokeAsync(() => AutoSaveStatus = "");
                            });
                    }
                    else
                    {
                        AutoSaveStatus = "Auto-save failed";
                    }
                });
            }
            catch (Exception ex)
            {
                DebugLogger.LogError("ConfigureFilterTab", $"Auto-save exception: {ex.Message}");
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

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
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
    /// <summary>
    /// UI state enum for Phase 2 state system
    /// Controls which parts of the UI are visible and how they're displayed
    /// </summary>
    public enum EditingState
    {
        Default,        // Normal view - all sections visible at normal size
        DragActive,     // User is dragging - show overlays
        ClauseEdit,     // Editing OR/AND clause - expand to full column
        ScoreEdit       // Editing score list - expand to full column
    }

    /// <summary>
    /// ViewModel for Visual Builder tab.
    /// Refactored to use FilterTabViewModelBase - eliminates ~800 lines of duplicate code.
    /// Retains all tab-specific features: edition/sticker/seal buttons, clause editing, flip animations.
    /// </summary>
    public partial class VisualBuilderTabViewModel : FilterTabViewModelBase
    {
        // Auto-save status properties (tab-specific)
        [ObservableProperty]
        private string _autoSaveStatus = "";

        [ObservableProperty]
        private bool _isAutoSaving = false;

        // Phase 2: UI State Management
        [ObservableProperty]
        private EditingState _currentEditingState = EditingState.Default;

        // Computed properties for state-based visibility
        public bool IsDefaultState => CurrentEditingState == EditingState.Default;
        public bool IsDragActiveState => CurrentEditingState == EditingState.DragActive;
        public bool IsClauseEditState => CurrentEditingState == EditingState.ClauseEdit;
        public bool IsScoreEditState => CurrentEditingState == EditingState.ScoreEdit;

        // Computed properties for layout control
        public bool ShouldShowItemShelf => CurrentEditingState == EditingState.Default || CurrentEditingState == EditingState.DragActive;
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

        // Hover state tracking for drop-zones
        [ObservableProperty]
        private bool _isMustHovered = false;

        [ObservableProperty]
        private bool _isShouldHovered = false;

        [ObservableProperty]
        private bool _isCantHovered = false;

        // Expose parent's SelectedDeck for flip animation
        public string SelectedDeck => _parentViewModel?.SelectedDeck ?? "Red";

        // Subcategory tracking within each main category
        [ObservableProperty]
        private string _selectedCategory = "Legendary";

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

        // Flip Animation Trigger - incremented whenever edition/sticker/seal changes
        [ObservableProperty]
        private int _flipAnimationTrigger = 0;

        // Legacy properties (kept for compatibility)
        [ObservableProperty]
        private string _currentEdition = "None";

        [ObservableProperty]
        private string _currentEnhancement = "None";

        [ObservableProperty]
        private string _currentSeal = "None";

        // Computed properties for button visibility based on category
        // Editions (Foil, Holographic, Polychrome, Negative) - JOKERS ONLY
        public bool ShowEditionButtons => SelectedMainCategory == "Joker";
        // Stickers (Eternal, Perishable, Rental) - JOKERS ONLY
        public bool ShowStickerButtons => SelectedMainCategory == "Joker";
        // Seals (Red, Blue, Gold, Purple) - STANDARD CARDS ONLY
        public bool ShowSealButtons => SelectedMainCategory == "StandardCard";
        // Enhancements (Bonus, Mult, Wild, Lucky, Glass, Steel, Stone, Gold) - STANDARD CARDS ONLY
        public bool ShowEnhancementButtons => SelectedMainCategory == "StandardCard";
        public bool SupportsFlipAnimation => SelectedMainCategory == "Joker" || SelectedMainCategory == "StandardCard";

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

        public VisualBuilderTabViewModel(
            FiltersModalViewModel? parentViewModel,
            IFilterItemDataService dataService,
            IFilterItemFilterService filterService)
            : base(parentViewModel, dataService, filterService)
        {
            // Initialize categories with proper data template approach
            Categories = new ObservableCollection<CategoryViewModel>
            {
                new() { Name = "Legendary", DisplayName = "Legendary", Items = FilteredJokers },
                new() { Name = "Rare", DisplayName = "Rare", Items = FilteredJokers },
                new() { Name = "Uncommon", DisplayName = "Uncommon", Items = FilteredJokers },
                new() { Name = "Common", DisplayName = "Common", Items = FilteredJokers },
                new() { Name = "Voucher", DisplayName = "Voucher", Items = FilteredVouchers },
                new() { Name = "Tarot", DisplayName = "Tarot", Items = FilteredTarots },
                new() { Name = "Planet", DisplayName = "Planet", Items = FilteredPlanets },
                new() { Name = "Spectral", DisplayName = "Spectral", Items = FilteredSpectrals },
                new() { Name = "Tag", DisplayName = "Tag", Items = FilteredTags },
                new() { Name = "Boss", DisplayName = "Boss", Items = FilteredBosses },
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

            CommitOrClauseCommand = new RelayCommand(CommitOrClause);
            CommitAndClauseCommand = new RelayCommand(CommitAndClause);

            // Simple property change handling
            PropertyChanged += (s, e) =>
            {
                if (e.PropertyName == nameof(SelectedCategory))
                {
                    RefreshFilteredItems();
                }
                else if (e.PropertyName == nameof(SelectedMainCategory))
                {
                    // Notify computed properties when main category changes
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

                    DebugLogger.Log("VisualBuilderTab", $"Category changed to: {SelectedMainCategory} - reset all editions/stickers/seals");
                }
            };
        }

        #region Category-Specific GroupedItems Override

        /// <summary>
        /// Override RebuildGroupedItems to add stagger delays for flip animation
        /// </summary>
        protected override void RebuildGroupedItems()
        {
            // Call base implementation
            base.RebuildGroupedItems();

            // Apply stagger delays for flip animation (20ms between each card for smooth wave effect)
            foreach (var group in GroupedItems)
            {
                int delayCounter = 0;
                foreach (var item in group.Items)
                {
                    item.StaggerDelay = delayCounter * 20; // 20ms stagger between cards
                    delayCounter++;
                }
            }
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
                    AllJokers, AllVouchers, AllTarots, AllPlanets, AllSpectrals, AllTags, AllBosses,
                };

                foreach (var collection in allCollections)
                {
                    foreach (var item in collection)
                    {
                        if (item.Name?.Contains(SearchFilter, StringComparison.OrdinalIgnoreCase) == true ||
                            item.DisplayName?.Contains(SearchFilter, StringComparison.OrdinalIgnoreCase) == true)
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
                "Uncommon" => FilteredJokers.Where(j => j.Type == "Joker" && j.Category == "Uncommon"),
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

        #endregion

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

        // Commit clause commands
        public ICommand CommitOrClauseCommand { get; }
        public ICommand CommitAndClauseCommand { get; }

        #endregion

        #region Command Implementations

        private void AddToMust(FilterItem? item)
        {
            if (item == null) return;

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

                    // Phase 3: Apply currently selected edition/stickers/seal
                    ApplyEditionStickersSeal(itemConfig, item);

                    _parentViewModel.ItemConfigs[itemKey] = itemConfig;
                    _parentViewModel.SelectedMust.Add(itemKey);
                }
            }

            DebugLogger.Log("VisualBuilderTab", $"Added {item.Name} to MUST");
            NotifyJsonEditorOfChanges();
        }

        private void AddToShould(FilterItem? item)
        {
            if (item == null) return;

            SelectedShould.Add(item);

            if (_parentViewModel != null)
            {
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

                    ApplyEditionStickersSeal(itemConfig, item);

                    _parentViewModel.ItemConfigs[itemKey] = itemConfig;
                    _parentViewModel.SelectedShould.Add(itemKey);
                }
            }

            DebugLogger.Log("VisualBuilderTab", $"Added {item.Name} to SHOULD");
            NotifyJsonEditorOfChanges();
        }

        private void AddToMustNot(FilterItem? item)
        {
            if (item == null) return;

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

                    ApplyEditionStickersSeal(itemConfig, item);

                    _parentViewModel.ItemConfigs[itemKey] = itemConfig;
                    _parentViewModel.SelectedMustNot.Add(itemKey);
                }
            }

            DebugLogger.Log("VisualBuilderTab", $"Added {item.Name} to MUST NOT");
            NotifyJsonEditorOfChanges();
        }

        private void RemoveFromMust(FilterItem? item)
        {
            if (item == null) return;

            SelectedMust.Remove(item);

            if (_parentViewModel != null)
            {
                RemoveItemFromParent(item, _parentViewModel.SelectedMust);
            }

            DebugLogger.Log("VisualBuilderTab", $"Removed {item.Name} from MUST");
            NotifyJsonEditorOfChanges();
        }

        private void RemoveFromShould(FilterItem? item)
        {
            if (item == null) return;

            SelectedShould.Remove(item);

            if (_parentViewModel != null)
            {
                RemoveItemFromParent(item, _parentViewModel.SelectedShould);
            }

            DebugLogger.Log("VisualBuilderTab", $"Removed {item.Name} from SHOULD");
            NotifyJsonEditorOfChanges();
        }

        private void RemoveFromMustNot(FilterItem? item)
        {
            if (item == null) return;

            SelectedMustNot.Remove(item);

            if (_parentViewModel != null)
            {
                RemoveItemFromParent(item, _parentViewModel.SelectedMustNot);
            }

            DebugLogger.Log("VisualBuilderTab", $"Removed {item.Name} from MUST NOT");
            NotifyJsonEditorOfChanges();
        }

        private void AddToOrTray(FilterItem? item)
        {
            if (item == null) return;

            if (OrTrayItems.Count == 0)
            {
                CurrentEditingState = EditingState.ClauseEdit;
                EditingClauseType = "Or";
                DebugLogger.Log("VisualBuilderTab", "Entered OR clause editing mode");
            }

            OrTrayItems.Add(item);

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
            if (item == null) return;

            if (AndTrayItems.Count == 0)
            {
                CurrentEditingState = EditingState.ClauseEdit;
                EditingClauseType = "And";
                DebugLogger.Log("VisualBuilderTab", "Entered AND clause editing mode");
            }

            AndTrayItems.Add(item);

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
            if (item == null) return;

            OrTrayItems.Remove(item);
            RemoveFromShould(item);

            DebugLogger.Log("VisualBuilderTab", $"Removed {item.Name} from OR tray");
        }

        private void RemoveFromAndTray(FilterItem? item)
        {
            if (item == null) return;

            AndTrayItems.Remove(item);
            RemoveFromShould(item);

            DebugLogger.Log("VisualBuilderTab", $"Removed {item.Name} from AND tray");
        }

        private void CommitOrClause()
        {
            if (OrTrayItems.Count == 0)
            {
                DebugLogger.Log("VisualBuilderTab", "Cannot commit empty OR clause");
                return;
            }

            var groupedConfig = new ItemConfig
            {
                ItemKey = $"or_clause_{Guid.NewGuid():N}",
                ItemType = "Clause",
                ItemName = $"OR ({OrTrayItems.Count} items)",
                OperatorType = "Or",
                Mode = "Max",
                Children = new List<ItemConfig>()
            };

            if (_parentViewModel != null)
            {
                foreach (var item in OrTrayItems.ToList())
                {
                    var existingConfig = _parentViewModel.ItemConfigs
                        .FirstOrDefault(kvp => kvp.Value.ItemName == item.Name && kvp.Value.ItemType == item.Type);

                    if (existingConfig.Value != null)
                    {
                        groupedConfig.Children.Add(existingConfig.Value);
                    }
                }
            }

            var operatorItem = new FilterOperatorItem("OR")
            {
                DisplayName = $"OR ({OrTrayItems.Count} items)"
            };

            foreach (var item in OrTrayItems.ToList())
            {
                operatorItem.Children.Add(item);
            }

            SelectedShould.Add(operatorItem);

            if (_parentViewModel != null)
            {
                _parentViewModel.ItemConfigs[groupedConfig.ItemKey] = groupedConfig;
                _parentViewModel.SelectedShould.Add(groupedConfig.ItemKey);
            }

            OrTrayItems.Clear();
            CurrentEditingState = EditingState.Default;
            EditingClauseType = "";

            DebugLogger.Log("VisualBuilderTab", $"Committed OR clause with {groupedConfig.Children.Count} children to SHOULD");
            NotifyJsonEditorOfChanges();
        }

        private void CommitAndClause()
        {
            if (AndTrayItems.Count == 0)
            {
                DebugLogger.Log("VisualBuilderTab", "Cannot commit empty AND clause");
                return;
            }

            var groupedConfig = new ItemConfig
            {
                ItemKey = $"and_clause_{Guid.NewGuid():N}",
                ItemType = "Clause",
                ItemName = $"AND ({AndTrayItems.Count} items)",
                OperatorType = "And",
                Children = new List<ItemConfig>()
            };

            if (_parentViewModel != null)
            {
                foreach (var item in AndTrayItems.ToList())
                {
                    var existingConfig = _parentViewModel.ItemConfigs
                        .FirstOrDefault(kvp => kvp.Value.ItemName == item.Name && kvp.Value.ItemType == item.Type);

                    if (existingConfig.Value != null)
                    {
                        groupedConfig.Children.Add(existingConfig.Value);
                    }
                }
            }

            var operatorItem = new FilterOperatorItem("AND")
            {
                DisplayName = $"AND ({AndTrayItems.Count} items)"
            };

            foreach (var item in AndTrayItems.ToList())
            {
                operatorItem.Children.Add(item);
            }

            SelectedShould.Add(operatorItem);

            if (_parentViewModel != null)
            {
                _parentViewModel.ItemConfigs[groupedConfig.ItemKey] = groupedConfig;
                _parentViewModel.SelectedShould.Add(groupedConfig.ItemKey);
            }

            AndTrayItems.Clear();
            CurrentEditingState = EditingState.Default;
            EditingClauseType = "";

            DebugLogger.Log("VisualBuilderTab", $"Committed AND clause with {groupedConfig.Children.Count} children to SHOULD");
            NotifyJsonEditorOfChanges();
        }

        private void RemoveItemFromParent(FilterItem item, ObservableCollection<string> parentCollection)
        {
            if (_parentViewModel == null) return;

            var itemKey = _parentViewModel.ItemConfigs.FirstOrDefault(kvp =>
                kvp.Value.ItemName == item.Name &&
                kvp.Value.ItemType == item.Type).Key;

            if (!string.IsNullOrEmpty(itemKey))
            {
                parentCollection.Remove(itemKey);
                _parentViewModel.ItemConfigs.Remove(itemKey);
                DebugLogger.Log("VisualBuilderTab", $"Removed {item.Name} from parent collection");
            }
        }

        private void SyncOperatorToParent(FilterOperatorItem operatorItem, string targetZone)
        {
            if (_parentViewModel == null) return;

            DebugLogger.Log("VisualBuilderTab",
                $"Syncing {operatorItem.OperatorType} operator with {operatorItem.Children.Count} children to {targetZone}");

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
                var childConfig = new ItemConfig
                {
                    ItemType = child.Type,
                    ItemName = child.Name,
                };
                operatorConfig.Children.Add(childConfig);
            }

            _parentViewModel.ItemConfigs[itemKey] = operatorConfig;

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

        private void NotifyJsonEditorOfChanges()
        {
            if (_parentViewModel?.JsonEditorTab is JsonEditorTabViewModel jsonEditorVm)
            {
                jsonEditorVm.AutoGenerateFromVisual();
            }
        }

        #endregion

        #region Helper Methods

        public void UpdateItemConfig(string itemKey, ItemConfig config)
        {
            ItemConfigs[itemKey] = config;

            if (_parentViewModel != null && _parentViewModel.ItemConfigs.ContainsKey(itemKey))
            {
                _parentViewModel.ItemConfigs[itemKey] = config;
            }

            DebugLogger.Log("VisualBuilderTab", $"Updated config for item: {itemKey}");
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

            if (!string.IsNullOrEmpty(itemKeyToRemove) && _parentViewModel != null)
            {
                ItemConfigs.Remove(itemKeyToRemove);
                _parentViewModel.ItemConfigs.Remove(itemKeyToRemove);

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

        #region Auto-Save Override

        protected override async Task PerformAutoSave()
        {
            if (_parentViewModel == null) return;

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
                    DebugLogger.Log("VisualBuilderTab", "Auto-save skipped: filter name is empty");
                    return;
                }

                var config = _parentViewModel.BuildConfigFromCurrentState();
                var configService = ServiceHelper.GetService<IConfigurationService>();
                var filterService = ServiceHelper.GetService<IFilterService>();

                if (configService == null || filterService == null)
                {
                    DebugLogger.LogError("VisualBuilderTab", "Auto-save failed: services not available");
                    return;
                }

                var filePath = filterService.GenerateFilterFileName(filterName);

                if (filterName.Contains(' '))
                {
                    var oldFilePath = Path.Combine(configService.GetFiltersDirectory(), $"{filterName}.json");
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

                var success = await configService.SaveFilterAsync(filePath, config);

                await Dispatcher.UIThread.InvokeAsync(() =>
                {
                    IsAutoSaving = false;
                    if (success)
                    {
                        AutoSaveStatus = "Auto-saved";
                        DebugLogger.Log("VisualBuilderTab", $"Auto-saved filter: {filterName}");

                        Task.Delay(2000).ContinueWith(_ =>
                        {
                            Dispatcher.UIThread.InvokeAsync(() => AutoSaveStatus = "");
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

        [RelayCommand]
        public void EnterDefaultState()
        {
            CurrentEditingState = EditingState.Default;
            IsDragging = false;
        }

        public void EnterDragActiveState()
        {
            CurrentEditingState = EditingState.DragActive;
            IsDragging = true;
        }

        [RelayCommand]
        public void EnterClauseEditState()
        {
            CurrentEditingState = EditingState.ClauseEdit;
        }

        [RelayCommand]
        public void EnterScoreEditState()
        {
            CurrentEditingState = EditingState.ScoreEdit;
        }

        [RelayCommand]
        public void ExitEditingState()
        {
            CurrentEditingState = EditingState.Default;
        }

        #endregion

        #region Phase 3: Edition/Sticker/Seal Commands

        [RelayCommand]
        public void SetEdition(string edition)
        {
            if (SelectedEdition == edition)
            {
                DebugLogger.Log("VisualBuilderTab", $"Edition already set to '{edition}' - skipping animation");
                return;
            }

            SelectedEdition = edition;

            foreach (var group in GroupedItems)
            {
                foreach (var item in group.Items)
                {
                    if (_parentViewModel != null && _parentViewModel.ItemConfigs.TryGetValue(item.ItemKey, out var config))
                    {
                        config.Edition = edition == "None" ? null : edition.ToLower();
                        item.Edition = config.Edition;
                    }
                }
            }

            if (_parentViewModel != null)
            {
                foreach (var itemKey in _parentViewModel.SelectedMust.Concat(_parentViewModel.SelectedShould).Concat(_parentViewModel.SelectedMustNot))
                {
                    if (_parentViewModel.ItemConfigs.TryGetValue(itemKey, out var config))
                    {
                        config.Edition = edition == "None" ? null : edition.ToLower();
                    }
                }
                TriggerAutoSave();
            }

            if (SupportsFlipAnimation)
            {
                FlipAnimationTrigger++;
            }

            DebugLogger.Log("VisualBuilderTab", $"Edition changed to: {edition} (FlipTrigger={FlipAnimationTrigger}, SupportsFlip={SupportsFlipAnimation})");
        }

        [RelayCommand]
        public void ToggleStickerPerishable()
        {
            StickerPerishable = !StickerPerishable;
            ApplyStickersToAllItems();

            if (SupportsFlipAnimation)
            {
                FlipAnimationTrigger++;
            }

            DebugLogger.Log("VisualBuilderTab", $"Perishable sticker: {StickerPerishable} (FlipTrigger={FlipAnimationTrigger})");
        }

        [RelayCommand]
        public void ToggleStickerEternal()
        {
            StickerEternal = !StickerEternal;
            ApplyStickersToAllItems();

            if (SupportsFlipAnimation)
            {
                FlipAnimationTrigger++;
            }

            DebugLogger.Log("VisualBuilderTab", $"Eternal sticker: {StickerEternal} (FlipTrigger={FlipAnimationTrigger})");
        }

        [RelayCommand]
        public void ToggleStickerRental()
        {
            StickerRental = !StickerRental;
            ApplyStickersToAllItems();

            if (SupportsFlipAnimation)
            {
                FlipAnimationTrigger++;
            }

            DebugLogger.Log("VisualBuilderTab", $"Rental sticker: {StickerRental} (FlipTrigger={FlipAnimationTrigger})");
        }

        [RelayCommand]
        public void SetSeal(string seal)
        {
            if (SelectedSeal == seal)
            {
                DebugLogger.Log("VisualBuilderTab", $"Seal already set to '{seal}' - skipping animation");
                return;
            }

            SelectedSeal = seal;

            if (_parentViewModel == null) return;

            foreach (var group in GroupedItems)
            {
                foreach (var item in group.Items)
                {
                    if (_parentViewModel.ItemConfigs.TryGetValue(item.ItemKey, out var config))
                    {
                        if (config.ItemType == "StandardCard")
                        {
                            config.Seal = seal == "None" ? null : seal;
                        }
                    }
                }
            }

            foreach (var itemKey in _parentViewModel.SelectedMust.Concat(_parentViewModel.SelectedShould).Concat(_parentViewModel.SelectedMustNot))
            {
                if (_parentViewModel.ItemConfigs.TryGetValue(itemKey, out var config))
                {
                    if (config.ItemType == "StandardCard")
                    {
                        config.Seal = seal == "None" ? null : seal;
                    }
                }
            }

            TriggerAutoSave();

            if (SupportsFlipAnimation)
            {
                FlipAnimationTrigger++;
            }

            DebugLogger.Log("VisualBuilderTab", $"Seal changed to: {seal} (FlipTrigger={FlipAnimationTrigger})");
        }

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
                _ => false
            };
        }

        private void ApplyStickersToAllItems()
        {
            if (_parentViewModel == null) return;

            var eternalRestrictedJokers = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                "Cavendish", "DietCola", "GrosMichel", "IceCream", "InvisibleJoker",
                "Luchador", "MrBones", "Popcorn", "Ramen", "Seltzer", "TurtleBean",
                "Perkeo", "Triboulet", "Yorick", "Chicot", "Canio"
            };

            foreach (var group in GroupedItems)
            {
                foreach (var item in group.Items)
                {
                    if (_parentViewModel.ItemConfigs.TryGetValue(item.ItemKey, out var config))
                    {
                        ApplyStickerLogic(config, item.Name, eternalRestrictedJokers);
                        item.Stickers = config.Stickers;
                    }
                }
            }

            foreach (var itemKey in _parentViewModel.SelectedMust.Concat(_parentViewModel.SelectedShould).Concat(_parentViewModel.SelectedMustNot))
            {
                if (_parentViewModel.ItemConfigs.TryGetValue(itemKey, out var config))
                {
                    ApplyStickerLogic(config, config.ItemName, eternalRestrictedJokers);
                }
            }

            TriggerAutoSave();
        }

        private void ApplyStickerLogic(ItemConfig config, string itemName, HashSet<string> eternalRestrictedJokers)
        {
            var stickers = new List<string>();

            if (StickerPerishable)
            {
                stickers.Add("perishable");
            }
            else if (StickerEternal)
            {
                bool canTypeBeEternal = config.ItemType switch
                {
                    "Joker" or "SoulJoker" => true,
                    "Tarot" or "Planet" or "Spectral" => true,
                    "Voucher" => true,
                    "StandardCard" => true,
                    "SmallBlindTag" or "BigBlindTag" => false,
                    "Boss" => false,
                    _ => false
                };

                bool isRestrictedJoker = (config.ItemType == "Joker" || config.ItemType == "SoulJoker")
                    && eternalRestrictedJokers.Contains(itemName);

                if (canTypeBeEternal && !isRestrictedJoker)
                {
                    stickers.Add("eternal");
                }
            }

            if (StickerRental && !StickerPerishable)
            {
                stickers.Add("rental");
            }

            config.Stickers = stickers.Any() ? stickers : null;
        }

        private void ApplyEditionStickersSeal(ItemConfig config, FilterItem item)
        {
            if (SelectedEdition != "None")
            {
                config.Edition = SelectedEdition.ToLower();
            }

            var eternalRestrictedJokers = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                "Cavendish", "DietCola", "GrosMichel", "IceCream", "InvisibleJoker",
                "Luchador", "MrBones", "Popcorn", "Ramen", "Seltzer", "TurtleBean",
                "Perkeo", "Triboulet", "Yorick", "Chicot", "Canio"
            };

            var stickers = new List<string>();

            if (StickerPerishable)
            {
                stickers.Add("perishable");
            }
            else if (StickerEternal && CanItemBeEternal(item))
            {
                bool isRestrictedJoker = (item.Type == "Joker" || item.Type == "SoulJoker")
                    && eternalRestrictedJokers.Contains(item.Name);

                if (!isRestrictedJoker)
                {
                    stickers.Add("eternal");
                }
            }

            if (StickerRental && !StickerPerishable)
            {
                stickers.Add("rental");
            }

            if (stickers.Any())
            {
                config.Stickers = stickers;
            }

            if (item.Type == "StandardCard" && SelectedSeal != "None")
            {
                config.Seal = SelectedSeal;
            }

            DebugLogger.Log("VisualBuilderTab",
                $"Applied edition={config.Edition}, stickers=[{string.Join(",", config.Stickers ?? new List<string>())}], seal={config.Seal} to {item.Name}");
        }

        #endregion
    }
}

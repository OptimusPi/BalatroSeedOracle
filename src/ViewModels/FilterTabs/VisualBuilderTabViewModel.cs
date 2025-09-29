using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.Input;
using BalatroSeedOracle.Models;
using BalatroSeedOracle.Controls;
using BalatroSeedOracle.Helpers;
using BalatroSeedOracle.Services;

namespace BalatroSeedOracle.ViewModels.FilterTabs
{
    public class VisualBuilderTabViewModel : BaseViewModel
    {
        private readonly FiltersModalViewModel? _parentViewModel;
        private string _searchFilter = "";
        private string _currentCategory = "Legendary";
        private string _currentCategoryDisplay = "Legendary";

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
        
        // Unified filtered items for current category
        public ObservableCollection<FilterItem> FilteredItems { get; }

        public string CurrentCategoryDisplay
        {
            get => _currentCategoryDisplay;
            set => SetProperty(ref _currentCategoryDisplay, value);
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

            // Defer loading sample data to UI thread with a small delay to ensure UI is ready
            Dispatcher.UIThread.Post(async () => {
                try 
                {
                    // Small delay to ensure UI is fully initialized
                    await Task.Delay(100);
                    
                    // Ensure SpriteService is initialized
                    var _ = SpriteService.Instance;
                    
                    LoadSampleData();
                    ApplyFilter();
                }
                catch (Exception ex)
                {
                    DebugLogger.LogError("VisualBuilderTab", $"Error loading sample data: {ex.Message}");
                }
            });
        }

        public void SetCategory(string category)
        {
            // Ensure UI thread safety to prevent binding cascade errors
            if (!Avalonia.Threading.Dispatcher.UIThread.CheckAccess())
            {
                Avalonia.Threading.Dispatcher.UIThread.Post(() => SetCategory(category));
                return;
            }
            
            _currentCategory = category;
            CurrentCategoryDisplay = category;
            RefreshFilteredItems();
        }

        private void RefreshFilteredItems()
        {
            // Ensure we're on UI thread to avoid binding errors
            if (!Avalonia.Threading.Dispatcher.UIThread.CheckAccess())
            {
                Avalonia.Threading.Dispatcher.UIThread.Post(RefreshFilteredItems);
                return;
            }

            FilteredItems.Clear();

            var sourceCollection = _currentCategory switch
            {
                "Legendary" or "Rare" or "Uncommon" or "Common" => FilteredJokers,
                "Voucher" => FilteredVouchers,
                "Tarot" => FilteredTarots,
                "Planet" => FilteredPlanets,
                "Spectral" => FilteredSpectrals,
                "Tag" => FilteredTags,
                "Boss" => FilteredBosses,
                "Favorites" => FilteredJokers,
                _ => FilteredJokers
            };

            var filteredItems = string.IsNullOrEmpty(_searchFilter)
                ? sourceCollection
                : sourceCollection.Where(item => item.Name?.Contains(_searchFilter, StringComparison.OrdinalIgnoreCase) == true ||
                                                  item.DisplayName?.Contains(_searchFilter, StringComparison.OrdinalIgnoreCase) == true);

            foreach (var item in filteredItems)
            {
                FilteredItems.Add(item);
            }
        }

        #region Properties

        public string SearchFilter
        {
            get => _searchFilter;
            set
            {
                if (SetProperty(ref _searchFilter, value))
                {
                    ApplyFilter();
                }
            }
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
            if (item != null && !SelectedMust.Any(x => x.Name == item.Name))
            {
                SelectedMust.Add(item);
                
                // Sync with parent ViewModel if available
                if (_parentViewModel != null)
                {
                    var itemKey = _parentViewModel.GenerateNextItemKey();
                    var itemConfig = new ItemConfig
                    {
                        ItemKey = itemKey,
                        ItemType = item.Type,
                        ItemName = item.Name
                    };
                    _parentViewModel.ItemConfigs[itemKey] = itemConfig;
                    _parentViewModel.SelectedMust.Add(itemKey);
                }
                
                DebugLogger.Log("VisualBuilderTab", $"Added {item.Name} to MUST");
            }
        }

        private void AddToShould(FilterItem? item)
        {
            if (item != null && !SelectedShould.Any(x => x.Name == item.Name))
            {
                SelectedShould.Add(item);
                DebugLogger.Log("VisualBuilderTab", $"Added {item.Name} to SHOULD");
            }
        }

        private void AddToMustNot(FilterItem? item)
        {
            if (item != null && !SelectedMustNot.Any(x => x.Name == item.Name))
            {
                SelectedMustNot.Add(item);
                DebugLogger.Log("VisualBuilderTab", $"Added {item.Name} to MUST NOT");
            }
        }

        private void RemoveFromMust(FilterItem? item)
        {
            if (item != null)
            {
                SelectedMust.Remove(item);
                DebugLogger.Log("VisualBuilderTab", $"Removed {item.Name} from MUST");
            }
        }

        private void RemoveFromShould(FilterItem? item)
        {
            if (item != null)
            {
                SelectedShould.Remove(item);
                DebugLogger.Log("VisualBuilderTab", $"Removed {item.Name} from SHOULD");
            }
        }

        private void RemoveFromMustNot(FilterItem? item)
        {
            if (item != null)
            {
                SelectedMustNot.Remove(item);
                DebugLogger.Log("VisualBuilderTab", $"Removed {item.Name} from MUST NOT");
            }
        }

        #endregion

        #region Helper Methods

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
                        ItemImage = spriteService.GetJokerImage(fav)
                    };
                    AllJokers.Add(item);
                }

                // Load Soul Jokers FIRST (they appear at top as legendary)
                var legendaryJokers = new[] { "Triboulet", "Yorick", "Chicot", "Perkeo", "Canio" };
                foreach (var legendaryName in legendaryJokers)
                {
                    var item = new FilterItem 
                    { 
                        Name = legendaryName, 
                        Type = "SoulJoker",
                        Category = "Legendary",
                        DisplayName = FormatDisplayName(legendaryName),
                        ItemImage = spriteService.GetJokerImage(legendaryName)
                    };
                    AllJokers.Add(item);
                }

                // Load Regular Jokers from BalatroData
                if (BalatroData.Jokers?.Keys != null)
                {
                    foreach (var jokerName in BalatroData.Jokers.Keys)
                    {
                        // Skip if already added from favorites or legendaries
                        if (AllJokers.Any(j => j.Name == jokerName))
                            continue;
                            
                        var item = new FilterItem 
                        { 
                            Name = jokerName, 
                            Type = "Joker",
                            DisplayName = FormatDisplayName(jokerName),
                            ItemImage = spriteService.GetJokerImage(jokerName) // null is OK if sprite missing
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
                            DisplayName = FormatDisplayName(tagName),
                            ItemImage = spriteService.GetTagImage(tagName)
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
                            DisplayName = FormatDisplayName(voucherName),
                            ItemImage = spriteService.GetVoucherImage(voucherName)
                        };
                        AllVouchers.Add(item);
                    }
                }

                // Load Tarots from BalatroData
                if (BalatroData.TarotCards?.Keys != null)
                {
                    foreach (var tarotName in BalatroData.TarotCards.Keys)
                    {
                        var item = new FilterItem 
                        { 
                            Name = tarotName, 
                            Type = "Tarot",
                            DisplayName = FormatDisplayName(tarotName),
                            ItemImage = spriteService.GetTarotImage(tarotName)
                        };
                        AllTarots.Add(item);
                    }
                }
                
                // Load Planets from BalatroData
                try
                {
                    if (BalatroData.PlanetCards?.Keys != null)
                    {
                        DebugLogger.Log("VisualBuilderTab", $"Loading {BalatroData.PlanetCards.Keys.Count} planets");
                        foreach (var planetName in BalatroData.PlanetCards.Keys)
                        {
                            try
                            {
                                var item = new FilterItem 
                                { 
                                    Name = planetName, 
                                    Type = "Planet",
                                    DisplayName = FormatDisplayName(planetName),
                                    ItemImage = spriteService.GetPlanetCardImage(planetName)
                                };
                                AllPlanets.Add(item);
                            }
                            catch (Exception ex)
                            {
                                DebugLogger.LogError("VisualBuilderTab", $"Error loading planet {planetName}: {ex.Message}");
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    DebugLogger.LogError("VisualBuilderTab", $"Error loading planets: {ex.Message}");
                }
                
                // Load Spectrals from BalatroData
                if (BalatroData.SpectralCards?.Keys != null)
                {
                    foreach (var spectralName in BalatroData.SpectralCards.Keys)
                    {
                        var item = new FilterItem 
                        { 
                            Name = spectralName, 
                            Type = "Spectral",
                            DisplayName = FormatDisplayName(spectralName),
                            ItemImage = spriteService.GetSpectralImage(spectralName)
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
                                    DisplayName = FormatDisplayName(bossName),
                                    ItemImage = spriteService.GetBossImage(bossName)
                                };
                                AllBosses.Add(item);
                            }
                            catch (Exception ex)
                            {
                                DebugLogger.LogError("VisualBuilderTab", $"Error loading boss {bossName}: {ex.Message}");
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

                DebugLogger.Log("VisualBuilderTab", $"Loaded {AllJokers.Count} jokers, {AllTags.Count} tags, {AllVouchers.Count} vouchers, {AllTarots.Count} tarots, {AllPlanets.Count} planets, {AllSpectrals.Count} spectrals, {AllBosses.Count} bosses with images");
            }
            catch (Exception ex)
            {
                DebugLogger.LogError("VisualBuilderTab", $"Error loading data: {ex.Message}");
                
                // Fallback to sample data
                var spriteService = SpriteService.Instance;
                AllJokers.Add(new FilterItem 
                { 
                    Name = "Joker", 
                    Type = "Joker",
                    ItemImage = spriteService.GetJokerImage("Joker")
                });
                AllTags.Add(new FilterItem 
                { 
                    Name = "Negative Tag", 
                    Type = "SmallBlindTag",
                    ItemImage = spriteService.GetTagImage("Negative Tag")
                });
                AllVouchers.Add(new FilterItem 
                { 
                    Name = "Overstock", 
                    Type = "Voucher",
                    ItemImage = spriteService.GetVoucherImage("Overstock")
                });
            }
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
            var ranks = new[] { "Ace", "2", "3", "4", "5", "6", "7", "8", "9", "10", "Jack", "Queen", "King" };
            
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
                if (string.IsNullOrEmpty(filter) || joker.Name.ToLowerInvariant().Contains(filter))
                {
                    FilteredJokers.Add(joker);
                }
            }

            foreach (var tag in AllTags)
            {
                if (string.IsNullOrEmpty(filter) || tag.Name.ToLowerInvariant().Contains(filter))
                {
                    FilteredTags.Add(tag);
                }
            }
            
            foreach (var voucher in AllVouchers)
            {
                if (string.IsNullOrEmpty(filter) || voucher.Name.ToLowerInvariant().Contains(filter))
                {
                    FilteredVouchers.Add(voucher);
                }
            }
            
            foreach (var tarot in AllTarots)
            {
                if (string.IsNullOrEmpty(filter) || tarot.Name.ToLowerInvariant().Contains(filter))
                {
                    FilteredTarots.Add(tarot);
                }
            }
            
            foreach (var planet in AllPlanets)
            {
                if (string.IsNullOrEmpty(filter) || planet.Name.ToLowerInvariant().Contains(filter))
                {
                    FilteredPlanets.Add(planet);
                }
            }
            
            foreach (var spectral in AllSpectrals)
            {
                if (string.IsNullOrEmpty(filter) || spectral.Name.ToLowerInvariant().Contains(filter))
                {
                    FilteredSpectrals.Add(spectral);
                }
            }
            
            foreach (var boss in AllBosses)
            {
                if (string.IsNullOrEmpty(filter) || boss.Name.ToLowerInvariant().Contains(filter))
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
                ItemConfigs[k].ItemName == item.Name && ItemConfigs[k].ItemType == item.Type);
            
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
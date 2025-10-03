using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
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
    public partial class VisualBuilderTabViewModel : ObservableObject
    {
        private readonly FiltersModalViewModel? _parentViewModel;
        [ObservableProperty]
        private string _searchFilter = "";

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

        [ObservableProperty]
        private string _selectedCategory = "Legendary";

        // Display name for current category
        public string CurrentCategoryDisplay => SelectedCategory switch
        {
            "Favorites" => "🔥 Favorites",
            "Legendary" => "🏆 Legendary",
            "Rare" => "💎 Rare",
            "Uncommon" => "🔸 Uncommon",
            "Common" => "⚪ Common",
            "Voucher" => "🎟️ Voucher",
            "Tarot" => "🔮 Tarot",
            "Planet" => "🪐 Planet",
            "Spectral" => "👻 Spectral",
            "PlayingCards" => "🃏 Playing Cards",
            "Tag" => "🏷️ Tag",
            "Boss" => "👹 Boss",
            _ => SelectedCategory
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
            
            // Initialize categories with proper data template approach
            Categories = new ObservableCollection<CategoryViewModel>
            {
                new() { Name = "Legendary", DisplayName = "🏆 Legendary", Items = FilteredJokers },
                new() { Name = "Rare", DisplayName = "💎 Rare", Items = FilteredJokers },
                new() { Name = "Uncommon", DisplayName = "🔸 Uncommon", Items = FilteredJokers },
                new() { Name = "Common", DisplayName = "⚪ Common", Items = FilteredJokers },
                new() { Name = "Voucher", DisplayName = "🎟️ Voucher", Items = FilteredVouchers },
                new() { Name = "Tarot", DisplayName = "🔮 Tarot", Items = FilteredTarots },
                new() { Name = "Planet", DisplayName = "🪐 Planet", Items = FilteredPlanets },
                new() { Name = "Spectral", DisplayName = "👻 Spectral", Items = FilteredSpectrals },
                new() { Name = "Tag", DisplayName = "🏷️ Tag", Items = FilteredTags },
                new() { Name = "Boss", DisplayName = "👹 Boss", Items = FilteredBosses }
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
                if (e.PropertyName == nameof(SelectedCategory) || e.PropertyName == nameof(SearchFilter))
                {
                    RefreshFilteredItems();
                }
            };

            // Load initial data
            LoadSampleData();
            ApplyFilter();
        }

        public void SetCategory(string category)
        {
            // Auto-clear search when switching tabs for clean navigation
            SearchFilter = "";
            SelectedCategory = category;
            OnPropertyChanged(nameof(CurrentCategoryDisplay));

            // Ensure collections are populated when switching categories
            if (FilteredJokers.Count == 0)
            {
                ApplyFilter();
            }
        }

        private void RefreshFilteredItems()
        {
            // CROSS-CATEGORY SEARCH - If searching, search ALL items regardless of category
            if (!string.IsNullOrEmpty(SearchFilter))
            {
                FilteredItems.Clear();
                
                // Search across ALL categories when filter is active
                var allCollections = new[] { FilteredJokers, FilteredVouchers, FilteredTarots, FilteredPlanets, FilteredSpectrals, FilteredTags, FilteredBosses };
                
                foreach (var collection in allCollections)
                {
                    foreach (var item in collection)
                    {
                        if (item.Name?.Contains(SearchFilter, StringComparison.OrdinalIgnoreCase) == true ||
                            item.DisplayName?.Contains(SearchFilter, StringComparison.OrdinalIgnoreCase) == true)
                        {
                            FilteredItems.Add(item);
                        }
                    }
                }
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
                _ => FilteredJokers.Where(j => j.Type == "Joker")
            };
            
            FilteredItems.Clear();
            foreach (var item in sourceCollection)
            {
                FilteredItems.Add(item);
            }
        }


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

                // Trigger auto-sync to JSON Editor
                NotifyJsonEditorOfChanges();
            }
        }

        private void AddToShould(FilterItem? item)
        {
            if (item != null && !SelectedShould.Any(x => x.Name == item.Name))
            {
                SelectedShould.Add(item);
                DebugLogger.Log("VisualBuilderTab", $"Added {item.Name} to SHOULD");

                // Trigger auto-sync to JSON Editor
                NotifyJsonEditorOfChanges();
            }
        }

        private void AddToMustNot(FilterItem? item)
        {
            if (item != null && !SelectedMustNot.Any(x => x.Name == item.Name))
            {
                SelectedMustNot.Add(item);
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
                        DisplayName = FormatDisplayName(favoriteName),
                        ItemImage = spriteService.GetJokerImage(favoriteName)
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
                    DebugLogger.Log("VisualBuilderTab", $"Loaded legendary {legendaryName}: Image={item.ItemImage != null}, ItemKey={item.ItemKey}");
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
                            DisplayName = FormatDisplayName(jokerName),
                            ItemImage = spriteService.GetJokerImage(jokerName)
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
                DebugLogger.Log("VisualBuilderTab", $"Joker types: {string.Join(", ", AllJokers.Take(5).Select(j => $"{j.Name}:{j.Type}:{j.Category}"))}");
                DebugLogger.Log("VisualBuilderTab", $"FilteredJokers after ApplyFilter: {FilteredJokers.Count}");
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
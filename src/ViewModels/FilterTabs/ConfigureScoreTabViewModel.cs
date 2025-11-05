using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;
using Avalonia.Threading;
using BalatroSeedOracle.Helpers;
using BalatroSeedOracle.Models;
using BalatroSeedOracle.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace BalatroSeedOracle.ViewModels.FilterTabs
{
    /// <summary>
    /// ViewModel for Configure Score tab - handles score columns with weights in a row-based UI.
    /// Replaces the SHOULD zone from Visual Builder with a more intuitive scoring interface.
    /// </summary>
    public partial class ConfigureScoreTabViewModel : ObservableObject
    {
        private readonly FiltersModalViewModel? _parentViewModel;

        // Auto-save debouncing
        private CancellationTokenSource? _autoSaveCts;
        private const int AutoSaveDebounceMs = 500;

        [ObservableProperty]
        private string _searchFilter = "";

        [ObservableProperty]
        private bool _isLoading = true;

        // Score row model
        public class ScoreRow : ObservableObject
        {
            private FilterItem _item = new();
            private int _weight = 10;

            public FilterItem Item
            {
                get => _item;
                set => SetProperty(ref _item, value);
            }

            public int Weight
            {
                get => _weight;
                set => SetProperty(ref _weight, value);
            }

            // For parent reference to handle removal
            public ConfigureScoreTabViewModel? Parent { get; set; }

            private ICommand? _removeCommand;
            public ICommand RemoveCommand => _removeCommand ??= new RelayCommand(Remove);

            private void Remove()
            {
                Parent?.RemoveScoreRow(this);
            }
        }

        // Score rows collection (replaces SHOULD zone)
        public ObservableCollection<ScoreRow> ScoreRows { get; } = new();

        // Expose parent's FilterName for display
        public string FilterName => _parentViewModel?.FilterName ?? "New Filter";

        // Available items for dragging
        public ObservableCollection<FilterItem> AllJokers { get; }
        public ObservableCollection<FilterItem> AllTags { get; }
        public ObservableCollection<FilterItem> AllVouchers { get; }
        public ObservableCollection<FilterItem> AllTarots { get; }
        public ObservableCollection<FilterItem> AllPlanets { get; }
        public ObservableCollection<FilterItem> AllSpectrals { get; }
        public ObservableCollection<FilterItem> AllBosses { get; }
        public ObservableCollection<FilterItem> AllWildcards { get; }

        // Filtered items
        public ObservableCollection<FilterItem> FilteredJokers { get; }
        public ObservableCollection<FilterItem> FilteredTags { get; }
        public ObservableCollection<FilterItem> FilteredVouchers { get; }
        public ObservableCollection<FilterItem> FilteredTarots { get; }
        public ObservableCollection<FilterItem> FilteredPlanets { get; }
        public ObservableCollection<FilterItem> FilteredSpectrals { get; }
        public ObservableCollection<FilterItem> FilteredBosses { get; }
        public ObservableCollection<FilterItem> FilteredWildcards { get; }

        // Main category selection
        [ObservableProperty]
        private string _selectedMainCategory = "Joker";

        // Grouped items for the new UI
        public class ItemGroup : ObservableObject
        {
            public string GroupName { get; set; } = "";
            public ObservableCollection<FilterItem> Items { get; set; } = new();
        }

        [ObservableProperty]
        private ObservableCollection<ItemGroup> _groupedItems = new();

        // Helper visibility properties
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

        public ConfigureScoreTabViewModel(FiltersModalViewModel? parentViewModel = null)
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

            FilteredJokers = new ObservableCollection<FilterItem>();
            FilteredTags = new ObservableCollection<FilterItem>();
            FilteredVouchers = new ObservableCollection<FilterItem>();
            FilteredTarots = new ObservableCollection<FilterItem>();
            FilteredPlanets = new ObservableCollection<FilterItem>();
            FilteredSpectrals = new ObservableCollection<FilterItem>();
            FilteredBosses = new ObservableCollection<FilterItem>();
            FilteredWildcards = new ObservableCollection<FilterItem>();

            GroupedItems = new ObservableCollection<ItemGroup>();

            // Subscribe to score rows changes for auto-save
            ScoreRows.CollectionChanged += (s, e) => TriggerAutoSave();

            // Property change handling
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
                    AddGroup("Rare Jokers", FilteredJokers.Where(j => j.Type == "Joker" && j.Category == "Rare"));
                    AddGroup("Uncommon Jokers", FilteredJokers.Where(j => j.Type == "Joker" && j.Category == "Uncommon"));
                    AddGroup("Common Jokers", FilteredJokers.Where(j => j.Type == "Joker" && j.Category == "Common"));
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
                        var baseVoucher = FilteredVouchers.FirstOrDefault(v => v.Name.Equals(baseName, StringComparison.OrdinalIgnoreCase));
                        if (baseVoucher != null) organizedVouchers.Add(baseVoucher);
                    }
                    foreach (var (_, upgradeName) in firstSet)
                    {
                        var upgradeVoucher = FilteredVouchers.FirstOrDefault(v => v.Name.Equals(upgradeName, StringComparison.OrdinalIgnoreCase));
                        if (upgradeVoucher != null) organizedVouchers.Add(upgradeVoucher);
                    }

                    var secondSet = voucherPairs.Skip(8).Take(8).ToList();
                    foreach (var (baseName, _) in secondSet)
                    {
                        var baseVoucher = FilteredVouchers.FirstOrDefault(v => v.Name.Equals(baseName, StringComparison.OrdinalIgnoreCase));
                        if (baseVoucher != null) organizedVouchers.Add(baseVoucher);
                    }
                    foreach (var (_, upgradeName) in secondSet)
                    {
                        var upgradeVoucher = FilteredVouchers.FirstOrDefault(v => v.Name.Equals(upgradeName, StringComparison.OrdinalIgnoreCase));
                        if (upgradeVoucher != null) organizedVouchers.Add(upgradeVoucher);
                    }

                    var remainingVouchers = FilteredVouchers.Except(organizedVouchers);
                    organizedVouchers.AddRange(remainingVouchers);
                    AddGroup("Vouchers", organizedVouchers);
                    break;
            }
        }

        private void AddGroup(string groupName, System.Collections.Generic.IEnumerable<FilterItem> items)
        {
            var group = new ItemGroup
            {
                GroupName = groupName,
                Items = new ObservableCollection<FilterItem>(items),
            };
            GroupedItems.Add(group);
        }

        private System.Collections.Generic.List<(string baseName, string upgradeName)> GetVoucherPairs()
        {
            return new System.Collections.Generic.List<(string, string)>
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

        /// <summary>
        /// Add a new score row when an item is dropped
        /// </summary>
        public void AddScoreRow(FilterItem item, int weight = 10)
        {
            var scoreRow = new ScoreRow
            {
                Item = item,
                Weight = weight,
                Parent = this
            };

            ScoreRows.Add(scoreRow);

            // Sync with parent ViewModel
            if (_parentViewModel != null)
            {
                var itemKey = _parentViewModel.GenerateNextItemKey();
                var itemConfig = new ItemConfig
                {
                    ItemKey = itemKey,
                    ItemType = item.Type,
                    ItemName = item.Name,
                    Score = weight // Weight becomes Score in config
                };
                _parentViewModel.ItemConfigs[itemKey] = itemConfig;
                _parentViewModel.SelectedShould.Add(itemKey); // Store in SHOULD zone (score columns)
            }

            DebugLogger.Log("ConfigureScoreTab", $"Added {item.Name} to score columns with weight {weight}");
            NotifyJsonEditorOfChanges();
        }

        /// <summary>
        /// Remove a score row
        /// </summary>
        public void RemoveScoreRow(ScoreRow row)
        {
            var index = ScoreRows.IndexOf(row);
            if (index < 0) return;

            ScoreRows.RemoveAt(index);

            // Sync with parent ViewModel
            if (_parentViewModel != null && index < _parentViewModel.SelectedShould.Count)
            {
                var itemKey = _parentViewModel.SelectedShould[index];
                _parentViewModel.SelectedShould.RemoveAt(index);
                _parentViewModel.ItemConfigs.Remove(itemKey);
            }

            DebugLogger.Log("ConfigureScoreTab", $"Removed {row.Item.Name} from score columns");
            NotifyJsonEditorOfChanges();
        }

        /// <summary>
        /// Update weight for a score row
        /// </summary>
        public void UpdateScoreWeight(ScoreRow row, int newWeight)
        {
            var index = ScoreRows.IndexOf(row);
            if (index < 0) return;

            row.Weight = newWeight;

            // Sync with parent ViewModel
            if (_parentViewModel != null && index < _parentViewModel.SelectedShould.Count)
            {
                var itemKey = _parentViewModel.SelectedShould[index];
                if (_parentViewModel.ItemConfigs.TryGetValue(itemKey, out var itemConfig))
                {
                    itemConfig.Score = newWeight;
                }
            }

            DebugLogger.Log("ConfigureScoreTab", $"Updated {row.Item.Name} weight to {newWeight}");
            NotifyJsonEditorOfChanges();
        }

        private void NotifyJsonEditorOfChanges()
        {
            if (_parentViewModel?.JsonEditorTab is JsonEditorTabViewModel jsonEditorVm)
            {
                jsonEditorVm.AutoGenerateFromVisual();
            }
        }

        #region Helper Methods

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
                    AllWildcards.Add(new FilterItem
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
                    });
                }

                // Load legendaries
                var legendaryJokers = new[] { "Triboulet", "Yorick", "Chicot", "Perkeo", "Canio" };
                foreach (var legendaryName in legendaryJokers)
                {
                    AllJokers.Add(new FilterItem
                    {
                        Name = legendaryName,
                        Type = "SoulJoker",
                        Category = "Legendary",
                        DisplayName = BalatroData.GetDisplayNameFromSprite(legendaryName),
                        ItemImage = spriteService.GetJokerImage(legendaryName),
                    });
                }

                // Load regular jokers
                if (BalatroData.Jokers?.Keys != null)
                {
                    foreach (var jokerName in BalatroData.Jokers.Keys)
                    {
                        if (AllJokers.Any(j => j.Name == jokerName)) continue;

                        string rarity = "Common";
                        foreach (var rarityKvp in BalatroData.JokersByRarity)
                        {
                            if (rarityKvp.Value.Contains(jokerName.ToLower()))
                            {
                                rarity = rarityKvp.Key;
                                break;
                            }
                        }

                        AllJokers.Add(new FilterItem
                        {
                            Name = jokerName,
                            Type = "Joker",
                            Category = rarity,
                            DisplayName = BalatroData.GetDisplayNameFromSprite(jokerName),
                            ItemImage = spriteService.GetJokerImage(jokerName),
                        });
                    }
                }

                // Load tags
                if (BalatroData.Tags?.Keys != null)
                {
                    foreach (var tagName in BalatroData.Tags.Keys)
                    {
                        AllTags.Add(new FilterItem
                        {
                            Name = tagName,
                            Type = "SmallBlindTag",
                            DisplayName = BalatroData.GetDisplayNameFromSprite(tagName),
                            ItemImage = spriteService.GetTagImage(tagName),
                        });
                    }
                }

                // Load vouchers
                if (BalatroData.Vouchers?.Keys != null)
                {
                    foreach (var voucherName in BalatroData.Vouchers.Keys)
                    {
                        AllVouchers.Add(new FilterItem
                        {
                            Name = voucherName,
                            Type = "Voucher",
                            DisplayName = BalatroData.GetDisplayNameFromSprite(voucherName),
                            ItemImage = spriteService.GetVoucherImage(voucherName),
                        });
                    }
                }

                // Load tarots
                if (BalatroData.TarotCards?.Keys != null)
                {
                    foreach (var tarotName in BalatroData.TarotCards.Keys)
                    {
                        if (tarotName == "any" || tarotName == "*") continue;
                        AllTarots.Add(new FilterItem
                        {
                            Name = tarotName,
                            Type = "Tarot",
                            DisplayName = BalatroData.GetDisplayNameFromSprite(tarotName),
                            ItemImage = spriteService.GetTarotImage(tarotName),
                        });
                    }
                }

                // Load planets
                if (BalatroData.PlanetCards?.Keys != null)
                {
                    foreach (var planetName in BalatroData.PlanetCards.Keys)
                    {
                        if (planetName == "any" || planetName == "*" || planetName == "anyplanet") continue;
                        AllPlanets.Add(new FilterItem
                        {
                            Name = planetName,
                            Type = "Planet",
                            DisplayName = BalatroData.GetDisplayNameFromSprite(planetName),
                            ItemImage = spriteService.GetPlanetCardImage(planetName),
                        });
                    }
                }

                // Load spectrals
                if (BalatroData.SpectralCards?.Keys != null)
                {
                    foreach (var spectralName in BalatroData.SpectralCards.Keys)
                    {
                        if (spectralName == "any" || spectralName == "*") continue;
                        AllSpectrals.Add(new FilterItem
                        {
                            Name = spectralName,
                            Type = "Spectral",
                            DisplayName = BalatroData.GetDisplayNameFromSprite(spectralName),
                            ItemImage = spriteService.GetSpectralImage(spectralName),
                        });
                    }
                }

                // Load bosses
                if (BalatroData.BossBlinds?.Keys != null)
                {
                    foreach (var bossName in BalatroData.BossBlinds.Keys)
                    {
                        AllBosses.Add(new FilterItem
                        {
                            Name = bossName,
                            Type = "Boss",
                            DisplayName = BalatroData.GetDisplayNameFromSprite(bossName),
                            ItemImage = spriteService.GetBossImage(bossName),
                        });
                    }
                }

                DebugLogger.Log("ConfigureScoreTab", $"Loaded {AllJokers.Count} jokers, {AllTags.Count} tags, {AllVouchers.Count} vouchers");
            }
            catch (Exception ex)
            {
                DebugLogger.LogError("ConfigureScoreTab", $"Error loading data: {ex.Message}");
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

            var filter = SearchFilter.ToLowerInvariant();

            foreach (var joker in AllJokers)
            {
                if (string.IsNullOrEmpty(filter) || joker.Name.ToLowerInvariant().Contains(filter) || joker.DisplayName.ToLowerInvariant().Contains(filter))
                {
                    FilteredJokers.Add(joker);
                }
            }

            foreach (var tag in AllTags)
            {
                if (string.IsNullOrEmpty(filter) || tag.Name.ToLowerInvariant().Contains(filter) || tag.DisplayName.ToLowerInvariant().Contains(filter))
                {
                    FilteredTags.Add(tag);
                }
            }

            foreach (var voucher in AllVouchers)
            {
                if (string.IsNullOrEmpty(filter) || voucher.Name.ToLowerInvariant().Contains(filter) || voucher.DisplayName.ToLowerInvariant().Contains(filter))
                {
                    FilteredVouchers.Add(voucher);
                }
            }

            foreach (var tarot in AllTarots)
            {
                if (string.IsNullOrEmpty(filter) || tarot.Name.ToLowerInvariant().Contains(filter) || tarot.DisplayName.ToLowerInvariant().Contains(filter))
                {
                    FilteredTarots.Add(tarot);
                }
            }

            foreach (var planet in AllPlanets)
            {
                if (string.IsNullOrEmpty(filter) || planet.Name.ToLowerInvariant().Contains(filter) || planet.DisplayName.ToLowerInvariant().Contains(filter))
                {
                    FilteredPlanets.Add(planet);
                }
            }

            foreach (var spectral in AllSpectrals)
            {
                if (string.IsNullOrEmpty(filter) || spectral.Name.ToLowerInvariant().Contains(filter) || spectral.DisplayName.ToLowerInvariant().Contains(filter))
                {
                    FilteredSpectrals.Add(spectral);
                }
            }

            foreach (var boss in AllBosses)
            {
                if (string.IsNullOrEmpty(filter) || boss.Name.ToLowerInvariant().Contains(filter) || boss.DisplayName.ToLowerInvariant().Contains(filter))
                {
                    FilteredBosses.Add(boss);
                }
            }

            foreach (var wildcard in AllWildcards)
            {
                if (string.IsNullOrEmpty(filter) || wildcard.Name.ToLowerInvariant().Contains(filter) || wildcard.DisplayName.ToLowerInvariant().Contains(filter))
                {
                    FilteredWildcards.Add(wildcard);
                }
            }

            RebuildGroupedItems();
        }

        #endregion

        #region Auto-Save Functionality

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
                    DebugLogger.LogError("ConfigureScoreTab", $"Auto-save error: {ex.Message}");
                }
            });
        }

        private async Task PerformAutoSave()
        {
            if (_parentViewModel == null) return;

            try
            {
                var filterName = _parentViewModel.FilterName;
                if (string.IsNullOrWhiteSpace(filterName)) return;

                var config = _parentViewModel.BuildConfigFromCurrentState();
                var configService = ServiceHelper.GetService<IConfigurationService>();
                if (configService == null) return;

                var filePath = System.IO.Path.Combine(configService.GetFiltersDirectory(), $"{filterName.Replace(" ", "_")}.json");
                await configService.SaveFilterAsync(filePath, config);

                DebugLogger.Log("ConfigureScoreTab", $"Auto-saved filter: {filterName}");
            }
            catch (Exception ex)
            {
                DebugLogger.LogError("ConfigureScoreTab", $"Auto-save exception: {ex.Message}");
            }
        }

        #endregion
    }
}

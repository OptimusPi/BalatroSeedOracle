using System;
using System.Collections.ObjectModel;
using System.Linq;
using BalatroSeedOracle.Helpers;
using BalatroSeedOracle.Models;

namespace BalatroSeedOracle.Services
{
    /// <summary>
    /// Holds references to all filter item collections.
    /// Used to pass collections to/from the data service.
    /// </summary>
    public class FilterItemCollections
    {
        public ObservableCollection<FilterItem> AllJokers { get; init; } = new();
        public ObservableCollection<FilterItem> AllTags { get; init; } = new();
        public ObservableCollection<FilterItem> AllVouchers { get; init; } = new();
        public ObservableCollection<FilterItem> AllTarots { get; init; } = new();
        public ObservableCollection<FilterItem> AllPlanets { get; init; } = new();
        public ObservableCollection<FilterItem> AllSpectrals { get; init; } = new();
        public ObservableCollection<FilterItem> AllBosses { get; init; } = new();
        public ObservableCollection<FilterItem> AllWildcards { get; init; } = new();
        public ObservableCollection<FilterItem> AllStandardCards { get; init; } = new();
    }

    /// <summary>
    /// Service interface for loading all game data (jokers, vouchers, tags, etc.)
    /// Single source of truth - eliminates ~400 lines of duplicate code.
    /// </summary>
    public interface IFilterItemDataService
    {
        /// <summary>
        /// Loads all game data into the provided collections
        /// </summary>
        void LoadGameData(FilterItemCollections collections);
    }

    /// <summary>
    /// Implementation of FilterItemDataService.
    /// Centralized loading logic for all filter items.
    /// </summary>
    public class FilterItemDataService : IFilterItemDataService
    {
        private readonly SpriteService _spriteService;

        public FilterItemDataService(SpriteService spriteService)
        {
            _spriteService = spriteService;
        }

        public void LoadGameData(FilterItemCollections collections)
        {
            try
            {
                LoadWildcards(collections.AllWildcards);
                LoadJokers(collections.AllJokers);
                LoadTags(collections.AllTags);
                LoadVouchers(collections.AllVouchers);
                LoadTarots(collections.AllTarots);
                LoadPlanets(collections.AllPlanets);
                LoadSpectrals(collections.AllSpectrals);
                LoadBosses(collections.AllBosses);
                LoadStandardCards(collections.AllStandardCards);

                DebugLogger.Log("FilterItemDataService",
                    $"Loaded {collections.AllJokers.Count} jokers, " +
                    $"{collections.AllTags.Count} tags, " +
                    $"{collections.AllVouchers.Count} vouchers, " +
                    $"{collections.AllTarots.Count} tarots, " +
                    $"{collections.AllPlanets.Count} planets, " +
                    $"{collections.AllSpectrals.Count} spectrals, " +
                    $"{collections.AllBosses.Count} bosses, " +
                    $"{collections.AllStandardCards.Count} standard cards");
            }
            catch (Exception ex)
            {
                DebugLogger.LogError("FilterItemDataService", $"Error loading data: {ex.Message}");
            }
        }

        #region Private Loading Methods

        private void LoadWildcards(ObservableCollection<FilterItem> allWildcards)
        {
            var wildcards = new[]
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

            foreach (var (name, displayName, type) in wildcards)
            {
                allWildcards.Add(new FilterItem
                {
                    Name = name,
                    Type = type,
                    Category = "Wildcard",
                    DisplayName = displayName,
                    ItemImage = type switch
                    {
                        "Joker" or "SoulJoker" => _spriteService.GetJokerImage(name),
                        "Tarot" => _spriteService.GetTarotImage(name),
                        "Planet" => _spriteService.GetPlanetCardImage(name),
                        "Spectral" => _spriteService.GetSpectralImage(name),
                        _ => null
                    }
                });
            }
        }

        private void LoadJokers(ObservableCollection<FilterItem> allJokers)
        {
            // Load favorites first
            var favoritesService = ServiceHelper.GetService<FavoritesService>();
            var favoriteNames = favoritesService?.GetFavoriteItems() ?? new System.Collections.Generic.List<string>();

            foreach (var favoriteName in favoriteNames)
            {
                allJokers.Add(new FilterItem
                {
                    Name = favoriteName,
                    Type = "Joker",
                    Category = "Favorite",
                    IsFavorite = true,
                    DisplayName = BalatroData.GetDisplayNameFromSprite(favoriteName),
                    ItemImage = _spriteService.GetJokerImage(favoriteName),
                });
            }

            // Load legendary jokers
            var legendaryJokers = new[] { "Triboulet", "Yorick", "Chicot", "Perkeo", "Canio" };
            foreach (var legendaryName in legendaryJokers)
            {
                allJokers.Add(new FilterItem
                {
                    Name = legendaryName,
                    Type = "SoulJoker",
                    Category = "Legendary",
                    DisplayName = BalatroData.GetDisplayNameFromSprite(legendaryName),
                    ItemImage = _spriteService.GetJokerImage(legendaryName),
                });
            }

            // Load regular jokers
            if (BalatroData.Jokers?.Keys != null)
            {
                foreach (var jokerName in BalatroData.Jokers.Keys)
                {
                    if (allJokers.Any(j => j.Name == jokerName)) continue;

                    string rarity = "Common";
                    foreach (var rarityKvp in BalatroData.JokersByRarity)
                    {
                        if (rarityKvp.Value.Contains(jokerName.ToLower()))
                        {
                            rarity = rarityKvp.Key;
                            break;
                        }
                    }

                    allJokers.Add(new FilterItem
                    {
                        Name = jokerName,
                        Type = "Joker",
                        Category = rarity,
                        DisplayName = BalatroData.GetDisplayNameFromSprite(jokerName),
                        ItemImage = _spriteService.GetJokerImage(jokerName),
                    });
                }
            }
        }

        private void LoadTags(ObservableCollection<FilterItem> allTags)
        {
            if (BalatroData.Tags?.Keys != null)
            {
                foreach (var tagName in BalatroData.Tags.Keys)
                {
                    allTags.Add(new FilterItem
                    {
                        Name = tagName,
                        Type = "SmallBlindTag",
                        DisplayName = BalatroData.GetDisplayNameFromSprite(tagName),
                        ItemImage = _spriteService.GetTagImage(tagName),
                    });
                }
            }
        }

        private void LoadVouchers(ObservableCollection<FilterItem> allVouchers)
        {
            if (BalatroData.Vouchers?.Keys != null)
            {
                foreach (var voucherName in BalatroData.Vouchers.Keys)
                {
                    allVouchers.Add(new FilterItem
                    {
                        Name = voucherName,
                        Type = "Voucher",
                        DisplayName = BalatroData.GetDisplayNameFromSprite(voucherName),
                        ItemImage = _spriteService.GetVoucherImage(voucherName),
                    });
                }
            }
        }

        private void LoadTarots(ObservableCollection<FilterItem> allTarots)
        {
            if (BalatroData.TarotCards?.Keys != null)
            {
                foreach (var tarotName in BalatroData.TarotCards.Keys)
                {
                    if (tarotName == "any" || tarotName == "*") continue;
                    allTarots.Add(new FilterItem
                    {
                        Name = tarotName,
                        Type = "Tarot",
                        DisplayName = BalatroData.GetDisplayNameFromSprite(tarotName),
                        ItemImage = _spriteService.GetTarotImage(tarotName),
                    });
                }
            }
        }

        private void LoadPlanets(ObservableCollection<FilterItem> allPlanets)
        {
            if (BalatroData.PlanetCards?.Keys != null)
            {
                foreach (var planetName in BalatroData.PlanetCards.Keys)
                {
                    if (planetName == "any" || planetName == "*" || planetName == "anyplanet") continue;
                    allPlanets.Add(new FilterItem
                    {
                        Name = planetName,
                        Type = "Planet",
                        DisplayName = BalatroData.GetDisplayNameFromSprite(planetName),
                        ItemImage = _spriteService.GetPlanetCardImage(planetName),
                    });
                }
            }
        }

        private void LoadSpectrals(ObservableCollection<FilterItem> allSpectrals)
        {
            if (BalatroData.SpectralCards?.Keys != null)
            {
                foreach (var spectralName in BalatroData.SpectralCards.Keys)
                {
                    if (spectralName == "any" || spectralName == "*") continue;
                    allSpectrals.Add(new FilterItem
                    {
                        Name = spectralName,
                        Type = "Spectral",
                        DisplayName = BalatroData.GetDisplayNameFromSprite(spectralName),
                        ItemImage = _spriteService.GetSpectralImage(spectralName),
                    });
                }
            }
        }

        private void LoadBosses(ObservableCollection<FilterItem> allBosses)
        {
            if (BalatroData.BossBlinds?.Keys != null)
            {
                foreach (var bossName in BalatroData.BossBlinds.Keys)
                {
                    allBosses.Add(new FilterItem
                    {
                        Name = bossName,
                        Type = "Boss",
                        DisplayName = BalatroData.GetDisplayNameFromSprite(bossName),
                        ItemImage = _spriteService.GetBossImage(bossName),
                    });
                }
            }
        }

        private void LoadStandardCards(ObservableCollection<FilterItem> allStandardCards)
        {
            var suits = new[] { "Hearts", "Spades", "Diamonds", "Clubs" };
            var ranks = new[] { "2", "3", "4", "5", "6", "7", "8", "9", "10", "Jack", "Queen", "King", "Ace" };

            // Generate base 52 cards (normal cards with no enhancement)
            foreach (var suit in suits)
            {
                foreach (var rank in ranks)
                {
                    var displayName = rank == "Ace" ? $"Ace of {suit}" : $"{rank} of {suit}";
                    allStandardCards.Add(new FilterItem
                    {
                        Name = $"{rank}_{suit}",
                        Type = "StandardCard",
                        Category = suit,
                        DisplayName = displayName,
                        Rank = rank,
                        Suit = suit,
                        Enhancement = null,
                        ItemImage = _spriteService.GetPlayingCardImage(suit, rank),
                    });
                }
            }

            // Generate enhanced variants
            var enhancements = new[] { "Mult", "Bonus", "Glass", "Gold", "Steel" };
            foreach (var enhancement in enhancements)
            {
                foreach (var rank in ranks)
                {
                    var suit = "Hearts"; // Use Hearts as default suit for enhanced cards
                    var displayName = $"{enhancement} {rank}";
                    if (rank == "Ace")
                        displayName = $"{enhancement} Ace";

                    allStandardCards.Add(new FilterItem
                    {
                        Name = $"{enhancement}_{rank}_{suit}",
                        Type = "StandardCard",
                        Category = enhancement,
                        DisplayName = displayName,
                        Rank = rank,
                        Suit = suit,
                        Enhancement = enhancement,
                        ItemImage = _spriteService.GetPlayingCardImage(suit, rank, enhancement),
                    });
                }
            }

            // Add Stone card (special case with no rank/suit)
            allStandardCards.Add(new FilterItem
            {
                Name = "Stone",
                Type = "StandardCard",
                Category = "Stone",
                DisplayName = "Stone Card",
                Rank = null,
                Suit = null,
                Enhancement = "Stone",
                ItemImage = _spriteService.GetPlayingCardImage("Hearts", "Ace", "Stone"),
            });
        }

        #endregion
    }
}

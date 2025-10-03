using System;
using System.Collections.ObjectModel;
using Avalonia.Media;
using Motely;

namespace BalatroSeedOracle.Models
{
    /// <summary>
    /// Represents the complete analysis result for a seed
    /// </summary>
    public class SeedAnalysisModel
    {
        public string Seed { get; set; } = "";
        public MotelyDeck Deck { get; set; }
        public MotelyStake Stake { get; set; }
        public ObservableCollection<AnteAnalysisModel> Antes { get; set; } = new();
        public string? Error { get; set; }
    }

    /// <summary>
    /// Represents analysis data for a single ante
    /// </summary>
    public class AnteAnalysisModel
    {
        public int AnteNumber { get; set; }
        public string AnteTitle => $"ANTE {AnteNumber}";

        public MotelyVoucher Voucher { get; set; }
        public bool HasVoucher => Voucher != 0;
        public string VoucherName => Voucher.ToString();

        public ObservableCollection<ShopItemModel> ShopItems { get; set; } = new();
        public bool HasShopItems => ShopItems.Count > 0;

        public ObservableCollection<BoosterPackModel> BoosterPacks { get; set; } = new();
        public bool HasBoosterPacks => BoosterPacks.Count > 0;

        public TagModel SmallBlindTag { get; set; } = new();
        public TagModel BigBlindTag { get; set; } = new();
    }

    /// <summary>
    /// Represents a shop item (Joker, Tarot, Planet, etc.)
    /// </summary>
    public class ShopItemModel
    {
        public MotelyItemTypeCategory TypeCategory { get; set; }
        public object ItemValue { get; set; } = null!;
        public MotelyItemEdition Edition { get; set; }
        public bool HasEdition => Edition != MotelyItemEdition.None;

        public string ItemName => ItemValue?.ToString() ?? "";
        public string EditionName => Edition.ToString();

        // For sprite binding
        public bool IsJoker => TypeCategory == MotelyItemTypeCategory.Joker;
        public bool IsTarot => TypeCategory == MotelyItemTypeCategory.TarotCard;
        public bool IsPlanet => TypeCategory == MotelyItemTypeCategory.PlanetCard;
        public bool IsSpectral => TypeCategory == MotelyItemTypeCategory.SpectralCard;

        public string SpriteKey
        {
            get
            {
                return TypeCategory switch
                {
                    MotelyItemTypeCategory.Joker => $"joker_{ItemValue?.ToString()}",
                    MotelyItemTypeCategory.TarotCard => $"tarot_{ItemValue?.ToString()}",
                    MotelyItemTypeCategory.PlanetCard => $"planet_{ItemValue?.ToString()}",
                    MotelyItemTypeCategory.SpectralCard => $"spectral_{ItemValue?.ToString()}",
                    _ => ""
                };
            }
        }

        public IBrush EditionColor
        {
            get
            {
                return Edition switch
                {
                    MotelyItemEdition.Foil => new SolidColorBrush(Color.Parse("#8FC5FF")),
                    MotelyItemEdition.Holographic => new SolidColorBrush(Color.Parse("#FF8FFF")),
                    MotelyItemEdition.Polychrome => new SolidColorBrush(Color.Parse("#FFD700")),
                    MotelyItemEdition.Negative => new SolidColorBrush(Color.Parse("#FF5555")),
                    _ => Brushes.White
                };
            }
        }
    }

    /// <summary>
    /// Represents a booster pack with its contents
    /// </summary>
    public class BoosterPackModel
    {
        public MotelyBoosterPackType PackType { get; set; }
        public ObservableCollection<string> Items { get; set; } = new();

        public string PackName => PackType.ToString().Replace("Pack", "");
        public string PackSpriteKey => PackType.ToString().ToLowerInvariant().Replace("_", "");
        public bool HasItems => Items.Count > 0;
        public string ItemsText => string.Join(", ", Items);
    }

    /// <summary>
    /// Represents a skip tag for small or big blind
    /// </summary>
    public class TagModel
    {
        public string BlindType { get; set; } = "";
        public MotelyTag Tag { get; set; }

        public string TagName => Tag.ToString().Replace("Tag", "");
        public string TagSpriteKey => Tag.ToString();
    }

    /// <summary>
    /// Tab state for the analyze modal
    /// </summary>
    public enum AnalyzeModalTab
    {
        Settings,
        Analyzer
    }
}

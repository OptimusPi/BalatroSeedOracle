using System;
using System.Collections.ObjectModel;
using Avalonia.Media;
using Motely;
using Motely.Analysis;

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
        public string DeckStakeText => $"Deck: {Deck} | Stake: {Stake}";
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

        public MotelyBossBlind Boss { get; set; }
        public bool HasBoss => Boss != 0;
        public string BossName => FormatUtils.FormatBoss(Boss);

        public MotelyVoucher Voucher { get; set; }
        public bool HasVoucher => Voucher != 0;
        public string VoucherName => FormatUtils.FormatVoucher(Voucher);

        public ObservableCollection<ShopItemModel> ShopItems { get; set; } = new();
        public bool HasShopItems => ShopItems.Count > 0;
        public string ShopItemsCountText => HasShopItems ? $"{ShopItems.Count} item(s)" : "";

        public ObservableCollection<BoosterPackModel> BoosterPacks { get; set; } = new();
        public bool HasBoosterPacks => BoosterPacks.Count > 0;

        public TagModel SmallBlindTag { get; set; } = new();
        public TagModel BigBlindTag { get; set; } = new();
    }

    /// <summary>
    /// One analyzed item, backed by the engine's packed <see cref="MotelyItem"/>.
    ///
    /// The packed int is the whole item — type, edition, seal, enhancement, suit, rank and the
    /// sticker flags all live inside it. Keep it and read what you need; unpacking to strings at
    /// this boundary is how playing cards lost their suit and seal and came out as raw struct text.
    /// </summary>
    public class ShopItemModel
    {
        /// <summary>The engine value this model displays. Everything below is derived from it.</summary>
        public required MotelyItem Item { get; set; }

        public MotelyItemTypeCategory TypeCategory => Item.TypeCategory;
        public MotelyItemType ItemType => Item.Type;
        public MotelyItemEdition Edition => Item.Edition;
        public MotelyItemSeal Seal => Item.Seal;
        public MotelyItemEnhancement Enhancement => Item.Enhancement;
        public MotelyStandardcardSuit Suit => Item.StandardcardSuit;
        public MotelyStandardcardRank Rank => Item.StandardcardRank;

        public bool IsEternal => Item.IsEternal;
        public bool IsPerishable => Item.IsPerishable;
        public bool IsRental => Item.IsRental;

        public bool HasEdition => Edition != MotelyItemEdition.None;
        public bool HasSeal => Seal != MotelyItemSeal.None;
        public bool HasEnhancement => Enhancement != MotelyItemEnhancement.None;

        /// <summary>Engine-formatted display name — "Steel 9 of Diamonds", not a struct dump.</summary>
        public string ItemName => FormatUtils.FormatItem(Item);

        public string EditionName => Edition.ToString();
        public string SealName => Seal.ToString();
        public string EnhancementName => Enhancement.ToString();

        public bool IsJoker => TypeCategory == MotelyItemTypeCategory.Joker;
        public bool IsTarot => TypeCategory == MotelyItemTypeCategory.TarotCard;
        public bool IsPlanet => TypeCategory == MotelyItemTypeCategory.PlanetCard;
        public bool IsSpectral => TypeCategory == MotelyItemTypeCategory.SpectralCard;
        public bool IsStandardCard => TypeCategory == MotelyItemTypeCategory.Standardcard;

        public string SpriteKey => ItemType.ToString().ToLowerInvariant();

        /// <summary>
        /// Rank and suit spelled the way the 8BitDeck sheet keys them ("9 of Diamonds", not
        /// "Nine of Diamonds"). Read straight off the engine's own formatter so the sheet and the
        /// engine can never drift apart — <see cref="MotelyStandardcardRank"/>.ToString() says
        /// "Nine", which is not a key in standard_cards_metadata.json and throws on lookup.
        /// </summary>
        private string[] StandardcardParts =>
            FormatUtils
                .FormatStandardcard((MotelyStandardCard)((int)Suit | (int)Rank))
                .Split(" of ", StringSplitOptions.None);

        public string SpriteRank => StandardcardParts[0];
        public string SpriteSuit => StandardcardParts[1];

        public static ShopItemModel From(MotelyItem item) => new() { Item = item };

        public IBrush EditionColor =>
            Edition switch
            {
                MotelyItemEdition.Foil => new SolidColorBrush(Color.Parse("#8FC5FF")),
                MotelyItemEdition.Holographic => new SolidColorBrush(Color.Parse("#FF8FFF")),
                MotelyItemEdition.Polychrome => new SolidColorBrush(Color.Parse("#FFD700")),
                MotelyItemEdition.Negative => new SolidColorBrush(Color.Parse("#FF5555")),
                _ => Brushes.White,
            };
    }

    /// <summary>
    /// Represents a booster pack with its contents
    /// </summary>
    public class BoosterPackModel
    {
        public MotelyBoosterPack Pack { get; set; }

        /// <summary>
        /// Pack contents as real items, so the pack card can draw the same sprites the shop row
        /// draws. This used to be a string list, which is what put struct dumps on screen.
        /// </summary>
        public ObservableCollection<ShopItemModel> Items { get; set; } = new();

        public MotelyBoosterPackType PackType => Pack.GetPackType();

        public string PackName => FormatUtils.FormatPackName(Pack);
        public string PackSpriteKey => Pack.ToString().ToLowerInvariant().Replace("_", "");
        public bool HasItems => Items.Count > 0;
    }

    /// <summary>
    /// Represents a skip tag for small or big blind
    /// </summary>
    public class TagModel
    {
        public string BlindType { get; set; } = "";
        public MotelyTag Tag { get; set; }

        public string TagName => FormatUtils.FormatTag(Tag);
        public string TagSpriteKey => Tag.ToString();
    }

    /// <summary>
    /// Tab state for the analyze modal
    /// </summary>
    public enum AnalyzeModalTab
    {
        Settings,
        Analyzer,
    }
}

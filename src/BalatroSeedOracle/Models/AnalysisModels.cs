using System.Collections.Generic;
using System.Linq;
using Avalonia.Media;
using Motely.Enums;

namespace BalatroSeedOracle.Models;

/// <summary>Tabs of the Analyze modal.</summary>
public enum AnalyzeModalTab
{
    Settings,
    Analyzer,
}

/// <summary>
/// UI model for one analyzed seed: header info plus per-ante breakdowns.
/// </summary>
public class SeedAnalysisModel
{
    public string Seed { get; set; } = string.Empty;

    public MotelyDeck Deck { get; set; }

    public MotelyStake Stake { get; set; }

    public string? Error { get; set; }

    /// <summary>
    /// IList so both <see cref="System.Collections.ObjectModel.ObservableCollection{T}"/>
    /// (live analysis) and plain lists (re-mapped snapshots) can be assigned.
    /// </summary>
    public IList<AnteAnalysisModel> Antes { get; set; } = new List<AnteAnalysisModel>();

    public string DeckStakeText => $"{Deck} Deck — {Stake} Stake";
}

/// <summary>UI model for one ante of an analyzed seed.</summary>
public class AnteAnalysisModel
{
    public int AnteNumber { get; set; }

    public string AnteTitle => $"Ante {AnteNumber}";

    public MotelyBossBlind Boss { get; set; }

    public string BossName => SplitPascal(Boss.ToString());

    public MotelyVoucher Voucher { get; set; }

    public bool HasVoucher => Voucher.ToString() != "None";

    public string VoucherName => HasVoucher ? SplitPascal(Voucher.ToString()) : string.Empty;

    public TagModel SmallBlindTag { get; set; } = new();

    public TagModel BigBlindTag { get; set; } = new();

    public IList<ShopItemModel> ShopItems { get; } = new List<ShopItemModel>();

    public IList<BoosterPackModel> BoosterPacks { get; } = new List<BoosterPackModel>();

    public bool HasShopItems => ShopItems.Count > 0;

    public bool HasBoosterPacks => BoosterPacks.Count > 0;

    public string ShopItemsCountText => ShopItems.Count == 1 ? "1 shop item" : $"{ShopItems.Count} shop items";

    private static string SplitPascal(string name) =>
        string.Concat(name.Select((c, i) => i > 0 && char.IsUpper(c) ? " " + c : c.ToString()));
}

/// <summary>UI model for a skip tag (small/big blind).</summary>
public class TagModel
{
    /// <summary>"Small Blind" / "Big Blind" — which blind this tag came from.</summary>
    public string BlindType { get; set; } = string.Empty;

    public MotelyTag Tag { get; set; }

    public string TagName => SplitPascal(Tag.ToString());

    public string TagSpriteKey => Tag.ToString();

    private static string SplitPascal(string name) =>
        string.Concat(name.Select((c, i) => i > 0 && char.IsUpper(c) ? " " + c : c.ToString()));
}

/// <summary>UI model for one shop queue entry.</summary>
public class ShopItemModel
{
    public MotelyItemTypeCategory TypeCategory { get; set; }

    public MotelyItemType ItemType { get; set; }

    public MotelyItemEdition Edition { get; set; }

    public string ItemName => ItemType.ToString();

    /// <summary>Sprite lookup key (engine enum name, e.g. "Joker4").</summary>
    public string SpriteKey => ItemType.ToString();

    public bool HasEdition => Edition.ToString() != "None";

    public string EditionName => HasEdition ? Edition.ToString() : string.Empty;

    /// <summary>
    /// Text color for the edition label, from the app palette (App.axaml): Foil = blue (chips),
    /// Holographic = red (mult), Polychrome = purple, Negative = bright silver (readable on the dark item boxes). Fixed brushes, not
    /// resource lookups, because this model is built off the visual tree.
    /// </summary>
    public IBrush EditionColor =>
        Edition switch
        {
            MotelyItemEdition.Foil => FoilBrush,
            MotelyItemEdition.Holographic => HolographicBrush,
            MotelyItemEdition.Polychrome => PolychromeBrush,
            MotelyItemEdition.Negative => NegativeBrush,
            _ => Brushes.White,
        };

    private static readonly IBrush FoilBrush = new SolidColorBrush(Color.Parse("#0093ff")).ToImmutable();
    private static readonly IBrush HolographicBrush = new SolidColorBrush(Color.Parse("#ff4c40")).ToImmutable();
    private static readonly IBrush PolychromeBrush = new SolidColorBrush(Color.Parse("#7d60e0")).ToImmutable();
    private static readonly IBrush NegativeBrush = new SolidColorBrush(Color.Parse("#b9c2d2")).ToImmutable();
}

/// <summary>UI model for one booster pack of an ante.</summary>
public class BoosterPackModel
{
    public MotelyBoosterPackType PackType { get; set; }

    public string PackName => PackType.ToString();

    public string PackSpriteKey => PackType.ToString();

    public List<string> Items { get; } = new();

    public bool HasItems => Items.Count > 0;

    public string ItemsText => string.Join(", ", Items);
}

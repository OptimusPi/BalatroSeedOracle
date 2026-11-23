using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using Motely;
using Motely.Analysis;
using EngineItem = Motely.MotelyItem;

namespace BalatroSeedOracle.Analysis;

/// <summary>Which seed, deck and stake to break down, and how deep to walk.</summary>
public sealed record class MotelyUnitTestAnalysisConfig(
    string Seed,
    MotelyDeck Deck,
    MotelyStake Stake,
    int MaxAnte = MotelyUnitTestAnalyzer.DefaultMaxAnte,
    int ShopSlots = 0,
    int MinAnte = 0
);

/// <summary>
/// One seed's per-ante breakdown for the desktop views, read from the engine's Jamlyzer
/// (<see cref="MotelyJamlyzer"/>). <see cref="ToString"/> is the classic "The Soul" text block.
/// </summary>
public sealed record class MotelyUnitTestAnalysis(
    string? Error,
    IReadOnlyList<MotelyAnteAnalysis> Antes,
    MotelyDeck? Deck = null,
    IReadOnlyList<MotelyJamlyzerLegendary>? LegendaryOrder = null,
    int MaxAnte = MotelyUnitTestAnalyzer.DefaultMaxAnte,
    int ShopSlots = 0
)
{
    public override string ToString()
    {
        if (!string.IsNullOrEmpty(Error))
            return $"❌ Error analyzing seed: {Error}";

        StringBuilder sb = new();
        foreach (var ante in Antes)
        {
            sb.AppendLine($"==ANTE {ante.Ante}==");
            sb.AppendLine($"Boss: {FormatUtils.FormatBoss(ante.Boss)}");
            sb.AppendLine($"Voucher: {FormatUtils.FormatVoucher(ante.Voucher)}");
            sb.AppendLine(
                $"Tags: {FormatUtils.FormatTag(ante.SmallBlindTag)}, {FormatUtils.FormatTag(ante.BigBlindTag)}"
            );

            sb.AppendLine("Shop Queue: ");
            for (int i = 0; i < ante.ShopQueue.Count; i++)
                sb.AppendLine($"{i + 1}) {FormatUtils.FormatItem(ante.ShopQueue[i].Item)}");
            sb.AppendLine();

            sb.AppendLine("Packs: ");
            foreach (var pack in ante.Packs)
            {
                var contents =
                    pack.Items.Count > 0
                        ? " - " + string.Join(", ", pack.Items.Select(item => FormatUtils.FormatItem(item.Item)))
                        : "";
                sb.AppendLine($"{FormatUtils.FormatPackName(pack.Type)}{contents}");
            }
            sb.AppendLine();
        }

        return sb.ToString();
    }
}

public sealed record class MotelyAnteAnalysis(
    int Ante,
    MotelyBossBlind Boss,
    MotelyVoucher Voucher,
    MotelyTag SmallBlindTag,
    MotelyTag BigBlindTag,
    IReadOnlyList<MotelyAnalyzedItem> ShopQueue,
    IReadOnlyList<MotelyBoosterPackAnalysis> Packs
);

/// <summary>An engine item with its facets exposed as properties, so views can bind to them.</summary>
public sealed record class MotelyAnalyzedItem([property: JsonIgnore] EngineItem Item)
{
    public string Name => FormatUtils.FormatItem(Item);
    public int Value => Item.Value;
    public MotelyItemType Type => Item.Type;
    public MotelyItemTypeCategory TypeCategory => Item.TypeCategory;
    public MotelyItemSeal Seal => Item.Seal;
    public MotelyItemEnhancement Enhancement => Item.Enhancement;
    public MotelyItemEdition Edition => Item.Edition;
    public MotelyStandardcardSuit StandardcardSuit => Item.StandardcardSuit;
    public MotelyStandardcardRank StandardcardRank => Item.StandardcardRank;
    public bool IsPerishable => Item.IsPerishable;
    public bool IsEternal => Item.IsEternal;
    public bool IsRental => Item.IsRental;
    public bool IsInvalid => Item.IsInvalid;

    public static implicit operator EngineItem(MotelyAnalyzedItem item) => item.Item;
}

public sealed record class MotelyBoosterPackAnalysis(
    MotelyBoosterPack Type,
    IReadOnlyList<MotelyAnalyzedItem> Items
);

public static class MotelyUnitTestAnalyzer
{
    /// <summary>Default analyze depth: past ante 8 so Cola / Negative Tag / Oops runs show up.</summary>
    public const int DefaultMaxAnte = 16;

    /// <summary>
    /// Break down one seed from <paramref name="cfg"/>.MinAnte through MaxAnte (inclusive).
    /// Uses the same Jamlyzer window as <see cref="MotelySeedLocator"/> so
    /// <c>[0, maxAnte]</c> is a contiguous range, not two discrete antes.
    /// </summary>
    public static MotelyUnitTestAnalysis Analyze(MotelyUnitTestAnalysisConfig cfg)
    {
        try
        {
            int maxAnte = Math.Clamp(cfg.MaxAnte, 0, MotelyGlobals.MaxAnte);
            int minAnte = Math.Clamp(cfg.MinAnte, 0, maxAnte);
            int shopSlots = Math.Clamp(cfg.ShopSlots, 0, MotelyJamlyzer.MaxShopSlots);

            // MotelyJamlyzerWindow reads first/last as StartAnte..MaxAnte (contiguous).
            int[] antes = minAnte == 0 ? [0, maxAnte] : [minAnte, maxAnte];

            var desc = new MotelyJamlyzerFilterDesc(
                antes,
                eventRolls: 0,
                resumeFrom: null,
                shopSlots,
                opensAnteZeroSouls: false
            );

            using (
                var search = new MotelySearchSettings<MotelyJamlyzerFilterDesc.JamlyzerFilter>(desc)
                    .WithDeck(cfg.Deck)
                    .WithStake(cfg.Stake)
                    .WithSeedList([MotelyGlobals.NormalizeSeed(cfg.Seed)])
                    .WithThreadCount(1)
                    .CreateSearch()
            )
            {
                search.Start();
                search.AwaitCompletion();
            }

            var result = desc.Result;
            if (result is null)
                return new MotelyUnitTestAnalysis(
                    $"'{cfg.Seed}' is not a Balatro seed.",
                    [],
                    cfg.Deck,
                    MaxAnte: maxAnte,
                    ShopSlots: shopSlots
                );

            var anteRows = result
                .Antes.Where(a => a.Ante >= minAnte && a.Ante <= maxAnte)
                .Select(a => new MotelyAnteAnalysis(
                    a.Ante,
                    a.Boss,
                    a.Voucher,
                    a.SmallBlindTag,
                    a.BigBlindTag,
                    [.. a.ShopItems.Select(i => new MotelyAnalyzedItem(i))],
                    [
                        .. a.Packs.Select(p => new MotelyBoosterPackAnalysis(
                            p.Pack,
                            [.. p.Items.Select(i => new MotelyAnalyzedItem(i))]
                        )),
                    ]
                ))
                .ToList();

            return new MotelyUnitTestAnalysis(
                null,
                anteRows,
                cfg.Deck,
                result.LegendaryOrder,
                maxAnte,
                shopSlots
            );
        }
        catch (Exception ex)
        {
            return new MotelyUnitTestAnalysis(ex.Message, [], cfg.Deck);
        }
    }
}

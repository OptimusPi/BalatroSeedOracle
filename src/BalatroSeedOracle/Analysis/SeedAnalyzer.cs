using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using Motely;
using Motely.Analysis;
using Motely.Filters;
using EngineItem = Motely.MotelyItem;

namespace BalatroSeedOracle.Analysis;

/// <summary>Which seed, deck and stake to break down.</summary>
public sealed record class MotelyUnitTestAnalysisConfig(string Seed, MotelyDeck Deck, MotelyStake Stake);

/// <summary>
/// One seed's per-ante breakdown for the desktop views, read from the engine's Jamlyzer
/// (<see cref="MotelyJamlyzer"/>). <see cref="ToString"/> is the classic "The Soul" text block.
/// </summary>
public sealed record class MotelyUnitTestAnalysis(
    string? Error,
    IReadOnlyList<MotelyAnteAnalysis> Antes,
    MotelyDeck? Deck = null
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
    /// <summary>Antes 1-8 of one seed: boss, voucher, tags, shop queue and packs.</summary>
    public static MotelyUnitTestAnalysis Analyze(MotelyUnitTestAnalysisConfig cfg)
    {
        try
        {
            var config = new JamlConfig
            {
                Deck = cfg.Deck,
                Stake = cfg.Stake,
                Seeds = [MotelyGlobals.NormalizeSeed(cfg.Seed)],
            };

            var result = MotelyJamlyzer.Analyze(config, eventRolls: 0).FirstOrDefault();
            if (result is null)
                return new MotelyUnitTestAnalysis($"'{cfg.Seed}' is not a Balatro seed.", [], cfg.Deck);

            var antes = result
                .Antes.Where(a => a.Ante >= 1)
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

            return new MotelyUnitTestAnalysis(null, antes, cfg.Deck);
        }
        catch (Exception ex)
        {
            return new MotelyUnitTestAnalysis(ex.Message, [], cfg.Deck);
        }
    }
}

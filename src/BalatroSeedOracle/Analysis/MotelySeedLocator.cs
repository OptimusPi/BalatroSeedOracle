using System;
using System.Collections.Generic;
using System.Linq;
using Motely;
using Motely.Analysis;
using Motely.Filters;

namespace BalatroSeedOracle.Analysis;

/// <summary>Where in an ante a <see cref="MotelySeedLocator"/> hit sits.</summary>
public enum MotelyLocateSource
{
    Boss,
    Voucher,
    SmallBlindTag,
    BigBlindTag,
    Shop,
    Pack,
    Soul,
    UncommonTag,
    RareTag,
    Judgement,
    Wraith,
    Emperor,
    PurpleSeal,
    SixthSense,
    Seance,
    RiffRaff,
}

/// <summary>
/// One place an item appears in a seed.
/// </summary>
/// <param name="Ante">The ante.</param>
/// <param name="Source">Which list it is in.</param>
/// <param name="Position">The 1-based number a player counts: the 11th shop card is 11, the 3rd
/// card of a pack is 3, the 3rd Uncommon Tag pull is 3, the Nth Soul opened in the run is N. Emperor
/// counts its cards, two per use. 1 for a boss, voucher or blind tag.</param>
/// <param name="Where">The source and position in words: "shop card #11", "Mega Spectral Pack
/// (pack 3) card 3", "Uncommon Tag pull #3", "Soul #1 in run order (Arcana Pack, pack 2)", "Boss".</param>
/// <param name="Item">The item as the analyzer prints it, edition and stickers included.</param>
/// <param name="Edition">The item's edition.</param>
/// <param name="PackNumber">For a pack card or a Soul: the 1-based pack of the ante it sits in; else 0.</param>
public sealed record MotelyLocateHit(
    int Ante,
    MotelyLocateSource Source,
    int Position,
    string Where,
    string Item,
    MotelyItemEdition Edition,
    int PackNumber = 0
);

/// <summary>
/// Everywhere an item appears in one seed, and how far the look went.
/// </summary>
/// <param name="Name">The resolved name ("Diet Cola"), with the edition when the request named one.</param>
/// <param name="MinAnte">First ante read.</param>
/// <param name="MaxAnte">Last ante read.</param>
/// <param name="EventRolls">Rolls read from each pull stream per ante.</param>
/// <param name="ShopSlots">Shop cards read per ante; 0 means the default (15 for antes 0-1, 50 after).</param>
/// <param name="Hits">Every hit, by ante, then source, then position.</param>
public sealed record MotelyLocateResult(
    string Seed,
    MotelyDeck Deck,
    MotelyStake Stake,
    string Name,
    int MinAnte,
    int MaxAnte,
    int EventRolls,
    int ShopSlots,
    IReadOnlyList<MotelyLocateHit> Hits
)
{
    /// <summary>How deep the look went, in words: "antes 1-12, 20 pull rolls and 15-50 shop cards per ante".</summary>
    public string Depth
    {
        get
        {
            string antes = MinAnte == MaxAnte ? $"ante {MinAnte}" : $"antes {MinAnte}-{MaxAnte}";
            string shop =
                ShopSlots > 0 ? $"{ShopSlots}"
                : MaxAnte <= 1 ? "15"
                : MinAnte >= 2 ? "50"
                : "15-50";
            return $"{antes}, {EventRolls} pull rolls and {shop} shop cards per ante";
        }
    }
}

/// <summary>
/// "Where is X in this seed": every ante and source an item appears in. One walk of the Jamlyzer
/// (<see cref="MotelyJamlyzerSeedWalk"/>) over the antes asked for, then a scan of what it read:
/// boss, voucher, both blind tags, the shop queue, every pack's cards, the Souls in run order, and
/// the pull streams (Uncommon and Rare Tag, Judgement, Wraith, Emperor, Purple Seal, Sixth Sense,
/// Seance, Riff-raff). The raw shop-source queues are not places a card is seen, so they are not
/// read; neither is the voucher stream past each ante's voucher.
/// </summary>
public static class MotelySeedLocator
{
    /// <summary>The deepest ante a locate walks.</summary>
    public const int MaxAnte = 39;

    /// <summary>
    /// Every place <paramref name="item"/> appears in <paramref name="seed"/> between
    /// <paramref name="minAnte"/> and <paramref name="maxAnte"/>.
    /// </summary>
    /// <param name="seed">The seed.</param>
    /// <param name="deck">The deck; it changes what the seed holds.</param>
    /// <param name="stake">The stake.</param>
    /// <param name="item">A joker, tarot, planet, spectral card, voucher, tag or boss name, read
    /// as shorthand reads one: any case, spaces and punctuation optional, a leading "The" optional.
    /// A bare name matches every edition and each hit reports its own; an edition first word
    /// ("Negative Blueprint", jokers only) keeps only that edition.</param>
    /// <param name="minAnte">First ante, 0 (the pre-run shop) or more.</param>
    /// <param name="maxAnte">Last ante, at most <see cref="MaxAnte"/>.</param>
    /// <param name="eventRolls">Rolls read from each pull stream per ante.</param>
    /// <param name="shopSlots">Shop cards read per ante; 0 is the analyzer's default (15 for antes 0-1, 50 after).</param>
    /// <exception cref="FormatException">The name is unknown or ambiguous; the message is the
    /// shorthand's one-line correction.</exception>
    /// <exception cref="ArgumentException">A bad seed, ante range, roll count or shop depth.</exception>
    public static MotelyLocateResult Locate(
        string seed,
        MotelyDeck deck,
        MotelyStake stake,
        string item,
        int minAnte = 1,
        int maxAnte = 8,
        int eventRolls = 20,
        int shopSlots = 0
    )
    {
        if (minAnte < 0 || maxAnte > MaxAnte || minAnte > maxAnte)
            throw new ArgumentOutOfRangeException(
                nameof(maxAnte),
                $"antes {minAnte}-{maxAnte}: give a range within 0-{MaxAnte}, low to high."
            );

        var (entry, edition) = JamlShorthand.ResolveName(item);
        var target = new Target(entry, edition);

        // The walk always starts at ante 1 (or 0 after Hieroglyph/Petroglyph, or when asked): the
        // Souls' run order counts from the first Soul opened, so a later minAnte trims hits, not the walk.
        int[] antes = minAnte == 0 ? [0, maxAnte] : [1, maxAnte];
        var desc = new MotelyJamlyzerFilterDesc(antes, eventRolls, resumeFrom: null, shopSlots, opensAnteZeroSouls: false);
        using (
            var search = new MotelySearchSettings<MotelyJamlyzerFilterDesc.JamlyzerFilter>(desc)
                .WithDeck(deck)
                .WithStake(stake)
                .WithSeedList([seed])
                .WithThreadCount(1)
                .CreateSearch()
        )
        {
            search.Start();
            search.AwaitCompletion();
        }

        var walk =
            desc.Result
            ?? throw new ArgumentException(
                $"'{seed}' is not a searchable seed (1-{MotelyGlobals.MaxSeedLength} characters, no '0').",
                nameof(seed)
            );

        var hits = new List<MotelyLocateHit>();
        foreach (var ante in walk.Antes)
        {
            if (ante.Ante < minAnte || ante.Ante > maxAnte)
                continue;
            Scan(ante, walk.LegendaryOrder, target, hits);
        }

        return new(walk.Seed, deck, stake, target.Name, minAnte, maxAnte, eventRolls, shopSlots, hits);
    }

    private static void Scan(
        MotelyJamlyzerAnteResult ante,
        IReadOnlyList<MotelyJamlyzerLegendary> legendaryOrder,
        Target target,
        List<MotelyLocateHit> hits
    )
    {
        int a = ante.Ante;
        switch (target.Family)
        {
            case JamlShorthand.Family.Boss:
                if ((int)ante.Boss == target.Value)
                    hits.Add(new(a, MotelyLocateSource.Boss, 1, "Boss", FormatUtils.FormatBoss(ante.Boss), MotelyItemEdition.None));
                return;
            case JamlShorthand.Family.Voucher:
                if ((int)ante.Voucher == target.Value)
                    hits.Add(new(a, MotelyLocateSource.Voucher, 1, "Voucher", FormatUtils.FormatVoucher(ante.Voucher), MotelyItemEdition.None));
                return;
            case JamlShorthand.Family.Tag:
                if ((int)ante.SmallBlindTag == target.Value)
                    hits.Add(new(a, MotelyLocateSource.SmallBlindTag, 1, "Small Blind Tag", FormatUtils.FormatTag(ante.SmallBlindTag), MotelyItemEdition.None));
                if ((int)ante.BigBlindTag == target.Value)
                    hits.Add(new(a, MotelyLocateSource.BigBlindTag, 1, "Big Blind Tag", FormatUtils.FormatTag(ante.BigBlindTag), MotelyItemEdition.None));
                return;
        }

        for (int i = 0; i < ante.ShopItems.Count; i++)
            if (target.Matches(ante.ShopItems[i]))
                hits.Add(Card(a, MotelyLocateSource.Shop, i + 1, $"shop card #{i + 1}", ante.ShopItems[i]));

        for (int p = 0; p < ante.Packs.Count; p++)
        {
            var pack = ante.Packs[p];
            for (int i = 0; i < pack.Items.Count; i++)
                if (target.Matches(pack.Items[i]))
                    hits.Add(
                        Card(a, MotelyLocateSource.Pack, i + 1, $"{FormatUtils.FormatPackName(pack.Pack)} (pack {p + 1}) card {i + 1}", pack.Items[i]) with
                        {
                            PackNumber = p + 1,
                        }
                    );
        }

        for (int n = 0; n < legendaryOrder.Count; n++)
        {
            var soul = legendaryOrder[n];
            if (soul.Ante != a || !target.Matches(soul.Item))
                continue;
            string pack = soul.PackSlot < ante.Packs.Count ? FormatUtils.FormatPackName(ante.Packs[soul.PackSlot].Pack) + ", " : "";
            hits.Add(
                Card(a, MotelyLocateSource.Soul, n + 1, $"Soul #{n + 1} in run order ({pack}pack {soul.PackSlot + 1})", soul.Item) with
                {
                    PackNumber = soul.PackSlot + 1,
                }
            );
        }

        var pulls = ante.Pulls;
        Pulls(a, MotelyLocateSource.UncommonTag, "Uncommon Tag pull", pulls.UncommonTagJokers, target, hits);
        Pulls(a, MotelyLocateSource.RareTag, "Rare Tag pull", pulls.RareTagJokers, target, hits);
        Pulls(a, MotelyLocateSource.Judgement, "Judgement pull", pulls.JudgementJokers, target, hits);
        Pulls(a, MotelyLocateSource.Wraith, "Wraith pull", pulls.WraithJokers, target, hits);
        Pulls(a, MotelyLocateSource.Emperor, "Emperor card", pulls.EmperorTarots, target, hits);
        Pulls(a, MotelyLocateSource.PurpleSeal, "Purple Seal pull", pulls.PurpleSealTarots, target, hits);
        Pulls(a, MotelyLocateSource.SixthSense, "Sixth Sense pull", pulls.SixthSenseSpectrals, target, hits);
        Pulls(a, MotelyLocateSource.Seance, "Seance pull", pulls.SeanceSpectrals, target, hits);
        Pulls(a, MotelyLocateSource.RiffRaff, "Riff-raff pull", pulls.RiffRaffJokers, target, hits);
    }

    private static void Pulls(
        int ante,
        MotelyLocateSource source,
        string label,
        IReadOnlyList<Motely.MotelyItem> items,
        Target target,
        List<MotelyLocateHit> hits
    )
    {
        for (int i = 0; i < items.Count; i++)
            if (target.Matches(items[i]))
                hits.Add(Card(ante, source, i + 1, $"{label} #{i + 1}", items[i]));
    }

    private static MotelyLocateHit Card(int ante, MotelyLocateSource source, int position, string where, Motely.MotelyItem item) =>
        new(ante, source, position, where, FormatUtils.FormatItem(item), item.Edition);

    /// <summary>The item a locate looks for: its family, its item type for cards, and an edition when the name gave one.</summary>
    private sealed class Target
    {
        public Target(JamlShorthand.Entry entry, MotelyItemEdition? edition)
        {
            Family = entry.Family;
            Value = entry.Value;
            Edition = edition;
            Type = entry.Family switch
            {
                JamlShorthand.Family.Joker => new Motely.MotelyItem((MotelyJoker)entry.Value).Type,
                JamlShorthand.Family.Tarot => (MotelyItemType)((int)MotelyItemTypeCategory.TarotCard | entry.Value),
                JamlShorthand.Family.Planet => (MotelyItemType)((int)MotelyItemTypeCategory.PlanetCard | entry.Value),
                JamlShorthand.Family.Spectral => (MotelyItemType)((int)MotelyItemTypeCategory.SpectralCard | entry.Value),
                _ => null,
            };
            string display = entry.Family switch
            {
                JamlShorthand.Family.Boss => FormatUtils.FormatBoss((MotelyBossBlind)entry.Value),
                JamlShorthand.Family.Voucher => FormatUtils.FormatVoucher((MotelyVoucher)entry.Value),
                JamlShorthand.Family.Tag => FormatUtils.FormatTag((MotelyTag)entry.Value),
                _ => FormatUtils.FormatItem(new Motely.MotelyItem(Type!.Value)),
            };
            Name = edition is { } ed ? $"{ed} {display}" : display;
        }

        public JamlShorthand.Family Family { get; }
        public int Value { get; }
        public MotelyItemType? Type { get; }
        public MotelyItemEdition? Edition { get; }
        public string Name { get; }

        public bool Matches(Motely.MotelyItem item) =>
            Type is { } type && item.Type == type && (Edition is not { } ed || item.Edition == ed);
    }
}

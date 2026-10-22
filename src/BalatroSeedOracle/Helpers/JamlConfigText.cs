using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text.Json.Nodes;
using Motely.Filters;

namespace BalatroSeedOracle.Helpers;

/// <summary>
/// Serializes a <see cref="JamlConfig"/> to JAML text.
/// TIER0-STUB: replace in Tier 1. The engine exposes JamlConfigLoader (text → config) and
/// text↔text conversion, but no config → text serializer, so this helper walks the config
/// graph into a JSON tree and lets the engine's own JamlConfigLoader.ToYaml render it.
/// Output is designed to round-trip through JamlConfigLoader.TryLoad; clause discriminator
/// keys mirror the engine's JamlClauseBinding.ClauseTypeByKey.
/// </summary>
public static class JamlConfigText
{
    /// <summary>Clause type → its JAML discriminator key (canonical singular forms).</summary>
    private static readonly IReadOnlyDictionary<Type, string> KeyByClauseType =
        new Dictionary<Type, string>
        {
            [typeof(JokerClause)] = "joker",
            [typeof(CommonJokerClause)] = "commonJoker",
            [typeof(UncommonJokerClause)] = "uncommonJoker",
            [typeof(RareJokerClause)] = "rareJoker",
            [typeof(LegendaryJokerClause)] = "legendaryJoker",
            [typeof(PlanetCardClause)] = "planetCard",
            [typeof(SpectralCardClause)] = "spectralCard",
            [typeof(TarotCardClause)] = "tarotCard",
            [typeof(StandardCardClause)] = "standardCard",
            [typeof(ErraticRankClause)] = "erraticRank",
            [typeof(ErraticSuitClause)] = "erraticSuit",
            [typeof(VoucherClause)] = "voucher",
            [typeof(TagClause)] = "tag",
            [typeof(SmallBlindTagClause)] = "smallBlindTag",
            [typeof(BigBlindTagClause)] = "bigBlindTag",
            [typeof(BossClause)] = "boss",
            [typeof(BoosterPackClause)] = "boosterPack",
            [typeof(PokerHandClause)] = "pokerHand",
            [typeof(StartingDrawClause)] = "startingDraw",
            [typeof(LuckyMoneyClause)] = "luckyMoney",
            [typeof(LuckyMultClause)] = "luckyMult",
            [typeof(MisprintMultClause)] = "misprintMult",
            [typeof(WheelOfFortuneClause)] = "wheelOfFortune",
            [typeof(WheelStaysFlippedClause)] = "wheelStaysFlipped",
            [typeof(GlassDestroyClause)] = "glassDestroy",
            [typeof(CavendishExtinctClause)] = "cavendishExtinct",
            [typeof(GrosMichelExtinctClause)] = "grosMichelExtinct",
            [typeof(SpaceLevelupClause)] = "spaceLevelup",
            [typeof(BusinessPayoutClause)] = "businessPayout",
            [typeof(ParkingPayoutClause)] = "parkingPayout",
            [typeof(BloodstoneTriggerClause)] = "bloodstoneTrigger",
            [typeof(AndClause)] = "and",
            [typeof(OrClause)] = "or",
            [typeof(LegendarySoulOrderClause)] = "soulOrder",
        };

    /// <summary>The filter as JAML text, loadable again through <see cref="JamlConfigLoader.TryLoad"/>.</summary>
    public static string ToJaml(JamlConfig config)
    {
        ArgumentNullException.ThrowIfNull(config);

        var root = new JsonObject
        {
            ["id"] = config.Id,
            ["deck"] = config.Deck.ToString(),
            ["stake"] = config.Stake.ToString(),
            ["must"] = ClauseArray(config.Must),
            ["should"] = ClauseArray(config.Should),
            ["mustNot"] = ClauseArray(config.MustNot),
        };
        if (config.Name is not null)
            root["name"] = config.Name;
        if (config.Description is not null)
            root["description"] = config.Description;
        if (config.Author is not null)
            root["author"] = config.Author;
        if (config.Seeds.Count > 0)
            root["seeds"] = new JsonArray(config.Seeds.Select(seed => (JsonNode)seed).ToArray());
        if (config.Cutoff is { } cutoff)
            root["cutoff"] = cutoff;

        // The engine's own JSON → YAML conversion guarantees the loader reads the result
        // back exactly as it reads the original document.
        return JamlConfigLoader.ToYaml(root.ToJsonString());
    }

    private static JsonArray ClauseArray(IEnumerable<IMotelyClause> clauses) =>
        new(clauses.Select(clause => (JsonNode)ClauseNode(clause)).ToArray());

    /// <summary>One clause item: a single-entry mapping from its discriminator key to its payload.</summary>
    private static JsonObject ClauseNode(IMotelyClause clause)
    {
        var type = clause.GetType();
        if (!KeyByClauseType.TryGetValue(type, out var key))
            throw new InvalidOperationException(
                $"JamlConfigText: no JAML discriminator key registered for clause type {type.Name}."
            );
        return new JsonObject { [key] = ClausePayload(clause) };
    }

    private static JsonNode ClausePayload(IMotelyClause clause)
    {
        // Logic clauses carry their children in a payload mapping the engine's And/Or
        // converters read (sequence or { mode, clauses, … }); defaults stay implicit.
        if (clause is LogicClause logic)
        {
            var payload = new JsonObject { ["clauses"] = ClauseArray(logic.Clauses) };
            if (logic.Mode != LogicScoreMode.Sum)
                payload["mode"] = logic.Mode.ToString();
            if (logic.Min != 1)
                payload["min"] = logic.Min;
            if (logic.Max is { } max)
                payload["max"] = max;
            if (logic.Score != 1)
                payload["score"] = logic.Score;
            if (logic.Label is { } label)
                payload["label"] = label;
            return payload;
        }

        return WalkObject(clause);
    }

    /// <summary>
    /// Public readable properties of a clause/payload object as a JSON mapping. Reference-equal
    /// duplicates are skipped, which drops the engine's plural alias properties (Joker/Jokers,
    /// Voucher/Vouchers, …) that share one backing array. JamlEventRolls aliases are skipped:
    /// their data is carried by the plain <c>rolls</c> property the clause also exposes.
    /// </summary>
    private static JsonObject WalkObject(object instance)
    {
        var map = new JsonObject();
        var seen = new HashSet<object>(ReferenceEqualityComparer.Instance);
        foreach (
            var prop in instance
                .GetType()
                .GetProperties(BindingFlags.Public | BindingFlags.Instance)
        )
        {
            if (!prop.CanRead || prop.GetIndexParameters().Length != 0)
                continue;
            if (prop.PropertyType == typeof(JamlEventRolls))
                continue;
            var value = prop.GetValue(instance);
            if (value is null)
                continue;
            if (value is not ValueType && !seen.Add(value))
                continue;
            map[Camel(prop.Name)] = WalkValue(value);
        }
        return map;
    }

    private static JsonNode WalkValue(object value) =>
        value switch
        {
            IMotelyClause clause => ClauseNode(clause),
            Enum e => Enum.GetName(e.GetType(), e) ?? e.ToString(),
            string s => s,
            bool b => b,
            int i => i,
            long l => l,
            double d => d,
            IEnumerable items => new JsonArray(items.Cast<object?>().Select(WalkItem).ToArray()),
            _ => WalkObject(value),
        };

    private static JsonNode? WalkItem(object? item) => item is null ? null : WalkValue(item);

    private static string Camel(string name) =>
        string.Create(name.Length, name, static (span, source) =>
        {
            source.AsSpan().CopyTo(span);
            span[0] = char.ToLowerInvariant(span[0]);
        });
}

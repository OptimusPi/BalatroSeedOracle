using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Motely;
using Motely.Filters;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace BalatroSeedOracle.Helpers;

/// <summary>One clause discriminator key (<c>joker</c>, <c>legendaryJokers</c>, <c>and</c>, …) bound to the type it selects.</summary>
public sealed record DiscriminatorInfo(string Key, Type ClauseType, PropertyInfo? ValueProperty);

/// <summary>
/// The filter dialect's live vocabulary: root keys, clause discriminators, clause properties,
/// value enums, sources blocks — all reflected from the types the loader itself binds
/// (<c>JamlConfig</c>, <c>JamlClauseBinding.ClauseTypeByKey</c>, the clause types, and every
/// property YamlDotNet can set on them). The JSON schema, the TextMate grammar, and the
/// language server all read this one table, so an editor answer is by construction an engine
/// answer — there is no second copy to keep in step.
/// </summary>
public static class FilterVocabulary
{
    private static readonly INamingConvention Naming = CamelCaseNamingConvention.Instance;

    private static readonly ConcurrentDictionary<Type, IReadOnlyList<PropertyInfo>> PropCache = new();

    // ── Tables (text order = init order; consumers follow their inputs) ───────────────

    /// <summary>
    /// Root keys in documentation order (the order a filter author meets them). Anything the
    /// engine grows beyond this list still lands — appended alphabetically — so a new root
    /// property can never hide from the schema.
    /// </summary>
    private static readonly string[] RootKeyOrder =
    [
        "id", "name", "description", "author", "dateCreated", "deck", "stake", "seeds", "cutoff",
        "defaults", "must", "should", "mustNot",
    ];

    private static readonly string[] RootPropKeys = PropertiesOf(typeof(JamlConfig))
        .Select(p => Naming.Apply(p.Name))
        .ToArray();

    public static IReadOnlyList<string> RootKeys { get; } =
    [
        .. RootKeyOrder.Where(k => RootPropKeys.Contains(k, StringComparer.OrdinalIgnoreCase)),
        .. RootPropKeys
            .Where(k => !RootKeyOrder.Contains(k, StringComparer.OrdinalIgnoreCase))
            .OrderBy(k => k, StringComparer.Ordinal),
    ];

    /// <summary>Every clause discriminator key in first-appearance order of the loader's binding table.</summary>
    public static IReadOnlyList<DiscriminatorInfo> DiscriminatorTable { get; } =
    [
        .. JamlConfigLoader.ClauseTypes.Select(kv => new DiscriminatorInfo(
            kv.Key,
            kv.Value,
            PropertiesOf(kv.Value)
                .FirstOrDefault(p => Naming.Apply(p.Name).Equals(kv.Key, StringComparison.OrdinalIgnoreCase))
        )),
    ];

    private static readonly IReadOnlyDictionary<string, Type> DiscriminatorTypes =
        DiscriminatorTable.ToDictionary(d => d.Key, d => d.ClauseType, StringComparer.OrdinalIgnoreCase);

    private static readonly HashSet<Type> ClauseTypes = [.. DiscriminatorTable.Select(d => d.ClauseType)];

    /// <summary>Root property name → enum type (<c>deck</c> → MotelyDeck, <c>stake</c> → MotelyStake).</summary>
    private static readonly KeyValuePair<string, Type>[] RootEnumKeys =
    [
        .. PropertiesOf(typeof(JamlConfig))
            .Select(p => (p, t: AsEnum(Unwrap(p.PropertyType))))
            .Where(x => x.t is not null)
            .Select(x => new KeyValuePair<string, Type>(Naming.Apply(x.p.Name), x.t!)),
    ];

    /// <summary>Every bindable property name → enum type across root and nested blocks; first occurrence wins.</summary>
    private static readonly IReadOnlyDictionary<string, Type> BlockEnumKeys = BuildBlockEnumKeys();

    /// <summary>Enum type → friendly kind label, for hover and Explain.</summary>
    private static readonly IReadOnlyDictionary<Type, string> EnumKinds = BuildEnumKinds();

    /// <summary>Enum values for hover/Explain, most specific type first (subsets before supersets).</summary>
    public static IReadOnlyList<(Type EnumType, string Kind)> ValueEnumKinds { get; } =
        EnumKinds
            .OrderBy(kv => Enum.GetNames(kv.Key).Length)
            .ThenBy(kv => kv.Key.Name, StringComparer.Ordinal)
            .Select(kv => (kv.Key, kv.Value))
            .ToArray();

    /// <summary>Keys of the <c>with:</c> block (<c>luck</c>, <c>jokers</c>).</summary>
    public static IReadOnlyList<string> WithBlockKeys { get; } =
    [
        .. PropertiesOf(typeof(MotelyWith)).Select(p => Naming.Apply(p.Name)),
    ];

    // ── Queries ────────────────────────────────────────────────────────────────────────

    /// <summary>All clause discriminator keys, exactly as the loader spells them.</summary>
    public static IReadOnlyCollection<string> Discriminators => JamlConfigLoader.ClauseKeys;

    public static bool IsDiscriminator(string word) => DiscriminatorTypes.ContainsKey(word);

    public static bool IsRootKey(string word) =>
        RootKeys.Any(k => k.Equals(word, StringComparison.OrdinalIgnoreCase));

    /// <summary>The type a discriminator key selects, or null when the word is not a clause key.</summary>
    public static Type? TypeForDiscriminator(string key) => DiscriminatorTypes.GetValueOrDefault(key);

    /// <summary>
    /// CamelCase keys a clause of this discriminator accepts — aliases included:
    /// <c>joker</c>, <c>sources</c>, <c>min</c>, … Empty for a word that is not a clause key.
    /// </summary>
    public static IReadOnlyList<string> ClauseKeysFor(string discriminator)
    {
        if (!DiscriminatorTypes.TryGetValue(discriminator, out var type))
            return [];
        return [.. ClauseProperties(type).Select(p => Naming.Apply(p.Name))];
    }

    /// <summary>
    /// CamelCase properties YamlDotNet can set on a bindable type — clause, payload, source
    /// config, slot, or the root itself — alphabetical. These are the type's accepted keys.
    /// </summary>
    public static IReadOnlyList<PropertyInfo> ClauseProperties(Type type) =>
        PropCache.GetOrAdd(type, PropertiesOf);

    /// <summary>
    /// The property the discriminator key itself binds (its value: the joker list, the roll list,
    /// the <c>and:</c> payload), or null when the alias table has no direct property for the key.
    /// </summary>
    public static PropertyInfo? ValuePropertyFor(string discriminator)
    {
        if (!DiscriminatorTypes.TryGetValue(discriminator, out var type))
            return null;
        return ClauseProperties(type)
            .FirstOrDefault(p => Naming.Apply(p.Name).Equals(discriminator, StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>The value enum behind a discriminator's own key: <c>joker</c> → MotelyJoker. Null for rolls/payload/block shorthands.</summary>
    public static Type? ValueEnumType(string discriminator)
    {
        var p = ValuePropertyFor(discriminator);
        return p is null ? null : AsEnum(Unwrap(p.PropertyType));
    }

    /// <summary>True when the clause's value is a roll list read inline (<c>luckyMoney: 3</c>).</summary>
    public static bool RollsAreInlineFor(string discriminator) =>
        ValuePropertyFor(discriminator)?.PropertyType == typeof(JamlEventRolls);

    /// <summary>
    /// Union of every <c>sources:</c>-style block on the clause — every property whose type is a
    /// <c>*SourceConfig</c> record (a clause may carry more than one, e.g. joker + legendary).
    /// </summary>
    public static IReadOnlyList<string> SourceKeysFor(string discriminator)
    {
        if (!DiscriminatorTypes.TryGetValue(discriminator, out var type))
            return [];
        var keys = new SortedSet<string>(StringComparer.Ordinal);
        foreach (var p in ClauseProperties(type))
            if (Unwrap(p.PropertyType) is { } t && t.Name.EndsWith("SourceConfig", StringComparison.Ordinal))
                foreach (var sp in ClauseProperties(t))
                    keys.Add(Naming.Apply(sp.Name));
        return [.. keys];
    }

    /// <summary>
    /// Enum type a key completes values from — <c>deck</c> → MotelyDeck, <c>edition</c> →
    /// MotelyItemEdition, <c>mode</c> → LogicScoreMode — searched across the root, every clause,
    /// and every nested block the loader can bind. Null when the key is not enum-valued.
    /// </summary>
    public static Type? EnumTypeForKey(string key)
    {
        foreach (var (k, t) in RootEnumKeys)
            if (k.Equals(key, StringComparison.OrdinalIgnoreCase))
                return t;
        return BlockEnumKeys.GetValueOrDefault(key);
    }

    /// <summary>
    /// Names under a vocabulary kind: a discriminator (<c>joker</c> → its value enum), a kind
    /// label (<c>edition foil</c> → MotelyItemEdition), or an enum type name (<c>MotelyVoucher</c>).
    /// <paramref name="query"/> filters case-insensitively. Unknown kinds throw
    /// <see cref="ArgumentException"/> — callers treat that as "not found".
    /// </summary>
    public static IReadOnlyList<string> ListItems(string kind, string? query)
    {
        var type = ResolveKind(kind);
        var names = Enum.GetNames(type);
        if (string.IsNullOrWhiteSpace(query))
            return names;
        return names.Where(n => n.Contains(query, StringComparison.OrdinalIgnoreCase)).ToArray();
    }

    // ── Build ──────────────────────────────────────────────────────────────────────────

    private static IReadOnlyDictionary<string, Type> BuildBlockEnumKeys()
    {
        var seen = new Dictionary<string, Type>(StringComparer.OrdinalIgnoreCase);
        foreach (var type in BindableTypes())
        foreach (var p in ClauseProperties(type))
        {
            var t = AsEnum(Unwrap(p.PropertyType));
            if (t is not null)
                seen.TryAdd(Naming.Apply(p.Name), t);
        }
        return seen;
    }

    private static IReadOnlyDictionary<Type, string> BuildEnumKinds()
    {
        var kinds = new Dictionary<Type, string>();
        foreach (var type in BindableTypes())
        foreach (var p in ClauseProperties(type))
            if (AsEnum(Unwrap(p.PropertyType)) is { } t)
                kinds.TryAdd(t, KindLabel(t));
        // Discriminator value enums are first-class: they are the words authors actually type.
        foreach (var d in DiscriminatorTable)
            if (ValueEnumType(d.Key) is { } t)
                kinds.TryAdd(t, KindLabel(t));
        return kinds;
    }

    /// <summary>Every type a filter document can name: the root, the clauses, and their nested blocks.</summary>
    private static IEnumerable<Type> BindableTypes()
    {
        yield return typeof(JamlConfig);
        yield return typeof(MotelyWith);
        yield return typeof(JamlStandardCardSlot);
        foreach (var t in ClauseTypes)
            yield return t;
        // Nested blocks named by clause properties: payloads, sources, soul-order entries.
        foreach (var t in ClauseTypes)
        foreach (var p in ClauseProperties(t))
        {
            var candidate = SequenceElement(p.PropertyType) ?? Unwrap(p.PropertyType);
            if (candidate is null
                || candidate == typeof(string)
                || candidate.IsPrimitive
                || candidate.IsEnum
                || ClauseTypes.Contains(candidate)
                || typeof(IMotelyClause).IsAssignableFrom(candidate))
                continue;
            yield return candidate;
        }
    }

    private static IReadOnlyList<PropertyInfo> PropertiesOf(Type type) =>
    [
        .. type
            .GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Where(IsBindable)
            .OrderBy(p => Naming.Apply(p.Name), StringComparer.Ordinal),
    ];

    private static Type ResolveKind(string kind)
    {
        var q = kind.Trim();
        if (q.Length == 0)
            throw new ArgumentException("empty vocabulary kind.", nameof(kind));

        if (ValueEnumType(q) is { } fromDisc)
            return fromDisc;

        foreach (var (type, label) in ValueEnumKinds)
            if (label.Equals(q, StringComparison.OrdinalIgnoreCase)
                || type.Name.Equals(q, StringComparison.OrdinalIgnoreCase)
                || type.Name.Equals($"Motely{q}", StringComparison.OrdinalIgnoreCase))
                return type;

        throw new ArgumentException($"unknown vocabulary kind '{kind}'.", nameof(kind));
    }

    /// <summary>Friendly label for an enum type: MotelyJokerCommon → "common joker name".</summary>
    private static string KindLabel(Type t) => t.Name switch
    {
        "MotelyJoker" => "joker name",
        "MotelyJokerCommon" => "common joker name",
        "MotelyJokerUncommon" => "uncommon joker name",
        "MotelyJokerRare" => "rare joker name",
        "MotelyJokerLegendary" => "legendary joker name",
        "MotelyJokerSticker" => "joker sticker",
        "MotelyJokerRarity" => "joker rarity",
        "MotelyPlanetCard" => "planet card",
        "MotelySpectralCard" => "spectral card",
        "MotelyTarotCard" => "tarot card",
        "MotelyVoucher" => "voucher",
        "MotelyTag" => "tag",
        "MotelyBossBlind" => "boss blind",
        "MotelyBoosterPack" => "booster pack",
        "MotelyPokerHand" => "poker hand",
        "MotelyStandardCard" => "standard card",
        "MotelyStandardcardRank" => "card rank",
        "MotelyStandardcardSuit" => "card suit",
        "MotelyItemEdition" => "item edition",
        "MotelyItemEnhancement" => "item enhancement",
        "MotelyItemSeal" => "card seal",
        "MotelyDeck" => "deck",
        "MotelyStake" => "stake",
        "MotelyLuck" => "luck multiplier",
        "LogicScoreMode" => "score mode",
        _ => Friendly(t.Name),
    };

    /// <summary>MotelyItemFoil → "item foil" — fallback for enums the map does not name.</summary>
    private static string Friendly(string typeName)
    {
        var s = typeName.StartsWith("Motely", StringComparison.Ordinal) ? typeName["Motely".Length..] : typeName;
        var words = new List<string>();
        var current = new System.Text.StringBuilder();
        foreach (var c in s)
        {
            if (char.IsUpper(c) && current.Length > 0)
            {
                words.Add(current.ToString());
                current.Clear();
            }
            current.Append(c);
        }
        if (current.Length > 0)
            words.Add(current.ToString());
        return string.Join(" ", words.Select(w => w.ToLowerInvariant()));
    }

    /// <summary>
    /// The element type when a property holds a list — <c>T[]</c> or <c>List&lt;T&gt;</c> (and the
    /// other common list interfaces); null when it holds a single value.
    /// </summary>
    public static Type? SequenceElement(Type type)
    {
        if (type.IsArray)
            return type.GetElementType();
        if (!type.IsGenericType)
            return null;
        var definition = type.GetGenericTypeDefinition();
        return definition == typeof(List<>)
            || definition == typeof(IList<>)
            || definition == typeof(IReadOnlyList<>)
            || definition == typeof(IEnumerable<>)
            || definition == typeof(ICollection<>)
            || definition == typeof(HashSet<>)
                ? type.GetGenericArguments()[0]
                : null;
    }

    /// <summary>Sequence element or <see cref="Nullable{T}"/> underlying type; otherwise the type itself.</summary>
    private static Type? Unwrap(Type t) =>
        SequenceElement(t) ?? Nullable.GetUnderlyingType(t) ?? t;

    /// <summary>The engine assembly version, stamped into generated artifacts so a stale one is obvious.</summary>
    public static string Version =>
        typeof(FilterVocabulary).Assembly.GetName().Version?.ToString() ?? "0.0.0";

    private static Type? AsEnum(Type? t) => t is { IsEnum: true } ? t : null;

    private static bool IsBindable(PropertyInfo p) =>
        (p.GetMethod ?? p.SetMethod)?.IsStatic == false
        && p.GetIndexParameters().Length == 0
        && p.SetMethod is { IsPublic: true };
}

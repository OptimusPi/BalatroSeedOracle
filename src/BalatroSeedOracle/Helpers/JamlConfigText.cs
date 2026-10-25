using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text.Json.Nodes;
using Motely.Filters;
using Motely.Schema;

namespace BalatroSeedOracle.Helpers;

/// <summary>
/// Serializes a <see cref="JamlConfig"/> to JAML text. The engine reads JAML
/// (<see cref="JamlConfigLoader"/>) and converts JSON → YAML text, but has no config → text
/// writer, so this walks the config into a JSON tree in the engine's own clause shape and lets
/// <see cref="JamlConfigLoader.ToYaml"/> render it.
/// <para>
/// Clause shape is the one every engine filter uses: a flat mapping whose discriminator key holds
/// the clause's names (<c>- boss: TheEye</c> then <c>antes: [3]</c> as a sibling); and/or carry
/// their children in a <c>clauses:</c> payload. Keys and their spelling come from
/// <see cref="FilterVocabulary"/> (the loader's binding table), and properties still at their
/// default are left out, so the text reads like a hand-written filter and loads back to the same
/// config.
/// </para>
/// </summary>
public static class JamlConfigText
{
    /// <summary>Clause type → its canonical discriminator key (the first, singular spelling in the engine table).</summary>
    private static readonly IReadOnlyDictionary<Type, string> KeyByClauseType = FilterVocabulary
        .DiscriminatorTable.GroupBy(d => d.ClauseType)
        .ToDictionary(g => g.Key, g => g.First().Key);

    /// <summary>The canonical JAML key for a clause type (<c>joker</c>, <c>boss</c>, <c>or</c>, …), or null.</summary>
    public static string? ClauseKeyFor(Type clauseType) => KeyByClauseType.GetValueOrDefault(clauseType);

    /// <summary>The filter as JAML text, loadable again through <see cref="JamlConfigLoader.TryLoad"/>.</summary>
    public static string ToJaml(JamlConfig config)
    {
        ArgumentNullException.ThrowIfNull(config);

        var root = new JsonObject();
        if (config.Id is not null)
            root["id"] = config.Id;
        if (config.Name is not null)
            root["name"] = config.Name;
        if (config.Description is not null)
            root["description"] = config.Description;
        if (config.Author is not null)
            root["author"] = config.Author;
        root["deck"] = config.Deck.ToString();
        root["stake"] = config.Stake.ToString();
        if (config.Seeds.Count > 0)
            root["seeds"] = new JsonArray(config.Seeds.Select(seed => (JsonNode)seed).ToArray());
        if (config.Cutoff is { } cutoff)
            root["cutoff"] = cutoff;
        root["must"] = ClauseArray(config.Must);
        root["should"] = ClauseArray(config.Should);
        root["mustNot"] = ClauseArray(config.MustNot);

        // The engine's own JSON → YAML conversion guarantees the loader reads the result
        // back exactly as it reads the original document.
        return JamlConfigLoader.ToYaml(root.ToJsonString());
    }

    private static JsonArray ClauseArray(IEnumerable<IMotelyClause> clauses) =>
        new(clauses.Select(clause => (JsonNode)ClauseNode(clause)).ToArray());

    /// <summary>One clause item in the engine's shape (see the class remarks).</summary>
    private static JsonObject ClauseNode(IMotelyClause clause)
    {
        var type = clause.GetType();
        var key = ClauseKeyFor(type)
            ?? throw new InvalidOperationException(
                $"JamlConfigText: the engine has no JAML key for clause type {type.Name}."
            );

        if (clause is LogicClause logic)
            return new JsonObject { [key] = LogicPayload(logic) };

        var node = new JsonObject();
        var valueProperty = FilterVocabulary.ValuePropertyFor(key);
        var value = valueProperty?.GetValue(clause);
        node[key] = value switch
        {
            // Event clauses: the key holds the roll list (which is also the plain rolls property).
            JamlEventRolls rolls => new JsonArray(rolls.Rolls.Select(r => (JsonNode)r).ToArray()),
            // Names: one reads as a scalar; none (or a write-only slot key) is the category "any".
            null or IList { Count: 0 } => "any",
            IList { Count: 1 } single when single[0] is Enum e => EnumName(e),
            _ => WalkValue(value),
        };

        var seen = new HashSet<object>(ReferenceEqualityComparer.Instance);
        if (value is IList { Count: > 0 })
            seen.Add(value);
        AddProperties(node, clause, seen, property =>
            property != valueProperty
            && !FilterVocabulary.IsDiscriminator(Camel(property.Name))
            && property.PropertyType != typeof(JamlEventRolls)
            && !(value is JamlEventRolls && property.Name == "Rolls"));
        return node;
    }

    /// <summary>
    /// Add the bindable properties of <paramref name="instance"/> to <paramref name="map"/>,
    /// leaving out nulls, values equal to a fresh instance's (defaults), and a non-empty list
    /// already written under another name (the engine's alias properties share one array).
    /// </summary>
    private static void AddProperties(JsonObject map, object instance, HashSet<object> seen, Func<PropertyInfo, bool> include)
    {
        var type = instance.GetType();
        var defaults = type.GetConstructor(Type.EmptyTypes) is not null ? Activator.CreateInstance(type) : null;
        foreach (var property in FilterVocabulary.ClauseProperties(type))
        {
            if (!property.CanRead || !include(property))
                continue;
            var propertyValue = property.GetValue(instance);
            if (propertyValue is null)
                continue;
            if (propertyValue is IList { Count: > 0 } && !seen.Add(propertyValue))
                continue;
            var walked = WalkValue(propertyValue);
            if (defaults is not null && IsDefault(property, defaults, walked))
                continue;
            map[Camel(property.Name)] = walked;
        }
    }

    private static JsonObject LogicPayload(LogicClause logic)
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

    private static bool IsDefault(PropertyInfo property, object defaults, JsonNode walked)
    {
        var defaultValue = property.GetValue(defaults);
        return defaultValue is not null && WalkValue(defaultValue).ToJsonString() == walked.ToJsonString();
    }

    /// <summary>Public readable properties of a nested block (sources, soul-order entry, …) as a mapping.</summary>
    private static JsonObject WalkObject(object instance)
    {
        var map = new JsonObject();
        AddProperties(map, instance, new HashSet<object>(ReferenceEqualityComparer.Instance), _ => true);
        return map;
    }

    private static JsonNode WalkValue(object value) =>
        value switch
        {
            IMotelyClause clause => ClauseNode(clause),
            Enum e => EnumName(e),
            string s => s,
            bool b => b,
            int i => i,
            long l => l,
            double d => d,
            JamlEventRolls rolls => new JsonArray(rolls.Rolls.Select(r => (JsonNode)r).ToArray()),
            IEnumerable items => new JsonArray(items.Cast<object?>().Select(WalkItem).ToArray()),
            _ => WalkObject(value),
        };

    private static JsonNode? WalkItem(object? item) => item is null ? null : WalkValue(item);

    private static string EnumName(Enum value) => Enum.GetName(value.GetType(), value) ?? value.ToString();

    private static string Camel(string name) =>
        name.Length == 0 ? name : char.ToLowerInvariant(name[0]) + name[1..];
}

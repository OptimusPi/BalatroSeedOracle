using System;
using System.Collections.Generic;
using System.Text;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace Motely.Filters;

// Converts BSO's legacy MotelyJsonConfig (discriminator-keyed clauses) into the
// new MotelyJAML JAML format (typed-key clauses) by emitting YAML and feeding it
// to JamlConfigLoader.TryLoad. Lets BSO keep its UI / file format unchanged
// while still driving the new JamlSearchBuilder pipeline.
public static class JamlConfigBridge
{
    public static bool TryConvertToJaml(
        MotelyJsonConfig config,
        out JamlConfig? jaml,
        out string? error,
        out string yamlText)
    {
        yamlText = EmitYaml(config);
        if (!JamlConfigLoader.TryLoad(yamlText, out jaml, out error))
            return false;
        return jaml is not null;
    }

    public static string EmitYaml(MotelyJsonConfig config)
    {
        var root = new Dictionary<object, object?>();
        if (!string.IsNullOrWhiteSpace(config.Name))
            root["name"] = config.Name;
        if (!string.IsNullOrWhiteSpace(config.Description))
            root["description"] = config.Description;
        if (!string.IsNullOrWhiteSpace(config.Author))
            root["author"] = config.Author;
        if (!string.IsNullOrWhiteSpace(config.Deck))
            root["deck"] = config.Deck;
        if (!string.IsNullOrWhiteSpace(config.Stake))
            root["stake"] = config.Stake;

        var must = EmitClauseList(config.Must);
        if (must.Count > 0) root["must"] = must;

        var should = EmitClauseList(config.Should);
        if (should.Count > 0) root["should"] = should;

        var mustNot = EmitClauseList(config.MustNot);
        if (mustNot.Count > 0) root["mustNot"] = mustNot;

        var serializer = new SerializerBuilder()
            .WithNamingConvention(CamelCaseNamingConvention.Instance)
            .ConfigureDefaultValuesHandling(DefaultValuesHandling.OmitNull)
            .Build();
        return serializer.Serialize(root);
    }

    private static List<object> EmitClauseList(List<MotelyJsonConfig.MotelyJsonFilterClause>? src)
    {
        var list = new List<object>();
        if (src is null) return list;
        foreach (var c in src)
        {
            var dict = EmitClause(c);
            if (dict is not null)
                list.Add(dict);
        }
        return list;
    }

    private static Dictionary<object, object?>? EmitClause(MotelyJsonConfig.MotelyJsonFilterClause c)
    {
        var dict = new Dictionary<object, object?>();
        var (typedKey, isCompound) = MapTypeKey(c.Type);
        if (typedKey is null)
            return null; // unknown clause type — skip, JAML loader would reject anyway

        if (isCompound)
        {
            var nested = new List<object>();
            if (c.Clauses is not null)
            {
                foreach (var child in c.Clauses)
                {
                    var childDict = EmitClause(child);
                    if (childDict is not null) nested.Add(childDict);
                }
            }
            dict[typedKey] = nested;
        }
        else if (c.Values is { Length: > 0 } values)
        {
            // typed lists in JAML are pluralised — pick the plural form.
            var pluralKey = PluralizeKey(typedKey);
            dict[pluralKey] = new List<object>(values);
        }
        else if (!string.IsNullOrWhiteSpace(c.Value))
        {
            dict[typedKey] = c.Value;
        }
        else
        {
            // Wildcard "Any" — the JAML loader accepts the singular key with value "Any".
            dict[typedKey] = "Any";
        }

        if (c.Antes is { Length: > 0 }) dict["antes"] = new List<int>(c.Antes);
        if (c.Score != 1) dict["score"] = c.Score;
        if (c.Min is int min) dict["min"] = min;
        if (!string.IsNullOrWhiteSpace(c.Label)) dict["label"] = c.Label;
        if (!string.IsNullOrWhiteSpace(c.Edition)) dict["edition"] = c.Edition;
        if (c.Stickers is { Length: > 0 } stickers) dict["stickers"] = new List<object>(stickers);
        if (!string.IsNullOrWhiteSpace(c.Seal)) dict["seal"] = c.Seal;
        if (!string.IsNullOrWhiteSpace(c.Enhancement)) dict["enhancement"] = c.Enhancement;
        if (!string.IsNullOrWhiteSpace(c.Rank)) dict["rank"] = c.Rank;
        if (!string.IsNullOrWhiteSpace(c.Suit)) dict["suit"] = c.Suit;
        if (c.Rolls is { Length: > 0 } rolls) dict["rolls"] = new List<int>(rolls);

        // Old "shopSlots" / "packSlots" become "shopItems" / "boosterPacks" on JAML clauses.
        var shop = c.ShopSlots ?? c.Sources?.ShopSlots;
        if (shop is { Length: > 0 }) dict["shopItems"] = new List<int>(shop);
        var packs = c.PackSlots ?? c.Sources?.PackSlots;
        if (packs is { Length: > 0 }) dict["boosterPacks"] = new List<int>(packs);

        return dict;
    }

    private static (string? key, bool isCompound) MapTypeKey(string type)
    {
        if (string.IsNullOrWhiteSpace(type)) return (null, false);
        return type.ToLowerInvariant() switch
        {
            "joker" => ("joker", false),
            "commonjoker" => ("commonJoker", false),
            "uncommonjoker" => ("uncommonJoker", false),
            "rarejoker" => ("rareJoker", false),
            "legendaryjoker" or "souljoker" => ("legendaryJoker", false),
            "voucher" => ("voucher", false),
            "tarotcard" or "tarot" => ("tarotCard", false),
            "spectralcard" or "spectral" => ("spectralCard", false),
            "planetcard" or "planet" => ("planetCard", false),
            "boss" => ("boss", false),
            "tag" or "smallblindtag" => ("smallBlindTag", false),
            "bigblindtag" => ("bigBlindTag", false),
            "standardcard" or "playingcard" => ("standardCard", false),
            "erraticrank" => ("erraticRank", false),
            "erraticsuit" => ("erraticSuit", false),
            "erraticcard" => ("erraticCard", false),
            "startingdraw" => ("startingDraw", false),
            "and" => ("and", true),
            "or" => ("or", true),
            _ => (null, false),
        };
    }

    private static string PluralizeKey(string singular) => singular switch
    {
        "joker" => "jokers",
        "commonJoker" => "commonJokers",
        "uncommonJoker" => "uncommonJokers",
        "rareJoker" => "rareJokers",
        "legendaryJoker" => "legendaryJokers",
        "voucher" => "vouchers",
        "tarotCard" => "tarotCards",
        "spectralCard" => "spectralCards",
        "standardCard" => "standardCards",
        _ => singular, // single-valued clause types stay singular
    };
}

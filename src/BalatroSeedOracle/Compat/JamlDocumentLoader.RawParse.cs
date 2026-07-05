using System;
using System.Collections.Generic;
using System.Linq;
using System.Collections.Frozen;
using System.Diagnostics.CodeAnalysis;
using Motely.Filters.Jaml.Converters;
using YamlDotNet.RepresentationModel;
using YamlDotNet.Serialization;

namespace Motely.Filters.Jaml;

/// <summary>
/// Raw YAML document walk for the JAML root: fills <see cref="JamlRootDocument"/> (no full-document YamlDotNet deserialize).
/// Clause fragments are still materialized into <see cref="JamlClauseUnion"/> for <see cref="JamlConfigLoader.CreateClauseFromDto"/>.
/// </summary>
public static partial class JamlDocumentLoader
{
    private static readonly FrozenSet<string> AllowedRootKeys = new[]
    {
        "id",
        "name",
        "author",
        "dateCreated",
        "description",
        "deck",
        "stake",
        "defaults",
        "must",
        "should",
        "mustNot",
        "aesthetics",
        "seeds",
        "hashtags",
    }.ToFrozenSet(StringComparer.OrdinalIgnoreCase);

    // Strict mode (default for DeserializerBuilder): unknown YAML keys throw with line+col so a
    // typo like `boses:` or `boosterPakcz:` is rejected up front instead of silently dropped.
    // Silent drops were the root cause of the v13/v14 false-positive class — a missing constraint
    // means the SIMD prefilter accepts seeds it shouldn't. The 3 `Unknown*Key_IsRejected` tests
    // in JamlConfigTests pin this behaviour. Do NOT add .IgnoreUnmatchedProperties() here.
    private static readonly IDeserializer JamlFragmentDeserializer = new DeserializerBuilder()
        .WithTypeConverter(new StandardCardValueConverter())
        .WithTypeConverter(new EnumOrAnyConverter<MotelyJoker>())
        .WithTypeConverter(new EnumOrAnyConverter<MotelyJokerCommon>())
        .WithTypeConverter(new EnumOrAnyConverter<MotelyJokerUncommon>())
        .WithTypeConverter(new EnumOrAnyConverter<MotelyJokerRare>())
        .WithTypeConverter(new EnumOrAnyConverter<MotelyJokerLegendary>())
        .Build();

    private static readonly ISerializer JamlStaticSerializer = new SerializerBuilder()
        .ConfigureDefaultValuesHandling(
            DefaultValuesHandling.OmitNull
                | DefaultValuesHandling.OmitEmptyCollections
                | DefaultValuesHandling.OmitDefaults
        )
        .WithTypeConverter(new StandardCardValueConverter())
        .WithTypeConverter(new EnumOrAnyConverter<MotelyJoker>())
        .WithTypeConverter(new EnumOrAnyConverter<MotelyJokerCommon>())
        .WithTypeConverter(new EnumOrAnyConverter<MotelyJokerUncommon>())
        .WithTypeConverter(new EnumOrAnyConverter<MotelyJokerRare>())
        .WithTypeConverter(new EnumOrAnyConverter<MotelyJokerLegendary>())
        .Build();

    public static string SerializeRoot(JamlRootDocument doc)
    {
        using var writer = new System.IO.StringWriter();
        JamlStaticSerializer.Serialize(writer, doc);
        return writer.ToString();
    }

    /// <summary>
    /// Returns canonical JAML text by applying loader normalizations and stable YAML emission.
    /// </summary>
    public static string Canonicalize(string jaml)
    {
        if (string.IsNullOrWhiteSpace(jaml))
            throw new ArgumentException("JAML content is required.", nameof(jaml));
        return NormalizeNestedLogicSyntax(jaml);
    }

    /// <summary>
    /// Nested and/or + clauses normalization, then round-trip through the YAML emitter (canonical text).
    /// </summary>
    internal static string NormalizeToCanonicalYaml(string jaml) => Canonicalize(jaml);

    private static YamlNode CloneYamlSubtree(YamlNode node) =>
        node switch
        {
            YamlScalarNode s => new YamlScalarNode(s.Value ?? "") { Style = s.Style },
            YamlSequenceNode seq => new YamlSequenceNode(seq.Select(CloneYamlSubtree)),
            YamlMappingNode map => new YamlMappingNode(
                map.Children.Select(kvp => new KeyValuePair<YamlNode, YamlNode>(
                    CloneYamlSubtree(kvp.Key),
                    CloneYamlSubtree(kvp.Value)
                ))
            ),
            _ => throw new InvalidOperationException(
                $"Unsupported YAML node in JAML: {node.GetType().Name}"
            ),
        };

    private static string YamlFragmentToString(YamlNode root)
    {
        var detached = CloneYamlSubtree(root);
        var doc = new YamlDocument(detached);
        var stream = new YamlStream(doc);
        using var writer = new StringWriter();
        stream.Save(writer, assignAnchors: false);
        return writer.ToString();
    }

    private static T DeserializeFragment<T>(YamlMappingNode mapping) =>
        JamlFragmentDeserializer.Deserialize<T>(YamlFragmentToString(mapping));

    private static List<JamlClauseUnion>? ParseClauseSequence(YamlMappingNode root, string key)
    {
        if (!TryGetYamlChild(root, key, out _, out var node) || node is not YamlSequenceNode seq)
            return null;

        var list = new List<JamlClauseUnion>(seq.Children.Count);
        var i = 0;
        foreach (var child in seq.Children)
        {
            i++;
            if (child is not YamlMappingNode map)
            {
                throw new InvalidOperationException(
                    $"'{key}' entry #{i} must be a mapping, got {child.GetType().Name}."
                );
            }

            try
            {
                list.Add(DeserializeFragment<JamlClauseUnion>(map));
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException(
                    $"Failed to parse '{key}' clause #{i}: {FormatLoadError(ex)}",
                    ex
                );
            }
        }

        return list;
    }

    private static List<string>? ParseStringSequence(YamlMappingNode root, string key)
    {
        if (!TryGetYamlChild(root, key, out _, out var node) || node is not YamlSequenceNode seq)
            return null;

        var list = new List<string>();
        foreach (var child in seq.Children)
        {
            if (child is YamlScalarNode s && s.Value != null)
                list.Add(s.Value);
        }

        return list.Count > 0 ? list : null;
    }

    private static string? ParseOptionalScalar(YamlMappingNode root, string key)
    {
        if (!TryGetYamlChild(root, key, out _, out var node) || node is not YamlScalarNode s)
            return null;
        return s.Value;
    }

    private static bool TryParseRootFromYaml(
        string normalizedJaml,
        [System.Diagnostics.CodeAnalysis.NotNullWhen(true)] out JamlRootDocument? result,
        out string? error
    )
    {
        result = null;
        error = null;

        var yaml = new YamlStream();
        using (var reader = new StringReader(normalizedJaml))
            yaml.Load(reader);

        if (yaml.Documents.Count == 0 || yaml.Documents[0].RootNode is not YamlMappingNode root)
        {
            error = "JAML document must be a YAML mapping at the root.";
            return false;
        }

        foreach (var child in root.Children)
        {
            if (child.Key is not YamlScalarNode kn || string.IsNullOrEmpty(kn.Value))
                continue;
            if (AllowedRootKeys.Contains(kn.Value))
                continue;

            var mark = kn.Start;
            var location =
                mark.Line > 0 && mark.Column > 0 ? $"on line {mark.Line}, col {mark.Column}: " : "";
            error = $"{location}Unknown property '{kn.Value}' in the top-level JAML document.";
            return false;
        }

        JamlDefaults? defaults = null;
        if (
            TryGetYamlChild(root, "defaults", out _, out var defNode)
            && defNode is YamlMappingNode defMap
        )
        {
            try
            {
                defaults = DeserializeFragment<JamlDefaults>(defMap);
            }
            catch (Exception ex)
            {
                error = $"Failed to parse 'defaults': {FormatLoadError(ex)}";
                return false;
            }
        }

        try
        {
            result = new JamlRootDocument
            {
                Id = ParseOptionalScalar(root, "id"),
                Name = ParseOptionalScalar(root, "name"),
                Author = ParseOptionalScalar(root, "author"),
                Description = ParseOptionalScalar(root, "description"),
                Deck = ParseOptionalScalar(root, "deck"),
                Stake = ParseOptionalScalar(root, "stake"),
                Defaults = defaults,
                Must = ParseClauseSequence(root, "must"),
                Should = ParseClauseSequence(root, "should"),
                MustNot = ParseClauseSequence(root, "mustNot"),
                Seeds = ParseStringSequence(root, "seeds"),
            };
        }
        catch (Exception ex)
        {
            error = ex.Message;
            return false;
        }

        return true;
    }
}

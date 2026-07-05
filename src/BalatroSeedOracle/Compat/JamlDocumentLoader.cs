// Legacy JAML document loader retained app-side: the engine's JamlConfigLoader now parses
// straight to JamlConfig/IJamlClause, while the Oracle UI edits the raw document shape
// (JamlRootDocument/JamlClauseUnion). This file carries the document-level parse verbatim
// from the engine's pre-migration loader (MotelyJAML commit 580301fd).
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.RegularExpressions;
using YamlDotNet.Core;
using YamlDotNet.RepresentationModel;

namespace Motely.Filters.Jaml;

public static partial class JamlDocumentLoader
{
    public static bool TryParseRoot(
        string jaml,
        [NotNullWhen(true)] out JamlRootDocument? doc,
        out string? error
    )
    {
        doc = null;
        error = null;
        if (string.IsNullOrWhiteSpace(jaml))
        {
            error = "JAML content is required.";
            return false;
        }
        try
        {
            var normalizedJaml = NormalizeNestedLogicSyntax(jaml);
            return TryParseRootFromYaml(normalizedJaml, out doc, out error);
        }
        catch (Exception ex)
        {
            error = FormatLoadError(ex);
            return false;
        }
    }
    private static string NormalizeNestedLogicSyntax(string jaml)
    {
        var yaml = new YamlStream();
        using (var reader = new StringReader(jaml))
            yaml.Load(reader);

        foreach (var document in yaml.Documents)
        {
            NormalizeNestedLogicSyntax(document.RootNode);
            ApplyPrimitiveSequenceFlowStyle(document.RootNode);
        }

        using var writer = new StringWriter();
        yaml.Save(writer, assignAnchors: false);
        return writer.ToString();
    }

    /// <summary>
    /// Emits scalar arrays in flow style (<c>[1, 2, 3]</c>) while leaving clause/object arrays block-style.
    /// </summary>
    private static void ApplyPrimitiveSequenceFlowStyle(YamlNode node)
    {
        switch (node)
        {
            case YamlMappingNode mapping:
                foreach (var child in mapping.Children.Values)
                    ApplyPrimitiveSequenceFlowStyle(child);
                break;
            case YamlSequenceNode sequence:
                foreach (var child in sequence.Children)
                    ApplyPrimitiveSequenceFlowStyle(child);

                if (
                    sequence.Children.Count > 0
                    && sequence.Children.All(static c => c is YamlScalarNode)
                )
                    sequence.Style = YamlDotNet.Core.Events.SequenceStyle.Flow;
                break;
        }
    }

    private static void NormalizeNestedLogicSyntax(YamlNode node)
    {
        switch (node)
        {
            case YamlMappingNode mapping:
                NormalizeNestedLogicBlock(mapping, "and");
                NormalizeNestedLogicBlock(mapping, "or");

                foreach (var child in mapping.Children.Values.ToArray())
                    NormalizeNestedLogicSyntax(child);
                break;

            case YamlSequenceNode sequence:
                foreach (var child in sequence.Children)
                    NormalizeNestedLogicSyntax(child);
                break;
        }
    }

    private static void NormalizeNestedLogicBlock(YamlMappingNode mapping, string key)
    {
        if (!TryGetYamlChild(mapping, key, out var keyNode, out var valueNode))
            return;

        if (valueNode is not YamlMappingNode legacyLogicBlock)
            return;

        if (!TryGetYamlChild(legacyLogicBlock, "clauses", out _, out var clausesNode))
            return;

        foreach (var child in legacyLogicBlock.Children)
        {
            if (child.Key is not YamlScalarNode childKey || childKey.Value == null)
                continue;

            if (string.Equals(childKey.Value, "clauses", StringComparison.OrdinalIgnoreCase))
                continue;

            if (!ContainsYamlKey(mapping, childKey.Value))
                mapping.Add(new YamlScalarNode(childKey.Value), child.Value);
        }

        mapping.Children[keyNode] = clausesNode;
    }

    private static bool ContainsYamlKey(YamlMappingNode mapping, string key) =>
        mapping
            .Children.Keys.OfType<YamlScalarNode>()
            .Any(node => string.Equals(node.Value, key, StringComparison.OrdinalIgnoreCase));

    private static bool TryGetYamlChild(
        YamlMappingNode mapping,
        string key,
        [NotNullWhen(true)] out YamlScalarNode? keyNode,
        [NotNullWhen(true)] out YamlNode? valueNode
    )
    {
        foreach (var child in mapping.Children)
        {
            if (
                child.Key is YamlScalarNode scalarNode
                && string.Equals(scalarNode.Value, key, StringComparison.OrdinalIgnoreCase)
            )
            {
                keyNode = scalarNode;
                valueNode = child.Value;
                return true;
            }
        }

        keyNode = null;
        valueNode = null;
        return false;
    }

    private static string FormatLoadError(Exception ex)
    {
        var message = ex.Message;

        if (ex is JsonException)
        {
            // STJ's UnmappedMemberHandling.Disallow message → the same shape the YAML path emits.
            var unmappedMatch = Regex.Match(
                message,
                "The JSON property '([^']+)' could not be mapped to any .NET member of type '([^']+)'",
                RegexOptions.CultureInvariant
            );
            if (unmappedMatch.Success)
            {
                var propertyName = unmappedMatch.Groups[1].Value;
                var targetType = unmappedMatch.Groups[2].Value;
                return $"Unknown property '{propertyName}' in {DescribeYamlTarget(targetType)}.";
            }
            return message;
        }

        if (ex is YamlException yamlEx)
        {
            var mark = yamlEx.Start;
            var location =
                mark.Line > 0 && mark.Column > 0
                    ? $"on line {mark.Line}, col {mark.Column}: "
                    : string.Empty;

            var unknownPropertyMatch = Regex.Match(
                message,
                "Property '([^']+)' not found on type '([^']+)'",
                RegexOptions.CultureInvariant
            );

            if (unknownPropertyMatch.Success)
            {
                var propertyName = unknownPropertyMatch.Groups[1].Value;
                var targetType = unknownPropertyMatch.Groups[2].Value;
                return $"{location}Unknown property '{propertyName}' in {DescribeYamlTarget(targetType)}.";
            }

            return $"{location}{message}";
        }

        return message;
    }

    private static string DescribeYamlTarget(string targetType)
    {
        var shortName = targetType.Contains('.')
            ? targetType[(targetType.LastIndexOf('.') + 1)..]
            : targetType;

        return shortName switch
        {
            "JamlRootDocument" => "the top-level JAML document",
            "JamlClauseUnion" => "a clause",
            "JamlSources" => "a clause's sources block",
            "JamlDefaults" => "the defaults block",
            "StandardCardValue" => "a standardCard value",
            "StandardCardConfig" => "a standardCard mapping",
            _ => targetType,
        };
    }

}

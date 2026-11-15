using System;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Xml;
using AvaloniaEdit.Highlighting;
using AvaloniaEdit.Highlighting.Xshd;

namespace BalatroSeedOracle.Helpers;

/// <summary>
/// The JAML editor's colors. The clause keys come from the engine's own list, so a clause type the
/// engine gains is colored without a change here. Colors are the app's palette (App.axaml).
/// </summary>
public static class JamlHighlighting
{
    private static readonly Lazy<IHighlightingDefinition> Lazy = new(Load);

    public static IHighlightingDefinition Definition => Lazy.Value;

    private static IHighlightingDefinition Load()
    {
        using var reader = XmlReader.Create(new StringReader(Xshd()));
        return HighlightingLoader.Load(reader, HighlightingManager.Instance);
    }

    /// <summary>The definition as AvaloniaEdit's xshd text.</summary>
    public static string Xshd()
    {
        // Longest first, so "legendaryJoker" is not read as a shorter key plus a tail.
        var clauseKeys = string.Join(
            "|",
            JamlConfigLoader.ClauseKeys.OrderByDescending(key => key.Length).Select(Regex.Escape)
        );

        // must, should and mustNot take the colors their zones have in the visual builder.
        return $"""
            <SyntaxDefinition name="JAML" xmlns="http://icsharpcode.net/sharpdevelop/syntaxdefinition/2008">
              <Color name="Comment" foreground="#777e89" fontStyle="italic" />
              <Color name="String" foreground="#ff9800" />
              <Color name="Must" foreground="#0093ff" fontWeight="bold" />
              <Color name="Should" foreground="#35bd86" fontWeight="bold" />
              <Color name="MustNot" foreground="#ff4c40" fontWeight="bold" />
              <Color name="ClauseKey" foreground="#eaba44" />
              <Color name="Key" foreground="#b9c2d2" />
              <Color name="Number" foreground="#9B7EDE" />
              <RuleSet>
                <Span color="Comment" begin="(?&lt;=^|\s)#" />
                <Span color="String" begin="&quot;" end="&quot;">
                  <RuleSet>
                    <Span begin="\\" end="." />
                  </RuleSet>
                </Span>
                <Span color="String" begin="'" end="'" />
                <Rule color="Must">^must(?=\s*:)</Rule>
                <Rule color="Should">^should(?=\s*:)</Rule>
                <Rule color="MustNot">^mustNot(?=\s*:)</Rule>
                <Rule color="ClauseKey">(?&lt;![\w.])(!({clauseKeys})(?![\w.])|({clauseKeys})(?=\s*:))</Rule>
                <Rule color="Key">[A-Za-z_][\w]*(?=\s*:)</Rule>
                <Rule color="Number">(?&lt;![\w.])-?\d+(?![\w.])</Rule>
              </RuleSet>
            </SyntaxDefinition>
            """;
    }
}

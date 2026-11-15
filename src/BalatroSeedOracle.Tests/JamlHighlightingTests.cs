using System.Collections.Generic;
using System.Linq;
using AvaloniaEdit.Document;
using AvaloniaEdit.Highlighting;
using BalatroSeedOracle.Helpers;
using Motely.Filters;
using Xunit;

namespace BalatroSeedOracle.Tests;

/// <summary>The JAML editor's colors: which color each piece of a filter line gets.</summary>
public class JamlHighlightingTests
{
    /// <summary>Each colored piece of <paramref name="line"/> with its color's name.</summary>
    private static List<(string Text, string Color)> Colors(string line)
    {
        var document = new TextDocument(line);
        var highlighter = new DocumentHighlighter(document, JamlHighlighting.Definition);
        return highlighter
            .HighlightLine(1)
            .Sections.Where(section => section.Color?.Name is not null)
            .Select(section => (document.GetText(section.Offset, section.Length), section.Color!.Name!))
            .ToList();
    }

    [Theory]
    [InlineData("must:", "must", "Must")]
    [InlineData("should:", "should", "Should")]
    [InlineData("mustNot:", "mustNot", "MustNot")]
    [InlineData("  - legendaryJoker: Perkeo", "legendaryJoker", "ClauseKey")]
    [InlineData("  - !joker", "!joker", "ClauseKey")]
    [InlineData("    antes: [1, 2]", "antes", "Key")]
    [InlineData("    antes: [1, 2]", "2", "Number")]
    [InlineData("name: \"Lola & Crouton Night\"", "\"Lola & Crouton Night\"", "String")]
    [InlineData("deck: Ghost  # two player night", "# two player night", "Comment")]
    public void APieceOfALine_GetsItsColor(string line, string piece, string color) =>
        Assert.Contains((piece, color), Colors(line));

    [Fact]
    public void EveryClauseKeyTheEngineReads_IsColoredAsAClauseKey()
    {
        Assert.NotEmpty(JamlConfigLoader.ClauseKeys);
        foreach (var key in JamlConfigLoader.ClauseKeys)
            Assert.Contains((key, "ClauseKey"), Colors($"  - {key}: X"));
    }

    [Fact]
    public void ASeedOrANameThatHoldsDigits_IsNotColoredAsANumber()
    {
        Assert.DoesNotContain(Colors("  - 1JHBI111"), c => c.Color == "Number");
        Assert.DoesNotContain(Colors("luck: X2"), c => c.Color == "Number");
    }
}

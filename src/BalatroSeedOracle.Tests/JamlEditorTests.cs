using AvaloniaEdit.Document;
using BalatroSeedOracle.Helpers;
using Motely.Enums;
using Motely.Filters;
using Xunit;

namespace BalatroSeedOracle.Tests;

// The JAML editor's language features: completions, hover, folding, diagnostics, snippets, and
// the config ↔ text helpers. Everything is answered by the engine, so these pin the wiring
// (which context gets which engine list, which line a marker lands on), not the vocabulary.
public class JamlCompletionTests
{
    private static string[] Labels(string textBeforeCursor) =>
        [.. JamlLanguage.Complete(textBeforeCursor).Select(c => c.Text)];

    [Fact]
    public void ListItem_InMust_OffersClauseKeys()
    {
        var labels = Labels("name: x\nmust:\n  - ");
        Assert.Contains("joker", labels);
        Assert.Contains("boss", labels);
        Assert.Contains("legendaryJoker", labels);
        Assert.Contains("or", labels);
        // Plural aliases load too, but are not offered twice.
        Assert.DoesNotContain("jokers", labels);
        Assert.DoesNotContain("bosses", labels);
        // Every engine clause type is reachable.
        var clauseTypes = Motely.Schema.FilterVocabulary.DiscriminatorTable.Select(d => d.ClauseType).Distinct().Count();
        Assert.Equal(clauseTypes, labels.Length);
    }

    [Fact]
    public void ListItem_TypedPrefix_FiltersClauseKeys()
    {
        var labels = Labels("must:\n  - bo");
        Assert.Equal("boss", labels[0]);
        Assert.Contains("boosterPack", labels);
        Assert.DoesNotContain("joker", labels);
    }

    [Fact]
    public void AfterBossColon_OffersBossNames()
    {
        var labels = Labels("must:\n  - boss: ");
        Assert.Equal(Enum.GetNames<MotelyBossBlind>().Distinct().Count(), labels.Length);
        Assert.Contains("TheEye", labels);
        Assert.DoesNotContain("Blueprint", labels);
    }

    [Fact]
    public void AfterBossColon_TypedPrefix_PrefixMatchesFirst()
    {
        var labels = Labels("must:\n  - boss: TheE");
        Assert.Equal("TheEye", labels[0]);
    }

    [Fact]
    public void InFlowList_CompletesTheLastName()
    {
        var labels = Labels("should:\n  - joker: [Blueprint, Brain");
        Assert.Contains("Brainstorm", labels);
        Assert.DoesNotContain("Blueprint", labels);
    }

    [Fact]
    public void RootKeys_AtColumnZero()
    {
        var labels = Labels("name: x\n");
        Assert.Contains("must", labels);
        Assert.Contains("should", labels);
        Assert.Contains("mustNot", labels);
        Assert.Contains("deck", labels);
    }

    [Fact]
    public void DeckValue_OffersDecks()
    {
        var labels = Labels("deck: ");
        Assert.Contains("Red", labels);
        Assert.Contains("Erratic", labels);
    }

    [Fact]
    public void UnderAClause_OffersItsKeys_NotOtherClauses()
    {
        var labels = Labels("must:\n  - joker: Blueprint\n    ");
        Assert.Contains("antes", labels);
        Assert.Contains("edition", labels);
        Assert.Contains("sources", labels);
        Assert.DoesNotContain("joker", labels);
        Assert.DoesNotContain("boss", labels);
    }

    [Fact]
    public void EditionValue_OffersEditions()
    {
        var labels = Labels("must:\n  - joker: Blueprint\n    edition: ");
        Assert.Contains("Negative", labels);
        Assert.Contains("Polychrome", labels);
    }

    [Fact]
    public void InsideOrClauses_ListItem_OffersClauseKeys()
    {
        var labels = Labels("must:\n  - or:\n      clauses:\n        - ");
        Assert.Contains("joker", labels);
        Assert.Contains("tag", labels);
    }

    [Fact]
    public void AntesValue_OffersNothing()
    {
        Assert.Empty(Labels("must:\n  - joker: Blueprint\n    antes: "));
    }

    [Theory]
    [InlineData("must:\n  - ", " ", true)]
    [InlineData("must:\n  - boss: ", " ", true)]
    [InlineData("must:\n  - j", "j", true)]
    [InlineData("must:\n  - jo", "o", false)]
    [InlineData("must:\n  -", "-", false)]
    [InlineData("# a comment ", " ", false)]
    [InlineData("name: My ", " ", false)]
    public void AutoTrigger(string before, string typed, bool expected) =>
        Assert.Equal(expected, JamlLanguage.ShouldAutoTrigger(before, typed));

    [Fact]
    public void TokenStart_StopsAtDashAndColon()
    {
        Assert.Equal(4, JamlLanguage.TokenStart("  - jok", 7));
        Assert.Equal(10, JamlLanguage.TokenStart("  - boss: ", 10));
        Assert.Equal(10, JamlLanguage.TokenStart("  - boss: Th", 12));
    }

    [Fact]
    public void Json_KeyAndValuePositions()
    {
        var root = JsonAutocompletionHelper.GetCompletionsForContext("{\n  \"").Select(c => c.Text).ToArray();
        Assert.Contains("must", root);

        var clause = JsonAutocompletionHelper.GetCompletionsForContext("{\n  \"must\": [\n    { \"").Select(c => c.Text).ToArray();
        Assert.Contains("joker", clause);

        var value = JsonAutocompletionHelper.GetCompletionsForContext("{ \"must\": [ { \"boss\": \"The").Select(c => c.Text).ToArray();
        Assert.Contains("TheEye", value);
    }
}

public class JamlHoverTests
{
    [Fact]
    public void ClauseKey_DescribesClause()
    {
        var info = JamlLanguage.Hover("must:\n  - boss: TheEye\n", 1, 5);
        Assert.NotNull(info);
        Assert.StartsWith("boss — JAML clause", info);
        Assert.Contains("boss blind", info);
        Assert.Contains("antes", info);
    }

    [Fact]
    public void Name_DescribesItsKind()
    {
        var info = JamlLanguage.Hover("must:\n  - boss: TheEye\n", 1, 13);
        Assert.Equal("TheEye — boss blind (MotelyBossBlind)", info);
    }

    [Fact]
    public void ClauseProperty_NamesItsClause()
    {
        var info = JamlLanguage.Hover("must:\n  - boss: TheEye\n    antes: [3]\n", 2, 5);
        Assert.NotNull(info);
        Assert.StartsWith("antes — key of the boss clause", info);
    }

    [Fact]
    public void RootKey_AndUnknownWord()
    {
        Assert.StartsWith("must — filter key", JamlLanguage.Hover("must:\n", 0, 1));
        Assert.Null(JamlLanguage.Hover("name: Zzzqx\n", 0, 8));
    }
}

public class JamlFoldingTests
{
    [Fact]
    public void Folds_Sections_And_Clauses()
    {
        const string text =
            "name: x\n" +          // 0
            "must:\n" +            // 1
            "  - joker: Blueprint\n" + // 2
            "    antes: [1, 2]\n" + // 3
            "\n" +                 // 4
            "  - boss: TheEye\n" + // 5
            "    antes: [3]\n" +   // 6
            "should:\n" +          // 7
            "- tag: NegativeTag\n" + // 8
            "  score: 5\n" +       // 9
            "- tag: CharmTag\n";   // 10

        var regions = JamlLanguage.FoldRegions(text);
        Assert.Contains((1, 6), regions); // must: through its last clause line (blank line inside is kept)
        Assert.Contains((2, 3), regions); // first clause
        Assert.Contains((5, 6), regions); // second clause
        Assert.Contains((7, 10), regions); // should: with items at the key's own indent
        Assert.Contains((8, 9), regions);
        Assert.DoesNotContain(regions, r => r.StartLine == 0 || r.StartLine == 10);
    }

    [Fact]
    public void Folds_MultiLineBrackets_ForJson()
    {
        const string text = "{\n  \"must\": [\n    { \"joker\": \"Blueprint\" }\n  ]\n}\n";
        var regions = JamlLanguage.FoldRegions(text);
        Assert.Contains((0, 4), regions);
        Assert.Contains((1, 3), regions);
    }

    [Fact]
    public void Foldings_StartAtEndOfHeaderLine_InOffsetOrder()
    {
        var document = new TextDocument("must:\n  - joker: Blueprint\n    antes: [1]\n");
        var foldings = JamlFoldingStrategy.CreateNewFoldings(document).ToList();
        Assert.Equal(2, foldings.Count);
        Assert.Equal(document.GetLineByNumber(1).EndOffset, foldings[0].StartOffset);
        Assert.Equal(document.GetLineByNumber(3).EndOffset, foldings[0].EndOffset);
        Assert.True(foldings[0].StartOffset < foldings[1].StartOffset);
    }
}

public class JamlDiagnosticsTests
{
    [Fact]
    public void ValidFilter_NoMarkers()
    {
        Assert.Empty(JamlLanguage.Diagnose("name: ok\nmust:\n  - joker: Blueprint\n    antes: [1, 2]\n"));
    }

    [Fact]
    public void UnknownName_MarksTheName()
    {
        const string text = "name: t\nmust:\n  - boss: TheEyez\n    antes: [3]\n";
        var d = Assert.Single(JamlLanguage.Diagnose(text));
        Assert.Equal(JamlDiagnosticSeverity.Error, d.Severity);
        Assert.Equal(3, d.Line);
        Assert.Equal(10, d.StartColumn);
        Assert.Equal(17, d.EndColumn);
        Assert.Contains("TheEyez", d.Message);
    }

    [Fact]
    public void UnknownKey_MarksTheKey()
    {
        const string text = "name: t\nmust:\n  - joker: Blueprint\n    antez: [3]\n";
        var d = Assert.Single(JamlLanguage.Diagnose(text));
        Assert.Equal(4, d.Line);
        Assert.Equal(4, d.StartColumn);
        Assert.Equal(9, d.EndColumn);
    }

    [Fact]
    public void SyntaxError_MarksTheReportedLine()
    {
        const string text = "name: t\nmust:\n  - joker: Blueprint\n   antes: [3\n";
        var d = Assert.Single(JamlLanguage.Diagnose(text));
        Assert.Equal(JamlDiagnosticSeverity.Error, d.Severity);
        Assert.InRange(d.Line, 1, 4);
        Assert.True(d.EndColumn > d.StartColumn);
    }

    [Fact]
    public void ImpossibleBossAnte_IsAWarningOnTheBossLine()
    {
        const string text = "name: t\nmust:\n  - joker: Blueprint\n  - boss: TheEye\n    antes: [1]\n";
        var d = Assert.Single(JamlLanguage.Diagnose(text));
        Assert.Equal(JamlDiagnosticSeverity.Warning, d.Severity);
        Assert.Equal(4, d.Line);
        Assert.Equal(10, d.StartColumn);
        Assert.Equal(16, d.EndColumn);
        Assert.Contains("TheEye", d.Message);
    }

    [Fact]
    public void ImpossibleBossAnte_InShould_LandsInShould()
    {
        const string text = "must:\n  - boss: TheEye\n    antes: [3]\nshould:\n  - boss: TheEye\n    antes: [1]\n";
        var d = Assert.Single(JamlLanguage.Diagnose(text));
        Assert.Equal(5, d.Line);
    }

    [Fact]
    public void EmptyText_NoMarkers() => Assert.Empty(JamlLanguage.Diagnose("  \n"));
}

public class JamlSnippetTests
{
    [Fact]
    public void ClauseKey_ExpandsWithAntes()
    {
        var e = JamlCodeSnippetService.Expand("  - joker", 9);
        Assert.NotNull(e);
        Assert.Equal(4, e!.Value.Start);
        Assert.Equal("joker: \n    antes: [1, 2, 3]", e.Value.Text);
        Assert.Equal("joker: ".Length, e.Value.Caret);
    }

    [Fact]
    public void Section_Expands_AndOtherWordsDoNot()
    {
        Assert.Equal("must:\n  - ", JamlCodeSnippetService.Expand("must", 4)!.Value.Text);
        Assert.Null(JamlCodeSnippetService.Expand("  - Blueprint", 13));
        Assert.Null(JamlCodeSnippetService.Expand("name: joker", 11));
    }

    [Fact]
    public void FilterSkeleton_Loads()
    {
        var e = JamlCodeSnippetService.Expand("filter", 6)!.Value;
        var text = e.Text.Replace("name: \n", "name: x\n").Replace("  - \n", "  - joker: Blueprint\n");
        text = text[..text.LastIndexOf("  - ", StringComparison.Ordinal)] + "  - tag: NegativeTag\n";
        Assert.True(JamlConfigLoader.TryLoad(text, out _, out var error), error);
    }
}

public class JamlConfigTextTests
{
    private static string FiltersDir()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null && !Directory.Exists(Path.Combine(dir.FullName, "src", "MotelyJAML", "JamlFilters")))
            dir = dir.Parent;
        Assert.NotNull(dir);
        return Path.Combine(dir!.FullName, "src", "MotelyJAML", "JamlFilters");
    }

    public static TheoryData<string> EngineFilters()
    {
        var data = new TheoryData<string>();
        foreach (var file in Directory.EnumerateFiles(FiltersDir(), "*.yaml").Order())
            data.Add(Path.GetFileName(file));
        return data;
    }

    [Theory]
    [MemberData(nameof(EngineFilters))]
    public void RoundTrips_EngineFilters(string file)
    {
        var original = JamlConfigLoader.FromFile(Path.Combine(FiltersDir(), file));
        var text = JamlConfigText.ToJaml(original);
        Assert.True(JamlConfigLoader.TryLoad(text, out var reloaded, out var error), $"{error}\n---\n{text}");
        // Same config: serializing the reloaded one gives the same text.
        Assert.Equal(text, JamlConfigText.ToJaml(reloaded!));
        Assert.Equal(original.Must.Count, reloaded!.Must.Count);
        Assert.Equal(original.Should.Count, reloaded.Should.Count);
        Assert.Equal(original.MustNot.Count, reloaded.MustNot.Count);
    }

    [Fact]
    public void WritesTheFlatClauseShape_WithoutDefaults()
    {
        var config = JamlConfigLoader.FromJaml("name: t\nmust:\n  - boss: TheEye\n    antes: [3, 4]\n");
        var text = JamlConfigText.ToJaml(config);
        Assert.Contains("- boss: TheEye", text);
        Assert.DoesNotContain("score:", text);
        Assert.DoesNotContain("bosses", text);
    }
}

public class ClauseExtensionsTests
{
    [Theory]
    [InlineData("Joker", "Blueprint", "Joker", "Blueprint")]
    [InlineData("SoulJoker", "Perkeo", "SoulJoker", "Perkeo")]
    [InlineData("Boss", "TheEye", "Boss", "TheEye")]
    [InlineData("Tarot", "TheFool", "Tarot", "TheFool")]
    [InlineData("SmallBlindTag", "NegativeTag", "SmallBlindTag", "NegativeTag")]
    [InlineData("SoulJoker", "Any", "SoulJoker", "Any")]
    public void BuilderItem_RoundTrips(string type, string value, string expectedType, string expectedValue)
    {
        var clause = JamlClauseExtensions.CreateClauseForDiscriminator(type, value);
        Assert.Equal(expectedType, clause.GetTypeName());
        Assert.Equal(expectedValue, clause.GetValueName());
    }

    [Fact]
    public void Decorations_SetAndRead()
    {
        var clause = JamlClauseExtensions.CreateClauseForDiscriminator("Joker", "Blueprint");
        clause.SetAntes([1, 2]);
        clause.SetEditionString("Negative");
        clause.SetStickerStrings(["Eternal"]);
        Assert.Equal([1, 2], clause.GetAntes());
        Assert.Equal("Negative", clause.GetEditionString());
        Assert.Equal(["Eternal"], clause.GetStickerStrings()!);
        Assert.Null(clause.GetClauses());
    }
}

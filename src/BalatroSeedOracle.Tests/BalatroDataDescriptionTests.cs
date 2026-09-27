using BalatroSeedOracle.Models;
using Xunit;

namespace BalatroSeedOracle.Tests;

// Issue #12: every joker / tarot / spectral / planet / voucher / tag / boss the pickers list must have
// hover effect text. Item lists are BSO's own (BalatroData.*), keyed by Motely enum name,
// which is exactly what SelectableItem.TooltipText passes to GetDescription.
public class BalatroDataDescriptionTests
{
    // Wildcard shelf entries, not cards.
    private static readonly HashSet<string> Wildcards = ["any", "*", "anytarot", "anyplanet"];

    public static TheoryData<string, string> AllItems()
    {
        var data = new TheoryData<string, string>();
        void Add(string list, IEnumerable<string> names)
        {
            foreach (var name in names.Where(n => !Wildcards.Contains(n)))
                data.Add(list, name);
        }
        Add(nameof(BalatroData.Jokers), BalatroData.Jokers.Keys);
        Add(nameof(BalatroData.TarotCards), BalatroData.TarotCards.Keys);
        Add(nameof(BalatroData.SpectralCards), BalatroData.SpectralCards.Keys);
        Add(nameof(BalatroData.PlanetCards), BalatroData.PlanetCards.Keys);
        Add(nameof(BalatroData.Vouchers), BalatroData.Vouchers.Keys);
        Add(nameof(BalatroData.Tags), BalatroData.Tags.Keys);
        Add(nameof(BalatroData.BossBlinds), BalatroData.BossBlinds.Keys);
        return data;
    }

    [Theory]
    [MemberData(nameof(AllItems))]
    public void EveryPickerItemHasEffectText(string list, string name)
    {
        var desc = BalatroData.GetDescription(name);
        Assert.False(string.IsNullOrWhiteSpace(desc), $"{list}[{name}] has no description");
    }

    [Fact]
    public void ItemListsHaveTheFullRoster()
    {
        Assert.Equal(150, BalatroData.Jokers.Count);
        Assert.Equal(22, BalatroData.TarotCards.Keys.Count(k => !Wildcards.Contains(k)));
        Assert.Equal(18, BalatroData.SpectralCards.Count);
        Assert.Equal(12, BalatroData.PlanetCards.Keys.Count(k => !Wildcards.Contains(k)));
        Assert.Equal(32, BalatroData.Vouchers.Count);
        Assert.Equal(24, BalatroData.Tags.Count);
        Assert.Equal(28, BalatroData.BossBlinds.Count);
    }

    [Theory]
    // enum name, alternate spelling that must resolve to the same text
    [InlineData("EightBall", "8 Ball")]
    [InlineData("OopsAll6s", "Oops! All 6s")]
    [InlineData("Seance", "Séance")]
    [InlineData("ChaostheClown", "Chaos the Clown")]
    [InlineData("Showman", "ring_master")]
    [InlineData("Canio", "caino")]
    [InlineData("Seltzer", "selzer")]
    [InlineData("GluttonousJoker", "gluttenous_joker")]
    [InlineData("TheWheelOfFortune", "The Wheel of Fortune")]
    [InlineData("DriversLicense", "Driver's License")]
    [InlineData("TheSoul", "the soul")]
    [InlineData("BlackHole", "BLACK HOLE")]
    public void AliasesResolveToTheSameText(string enumName, string alias)
    {
        var expected = BalatroData.GetDescription(enumName);
        Assert.False(string.IsNullOrWhiteSpace(expected));
        Assert.Equal(expected, BalatroData.GetDescription(alias));
    }

    [Theory]
    // Corpus text carries the real numbers, not the en-us "#1#" placeholders ("X in X chance").
    [InlineData("EightBall", "1 in 4 chance")]
    [InlineData("Bloodstone", "1 in 2 chance")]
    [InlineData("Baron", "X1.5 Mult")]
    [InlineData("TheSoul", "Legendary Joker")]
    [InlineData("BlackHole", "poker hand by 1 level")]
    [InlineData("Antimatter", "+1 Joker slot")]
    [InlineData("GlowUp", "Upgrades Hone")]
    [InlineData("Pluto", "+10 Chips per level")]
    [InlineData("ClearanceSale", "discounts vouchers")]
    [InlineData("Liquidation", "discounts vouchers")]
    // A corpus ";" before a parenthetical is the game's line break; it joins with a space.
    [InlineData("EvenSteven", "when scored (10, 8, 6, 4, 2)")]
    [InlineData("InvisibleJoker", "random Joker (Removes Negative from copy)")]
    // A prose semicolon is kept.
    [InlineData("GrosMichel", "+15 Mult; 1 in 6 chance")]
    // Tags and bosses: the legacy en-us text here was "gain $X" and "Playing a X".
    [InlineData("InvestmentTag", "Gain $25 after defeating the next Boss Blind")]
    [InlineData("EconomyTag", "max +$40")]
    [InlineData("TheOx", "most-played hand sets money to $0")]
    [InlineData("TheOx", "Appears from ante 6")]
    [InlineData("NegativeTag", "(+1 Joker slot)")]
    [InlineData("VioletVessel", "Showdown boss")]
    [InlineData("Top-up Tag", "2 random Common Jokers")]
    public void TextComesFromTheCorpus(string name, string fragment)
    {
        Assert.Contains(fragment, BalatroData.GetDescription(name));
    }

    [Theory]
    // Run-state readouts are stripped like "(Currently: ...)": they show a fixed number that ticks in-game.
    [InlineData("LoyaltyCard", "remaining")]
    [InlineData("Yorick", "[23]")]
    public void RunStateCountersAreStripped(string name, string readout)
    {
        var desc = BalatroData.GetDescription(name);
        Assert.False(string.IsNullOrWhiteSpace(desc));
        Assert.DoesNotContain(readout, desc);
    }

    [Theory]
    // Corpus commentary in a parenthetical with a ";" is not game text.
    [InlineData("UncommonTag", "force-generated")]
    [InlineData("InvestmentTag", "pre-1.0.1f")]
    public void CorpusCommentaryIsStripped(string name, string commentary)
    {
        var desc = BalatroData.GetDescription(name);
        Assert.False(string.IsNullOrWhiteSpace(desc));
        Assert.DoesNotContain(commentary, desc);
    }

    [Theory]
    [MemberData(nameof(AllItems))]
    public void NoLocalizationPlaceholdersSurvive(string list, string name)
    {
        // en-us strings with #1# vars flattened to X: "+X Mult", "xX Mult", "X in X chance", "$X".
        var desc = BalatroData.GetDescription(name) ?? "";
        Assert.DoesNotMatch(@"(\+|x|\$)X\b|\bX in X\b", desc);
        Assert.True(desc.Length > 0, $"{list}[{name}]");
    }

    [Theory]
    [MemberData(nameof(AllItems))]
    public void NoLineBreakMarkerBeforeParenthetical(string list, string name)
    {
        var desc = BalatroData.GetDescription(name) ?? "";
        Assert.DoesNotMatch(@";\s*\(", desc);
        Assert.True(desc.Length > 0, $"{list}[{name}]");
    }

    public static TheoryData<string, string> ConsumableItems()
    {
        var data = new TheoryData<string, string>();
        foreach (var (list, names) in new[]
        {
            (nameof(BalatroData.TarotCards), BalatroData.TarotCards.Keys),
            (nameof(BalatroData.SpectralCards), BalatroData.SpectralCards.Keys),
            (nameof(BalatroData.PlanetCards), BalatroData.PlanetCards.Keys),
        })
        {
            foreach (var name in names.Where(n => !Wildcards.Contains(n)))
                data.Add(list, name);
        }
        return data;
    }

    // Consumable text is the corpus's first sentence; the corpus appends hunter notes after a
    // top-level " - " (Black Hole: "... - the only way to level ..."). Inside parentheses the dash
    // is part of the effect (Deja Vu's Red Seal gloss) and is kept. Vouchers are parsed from the
    // Base/Upgraded line instead, and Illusion's " - rates 40% enhanced ..." tail is effect data.
    [Theory]
    [MemberData(nameof(ConsumableItems))]
    public void NoCorpusCommentaryAfterTopLevelDash(string list, string name)
    {
        var desc = BalatroData.GetDescription(name) ?? "";
        Assert.True(desc.Length > 0, $"{list}[{name}]");
        var depth = 0;
        for (var i = 0; i < desc.Length; i++)
        {
            if (desc[i] == '(') depth++;
            else if (desc[i] == ')') depth = Math.Max(0, depth - 1);
            else if (depth == 0 && string.CompareOrdinal(desc, i, " - ", 0, 3) == 0)
                Assert.Fail($"{list}[{name}] has commentary after a top-level dash: {desc}");
        }
    }

    [Fact]
    public void BlackHoleIsTheEffectSentenceOnly()
    {
        Assert.Equal(
            "Upgrade EVERY poker hand by 1 level, including undiscovered secret hands.",
            BalatroData.GetDescription("Black Hole"));
        Assert.Contains("(Red Seal: retrigger this card 1 time - applies to scoring AND in-hand effects)",
            BalatroData.GetDescription("DejaVu"));
    }

    [Fact]
    public void UnknownOrEmptyNamesReturnNull()
    {
        Assert.Null(BalatroData.GetDescription(null));
        Assert.Null(BalatroData.GetDescription("   "));
        Assert.Null(BalatroData.GetDescription("NotARealBalatroItem"));
    }
}

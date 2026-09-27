using BalatroSeedOracle.Models;
using Xunit;

namespace BalatroSeedOracle.Tests;

// Issue #12: every joker / tarot / spectral / planet / voucher the pickers list must have
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
    public void TextComesFromTheCorpus(string name, string fragment)
    {
        Assert.Contains(fragment, BalatroData.GetDescription(name));
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

    [Fact]
    public void UnknownOrEmptyNamesReturnNull()
    {
        Assert.Null(BalatroData.GetDescription(null));
        Assert.Null(BalatroData.GetDescription("   "));
        Assert.Null(BalatroData.GetDescription("NotARealBalatroItem"));
    }
}

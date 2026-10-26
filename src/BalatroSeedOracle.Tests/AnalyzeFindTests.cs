using BalatroSeedOracle.Services;
using BalatroSeedOracle.ViewModels;
using Motely.Enums;
using Xunit;

namespace BalatroSeedOracle.Tests;

/// <summary>
/// The Analyzer's Find: the real engine's locate, mapped to rows, its not-found depth and its
/// error line. Seed 9KOCZ9YO on the Ghost deck holds Diet Cola 8 times in antes 1-12, first in
/// ante 4's first Buffoon Pack.
/// </summary>
public sealed class AnalyzeFindTests : IDisposable
{
    private readonly string _dir = Path.Combine(Path.GetTempPath(), "bso-find-" + Guid.NewGuid().ToString("N"));

    public AnalyzeFindTests() => Directory.CreateDirectory(_dir);

    public void Dispose() => Directory.Delete(_dir, recursive: true);

    [Theory]
    // The stake picker's positions, White to Gold. MotelyStake has no value 5, so position 5 is
    // Purple (6), 6 Orange (7), 7 Gold (8): a cast of the position read Gold as Orange.
    [InlineData(0, MotelyStake.White)]
    [InlineData(1, MotelyStake.Red)]
    [InlineData(2, MotelyStake.Green)]
    [InlineData(3, MotelyStake.Black)]
    [InlineData(4, MotelyStake.Blue)]
    [InlineData(5, MotelyStake.Purple)]
    [InlineData(6, MotelyStake.Orange)]
    [InlineData(7, MotelyStake.Gold)]
    public void Stake_picker_position_is_the_stake_it_shows(int position, MotelyStake stake)
    {
        var vm = new AnalyzeModalViewModel(SpriteService.Instance, new UserProfileService(Path.Combine(_dir, "userprofile.json")))
        {
            StakeIndex = position,
        };
        Assert.Equal(stake, vm.SelectedStake);
    }

    private AnalyzeModalViewModel GhostSeed(string find, int minAnte, int maxAnte) =>
        new(SpriteService.Instance, new UserProfileService(Path.Combine(_dir, "userprofile.json")))
        {
            DeckIndex = (int)MotelyDeck.Ghost,
            StakeIndex = 0, // the picker's first position, White
            SeedInput = "9KOCZ9YO",
            FindInput = find,
            FindMinAnte = minAnte,
            FindMaxAnte = maxAnte,
        };

    [Fact]
    public async Task Every_hit_becomes_a_row_with_its_ante_place_and_item()
    {
        var vm = GhostSeed("Diet Cola", 1, 12);

        await vm.FindCommand.ExecuteAsync(null);

        Assert.Equal(8, vm.FindRows.Count);
        Assert.Equal(new LocateHitRow(4, "Buffoon Pack (pack 1) card 1", "Diet Cola"), vm.FindRows[0]);
        Assert.Equal("Ante 4", vm.FindRows[0].AnteText);
        Assert.All(vm.FindRows, row => Assert.Contains("Diet Cola", row.Item));
        Assert.Equal("Diet Cola: 8 hits in antes 1-12, 20 pull rolls and 15-50 shop cards per ante", vm.FindStatus);
        Assert.False(vm.HasFindError);
    }

    [Fact]
    public async Task No_hit_says_how_deep_it_looked()
    {
        var vm = GhostSeed("Diet Cola", 1, 3);

        await vm.FindCommand.ExecuteAsync(null);

        Assert.Empty(vm.FindRows);
        Assert.Equal("Diet Cola: not found in antes 1-3, 20 pull rolls and 15-50 shop cards per ante", vm.FindStatus);
        Assert.False(vm.HasFindError);
    }

    [Fact]
    public async Task An_unknown_name_shows_the_engines_correction()
    {
        var vm = GhostSeed("Diet Coke", 1, 12);

        await vm.FindCommand.ExecuteAsync(null);

        Assert.Empty(vm.FindRows);
        Assert.True(vm.HasFindError);
        Assert.Contains("did you mean DietCola?", vm.FindError);
        Assert.False(vm.HasFindStatus);
    }

    [Fact]
    public async Task A_bad_ante_range_shows_the_engines_line_and_a_new_find_clears_it()
    {
        var vm = GhostSeed("Diet Cola", 9, 2);

        await vm.FindCommand.ExecuteAsync(null);
        Assert.Contains("antes 9-2", vm.FindError);

        vm.FindMinAnte = 1;
        vm.FindMaxAnte = 12;
        await vm.FindCommand.ExecuteAsync(null);
        Assert.False(vm.HasFindError);
        Assert.Equal(8, vm.FindRows.Count);
    }

    [Fact]
    public void Find_needs_a_seed_and_a_name()
    {
        var vm = GhostSeed("", 1, 8);
        Assert.False(vm.FindCommand.CanExecute(null));

        vm.FindInput = "Perkeo";
        Assert.True(vm.FindCommand.CanExecute(null));

        vm.SeedInput = " ";
        Assert.False(vm.FindCommand.CanExecute(null));
    }
}

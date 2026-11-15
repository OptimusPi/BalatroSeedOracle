using System.Linq;
using Avalonia.Controls;
using Avalonia.Threading;
using Avalonia.VisualTree;
using BalatroSeedOracle.Controls;
using BalatroSeedOracle.Models;
using BalatroSeedOracle.Services;
using BalatroSeedOracle.ViewModels;
using BalatroSeedOracle.ViewModels.Controls;
using BalatroSeedOracle.Views.Modals;
using BalatroSeedOracle.Views.SearchModalTabs;
using Xunit;

namespace BalatroSeedOracle.Tests;

/// <summary>
/// The search modal's Results tab: found seeds reach the grid, the grid builds its tally columns,
/// and the tab shows with or without an Avalonia Accelerate key.
/// </summary>
[Collection(HeadlessAvaloniaCollection.Name)]
public class ResultsGridTests(HeadlessAvaloniaFixture avalonia)
{
    private static readonly string[] Labels = ["Perkeo", "Blueprint"];

    private static SearchResult Found(string seed, int total, params int[] tallies) =>
        new()
        {
            Seed = seed,
            TotalScore = total,
            Scores = tallies,
            Labels = Labels,
        };

    private static SearchModalViewModel NewSearchViewModel()
    {
        var profile = new UserProfileService();
        return new SearchModalViewModel(profile, () => new AnalyzeModalViewModel(SpriteService.Instance, profile));
    }

    /// <summary>A handful of results waits 300 ms before it shows; this shows it now.</summary>
    private static void Show(SortableResultsGridViewModel grid)
    {
        grid.ForceRefresh();
        Dispatcher.UIThread.RunJobs();
    }

    [Fact]
    public void TheGridSource_BuildsOneColumnPerTally_AndReadsEachTally()
    {
        avalonia.Run(() =>
        {
            var grid = new SortableResultsGridViewModel();
            grid.AddResults([Found("ALEEB", 7, 3, 4), Found("PIROCKS", 2, 2, 0)]);
            Show(grid);

            Assert.Equal(Labels, grid.TallyLabels);
            var source = grid.GridSource!;
            Assert.Equal(["SEED", "SCORE", "PERKEO", "BLUEPRINT"], source.Columns.Select(c => c.Header));

            // Default order is score, high to low, so ALEEB is the first row.
            Assert.Equal(["ALEEB", "PIROCKS"], grid.DisplayedResults.Select(r => r.Seed));
            return 0;
        });
    }

    [Fact]
    public void TheResultsTab_ShowsTheFoundSeeds_AndTheSearchTabComesBack()
    {
        avalonia.Run(() =>
        {
            LifecycleAndRegressionTests.EnsureAppResourcesLoaded();
            var vm = NewSearchViewModel();
            vm.SearchResults.Add(Found("ALEEB", 7, 3, 4));
            vm.SearchResults.Add(Found("PIROCKS", 2, 2, 0));

            var modal = new SearchModal(vm);
            var window = new Window { Content = modal, Width = 1200, Height = 700 };
            window.Show();
            try
            {
                var host = modal.FindControl<ContentControl>("TabContentHost")!;

                vm.SelectedTabIndex = 1;
                Dispatcher.UIThread.RunJobs();

                var tab = Assert.IsType<ResultsTab>(host.Content);
                var results = tab.GetVisualDescendants().OfType<SortableResultsGrid>().Single();
                Show(results.ViewModel);
                Assert.Equal(["ALEEB", "PIROCKS"], results.ViewModel.DisplayedResults.Select(r => r.Seed));

                // TreeDataGrid with an Accelerate key, DataGrid without one: either way a grid, with
                // SEED, SCORE and the two tallies.
                var gridHost = results.FindControl<ContentControl>("GridHost")!;
                if (gridHost.Content is DataGrid free)
                    Assert.Equal(["SEED", "SCORE", "PERKEO", "BLUEPRINT"], free.Columns.Select(c => c.Header));
                else
                    Assert.Equal(4, Assert.IsType<TreeDataGrid>(gridHost.Content).Source!.Columns.Count);

                vm.SelectedTabIndex = 0;
                Dispatcher.UIThread.RunJobs();
                Assert.IsType<SearchTab>(host.Content);
            }
            finally
            {
                window.Close();
            }
            return 0;
        });
    }

    [Fact]
    public void ARowSelectedInTheFreeGrid_EnablesCopy()
    {
        avalonia.Run(() =>
        {
            LifecycleAndRegressionTests.EnsureAppResourcesLoaded();
            var results = new SortableResultsGrid();
            var window = new Window { Content = results, Width = 800, Height = 400 };
            window.Show();
            try
            {
                results.ForceRefreshResults([Found("ALEEB", 7, 3, 4), Found("PIROCKS", 2, 2, 0)]);
                Show(results.ViewModel);

                // Only the free grid is under test here; a licensed build shows TreeDataGrid.
                if (results.FindControl<ContentControl>("GridHost")!.Content is not DataGrid free)
                    return 0;

                Assert.False(results.ViewModel.CopySelectedCommand.CanExecute(null));
                free.SelectedItem = results.ViewModel.DisplayedResults[1];
                Assert.Equal(["PIROCKS"], results.ViewModel.SelectedItems.Select(r => r.Seed));
                Assert.True(results.ViewModel.CopySelectedCommand.CanExecute(null));
            }
            finally
            {
                window.Close();
            }
            return 0;
        });
    }
}

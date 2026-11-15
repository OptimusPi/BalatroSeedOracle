using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Threading;
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

    [Fact]
    public void AResultDroppedFromTheLiveWindow_IsDroppedAlone_WithNoReload()
    {
        avalonia.Run(() =>
        {
            LifecycleAndRegressionTests.EnsureAppResourcesLoaded();
            var live = new ObservableCollection<SearchResult>(
                Enumerable.Range(0, 50).Select(i => Found($"SEED{i:D4}", i, i, 0))
            );
            var results = new SortableResultsGrid { ItemsSource = live };
            Show(results.ViewModel);
            Assert.Equal(50, results.ViewModel.DisplayedResults.Count);

            var rowChanges = 0;
            results.ViewModel.DisplayedResults.CollectionChanged += (_, _) => rowChanges++;

            // What a search past its cap does on every match: one in, the oldest out.
            live.Add(Found("NEWEST", 99, 9, 9));
            live.RemoveAt(0);
            Dispatcher.UIThread.RunJobs();

            // The buffer follows at once; the rows wait for the next refresh.
            Assert.Equal(50, results.ViewModel.TotalResultCount);
            Assert.Equal(0, rowChanges);

            Show(results.ViewModel);
            var seeds = results.ViewModel.DisplayedResults.Select(r => r.Seed).ToList();
            Assert.Contains("NEWEST", seeds);
            Assert.DoesNotContain("SEED0000", seeds);
            return 0;
        });
    }

    [Fact]
    public void ResultsThatKeepArriving_ShowWhileTheyArrive()
    {
        avalonia.Run(() =>
        {
            var grid = new SortableResultsGridViewModel();

            // One result every 50 ms for a second: faster than the 300 ms refresh interval, so a
            // timer that restarts on each result would show nothing until the stream stopped.
            var clock = Stopwatch.StartNew();
            var sent = 0;
            while (clock.ElapsedMilliseconds < 1000)
            {
                grid.AddResult(Found($"SEED{sent++:D4}", sent, sent, 0));
                Thread.Sleep(50);
                Dispatcher.UIThread.RunJobs();
            }

            Assert.NotEmpty(grid.DisplayedResults);
            return 0;
        });
    }

    [Fact]
    public void ARefreshThatChangesNoRow_LeavesTheRowsAlone()
    {
        avalonia.Run(() =>
        {
            var grid = new SortableResultsGridViewModel();
            grid.AddResults(Enumerable.Range(0, 1200).Select(i => Found($"SEED{i:D4}", 1000 + i, i, 0)));
            Show(grid);
            Assert.Equal(1000, grid.DisplayedResults.Count);

            var rowChanges = 0;
            grid.DisplayedResults.CollectionChanged += (_, _) => rowChanges++;

            // Scores below every row on show: the top 1000 are the same seeds as before.
            grid.AddResult(Found("LOWEST", 1, 0, 0));
            Show(grid);
            Assert.Equal(0, rowChanges);
            Assert.Equal(1201, grid.TotalResultCount);

            // A new best seed does change the rows.
            grid.AddResult(Found("BEST", 99999, 9, 9));
            Show(grid);
            Assert.Equal("BEST", grid.DisplayedResults[0].Seed);
            return 0;
        });
    }
}

using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Avalonia.Themes.Fluent;
using Avalonia.Xaml.Interactivity;
using AvaloniaEdit;
using BalatroSeedOracle.Behaviors;
using BalatroSeedOracle.Components;
using BalatroSeedOracle.Components.FilterTabs;
using BalatroSeedOracle.Models;
using BalatroSeedOracle.Services;
using BalatroSeedOracle.ViewModels;
using BalatroSeedOracle.ViewModels.FilterTabs;
using BalatroSeedOracle.Views.Modals;
using BalatroSeedOracle.Views.SearchModalTabs;
using BalatroSeedOracle.Windows;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace BalatroSeedOracle.Tests;

[Collection(HeadlessAvaloniaCollection.Name)]
public class LifecycleAndRegressionTests(HeadlessAvaloniaFixture avalonia)
{
    private T OnUi<T>(Func<T> action) => avalonia.Run(action);

    [Fact]
    public void SwayAndDragBehaviors_UnregisterWhenControlDetachesFromVisualTree()
    {
        OnUi(() =>
        {
            var swaySetField = typeof(BalatroCardSwayBehavior).GetField(
                "_activeBehaviors",
                BindingFlags.NonPublic | BindingFlags.Static
            )!;
            var dragSetField = typeof(CardDragBehavior).GetField(
                "_activeBehaviors",
                BindingFlags.NonPublic | BindingFlags.Static
            )!;

            var swaySet = (HashSet<BalatroCardSwayBehavior>)swaySetField.GetValue(null)!;
            var dragSet = (HashSet<CardDragBehavior>)dragSetField.GetValue(null)!;

            var sway = new BalatroCardSwayBehavior();
            var drag = new CardDragBehavior();
            var card = new Border();
            Interaction.GetBehaviors(card).Add(sway);
            Interaction.GetBehaviors(card).Add(drag);

            // Not yet in a TopLevel/Window visual tree
            Assert.DoesNotContain(sway, swaySet);
            Assert.DoesNotContain(drag, dragSet);

            var window = new Window { Content = card };
            window.Show();
            try
            {
                Assert.Contains(sway, swaySet);
                Assert.Contains(drag, dragSet);

                // Detach card from visual tree without calling Behavior.Detach()
                window.Content = null;

                Assert.DoesNotContain(sway, swaySet);
                Assert.DoesNotContain(drag, dragSet);

                // Re-attach card to visual tree
                window.Content = card;

                Assert.Contains(sway, swaySet);
                Assert.Contains(drag, dragSet);
            }
            finally
            {
                window.Close();
            }

            Assert.DoesNotContain(sway, swaySet);
            Assert.DoesNotContain(drag, dragSet);
            return 0;
        });
    }

    [Fact]
    public void CloneFilterItem_CopiesAllFilterItemMetadata()
    {
        OnUi(() =>
        {
            var cloneMethod = typeof(VisualBuilderTab).GetMethod(
                "CloneFilterItem",
                BindingFlags.NonPublic | BindingFlags.Static
            )!;

            var source = new FilterItem
            {
                Name = "Blueprint",
                DisplayName = "Blueprint",
                Type = "RareJoker",
                Category = "Jokers",
                ItemKey = "item_42",
                Edition = "Negative",
                Stickers = new List<string> { "eternal", "rental" },
                Rank = "Ace",
                Suit = "Spades",
                Enhancement = "Steel",
                Seal = "Red",
                IncludeBoosterPacks = false,
                IncludeShopStream = true,
                IncludeSkipTags = true,
                Status = FilterItemStatus.ShouldHave,
                IsFavorite = true,
                Score = 25,
                MinCount = 2,
                ShopSlots = new[] { 0, 1 },
                PackPositions = new[] { 2 },
                Sources = new[] { "shop", "pack" },
                IsDebuffed = true,
                IsInvertedFilter = true,
                IsInBannedItemsTray = true,
                Value = "Blueprint",
                Label = "Negative Blueprint",
                Antes = new[] { 1, 2, 3 },
            };

            var clone = (FilterItem)cloneMethod.Invoke(null, new object?[] { source, null })!;

            Assert.NotSame(source, clone);
            Assert.Equal(source.Name, clone.Name);
            Assert.Equal(source.DisplayName, clone.DisplayName);
            Assert.Equal(source.Type, clone.Type);
            Assert.Equal(source.Category, clone.Category);
            Assert.Equal(source.ItemKey, clone.ItemKey);
            Assert.Equal(source.Edition, clone.Edition);
            Assert.Equal(source.Stickers, clone.Stickers);
            Assert.NotSame(source.Stickers, clone.Stickers);
            Assert.Equal(source.Rank, clone.Rank);
            Assert.Equal(source.Suit, clone.Suit);
            Assert.Equal(source.Enhancement, clone.Enhancement);
            Assert.Equal(source.Seal, clone.Seal);
            Assert.Equal(source.IncludeBoosterPacks, clone.IncludeBoosterPacks);
            Assert.Equal(source.IncludeShopStream, clone.IncludeShopStream);
            Assert.Equal(source.IncludeSkipTags, clone.IncludeSkipTags);
            Assert.Equal(source.Status, clone.Status);
            Assert.Equal(source.IsFavorite, clone.IsFavorite);
            Assert.Equal(source.Score, clone.Score);
            Assert.Equal(source.MinCount, clone.MinCount);
            Assert.Equal(source.ShopSlots, clone.ShopSlots);
            Assert.NotSame(source.ShopSlots, clone.ShopSlots);
            Assert.Equal(source.PackPositions, clone.PackPositions);
            Assert.NotSame(source.PackPositions, clone.PackPositions);
            Assert.Equal(source.Sources, clone.Sources);
            Assert.NotSame(source.Sources, clone.Sources);
            Assert.Equal(source.IsDebuffed, clone.IsDebuffed);
            Assert.Equal(source.IsInvertedFilter, clone.IsInvertedFilter);
            Assert.Equal(source.IsInBannedItemsTray, clone.IsInBannedItemsTray);
            Assert.Equal(source.Value, clone.Value);
            Assert.Equal(source.Label, clone.Label);
            Assert.Equal(source.Antes, clone.Antes);
            Assert.NotSame(source.Antes, clone.Antes);
            return 0;
        });
    }

    [Fact]
    public void SearchModalViewModel_DetachTabViews_DetachesCachedTabFromParentWithoutThrowing()
    {
        OnUi(() =>
        {
            var detachMethod = typeof(SearchModalViewModel).GetMethod(
                "DetachControlFromParent",
                BindingFlags.NonPublic | BindingFlags.Static
            )!;

            var child = new Border();
            var host1 = new ContentControl { Content = child };
            var window = new Window { Content = host1 };
            window.Show();
            try
            {
                // Detach from host1 and reparent into host2 while window is live
                detachMethod.Invoke(null, new object?[] { child });
                Assert.Null(host1.Content);

                var host2 = new ContentControl { Content = child };
                window.Content = host2;
                Assert.Same(child, host2.Content);
            }
            finally
            {
                window.Close();
            }

            return 0;
        });
    }

    [Fact]
    public void DataGridResultsWindow_LoadResults_PopulatesColumnsAndSortedRows()
    {
        OnUi(() =>
        {
            var window = new DataGridResultsWindow();
            try
            {
                var results = new[]
                {
                    new SearchResult
                    {
                        Seed = "SEED_LOW",
                        TotalScore = 10,
                        Scores = new[] { 4, 6 },
                        Labels = new[] { "Blueprint", "Brainstorm" },
                    },
                    new SearchResult
                    {
                        Seed = "SEED_HIGH",
                        TotalScore = 50,
                        Scores = new[] { 20, 30 },
                        Labels = new[] { "Blueprint", "Brainstorm" },
                    },
                };

                window.LoadResults(results, "Test Filter");

                var gridField = typeof(DataGridResultsWindow).GetField(
                    "_resultsGrid",
                    BindingFlags.NonPublic | BindingFlags.Instance
                )!;
                var resultsGrid = (DataGrid)gridField.GetValue(window)!;

                Assert.Equal("Search Results - Test Filter", window.Title);
                Assert.Equal(5, resultsGrid.Columns.Count);
                Assert.Equal("Rank", resultsGrid.Columns[0].Header);
                Assert.Equal("Seed", resultsGrid.Columns[1].Header);
                Assert.Equal("Total Score", resultsGrid.Columns[2].Header);
                Assert.Equal("Blueprint", resultsGrid.Columns[3].Header);
                Assert.Equal("Brainstorm", resultsGrid.Columns[4].Header);

                var items = resultsGrid
                    .ItemsSource.Cast<BalatroSeedOracle.Windows.DataGridResultItem>()
                    .ToList();
                Assert.Equal(2, items.Count);
                Assert.Equal("SEED_HIGH", items[0].Seed);
                Assert.Equal(1, items[0].Rank);
                Assert.Equal(50, items[0].TotalScore);
                Assert.Equal(new[] { 20, 30 }, items[0].TallyScores);
                Assert.Equal("SEED_LOW", items[1].Seed);
                Assert.Equal(2, items[1].Rank);
            }
            finally
            {
                window.Close();
            }

            return 0;
        });
    }

    [Fact]
    public void Tabs_ReattachingToVisualTree_ResubscribesToViewModelEvents()
    {
        OnUi(() =>
        {
            var parentVm = new FiltersModalViewModel();
            var yamlVm = new JamlEditorTabViewModel(parentVm);
            var yamlTab = new YAMLEditorTab { DataContext = yamlVm };
            var yamlEditor = yamlTab.FindControl<TextEditor>("YAMLEditor")!;

            var jsonVm = new JsonEditorTabViewModel(parentVm);
            var jsonTab = new JsonEditorTab { DataContext = jsonVm };

            var validateVm = new ValidateFilterTabViewModel(parentVm);
            var validateTab = new ValidateFilterTab { DataContext = validateVm };

            var window = new Window { Content = yamlTab };
            window.Show();
            try
            {
                // Detach yamlTab (simulating tab switch)
                window.Content = validateTab;

                // Update YAML while detached, then re-attach
                yamlVm.YamlContent = "name: DetachedUpdate\ndeck: Red\nstake: White\n";
                window.Content = yamlTab;
                Assert.Equal(yamlVm.YamlContent, yamlEditor.Text);

                // Update YAML while re-attached — must still receive PropertyChanged
                yamlVm.YamlContent = "name: ReattachedUpdate\ndeck: Blue\nstake: Gold\n";
                Assert.Equal(yamlVm.YamlContent, yamlEditor.Text);

                // Detach and re-attach JsonEditorTab
                window.Content = jsonTab;
                window.Content = validateTab;
                window.Content = jsonTab;
                var jsonSubField = typeof(JsonEditorTab).GetField(
                    "_subscribedViewModel",
                    BindingFlags.NonPublic | BindingFlags.Instance
                )!;
                Assert.Same(jsonVm, jsonSubField.GetValue(jsonTab));

                // Detach and re-attach ValidateFilterTab
                window.Content = yamlTab;
                window.Content = validateTab;
                var validateSubField = typeof(ValidateFilterTab).GetField(
                    "_subscribedViewModel",
                    BindingFlags.NonPublic | BindingFlags.Instance
                )!;
                Assert.Same(validateVm, validateSubField.GetValue(validateTab));
            }
            finally
            {
                window.Close();
            }

            return 0;
        });
    }

    [Fact]
    public void ChildTabViewModels_Dispose_UnsubscribesFromParentViewModel()
    {
        OnUi(() =>
        {
            var parentVm = new FiltersModalViewModel();

            var firstVisual = new VisualBuilderTabViewModel(parentVm);
            var firstYaml = new JamlEditorTabViewModel(parentVm);

            int oldVisualNotifications = 0;
            int oldYamlNotifications = 0;
            firstVisual.PropertyChanged += (_, _) => oldVisualNotifications++;
            firstYaml.PropertyChanged += (_, _) => oldYamlNotifications++;

            // Dispose firstVisual and firstYaml (as InitializeTabs() now does)
            firstVisual.Dispose();
            firstYaml.Dispose();

            var secondVisual = new VisualBuilderTabViewModel(parentVm);
            var secondYaml = new JamlEditorTabViewModel(parentVm);

            int newVisualNotifications = 0;
            int newYamlNotifications = 0;
            secondVisual.PropertyChanged += (_, _) => newVisualNotifications++;
            secondYaml.PropertyChanged += (_, _) => newYamlNotifications++;

            parentVm.FilterName = "Updated Filter Name";

            Assert.Equal(0, oldVisualNotifications);
            Assert.Equal(0, oldYamlNotifications);
            Assert.True(newVisualNotifications > 0);
            Assert.True(newYamlNotifications > 0);

            secondVisual.Dispose();
            secondYaml.Dispose();
            return 0;
        });
    }

    [Fact]
    public void DeckStakeTabViewModel_And_AnalyzeModalViewModel_SyncDeckAndStakeIndices()
    {
        OnUi(() =>
        {
            var parentVm = new FiltersModalViewModel
            {
                SelectedDeckIndex = 4,
                SelectedStakeIndex = 6,
            };
            var deckStakeVm = new DeckStakeTabViewModel(parentVm);

            Assert.Equal(4, deckStakeVm.SelectedDeckIndex);
            Assert.Equal(6, deckStakeVm.SelectedStakeIndex);

            deckStakeVm.SelectedDeckIndex = 1;
            deckStakeVm.SelectedStakeIndex = 2;

            Assert.Equal(1, parentVm.SelectedDeckIndex);
            Assert.Equal(2, parentVm.SelectedStakeIndex);
            Assert.Equal("Blue", parentVm.SelectedDeck.ToString());
            Assert.Equal("Green", parentVm.SelectedStake.ToString());

            var analyzeVm = new AnalyzeModalViewModel(
                SpriteService.Instance,
                new UserProfileService()
            );
            analyzeVm.DeckIndex = 2;
            analyzeVm.StakeIndex = 5;
            Assert.Equal("Yellow", analyzeVm.SelectedDeck.ToString());
            Assert.Equal("Purple", analyzeVm.SelectedStake.ToString());
            return 0;
        });
    }

    [Fact]
    public void StandardModal_SetContent_DetachesPreviousParentWithoutThrowing()
    {
        OnUi(() =>
        {
            var sharedChild = new Border();
            var modal1 = new StandardModal("FIRST");
            modal1.SetContent(sharedChild);

            var window = new Window { Content = modal1 };
            window.Show();
            try
            {
                var modal2 = new StandardModal("SECOND");
                modal2.SetContent(sharedChild);
                window.Content = modal2;

                Assert.Null(modal1.GetContent());
                Assert.Same(sharedChild, modal2.GetContent());
            }
            finally
            {
                window.Close();
            }

            return 0;
        });
    }
}

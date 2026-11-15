using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Runtime.CompilerServices;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Templates;
using Avalonia.Input.Platform;
using Avalonia.Layout;
using Avalonia.Media;
using BalatroSeedOracle.Helpers;
using BalatroSeedOracle.Models;
using BalatroSeedOracle.ViewModels.Controls;

namespace BalatroSeedOracle.Controls;

/// <summary>
/// The results grid: stats header, actions, and the grid of seeds.
/// The grid control is chosen at construction, see <see cref="BuildGrid"/>.
/// </summary>
public partial class SortableResultsGrid : UserControl
{
    public static readonly StyledProperty<ObservableCollection<SearchResult>?> ItemsSourceProperty =
        AvaloniaProperty.Register<SortableResultsGrid, ObservableCollection<SearchResult>?>(
            nameof(ItemsSource)
        );

    public ObservableCollection<SearchResult>? ItemsSource
    {
        get => GetValue(ItemsSourceProperty);
        set => SetValue(ItemsSourceProperty, value);
    }

    public SortableResultsGridViewModel ViewModel { get; }

    // Events forwarded from ViewModel
    public event EventHandler<SearchResult>? AddToFavoritesRequested
    {
        add => ViewModel.AddToFavoritesRequested += value;
        remove => ViewModel.AddToFavoritesRequested -= value;
    }

    public event EventHandler<IEnumerable<SearchResult>>? ExportAllRequested
    {
        add => ViewModel.ExportAllRequested += value;
        remove => ViewModel.ExportAllRequested -= value;
    }

    public event EventHandler? PopOutRequested
    {
        add => ViewModel.PopOutRequested += value;
        remove => ViewModel.PopOutRequested -= value;
    }

    public event EventHandler<SearchResult>? AnalyzeRequested
    {
        add => ViewModel.AnalyzeRequested += value;
        remove => ViewModel.AnalyzeRequested -= value;
    }

    public SortableResultsGrid()
    {
        ViewModel = new SortableResultsGridViewModel();
        DataContext = ViewModel;

        InitializeComponent();
        GridHost.Content = BuildGrid();

        // Wire up clipboard
        ViewModel.CopyToClipboardRequested += async (s, text) =>
        {
            try
            {
                var topLevel = TopLevel.GetTopLevel(this);
                if (topLevel?.Clipboard != null)
                {
                    await topLevel.Clipboard.SetTextAsync(text);
                    DebugLogger.Log("SortableResultsGrid", $"Copied: {text}");
                }
            }
            catch (Exception ex)
            {
                DebugLogger.LogError("SortableResultsGrid", $"Copy failed: {ex.Message}");
            }
        };
    }

    /// <summary>
    /// TreeDataGrid when this build carries an Avalonia Accelerate key. Without one its type
    /// initializer throws, and the same columns show in the free DataGrid.
    /// </summary>
    private Control BuildGrid()
    {
        try
        {
            return BuildLicensedGrid();
        }
        catch (TypeInitializationException ex)
        {
            DebugLogger.Log(
                "SortableResultsGrid",
                $"TreeDataGrid cannot load ({ex.InnerException?.Message ?? ex.Message}). Showing results in DataGrid."
            );
            return BuildFreeGrid();
        }
    }

    // Kept out of line so the TreeDataGrid type initializer runs inside BuildGrid's try.
    [MethodImpl(MethodImplOptions.NoInlining)]
    private TreeDataGrid BuildLicensedGrid()
    {
        var grid = new TreeDataGrid
        {
            CanUserSortColumns = true,
            CanUserResizeColumns = true,
            Foreground = Brushes.White,
            Source = ViewModel.GridSource,
        };
        grid.Bind(BackgroundProperty, this.GetResourceObservable("DarkBackground"));
        ViewModel.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName == nameof(SortableResultsGridViewModel.GridSource))
                grid.Source = ViewModel.GridSource;
        };
        return grid;
    }

    private DataGrid BuildFreeGrid()
    {
        var grid = new DataGrid
        {
            AutoGenerateColumns = false,
            IsReadOnly = true,
            CanUserSortColumns = true,
            CanUserResizeColumns = true,
            SelectionMode = DataGridSelectionMode.Extended,
            ItemsSource = ViewModel.DisplayedResults,
            FontSize = 18,
        };
        grid.SelectionChanged += (_, _) =>
        {
            ViewModel.SelectedItems.Clear();
            foreach (var item in grid.SelectedItems)
            {
                if (item is SearchResult result)
                    ViewModel.SelectedItems.Add(result);
            }
        };
        ViewModel.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName == nameof(SortableResultsGridViewModel.TallyLabels))
                RebuildFreeGridColumns(grid);
        };
        RebuildFreeGridColumns(grid);
        return grid;
    }

    /// <summary>SEED, SCORE and one column per tally label, as the TreeDataGrid source defines them.</summary>
    private void RebuildFreeGridColumns(DataGrid grid)
    {
        grid.Columns.Clear();
        grid.Columns.Add(FreeGridColumn("SEED", 130, r => r.Seed, (a, b) => string.CompareOrdinal(a.Seed, b.Seed)));
        grid.Columns.Add(
            FreeGridColumn("SCORE", 90, r => r.TotalScore.ToString(), (a, b) => a.TotalScore.CompareTo(b.TotalScore))
        );
        for (int i = 0; i < ViewModel.TallyLabels.Count; i++)
        {
            var index = i;
            grid.Columns.Add(
                FreeGridColumn(
                    ViewModel.TallyLabels[i].ToUpperInvariant(),
                    70,
                    r => TallyAt(r, index)?.ToString() ?? "-",
                    (a, b) => (TallyAt(a, index) ?? int.MinValue).CompareTo(TallyAt(b, index) ?? int.MinValue)
                )
            );
        }
    }

    private static int? TallyAt(SearchResult result, int index) =>
        result.Scores is not null && index < result.Scores.Length ? result.Scores[index] : null;

    // A template column with its own comparer: no reflection binding, so it holds in a trimmed build.
    // It grows past minWidth to fit its header, which for a tally is the clause's name.
    private static DataGridTemplateColumn FreeGridColumn(
        string header,
        double minWidth,
        Func<SearchResult, string> text,
        Comparison<SearchResult> compare
    ) =>
        new()
        {
            Header = header,
            Width = DataGridLength.Auto,
            MinWidth = minWidth,
            CanUserSort = true,
            CustomSortComparer = Comparer<object>.Create(
                (a, b) => a is SearchResult x && b is SearchResult y ? compare(x, y) : 0
            ),
            CellTemplate = new FuncDataTemplate<SearchResult>(
                (result, _) =>
                {
                    var cell = new TextBlock
                    {
                        Text = result is null ? "" : text(result),
                        VerticalAlignment = VerticalAlignment.Center,
                    };
                    // A recycled row hands the cell a new result.
                    cell.DataContextChanged += (_, _) =>
                        cell.Text = cell.DataContext is SearchResult current ? text(current) : "";
                    return cell;
                },
                supportsRecycling: true
            ),
        };

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);

        if (change.Property == ItemsSourceProperty)
        {
            var oldCollection = change.OldValue as ObservableCollection<SearchResult>;
            var newCollection = change.NewValue as ObservableCollection<SearchResult>;

            // Unsubscribe from old
            if (oldCollection != null)
            {
                oldCollection.CollectionChanged -= OnItemsSourceCollectionChanged;
            }

            // Subscribe to new
            if (newCollection != null)
            {
                newCollection.CollectionChanged += OnItemsSourceCollectionChanged;

                // Initial load
                if (newCollection.Count > 0)
                {
                    ViewModel.ClearResults();
                    ViewModel.AddResults(newCollection);
                }
            }
            else
            {
                ViewModel.ClearResults();
            }
        }
    }

    private void OnItemsSourceCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        switch (e.Action)
        {
            case NotifyCollectionChangedAction.Add:
                if (e.NewItems != null)
                {
                    foreach (SearchResult item in e.NewItems)
                    {
                        ViewModel.AddResult(item);
                    }
                }
                break;

            case NotifyCollectionChangedAction.Reset:
                ViewModel.ClearResults();
                break;

            case NotifyCollectionChangedAction.Remove:
                // Rebuild from source
                ViewModel.ClearResults();
                if (ItemsSource != null)
                {
                    ViewModel.AddResults(ItemsSource);
                }
                break;
        }
    }

    public void ForceRefreshResults(IEnumerable<SearchResult> results)
    {
        ViewModel.ClearResults();
        ViewModel.AddResults(results);
    }
}

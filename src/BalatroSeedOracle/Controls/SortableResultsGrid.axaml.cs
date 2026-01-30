using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using Avalonia;
using Avalonia.Controls;
using BalatroSeedOracle.Helpers;
using BalatroSeedOracle.Models;
using BalatroSeedOracle.ViewModels.Controls;

namespace BalatroSeedOracle.Controls;

/// <summary>
/// Clean results grid using TreeDataGrid with proper MVVM.
/// No code-behind logic - just property forwarding and event exposure.
/// </summary>
public partial class SortableResultsGrid : UserControl
{
    public static readonly StyledProperty<ObservableCollection<SearchResult>?> ItemsSourceProperty =
        AvaloniaProperty.Register<SortableResultsGrid, ObservableCollection<SearchResult>?>(
            nameof(ItemsSource));

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

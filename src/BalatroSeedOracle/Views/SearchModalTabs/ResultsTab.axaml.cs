using System;
using System.Collections.Generic;
using Avalonia.Controls;
using BalatroSeedOracle.Helpers;
using BalatroSeedOracle.Models;
using BalatroSeedOracle.ViewModels;

namespace BalatroSeedOracle.Views.SearchModalTabs
{
    /// <summary>
    /// Results tab for search modal.
    /// MVVM: Bindings come from SearchModalViewModel. Control events on SortableResultsGrid
    /// are forwarded to ViewModel commands; window pop-out is handled via a VM event.
    /// </summary>
    public partial class ResultsTab : UserControl
    {
        public ResultsTab()
        {
            InitializeComponent();
            this.AttachedToVisualTree += OnAttachedToVisualTree;
            this.DetachedFromVisualTree += OnDetachedFromVisualTree;
        }

        private void OnAttachedToVisualTree(object? sender, Avalonia.VisualTreeAttachmentEventArgs e)
        {
            if (DataContext is SearchModalViewModel vm && ResultsGrid != null)
            {
                DebugLogger.Log(
                    "ResultsTab",
                    $"OnAttachedToVisualTree: Grid found with {vm.SearchResults.Count} search results available for binding"
                );

                // Ensure no duplicate subscriptions across tab switches
                ResultsGrid.ExportAllRequested -= OnExportAllRequested;
                ResultsGrid.AnalyzeRequested -= OnAnalyzeRequested;
                ResultsGrid.PopOutRequested -= OnPopOutRequested;

                ResultsGrid.ExportAllRequested += OnExportAllRequested;
                ResultsGrid.AnalyzeRequested += OnAnalyzeRequested;
                ResultsGrid.PopOutRequested += OnPopOutRequested;
            }
        }

        private void OnDetachedFromVisualTree(object? sender, Avalonia.VisualTreeAttachmentEventArgs e)
        {
            if (ResultsGrid != null)
            {
                ResultsGrid.ExportAllRequested -= OnExportAllRequested;
                ResultsGrid.AnalyzeRequested -= OnAnalyzeRequested;
                ResultsGrid.PopOutRequested -= OnPopOutRequested;
            }
        }

        private async void OnExportAllRequested(object? sender, IEnumerable<SearchResult> results)
        {
            if (DataContext is SearchModalViewModel vm)
            {
                await vm.ExportSearchResultsAsync(TopLevel.GetTopLevel(this), results);
            }
        }

        private void OnAnalyzeRequested(object? sender, SearchResult? result)
        {
            if (DataContext is SearchModalViewModel vm)
            {
                vm.OpenAnalyzeModalForSeed(result?.Seed);
            }
        }

        private void OnPopOutRequested(object? sender, EventArgs e)
        {
            if (DataContext is SearchModalViewModel vm)
            {
                vm.RequestPopOutResults();
            }
        }

        /// <summary>
        /// Public shim retained for SearchModalViewModel which calls this directly to bypass
        /// ItemsSource async refresh latency. See SearchModalViewModel.LoadExistingResults.
        /// </summary>
        public void ForceRefreshResults(
            System.Collections.Generic.IEnumerable<Models.SearchResult> results
        )
        {
            ResultsGrid?.ForceRefreshResults(results);
        }
    }
}

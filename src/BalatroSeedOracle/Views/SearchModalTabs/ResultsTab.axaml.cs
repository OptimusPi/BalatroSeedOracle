using System;
using Avalonia.Controls;
using BalatroSeedOracle.Helpers;
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
        }

        private void OnAttachedToVisualTree(object? sender, Avalonia.VisualTreeAttachmentEventArgs e)
        {
            if (DataContext is SearchModalViewModel vm && ResultsGrid != null)
            {
                DebugLogger.Log(
                    "ResultsTab",
                    $"OnAttachedToVisualTree: Grid found with {vm.SearchResults.Count} search results available for binding"
                );

                // Forward grid events to VM commands (business logic lives in the VM).
                ResultsGrid.ExportAllRequested += async (s, results) =>
                    await vm.ExportSearchResultsAsync(TopLevel.GetTopLevel(this), results);

                ResultsGrid.AnalyzeRequested += (s, result) =>
                    vm.OpenAnalyzeModalForSeed(result?.Seed);

                ResultsGrid.PopOutRequested += (s, e2) => vm.RequestPopOutResults();
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

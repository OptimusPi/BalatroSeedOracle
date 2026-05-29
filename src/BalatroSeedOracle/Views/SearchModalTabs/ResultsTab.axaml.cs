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

                ResultsGrid.AddToFavoritesRequested += (s, result) =>
                    vm.AddSeedToFavorites(result?.Seed);

                ResultsGrid.AnalyzeRequested += (s, result) =>
                    vm.OpenAnalyzeModalForSeed(result?.Seed);

                // VM raises ShowDataGridResultsRequested; the View opens the window
                // (window construction is a View concern, kept out of the VM).
                vm.ShowDataGridResultsRequested -= OnShowDataGridResultsRequested;
                vm.ShowDataGridResultsRequested += OnShowDataGridResultsRequested;

                ResultsGrid.PopOutRequested += (s, e2) => vm.RequestPopOutResults();
            }
        }

        private void OnShowDataGridResultsRequested(
            object? sender,
            (Services.ActiveSearchContext Search, string? FilterName) args
        )
        {
            try
            {
                var exportService = App.GetService<Services.Export.ResultsExportService>();
                if (exportService == null)
                {
                    DebugLogger.LogError("ResultsTab", "ResultsExportService not found in App services");
                    return;
                }
                var window = new Windows.DataGridResultsWindow(args.Search, exportService, args.FilterName);
                window.Show();
            }
            catch (Exception ex)
            {
                DebugLogger.LogError("ResultsTab", $"Failed to open DataGrid window: {ex.Message}");
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

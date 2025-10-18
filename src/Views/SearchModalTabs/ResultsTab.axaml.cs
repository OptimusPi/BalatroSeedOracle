using System;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using BalatroSeedOracle.Controls;
using BalatroSeedOracle.ViewModels;
using BalatroSeedOracle.Services;
using BalatroSeedOracle.Views.Modals;
using BalatroSeedOracle.Helpers;

namespace BalatroSeedOracle.Views.SearchModalTabs
{
    public partial class ResultsTab : UserControl
    {
        public ResultsTab()
        {
            InitializeComponent();
            this.AttachedToVisualTree += OnAttachedToVisualTree;
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }

        private void OnAttachedToVisualTree(object? sender, VisualTreeAttachmentEventArgs e)
        {
            var grid = this.FindControl<SortableResultsGrid>("ResultsGrid");
            if (DataContext is SearchModalViewModel vm && grid != null)
            {
                // Bind results to the sortable grid
                grid.ItemsSource = vm.SearchResults;

                // Forward export-all with CSV format: SEED,TOTALSCORE,<tally labels>
                grid.ExportAllRequested += async (s, results) =>
                {
                    try
                    {
                        if (results == null || !results.Any())
                        {
                            DebugLogger.Log("ResultsTab", "No results to export");
                            return;
                        }

                        var first = results.First();
                        var labels = first?.Labels ?? Array.Empty<string>();

                        // Build CSV header: SEED,TOTALSCORE,<labels>
                        var header = "SEED,TOTALSCORE";
                        if (labels.Length > 0)
                        {
                            header += "," + string.Join(",", labels.Select(l => l.ToUpperInvariant()));
                        }

                        // Build CSV rows
                        var csv = new System.Text.StringBuilder();
                        csv.AppendLine(header);

                        foreach (var result in results)
                        {
                            var row = $"{result.Seed},{result.TotalScore}";
                            if (result.Scores != null && result.Scores.Length > 0)
                            {
                                row += "," + string.Join(",", result.Scores);
                            }
                            csv.AppendLine(row);
                        }

                        // Save to publish/results.csv
                        var publishDir = System.IO.Path.Combine(System.IO.Directory.GetCurrentDirectory(), "publish");
                        System.IO.Directory.CreateDirectory(publishDir);
                        var csvPath = System.IO.Path.Combine(publishDir, "results.csv");

                        await System.IO.File.WriteAllTextAsync(csvPath, csv.ToString());
                        DebugLogger.Log("ResultsTab", $"Exported {results.Count()} results to {csvPath}");
                    }
                    catch (System.Exception ex)
                    {
                        DebugLogger.LogError("ResultsTab", $"Export failed: {ex.Message}");
                    }
                };

                // Wire up add-to-favorites
                grid.AddToFavoritesRequested += (s, result) =>
                {
                    if (!string.IsNullOrWhiteSpace(result?.Seed))
                    {
                        FavoritesService.Instance.AddFavoriteItem(result.Seed);
                    }
                };

                // Wire up analyze request to open Analyze modal with seed
                grid.AnalyzeRequested += (s, result) =>
                {
                    if (!string.IsNullOrWhiteSpace(result?.Seed) && vm.MainMenu != null)
                    {
                        var analyzeModal = new AnalyzeModal();
                        analyzeModal.SetSeedAndAnalyze(result.Seed);
                        vm.MainMenu.ShowModal("SEED ANALYZER", analyzeModal);
                    }
                };
            }
        }
    }
}

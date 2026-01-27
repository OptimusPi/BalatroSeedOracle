using System;
using System.Collections.Generic;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using BalatroSeedOracle.Controls;
using BalatroSeedOracle.Helpers;
using BalatroSeedOracle.Services;
using BalatroSeedOracle.Services.Export;
using BalatroSeedOracle.ViewModels;
using BalatroSeedOracle.Views.Modals;
using Microsoft.Extensions.DependencyInjection;

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
                // The ItemsSource binding is handled via XAML - no need to set explicitly
                // This allows the proper binding flow: SearchResults -> grid.ItemsSource -> grid.ViewModel.DisplayedResults

                DebugLogger.Log(
                    "ResultsTab",
                    $"OnAttachedToVisualTree: Grid found with {vm.SearchResults.Count} search results available for binding"
                );

                // CRITICAL FIX: Export to EXCEL with ClosedXML for proper data handling
                grid.ExportAllRequested += async (s, results) =>
                {
                    try
                    {
                        if (results == null || !results.Any())
                        {
                            DebugLogger.Log("ResultsTab", "No results to export");
                            return;
                        }

                        // Get TopLevel for file picker
                        var topLevel = Avalonia.Controls.TopLevel.GetTopLevel(this);
                        if (topLevel == null)
                        {
                            DebugLogger.LogError(
                                "ResultsTab",
                                "Could not get TopLevel for file picker"
                            );
                            return;
                        }

                        // Show save file dialog
                        var file = await topLevel.StorageProvider.SaveFilePickerAsync(
                            new Avalonia.Platform.Storage.FilePickerSaveOptions
                            {
                                Title = "Export Search Results to Excel",
                                DefaultExtension = "xlsx",
                                SuggestedFileName =
                                    $"search_results_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx",
                                FileTypeChoices = new[]
                                {
                                    new Avalonia.Platform.Storage.FilePickerFileType("Excel Files")
                                    {
                                        Patterns = new[] { "*.xlsx" },
                                    },
                                    new Avalonia.Platform.Storage.FilePickerFileType("CSV Files")
                                    {
                                        Patterns = new[] { "*.csv" },
                                    },
                                },
                            }
                        );

                        if (file == null)
                        {
                            DebugLogger.Log("ResultsTab", "Export cancelled by user");
                            return;
                        }

                        var first = results.First();
                        var labels = first?.Labels ?? Array.Empty<string>();

                        // Check file extension to determine export format
                        var filePath = file.Path.LocalPath;
                        if (filePath.EndsWith(".xlsx", StringComparison.OrdinalIgnoreCase))
                        {
                            // EXCEL EXPORT using platform-specific IExcelExporter
                            var excelExporter = App.GetService<IExcelExporter>();
                            if (excelExporter == null || !excelExporter.IsAvailable)
                            {
                                DebugLogger.Log(
                                    "ResultsTab",
                                    "Excel export not available on this platform"
                                );
                                return;
                            }

                            // Build headers
                            var headers = new List<string> { "SEED", "TOTALSCORE" };
                            headers.AddRange(labels.Select(l => l.ToUpperInvariant()));

                            // Build data rows
                            var rows = new List<IReadOnlyList<object?>>();
                            foreach (var result in results)
                            {
                                var row = new List<object?> { result.Seed, result.TotalScore };
                                if (result.Scores != null)
                                {
                                    row.AddRange(result.Scores.Cast<object?>());
                                }
                                rows.Add(row);
                            }

                            await excelExporter.ExportAsync(
                                filePath,
                                "Search Results",
                                headers,
                                rows
                            );
                            DebugLogger.Log(
                                "ResultsTab",
                                $"Exported {results.Count()} results to Excel: {filePath}"
                            );
                        }
                        else
                        {
                            // CSV EXPORT as fallback
                            var header = "SEED,TOTALSCORE";
                            if (labels.Length > 0)
                            {
                                header +=
                                    ","
                                    + string.Join(",", labels.Select(l => l.ToUpperInvariant()));
                            }

                            var csv = new System.Text.StringBuilder();
                            csv.AppendLine(header);

                            foreach (var result in results)
                            {
                                var csvRow = $"{result.Seed},{result.TotalScore}";
                                if (result.Scores != null && result.Scores.Length > 0)
                                {
                                    csvRow += "," + string.Join(",", result.Scores);
                                }
                                csv.AppendLine(csvRow);
                            }

                            await System.IO.File.WriteAllTextAsync(filePath, csv.ToString());
                            DebugLogger.Log(
                                "ResultsTab",
                                $"Exported {results.Count()} results to CSV: {filePath}"
                            );
                        }
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

                // Wire up pop-out to separate window
                grid.PopOutRequested += (s, e) =>
                {
                    try
                    {
                        // Get the search manager to find the active search instance
                        var searchManager = App.GetService<SearchManager>();
                        if (searchManager == null || string.IsNullOrEmpty(vm.CurrentSearchId))
                        {
                            DebugLogger.LogError(
                                "ResultsTab",
                                "Cannot pop out - no active search manager or search ID"
                            );
                            return;
                        }

                        var searchInstance = searchManager.GetSearch(vm.CurrentSearchId);
                        if (searchInstance == null)
                        {
                            DebugLogger.LogError(
                                "ResultsTab",
                                $"Cannot pop out - search instance not found: {vm.CurrentSearchId}"
                            );
                            return;
                        }

                        // Create and show the pop-out window
#if !BROWSER
                        var popOutWindow = new Windows.DataGridResultsWindow(
                            searchInstance,
                            vm.LoadedConfig?.Name
                        );
                        popOutWindow.Show();
#endif
                        DebugLogger.Log(
                            "ResultsTab",
                            $"Popped out results to separate window for search: {vm.CurrentSearchId}"
                        );
                    }
                    catch (Exception ex)
                    {
                        DebugLogger.LogError(
                            "ResultsTab",
                            $"Failed to pop out results: {ex.Message}"
                        );
                    }
                };
            }
        }

        public void ForceRefreshResults(
            System.Collections.Generic.IEnumerable<Models.SearchResult> results
        )
        {
            var grid = this.FindControl<SortableResultsGrid>("ResultsGrid");
            grid?.ForceRefreshResults(results);
        }
    }
}

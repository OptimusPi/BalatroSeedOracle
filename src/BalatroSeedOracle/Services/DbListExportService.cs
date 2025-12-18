using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Avalonia.Platform.Storage;
using BalatroSeedOracle.Models;
using BalatroSeedOracle.Helpers;

namespace BalatroSeedOracle.Services
{
    /// <summary>
    /// Export service for DBList query results
    /// </summary>
    public class DbListExportService
    {
        /// <summary>
        /// Export search results to CSV format
        /// </summary>
        public async Task<bool> ExportToCsvAsync(
            List<SearchResult> results, 
            IStorageProvider storageProvider,
            string defaultFileName = "dblist_results.csv")
        {
            try
            {
                if (results == null || results.Count == 0)
                {
                    DebugLogger.Log("DbListExportService", "No results to export");
                    return false;
                }

                // Show save file dialog
                var file = await storageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
                {
                    Title = "Export DBList Results to CSV",
                    SuggestedFileName = defaultFileName,
                    FileTypeChoices = new[]
                    {
                        new FilePickerFileType("CSV Files")
                        {
                            Patterns = new[] { "*.csv" },
                            MimeTypes = new[] { "text/csv" }
                        }
                    }
                });

                if (file == null)
                    return false;

                // Generate CSV content
                var csvContent = GenerateCsvContent(results);
                
                // Write to file
                await using var stream = await file.OpenWriteAsync();
                using var writer = new StreamWriter(stream, Encoding.UTF8);
                
                await writer.WriteAsync(csvContent);
                await writer.FlushAsync();

                DebugLogger.Log("DbListExportService", $"Exported {results.Count} results to {file.Name}");
                return true;
            }
            catch (Exception ex)
            {
                DebugLogger.LogError("DbListExportService", $"Failed to export CSV: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Export search results to JSON format
        /// </summary>
        public async Task<bool> ExportToJsonAsync(
            List<SearchResult> results, 
            IStorageProvider storageProvider,
            string defaultFileName = "dblist_results.json")
        {
            try
            {
                if (results == null || results.Count == 0)
                {
                    DebugLogger.Log("DbListExportService", "No results to export");
                    return false;
                }

                // Show save file dialog
                var file = await storageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
                {
                    Title = "Export DBList Results to JSON",
                    SuggestedFileName = defaultFileName,
                    FileTypeChoices = new[]
                    {
                        new FilePickerFileType("JSON Files")
                        {
                            Patterns = new[] { "*.json" },
                            MimeTypes = new[] { "application/json" }
                        }
                    }
                });

                if (file == null)
                    return false;

                // Generate JSON content
                var jsonContent = GenerateJsonContent(results);
                
                // Write to file
                await using var stream = await file.OpenWriteAsync();
                using var writer = new StreamWriter(stream, Encoding.UTF8);
                
                await writer.WriteAsync(jsonContent);
                await writer.FlushAsync();

                DebugLogger.Log("DbListExportService", $"Exported {results.Count} results to {file.Name}");
                return true;
            }
            catch (Exception ex)
            {
                DebugLogger.LogError("DbListExportService", $"Failed to export JSON: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Export search results to text format (wordlist)
        /// </summary>
        public async Task<bool> ExportToTextAsync(
            List<SearchResult> results, 
            IStorageProvider storageProvider,
            string defaultFileName = "dblist_seeds.txt")
        {
            try
            {
                if (results == null || results.Count == 0)
                {
                    DebugLogger.Log("DbListExportService", "No results to export");
                    return false;
                }

                // Show save file dialog
                var file = await storageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
                {
                    Title = "Export DBList Seeds to Text",
                    SuggestedFileName = defaultFileName,
                    FileTypeChoices = new[]
                    {
                        new FilePickerFileType("Text Files")
                        {
                            Patterns = new[] { "*.txt" },
                            MimeTypes = new[] { "text/plain" }
                        }
                    }
                });

                if (file == null)
                    return false;

                // Generate text content (just seeds, one per line)
                var textContent = string.Join(Environment.NewLine, results.Select(r => r.Seed));
                
                // Write to file
                await using var stream = await file.OpenWriteAsync();
                using var writer = new StreamWriter(stream, Encoding.UTF8);
                
                await writer.WriteAsync(textContent);
                await writer.FlushAsync();

                DebugLogger.Log("DbListExportService", $"Exported {results.Count} seeds to {file.Name}");
                return true;
            }
            catch (Exception ex)
            {
                DebugLogger.LogError("DbListExportService", $"Failed to export text: {ex.Message}");
                return false;
            }
        }

        private string GenerateCsvContent(List<SearchResult> results)
        {
            var csv = new StringBuilder();
            
            // Header
            csv.AppendLine("Seed,TotalScore,Scores,Labels");
            
            // Data rows
            foreach (var result in results)
            {
                var seed = EscapeCsvField(result.Seed);
                var score = result.TotalScore;
                var scores = result.Scores != null ? string.Join(";", result.Scores) : "";
                var labels = result.Labels != null ? string.Join(";", result.Labels) : "";
                
                csv.AppendLine($"{seed},{score},{EscapeCsvField(scores)},{EscapeCsvField(labels)}");
            }
            
            return csv.ToString();
        }

        private string GenerateJsonContent(List<SearchResult> results)
        {
            var exportData = new
            {
                ExportDate = DateTime.UtcNow,
                TotalResults = results.Count,
                Results = results.Select(r => new
                {
                    Seed = r.Seed,
                    TotalScore = r.TotalScore,
                    Scores = r.Scores,
                    Labels = r.Labels,
                    ScoresDisplay = r.ScoresDisplay
                }).ToArray()
            };

            return System.Text.Json.JsonSerializer.Serialize(exportData, new System.Text.Json.JsonSerializerOptions
            {
                WriteIndented = true,
                PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase
            });
        }

        private string EscapeCsvField(string field)
        {
            if (string.IsNullOrEmpty(field))
                return "";
            
            // If field contains comma, quote, or newline, wrap in quotes and escape internal quotes
            if (field.Contains(',') || field.Contains('"') || field.Contains('\n') || field.Contains('\r'))
            {
                return $"\"{field.Replace("\"", "\"\"")}\"";
            }
            
            return field;
        }
    }
}

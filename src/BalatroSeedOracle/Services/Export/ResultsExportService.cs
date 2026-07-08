using System;
using BalatroSeedOracle.Json;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using BalatroSeedOracle.Models;
using BalatroSeedOracle.Helpers;

namespace BalatroSeedOracle.Services.Export
{
    public class ResultsExportService
    {
        private readonly IPlatformServices _platformServices;

        public ResultsExportService(IPlatformServices platformServices)
        {
            _platformServices = platformServices ?? throw new ArgumentNullException(nameof(platformServices));
        }

        /// <summary>
        /// Exports search results to CSV format.
        /// </summary>
        public async Task ExportToCsvAsync(Stream stream, IEnumerable<SearchResult> results)
        {
            if (results == null || !results.Any())
                return;

            var first = results.First();
            var labels = first?.Labels ?? Array.Empty<string>();

            var header = "SEED,TOTALSCORE";
            if (labels.Length > 0)
            {
                header += "," + string.Join(",", labels.Select(l => l.ToUpperInvariant()));
            }

            var csv = new StringBuilder();
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

            using var writer = new StreamWriter(stream, Encoding.UTF8, leaveOpen: true);
            await writer.WriteAsync(csv.ToString());
            await writer.FlushAsync();
        }

        /// <summary>
        /// Exports raw seeds list to plain text wordlist format (one seed per line).
        /// </summary>
        public async Task ExportToWordlistAsync(Stream stream, IEnumerable<string> seeds)
        {
            using var writer = new StreamWriter(stream, Encoding.UTF8, leaveOpen: true);
            foreach (var seed in seeds)
            {
                await writer.WriteLineAsync(seed);
            }
            await writer.FlushAsync();
        }

        /// <summary>
        /// Exports search results to a human-readable text report.
        /// </summary>
        public async Task ExportToTextReportAsync(Stream stream, IEnumerable<SearchResult> results, string? filterName = null)
        {
            var exportText = $"Balatro Seed Search Results\n";
            exportText += $"Generated: {DateTime.Now:yyyy-MM-dd HH:mm:ss}\n";
            exportText += $"Filter: {filterName ?? "Unknown"}\n";
            exportText += $"Total Results: {results.Count()}\n";
            exportText += new string('=', 50) + "\n\n";

            foreach (var result in results)
            {
                exportText += $"Seed: {result.Seed}\n";
                exportText += $"Score: {result.TotalScore}\n";
                if (result.Scores is not null && result.Scores.Length > 0)
                {
                    exportText += $"Scores: {string.Join(", ", result.Scores)}\n";
                }
                exportText += "\n";
            }

            using var writer = new StreamWriter(stream, Encoding.UTF8, leaveOpen: true);
            await writer.WriteAsync(exportText);
            await writer.FlushAsync();
        }

        /// <summary>
        /// Exports search results to JSON format.
        /// </summary>
        public async Task ExportToJsonAsync(Stream stream, IEnumerable<SearchResult> results)
        {
            var exportData = new SearchResultExport
            {
                ExportDate = DateTime.UtcNow,
                TotalResults = results.Count(),
                Results = results
                    .Select(r => new SearchResultExportRow
                    {
                        Seed = r.Seed,
                        TotalScore = r.TotalScore,
                        Scores = r.Scores,
                        Labels = r.Labels,
                        ScoresDisplay = r.ScoresDisplay,
                    })
                    .ToList(),
            };

            var json = JsonSerializer.Serialize(
                exportData,
                Json.BsoJsonSerializerContext.Default.SearchResultExport
            );

            using var writer = new StreamWriter(stream, Encoding.UTF8, leaveOpen: true);
            await writer.WriteAsync(json);
            await writer.FlushAsync();
        }

        /// <summary>
        /// Exports search results to CSV format file.
        /// </summary>
        public async Task ExportToCsvFileAsync(string filePath, IEnumerable<SearchResult> results)
        {
            using var stream = new FileStream(filePath, FileMode.Create, FileAccess.Write, FileShare.None, 4096, useAsync: true);
            await ExportToCsvAsync(stream, results);
        }

        /// <summary>
        /// Exports raw seeds to plain text file.
        /// </summary>
        public async Task ExportToWordlistFileAsync(string filePath, IEnumerable<string> seeds)
        {
            using var stream = new FileStream(filePath, FileMode.Create, FileAccess.Write, FileShare.None, 4096, useAsync: true);
            await ExportToWordlistAsync(stream, seeds);
        }

        /// <summary>
        /// Exports search results to a human-readable text report file.
        /// </summary>
        public async Task ExportToTextReportFileAsync(string filePath, IEnumerable<SearchResult> results, string? filterName = null)
        {
            using var stream = new FileStream(filePath, FileMode.Create, FileAccess.Write, FileShare.None, 4096, useAsync: true);
            await ExportToTextReportAsync(stream, results, filterName);
        }

        /// <summary>
        /// Exports search results to JSON format file.
        /// </summary>
        public async Task ExportToJsonFileAsync(string filePath, IEnumerable<SearchResult> results)
        {
            using var stream = new FileStream(filePath, FileMode.Create, FileAccess.Write, FileShare.None, 4096, useAsync: true);
            await ExportToJsonAsync(stream, results);
        }

        /// <summary>
        /// Exports search results to Parquet or other formats handled by the search context.
        /// </summary>
        public void ExportToContextFormat(ActiveSearchContext searchContext, string outputPath)
        {
            if (searchContext == null)
                throw new ArgumentNullException(nameof(searchContext));
            searchContext.ExportTo(outputPath);
        }
    }
}

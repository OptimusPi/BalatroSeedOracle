using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BalatroSeedOracle.Models;
using BalatroSeedOracle.Services.Export;
using Motely.DuckDB;

namespace BalatroSeedOracle.Desktop.Services;

/// <summary>
/// Desktop implementation of IResultsDatabaseExporter using Motely.DB.
/// BSO does not reference DuckDB directly; all database export goes through Motely.
/// </summary>
public sealed class ResultsDatabaseExporter : IResultsDatabaseExporter
{
    public bool IsAvailable => true;

    public Task ExportToAsync(
        string path,
        IReadOnlyList<SearchResult> results,
        IReadOnlyList<string> columnNames)
    {
        var rows = new List<(string seed, int score, IReadOnlyList<object?>? columnValues)>();
        foreach (var r in results)
        {
            IReadOnlyList<object?>? vals = null;
            if (r.Scores != null && r.Scores.Length > 0)
                vals = r.Scores.Cast<object?>().ToArray();
            rows.Add((r.Seed ?? "", r.TotalScore, vals));
        }
        ResultsExportHelper.ExportResultsTo(path, columnNames ?? new List<string>(), rows);
        return Task.CompletedTask;
    }
}

using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Threading.Tasks;
using BalatroSeedOracle.Models;
using BalatroSeedOracle.Services.Export;

namespace BalatroSeedOracle.Desktop.Services;

public sealed class ResultsDatabaseExporter : IResultsDatabaseExporter
{
    public bool IsAvailable => true;

    public Task ExportToAsync(
        string path,
        IReadOnlyList<SearchResult> results,
        IReadOnlyList<string> columnNames
    )
    {
        using var w = new StreamWriter(path);
        var header = new List<string> { "seed", "score" };
        if (columnNames != null) header.AddRange(columnNames);
        w.WriteLine(string.Join(",", header));
        foreach (var r in results)
        {
            var cells = new List<string> { r.Seed ?? "", r.TotalScore.ToString(CultureInfo.InvariantCulture) };
            if (r.Scores != null)
                foreach (var s in r.Scores)
                    cells.Add(s.ToString(CultureInfo.InvariantCulture));
            w.WriteLine(string.Join(",", cells));
        }
        return Task.CompletedTask;
    }
}

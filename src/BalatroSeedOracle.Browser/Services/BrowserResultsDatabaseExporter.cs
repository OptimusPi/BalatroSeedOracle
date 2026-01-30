using System.Collections.Generic;
using System.Threading.Tasks;
using BalatroSeedOracle.Models;
using BalatroSeedOracle.Services.Export;

namespace BalatroSeedOracle.Browser.Services;

/// <summary>
/// Browser stub: database export (.db / .ducklake) is available on Desktop only.
/// </summary>
public sealed class BrowserResultsDatabaseExporter : IResultsDatabaseExporter
{
    public bool IsAvailable => false;

    public Task ExportToAsync(
        string path,
        IReadOnlyList<SearchResult> results,
        IReadOnlyList<string> columnNames)
    {
        throw new System.NotImplementedException(
            "Export to search results file (.db / .ducklake) is available on Desktop only. Use CSV export in the browser.");
    }
}

using System.Collections.Generic;
using System.Threading.Tasks;
using BalatroSeedOracle.Services.Export;

namespace BalatroSeedOracle.Browser.Services;

/// <summary>
/// Browser stub for Parquet export. Not implemented.
/// </summary>
internal sealed class BrowserParquetExporter : IParquetExporter
{
    public bool IsAvailable => false;

    public Task ExportAsync(
        string filePath,
        IReadOnlyList<string> headers,
        IReadOnlyList<IReadOnlyList<object?>> rows
    )
    {
        throw new System.NotImplementedException("Parquet export not available in browser.");
    }
}

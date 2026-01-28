using System.Collections.Generic;
using System.Threading.Tasks;

namespace BalatroSeedOracle.Services.Export;

/// <summary>
/// Platform abstraction for Parquet export functionality.
/// Desktop uses Parquet.Net, Browser uses JS interop or DuckDB-WASM.
/// </summary>
public interface IParquetExporter
{
    /// <summary>
    /// Whether Parquet export is available on this platform
    /// </summary>
    bool IsAvailable { get; }

    /// <summary>
    /// Export data to a Parquet file
    /// </summary>
    /// <param name="filePath">Path to save the Parquet file</param>
    /// <param name="headers">Column headers</param>
    /// <param name="rows">Data rows (each row is a list of cell values)</param>
    Task ExportAsync(
        string filePath,
        IReadOnlyList<string> headers,
        IReadOnlyList<IReadOnlyList<object?>> rows
    );
}

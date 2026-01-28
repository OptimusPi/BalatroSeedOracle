using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using BalatroSeedOracle.Services.Export;

namespace BalatroSeedOracle.Browser.Services;

/// <summary>
/// Browser implementation of IParquetExporter.
/// Uses DuckDB-WASM to export to Parquet format.
/// </summary>
public class BrowserParquetExporter : IParquetExporter
{
    public bool IsAvailable => true; // DuckDB-WASM supports Parquet export

    public async Task ExportAsync(
        string filePath,
        IReadOnlyList<string> headers,
        IReadOnlyList<IReadOnlyList<object?>> rows
    )
    {
        // TODO: Implement Parquet export via DuckDB-WASM
        // DuckDB-WASM can write Parquet files using COPY TO 'file.parquet' (FORMAT PARQUET)
        // For now, fallback to CSV or implement via JS interop
        
        // Not implemented yet - users can export to CSV or DuckDB instead
        await Task.CompletedTask;
        throw new NotImplementedException("Parquet export not yet implemented for browser. Use CSV or DuckDB export instead.");
    }
}

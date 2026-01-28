using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using BalatroSeedOracle.Services.Export;

namespace BalatroSeedOracle.Desktop.Services;

/// <summary>
/// Desktop implementation of IParquetExporter.
/// TODO: Implement using DuckDB's COPY TO 'file.parquet' when needed.
/// </summary>
public class ParquetExporter : IParquetExporter
{
    public bool IsAvailable => false; // Not implemented yet - use DuckDB export instead

    public Task ExportAsync(
        string filePath,
        IReadOnlyList<string> headers,
        IReadOnlyList<IReadOnlyList<object?>> rows
    )
    {
        throw new NotImplementedException("Parquet export not implemented. Use DuckDB COPY TO for Parquet export.");
    }
}

using System.Collections.Generic;
using System.Threading.Tasks;
using BalatroSeedOracle.Models;

namespace BalatroSeedOracle.Services.Export;

/// <summary>
/// Interface for exporting search results to a database/file format.
/// Desktop implements this using Motely.DB; WASM provides a no-op.
/// </summary>
public interface IResultsDatabaseExporter
{
    bool IsAvailable { get; }

    Task ExportToAsync(
        string path,
        IReadOnlyList<SearchResult> results,
        IReadOnlyList<string> columnNames);
}

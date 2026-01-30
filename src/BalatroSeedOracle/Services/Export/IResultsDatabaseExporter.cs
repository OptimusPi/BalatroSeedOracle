using System.Collections.Generic;
using System.Threading.Tasks;
using BalatroSeedOracle.Models;

namespace BalatroSeedOracle.Services.Export;

/// <summary>
/// Platform abstraction for exporting search results to a database file (.db or .ducklake).
/// Desktop uses Motely.DB; Browser uses stub (export available on Desktop only).
/// </summary>
public interface IResultsDatabaseExporter
{
    /// <summary>
    /// Whether database export is available on this platform (e.g. Desktop yes, Browser no).
    /// </summary>
    bool IsAvailable { get; }

    /// <summary>
    /// Export results to the given path. Format is inferred from extension: .db or .ducklake.
    /// </summary>
    /// <param name="path">File path ending in .db or .ducklake.</param>
    /// <param name="results">Search results to export.</param>
    /// <param name="columnNames">Names of tally/extra columns (e.g. from first result's Labels).</param>
    Task ExportToAsync(string path, IReadOnlyList<SearchResult> results, IReadOnlyList<string> columnNames);
}

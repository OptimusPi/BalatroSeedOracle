using System.Collections.Generic;
using System.Threading.Tasks;

namespace BalatroSeedOracle.Services.Export;

/// <summary>
/// Platform abstraction for Excel export functionality.
/// Desktop uses ClosedXML, Browser uses JS interop (SheetJS or similar).
/// </summary>
public interface IExcelExporter
{
    /// <summary>
    /// Whether Excel export is available on this platform
    /// </summary>
    bool IsAvailable { get; }

    /// <summary>
    /// Export data to an Excel file
    /// </summary>
    /// <param name="filePath">Path to save the Excel file</param>
    /// <param name="sheetName">Name of the worksheet</param>
    /// <param name="headers">Column headers</param>
    /// <param name="rows">Data rows (each row is a list of cell values)</param>
    Task ExportAsync(
        string filePath,
        string sheetName,
        IReadOnlyList<string> headers,
        IReadOnlyList<IReadOnlyList<object?>> rows
    );
}

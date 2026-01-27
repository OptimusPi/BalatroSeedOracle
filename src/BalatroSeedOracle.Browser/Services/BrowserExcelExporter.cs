using System.Collections.Generic;
using System.Threading.Tasks;
using BalatroSeedOracle.Services.Export;

namespace BalatroSeedOracle.Browser.Services;

/// <summary>
/// Browser implementation of IExcelExporter.
/// Currently a stub - Excel export not yet implemented for browser.
/// TODO: Implement using SheetJS via JS interop for browser Excel export.
/// </summary>
public class BrowserExcelExporter : IExcelExporter
{
    public bool IsAvailable => false;

    public Task ExportAsync(
        string filePath,
        string sheetName,
        IReadOnlyList<string> headers,
        IReadOnlyList<IReadOnlyList<object?>> rows
    )
    {
        // Not implemented for browser yet - users can export to CSV instead
        return Task.CompletedTask;
    }
}

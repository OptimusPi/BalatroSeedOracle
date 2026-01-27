using System.Collections.Generic;
using System.Threading.Tasks;
using BalatroSeedOracle.Services.Export;
using ClosedXML.Excel;

namespace BalatroSeedOracle.Desktop.Services;

/// <summary>
/// Desktop implementation of IExcelExporter using ClosedXML
/// </summary>
public class ClosedXmlExcelExporter : IExcelExporter
{
    public bool IsAvailable => true;

    public Task ExportAsync(
        string filePath,
        string sheetName,
        IReadOnlyList<string> headers,
        IReadOnlyList<IReadOnlyList<object?>> rows
    )
    {
        using var workbook = new XLWorkbook();
        var worksheet = workbook.Worksheets.Add(sheetName);

        // Headers
        for (int i = 0; i < headers.Count; i++)
        {
            worksheet.Cell(1, i + 1).Value = headers[i];
        }

        // Data rows
        for (int rowIndex = 0; rowIndex < rows.Count; rowIndex++)
        {
            var row = rows[rowIndex];
            for (int colIndex = 0; colIndex < row.Count; colIndex++)
            {
                var value = row[colIndex];
                var cell = worksheet.Cell(rowIndex + 2, colIndex + 1);

                if (value is int intVal)
                    cell.Value = intVal;
                else if (value is long longVal)
                    cell.Value = longVal;
                else if (value is double doubleVal)
                    cell.Value = doubleVal;
                else if (value is decimal decVal)
                    cell.Value = decVal;
                else if (value is bool boolVal)
                    cell.Value = boolVal;
                else
                    cell.Value = value?.ToString() ?? "";
            }
        }

        workbook.SaveAs(filePath);
        return Task.CompletedTask;
    }
}

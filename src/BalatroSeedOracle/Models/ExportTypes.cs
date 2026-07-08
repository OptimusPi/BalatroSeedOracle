using System;
using System.Collections.Generic;

namespace BalatroSeedOracle.Models;

/// <summary>
/// DataGrid result item for export
/// </summary>
public class DataGridResultItem
{
    public string? Seed { get; set; }
    public long Score { get; set; }
    public Dictionary<string, object>? Tallies { get; set; }
}

/// <summary>
/// Named export shape for DbListExportService JSON export
/// </summary>
public class SearchResultExport
{
    public DateTime ExportDate { get; set; }
    public int TotalResults { get; set; }
    public List<SearchResultExportRow> Results { get; set; } = new();
}

public class SearchResultExportRow
{
    public string? Seed { get; set; }
    public int TotalScore { get; set; }
    public int[]? Scores { get; set; }
    public string[]? Labels { get; set; }
    public string? ScoresDisplay { get; set; }
}

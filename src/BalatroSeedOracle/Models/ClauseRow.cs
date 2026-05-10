using System.Collections.Generic;
using Avalonia.Media;

namespace BalatroSeedOracle.Models;

/// <summary>
/// Represents a single clause row for filter validation display.
/// Supports nested clauses for OR/AND operators.
/// </summary>
public class ClauseRow
{
    public string ClauseType { get; set; } = "";
    public string DisplayText { get; set; } = "";
    public string ItemKey { get; set; } = "";
    public bool IsExpanded { get; set; }
    public int NestingLevel { get; set; }
    public List<ClauseRow> Children { get; set; } = new();

    // Display
    public IImage? IconPath { get; set; }
    public string? EditionBadge { get; set; }
    public string AnteRange { get; set; } = "";
    public int? MinCount { get; set; }
    public int ScoreValue { get; set; }
}

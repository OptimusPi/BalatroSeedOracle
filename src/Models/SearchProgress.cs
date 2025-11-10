using System;
using BalatroSeedOracle.Services;

namespace BalatroSeedOracle.Models;

/// <summary>
/// Progress information for ongoing searches
/// </summary>
public class SearchProgress
{
    public double PercentComplete { get; set; }
    public ulong SeedsSearched { get; set; }
    public double SeedsPerMillisecond { get; set; }
    public string Message { get; set; } = string.Empty;
    public bool IsComplete { get; set; }
    public bool HasError { get; set; }
    public int ResultsFound { get; set; }
    public SearchResult? NewResult { get; set; }

    /// <summary>
    /// Estimated time remaining for search completion (null if indeterminate)
    /// </summary>
    public TimeSpan? EstimatedTimeRemaining { get; set; }
}

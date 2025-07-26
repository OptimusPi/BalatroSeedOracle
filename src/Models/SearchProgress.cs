using Oracle.Services;

namespace Oracle.Models;

/// <summary>
/// Progress information for ongoing searches
/// </summary>
public class SearchProgress
{
    public double PercentComplete { get; set; }
    public long SeedsSearched { get; set; }
    public double SeedsPerSecond { get; set; }
    public string Message { get; set; } = string.Empty;
    public bool IsComplete { get; set; }
    public bool HasError { get; set; }
    public int ResultsFound { get; set; }
    public SearchResult? NewResult { get; set; }
}
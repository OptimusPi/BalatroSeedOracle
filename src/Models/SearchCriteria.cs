using System;

namespace Oracle.Models;

/// <summary>
/// Minimal search criteria for Motely searches
/// </summary>
public class SearchCriteria
{
    public string? ConfigPath { get; set; }
    public int ThreadCount { get; set; } = Environment.ProcessorCount;
    public long MaxSeeds { get; set; } = long.MaxValue; // Search all seeds by default
    public int MinScore { get; set; } = 0;
    public int BatchSize { get; set; } = 4; // Default batch size
    public string? Deck { get; set; } = "Red Deck";
    public string? Stake { get; set; } = "White Stake";
    public bool EnableDebugOutput { get; set; } = false;
}
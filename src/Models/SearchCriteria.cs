using System;

namespace Oracle.Models;

/// <summary>
/// Minimal search criteria for Motely searches
/// </summary>
public class SearchCriteria
{
    public string? ConfigPath { get; set; }
    public int ThreadCount { get; set; } = Environment.ProcessorCount;
    public long MaxSeeds { get; set; } = 2251875390625; // Search all seeds by default
    public int MinScore { get; set; } = 0;
    public int BatchSize { get; set; } = 4; // Default batch size
    public int StartBatch { get; set; } = 0;
    public int EndBatch { get; set; } = -1;
    public string? Deck { get; set; } = "Red";
    public string? Stake { get; set; } = "White";
    public bool EnableDebugOutput { get; set; } = false;
}

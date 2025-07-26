using System;

namespace Oracle.Models;

/// <summary>
/// Minimal search criteria for Motely searches
/// </summary>
public class SearchCriteria
{
    public string? ConfigPath { get; set; }
    public int ThreadCount { get; set; } = Environment.ProcessorCount;
    public long MaxSeeds { get; set; } = 10_000_000;
    public int MinScore { get; set; } = 0;
    public int BatchSize { get; set; } = 4; // Default batch size
}
using System;

namespace BalatroSeedOracle.Models;

/// <summary>
/// Minimal search criteria for Motely searches
/// </summary>
public class SearchCriteria
{
    public string? ConfigPath { get; set; }
    public int ThreadCount { get; set; } = Environment.ProcessorCount;
    public int MinScore { get; set; } = 0;
    public int BatchSize { get; set; } = 2; // Default batch size to 2 digits, 35^2 seeds.
    public ulong StartBatch { get; set; } = 0;
    public ulong EndBatch { get; set; } = ulong.MaxValue;
    public string? Deck { get; set; } = "Red";
    public string? Stake { get; set; } = "White";
    public bool EnableDebugOutput { get; set; } = false;
    public string? DebugSeed { get; set; }
}

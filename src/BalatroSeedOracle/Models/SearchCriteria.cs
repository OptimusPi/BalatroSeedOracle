using System;

namespace BalatroSeedOracle.Models;

/// <summary>
/// Minimal search criteria for Motely searches
/// </summary>
public class SearchCriteria
{
    public string? ConfigPath { get; set; }
    public int ThreadCount { get; set; } = Math.Max(Environment.ProcessorCount / 2, 1);
    public int MinScore { get; set; } = 0;

    // Tip: this is the seed digits (BatchCharacterCount in Motely)
    public int BatchSize { get; set; } = 3; // Default batch size to 3 digits, 35^3 seeds per batch.
    public ulong StartBatch { get; set; } = 0;

    // Balatro has 8-character base-35 seeds: 35^8 = 2,251,875,390,625 total seeds (~2.25 trillion)
    // This is the actual maximum, not infinite
    public ulong EndBatch { get; set; } = 2251875390625;
    public string? Deck { get; set; } = "Red";
    public string? Stake { get; set; } = "White";

    // Similar to MotelyCLI --wordlist parameter (e.g., "leet" for the 1337-Speak Wordlist I made..)
    // e.g. `--wordlist leet` would use this file: `./WordLists/sick.txt`
    public string? WordList { get; set; }

    // similar to MotelyCLI --debug parameter
    public bool EnableDebugOutput { get; set; } = false;

    // similar to MotelyCLI --seed parameter
    public string? DebugSeed { get; set; }

    // Maximum number of results to find before stopping (for quick tests)
    public int MaxResults { get; set; } = 0; // 0 = unlimited
}

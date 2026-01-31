using System;

namespace BalatroSeedOracle.Models
{
    /// <summary>
    /// Configuration for search parameters
    /// </summary>
    public class SearchConfiguration
    {
        public string FilterPath { get; set; } = string.Empty;
        public string Deck { get; set; } = "Red";
        public string Stake { get; set; } = "White";
        public int ThreadCount { get; set; } = Environment.ProcessorCount;
        public int BatchSize { get; set; } = 3;
        public int TimeoutSeconds { get; set; } = 300;
        public int MinScore { get; set; } = 0;
        public ulong StartBatch { get; set; } = 0;
        public ulong EndBatch { get; set; } = 0;
        public bool DebugMode { get; set; } = false;
        public string? DebugSeed { get; set; }
    }
}

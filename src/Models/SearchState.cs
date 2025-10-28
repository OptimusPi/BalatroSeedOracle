using System;

namespace BalatroSeedOracle.Models
{
    /// <summary>
    /// Represents the persisted state of a search session for resume functionality
    /// </summary>
    public class SearchState
    {
        public int Id { get; set; }
        public int DeckIndex { get; set; }
        public int StakeIndex { get; set; }
        public int BatchSize { get; set; }
        public int LastCompletedBatch { get; set; }
        public int SearchMode { get; set; }
        public string? WordListName { get; set; }
        public DateTime UpdatedAt { get; set; }
    }
}

using System;
using System.Collections.Generic;

namespace BalatroSeedOracle.Models
{
    /// <summary>
    /// User profile configuration for saving author information and widget preferences
    /// </summary>
    public class UserProfile
    {
        /// <summary>
        /// The author name (defaults to "Jimbo")
        /// </summary>
        public string AuthorName { get; set; } = "Jimbo";

        /// <summary>
        /// SearchWidget removed - using desktop icons now
        /// </summary>
        // public List<SearchWidgetConfig> ActiveWidgets { get; set; } = new();

        /// <summary>
        /// Background theme preference
        /// </summary>
        public string? BackgroundTheme { get; set; }

        /// <summary>
        /// Whether background animation is enabled
        /// </summary>
        public bool AnimationEnabled { get; set; } = true;

        /// <summary>
        /// Last search state for resuming interrupted searches
        /// </summary>
        public SearchResumeState? LastSearchState { get; set; }
    }

    /// <summary>
    /// Represents the state of a search that can be resumed
    /// </summary>
    public class SearchResumeState
    {
        /// <summary>
        /// Path to the filter config file (always required)
        /// </summary>
        public string? ConfigPath { get; set; }
        
        /// <summary>
        /// The last completed batch index
        /// </summary>
        public ulong LastCompletedBatch { get; set; }
        
        /// <summary>
        /// The end batch index
        /// </summary>
        public ulong EndBatch { get; set; }
        
        /// <summary>
        /// The batch size
        /// </summary>
        public int BatchSize { get; set; }
        
        /// <summary>
        /// Thread count
        /// </summary>
        public int ThreadCount { get; set; }
        
        /// <summary>
        /// Minimum score filter
        /// </summary>
        public int MinScore { get; set; }
        
        /// <summary>
        /// Selected deck
        /// </summary>
        public string? Deck { get; set; }
        
        /// <summary>
        /// Selected stake
        /// </summary>
        public string? Stake { get; set; }
        
        /// <summary>
        /// When the search was last active
        /// </summary>
        public DateTime LastActiveTime { get; set; }
        
        /// <summary>
        /// Total batches in the search
        /// </summary>
        public ulong TotalBatches { get; set; }
    }

    // SearchWidgetConfig removed - using desktop icons now
}

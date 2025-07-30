using System.Collections.Generic;

namespace Oracle.Models
{
    /// <summary>
    /// User profile configuration for saving author information and widget preferences
    /// </summary>
    public class UserProfile
    {
        /// <summary>
        /// The author name (defaults to "Jimbo" for fun)
        /// </summary>
        public string AuthorName { get; set; } = "Jimbo";
        
        /// <summary>
        /// List of active search widgets with their configurations
        /// </summary>
        public List<SearchWidgetConfig> ActiveWidgets { get; set; } = new();
        
        /// <summary>
        /// Background theme preference
        /// </summary>
        public string? BackgroundTheme { get; set; }
        
        /// <summary>
        /// Whether background animation is enabled
        /// </summary>
        public bool AnimationEnabled { get; set; } = true;
        
        /// <summary>
        /// Volume level (0-3)
        /// </summary>
        public int VolumeLevel { get; set; } = 2;
        
        /// <summary>
        /// Whether music is enabled
        /// </summary>
        public bool MusicEnabled { get; set; } = true;
    }
    
    /// <summary>
    /// Configuration for a single search widget
    /// </summary>
    public class SearchWidgetConfig
    {
        /// <summary>
        /// Path to the filter configuration file
        /// </summary>
        public string? FilterConfigPath { get; set; }
        
        /// <summary>
        /// Widget position X
        /// </summary>
        public double X { get; set; }
        
        /// <summary>
        /// Widget position Y
        /// </summary>
        public double Y { get; set; }
        
        /// <summary>
        /// Whether the widget is minimized
        /// </summary>
        public bool IsMinimized { get; set; }
        
        /// <summary>
        /// Thread count setting
        /// </summary>
        public int ThreadCount { get; set; } = 4;
        
        /// <summary>
        /// Minimum score setting
        /// </summary>
        public int MinScore { get; set; } = 1;
        
        /// <summary>
        /// Batch size setting
        /// </summary>
        public int BatchSize { get; set; } = 4;
    }
}
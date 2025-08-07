using System.Collections.Generic;

namespace Oracle.Models
{
    /// <summary>
    /// User profile configuration for saving author information and widget preferences
    /// </summary>
    public class UserProfile
    {
        /// <summary>
        /// The author name (defaults to "pifreak")
        /// </summary>
        public string AuthorName { get; set; } = "pifreak";

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
        /// Volume level (0-3)
        /// </summary>
        public int VolumeLevel { get; set; } = 2;

        /// <summary>
        /// Whether music is enabled
        /// </summary>
        public bool MusicEnabled { get; set; } = true;
    }

    // SearchWidgetConfig removed - using desktop icons now
}
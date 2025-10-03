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

        /// <summary>
        /// Feature flags for experimental features
        /// </summary>
        public FeatureFlags Features { get; set; } = new();

        /// <summary>
        /// Vibe Out visualizer settings
        /// </summary>
        public VibeOutSettings VibeOutSettings { get; set; } = new();
    }

    /// <summary>
    /// Feature flags for enabling experimental features
    /// </summary>
    public class FeatureFlags
    {

        /// <summary>
        /// Use .NET 9 features when available
        /// </summary>
        public bool UseNet9Features { get; set; } = false;
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

    /// <summary>
    /// Vibe Out visualizer settings for music-reactive shader parameters
    /// </summary>
    public class VibeOutSettings
    {
        /// <summary>
        /// Audio source for ShadowFlicker effect (0=None, 1=Drums, 2=Bass, 3=Chords, 4=Melody)
        /// </summary>
        public int ShadowFlickerSource { get; set; } = 1; // Drums

        /// <summary>
        /// Audio source for Spin effect
        /// </summary>
        public int SpinSource { get; set; } = 2; // Bass

        /// <summary>
        /// Audio source for Twirl effect
        /// </summary>
        public int TwirlSource { get; set; } = 3; // Chords

        /// <summary>
        /// Audio source for ZoomThump effect
        /// </summary>
        public int ZoomThumpSource { get; set; } = 4; // Melody

        /// <summary>
        /// Audio source for Color Saturation effect
        /// </summary>
        public int ColorSaturationSource { get; set; } = 4; // Melody

        /// <summary>
        /// Audio source for Beat Pulse effect
        /// </summary>
        public int BeatPulseSource { get; set; } = 0; // None (disabled by default)

        /// <summary>
        /// Intensity multipliers for shader effects (0-100% sliders for user control)
        /// </summary>
        public float ShadowFlickerIntensity { get; set; } = 50f;
        public float SpinIntensity { get; set; } = 50f;
        public float TwirlIntensity { get; set; } = 50f;
        public float ZoomThumpIntensity { get; set; } = 50f;
        public float ColorSaturationIntensity { get; set; } = 50f;
        public float BeatPulseIntensity { get; set; } = 50f;

        /// <summary>
        /// Selected theme index (0=Default, 1=Wave Rider, 2=Inferno, etc.)
        /// </summary>
        public int ThemeIndex { get; set; } = 0;

        /// <summary>
        /// Music reactivity intensity (0-2, default 0.0 = OFF for non-vomit-inducing experience)
        /// </summary>
        public float AudioIntensity { get; set; } = 0.0f;

        /// <summary>
        /// Mouse parallax strength (0-2, default 0.29)
        /// </summary>
        public float ParallaxStrength { get; set; } = 0.29f;

        /// <summary>
        /// Base animation speed (0-3, default 1.0)
        /// </summary>
        public float TimeSpeed { get; set; } = 1.0f;

        /// <summary>
        /// Custom main color index (0-7 for colors, 8 for None/Theme Default)
        /// </summary>
        public int MainColor { get; set; } = 8; // None (use theme default)

        /// <summary>
        /// Custom accent color index (0-7 for colors, 8 for None/Theme Default)
        /// </summary>
        public int AccentColor { get; set; } = 8; // None (use theme default)

        /// <summary>
        /// Enable seed found audio event trigger
        /// </summary>
        public bool SeedFoundTrigger { get; set; } = true;

        /// <summary>
        /// Enable high score seed audio event trigger
        /// </summary>
        public bool HighScoreSeedTrigger { get; set; } = true;

        /// <summary>
        /// Enable search complete audio event trigger
        /// </summary>
        public bool SearchCompleteTrigger { get; set; } = true;

        /// <summary>
        /// Audio source for seed found trigger (0=None, 1=Drums, 2=Bass, 3=Chords, 4=Melody)
        /// </summary>
        public int SeedFoundAudioSource { get; set; } = 1; // Default to Drums

        /// <summary>
        /// Audio source for high score trigger (0=None, 1=Drums, 2=Bass, 3=Chords, 4=Melody)
        /// </summary>
        public int HighScoreAudioSource { get; set; } = 4; // Default to Melody

        /// <summary>
        /// Audio source for search complete trigger (0=None, 1=Drums, 2=Bass, 3=Chords, 4=Melody)
        /// </summary>
        public int SearchCompleteAudioSource { get; set; } = 3; // Default to Chords
    }
}

using System.Collections.Generic;

namespace BalatroSeedOracle.Models
{
    /// <summary>
    /// Base interface for all music-reactive breakpoint types.
    /// Breakpoints define custom frequency ranges or melodic patterns that trigger visual effects.
    /// </summary>
    public interface IMusicReactiveBreakpoint
    {
        string Name { get; set; }
        bool Enabled { get; set; }
    }

    /// <summary>
    /// Frequency-based breakpoint: Triggers when audio energy in a specific Hz range exceeds threshold.
    /// Example: "Bass Drop" (20-80Hz), "High Treble" (8000-20000Hz)
    /// </summary>
    public class FrequencyBreakpoint : IMusicReactiveBreakpoint
    {
        public string Name { get; set; } = "Custom Band";
        public bool Enabled { get; set; } = true;

        /// <summary>Start frequency in Hz (20-20000)</summary>
        public float StartHz { get; set; } = 20f;

        /// <summary>End frequency in Hz (20-20000)</summary>
        public float EndHz { get; set; } = 250f;

        /// <summary>Use logarithmic frequency scaling (better for musical perception)</summary>
        public bool UseLogarithmic { get; set; } = false;

        /// <summary>Intensity threshold to trigger effect (0-1)</summary>
        public float Threshold { get; set; } = 0.5f;

        // Keep backwards compatibility properties
        public float FrequencyHz { get; set; }
        public float AmplitudeThreshold { get; set; } = 0.5f;
        public string EffectName { get; set; } = string.Empty;
        public float EffectIntensity { get; set; } = 1.0f;
        public int DurationMs { get; set; } = 500;
    }

    /// <summary>
    /// Melodic/harmonic breakpoint: Triggers on chord detection or sustained notes.
    /// Example: "Chord Detection" (multiple simultaneous frequencies)
    /// </summary>
    public class MelodicBreakpoint : IMusicReactiveBreakpoint
    {
        public string Name { get; set; } = "Melodic Event";
        public bool Enabled { get; set; } = true;

        /// <summary>Minimum frequency for melodic analysis (Hz)</summary>
        public float MinFrequency { get; set; } = 200f;

        /// <summary>Maximum frequency for melodic analysis (Hz)</summary>
        public float MaxFrequency { get; set; } = 2000f;

        /// <summary>Minimum number of simultaneous notes to detect (1=single note, 2+=chord)</summary>
        public int MinNoteCount { get; set; } = 2;

        /// <summary>Minimum sustain duration in milliseconds</summary>
        public float SustainMs { get; set; } = 100f;

        // Keep backwards compatibility properties
        public float TargetNoteHz { get; set; }
        public float FrequencyTolerance { get; set; } = 5.0f;
        public int MinDurationMs { get; set; } = 100;
        public string EffectName { get; set; } = string.Empty;
        public float EffectIntensity { get; set; } = 1.0f;
    }

    /// <summary>
    /// Defines the range for a shader parameter (used for audio mapping).
    /// Example: ZoomPunch ranges from 0 (min) to 10 (max), defaults to 0, smoothing 0.8
    /// </summary>
    public class ParameterRange
    {
        public float Min { get; set; }
        public float Max { get; set; }
        public float DefaultValue { get; set; }

        /// <summary>Smoothing factor for transitions (0=instant, 1=never changes)</summary>
        public float Smoothing { get; set; } = 0.8f;

        // Keep backwards compatibility
        public string ParameterName { get; set; } = string.Empty;
        public float MinValue
        {
            get => Min;
            set => Min = value;
        }
        public float MaxValue
        {
            get => Max;
            set => Max = value;
        }
    }

    /// <summary>
    /// Maps audio sources to shader effects with advanced control.
    /// Example: "Spin" effect can react to Drums1 + Bass2 with 1.5x intensity
    /// </summary>
    public class EffectMapping
    {
        /// <summary>List of audio source names (e.g., "Drums1", "Bass2")</summary>
        public List<string> AudioSources { get; set; } = new();

        /// <summary>Effect intensity multiplier (0-2, where 1.0 = normal)</summary>
        public float Intensity { get; set; } = 1.0f;

        /// <summary>Enable/disable this effect mapping</summary>
        public bool Enabled { get; set; } = true;

        // Keep backwards compatibility
        public string EffectName { get; set; } = string.Empty;
        public int AudioSource { get; set; }
        public bool IsEnabled
        {
            get => Enabled;
            set => Enabled = value;
        }
        public Dictionary<string, float>? CustomParameters { get; set; }
    }
}

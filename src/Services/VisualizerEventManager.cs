using System;
using BalatroSeedOracle.Helpers;

namespace BalatroSeedOracle.Services
{
    /// <summary>
    /// Event arguments for beat detection events
    /// </summary>
    public class BeatDetectedEventArgs : EventArgs
    {
        /// <summary>
        /// Beat strength/intensity (0.0-1.0)
        /// </summary>
        public float Intensity { get; set; }

        /// <summary>
        /// Audio source that triggered the beat (1=Drums, 2=Bass, 3=Chords, 4=Melody)
        /// </summary>
        public int AudioSource { get; set; }

        /// <summary>
        /// Timestamp when the beat was detected
        /// </summary>
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    }

    /// <summary>
    /// Event arguments for drop/impact events in music
    /// </summary>
    public class DropDetectedEventArgs : EventArgs
    {
        /// <summary>
        /// Drop intensity (0.0-1.0)
        /// </summary>
        public float Intensity { get; set; }

        /// <summary>
        /// Frequency band that triggered the drop (Hz)
        /// </summary>
        public float FrequencyHz { get; set; }

        /// <summary>
        /// Timestamp when the drop was detected
        /// </summary>
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    }

    /// <summary>
    /// Event arguments for frequency breakpoint hits
    /// </summary>
    public class FrequencyBreakpointEventArgs : EventArgs
    {
        /// <summary>
        /// ID of the breakpoint that was triggered
        /// </summary>
        public string BreakpointId { get; set; } = string.Empty;

        /// <summary>
        /// Name of the breakpoint
        /// </summary>
        public string BreakpointName { get; set; } = string.Empty;

        /// <summary>
        /// Current amplitude value that triggered the breakpoint
        /// </summary>
        public float Amplitude { get; set; }

        /// <summary>
        /// Effect name to apply
        /// </summary>
        public string EffectName { get; set; } = string.Empty;

        /// <summary>
        /// Effect intensity
        /// </summary>
        public float EffectIntensity { get; set; }

        /// <summary>
        /// Effect duration in milliseconds
        /// </summary>
        public int DurationMs { get; set; }

        /// <summary>
        /// Timestamp when the breakpoint was hit
        /// </summary>
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    }

    /// <summary>
    /// Event arguments for seed found events
    /// </summary>
    public class SeedFoundEventArgs : EventArgs
    {
        /// <summary>
        /// The seed that was found
        /// </summary>
        public string Seed { get; set; } = string.Empty;

        /// <summary>
        /// Score associated with the seed
        /// </summary>
        public long Score { get; set; }

        /// <summary>
        /// Timestamp when the seed was found
        /// </summary>
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    }

    /// <summary>
    /// Event arguments for high score events
    /// </summary>
    public class HighScoreEventArgs : EventArgs
    {
        /// <summary>
        /// The seed that achieved the high score
        /// </summary>
        public string Seed { get; set; } = string.Empty;

        /// <summary>
        /// The high score value
        /// </summary>
        public long Score { get; set; }

        /// <summary>
        /// Whether this is a new personal record
        /// </summary>
        public bool IsPersonalRecord { get; set; }

        /// <summary>
        /// Timestamp when the high score was achieved
        /// </summary>
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    }

    /// <summary>
    /// Event arguments for search complete events
    /// </summary>
    public class SearchCompleteEventArgs : EventArgs
    {
        /// <summary>
        /// Total number of seeds searched
        /// </summary>
        public int TotalSearched { get; set; }

        /// <summary>
        /// Number of seeds that matched criteria
        /// </summary>
        public int MatchesFound { get; set; }

        /// <summary>
        /// Search duration
        /// </summary>
        public TimeSpan Duration { get; set; }

        /// <summary>
        /// Timestamp when the search completed
        /// </summary>
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    }

    /// <summary>
    /// Manages visualizer events for both music-reactive and game-based triggers
    /// </summary>
    public class VisualizerEventManager
    {
        private static VisualizerEventManager? _instance;
        public static VisualizerEventManager Instance => _instance ??= new VisualizerEventManager();

        #region Music-Based Events

        /// <summary>
        /// Triggered when a beat is detected in the audio
        /// </summary>
        public event EventHandler<BeatDetectedEventArgs>? BeatDetected;

        /// <summary>
        /// Triggered when a drop/impact is detected in the audio
        /// </summary>
        public event EventHandler<DropDetectedEventArgs>? DropDetected;

        /// <summary>
        /// Triggered when a frequency breakpoint threshold is hit
        /// </summary>
        public event EventHandler<FrequencyBreakpointEventArgs>? FrequencyBreakpointHit;

        #endregion

        #region Game-Based Events

        /// <summary>
        /// Triggered when a seed matching criteria is found
        /// </summary>
        public event EventHandler<SeedFoundEventArgs>? SeedFound;

        /// <summary>
        /// Triggered when a high score seed is found
        /// </summary>
        public event EventHandler<HighScoreEventArgs>? HighScore;

        /// <summary>
        /// Triggered when a search operation completes
        /// </summary>
        public event EventHandler<SearchCompleteEventArgs>? SearchComplete;

        #endregion

        #region Trigger Methods

        /// <summary>
        /// Triggers a beat detected event
        /// </summary>
        /// <param name="intensity">Beat intensity (0.0-1.0)</param>
        /// <param name="audioSource">Audio source index (1=Drums, 2=Bass, 3=Chords, 4=Melody)</param>
        public void TriggerBeatDetected(float intensity, int audioSource)
        {
            DebugLogger.Log("VisualizerEventManager", $"Beat detected: intensity={intensity:F2}, source={audioSource}");
            BeatDetected?.Invoke(this, new BeatDetectedEventArgs
            {
                Intensity = intensity,
                AudioSource = audioSource
            });
        }

        /// <summary>
        /// Triggers a drop detected event
        /// </summary>
        /// <param name="intensity">Drop intensity (0.0-1.0)</param>
        /// <param name="frequencyHz">Frequency in Hz</param>
        public void TriggerDropDetected(float intensity, float frequencyHz)
        {
            DebugLogger.Log("VisualizerEventManager", $"Drop detected: intensity={intensity:F2}, frequency={frequencyHz}Hz");
            DropDetected?.Invoke(this, new DropDetectedEventArgs
            {
                Intensity = intensity,
                FrequencyHz = frequencyHz
            });
        }

        /// <summary>
        /// Triggers a frequency breakpoint hit event
        /// </summary>
        /// <param name="breakpointId">ID of the breakpoint</param>
        /// <param name="breakpointName">Name of the breakpoint</param>
        /// <param name="amplitude">Current amplitude</param>
        /// <param name="effectName">Effect to trigger</param>
        /// <param name="effectIntensity">Effect intensity</param>
        /// <param name="durationMs">Effect duration in milliseconds</param>
        public void TriggerFrequencyBreakpoint(string breakpointId, string breakpointName, float amplitude,
            string effectName, float effectIntensity, int durationMs)
        {
            DebugLogger.Log("VisualizerEventManager",
                $"Frequency breakpoint hit: {breakpointName} (amplitude={amplitude:F2}, effect={effectName})");
            FrequencyBreakpointHit?.Invoke(this, new FrequencyBreakpointEventArgs
            {
                BreakpointId = breakpointId,
                BreakpointName = breakpointName,
                Amplitude = amplitude,
                EffectName = effectName,
                EffectIntensity = effectIntensity,
                DurationMs = durationMs
            });
        }

        /// <summary>
        /// Triggers a frequency breakpoint hit event using a FrequencyBreakpoint object
        /// </summary>
        /// <param name="breakpoint">The frequency breakpoint that was hit</param>
        public void TriggerFrequencyBreakpoint(Models.FrequencyBreakpoint breakpoint)
        {
            if (breakpoint == null) return;

            TriggerFrequencyBreakpoint(
                string.Empty,  // No ID in the new model
                breakpoint.Name,
                breakpoint.Threshold,
                breakpoint.EffectName,
                breakpoint.EffectIntensity,
                breakpoint.DurationMs
            );
        }

        /// <summary>
        /// Triggers a seed found event
        /// </summary>
        /// <param name="seed">The seed that was found</param>
        /// <param name="score">Score associated with the seed</param>
        public void TriggerSeedFound(string seed, long score)
        {
            DebugLogger.Log("VisualizerEventManager", $"Seed found: {seed} (score={score})");
            SeedFound?.Invoke(this, new SeedFoundEventArgs
            {
                Seed = seed,
                Score = score
            });
        }

        /// <summary>
        /// Triggers a high score event
        /// </summary>
        /// <param name="seed">The seed that achieved the high score</param>
        /// <param name="score">The high score value</param>
        /// <param name="isPersonalRecord">Whether this is a personal record</param>
        public void TriggerHighScore(string seed, long score, bool isPersonalRecord = false)
        {
            DebugLogger.Log("VisualizerEventManager",
                $"High score: {seed} (score={score}, PR={isPersonalRecord})");
            HighScore?.Invoke(this, new HighScoreEventArgs
            {
                Seed = seed,
                Score = score,
                IsPersonalRecord = isPersonalRecord
            });
        }

        /// <summary>
        /// Triggers a search complete event
        /// </summary>
        /// <param name="totalSearched">Total number of seeds searched</param>
        /// <param name="matchesFound">Number of matches found</param>
        /// <param name="duration">Search duration</param>
        public void TriggerSearchComplete(int totalSearched, int matchesFound, TimeSpan duration)
        {
            DebugLogger.Log("VisualizerEventManager",
                $"Search complete: {matchesFound}/{totalSearched} matches in {duration.TotalSeconds:F1}s");
            SearchComplete?.Invoke(this, new SearchCompleteEventArgs
            {
                TotalSearched = totalSearched,
                MatchesFound = matchesFound,
                Duration = duration
            });
        }

        #endregion
    }
}

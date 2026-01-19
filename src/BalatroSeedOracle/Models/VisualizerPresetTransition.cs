using System;
using SkiaSharp;

namespace BalatroSeedOracle.Models
{
    /// <summary>
    /// Generic transition system for interpolating between two shader parameter states.
    /// Progress-driven LERP of ALL shader parameters based on CurrentProgress (0.0 to 1.0).
    /// Completely agnostic - visual design is determined by Start/End parameters provided by user.
    /// </summary>
    public class VisualizerPresetTransition
    {
        /// <summary>
        /// Starting shader parameters (e.g., dark, pixelated intro state)
        /// </summary>
        public ShaderParameters StartParameters { get; set; } = new ShaderParameters();

        /// <summary>
        /// Ending shader parameters (e.g., normal Balatro state)
        /// </summary>
        public ShaderParameters EndParameters { get; set; } = new ShaderParameters();

        private float _currentProgress = 0f;

        /// <summary>
        /// Current transition progress (0.0 = StartParameters, 1.0 = EndParameters)
        /// </summary>
        public float CurrentProgress
        {
            get => _currentProgress;
            set => _currentProgress = Math.Clamp(value, 0f, 1f);
        }

        /// <summary>
        /// Optional: Duration for time-based auto-transition
        /// If set, transition will automatically advance based on elapsed time
        /// </summary>
        public TimeSpan? Duration { get; set; }

        /// <summary>
        /// Optional: Start time for time-based transition tracking
        /// </summary>
        public DateTime? TransitionStartTime { get; set; }

        /// <summary>
        /// Interpolates all shader parameters based on CurrentProgress.
        /// Returns a new ShaderParameters instance with LERP-ed values.
        /// Pure mathematical interpolation - no visual logic.
        /// </summary>
        public ShaderParameters GetInterpolatedParameters()
        {
            var t = CurrentProgress; // Already clamped 0-1

            return new ShaderParameters
            {
                // Time speeds
                TimeSpeed = Lerp(StartParameters.TimeSpeed, EndParameters.TimeSpeed, t),
                SpinTimeSpeed = Lerp(StartParameters.SpinTimeSpeed, EndParameters.SpinTimeSpeed, t),

                // Colors (LERP RGBA components)
                MainColor = LerpColor(StartParameters.MainColor, EndParameters.MainColor, t),
                AccentColor = LerpColor(StartParameters.AccentColor, EndParameters.AccentColor, t),
                BackgroundColor = LerpColor(StartParameters.BackgroundColor, EndParameters.BackgroundColor, t),

                // Effect parameters
                Contrast = Lerp(StartParameters.Contrast, EndParameters.Contrast, t),
                SpinAmount = Lerp(StartParameters.SpinAmount, EndParameters.SpinAmount, t),
                ParallaxX = Lerp(StartParameters.ParallaxX, EndParameters.ParallaxX, t),
                ParallaxY = Lerp(StartParameters.ParallaxY, EndParameters.ParallaxY, t),
                ZoomScale = Lerp(StartParameters.ZoomScale, EndParameters.ZoomScale, t),
                SaturationAmount = Lerp(StartParameters.SaturationAmount, EndParameters.SaturationAmount, t),
                SaturationAmount2 = Lerp(StartParameters.SaturationAmount2, EndParameters.SaturationAmount2, t),
                PixelSize = Lerp(StartParameters.PixelSize, EndParameters.PixelSize, t),
                SpinEase = Lerp(StartParameters.SpinEase, EndParameters.SpinEase, t),
                LoopCount = Lerp(StartParameters.LoopCount, EndParameters.LoopCount, t),
            };
        }

        /// <summary>
        /// Updates CurrentProgress based on elapsed time since TransitionStartTime.
        /// Call this in a timer/update loop for time-based transitions.
        /// </summary>
        public void UpdateProgressFromElapsedTime()
        {
            if (Duration == null || TransitionStartTime == null)
                return;

            var elapsed = DateTime.UtcNow - TransitionStartTime.Value;
            CurrentProgress = (float)(elapsed.TotalMilliseconds / Duration.Value.TotalMilliseconds);
        }

        /// <summary>
        /// Starts a time-based transition with the specified duration.
        /// CurrentProgress will auto-advance when UpdateProgressFromElapsedTime() is called.
        /// </summary>
        public void StartTimeBasedTransition(TimeSpan duration)
        {
            Duration = duration;
            TransitionStartTime = DateTime.UtcNow;
            CurrentProgress = 0f;
        }

        #region LERP Helpers

        /// <summary>
        /// Linear interpolation between two floats
        /// </summary>
        private float Lerp(float a, float b, float t)
        {
            return a + (b - a) * t;
        }

        /// <summary>
        /// Linear interpolation between two SKColors (RGBA components)
        /// </summary>
        private SKColor LerpColor(SKColor a, SKColor b, float t)
        {
            byte r = (byte)Math.Clamp(a.Red + (b.Red - a.Red) * t, 0, 255);
            byte g = (byte)Math.Clamp(a.Green + (b.Green - a.Green) * t, 0, 255);
            byte bl = (byte)Math.Clamp(a.Blue + (b.Blue - a.Blue) * t, 0, 255);
            byte alpha = (byte)Math.Clamp(a.Alpha + (b.Alpha - a.Alpha) * t, 0, 255);

            return new SKColor(r, g, bl, alpha);
        }

        #endregion
    }
}

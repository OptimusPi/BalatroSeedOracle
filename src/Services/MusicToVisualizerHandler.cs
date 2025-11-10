using System;
using Avalonia;
using Avalonia.Media;
using Avalonia.Platform;
using Avalonia.Rendering.Composition;
using Avalonia.Skia;
using BalatroSeedOracle.Constants;
using BalatroSeedOracle.Controls;
using BalatroSeedOracle.Helpers;
using SkiaSharp;

namespace BalatroSeedOracle.Services
{
    /// <summary>
    /// Handles the mapping of audio/music data to visual shader parameters.
    /// This class is responsible for ALL audio-to-visual processing.
    /// The shader (BalatroShaderBackground) should know nothing about audio.
    /// </summary>
    public class MusicToVisualizerHandler : CompositionCustomVisualHandler, IDisposable
    {
        private SKRuntimeShaderBuilder? _shaderBuilder;
        private bool _isDisposed;
        private bool _isAnimating = true;
        private float _vibeIntensity = 0f;
        private float _beatPulse = 0f;
        private float _melodicMid = 0f;
        private float _melodicTreble = 0f;
        private float _melodicPeak = 0f;
        private float _melodyIntensity = 0f;
        private float _chordsIntensity = 0f;
        private float _bassIntensity = 0f;
        private float _smoothedMelodySaturation = 0f;

        // Event-triggered effects
        private float _eventEffectIntensity = 0f;
        private float _eventZoomPunch = 0f;
        private float _eventContrastBoost = 0f;

        public MusicToVisualizerHandler()
        {
            // Subscribe to visualizer events
            var eventManager = VisualizerEventManager.Instance;
            eventManager.SeedFound += OnSeedFound;
            eventManager.HighScore += OnHighScore;
            eventManager.SearchComplete += OnSearchComplete;
            eventManager.FrequencyBreakpointHit += OnFrequencyBreakpointHit;
            eventManager.BeatDetected += OnBeatDetected;
            eventManager.DropDetected += OnDropDetected;

            DebugLogger.Log(
                "MusicToVisualizerHandler",
                "Subscribed to VisualizerEventManager events"
            );
        }

        // Shader parameters (controllable via settings)
        private float _contrast = 2.0f;
        private float _spinAmount = 0.3f;
        private float _speed = 1.0f;
        private float _baseTimeSpeed = 1.0f;
        private float _audioReactivityIntensity = 1.0f;

        // Per-parameter ranges for mapping audio to visuals
        private float _contrastRangeMin;
        private float _contrastRangeMax;
        private float _spinRangeMin;
        private float _spinRangeMax;
        private float _twirlRangeMin;
        private float _twirlRangeMax;
        private float _zoomPunchRangeMin;
        private float _zoomPunchRangeMax;
        private float _melodySatRangeMin;
        private float _melodySatRangeMax;

        // Audio source mappings
        public enum AudioSource
        {
            None,
            Drums,
            Bass,
            Chords,
            Melody,
        }

        private AudioSource _shadowFlickerSource = AudioSource.Drums;
        private AudioSource _spinSource = AudioSource.Bass;
        private AudioSource _twirlSource = AudioSource.Chords;
        private AudioSource _zoomThumpSource = AudioSource.Melody;
        private AudioSource _colorSaturationSource = AudioSource.Melody;
        private AudioSource _beatPulseSource = AudioSource.None;

        // Color configuration
        private int _mainColorIndex = 0; // Red by default
        private int _accentColorIndex = 4; // Blue by default
        private SKColor _backgroundColor = SKColors.Black;

        // Smoothed FFT values
        private float _smoothedMid = 0f;
        private float _smoothedTreble = 0f;
        private float _smoothedPeak = 0f;

        // Beat-driven rotation
        private float _musicAccumulatedRotation = 0f;
        private float _rotationVelocity = 0f;
        private float _spinDirection = 1f;
        private int _beatCounter = 0;
        private readonly System.Diagnostics.Stopwatch _stopwatch =
            System.Diagnostics.Stopwatch.StartNew();
        private double _lastUpdateTime = 0;

        // Mouse parallax
        private float _parallaxOffsetX = 0f;
        private float _parallaxOffsetY = 0f;
        private float _parallaxStrength = 0.15f;

        // Zoom effect
        private float _zoomPunch = 0f;

        public override void OnMessage(object message)
        {
            base.OnMessage(message);
            if (!_isDisposed && _isAnimating)
            {
                RegisterForNextAnimationFrameUpdate();
            }
        }

        public void SetAnimating(bool animating)
        {
            _isAnimating = animating;
            if (animating)
            {
                _lastUpdateTime = _stopwatch.Elapsed.TotalSeconds;
            }
        }

        public void SetVibeIntensity(float intensity)
        {
            _vibeIntensity = Math.Clamp(intensity, 0f, 1f);
        }

        public void SetMelodicFFT(float mid, float treble, float peak)
        {
            _melodicMid = Math.Clamp(mid, 0f, 10f);
            _melodicTreble = Math.Clamp(treble, 0f, 10f);
            _melodicPeak = Math.Clamp(peak, 0f, 10f);

            // Smooth the values
            const float smoothing = 0.7f;
            _smoothedMid = _smoothedMid * smoothing + _melodicMid * (1f - smoothing);
            _smoothedTreble = _smoothedTreble * smoothing + _melodicTreble * (1f - smoothing);
            _smoothedPeak = _smoothedPeak * smoothing + _melodicPeak * (1f - smoothing);
        }

        public void SetTrackIntensities(float melody, float chords, float bass)
        {
            _melodyIntensity = Math.Clamp(melody, 0f, 1f);
            _chordsIntensity = Math.Clamp(chords, 0f, 1f);
            _bassIntensity = Math.Clamp(bass, 0f, 1f);
        }

        // Shader parameter setters
        public void SetContrast(float contrast)
        {
            _contrast = Math.Clamp(contrast, 0.5f, 5.0f);
        }

        public void SetSpinAmount(float spinAmount)
        {
            _spinAmount = Math.Clamp(spinAmount, 0.0f, 1.0f);
        }

        public void SetSpeed(float speed)
        {
            _speed = Math.Clamp(speed, 0.1f, 3.0f);
        }

        public void SetBaseTimeSpeed(float speed)
        {
            _baseTimeSpeed = Math.Clamp(speed, 0.0f, 3.0f);
        }

        public void SetAudioReactivityIntensity(float intensity)
        {
            _audioReactivityIntensity = Math.Clamp(intensity, 0.0f, 2.0f);
        }

        public void SetParallaxStrength(float strength)
        {
            _parallaxStrength = Math.Clamp(strength, 0.0f, 1.0f);
        }

        public void UpdateParallax(float normalizedX, float normalizedY)
        {
            const float smoothing = 0.85f;
            _parallaxOffsetX =
                _parallaxOffsetX * smoothing + (normalizedX * _parallaxStrength) * (1f - smoothing);
            _parallaxOffsetY =
                _parallaxOffsetY * smoothing + (normalizedY * _parallaxStrength) * (1f - smoothing);
        }

        // Range configuration
        public void SetContrastRange(float min, float max)
        {
            _contrastRangeMin = Math.Min(min, max);
            _contrastRangeMax = Math.Max(min, max);
        }

        public void SetSpinAmountRange(float min, float max)
        {
            _spinRangeMin = Math.Min(min, max);
            _spinRangeMax = Math.Max(min, max);
        }

        public void SetTwirlSpeedRange(float min, float max)
        {
            _twirlRangeMin = Math.Min(min, max);
            _twirlRangeMax = Math.Max(min, max);
        }

        public void SetZoomPunchRange(float min, float max)
        {
            _zoomPunchRangeMin = Math.Min(min, max);
            _zoomPunchRangeMax = Math.Max(min, max);
        }

        public void SetMelodySaturationRange(float min, float max)
        {
            _melodySatRangeMin = Math.Min(min, max);
            _melodySatRangeMax = Math.Max(min, max);
        }

        // Audio source mapping
        public void SetShadowFlickerSource(AudioSource source)
        {
            _shadowFlickerSource = source;
        }

        public void SetSpinSource(AudioSource source)
        {
            _spinSource = source;
        }

        public void SetTwirlSource(AudioSource source)
        {
            _twirlSource = source;
        }

        public void SetZoomThumpSource(AudioSource source)
        {
            _zoomThumpSource = source;
        }

        public void SetColorSaturationSource(AudioSource source)
        {
            _colorSaturationSource = source;
        }

        public void SetBeatPulseSource(AudioSource source)
        {
            _beatPulseSource = source;
        }

        // Color configuration
        public void SetMainColor(int colorIndex)
        {
            _mainColorIndex = Math.Clamp(colorIndex, 0, 7);
        }

        public void SetAccentColor(int colorIndex)
        {
            _accentColorIndex = Math.Clamp(colorIndex, 0, 7);
        }

        // Get audio intensity from VLCAudioManager
        private float GetAudioIntensity(AudioSource source)
        {
            try
            {
                var audioManager = ServiceHelper.GetService<SoundFlowAudioManager>();
                if (audioManager == null)
                    return 0f;

                return source switch
                {
                    AudioSource.None => 0f,
                    AudioSource.Drums => audioManager.DrumsIntensity,
                    AudioSource.Bass => audioManager.BassIntensity,
                    AudioSource.Chords => audioManager.ChordsIntensity,
                    AudioSource.Melody => audioManager.MelodyIntensity,
                    _ => 0f,
                };
            }
            catch
            {
                return 0f;
            }
        }

        private static float Lerp(float a, float b, float t) => a + (b - a) * t;

        // Map color index to RGB
        private float[] GetColorFromIndex(int index)
        {
            return index switch
            {
                0 => new float[] { 1.0f, 0.0f, 0.0f, 1.0f }, // Red
                1 => new float[] { 1.0f, 0.5f, 0.0f, 1.0f }, // Orange
                2 => new float[] { 1.0f, 1.0f, 0.0f, 1.0f }, // Yellow
                3 => new float[] { 0.0f, 1.0f, 0.0f, 1.0f }, // Green
                4 => new float[] { 0.0f, 0.42f, 0.706f, 1.0f }, // Blue
                5 => new float[] { 0.6f, 0.2f, 0.8f, 1.0f }, // Purple
                6 => new float[] { 0.6f, 0.4f, 0.2f, 1.0f }, // Brown
                7 => new float[] { 1.0f, 1.0f, 1.0f, 1.0f }, // White
                _ => new float[] { 1.0f, 0.0f, 0.0f, 1.0f }, // Default to Red
            };
        }

        public override void OnRender(ImmediateDrawingContext context)
        {
            if (_isDisposed)
                return;

            // Update time and animations when animating
            if (_isAnimating)
            {
                var currentTime = _stopwatch.Elapsed.TotalSeconds;
                var deltaTime = (float)(currentTime - _lastUpdateTime);
                _lastUpdateTime = currentTime;

                // Beat-driven rotation
                var beatKick = _beatPulse * 8.0f;
                _rotationVelocity += beatKick * _spinDirection * deltaTime;
                _rotationVelocity *= 0.50f;
                _musicAccumulatedRotation += _rotationVelocity * deltaTime;
            }

            if (
                context.TryGetFeature(typeof(ISkiaSharpApiLeaseFeature))
                is ISkiaSharpApiLeaseFeature leaseFeature
            )
            {
                using var lease = leaseFeature.Lease();
                var canvas = lease.SkCanvas;

                InitializeShader();
                RenderShader(canvas);

                if (_isAnimating)
                {
                    RegisterForNextAnimationFrameUpdate();
                }
            }
        }

        private void InitializeShader()
        {
            if (_shaderBuilder != null)
                return;

            var effect = SKRuntimeEffect.CreateShader(
                ShaderConstants.BALATRO_SHADER,
                out var error
            );
            if (effect != null)
            {
                _shaderBuilder = new SKRuntimeShaderBuilder(effect);
                UpdateColors();
            }
            else
            {
                DebugLogger.LogError(
                    "MusicToVisualizerHandler",
                    $"Shader compilation failed: {error}"
                );
            }
        }

        private void UpdateColors()
        {
            if (_shaderBuilder == null)
                return;

            var mainColor = GetColorFromIndex(_mainColorIndex);
            var accentColor = GetColorFromIndex(_accentColorIndex);
            var backgroundColor = new float[] { 0.01f, 0.01f, 0.01f, 1.0f };

            _shaderBuilder.Uniforms["colour_1"] = mainColor;
            _shaderBuilder.Uniforms["colour_2"] = accentColor;
            _shaderBuilder.Uniforms["colour_3"] = backgroundColor;
            _shaderBuilder.Uniforms["contrast"] = _contrast;
            _shaderBuilder.Uniforms["spin_amount"] = _spinAmount;
        }

        private void RenderShader(SKCanvas canvas)
        {
            if (_shaderBuilder == null)
                return;

            var bounds = GetRenderBounds();
            var time = (float)_stopwatch.Elapsed.TotalSeconds;

            if (bounds.Width > 0 && bounds.Height > 0)
            {
                var currentSize = new SKSize((float)bounds.Width, (float)bounds.Height);
                var adjustedTime = time * _speed;

                // Get audio intensities
                float shadowFlickerIntensity = GetAudioIntensity(_shadowFlickerSource);
                float spinIntensity = GetAudioIntensity(_spinSource);
                float twirlIntensity = GetAudioIntensity(_twirlSource);
                float zoomThumpIntensity = GetAudioIntensity(_zoomThumpSource);
                float beatPulseIntensity = GetAudioIntensity(_beatPulseSource);

                _beatPulse = Math.Clamp(beatPulseIntensity, 0f, 1f);

                // Beat detection for spin direction
                if (_beatPulse > 0.3f)
                {
                    _beatCounter++;
                    if (_beatCounter % 2 == 0)
                    {
                        _spinDirection *= -1f;
                    }
                }

                // Map audio to visual parameters
                float intensityScale = _audioReactivityIntensity;

                float audioContrast = _contrast;
                if (_contrastRangeMax != _contrastRangeMin)
                {
                    audioContrast = Lerp(
                        _contrastRangeMin,
                        _contrastRangeMax,
                        shadowFlickerIntensity * intensityScale
                    );
                }
                // Apply event-driven contrast boost
                audioContrast += _eventContrastBoost;
                _eventContrastBoost *= 0.9f; // Decay event effect

                float audioSpinAmount = _spinAmount;
                if (_spinRangeMax != _spinRangeMin)
                {
                    audioSpinAmount = Lerp(
                        _spinRangeMin,
                        _spinRangeMax,
                        spinIntensity * intensityScale
                    );
                }

                float audioSpeedBoost = (twirlIntensity * intensityScale);
                if (_twirlRangeMax != _twirlRangeMin)
                {
                    audioSpeedBoost = Lerp(
                        _twirlRangeMin,
                        _twirlRangeMax,
                        twirlIntensity * intensityScale
                    );
                }

                // Zoom punch effect (audio + events)
                if (zoomThumpIntensity > 0.5f)
                {
                    float punchStrength = (zoomThumpIntensity - 0.5f) * 2.0f;
                    _zoomPunch += punchStrength * 10.0f * intensityScale;
                }
                _zoomPunch += _eventZoomPunch;
                _eventZoomPunch *= 0.8f; // Decay event effect
                _zoomPunch *= 0.85f;

                // Saturation mapping
                float saturationIntensity = GetAudioIntensity(_colorSaturationSource);
                float targetSaturation = saturationIntensity * intensityScale;
                if (_melodySatRangeMax != _melodySatRangeMin)
                {
                    targetSaturation = Lerp(
                        _melodySatRangeMin,
                        _melodySatRangeMax,
                        saturationIntensity * intensityScale
                    );
                }
                _smoothedMelodySaturation =
                    _smoothedMelodySaturation * 0.95f + targetSaturation * 0.05f;

                float totalSpeed = _baseTimeSpeed + audioSpeedBoost;

                // Update shader uniforms
                _shaderBuilder.Uniforms["time"] = adjustedTime * totalSpeed;
                _shaderBuilder.Uniforms["spin_time"] = adjustedTime * totalSpeed;
                _shaderBuilder.Uniforms["resolution"] = currentSize;
                _shaderBuilder.Uniforms["contrast"] = Math.Clamp(audioContrast, 0.5f, 8.0f);
                _shaderBuilder.Uniforms["spin_amount"] = Math.Clamp(audioSpinAmount, 0.0f, 1.0f);
                _shaderBuilder.Uniforms["parallax_x"] = _parallaxOffsetX;
                _shaderBuilder.Uniforms["parallax_y"] = _parallaxOffsetY;
                _shaderBuilder.Uniforms["zoom_scale"] = _zoomPunch;
                _shaderBuilder.Uniforms["saturation_amount"] = Math.Clamp(
                    _smoothedMelodySaturation,
                    0.0f,
                    1.0f
                );

                using var shader = _shaderBuilder.Build();
                using var paint = new SKPaint { Shader = shader };

                var rect = new SKRect(0, 0, currentSize.Width, currentSize.Height);
                canvas.DrawRect(rect, paint);
            }
        }

        public override void OnAnimationFrameUpdate()
        {
            if (!_isDisposed && _isAnimating)
            {
                Invalidate();
                RegisterForNextAnimationFrameUpdate();
            }
        }

        #region Event Handlers

        /// <summary>
        /// Handles seed found events - triggers a visual flash effect
        /// </summary>
        private void OnSeedFound(object? sender, SeedFoundEventArgs e)
        {
            DebugLogger.Log(
                "MusicToVisualizerHandler",
                $"Seed found event: {e.Seed} (score={e.Score})"
            );
            _eventZoomPunch += 15.0f;
            _eventContrastBoost += 2.0f;
        }

        /// <summary>
        /// Handles high score events - triggers an intense flash effect
        /// </summary>
        private void OnHighScore(object? sender, HighScoreEventArgs e)
        {
            DebugLogger.Log(
                "MusicToVisualizerHandler",
                $"High score event: {e.Seed} (score={e.Score}, PR={e.IsPersonalRecord})"
            );
            _eventZoomPunch += e.IsPersonalRecord ? 30.0f : 20.0f;
            _eventContrastBoost += e.IsPersonalRecord ? 4.0f : 3.0f;
        }

        /// <summary>
        /// Handles search complete events - triggers a completion effect
        /// </summary>
        private void OnSearchComplete(object? sender, SearchCompleteEventArgs e)
        {
            DebugLogger.Log(
                "MusicToVisualizerHandler",
                $"Search complete event: {e.MatchesFound}/{e.TotalSearched} in {e.Duration.TotalSeconds:F1}s"
            );
            _eventZoomPunch += 10.0f;
            _eventEffectIntensity += 0.5f;
        }

        /// <summary>
        /// Handles frequency breakpoint hit events - applies specified effect
        /// </summary>
        private void OnFrequencyBreakpointHit(object? sender, FrequencyBreakpointEventArgs e)
        {
            DebugLogger.Log(
                "MusicToVisualizerHandler",
                $"Frequency breakpoint hit: {e.BreakpointName} - {e.EffectName} @ {e.EffectIntensity}"
            );

            // Apply effect based on effect name
            switch (e.EffectName.ToLowerInvariant())
            {
                case "pulse":
                case "zoom":
                    _eventZoomPunch += e.EffectIntensity * 10.0f;
                    break;
                case "flash":
                case "contrast":
                    _eventContrastBoost += e.EffectIntensity * 2.0f;
                    break;
                case "spin":
                    _rotationVelocity += e.EffectIntensity * 5.0f;
                    break;
                default:
                    _eventEffectIntensity += e.EffectIntensity;
                    break;
            }
        }

        /// <summary>
        /// Handles beat detected events
        /// </summary>
        private void OnBeatDetected(object? sender, BeatDetectedEventArgs e)
        {
            // Beat detection is already handled in the main render loop
            // This is here for extensibility
        }

        /// <summary>
        /// Handles drop detected events - triggers an intense effect
        /// </summary>
        private void OnDropDetected(object? sender, DropDetectedEventArgs e)
        {
            DebugLogger.Log(
                "MusicToVisualizerHandler",
                $"Drop detected: intensity={e.Intensity:F2}, freq={e.FrequencyHz}Hz"
            );
            _eventZoomPunch += e.Intensity * 25.0f;
            _eventContrastBoost += e.Intensity * 3.0f;
        }

        #endregion

        public void Dispose()
        {
            if (_isDisposed)
                return;
            _isDisposed = true;

            // Unsubscribe from events
            var eventManager = VisualizerEventManager.Instance;
            eventManager.SeedFound -= OnSeedFound;
            eventManager.HighScore -= OnHighScore;
            eventManager.SearchComplete -= OnSearchComplete;
            eventManager.FrequencyBreakpointHit -= OnFrequencyBreakpointHit;
            eventManager.BeatDetected -= OnBeatDetected;
            eventManager.DropDetected -= OnDropDetected;

            _shaderBuilder?.Dispose();
            _shaderBuilder = null;
        }
    }
}

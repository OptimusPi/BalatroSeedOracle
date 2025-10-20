using System;
using Avalonia;
using Avalonia.Media;
using Avalonia.Platform;
using Avalonia.Rendering.Composition;
using Avalonia.Skia;
using SkiaSharp;
using BalatroSeedOracle.Controls;
using BalatroSeedOracle.Helpers;

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
            Melody
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
        private readonly System.Diagnostics.Stopwatch _stopwatch = System.Diagnostics.Stopwatch.StartNew();
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
            _parallaxOffsetX = _parallaxOffsetX * smoothing + (normalizedX * _parallaxStrength) * (1f - smoothing);
            _parallaxOffsetY = _parallaxOffsetY * smoothing + (normalizedY * _parallaxStrength) * (1f - smoothing);
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

        // Get audio intensity from VibeAudioManager
        private float GetAudioIntensity(AudioSource source)
        {
            try
            {
                var audioManager = ServiceHelper.GetService<VibeAudioManager>();
                if (audioManager == null) return 0f;

                return source switch
                {
                    AudioSource.None => 0f,
                    AudioSource.Drums => audioManager.DrumsIntensity,
                    AudioSource.Bass => audioManager.BassIntensity,
                    AudioSource.Chords => audioManager.ChordsIntensity,
                    AudioSource.Melody => audioManager.MelodyIntensity,
                    _ => 0f
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
                0 => new float[] { 1.0f, 0.0f, 0.0f, 1.0f },      // Red
                1 => new float[] { 1.0f, 0.5f, 0.0f, 1.0f },      // Orange
                2 => new float[] { 1.0f, 1.0f, 0.0f, 1.0f },      // Yellow
                3 => new float[] { 0.0f, 1.0f, 0.0f, 1.0f },      // Green
                4 => new float[] { 0.0f, 0.42f, 0.706f, 1.0f },   // Blue
                5 => new float[] { 0.6f, 0.2f, 0.8f, 1.0f },      // Purple
                6 => new float[] { 0.6f, 0.4f, 0.2f, 1.0f },      // Brown
                7 => new float[] { 1.0f, 1.0f, 1.0f, 1.0f },      // White
                _ => new float[] { 1.0f, 0.0f, 0.0f, 1.0f }       // Default to Red
            };
        }

        public override void OnRender(ImmediateDrawingContext context)
        {
            if (_isDisposed) return;

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

            if (context.TryGetFeature(typeof(ISkiaSharpApiLeaseFeature)) is ISkiaSharpApiLeaseFeature leaseFeature)
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
            if (_shaderBuilder != null) return;

            // Pure Balatro paint-mixing shader
            var sksl = @"
                uniform float2 resolution;
                uniform float time;
                uniform float spin_time;
                uniform float4 colour_1;
                uniform float4 colour_2;
                uniform float4 colour_3;
                uniform float contrast;
                uniform float spin_amount;
                uniform float parallax_x;
                uniform float parallax_y;
                uniform float zoom_scale;
                uniform float saturation_amount;

                const float PIXEL_SIZE_FAC = 1440.0;
                const float SPIN_EASE = 0.5;

                // HSV conversion functions
                float3 rgb2hsv(float3 c) {
                    float4 K = float4(0.0, -1.0 / 3.0, 2.0 / 3.0, -1.0);
                    float4 p = mix(float4(c.bg, K.wz), float4(c.gb, K.xy), step(c.b, c.g));
                    float4 q = mix(float4(p.xyw, c.r), float4(c.r, p.yzx), step(p.x, c.r));
                    float d = q.x - min(q.w, q.y);
                    float e = 1.0e-10;
                    return float3(abs(q.z + (q.w - q.y) / (6.0 * d + e)), d / (q.x + e), q.x);
                }

                float3 hsv2rgb(float3 c) {
                    float4 K = float4(1.0, 2.0 / 3.0, 1.0 / 3.0, 3.0);
                    float3 p = abs(fract(c.xxx + K.xyz) * 6.0 - K.www);
                    return c.z * mix(K.xxx, clamp(p - K.xxx, 0.0, 1.0), c.y);
                }

                float4 main(float2 screen_coords) {
                    // Pixelated UV with parallax
                    float pixel_size = length(resolution) / PIXEL_SIZE_FAC;
                    float2 uv = (floor(screen_coords * (1.0 / pixel_size)) * pixel_size - 0.5 * resolution) / length(resolution) - float2(parallax_x, parallax_y);
                    float uv_len = length(uv);

                    // Center swirl
                    float speed = (spin_time * SPIN_EASE * 0.2) + 302.2;
                    float new_pixel_angle = atan(uv.y, uv.x) + speed - SPIN_EASE * 20.0 * (1.0 * spin_amount * uv_len + (1.0 - 1.0 * spin_amount));
                    float2 mid = (resolution / length(resolution)) / 2.0;
                    uv = float2((uv_len * cos(new_pixel_angle) + mid.x), (uv_len * sin(new_pixel_angle) + mid.y)) - mid;

                    // Paint effect with zoom
                    uv *= (30.0 + zoom_scale);
                    speed = time * 2.0;
                    float2 uv2 = float2(uv.x + uv.y);

                    for (int i = 0; i < 5; i++) {
                        uv2 += sin(max(uv.x, uv.y)) + uv;
                        uv += 0.5 * float2(cos(5.1123314 + 0.353 * uv2.y + speed * 0.131121), sin(uv2.x - 0.113 * speed));
                        uv -= 1.0 * cos(uv.x + uv.y) - 1.0 * sin(uv.x * 0.711 - uv.y);
                    }

                    // Paint mixing
                    float contrast_mod = (0.25 * contrast + 0.5 * spin_amount + 1.2);
                    float paint_res = min(2.0, max(0.0, length(uv) * 0.035 * contrast_mod));
                    float c1p = max(0.0, 1.0 - contrast_mod * abs(1.0 - paint_res));
                    float c2p = max(0.0, 1.0 - contrast_mod * abs(paint_res));
                    float c3p = 1.0 - min(1.0, c1p + c2p);

                    // Apply saturation boost
                    float4 adjusted_colour_1 = colour_1;
                    if (saturation_amount > 0.01) {
                        float3 hsv = rgb2hsv(colour_1.rgb);
                        float satBoost = saturation_amount * 0.3;
                        hsv.y = clamp(hsv.y + satBoost, 0.0, 1.0);
                        adjusted_colour_1 = float4(hsv2rgb(hsv), colour_1.a);
                    }

                    float4 ret_col = (0.3 / contrast) * adjusted_colour_1 + (1.0 - 0.3 / contrast) * (adjusted_colour_1 * c1p + colour_2 * c2p + float4(c3p * colour_3.rgb, c3p * colour_1.a));
                    return ret_col;
                }";

            var effect = SKRuntimeEffect.CreateShader(sksl, out var error);
            if (effect != null)
            {
                _shaderBuilder = new SKRuntimeShaderBuilder(effect);
                UpdateColors();
            }
            else
            {
                DebugLogger.LogError("MusicToVisualizerHandler", $"Shader compilation failed: {error}");
            }
        }

        private void UpdateColors()
        {
            if (_shaderBuilder == null) return;

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
            if (_shaderBuilder == null) return;

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
                    audioContrast = Lerp(_contrastRangeMin, _contrastRangeMax, shadowFlickerIntensity * intensityScale);
                }

                float audioSpinAmount = _spinAmount;
                if (_spinRangeMax != _spinRangeMin)
                {
                    audioSpinAmount = Lerp(_spinRangeMin, _spinRangeMax, spinIntensity * intensityScale);
                }

                float audioSpeedBoost = (twirlIntensity * intensityScale);
                if (_twirlRangeMax != _twirlRangeMin)
                {
                    audioSpeedBoost = Lerp(_twirlRangeMin, _twirlRangeMax, twirlIntensity * intensityScale);
                }

                // Zoom punch effect
                if (zoomThumpIntensity > 0.5f)
                {
                    float punchStrength = (zoomThumpIntensity - 0.5f) * 2.0f;
                    _zoomPunch += punchStrength * 10.0f * intensityScale;
                }
                _zoomPunch *= 0.85f;

                // Saturation mapping
                float saturationIntensity = GetAudioIntensity(_colorSaturationSource);
                float targetSaturation = saturationIntensity * intensityScale;
                if (_melodySatRangeMax != _melodySatRangeMin)
                {
                    targetSaturation = Lerp(_melodySatRangeMin, _melodySatRangeMax, saturationIntensity * intensityScale);
                }
                _smoothedMelodySaturation = _smoothedMelodySaturation * 0.95f + targetSaturation * 0.05f;

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
                _shaderBuilder.Uniforms["saturation_amount"] = Math.Clamp(_smoothedMelodySaturation, 0.0f, 1.0f);

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

        public void Dispose()
        {
            if (_isDisposed) return;
            _isDisposed = true;
            _shaderBuilder?.Dispose();
            _shaderBuilder = null;
        }
    }
}
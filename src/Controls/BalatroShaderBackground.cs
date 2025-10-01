using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Rendering.Composition;
using Avalonia.Skia;
using SkiaSharp;

namespace BalatroSeedOracle.Controls
{
    public class BalatroShaderBackground : Control
    {
        private CompositionCustomVisual? _customVisual;
        private BalatroShaderHandler? _handler;
        
        // Compatibility interface for existing code
        public enum BackgroundTheme { Default, Plasma, Sepia, Ocean, Sunset, Midnight, Dynamic, VibeOut }
        
        private bool _isAnimating = true;
        public bool IsAnimating
        {
            get => _isAnimating;
            set
            {
                _isAnimating = value;
                _handler?.SetAnimating(value);
            }
        }

        // Music-reactive properties for VibeOut mode
        public void UpdateVibeIntensity(float intensity)
        {
            _handler?.SetVibeIntensity(intensity);
        }

        public void OnBeatDetected(float intensity)
        {
            _handler?.SetBeatPulse(intensity);
        }

        public void UpdateMelodicFFT(float mid, float treble, float peak)
        {
            _handler?.SetMelodicFFT(mid, treble, peak);
        }

        public void UpdateTrackIntensities(float melody, float chords, float bass)
        {
            _handler?.SetTrackIntensities(melody, chords, bass);
        }
        
        private BackgroundTheme _theme = BackgroundTheme.Default;
        public BackgroundTheme Theme
        {
            get => _theme;
            set
            {
                _theme = value;
                _handler?.SetTheme(value);
            }
        }

        public void SetTheme(int themeIndex)
        {
            var themes = Enum.GetValues<BackgroundTheme>();
            if (themeIndex >= 0 && themeIndex < themes.Length)
            {
                Theme = themes[themeIndex];
            }
        }

        public void CycleTheme()
        {
            var themes = Enum.GetValues<BackgroundTheme>();
            int currentIndex = Array.IndexOf(themes, _theme);
            int nextIndex = (currentIndex + 1) % themes.Length;
            Theme = themes[nextIndex];
        }

        protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
        {
            base.OnAttachedToVisualTree(e);

            var compositionTarget = ElementComposition.GetElementVisual(this);
            if (compositionTarget?.Compositor != null)
            {
                _handler = new BalatroShaderHandler();
                _customVisual = compositionTarget.Compositor.CreateCustomVisual(_handler);
                ElementComposition.SetElementChildVisual(this, _customVisual);
                _customVisual.Size = new Vector(Bounds.Width, Bounds.Height);
            }
        }

        protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
        {
            // CompositionCustomVisual disposal handled by framework
            _customVisual = null;
            _handler?.Dispose();
            _handler = null;
            base.OnDetachedFromVisualTree(e);
        }

        protected override Size ArrangeOverride(Size finalSize)
        {
            if (_customVisual != null)
            {
                _customVisual.Size = new Vector(finalSize.Width, finalSize.Height);
            }
            return base.ArrangeOverride(finalSize);
        }
    }

    public class BalatroShaderHandler : CompositionCustomVisualHandler, IDisposable
    {
        private SKRuntimeShaderBuilder? _shaderBuilder;
        private bool _isDisposed;
        private bool _isAnimating = true;
        private BalatroShaderBackground.BackgroundTheme _theme = BalatroShaderBackground.BackgroundTheme.Default;
        private float _vibeIntensity = 0f;
        private float _beatPulse = 0f;
        private float _melodicMid = 0f;
        private float _melodicTreble = 0f;
        private float _melodicPeak = 0f;
        private float _melodyIntensity = 0f;
        private float _chordsIntensity = 0f;
        private float _bassIntensity = 0f;

        // Smoothed FFT values to prevent jitter
        private float _smoothedMid = 0f;
        private float _smoothedTreble = 0f;
        private float _smoothedPeak = 0f;

        // Beat-driven rotation with inertia and direction flip
        private float _musicAccumulatedRotation = 0f;
        private float _rotationVelocity = 0f; // Current spin velocity with inertia
        private float _spinDirection = 1f; // 1 or -1, flips every other beat
        private int _beatCounter = 0; // Count beats for alternating direction
        private readonly System.Diagnostics.Stopwatch _stopwatch = System.Diagnostics.Stopwatch.StartNew();
        private double _lastUpdateTime = 0;

        public void SetAnimating(bool animating)
        {
            _isAnimating = animating;

            // Reset last update time to prevent catch-up spin when resuming
            if (animating)
            {
                _lastUpdateTime = _stopwatch.Elapsed.TotalSeconds;
            }
        }

        public void SetTheme(BalatroShaderBackground.BackgroundTheme theme)
        {
            _theme = theme;
            UpdateThemeColors();
        }

        public void SetVibeIntensity(float intensity)
        {
            _vibeIntensity = Math.Clamp(intensity, 0f, 1f);
        }

        public void SetBeatPulse(float pulse)
        {
            _beatPulse = Math.Clamp(pulse, 0f, 1f);

            // Flip spin direction every other beat for back-and-forth effect
            if (pulse > 0.3f) // Only count significant beats
            {
                _beatCounter++;
                if (_beatCounter % 2 == 0)
                {
                    _spinDirection *= -1f; // Reverse direction every other beat
                }
            }
        }

        public void SetMelodicFFT(float mid, float treble, float peak)
        {
            _melodicMid = Math.Clamp(mid, 0f, 10f);
            _melodicTreble = Math.Clamp(treble, 0f, 10f);
            _melodicPeak = Math.Clamp(peak, 0f, 10f);

            // Smooth the values with exponential moving average (prevents jitter)
            const float smoothing = 0.7f; // Higher = smoother but slower response
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

        public override void OnRender(ImmediateDrawingContext context)
        {
            if (_isDisposed) return;

            // Update "fake time" - constant spin + music kicks
            var currentTime = _stopwatch.Elapsed.TotalSeconds;
            var deltaTime = (float)(currentTime - _lastUpdateTime);
            _lastUpdateTime = currentTime;

            // Beat-driven rotation with inertia and alternating direction
            if (_isAnimating)
            {
                // Beat kicks add GENTLE rotational impulse with direction flip
                var beatKick = _beatPulse * 8.0f; // Much gentler (was 50!)
                _rotationVelocity += beatKick * _spinDirection * deltaTime;

                // Strong decay - actually stops between beats
                _rotationVelocity *= 0.50f;

                // Accumulate rotation from beat impulses
                _musicAccumulatedRotation += _rotationVelocity * deltaTime;
            }

            if (context.TryGetFeature(typeof(ISkiaSharpApiLeaseFeature)) is ISkiaSharpApiLeaseFeature leaseFeature)
            {
                using var lease = leaseFeature.Lease();
                var canvas = lease.SkCanvas;

                InitializeShader();
                RenderShader(canvas);

                RegisterForNextAnimationFrameUpdate();
            }
        }

        private void InitializeShader()
        {
            if (_shaderBuilder != null) return;

            // Music-driven Balatro shader with accumulated rotation
            var sksl = @"
                uniform float2 resolution;
                uniform float time;
                uniform float spin_time;
                uniform float4 colour_1;
                uniform float4 colour_2;
                uniform float4 colour_3;
                uniform float contrast;
                uniform float spin_amount;
                uniform float vibe_intensity;
                uniform float beat_pulse;
                uniform float music_rotation; // Accumulated rotation (forward-only)
                uniform float melodic_mid;    // Bass/Chords mid frequencies
                uniform float melodic_treble; // Melody treble
                uniform float melodic_peak;   // Overall melodic energy
                uniform float melody_intensity;  // Melody volume
                uniform float chords_intensity;  // Chords volume
                uniform float bass_intensity;    // Bass volume

                const float PIXEL_SIZE_FAC = 1080;
                const float SPIN_EASE = 2.2;

                float4 main(float2 screen_coords) {
                    float pixel_size = length(resolution) / PIXEL_SIZE_FAC;
                    float2 uv = (floor(screen_coords * (1.0 / pixel_size)) * pixel_size - 0.5 * resolution) / length(resolution);
                    float uv_len = length(uv);

                    // Music-driven animation: individual tracks drive different effects
                    float color_intensity = melodic_peak * 0.5; // Overall energy
                    float white_flash = bass_intensity * 0.25; // Bass creates white flash!

                    // Rotation driven by accumulated music_rotation (from drum beats)
                    float new_pixel_angle = atan(uv.y, uv.x) + music_rotation - SPIN_EASE * 20.0 * (1.0 * spin_amount * uv_len + (1.0 - 1.0 * spin_amount));
                    float2 mid = (resolution / length(resolution)) / 2.0;
                    uv = float2((uv_len * cos(new_pixel_angle) + mid.x), (uv_len * sin(new_pixel_angle) + mid.y)) - mid;

                    // Zoom: constant (no breathing)
                    float bass_zoom = 30.0;
                    uv *= bass_zoom;

                    // Pattern generation - gentle wiggle + TINY music boost
                    float wiggle = sin(time * 2.5) * 1.1; // Gentle side-to-side wiggle
                    float music_boost = (melodic_mid + melodic_treble) * 1; // TINY music boost
                    float pattern_speed = time * 5.0 + wiggle + music_boost; // Forward + wiggle + subtle music
                    float2 uv2 = float2(uv.x + uv.y);

                    for (int i = 0; i < 4; i++) {
                        uv2 += sin(max(uv.x, uv.y)) + uv;
                        uv += 0.5 * float2(cos(5.1123314 + 0.353 * uv2.y + pattern_speed * 0.131121), sin(uv2.x - 0.113 * pattern_speed));
                        uv -= 1.0 * cos(uv.x + uv.y) - 1.0 * sin(uv.x * 0.711 - uv.y);
                    }

                    float contrast_mod = (0.25 * contrast + 0.5 * spin_amount + 1.2);
                    float paint_res = min(8.0, max(0.0, length(uv) * 0.035 * contrast_mod));
                    float c1p = max(0.0, 1.0 - contrast_mod * abs(1.0 - paint_res));
                    float c2p = max(0.0, 1.0 - contrast_mod * abs(paint_res));
                    float c3p = 1.0 - min(1.0, c1p + c2p);

                    // Simple colors - just use them as-is, they work fine!
                    float4 base_col = colour_1 * c1p + colour_2 * c2p + float4(c3p * colour_3.rgb, c3p * colour_1.a);

                    float4 ret_col = (0.3 / contrast) * colour_1 + (1.0 - 0.3 / contrast) * base_col;

                    return ret_col;
                }";

            var effect = SKRuntimeEffect.CreateShader(sksl, out var error);
            if (effect != null)
            {
                _shaderBuilder = new SKRuntimeShaderBuilder(effect);
                SetUniforms();
            }
        }

        private void SetUniforms()
        {
            if (_shaderBuilder == null) return;

            UpdateThemeColors();
            _shaderBuilder.Uniforms["contrast"] = 3.5f;
            _shaderBuilder.Uniforms["spin_amount"] = 0.2f;
            _shaderBuilder.Uniforms["vibe_intensity"] = 0f;
            _shaderBuilder.Uniforms["beat_pulse"] = 0f;
            _shaderBuilder.Uniforms["music_rotation"] = 0f;
            _shaderBuilder.Uniforms["melodic_mid"] = 0f;
            _shaderBuilder.Uniforms["melodic_treble"] = 0f;
            _shaderBuilder.Uniforms["melodic_peak"] = 0f;
            _shaderBuilder.Uniforms["melody_intensity"] = 0f;
            _shaderBuilder.Uniforms["chords_intensity"] = 0f;
            _shaderBuilder.Uniforms["bass_intensity"] = 0f;
        }

        private void UpdateThemeColors()
        {
            if (_shaderBuilder == null) return;

            var (c1, c2, c3) = GetThemeColors(_theme);
            _shaderBuilder.Uniforms["colour_1"] = c1;
            _shaderBuilder.Uniforms["colour_2"] = c2;
            _shaderBuilder.Uniforms["colour_3"] = c3;
        }

        private (float[], float[], float[]) GetThemeColors(BalatroShaderBackground.BackgroundTheme theme)
        {
            return theme switch
            {
                BalatroShaderBackground.BackgroundTheme.Default => (
                    new float[] { 0.871f, 0.267f, 0.231f, 1.0f },
                    new float[] { 0.0f, 0.42f, 0.706f, 1.0f },
                    new float[] { 0.086f, 0.137f, 0.145f, 1.0f }
                ),
                BalatroShaderBackground.BackgroundTheme.Plasma => (
                    new float[] { 0.6f, 0.2f, 0.8f, 1.0f },
                    new float[] { 0.2f, 0.8f, 0.4f, 1.0f },
                    new float[] { 0.1f, 0.05f, 0.15f, 1.0f }
                ),
                BalatroShaderBackground.BackgroundTheme.Ocean => (
                    new float[] { 0.0f, 0.4f, 0.6f, 1.0f },
                    new float[] { 0.0f, 0.7f, 0.8f, 1.0f },
                    new float[] { 0.0f, 0.1f, 0.2f, 1.0f }
                ),
                BalatroShaderBackground.BackgroundTheme.Sunset => (
                    new float[] { 1.0f, 0.4f, 0.2f, 1.0f },
                    new float[] { 0.8f, 0.2f, 0.6f, 1.0f },
                    new float[] { 0.2f, 0.05f, 0.1f, 1.0f }
                ),
                BalatroShaderBackground.BackgroundTheme.Midnight => (
                    new float[] { 0.1f, 0.1f, 0.3f, 1.0f },
                    new float[] { 0.3f, 0.1f, 0.5f, 1.0f },
                    new float[] { 0.05f, 0.05f, 0.1f, 1.0f }
                ),
                BalatroShaderBackground.BackgroundTheme.VibeOut => (
                    new float[] { 0.0f, 1.0f, 0.53f, 1.0f }, // Neon green (#00FF88)
                    new float[] { 0.61f, 0.35f, 0.71f, 1.0f }, // Purple (#9b59b6)
                    new float[] { 0.0f, 0.0f, 0.0f, 1.0f } // Pure black
                ),
                _ => (
                    new float[] { 0.871f, 0.267f, 0.231f, 1.0f },
                    new float[] { 0.0f, 0.42f, 0.706f, 1.0f },
                    new float[] { 0.086f, 0.137f, 0.145f, 1.0f }
                )
            };
        }

        private void RenderShader(SKCanvas canvas)
        {
            if (_shaderBuilder == null) return;

            var bounds = GetRenderBounds();
            var time = (float)_stopwatch.Elapsed.TotalSeconds;

            if (bounds.Width > 0 && bounds.Height > 0)
            {
                var currentSize = new SKSize((float)bounds.Width, (float)bounds.Height);

                // Update uniforms (this is fast, no allocation)
                _shaderBuilder.Uniforms["time"] = time;
                _shaderBuilder.Uniforms["spin_time"] = time;
                _shaderBuilder.Uniforms["resolution"] = currentSize;
                _shaderBuilder.Uniforms["vibe_intensity"] = _vibeIntensity;
                _shaderBuilder.Uniforms["beat_pulse"] = _beatPulse;
                _shaderBuilder.Uniforms["music_rotation"] = _musicAccumulatedRotation;
                // Use smoothed values for shader (prevents jitter)
                _shaderBuilder.Uniforms["melodic_mid"] = _smoothedMid;
                _shaderBuilder.Uniforms["melodic_treble"] = _smoothedTreble;
                _shaderBuilder.Uniforms["melodic_peak"] = _smoothedPeak;
                _shaderBuilder.Uniforms["melody_intensity"] = _melodyIntensity;
                _shaderBuilder.Uniforms["chords_intensity"] = _chordsIntensity;
                _shaderBuilder.Uniforms["bass_intensity"] = _bassIntensity;


                // Build shader (SKRuntimeEffect reuses internal resources)
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
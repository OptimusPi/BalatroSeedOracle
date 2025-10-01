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

        // Public methods to control shader parameters
        public void SetContrast(float contrast)
        {
            _handler?.SetContrast(contrast);
        }

        public void SetSpinAmount(float spinAmount)
        {
            _handler?.SetSpinAmount(spinAmount);
        }

        public void SetSpeed(float speed)
        {
            _handler?.SetSpeed(speed);
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

        // Shader parameters (controllable via settings)
        private float _contrast = 2.0f;
        private float _spinAmount = 0.3f;
        private float _speed = 1.0f;

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

        // Public methods to control shader parameters
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

            // REAL Balatro background shader (converted from external/Balatro/resources/shaders/background.fs)
            var sksl = @"
                uniform float2 resolution;
                uniform float time;
                uniform float spin_time;
                uniform float4 colour_1;
                uniform float4 colour_2;
                uniform float4 colour_3;
                uniform float contrast;
                uniform float spin_amount;

                const float PIXEL_SIZE_FAC = 700.0;
                const float SPIN_EASE = 0.5;

                float4 main(float2 screen_coords) {
                    // Convert to UV coords (0-1) and floor for pixel effect
                    float pixel_size = length(resolution) / PIXEL_SIZE_FAC;
                    float2 uv = (floor(screen_coords * (1.0 / pixel_size)) * pixel_size - 0.5 * resolution) / length(resolution) - float2(0.12, 0.0);
                    float uv_len = length(uv);

                    // Center swirl that changes with time
                    float speed = (spin_time * SPIN_EASE * 0.2) + 302.2;
                    float new_pixel_angle = atan(uv.y, uv.x) + speed - SPIN_EASE * 20.0 * (1.0 * spin_amount * uv_len + (1.0 - 1.0 * spin_amount));
                    float2 mid = (resolution / length(resolution)) / 2.0;
                    uv = float2((uv_len * cos(new_pixel_angle) + mid.x), (uv_len * sin(new_pixel_angle) + mid.y)) - mid;

                    // Paint effect
                    uv *= 30.0;
                    speed = time * 2.0;
                    float2 uv2 = float2(uv.x + uv.y);

                    for (int i = 0; i < 5; i++) {
                        uv2 += sin(max(uv.x, uv.y)) + uv;
                        uv += 0.5 * float2(cos(5.1123314 + 0.353 * uv2.y + speed * 0.131121), sin(uv2.x - 0.113 * speed));
                        uv -= 1.0 * cos(uv.x + uv.y) - 1.0 * sin(uv.x * 0.711 - uv.y);
                    }

                    // Paint amount ranges from 0-2
                    float contrast_mod = (0.25 * contrast + 0.5 * spin_amount + 1.2);
                    float paint_res = min(2.0, max(0.0, length(uv) * 0.035 * contrast_mod));
                    float c1p = max(0.0, 1.0 - contrast_mod * abs(1.0 - paint_res));
                    float c2p = max(0.0, 1.0 - contrast_mod * abs(paint_res));
                    float c3p = 1.0 - min(1.0, c1p + c2p);

                    float4 ret_col = (0.3 / contrast) * colour_1 + (1.0 - 0.3 / contrast) * (colour_1 * c1p + colour_2 * c2p + float4(c3p * colour_3.rgb, c3p * colour_1.a));

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
            _shaderBuilder.Uniforms["contrast"] = _contrast;
            _shaderBuilder.Uniforms["spin_amount"] = _spinAmount;
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

                // Apply speed multiplier to time for faster/slower animation
                var adjustedTime = time * _speed;

                // Update uniforms (this is fast, no allocation)
                _shaderBuilder.Uniforms["time"] = adjustedTime;
                _shaderBuilder.Uniforms["spin_time"] = adjustedTime;
                _shaderBuilder.Uniforms["resolution"] = currentSize;
                _shaderBuilder.Uniforms["contrast"] = _contrast;
                _shaderBuilder.Uniforms["spin_amount"] = _spinAmount;


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
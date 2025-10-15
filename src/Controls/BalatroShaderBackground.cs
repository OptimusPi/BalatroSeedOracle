using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Rendering.Composition;
using Avalonia.Skia;
using SkiaSharp;
using BalatroSeedOracle.Views;

namespace BalatroSeedOracle.Controls
{
    /// <summary>
    /// Audio source enumeration for shader parameters
    /// </summary>
    public enum AudioSource
    {
        None = 0,
        Drums = 1,
        Bass = 2,
        Chords = 3,
        Melody = 4
    }

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

        public void SetZoomPunch(float zoom)
        {
            _handler?.SetZoomPunch(zoom);
        }

        public void SetMelodySaturation(float saturation)
        {
            _handler?.SetMelodySaturation(saturation);
        }

        // Audio-reactive shader parameter control
        public void SetShadowFlickerSource(AudioSource source)
        {
            _handler?.SetShadowFlickerSource(source);
        }

        // Legacy support - for backward compatibility
        public void SetTwistSource(AudioSource source) => SetShadowFlickerSource(source);

        public void SetSpinSource(AudioSource source)
        {
            _handler?.SetSpinSource(source);
        }

        public void SetTwirlSource(AudioSource source)
        {
            _handler?.SetTwirlSource(source);
        }

        public void SetZoomThumpSource(AudioSource source)
        {
            _handler?.SetZoomThumpSource(source);
        }

        public void SetColorSaturationSource(AudioSource source)
        {
            _handler?.SetColorSaturationSource(source);
        }

        public void SetBeatPulseSource(AudioSource source)
        {
            _handler?.SetBeatPulseSource(source);
        }

        public void SetMainColor(int colorIndex)
        {
            _handler?.SetMainColor(colorIndex);
        }

        public void SetAccentColor(int colorIndex)
        {
            _handler?.SetAccentColor(colorIndex);
        }

        public void SetAudioReactivityIntensity(float intensity)
        {
            _handler?.SetAudioReactivityIntensity(intensity);
        }

        public void SetParallaxStrength(float strength)
        {
            _handler?.SetParallaxStrength(strength);
        }

        public void SetBaseTimeSpeed(float speed)
        {
            _handler?.SetBaseTimeSpeed(speed);
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
                
                // Start the animation loop by invalidating and registering for updates
                _handler.Invalidate();
                _handler.RegisterForNextAnimationFrameUpdate();
            }

            // Hook up mouse move for parallax effect
            this.PointerMoved += OnPointerMoved;
        }

        private void OnPointerMoved(object? sender, Avalonia.Input.PointerEventArgs e)
        {
            var position = e.GetPosition(this);
            var bounds = this.Bounds;

            // Convert mouse position to -1 to 1 range, inverted for parallax
            float normalizedX = -((float)(position.X / bounds.Width) * 2f - 1f);
            float normalizedY = -((float)(position.Y / bounds.Height) * 2f - 1f);

            _handler?.UpdateParallax(normalizedX, normalizedY);
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
        private float _smoothedMelodySaturation = 0f; // Heavily smoothed for non-flickering saturation

        // Shader parameters (controllable via settings)
        private float _contrast = 2.0f;
        private float _spinAmount = 0.3f;
        private float _speed = 1.0f; // Kept for compatibility, not used for base time anymore
        private float _baseTimeSpeed = 1.0f;
        private float _audioReactivityIntensity = 0.0f;

        // Audio source mappings for shader parameters
        private AudioSource _shadowFlickerSource = AudioSource.Drums;
        private AudioSource _spinSource = AudioSource.Bass;
        private AudioSource _twirlSource = AudioSource.Chords;
        private AudioSource _zoomThumpSource = AudioSource.Melody;
        private AudioSource _colorSaturationSource = AudioSource.Melody;
        private AudioSource _beatPulseSource = AudioSource.None; // Disabled by default

        // Color indices (0-7)
        private int _mainColorIndex = 0; // Red by default
        private int _accentColorIndex = 4; // Blue by default

        // Smoothed FFT values to prevent jitter
        private float _smoothedMid = 0f;
        private float _smoothedTreble = 0f;
        private float _smoothedPeak = 0f;

        // Beat-driven rotation with inertia and direction flip
        private float _musicAccumulatedRotation = 0f;
        private float _rotationVelocity = 0f; // Current spin velocity with inertia

        // Zoom punch effect
        private float _zoomPunch = 0f; // Current zoom punch value with decay
        private float _spinDirection = 1f; // 1 or -1, flips every other beat
        private int _beatCounter = 0; // Count beats for alternating direction
        private readonly System.Diagnostics.Stopwatch _stopwatch = System.Diagnostics.Stopwatch.StartNew();
        private double _lastUpdateTime = 0;

        // Mouse parallax effect
        private float _parallaxOffsetX = 0f;
        private float _parallaxOffsetY = 0f;
        private float _parallaxStrength = 0.15f; // How much the mouse affects offset (0-1)

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

        public void SetZoomPunch(float zoom)
        {
            _zoomPunch = Math.Clamp(zoom, 0.0f, 2.0f);
        }

        public void SetMelodySaturation(float saturation)
        {
            _smoothedMelodySaturation = Math.Clamp(saturation, 0.0f, 1.0f);
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
            // Smooth the parallax movement
            const float smoothing = 0.85f;
            _parallaxOffsetX = _parallaxOffsetX * smoothing + (normalizedX * _parallaxStrength) * (1f - smoothing);
            _parallaxOffsetY = _parallaxOffsetY * smoothing + (normalizedY * _parallaxStrength) * (1f - smoothing);
        }

        public void SetBaseTimeSpeed(float speed)
        {
            _baseTimeSpeed = Math.Clamp(speed, 0.0f, 3.0f);
        }

        // Audio source mapping methods
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

        public void SetMainColor(int colorIndex)
        {
            _mainColorIndex = Math.Clamp(colorIndex, 0, 7);
            UpdateThemeColors();
        }

        public void SetAccentColor(int colorIndex)
        {
            _accentColorIndex = Math.Clamp(colorIndex, 0, 7);
            UpdateThemeColors();
        }

        // Get audio intensity from VibeAudioManager for a given source
        private float GetAudioIntensity(AudioSource source)
        {
            try
            {
                var audioManager = Helpers.ServiceHelper.GetService<Services.VibeAudioManager>();
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

        // Map color index (0-7) to RGB float array
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
                uniform float parallax_x;
                uniform float parallax_y;
                uniform float zoom_scale;
                uniform float melody_saturation;

                const float PIXEL_SIZE_FAC = 1440.0;
                const float SPIN_EASE = 0.5;

                // Helper function to convert RGB to HSV
                float3 rgb2hsv(float3 c) {
                    float4 K = float4(0.0, -1.0 / 3.0, 2.0 / 3.0, -1.0);
                    float4 p = mix(float4(c.bg, K.wz), float4(c.gb, K.xy), step(c.b, c.g));
                    float4 q = mix(float4(p.xyw, c.r), float4(c.r, p.yzx), step(p.x, c.r));

                    float d = q.x - min(q.w, q.y);
                    float e = 1.0e-10;
                    return float3(abs(q.z + (q.w - q.y) / (6.0 * d + e)), d / (q.x + e), q.x);
                }

                // Helper function to convert HSV to RGB
                float3 hsv2rgb(float3 c) {
                    float4 K = float4(1.0, 2.0 / 3.0, 1.0 / 3.0, 3.0);
                    float3 p = abs(fract(c.xxx + K.xyz) * 6.0 - K.www);
                    return c.z * mix(K.xxx, clamp(p - K.xxx, 0.0, 1.0), c.y);
                }

                float4 main(float2 screen_coords) {
                    // Convert to UV coords (0-1) and floor for pixel effect
                    float pixel_size = length(resolution) / PIXEL_SIZE_FAC;
                    float2 uv = (floor(screen_coords * (1.0 / pixel_size)) * pixel_size - 0.5 * resolution) / length(resolution) - float2(parallax_x, parallax_y);
                    float uv_len = length(uv);

                    // Center swirl that changes with time
                    float speed = (spin_time * SPIN_EASE * 0.2) + 302.2;
                    float new_pixel_angle = atan(uv.y, uv.x) + speed - SPIN_EASE * 20.0 * (1.0 * spin_amount * uv_len + (1.0 - 1.0 * spin_amount));
                    float2 mid = (resolution / length(resolution)) / 2.0;
                    uv = float2((uv_len * cos(new_pixel_angle) + mid.x), (uv_len * sin(new_pixel_angle) + mid.y)) - mid;

                    // Paint effect - zoom punch scale
                    uv *= (30.0 + zoom_scale);
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

                    // Apply melody-driven saturation boost to colour_1 SMOOTHLY
                    float4 adjusted_colour_1 = colour_1;
                    if (melody_saturation > 0.01) {
                        float3 hsv = rgb2hsv(colour_1.rgb);
                        // Boost saturation gently (0.0-0.3 range max, smoothed)
                        float satBoost = melody_saturation * 0.3;
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

            // For VibeOut theme, use the color pickers
            if (_theme == BalatroShaderBackground.BackgroundTheme.VibeOut)
            {
                var mainColor = GetColorFromIndex(_mainColorIndex);
                var accentColor = GetColorFromIndex(_accentColorIndex);
                var backgroundColor = new float[] { 0.01f, 0.01f, 0.01f, 1.0f }; // Dark background

                _shaderBuilder.Uniforms["colour_1"] = mainColor;
                _shaderBuilder.Uniforms["colour_2"] = accentColor;
                _shaderBuilder.Uniforms["colour_3"] = backgroundColor;
            }
            else
            {
                // Use predefined theme colors
                var (c1, c2, c3) = GetThemeColors(_theme);
                _shaderBuilder.Uniforms["colour_1"] = c1;
                _shaderBuilder.Uniforms["colour_2"] = c2;
                _shaderBuilder.Uniforms["colour_3"] = c3;
            }
        }

        private (float[], float[], float[]) GetThemeColors(BalatroShaderBackground.BackgroundTheme theme)
        {
            return theme switch
            {
                // Default: Balatro Red (#ff4c40), Blue (#0093ff), Dark Teal (#1e2b2d)
                BalatroShaderBackground.BackgroundTheme.Default => (
                    new float[] { 1.0f, 0.298f, 0.251f, 1.0f },    // Red
                    new float[] { 0.0f, 0.576f, 1.0f, 1.0f },       // Blue
                    new float[] { 0.118f, 0.169f, 0.176f, 1.0f }    // Dark Teal
                ),
                // Wave Rider: Ocean blues - Blue (#0093ff), Pale Green (#56a887), Dark Blue (#004d7a)
                BalatroShaderBackground.BackgroundTheme.Plasma => (
                    new float[] { 0.0f, 0.576f, 1.0f, 1.0f },       // Blue
                    new float[] { 0.337f, 0.659f, 0.529f, 1.0f },   // Pale Green
                    new float[] { 0.0f, 0.302f, 0.478f, 1.0f }      // Dark Blue
                ),
                // Inferno: Hot reds/oranges - Red (#ff4c40), Orange (#ff9800), Dark Red (#8b1538)
                BalatroShaderBackground.BackgroundTheme.Ocean => (
                    new float[] { 1.0f, 0.298f, 0.251f, 1.0f },     // Red
                    new float[] { 1.0f, 0.596f, 0.0f, 1.0f },       // Orange
                    new float[] { 0.545f, 0.082f, 0.220f, 1.0f }    // Dark Red
                ),
                // Frozen: Cool purples/blues - Purple (#525db0), Blue (#0093ff), Dark (#1e2b2d)
                BalatroShaderBackground.BackgroundTheme.Sunset => (
                    new float[] { 0.322f, 0.365f, 0.690f, 1.0f },   // Purple
                    new float[] { 0.0f, 0.576f, 1.0f, 1.0f },       // Blue
                    new float[] { 0.118f, 0.169f, 0.176f, 1.0f }    // Dark Teal
                ),
                // Rainbow Cascade: Vibrant mix - Orange (#ff9800), Green (#429f79), Purple (#525db0)
                BalatroShaderBackground.BackgroundTheme.Midnight => (
                    new float[] { 1.0f, 0.596f, 0.0f, 1.0f },       // Orange
                    new float[] { 0.259f, 0.624f, 0.475f, 1.0f },   // Green
                    new float[] { 0.322f, 0.365f, 0.690f, 1.0f }    // Purple
                ),
                // Electric Storm: Purple/gold - Purple (#525db0), Gold (#eac058), Dark Grey (#1a1a1a)
                BalatroShaderBackground.BackgroundTheme.Sepia => (
                    new float[] { 0.322f, 0.365f, 0.690f, 1.0f },   // Purple
                    new float[] { 0.918f, 0.753f, 0.345f, 1.0f },   // Gold
                    new float[] { 0.102f, 0.102f, 0.102f, 1.0f }    // Dark Grey
                ),
                // Sakura Dream: Soft pink/purple - Light Purple (#B19CD9), Pale Green (#56a887), Light Grey (#E2E2E3)
                BalatroShaderBackground.BackgroundTheme.Dynamic => (
                    new float[] { 0.694f, 0.612f, 0.851f, 1.0f },   // Light Purple
                    new float[] { 0.337f, 0.659f, 0.529f, 1.0f },   // Pale Green
                    new float[] { 0.886f, 0.886f, 0.890f, 1.0f }    // Light Grey
                ),
                // Lunar Eclipse: Deep space - Orange (#ff9800), Purple (#525db0), Pure Black (#000000)
                BalatroShaderBackground.BackgroundTheme.VibeOut => (
                    new float[] { 1.0f, 0.596f, 0.0f, 1.0f },       // Orange
                    new float[] { 0.322f, 0.365f, 0.690f, 1.0f },   // Purple
                    new float[] { 0.0f, 0.0f, 0.0f, 1.0f }          // Pure Black
                ),
                _ => (
                    new float[] { 1.0f, 0.298f, 0.251f, 1.0f },     // Red (fallback)
                    new float[] { 0.0f, 0.576f, 1.0f, 1.0f },       // Blue
                    new float[] { 0.118f, 0.169f, 0.176f, 1.0f }    // Dark Teal
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

                // Get audio intensities for each mapped parameter (0-1 range)
                float shadowFlickerIntensity = GetAudioIntensity(_shadowFlickerSource);
                float spinIntensity = GetAudioIntensity(_spinSource);
                float twirlIntensity = GetAudioIntensity(_twirlSource);
                float zoomThumpIntensity = GetAudioIntensity(_zoomThumpSource);
                float beatPulseIntensity = GetAudioIntensity(_beatPulseSource);

                // Update beat pulse from mapped audio source
                _beatPulse = Math.Clamp(beatPulseIntensity, 0f, 1f);

                // Flip spin direction every other beat for back-and-forth effect
                if (_beatPulse > 0.3f) // Only count significant beats
                {
                    _beatCounter++;
                    if (_beatCounter % 2 == 0)
                    {
                        _spinDirection *= -1f; // Reverse direction every other beat
                    }
                }

                // Map audio intensities to appropriate shader parameter ranges
                // Use global intensity multiplier for user control
                float intensityScale = _audioReactivityIntensity;

                // ShadowFlicker affects contrast: base 2.0, audio can add 0-0.5 (reduced from 3.0)
                float audioContrast = _contrast + (shadowFlickerIntensity * 0.5f * intensityScale);

                // Spin affects spin_amount: base 0.3, audio can add 0-0.15 (reduced from 0.7)
                float audioSpinAmount = _spinAmount + (spinIntensity * 0.15f * intensityScale);

                // Twirl affects ADDITIONAL speed on top of base: audio can add 0-0.3x (reduced from 2.0)
                float audioSpeedBoost = (twirlIntensity * 0.3f * intensityScale);

                // ZoomThump creates punch effect - sudden zoom that decays
                // When intensity spikes, add to zoom punch (range: 0-10 for noticeable but not extreme effect)
                if (zoomThumpIntensity > 0.5f) // Only punch on strong beats
                {
                    float punchStrength = (zoomThumpIntensity - 0.5f) * 2.0f; // 0-1 range for strong beats
                    _zoomPunch += punchStrength * 10.0f * intensityScale; // Add to existing zoom
                }

                // Decay zoom punch exponentially (faster decay = snappier punch)
                _zoomPunch *= 0.85f; // 15% decay per frame (~60fps = quick decay)

                // Get saturation intensity from mapped audio source
                float saturationIntensity = GetAudioIntensity(_colorSaturationSource);

                // Smooth saturation heavily to prevent flickering (95% previous, 5% new)
                float targetSaturation = saturationIntensity * intensityScale;
                _smoothedMelodySaturation = _smoothedMelodySaturation * 0.95f + targetSaturation * 0.05f;

                // Total speed = base time speed + audio boost
                float totalSpeed = _baseTimeSpeed + audioSpeedBoost;

                // Update uniforms (this is fast, no allocation)
                _shaderBuilder.Uniforms["time"] = adjustedTime * totalSpeed;
                _shaderBuilder.Uniforms["spin_time"] = adjustedTime * totalSpeed;
                _shaderBuilder.Uniforms["resolution"] = currentSize;
                _shaderBuilder.Uniforms["contrast"] = Math.Clamp(audioContrast, 0.5f, 8.0f);
                _shaderBuilder.Uniforms["spin_amount"] = Math.Clamp(audioSpinAmount, 0.0f, 1.0f);
                _shaderBuilder.Uniforms["parallax_x"] = _parallaxOffsetX;
                _shaderBuilder.Uniforms["parallax_y"] = _parallaxOffsetY;
                _shaderBuilder.Uniforms["zoom_scale"] = _zoomPunch; // Zoom punch effect
                _shaderBuilder.Uniforms["melody_saturation"] = Math.Clamp(_smoothedMelodySaturation, 0.0f, 1.0f); // Melody-driven saturation

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
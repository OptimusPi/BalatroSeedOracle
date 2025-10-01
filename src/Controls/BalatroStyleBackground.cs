using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Media;
using Avalonia.Rendering;
using Avalonia.Rendering.SceneGraph;
using Avalonia.Skia;
using Avalonia.Threading;
using SkiaSharp;

namespace BalatroSeedOracle.Controls
{
    public class BalatroStyleBackground : Control
    {
        public enum BackgroundTheme
        {
            Default, // Red/Blue
            Plasma, // Purple/Green
            Sepia, // Brown/Tan
            Ocean, // Blue/Teal
            Sunset, // Orange/Purple
            Midnight, // Dark Blue/Purple
            Forest, // Green/Brown
            Cherry, // Pink/Red
            Gold, // Gold/Black
            Monochrome, // Gray scale
            Dynamic, // Dynamic hue-shifted colors
            VibeOut, // Music-reactive mode
        }

        private BackgroundTheme _currentTheme = BackgroundTheme.Default;
        private float _contrast = 1.0f;
        private float _spinAmount = 0.5f;
        private bool _isAnimating = true;
        private float _currentHue = 0.0f;
        private float _targetHue = 0.0f;
        private int _seedCount = 0;
        
        // Music visualizer properties
        private float _vibeIntensity = 0f;
        private float _beatPulse = 0f;

        public new BackgroundTheme Theme
        {
            get => _currentTheme;
            set
            {
                _currentTheme = value;
                InvalidateVisual();
            }
        }

        public float Contrast
        {
            get => _contrast;
            set
            {
                _contrast = Math.Clamp(value, 0.1f, 5.0f);
                InvalidateVisual();
            }
        }

        public float SpinAmount
        {
            get => _spinAmount;
            set
            {
                _spinAmount = Math.Clamp(value, 0.0f, 1.0f);
                InvalidateVisual();
            }
        }

        public new bool IsAnimating
        {
            get => _isAnimating;
            set
            {
                if (_isAnimating != value)
                {
                    _isAnimating = value;
                    if (_isAnimating)
                    {
                        StartAnimation();
                    }
                    else
                    {
                        StopAnimation();
                    }
                }
            }
        }

        public BalatroStyleBackground()
        {
            // Use proper Avalonia rendering integration
            this.AttachedToVisualTree += (s, e) => StartAnimation();
            this.DetachedFromVisualTree += (s, e) => StopAnimation();
        }

        private void StartAnimation()
        {
            // Self-invalidating render cycle
            if (_isAnimating)
            {
                InvalidateVisual();
            }
        }

        private void StopAnimation()
        {
            // No timer to stop - animation controlled by _isAnimating flag
        }

        /// <summary>
        /// Call this when a new seed is found to shift the background hue
        /// </summary>
        public void OnSeedFound()
        {
            _seedCount++;

            // Set theme to Dynamic if not already
            if (_currentTheme != BackgroundTheme.Dynamic && _currentTheme != BackgroundTheme.VibeOut)
            {
                _currentTheme = BackgroundTheme.Dynamic;
            }

            // Shift hue by a golden ratio-based amount for nice distribution
            const float goldenAngle = 137.5f; // Golden angle in degrees
            _targetHue = (_targetHue + goldenAngle) % 360.0f;

            // Ensure we're animating to see the smooth transition
            if (!_isAnimating)
            {
                IsAnimating = true;
            }
        }
        
        /// <summary>
        /// Enter VIBE OUT mode with music visualization
        /// </summary>
        public void EnterVibeOutMode()
        {
            Theme = BackgroundTheme.VibeOut;
            IsAnimating = true;
        }
        
        /// <summary>
        /// Exit VIBE OUT mode
        /// </summary>
        public void ExitVibeOutMode()
        {
            Theme = BackgroundTheme.Default;
            _beatPulse = 0f;
            _vibeIntensity = 0f;
        }
        
        /// <summary>
        /// Update vibe intensity for music-reactive effects
        /// </summary>
        public void UpdateVibeIntensity(float intensity)
        {
            _vibeIntensity = Math.Clamp(intensity, 0f, 1f);
            
            if (_currentTheme == BackgroundTheme.VibeOut)
            {
                // Vibe intensity affects spin and contrast
                SpinAmount = Math.Clamp(0.3f + _vibeIntensity * 0.7f, 0f, 1f);
                Contrast = Math.Clamp(1f + _vibeIntensity * 1.5f, 0.5f, 3f);
                
                // Higher intensity = faster hue shifting
                _targetHue = (_targetHue + _vibeIntensity * 10f) % 360f;
            }
        }
        
        /// <summary>
        /// Trigger beat pulse effect
        /// </summary>
        public void OnBeatDetected(float intensity)
        {
            _beatPulse = Math.Clamp(intensity, 0f, 1f);
            InvalidateVisual();
        }

        public float CurrentHue => _currentHue;
        public float TargetHue => _targetHue;

        public override void Render(DrawingContext context)
        {
            // Smoothly interpolate hue
            if (Math.Abs(_currentHue - _targetHue) > 0.1f)
            {
                float diff = _targetHue - _currentHue;

                // Handle wrapping around 360 degrees
                if (diff > 180)
                {
                    diff -= 360;
                }

                if (diff < -180)
                {
                    diff += 360;
                }

                _currentHue += diff * 0.1f; // Smooth interpolation

                // Normalize to 0-360
                if (_currentHue < 0)
                {
                    _currentHue += 360;
                }

                if (_currentHue >= 360)
                {
                    _currentHue -= 360;
                }
            }
            
            // Decay beat pulse
            _beatPulse *= 0.95f;

            // YOUR SKIACONTROL PATTERN - Self-invalidating render cycle
            Dispatcher.UIThread.InvokeAsync(InvalidateVisual, DispatcherPriority.Background);

            context.Custom(
                new BalatroStyleBackgroundDrawOp(
                    new Rect(0, 0, Bounds.Width, Bounds.Height),
                    _currentTheme,
                    _contrast,
                    _spinAmount,
                    _isAnimating,
                    _currentHue,
                    _vibeIntensity,
                    _beatPulse
                )
            );
        }
    }

    internal sealed class BalatroStyleBackgroundDrawOp : ICustomDrawOperation
    {
        private SKRuntimeShaderBuilder? _shaderBuilder;
        private readonly BalatroStyleBackground.BackgroundTheme _theme;
        private readonly float _contrast;
        private readonly float _spinAmount;
        private readonly bool _isAnimating;
        private readonly float _hue;
        private readonly float _vibeIntensity;
        private readonly float _beatPulse;
        private static float _lastTime = Environment.TickCount / 1000.0f;

        public BalatroStyleBackgroundDrawOp(
            Rect bounds,
            BalatroStyleBackground.BackgroundTheme theme,
            float contrast,
            float spinAmount,
            bool isAnimating,
            float hue = 0.0f,
            float vibeIntensity = 0f,
            float beatPulse = 0f
        )
        {
            Bounds = bounds;
            _theme = theme;
            _contrast = contrast;
            _spinAmount = spinAmount;
            _isAnimating = isAnimating;
            _hue = hue;
            _vibeIntensity = vibeIntensity;
            _beatPulse = beatPulse;
            InitShader();
        }

        private void InitShader()
        {
            // Enhanced Balatro shader with music visualization
            var sksl =
                @"
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

                const float PIXEL_SIZE_FAC = 700.0;
                const float SPIN_EASE = 0.5;

                float4 main(float2 screen_coords) {
                    // Convert to UV coords (0-1) and floor for pixel effect
                    float pixel_size = length(resolution) / PIXEL_SIZE_FAC;
                    float2 uv = (floor(screen_coords * (1.0 / pixel_size)) * pixel_size - 0.5 * resolution) / length(resolution);
                    float uv_len = length(uv);

                    // MUSIC VISUALIZATION: Modulate effects based on vibe
                    float music_spin_boost = vibe_intensity * 2.0;
                    float music_contrast_boost = vibe_intensity * 0.5;
                    float beat_effect = beat_pulse * 3.0;

                    // Adding in a center swirl, changes with time AND music
                    float speed = (spin_time * SPIN_EASE * 0.2) + 302.2 + music_spin_boost;
                    float new_pixel_angle = atan(uv.y, uv.x) + speed - SPIN_EASE * 20.0 * (1.0 * (spin_amount + music_spin_boost * 0.3) * uv_len + (1.0 - 1.0 * (spin_amount + music_spin_boost * 0.3)));
                    float2 mid = (resolution / length(resolution)) / 2.0;
                    uv = float2((uv_len * cos(new_pixel_angle) + mid.x), (uv_len * sin(new_pixel_angle) + mid.y)) - mid;

                    // BEAT PULSE: Scale UV based on beat detection
                    uv *= 30.0 * (1.0 + beat_effect * 0.1);
                    
                    // BEAUTIFUL PAINT EFFECT - Optimized but keeps visual quality
                    speed = time * 1.5 + vibe_intensity * 2.0;
                    float2 uv2 = float2(uv.x + uv.y);

                    // Keep 5 iterations for authentic fractal beauty - optimize the math instead
                    for (int i = 0; i < 5; i++) {
                        // Optimized: combine operations and reduce precision where possible
                        float sin_val = sin(max(uv.x, uv.y));
                        uv2 += sin_val + uv;
                        
                        // Pre-calculate speed factors
                        float speed_factor = speed * (0.131121 + vibe_intensity * 0.2);
                        uv += 0.5 * float2(
                            cos(5.1123314 + 0.353 * uv2.y + speed_factor), 
                            sin(uv2.x - 0.113 * speed_factor)
                        );
                        uv -= cos(uv.x + uv.y) - sin(uv.x * 0.711 - uv.y);
                    }

                    // Make the paint amount range from 0 - 2 with music modulation
                    float contrast_mod = (0.25 * (contrast + music_contrast_boost) + 0.5 * spin_amount + 1.2);
                    float paint_res = min(8.0, max(0.0, length(uv) * 0.035 * contrast_mod));
                    float c1p = max(0.0, 1.0 - contrast_mod * abs(1.0 - paint_res));
                    float c2p = max(0.0, 1.0 - contrast_mod * abs(paint_res));
                    float c3p = 1.0 - min(1.0, c1p + c2p);

                    // MUSIC VISUALIZATION: Color mixing based on vibe intensity
                    float4 base_col = (0.3 / contrast) * colour_1 + (1.0 - 0.3 / contrast) * (colour_1 * c1p + colour_2 * c2p + float4(c3p * colour_3.rgb, c3p * colour_1.a));
                    
                    // Vibe intensity adds color energy
                    base_col.r += vibe_intensity * 0.3;
                    base_col.g += vibe_intensity * 0.2;
                    base_col.b += vibe_intensity * 0.4;
                    
                    // Beat pulse creates overall brightness boost
                    base_col.rgb *= (1.0 + beat_pulse * 0.5);
                    
                    // Clamp to valid range
                    base_col = clamp(base_col, 0.0, 1.0);

                    return base_col;
                }
";

            var effect = SKRuntimeEffect.CreateShader(sksl, out var errorStr);
            if (effect != null)
            {
                _shaderBuilder = new SKRuntimeShaderBuilder(effect);

                // Set colors based on theme
                var (color1, color2, color3) = GetThemeColors(_theme);
                _shaderBuilder.Uniforms["colour_1"] = color1;
                _shaderBuilder.Uniforms["colour_2"] = color2;
                _shaderBuilder.Uniforms["colour_3"] = color3;

                // Apply user settings
                _shaderBuilder.Uniforms["contrast"] = _contrast;
                _shaderBuilder.Uniforms["spin_amount"] = _spinAmount;
                
                // Music visualization uniforms
                _shaderBuilder.Uniforms["vibe_intensity"] = _vibeIntensity;
                _shaderBuilder.Uniforms["beat_pulse"] = _beatPulse;
            }
            else if (!string.IsNullOrEmpty(errorStr))
            {
                System.Diagnostics.Debug.WriteLine($"Shader compilation error: {errorStr}");
            }
        }

        public Rect Bounds { get; }

        public void Dispose() { }

        public bool Equals(ICustomDrawOperation? other)
        {
            return false;
        }

        public bool HitTest(Point p)
        {
            return false;
        }

        public void Render(ImmediateDrawingContext context)
        {
            if (
                context.TryGetFeature(typeof(ISkiaSharpApiLeaseFeature))
                is ISkiaSharpApiLeaseFeature leaseFeature
            )
            {
                using var lease = leaseFeature.Lease();
                var canvas = lease.SkCanvas;
                canvas.Save();

                if (_shaderBuilder != null)
                {
                    // Update time-based uniforms to match Balatro
                    float time;
                    if (_isAnimating)
                    {
                        time = Environment.TickCount / 1000.0f;
                        _lastTime = time;
                    }
                    else
                    {
                        // Use the last time when animation was active
                        time = _lastTime;
                    }

                    _shaderBuilder.Uniforms["time"] = time;
                    _shaderBuilder.Uniforms["spin_time"] = time; // Balatro uses separate spin_time
                    _shaderBuilder.Uniforms["resolution"] = new SKSize(
                        (float)Bounds.Width,
                        (float)Bounds.Height
                    );

                    // Create the shader from the builder
                    var shader = _shaderBuilder.Build();

                    using var paint = new SKPaint { Shader = shader };

                    // Fill the entire bounds with the shader
                    var rect = new SKRect(0, 0, (float)Bounds.Width, (float)Bounds.Height);
                    canvas.DrawRect(rect, paint);
                }

                canvas.Restore();
            }
        }

        private static (float r, float g, float b) HsvToRgb(float h, float s, float v)
        {
            h = h / 60.0f;
            float c = v * s;
            float x = c * (1 - Math.Abs((h % 2) - 1));
            float m = v - c;

            float r = 0,
                g = 0,
                b = 0;
            if (h < 1)
            {
                r = c;
                g = x;
                b = 0;
            }
            else if (h < 2)
            {
                r = x;
                g = c;
                b = 0;
            }
            else if (h < 3)
            {
                r = 0;
                g = c;
                b = x;
            }
            else if (h < 4)
            {
                r = 0;
                g = x;
                b = c;
            }
            else if (h < 5)
            {
                r = x;
                g = 0;
                b = c;
            }
            else
            {
                r = c;
                g = 0;
                b = x;
            }

            return (r + m, g + m, b + m);
        }

        private (float[], float[], float[]) GetThemeColors(
            BalatroStyleBackground.BackgroundTheme theme
        )
        {
            if (theme == BalatroStyleBackground.BackgroundTheme.Dynamic || theme == BalatroStyleBackground.BackgroundTheme.VibeOut)
            {
                // Use the current hue to generate colors
                // Primary color: full saturation and value
                var (r1, g1, b1) = HsvToRgb(_hue, 0.8f, 0.9f);

                // Secondary color: complementary hue (180 degrees offset)
                var (r2, g2, b2) = HsvToRgb((_hue + 180) % 360, 0.7f, 0.8f);

                // Background: darker version of primary
                var (r3, g3, b3) = HsvToRgb(_hue, 0.3f, 0.2f);

                return (
                    new float[] { r1, g1, b1, 1.0f },
                    new float[] { r2, g2, b2, 1.0f },
                    new float[] { r3, g3, b3, 1.0f }
                );
            }

            return theme switch
            {
                BalatroStyleBackground.BackgroundTheme.Default => (
                    new float[] { 0.871f, 0.267f, 0.231f, 1.0f }, // Red (original Balatro red)
                    new float[] { 0.0f, 0.42f, 0.706f, 1.0f }, // Blue (original Balatro blue)
                    new float[] { 0.086f, 0.137f, 0.145f, 1.0f } // Dark gray background
                ),

                BalatroStyleBackground.BackgroundTheme.Plasma => (
                    new float[] { 0.6f, 0.2f, 0.8f, 1.0f }, // Purple
                    new float[] { 0.2f, 0.8f, 0.4f, 1.0f }, // Green
                    new float[] { 0.1f, 0.05f, 0.15f, 1.0f } // Dark purple
                ),

                BalatroStyleBackground.BackgroundTheme.Sepia => (
                    new float[] { 0.7f, 0.5f, 0.3f, 1.0f }, // Brown
                    new float[] { 0.9f, 0.8f, 0.6f, 1.0f }, // Tan
                    new float[] { 0.2f, 0.15f, 0.1f, 1.0f } // Dark brown
                ),

                BalatroStyleBackground.BackgroundTheme.Ocean => (
                    new float[] { 0.0f, 0.4f, 0.6f, 1.0f }, // Deep blue
                    new float[] { 0.0f, 0.7f, 0.8f, 1.0f }, // Teal
                    new float[] { 0.0f, 0.1f, 0.2f, 1.0f } // Dark blue
                ),

                BalatroStyleBackground.BackgroundTheme.Sunset => (
                    new float[] { 1.0f, 0.4f, 0.2f, 1.0f }, // Orange
                    new float[] { 0.8f, 0.2f, 0.6f, 1.0f }, // Purple
                    new float[] { 0.2f, 0.05f, 0.1f, 1.0f } // Dark red
                ),

                BalatroStyleBackground.BackgroundTheme.Midnight => (
                    new float[] { 0.1f, 0.1f, 0.3f, 1.0f }, // Dark blue
                    new float[] { 0.3f, 0.1f, 0.5f, 1.0f }, // Purple
                    new float[] { 0.05f, 0.05f, 0.1f, 1.0f } // Very dark blue
                ),

                BalatroStyleBackground.BackgroundTheme.Forest => (
                    new float[] { 0.2f, 0.6f, 0.2f, 1.0f }, // Green
                    new float[] { 0.5f, 0.4f, 0.2f, 1.0f }, // Brown
                    new float[] { 0.1f, 0.15f, 0.1f, 1.0f } // Dark green
                ),

                BalatroStyleBackground.BackgroundTheme.Cherry => (
                    new float[] { 0.9f, 0.3f, 0.5f, 1.0f }, // Pink
                    new float[] { 0.8f, 0.1f, 0.3f, 1.0f }, // Red
                    new float[] { 0.2f, 0.05f, 0.1f, 1.0f } // Dark red
                ),

                BalatroStyleBackground.BackgroundTheme.Gold => (
                    new float[] { 1.0f, 0.843f, 0.0f, 1.0f }, // Gold
                    new float[] { 0.8f, 0.6f, 0.0f, 1.0f }, // Dark gold
                    new float[] { 0.1f, 0.1f, 0.1f, 1.0f } // Black
                ),

                BalatroStyleBackground.BackgroundTheme.Monochrome => (
                    new float[] { 0.7f, 0.7f, 0.7f, 1.0f }, // Light gray
                    new float[] { 0.3f, 0.3f, 0.3f, 1.0f }, // Dark gray
                    new float[] { 0.1f, 0.1f, 0.1f, 1.0f } // Very dark gray
                ),

                _ => ( // Default fallback
                    new float[] { 0.871f, 0.267f, 0.231f, 1.0f },
                    new float[] { 0.0f, 0.42f, 0.706f, 1.0f },
                    new float[] { 0.086f, 0.137f, 0.145f, 1.0f }
                ),
            };
        }
    }
}

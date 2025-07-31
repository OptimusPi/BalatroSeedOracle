using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Rendering.SceneGraph;
using Avalonia.Skia;
using Avalonia.Threading;
using SkiaSharp;
using System;

namespace Oracle.Controls
{
    public class BalatroStyleBackground : Control
    {
        public enum BackgroundTheme
        {
            Default,      // Red/Blue
            Plasma,       // Purple/Green
            Sepia,        // Brown/Tan
            Ocean,        // Blue/Teal
            Sunset,       // Orange/Purple
            Midnight,     // Dark Blue/Purple
            Forest,       // Green/Brown
            Cherry,       // Pink/Red
            Gold,         // Gold/Black
            Monochrome    // Gray scale
        }
        
        private BackgroundTheme _currentTheme = BackgroundTheme.Default;
        private float _contrast = 1.0f;
        private float _spinAmount = 0.5f;
        private bool _isAnimating = true;
        private DispatcherTimer? _animationTimer;
        
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
            // Initialize animation timer
            _animationTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromMilliseconds(33) // ~30 FPS
            };
            _animationTimer.Tick += (s, e) => InvalidateVisual();
            
            if (_isAnimating)
            {
                StartAnimation();
            }
        }
        
        private void StartAnimation()
        {
            _animationTimer?.Start();
        }
        
        private void StopAnimation()
        {
            _animationTimer?.Stop();
        }
        
        public override void Render(DrawingContext context)
        {
            context.Custom(new BalatroStyleBackgroundDrawOp(new Rect(0, 0, Bounds.Width, Bounds.Height), _currentTheme, _contrast, _spinAmount, _isAnimating));
            
            // We no longer need this as we're using a timer for animation
            // Dispatcher.UIThread.InvokeAsync(InvalidateVisual, DispatcherPriority.Background);
        }
    }

    internal class BalatroStyleBackgroundDrawOp : ICustomDrawOperation
    {
        private SKRuntimeShaderBuilder? _shaderBuilder;
        private readonly BalatroStyleBackground.BackgroundTheme _theme;
        private readonly float _contrast;
        private readonly float _spinAmount;
        private readonly bool _isAnimating;
        private static float _lastTime = Environment.TickCount / 1000.0f;

        public BalatroStyleBackgroundDrawOp(Rect bounds, BalatroStyleBackground.BackgroundTheme theme, float contrast, float spinAmount, bool isAnimating)
        {
            Bounds = bounds;
            _theme = theme;
            _contrast = contrast;
            _spinAmount = spinAmount;
            _isAnimating = isAnimating;
            InitShader();
        }

        private void InitShader()
        {
            // Direct conversion from Balatro's background.fs shader
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
                    float2 uv = (floor(screen_coords * (1.0 / pixel_size)) * pixel_size - 0.5 * resolution) / length(resolution);
                    float uv_len = length(uv);

                    // Adding in a center swirl, changes with time. Only applies meaningfully if the 'spin amount' is a non-zero number
                    float speed = (spin_time * SPIN_EASE * 0.2) + 302.2;
                    float new_pixel_angle = atan(uv.y, uv.x) + speed - SPIN_EASE * 20.0 * (1.0 * spin_amount * uv_len + (1.0 - 1.0 * spin_amount));
                    float2 mid = (resolution / length(resolution)) / 2.0;
                    uv = float2((uv_len * cos(new_pixel_angle) + mid.x), (uv_len * sin(new_pixel_angle) + mid.y)) - mid;

                    // Now add the paint effect to the swirled UV
                    uv *= 30.0;
                    speed = time * 2.0;
                    float2 uv2 = float2(uv.x + uv.y);

                    for (int i = 0; i < 5; i++) {
                        uv2 += sin(max(uv.x, uv.y)) + uv;
                        uv += 0.5 * float2(cos(5.1123314 + 0.353 * uv2.y + speed * 0.131121), sin(uv2.x - 0.113 * speed));
                        uv -= 1.0 * cos(uv.x + uv.y) - 1.0 * sin(uv.x * 0.711 - uv.y);
                    }

                    // Make the paint amount range from 0 - 2
                    float contrast_mod = (0.25 * contrast + 0.5 * spin_amount + 1.2);
                    float paint_res = min(8.0, max(0.0, length(uv) * 0.035 * contrast_mod));
                    float c1p = max(0.0, 1.0 - contrast_mod * abs(1.0 - paint_res));
                    float c2p = max(0.0, 1.0 - contrast_mod * abs(paint_res));
                    float c3p = 1.0 - min(1.0, c1p + c2p);

                    float4 ret_col = (0.3 / contrast) * colour_1 + (1.0 - 0.3 / contrast) * (colour_1 * c1p + colour_2 * c2p + float4(c3p * colour_3.rgb, c3p * colour_1.a));

                    return ret_col;
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
            }
            else if (!string.IsNullOrEmpty(errorStr))
            {
                System.Diagnostics.Debug.WriteLine($"Shader compilation error: {errorStr}");
            }
        }

        public Rect Bounds { get; }

        public void Dispose() { }
        public bool Equals(ICustomDrawOperation? other) => false;
        public bool HitTest(Point p) => false;

        public void Render(ImmediateDrawingContext context)
        {
            if (context.TryGetFeature(typeof(ISkiaSharpApiLeaseFeature)) is ISkiaSharpApiLeaseFeature leaseFeature)
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
                    _shaderBuilder.Uniforms["spin_time"] = time;  // Balatro uses separate spin_time
                    _shaderBuilder.Uniforms["resolution"] = new SKSize((float)Bounds.Width, (float)Bounds.Height);
                    
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
        
        private (float[], float[], float[]) GetThemeColors(BalatroStyleBackground.BackgroundTheme theme)
        {
            return theme switch
            {
                BalatroStyleBackground.BackgroundTheme.Default => (
                    new float[] { 0.871f, 0.267f, 0.231f, 1.0f }, // Red (original Balatro red)
                    new float[] { 0.0f, 0.42f, 0.706f, 1.0f },    // Blue (original Balatro blue)
                    new float[] { 0.086f, 0.137f, 0.145f, 1.0f }  // Dark gray background
                ),
                
                BalatroStyleBackground.BackgroundTheme.Plasma => (
                    new float[] { 0.6f, 0.2f, 0.8f, 1.0f },       // Purple
                    new float[] { 0.2f, 0.8f, 0.4f, 1.0f },       // Green
                    new float[] { 0.1f, 0.05f, 0.15f, 1.0f }      // Dark purple
                ),
                
                BalatroStyleBackground.BackgroundTheme.Sepia => (
                    new float[] { 0.7f, 0.5f, 0.3f, 1.0f },       // Brown
                    new float[] { 0.9f, 0.8f, 0.6f, 1.0f },       // Tan
                    new float[] { 0.2f, 0.15f, 0.1f, 1.0f }       // Dark brown
                ),
                
                BalatroStyleBackground.BackgroundTheme.Ocean => (
                    new float[] { 0.0f, 0.4f, 0.6f, 1.0f },       // Deep blue
                    new float[] { 0.0f, 0.7f, 0.8f, 1.0f },       // Teal
                    new float[] { 0.0f, 0.1f, 0.2f, 1.0f }        // Dark blue
                ),
                
                BalatroStyleBackground.BackgroundTheme.Sunset => (
                    new float[] { 1.0f, 0.4f, 0.2f, 1.0f },       // Orange
                    new float[] { 0.8f, 0.2f, 0.6f, 1.0f },       // Purple
                    new float[] { 0.2f, 0.05f, 0.1f, 1.0f }       // Dark red
                ),
                
                BalatroStyleBackground.BackgroundTheme.Midnight => (
                    new float[] { 0.1f, 0.1f, 0.3f, 1.0f },       // Dark blue
                    new float[] { 0.3f, 0.1f, 0.5f, 1.0f },       // Purple
                    new float[] { 0.05f, 0.05f, 0.1f, 1.0f }      // Very dark blue
                ),
                
                BalatroStyleBackground.BackgroundTheme.Forest => (
                    new float[] { 0.2f, 0.6f, 0.2f, 1.0f },       // Green
                    new float[] { 0.5f, 0.4f, 0.2f, 1.0f },       // Brown
                    new float[] { 0.1f, 0.15f, 0.1f, 1.0f }       // Dark green
                ),
                
                BalatroStyleBackground.BackgroundTheme.Cherry => (
                    new float[] { 0.9f, 0.3f, 0.5f, 1.0f },       // Pink
                    new float[] { 0.8f, 0.1f, 0.3f, 1.0f },       // Red
                    new float[] { 0.2f, 0.05f, 0.1f, 1.0f }       // Dark red
                ),
                
                BalatroStyleBackground.BackgroundTheme.Gold => (
                    new float[] { 1.0f, 0.843f, 0.0f, 1.0f },     // Gold
                    new float[] { 0.8f, 0.6f, 0.0f, 1.0f },       // Dark gold
                    new float[] { 0.1f, 0.1f, 0.1f, 1.0f }        // Black
                ),
                
                BalatroStyleBackground.BackgroundTheme.Monochrome => (
                    new float[] { 0.7f, 0.7f, 0.7f, 1.0f },       // Light gray
                    new float[] { 0.3f, 0.3f, 0.3f, 1.0f },       // Dark gray
                    new float[] { 0.1f, 0.1f, 0.1f, 1.0f }        // Very dark gray
                ),
                
                _ => ( // Default fallback
                    new float[] { 0.871f, 0.267f, 0.231f, 1.0f },
                    new float[] { 0.0f, 0.42f, 0.706f, 1.0f },
                    new float[] { 0.086f, 0.137f, 0.145f, 1.0f }
                )
            };
        }
    }
}
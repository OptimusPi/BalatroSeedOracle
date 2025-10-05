using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Rendering.Composition;
using Avalonia.Skia;
using SkiaSharp;

namespace BalatroSeedOracle.Controls
{
    /// <summary>
    /// Psychedelic fractal shader background - WinAmp MilkDrop inspired
    /// Relaxing, captivating, cozy pixel art vibes with HD psychedelic visuals
    /// </summary>
    public class PsychedelicShaderBackground : Control
    {
        private CompositionCustomVisual? _customVisual;
        private PsychedelicShaderHandler? _handler;

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

        // Music-reactive properties
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

        // Shader parameters
        public void SetSpeed(float speed)
        {
            _handler?.SetSpeed(speed);
        }

        public void SetFractalComplexity(float complexity)
        {
            _handler?.SetFractalComplexity(complexity);
        }

        public void SetColorCycle(float cycle)
        {
            _handler?.SetColorCycle(cycle);
        }

        public void SetKaleidoscope(float strength)
        {
            _handler?.SetKaleidoscope(strength);
        }

        public void SetFluidFlow(float flow)
        {
            _handler?.SetFluidFlow(flow);
        }

        protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
        {
            base.OnAttachedToVisualTree(e);

            var compositionTarget = ElementComposition.GetElementVisual(this);
            if (compositionTarget?.Compositor != null)
            {
                _handler = new PsychedelicShaderHandler();
                _customVisual = compositionTarget.Compositor.CreateCustomVisual(_handler);
                ElementComposition.SetElementChildVisual(this, _customVisual);
                _customVisual.Size = new Vector(Bounds.Width, Bounds.Height);
            }

            this.PointerMoved += OnPointerMoved;
        }

        private void OnPointerMoved(object? sender, Avalonia.Input.PointerEventArgs e)
        {
            var position = e.GetPosition(this);
            var bounds = this.Bounds;

            float normalizedX = (float)(position.X / bounds.Width);
            float normalizedY = (float)(position.Y / bounds.Height);

            _handler?.UpdateMousePosition(normalizedX, normalizedY);
        }

        protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
        {
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

    public class PsychedelicShaderHandler : CompositionCustomVisualHandler, IDisposable
    {
        private SKRuntimeShaderBuilder? _shaderBuilder;
        private bool _isDisposed;
        private bool _isAnimating = true;
        private readonly System.Diagnostics.Stopwatch _stopwatch = System.Diagnostics.Stopwatch.StartNew();

        // Shader parameters
        private float _speed = 1.0f;
        private float _fractalComplexity = 4.0f;
        private float _colorCycle = 1.0f;
        private float _kaleidoscope = 6.0f;
        private float _fluidFlow = 1.5f;

        // Music reactive
        private float _vibeIntensity = 0f;
        private float _melodyIntensity = 0f;
        private float _chordsIntensity = 0f;
        private float _bassIntensity = 0f;

        // Mouse interaction
        private float _mouseX = 0.5f;
        private float _mouseY = 0.5f;

        public void SetAnimating(bool animating) => _isAnimating = animating;
        public void SetSpeed(float speed) => _speed = Math.Clamp(speed, 0.1f, 3.0f);
        public void SetFractalComplexity(float complexity) => _fractalComplexity = Math.Clamp(complexity, 1.0f, 10.0f);
        public void SetColorCycle(float cycle) => _colorCycle = Math.Clamp(cycle, 0.1f, 5.0f);
        public void SetKaleidoscope(float strength) => _kaleidoscope = Math.Clamp(strength, 0.0f, 12.0f);
        public void SetFluidFlow(float flow) => _fluidFlow = Math.Clamp(flow, 0.0f, 5.0f);

        public void SetVibeIntensity(float intensity) => _vibeIntensity = Math.Clamp(intensity, 0f, 1f);

        public void SetMelodicFFT(float mid, float treble, float peak)
        {
            // Not used in this shader currently
        }

        public void SetTrackIntensities(float melody, float chords, float bass)
        {
            _melodyIntensity = Math.Clamp(melody, 0f, 1f);
            _chordsIntensity = Math.Clamp(chords, 0f, 1f);
            _bassIntensity = Math.Clamp(bass, 0f, 1f);
        }

        public void UpdateMousePosition(float x, float y)
        {
            _mouseX = x;
            _mouseY = y;
        }

        public override void OnRender(ImmediateDrawingContext context)
        {
            if (_isDisposed) return;

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

            // Psychedelic fractal shader - MilkDrop inspired
            var sksl = @"
                uniform float2 resolution;
                uniform float time;
                uniform float speed;
                uniform float fractal_complexity;
                uniform float color_cycle;
                uniform float kaleidoscope;
                uniform float fluid_flow;
                uniform float2 mouse;
                uniform float melody;
                uniform float chords;
                uniform float bass;

                // Smooth HSV to RGB conversion
                float3 hsv2rgb(float3 c) {
                    float4 K = float4(1.0, 2.0 / 3.0, 1.0 / 3.0, 3.0);
                    float3 p = abs(fract(c.xxx + K.xyz) * 6.0 - K.www);
                    return c.z * mix(K.xxx, clamp(p - K.xxx, 0.0, 1.0), c.y);
                }

                // Fractal noise function - creates organic patterns
                float fractalNoise(float2 p) {
                    float value = 0.0;
                    float amplitude = 1.0;
                    float frequency = 1.0;

                    for (int i = 0; i < 6; i++) {
                        value += amplitude * sin(frequency * p.x) * cos(frequency * p.y);
                        p = float2(p.x * 1.7 - p.y * 0.7, p.y * 1.7 + p.x * 0.7);
                        amplitude *= 0.5;
                        frequency *= 2.0;
                    }

                    return value;
                }

                // Kaleidoscope effect
                float2 kaleidoscopeUV(float2 uv, float segments) {
                    float angle = atan(uv.y, uv.x);
                    float radius = length(uv);

                    angle = mod(angle, 3.14159265 * 2.0 / segments);
                    angle = abs(angle - 3.14159265 / segments);

                    return float2(cos(angle), sin(angle)) * radius;
                }

                // Fluid flow distortion
                float2 fluidDistortion(float2 p, float t) {
                    float2 q = float2(
                        fractalNoise(p + float2(0.0, t * 0.3)),
                        fractalNoise(p + float2(t * 0.4, 0.0))
                    );

                    return p + fluid_flow * 0.2 * q;
                }

                // MilkDrop-style tunnel effect
                float tunnel(float2 uv, float t) {
                    float r = length(uv);
                    float a = atan(uv.y, uv.x);

                    float tunnel_depth = 1.0 / (r + 0.01);
                    float spiral = a + tunnel_depth * 0.5 + t * 0.5;

                    return sin(tunnel_depth * 3.0 + t) * 0.5 +
                           cos(spiral * 8.0) * 0.3 +
                           sin(a * 6.0 + t) * 0.2;
                }

                float4 main(float2 screen_coords) {
                    // Normalize coordinates
                    float2 uv = (screen_coords - resolution * 0.5) / min(resolution.x, resolution.y);
                    float t = time * speed;

                    // Mouse interaction - distort space
                    float2 mouseOffset = (mouse - 0.5) * 2.0;
                    uv -= mouseOffset * 0.3 * (bass + 0.5);

                    // Kaleidoscope effect (strength controlled by parameter)
                    if (kaleidoscope > 0.1) {
                        uv = kaleidoscopeUV(uv, kaleidoscope);
                    }

                    // Fluid flow distortion
                    uv = fluidDistortion(uv, t);

                    // Zoom breathing effect (bass reactive)
                    float zoom = 1.0 + bass * 0.3 + sin(t * 0.5) * 0.2;
                    uv *= zoom;

                    // Rotation (chords reactive)
                    float rotation = t * 0.3 + chords * 3.14159265;
                    float2x2 rot = float2x2(cos(rotation), -sin(rotation), sin(rotation), cos(rotation));
                    uv = rot * uv;

                    // Fractal layering - MilkDrop style
                    float color_base = 0.0;
                    float2 p = uv * fractal_complexity;

                    for (int i = 0; i < 5; i++) {
                        float layer = fractalNoise(p + t * (0.1 + melody * 0.2));
                        color_base += layer / (float(i + 1) * 2.0);
                        p = p * 1.5 + float2(sin(t * 0.2), cos(t * 0.3));
                    }

                    // Add tunnel effect
                    color_base += tunnel(uv, t) * 0.5;

                    // Colorful psychedelic palette (cycling through hues)
                    float hue = color_base * 0.3 + t * color_cycle * 0.1 + melody * 0.5;
                    hue = fract(hue); // Wrap to 0-1

                    // Saturation pulsing with music
                    float saturation = 0.7 + chords * 0.3 + sin(t * 2.0) * 0.1;
                    saturation = clamp(saturation, 0.0, 1.0);

                    // Brightness pulsing
                    float brightness = 0.6 + melody * 0.3 + abs(sin(t * 1.5)) * 0.1;
                    brightness = clamp(brightness, 0.0, 1.0);

                    // Convert HSV to RGB
                    float3 color = hsv2rgb(float3(hue, saturation, brightness));

                    // Add glow in dark areas (cozy vibe)
                    float glow = exp(-length(uv) * 0.5) * 0.3;
                    color += glow * float3(0.3, 0.1, 0.5);

                    // Vignette effect (darker edges, cozy framing)
                    float vignette = 1.0 - length(uv * 0.7);
                    vignette = smoothstep(0.0, 1.0, vignette);
                    color *= mix(0.3, 1.0, vignette);

                    // Subtle scanline/pixel effect for retro charm
                    float scanline = sin(screen_coords.y * 0.5) * 0.03 + 0.97;
                    color *= scanline;

                    return float4(color, 1.0);
                }
            ";

            var effect = SKRuntimeEffect.CreateShader(sksl, out var error);
            if (effect != null)
            {
                _shaderBuilder = new SKRuntimeShaderBuilder(effect);
            }
            else
            {
                Console.WriteLine($"Shader compilation error: {error}");
            }
        }

        private void RenderShader(SKCanvas canvas)
        {
            if (_shaderBuilder == null) return;

            var bounds = GetRenderBounds();
            var time = (float)_stopwatch.Elapsed.TotalSeconds;

            if (bounds.Width > 0 && bounds.Height > 0)
            {
                var currentSize = new SKSize((float)bounds.Width, (float)bounds.Height);

                // Update uniforms
                _shaderBuilder.Uniforms["resolution"] = currentSize;
                _shaderBuilder.Uniforms["time"] = time;
                _shaderBuilder.Uniforms["speed"] = _speed;
                _shaderBuilder.Uniforms["fractal_complexity"] = _fractalComplexity;
                _shaderBuilder.Uniforms["color_cycle"] = _colorCycle;
                _shaderBuilder.Uniforms["kaleidoscope"] = _kaleidoscope;
                _shaderBuilder.Uniforms["fluid_flow"] = _fluidFlow;
                _shaderBuilder.Uniforms["mouse"] = new SKPoint(_mouseX, _mouseY);
                _shaderBuilder.Uniforms["melody"] = _melodyIntensity;
                _shaderBuilder.Uniforms["chords"] = _chordsIntensity;
                _shaderBuilder.Uniforms["bass"] = _bassIntensity;

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

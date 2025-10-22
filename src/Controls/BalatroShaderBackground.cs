using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Platform;
using Avalonia.Rendering.Composition;
using Avalonia.Skia;
using SkiaSharp;

namespace BalatroSeedOracle.Controls
{
    /// <summary>
    /// Pure shader renderer for the Balatro paint-mixing background.
    /// This class has a single responsibility: render the shader based on uniform values.
    /// It knows NOTHING about audio, FFT, or music. Those concerns belong in MusicToVisualizerHandler.
    /// </summary>
    public class BalatroShaderBackground : Control
    {
        // Legacy enums for compatibility - will be removed after full refactor
        public enum BackgroundTheme
        {
            Default,
            Plasma,
            Ocean,
            Sunset,
            Midnight,
            Sepia,
            Dynamic
            // REMOVED: VibeOut (feature removed)
        }

        public enum AudioSource
        {
            None,
            Drums,
            Bass,
            Chords,
            Melody
        }
        private CompositionCustomVisual? _customVisual;
        private ShaderRenderer? _renderer;

        // Animation state
        private bool _isAnimating = true;
        public bool IsAnimating
        {
            get => _isAnimating;
            set
            {
                _isAnimating = value;
                _renderer?.SetAnimating(value);
            }
        }

        // Legacy theme property for compatibility
        private BackgroundTheme _theme = BackgroundTheme.Default;
        public BackgroundTheme Theme
        {
            get => _theme;
            set => _theme = value; // Just store it, doesn't affect rendering anymore
        }

        protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
        {
            base.OnAttachedToVisualTree(e);

            var compositor = ElementComposition.GetElementVisual(this)?.Compositor;
            if (compositor == null) return;

            _renderer = new ShaderRenderer();
            _customVisual = compositor.CreateCustomVisual(_renderer);
            ElementComposition.SetElementChildVisual(this, _customVisual);

            _customVisual.Size = new Vector(Bounds.Width, Bounds.Height);
            _customVisual.SendHandlerMessage(_renderer);
        }

        protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
        {
            base.OnDetachedFromVisualTree(e);

            _renderer?.Dispose();
            _renderer = null;
            _customVisual = null;
        }

        protected override Size ArrangeOverride(Size finalSize)
        {
            var result = base.ArrangeOverride(finalSize);
            if (_customVisual != null)
            {
                _customVisual.Size = new Vector(finalSize.Width, finalSize.Height);
            }
            return result;
        }

        #region Pure Shader Uniform Setters

        // Time controls
        public void SetTime(float time) => _renderer?.SetUniform("time", time);
        public void SetSpinTime(float spinTime) => _renderer?.SetUniform("spin_time", spinTime);

        // Color controls (accept SKColor for flexibility)
        public void SetMainColor(SKColor color) => _renderer?.SetColor1(color);
        public void SetAccentColor(SKColor color) => _renderer?.SetColor2(color);
        public void SetBackgroundColor(SKColor color) => _renderer?.SetColor3(color);

        // Effect parameters
        public void SetContrast(float value) => _renderer?.SetUniform("contrast", Math.Clamp(value, 0.1f, 10.0f));
        public void SetSpinAmount(float value) => _renderer?.SetUniform("spin_amount", Math.Clamp(value, 0.0f, 1.0f));
        public void SetParallaxX(float value) => _renderer?.SetUniform("parallax_x", Math.Clamp(value, -1.0f, 1.0f));
        public void SetParallaxY(float value) => _renderer?.SetUniform("parallax_y", Math.Clamp(value, -1.0f, 1.0f));
        public void SetZoomScale(float value) => _renderer?.SetUniform("zoom_scale", Math.Clamp(value, -50.0f, 50.0f));
        public void SetSaturationAmount(float value) => _renderer?.SetUniform("saturation_amount", Math.Clamp(value, 0.0f, 1.0f));
        public void SetPixelSize(float value) => _renderer?.SetUniform("pixel_size", Math.Clamp(value, 100.0f, 5000.0f));
        public void SetSpinEase(float value) => _renderer?.SetUniform("spin_ease", Math.Clamp(value, 0.0f, 2.0f));
        public void SetLoopCount(float value) => _renderer?.SetUniform("loop_count", Math.Clamp(value, 1.0f, 10.0f));

        // Convenience method for setting parallax from mouse position
        public void SetParallax(float x, float y)
        {
            SetParallaxX(x);
            SetParallaxY(y);
        }

        #endregion

        /// <summary>
        /// The actual shader renderer - handles the GPU shader compilation and rendering
        /// </summary>
        private class ShaderRenderer : CompositionCustomVisualHandler, IDisposable
        {
            private SKRuntimeShaderBuilder? _shaderBuilder;
            private bool _isDisposed;
            private bool _isAnimating = true;
            private readonly System.Diagnostics.Stopwatch _stopwatch = System.Diagnostics.Stopwatch.StartNew();

            // Animation speed multipliers (controlled by sliders)
            private float _timeSpeed = 1.0f;
            private float _spinTimeSpeed = 1.0f;

            // Current uniform values (calculated from elapsed time * speed)
            private float _time = 0f;
            private float _spinTime = 0f;
            private SKColor _color1 = new SKColor(255, 76, 64);   // Balatro Red
            private SKColor _color2 = new SKColor(0, 147, 255);   // Balatro Blue
            private SKColor _color3 = new SKColor(30, 43, 45);    // Balatro Dark Teal
            private float _contrast = 2.0f;
            private float _spinAmount = 0.3f;
            private float _parallaxX = 0f;
            private float _parallaxY = 0f;
            private float _zoomScale = 0f;
            private float _saturationAmount = 0f;
            private float _pixelSize = 1440.0f;  // Default from const
            private float _spinEase = 0.5f;      // Default from const
            private float _loopCount = 5.0f;     // Default loop count for paint effect

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
                // Animation will resume/stop on next frame
                // We can't call Invalidate() here as we may not have compositor lock
            }

            public void SetUniform(string name, float value)
            {
                switch (name)
                {
                    case "time": _timeSpeed = value; break;  // NOW a speed multiplier!
                    case "spin_time": _spinTimeSpeed = value; break;  // NOW a speed multiplier!
                    case "contrast": _contrast = value; break;
                    case "spin_amount": _spinAmount = value; break;
                    case "parallax_x": _parallaxX = value; break;
                    case "parallax_y": _parallaxY = value; break;
                    case "zoom_scale": _zoomScale = value; break;
                    case "saturation_amount": _saturationAmount = value; break;
                    case "pixel_size": _pixelSize = value; break;
                    case "spin_ease": _spinEase = value; break;
                    case "loop_count": _loopCount = value; break;
                }
            }

            public void SetColor1(SKColor color) => _color1 = color;
            public void SetColor2(SKColor color) => _color2 = color;
            public void SetColor3(SKColor color) => _color3 = color;

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

                // PURE Balatro background shader (from external/Balatro/resources/shaders/background.fs)
                // Enhanced with parallax, zoom, and saturation for visualizer use
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
                    uniform float pixel_size;  // Was const, now uniform!
                    uniform float spin_ease;   // Was const, now uniform!
                    uniform float loop_count;  // Controls paint effect complexity (1-10)

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
                        // Convert to UV coords with pixelation and parallax
                        float pix_size = length(resolution) / pixel_size;
                        float2 uv = (floor(screen_coords * (1.0 / pix_size)) * pix_size - 0.5 * resolution) / length(resolution) - float2(parallax_x, parallax_y);
                        float uv_len = length(uv);

                        // Center swirl that changes with time
                        float speed = (spin_time * spin_ease * 0.2) + 302.2;
                        float new_pixel_angle = atan(uv.y, uv.x) + speed - spin_ease * 20.0 * (1.0 * spin_amount * uv_len + (1.0 - 1.0 * spin_amount));
                        float2 mid = (resolution / length(resolution)) / 2.0;
                        uv = float2((uv_len * cos(new_pixel_angle) + mid.x), (uv_len * sin(new_pixel_angle) + mid.y)) - mid;

                        // Paint effect with zoom scale
                        uv *= (30.0 + zoom_scale);
                        speed = time * 2.0;
                        float2 uv2 = float2(uv.x + uv.y);

                        // Dynamic loop count (max 10 for shader compatibility)
                        int max_loops = int(clamp(loop_count, 1.0, 10.0));
                        for (int i = 0; i < 10; i++) {
                            if (i >= max_loops) break;  // Early exit based on uniform
                            uv2 += sin(max(uv.x, uv.y)) + uv;
                            uv += 0.5 * float2(cos(5.1123314 + 0.353 * uv2.y + speed * 0.131121), sin(uv2.x - 0.113 * speed));
                            uv -= 1.0 * cos(uv.x + uv.y) - 1.0 * sin(uv.x * 0.711 - uv.y);
                        }

                        // Paint amount calculation
                        float contrast_mod = (0.25 * contrast + 0.5 * spin_amount + 1.2);
                        float paint_res = min(2.0, max(0.0, length(uv) * 0.035 * contrast_mod));
                        float c1p = max(0.0, 1.0 - contrast_mod * abs(1.0 - paint_res));
                        float c2p = max(0.0, 1.0 - contrast_mod * abs(paint_res));
                        float c3p = 1.0 - min(1.0, c1p + c2p);

                        // Apply saturation boost to colour_1 if requested
                        float4 adjusted_colour_1 = colour_1;
                        if (saturation_amount > 0.01) {
                            float3 hsv = rgb2hsv(colour_1.rgb);
                            float satBoost = saturation_amount * 0.3;
                            hsv.y = clamp(hsv.y + satBoost, 0.0, 1.0);
                            adjusted_colour_1 = float4(hsv2rgb(hsv), colour_1.a);
                        }

                        // Final color mixing
                        float4 ret_col = (0.3 / contrast) * adjusted_colour_1 + (1.0 - 0.3 / contrast) * (adjusted_colour_1 * c1p + colour_2 * c2p + float4(c3p * colour_3.rgb, c3p * colour_1.a));
                        return ret_col;
                    }";

                var effect = SKRuntimeEffect.CreateShader(sksl, out var error);
                if (effect != null)
                {
                    _shaderBuilder = new SKRuntimeShaderBuilder(effect);
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine($"Shader compilation failed: {error}");
                }
            }

            private void RenderShader(SKCanvas canvas)
            {
                if (_shaderBuilder == null) return;

                var bounds = GetRenderBounds();
                if (bounds.Width <= 0 || bounds.Height <= 0) return;

                var currentSize = new SKSize((float)bounds.Width, (float)bounds.Height);

                // Auto-advance time when animating (multiply by speed)
                if (_isAnimating)
                {
                    var elapsedSeconds = (float)_stopwatch.Elapsed.TotalSeconds;
                    _time = elapsedSeconds * _timeSpeed;
                    _spinTime = elapsedSeconds * _spinTimeSpeed;
                }

                // Update all uniforms
                _shaderBuilder.Uniforms["resolution"] = currentSize;
                _shaderBuilder.Uniforms["time"] = _time;
                _shaderBuilder.Uniforms["spin_time"] = _spinTime;
                _shaderBuilder.Uniforms["colour_1"] = new float[] { _color1.Red / 255f, _color1.Green / 255f, _color1.Blue / 255f, 1f };
                _shaderBuilder.Uniforms["colour_2"] = new float[] { _color2.Red / 255f, _color2.Green / 255f, _color2.Blue / 255f, 1f };
                _shaderBuilder.Uniforms["colour_3"] = new float[] { _color3.Red / 255f, _color3.Green / 255f, _color3.Blue / 255f, 1f };
                _shaderBuilder.Uniforms["contrast"] = _contrast;
                _shaderBuilder.Uniforms["spin_amount"] = _spinAmount;
                _shaderBuilder.Uniforms["parallax_x"] = _parallaxX;
                _shaderBuilder.Uniforms["parallax_y"] = _parallaxY;
                _shaderBuilder.Uniforms["zoom_scale"] = _zoomScale;
                _shaderBuilder.Uniforms["saturation_amount"] = _saturationAmount;
                _shaderBuilder.Uniforms["pixel_size"] = _pixelSize;
                _shaderBuilder.Uniforms["spin_ease"] = _spinEase;
                _shaderBuilder.Uniforms["loop_count"] = _loopCount;

                // Build and apply shader
                using var shader = _shaderBuilder.Build();
                using var paint = new SKPaint { Shader = shader };

                var rect = new SKRect(0, 0, currentSize.Width, currentSize.Height);
                canvas.DrawRect(rect, paint);
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
}
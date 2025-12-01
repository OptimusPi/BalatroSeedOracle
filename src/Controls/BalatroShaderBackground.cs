using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Platform;
using Avalonia.Rendering.Composition;
using Avalonia.Skia;
using BalatroSeedOracle.Constants;
using SkiaSharp;

namespace BalatroSeedOracle.Controls
{
    /// <summary>
    /// Pure shader renderer for the Balatro paint-mixing background.
    /// This class has a single responsibility: render the shader based on uniform values.
    /// It knows NOTHING about audio, FFT, or music. Audio reactivity is handled externally.
    /// </summary>
    public class BalatroShaderBackground : Control
    {
        private CompositionCustomVisual? _customVisual;
        private ShaderRenderer? _renderer;

        // Animation state
        // Renamed from IsAnimating to avoid collision with AvaloniaObject.IsAnimating(AvaloniaProperty) method
        private bool _isAnimating = true;
        public bool AnimationEnabled
        {
            get => _isAnimating;
            set
            {
                _isAnimating = value;
                _renderer?.SetAnimating(value);
            }
        }

        // Shader theme property removed - theme is now handled via direct shader parameter setters

        protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
        {
            base.OnAttachedToVisualTree(e);

            var compositor = ElementComposition.GetElementVisual(this)?.Compositor;
            if (compositor == null)
                return;

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
        public void SetTime(float time) => _renderer?.SetTimeSpeed(time);

        public void SetSpinTime(float spinTime) => _renderer?.SetSpinTimeSpeed(spinTime);

        public float GetTimeSpeed() => _renderer != null ? _renderer.GetTimeSpeed() : 1.0f;

        public float GetSpinTimeSpeed() => _renderer != null ? _renderer.GetSpinTimeSpeed() : 1.0f;

        public float GetContrast() => _renderer != null ? _renderer.GetContrast() : 2.0f;

        public float GetSpinAmount() => _renderer != null ? _renderer.GetSpinAmount() : 0.3f;

        public float GetParallaxX() => _renderer != null ? _renderer.GetParallaxX() : 0f;

        public float GetParallaxY() => _renderer != null ? _renderer.GetParallaxY() : 0f;

        public float GetZoomScale() => _renderer != null ? _renderer.GetZoomScale() : 0f;

        public float GetSaturationAmount() =>
            _renderer != null ? _renderer.GetSaturationAmount() : 0f;

        public float GetSaturationAmount2() =>
            _renderer != null ? _renderer.GetSaturationAmount2() : 0f;

        public float GetPixelSize() => _renderer != null ? _renderer.GetPixelSize() : 1440.0f;

        public float GetSpinEase() => _renderer != null ? _renderer.GetSpinEase() : 0.5f;

        public float GetLoopCount() => _renderer != null ? _renderer.GetLoopCount() : 5.0f;

        // Color controls (accept SKColor for flexibility)
        public void SetMainColor(SKColor color) => _renderer?.SetColor1(color);

        public void SetAccentColor(SKColor color) => _renderer?.SetColor2(color);

        public void SetBackgroundColor(SKColor color) => _renderer?.SetColor3(color);

        // Effect parameters
        public void SetContrast(float value) => _renderer?.SetContrast(value);

        public void SetSpinAmount(float value) => _renderer?.SetSpinAmount(value);

        public void SetParallaxX(float value) => _renderer?.SetParallaxX(value);

        public void SetParallaxY(float value) => _renderer?.SetParallaxY(value);

        public void SetZoomScale(float value) => _renderer?.SetZoomScale(value);

        public void SetSaturationAmount(float value) => _renderer?.SetSaturationAmount(value);

        public void SetSaturationAmount2(float value) => _renderer?.SetSaturationAmount2(value);

        public void SetPixelSize(float value) => _renderer?.SetPixelSize(value);

        public void SetSpinEase(float value) => _renderer?.SetSpinEase(value);

        public void SetLoopCount(float value) => _renderer?.SetLoopCount(value);

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
            private SKRuntimeShaderBuilder? _psyShaderBuilder;
            private bool _isDisposed;
            private bool _isAnimating = true;
            private readonly System.Diagnostics.Stopwatch _stopwatch =
                System.Diagnostics.Stopwatch.StartNew();

            // Animation speed multipliers (controlled by sliders)
            private float _timeSpeed = 1.0f;
            private float _spinTimeSpeed = 1.0f;

            // Current uniform values (calculated from elapsed time * speed)
            private float _time = 0f;
            private float _spinTime = 0f;
            private SKColor _color1 = new SKColor(255, 76, 64); // Balatro Red
            private SKColor _color2 = new SKColor(0, 147, 255); // Balatro Blue
            private SKColor _color3 = new SKColor(30, 43, 45); // Balatro Dark Teal
            private float _contrast = 2.0f;
            private float _spinAmount = 0.3f;
            private float _parallaxX = 0f;
            private float _parallaxY = 0f;
            private float _zoomScale = 0f;
            private float _saturationAmount = 0f;
            private float _saturationAmount2 = 0f;
            private float _pixelSize = 1440.0f; // Default from const
            private float _spinEase = 0.5f; // Default from const
            private float _loopCount = 5.0f; // Default loop count for paint effect

            // Psychedelic parameters
            private float _psySpeed = 1.0f;
            private float _psyComplexity = 1.0f;
            private float _psyColorCycle = 1.0f;
            private float _psyKaleidoscope = 0.0f;
            private float _psyFluidFlow = 0.0f;
            private SKPoint _psyMouse = new SKPoint(0.5f, 0.5f);
            private float _psyMelody = 0.0f;
            private float _psyChords = 0.0f;
            private float _psyBass = 0.0f;
            private float _psyBlend = 0.0f; // 0..1 blend factor for psychedelic overlay

            public float GetTimeSpeed() => _timeSpeed;

            public float GetSpinTimeSpeed() => _spinTimeSpeed;

            public float GetContrast() => _contrast;

            public float GetSpinAmount() => _spinAmount;

            public float GetParallaxX() => _parallaxX;

            public float GetParallaxY() => _parallaxY;

            public float GetZoomScale() => _zoomScale;

            public float GetSaturationAmount() => _saturationAmount;

            public float GetSaturationAmount2() => _saturationAmount2;

            public float GetPixelSize() => _pixelSize;

            public float GetSpinEase() => _spinEase;

            public float GetLoopCount() => _loopCount;

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
                    case "time":
                        _timeSpeed = value;
                        break; // NOW a speed multiplier!
                    case "spin_time":
                        _spinTimeSpeed = value;
                        break; // NOW a speed multiplier!
                    case "contrast":
                        _contrast = value;
                        break;
                    case "spin_amount":
                        _spinAmount = value;
                        break;
                    case "parallax_x":
                        _parallaxX = value;
                        break;
                    case "parallax_y":
                        _parallaxY = value;
                        break;
                    case "zoom_scale":
                        _zoomScale = value;
                        break;
                    case "saturation_amount":
                        _saturationAmount = value;
                        break;
                    case "saturation_amount_2":
                        _saturationAmount2 = value;
                        break;
                    case "pixel_size":
                        _pixelSize = value;
                        break;
                    case "spin_ease":
                        _spinEase = value;
                        break;
                    case "loop_count":
                        _loopCount = value;
                        break;
                    case "psy_blend":
                        _psyBlend = value;
                        break;
                    case "psy_speed":
                        _psySpeed = value;
                        break;
                    case "psy_complexity":
                        _psyComplexity = value;
                        break;
                    case "psy_color_cycle":
                        _psyColorCycle = value;
                        break;
                    case "psy_kaleidoscope":
                        _psyKaleidoscope = value;
                        break;
                    case "psy_fluid_flow":
                        _psyFluidFlow = value;
                        break;
                }
            }

            public void SetColor1(SKColor color) => _color1 = color;

            public void SetColor2(SKColor color) => _color2 = color;

            public void SetColor3(SKColor color) => _color3 = color;

            // Direct setters (replace string-based SetUniform for performance)
            public void SetTimeSpeed(float value) => _timeSpeed = value;

            public void SetSpinTimeSpeed(float value) => _spinTimeSpeed = value;

            public void SetContrast(float value) => _contrast = value;

            public void SetSpinAmount(float value) => _spinAmount = value;

            public void SetParallaxX(float value) => _parallaxX = value;

            public void SetParallaxY(float value) => _parallaxY = value;

            public void SetZoomScale(float value) => _zoomScale = value;

            public void SetSaturationAmount(float value) => _saturationAmount = value;

            public void SetSaturationAmount2(float value) => _saturationAmount2 = value;

            public void SetPixelSize(float value) => _pixelSize = value;

            public void SetSpinEase(float value) => _spinEase = value;

            public void SetLoopCount(float value) => _loopCount = value;

            public void SetPsyBlend(float value) => _psyBlend = value;

            public void SetPsySpeed(float value) => _psySpeed = value;

            public void SetPsyComplexity(float value) => _psyComplexity = value;

            public void SetPsyColorCycle(float value) => _psyColorCycle = value;

            public void SetPsyKaleidoscope(float value) => _psyKaleidoscope = value;

            public void SetPsyFluidFlow(float value) => _psyFluidFlow = value;

            public override void OnRender(ImmediateDrawingContext context)
            {
                if (_isDisposed)
                    return;

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
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine($"Shader compilation failed: {error}");
                }

                var psyEffect = SKRuntimeEffect.CreateShader(
                    ShaderConstants.PSYCHEDELIC_SHADER,
                    out var perr
                );
                if (psyEffect != null)
                {
                    _psyShaderBuilder = new SKRuntimeShaderBuilder(psyEffect);
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine(
                        $"Psychedelic shader compilation failed: {perr}"
                    );
                }
            }

            private void RenderShader(SKCanvas canvas)
            {
                if (_shaderBuilder == null)
                    return;

                var bounds = GetRenderBounds();
                if (bounds.Width <= 0 || bounds.Height <= 0)
                    return;

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
                _shaderBuilder.Uniforms["colour_1"] = new float[]
                {
                    _color1.Red / 255f,
                    _color1.Green / 255f,
                    _color1.Blue / 255f,
                    1f,
                };
                _shaderBuilder.Uniforms["colour_2"] = new float[]
                {
                    _color2.Red / 255f,
                    _color2.Green / 255f,
                    _color2.Blue / 255f,
                    1f,
                };
                _shaderBuilder.Uniforms["colour_3"] = new float[]
                {
                    _color3.Red / 255f,
                    _color3.Green / 255f,
                    _color3.Blue / 255f,
                    1f,
                };
                _shaderBuilder.Uniforms["contrast"] = _contrast;
                _shaderBuilder.Uniforms["spin_amount"] = _spinAmount;
                _shaderBuilder.Uniforms["parallax_x"] = _parallaxX;
                _shaderBuilder.Uniforms["parallax_y"] = _parallaxY;
                _shaderBuilder.Uniforms["zoom_scale"] = _zoomScale;
                _shaderBuilder.Uniforms["saturation_amount"] = _saturationAmount;
                _shaderBuilder.Uniforms["saturation_amount_2"] = _saturationAmount2;
                _shaderBuilder.Uniforms["pixel_size"] = _pixelSize;
                _shaderBuilder.Uniforms["spin_ease"] = _spinEase;
                _shaderBuilder.Uniforms["loop_count"] = _loopCount;

                // Build and apply shader
                using var shader = _shaderBuilder.Build();
                using var paint = new SKPaint { Shader = shader };

                var rect = new SKRect(0, 0, currentSize.Width, currentSize.Height);
                canvas.DrawRect(rect, paint);

                if (_psyShaderBuilder != null && _psyBlend > 0.001f)
                {
                    _psyShaderBuilder.Uniforms["resolution"] = currentSize;
                    _psyShaderBuilder.Uniforms["time"] = _time;
                    _psyShaderBuilder.Uniforms["speed"] = _psySpeed;
                    _psyShaderBuilder.Uniforms["fractal_complexity"] = _psyComplexity;
                    _psyShaderBuilder.Uniforms["color_cycle"] = _psyColorCycle;
                    _psyShaderBuilder.Uniforms["kaleidoscope"] = _psyKaleidoscope;
                    _psyShaderBuilder.Uniforms["fluid_flow"] = _psyFluidFlow;
                    _psyShaderBuilder.Uniforms["mouse"] = new float[] { _psyMouse.X, _psyMouse.Y };
                    _psyShaderBuilder.Uniforms["melody"] = _psyMelody;
                    _psyShaderBuilder.Uniforms["chords"] = _psyChords;
                    _psyShaderBuilder.Uniforms["bass"] = _psyBass;

                    using var psyShader = _psyShaderBuilder.Build();
                    using var paint2 = new SKPaint { Shader = psyShader };
                    paint2.Color = new SKColor(
                        255,
                        255,
                        255,
                        (byte)(Math.Clamp(_psyBlend, 0f, 1f) * 255)
                    );
                    canvas.DrawRect(rect, paint2);
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
                if (_isDisposed)
                    return;
                _isDisposed = true;
                _shaderBuilder?.Dispose();
                _shaderBuilder = null;
                _psyShaderBuilder?.Dispose();
                _psyShaderBuilder = null;
            }
        }

        public void SetPsychedelicBlend(float blend) => _renderer?.SetPsyBlend(blend);

        public void SetPsychedelicSpeed(float speed) => _renderer?.SetPsySpeed(speed);

        public void SetPsychedelicComplexity(float value) => _renderer?.SetPsyComplexity(value);

        public void SetPsychedelicColorCycle(float value) => _renderer?.SetPsyColorCycle(value);

        public void SetPsychedelicKaleidoscope(float value) => _renderer?.SetPsyKaleidoscope(value);

        public void SetPsychedelicFluidFlow(float value) => _renderer?.SetPsyFluidFlow(value);
    }
}

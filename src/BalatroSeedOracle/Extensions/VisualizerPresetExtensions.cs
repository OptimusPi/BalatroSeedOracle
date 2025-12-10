using BalatroSeedOracle.Models;
using SkiaSharp;

namespace BalatroSeedOracle.Extensions
{
    /// <summary>
    /// Extension methods for converting high-level VisualizerPreset to low-level ShaderParameters.
    /// Handles theme mapping, color conversion, and intensity scaling.
    /// </summary>
    public static class VisualizerPresetExtensions
    {
        private const float DEFAULT_PIXEL_SIZE = 1440.0f; // Full HD width - higher value = less pixelation
        private const float HEAVY_PIXELATION_SIZE = 200.0f; // Low resolution for intro effects
        private const float DEFAULT_CONTRAST = 2.0f;
        private const float DEFAULT_SPIN_AMOUNT = 0.3f;
        private const float DEFAULT_SPIN_EASE = 0.5f;
        private const float DEFAULT_LOOP_COUNT = 5.0f;
        private const float SPIN_TIME_RATIO = 0.5f; // Spin typically slower than main time

        // Color constants
        private static readonly SKColor BALATRO_DARK_TEAL = new SKColor(30, 43, 45);
        private static readonly SKColor BALATRO_RED = new SKColor(255, 76, 64);
        private static readonly SKColor BALATRO_BLUE = new SKColor(0, 147, 255);
        private static readonly SKColor INTRO_DARK_BLUE_GREY = new SKColor(20, 20, 30);
        private static readonly SKColor INTRO_LIGHT_GREY = new SKColor(50, 50, 60);
        private static readonly SKColor INTRO_ALMOST_BLACK = new SKColor(10, 10, 15);

        /// <summary>
        /// Converts a VisualizerPreset to raw ShaderParameters.
        /// Maps high-level settings (theme, colors, intensity) to shader uniforms.
        /// </summary>
        public static ShaderParameters ToShaderParameters(this VisualizerPreset preset)
        {
            var parameters = new ShaderParameters
            {
                // Time speeds (affected by TimeSpeed setting)
                TimeSpeed = preset.TimeSpeed,
                SpinTimeSpeed = preset.TimeSpeed * SPIN_TIME_RATIO,

                // Colors (convert from preset color indices)
                MainColor = GetColorFromIndex(preset.MainColor),
                AccentColor = GetColorFromIndex(preset.AccentColor),
                BackgroundColor = BALATRO_DARK_TEAL,

                // Effect parameters (affected by AudioIntensity and ParallaxStrength)
                Contrast = DEFAULT_CONTRAST,
                SpinAmount = DEFAULT_SPIN_AMOUNT * preset.AudioIntensity,
                ParallaxX = 0f, // Will be set by mouse movement
                ParallaxY = 0f,
                ZoomScale = 0f, // Base zoom
                SaturationAmount = preset.AudioIntensity * 0.5f, // Subtle saturation boost
                SaturationAmount2 = preset.AudioIntensity * 0.3f,
                PixelSize = DEFAULT_PIXEL_SIZE,
                SpinEase = DEFAULT_SPIN_EASE,
                LoopCount = DEFAULT_LOOP_COUNT,
            };

            return parameters;
        }

        /// <summary>
        /// Creates a default "intro" shader parameters preset.
        /// This is a BUILDING BLOCK - user can customize these values in-app.
        /// Example: Dark, highly pixelated, slow animation.
        /// </summary>
        public static ShaderParameters CreateDefaultIntroParameters()
        {
            return new ShaderParameters
            {
                TimeSpeed = 0.2f, // Very slow animation
                SpinTimeSpeed = 0.1f,
                MainColor = INTRO_DARK_BLUE_GREY,
                AccentColor = INTRO_LIGHT_GREY,
                BackgroundColor = INTRO_ALMOST_BLACK,
                Contrast = 1.0f, // Low contrast
                SpinAmount = 0.1f,
                ParallaxX = 0f,
                ParallaxY = 0f,
                ZoomScale = 0f,
                SaturationAmount = 0f, // No saturation
                SaturationAmount2 = 0f,
                PixelSize = HEAVY_PIXELATION_SIZE,
                SpinEase = DEFAULT_SPIN_EASE,
                LoopCount = 2.0f, // Fewer loops = simpler pattern
            };
        }

        /// <summary>
        /// Creates a default "normal" shader parameters preset.
        /// This is a BUILDING BLOCK - user can customize these values in-app.
        /// Example: Normal Balatro colors, standard animation speed.
        /// </summary>
        public static ShaderParameters CreateDefaultNormalParameters()
        {
            return new ShaderParameters
            {
                TimeSpeed = 1.0f, // Normal animation speed
                SpinTimeSpeed = SPIN_TIME_RATIO,
                MainColor = BALATRO_RED,
                AccentColor = BALATRO_BLUE,
                BackgroundColor = BALATRO_DARK_TEAL,
                Contrast = DEFAULT_CONTRAST,
                SpinAmount = DEFAULT_SPIN_AMOUNT,
                ParallaxX = 0f,
                ParallaxY = 0f,
                ZoomScale = 0f,
                SaturationAmount = 0f,
                SaturationAmount2 = 0f,
                PixelSize = DEFAULT_PIXEL_SIZE,
                SpinEase = DEFAULT_SPIN_EASE,
                LoopCount = DEFAULT_LOOP_COUNT,
            };
        }

        /// <summary>
        /// Maps color index to SKColor.
        /// Index 0-7 = predefined colors, 8 = theme default, null = theme default
        /// </summary>
        private static SKColor GetColorFromIndex(int? colorIndex)
        {
            if (!colorIndex.HasValue || colorIndex == 8)
            {
                return BALATRO_RED; // Theme default
            }

            // Predefined color palette
            return colorIndex.Value switch
            {
                0 => BALATRO_RED,
                1 => BALATRO_BLUE,
                2 => new SKColor(0, 255, 0), // Green
                3 => new SKColor(255, 215, 0), // Gold
                4 => new SKColor(255, 105, 180), // Hot Pink
                5 => new SKColor(138, 43, 226), // Blue Violet
                6 => new SKColor(255, 140, 0), // Dark Orange
                7 => new SKColor(0, 255, 255), // Cyan
                _ => BALATRO_RED, // Default
            };
        }
    }
}

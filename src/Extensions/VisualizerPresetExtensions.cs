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
                SpinTimeSpeed = preset.TimeSpeed * 0.5f, // Spin typically slower than main time

                // Colors (convert from preset color indices)
                MainColor = GetColorFromIndex(preset.MainColor),
                AccentColor = GetColorFromIndex(preset.AccentColor),
                BackgroundColor = new SKColor(30, 43, 45), // Default Balatro dark teal

                // Effect parameters (affected by AudioIntensity and ParallaxStrength)
                Contrast = 2.0f, // Base contrast
                SpinAmount = 0.3f * preset.AudioIntensity, // Audio intensity affects spin
                ParallaxX = 0f, // Will be set by mouse movement
                ParallaxY = 0f,
                ZoomScale = 0f, // Base zoom
                SaturationAmount = preset.AudioIntensity * 0.5f, // Subtle saturation boost
                SaturationAmount2 = preset.AudioIntensity * 0.3f,
                PixelSize = 1440.0f, // Default pixel size (higher = less pixelation)
                SpinEase = 0.5f,
                LoopCount = 5.0f,
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
                MainColor = new SKColor(20, 20, 30), // Very dark blue-grey
                AccentColor = new SKColor(50, 50, 60), // Slightly lighter grey
                BackgroundColor = new SKColor(10, 10, 15), // Almost black
                Contrast = 1.0f, // Low contrast
                SpinAmount = 0.1f,
                ParallaxX = 0f,
                ParallaxY = 0f,
                ZoomScale = 0f,
                SaturationAmount = 0f, // No saturation
                SaturationAmount2 = 0f,
                PixelSize = 200.0f, // Heavy pixelation (lower = more pixelated)
                SpinEase = 0.5f,
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
                SpinTimeSpeed = 0.5f,
                MainColor = new SKColor(255, 76, 64), // Balatro Red
                AccentColor = new SKColor(0, 147, 255), // Balatro Blue
                BackgroundColor = new SKColor(30, 43, 45), // Balatro Dark Teal
                Contrast = 2.0f,
                SpinAmount = 0.3f,
                ParallaxX = 0f,
                ParallaxY = 0f,
                ZoomScale = 0f,
                SaturationAmount = 0f,
                SaturationAmount2 = 0f,
                PixelSize = 1440.0f, // No pixelation
                SpinEase = 0.5f,
                LoopCount = 5.0f,
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
                // Theme default (Balatro Red)
                return new SKColor(255, 76, 64);
            }

            // Predefined color palette
            return colorIndex.Value switch
            {
                0 => new SKColor(255, 76, 64), // Red
                1 => new SKColor(0, 147, 255), // Blue
                2 => new SKColor(0, 255, 0), // Green
                3 => new SKColor(255, 215, 0), // Gold
                4 => new SKColor(255, 105, 180), // Hot Pink
                5 => new SKColor(138, 43, 226), // Blue Violet
                6 => new SKColor(255, 140, 0), // Dark Orange
                7 => new SKColor(0, 255, 255), // Cyan
                _ => new SKColor(255, 76, 64), // Default Red
            };
        }
    }
}

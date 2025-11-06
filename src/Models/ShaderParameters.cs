using SkiaSharp;

namespace BalatroSeedOracle.Models
{
    /// <summary>
    /// Pure data class representing raw shader uniform parameters.
    /// Direct 1:1 mapping to BalatroShaderBackground uniforms.
    /// Used as the target for LERP operations in VisualizerPresetTransition.
    /// </summary>
    public class ShaderParameters
    {
        // Time animation speeds
        public float TimeSpeed { get; set; } = 1.0f;
        public float SpinTimeSpeed { get; set; } = 1.0f;

        // Colors (SKColor supports LERP via RGBA components)
        public SKColor MainColor { get; set; } = new SKColor(255, 76, 64); // Balatro Red
        public SKColor AccentColor { get; set; } = new SKColor(0, 147, 255); // Balatro Blue
        public SKColor BackgroundColor { get; set; } = new SKColor(30, 43, 45); // Balatro Dark Teal

        // Effect parameters
        public float Contrast { get; set; } = 2.0f;
        public float SpinAmount { get; set; } = 0.3f;
        public float ParallaxX { get; set; } = 0f;
        public float ParallaxY { get; set; } = 0f;
        public float ZoomScale { get; set; } = 0f;
        public float SaturationAmount { get; set; } = 0f;
        public float SaturationAmount2 { get; set; } = 0f;
        public float PixelSize { get; set; } = 1440.0f;
        public float SpinEase { get; set; } = 0.5f;
        public float LoopCount { get; set; } = 5.0f;

        /// <summary>
        /// Creates a deep copy of this ShaderParameters instance
        /// </summary>
        public ShaderParameters Clone()
        {
            return new ShaderParameters
            {
                TimeSpeed = this.TimeSpeed,
                SpinTimeSpeed = this.SpinTimeSpeed,
                MainColor = this.MainColor,
                AccentColor = this.AccentColor,
                BackgroundColor = this.BackgroundColor,
                Contrast = this.Contrast,
                SpinAmount = this.SpinAmount,
                ParallaxX = this.ParallaxX,
                ParallaxY = this.ParallaxY,
                ZoomScale = this.ZoomScale,
                SaturationAmount = this.SaturationAmount,
                SaturationAmount2 = this.SaturationAmount2,
                PixelSize = this.PixelSize,
                SpinEase = this.SpinEase,
                LoopCount = this.LoopCount
            };
        }
    }
}

using SkiaSharp;

namespace BalatroSeedOracle.Models
{
    public class ShaderParametersConfig
    {
        public string? MainColor { get; set; }
        public string? AccentColor { get; set; }
        public string? BackgroundColor { get; set; }

        public float? TimeSpeed { get; set; }
        public float? SpinTimeSpeed { get; set; }
        public float? Contrast { get; set; }
        public float? SpinAmount { get; set; }
        public float? ParallaxX { get; set; }
        public float? ParallaxY { get; set; }
        public float? ZoomScale { get; set; }
        public float? SaturationAmount { get; set; }
        public float? SaturationAmount2 { get; set; }
        public float? PixelSize { get; set; }
        public float? SpinEase { get; set; }
        public float? LoopCount { get; set; }

        public ShaderParameters ToShaderParameters(ShaderParameters defaults)
        {
            var p = new ShaderParameters
            {
                TimeSpeed = TimeSpeed ?? defaults.TimeSpeed,
                SpinTimeSpeed = SpinTimeSpeed ?? defaults.SpinTimeSpeed,
                MainColor = TryParseColor(MainColor) ?? defaults.MainColor,
                AccentColor = TryParseColor(AccentColor) ?? defaults.AccentColor,
                BackgroundColor = TryParseColor(BackgroundColor) ?? defaults.BackgroundColor,
                Contrast = Contrast ?? defaults.Contrast,
                SpinAmount = SpinAmount ?? defaults.SpinAmount,
                ParallaxX = ParallaxX ?? defaults.ParallaxX,
                ParallaxY = ParallaxY ?? defaults.ParallaxY,
                ZoomScale = ZoomScale ?? defaults.ZoomScale,
                SaturationAmount = SaturationAmount ?? defaults.SaturationAmount,
                SaturationAmount2 = SaturationAmount2 ?? defaults.SaturationAmount2,
                PixelSize = PixelSize ?? defaults.PixelSize,
                SpinEase = SpinEase ?? defaults.SpinEase,
                LoopCount = LoopCount ?? defaults.LoopCount,
            };
            return p;
        }

        private SKColor? TryParseColor(string? s)
        {
            if (string.IsNullOrWhiteSpace(s)) return null;
            try
            {
                return SKColor.Parse(s);
            }
            catch
            {
                return null;
            }
        }
    }
}
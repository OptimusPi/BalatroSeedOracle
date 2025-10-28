using System;
using System.Globalization;
using Avalonia.Data.Converters;
using Avalonia.Media;

namespace BalatroSeedOracle.Converters
{
    /// <summary>
    /// Converts brightness value (0.0 to 1.0) to LED indicator color.
    /// Interpolates between dim gray (0.0) and bright red (1.0).
    /// </summary>
    public class BoolToLedColorConverter : IValueConverter
    {
        public object? Convert(
            object? value,
            Type targetType,
            object? parameter,
            CultureInfo culture
        )
        {
            double brightness = 0.0;

            if (value is double d)
                brightness = d;
            else if (value is bool isLit)
                brightness = isLit ? 1.0 : 0.0;

            // Clamp brightness between 0 and 1
            brightness = Math.Max(0.0, Math.Min(1.0, brightness));

            // Interpolate between dim gray (60,60,60) and bright red (255,50,50)
            byte r = (byte)(60 + brightness * (255 - 60));
            byte g = (byte)(60 + brightness * (50 - 60));
            byte b = (byte)(60 + brightness * (50 - 60));

            return Color.FromRgb(r, g, b);
        }

        public object? ConvertBack(
            object? value,
            Type targetType,
            object? parameter,
            CultureInfo culture
        )
        {
            throw new NotImplementedException();
        }
    }
}

using System;
using System.Globalization;
using Avalonia.Data.Converters;

namespace BalatroSeedOracle.Converters
{
    public class TruncateConverter : IValueConverter
    {
        public object Convert(
            object? value,
            Type targetType,
            object? parameter,
            CultureInfo culture
        )
        {
            var input = value as string ?? string.Empty;

            // Default max length
            var max = 25;

            if (parameter is int intParam)
            {
                max = intParam;
            }
            else if (parameter is string strParam && int.TryParse(strParam, out var parsed))
            {
                max = parsed;
            }

            if (input.Length <= max)
                return input;

            // Use ellipsis; avoid negative substring length
            var take = Math.Max(0, max - 1);
            return input.Substring(0, take) + "…";
        }

        public object ConvertBack(
            object? value,
            Type targetType,
            object? parameter,
            CultureInfo culture
        )
        {
            return value ?? string.Empty;
        }
    }
}

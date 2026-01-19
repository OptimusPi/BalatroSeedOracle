using System;
using System.Globalization;
using Avalonia.Data.Converters;

namespace BalatroSeedOracle.Converters
{
    public class AnteCheckboxConverter : IValueConverter
    {
        public static readonly AnteCheckboxConverter Instance = new();

        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value is int[] antes && parameter is string anteStr && int.TryParse(anteStr, out int ante))
            {
                return Array.IndexOf(antes, ante) >= 0;
            }
            return false;
        }

        public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            return null;
        }
    }
}

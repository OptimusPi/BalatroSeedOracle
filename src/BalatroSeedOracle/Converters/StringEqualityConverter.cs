using System;
using System.Globalization;
using Avalonia.Data.Converters;

namespace BalatroSeedOracle.Converters
{
    public class StringEqualityConverter : IValueConverter
    {
        public static readonly StringEqualityConverter Instance = new();

        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value is string str && parameter is string compareStr)
            {
                return string.Equals(str, compareStr, StringComparison.OrdinalIgnoreCase);
            }
            return false;
        }

        public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value is bool isChecked && isChecked && parameter is string str)
            {
                return str;
            }
            return null;
        }
    }
}

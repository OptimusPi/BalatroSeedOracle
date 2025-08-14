using System;
using System.Globalization;
using Avalonia.Data.Converters;

namespace BalatroSeedOracle.Converters
{
    public class IntArrayIndexConverter : IValueConverter
    {
        public static readonly IntArrayIndexConverter Instance = new();
        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value is int[] arr && parameter is string s && int.TryParse(s, out var idx))
            {
                if (idx >= 0 && idx < arr.Length) return arr[idx];
            }
            return 0;
        }

        public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
            => throw new NotSupportedException();
    }
}

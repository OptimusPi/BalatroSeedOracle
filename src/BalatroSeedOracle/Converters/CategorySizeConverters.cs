using System;
using System.Globalization;
using Avalonia.Data.Converters;

namespace BalatroSeedOracle.Converters
{
    /// <summary>
    /// Returns standard card width (71px - original sprite size)
    /// </summary>
    public class CategoryToWidthConverter : IValueConverter
    {
        public static readonly CategoryToWidthConverter Instance = new();

        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            return 71.0; // Standard card width
        }

        public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            throw new NotSupportedException("One-way binding only");
        }
    }

    /// <summary>
    /// Returns standard card height (95px - proportional to 71px width)
    /// </summary>
    public class CategoryToHeightConverter : IValueConverter
    {
        public static readonly CategoryToHeightConverter Instance = new();

        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            return 95.0; // Standard card height
        }

        public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            throw new NotSupportedException("One-way binding only");
        }
    }
}

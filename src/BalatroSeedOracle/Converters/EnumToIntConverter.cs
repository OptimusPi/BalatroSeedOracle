using System;
using System.Globalization;
using Avalonia.Data.Converters;

namespace BalatroSeedOracle.Converters
{
    /// <summary>
    /// Converts an enum value to its integer representation and back.
    /// Useful for binding enum properties to controls that expect integer indices.
    /// </summary>
    public class EnumToIntConverter : IValueConverter
    {
        public static readonly EnumToIntConverter Instance = new();

        public object? Convert(
            object? value,
            Type targetType,
            object? parameter,
            CultureInfo culture
        )
        {
            if (value == null)
                return 0;

            if (value is Enum enumValue)
            {
                return System.Convert.ToInt32(enumValue);
            }

            return 0;
        }

        public object? ConvertBack(
            object? value,
            Type targetType,
            object? parameter,
            CultureInfo culture
        )
        {
            if (value == null)
                return null;

            if (value is int intValue && targetType.IsEnum)
            {
                return Enum.ToObject(targetType, intValue);
            }

            return null;
        }
    }
}

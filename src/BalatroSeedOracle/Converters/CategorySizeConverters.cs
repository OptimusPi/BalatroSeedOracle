using System;
using System.Globalization;
using Avalonia.Data.Converters;

namespace BalatroSeedOracle.Converters
{
    /// <summary>
    /// Returns the display width for an item by category.
    /// Cards are 71x95 native; tags and boss blinds are 34x34 chips —
    /// rendering those in a card-sized box scales them ~2X too big.
    /// </summary>
    public class CategoryToWidthConverter : IValueConverter
    {
        public static readonly CategoryToWidthConverter Instance = new();

        public object? Convert(
            object? value,
            Type targetType,
            object? parameter,
            CultureInfo culture
        )
        {
            return value is string category && (category == "Tags" || category == "Bosses")
                ? 34.0
                : 71.0;
        }

        public object? ConvertBack(
            object? value,
            Type targetType,
            object? parameter,
            CultureInfo culture
        )
        {
            throw new NotSupportedException("One-way binding only");
        }
    }

    /// <summary>
    /// Returns the display height for an item by category (see CategoryToWidthConverter).
    /// </summary>
    public class CategoryToHeightConverter : IValueConverter
    {
        public static readonly CategoryToHeightConverter Instance = new();

        public object? Convert(
            object? value,
            Type targetType,
            object? parameter,
            CultureInfo culture
        )
        {
            return value is string category && (category == "Tags" || category == "Bosses")
                ? 34.0
                : 95.0;
        }

        public object? ConvertBack(
            object? value,
            Type targetType,
            object? parameter,
            CultureInfo culture
        )
        {
            throw new NotSupportedException("One-way binding only");
        }
    }
}

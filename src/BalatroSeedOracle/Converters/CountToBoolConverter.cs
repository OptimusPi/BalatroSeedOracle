using System;
using System.Globalization;
using Avalonia.Data.Converters;

namespace BalatroSeedOracle.Converters
{
    /// <summary>
    /// Converts count to boolean for visibility logic.
    /// Used to hide OR/AND trays when the other has items.
    /// </summary>
    public class CountToBoolConverter : IValueConverter
    {
        public static readonly CountToBoolConverter IsZero = new();

        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value is int count)
            {
                // IsZero: Returns true when count == 0 (visible when other tray empty)
                return count == 0;
            }
            return true; // Default to visible
        }

        public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            throw new NotSupportedException("One-way binding only");
        }
    }
}

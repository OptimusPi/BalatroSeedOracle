using System;
using System.Globalization;
using Avalonia.Controls;
using Avalonia.Data.Converters;

namespace BalatroSeedOracle.Converters
{
    /// <summary>
    /// Converts boolean to GridLength for collapsible drop zones.
    /// When expanded (true): returns * (star) to take available space
    /// When collapsed (false): returns fixed 40px height
    /// </summary>
    public class BoolToGridLengthConverter : IValueConverter
    {
        public static readonly BoolToGridLengthConverter Instance = new();

        public object? Convert(
            object? value,
            Type targetType,
            object? parameter,
            CultureInfo culture
        )
        {
            if (value is bool isExpanded)
            {
                // When expanded: take all available space (*)
                // When collapsed: fixed 30px height (compact size for label + badge)
                return isExpanded ? new GridLength(1, GridUnitType.Star) : new GridLength(30);
            }
            return new GridLength(30); // Default to collapsed
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

using System;
using System.Globalization;
using Avalonia.Data.Converters;

namespace BalatroSeedOracle.Converters
{
    /// <summary>
    /// Converts boolean IsExpanded property to expand/collapse icon
    /// True (expanded) -> "▲" (collapse)
    /// False (collapsed) -> "▼" (expand)
    /// </summary>
    public class BoolToExpandIconConverter : IValueConverter
    {
        public static readonly BoolToExpandIconConverter Instance = new();

        public object? Convert(
            object? value,
            Type targetType,
            object? parameter,
            CultureInfo culture
        )
        {
            if (value is bool isExpanded)
            {
                return isExpanded ? "▲" : "▼";
            }
            return "▼"; // Default to collapsed
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

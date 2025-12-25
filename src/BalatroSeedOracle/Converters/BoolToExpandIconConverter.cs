using System;
using System.Globalization;
using Avalonia.Data.Converters;
using IconPacks.Avalonia;
using IconPacks.Avalonia.Material;

namespace BalatroSeedOracle.Converters
{
    /// <summary>
    /// Converts boolean IsExpanded property to expand/collapse icon
    /// True (expanded) -> ChevronUp
    /// False (collapsed) -> ChevronDown
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
                return isExpanded ? PackIconMaterialKind.ChevronUp : PackIconMaterialKind.ChevronDown;
            }
            return PackIconMaterialKind.ChevronDown; // Default to collapsed
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

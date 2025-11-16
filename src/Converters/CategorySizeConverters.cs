using System;
using System.Globalization;
using Avalonia.Data.Converters;

namespace BalatroSeedOracle.Converters
{
    /// <summary>
    /// Converter that returns appropriate width based on item category
    /// Tags and Bosses get wider but shorter dimensions (more horizontal)
    /// </summary>
    public class CategoryToWidthConverter : IValueConverter
    {
        public static readonly CategoryToWidthConverter Instance = new();

        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value is string category)
            {
                var lowerCategory = category.ToLower();

                // Tags and Bosses get wider horizontal layout (80% width)
                if (lowerCategory == "tags" || lowerCategory == "bosses")
                {
                    return 72.0; // Wider for better horizontal display
                }
            }

            // Default size for cards, jokers, etc.
            return 64.0;
        }

        public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    /// <summary>
    /// Converter that returns appropriate height based on item category
    /// Tags and Bosses are 50% the height of regular items
    /// </summary>
    public class CategoryToHeightConverter : IValueConverter
    {
        public static readonly CategoryToHeightConverter Instance = new();

        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value is string category)
            {
                var lowerCategory = category.ToLower();

                // Tags and Bosses are 50% height (43 instead of 86)
                if (lowerCategory == "tags" || lowerCategory == "bosses")
                {
                    return 43.0; // 50% of normal height
                }
            }

            // Default size for cards, jokers, etc.
            return 86.0;
        }

        public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
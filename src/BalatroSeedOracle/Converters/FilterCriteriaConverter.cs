using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Avalonia.Data.Converters;

namespace BalatroSeedOracle.Converters
{
    /// <summary>
    /// Converts an array of filter criteria item keys to human-readable strings
    /// Example: "Negative Blueprint @ Ante 1-2"
    /// </summary>
    public class FilterCriteriaConverter : IValueConverter
    {
        public object? Convert(
            object? value,
            Type targetType,
            object? parameter,
            CultureInfo culture
        )
        {
            if (value is not List<string> itemKeys || itemKeys.Count == 0)
            {
                return new List<string> { "(No criteria specified)" };
            }

            var result = new List<string>();

            foreach (var itemKey in itemKeys)
            {
                // Parse the item key format: "ItemName" or "ItemName@Config"
                var parts = itemKey.Split('@');
                var itemName = parts[0].Trim();

                // Format the item name nicely (handle underscores, etc.)
                itemName = FormatItemName(itemName);

                if (parts.Length > 1)
                {
                    // Has configuration, format it
                    var config = parts[1].Trim();
                    result.Add($"{itemName} @ {config}");
                }
                else
                {
                    // No configuration
                    result.Add(itemName);
                }
            }

            return result;
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

        private string FormatItemName(string name)
        {
            // Replace underscores with spaces
            name = name.Replace('_', ' ');

            // Handle common patterns
            if (name.Contains("Joker", StringComparison.OrdinalIgnoreCase))
            {
                // Joker names are usually fine as-is
                return name;
            }

            return name;
        }
    }
}

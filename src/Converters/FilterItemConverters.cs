using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Avalonia.Data.Converters;
using BalatroSeedOracle.Models;
using Motely;

namespace BalatroSeedOracle.Converters
{
    /// <summary>
    /// Converts an item name string to a formatted display string using FormatUtils.FormatItem().
    /// Supports ItemConfig, string, and handles Edition/Stickers/Seal/Enhancement formatting.
    /// </summary>
    public class ItemNameToFormattedStringConverter : IValueConverter
    {
        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            return value switch
            {
                ItemConfig config => FormatItemConfig(config),
                string str => FormatUtils.FormatDisplayName(str),
                _ => value?.ToString() ?? ""
            };
        }

        private static string FormatItemConfig(ItemConfig config)
        {
            var parts = new List<string>();

            // Add Edition (Polychrome, Negative, Foil, Holographic)
            if (!string.IsNullOrEmpty(config.Edition) && config.Edition != "none")
            {
                parts.Add(config.Edition);
            }

            // Add Seal (Red, Blue, Gold, Purple)
            if (!string.IsNullOrEmpty(config.Seal) && config.Seal != "None")
            {
                parts.Add(config.Seal);
            }

            // Add Enhancement (Bonus, Mult, Wild, Glass, Steel, Stone, Lucky)
            if (!string.IsNullOrEmpty(config.Enhancement) && config.Enhancement != "None")
            {
                parts.Add(config.Enhancement);
            }

            // Add Stickers (Eternal, Perishable, Rental)
            if (config.Stickers != null && config.Stickers.Count > 0)
            {
                parts.AddRange(config.Stickers);
            }

            // Add formatted item name with wildcard handling
            parts.Add(FormatItemNameWithWildcards(config.ItemName));

            return string.Join(" ", parts);
        }

        private static string FormatItemNameWithWildcards(string? itemName)
        {
            if (string.IsNullOrWhiteSpace(itemName))
                return "Any";

            // Handle wildcards: AnyJoker -> "Any Joker", AnyRare -> "Any Rare Joker"
            // Use case-insensitive comparison to catch variations
            var normalized = itemName.Trim();

            if (normalized.Equals("AnyJoker", StringComparison.OrdinalIgnoreCase))
                return "Any Joker";
            if (normalized.Equals("AnyCommon", StringComparison.OrdinalIgnoreCase))
                return "Any Common Joker";
            if (normalized.Equals("AnyUncommon", StringComparison.OrdinalIgnoreCase))
                return "Any Uncommon Joker";
            if (normalized.Equals("AnyRare", StringComparison.OrdinalIgnoreCase))
                return "Any Rare Joker";
            if (normalized.Equals("AnyLegendary", StringComparison.OrdinalIgnoreCase))
                return "Any Legendary Joker";
            if (normalized.Equals("AnyTarot", StringComparison.OrdinalIgnoreCase))
                return "Any Tarot";
            if (normalized.Equals("AnySpectral", StringComparison.OrdinalIgnoreCase))
                return "Any Spectral";
            if (normalized.Equals("AnyPlanet", StringComparison.OrdinalIgnoreCase))
                return "Any Planet";

            return FormatUtils.FormatDisplayName(itemName);
        }

        public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    /// <summary>
    /// Formats a list of ante integers into a human-readable range string.
    /// Examples: [1] -> "1", [1,2,3] -> "1-3", [1,2,3,5,8] -> "1-3, 5, 8"
    /// Supports ItemConfig, List<int>, and int[] types.
    /// </summary>
    public class AntesFormatterConverter : IValueConverter
    {
        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            List<int>? antes = value switch
            {
                ItemConfig config => config.Antes,
                List<int> list => list,
                int[] array => array.ToList(),
                _ => null
            };

            if (antes == null || antes.Count == 0)
                return "Any";

            try
            {
                var sorted = antes.Distinct().OrderBy(x => x).ToList();
                return FormatAntesWithRanges(sorted);
            }
            catch
            {
                return string.Join(", ", antes);
            }
        }

        public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }

        private static string FormatAntesWithRanges(List<int> sorted)
        {
            if (sorted.Count == 0) return "Any";
            if (sorted.Count == 1) return sorted[0].ToString();

            var result = new List<string>();
            int rangeStart = sorted[0];
            int rangeEnd = sorted[0];

            for (int i = 1; i < sorted.Count; i++)
            {
                if (sorted[i] == rangeEnd + 1)
                {
                    // Continue the range
                    rangeEnd = sorted[i];
                }
                else
                {
                    // End the current range and start a new one
                    result.Add(FormatRange(rangeStart, rangeEnd));
                    rangeStart = sorted[i];
                    rangeEnd = sorted[i];
                }
            }

            // Add the last range
            result.Add(FormatRange(rangeStart, rangeEnd));

            return string.Join(", ", result);
        }

        private static string FormatRange(int start, int end)
        {
            if (start == end)
                return start.ToString();
            else
                return $"{start}-{end}";
        }
    }

    /// <summary>
    /// Checks if a deck name is "Erratic Deck"
    /// Use parameter="Inverse" to invert the result
    /// </summary>
    public class IsErraticDeckConverter : IValueConverter
    {
        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value is string deckName)
            {
                bool isErratic = deckName?.Equals("Erratic Deck", StringComparison.OrdinalIgnoreCase) == true;
                bool shouldInvert = parameter is string param && param.Equals("Inverse", StringComparison.OrdinalIgnoreCase);
                return shouldInvert ? !isErratic : isErratic;
            }

            // Default: if no deck name, it's not erratic
            bool defaultResult = false;
            bool shouldInvertDefault = parameter is string p && p.Equals("Inverse", StringComparison.OrdinalIgnoreCase);
            return shouldInvertDefault ? !defaultResult : defaultResult;
        }

        public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    /// <summary>
    /// Inverts a boolean value
    /// </summary>
    public class InverseBoolConverter : IValueConverter
    {
        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value is bool boolValue)
            {
                return !boolValue;
            }
            return true;
        }

        public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value is bool boolValue)
            {
                return !boolValue;
            }
            return true;
        }
    }
}

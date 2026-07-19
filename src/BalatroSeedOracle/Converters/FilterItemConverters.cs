using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Avalonia.Data.Converters;
using BalatroSeedOracle.Services;
using Motely;
using Motely.Filters.Jaml;

namespace BalatroSeedOracle.Converters
{
    /// <summary>
    /// Converts an item name string to a formatted display string using FormatUtils.FormatItem().
    /// Supports IJamlClause, string, and handles Edition/Stickers/Seal/Enhancement formatting.
    /// </summary>
    public class ItemNameToFormattedStringConverter : IValueConverter
    {
        public object? Convert(
            object? value,
            Type targetType,
            object? parameter,
            CultureInfo culture
        )
        {
            return value switch
            {
                IJamlClause clause => FormatJamlClause(clause),
                string str => FormatUtils.FormatDisplayName(str),
                _ => value?.ToString() ?? "",
            };
        }

        private static string FormatJamlClause(IJamlClause clause)
        {
            var itemName = clause.GetValueName();
            var stickers = clause.GetStickerStrings();
            var edition = clause.GetEditionString();
            bool isSoulJoker = clause is LegendaryJokerClause;

            if (IsWildcardItem(itemName))
            {
                var parts = new List<string>();

                if (stickers != null && stickers.Length > 0)
                {
                    parts.AddRange(stickers);
                }

                if (!string.IsNullOrEmpty(edition) && edition != "none")
                {
                    parts.Add(edition);
                }

                parts.Add(FormatItemNameWithWildcards(itemName, isSoulJoker));

                return string.Join(" ", parts);
            }

            var typeName = clause.GetTypeName() ?? "";
            bool isJoker = typeName is "joker" or "souljoker";
            bool isStandardCard = typeName is "standardcard";

            var cardParts = new List<string>();

            if (isJoker)
            {
                if (stickers != null && stickers.Length > 0)
                {
                    cardParts.AddRange(stickers);
                }

                if (!string.IsNullOrEmpty(edition) && !edition.Equals("none", StringComparison.OrdinalIgnoreCase))
                {
                    cardParts.Add(edition);
                }

                cardParts.Add(!string.IsNullOrEmpty(itemName) ? itemName : "Unknown");
            }
            else if (isStandardCard)
            {
                var seal = clause.GetSealString();
                var enhancement = clause.GetEnhancementString();
                string? rank = clause is StandardCardClause sc ? sc.Rank?.ToString() : null;
                string? suit = clause is StandardCardClause sc2 ? sc2.Suit?.ToString() : null;

                if (!string.IsNullOrEmpty(seal))
                {
                    cardParts.Add($"{seal} Seal");
                }

                if (!string.IsNullOrEmpty(enhancement) && enhancement != "None")
                {
                    cardParts.Add(enhancement);
                }

                if (!string.IsNullOrEmpty(edition) && edition != "Negative")
                {
                    cardParts.Add(edition);
                }

                if (!string.IsNullOrEmpty(rank) && !string.IsNullOrEmpty(suit))
                {
                    cardParts.Add($"{rank} of {suit}");
                }
                else if (!string.IsNullOrEmpty(itemName))
                {
                    cardParts.Add(itemName);
                }
            }
            else
            {
                if (!string.IsNullOrEmpty(edition) && !edition.Equals("none", StringComparison.OrdinalIgnoreCase))
                {
                    cardParts.Add(edition);
                }

                cardParts.Add(!string.IsNullOrEmpty(itemName) ? itemName : "Unknown");
            }

            return string.Join(" ", cardParts);
        }

        private static bool IsWildcardItem(string? itemName)
        {
            if (string.IsNullOrWhiteSpace(itemName))
                return true; // Empty names are wildcards ("Any")

            var normalized = itemName.Trim();
            return normalized.Equals("AnyJoker", StringComparison.OrdinalIgnoreCase)
                || normalized.Equals("AnyCommon", StringComparison.OrdinalIgnoreCase)
                || normalized.Equals("AnyUncommon", StringComparison.OrdinalIgnoreCase)
                || normalized.Equals("AnyRare", StringComparison.OrdinalIgnoreCase)
                || normalized.Equals("AnyLegendary", StringComparison.OrdinalIgnoreCase)
                || normalized.Equals("AnyTarot", StringComparison.OrdinalIgnoreCase)
                || normalized.Equals("AnyPlanet", StringComparison.OrdinalIgnoreCase)
                || normalized.Equals("Any", StringComparison.OrdinalIgnoreCase);
        }

        private static string FormatItemNameWithWildcards(
            string? itemName,
            bool isSoulJoker = false
        )
        {
            if (string.IsNullOrWhiteSpace(itemName))
            {
                // If it's a Soul Joker with no specific name, it's "Any Soul Joker"
                return isSoulJoker ? "Any Soul Joker" : "Any";
            }

            // Handle wildcards: AnyJoker -> "Any Joker", AnyRare -> "Any Rare Joker"
            // Use case-insensitive comparison to catch variations
            var normalized = itemName.Trim();

            // Handle Soul Joker wildcards
            if (isSoulJoker)
            {
                if (
                    normalized.Equals("AnyJoker", StringComparison.OrdinalIgnoreCase)
                    || normalized.Equals("Any", StringComparison.OrdinalIgnoreCase)
                )
                    return "Any Soul Joker";
                if (normalized.Equals("AnyCommon", StringComparison.OrdinalIgnoreCase))
                    return "Any Common Soul Joker";
                if (normalized.Equals("AnyUncommon", StringComparison.OrdinalIgnoreCase))
                    return "Any Uncommon Soul Joker";
                if (normalized.Equals("AnyRare", StringComparison.OrdinalIgnoreCase))
                    return "Any Rare Soul Joker";
                if (normalized.Equals("AnyLegendary", StringComparison.OrdinalIgnoreCase))
                    return "Any Legendary Soul Joker";
            }

            // Handle regular joker wildcards
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
            if (normalized.Equals("AnyPlanet", StringComparison.OrdinalIgnoreCase))
                return "Any Planet";

            return FormatUtils.FormatDisplayName(itemName);
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
    /// Formats a list of ante integers into a human-readable range string.
    /// Examples: [1] -> "1", [1,2,3] -> "1-3", [1,2,3,5,8] -> "1-3, 5, 8"
    /// Supports IJamlClause, List<int>, and int[] types.
    /// </summary>
    public class AntesFormatterConverter : IValueConverter
    {
        public object? Convert(
            object? value,
            Type targetType,
            object? parameter,
            CultureInfo culture
        )
        {
            List<int>? antes = value switch
            {
                IJamlClause clause => clause.GetAntes()?.ToList(),
                List<int> list => list,
                int[] array => array.ToList(),
                _ => null,
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

        public object? ConvertBack(
            object? value,
            Type targetType,
            object? parameter,
            CultureInfo culture
        )
        {
            throw new NotSupportedException("One-way binding only");
        }

        private static string FormatAntesWithRanges(List<int> sorted)
        {
            if (sorted.Count == 0)
                return "Any";
            if (sorted.Count == 1)
                return sorted[0].ToString();

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
        public object? Convert(
            object? value,
            Type targetType,
            object? parameter,
            CultureInfo culture
        )
        {
            if (value is string deckName)
            {
                bool isErratic =
                    deckName?.Equals("Erratic Deck", StringComparison.OrdinalIgnoreCase) == true;
                bool shouldInvert =
                    parameter is string param
                    && param.Equals("Inverse", StringComparison.OrdinalIgnoreCase);
                return shouldInvert ? !isErratic : isErratic;
            }

            // Default: if no deck name, it's not erratic
            bool defaultResult = false;
            bool shouldInvertDefault =
                parameter is string p && p.Equals("Inverse", StringComparison.OrdinalIgnoreCase);
            return shouldInvertDefault ? !defaultResult : defaultResult;
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
    /// Inverts a boolean value
    /// </summary>
    public class InverseBoolConverter : IValueConverter
    {
        public object? Convert(
            object? value,
            Type targetType,
            object? parameter,
            CultureInfo culture
        )
        {
            if (value is bool boolValue)
            {
                return !boolValue;
            }
            return true;
        }

        public object? ConvertBack(
            object? value,
            Type targetType,
            object? parameter,
            CultureInfo culture
        )
        {
            if (value is bool boolValue)
            {
                return !boolValue;
            }
            return true;
        }
    }
}

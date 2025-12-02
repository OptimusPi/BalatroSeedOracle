using System;
using System.Globalization;
using Avalonia;
using Avalonia.Data.Converters;
using Avalonia.Media;

namespace BalatroSeedOracle.Converters
{
    /// <summary>
    /// Converts operator type string ("OR", "AND", or "BannedItems") to appropriate color brush
    /// Uses the SAME colors as SHOULD (Green), MUST (Blue), and MustNot (Red) drop zones from App.axaml resources
    /// </summary>
    public class OperatorColorConverter : IValueConverter
    {
        public static readonly OperatorColorConverter Instance = new();

        public object? Convert(
            object? value,
            Type targetType,
            object? parameter,
            CultureInfo culture
        )
        {
            if (value is not string operatorType)
                throw new ArgumentException(
                    $"Expected string operatorType, got {value?.GetType().Name ?? "null"}"
                );

            var colorKey = operatorType switch
            {
                "OR" => "Green", // Matches SHOULD zone
                "AND" => "Blue", // Matches MUST zone
                "BannedItems" => "Red", // Matches MustNot logic - RED theme
                _ => throw new ArgumentException($"Unknown operator type: {operatorType}"),
            };

            if (
                Application.Current?.Resources.TryGetResource(colorKey, null, out var resource)
                    == true
                && resource is IBrush brush
            )
            {
                return brush;
            }
            throw new InvalidOperationException(
                $"Color resource '{colorKey}' not found in App.axaml! Fix your resources!"
            );
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
    /// Returns true if count is less than 5
    /// </summary>
    public class LessThanFiveConverter : IValueConverter
    {
        public static readonly LessThanFiveConverter Instance = new();

        public object? Convert(
            object? value,
            Type targetType,
            object? parameter,
            CultureInfo culture
        )
        {
            if (value is int count)
            {
                return count < 5;
            }
            return false;
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
    /// Returns true if count is greater than or equal to 5
    /// </summary>
    public class GreaterThanFourConverter : IValueConverter
    {
        public static readonly GreaterThanFourConverter Instance = new();

        public object? Convert(
            object? value,
            Type targetType,
            object? parameter,
            CultureInfo culture
        )
        {
            if (value is int count)
            {
                return count >= 5;
            }
            return false;
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
    /// Returns true if count equals zero
    /// </summary>
    public class EqualsZeroConverter : IValueConverter
    {
        public static readonly EqualsZeroConverter Instance = new();

        public object? Convert(
            object? value,
            Type targetType,
            object? parameter,
            CultureInfo culture
        )
        {
            if (value is int count)
            {
                return count == 0;
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
            throw new NotSupportedException("One-way binding only");
        }
    }

    /// <summary>
    /// Returns true if operator type is "BannedItems"
    /// Used to conditionally apply the "banned" CSS class
    /// </summary>
    public class IsBannedItemsConverter : IValueConverter
    {
        public static readonly IsBannedItemsConverter Instance = new();

        public object? Convert(
            object? value,
            Type targetType,
            object? parameter,
            CultureInfo culture
        )
        {
            if (value is string operatorType)
            {
                return operatorType == "BannedItems";
            }
            return false;
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

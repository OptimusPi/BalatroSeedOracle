using System;
using System.Globalization;
using Avalonia;
using Avalonia.Data.Converters;
using Avalonia.Media;

namespace BalatroSeedOracle.Converters
{
    /// <summary>
    /// Converts boolean to brush - true = Green/AccentGreen, false = DarkBackground
    /// Parameter can specify "Green" for border color
    /// </summary>
    public class BoolToBrushConverter : IValueConverter
    {
        public static readonly BoolToBrushConverter Instance = new();

        public object? Convert(
            object? value,
            Type targetType,
            object? parameter,
            CultureInfo culture
        )
        {
            if (value is not bool isTrue)
                return GetDefaultBrush();

            // If parameter is "Green", use green for true, otherwise use dark background
            if (parameter is string param && param == "Green")
            {
                if (isTrue)
                {
                    if (
                        Application.Current?.Resources.TryGetResource(
                            "AccentGreen",
                            null,
                            out var greenRes
                        ) == true
                        && greenRes is IBrush greenBrush
                    )
                        return greenBrush;
                    return Brushes.Green;
                }
                else
                {
                    if (
                        Application.Current?.Resources.TryGetResource(
                            "ModalBorder",
                            null,
                            out var borderRes
                        ) == true
                        && borderRes is IBrush borderBrush
                    )
                        return borderBrush;
                    return Brushes.Gray;
                }
            }

            // Default: background color
            if (isTrue)
            {
                if (
                    Application.Current?.Resources.TryGetResource(
                        "AccentGreen",
                        null,
                        out var greenRes
                    ) == true
                    && greenRes is IBrush greenBrush
                )
                    return greenBrush;
                return Brushes.Green;
            }
            else
            {
                if (
                    Application.Current?.Resources.TryGetResource(
                        "DarkBackground",
                        null,
                        out var darkRes
                    ) == true
                    && darkRes is IBrush darkBrush
                )
                    return darkBrush;
                return Brushes.DarkGray;
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

        private IBrush GetDefaultBrush()
        {
            if (
                Application.Current?.Resources.TryGetResource("DarkBackground", null, out var res)
                    == true
                && res is IBrush brush
            )
                return brush;
            return Brushes.DarkGray;
        }
    }
}

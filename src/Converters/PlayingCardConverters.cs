using System;
using System.Globalization;
using Avalonia;
using Avalonia.Data.Converters;
using Avalonia.Media;

namespace BalatroSeedOracle.Converters
{
    /// <summary>
    /// Converts a boolean IsSelected value to the appropriate card background brush
    /// </summary>
    public class CardBackgroundConverter : IValueConverter
    {
        public object? Convert(
            object? value,
            Type targetType,
            object? parameter,
            CultureInfo culture
        )
        {
            if (value is bool isSelected)
            {
                return isSelected ? new SolidColorBrush(Color.Parse("#E6F3FF")) : Brushes.White;
            }

            return Brushes.White;
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

    /// <summary>
    /// Converts a boolean IsSelected value to the appropriate card border brush
    /// </summary>
    public class CardBorderBrushConverter : IValueConverter
    {
        public object? Convert(
            object? value,
            Type targetType,
            object? parameter,
            CultureInfo culture
        )
        {
            if (value is bool isSelected)
            {
                if (isSelected)
                {
                    if (
                        Application.Current?.Resources.TryGetResource(
                            "AccentBlue",
                            null,
                            out var resource
                        ) == true
                        && resource is IBrush brush
                    )
                        return brush;
                    return Brushes.Blue;
                }
                else
                {
                    if (
                        Application.Current?.Resources.TryGetResource(
                            "DarkerGrey",
                            null,
                            out var resource
                        ) == true
                        && resource is IBrush brush
                    )
                        return brush;
                    return Brushes.Gray;
                }
            }

            if (
                Application.Current?.Resources.TryGetResource(
                    "DarkerGrey",
                    null,
                    out var defaultResource
                ) == true
                && defaultResource is IBrush defaultBrush
            )
                return defaultBrush;
            return Brushes.Gray;
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

    /// <summary>
    /// Converts a boolean IsSelected value to the appropriate card border thickness
    /// </summary>
    public class CardBorderThicknessConverter : IValueConverter
    {
        public object? Convert(
            object? value,
            Type targetType,
            object? parameter,
            CultureInfo culture
        )
        {
            if (value is bool isSelected)
            {
                return isSelected ? new Thickness(3) : new Thickness(2);
            }

            return new Thickness(2);
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

    /// <summary>
    /// Converts a hex color string to a SolidColorBrush
    /// </summary>
    public class HexToBrushConverter : IValueConverter
    {
        public object? Convert(
            object? value,
            Type targetType,
            object? parameter,
            CultureInfo culture
        )
        {
            if (value is string hex && !string.IsNullOrEmpty(hex))
            {
                try
                {
                    return new SolidColorBrush(Color.Parse(hex));
                }
                catch
                {
                    return Brushes.Black;
                }
            }

            return Brushes.Black;
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

    /// <summary>
    /// Converts a boolean value to a CSS class name
    /// Used for applying selected class to buttons
    /// </summary>
    public class BoolToClassConverter : IValueConverter
    {
        public object? Convert(
            object? value,
            Type targetType,
            object? parameter,
            CultureInfo culture
        )
        {
            if (value is bool boolValue && parameter is string className)
            {
                return boolValue ? className : string.Empty;
            }

            return string.Empty;
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

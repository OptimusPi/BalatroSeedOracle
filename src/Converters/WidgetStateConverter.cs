using System;
using System.Globalization;
using Avalonia;
using Avalonia.Data.Converters;
using BalatroSeedOracle.Models;

namespace BalatroSeedOracle.Converters
{
    /// <summary>
    /// Converter for widget state-specific UI rendering
    /// </summary>
    public class WidgetStateConverter : IValueConverter
    {
        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value is not WidgetState state || parameter is not string param)
                return AvaloniaProperty.UnsetValue;

            return param.ToLowerInvariant() switch
            {
                "isminimized" => state == WidgetState.Minimized,
                "isopen" => state == WidgetState.Open,
                "istransitioning" => state == WidgetState.Transitioning,
                "isnotminimized" => state != WidgetState.Minimized,
                "opacity" => state == WidgetState.Transitioning ? 0.5 : 1.0,
                _ => AvaloniaProperty.UnsetValue
            };
        }

        public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            return AvaloniaProperty.UnsetValue;
        }
    }
    
    /// <summary>
    /// Converts numeric values to boolean for visibility logic
    /// </summary>
    public class WidgetVisibilityConverter : IValueConverter
    {
        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value is int intValue)
                return intValue > 0;
            if (value is double doubleValue)
                return doubleValue > 0.0;
            
            return false;
        }

        public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            return AvaloniaProperty.UnsetValue;
        }
    }
    
    /// <summary>
    /// Static instances for XAML binding
    /// </summary>
    public static class WidgetStateConverters
    {
        public static readonly WidgetStateConverter Instance = new();
        public static readonly WidgetVisibilityConverter GreaterThanZero = new();
    }
}
using System;
using System.Globalization;
using Avalonia.Data.Converters;
using Avalonia.Media;

namespace BalatroSeedOracle.Converters
{
    /// <summary>
    /// Converts operator type string ("OR" or "AND") to appropriate color brush
    /// </summary>
    public class OperatorColorConverter : IValueConverter
    {
        public static readonly OperatorColorConverter Instance = new();

        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value is string operatorType)
            {
                if (operatorType == "OR")
                {
                    // Green: #2ECC71
                    return new SolidColorBrush(Color.FromRgb(46, 204, 113));
                }
                else if (operatorType == "AND")
                {
                    // Blue: #3498DB
                    return new SolidColorBrush(Color.FromRgb(52, 152, 219));
                }
            }
            return new SolidColorBrush(Color.FromRgb(128, 128, 128));
        }

        public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    /// <summary>
    /// Returns true if count is less than 5
    /// </summary>
    public class LessThanFiveConverter : IValueConverter
    {
        public static readonly LessThanFiveConverter Instance = new();

        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value is int count)
            {
                return count < 5;
            }
            return false;
        }

        public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    /// <summary>
    /// Returns true if count is greater than or equal to 5
    /// </summary>
    public class GreaterThanFourConverter : IValueConverter
    {
        public static readonly GreaterThanFourConverter Instance = new();

        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value is int count)
            {
                return count >= 5;
            }
            return false;
        }

        public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    /// <summary>
    /// Returns true if count equals zero
    /// </summary>
    public class EqualsZeroConverter : IValueConverter
    {
        public static readonly EqualsZeroConverter Instance = new();

        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value is int count)
            {
                return count == 0;
            }
            return true;
        }

        public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}

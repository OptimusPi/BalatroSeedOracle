using System;
using System.Globalization;
using Avalonia;
using Avalonia.Data.Converters;

namespace BalatroSeedOracle.Converters
{
    public class ShadowDirectionToMarginConverter : IValueConverter
    {
        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value is string direction && parameter is string elementType)
            {
                // For buttons (arrows)
                if (elementType == "button")
                {
                    return direction switch
                    {
                        "south-east" => new Thickness(2, 4, 0, 0), // Shadow to bottom-right
                        "south-west" => new Thickness(-2, 4, 0, 0), // Shadow to bottom-left
                        _ => new Thickness(0, 4, 0, 0), // Default south
                    };
                }
                // For value badge
                else if (elementType == "badge")
                {
                    return direction switch
                    {
                        "south-east" => new Thickness(3, 4, 0, 0), // Shadow to bottom-right
                        "south-west" => new Thickness(-3, 4, 0, 0), // Shadow to bottom-left
                        _ => new Thickness(0, 4, 0, 0), // Default south
                    };
                }
            }

            return new Thickness(0, 4, 0, 0); // Default
        }

        public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            throw new NotSupportedException("ConvertBack is not supported");
        }
    }

    public class ShadowDirectionToPressedMarginConverter : IValueConverter
    {
        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value is string direction && parameter is string elementType)
            {
                // For pressed state, reduce offset
                if (elementType == "button")
                {
                    return direction switch
                    {
                        "south-east" => new Thickness(1, 2, 0, 0), // Shadow to bottom-right
                        "south-west" => new Thickness(-1, 2, 0, 0), // Shadow to bottom-left
                        _ => new Thickness(0, 2, 0, 0), // Default south
                    };
                }
                else if (elementType == "badge")
                {
                    return direction switch
                    {
                        "south-east" => new Thickness(2, 2, 0, 0), // Shadow to bottom-right
                        "south-west" => new Thickness(-2, 2, 0, 0), // Shadow to bottom-left
                        _ => new Thickness(0, 2, 0, 0), // Default south
                    };
                }
            }

            return new Thickness(0, 2, 0, 0); // Default
        }

        public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            throw new NotSupportedException("ConvertBack is not supported");
        }
    }
}

using System;
using System.Globalization;
using Avalonia.Data.Converters;

namespace BalatroSeedOracle.Converters
{
    /// <summary>
    /// Converts boolean to opacity value for drag-and-drop feedback.
    /// When IsBeingDragged is true, makes the card semi-transparent (Balatro-style).
    /// </summary>
    public class BoolToOpacityConverter : IValueConverter
    {
        public static readonly BoolToOpacityConverter Dragging = new();

        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value is bool isDragging)
            {
                // When dragging: 15% opacity (very faint, like card was "picked up")
                // When not dragging: 100% opacity (normal)
                return isDragging ? 0.15 : 1.0;
            }
            return 1.0; // Default to fully visible
        }

        public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}

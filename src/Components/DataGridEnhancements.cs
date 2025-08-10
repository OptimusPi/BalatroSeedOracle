using System;
using System.Globalization;
using Avalonia.Data.Converters;
using Avalonia.Media;

namespace Oracle.Components
{
    public class ScoreToColorConverter : IValueConverter
    {
        public object? Convert(
            object? value,
            Type targetType,
            object? parameter,
            CultureInfo culture
        )
        {
            if (value is double score)
            {
                if (score >= 100)
                    return new SolidColorBrush(Color.Parse("#4BC292")); // Green
                if (score >= 50)
                    return new SolidColorBrush(Color.Parse("#FEB95F")); // Orange
                return new SolidColorBrush(Color.Parse("#FE5F55")); // Red
            }
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

    public class ScoreToBackgroundConverter : IValueConverter
    {
        public object? Convert(
            object? value,
            Type targetType,
            object? parameter,
            CultureInfo culture
        )
        {
            if (value is double score)
            {
                Color color;
                if (score >= 100)
                    color = Color.Parse("#4BC292"); // Green
                else if (score >= 50)
                    color = Color.Parse("#FEB95F"); // Orange
                else
                    color = Color.Parse("#FE5F55"); // Red

                return new SolidColorBrush(color, 0.2); // 20% opacity
            }
            return new SolidColorBrush(Colors.Transparent);
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

    public class ScoreToIconConverter : IValueConverter
    {
        public object? Convert(
            object? value,
            Type targetType,
            object? parameter,
            CultureInfo culture
        )
        {
            if (value is double score)
            {
                if (score >= 100)
                    return "⭐";
                if (score >= 50)
                    return "✨";
                return "·";
            }
            return "";
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

    public static class DataGridHelper
    {
        public static string FormatScore(double score)
        {
            return score.ToString("N0", CultureInfo.InvariantCulture);
        }

        public static string GetAnteColor(int ante)
        {
            return ante switch
            {
                1 => "#5DADE2", // Light Blue
                2 => "#52BE80", // Green
                3 => "#F4D03F", // Yellow
                4 => "#E59866", // Orange
                5 => "#EC7063", // Red
                6 => "#AF7AC5", // Purple
                7 => "#5D6D7E", // Dark Gray
                8 => "#FFFFFF", // White
                _ => "#ABB2B9", // Gray
            };
        }
    }
}

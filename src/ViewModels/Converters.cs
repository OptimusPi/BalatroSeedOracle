using System;
using System.Globalization;
using Avalonia.Controls;
using Avalonia.Controls.Presenters;
using Avalonia.Data.Converters;

namespace BalatroSeedOracle.ViewModels
{
    /// <summary>
    /// Simple converters for XAML bindings
    /// </summary>
    public static class Converters
    {
        public static readonly IValueConverter EqualsConverter = new EqualsValueConverter();
        public static readonly IValueConverter NotEqualsConverter = new NotEqualsValueConverter();
    }

    public class IndexConverter : IValueConverter
    {
        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value is ContentPresenter contentPresenter)
            {
                var itemsControl = ItemsControl.ItemsControlFromItemContainer(contentPresenter);
                if (itemsControl != null)
                {
                    return itemsControl.IndexFromContainer(contentPresenter).ToString();
                }
            }
            return string.Empty;
        }

        public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public class EqualsValueConverter : IValueConverter
    {
        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value == null || parameter == null)
                return false;

            return value.ToString() == parameter.ToString();
        }

        public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public class NotEqualsValueConverter : IValueConverter
    {
        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value == null || parameter == null)
                return true;

            return value.ToString() != parameter.ToString();
        }

        public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}

using System;
using System.Globalization;
using Avalonia.Data.Converters;
using Avalonia.Media;
using BalatroSeedOracle.Models;
using BalatroSeedOracle.Services;

namespace BalatroSeedOracle.Converters
{
    public class ItemTypeToImageConverter : IValueConverter
    {
        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value is not FilterItem item)
                return null;

            var spriteService = SpriteService.Instance;
            var typeHint = parameter as string ?? item.ItemType;

            return typeHint?.ToLower() switch
            {
                "joker" => spriteService.GetJokerImage(item.Name),
                "tag" or "smallblindtag" => spriteService.GetTagImage(item.Name),
                "voucher" => spriteService.GetVoucherImage(item.Name),
                "tarot" => spriteService.GetTarotImage(item.Name),
                "planet" => spriteService.GetPlanetCardImage(item.Name),
                "spectral" => spriteService.GetSpectralImage(item.Name),
                _ => null
            };
        }

        public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            return null; // ConvertBack not needed for this converter
        }
    }
}
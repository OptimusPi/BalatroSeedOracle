using System;
using System.Globalization;
using Avalonia.Data.Converters;
using Avalonia.Media.Imaging;
using BalatroSeedOracle.Helpers;
using BalatroSeedOracle.Models;
using BalatroSeedOracle.Services;

namespace BalatroSeedOracle.Converters
{
    /// <summary>
    /// Converts a ShopItemModel to its sprite image
    /// </summary>
    public class ShopItemSpriteConverter : IValueConverter
    {
        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value is not ShopItemModel item)
                return null;

            try
            {
                var spriteService = ServiceHelper.GetRequiredService<SpriteService>();
                return item.TypeCategory switch
                {
                    Motely.MotelyItemTypeCategory.Joker => spriteService.GetJokerImage(item.ItemName),
                    Motely.MotelyItemTypeCategory.TarotCard => spriteService.GetTarotImage(item.ItemName),
                    Motely.MotelyItemTypeCategory.PlanetCard => spriteService.GetTarotImage(item.ItemName),
                    Motely.MotelyItemTypeCategory.SpectralCard => spriteService.GetTarotImage(item.ItemName),
                    _ => null
                };
            }
            catch
            {
                return null;
            }
        }

        public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    /// <summary>
    /// Converts a BoosterPackModel to its sprite image
    /// </summary>
    public class BoosterPackSpriteConverter : IValueConverter
    {
        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value is not BoosterPackModel pack)
                return null;

            try
            {
                var spriteService = ServiceHelper.GetRequiredService<SpriteService>();
                return spriteService.GetBoosterImage(pack.PackSpriteKey);
            }
            catch
            {
                return null;
            }
        }

        public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    /// <summary>
    /// Converts a TagModel to its sprite image
    /// </summary>
    public class TagSpriteConverter : IValueConverter
    {
        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value is not TagModel tag)
                return null;

            try
            {
                var spriteService = ServiceHelper.GetRequiredService<SpriteService>();
                return spriteService.GetTagImage(tag.TagSpriteKey);
            }
            catch
            {
                return null;
            }
        }

        public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    /// <summary>
    /// Converts an item type string to its sprite image
    /// </summary>
    public class ItemTypeToSpriteConverter : IValueConverter
    {
        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value is not string itemType || string.IsNullOrEmpty(itemType))
                return null;

            try
            {
                var spriteService = ServiceHelper.GetRequiredService<SpriteService>();
                return spriteService.GetItemImage(itemType);
            }
            catch
            {
                return null;
            }
        }

        public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}

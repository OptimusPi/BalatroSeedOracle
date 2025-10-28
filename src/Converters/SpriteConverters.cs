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
        public object? Convert(
            object? value,
            Type targetType,
            object? parameter,
            CultureInfo culture
        )
        {
            if (value is not ShopItemModel item)
                return null;

            try
            {
                var spriteService = ServiceHelper.GetRequiredService<SpriteService>();
                return item.TypeCategory switch
                {
                    Motely.MotelyItemTypeCategory.Joker => spriteService.GetJokerImage(
                        item.ItemName
                    ),
                    Motely.MotelyItemTypeCategory.TarotCard => spriteService.GetTarotImage(
                        item.ItemName
                    ),
                    Motely.MotelyItemTypeCategory.PlanetCard => spriteService.GetTarotImage(
                        item.ItemName
                    ),
                    Motely.MotelyItemTypeCategory.SpectralCard => spriteService.GetTarotImage(
                        item.ItemName
                    ),
                    _ => null,
                };
            }
            catch
            {
                return null;
            }
        }

        public object ConvertBack(
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
    /// Converts a BoosterPackModel to its sprite image
    /// </summary>
    public class BoosterPackSpriteConverter : IValueConverter
    {
        public object? Convert(
            object? value,
            Type targetType,
            object? parameter,
            CultureInfo culture
        )
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

        public object ConvertBack(
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
    /// Converts a TagModel to its sprite image
    /// </summary>
    public class TagSpriteConverter : IValueConverter
    {
        public object? Convert(
            object? value,
            Type targetType,
            object? parameter,
            CultureInfo culture
        )
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

        public object ConvertBack(
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
    /// Converts an item type string to its sprite image
    /// </summary>
    public class ItemTypeToSpriteConverter : IValueConverter
    {
        public object? Convert(
            object? value,
            Type targetType,
            object? parameter,
            CultureInfo culture
        )
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

        public object ConvertBack(
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
    /// Converts an item name string to its sprite image, automatically determining type.
    /// Supports Jokers, Tarots, Planets, Spectrals, Vouchers, Tags, and Bosses.
    /// </summary>
    public class ItemNameToSpriteConverter : IValueConverter
    {
        public object? Convert(
            object? value,
            Type targetType,
            object? parameter,
            CultureInfo culture
        )
        {
            string? itemName = value switch
            {
                string str => str,
                ItemConfig config => config.ItemName,
                _ => null,
            };

            if (string.IsNullOrEmpty(itemName))
                return null;

            try
            {
                var spriteService = ServiceHelper.GetRequiredService<SpriteService>();

                // The parameter can optionally specify the type category
                string? typeHint = parameter as string;

                return spriteService.GetItemImage(itemName, typeHint);
            }
            catch (Exception ex)
            {
                DebugLogger.LogError(
                    "ItemNameToSpriteConverter",
                    $"Failed to get sprite for '{itemName}': {ex.Message}"
                );
                return null;
            }
        }

        public object ConvertBack(
            object? value,
            Type targetType,
            object? parameter,
            CultureInfo culture
        )
        {
            throw new NotImplementedException();
        }
    }

    public class ItemNameToSoulFaceConverter : IValueConverter
    {
        public object? Convert(
            object? value,
            Type targetType,
            object? parameter,
            CultureInfo culture
        )
        {
            string? itemName = value switch
            {
                string str => str,
                ItemConfig config => config.ItemName,
                _ => null,
            };

            if (string.IsNullOrEmpty(itemName))
                return null;

            try
            {
                var spriteService = ServiceHelper.GetRequiredService<SpriteService>();
                return spriteService.GetJokerSoulImage(itemName);
            }
            catch
            {
                return null;
            }
        }

        public object ConvertBack(
            object? value,
            Type targetType,
            object? parameter,
            CultureInfo culture
        )
        {
            throw new NotImplementedException();
        }
    }

    public class StandardCardToSpriteConverter : IValueConverter
    {
        public object? Convert(
            object? value,
            Type targetType,
            object? parameter,
            CultureInfo culture
        )
        {
            if (value is not string cardString || string.IsNullOrEmpty(cardString))
                return null;

            try
            {
                var spriteService = ServiceHelper.GetRequiredService<SpriteService>();

                var parts = cardString.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                if (parts.Length < 3)
                    return null;

                string? seal = null;
                string? edition = null;
                string rank = "";
                string suit = "";

                int rankIndex = -1;
                for (int i = 0; i < parts.Length; i++)
                {
                    if (parts[i].ToLowerInvariant() == "of")
                    {
                        rank = parts[i - 1];
                        suit = parts[i + 1];
                        rankIndex = i - 1;
                        break;
                    }
                }

                if (string.IsNullOrEmpty(rank) || string.IsNullOrEmpty(suit))
                    return null;

                for (int i = 0; i < rankIndex; i++)
                {
                    var part = parts[i].ToLowerInvariant();
                    if (part == "red" || part == "blue" || part == "gold" || part == "purple")
                    {
                        seal = parts[i];
                    }
                    else if (
                        part == "foil"
                        || part == "holographic"
                        || part == "polychrome"
                        || part == "negative"
                    )
                    {
                        edition = parts[i];
                    }
                }

                return spriteService.GetPlayingCardImage(suit, rank, null, seal, edition);
            }
            catch (Exception ex)
            {
                DebugLogger.LogError(
                    "StandardCardToSpriteConverter",
                    $"Failed to parse card '{value}': {ex.Message}"
                );
                return null;
            }
        }

        public object ConvertBack(
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

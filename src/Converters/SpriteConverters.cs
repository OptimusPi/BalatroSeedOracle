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
                        item.SpriteKey
                    ),
                    Motely.MotelyItemTypeCategory.TarotCard => spriteService.GetTarotImage(
                        item.SpriteKey
                    ),
                    Motely.MotelyItemTypeCategory.PlanetCard => spriteService.GetPlanetCardImage(
                        item.SpriteKey
                    ),
                    Motely.MotelyItemTypeCategory.SpectralCard => spriteService.GetSpectralImage(
                        item.SpriteKey
                    ),
                    _ => null,
                };
            }
            catch (Exception ex)
            {
                DebugLogger.LogError(
                    "ShopItemSpriteConverter",
                    $"Failed to get sprite for '{item.SpriteKey}' (ItemName: {item.ItemName}, Type: {item.TypeCategory}): {ex.Message}"
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
            catch (Exception ex)
            {
                DebugLogger.LogError(
                    "BoosterPackSpriteConverter",
                    $"Failed to get booster sprite for '{pack.PackSpriteKey}': {ex.Message}"
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
            catch (Exception ex)
            {
                DebugLogger.LogError(
                    "TagSpriteConverter",
                    $"Failed to get tag sprite for '{tag.TagSpriteKey}': {ex.Message}"
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
            catch (Exception ex)
            {
                DebugLogger.LogError(
                    "ItemTypeToSpriteConverter",
                    $"Failed to get item sprite for type '{itemType}': {ex.Message}"
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
            string? itemName = null;
            bool isSoulJoker = false;

            switch (value)
            {
                case string str:
                    itemName = str;
                    break;
                case ItemConfig config:
                    itemName = config.ItemName;
                    // Check both IsSoulJoker property AND ItemType == "SoulJoker"
                    isSoulJoker = config.IsSoulJoker || config.ItemType == "SoulJoker";
                    break;
                default:
                    return null;
            }

            if (string.IsNullOrEmpty(itemName))
                return null;

            // Only get soul face if it's marked as a soul joker
            if (!isSoulJoker)
                return null;

            try
            {
                var spriteService = ServiceHelper.GetRequiredService<SpriteService>();
                return spriteService.GetJokerSoulImage(itemName);
            }
            catch (Exception ex)
            {
                DebugLogger.LogError(
                    "ItemNameToSoulFaceConverter",
                    $"Failed to get soul sprite for '{itemName}': {ex.Message}"
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

    /// <summary>
    /// Converts a spectral card name to its Soul Gem overlay (for "The Soul" spectral card)
    /// </summary>
    public class SoulGemOverlayConverter : IValueConverter
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
                _ => null
            };

            if (string.IsNullOrEmpty(itemName))
                return null;

            // Only return soul gem overlay for "The Soul" spectral card
            if (!itemName.Equals("soul", StringComparison.OrdinalIgnoreCase))
                return null;

            try
            {
                var spriteService = ServiceHelper.GetRequiredService<SpriteService>();
                return spriteService.GetSoulGemImage();
            }
            catch (Exception ex)
            {
                DebugLogger.LogError("SoulGemOverlayConverter", $"Error: {ex.Message}");
                return null;
            }
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
    /// Converts a joker name to its Mystery Face overlay (for wildcard jokers)
    /// </summary>
    public class MysteryJokerFaceOverlayConverter : IValueConverter
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
                _ => null
            };

            if (string.IsNullOrEmpty(itemName))
                return null;

            // Only return mystery face overlay for wildcard jokers
            if (!itemName.StartsWith("Wildcard_", StringComparison.OrdinalIgnoreCase))
                return null;

            try
            {
                var spriteService = ServiceHelper.GetRequiredService<SpriteService>();
                return spriteService.GetMysteryJokerFaceImage();
            }
            catch (Exception ex)
            {
                DebugLogger.LogError("MysteryJokerFaceOverlayConverter", $"Error: {ex.Message}");
                return null;
            }
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

                string rank = "";
                string suit = "";

                // Parse the card string to extract rank and suit
                // Format: "[modifiers...] <rank> of <suit>"
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

                // StandardCards are rendered with proper multi-layer compositing:
                // - Base layer: BlankCard or Enhancement sprite (Glass, Gold, Steel, etc.)
                // - Overlay layer: Rank + Suit pattern (transparent PNG)
                // - Optional: Mult/Bonus glyph for Type B2 enhancements
                // Note: StandardCards do NOT use negative edition (that's jokers only)
                return spriteService.GetPlayingCardImage(suit, rank, enhancement: null);
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

    /// <summary>
    /// Converts a MotelyBossBlind enum to its sprite image
    /// </summary>
    public class BossSpriteConverter : IValueConverter
    {
        public object? Convert(
            object? value,
            Type targetType,
            object? parameter,
            CultureInfo culture
        )
        {
            if (value == null)
                return null;

            try
            {
                var spriteService = ServiceHelper.GetRequiredService<SpriteService>();
                var bossName = value.ToString();
                if (string.IsNullOrEmpty(bossName))
                    return null;

                return spriteService.GetBossImage(bossName);
            }
            catch (Exception ex)
            {
                DebugLogger.LogError(
                    "BossSpriteConverter",
                    $"Failed to get boss sprite for '{value}': {ex.Message}"
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

    /// <summary>
    /// Converts a MotelyVoucher enum to its sprite image
    /// </summary>
    public class VoucherSpriteConverter : IValueConverter
    {
        public object? Convert(
            object? value,
            Type targetType,
            object? parameter,
            CultureInfo culture
        )
        {
            if (value == null)
                return null;

            try
            {
                var spriteService = ServiceHelper.GetRequiredService<SpriteService>();
                var voucherName = value.ToString();
                if (string.IsNullOrEmpty(voucherName) || voucherName == "None")
                    return null;

                return spriteService.GetVoucherImage(voucherName);
            }
            catch (Exception ex)
            {
                DebugLogger.LogError(
                    "VoucherSpriteConverter",
                    $"Failed to get voucher sprite for '{value}': {ex.Message}"
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

    /// <summary>
    /// Converts an edition string to its sprite image
    /// </summary>
    public class EditionSpriteConverter : IValueConverter
    {
        public static readonly EditionSpriteConverter Instance = new();

        public object? Convert(
            object? value,
            Type targetType,
            object? parameter,
            CultureInfo culture
        )
        {
            var edition = parameter as string ?? value as string;
            if (string.IsNullOrEmpty(edition))
                return null;

            // Special case for "None" - return regular joker sprite
            if (edition.Equals("None", StringComparison.OrdinalIgnoreCase))
            {
                var spriteService = ServiceHelper.GetRequiredService<SpriteService>();
                return spriteService.GetItemImage("Joker", "Joker");
            }

            try
            {
                var spriteService = ServiceHelper.GetRequiredService<SpriteService>();
                return spriteService.GetEditionImage(edition);
            }
            catch (Exception ex)
            {
                DebugLogger.LogError("EditionSpriteConverter", $"Failed to get edition sprite for '{edition}': {ex.Message}");
                return null;
            }
        }

        public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    /// <summary>
    /// Converts a sticker string to its sprite image (Joker with sticker overlay)
    /// </summary>
    public class StickerSpriteConverter : IValueConverter
    {
        public static readonly StickerSpriteConverter Instance = new();

        public object? Convert(
            object? value,
            Type targetType,
            object? parameter,
            CultureInfo culture
        )
        {
            var sticker = parameter as string ?? value as string;
            if (string.IsNullOrEmpty(sticker))
                return null;

            try
            {
                var spriteService = ServiceHelper.GetRequiredService<SpriteService>();
                // Return composite image of Joker with sticker overlay
                return spriteService.GetJokerWithStickerImage(sticker);
            }
            catch (Exception ex)
            {
                DebugLogger.LogError("StickerSpriteConverter", $"Failed to get sticker sprite for '{sticker}': {ex.Message}");
                return null;
            }
        }

        public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    /// <summary>
    /// Converts a seal string to its sprite image
    /// </summary>
    public class SealSpriteConverter : IValueConverter
    {
        public static readonly SealSpriteConverter Instance = new();

        public object? Convert(
            object? value,
            Type targetType,
            object? parameter,
            CultureInfo culture
        )
        {
            var seal = parameter as string ?? value as string;
            if (string.IsNullOrEmpty(seal) || seal.Equals("None", StringComparison.OrdinalIgnoreCase))
                return null;

            try
            {
                var spriteService = ServiceHelper.GetRequiredService<SpriteService>();
                return spriteService.GetSealImage(seal);
            }
            catch (Exception ex)
            {
                DebugLogger.LogError("SealSpriteConverter", $"Failed to get seal sprite for '{seal}': {ex.Message}");
                return null;
            }
        }

        public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}

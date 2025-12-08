using System;

namespace BalatroSeedOracle.Helpers
{
    /// <summary>
    /// Utility class for mapping between UI category names (plural) and JSON type values (singular).
    /// Extracted from FiltersModal to promote code reuse and maintainability.
    /// </summary>
    public static class CategoryMapper
    {
        /// <summary>
        /// Maps UI category names (plural) to JSON Type values (singular).
        /// Examples: "Jokers" => "Joker", "Tarots" => "Tarot"
        /// </summary>
        /// <param name="category">UI category name (plural form)</param>
        /// <returns>JSON type value (singular form)</returns>
        public static string MapCategoryToType(string category)
        {
            return category switch
            {
                "Jokers" => "Joker",
                "SoulJokers" => "SoulJoker",
                "Tarots" => "Tarot",
                "Spectrals" => "Spectral",
                "Vouchers" => "Voucher",
                "Tags" => "Tag",
                "Bosses" => "Boss",
                "PlayingCards" => "PlayingCard",
                _ => category, // Fallback to original if not mapped
            };
        }

        /// <summary>
        /// Maps JSON Type values (singular) to UI category names (plural).
        /// Examples: "Joker" => "Jokers", "Tarot" => "Tarots"
        /// </summary>
        /// <param name="type">JSON type value (singular form)</param>
        /// <returns>UI category name (plural form)</returns>
        /// <exception cref="ArgumentException">Thrown when an unknown type is provided</exception>
        public static string MapTypeToCategory(string type)
        {
            return type switch
            {
                "SoulJoker" => "SoulJokers",
                "Joker" => "Jokers",
                "Tarot" => "Tarots",
                "Spectral" => "Spectrals",
                "Voucher" => "Vouchers",
                "Tag" => "Tags",
                "Boss" => "Bosses",
                "PlayingCard" => "PlayingCards",
                _ => throw new ArgumentException($"Unknown type: {type}"),
            };
        }

        /// <summary>
        /// Gets the UI category name from a JSON type value, using case-insensitive matching.
        /// Handles various type aliases (e.g., "TarotCard", "SpectralCard", "SmallBlindTag").
        /// </summary>
        /// <param name="type">JSON type value (singular form, case-insensitive)</param>
        /// <returns>UI category name (plural form)</returns>
        public static string GetCategoryFromType(string type)
        {
            return type.ToLower() switch
            {
                "joker" or "souljoker" => "Jokers",
                "tarot" or "tarotcard" => "Tarots",
                "spectral" or "spectralcard" => "Spectrals",
                "planet" or "planetcard" => "Planets",
                "tag" or "smallblindtag" or "bigblindtag" => "Tags",
                "voucher" => "Vouchers",
                "playingcard" => "PlayingCards",
                _ => "Unknown",
            };
        }
    }
}

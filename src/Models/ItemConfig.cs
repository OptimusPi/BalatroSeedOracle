using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace BalatroSeedOracle.Models
{
    public class ItemConfigEventArgs : EventArgs
    {
        public ItemConfig Config { get; set; } = new();
        public ItemConfig Configuration => Config; // Alias for compatibility
    }

    public class ItemConfig
    {
        public string ItemKey { get; set; } = "";
        public string ItemType { get; set; } = ""; // Joker, Tag, Voucher, etc
        public string ItemName { get; set; } = ""; // Display name

        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public List<int>? Antes { get; set; }

        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string? Edition { get; set; }

        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string? Seal { get; set; } // Red, Blue, Gold, Purple

        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string? Enhancement { get; set; } // Bonus, Mult, Wild, Glass, Steel, Stone, Lucky

        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string? Rank { get; set; } // For playing cards

        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string? Suit { get; set; } // For playing cards

        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
        public int Score { get; set; } = 1; // Score for should clauses

        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public object? Sources { get; set; }

        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string? Label { get; set; }

        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string? TagType { get; set; } // "smallblindtag" or "bigblindtag" for tag items

        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public List<string>? Stickers { get; set; } // "eternal", "perishable", "rental"

        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public int? Min { get; set; } // Minimum count required (for Must items)

        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public List<int>? ShopSlots { get; set; } // Shop slot positions

        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public List<int>? PackSlots { get; set; } // Pack slot positions

        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
        public bool SkipBlindTags { get; set; } // From skip blind tags

        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
        public bool IsMegaArcana { get; set; } // Mega arcana pack only

        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
        public bool IsSoulJoker { get; set; } // For SoulJoker type

        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
        public bool IsMultiValue { get; set; } // For multi-value clauses

        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public List<string>? Values { get; set; } // For multi-value clauses

        // Operator-specific fields
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string? OperatorType { get; set; } // "Or" or "And"

        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string? Mode { get; set; } // "Max" for Or operators

        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public List<ItemConfig>? Children { get; set; } // Child items for operators
    }
}

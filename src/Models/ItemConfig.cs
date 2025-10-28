using System;
using System.Collections.Generic;

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
        public List<int>? Antes { get; set; }
        public string Edition { get; set; } = "none";
        public string Seal { get; set; } = "None"; // Red, Blue, Gold, Purple
        public string Enhancement { get; set; } = "None"; // Bonus, Mult, Wild, Glass, Steel, Stone, Lucky
        public string? Rank { get; set; } // For playing cards
        public string? Suit { get; set; } // For playing cards
        public int Score { get; set; } = 1; // Score for should clauses
        public object? Sources { get; set; }
        public string? Label { get; set; }
        public string? TagType { get; set; } // "smallblindtag" or "bigblindtag" for tag items
        public List<string>? Stickers { get; set; } // "eternal", "perishable", "rental"
        public int? Min { get; set; } // Minimum count required (for Must items)
        public List<int>? ShopSlots { get; set; } // Shop slot positions
        public List<int>? PackSlots { get; set; } // Pack slot positions
        public bool SkipBlindTags { get; set; } // From skip blind tags
        public bool IsMegaArcana { get; set; } // Mega arcana pack only
        public bool IsSoulJoker { get; set; } // For SoulJoker type
        public bool IsMultiValue { get; set; } // For multi-value clauses
        public List<string>? Values { get; set; } // For multi-value clauses
    }
}

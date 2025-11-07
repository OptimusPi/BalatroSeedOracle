using System.Collections.Generic;

namespace BalatroSeedOracle.Helpers
{
    /// <summary>
    /// Shared helper for voucher pairing logic.
    /// Eliminates duplication across ConfigureFilterTabViewModel, ConfigureScoreTabViewModel, and VisualBuilderTabViewModel.
    /// </summary>
    public static class VoucherHelper
    {
        /// <summary>
        /// Get vouchers organized into pairs (base + upgrade) matching sprite sheet layout.
        /// Returns 16 pairs where each pair has base voucher followed by upgrade voucher.
        /// </summary>
        public static List<(string baseName, string upgradeName)> GetVoucherPairs()
        {
            return new List<(string, string)>
            {
                // Row 0 -> Row 1 pairs (8 pairs)
                ("overstock", "overstockplus"),
                ("tarotmerchant", "tarottycoon"),
                ("planetmerchant", "planettycoon"),
                ("clearancesale", "liquidation"),
                ("hone", "glowup"),
                ("grabber", "nachotong"),
                ("wasteful", "recyclomancy"),
                ("blank", "antimatter"),

                // Row 2 -> Row 3 pairs (8 pairs)
                ("rerollsurplus", "rerollglut"),
                ("seedmoney", "moneytree"),
                ("crystalball", "omenglobe"),
                ("telescope", "observatory"),
                ("magictrick", "illusion"),
                ("hieroglyph", "petroglyph"),
                ("directorscut", "retcon"),
                ("paintbrush", "palette"),
            };
        }
    }
}

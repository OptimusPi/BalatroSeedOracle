using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace BalatroSeedOracle.Models
{
    /// <summary>
    /// Compares Balatro item names by letters and digits only, case- and accent-insensitive:
    /// "EightBall" / "eightball", "Oops! All 6s" / "OopsAll6s", "Séance" / "Seance",
    /// "The Wheel of Fortune" / "TheWheelOfFortune" are equal.
    /// </summary>
    public sealed class ItemNameComparer : IEqualityComparer<string>
    {
        public static readonly ItemNameComparer Instance = new();

        public static string Normalize(string name)
        {
            var decomposed = name.Normalize(NormalizationForm.FormD);
            var sb = new StringBuilder(decomposed.Length);
            foreach (var c in decomposed)
            {
                if (CharUnicodeInfo.GetUnicodeCategory(c) == UnicodeCategory.NonSpacingMark)
                    continue;
                if (char.IsLetterOrDigit(c))
                    sb.Append(char.ToLowerInvariant(c));
            }
            return sb.ToString();
        }

        public bool Equals(string? x, string? y) =>
            ReferenceEquals(x, y)
            || (x is not null && y is not null && string.Equals(Normalize(x), Normalize(y), StringComparison.Ordinal));

        public int GetHashCode(string obj) => StringComparer.Ordinal.GetHashCode(Normalize(obj));
    }
}

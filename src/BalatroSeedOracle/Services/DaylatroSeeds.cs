using System;

namespace BalatroSeedOracle.Services
{
    /// <summary>
    /// C# port of Daylatro's daily seed generation algorithm
    /// Generates the same Balatro seed that the live Haskell Daylatro (random >=1.2 SplitMix StdGen) shows for any given day
    /// </summary>
    public static class DaylatroSeeds
    {
        /// <summary>
        /// Gets the daily Balatro seed for today (UTC)
        /// </summary>
        public static string GetDailyBalatroSeed()
        {
            return GetDailyBalatroSeed(DateTime.UtcNow);
        }

        /// <summary>
        /// Gets the daily Balatro seed for a specific date (UTC)
        /// </summary>
        public static string GetDailyBalatroSeed(DateTime date)
        {
            // Convert to Modified Julian Day (Haskell Time.toModifiedJulianDay)
            ulong modifiedJulianDay = (ulong)GetModifiedJulianDay(date.Date);

            // Haskell: mkStdGen (fromIntegral mjd)
            var gen = StdGen.FromSeed(modifiedJulianDay);

            Span<char> chars = stackalloc char[8];
            for (int i = 0; i < 8; i++)
            {
                int n;
                gen = gen.UniformInclusive(0, 35, out n);
                chars[i] = (char)(n + (n < 10 ? 48 : 55));
            }
            return new string(chars);
        }

        /// <summary>
        /// Calculate Modified Julian Day for a given date
        /// </summary>
        private static long GetModifiedJulianDay(DateTime date)
        {
            var mjdEpoch = new DateTime(1858, 11, 17, 0, 0, 0, DateTimeKind.Utc);
            var daysSinceEpoch = (date.Date - mjdEpoch).TotalDays;
            return (long)daysSinceEpoch;
        }

        /// <summary>
        /// Get yesterday's seed
        /// </summary>
        public static string GetYesterdaySeed()
        {
            return GetDailyBalatroSeed(DateTime.UtcNow.AddDays(-1));
        }

        /// <summary>
        /// Get tomorrow's seed
        /// </summary>
        public static string GetTomorrowSeed()
        {
            return GetDailyBalatroSeed(DateTime.UtcNow.AddDays(1));
        }

        /// <summary>
        /// Get seed for a specific date string (yyyy-MM-dd format)
        /// </summary>
        public static string GetSeedForDate(string dateString)
        {
            if (DateTime.TryParse(dateString, out DateTime date))
            {
                return GetDailyBalatroSeed(date);
            }
            throw new ArgumentException($"Invalid date format: {dateString}");
        }

        // SplitMix-based StdGen (random >=1.2). Matches Haskell mkStdGen & uniformListR semantics for small Int ranges.
        private readonly struct StdGen
        {
            public readonly ulong Seed;
            public readonly ulong Gamma;

            private StdGen(ulong seed, ulong gamma)
            {
                Seed = seed;
                Gamma = gamma;
            }

            public static StdGen FromSeed(ulong rawSeed)
            {
                unchecked
                {
                    var seed = Mix64(rawSeed);
                    var gamma = MixGamma(rawSeed + GoldenGamma);
                    return new StdGen(seed, gamma);
                }
            }

            private StdGen Step(out ulong word)
            {
                unchecked
                {
                    ulong newSeed = Seed + Gamma; // wrap-around
                    word = Mix64(newSeed);
                    return new StdGen(newSeed, Gamma);
                }
            }

            public StdGen UniformInclusive(int lo, int hi, out int value)
            {
                if (lo > hi)
                {
                    var g2 = UniformInclusive(hi, lo, out int swapped);
                    value = swapped;
                    return g2;
                }
                uint range = (uint)(hi - lo); // inclusive distance
                if (range == 0)
                {
                    value = lo;
                    return this;
                }
                // inclusive range => size = range + 1
                ulong size = range + 1UL;
                // mask with all bits set up to highest bit of (size-1)
                ulong mask = size - 1;
                // If size not power of two, extend mask to next power-of-two -1
                mask |= mask >> 1;
                mask |= mask >> 2;
                mask |= mask >> 4;
                mask |= mask >> 8;
                mask |= mask >> 16;
                mask |= mask >> 32;

                StdGen g = this;
                while (true)
                {
                    g = g.Step(out ulong word);
                    ulong candidate = word & mask;
                    if (candidate < size)
                    {
                        value = lo + (int)candidate;
                        return g;
                    }
                }
            }

            private const ulong GoldenGamma = 0x9E3779B97F4A7C15UL;

            private static ulong Mix64(ulong z)
            {
                unchecked
                {
                    z = (z ^ (z >> 33)) * 0xff51afd7ed558ccdUL;
                    z = (z ^ (z >> 33)) * 0xc4ceb9fe1a85ec53UL;
                    return z ^ (z >> 33);
                }
            }

            private static ulong Mix64Variant13(ulong z)
            {
                unchecked
                {
                    z = (z ^ (z >> 30)) * 0xbf58476d1ce4e5b9UL;
                    z = (z ^ (z >> 27)) * 0x94d049bb133111ebUL;
                    return z ^ (z >> 31);
                }
            }

            private static int PopCount(ulong v)
            {
#if NET7_0_OR_GREATER
                return System.Numerics.BitOperations.PopCount(v);
#else
                // Fallback popcount
                int c = 0;
                while (v != 0)
                {
                    v &= v - 1;
                    c++;
                }
                return c;
#endif
            }

            private static ulong MixGamma(ulong z)
            {
                unchecked
                {
                    ulong z1 = Mix64Variant13(z) | 1UL; // force odd
                    int n = PopCount(z1 ^ (z1 >> 1));
                    return (n >= 24) ? z1 : (z1 ^ 0xaaaaaaaaaaaaaaaaUL);
                }
            }
        }
    }
}

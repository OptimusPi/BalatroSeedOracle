using System;

namespace Oracle.Services
{
    /// <summary>
    /// C# port of Daylatro's daily seed generation algorithm
    /// Generates the same Balatro seed that Daylatro shows for any given day
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
            // Convert to Modified Julian Day (same as Haskell Time.toModifiedJulianDay)
            long modifiedJulianDay = GetModifiedJulianDay(date.Date);

            // Create StdGen using the old Haskell mkStdGen algorithm
            var gen = MkStdGen32((int)modifiedJulianDay);

            // Generate 8 random values in range [0, 35] using uniformListR
            var seedChars = new char[8];
            for (int i = 0; i < 8; i++)
            {
                var (value, nextGen) = RandomR(0, 35, gen);
                gen = nextGen;

                // Convert to Base36 character (0-9 → '0'-'9', 10-35 → 'A'-'Z')
                // Same logic as Haskell: x + if x < 10 then 48 else 55
                seedChars[i] = (char)(value + (value < 10 ? 48 : 55));
            }

            return new string(seedChars);
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

        // Haskell StdGen implementation (old version)
        private struct StdGen
        {
            public int s1;
            public int s2;

            public StdGen(int s1, int s2)
            {
                this.s1 = s1;
                this.s2 = s2;
            }
        }

        // mkStdGen32 :: Int32 -> StdGen
        private static StdGen MkStdGen32(int s)
        {
            // From old Haskell System.Random:
            // mkStdGen32 s
            //  | s < 0     = mkStdGen32 (-s)
            //  | otherwise = StdGen (s1+1) (s2+1)
            //       where
            //         (q, s1) = s `divMod` 2147483562
            //         s2      = q `mod` 2147483398

            if (s < 0) s = -s;

            int q = Math.DivRem(s, 2147483562, out int s1);
            int s2 = q % 2147483398;
            if (s2 < 0) s2 += 2147483398;

            return new StdGen(s1 + 1, s2 + 1);
        }

        // randomR :: (Int, Int) -> StdGen -> (Int, StdGen)
        private static (int, StdGen) RandomR(int lo, int hi, StdGen g)
        {
            if (lo > hi)
            {
                var (swapped, g2) = RandomR(hi, lo, g);
                return (swapped, g2);
            }

            // For small ranges, we can use a simple approach
            var (n, nextGen) = Next(g);

            // Scale to range [lo, hi] using randomIvalInteger logic
            int k = hi - lo + 1;
            int b = 2147483561; // maxBound - 1
            int q = b / k;
            int r = b % k;

            int x = n;
            StdGen currentGen = nextGen;

            // Rejection sampling to ensure uniform distribution
            while (x > b - r)
            {
                var (next, nextG) = Next(currentGen);
                x = next;
                currentGen = nextG;
            }

            return (lo + (x % k), currentGen);
        }

        // next :: StdGen -> (Int, StdGen)
        private static (int, StdGen) Next(StdGen g)
        {
            // L'Ecuyer's algorithm from old Haskell System.Random
            int s1 = g.s1;
            int s2 = g.s2;

            int k = s1 / 53668;
            s1 = 40014 * (s1 - k * 53668) - k * 12211;
            if (s1 < 0) s1 += 2147483563;

            k = s2 / 52774;
            s2 = 40692 * (s2 - k * 52774) - k * 3791;
            if (s2 < 0) s2 += 2147483399;

            int z = s1 - s2;
            if (z < 1) z += 2147483562;

            return (z, new StdGen(s1, s2));
        }
    }
}
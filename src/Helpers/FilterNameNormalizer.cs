using System;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace BalatroSeedOracle.Helpers
{
    /// <summary>
    /// Normalizes filter names to file-safe formats with unique IDs
    /// </summary>
    public static class FilterNameNormalizer
    {
        // Regex to match any character that's not alphanumeric or space
        private static readonly Regex InvalidCharsRegex = new Regex(@"[^a-zA-Z0-9\s]", RegexOptions.Compiled);
        
        // Regex to match multiple spaces
        private static readonly Regex MultipleSpacesRegex = new Regex(@"\s+", RegexOptions.Compiled);
        
        /// <summary>
        /// Normalizes a filter name to a file-safe format with unique short ID
        /// Example: "pifreak's mega filter-- I love Wee Joker!!" -> "pifreaksMegaFilterILoveWeeJoker-ABC123"
        /// </summary>
        /// <param name="filterName">The original filter name</param>
        /// <param name="shortId">Optional custom short ID. If null, generates one from timestamp</param>
        /// <returns>File-safe normalized name with unique ID</returns>
        public static string NormalizeFilterName(string filterName, string? shortId = null)
        {
            Debug.Assert(!string.IsNullOrWhiteSpace(filterName), "Filter name must not be null or empty");
            
            // Step 1: Remove all non-alphanumeric characters except spaces
            string cleaned = InvalidCharsRegex.Replace(filterName, " ");
            
            // Step 2: Replace multiple spaces with single space
            cleaned = MultipleSpacesRegex.Replace(cleaned, " ");
            
            // Step 3: Trim whitespace
            cleaned = cleaned.Trim();
            
            // Step 4: Convert to PascalCase (capitalize first letter of each word)
            if (!string.IsNullOrEmpty(cleaned))
            {
                var words = cleaned.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                var pascalCased = new StringBuilder();
                
                foreach (var word in words)
                {
                    if (word.Length > 0)
                    {
                        // Capitalize first letter, keep rest as-is
                        pascalCased.Append(char.ToUpperInvariant(word[0]));
                        if (word.Length > 1)
                        {
                            pascalCased.Append(word.Substring(1).ToLowerInvariant());
                        }
                    }
                }
                
                cleaned = pascalCased.ToString();
            }
            
            // Step 5: Generate short ID if not provided
            if (string.IsNullOrEmpty(shortId))
            {
                shortId = GenerateShortId();
            }
            
            // Step 6: Limit length to prevent extremely long filenames
            // Max total length: ~200 chars (safe for most filesystems)
            // Reserve space for "-XXXXXX" (7 chars for dash and ID)
            const int maxBaseLength = 193;
            if (cleaned.Length > maxBaseLength)
            {
                cleaned = cleaned.Substring(0, maxBaseLength);
            }
            
            // Step 7: Combine with short ID
            return $"{cleaned}-{shortId}";
        }
        
        /// <summary>
        /// Generates a short unique ID based on timestamp and random component
        /// Format: 6 characters (3 from timestamp, 3 random)
        /// </summary>
        /// <returns>6-character unique ID</returns>
        public static string GenerateShortId()
        {
            // Use Unix timestamp seconds, take last 3 digits in base36
            var timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            var timeComponent = ConvertToBase36(timestamp % 46656); // 36^3 = 46656
            
            // Generate random component (3 chars)
            var random = new Random();
            var randomComponent = ConvertToBase36(random.Next(46656));
            
            // Combine and pad to ensure 6 chars total
            var shortId = (timeComponent + randomComponent).PadLeft(6, '0').ToUpperInvariant();
            
            // Ensure exactly 6 characters
            if (shortId.Length > 6)
            {
                shortId = shortId.Substring(0, 6);
            }
            
            return shortId;
        }
        
        /// <summary>
        /// Extracts the original filter name from a normalized filename (removes the ID suffix)
        /// </summary>
        /// <param name="normalizedName">The normalized filename</param>
        /// <returns>The filter name without ID, or original if no ID found</returns>
        public static string ExtractFilterName(string normalizedName)
        {
            // Look for last dash followed by ID pattern
            var lastDashIndex = normalizedName.LastIndexOf('-');
            if (lastDashIndex > 0 && lastDashIndex < normalizedName.Length - 1)
            {
                // Check if what follows looks like an ID (6 alphanumeric chars)
                var possibleId = normalizedName.Substring(lastDashIndex + 1);
                if (possibleId.Length == 6 && possibleId.All(char.IsLetterOrDigit))
                {
                    return normalizedName.Substring(0, lastDashIndex);
                }
            }
            
            return normalizedName;
        }
        
        /// <summary>
        /// Creates a DuckDB filename from a filter name
        /// </summary>
        /// <param name="filterName">The original filter name</param>
        /// <param name="searchId">Optional search GUID for uniqueness</param>
        /// <returns>Full path to DuckDB file</returns>
        public static string CreateDuckDbFilename(string filterName, string? searchId = null)
        {
            var normalizedName = NormalizeFilterName(filterName);
            var searchResultsDir = System.IO.Path.Combine(
                System.IO.Directory.GetCurrentDirectory(), 
                "SearchResults"
            );
            
            System.IO.Directory.CreateDirectory(searchResultsDir);
            
            // If searchId provided, append it for extra uniqueness
            if (!string.IsNullOrEmpty(searchId))
            {
                // Take first 8 chars of GUID for brevity
                var shortGuid = searchId.Replace("-", "").Substring(0, 8);
                normalizedName = $"{normalizedName}_{shortGuid}";
            }
            
            return System.IO.Path.Combine(searchResultsDir, $"{normalizedName}.duckdb");
        }
        
        /// <summary>
        /// Lists all DuckDB files that match a filter name pattern
        /// </summary>
        /// <param name="filterNamePattern">The filter name to search for</param>
        /// <returns>Array of matching DuckDB file paths</returns>
        public static string[] FindMatchingDuckDbFiles(string filterNamePattern)
        {
            var searchResultsDir = System.IO.Path.Combine(
                System.IO.Directory.GetCurrentDirectory(), 
                "SearchResults"
            );
            
            if (!System.IO.Directory.Exists(searchResultsDir))
                return Array.Empty<string>();
            
            var normalizedPattern = NormalizeFilterName(filterNamePattern, "*");
            var searchPattern = normalizedPattern.Replace("-*", "*");
            
            return System.IO.Directory.GetFiles(searchResultsDir, $"{searchPattern}*.duckdb");
        }
        
        private static string ConvertToBase36(long value)
        {
            const string chars = "0123456789abcdefghijklmnopqrstuvwxyz";
            var result = new StringBuilder();
            
            while (value > 0)
            {
                result.Insert(0, chars[(int)(value % 36)]);
                value /= 36;
            }
            
            return result.Length == 0 ? "0" : result.ToString();
        }
    }
}
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using BalatroSeedOracle.Services.Storage;

namespace BalatroSeedOracle.Services
{
    public interface IFilterService
    {
        Task<List<string>> GetAvailableFiltersAsync();
        Task<bool> DeleteFilterAsync(string filePath);
        Task<bool> ValidateFilterAsync(string filePath);
        string GenerateFilterFileName(string baseName);
        Task<string> GetFilterNameAsync(string filterId);
        Task<string> CloneFilterAsync(string filterId, string newName);
    }

    public class FilterService : IFilterService
    {
        private readonly IConfigurationService _configurationService;
        private readonly IAppDataStore _store;

        public FilterService(IConfigurationService configurationService, IAppDataStore store)
        {
            _configurationService = configurationService;
            _store = store;
        }

        public Task<List<string>> GetAvailableFiltersAsync()
        {
            var filters = new List<string>();

            try
            {
#if BROWSER
                var keys = _store.ListKeysAsync("Filters/").GetAwaiter().GetResult();
                foreach (var key in keys)
                {
                    if (!key.EndsWith(".json", StringComparison.OrdinalIgnoreCase))
                        continue;

                    var fileName = Path.GetFileName(key);
                    if (fileName.StartsWith("_UNSAVED_", StringComparison.OrdinalIgnoreCase)
                        || fileName.StartsWith("__TEMP_", StringComparison.OrdinalIgnoreCase))
                        continue;

                    filters.Add(key);
                }
#else
                var filtersDir = _configurationService.GetFiltersDirectory();
                if (Directory.Exists(filtersDir))
                {
                    var jsonFiles = Directory.GetFiles(filtersDir, "*.json");
                    foreach (var file in jsonFiles)
                    {
                        if (
                            !Path.GetFileName(file).StartsWith("_UNSAVED_")
                            && !Path.GetFileName(file).StartsWith("__TEMP_")
                        )
                        {
                            filters.Add(file);
                        }
                    }
                }
#endif
            }
            catch
            {
                // Error getting filters - return empty list
            }

            return Task.FromResult(filters);
        }

        public async Task<bool> DeleteFilterAsync(string filePath)
        {
            try
            {
                if (_configurationService.FileExists(filePath))
                {
#if BROWSER
                    await _store.DeleteAsync(filePath.Replace('\\', '/')).ConfigureAwait(false);
#else
                    // File.Delete is fast, no need for Task.Run
                    File.Delete(filePath);
#endif

                    // Remove from cache (use ServiceHelper to avoid circular dependency)
                    var filterId = Path.GetFileNameWithoutExtension(filePath);
                    var filterCache = Helpers.ServiceHelper.GetService<IFilterCacheService>();
                    filterCache?.RemoveFilter(filterId);

                    return true;
                }
                return false;
            }
            catch
            {
                // Error deleting filter
                return false;
            }
        }

        public async Task<bool> ValidateFilterAsync(string filePath)
        {
            try
            {
                var config =
                    await _configurationService.LoadFilterAsync<Motely.Filters.MotelyJsonConfig>(
                        filePath
                    );
                return config != null;
            }
            catch
            {
                return false;
            }
        }

        public string GenerateFilterFileName(string baseName)
        {
            var filtersDir = _configurationService.GetFiltersDirectory();
            // NORMALIZE the filter name to create a safe filename/ID
            var safeFileName = NormalizeFilterName(baseName);
            var fileName = $"{safeFileName}.json";
#if BROWSER
            return $"{filtersDir}/{fileName}";
#else
            return Path.Combine(filtersDir, fileName);
#endif
        }

        /// <summary>
        /// Normalize a filter name into a safe filename/ID that can be used across:
        /// - File system (JSON files)
        /// - Memory (in-memory cache keys)
        /// - DuckDB (database identifiers)
        ///
        /// Rules:
        /// - Replace spaces with underscores
        /// - Remove/replace invalid filename characters
        /// - Keep alphanumeric, underscore, hyphen only
        /// - Trim and collapse multiple underscores
        ///
        /// Examples:
        /// "Mega Search" -> "Mega_Search"
        /// "My Filter v2.1" -> "My_Filter_v21"
        /// "Test: Special (Cool)" -> "Test_Special_Cool"
        /// </summary>
        public static string NormalizeFilterName(string filterName)
        {
            if (string.IsNullOrWhiteSpace(filterName))
                return "unnamed_filter";

            // Start with trimmed input
            var normalized = filterName.Trim();

            // Replace spaces with underscores
            normalized = normalized.Replace(' ', '_');

            // Remove invalid filename characters: \ / : * ? " < > |
            var invalidChars = Path.GetInvalidFileNameChars();
            foreach (var ch in invalidChars)
            {
                normalized = normalized.Replace(ch.ToString(), "");
            }

            // Keep only alphanumeric, underscore, hyphen
            normalized = new string(
                normalized.Where(c => char.IsLetterOrDigit(c) || c == '_' || c == '-').ToArray()
            );

            // Collapse multiple underscores into single underscore
            while (normalized.Contains("__"))
            {
                normalized = normalized.Replace("__", "_");
            }

            // Trim underscores from start/end
            normalized = normalized.Trim('_');

            // If somehow empty after all that, use default
            if (string.IsNullOrEmpty(normalized))
                return "unnamed_filter";

            return normalized;
        }

        public async Task<string> GetFilterNameAsync(string filterId)
        {
            try
            {
                var filtersDir = _configurationService.GetFiltersDirectory();
                var filterPath = Path.Combine(filtersDir, $"{filterId}.json");

                if (File.Exists(filterPath))
                {
                    var json = await File.ReadAllTextAsync(filterPath);

                    var deserializeOptions = new System.Text.Json.JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true,
                        ReadCommentHandling = System.Text.Json.JsonCommentHandling.Skip,
                        AllowTrailingCommas = true,
                    };

                    var config =
                        System.Text.Json.JsonSerializer.Deserialize<Motely.Filters.MotelyJsonConfig>(
                            json,
                            deserializeOptions
                        );
                    return config?.Name ?? "";
                }
            }
            catch (Exception ex)
            {
                Helpers.DebugLogger.LogError(
                    "FilterService",
                    $"Error reading filter name: {ex.Message}"
                );
            }

            return string.Empty;
        }

        public async Task<string> CloneFilterAsync(string filterId, string newName)
        {
            try
            {
                var filtersDir = _configurationService.GetFiltersDirectory();
                var filterPath = Path.Combine(filtersDir, $"{filterId}.json");

                if (!File.Exists(filterPath))
                {
                    Helpers.DebugLogger.LogError(
                        "FilterService",
                        $"Filter not found: {filterPath}"
                    );
                    return string.Empty;
                }

                var json = await File.ReadAllTextAsync(filterPath);

                // Use same deserialization options as MotelyJsonConfig.TryLoadFromJsonFile
                var deserializeOptions = new System.Text.Json.JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true,
                    ReadCommentHandling = System.Text.Json.JsonCommentHandling.Skip,
                    AllowTrailingCommas = true,
                };

                var config =
                    System.Text.Json.JsonSerializer.Deserialize<Motely.Filters.MotelyJsonConfig>(
                        json,
                        deserializeOptions
                    );

                if (config != null)
                {
                    config.Name = newName; // Use custom name
                    config.DateCreated = DateTime.UtcNow;
                    var userProfileService = Helpers.ServiceHelper.GetService<UserProfileService>();
                    config.Author = userProfileService?.GetAuthorName() ?? "Unknown";

                    // Generate clean ID from name
                    var cleanName = newName.Replace(" ", "").ToLower();
                    var newId = $"{cleanName}_{Guid.NewGuid():N}";
                    var newPath = Path.Combine(filtersDir, $"{newId}.json");

                    // Use camelCase serialization to maintain JSON format consistency
                    var serializeOptions = new System.Text.Json.JsonSerializerOptions
                    {
                        WriteIndented = true,
                        PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase,
                        DefaultIgnoreCondition = System
                            .Text
                            .Json
                            .Serialization
                            .JsonIgnoreCondition
                            .WhenWritingNull,
                    };

                    var newJson = System.Text.Json.JsonSerializer.Serialize(
                        config,
                        serializeOptions
                    );
                    await File.WriteAllTextAsync(newPath, newJson);

                    // Cache will auto-refresh on next access (file watcher)
                    Helpers.DebugLogger.Log(
                        "FilterService",
                        $"Filter cloned: {filterId} -> {newId} (name: {newName})"
                    );
                    return newId;
                }
            }
            catch (Exception ex)
            {
                Helpers.DebugLogger.LogError(
                    "FilterService",
                    $"Error cloning filter: {ex.Message}"
                );
            }

            return string.Empty;
        }
    }
}

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using BalatroSeedOracle.Services.Storage;
using Motely.Filters.Jaml;

namespace BalatroSeedOracle.Services
{
    /// <summary>Saved-filter file management: listing, deleting, cloning, and naming filters
    /// on disk. Filter content itself loads through <see cref="IConfigurationService"/> and
    /// <c>JamlConfigLoader</c>; this service is the file/identity layer above that.</summary>
    public interface IFilterService
    {
        Task<List<string>> GetAvailableFiltersAsync();
        Task<bool> DeleteFilterAsync(string filePath);

        /// <summary>True if the file loads and deserializes as a <c>JamlConfig</c> without
        /// throwing — this is a load-succeeds check, not full JAML clause validation (that
        /// lives in <c>JamlConfigLoader.TryLoad</c>'s own error output).</summary>
        Task<bool> ValidateFilterAsync(string filePath);

        string GenerateFilterFileName(string baseName);
        Task<string> GetFilterNameAsync(string filterId);

        /// <summary>Copies an existing filter under a new name and a fresh generated ID
        /// (<c>{slug}_{guid}</c>), re-serialized through <c>JamlConfigLoader.ToJaml</c>. Returns
        /// the new filter's ID, or <see cref="string.Empty"/> if the source doesn't exist or
        /// fails to parse.</summary>
        Task<string> CloneFilterAsync(string filterId, string newName);
    }

    public class FilterService : IFilterService
    {
        private readonly IConfigurationService _configurationService;
        private readonly IAppDataStore _store;
        private readonly IFilterCacheService _filterCache;
        private readonly UserProfileService _userProfileService;
        private readonly IPlatformServices? _platformServices;

        public FilterService(
            IConfigurationService configurationService,
            IAppDataStore store,
            IFilterCacheService filterCache,
            UserProfileService userProfileService,
            IPlatformServices? platformServices = null
        )
        {
            _configurationService = configurationService;
            _store = store;
            _filterCache = filterCache;
            _userProfileService = userProfileService;
            _platformServices = platformServices;
        }

        public async Task<List<string>> GetAvailableFiltersAsync()
        {
            var filters = new List<string>();

            try
            {
                var isBrowser = _platformServices != null && !_platformServices.SupportsFileSystem;
                if (isBrowser)
                {
                    var keys = await _store.ListKeysAsync("Filters/").ConfigureAwait(false);
                    foreach (var key in keys)
                    {
                        if (!key.EndsWith(".json", StringComparison.OrdinalIgnoreCase))
                            continue;

                        var fileName = Path.GetFileName(key);
                        if (
                            fileName.StartsWith("_UNSAVED_", StringComparison.OrdinalIgnoreCase)
                            || fileName.StartsWith("__TEMP_", StringComparison.OrdinalIgnoreCase)
                        )
                            continue;

                        filters.Add(key);
                    }
                }
                else
                {
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
                }
            }
            catch
            {
                // Error getting filters - return empty list
            }

            return filters;
        }

        public async Task<bool> DeleteFilterAsync(string filePath)
        {
            try
            {
                // Check if file exists (synchronous check is acceptable here)
                if (_configurationService.FileExists(filePath))
                {
                    var isBrowser =
                        _platformServices != null && !_platformServices.SupportsFileSystem;
                    if (isBrowser)
                    {
                        await _store.DeleteAsync(filePath.Replace('\\', '/')).ConfigureAwait(false);
                    }
                    else
                    {
                        // File.Delete is fast, no need for Task.Run
                        File.Delete(filePath);
                    }

                    var filterId = Path.GetFileNameWithoutExtension(filePath);
                    _filterCache.RemoveFilter(filterId);

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
                    await _configurationService.LoadFilterAsync<Motely.Filters.Jaml.JamlConfig>(
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
            var isBrowser = _platformServices != null && !_platformServices.SupportsFileSystem;
            if (isBrowser)
            {
                return $"{filtersDir}/{fileName}";
            }
            else
            {
                return Path.Combine(filtersDir, fileName);
            }
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
                var filterPath = Path.Combine(
                    _configurationService.GetFiltersDirectory(),
                    $"{filterId}.json"
                );
                if (!File.Exists(filterPath))
                    return string.Empty;

                var yaml = await File.ReadAllTextAsync(filterPath);
                if (JamlConfigLoader.TryLoad(yaml, out var config, out _))
                    return config?.Name ?? "";
                return "";
            }
            catch (Exception ex)
            {
                Helpers.DebugLogger.LogError(
                    "FilterService",
                    $"Error reading filter name: {ex.Message}"
                );
                return string.Empty;
            }
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

                var yaml = await File.ReadAllTextAsync(filterPath);
                if (!JamlConfigLoader.TryLoad(yaml, out var config, out _) || config is null)
                    return string.Empty;

                config.Name = newName;
                config.Author = _userProfileService.GetAuthorName() ?? "Unknown";

                var newId = $"{newName.Replace(" ", "").ToLower()}_{Guid.NewGuid():N}";
                var newPath = Path.Combine(filtersDir, $"{newId}.json");
                var newYaml = JamlConfigLoader.ToJaml(config);
                await File.WriteAllTextAsync(newPath, newYaml);

                Helpers.DebugLogger.Log(
                    "FilterService",
                    $"Filter cloned: {filterId} -> {newId} (name: {newName})"
                );
                return newId;
            }
            catch (Exception ex)
            {
                Helpers.DebugLogger.LogError(
                    "FilterService",
                    $"Error cloning filter: {ex.Message}"
                );
                return string.Empty;
            }
        }
    }
}

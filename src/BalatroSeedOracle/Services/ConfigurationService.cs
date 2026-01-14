using System;
using System.IO;
using System.Diagnostics;
using System.Text.Json;
using System.Threading.Tasks;
using BalatroSeedOracle.Helpers;
using Microsoft.Extensions.Logging;
using BalatroSeedOracle.Models;
using BalatroSeedOracle.Services.Storage;
using Motely.Filters;

namespace BalatroSeedOracle.Services
{
    public interface IConfigurationService
    {
        Task<bool> SaveFilterAsync(string filePath, object config);
        Task<T?> LoadFilterAsync<T>(string filePath)
            where T : class;
        string GetTempFilterPath();
        string GetFiltersDirectory();
        bool FileExists(string filePath);
        void EnsureDirectoryExists(string directoryPath);
    }

    public class ConfigurationService : IConfigurationService
    {
        private readonly UserProfileService? _userProfileService;
        private readonly IFilterCacheService? _filterCacheService;
        private readonly IAppDataStore _store;
        private readonly IPlatformServices? _platformServices;

        public ConfigurationService(
            IAppDataStore store,
            UserProfileService? userProfileService = null,
            IFilterCacheService? filterCacheService = null,
            IPlatformServices? platformServices = null
        )
        {
            _store = store ?? throw new ArgumentNullException(nameof(store));
            _userProfileService = userProfileService;
            _filterCacheService = filterCacheService;
            _platformServices = platformServices;
        }

        public async Task<bool> SaveFilterAsync(string filePath, object config)
        {
            try
            {
                DebugLogger.Log("ConfigurationService", $"SaveFilterAsync called with path: {filePath}");
                
                var isBrowser = _platformServices != null && !_platformServices.SupportsFileSystem;
                DebugLogger.Log("ConfigurationService", isBrowser 
                    ? "Running in BROWSER mode - using IAppDataStore" 
                    : "Running in DESKTOP mode - using file system");

                // In browser, filePath is treated as a logical key (e.g. "Filters/MyFilter.json").
                if (_platformServices?.SupportsFileSystem == true)
                {
                    EnsureDirectoryExists(Path.GetDirectoryName(filePath)!);
                }

                if (config is Motely.Filters.MotelyJsonConfig motelyConfig)
                {
                    // Use FilterSerializationService for consistent, clean JSON output
                    // This omits null properties, empty arrays, and empty strings
                    var serializationService = new FilterSerializationService(_userProfileService!);
                    var json = serializationService.SerializeConfig(motelyConfig);

                    DebugLogger.Log("ConfigurationService", $"Serialized config to {json.Length} characters");

                    if (isBrowser)
                    {
                        await _store.WriteTextAsync(filePath.Replace('\\', '/'), json).ConfigureAwait(false);
                        DebugLogger.Log("ConfigurationService", $"Successfully wrote to browser store with key: {filePath.Replace('\\', '/')}");
                    }
                    else
                    {
                        await File.WriteAllTextAsync(filePath, json).ConfigureAwait(false);
                        DebugLogger.Log("ConfigurationService", $"Successfully wrote to file: {filePath}");
                    }

                    // Invalidate cache for this filter
                    var filterId = Path.GetFileNameWithoutExtension(filePath);
                    _filterCacheService?.InvalidateFilter(filterId);

                    return true;
                }

                return false;
            }
            catch (Exception ex)
            {
                // Error saving filter
                DebugLogger.LogError("ConfigurationService", $"ERROR saving filter to {filePath}: {ex.Message}");
                DebugLogger.LogError("ConfigurationService", $"Stack trace: {ex.StackTrace}");
                return false;
            }
        }

        public async Task<T?> LoadFilterAsync<T>(string filePath)
            where T : class
        {
            try
            {
                var isBrowser = _platformServices != null && !_platformServices.SupportsFileSystem;
                
                if (isBrowser)
                {
                    var json = await _store.ReadTextAsync(filePath.Replace('\\', '/')).ConfigureAwait(false);
                    if (string.IsNullOrWhiteSpace(json))
                        return null;

                    if (typeof(T) == typeof(Motely.Filters.MotelyJsonConfig))
                    {
                        // Hardened options (match FilterSerializationService)
                        var options = new JsonSerializerOptions
                        {
                            PropertyNameCaseInsensitive = true,
                            ReadCommentHandling = JsonCommentHandling.Skip,
                            AllowTrailingCommas = true,
                        };
                        var config = JsonSerializer.Deserialize<Motely.Filters.MotelyJsonConfig>(json, options);
                        return config as T;
                    }

                    return null;
                }
                else
                {
                    if (!File.Exists(filePath))
                        return null;

                    // For MotelyJsonConfig, try cache first for better performance
                    if (typeof(T) == typeof(Motely.Filters.MotelyJsonConfig))
                    {
                        if (_filterCacheService != null)
                        {
                            var cached = _filterCacheService.GetFilterByPath(filePath);
                            if (cached != null)
                            {
                                return cached as T;
                            }
                        }
                    }

                    // Fallback to disk loading
                    return await Task.Run(() =>
                    {
                        if (typeof(T) == typeof(Motely.Filters.MotelyJsonConfig))
                        {
                            if (
                                Motely.Filters.MotelyJsonConfig.TryLoadFromJsonFile(
                                    filePath,
                                    out var config
                                )
                            )
                            {
                                return config as T;
                            }
                        }
                        return null;
                    });
                }
            }
            catch
            {
                // Error loading filter
                return null;
            }
        }

        public string GetTempFilterPath()
        {
            // Use AppPaths which handles both dev and installed scenarios correctly
            var isBrowser = _platformServices != null && !_platformServices.SupportsFileSystem;
            if (isBrowser)
            {
                return "Filters/_UNSAVED_CREATION.json";
            }
            else
            {
                var filtersDir = Helpers.AppPaths.FiltersDir;
                return Path.Combine(filtersDir, "_UNSAVED_CREATION.json");
            }
        }

        public string GetFiltersDirectory()
        {
            // Use AppPaths which handles both dev and installed scenarios correctly
            var isBrowser = _platformServices != null && !_platformServices.SupportsFileSystem;
            if (isBrowser)
            {
                return "Filters";
            }
            else
            {
                return Helpers.AppPaths.FiltersDir;
            }
        }

        public bool FileExists(string filePath)
        {
            var isBrowser = _platformServices != null && !_platformServices.SupportsFileSystem;
            if (isBrowser)
            {
                // Synchronous check for browser - use cached result if available
                // For browser, we can't block, so we return false and let async methods handle it
                // This is acceptable because FileExists is typically used for quick checks
                // and the actual async operations will handle the real check
                try
                {
                    // Try to get result synchronously with timeout
                    var task = _store.ExistsAsync(filePath.Replace('\\', '/'));
                    if (task.IsCompleted)
                        return task.Result;
                    // If not completed, return false and let caller use async method
                    return false;
                }
                catch
                {
                    return false;
                }
            }
            else
            {
                return File.Exists(filePath);
            }
        }

        public void EnsureDirectoryExists(string directoryPath)
        {
            if (_platformServices?.SupportsFileSystem == true && !string.IsNullOrEmpty(directoryPath))
            {
                _platformServices.EnsureDirectoryExists(directoryPath);
            }
        }
    }
}

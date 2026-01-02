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

        public ConfigurationService(
            IAppDataStore store,
            UserProfileService? userProfileService = null,
            IFilterCacheService? filterCacheService = null
        )
        {
            _store = store ?? throw new ArgumentNullException(nameof(store));
            _userProfileService = userProfileService;
            _filterCacheService = filterCacheService;
        }

        public async Task<bool> SaveFilterAsync(string filePath, object config)
        {
            try
            {
                DebugLogger.Log("ConfigurationService", $"SaveFilterAsync called with path: {filePath}");
                Debug.WriteLine($"ConfigurationService.SaveFilterAsync called with path: {filePath}");
                
#if BROWSER
                DebugLogger.Log("ConfigurationService", "Running in BROWSER mode - using IAppDataStore");
                Debug.WriteLine("Running in BROWSER mode - using IAppDataStore");
#else
                DebugLogger.Log("ConfigurationService", "Running in DESKTOP mode - using file system");
                Debug.WriteLine("Running in DESKTOP mode - using file system");
#endif

                // In browser, filePath is treated as a logical key (e.g. "Filters/MyFilter.json").
#if !BROWSER
                EnsureDirectoryExists(Path.GetDirectoryName(filePath)!);
#endif

                if (config is Motely.Filters.MotelyJsonConfig motelyConfig)
                {
                    // Use FilterSerializationService for consistent, clean JSON output
                    // This omits null properties, empty arrays, and empty strings
                    var serializationService = new FilterSerializationService(_userProfileService!);
                    var json = serializationService.SerializeConfig(motelyConfig);

                    DebugLogger.Log("ConfigurationService", $"Serialized config to {json.Length} characters");
                    Debug.WriteLine($"Serialized config to {json.Length} characters");

#if BROWSER
                    await _store.WriteTextAsync(filePath.Replace('\\', '/'), json).ConfigureAwait(false);
                    DebugLogger.Log("ConfigurationService", $"Successfully wrote to browser store with key: {filePath.Replace('\\', '/')}");
                    Debug.WriteLine($"Successfully wrote to browser store with key: {filePath.Replace('\\', '/')}");
#else
                    await File.WriteAllTextAsync(filePath, json).ConfigureAwait(false);
                    DebugLogger.Log("ConfigurationService", $"Successfully wrote to file: {filePath}");
                    Debug.WriteLine($"Successfully wrote to file: {filePath}");
#endif

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
                Debug.WriteLine($"ERROR saving filter to {filePath}: {ex.Message}");
                Helpers.DebugLogger.LogError("ConfigurationService", $"Error saving filter to {filePath}: {ex.Message}");
                return false;
            }
        }

        public async Task<T?> LoadFilterAsync<T>(string filePath)
            where T : class
        {
            try
            {
#if BROWSER
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
#else
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
#endif
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
#if BROWSER
            return "Filters/_UNSAVED_CREATION.json";
#else
            var filtersDir = Helpers.AppPaths.FiltersDir;
            return Path.Combine(filtersDir, "_UNSAVED_CREATION.json");
#endif
        }

        public string GetFiltersDirectory()
        {
            // Use AppPaths which handles both dev and installed scenarios correctly
#if BROWSER
            return "Filters";
#else
            return Helpers.AppPaths.FiltersDir;
#endif
        }

        public bool FileExists(string filePath)
        {
#if BROWSER
            return _store.ExistsAsync(filePath.Replace('\\', '/')).GetAwaiter().GetResult();
#else
            return File.Exists(filePath);
#endif
        }

        public void EnsureDirectoryExists(string directoryPath)
        {
#if !BROWSER
            if (!string.IsNullOrEmpty(directoryPath))
            {
                Directory.CreateDirectory(directoryPath);
            }
#endif
        }
    }
}

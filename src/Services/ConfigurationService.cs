using System;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;

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

        public ConfigurationService(
            UserProfileService? userProfileService = null,
            IFilterCacheService? filterCacheService = null
        )
        {
            _userProfileService = userProfileService;
            _filterCacheService = filterCacheService;
        }

        public async Task<bool> SaveFilterAsync(string filePath, object config)
        {
            try
            {
                EnsureDirectoryExists(Path.GetDirectoryName(filePath)!);

                if (config is Motely.Filters.MotelyJsonConfig motelyConfig)
                {
                    // Use FilterSerializationService for consistent, clean JSON output
                    // This omits null properties, empty arrays, and empty strings
                    var serializationService = new FilterSerializationService(_userProfileService!);
                    var json = serializationService.SerializeConfig(motelyConfig);

                    await File.WriteAllTextAsync(filePath, json);

                    // Invalidate cache for this filter
                    var filterId = Path.GetFileNameWithoutExtension(filePath);
                    _filterCacheService?.InvalidateFilter(filterId);

                    return true;
                }

                return false;
            }
            catch
            {
                // Error saving filter
                return false;
            }
        }

        public async Task<T?> LoadFilterAsync<T>(string filePath)
            where T : class
        {
            try
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
            catch
            {
                // Error loading filter
                return null;
            }
        }

        public string GetTempFilterPath()
        {
            // Use AppPaths which handles both dev and installed scenarios correctly
            var filtersDir = Helpers.AppPaths.FiltersDir;
            return Path.Combine(filtersDir, "_UNSAVED_CREATION.json");
        }

        public string GetFiltersDirectory()
        {
            // Use AppPaths which handles both dev and installed scenarios correctly
            return Helpers.AppPaths.FiltersDir;
        }

        public bool FileExists(string filePath)
        {
            return File.Exists(filePath);
        }

        public void EnsureDirectoryExists(string directoryPath)
        {
            if (!string.IsNullOrEmpty(directoryPath))
            {
                Directory.CreateDirectory(directoryPath);
            }
        }
    }
}

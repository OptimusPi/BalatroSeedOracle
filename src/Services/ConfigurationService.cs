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
        public async Task<bool> SaveFilterAsync(string filePath, object config)
        {
            try
            {
                EnsureDirectoryExists(Path.GetDirectoryName(filePath)!);

                if (config is Motely.Filters.MotelyJsonConfig motelyConfig)
                {
                    // Use FilterSerializationService for consistent, clean JSON output
                    // This omits null properties, empty arrays, and empty strings
                    var userProfileService = Helpers.ServiceHelper.GetService<UserProfileService>();
                    var serializationService = new FilterSerializationService(userProfileService!);
                    var json = serializationService.SerializeConfig(motelyConfig);

                    await File.WriteAllTextAsync(filePath, json);

                    // Invalidate cache for this filter (use ServiceHelper to avoid circular dependency)
                    var filterId = Path.GetFileNameWithoutExtension(filePath);
                    var filterCache = Helpers.ServiceHelper.GetService<IFilterCacheService>();
                    filterCache?.InvalidateFilter(filterId);

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
                    var filterCache = Helpers.ServiceHelper.GetService<IFilterCacheService>();
                    if (filterCache != null)
                    {
                        var cached = filterCache.GetFilterByPath(filePath);
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
            // Use current working directory so filters are loaded from project root when running with `dotnet run`
            var baseDir = Environment.CurrentDirectory;
            var filtersDir = Path.Combine(baseDir, "JsonItemFilters");
            EnsureDirectoryExists(filtersDir);
            return Path.Combine(filtersDir, "_UNSAVED_CREATION.json");
        }

        public string GetFiltersDirectory()
        {
            // Use current working directory so filters are loaded from project root when running with `dotnet run`
            var baseDir = Environment.CurrentDirectory;
            return Path.Combine(baseDir, "JsonItemFilters");
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

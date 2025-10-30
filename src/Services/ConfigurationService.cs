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
                    var json = System.Text.Json.JsonSerializer.Serialize(
                        motelyConfig,
                        new System.Text.Json.JsonSerializerOptions
                        {
                            WriteIndented = true,
                            DefaultIgnoreCondition = System
                                .Text
                                .Json
                                .Serialization
                                .JsonIgnoreCondition
                                .WhenWritingNull,
                        }
                    );
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
            var baseDir =
                Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location)
                ?? AppDomain.CurrentDomain.BaseDirectory;
            var filtersDir = Path.Combine(baseDir, "JsonItemFilters");
            EnsureDirectoryExists(filtersDir);
            return Path.Combine(filtersDir, "_UNSAVED_CREATION.json");
        }

        public string GetFiltersDirectory()
        {
            var baseDir =
                Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location)
                ?? AppDomain.CurrentDomain.BaseDirectory;
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

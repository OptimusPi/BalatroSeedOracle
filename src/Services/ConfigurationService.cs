using System;
using System.IO;
using System.Threading.Tasks;
using System.Text.Json;

namespace BalatroSeedOracle.Services
{
    public interface IConfigurationService
    {
        Task<bool> SaveFilterAsync(string filePath, object config);
        Task<T?> LoadFilterAsync<T>(string filePath) where T : class;
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
                    var json = System.Text.Json.JsonSerializer.Serialize(motelyConfig, new System.Text.Json.JsonSerializerOptions
                    {
                        WriteIndented = true,
                        DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
                    });
                    await File.WriteAllTextAsync(filePath, json);
                    return true;
                }
                
                return false;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error saving filter: {ex.Message}");
                return false;
            }
        }

        public async Task<T?> LoadFilterAsync<T>(string filePath) where T : class
        {
            try
            {
                if (!File.Exists(filePath))
                    return null;

                return await Task.Run(() =>
                {
                    if (typeof(T) == typeof(Motely.Filters.MotelyJsonConfig))
                    {
                        if (Motely.Filters.MotelyJsonConfig.TryLoadFromJsonFile(filePath, out var config))
                        {
                            return config as T;
                        }
                    }
                    return null;
                });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error loading filter: {ex.Message}");
                return null;
            }
        }

        public string GetTempFilterPath()
        {
            var baseDir = Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location) 
                         ?? AppDomain.CurrentDomain.BaseDirectory;
            var filtersDir = Path.Combine(baseDir, "JsonItemFilters");
            EnsureDirectoryExists(filtersDir);
            return Path.Combine(filtersDir, "_UNSAVED_CREATION.json");
        }

        public string GetFiltersDirectory()
        {
            var baseDir = Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location) 
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
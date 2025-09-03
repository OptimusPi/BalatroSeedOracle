using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace BalatroSeedOracle.Services
{
    public interface IFilterService
    {
        Task<List<string>> GetAvailableFiltersAsync();
        Task<bool> DeleteFilterAsync(string filePath);
        Task<bool> ValidateFilterAsync(string filePath);
        string GenerateFilterFileName(string baseName);
    }

    public class FilterService : IFilterService
    {
        private readonly IConfigurationService _configurationService;

        public FilterService(IConfigurationService configurationService)
        {
            _configurationService = configurationService;
        }

        public Task<List<string>> GetAvailableFiltersAsync()
        {
            var filters = new List<string>();
            
            try
            {
                var filtersDir = _configurationService.GetFiltersDirectory();
                if (Directory.Exists(filtersDir))
                {
                    var jsonFiles = Directory.GetFiles(filtersDir, "*.json");
                    foreach (var file in jsonFiles)
                    {
                        if (!Path.GetFileName(file).StartsWith("_UNSAVED_") && !Path.GetFileName(file).StartsWith("__TEMP_"))
                        {
                            filters.Add(file);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error getting available filters: {ex.Message}");
            }

            return Task.FromResult(filters);
        }

        public async Task<bool> DeleteFilterAsync(string filePath)
        {
            try
            {
                if (_configurationService.FileExists(filePath))
                {
                    await Task.Run(() => File.Delete(filePath));
                    return true;
                }
                return false;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error deleting filter: {ex.Message}");
                return false;
            }
        }

        public async Task<bool> ValidateFilterAsync(string filePath)
        {
            try
            {
                var config = await _configurationService.LoadFilterAsync<Motely.Filters.MotelyJsonConfig>(filePath);
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
            var timestamp = DateTime.Now.ToString("yyyyMMddHHmmss");
            var fileName = $"{baseName}_{timestamp}.json";
            return Path.Combine(filtersDir, fileName);
        }
    }
}
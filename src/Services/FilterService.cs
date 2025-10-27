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
                    await Task.Run(() => File.Delete(filePath));
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
            var fileName = $"{baseName}.json";
            return Path.Combine(filtersDir, fileName);
        }
    }
}
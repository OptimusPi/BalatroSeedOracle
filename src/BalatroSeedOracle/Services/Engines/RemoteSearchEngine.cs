using System;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using Motely.Filters;
using Motely; // For SearchOptionsDto
using BalatroSeedOracle.Helpers;
using DebugLogger = BalatroSeedOracle.Helpers.DebugLogger; // Resolve ambiguity

namespace BalatroSeedOracle.Services.Engines
{
    public class RemoteSearchEngine : ISearchEngine
    {
        private readonly HttpClient _httpClient;
        private readonly string _baseUrl;

        public string Name => $"Remote ({_baseUrl})";
        public bool IsLocal => false;

        public RemoteSearchEngine(string baseUrl)
        {
            _baseUrl = baseUrl.TrimEnd('/');
            _httpClient = new HttpClient { BaseAddress = new Uri(_baseUrl) };
        }

        public async Task<string> StartSearchAsync(MotelyJsonConfig config, SearchOptionsDto options)
        {
            try 
            {
                // Map to API Request Model
                var request = new 
                {
                    Config = config,
                    Options = options
                };

                var response = await _httpClient.PostAsJsonAsync("/search", request);
                response.EnsureSuccessStatusCode();
                
                var result = await response.Content.ReadFromJsonAsync<SearchStartResponse>();
                return result?.SearchId ?? throw new Exception("No search ID returned");
            }
            catch (Exception ex)
            {
                DebugLogger.LogError("RemoteSearch", $"Failed to start search: {ex.Message}");
                throw;
            }
        }

        public async Task StopSearchAsync(string searchId)
        {
            try
            {
                await _httpClient.DeleteAsync($"/search/{searchId}");
            }
            catch (Exception ex)
            {
                DebugLogger.LogError("RemoteSearch", $"Failed to stop search: {ex.Message}");
            }
        }

        public async Task<bool> PingAsync()
        {
            try
            {
                var response = await _httpClient.GetAsync("/health"); // or /searches
                return response.IsSuccessStatusCode;
            }
            catch
            {
                return false;
            }
        }
        
        private record SearchStartResponse(string SearchId);
    }
}

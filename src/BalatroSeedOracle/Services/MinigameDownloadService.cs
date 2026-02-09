using System;
using System.Net.Http;
using System.Threading.Tasks;
using System.Text.Json;
using Motely.Filters;
using Motely; // For JamlConfigLoader (it's in Motely namespace)

namespace BalatroSeedOracle.Services
{
    public class MinigameDownloadService
    {
        private readonly HttpClient _httpClient;
        // In production, this would be your worker URL (e.g. https://api.weejoker.app)
        private const string BaseUrl = "https://api.weejoker.app"; 

        public MinigameDownloadService()
        {
            _httpClient = new HttpClient();
        }

        public async Task<MotelyJsonConfig?> FetchGameConfigAsync(string gameId)
        {
            // ONE JAML TO RULE THEM ALL
            // e.g. https://r2.weejoker.app/games/weejoker_season1.jaml
            string url = $"{BaseUrl}/games/{gameId}.jaml";
            
            try 
            {
                var response = await _httpClient.GetAsync(url);
                if (!response.IsSuccessStatusCode) return null;

                var content = await response.Content.ReadAsStringAsync();
                
                // Return raw config - the ViewModel will handle the "Pick Seed by Day" logic
                // The JAML contains ALL seeds for the season.
                // We need to parse JAML here. Since JamlConfigLoader might be available:
                if (JamlConfigLoader.TryLoadFromJamlString(content, out var config, out _))
                {
                    return config;
                }
                
                return null; 
            }
            catch
            {
                return null;
            }
        }
    }
}
using System;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using BalatroSeedOracle.Helpers;

namespace BalatroSeedOracle.ViewModels
{
    /// <summary>
    /// ViewModel for GenieWidget - AI-powered filter generation
    /// Uses Cloudflare Workers AI (FREE!) to convert natural language to Motely JSON
    /// </summary>
    public partial class GenieWidgetViewModel : BaseWidgetViewModel
    {
        private static readonly HttpClient _httpClient = new();
        private const string GENIE_API = "https://balatrogenie.app/generate";

        [ObservableProperty]
        private string _userPrompt = string.Empty;

        [ObservableProperty]
        private bool _isGenerating = false;

        [ObservableProperty]
        private string _statusMessage = string.Empty;

        [ObservableProperty]
        private string _statusColor = "#666666";

        [ObservableProperty]
        private bool _hasStatusMessage = false;

        [ObservableProperty]
        private bool _hasGeneratedConfig = false;

        [ObservableProperty]
        private string _generatedFilterName = string.Empty;

        [ObservableProperty]
        private string _generatedFilterSummary = string.Empty;

        [ObservableProperty]
        private string _generatedJson = string.Empty;

        public string GenerateButtonText => IsGenerating ? "✨ Generating..." : "🧞 Generate Filter";

        public string GeneratingSpinner => IsGenerating ? "⏳" : "";

        public GenieWidgetViewModel()
        {
            WidgetTitle = "Filter Genie";
            WidgetIcon = "🧞";
            IsMinimized = true;

            // Position in top-left corner
            PositionX = 20;
            PositionY = 20;
        }

        [RelayCommand]
        private void ToggleMinimize()
        {
            IsMinimized = !IsMinimized;
        }

        [RelayCommand(CanExecute = nameof(CanGenerate))]
        private async Task GenerateFilter()
        {
            if (string.IsNullOrWhiteSpace(UserPrompt))
                return;

            IsGenerating = true;
            HasGeneratedConfig = false;
            SetStatus("🧞 Asking the genie...", "#6B46C1");

            try
            {
                // Call Cloudflare Workers AI via your existing balatrogenie.app
                var requestBody = new { prompt = UserPrompt };
                var json = JsonSerializer.Serialize(requestBody);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await _httpClient.PostAsync(GENIE_API, content);
                var responseText = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode)
                {
                    var result = JsonSerializer.Deserialize<GenieResponse>(responseText);

                    if (result?.success == true && result.config != null)
                    {
                        // Extract filter info
                        GeneratedFilterName = result.config.name ?? "Generated Filter";
                        GeneratedJson = JsonSerializer.Serialize(result.config, new JsonSerializerOptions { WriteIndented = true });

                        // Create summary
                        int mustCount = result.config.must?.Count ?? 0;
                        int shouldCount = result.config.should?.Count ?? 0;
                        int mustNotCount = result.config.mustNot?.Count ?? 0;

                        GeneratedFilterSummary = $"Deck: {result.config.deck ?? "Any"} | Stake: {result.config.stake ?? "Any"}\n";
                        GeneratedFilterSummary += $"{mustCount} must-have, {shouldCount} should-have, {mustNotCount} must-not items";

                        HasGeneratedConfig = true;
                        SetStatus("✅ Filter generated!", "#22C55E");
                    }
                    else
                    {
                        SetStatus($"❌ Generation failed: {result?.error ?? "Unknown error"}", "#EF4444");
                    }
                }
                else
                {
                    SetStatus($"❌ API error: {response.StatusCode}", "#EF4444");
                }
            }
            catch (Exception ex)
            {
                DebugLogger.LogError("GenieWidget", $"Generation failed: {ex.Message}");
                SetStatus($"❌ Error: {ex.Message}", "#EF4444");
            }
            finally
            {
                IsGenerating = false;
                OnPropertyChanged(nameof(GenerateButtonText));
                OnPropertyChanged(nameof(GeneratingSpinner));
            }
        }

        private bool CanGenerate() => !string.IsNullOrWhiteSpace(UserPrompt) && !IsGenerating;

        [RelayCommand]
        private async Task SaveFilter()
        {
            if (string.IsNullOrWhiteSpace(GeneratedJson))
                return;

            try
            {
                // Save to filters directory
                var filtersPath = System.IO.Path.Combine(
                    AppDomain.CurrentDomain.BaseDirectory,
                    "filters"
                );

                if (!System.IO.Directory.Exists(filtersPath))
                    System.IO.Directory.CreateDirectory(filtersPath);

                // Sanitize filename
                var fileName = GeneratedFilterName
                    .Replace(" ", "_")
                    .Replace("/", "_")
                    .Replace("\\", "_");

                var filePath = System.IO.Path.Combine(filtersPath, $"{fileName}.json");

                // Check if file exists, add number if needed
                int counter = 1;
                while (System.IO.File.Exists(filePath))
                {
                    filePath = System.IO.Path.Combine(filtersPath, $"{fileName}_{counter}.json");
                    counter++;
                }

                await System.IO.File.WriteAllTextAsync(filePath, GeneratedJson);

                SetStatus($"💾 Saved to {System.IO.Path.GetFileName(filePath)}", "#22C55E");
            }
            catch (Exception ex)
            {
                DebugLogger.LogError("GenieWidget", $"Save failed: {ex.Message}");
                SetStatus($"❌ Save failed: {ex.Message}", "#EF4444");
            }
        }

        [RelayCommand]
        private async Task SearchFilter()
        {
            if (string.IsNullOrWhiteSpace(GeneratedJson))
            {
                SetStatus("❌ No filter to search!", "#EF4444");
                return;
            }

            try
            {
                // Parse the generated JSON into a MotelyJsonConfig
                var config = System.Text.Json.JsonSerializer.Deserialize<Motely.Filters.MotelyJsonConfig>(GeneratedJson);

                if (config == null)
                {
                    SetStatus("❌ Failed to parse filter config", "#EF4444");
                    return;
                }

                // Get the SearchManager service
                var searchManager = ServiceHelper.GetRequiredService<Services.SearchManager>();

                // Create search criteria with defaults
                var criteria = new Models.SearchCriteria
                {
                    Deck = config.Deck ?? "Red",
                    Stake = config.Stake ?? "White",
                    ThreadCount = Environment.ProcessorCount,
                    BatchSize = 3
                };

                // Start the search
                var searchInstance = await searchManager.StartSearchAsync(criteria, config);

                SetStatus($"🔍 Search started: {GeneratedFilterName}", "#22C55E");

                // Future enhancement: Add progress tracking UI
                // Currently the search runs successfully in background
            }
            catch (Exception ex)
            {
                DebugLogger.LogError("GenieWidget", $"Search failed: {ex.Message}");
                SetStatus($"❌ Search failed: {ex.Message}", "#EF4444");
            }
        }

        [RelayCommand]
        private void UseExample(string examplePrompt)
        {
            UserPrompt = examplePrompt;
        }

        private void SetStatus(string message, string color)
        {
            StatusMessage = message;
            StatusColor = color;
            HasStatusMessage = !string.IsNullOrEmpty(message);
        }

        partial void OnUserPromptChanged(string value)
        {
            GenerateFilterCommand.NotifyCanExecuteChanged();
        }

        // Response models
        private class GenieResponse
        {
            public bool success { get; set; }
            public string? error { get; set; }
            public FilterConfig? config { get; set; }
        }

        private class FilterConfig
        {
            public string? name { get; set; }
            public string? description { get; set; }
            public string? author { get; set; }
            public string? deck { get; set; }
            public string? stake { get; set; }
            public System.Collections.Generic.List<object>? must { get; set; }
            public System.Collections.Generic.List<object>? should { get; set; }
            public System.Collections.Generic.List<object>? mustNot { get; set; }
        }
    }
}

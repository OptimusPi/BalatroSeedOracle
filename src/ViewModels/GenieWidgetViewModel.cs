using System;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using BalatroSeedOracle.Helpers;
using BalatroSeedOracle.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace BalatroSeedOracle.ViewModels
{
    /// <summary>
    /// ViewModel for GenieWidget - AI-powered filter generation
    /// Uses local Host API (/genie endpoint) for keyword-based JAML generation
    /// Falls back to Cloudflare Workers AI if configured
    /// </summary>
    public partial class GenieWidgetViewModel : BaseWidgetViewModel
    {
        private static readonly HttpClient _httpClient = new();
        private const string LOCAL_GENIE_API = "http://localhost:3141/genie";
        private const string CLOUD_GENIE_API = "https://balatrogenie.app/generate";
        private readonly SearchManager _searchManager;

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

        /// <summary>
        /// Combined button content to prevent TextBlock selection issues
        /// </summary>
        public string ButtonContent => GenerateButtonText + " " + GeneratingSpinner;

        public GenieWidgetViewModel(
            SearchManager searchManager,
            WidgetPositionService? widgetPositionService = null
        )
            : base(widgetPositionService)
        {
            _searchManager = searchManager;
            WidgetTitle = "Filter Genie";
            WidgetIcon = "🧞";
            IsMinimized = true;

            // Set fixed position for Genie widget - first position
            PositionX = 20;
            PositionY = 80;
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
                var requestBody = new { prompt = UserPrompt };
                var json = JsonSerializer.Serialize(requestBody);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                // Try local Host API first (fast, no network dependency)
                string? jamlResult = null;

                try
                {
                    _httpClient.Timeout = TimeSpan.FromSeconds(5);
                    var localResponse = await _httpClient.PostAsync(LOCAL_GENIE_API, content);

                    if (localResponse.IsSuccessStatusCode)
                    {
                        var localResponseText = await localResponse.Content.ReadAsStringAsync();
                        var localResult = JsonSerializer.Deserialize<LocalGenieResponse>(localResponseText);
                        jamlResult = localResult?.jaml;
                        SetStatus("🧞 Local genie responded!", "#6B46C1");
                    }
                }
                catch (Exception localEx)
                {
                    DebugLogger.Log("GenieWidget", $"Local API not available: {localEx.Message}");
                    // Local not available - that's OK, we'll show helpful message
                }

                if (string.IsNullOrWhiteSpace(jamlResult))
                {
                    // Local API not running - show helpful message
                    SetStatus("💡 Start Host API widget first! (localhost:3141)", "#F59E0B");
                    return;
                }

                // Parse the JAML to get a JSON config for display
                if (Motely.JamlConfigLoader.TryLoadFromJamlString(jamlResult!, out var config, out var parseError) && config != null)
                {
                    GeneratedFilterName = config.Name ?? "Generated Filter";
                    GeneratedJson = JsonSerializer.Serialize(
                        config,
                        new JsonSerializerOptions { WriteIndented = true }
                    );

                    int mustCount = config.Must?.Count ?? 0;
                    int shouldCount = config.Should?.Count ?? 0;
                    int mustNotCount = config.MustNot?.Count ?? 0;

                    GeneratedFilterSummary =
                        $"Deck: {config.Deck ?? "Any"} | Stake: {config.Stake ?? "Any"}\n";
                    GeneratedFilterSummary +=
                        $"{mustCount} must-have, {shouldCount} should-have, {mustNotCount} must-not items";

                    HasGeneratedConfig = true;
                    SetStatus("✅ Filter generated!", "#22C55E");
                }
                else
                {
                    // JAML parsing failed - show the raw JAML anyway
                    GeneratedFilterName = "Generated Filter";
                    GeneratedJson = jamlResult;
                    GeneratedFilterSummary = "Raw JAML (parsing issue)";
                    HasGeneratedConfig = true;
                    SetStatus($"⚠️ Generated but couldn't parse: {parseError}", "#F59E0B");
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
                OnPropertyChanged(nameof(ButtonContent));
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
                var filtersPath = System.IO.Path.Combine(AppContext.BaseDirectory, "filters");

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
                var config =
                    System.Text.Json.JsonSerializer.Deserialize<Motely.Filters.MotelyJsonConfig>(
                        GeneratedJson
                    );

                if (config == null)
                {
                    SetStatus("❌ Failed to parse filter config", "#EF4444");
                    return;
                }

                // Create search criteria with defaults
                var criteria = new Models.SearchCriteria
                {
                    Deck = config.Deck ?? "Red",
                    Stake = config.Stake ?? "White",
                    ThreadCount = Environment.ProcessorCount,
                    BatchSize = 3,
                };

                // Start the search
                var searchInstance = await _searchManager.StartSearchAsync(criteria, config);

                SetStatus($"🔍 Search started: {GeneratedFilterName}", "#22C55E");
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
        private class LocalGenieResponse
        {
            public string? jaml { get; set; }
            public string? error { get; set; }
        }
    }
}

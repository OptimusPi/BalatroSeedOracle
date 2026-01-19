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
    /// ViewModel for GenieWidget - RAG-powered AI filter generation
    /// Uses Cloudflare Workers AI with Vectorize RAG for intelligent JAML generation
    /// Falls back to local Host API (/genie endpoint) for keyword-based generation
    /// </summary>
    public partial class GenieWidgetViewModel : BaseWidgetViewModel
    {
        private static readonly HttpClient _httpClient = new();
        private const string LOCAL_GENIE_API = "http://localhost:3141/genie";
        private const string CLOUD_GENIE_API = "https://jamlgenie.optimuspi.workers.dev";
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
            WidgetIcon = "Creation";
            IsMinimized = true;

            // Set fixed position for Genie widget - first position
            PositionX = 20;
            PositionY = 80;
        }

        // ToggleMinimize is inherited from BaseWidgetViewModel

        [RelayCommand(CanExecute = nameof(CanGenerate))]
        private async Task GenerateFilter()
        {
            if (string.IsNullOrWhiteSpace(UserPrompt))
                return;

            IsGenerating = true;
            HasGeneratedConfig = false;
            SetStatus("Asking RAG-powered genie...", "#6B46C1");

            try
            {
                var requestBody = new { prompt = UserPrompt };
                var json = JsonSerializer.Serialize(requestBody);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                string? jamlResult = null;

                // Try Cloudflare Workers AI first
                try
                {
                    _httpClient.Timeout = TimeSpan.FromSeconds(30);
                    SetStatus("Consulting RAG AI...", "#6B46C1");
                    var cloudResponse = await _httpClient.PostAsync(CLOUD_GENIE_API, content);

                    if (cloudResponse.IsSuccessStatusCode)
                    {
                        var cloudResponseText = await cloudResponse.Content.ReadAsStringAsync();
                        var cloudResult = JsonSerializer.Deserialize<CloudGenieResponse>(
                            cloudResponseText,
                            new JsonSerializerOptions { PropertyNameCaseInsensitive = true }
                        );
                        jamlResult = cloudResult?.jaml;
                        if (!string.IsNullOrWhiteSpace(jamlResult))
                        {
                            SetStatus("RAG AI responded with context!", "#6B46C1");
                        }
                        else if (cloudResult?.success == false)
                        {
                            SetStatus($"AI error: {cloudResult?.error}", "#EF4444");
                        }
                    }
                }
                catch (Exception cloudEx)
                {
                    DebugLogger.Log("GenieWidget", $"Cloud API error: {cloudEx.Message}");
                }

                // Fall back to local Host API if cloud failed
                if (string.IsNullOrWhiteSpace(jamlResult))
                {
                    try
                    {
                        _httpClient.Timeout = TimeSpan.FromSeconds(5);
                        SetStatus("Trying local genie...", "#6B46C1");
                        var localResponse = await _httpClient.PostAsync(LOCAL_GENIE_API, content);

                        if (localResponse.IsSuccessStatusCode)
                        {
                            var localResponseText = await localResponse.Content.ReadAsStringAsync();
                            var localResult = JsonSerializer.Deserialize<LocalGenieResponse>(
                                localResponseText
                            );
                            jamlResult = localResult?.jaml;
                        }
                    }
                    catch (Exception localEx)
                    {
                        DebugLogger.Log(
                            "GenieWidget",
                            $"Local API not available: {localEx.Message}"
                        );
                    }
                }

                if (string.IsNullOrWhiteSpace(jamlResult))
                {
                    SetStatus("Both cloud and local APIs unavailable", "#EF4444");
                    return;
                }

                // Parse the JAML to get a JSON config for display
                if (
                    Motely.JamlConfigLoader.TryLoadFromJamlString(
                        jamlResult!,
                        out var config,
                        out var parseError
                    )
                    && config != null
                )
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
                var fileName = GeneratedFilterName
                    .Replace(" ", "_")
                    .Replace("/", "-")
                    .Replace("\\", "-")
                    .Replace(":", "-");

                var filePath = System.IO.Path.Combine(AppPaths.FiltersDir, $"{fileName}.jaml");

                int counter = 1;
                while (System.IO.File.Exists(filePath))
                {
                    filePath = System.IO.Path.Combine(
                        AppPaths.FiltersDir,
                        $"{fileName}_{counter}.jaml"
                    );
                    counter++;
                }

                await System.IO.File.WriteAllTextAsync(filePath, GeneratedJson);

                SetStatus($"Saved to {System.IO.Path.GetFileName(filePath)}", "#22C55E");
            }
            catch (Exception ex)
            {
                DebugLogger.LogError("GenieWidget", $"Save failed: {ex.Message}");
                SetStatus($"Save failed: {ex.Message}", "#EF4444");
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
                    BatchSize = 2, // 35^2 = 1,225 seeds per batch for better API responsiveness
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

        private class LocalGenieResponse
        {
            public string? jaml { get; set; }
            public string? error { get; set; }
        }

        private class CloudGenieResponse
        {
            public bool success { get; set; }
            public string? jaml { get; set; }
            public object? config { get; set; }
            public string? error { get; set; }
        }
    }
}

using System;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Avalonia.Media;
using BalatroSeedOracle.Helpers;
using BalatroSeedOracle.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Motely.Filters;

namespace BalatroSeedOracle.ViewModels.FilterTabs
{
    public partial class JsonEditorTabViewModel : ObservableObject
    {
        private readonly FiltersModalViewModel? _parentViewModel;

        [ObservableProperty]
        private string _jsonContent = "";

        [ObservableProperty]
        private string _validationStatus = "Ready";

        [ObservableProperty]
        private IBrush _validationStatusColor = Brushes.Gray;

        /// <summary>
        /// Returns the current filter name from the parent ViewModel for display in the editor header
        /// </summary>
        public string FilterFileName =>
            !string.IsNullOrWhiteSpace(_parentViewModel?.FilterName)
                ? $"📄 {_parentViewModel.FilterName}.json"
                : "📄 filter.json";

        public JsonEditorTabViewModel(FiltersModalViewModel? parentViewModel = null)
        {
            _parentViewModel = parentViewModel;

            // Set default JSON content
            JsonContent = GetDefaultJsonContent();

            // Listen for filter name changes from parent to update display
            if (_parentViewModel != null)
            {
                _parentViewModel.PropertyChanged += (s, e) =>
                {
                    if (e.PropertyName == nameof(FiltersModalViewModel.FilterName))
                    {
                        OnPropertyChanged(nameof(FilterFileName));
                    }
                    else if (
                        e.PropertyName == nameof(FiltersModalViewModel.SelectedDeckIndex)
                        || e.PropertyName == nameof(FiltersModalViewModel.SelectedStakeIndex)
                        || e.PropertyName == nameof(FiltersModalViewModel.SelectedDeck)
                    )
                    {
                        AutoGenerateFromVisual();
                    }
                };
            }
        }

        #region Command Implementations

        /// <summary>
        /// Auto-generates JSON from Visual Builder without showing status messages (silent mode).
        /// Called automatically when Visual Builder items change.
        /// </summary>
        public void AutoGenerateFromVisual()
        {
            try
            {
                if (_parentViewModel?.VisualBuilderTab == null)
                    return;

                var visualTab = _parentViewModel.VisualBuilderTab as VisualBuilderTabViewModel;
                if (visualTab == null)
                    return;

                var config = new MotelyJsonConfig
                {
                    Name = "Generated Filter",
                    Description = "Auto-generated from visual builder",
                    Author = "pifreak",
                    DateCreated = DateTime.UtcNow,
                    Deck = GetDeckName(_parentViewModel.SelectedDeckIndex),
                    Stake = GetStakeName(_parentViewModel.SelectedStakeIndex),
                    Must =
                        new System.Collections.Generic.List<MotelyJsonConfig.MotleyJsonFilterClause>(),
                    Should =
                        new System.Collections.Generic.List<MotelyJsonConfig.MotleyJsonFilterClause>(),
                    MustNot =
                        new System.Collections.Generic.List<MotelyJsonConfig.MotleyJsonFilterClause>(),
                };

                // Generate Must clauses from visual builder
                foreach (var item in visualTab.SelectedMust)
                {
                    config.Must.Add(
                        new MotelyJsonConfig.MotleyJsonFilterClause
                        {
                            Type = item.Type,
                            Value = item.Name,
                        }
                    );
                }

                // Generate Should clauses from visual builder
                foreach (var item in visualTab.SelectedShould)
                {
                    config.Should.Add(
                        new MotelyJsonConfig.MotleyJsonFilterClause
                        {
                            Type = item.Type,
                            Value = item.Name,
                        }
                    );
                }

                // MUST-NOT is now handled via IsInvertedFilter flag on items in MUST array
                // No separate MustNot collection needed

                // Update JSON content silently using FilterSerializationService for proper formatting
                var serializationService = ServiceHelper.GetService<FilterSerializationService>();
                JsonContent =
                    serializationService?.SerializeConfig(config)
                    ?? JsonSerializer.Serialize(
                        config,
                        new JsonSerializerOptions
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

                // Silent status update (no user-visible message)
                var totalItems = config.Must.Count + config.Should.Count;
                ValidationStatus = totalItems > 0 ? $"Auto-synced ({totalItems} items)" : "Ready";
                ValidationStatusColor = Brushes.Gray;

                DebugLogger.Log(
                    "JsonEditorTab",
                    $"Auto-synced JSON from visual builder: {config.Must.Count} must, {config.Should.Count} should"
                );
            }
            catch (Exception ex)
            {
                DebugLogger.LogError("JsonEditorTab", $"Error auto-generating JSON: {ex.Message}");
            }
        }

        [RelayCommand]
        private void GenerateFromVisual()
        {
            try
            {
                if (_parentViewModel?.VisualBuilderTab == null)
                {
                    ValidationStatus = "Visual builder not available";
                    ValidationStatusColor = Brushes.Red;
                    return;
                }

                var visualTab = _parentViewModel.VisualBuilderTab as VisualBuilderTabViewModel;
                if (visualTab == null)
                {
                    ValidationStatus = "Visual builder not initialized";
                    ValidationStatusColor = Brushes.Red;
                    return;
                }

                var config = new MotelyJsonConfig
                {
                    Name = "Generated Filter",
                    Description = "Generated from visual builder",
                    Author = "pifreak",
                    DateCreated = DateTime.UtcNow,
                    Deck = GetDeckName(_parentViewModel.SelectedDeckIndex),
                    Stake = GetStakeName(_parentViewModel.SelectedStakeIndex),
                    Must =
                        new System.Collections.Generic.List<MotelyJsonConfig.MotleyJsonFilterClause>(),
                    Should =
                        new System.Collections.Generic.List<MotelyJsonConfig.MotleyJsonFilterClause>(),
                    MustNot =
                        new System.Collections.Generic.List<MotelyJsonConfig.MotleyJsonFilterClause>(),
                };

                // Generate Must clauses from visual builder
                foreach (var item in visualTab.SelectedMust)
                {
                    config.Must.Add(
                        new MotelyJsonConfig.MotleyJsonFilterClause
                        {
                            Type = item.Type,
                            Value = item.Name,
                        }
                    );
                }

                // Generate Should clauses from visual builder
                foreach (var item in visualTab.SelectedShould)
                {
                    config.Should.Add(
                        new MotelyJsonConfig.MotleyJsonFilterClause
                        {
                            Type = item.Type,
                            Value = item.Name,
                        }
                    );
                }

                // MUST-NOT is now handled via IsInvertedFilter flag on items in MUST array
                // No separate MustNot collection needed

                JsonContent = JsonSerializer.Serialize(
                    config,
                    new JsonSerializerOptions
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

                ValidationStatus =
                    $"✓ Generated from visual ({config.Must.Count + config.Should.Count} items)";
                ValidationStatusColor = Brushes.Green;

                DebugLogger.Log(
                    "JsonEditorTab",
                    $"Generated JSON from visual builder with {config.Must.Count} must, {config.Should.Count} should items"
                );
            }
            catch (Exception ex)
            {
                ValidationStatus = $"Error generating JSON: {ex.Message}";
                ValidationStatusColor = Brushes.Red;
                DebugLogger.LogError("JsonEditorTab", $"Error generating JSON: {ex.Message}");
            }
        }

        [RelayCommand]
        private void ApplyToVisual()
        {
            try
            {
                // Validate JSON first
                if (!ValidateJsonSyntax())
                {
                    ValidationStatus = "Invalid JSON - cannot apply to visual";
                    ValidationStatusColor = Brushes.Red;
                    return;
                }

                if (_parentViewModel?.VisualBuilderTab == null)
                {
                    ValidationStatus = "Visual builder not available";
                    ValidationStatusColor = Brushes.Red;
                    return;
                }

                var visualTab = _parentViewModel.VisualBuilderTab as VisualBuilderTabViewModel;
                if (visualTab == null)
                {
                    ValidationStatus = "Visual builder not initialized";
                    ValidationStatusColor = Brushes.Red;
                    return;
                }

                // Parse the JSON
                var config = JsonSerializer.Deserialize<MotelyJsonConfig>(
                    JsonContent,
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true }
                );

                if (config == null)
                {
                    ValidationStatus = "Failed to parse JSON";
                    ValidationStatusColor = Brushes.Red;
                    return;
                }

                // Clear existing selections in visual builder
                visualTab.SelectedMust.Clear();
                visualTab.SelectedShould.Clear();

                int itemsAdded = 0;

                // Apply Must items
                if (config.Must != null)
                {
                    foreach (var clause in config.Must)
                    {
                        var item = FindOrCreateFilterItem(visualTab, clause.Type, clause.Value);
                        if (item != null && !visualTab.SelectedMust.Any(x => x.Name == item.Name))
                        {
                            visualTab.SelectedMust.Add(item);
                            itemsAdded++;
                        }
                    }
                }

                // Apply Should items
                if (config.Should != null)
                {
                    foreach (var clause in config.Should)
                    {
                        var item = FindOrCreateFilterItem(visualTab, clause.Type, clause.Value);
                        if (item != null && !visualTab.SelectedShould.Any(x => x.Name == item.Name))
                        {
                            visualTab.SelectedShould.Add(item);
                            itemsAdded++;
                        }
                    }
                }

                // MUST-NOT functionality removed - items with IsInvertedFilter=true in Must collection are treated as MUST-NOT
                // No separate MustNot collection exists anymore

                ValidationStatus = $"✓ Applied to visual ({itemsAdded} items)";
                ValidationStatusColor = Brushes.Green;

                DebugLogger.Log(
                    "JsonEditorTab",
                    $"Applied JSON to visual builder: {itemsAdded} items"
                );
            }
            catch (Exception ex)
            {
                ValidationStatus = $"Error applying JSON: {ex.Message}";
                ValidationStatusColor = Brushes.Red;
                DebugLogger.LogError("JsonEditorTab", $"Error applying JSON: {ex.Message}");
            }
        }

        [RelayCommand]
        private void ValidateJson()
        {
            if (ValidateJsonSyntax())
            {
                ValidationStatus = "✓ Valid JSON";
                ValidationStatusColor = Brushes.Green;
            }
            else
            {
                ValidationStatus = "✗ Invalid JSON syntax";
                ValidationStatusColor = Brushes.Red;
            }
        }

        [RelayCommand]
        private void FormatJson()
        {
            try
            {
                if (ValidateJsonSyntax())
                {
                    // Use custom compact formatter that keeps arrays horizontal
                    JsonContent = CompactJsonFormatter.Format(JsonContent, maxArrayWidth: 120);

                    ValidationStatus = "✓ JSON formatted";
                    ValidationStatusColor = Brushes.Green;
                }
                else
                {
                    ValidationStatus = "Cannot format invalid JSON";
                    ValidationStatusColor = Brushes.Red;
                }
            }
            catch (Exception ex)
            {
                ValidationStatus = $"Format error: {ex.Message}";
                ValidationStatusColor = Brushes.Red;
            }
        }

        [RelayCommand]
        private async Task CopyJson()
        {
            try
            {
                await Services.ClipboardService.CopyToClipboardAsync(JsonContent);

                ValidationStatus = "✓ JSON copied to clipboard";
                ValidationStatusColor = Brushes.Green;
            }
            catch (Exception ex)
            {
                ValidationStatus = $"Copy error: {ex.Message}";
                ValidationStatusColor = Brushes.Red;
            }
        }

        #endregion

        #region Helper Methods - Copied from original FiltersModal

        private bool ValidateJsonSyntax()
        {
            try
            {
                if (string.IsNullOrWhiteSpace(JsonContent))
                    return false;

                JsonDocument.Parse(JsonContent);
                return true;
            }
            catch
            {
                return false;
            }
        }

        private string GetDefaultJsonContent()
        {
            // Default JSON template - compact formatting
            return @"{
  ""name"": ""New Filter"",
  ""description"": ""Created with visual filter builder"",
  ""author"": ""pifreak"",
  ""dateCreated"": """
                + DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ss.fffffffZ")
                + @""",
  ""deck"": ""Red"",
  ""stake"": ""White"",
  ""must"": [],
  ""should"": [],
  ""mustNot"": []
}";
        }

        private Models.FilterItem? FindOrCreateFilterItem(
            VisualBuilderTabViewModel visualTab,
            string? type,
            string? name
        )
        {
            if (string.IsNullOrEmpty(name))
                return null;

            // Search in all collections based on type
            Models.FilterItem? item = null;

            switch (type?.ToLower())
            {
                case "joker":
                    item = visualTab.AllJokers.FirstOrDefault(j => j.Name == name);
                    break;
                case "tag":
                case "smallblindtag":
                    item = visualTab.AllTags.FirstOrDefault(t => t.Name == name);
                    break;
                case "voucher":
                    item = visualTab.AllVouchers.FirstOrDefault(v => v.Name == name);
                    break;
                case "tarot":
                    item = visualTab.AllTarots.FirstOrDefault(t => t.Name == name);
                    break;
                case "planet":
                    item = visualTab.AllPlanets.FirstOrDefault(p => p.Name == name);
                    break;
                case "spectral":
                    item = visualTab.AllSpectrals.FirstOrDefault(s => s.Name == name);
                    break;
            }

            // If not found, create a new item (this handles custom items)
            if (item == null)
            {
                var spriteService = SpriteService.Instance;
                item = new Models.FilterItem
                {
                    Name = name,
                    Type = type ?? "Joker",
                    DisplayName = Motely.FormatUtils.FormatDisplayName(name),
                    ItemImage = type?.ToLower() switch
                    {
                        "joker" => spriteService.GetJokerImage(name),
                        "tag" or "smallblindtag" => spriteService.GetTagImage(name),
                        "voucher" => spriteService.GetVoucherImage(name),
                        "tarot" => spriteService.GetTarotImage(name),
                        "planet" => spriteService.GetPlanetCardImage(name),
                        "spectral" => spriteService.GetSpectralImage(name),
                        _ => null,
                    },
                };

                // Add to the appropriate collection
                switch (type?.ToLower())
                {
                    case "joker":
                        visualTab.AllJokers.Add(item);
                        if (
                            string.IsNullOrEmpty(visualTab.SearchFilter)
                            || item.Name.ToLowerInvariant()
                                .Contains(visualTab.SearchFilter.ToLowerInvariant())
                        )
                            visualTab.FilteredJokers.Add(item);
                        break;
                    case "tag":
                    case "smallblindtag":
                        visualTab.AllTags.Add(item);
                        if (
                            string.IsNullOrEmpty(visualTab.SearchFilter)
                            || item.Name.ToLowerInvariant()
                                .Contains(visualTab.SearchFilter.ToLowerInvariant())
                        )
                            visualTab.FilteredTags.Add(item);
                        break;
                    case "voucher":
                        visualTab.AllVouchers.Add(item);
                        if (
                            string.IsNullOrEmpty(visualTab.SearchFilter)
                            || item.Name.ToLowerInvariant()
                                .Contains(visualTab.SearchFilter.ToLowerInvariant())
                        )
                            visualTab.FilteredVouchers.Add(item);
                        break;
                    case "tarot":
                        visualTab.AllTarots.Add(item);
                        if (
                            string.IsNullOrEmpty(visualTab.SearchFilter)
                            || item.Name.ToLowerInvariant()
                                .Contains(visualTab.SearchFilter.ToLowerInvariant())
                        )
                            visualTab.FilteredTarots.Add(item);
                        break;
                    case "planet":
                        visualTab.AllPlanets.Add(item);
                        if (
                            string.IsNullOrEmpty(visualTab.SearchFilter)
                            || item.Name.ToLowerInvariant()
                                .Contains(visualTab.SearchFilter.ToLowerInvariant())
                        )
                            visualTab.FilteredPlanets.Add(item);
                        break;
                    case "spectral":
                        visualTab.AllSpectrals.Add(item);
                        if (
                            string.IsNullOrEmpty(visualTab.SearchFilter)
                            || item.Name.ToLowerInvariant()
                                .Contains(visualTab.SearchFilter.ToLowerInvariant())
                        )
                            visualTab.FilteredSpectrals.Add(item);
                        break;
                }
            }

            return item;
        }

        // Convert index to deck name via enum
        private string GetDeckName(int index)
        {
            if (index >= 0 && index <= 14)
                return ((Motely.MotelyDeck)index).ToString();
            return "Red";
        }

        // Convert index to stake name via enum (handles gaps in enum values)
        private string GetStakeName(int index)
        {
            var stake = index switch
            {
                0 => Motely.MotelyStake.White,
                1 => Motely.MotelyStake.Red,
                2 => Motely.MotelyStake.Green,
                3 => Motely.MotelyStake.Black,
                4 => Motely.MotelyStake.Blue,
                5 => Motely.MotelyStake.Purple,
                6 => Motely.MotelyStake.Orange,
                7 => Motely.MotelyStake.Gold,
                _ => Motely.MotelyStake.White,
            };
            return stake.ToString().ToLower();
        }

        #endregion
    }
}

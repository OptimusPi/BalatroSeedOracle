using System;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows.Input;
using CommunityToolkit.Mvvm.Input;
using Avalonia.Media;
using BalatroSeedOracle.Helpers;
using BalatroSeedOracle.Services;
using Motely.Filters;

namespace BalatroSeedOracle.ViewModels.FilterTabs
{
    public class JsonEditorTabViewModel : BaseViewModel
    {
        private readonly FiltersModalViewModel? _parentViewModel;
        private string _jsonContent = "";
        private string _validationStatus = "Ready";
        private IBrush _validationStatusColor = Brushes.Gray;

        public JsonEditorTabViewModel(FiltersModalViewModel? parentViewModel = null)
        {
            _parentViewModel = parentViewModel;
            
            // Initialize commands
            GenerateFromVisualCommand = new RelayCommand(GenerateFromVisual);
            ApplyToVisualCommand = new RelayCommand(ApplyToVisual);
            ValidateJsonCommand = new RelayCommand(ValidateJson);
            FormatJsonCommand = new RelayCommand(FormatJson);

            // Set default JSON content
            JsonContent = GetDefaultJsonContent();
        }

        #region Properties

        public string JsonContent
        {
            get => _jsonContent;
            set => SetProperty(ref _jsonContent, value);
        }

        public string ValidationStatus
        {
            get => _validationStatus;
            set => SetProperty(ref _validationStatus, value);
        }

        public IBrush ValidationStatusColor
        {
            get => _validationStatusColor;
            set => SetProperty(ref _validationStatusColor, value);
        }

        #endregion

        #region Commands

        public ICommand GenerateFromVisualCommand { get; }
        public ICommand ApplyToVisualCommand { get; }
        public ICommand ValidateJsonCommand { get; }
        public ICommand FormatJsonCommand { get; }

        #endregion

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
                    Must = new System.Collections.Generic.List<MotelyJsonConfig.MotleyJsonFilterClause>(),
                    Should = new System.Collections.Generic.List<MotelyJsonConfig.MotleyJsonFilterClause>(),
                    MustNot = new System.Collections.Generic.List<MotelyJsonConfig.MotleyJsonFilterClause>()
                };

                // Generate Must clauses from visual builder
                foreach (var item in visualTab.SelectedMust)
                {
                    config.Must.Add(new MotelyJsonConfig.MotleyJsonFilterClause
                    {
                        Type = item.Type,
                        Value = item.Name
                    });
                }

                // Generate Should clauses from visual builder
                foreach (var item in visualTab.SelectedShould)
                {
                    config.Should.Add(new MotelyJsonConfig.MotleyJsonFilterClause
                    {
                        Type = item.Type,
                        Value = item.Name
                    });
                }

                // Generate MustNot clauses from visual builder
                foreach (var item in visualTab.SelectedMustNot)
                {
                    config.MustNot.Add(new MotelyJsonConfig.MotleyJsonFilterClause
                    {
                        Type = item.Type,
                        Value = item.Name
                    });
                }

                // Update JSON content silently
                JsonContent = JsonSerializer.Serialize(config, new JsonSerializerOptions
                {
                    WriteIndented = true,
                    DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
                });

                // Silent status update (no user-visible message)
                var totalItems = config.Must.Count + config.Should.Count + config.MustNot.Count;
                ValidationStatus = totalItems > 0 ? $"Auto-synced ({totalItems} items)" : "Ready";
                ValidationStatusColor = Brushes.Gray;

                DebugLogger.Log("JsonEditorTab", $"Auto-synced JSON from visual builder: {config.Must.Count} must, {config.Should.Count} should, {config.MustNot.Count} must not");
            }
            catch (Exception ex)
            {
                DebugLogger.LogError("JsonEditorTab", $"Error auto-generating JSON: {ex.Message}");
            }
        }

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
                    Must = new System.Collections.Generic.List<MotelyJsonConfig.MotleyJsonFilterClause>(),
                    Should = new System.Collections.Generic.List<MotelyJsonConfig.MotleyJsonFilterClause>(),
                    MustNot = new System.Collections.Generic.List<MotelyJsonConfig.MotleyJsonFilterClause>()
                };

                // Generate Must clauses from visual builder
                foreach (var item in visualTab.SelectedMust)
                {
                    config.Must.Add(new MotelyJsonConfig.MotleyJsonFilterClause
                    {
                        Type = item.Type,
                        Value = item.Name
                    });
                }

                // Generate Should clauses from visual builder
                foreach (var item in visualTab.SelectedShould)
                {
                    config.Should.Add(new MotelyJsonConfig.MotleyJsonFilterClause
                    {
                        Type = item.Type,
                        Value = item.Name
                    });
                }

                // Generate MustNot clauses from visual builder
                foreach (var item in visualTab.SelectedMustNot)
                {
                    config.MustNot.Add(new MotelyJsonConfig.MotleyJsonFilterClause
                    {
                        Type = item.Type,
                        Value = item.Name
                    });
                }

                JsonContent = JsonSerializer.Serialize(config, new JsonSerializerOptions
                {
                    WriteIndented = true,
                    DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
                });

                ValidationStatus = $"✓ Generated from visual ({config.Must.Count + config.Should.Count + config.MustNot.Count} items)";
                ValidationStatusColor = Brushes.Green;

                DebugLogger.Log("JsonEditorTab", $"Generated JSON from visual builder with {config.Must.Count} must, {config.Should.Count} should, {config.MustNot.Count} must not items");
            }
            catch (Exception ex)
            {
                ValidationStatus = $"Error generating JSON: {ex.Message}";
                ValidationStatusColor = Brushes.Red;
                DebugLogger.LogError("JsonEditorTab", $"Error generating JSON: {ex.Message}");
            }
        }

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
                var config = JsonSerializer.Deserialize<MotelyJsonConfig>(JsonContent, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

                if (config == null)
                {
                    ValidationStatus = "Failed to parse JSON";
                    ValidationStatusColor = Brushes.Red;
                    return;
                }

                // Clear existing selections in visual builder
                visualTab.SelectedMust.Clear();
                visualTab.SelectedShould.Clear();
                visualTab.SelectedMustNot.Clear();

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

                // Apply MustNot items
                if (config.MustNot != null)
                {
                    foreach (var clause in config.MustNot)
                    {
                        var item = FindOrCreateFilterItem(visualTab, clause.Type, clause.Value);
                        if (item != null && !visualTab.SelectedMustNot.Any(x => x.Name == item.Name))
                        {
                            visualTab.SelectedMustNot.Add(item);
                            itemsAdded++;
                        }
                    }
                }

                ValidationStatus = $"✓ Applied to visual ({itemsAdded} items)";
                ValidationStatusColor = Brushes.Green;
                
                DebugLogger.Log("JsonEditorTab", $"Applied JSON to visual builder: {itemsAdded} items");
            }
            catch (Exception ex)
            {
                ValidationStatus = $"Error applying JSON: {ex.Message}";
                ValidationStatusColor = Brushes.Red;
                DebugLogger.LogError("JsonEditorTab", $"Error applying JSON: {ex.Message}");
            }
        }

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

        private void FormatJson()
        {
            try
            {
                if (ValidateJsonSyntax())
                {
                    var parsed = JsonSerializer.Deserialize<object>(JsonContent);
                    JsonContent = JsonSerializer.Serialize(parsed, new JsonSerializerOptions
                    {
                        WriteIndented = true
                    });
                    
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
            // Default JSON template - copied from original FiltersModal logic
            return @"{
  ""name"": ""New Filter"",
  ""description"": ""Created with visual filter builder"",
  ""author"": ""pifreak"",
  ""dateCreated"": """ + DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ss.fffffffZ") + @""",
  ""deck"": ""Red"",
  ""stake"": ""White"",
  ""must"": [
  ],
  ""should"": [
  ],
  ""mustNot"": [
  ]
}";
        }

        private Models.FilterItem? FindOrCreateFilterItem(VisualBuilderTabViewModel visualTab, string? type, string? name)
        {
            if (string.IsNullOrEmpty(name)) return null;

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
                    DisplayName = FormatDisplayName(name),
                    ItemImage = type?.ToLower() switch
                    {
                        "joker" => spriteService.GetJokerImage(name),
                        "tag" or "smallblindtag" => spriteService.GetTagImage(name),
                        "voucher" => spriteService.GetVoucherImage(name),
                        "tarot" => spriteService.GetTarotImage(name),
                        "planet" => spriteService.GetPlanetCardImage(name),
                        "spectral" => spriteService.GetSpectralImage(name),
                        _ => null
                    }
                };

                // Add to the appropriate collection
                switch (type?.ToLower())
                {
                    case "joker":
                        visualTab.AllJokers.Add(item);
                        if (string.IsNullOrEmpty(visualTab.SearchFilter) || item.Name.ToLowerInvariant().Contains(visualTab.SearchFilter.ToLowerInvariant()))
                            visualTab.FilteredJokers.Add(item);
                        break;
                    case "tag":
                    case "smallblindtag":
                        visualTab.AllTags.Add(item);
                        if (string.IsNullOrEmpty(visualTab.SearchFilter) || item.Name.ToLowerInvariant().Contains(visualTab.SearchFilter.ToLowerInvariant()))
                            visualTab.FilteredTags.Add(item);
                        break;
                    case "voucher":
                        visualTab.AllVouchers.Add(item);
                        if (string.IsNullOrEmpty(visualTab.SearchFilter) || item.Name.ToLowerInvariant().Contains(visualTab.SearchFilter.ToLowerInvariant()))
                            visualTab.FilteredVouchers.Add(item);
                        break;
                    case "tarot":
                        visualTab.AllTarots.Add(item);
                        if (string.IsNullOrEmpty(visualTab.SearchFilter) || item.Name.ToLowerInvariant().Contains(visualTab.SearchFilter.ToLowerInvariant()))
                            visualTab.FilteredTarots.Add(item);
                        break;
                    case "planet":
                        visualTab.AllPlanets.Add(item);
                        if (string.IsNullOrEmpty(visualTab.SearchFilter) || item.Name.ToLowerInvariant().Contains(visualTab.SearchFilter.ToLowerInvariant()))
                            visualTab.FilteredPlanets.Add(item);
                        break;
                    case "spectral":
                        visualTab.AllSpectrals.Add(item);
                        if (string.IsNullOrEmpty(visualTab.SearchFilter) || item.Name.ToLowerInvariant().Contains(visualTab.SearchFilter.ToLowerInvariant()))
                            visualTab.FilteredSpectrals.Add(item);
                        break;
                }
            }

            return item;
        }
        
        private string FormatDisplayName(string name)
        {
            if (string.IsNullOrEmpty(name))
                return name;
                
            var words = name.Replace('_', ' ').Split(' ');
            for (int i = 0; i < words.Length; i++)
            {
                if (words[i].Length > 0)
                {
                    words[i] = char.ToUpper(words[i][0]) + words[i].Substring(1).ToLower();
                }
            }
            return string.Join(" ", words);
        }
        
        private string GetDeckName(int index)
        {
            var deckNames = new[] { "Red", "Blue", "Yellow", "Green", "Black", "Magic", "Nebula", "Ghost", 
                                   "Abandoned", "Checkered", "Zodiac", "Painted", "Anaglyph", "Plasma", 
                                   "Erratic", "Challenge" };
            return index >= 0 && index < deckNames.Length ? deckNames[index] : "Red";
        }
        
        private string GetStakeName(int index)
        {
            var stakeNames = new[] { "white", "red", "green", "black", "blue", "purple", "orange", "gold" };
            return index >= 0 && index < stakeNames.Length ? stakeNames[index] : "white";
        }

        #endregion
    }
}
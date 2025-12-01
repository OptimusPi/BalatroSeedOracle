using System;
using System.Text.Json;
using System.Threading.Tasks;
using Avalonia.Media;
using BalatroSeedOracle.Helpers;
using BalatroSeedOracle.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Motely.Filters;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace BalatroSeedOracle.ViewModels.FilterTabs
{
    /// <summary>
    /// ViewModel for JAML Editor Tab
    /// JAML (Joker Ante Markup Language) is a YAML-based format for Balatro filters
    /// </summary>
    public partial class JamlEditorTabViewModel : ObservableObject
    {
        private readonly FiltersModalViewModel? _parentViewModel;

        [ObservableProperty]
        private string _jamlContent = "";

        [ObservableProperty]
        private string _validationStatus = "Ready";

        [ObservableProperty]
        private IBrush _validationStatusColor = Brushes.Gray;

        /// <summary>
        /// Returns the current filter name from the parent ViewModel for display in the editor header
        /// </summary>
        public string FilterFileName =>
            !string.IsNullOrWhiteSpace(_parentViewModel?.FilterName)
                ? $"📄 {_parentViewModel.FilterName}.jaml"
                : "📄 filter.jaml";

        public JamlEditorTabViewModel(FiltersModalViewModel? parentViewModel = null)
        {
            _parentViewModel = parentViewModel;

            // Set default JAML content
            JamlContent = GetDefaultJamlContent();

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
        /// Auto-generates JAML from Visual Builder (called when visual builder changes)
        /// </summary>
        public void AutoGenerateFromVisual()
        {
            try
            {
                if (_parentViewModel == null)
                    return;

                // Use the parent's BuildConfigFromCurrentState method - it handles everything properly!
                var config = _parentViewModel.BuildConfigFromCurrentState();

                // Convert to JAML (uses YAML serialization since JAML is YAML-based)
                JamlContent = ConvertConfigToJaml(config);

                // Silent status update
                var totalItems = config.Must.Count + config.Should.Count;
                ValidationStatus = totalItems > 0 ? $"Auto-synced ({totalItems} items)" : "Ready";
                ValidationStatusColor = Brushes.Gray;

                DebugLogger.Log(
                    "JamlEditorTab",
                    $"Auto-synced JAML from visual builder: {config.Must.Count} must, {config.Should.Count} should"
                );
            }
            catch (Exception ex)
            {
                ValidationStatus = $"Error generating JAML: {ex.Message}";
                ValidationStatusColor = Brushes.Red;
                DebugLogger.LogError("JamlEditorTab", $"Error generating JAML: {ex.Message}");
            }
        }

        [RelayCommand]
        private void ValidateJaml()
        {
            if (ValidateJamlSyntax())
            {
                ValidationStatus = "✓ JAML is valid";
                ValidationStatusColor = Brushes.Green;
            }
            else
            {
                ValidationStatus = "✗ Invalid JAML syntax";
                ValidationStatusColor = Brushes.Red;
            }
        }

        [RelayCommand]
        private void FormatJaml()
        {
            try
            {
                if (ValidateJamlSyntax())
                {
                    // Parse and re-serialize to format
                    var deserializer = new DeserializerBuilder()
                        .WithNamingConvention(CamelCaseNamingConvention.Instance)
                        .Build();

                    var jamlObject = deserializer.Deserialize(JamlContent);

                    var serializer = new SerializerBuilder()
                        .WithNamingConvention(CamelCaseNamingConvention.Instance)
                        .Build();

                    JamlContent = serializer.Serialize(jamlObject);

                    ValidationStatus = "✓ JAML formatted";
                    ValidationStatusColor = Brushes.Green;
                }
                else
                {
                    ValidationStatus = "Cannot format invalid JAML";
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
        private async Task CopyJaml()
        {
            try
            {
                await Services.ClipboardService.CopyToClipboardAsync(JamlContent);

                ValidationStatus = "✓ JAML copied to clipboard";
                ValidationStatusColor = Brushes.Green;
            }
            catch (Exception ex)
            {
                ValidationStatus = $"Copy error: {ex.Message}";
                ValidationStatusColor = Brushes.Red;
            }
        }

        #endregion

        #region Helper Methods

        private bool ValidateJamlSyntax()
        {
            try
            {
                if (string.IsNullOrWhiteSpace(JamlContent))
                    return false;

                var deserializer = new DeserializerBuilder()
                    .WithNamingConvention(CamelCaseNamingConvention.Instance)
                    .Build();

                deserializer.Deserialize(JamlContent);
                return true;
            }
            catch
            {
                return false;
            }
        }

        private string ConvertConfigToJaml(MotelyJsonConfig config)
        {
            try
            {
                // Build clean JAML output that matches the expected format (no null garbage)
                var sb = new System.Text.StringBuilder();

                // Header
                sb.AppendLine($"name: {config.Name ?? "New Filter"}");
                if (!string.IsNullOrWhiteSpace(config.Description))
                    sb.AppendLine($"description: {config.Description}");
                sb.AppendLine($"author: {config.Author ?? "pifreak"}");
                sb.AppendLine($"dateCreated: {config.DateCreated:yyyy-MM-ddTHH:mm:ss.fffffffZ}");

                // Deck/Stake (only if set)
                if (!string.IsNullOrWhiteSpace(config.Deck))
                    sb.AppendLine($"deck: {config.Deck}");
                if (!string.IsNullOrWhiteSpace(config.Stake))
                    sb.AppendLine($"stake: {config.Stake}");

                // Must clauses
                sb.AppendLine();
                if (config.Must?.Count > 0)
                {
                    sb.AppendLine("must:");
                    foreach (var clause in config.Must)
                    {
                        sb.AppendLine($"  - type: {clause.Type}");
                        sb.AppendLine($"    value: {clause.Value}");
                        if (clause.Antes is { Length: > 0 })
                            sb.AppendLine($"    antes: [{string.Join(",", clause.Antes)}]");
                    }
                }
                else
                {
                    sb.AppendLine("must: []");
                }

                // Should clauses
                sb.AppendLine();
                if (config.Should?.Count > 0)
                {
                    sb.AppendLine("should:");
                    foreach (var clause in config.Should)
                    {
                        sb.AppendLine($"  - type: {clause.Type}");
                        sb.AppendLine($"    value: {clause.Value}");
                        if (clause.Antes is { Length: > 0 })
                            sb.AppendLine($"    antes: [{string.Join(",", clause.Antes)}]");
                    }
                }
                else
                {
                    sb.AppendLine("should: []");
                }

                // MustNot - only show if has items
                sb.AppendLine();
                if (config.MustNot?.Count > 0)
                {
                    sb.AppendLine("mustNot:");
                    foreach (var clause in config.MustNot)
                    {
                        sb.AppendLine($"  - type: {clause.Type}");
                        sb.AppendLine($"    value: {clause.Value}");
                        if (clause.Antes is { Length: > 0 })
                            sb.AppendLine($"    antes: [{string.Join(",", clause.Antes)}]");
                    }
                }
                else
                {
                    sb.AppendLine("mustNot: []");
                }

                return sb.ToString();
            }
            catch (Exception ex)
            {
                DebugLogger.LogError(
                    "JamlEditorTab",
                    $"Failed to convert config to JAML: {ex.Message}"
                );
                return GetDefaultJamlContent();
            }
        }

        private string GetDefaultJamlContent()
        {
            return @"# JAML - Joker Ante Markup Language
# A YAML-based format for Balatro seed filters
name: New Filter
description: Created with visual filter builder
author: pifreak
dateCreated: "
                + DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ss.fffffffZ")
                + @"
deck: Red
stake: White
must: []
should: []
mustNot: []
";
        }

        private string? GetDeckName(int index)
        {
            var deckNames = new[]
            {
                "Red",
                "Blue",
                "Yellow",
                "Green",
                "Black",
                "Magic",
                "Nebula",
                "Ghost",
                "Abandoned",
                "Checkered",
                "Zodiac",
                "Painted",
                "Anaglyph",
                "Plasma",
                "Erratic",
                "Challenge",
            };
            return index >= 0 && index < deckNames.Length ? deckNames[index] : null;
        }

        private string? GetStakeName(int index)
        {
            var stakeNames = new[]
            {
                "White",
                "Red",
                "Green",
                "Black",
                "Blue",
                "Purple",
                "Orange",
                "Gold",
            };
            return index >= 0 && index < stakeNames.Length ? stakeNames[index] : null;
        }

        #endregion
    }
}

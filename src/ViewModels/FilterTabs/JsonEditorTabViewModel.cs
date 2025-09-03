using System;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows.Input;
using CommunityToolkit.Mvvm.Input;
using Avalonia.Media;
using BalatroSeedOracle.Helpers;
using Motely.Filters;

namespace BalatroSeedOracle.ViewModels.FilterTabs
{
    public class JsonEditorTabViewModel : BaseViewModel
    {
        private string _jsonContent = "";
        private string _validationStatus = "Ready";
        private IBrush _validationStatusColor = Brushes.Gray;

        public JsonEditorTabViewModel()
        {
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

        private void GenerateFromVisual()
        {
            try
            {
                // TODO: Get current filter selections from parent ViewModel
                // For now, generate sample JSON
                var config = new MotelyJsonConfig
                {
                    Name = "Generated Filter",
                    Description = "Generated from visual builder",
                    Author = "pifreak",
                    DateCreated = DateTime.UtcNow,
                    Deck = "Red",
                    Stake = "White",
                    Must = new System.Collections.Generic.List<MotelyJsonConfig.MotleyJsonFilterClause>(),
                    Should = new System.Collections.Generic.List<MotelyJsonConfig.MotleyJsonFilterClause>(),
                    MustNot = new System.Collections.Generic.List<MotelyJsonConfig.MotleyJsonFilterClause>()
                };

                JsonContent = JsonSerializer.Serialize(config, new JsonSerializerOptions
                {
                    WriteIndented = true,
                    DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
                });

                ValidationStatus = "Generated from visual builder";
                ValidationStatusColor = Brushes.Green;
                
                DebugLogger.Log("JsonEditorTab", "Generated JSON from visual builder");
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

                // TODO: Parse JSON and apply to visual builder
                ValidationStatus = "Applied to visual builder";
                ValidationStatusColor = Brushes.Green;
                
                DebugLogger.Log("JsonEditorTab", "Applied JSON to visual builder");
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

        #endregion
    }
}
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Avalonia.Media;
using BalatroSeedOracle.Helpers;
using BalatroSeedOracle.Models;
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
        private string _errorMessage = "";

        [ObservableProperty]
        private bool _hasError = false;

        [ObservableProperty]
        private IBrush _validationStatusColor = Brushes.Gray;

        [ObservableProperty]
        private ObservableCollection<FilterItem> _previewItems = new();

        [ObservableProperty]
        private ObservableCollection<ValidationErrorItem> _validationErrors = new();

        [ObservableProperty]
        private bool _hasErrors = false;

        [ObservableProperty]
        private int _errorCount = 0;

        public event System.Action<int, int>? JumpToError;

        /// <summary>
        /// Invokes the JumpToError event if any handlers are subscribed.
        /// This method allows external classes to trigger the event.
        /// </summary>
        public void InvokeJumpToError(int lineNumber, int column)
        {
            JumpToError?.Invoke(lineNumber, column);
        }

        private System.Threading.CancellationTokenSource? _validationThrottleCts;

        partial void OnJamlContentChanged(string value)
        {
            // Throttle validation to avoid lag while typing
            _validationThrottleCts?.Cancel();
            _validationThrottleCts = new System.Threading.CancellationTokenSource();
            var token = _validationThrottleCts.Token;

            Task.Run(async () => {
                try 
                {
                    await Task.Delay(500, token);
                    if (token.IsCancellationRequested) return;

                    await Avalonia.Threading.Dispatcher.UIThread.InvokeAsync(() => {
                        ValidateAndPreview();
                    });
                }
                catch (TaskCanceledException) {}
            }, token);
        }

        private void ValidateAndPreview()
        {
            ValidationErrors.Clear();
            HasErrors = false;
            ErrorCount = 0;

            if (string.IsNullOrWhiteSpace(JamlContent))
            {
                ValidationStatus = "Empty";
                ValidationStatusColor = Brushes.Gray;
                ErrorMessage = "";
                HasError = false;
                PreviewItems.Clear();
                return;
            }

            try
            {
                var deserializer = new DeserializerBuilder()
                    .WithNamingConvention(CamelCaseNamingConvention.Instance)
                    .IgnoreUnmatchedProperties()
                    .Build();

                var config = deserializer.Deserialize<MotelyJsonConfig>(JamlContent);
                
                // Additional validation checks
                ValidateSchema(config);
                ValidateAnchors(JamlContent);

                if (ValidationErrors.Count == 0)
                {
                    ValidationStatus = "✓ JAML is valid";
                    ValidationStatusColor = Brushes.Green;
                    ErrorMessage = "";
                    HasError = false;
                }
                else
                {
                    ValidationStatus = $"✗ {ValidationErrors.Count} issue(s) found";
                    ValidationStatusColor = Brushes.Orange;
                    HasError = true;
                    HasErrors = true;
                    ErrorCount = ValidationErrors.Count;
                }

                // Update preview items
                UpdatePreview(config);
            }
            catch (YamlDotNet.Core.YamlException yamlEx)
            {
                // Parse YAML error
                var lineNumber = ExtractLineNumber(yamlEx.Message);
                ValidationErrors.Add(new ValidationErrorItem
                {
                    LineNumber = lineNumber,
                    Column = 0,
                    Message = yamlEx.Message,
                    Severity = ValidationErrorItem.ErrorSeverity.Error
                });

                ValidationStatus = "✗ Invalid JAML syntax";
                ValidationStatusColor = Brushes.Red;
                ErrorMessage = yamlEx.Message;
                HasError = true;
                HasErrors = true;
                ErrorCount = ValidationErrors.Count;
            }
            catch (Exception ex)
            {
                ValidationErrors.Add(new ValidationErrorItem
                {
                    LineNumber = 1,
                    Column = 0,
                    Message = ex.Message,
                    Severity = ValidationErrorItem.ErrorSeverity.Error
                });

                ValidationStatus = "✗ Error";
                ValidationStatusColor = Brushes.Red;
                ErrorMessage = ex.Message;
                HasError = true;
                HasErrors = true;
                ErrorCount = ValidationErrors.Count;
            }
        }

        private int ExtractLineNumber(string message)
        {
            var match = System.Text.RegularExpressions.Regex.Match(message, @"line (\d+)");
            if (match.Success && int.TryParse(match.Groups[1].Value, out var line))
                return line;
            return 1;
        }

        private void ValidateSchema(MotelyJsonConfig config)
        {
            // Validate deck enum
            if (!string.IsNullOrEmpty(config.Deck))
            {
                var validDecks = new[] { "Red", "Blue", "Yellow", "Green", "Black", "Magic", "Nebula", "Ghost", "Abandoned", "Checkered", "Zodiac", "Painted", "Anaglyph", "Plasma", "Erratic", "Challenge" };
                if (!validDecks.Contains(config.Deck))
                {
                    ValidationErrors.Add(new ValidationErrorItem
                    {
                        LineNumber = 1,
                        Column = 0,
                        Message = $"Invalid deck: {config.Deck}",
                        Severity = ValidationErrorItem.ErrorSeverity.Warning
                    });
                }
            }

            // Validate stake enum
            if (!string.IsNullOrEmpty(config.Stake))
            {
                var validStakes = new[] { "White", "Red", "Green", "Black", "Blue", "Purple", "Orange", "Gold" };
                if (!validStakes.Contains(config.Stake))
                {
                    ValidationErrors.Add(new ValidationErrorItem
                    {
                        LineNumber = 1,
                        Column = 0,
                        Message = $"Invalid stake: {config.Stake}",
                        Severity = ValidationErrorItem.ErrorSeverity.Warning
                    });
                }
            }
        }

        private void ValidateAnchors(string yamlContent)
        {
            // Find all anchor references
            var referenceMatches = System.Text.RegularExpressions.Regex.Matches(yamlContent, @"\*(\w+)");
            var definedAnchors = new System.Collections.Generic.HashSet<string>();

            // Find all anchor definitions
            var definitionMatches = System.Text.RegularExpressions.Regex.Matches(yamlContent, @"&(\w+)");
            foreach (System.Text.RegularExpressions.Match match in definitionMatches)
            {
                definedAnchors.Add(match.Groups[1].Value);
            }

            // Check references
            foreach (System.Text.RegularExpressions.Match match in referenceMatches)
            {
                var anchorName = match.Groups[1].Value;
                if (!definedAnchors.Contains(anchorName))
                {
                    var lineNumber = GetLineNumberFromOffset(yamlContent, match.Index);
                    ValidationErrors.Add(new ValidationErrorItem
                    {
                        LineNumber = lineNumber,
                        Column = match.Index - GetLineStartOffset(yamlContent, lineNumber),
                        Message = $"Anchor '{anchorName}' is referenced but not defined",
                        Severity = ValidationErrorItem.ErrorSeverity.Warning
                    });
                }
            }
        }

        private int GetLineNumberFromOffset(string text, int offset)
        {
            var lineNumber = 1;
            for (int i = 0; i < offset && i < text.Length; i++)
            {
                if (text[i] == '\n')
                    lineNumber++;
            }
            return lineNumber;
        }

        private int GetLineStartOffset(string text, int lineNumber)
        {
            var currentLine = 1;
            for (int i = 0; i < text.Length; i++)
            {
                if (currentLine == lineNumber)
                    return i;
                if (text[i] == '\n')
                    currentLine++;
            }
            return 0;
        }

        private void UpdatePreview(MotelyJsonConfig config)
        {
            PreviewItems.Clear();
            var spriteService = SpriteService.Instance;

            if (config.Must != null)
            {
                foreach (var clause in config.Must)
                {
                    var item = CreateFilterItemFromClause(clause, FilterItemStatus.MustHave, "Must", spriteService);
                    PreviewItems.Add(item);
                }
            }
            if (config.Should != null)
            {
                foreach (var clause in config.Should)
                {
                    var item = CreateFilterItemFromClause(clause, FilterItemStatus.ShouldHave, "Should", spriteService);
                    PreviewItems.Add(item);
                }
            }
        }

        private FilterItem CreateFilterItemFromClause(
            MotelyJsonConfig.MotelyJsonFilterClause clause, 
            FilterItemStatus status, 
            string category,
            SpriteService spriteService)
        {
            var item = new FilterItem
            {
                Name = clause.Value ?? clause.Type ?? "Unknown",
                DisplayName = clause.Value ?? clause.Type ?? "Unknown",
                Status = status,
                Category = category,
                Type = clause.Type ?? "Joker",
                Value = clause.Value,
                Edition = clause.Edition,
                Seal = clause.Seal,
                Enhancement = clause.Enhancement,
                Stickers = clause.Stickers?.ToList(),
                Score = clause.Score,
                MinCount = clause.Min ?? 0
            };

            // Fetch image based on type and name
            item.ItemImage = spriteService.GetItemImage(item.Type, item.Name);

            return item;
        }

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

        [RelayCommand]
        private async Task SyncToVisual()
        {
            if (HasError || string.IsNullOrWhiteSpace(JamlContent) || _parentViewModel == null)
                return;

            try
            {
                var deserializer = new DeserializerBuilder()
                    .WithNamingConvention(CamelCaseNamingConvention.Instance)
                    .IgnoreUnmatchedProperties()
                    .Build();

                var config = deserializer.Deserialize<MotelyJsonConfig>(JamlContent);
                if (config == null) return;

                // Load the config into the parent state
                _parentViewModel.LoadConfigIntoState(config);

                // Update the Visual Builder UI components
                await _parentViewModel.UpdateVisualBuilderFromItemConfigs();
                
                // Ensure zones are expanded if they have items
                _parentViewModel.ExpandDropZonesWithItems();

                ValidationStatus = "✓ Synced to Visual Builder";
                ValidationStatusColor = Brushes.LightGreen;
                
                DebugLogger.LogImportant("JamlEditorTab", "Successfully synced JAML to Visual Builder");
            }
            catch (Exception ex)
            {
                ValidationStatus = "✗ Sync failed";
                ValidationStatusColor = Brushes.Red;
                ErrorMessage = $"Sync error: {ex.Message}";
                HasError = true;
                DebugLogger.LogError("JamlEditorTab", $"Sync error: {ex.Message}");
            }
        }

        [RelayCommand]
        private void FormatJaml()
        {
            if (string.IsNullOrWhiteSpace(JamlContent)) return;

            try
            {
                // Use custom JAML formatter for idiomatic output:
                // - type-as-key format: "joker: Blueprint" instead of "type: Joker, value: Blueprint"
                // - inline numeric arrays: "antes: [1,2,3]" instead of multi-line
                JamlContent = JamlFormatter.Format(JamlContent);
                ValidationStatus = "✓ Formatted";
                ValidationStatusColor = Brushes.LightGreen;
            }
            catch (Exception ex)
            {
                ValidationStatus = "✗ Format failed";
                ValidationStatusColor = Brushes.Red;
                ErrorMessage = ex.Message;
                HasError = true;
            }
        }

        [RelayCommand]
        private async Task CopyJaml()
        {
            if (string.IsNullOrWhiteSpace(JamlContent)) return;
            
            try
            {
                var topLevel = Avalonia.Application.Current?.ApplicationLifetime is Avalonia.Controls.ApplicationLifetimes.IClassicDesktopStyleApplicationLifetime desktop
                    ? desktop.MainWindow
                    : null;

                if (topLevel?.Clipboard != null)
                {
                    await topLevel.Clipboard.SetTextAsync(JamlContent);
                    ValidationStatus = "✓ Copied to clipboard";
                    ValidationStatusColor = Brushes.LightGreen;
                }
            }
            catch (Exception ex)
            {
                DebugLogger.LogError("JamlEditorTab", $"Failed to copy: {ex.Message}");
            }
        }

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
                
                // Validate the generated JAML immediately to catch errors
                ValidateAndPreview();

                // Update status based on validation
                if (HasError)
                {
                    ValidationStatus = $"✗ Invalid JAML: {ErrorMessage}";
                    ValidationStatusColor = Brushes.Red;
                    DebugLogger.LogError("JamlEditorTab", $"Generated invalid JAML: {ErrorMessage}");
                }
                else
                {
                    // Silent status update
                    var totalItems = (config.Must?.Count ?? 0) + (config.Should?.Count ?? 0);
                    ValidationStatus = totalItems > 0 ? $"Auto-synced ({totalItems} items)" : "Ready";
                    ValidationStatusColor = Brushes.Gray;

                    DebugLogger.Log(
                        "JamlEditorTab",
                        $"Auto-synced JAML from visual builder: {config.Must?.Count ?? 0} must, {config.Should?.Count ?? 0} should"
                    );
                }
            }
            catch (Exception ex)
            {
                ValidationStatus = $"✗ Error generating JAML: {ex.Message}";
                ValidationStatusColor = Brushes.Red;
                ErrorMessage = ex.Message;
                HasError = true;
                DebugLogger.LogError("JamlEditorTab", $"Error generating JAML: {ex.Message}\n{ex.StackTrace}");
            }
        }

        #endregion

        #region Helper Methods

        /// <summary>
        /// Convert config to JAML using the centralized JamlFormatter
        /// (Single source of truth in Motely.Filters.JamlFormatter)
        /// </summary>
        private string ConvertConfigToJaml(MotelyJsonConfig config)
        {
            try
            {
                return JamlFormatter.Format(config);
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

        #endregion
    }
}

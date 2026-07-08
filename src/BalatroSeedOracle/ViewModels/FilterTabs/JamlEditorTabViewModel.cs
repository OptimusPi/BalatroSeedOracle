using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using Avalonia.Input.Platform;
using Avalonia.Media;
using BalatroSeedOracle.Helpers;
using BalatroSeedOracle.Models;
using BalatroSeedOracle.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Motely.Filters.Jaml;

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
        /// Public method to trigger jump to error (for use from outside the class)
        /// </summary>
        public void RequestJumpToError(int lineNumber, int column)
        {
            JumpToError?.Invoke(lineNumber, column);
        }

        private System.Threading.CancellationTokenSource? _validationThrottleCts;
        private Task? _validationThrottleTask;

        partial void OnJamlContentChanged(string value)
        {
            // Throttle validation to avoid lag while typing
            _validationThrottleCts?.Cancel();
            _validationThrottleCts = new System.Threading.CancellationTokenSource();
            var token = _validationThrottleCts.Token;

            // Track throttled validation task - no fire-and-forget!
            _validationThrottleTask = ThrottledValidationAsync(token);
        }

        private async Task ThrottledValidationAsync(
            System.Threading.CancellationToken cancellationToken
        )
        {
            try
            {
                await Task.Delay(500, cancellationToken);
                if (cancellationToken.IsCancellationRequested)
                    return;

                await Avalonia.Threading.Dispatcher.UIThread.InvokeAsync(() =>
                {
                    ValidateAndPreview();
                });
            }
            catch (TaskCanceledException)
            {
                // Expected when throttling - ignore
            }
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
                if (!JamlConfigLoader.TryLoad(JamlContent, out var config, out var loadError) || config is null)
                {
                    var lineNumber = ExtractLineNumber(loadError ?? "Unknown error");
                    ValidationErrors.Add(
                        new ValidationErrorItem
                        {
                            LineNumber = lineNumber,
                            Column = 0,
                            Message = loadError ?? "Invalid JAML syntax",
                            Severity = ValidationErrorItem.ErrorSeverity.Error,
                        }
                    );

                    ValidationStatus = "✗ Invalid JAML syntax";
                    ValidationStatusColor = Brushes.Red;
                    ErrorMessage = loadError ?? "Invalid JAML syntax";
                    HasError = true;
                    HasErrors = true;
                    ErrorCount = ValidationErrors.Count;
                    return;
                }

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
            catch (Exception ex)
            {
                ValidationErrors.Add(
                    new ValidationErrorItem
                    {
                        LineNumber = 1,
                        Column = 0,
                        Message = ex.Message,
                        Severity = ValidationErrorItem.ErrorSeverity.Error,
                    }
                );

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

        private void ValidateSchema(JamlConfig config)
        {
            var deckName = config.Deck.ToString();
            if (!Enum.IsDefined(typeof(Motely.Enums.MotelyDeck), config.Deck))
            {
                ValidationErrors.Add(
                    new ValidationErrorItem
                    {
                        LineNumber = 1,
                        Column = 0,
                        Message = $"Invalid deck: {deckName}",
                        Severity = ValidationErrorItem.ErrorSeverity.Warning,
                    }
                );
            }

            var stakeName = config.Stake.ToString();
            if (!Enum.IsDefined(typeof(Motely.Enums.MotelyStake), config.Stake))
            {
                ValidationErrors.Add(
                    new ValidationErrorItem
                    {
                        LineNumber = 1,
                        Column = 0,
                        Message = $"Invalid stake: {stakeName}",
                        Severity = ValidationErrorItem.ErrorSeverity.Warning,
                    }
                );
            }
        }

        private void ValidateAnchors(string yamlContent)
        {
            // Find all anchor references
            var referenceMatches = System.Text.RegularExpressions.Regex.Matches(
                yamlContent,
                @"\*(\w+)"
            );
            var definedAnchors = new System.Collections.Generic.HashSet<string>();

            // Find all anchor definitions
            var definitionMatches = System.Text.RegularExpressions.Regex.Matches(
                yamlContent,
                @"&(\w+)"
            );
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
                    ValidationErrors.Add(
                        new ValidationErrorItem
                        {
                            LineNumber = lineNumber,
                            Column = match.Index - GetLineStartOffset(yamlContent, lineNumber),
                            Message = $"Anchor '{anchorName}' is referenced but not defined",
                            Severity = ValidationErrorItem.ErrorSeverity.Warning,
                        }
                    );
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

        private void UpdatePreview(JamlConfig config)
        {
            PreviewItems.Clear();
            var spriteService = SpriteService.Instance;

            if (config.Must is not null)
            {
                foreach (var clause in config.Must)
                {
                    var item = CreateFilterItemFromClause(
                        clause,
                        FilterItemStatus.MustHave,
                        "Must",
                        spriteService
                    );
                    PreviewItems.Add(item);
                }
            }
            if (config.Should is not null)
            {
                foreach (var clause in config.Should)
                {
                    var item = CreateFilterItemFromClause(
                        clause,
                        FilterItemStatus.ShouldHave,
                        "Should",
                        spriteService
                    );
                    PreviewItems.Add(item);
                }
            }
        }

        private FilterItem CreateFilterItemFromClause(
            IJamlClause clause,
            FilterItemStatus status,
            string category,
            SpriteService spriteService
        )
        {
            var value = clause.GetValueName() ?? "";
            var typeName = clause.GetTypeName() ?? "";
            var displayName = string.IsNullOrEmpty(value) ? typeName : value;
            var item = new FilterItem
            {
                Name = displayName ?? "",
                DisplayName = displayName ?? "",
                Status = status,
                Category = category,
                Type = string.IsNullOrEmpty(typeName) ? "Joker" : typeName,
                Value = value,
                Edition = clause.GetEditionString() ?? "",
                Seal = clause.GetSealString() ?? "",
                Enhancement = clause.GetEnhancementString() ?? "",
                Stickers = clause.GetStickerStrings()?.ToList(),
                Score = clause.Score > 0 ? clause.Score : 1,
                MinCount = clause.Min,
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
            if (_parentViewModel is not null)
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
            if (HasError || string.IsNullOrWhiteSpace(JamlContent) || _parentViewModel is null)
                return;

            try
            {
                if (!JamlConfigLoader.TryLoad(JamlContent, out var config, out var loadError) || config is null)
                {
                    ValidationStatus = $"✗ Sync failed: {loadError ?? "Invalid JAML"}";
                    ValidationStatusColor = Brushes.Red;
                    ErrorMessage = loadError ?? "Invalid JAML";
                    HasError = true;
                    return;
                }

                // Load the config into the parent state
                _parentViewModel.LoadConfigIntoState(config);

                // Update the Visual Builder UI components
                await _parentViewModel.UpdateVisualBuilderFromItemConfigs();

                // Ensure zones are expanded if they have items
                _parentViewModel.ExpandDropZonesWithItems();

                ValidationStatus = "✓ Synced to Visual Builder";
                ValidationStatusColor = Brushes.LightGreen;

                DebugLogger.LogImportant(
                    "JamlEditorTab",
                    "Successfully synced JAML to Visual Builder"
                );
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
            if (string.IsNullOrWhiteSpace(JamlContent))
                return;

            try
            {
                if (JamlConfigLoader.TryLoad(JamlContent, out var config, out _) && config is not null)
                {
                    JamlContent = JamlConfigLoader.ToYaml(config);
                    ValidationStatus = "✓ Formatted";
                    ValidationStatusColor = Brushes.LightGreen;
                }
                else
                {
                    ValidationStatus = "✗ Cannot format invalid JAML";
                    ValidationStatusColor = Brushes.Red;
                }
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
            if (string.IsNullOrWhiteSpace(JamlContent))
                return;

            try
            {
                var topLevel = Avalonia.Application.Current?.ApplicationLifetime
                    is Avalonia.Controls.ApplicationLifetimes.IClassicDesktopStyleApplicationLifetime desktop
                    ? desktop.MainWindow
                    : null;

                if (topLevel?.Clipboard is not null)
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
                if (_parentViewModel is null)
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
                    DebugLogger.LogError(
                        "JamlEditorTab",
                        $"Generated invalid JAML: {ErrorMessage}"
                    );
                }
                else
                {
                    // Silent status update
                    var totalItems = (config.Must?.Count ?? 0) + (config.Should?.Count ?? 0);
                    ValidationStatus =
                        totalItems > 0 ? $"Auto-synced ({totalItems} items)" : "Ready";
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
                DebugLogger.LogError(
                    "JamlEditorTab",
                    $"Error generating JAML: {ex.Message}\n{ex.StackTrace}"
                );
            }
        }

        #endregion

        #region Helper Methods

        /// <summary>
        /// Convert config to JAML using the engine's canonical YAML serializer.
        /// </summary>
        private string ConvertConfigToJaml(JamlConfig config)
        {
            try
            {
                return JamlConfigLoader.ToYaml(config);
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
            return JamlConfigLoader.ToYaml(new JamlConfig
            {
                Id = Guid.NewGuid().ToString("N"),
                Name = "New Filter",
                Description = "Created with visual filter builder",
                Author = "pifreak",
                Deck = Motely.Enums.MotelyDeck.Red,
                Stake = Motely.Enums.MotelyStake.White,
                Must = [],
                Should = [],
                MustNot = [],
            });
        }

        #endregion
    }
}

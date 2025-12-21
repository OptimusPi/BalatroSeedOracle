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
                
                ValidationStatus = "✓ JAML is valid";
                ValidationStatusColor = Brushes.Green;
                ErrorMessage = "";
                HasError = false;

                // Update preview items
                UpdatePreview(config);
            }
            catch (Exception ex)
            {
                ValidationStatus = "✗ Invalid JAML syntax";
                ValidationStatusColor = Brushes.Red;
                ErrorMessage = ex.Message;
                HasError = true;
                // Don't clear preview on error, keep the last valid one
            }
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
            MotelyJsonConfig.MotleyJsonFilterClause clause, 
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
                var deserializer = new DeserializerBuilder()
                    .WithNamingConvention(CamelCaseNamingConvention.Instance)
                    .IgnoreUnmatchedProperties()
                    .Build();

                var config = deserializer.Deserialize<MotelyJsonConfig>(JamlContent);
                
                var serializer = new SerializerBuilder()
                    .WithNamingConvention(CamelCaseNamingConvention.Instance)
                    .Build();

                JamlContent = serializer.Serialize(config);
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

                // Silent status update
                var totalItems = (config.Must?.Count ?? 0) + (config.Should?.Count ?? 0);
                ValidationStatus = totalItems > 0 ? $"Auto-synced ({totalItems} items)" : "Ready";
                ValidationStatusColor = Brushes.Gray;

                DebugLogger.Log(
                    "JamlEditorTab",
                    $"Auto-synced JAML from visual builder: {config.Must?.Count ?? 0} must, {config.Should?.Count ?? 0} should"
                );
            }
            catch (Exception ex)
            {
                ValidationStatus = $"Error generating JAML: {ex.Message}";
                ValidationStatusColor = Brushes.Red;
                DebugLogger.LogError("JamlEditorTab", $"Error generating JAML: {ex.Message}");
            }
        }

        #endregion

        #region Helper Methods

        private string ConvertConfigToJaml(MotelyJsonConfig config)
        {
            try
            {
                var sb = new System.Text.StringBuilder();

                sb.AppendLine($"name: {config.Name ?? "New Filter"}");
                if (!string.IsNullOrWhiteSpace(config.Description))
                    sb.AppendLine($"description: {config.Description}");
                sb.AppendLine($"author: {config.Author ?? "pifreak"}");
                sb.AppendLine($"dateCreated: {config.DateCreated:yyyy-MM-ddTHH:mm:ss.fffffffZ}");

                if (!string.IsNullOrWhiteSpace(config.Deck))
                    sb.AppendLine($"deck: {config.Deck}");
                if (!string.IsNullOrWhiteSpace(config.Stake))
                    sb.AppendLine($"stake: {config.Stake}");

                sb.AppendLine();
                if (config.Must?.Count > 0)
                {
                    sb.AppendLine("must:");
                    foreach (var clause in config.Must)
                        WriteJamlClause(sb, clause, false);
                }
                else
                {
                    sb.AppendLine("must: []");
                }

                sb.AppendLine();
                if (config.Should?.Count > 0)
                {
                    sb.AppendLine("should:");
                    foreach (var clause in config.Should)
                        WriteJamlClause(sb, clause, true);
                }
                else
                {
                    sb.AppendLine("should: []");
                }

                sb.AppendLine();
                if (config.MustNot?.Count > 0)
                {
                    sb.AppendLine("mustNot:");
                    foreach (var clause in config.MustNot)
                        WriteJamlClause(sb, clause, false);
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

        private void WriteJamlClause(
            System.Text.StringBuilder sb,
            MotelyJsonConfig.MotleyJsonFilterClause clause,
            bool includeScore
        )
        {
            var typeKey = MapTypeToJamlKey(clause.Type);
            sb.AppendLine($"  - {typeKey}: {clause.Value}");

            if (clause.Edition != null)
                sb.AppendLine($"    edition: {clause.Edition}");
            if (clause.Seal != null)
                sb.AppendLine($"    seal: {clause.Seal}");
            if (clause.Enhancement != null)
                sb.AppendLine($"    enhancement: {clause.Enhancement}");
            if (clause.Stickers?.Count > 0)
                sb.AppendLine($"    stickers: [{string.Join(", ", clause.Stickers)}]");
            if (clause.Min.HasValue && clause.Min.Value > 0)
                sb.AppendLine($"    min: {clause.Min.Value}");
            if (includeScore && clause.Score != 0)
                sb.AppendLine($"    score: {clause.Score}");
            if (clause.Antes is { Length: > 0 })
                sb.AppendLine($"    antes: [{string.Join(", ", clause.Antes)}]");
            if (clause.Sources?.ShopSlots is { Length: > 0 })
                sb.AppendLine($"    shopSlots: [{string.Join(", ", clause.Sources.ShopSlots)}]");
            if (clause.Sources?.PackSlots is { Length: > 0 })
                sb.AppendLine($"    packSlots: [{string.Join(", ", clause.Sources.PackSlots)}]");
        }

        private string MapTypeToJamlKey(string? type)
        {
            if (string.IsNullOrEmpty(type))
                return "unknown";

            return type.ToLowerInvariant() switch
            {
                "joker" => "joker",
                "souljoker" => "souljoker",
                "tarotcard" => "tarot",
                "planetcard" => "planet",
                "spectralcard" => "spectral",
                "playingcard" => "card",
                "voucher" => "voucher",
                "tag" => "tag",
                "boss" => "boss",
                _ => type.ToLowerInvariant()
            };
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

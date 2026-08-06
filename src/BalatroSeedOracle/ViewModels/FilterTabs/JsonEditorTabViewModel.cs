using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Avalonia.Media;
using BalatroSeedOracle.Helpers;
using BalatroSeedOracle.Models;
using BalatroSeedOracle.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Motely.Enums;
using Motely.Filters.Jaml;

namespace BalatroSeedOracle.ViewModels.FilterTabs
{
    public partial class JsonEditorTabViewModel : ObservableObject
    {
        private readonly FiltersModalViewModel? _parentViewModel;

        public event EventHandler<string>? CopyToClipboardRequested;

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
                ? $"📄 {_parentViewModel.FilterName}.jaml"
                : "📄 filter.jaml";

        public JsonEditorTabViewModel(FiltersModalViewModel? parentViewModel = null)
        {
            _parentViewModel = parentViewModel;

            // Set default JSON content
            JsonContent = GetDefaultJsonContent();

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

        /// <summary>
        /// Auto-generates JAML from Visual Builder without showing status messages (silent mode).
        /// Called automatically when Visual Builder items change.
        /// </summary>
        public void AutoGenerateFromVisual()
        {
            if (_parentViewModel?.VisualBuilderTab is not VisualBuilderTabViewModel)
                return;

            var config = _parentViewModel.BuildConfigFromCurrentState();
            JsonContent = JamlConfigLoader.ToJaml(config);

            // Silent status update (no user-visible message)
            var totalItems = config.Must.Count + config.Should.Count;
            ValidationStatus = totalItems > 0 ? $"Auto-synced ({totalItems} items)" : "Ready";
            ValidationStatusColor = Brushes.Gray;

            DebugLogger.Log(
                "JsonEditorTab",
                $"Auto-synced JAML from visual builder: {config.Must.Count} must, {config.Should.Count} should"
            );
        }

        [RelayCommand]
        private void GenerateFromVisual()
        {
            if (_parentViewModel?.VisualBuilderTab is not VisualBuilderTabViewModel)
            {
                ValidationStatus = "Visual builder not available";
                ValidationStatusColor = Brushes.Red;
                return;
            }

            var config = _parentViewModel.BuildConfigFromCurrentState();
            JsonContent = JamlConfigLoader.ToJaml(config);

            ValidationStatus =
                $"✓ Generated from visual ({config.Must.Count + config.Should.Count} items)";
            ValidationStatusColor = Brushes.Green;
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

                if (_parentViewModel?.VisualBuilderTab is null)
                {
                    ValidationStatus = "Visual builder not available";
                    ValidationStatusColor = Brushes.Red;
                    return;
                }

                var visualTab = _parentViewModel.VisualBuilderTab as VisualBuilderTabViewModel;
                if (visualTab is null)
                {
                    ValidationStatus = "Visual builder not initialized";
                    ValidationStatusColor = Brushes.Red;
                    return;
                }

                // Parse the YAML content
                if (!JamlConfigLoader.TryLoad(JsonContent, out var config, out var loadError))
                {
                    ValidationStatus = $"Failed to parse YAML: {loadError}";
                    ValidationStatusColor = Brushes.Red;
                    return;
                }

                if (config is null)
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
                if (config.Must is not null)
                {
                    foreach (var clause in config.Must)
                    {
                        var item = FindOrCreateFilterItem(visualTab, clause.GetTypeName(), clause.GetValueName());
                        if (
                            item is not null
                            && !visualTab.SelectedMust.Any(x => x.Name == item.Name)
                        )
                        {
                            visualTab.SelectedMust.Add(item);
                            itemsAdded++;
                        }
                    }
                }

                // Apply Should items
                if (config.Should is not null)
                {
                    foreach (var clause in config.Should)
                    {
                        var item = FindOrCreateFilterItem(visualTab, clause.GetTypeName(), clause.GetValueName());
                        if (
                            item is not null
                            && !visualTab.SelectedShould.Any(x => x.Name == item.Name)
                        )
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
                ValidationStatus = "✓ Valid YAML";
                ValidationStatusColor = Brushes.Green;
            }
            else
            {
                ValidationStatus = "✗ Invalid YAML syntax";
                ValidationStatusColor = Brushes.Red;
            }
        }

        [RelayCommand]
        private void FormatJson()
        {
            try
            {
                if (JamlConfigLoader.TryLoad(JsonContent, out var config, out _) && config is not null)
                {
                    JsonContent = JamlConfigLoader.ToJaml(config);
                    ValidationStatus = "✓ YAML formatted";
                    ValidationStatusColor = Brushes.Green;
                }
                else
                {
                    ValidationStatus = "Cannot format invalid YAML";
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
        private void CopyJson()
        {
            try
            {
                CopyToClipboardRequested?.Invoke(this, JsonContent);

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
            if (string.IsNullOrWhiteSpace(JsonContent))
                return false;
            return JamlConfigLoader.TryLoad(JsonContent, out _, out _);
        }

        private string GetDefaultJsonContent()
        {
            return JamlConfigLoader.ToJaml(new JamlConfig
            {
                Id = Guid.NewGuid().ToString("N"),
                Name = "New Filter",
                Description = "Created with visual filter builder",
                Author = "pifreak",
                Deck = MotelyDeck.Red,
                Stake = MotelyStake.White,
                Must = [],
                Should = [],
                MustNot = [],
            });
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
            if (item is null)
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

        #endregion
    }
}

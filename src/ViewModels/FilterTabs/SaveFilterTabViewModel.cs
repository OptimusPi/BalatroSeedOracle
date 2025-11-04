using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Avalonia.Media;
using BalatroSeedOracle.Controls;
using BalatroSeedOracle.Helpers;
using BalatroSeedOracle.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Motely.Filters;

namespace BalatroSeedOracle.ViewModels.FilterTabs
{
    public partial class SaveFilterTabViewModel : ObservableObject
    {
        private readonly IConfigurationService _configurationService;
        private readonly IFilterService _filterService;
        private readonly IFilterConfigurationService _filterConfigurationService;
        private readonly FiltersModalViewModel _parentViewModel;

        [ObservableProperty]
        [NotifyCanExecuteChangedFor(nameof(SaveCurrentFilterCommand))]
        [NotifyCanExecuteChangedFor(nameof(SaveAsCommand))]
        [NotifyCanExecuteChangedFor(nameof(ExportFilterCommand))]
        private string _filterName = "";

        [ObservableProperty]
        private string _filterDescription = "";

        [ObservableProperty]
        private string _currentFileName = "_UNSAVED_CREATION.json";

        [ObservableProperty]
        private string _lastModified = DateTime.Now.ToString("yyyy-MM-dd HH:mm");

        [ObservableProperty]
        private string _statusMessage = "Ready to save filter";

        [ObservableProperty]
        private IBrush _statusColor = Brushes.Gray;

        // Filter Test State Management (4 states: Idle, Testing, Success, NoMatch)
        [ObservableProperty]
        private bool _isTestRunning = false;

        [ObservableProperty]
        private bool _showTestSuccess = false;

        [ObservableProperty]
        private bool _showTestError = false;

        [ObservableProperty]
        private string _testResultMessage = "";

        [ObservableProperty]
        private string _foundSeed = "";

        [ObservableProperty]
        private bool _showFoundSeed = false;

        [ObservableProperty]
        private int _seedsChecked = 0;

        [ObservableProperty]
        private double _testElapsedTime = 0.0;

        [ObservableProperty]
        private bool _isFilterVerified = false;

        // Expose parent's deck/stake properties for binding
        public int SelectedDeckIndex
        {
            get => _parentViewModel.SelectedDeckIndex;
            set => _parentViewModel.SelectedDeckIndex = value;
        }

        public int SelectedStakeIndex
        {
            get => _parentViewModel.SelectedStakeIndex;
            set => _parentViewModel.SelectedStakeIndex = value;
        }

        public string[] DeckDisplayValues => _parentViewModel.DeckDisplayValues;
        public string[] StakeDisplayValues => _parentViewModel.StakeDisplayValues;

        public SaveFilterTabViewModel(
            FiltersModalViewModel parentViewModel,
            IConfigurationService configurationService,
            IFilterService filterService,
            IFilterConfigurationService filterConfigurationService
        )
        {
            _parentViewModel = parentViewModel;
            _configurationService = configurationService;
            _filterService = filterService;
            _filterConfigurationService = filterConfigurationService;

            // PRE-FILL filter name and description if available
            PreFillFilterData();
        }

        /// <summary>
        /// Pre-fill filter name and description from current filter if available
        /// </summary>
        public void PreFillFilterData()
        {
            try
            {
                // Try to get filter name from parent's loaded config
                if (_parentViewModel.LoadedConfig != null && !string.IsNullOrWhiteSpace(_parentViewModel.LoadedConfig.Name))
                {
                    FilterName = _parentViewModel.LoadedConfig.Name;
                    FilterDescription = _parentViewModel.LoadedConfig.Description ?? "";
                    DebugLogger.Log("SaveFilterTab", $"Pre-filled from LoadedConfig: {FilterName}");
                }
                // Fall back to loaded filter file name
                else if (!string.IsNullOrWhiteSpace(_parentViewModel.CurrentFilterPath))
                {
                    FilterName = Path.GetFileNameWithoutExtension(_parentViewModel.CurrentFilterPath);
                    DebugLogger.Log("SaveFilterTab", $"Pre-filled from CurrentFilterPath: {FilterName}");
                }
            }
            catch (Exception ex)
            {
                DebugLogger.LogError("SaveFilterTab", $"Error pre-filling filter data: {ex.Message}");
            }
        }

        #region Command Implementations - Copied from original FiltersModal

        [RelayCommand(CanExecute = nameof(CanSave))]
        private async Task SaveCurrentFilter()
        {
            try
            {
                if (string.IsNullOrWhiteSpace(FilterName))
                {
                    UpdateStatus("Please enter a filter name", true);
                    return;
                }

                var config = BuildConfigFromCurrentState();
                config.Name = FilterName;
                config.Description = FilterDescription;

                // Generate proper filename in JsonItemFilters folder (same as Save As)
                var filePath = _filterService.GenerateFilterFileName(FilterName);
                var success = await _configurationService.SaveFilterAsync(filePath, config);

                if (success)
                {
                    CurrentFileName = Path.GetFileName(filePath);
                    LastModified = DateTime.Now.ToString("yyyy-MM-dd HH:mm");
                    UpdateStatus($"✓ Filter saved: {CurrentFileName}", false);
                    DebugLogger.Log("SaveFilterTab", $"Filter saved to: {filePath}");
                }
                else
                {
                    UpdateStatus("Failed to save filter", true);
                }
            }
            catch (Exception ex)
            {
                UpdateStatus($"Save error: {ex.Message}", true);
                DebugLogger.LogError("SaveFilterTab", $"Error saving filter: {ex.Message}");
            }
        }

        [RelayCommand(CanExecute = nameof(CanSave))]
        private async Task SaveAs()
        {
            try
            {
                if (string.IsNullOrWhiteSpace(FilterName))
                {
                    UpdateStatus("Please enter a filter name", true);
                    return;
                }

                var newFileName = _filterService.GenerateFilterFileName(FilterName);
                var config = BuildConfigFromCurrentState();
                config.Name = FilterName;
                config.Description = FilterDescription;

                var success = await _configurationService.SaveFilterAsync(newFileName, config);

                if (success)
                {
                    CurrentFileName = Path.GetFileName(newFileName);
                    LastModified = DateTime.Now.ToString("yyyy-MM-dd HH:mm");
                    UpdateStatus($"Filter saved as: {CurrentFileName}", false);
                }
                else
                {
                    UpdateStatus("Failed to save filter", true);
                }
            }
            catch (Exception ex)
            {
                UpdateStatus($"Save As error: {ex.Message}", true);
                DebugLogger.LogError("SaveFilterTab", $"Error in Save As: {ex.Message}");
            }
        }

        [RelayCommand(CanExecute = nameof(CanSave))]
        private async Task ExportFilter()
        {
            try
            {
                var config = BuildConfigFromCurrentState();
                if (config == null || string.IsNullOrWhiteSpace(config.Name))
                {
                    UpdateStatus("Please enter a filter name before exporting", true);
                    return;
                }

                // Export to desktop as JSON
                var desktopPath = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
                var exportFileName = $"{config.Name}_{DateTime.Now:yyyyMMdd_HHmmss}.json";
                var exportPath = Path.Combine(desktopPath, exportFileName);

                // Use custom serializer to include mode and preserve score formatting
                var userProfileService = ServiceHelper.GetService<UserProfileService>();
                var serializer = new FilterSerializationService(userProfileService!);
                var json = serializer.SerializeConfig(config);
                await File.WriteAllTextAsync(exportPath, json);

                UpdateStatus($"✅ Exported to Desktop: {exportFileName}", false);
                DebugLogger.Log("SaveFilterTab", $"Filter exported to: {exportPath}");
            }
            catch (Exception ex)
            {
                UpdateStatus($"Export error: {ex.Message}", true);
                DebugLogger.LogError("SaveFilterTab", $"Error exporting: {ex.Message}");
            }
        }

        private bool CanSave()
        {
            return !string.IsNullOrWhiteSpace(FilterName);
        }

        #endregion

        #region Helper Methods

        // Uses shared FilterConfigurationService instead of duplicating massive logic
        private MotelyJsonConfig BuildConfigFromCurrentState()
        {
            // CRITICAL FIX: Read from VisualBuilderTab's collections if available
            // The VisualBuilderTab has its own SelectedMust/Should/MustNot collections (FilterItem objects)
            IEnumerable<string> mustKeys;
            IEnumerable<string> shouldKeys;
            IEnumerable<string> mustNotKeys;

            if (_parentViewModel.VisualBuilderTab is VisualBuilderTabViewModel visualVm)
            {
                // Convert FilterItem objects to their keys (ItemKey property)
                mustKeys = visualVm.SelectedMust.Select(item => item.ItemKey);
                shouldKeys = visualVm.SelectedShould.Select(item => item.ItemKey);
                mustNotKeys = visualVm.SelectedMustNot.Select(item => item.ItemKey);
                DebugLogger.Log("SaveFilterTab",
                    $"Building config from VisualBuilderTab: {visualVm.SelectedMust.Count} must, {visualVm.SelectedShould.Count} should, {visualVm.SelectedMustNot.Count} mustNot");
            }
            else
            {
                // Fallback to parent's collections (for JSON editor mode)
                mustKeys = _parentViewModel.SelectedMust;
                shouldKeys = _parentViewModel.SelectedShould;
                mustNotKeys = _parentViewModel.SelectedMustNot;
                DebugLogger.Log("SaveFilterTab",
                    $"Building config from parent collections: {mustKeys.Count()} must, {shouldKeys.Count()} should, {mustNotKeys.Count()} mustNot");
            }

            var itemConfigs = _parentViewModel.ItemConfigs;

            return _filterConfigurationService.BuildConfigFromSelections(
                mustKeys.ToList(),
                shouldKeys.ToList(),
                mustNotKeys.ToList(),
                itemConfigs,
                FilterName,
                FilterDescription
            );
        }

        // Logic moved to shared FilterConfigurationService

        [RelayCommand]
        private async Task TestFilter()
        {
            try
            {
                // Reset UI state to Testing
                IsTestRunning = true;
                ShowTestSuccess = false;
                ShowTestError = false;
                ShowFoundSeed = false;
                TestResultMessage = "";
                FoundSeed = "";
                SeedsChecked = 0;
                TestElapsedTime = 0.0;

                // Build the filter configuration from current selections (in-memory only!)
                var config = BuildConfigFromCurrentState();

                // Validate the filter name
                if (string.IsNullOrWhiteSpace(config?.Name))
                {
                    IsTestRunning = false;
                    ShowTestError = true;
                    TestResultMessage = "Please enter a filter name before testing";
                    UpdateStatus("Please enter a filter name before testing", true);
                    return;
                }

                // Derive deck/stake from parent selections
                var deckName = GetDeckName(_parentViewModel.SelectedDeckIndex);
                var stakeName = GetStakeName(_parentViewModel.SelectedStakeIndex);

                // Build test search criteria: Test millions of seeds
                // Batch size 3 = 35^3 = 42,875 seeds per batch
                // Testing 10,000 batches = ~428 MILLION seeds
                // At millions/sec, this should complete in under a minute
                var criteria = new BalatroSeedOracle.Models.SearchCriteria
                {
                    BatchSize = 3, // 35^3 = 42,875 seeds per batch
                    StartBatch = 0,
                    EndBatch = 10000, // 10K batches = ~428M seeds tested
                    Deck = deckName,
                    Stake = stakeName,
                    MinScore = 0,
                    MaxResults = 10, // Find up to 10 matching seeds
                };

                // Start search via SearchManager and wait for results
                var searchManager =
                    ServiceHelper.GetService<BalatroSeedOracle.Services.SearchManager>();
                if (searchManager == null)
                {
                    IsTestRunning = false;
                    ShowTestError = true;
                    TestResultMessage = "SearchManager not available";
                    UpdateStatus("SearchManager not available", true);
                    return;
                }

                // Run quick search with in-memory config (no file I/O!)
                UpdateStatus(
                    $"Testing '{config.Name}' on {deckName} deck, {stakeName} stake...",
                    false
                );
                var results = await searchManager.RunQuickSearchAsync(criteria, config);

                // Update UI with results
                IsTestRunning = false;
                TestElapsedTime = results.ElapsedTime;

                if (results.Success && results.Count > 0)
                {
                    // SUCCESS STATE: Filter found at least 1 matching seed!
                    ShowTestSuccess = true;
                    ShowFoundSeed = true;
                    IsFilterVerified = true;

                    // Extract the seed from results
                    if (results.Seeds != null && results.Seeds.Count > 0)
                    {
                        FoundSeed = results.Seeds[0].ToString() ?? "Unknown";
                    }

                    TestResultMessage = $"✓ VERIFIED - Found matching seed in {results.ElapsedTime:F1}s";
                    UpdateStatus($"✓ Filter verified: Found seed {FoundSeed}", false);
                }
                else if (results.Success && results.Count == 0)
                {
                    // NO MATCH STATE: Filter valid but no seeds found in search range
                    ShowTestError = true;
                    TestResultMessage = $"⚠ NO MATCHES FOUND in {results.ElapsedTime:F1}s\nTry different deck/stake or wider search";
                    UpdateStatus($"⚠ No matching seeds found (searched {criteria.EndBatch} batches)", true);
                }
                else
                {
                    // ERROR STATE: Search failed
                    ShowTestError = true;
                    TestResultMessage = $"❌ Test failed: {results.Error}";
                    UpdateStatus($"Test failed: {results.Error}", true);
                }
            }
            catch (Exception ex)
            {
                IsTestRunning = false;
                ShowTestError = true;
                TestResultMessage = $"❌ Error: {ex.Message}";
                UpdateStatus($"Error testing filter: {ex.Message}", true);
                DebugLogger.LogError("SaveFilterTab", $"Test filter error: {ex.Message}");
            }
        }

        // Helper to compute total batches for a given batch size (1-8)
        private static ulong GetMaxBatchesForBatchSize(int batchSize)
        {
            return batchSize switch
            {
                1 => 64_339_296_875UL,
                2 => 1_838_265_625UL,
                3 => 52_521_875UL,
                4 => 1_500_625UL,
                5 => 42_875UL,
                6 => 1_225UL,
                7 => 35UL,
                8 => 1UL,
                _ => throw new ArgumentException(
                    $"Invalid batch size: {batchSize}. Valid range is 1-8."
                ),
            };
        }

        private string GetDeckName(int index)
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
            };
            return index >= 0 && index < deckNames.Length ? deckNames[index] : "Red";
        }

        private string GetStakeName(int index)
        {
            var stakeNames = new[]
            {
                "white",
                "red",
                "green",
                "black",
                "blue",
                "purple",
                "orange",
                "gold",
            };
            return index >= 0 && index < stakeNames.Length ? stakeNames[index] : "white";
        }

        private void UpdateStatus(string message, bool isError)
        {
            StatusMessage = message;
            StatusColor = isError ? Brushes.Red : Brushes.Green;

            DebugLogger.Log("SaveFilterTab", $"Status: {message} (Error: {isError})");
        }

        [RelayCommand]
        private async Task CopySeed()
        {
            try
            {
                if (!string.IsNullOrWhiteSpace(FoundSeed))
                {
                    await ClipboardService.CopyToClipboardAsync(FoundSeed);
                    UpdateStatus($"✓ Copied seed {FoundSeed} to clipboard", false);
                    DebugLogger.Log("SaveFilterTab", $"Copied seed to clipboard: {FoundSeed}");
                }
            }
            catch (Exception ex)
            {
                UpdateStatus($"Failed to copy seed: {ex.Message}", true);
                DebugLogger.LogError("SaveFilterTab", $"Error copying seed: {ex.Message}");
            }
        }

        #endregion
    }
}

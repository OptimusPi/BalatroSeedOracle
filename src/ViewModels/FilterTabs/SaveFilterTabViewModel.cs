using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using Avalonia.Media;
using BalatroSeedOracle.Controls;
using BalatroSeedOracle.Helpers;
using BalatroSeedOracle.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Motely.Filters;

namespace BalatroSeedOracle.ViewModels.FilterTabs
{
    public class SaveFilterTabViewModel : BaseViewModel
    {
        private readonly IConfigurationService _configurationService;
        private readonly IFilterService _filterService;
        private readonly IFilterConfigurationService _filterConfigurationService;
        private readonly FiltersModalViewModel _parentViewModel;

        private string _filterName = "";
        private string _filterDescription = "";
        private string _currentFileName = "_UNSAVED_CREATION.json";
        private string _lastModified = DateTime.Now.ToString("yyyy-MM-dd HH:mm");
        private string _statusMessage = "Ready to save filter";
        private IBrush _statusColor = Brushes.Gray;

        // Test feedback properties
        private bool _isTestRunning = false;
        private bool _showTestSuccess = false;
        private bool _showTestError = false;
        private string _testResultMessage = "";

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

            // Initialize commands
            SaveCommand = new AsyncRelayCommand(SaveCurrentFilterAsync, CanSave);
            SaveAsCommand = new AsyncRelayCommand(SaveAsAsync, CanSave);
            ExportCommand = new AsyncRelayCommand(ExportFilterAsync, CanSave);
            TestFilterCommand = new AsyncRelayCommand(TestFilterAsync);
        }

        #region Properties

        public string FilterName
        {
            get => _filterName;
            set
            {
                if (SetProperty(ref _filterName, value))
                {
                    ((AsyncRelayCommand)SaveCommand).NotifyCanExecuteChanged();
                    ((AsyncRelayCommand)SaveAsCommand).NotifyCanExecuteChanged();
                    ((AsyncRelayCommand)ExportCommand).NotifyCanExecuteChanged();
                }
            }
        }

        public string FilterDescription
        {
            get => _filterDescription;
            set => SetProperty(ref _filterDescription, value);
        }

        public string CurrentFileName
        {
            get => _currentFileName;
            set => SetProperty(ref _currentFileName, value);
        }

        public string LastModified
        {
            get => _lastModified;
            set => SetProperty(ref _lastModified, value);
        }

        public string StatusMessage
        {
            get => _statusMessage;
            set => SetProperty(ref _statusMessage, value);
        }

        public IBrush StatusColor
        {
            get => _statusColor;
            set => SetProperty(ref _statusColor, value);
        }

        public bool IsTestRunning
        {
            get => _isTestRunning;
            set => SetProperty(ref _isTestRunning, value);
        }

        public bool ShowTestSuccess
        {
            get => _showTestSuccess;
            set => SetProperty(ref _showTestSuccess, value);
        }

        public bool ShowTestError
        {
            get => _showTestError;
            set => SetProperty(ref _showTestError, value);
        }

        public string TestResultMessage
        {
            get => _testResultMessage;
            set => SetProperty(ref _testResultMessage, value);
        }

        #endregion

        #region Commands

        public ICommand SaveCommand { get; }
        public ICommand SaveAsCommand { get; }
        public ICommand ExportCommand { get; }
        public ICommand TestFilterCommand { get; }

        #endregion

        #region Command Implementations - Copied from original FiltersModal

        private async Task SaveCurrentFilterAsync()
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

        private async Task SaveAsAsync()
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

        private async Task ExportFilterAsync()
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
            // Get actual selections from parent ViewModel
            var selectedMust = _parentViewModel.SelectedMust.ToList();
            var selectedShould = _parentViewModel.SelectedShould.ToList();
            var selectedMustNot = _parentViewModel.SelectedMustNot.ToList();
            var itemConfigs = _parentViewModel.ItemConfigs;

            return _filterConfigurationService.BuildConfigFromSelections(
                selectedMust,
                selectedShould,
                selectedMustNot,
                itemConfigs,
                FilterName,
                FilterDescription
            );
        }

        // Logic moved to shared FilterConfigurationService

        private async Task TestFilterAsync()
        {
            try
            {
                // Reset UI state
                IsTestRunning = true;
                ShowTestSuccess = false;
                ShowTestError = false;
                TestResultMessage = "";

                // Build the filter configuration from current selections
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

                // Persist config to a temp file so search can load it
                var tempPath = _configurationService.GetTempFilterPath();
                var saved = await _configurationService.SaveFilterAsync(tempPath, config);
                if (!saved)
                {
                    IsTestRunning = false;
                    ShowTestError = true;
                    TestResultMessage = "Failed to save temp filter for testing";
                    UpdateStatus("Failed to save temp filter for testing", true);
                    return;
                }

                // Derive deck/stake from parent selections
                var deckName = GetDeckName(_parentViewModel.SelectedDeckIndex);
                var stakeName = GetStakeName(_parentViewModel.SelectedStakeIndex);

                // Build quick test search criteria: BatchSize=7 => only 35 batches total
                var criteria = new BalatroSeedOracle.Models.SearchCriteria
                {
                    ConfigPath = tempPath,
                    BatchSize = 7, // 7-char batch = only 35 batches total (not billions!)
                    StartBatch = 0,
                    EndBatch = Math.Min(50, GetMaxBatchesForBatchSize(7)), // Stop after 50 batches max
                    Deck = deckName,
                    Stake = stakeName,
                    MinScore = 0,
                    MaxResults = 10, // Stop after finding 10 seeds
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

                // Run quick search and get results
                UpdateStatus(
                    $"Testing '{config.Name}' on {deckName} deck, {stakeName} stake...",
                    false
                );
                var results = await searchManager.RunQuickSearchAsync(criteria, config);

                // Update UI with results
                IsTestRunning = false;
                if (results.Success)
                {
                    ShowTestSuccess = true;
                    TestResultMessage = $"Found {results.Count} seeds in {results.ElapsedTime:F1}s";
                    UpdateStatus(
                        $"Test passed: Found {results.Count} seeds in {results.ElapsedTime:F1}s",
                        false
                    );
                }
                else
                {
                    ShowTestError = true;
                    TestResultMessage = $"Test failed: {results.Error}";
                    UpdateStatus($"Test failed: {results.Error}", true);
                }
            }
            catch (Exception ex)
            {
                IsTestRunning = false;
                ShowTestError = true;
                TestResultMessage = $"Error: {ex.Message}";
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

        #endregion
    }
}

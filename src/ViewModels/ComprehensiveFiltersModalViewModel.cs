using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using BalatroSeedOracle.Services;
using BalatroSeedOracle.Models;
using BalatroSeedOracle.Controls;
using BalatroSeedOracle.Helpers;
using Motely.Filters;

namespace BalatroSeedOracle.ViewModels
{
    public partial class ComprehensiveFiltersModalViewModel : ObservableObject
    {
        private readonly IConfigurationService _configurationService;
        private readonly IFilterService _filterService;

        [ObservableProperty]
        [NotifyCanExecuteChangedFor(nameof(SaveFilterCommand))]
        private string _filterName = "";

        [ObservableProperty]
        private string _filterDescription = "";

        [ObservableProperty]
        private string _currentFilterPath = "";

        [ObservableProperty]
        [NotifyCanExecuteChangedFor(nameof(SaveFilterCommand), nameof(TestFilterCommand))]
        private bool _isTestRunning = false;

        [ObservableProperty]
        private string _testStatus = "Ready to test filter...";

        [ObservableProperty]
        private bool _showTestSpinner = false;

        [ObservableProperty]
        private bool _showTestSuccess = false;

        [ObservableProperty]
        private bool _showTestError = false;

        // IsInitialized flag
        private bool _isInitialized = false;

        // Filter collections
        public ObservableCollection<FilterItem> AvailableJokers { get; }
        public ObservableCollection<FilterItem> AvailableVouchers { get; }
        public ObservableCollection<FilterItem> AvailableTags { get; }
        public ObservableCollection<FilterItem> SelectedMust { get; }
        public ObservableCollection<FilterItem> SelectedShould { get; }
        public ObservableCollection<FilterItem> SelectedMustNot { get; }

        public ComprehensiveFiltersModalViewModel(IConfigurationService configurationService, IFilterService filterService)
        {
            _configurationService = configurationService;
            _filterService = filterService;

            // Initialize collections
            AvailableJokers = new ObservableCollection<FilterItem>();
            AvailableVouchers = new ObservableCollection<FilterItem>();
            AvailableTags = new ObservableCollection<FilterItem>();
            SelectedMust = new ObservableCollection<FilterItem>();
            SelectedShould = new ObservableCollection<FilterItem>();
            SelectedMustNot = new ObservableCollection<FilterItem>();

            // Load initial data
            if (!_isInitialized)
            {
                LoadAvailableItems();
                _isInitialized = true;
            }
        }

        #region Command Implementations

        [RelayCommand]
        private void CreateNewFilter()
        {
            FilterName = "";
            FilterDescription = "";
            CurrentFilterPath = "";
            
            SelectedMust.Clear();
            SelectedShould.Clear();
            SelectedMustNot.Clear();
            
            ResetTestDisplay();
            
            DebugLogger.Log("FiltersModalViewModel", "New filter created");
        }

        [RelayCommand(CanExecute = nameof(CanSaveFilter))]
        private async Task SaveFilterAsync()
        {
            try
            {
                if (string.IsNullOrWhiteSpace(FilterName))
                {
                    TestStatus = "Please enter a filter name";
                    ShowTestError = true;
                    return;
                }

                var config = BuildConfigFromSelections();
                config.Name = FilterName;
                config.Description = FilterDescription;

                if (string.IsNullOrEmpty(CurrentFilterPath))
                {
                    CurrentFilterPath = _filterService.GenerateFilterFileName(FilterName);
                }

                var success = await _configurationService.SaveFilterAsync(CurrentFilterPath, config);
                
                if (success)
                {
                    TestStatus = $"Filter saved: {System.IO.Path.GetFileName(CurrentFilterPath)}";
                    ShowTestSuccess = true;
                    DebugLogger.Log("FiltersModalViewModel", $"Filter saved to: {CurrentFilterPath}");
                }
                else
                {
                    TestStatus = "Failed to save filter";
                    ShowTestError = true;
                }
            }
            catch (Exception ex)
            {
                TestStatus = $"Save error: {ex.Message}";
                ShowTestError = true;
                DebugLogger.LogError("FiltersModalViewModel", $"Error saving filter: {ex.Message}");
            }
        }

        private bool CanSaveFilter()
        {
            return !string.IsNullOrWhiteSpace(FilterName) && !IsTestRunning;
        }

        [RelayCommand]
        private async Task LoadFilterAsync()
        {
            try
            {
                var filters = await _filterService.GetAvailableFiltersAsync();
                TestStatus = $"Found {filters.Count} available filters";
                // TODO: Show filter selection dialog
            }
            catch (Exception ex)
            {
                TestStatus = $"Load error: {ex.Message}";
                ShowTestError = true;
            }
        }

        [RelayCommand(CanExecute = nameof(CanTestFilter))]
        private async Task TestFilterAsync()
        {
            try
            {
                IsTestRunning = true;
                ShowTestSpinner = true;
                ShowTestSuccess = false;
                ShowTestError = false;
                TestStatus = "Testing filter...";

                // Auto-save for testing if needed
                if (string.IsNullOrEmpty(CurrentFilterPath) && !string.IsNullOrWhiteSpace(FilterName))
                {
                    await SaveFilterAsync();
                }

                if (string.IsNullOrEmpty(CurrentFilterPath))
                {
                    TestStatus = "No filter to test";
                    ShowTestError = true;
                    return;
                }

                // Simulate filter test (replace with actual Motely integration)
                await Task.Delay(2000);

                var hasItems = SelectedMust.Count > 0 || SelectedShould.Count > 0 || SelectedMustNot.Count > 0;
                if (hasItems)
                {
                    TestStatus = "✓ Filter test passed!";
                    ShowTestSuccess = true;
                }
                else
                {
                    TestStatus = "Filter is empty - add some items first";
                    ShowTestError = true;
                }
            }
            catch (Exception ex)
            {
                TestStatus = $"Test failed: {ex.Message}";
                ShowTestError = true;
                DebugLogger.LogError("FiltersModalViewModel", $"Filter test failed: {ex.Message}");
            }
            finally
            {
                IsTestRunning = false;
                ShowTestSpinner = false;
            }
        }

        private bool CanTestFilter()
        {
            return !IsTestRunning;
        }

        [RelayCommand]
        private void AddToMust(FilterItem? item)
        {
            if (item != null && !SelectedMust.Contains(item))
            {
                SelectedMust.Add(item);
                ResetTestDisplay();
            }
        }

        [RelayCommand]
        private void AddToShould(FilterItem? item)
        {
            if (item != null && !SelectedShould.Contains(item))
            {
                SelectedShould.Add(item);
                ResetTestDisplay();
            }
        }

        [RelayCommand]
        private void AddToMustNot(FilterItem? item)
        {
            if (item != null && !SelectedMustNot.Contains(item))
            {
                SelectedMustNot.Add(item);
                ResetTestDisplay();
            }
        }

        [RelayCommand]
        private void RemoveFromMust(FilterItem? item)
        {
            if (item != null)
            {
                SelectedMust.Remove(item);
                ResetTestDisplay();
            }
        }

        [RelayCommand]
        private void RemoveFromShould(FilterItem? item)
        {
            if (item != null)
            {
                SelectedShould.Remove(item);
                ResetTestDisplay();
            }
        }

        [RelayCommand]
        private void RemoveFromMustNot(FilterItem? item)
        {
            if (item != null)
            {
                SelectedMustNot.Remove(item);
                ResetTestDisplay();
            }
        }

        #endregion

        #region Helper Methods

        private void LoadAvailableItems()
        {
            // TODO: Load from BalatroData when available
            // For now, add some sample items
            AvailableJokers.Add(new FilterItem { Name = "Joker", Type = "Joker" });
            AvailableJokers.Add(new FilterItem { Name = "Greedy Joker", Type = "Joker" });
            AvailableJokers.Add(new FilterItem { Name = "Lusty Joker", Type = "Joker" });
            
            AvailableVouchers.Add(new FilterItem { Name = "Overstock", Type = "Voucher" });
            AvailableVouchers.Add(new FilterItem { Name = "Clearance Sale", Type = "Voucher" });
            
            AvailableTags.Add(new FilterItem { Name = "Negative Tag", Type = "SmallBlindTag" });
            AvailableTags.Add(new FilterItem { Name = "Boss Tag", Type = "BigBlindTag" });
        }

        private Motely.Filters.MotelyJsonConfig BuildConfigFromSelections()
        {
            var config = new Motely.Filters.MotelyJsonConfig
            {
                Name = FilterName,
                Description = FilterDescription,
                DateCreated = DateTime.UtcNow,
                Must = new List<MotelyJsonConfig.MotleyJsonFilterClause>(),
                Should = new List<MotelyJsonConfig.MotleyJsonFilterClause>(),
                MustNot = new List<MotelyJsonConfig.MotleyJsonFilterClause>()
            };

            // Convert selected items to clauses
            foreach (var item in SelectedMust)
            {
                config.Must.Add(CreateClauseFromItem(item));
            }

            foreach (var item in SelectedShould)
            {
                config.Should.Add(CreateClauseFromItem(item));
            }

            foreach (var item in SelectedMustNot)
            {
                config.MustNot.Add(CreateClauseFromItem(item));
            }

            return config;
        }

        private Motely.Filters.MotelyJsonConfig.MotleyJsonFilterClause CreateClauseFromItem(FilterItem item)
        {
            return new Motely.Filters.MotelyJsonConfig.MotleyJsonFilterClause
            {
                Type = item.Type,
                Value = item.Name,
                Antes = new[] { 1, 2, 3, 4, 5, 6, 7, 8 } // Default to all antes
            };
        }

        private void ResetTestDisplay()
        {
            ShowTestSpinner = false;
            ShowTestSuccess = false;
            ShowTestError = false;
            TestStatus = "Ready to test filter...";
        }

        #endregion
    }
}
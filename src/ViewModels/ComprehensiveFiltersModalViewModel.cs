using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using CommunityToolkit.Mvvm.Input;
using BalatroSeedOracle.Services;
using BalatroSeedOracle.Models;
using BalatroSeedOracle.Controls;
using BalatroSeedOracle.Helpers;
using Motely.Filters;

namespace BalatroSeedOracle.ViewModels
{
    public class ComprehensiveFiltersModalViewModel : BaseViewModel
    {
        private readonly IConfigurationService _configurationService;
        private readonly IFilterService _filterService;

        // Filter properties
        private string _filterName = "";
        private string _filterDescription = "";
        private string _currentFilterPath = "";
        
        // UI state
        private bool _isTestRunning = false;
        private string _testStatus = "Ready to test filter...";
        private bool _showTestSpinner = false;
        private bool _showTestSuccess = false;
        private bool _showTestError = false;

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

            // Initialize commands
            NewFilterCommand = new RelayCommand(CreateNewFilter);
            SaveFilterCommand = new AsyncRelayCommand(SaveFilterAsync, CanSaveFilter);
            LoadFilterCommand = new AsyncRelayCommand(LoadFilterAsync);
            TestFilterCommand = new AsyncRelayCommand(TestFilterAsync, CanTestFilter);
            
            AddToMustCommand = new RelayCommand<FilterItem>(AddToMust);
            AddToShouldCommand = new RelayCommand<FilterItem>(AddToShould);
            AddToMustNotCommand = new RelayCommand<FilterItem>(AddToMustNot);
            
            RemoveFromMustCommand = new RelayCommand<FilterItem>(RemoveFromMust);
            RemoveFromShouldCommand = new RelayCommand<FilterItem>(RemoveFromShould);
            RemoveFromMustNotCommand = new RelayCommand<FilterItem>(RemoveFromMustNot);

            // Load initial data
            LoadAvailableItems();
        }

        #region Properties

        public string FilterName
        {
            get => _filterName;
            set
            {
                if (SetProperty(ref _filterName, value))
                {
                    ((AsyncRelayCommand)SaveFilterCommand).NotifyCanExecuteChanged();
                }
            }
        }

        public string FilterDescription
        {
            get => _filterDescription;
            set => SetProperty(ref _filterDescription, value);
        }

        public string CurrentFilterPath
        {
            get => _currentFilterPath;
            set => SetProperty(ref _currentFilterPath, value);
        }

        public bool IsTestRunning
        {
            get => _isTestRunning;
            set
            {
                if (SetProperty(ref _isTestRunning, value))
                {
                    ((AsyncRelayCommand)TestFilterCommand).NotifyCanExecuteChanged();
                }
            }
        }

        public string TestStatus
        {
            get => _testStatus;
            set => SetProperty(ref _testStatus, value);
        }

        public bool ShowTestSpinner
        {
            get => _showTestSpinner;
            set => SetProperty(ref _showTestSpinner, value);
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

        #endregion

        #region Commands

        public ICommand NewFilterCommand { get; }
        public ICommand SaveFilterCommand { get; }
        public ICommand LoadFilterCommand { get; }
        public ICommand TestFilterCommand { get; }
        
        public ICommand AddToMustCommand { get; }
        public ICommand AddToShouldCommand { get; }
        public ICommand AddToMustNotCommand { get; }
        
        public ICommand RemoveFromMustCommand { get; }
        public ICommand RemoveFromShouldCommand { get; }
        public ICommand RemoveFromMustNotCommand { get; }

        #endregion

        #region Command Implementations

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

        private void AddToMust(FilterItem? item)
        {
            if (item != null && !SelectedMust.Contains(item))
            {
                SelectedMust.Add(item);
                ResetTestDisplay();
            }
        }

        private void AddToShould(FilterItem? item)
        {
            if (item != null && !SelectedShould.Contains(item))
            {
                SelectedShould.Add(item);
                ResetTestDisplay();
            }
        }

        private void AddToMustNot(FilterItem? item)
        {
            if (item != null && !SelectedMustNot.Contains(item))
            {
                SelectedMustNot.Add(item);
                ResetTestDisplay();
            }
        }

        private void RemoveFromMust(FilterItem? item)
        {
            if (item != null)
            {
                SelectedMust.Remove(item);
                ResetTestDisplay();
            }
        }

        private void RemoveFromShould(FilterItem? item)
        {
            if (item != null)
            {
                SelectedShould.Remove(item);
                ResetTestDisplay();
            }
        }

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
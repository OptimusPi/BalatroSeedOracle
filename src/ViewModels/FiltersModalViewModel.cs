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
using Motely.Filters;
using BalatroSeedOracle.Helpers;

namespace BalatroSeedOracle.ViewModels
{
    public class FiltersModalViewModel : BaseViewModel
    {
        private readonly IConfigurationService _configurationService;
        private readonly IFilterService _filterService;

        // Core properties  
        private string _currentCategory = "Jokers";
        private string _searchFilter = "";
        private string _currentActiveTab = "Visual";
        private string? _currentFilterPath;
        private Motely.Filters.MotelyJsonConfig? _loadedConfig;
        private string _filterName = "";
        private string _filterDescription = "";

        // Collections - Observable for data binding
        private readonly Dictionary<string, List<string>> _itemCategories;
        private readonly ObservableCollection<string> _selectedMust = new();
        private readonly ObservableCollection<string> _selectedShould = new();
        private readonly ObservableCollection<string> _selectedMustNot = new();
        private readonly Dictionary<string, ItemConfig> _itemConfigs = new();

        // Counters
        private int _itemKeyCounter = 0;
        private int _instanceCounter = 0;

        public FiltersModalViewModel(IConfigurationService configurationService, IFilterService filterService)
        {
            _configurationService = configurationService;
            _filterService = filterService;
            
            _itemCategories = InitializeItemCategories();
            
            // Initialize commands
            SaveCommand = new AsyncRelayCommand(SaveCurrentFilterAsync);
            LoadCommand = new AsyncRelayCommand(LoadFilterAsync);
            NewCommand = new AsyncRelayCommand(CreateNewFilterAsync);
            DeleteCommand = new AsyncRelayCommand(DeleteCurrentFilterAsync);
            RefreshCommand = new RelayCommand(RefreshFromConfig);
            ReloadVisualCommand = new AsyncRelayCommand(ReloadVisualFromSavedFileAsync);
            SwitchTabCommand = new RelayCommand<string>(SwitchTab);
        }

        #region Properties

        public string CurrentCategory
        {
            get => _currentCategory;
            set => SetProperty(ref _currentCategory, value);
        }

        public string SearchFilter
        {
            get => _searchFilter;
            set => SetProperty(ref _searchFilter, value);
        }

        public string CurrentActiveTab
        {
            get => _currentActiveTab;
            set => SetProperty(ref _currentActiveTab, value);
        }

        public string? CurrentFilterPath
        {
            get => _currentFilterPath;
            set => SetProperty(ref _currentFilterPath, value);
        }

        public Motely.Filters.MotelyJsonConfig? LoadedConfig
        {
            get => _loadedConfig;
            set => SetProperty(ref _loadedConfig, value);
        }

        public string FilterName
        {
            get => _filterName;
            set => SetProperty(ref _filterName, value);
        }

        public string FilterDescription
        {
            get => _filterDescription;
            set => SetProperty(ref _filterDescription, value);
        }

        public Dictionary<string, List<string>> ItemCategories => _itemCategories;
        public ObservableCollection<string> SelectedMust => _selectedMust;
        public ObservableCollection<string> SelectedShould => _selectedShould;
        public ObservableCollection<string> SelectedMustNot => _selectedMustNot;
        public Dictionary<string, ItemConfig> ItemConfigs => _itemConfigs;

        // Tab visibility properties
        public bool IsVisualTabActive => CurrentActiveTab == "Visual";
        public bool IsJsonTabActive => CurrentActiveTab == "Json";
        public bool IsSaveTabActive => CurrentActiveTab == "Save";
        public bool IsLoadTabActive => CurrentActiveTab == "Load";

        #endregion

        #region Commands

        public ICommand SaveCommand { get; }
        public ICommand LoadCommand { get; }
        public ICommand NewCommand { get; }
        public ICommand DeleteCommand { get; }
        public ICommand RefreshCommand { get; }
        public ICommand ReloadVisualCommand { get; }
        public ICommand SwitchTabCommand { get; }

        #endregion

        #region Command Implementations

        private async Task SaveCurrentFilterAsync()
        {
            try
            {
                if (string.IsNullOrEmpty(CurrentFilterPath))
                {
                    CurrentFilterPath = _configurationService.GetTempFilterPath();
                }

                var config = BuildConfigFromCurrentState();
                var success = await _configurationService.SaveFilterAsync(CurrentFilterPath, config);
                
                if (success)
                {
                    LoadedConfig = config;
                    DebugLogger.Log("FiltersModalViewModel", $"Filter saved to: {CurrentFilterPath}");
                }
                else
                {
                    DebugLogger.LogError("FiltersModalViewModel", "Failed to save filter");
                }
            }
            catch (Exception ex)
            {
                DebugLogger.LogError("FiltersModalViewModel", $"Error saving filter: {ex.Message}");
            }
        }

        private async Task LoadFilterAsync()
        {
            try
            {
                var filters = await _filterService.GetAvailableFiltersAsync();
                // This would typically open a file dialog or selection UI
                // For now, we'll need UI interaction to select which filter to load
                DebugLogger.Log("FiltersModalViewModel", $"Found {filters.Count} available filters");
            }
            catch (Exception ex)
            {
                DebugLogger.LogError("FiltersModalViewModel", $"Error loading filter: {ex.Message}");
            }
        }

        private Task CreateNewFilterAsync()
        {
            try
            {
                ClearAllSelections();
                CurrentFilterPath = null;
                LoadedConfig = null;
                DebugLogger.Log("FiltersModalViewModel", "Created new filter");
                return Task.CompletedTask;
            }
            catch (Exception ex)
            {
                DebugLogger.LogError("FiltersModalViewModel", $"Error creating new filter: {ex.Message}");
                return Task.CompletedTask;
            }
        }

        private async Task DeleteCurrentFilterAsync()
        {
            try
            {
                if (!string.IsNullOrEmpty(CurrentFilterPath))
                {
                    var success = await _filterService.DeleteFilterAsync(CurrentFilterPath);
                    if (success)
                    {
                        await CreateNewFilterAsync();
                        DebugLogger.Log("FiltersModalViewModel", $"Deleted filter: {CurrentFilterPath}");
                    }
                }
            }
            catch (Exception ex)
            {
                DebugLogger.LogError("FiltersModalViewModel", $"Error deleting filter: {ex.Message}");
            }
        }

        private void RefreshFromConfig()
        {
            try
            {
                if (LoadedConfig != null)
                {
                    LoadConfigIntoState(LoadedConfig);
                    DebugLogger.Log("FiltersModalViewModel", "Refreshed from config");
                }
            }
            catch (Exception ex)
            {
                DebugLogger.LogError("FiltersModalViewModel", $"Error refreshing from config: {ex.Message}");
            }
        }

        private async Task ReloadVisualFromSavedFileAsync()
        {
            try
            {
                if (string.IsNullOrEmpty(CurrentFilterPath) || !_configurationService.FileExists(CurrentFilterPath))
                {
                    DebugLogger.Log("FiltersModalViewModel", "No saved file to reload visual from");
                    return;
                }

                DebugLogger.Log("FiltersModalViewModel", $"Reloading visual from file: {CurrentFilterPath}");

                var config = await _configurationService.LoadFilterAsync<Motely.Filters.MotelyJsonConfig>(CurrentFilterPath);
                if (config != null)
                {
                    LoadConfigIntoState(config);
                    LoadedConfig = config;
                    DebugLogger.Log("FiltersModalViewModel", "Visual reloaded from saved file");
                }
            }
            catch (Exception ex)
            {
                DebugLogger.LogError("FiltersModalViewModel", $"Error reloading visual: {ex.Message}");
            }
        }

        private void SwitchTab(string? tabName)
        {
            if (!string.IsNullOrEmpty(tabName))
            {
                CurrentActiveTab = tabName;
                
                // Notify all tab visibility properties
                OnPropertyChanged(nameof(IsVisualTabActive));
                OnPropertyChanged(nameof(IsJsonTabActive));
                OnPropertyChanged(nameof(IsSaveTabActive));
                OnPropertyChanged(nameof(IsLoadTabActive));
                
                DebugLogger.Log("FiltersModalViewModel", $"Switched to {tabName} tab");
            }
        }

        #endregion

        #region Helper Methods

        private Dictionary<string, List<string>> InitializeItemCategories()
        {
            // Initialize from BalatroData like the original implementation
            return new Dictionary<string, List<string>>
            {
                ["Favorites"] = new List<string>(), // TODO: Load from FavoritesService
                ["Jokers"] = new List<string>(), // TODO: Load from BalatroData.Jokers.Keys
                ["Tarots"] = new List<string>(), // TODO: Load from BalatroData.TarotCards.Keys
                ["Planets"] = new List<string>(), // TODO: Load from BalatroData.PlanetCards.Keys
                ["Spectrals"] = new List<string>(), // TODO: Load from BalatroData.SpectralCards.Keys
                ["PlayingCards"] = new List<string>(), // TODO: Generate playing cards list
                ["Vouchers"] = new List<string>(), // TODO: Load from BalatroData.Vouchers.Keys
                ["Tags"] = new List<string>(), // TODO: Load from BalatroData.Tags.Keys
                ["Bosses"] = new List<string>() // TODO: Load from BalatroData.BossBlinds.Keys
            };
        }

        private Motely.Filters.MotelyJsonConfig BuildConfigFromCurrentState()
        {
            var config = new Motely.Filters.MotelyJsonConfig();
            
            // Build configuration from current selections and item configs
            // This would need to be implemented based on the existing logic
            
            return config;
        }

        private void LoadConfigIntoState(Motely.Filters.MotelyJsonConfig config)
        {
            // Clear current state
            ClearAllSelections();
            
            // Load config into state
            // This would need to be implemented based on the existing logic
            
            LoadedConfig = config;
        }

        private void ClearAllSelections()
        {
            _selectedMust.Clear();
            _selectedShould.Clear();
            _selectedMustNot.Clear();
            _itemConfigs.Clear();
            _itemKeyCounter = 0;
            _instanceCounter = 0;
        }

        public string GenerateNextItemKey()
        {
            return $"item_{++_itemKeyCounter}";
        }

        public int GenerateNextInstance()
        {
            return ++_instanceCounter;
        }

        #endregion
    }
}
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using CommunityToolkit.Mvvm.Input;
using Avalonia.Media;
using BalatroSeedOracle.Services;
using BalatroSeedOracle.Helpers;
using BalatroSeedOracle.Controls;
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

        public SaveFilterTabViewModel(FiltersModalViewModel parentViewModel, IConfigurationService configurationService, IFilterService filterService, IFilterConfigurationService filterConfigurationService)
        {
            _parentViewModel = parentViewModel;
            _configurationService = configurationService;
            _filterService = filterService;
            _filterConfigurationService = filterConfigurationService;

            // Initialize commands
            SaveCommand = new AsyncRelayCommand(SaveCurrentFilterAsync, CanSave);
            SaveAsCommand = new AsyncRelayCommand(SaveAsAsync, CanSave);
            ExportCommand = new AsyncRelayCommand(ExportFilterAsync, CanSave);
            TestFilterCommand = new RelayCommand(TestFilter);
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

                var filePath = _configurationService.GetTempFilterPath();
                var success = await _configurationService.SaveFilterAsync(filePath, config);

                if (success)
                {
                    CurrentFileName = Path.GetFileName(filePath);
                    LastModified = DateTime.Now.ToString("yyyy-MM-dd HH:mm");
                    UpdateStatus($"Filter saved: {CurrentFileName}", false);
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

                var json = System.Text.Json.JsonSerializer.Serialize(config, new System.Text.Json.JsonSerializerOptions { WriteIndented = true });
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
                FilterDescription);
        }

        // Logic moved to shared FilterConfigurationService
        
        private void TestFilter()
        {
            try
            {
                // Build the filter configuration
                var config = BuildConfigFromCurrentState();
                
                // Validate the filter
                if (string.IsNullOrWhiteSpace(config?.Name))
                {
                    UpdateStatus("Please enter a filter name before testing", true);
                    return;
                }
                
                // TODO: Launch a test search with this filter
                UpdateStatus($"Filter '{config.Name}' is ready for testing!", false);
                DebugLogger.Log("SaveFilterTab", $"Test filter clicked for: {config.Name}");
                
                // Could emit an event or call a service to actually run the test
            }
            catch (Exception ex)
            {
                UpdateStatus($"Error testing filter: {ex.Message}", true);
                DebugLogger.LogError("SaveFilterTab", $"Test filter error: {ex.Message}");
            }
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
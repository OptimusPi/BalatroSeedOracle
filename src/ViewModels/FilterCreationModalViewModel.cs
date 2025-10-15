using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using BalatroSeedOracle.Models;
using BalatroSeedOracle.Helpers;

namespace BalatroSeedOracle.ViewModels
{
    public partial class FilterCreationModalViewModel : ObservableObject
    {
        [ObservableProperty]
        private string _importPath = "";

        [ObservableProperty]
        private string _importStatusIcon = "";

        [ObservableProperty]
        private bool _importSuccess = false;

        private string? _selectedFilterPath = null;

        [ObservableProperty]
        private string _selectedFilterName = "";

        [ObservableProperty]
        private string _selectedFilterDescription = "";

        [ObservableProperty]
        private int _selectedFilterIndex = -1;

        // Properties
        public bool HasSelectedFilter => !string.IsNullOrEmpty(_selectedFilterPath);

        public ObservableCollection<FilterListItem> Filters { get; } = new();

        // Commands - ImportJsonCommand is set by View
        public RelayCommand ImportJsonCommand { get; set; } = null!;

        // Events
        public event EventHandler<string>? FilterSelectedForEdit;
        public event EventHandler<string>? FilterCloneRequested;
        public event EventHandler<string>? FilterDeleteRequested;
        public event EventHandler? NewFilterRequested;
        public event EventHandler<string>? FilterImported;
        public event EventHandler? CloseRequested;

        public FilterCreationModalViewModel()
        {
            // Load filters
            LoadFilters();
        }

        private void LoadFilters()
        {
            try
            {
                var baseDir = AppDomain.CurrentDomain.BaseDirectory;
                var filtersDir = System.IO.Path.Combine(baseDir, "JsonItemFilters");

                if (!Directory.Exists(filtersDir))
                {
                    DebugLogger.LogError("FilterCreationModalViewModel", $"Filters directory not found: {filtersDir}");
                    return;
                }

                var filterFiles = Directory.GetFiles(filtersDir, "*.json")
                    .OrderBy(f => System.IO.Path.GetFileNameWithoutExtension(f))
                    .ToList();

                Filters.Clear();
                for (int i = 0; i < filterFiles.Count; i++)
                {
                    var filterPath = filterFiles[i];
                    var filterName = System.IO.Path.GetFileNameWithoutExtension(filterPath);
                    var author = GetFilterAuthor(filterPath);

                    Filters.Add(new FilterListItem
                    {
                        Number = i + 1,
                        Name = filterName,
                        Author = author,
                        FilePath = filterPath
                    });
                }

                DebugLogger.Log("FilterCreationModalViewModel", $"Loaded {Filters.Count} filters");
            }
            catch (Exception ex)
            {
                DebugLogger.LogError("FilterCreationModalViewModel", $"Error loading filters: {ex.Message}");
            }
        }

        [RelayCommand]
        private void CreateNewFilter()
        {
            DebugLogger.Log("FilterCreationModalViewModel", "Create new filter requested");
            NewFilterRequested?.Invoke(this, EventArgs.Empty);
        }

        [RelayCommand]
        private void Back()
        {
            DebugLogger.Log("FilterCreationModalViewModel", "Back button clicked");
            CloseRequested?.Invoke(this, EventArgs.Empty);
        }

        [RelayCommand(CanExecute = nameof(HasSelectedFilter))]
        private void DeleteFilter()
        {
            if (string.IsNullOrEmpty(_selectedFilterPath))
            {
                DebugLogger.LogError("FilterCreationModalViewModel", "Cannot delete filter: no filter selected");
                return;
            }

            DebugLogger.Log("FilterCreationModalViewModel", $"Delete filter requested: {_selectedFilterPath}");
            FilterDeleteRequested?.Invoke(this, _selectedFilterPath);
        }

        [RelayCommand(CanExecute = nameof(HasSelectedFilter))]
        private void EditFilter()
        {
            if (string.IsNullOrEmpty(_selectedFilterPath))
            {
                DebugLogger.LogError("FilterCreationModalViewModel", "Cannot edit filter: no filter selected");
                return;
            }

            DebugLogger.Log("FilterCreationModalViewModel", $"Edit filter requested: {_selectedFilterPath}");
            FilterSelectedForEdit?.Invoke(this, _selectedFilterPath);
        }

        [RelayCommand(CanExecute = nameof(HasSelectedFilter))]
        private void CloneFilter()
        {
            if (string.IsNullOrEmpty(_selectedFilterPath))
            {
                DebugLogger.LogError("FilterCreationModalViewModel", "Cannot clone filter: no filter selected");
                return;
            }

            DebugLogger.Log("FilterCreationModalViewModel", $"Clone filter requested: {_selectedFilterPath}");
            FilterCloneRequested?.Invoke(this, _selectedFilterPath);
        }

        public void OnFilterSelected(string filterPath)
        {
            _selectedFilterPath = filterPath;

            // Extract filter name and description from path
            SelectedFilterName = System.IO.Path.GetFileNameWithoutExtension(filterPath);
            SelectedFilterDescription = $"by {GetFilterAuthor(filterPath)}";

            // Update command states
            EditFilterCommand.NotifyCanExecuteChanged();
            CloneFilterCommand.NotifyCanExecuteChanged();
            DeleteFilterCommand.NotifyCanExecuteChanged();
            OnPropertyChanged(nameof(HasSelectedFilter));

            DebugLogger.Log("FilterCreationModalViewModel", $"Filter selected: {filterPath}");
        }

        public void SelectFilter(FilterListItem filter)
        {
            _selectedFilterPath = filter.FilePath;
            SelectedFilterName = filter.Name;
            SelectedFilterDescription = $"by {filter.Author}";

            // Update command states
            EditFilterCommand.NotifyCanExecuteChanged();
            CloneFilterCommand.NotifyCanExecuteChanged();
            DeleteFilterCommand.NotifyCanExecuteChanged();
            OnPropertyChanged(nameof(HasSelectedFilter));

            DebugLogger.Log("FilterCreationModalViewModel", $"Filter selected: {filter.Name}");
        }

        private string GetFilterAuthor(string filterPath)
        {
            try
            {
                if (System.IO.File.Exists(filterPath))
                {
                    var json = System.IO.File.ReadAllText(filterPath);
                    var config = System.Text.Json.JsonSerializer.Deserialize<Motely.Filters.MotelyJsonConfig>(json);
                    return config?.Author ?? "Unknown";
                }
            }
            catch { }
            return "Unknown";
        }

        public async Task ValidateAndImportJsonFile(string filePath)
        {
            try
            {
                // Update UI to show file path
                ImportPath = $"...{Path.GetFileName(filePath)}";
                ImportStatusIcon = ""; // Clear previous status
                
                // Read and validate JSON
                var jsonContent = await File.ReadAllTextAsync(filePath);
                var options = new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true,
                    ReadCommentHandling = JsonCommentHandling.Skip,
                    AllowTrailingCommas = true
                };
                
                var config = JsonSerializer.Deserialize<Motely.Filters.MotelyJsonConfig>(jsonContent, options);
                
                if (config != null)
                {
                    // Validation successful
                    ImportStatusIcon = "";
                    ImportSuccess = true;
                    DebugLogger.Log("FilterCreationModalViewModel", $"Successfully imported filter: {config.Name ?? "Unnamed"}");
                    FilterImported?.Invoke(this, filePath);
                }
                else
                {
                    ShowImportError("Invalid JSON structure");
                }
            }
            catch (JsonException ex)
            {
                DebugLogger.LogError("FilterCreationModalViewModel", $"JSON validation error: {ex.Message}");
                ShowImportError($"Invalid JSON: {ex.Message}");
            }
            catch (Exception ex)
            {
                DebugLogger.LogError("FilterCreationModalViewModel", $"Import error: {ex.Message}");
                ShowImportError($"Import failed: {ex.Message}");
            }
        }

        private void ShowImportError(string errorMessage)
        {
            ImportStatusIcon = "";
            ImportSuccess = false;
            
            // Log error - View can handle showing user dialog if needed
            DebugLogger.LogError("FilterCreationModalViewModel", $"Import validation failed: {errorMessage}");
        }
    }

    /// <summary>
    /// Represents a filter in the list
    /// </summary>
    // Moved to Models/FilterListItem.cs
}
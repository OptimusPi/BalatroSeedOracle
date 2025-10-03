using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows.Input;
using CommunityToolkit.Mvvm.Input;
using BalatroSeedOracle.Helpers;

namespace BalatroSeedOracle.ViewModels
{
    public class FilterCreationModalViewModel : BaseViewModel
    {
        private string _importPath = "";
        private string _importStatusIcon = "";
        private bool _importSuccess = false;
        private string? _selectedFilterPath = null;
        private string _selectedFilterName = "";
        private string _selectedFilterDescription = "";
        private int _selectedFilterIndex = -1;

        // Properties
        public string ImportPath
        {
            get => _importPath;
            private set => SetProperty(ref _importPath, value);
        }

        public string ImportStatusIcon
        {
            get => _importStatusIcon;
            private set => SetProperty(ref _importStatusIcon, value);
        }

        public bool ImportSuccess
        {
            get => _importSuccess;
            private set => SetProperty(ref _importSuccess, value);
        }

        public bool HasSelectedFilter => !string.IsNullOrEmpty(_selectedFilterPath);

        public string SelectedFilterName
        {
            get => _selectedFilterName;
            private set => SetProperty(ref _selectedFilterName, value);
        }

        public string SelectedFilterDescription
        {
            get => _selectedFilterDescription;
            private set => SetProperty(ref _selectedFilterDescription, value);
        }

        public int SelectedFilterIndex
        {
            get => _selectedFilterIndex;
            set => SetProperty(ref _selectedFilterIndex, value);
        }

        public ObservableCollection<FilterListItem> Filters { get; } = new();

        // Commands
        public ICommand CreateNewFilterCommand { get; }
        public ICommand EditFilterCommand { get; }
        public ICommand CloneFilterCommand { get; }
        public ICommand ImportJsonCommand { get; set; } = null!;
        public ICommand DeleteFilterCommand { get; }
        public ICommand BackCommand { get; }

        // Events
        public event EventHandler<string>? FilterSelectedForEdit;
        public event EventHandler<string>? FilterCloneRequested;
        public event EventHandler<string>? FilterDeleteRequested;
        public event EventHandler? NewFilterRequested;
        public event EventHandler<string>? FilterImported;
        public event EventHandler? CloseRequested;

        public FilterCreationModalViewModel()
        {
            CreateNewFilterCommand = new RelayCommand(OnCreateNewFilter);
            EditFilterCommand = new RelayCommand(OnEditFilter, () => !string.IsNullOrEmpty(_selectedFilterPath));
            CloneFilterCommand = new RelayCommand(OnCloneFilter, () => !string.IsNullOrEmpty(_selectedFilterPath));
            DeleteFilterCommand = new RelayCommand(OnDeleteFilter, () => !string.IsNullOrEmpty(_selectedFilterPath));
            ImportJsonCommand = new RelayCommand(() => { }); // Placeholder, View will handle actual browse
            BackCommand = new RelayCommand(OnBack);

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

        private void OnCreateNewFilter()
        {
            DebugLogger.Log("FilterCreationModalViewModel", "Create new filter requested");
            NewFilterRequested?.Invoke(this, EventArgs.Empty);
        }

        private void OnBack()
        {
            DebugLogger.Log("FilterCreationModalViewModel", "Back button clicked");
            CloseRequested?.Invoke(this, EventArgs.Empty);
        }

        private void OnDeleteFilter()
        {
            if (string.IsNullOrEmpty(_selectedFilterPath))
            {
                DebugLogger.LogError("FilterCreationModalViewModel", "Cannot delete filter: no filter selected");
                return;
            }

            DebugLogger.Log("FilterCreationModalViewModel", $"Delete filter requested: {_selectedFilterPath}");
            FilterDeleteRequested?.Invoke(this, _selectedFilterPath);
        }

        private void OnEditFilter()
        {
            if (string.IsNullOrEmpty(_selectedFilterPath))
            {
                DebugLogger.LogError("FilterCreationModalViewModel", "Cannot edit filter: no filter selected");
                return;
            }

            DebugLogger.Log("FilterCreationModalViewModel", $"Edit filter requested: {_selectedFilterPath}");
            FilterSelectedForEdit?.Invoke(this, _selectedFilterPath);
        }

        private void OnCloneFilter()
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
            ((RelayCommand)EditFilterCommand).NotifyCanExecuteChanged();
            ((RelayCommand)CloneFilterCommand).NotifyCanExecuteChanged();
            ((RelayCommand)DeleteFilterCommand).NotifyCanExecuteChanged();
            OnPropertyChanged(nameof(HasSelectedFilter));

            DebugLogger.Log("FilterCreationModalViewModel", $"Filter selected: {filterPath}");
        }

        public void SelectFilter(FilterListItem filter)
        {
            _selectedFilterPath = filter.FilePath;
            SelectedFilterName = filter.Name;
            SelectedFilterDescription = $"by {filter.Author}";

            // Update command states
            ((RelayCommand)EditFilterCommand).NotifyCanExecuteChanged();
            ((RelayCommand)CloneFilterCommand).NotifyCanExecuteChanged();
            ((RelayCommand)DeleteFilterCommand).NotifyCanExecuteChanged();
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
    public class FilterListItem
    {
        public int Number { get; set; }
        public string Name { get; set; } = "";
        public string Author { get; set; } = "";
        public string FilePath { get; set; } = "";
    }
}
using System;
using System.IO;
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

        // Commands
        public ICommand StartDesigningCommand { get; }
        public ICommand EditFilterCommand { get; }
        public ICommand CloneFilterCommand { get; }
        public ICommand BrowseCommand { get; set; } = null!;

        // Events
        public event EventHandler<string>? FilterSelectedForEdit;
        public event EventHandler<string>? FilterCloneRequested;
        public event EventHandler? NewFilterRequested;
        public event EventHandler<string>? FilterImported;

        public FilterCreationModalViewModel()
        {
            StartDesigningCommand = new RelayCommand(OnStartDesigning);
            EditFilterCommand = new RelayCommand(OnEditFilter);
            CloneFilterCommand = new RelayCommand(OnCloneFilter);
            BrowseCommand = new RelayCommand(() => { }); // Placeholder, View will handle actual browse
        }

        private void OnStartDesigning()
        {
            DebugLogger.Log("FilterCreationModalViewModel", "Start designing new filter requested");
            NewFilterRequested?.Invoke(this, EventArgs.Empty);
        }

        private void OnEditFilter()
        {
            DebugLogger.Log("FilterCreationModalViewModel", "Edit filter requested");
            // Will be handled by FilterSelected event when user picks a filter
        }

        private void OnCloneFilter()
        {
            DebugLogger.Log("FilterCreationModalViewModel", "Clone filter requested");
            // Trigger clone event - View will need to get selected filter path
            FilterCloneRequested?.Invoke(this, string.Empty); // View will provide actual path
        }

        public void OnFilterSelected(string filterPath)
        {
            DebugLogger.Log("FilterCreationModalViewModel", $"Existing filter selected: {filterPath}");
            FilterSelectedForEdit?.Invoke(this, filterPath);
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

}
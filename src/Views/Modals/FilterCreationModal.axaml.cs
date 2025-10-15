using System;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Avalonia.Platform.Storage;
using Avalonia.VisualTree;
using BalatroSeedOracle.Components;
using BalatroSeedOracle.ViewModels;
using BalatroSeedOracle.Helpers;
using System.IO;
using MsBox.Avalonia;
using MsBox.Avalonia.Enums;

namespace BalatroSeedOracle.Views.Modals
{
    public partial class FilterCreationModal : UserControl
    {
        private FilterSelectorControl? _filterSelector;
        public FilterCreationModalViewModel ViewModel { get; }

        // Events to communicate with parent
        public event EventHandler<string>? FilterSelectedForEdit;
        public event EventHandler? NewFilterRequested;
        public event EventHandler<string>? FilterImported;

        public FilterCreationModal()
        {
            ViewModel = new FilterCreationModalViewModel();
            DataContext = ViewModel;
            
            InitializeComponent();

            // Get the FilterSelectorControl from XAML
            _filterSelector = this.FindControl<FilterSelectorControl>("FilterSelector");
            
            // Wire up ViewModel events to external events
            ViewModel.FilterSelectedForEdit += (s, e) => FilterSelectedForEdit?.Invoke(this, e);
            ViewModel.FilterCloneRequested += (s, filterPath) => 
            {
                // Clone event - create a separate event for this
                // For now, treat clone same as import since both open editor with loaded filter
                FilterImported?.Invoke(this, filterPath);
            };
            ViewModel.NewFilterRequested += (s, e) => NewFilterRequested?.Invoke(this, e);
            ViewModel.FilterImported += (s, e) => FilterImported?.Invoke(this, e);
            
            // Wire up FilterSelectorControl events to bubble to parent
            if (_filterSelector != null)
            {
                _filterSelector.FilterEditRequested += (s, path) => FilterSelectedForEdit?.Invoke(this, path);
                _filterSelector.FilterCopyRequested += (s, path) => FilterImported?.Invoke(this, path);
                _filterSelector.FilterDeleteRequested += OnFilterDeleteRequested;
                _filterSelector.NewFilterRequested += (s, e) => NewFilterRequested?.Invoke(this, e);
            }
            
            // Handle ImportJsonCommand since it needs file picker access
            ViewModel.ImportJsonCommand = new CommunityToolkit.Mvvm.Input.RelayCommand(async () => await OnBrowse());
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }

        private async void OnFilterDeleteRequested(object? sender, string filterPath)
        {
            if (string.IsNullOrEmpty(filterPath))
                return;

            try
            {
                var filterName = Path.GetFileNameWithoutExtension(filterPath);

                var result = await MessageBoxManager
                    .GetMessageBoxStandard("Delete Filter?",
                        $"Are you sure you want to delete '{filterName}'?\n\nThis cannot be undone.",
                        ButtonEnum.YesNo,
                        Icon.Warning)
                    .ShowAsync();

                if (result == ButtonResult.Yes)
                {
                    if (File.Exists(filterPath))
                    {
                        File.Delete(filterPath);

                        var dbPath = Path.Combine(Directory.GetCurrentDirectory(), "SearchResults", $"{filterName}.duckdb");
                        if (File.Exists(dbPath))
                        {
                            File.Delete(dbPath);
                        }

                        _filterSelector?.RefreshFilters();
                        DebugLogger.Log("FilterCreationModal", $"Deleted filter: {filterName}");
                    }
                }
            }
            catch (Exception ex)
            {
                DebugLogger.LogError("FilterCreationModal", $"Error deleting filter: {ex.Message}");
            }
        }

        private async System.Threading.Tasks.Task OnBrowse()
        {
            try
            {
                var window = this.GetVisualRoot() as Window;
                if (window == null) return;

                var storageProvider = window.StorageProvider;
                var filePickerOptions = new FilePickerOpenOptions
                {
                    Title = "Import Filter JSON",
                    AllowMultiple = false,
                    FileTypeFilter = new[]
                    {
                        new FilePickerFileType("JSON Files")
                        {
                            Patterns = new[] { "*.json" }
                        },
                        new FilePickerFileType("All Files")
                        {
                            Patterns = new[] { "*" }
                        }
                    }
                };

                var files = await storageProvider.OpenFilePickerAsync(filePickerOptions);
                if (files?.Count > 0)
                {
                    var file = files[0];
                    var filePath = file.Path.LocalPath;
                    await ViewModel.ValidateAndImportJsonFile(filePath);
                }
            }
            catch (Exception ex)
            {
                DebugLogger.LogError("FilterCreationModal", $"Error browsing for file: {ex.Message}");
            }
        }

        // Legacy selection handler removed; FilterSelectorControl manages selection internally
    }
}
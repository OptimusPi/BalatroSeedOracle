using System;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Avalonia.Platform.Storage;
using Avalonia.VisualTree;
using BalatroSeedOracle.Components;
using BalatroSeedOracle.ViewModels;
using BalatroSeedOracle.Helpers;

namespace BalatroSeedOracle.Views.Modals
{
    public partial class FilterCreationModal : UserControl
    {
        private FilterSelector? _filterSelector;
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
            
            _filterSelector = this.FindControl<FilterSelector>("ExistingFilterSelector");
            
            // Hide the built-in select button since we have our own action buttons
            if (_filterSelector != null)
            {
                _filterSelector.ShowSelectButton = false;
            }
            
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
            
            // Wire up FilterSelector events to ViewModel
            if (_filterSelector != null)
            {
                _filterSelector.FilterLoaded += (s, path) => ViewModel.OnFilterSelected(path);
                _filterSelector.FilterSelected += (s, path) => ViewModel.OnFilterSelected(path);
            }
            
            // Handle ImportJsonCommand since it needs file picker access
            ViewModel.ImportJsonCommand = new CommunityToolkit.Mvvm.Input.RelayCommand(async () => await OnBrowse());
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
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

        private void OnFilterButtonClick(object? sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is FilterListItem filter)
            {
                ViewModel.SelectFilter(filter);
            }
        }
    }
}
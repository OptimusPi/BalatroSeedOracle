using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Avalonia.Media;
using Avalonia.Platform.Storage;
using Avalonia.Threading;
using BalatroSeedOracle.Helpers;
using BalatroSeedOracle.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Motely.Filters.Jaml;

namespace BalatroSeedOracle.ViewModels.FilterTabs
{
    public partial class ValidateFilterTabViewModel : ObservableObject
    {
        private readonly FiltersModalViewModel _parentViewModel;

        public event EventHandler<string>? CopyToClipboardRequested;

        [ObservableProperty]
        [NotifyCanExecuteChangedFor(nameof(SaveCurrentFilterCommand))]
        [NotifyCanExecuteChangedFor(nameof(SaveAsCommand))]
        [NotifyCanExecuteChangedFor(nameof(ExportFilterCommand))]
        private string _filterName = "";

        [ObservableProperty]
        private string _filterDescription = "";

        [ObservableProperty]
        private string _currentFileName = "_UNSAVED_CREATION.jaml";

        [ObservableProperty]
        private string _lastModified = DateTime.Now.ToString("yyyy-MM-dd HH:mm");

        [ObservableProperty]
        private string _statusMessage = "Ready to validate filter";

        [ObservableProperty]
        private IBrush _statusColor = Brushes.Gray;

        public ValidateFilterTabViewModel(FiltersModalViewModel parentViewModel)
        {
            _parentViewModel = parentViewModel;

            // PRE-FILL filter name and description if available
            PreFillFilterData();
        }

        /// <summary>
        /// Pre-fill filter name and description from current filter if available
        /// </summary>
        public void PreFillFilterData()
        {
            // Try to get filter name from parent's loaded config
            if (
                _parentViewModel.LoadedConfig is not null
                && !string.IsNullOrWhiteSpace(_parentViewModel.LoadedConfig.Name)
            )
            {
                FilterName = _parentViewModel.LoadedConfig.Name;
                FilterDescription = _parentViewModel.LoadedConfig.Description ?? "";
            }
            // Fall back to loaded filter file name
            else if (!string.IsNullOrWhiteSpace(_parentViewModel.CurrentFilterPath))
            {
                FilterName = Path.GetFileNameWithoutExtension(
                    _parentViewModel.CurrentFilterPath
                );
            }
            // Fall back to parent's current name/description (for new unsaved filters)
            else
            {
                if (!string.IsNullOrWhiteSpace(_parentViewModel.FilterName))
                {
                    FilterName = _parentViewModel.FilterName;
                }

                if (!string.IsNullOrWhiteSpace(_parentViewModel.FilterDescription))
                {
                    FilterDescription = _parentViewModel.FilterDescription;
                }
            }

            DebugLogger.Log("ValidateFilterTab", $"Pre-filled: {FilterName}");
        }

        #region Command Implementations

        [RelayCommand(CanExecute = nameof(CanSave))]
        private void SaveCurrentFilter()
        {
            if (string.IsNullOrWhiteSpace(FilterName))
            {
                UpdateStatus("Please enter a filter name", true);
                return;
            }

            var config = BuildConfigFromCurrentState();

            var filePath = FilterFiles.Resolve(FilterName.Trim());
            FilterFiles.Save(config, filePath);

            CurrentFileName = Path.GetFileName(filePath);
            LastModified = DateTime.Now.ToString("yyyy-MM-dd HH:mm");
            UpdateStatus($"✓ Filter saved: {CurrentFileName}", false);
            DebugLogger.Log("ValidateFilterTab", $"Filter saved to: {filePath}");
        }

        [RelayCommand(CanExecute = nameof(CanSave))]
        private void SaveAs()
        {
            SaveCurrentFilter();
        }

        [RelayCommand]
        private void FinishAndClose()
        {
            DebugLogger.Log("ValidateFilterTab", "Finish & Close button clicked");
            _parentViewModel.RequestClose?.Invoke();
        }

        [RelayCommand]
        private void CopyJson()
        {
            var config = BuildConfigFromCurrentState();
            var jaml = JamlConfigLoader.ToJaml(config);

            // Copy to clipboard via event
            CopyToClipboardRequested?.Invoke(this, jaml);

            UpdateStatus("✅ JAML copied to clipboard", false);
            DebugLogger.Log("ValidateFilterTab", "Filter JAML copied to clipboard");
        }

        [RelayCommand(CanExecute = nameof(CanSave))]
        private void GoToSearch()
        {
            // Save the filter first
            SaveCurrentFilter();

            // Get the filter filename to pass to search modal
            var filterFileName = CurrentFileName;
            if (string.IsNullOrWhiteSpace(filterFileName))
            {
                filterFileName = Path.GetFileName(FilterFiles.Resolve(FilterName.Trim()));
            }

            // Close the filter modal and open search modal with this filter
            _parentViewModel.RequestClose?.Invoke();
            _parentViewModel.RequestNavigateToSearch?.Invoke(filterFileName);

            DebugLogger.Log(
                "ValidateFilterTab",
                $"Navigating to search modal with filter: {filterFileName}"
            );
        }

        [RelayCommand(CanExecute = nameof(CanSave))]
        private async Task ExportFilter()
        {
            var config = BuildConfigFromCurrentState();
            if (string.IsNullOrWhiteSpace(config.Name))
            {
                UpdateStatus("Please enter a filter name before exporting", true);
                return;
            }

            var jaml = JamlConfigLoader.ToJaml(config);
            var exportFileName = $"{config.Name}_{DateTime.Now:yyyyMMdd_HHmmss}.jaml";

            var topLevel = TopLevelHelper.GetTopLevel();
            if (topLevel?.StorageProvider is null || !topLevel.StorageProvider.CanSave)
            {
                UpdateStatus("Export not available (no StorageProvider)", true);
                return;
            }

            var file = await topLevel.StorageProvider.SaveFilePickerAsync(
                new FilePickerSaveOptions
                {
                    Title = "Export Filter",
                    SuggestedFileName = exportFileName,
                    FileTypeChoices = new[]
                    {
                        new FilePickerFileType("JAML") { Patterns = new[] { "*.jaml" } },
                    },
                }
            );

            if (file is null)
            {
                UpdateStatus("Export cancelled", true);
                return;
            }

            await using (var stream = await file.OpenWriteAsync())
            {
                var bytes = Encoding.UTF8.GetBytes(jaml);
                await stream.WriteAsync(bytes, 0, bytes.Length);
            }

            UpdateStatus($"✅ Exported: {file.Name}", false);
            DebugLogger.Log("ValidateFilterTab", $"Filter exported to: {file.Name}");
        }

        private bool CanSave()
        {
            return !string.IsNullOrWhiteSpace(FilterName);
        }

        #endregion

        #region Helper Methods

        private JamlConfig BuildConfigFromCurrentState()
        {
            // MUST run on UI thread to access ObservableCollections
            return Dispatcher.UIThread.Invoke(() =>
            {
                // Use the parent's robust implementation
                var config = _parentViewModel.BuildConfigFromCurrentState();

                // Override name and description from this tab's inputs
                config.Name = string.IsNullOrEmpty(FilterName) ? "Untitled Filter" : FilterName;
                config.Description = FilterDescription;

                return config;
            });
        }

        private void UpdateStatus(string message, bool isError)
        {
            StatusMessage = message;
            StatusColor = isError ? Brushes.Red : Brushes.Green;
            DebugLogger.Log("ValidateFilterTab", $"Status: {message} (Error: {isError})");
        }

        #endregion
    }
}

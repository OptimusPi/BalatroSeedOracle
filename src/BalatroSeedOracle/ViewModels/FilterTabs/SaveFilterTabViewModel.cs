using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Avalonia.Media;
using Avalonia.Platform.Storage;
using BalatroSeedOracle.Helpers;
using BalatroSeedOracle.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Motely.Filters.Jaml;

namespace BalatroSeedOracle.ViewModels.FilterTabs
{
    public partial class SaveFilterTabViewModel : ObservableObject
    {
        private readonly FiltersModalViewModel _parentViewModel;

        // Proxy properties to parent ViewModel to ensure sync
        public string FilterName
        {
            get => _parentViewModel.FilterName;
            set
            {
                _parentViewModel.FilterName = value;
                OnPropertyChanged(nameof(FilterName));
                OnPropertyChanged(nameof(NormalizedFilterName));
                SaveCurrentFilterCommand.NotifyCanExecuteChanged();
                SaveAsCommand.NotifyCanExecuteChanged();
                ExportFilterCommand.NotifyCanExecuteChanged();
            }
        }

        public string FilterDescription
        {
            get => _parentViewModel.FilterDescription;
            set
            {
                _parentViewModel.FilterDescription = value;
                OnPropertyChanged(nameof(FilterDescription));
            }
        }

        // Computed property for normalized filter name (auto-generated ID)
        public string NormalizedFilterName => NormalizeFilterName(FilterName);

        // Criteria tree properties
        public ObservableCollection<string> MustItems { get; } = new();
        public ObservableCollection<string> ShouldItems { get; } = new();
        public ObservableCollection<string> BannedItems { get; } = new();

        // Empty state properties
        public bool HasNoMustItems => MustItems.Count == 0;
        public bool HasNoShouldItems => ShouldItems.Count == 0;
        public bool HasNoBannedItems => BannedItems.Count == 0;

        // Header text properties with counts
        public string MustHeaderText => $"MUST ({MustItems.Count} items)";
        public string ShouldHeaderText => $"SHOULD ({ShouldItems.Count} items)";
        public string BannedHeaderText => $"BANNED ({BannedItems.Count} items)";

        [ObservableProperty]
        private string _currentFileName = "_UNSAVED_CREATION.jaml";

        [ObservableProperty]
        private string _lastModified = DateTime.Now.ToString("yyyy-MM-dd HH:mm");

        [ObservableProperty]
        private string _statusMessage = "Ready to save filter";

        [ObservableProperty]
        private IBrush _statusColor = Brushes.Gray;

        // Expose parent's deck/stake properties for binding
        public int SelectedDeckIndex
        {
            get => _parentViewModel.SelectedDeckIndex;
            set => _parentViewModel.SelectedDeckIndex = value;
        }

        public int SelectedStakeIndex
        {
            get => _parentViewModel.SelectedStakeIndex;
            set => _parentViewModel.SelectedStakeIndex = value;
        }

        public string[] DeckDisplayValues => _parentViewModel.DeckDisplayValues;
        public string[] StakeDisplayValues => _parentViewModel.StakeDisplayValues;

        /// <summary>
        /// Current deck display string for binding (avoids indexer in compiled bindings).
        /// </summary>
        public string CurrentDeckDisplayValue =>
            SelectedDeckIndex >= 0 && SelectedDeckIndex < DeckDisplayValues.Length
                ? DeckDisplayValues[SelectedDeckIndex]
                : "";

        // Deck/Stake preview image
        public Avalonia.Media.IImage? DeckStakePreviewImage
        {
            get
            {
                var sprites = SpriteService.Instance;
                var deckName = _parentViewModel.SelectedDeck.ToString();
                var stakeName =
                    StakeDisplayValues.ElementAtOrDefault(SelectedStakeIndex) ?? "White";
                return sprites.GetDeckWithStakeSticker(deckName, stakeName);
            }
        }

        // Quick action: open Joker configuration in Build tab
        [RelayCommand]
        private void OpenJokerConfig()
        {
            _parentViewModel.SelectedTabIndex = 0; // Build Filter
            _parentViewModel.CurrentCategory = "Joker";
        }

        /// <summary>
        /// Save filter and navigate to Search Modal
        /// </summary>
        [RelayCommand(CanExecute = nameof(CanSave))]
        private void GoToSearch()
        {
            SaveCurrentFilter();

            // Close the filters modal and open search modal with this filter
            if (!string.IsNullOrWhiteSpace(CurrentFileName))
            {
                _parentViewModel.RequestNavigateToSearch?.Invoke(CurrentFileName);
            }
        }

        /// <summary>
        /// Save filter and close the Filters Modal
        /// </summary>
        [RelayCommand(CanExecute = nameof(CanSave))]
        private void SaveAndClose()
        {
            SaveCurrentFilter();
            _parentViewModel.RequestClose?.Invoke();
        }

        public SaveFilterTabViewModel(FiltersModalViewModel parentViewModel)
        {
            _parentViewModel = parentViewModel;

            // PRE-FILL filter name and description if available
            PreFillFilterData();
        }

        /// <summary>
        /// Pre-fill filter data from parent if necessary
        /// </summary>
        public void PreFillFilterData()
        {
            // Name/Description are proxied; refresh the criteria display and preview image.
            RefreshCriteriaDisplay();
            OnPropertyChanged(nameof(DeckStakePreviewImage));
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

            var filePath = FilterFiles.Resolve(NormalizeFilterName(FilterName));
            FilterFiles.Save(config, filePath);

            CurrentFileName = Path.GetFileName(filePath);
            LastModified = DateTime.Now.ToString("yyyy-MM-dd HH:mm");
            UpdateStatus($"✓ Filter saved: {CurrentFileName}", false);
            DebugLogger.Log("SaveFilterTab", $"Filter saved to: {filePath}");

            // Sync back to parent ViewModel so it knows the filter is saved
            _parentViewModel.LoadedConfig = config;
            _parentViewModel.CurrentFilterPath = filePath;
        }

        [RelayCommand(CanExecute = nameof(CanSave))]
        private void SaveAs()
        {
            SaveCurrentFilter();
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
            var exportFileName = $"{NormalizeFilterName(config.Name ?? "filter")}.jaml";

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
            DebugLogger.Log("SaveFilterTab", $"Filter exported to: {file.Name}");
        }

        private bool CanSave()
        {
            return !string.IsNullOrWhiteSpace(FilterName);
        }

        #endregion

        #region Helper Methods

        private JamlConfig BuildConfigFromCurrentState()
        {
            // Use the parent's robust implementation
            var config = _parentViewModel.BuildConfigFromCurrentState();

            // Override name and description from this tab's inputs
            config.Name = string.IsNullOrEmpty(FilterName) ? "Untitled Filter" : FilterName;
            config.Description = FilterDescription;

            return config;
        }

        private void UpdateStatus(string message, bool isError)
        {
            StatusMessage = message;
            StatusColor = isError ? Brushes.Red : Brushes.Green;

            DebugLogger.Log("SaveFilterTab", $"Status: {message} (Error: {isError})");
        }

        /// <summary>
        /// Normalizes filter name to valid filename format
        /// </summary>
        private string NormalizeFilterName(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
                return "untitled_filter";

            return name.ToLower()
                .Replace(" ", "_")
                .Replace("-", "_")
                .Where(c => char.IsLetterOrDigit(c) || c == '_')
                .Aggregate("", (acc, c) => acc + c);
        }

        /// <summary>
        /// Refreshes the criteria tree display from parent ViewModel collections
        /// </summary>
        public void RefreshCriteriaDisplay()
        {
            MustItems.Clear();
            ShouldItems.Clear();
            BannedItems.Clear();

            // Get criteria from VisualBuilderTab if available
            if (_parentViewModel.VisualBuilderTab is VisualBuilderTabViewModel visualVm)
            {
                // MUST items
                foreach (var item in visualVm.SelectedMust)
                {
                    if (item is Models.FilterOperatorItem operatorItem)
                    {
                        // Display operator items (OR/AND/BannedItems)
                        if (operatorItem.OperatorType == "BannedItems")
                        {
                            // Add children to Banned list
                            foreach (var child in operatorItem.Children)
                            {
                                BannedItems.Add($"  {child.Type}: {child.DisplayName}");
                            }
                        }
                        else
                        {
                            // OR/AND operators
                            MustItems.Add(
                                $"{operatorItem.OperatorType} ({operatorItem.Children.Count} items)"
                            );
                            foreach (var child in operatorItem.Children)
                            {
                                MustItems.Add($"  {child.Type}: {child.DisplayName}");
                            }
                        }
                    }
                    else
                    {
                        // Regular filter item
                        MustItems.Add($"{item.Type}: {item.DisplayName}");
                    }
                }

                // SHOULD items
                foreach (var item in visualVm.SelectedShould)
                {
                    if (item is Models.FilterOperatorItem operatorItem)
                    {
                        ShouldItems.Add(
                            $"{operatorItem.OperatorType} ({operatorItem.Children.Count} items)"
                        );
                        foreach (var child in operatorItem.Children)
                        {
                            ShouldItems.Add($"  {child.Type}: {child.DisplayName}");
                        }
                    }
                    else
                    {
                        ShouldItems.Add($"{item.Type}: {item.DisplayName}");
                    }
                }
            }

            // Notify property changes for header texts and empty states
            OnPropertyChanged(nameof(MustHeaderText));
            OnPropertyChanged(nameof(ShouldHeaderText));
            OnPropertyChanged(nameof(BannedHeaderText));
            OnPropertyChanged(nameof(HasNoMustItems));
            OnPropertyChanged(nameof(HasNoShouldItems));
            OnPropertyChanged(nameof(HasNoBannedItems));

            DebugLogger.Log(
                "SaveFilterTab",
                $"Refreshed criteria display: {MustItems.Count} must, {ShouldItems.Count} should, {BannedItems.Count} banned"
            );
        }

        #endregion
    }
}

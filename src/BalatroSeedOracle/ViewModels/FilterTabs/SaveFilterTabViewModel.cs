using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Avalonia.Media;
using Avalonia.Platform.Storage;
using BalatroSeedOracle.Controls;
using BalatroSeedOracle.Helpers;
using BalatroSeedOracle.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Motely.Filters;

namespace BalatroSeedOracle.ViewModels.FilterTabs
{
    public partial class SaveFilterTabViewModel : ObservableObject
    {
        private readonly IConfigurationService _configurationService;
        private readonly IFilterService _filterService;
        private readonly IFilterConfigurationService _filterConfigurationService;
        private readonly FiltersModalViewModel _parentViewModel;
        private readonly IPlatformServices _platformServices;

        public event EventHandler<string>? CopyToClipboardRequested;

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
        private string _currentFileName = "_UNSAVED_CREATION.json";

        [ObservableProperty]
        private string _lastModified = DateTime.Now.ToString("yyyy-MM-dd HH:mm");

        [ObservableProperty]
        private string _statusMessage = "Ready to save filter";

        [ObservableProperty]
        private IBrush _statusColor = Brushes.Gray;

        // Filter Test State Management (4 states: Idle, Testing, Success, NoMatch)
        [ObservableProperty]
        private bool _isTestRunning = false;

        [ObservableProperty]
        private bool _showTestSuccess = false;

        [ObservableProperty]
        private bool _showTestError = false;

        [ObservableProperty]
        private string _testResultMessage = "";

        [ObservableProperty]
        private string _foundSeed = "";

        [ObservableProperty]
        private bool _showFoundSeed = false;

        [ObservableProperty]
        private int _seedsChecked = 0;

        [ObservableProperty]
        private double _testElapsedTime = 0.0;

        [ObservableProperty]
        private bool _isFilterVerified = false;

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

        // Deck/Stake preview image
        public Avalonia.Media.IImage? DeckStakePreviewImage
        {
            get
            {
                var sprites = Services.SpriteService.Instance;
                var deckName = _parentViewModel.SelectedDeck.ToString();
                var stakeName = StakeDisplayValues.ElementAtOrDefault(SelectedStakeIndex) ?? "White";
                return sprites.GetDeckWithStakeSticker(deckName, stakeName);
            }
        }

        // Quick action: open Joker configuration in Build tab
        [RelayCommand]
        private void OpenJokerConfig()
        {
            try
            {
                _parentViewModel.SelectedTabIndex = 0; // Build Filter
                _parentViewModel.CurrentCategory = "Joker";
            }
            catch (Exception ex)
            {
                // FAIL LOUD: User clicked button, they need to know if navigation failed
                DebugLogger.LogError("SaveFilterTab", $"❌ Failed to navigate to Joker config: {ex.Message}");
                StatusMessage = "Failed to open Joker configuration";
            }
        }

        /// <summary>
        /// Save filter and navigate to Search Modal
        /// </summary>
        [RelayCommand(CanExecute = nameof(CanSave))]
        private async Task GoToSearch()
        {
            try
            {
                // First save the filter
                await SaveCurrentFilter();

                // Close the filters modal and open search modal with this filter
                if (!string.IsNullOrWhiteSpace(CurrentFileName))
                {
                    _parentViewModel.RequestNavigateToSearch?.Invoke(CurrentFileName);
                }
            }
            catch (Exception ex)
            {
                UpdateStatus($"Error: {ex.Message}", true);
                DebugLogger.LogError("SaveFilterTab", $"Error navigating to search: {ex.Message}");
            }
        }

        /// <summary>
        /// Save filter and close the Filters Modal
        /// </summary>
        [RelayCommand(CanExecute = nameof(CanSave))]
        private async Task SaveAndClose()
        {
            try
            {
                // Save the filter
                await SaveCurrentFilter();

                // Close the filters modal
                _parentViewModel.RequestClose?.Invoke();
            }
            catch (Exception ex)
            {
                UpdateStatus($"Error: {ex.Message}", true);
                DebugLogger.LogError("SaveFilterTab", $"Error saving and closing: {ex.Message}");
            }
        }

        public SaveFilterTabViewModel(
            FiltersModalViewModel parentViewModel,
            IConfigurationService configurationService,
            IFilterService filterService,
            IFilterConfigurationService filterConfigurationService,
            IPlatformServices platformServices
        )
        {
            _parentViewModel = parentViewModel;
            _configurationService = configurationService;
            _filterService = filterService;
            _filterConfigurationService = filterConfigurationService;
            _platformServices = platformServices;

            // PRE-FILL filter name and description if available
            PreFillFilterData();
        }

        /// <summary>
        /// Pre-fill filter data from parent if necessary
        /// </summary>
        public void PreFillFilterData()
        {
            try
            {
                // We no longer need to manually copy Name/Description as they are now proxied.
                // But we still need to refresh the criteria display and preview image.

                // CRITICAL: Refresh criteria display when tab becomes visible
                RefreshCriteriaDisplay();

                OnPropertyChanged(nameof(DeckStakePreviewImage));
            }
            catch (Exception ex)
            {
                DebugLogger.LogError("SaveFilterTab", $"Error pre-filling filter data: {ex.Message}");
            }
        }

        #region Command Implementations - Copied from original FiltersModal

        [RelayCommand(CanExecute = nameof(CanSave))]
        private async Task SaveCurrentFilter()
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

                // Generate proper filename in JsonFilters folder (same as Save As)
                var filePath = _filterService.GenerateFilterFileName(FilterName);
                var success = await _configurationService.SaveFilterAsync(filePath, config);

                if (success)
                {
                    CurrentFileName = Path.GetFileName(filePath);
                    LastModified = DateTime.Now.ToString("yyyy-MM-dd HH:mm");
                    UpdateStatus($"✓ Filter saved: {CurrentFileName}", false);
                    DebugLogger.Log("SaveFilterTab", $"Filter saved to: {filePath}");

                    // Sync back to parent ViewModel so it knows the filter is saved
                    _parentViewModel.LoadedConfig = config;
                    _parentViewModel.CurrentFilterPath = filePath;
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
                DebugLogger.LogError("SaveFilterTab", $"Stack trace: {ex.StackTrace}");
                // Throw to make it visible instead of silent fail
                throw new InvalidOperationException($"Failed to save filter: {ex.Message}", ex);
            }
        }

        [RelayCommand(CanExecute = nameof(CanSave))]
        private async Task SaveAs()
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

                    // Sync back to parent ViewModel so it knows the filter is saved
                    _parentViewModel.LoadedConfig = config;
                    _parentViewModel.CurrentFilterPath = newFileName;
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

        [RelayCommand(CanExecute = nameof(CanSave))]
        private async Task ExportFilter()
        {
            try
            {
                var config = BuildConfigFromCurrentState();
                if (config == null || string.IsNullOrWhiteSpace(config.Name))
                {
                    UpdateStatus("Please enter a filter name before exporting", true);
                    return;
                }

                // Use custom serializer to include mode and preserve score formatting
                var userProfileService = ServiceHelper.GetService<UserProfileService>();
                var serializer = new FilterSerializationService(userProfileService!);
                var json = serializer.SerializeConfig(config);

                var exportFileName = $"{NormalizeFilterName(config.Name ?? "filter")}.json";

                if (!_platformServices.SupportsFileSystem)
                {
                    var topLevel = TopLevelHelper.GetTopLevel();
                    if (topLevel?.StorageProvider == null)
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
                                new FilePickerFileType("JSON") { Patterns = new[] { "*.json" } },
                            },
                        }
                    );

                    if (file == null)
                    {
                        UpdateStatus("Export cancelled", true);
                        return;
                    }

                    await using (var stream = await file.OpenWriteAsync())
                    {
                        var bytes = Encoding.UTF8.GetBytes(json);
                        await stream.WriteAsync(bytes, 0, bytes.Length);
                    }

                    UpdateStatus($"✅ Exported: {exportFileName}", false);
                }
                else
                {
                    // Desktop: Export to desktop folder
                    var desktopPath = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
                    var exportPath = Path.Combine(desktopPath, exportFileName);
                    await File.WriteAllTextAsync(exportPath, json);

                    UpdateStatus($"✅ Exported to Desktop: {exportFileName}", false);
                    DebugLogger.Log("SaveFilterTab", $"Filter exported to: {exportPath}");
                }
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
            // Use the parent's robust implementation if possible
            var config = _parentViewModel.BuildConfigFromCurrentState();

            // Override name and description from this tab's inputs
            config.Name = string.IsNullOrEmpty(FilterName) ? "Untitled Filter" : FilterName;
            config.Description = FilterDescription;

            return config;
        }

        /// <summary>
        /// Converts a FilterItem (including FilterOperatorItem with nested children) to a MotleyJsonFilterClause
        /// </summary>
        private MotelyJsonConfig.MotelyJsonFilterClause? ConvertFilterItemToClause(
            Models.FilterItem item,
            Dictionary<string, Models.ItemConfig> itemConfigs
        )
        {
            // Handle FilterOperatorItem specially - it has Children that need to be recursively converted
            if (item is Models.FilterOperatorItem operatorItem)
            {
                var operatorClause = new MotelyJsonConfig.MotelyJsonFilterClause
                {
                    Type = operatorItem.OperatorType.ToLowerInvariant(), // "or" or "and"
                    Clauses = new List<MotelyJsonConfig.MotelyJsonFilterClause>(),
                };

                DebugLogger.Log(
                    "SaveFilterTab",
                    $"Converting FilterOperatorItem: {operatorItem.OperatorType} with {operatorItem.Children.Count} children"
                );

                // Recursively convert all children
                foreach (var child in operatorItem.Children)
                {
                    var childClause = ConvertFilterItemToClause(child, itemConfigs);
                    if (childClause != null)
                    {
                        operatorClause.Clauses.Add(childClause);
                    }
                    else
                    {
                        DebugLogger.LogError(
                            "SaveFilterTab",
                            $"Failed to convert child: {child.ItemKey} (Type={child.Type}, Name={child.Name})"
                        );
                    }
                }

                DebugLogger.Log(
                    "SaveFilterTab",
                    $"Created {operatorItem.OperatorType} clause with {operatorClause.Clauses.Count} children"
                );

                return operatorClause;
            }

            // Normal FilterItem - TRY to look up in ItemConfigs first
            if (itemConfigs.TryGetValue(item.ItemKey, out var itemConfig))
            {
                // Use FilterConfigurationService to convert ItemConfig to clause
                // We need to create a simple clause from the itemConfig
                var clause = new MotelyJsonConfig.MotelyJsonFilterClause
                {
                    Antes = itemConfig.Antes?.ToArray() ?? new[] { 1, 2, 3, 4, 5, 6, 7, 8 },
                    Min = itemConfig.Min,
                };

                // Map ItemType to Motely type
                var normalizedType = itemConfig.ItemType.ToLower();
                switch (normalizedType)
                {
                    case "joker":
                        clause.Type = "Joker";
                        clause.Value = itemConfig.ItemName;
                        if (!string.IsNullOrEmpty(itemConfig.Edition) && itemConfig.Edition != "none")
                        {
                            clause.Edition = itemConfig.Edition;
                        }
                        break;

                    case "souljoker":
                        clause.Type = "SoulJoker";
                        clause.Value = itemConfig.ItemName;
                        break;

                    case "tarot":
                        clause.Type = "TarotCard";
                        clause.Value = itemConfig.ItemName;
                        break;

                    case "voucher":
                        clause.Type = "Voucher";
                        clause.Value = itemConfig.ItemName;
                        break;

                    case "planet":
                        clause.Type = "PlanetCard";
                        clause.Value = itemConfig.ItemName;
                        break;

                    case "spectral":
                        clause.Type = "SpectralCard";
                        clause.Value = itemConfig.ItemName;
                        break;

                    default:
                        DebugLogger.Log("SaveFilterTab", $"Unknown item type: {itemConfig.ItemType}");
                        return null;
                }

                // Apply additional properties from itemConfig
                if (itemConfig.Stickers != null && itemConfig.Stickers.Count > 0)
                {
                    clause.Stickers = new List<string>(itemConfig.Stickers);
                }
                if (!string.IsNullOrEmpty(itemConfig.Seal) && itemConfig.Seal != "none")
                {
                    clause.Seal = itemConfig.Seal;
                }

                return clause;
            }

            // CRITICAL FIX: Fallback to creating clause from FilterItem properties directly
            // This handles operator children that may not be in itemConfigs dictionary
            DebugLogger.Log(
                "SaveFilterTab",
                $"ItemKey '{item.ItemKey}' not in ItemConfigs - creating clause from FilterItem properties"
            );

            var fallbackClause = new MotelyJsonConfig.MotelyJsonFilterClause
            {
                Antes = item.Antes ?? new[] { 1, 2, 3, 4, 5, 6, 7, 8 },
            };

            // Map Type from FilterItem directly
            var normalizedItemType = item.Type.ToLower();
            switch (normalizedItemType)
            {
                case "joker":
                    fallbackClause.Type = "Joker";
                    fallbackClause.Value = item.Name;
                    if (!string.IsNullOrEmpty(item.Edition) && item.Edition != "none")
                    {
                        fallbackClause.Edition = item.Edition;
                    }
                    break;

                case "souljoker":
                    fallbackClause.Type = "SoulJoker";
                    fallbackClause.Value = item.Name;
                    break;

                case "tarot":
                    fallbackClause.Type = "TarotCard";
                    fallbackClause.Value = item.Name;
                    break;

                case "voucher":
                    fallbackClause.Type = "Voucher";
                    fallbackClause.Value = item.Name;
                    break;

                case "planet":
                    fallbackClause.Type = "PlanetCard";
                    fallbackClause.Value = item.Name;
                    break;

                case "spectral":
                    fallbackClause.Type = "SpectralCard";
                    fallbackClause.Value = item.Name;
                    break;

                default:
                    DebugLogger.LogError("SaveFilterTab", $"Cannot convert FilterItem with unknown type: {item.Type}");
                    return null;
            }

            // Apply additional properties from FilterItem
            if (item.Stickers != null && item.Stickers.Count > 0)
            {
                fallbackClause.Stickers = new List<string>(item.Stickers);
            }
            if (!string.IsNullOrEmpty(item.Seal) && item.Seal != "none")
            {
                fallbackClause.Seal = item.Seal;
            }

            DebugLogger.Log(
                "SaveFilterTab",
                $"Created fallback clause: Type={fallbackClause.Type}, Value={fallbackClause.Value}, Edition={fallbackClause.Edition ?? "none"}, Seal={fallbackClause.Seal ?? "none"}"
            );

            return fallbackClause;
        }

        // Logic moved to shared FilterConfigurationService

        [RelayCommand]
        private async Task TestFilter()
        {
            try
            {
                // Reset UI state to Testing
                IsTestRunning = true;
                ShowTestSuccess = false;
                ShowTestError = false;
                ShowFoundSeed = false;
                TestResultMessage = "";
                FoundSeed = "";
                SeedsChecked = 0;
                TestElapsedTime = 0.0;

                // Build the filter configuration from current selections (in-memory only!)
                var config = BuildConfigFromCurrentState();

                // Validate the filter name
                if (string.IsNullOrWhiteSpace(config?.Name))
                {
                    IsTestRunning = false;
                    ShowTestError = true;
                    TestResultMessage = "Please enter a filter name before testing";
                    UpdateStatus("Please enter a filter name before testing", true);
                    return;
                }

                // Derive deck/stake from parent selections
                var deckName = GetDeckName(_parentViewModel.SelectedDeckIndex);
                var stakeName = GetStakeName(_parentViewModel.SelectedStakeIndex);

                // Build test search criteria: Test millions of seeds
                // Batch size 2 = 35^2 = 1,225 seeds per batch (better API responsiveness)
                // Testing 10,000 batches = ~12.25 MILLION seeds
                var criteria = new BalatroSeedOracle.Models.SearchCriteria
                {
                    BatchSize = 2, // 35^2 = 1,225 seeds per batch
                    StartBatch = 0,
                    EndBatch = 10000, // 10K batches = ~428M seeds tested
                    Deck = deckName,
                    Stake = stakeName,
                    MinScore = 0,
                    MaxResults = 10, // Find up to 10 matching seeds
                };

                // Start search via SearchManager and wait for results
                var searchManager = ServiceHelper.GetService<BalatroSeedOracle.Services.SearchManager>();
                if (searchManager == null)
                {
                    IsTestRunning = false;
                    ShowTestError = true;
                    TestResultMessage = "SearchManager not available";
                    UpdateStatus("SearchManager not available", true);
                    return;
                }

                // Run quick search with in-memory config (no file I/O!)
                UpdateStatus($"Testing '{config.Name}' on {deckName} deck, {stakeName} stake...", false);
                var results = await searchManager.RunQuickSearchAsync(criteria, config);

                // Update UI with results
                IsTestRunning = false;
                TestElapsedTime = results.ElapsedTime;

                if (results.Success && results.Count > 0)
                {
                    // SUCCESS STATE: Filter found at least 1 matching seed!
                    ShowTestSuccess = true;
                    ShowFoundSeed = true;
                    IsFilterVerified = true;

                    // Find seed with highest TotalScore
                    string? verifiedSeed = null;
                    if (results.Results != null && results.Results.Count > 0)
                    {
                        var bestResult = results.Results.OrderByDescending(r => r.TotalScore).First();
                        verifiedSeed = bestResult.Seed;
                        FoundSeed = verifiedSeed ?? "Unknown";
                        DebugLogger.Log(
                            "SaveFilterTab",
                            $"Selected seed {FoundSeed} with TotalScore {bestResult.TotalScore}"
                        );
                    }
                    else if (results.Seeds != null && results.Seeds.Count > 0)
                    {
                        // Fallback to first seed if Results not available
                        verifiedSeed = results.Seeds[0];
                        FoundSeed = verifiedSeed ?? "Unknown";
                    }

                    // Save verified seed to config and persist to file
                    if (!string.IsNullOrEmpty(verifiedSeed))
                    {
                        config.VerifiedSeed = verifiedSeed;
                        var filePath = _filterService.GenerateFilterFileName(config.Name ?? "filter");
                        await _configurationService.SaveFilterAsync(filePath, config);
                        DebugLogger.Log("SaveFilterTab", $"Saved verified seed {verifiedSeed} to filter config");
                    }

                    TestResultMessage = $"✓ VERIFIED - Found matching seed in {results.ElapsedTime:F1}s";
                    UpdateStatus($"✓ Filter verified: Found seed {FoundSeed}", false);
                }
                else if (results.Success && results.Count == 0)
                {
                    // NO MATCH STATE: Filter valid but no seeds found in search range
                    ShowTestError = true;
                    TestResultMessage =
                        $"⚠ NO MATCHES FOUND in {results.ElapsedTime:F1}s\nTry different deck/stake or wider search";
                    UpdateStatus($"⚠ No matching seeds found (searched {criteria.EndBatch} batches)", true);
                }
                else
                {
                    // ERROR STATE: Search failed
                    ShowTestError = true;
                    TestResultMessage = $"❌ Test failed: {results.Error}";
                    UpdateStatus($"Test failed: {results.Error}", true);
                }
            }
            catch (Exception ex)
            {
                IsTestRunning = false;
                ShowTestError = true;
                TestResultMessage = $"❌ Error: {ex.Message}";
                UpdateStatus($"Error testing filter: {ex.Message}", true);
                DebugLogger.LogError("SaveFilterTab", $"Test filter error: {ex.Message}");
            }
        }

        // Helper to compute total batches for a given batch size (1-8)
        private static ulong GetMaxBatchesForBatchSize(int batchSize)
        {
            return batchSize switch
            {
                1 => 64_339_296_875UL,
                2 => 1_838_265_625UL,
                3 => 52_521_875UL,
                4 => 1_500_625UL,
                5 => 42_875UL,
                6 => 1_225UL,
                7 => 35UL,
                8 => 1UL,
                _ => throw new ArgumentException($"Invalid batch size: {batchSize}. Valid range is 1-8."),
            };
        }

        // Convert index to deck name via enum
        private string GetDeckName(int index)
        {
            if (index >= 0 && index <= 14)
                return ((Motely.MotelyDeck)index).ToString();
            return "Red";
        }

        // Convert index to stake name via enum (handles gaps in enum values)
        private string GetStakeName(int index)
        {
            var stake = index switch
            {
                0 => Motely.MotelyStake.White,
                1 => Motely.MotelyStake.Red,
                2 => Motely.MotelyStake.Green,
                3 => Motely.MotelyStake.Black,
                4 => Motely.MotelyStake.Blue,
                5 => Motely.MotelyStake.Purple,
                6 => Motely.MotelyStake.Orange,
                7 => Motely.MotelyStake.Gold,
                _ => Motely.MotelyStake.White,
            };
            return stake.ToString().ToLower();
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
                            MustItems.Add($"{operatorItem.OperatorType} ({operatorItem.Children.Count} items)");
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
                        ShouldItems.Add($"{operatorItem.OperatorType} ({operatorItem.Children.Count} items)");
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

        [RelayCommand]
        private void CopySeed()
        {
            try
            {
                if (!string.IsNullOrWhiteSpace(FoundSeed))
                {
                    CopyToClipboardRequested?.Invoke(this, FoundSeed);
                    UpdateStatus($"✓ Copied seed {FoundSeed} to clipboard", false);
                    DebugLogger.Log("SaveFilterTab", $"Copied seed to clipboard: {FoundSeed}");
                }
            }
            catch (Exception ex)
            {
                UpdateStatus($"Failed to copy seed: {ex.Message}", true);
                DebugLogger.LogError("SaveFilterTab", $"Error copying seed: {ex.Message}");
            }
        }

        #endregion
    }
}

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Avalonia.Media;
using BalatroSeedOracle.Controls;
using BalatroSeedOracle.Helpers;
using BalatroSeedOracle.Models;
using BalatroSeedOracle.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Motely.Filters;

namespace BalatroSeedOracle.ViewModels.FilterTabs
{
    public partial class ValidateFilterTabViewModel : ObservableObject
    {
        private readonly IConfigurationService _configurationService;
        private readonly IFilterService _filterService;
        private readonly IFilterConfigurationService _filterConfigurationService;
        private readonly FiltersModalViewModel _parentViewModel;
        private readonly ClauseConversionService _clauseConversionService;

        [ObservableProperty]
        [NotifyCanExecuteChangedFor(nameof(SaveCurrentFilterCommand))]
        [NotifyCanExecuteChangedFor(nameof(SaveAsCommand))]
        [NotifyCanExecuteChangedFor(nameof(ExportFilterCommand))]
        private string _filterName = "";

        [ObservableProperty]
        private string _filterDescription = "";

        [ObservableProperty]
        private string _currentFileName = "_UNSAVED_CREATION.json";

        [ObservableProperty]
        private string _lastModified = DateTime.Now.ToString("yyyy-MM-dd HH:mm");

        [ObservableProperty]
        private string _statusMessage = "Ready to validate filter";

        [ObservableProperty]
        private IBrush _statusColor = Brushes.Gray;

        // NEW: Clause display collections for row-based UI
        [ObservableProperty]
        private ObservableCollection<ClauseRowViewModel> _mustClauses = new();

        [ObservableProperty]
        private ObservableCollection<ClauseRowViewModel> _mustNotClauses = new();

        [ObservableProperty]
        private ObservableCollection<ClauseRowViewModel> _shouldClauses = new();

        // NEW: Live search feedback
        [ObservableProperty]
        private bool _isLiveSearchActive = false;

        [ObservableProperty]
        private int _liveSearchMatchCount = 0;

        [ObservableProperty]
        private double _liveSearchProgress = 0;

        [ObservableProperty]
        private string _liveSearchStatus = "";

        // NEW: Validation warnings
        [ObservableProperty]
        private bool _hasValidationWarnings = false;

        [ObservableProperty]
        private string _validationWarningText = "";

        // NEW: Example seed storage
        [ObservableProperty]
        private string _exampleSeedForPreview = "";

        // Filter Test State Management
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

        public ValidateFilterTabViewModel(
            FiltersModalViewModel parentViewModel,
            IConfigurationService configurationService,
            IFilterService filterService,
            IFilterConfigurationService filterConfigurationService
        )
        {
            _parentViewModel = parentViewModel;
            _configurationService = configurationService;
            _filterService = filterService;
            _filterConfigurationService = filterConfigurationService;
            _clauseConversionService = new ClauseConversionService();

            // PRE-FILL filter name and description if available
            PreFillFilterData();

            // Refresh clause display when ViewModel is created
            RefreshClauseDisplay();
        }

        /// <summary>
        /// Pre-fill filter name and description from current filter if available
        /// </summary>
        public void PreFillFilterData()
        {
            try
            {
                // Try to get filter name from parent's loaded config
                if (
                    _parentViewModel.LoadedConfig != null
                    && !string.IsNullOrWhiteSpace(_parentViewModel.LoadedConfig.Name)
                )
                {
                    FilterName = _parentViewModel.LoadedConfig.Name;
                    FilterDescription = _parentViewModel.LoadedConfig.Description ?? "";
                    DebugLogger.Log("ValidateFilterTab", $"Pre-filled from LoadedConfig: {FilterName}");
                }
                // Fall back to loaded filter file name
                else if (!string.IsNullOrWhiteSpace(_parentViewModel.CurrentFilterPath))
                {
                    FilterName = Path.GetFileNameWithoutExtension(
                        _parentViewModel.CurrentFilterPath
                    );
                    DebugLogger.Log(
                        "ValidateFilterTab",
                        $"Pre-filled from CurrentFilterPath: {FilterName}"
                    );
                }
            }
            catch (Exception ex)
            {
                DebugLogger.LogError(
                    "ValidateFilterTab",
                    $"Error pre-filling filter data: {ex.Message}"
                );
            }
        }

        /// <summary>
        /// Refreshes the clause display from current filter state
        /// </summary>
        public void RefreshClauseDisplay()
        {
            MustClauses.Clear();
            MustNotClauses.Clear();
            ShouldClauses.Clear();

            try
            {
                // Get VisualBuilderTab if available
                if (_parentViewModel.VisualBuilderTab is VisualBuilderTabViewModel visualVm)
                {
                    DebugLogger.Log("ValidateFilterTab",
                        $"✅ FOUND VisualBuilderTab - Refreshing from VisualBuilder: {visualVm.SelectedMust.Count} must, {visualVm.SelectedShould.Count} should");

                    // Convert Must items
                    foreach (var item in visualVm.SelectedMust)
                    {
                        // Check for BannedItems operator
                        if (item is Models.FilterOperatorItem operatorItem &&
                            operatorItem.OperatorType == "BannedItems")
                        {
                            // Add children to MustNot
                            foreach (var child in operatorItem.Children)
                            {
                                var config = _parentViewModel.ItemConfigs.ContainsKey($"{child.Category}:{child.Name}")
                                    ? _parentViewModel.ItemConfigs[$"{child.Category}:{child.Name}"]
                                    : new ItemConfig();
                                var clause = _clauseConversionService.ConvertFilterItemToClause(child, config);
                                if (clause != null)
                                {
                                    var clauseRow = _clauseConversionService.ConvertToClauseViewModel(clause, child.Category, 0);
                                    if (clauseRow != null)
                                        MustNotClauses.Add(clauseRow);
                                }
                            }
                        }
                        else
                        {
                            var config = _parentViewModel.ItemConfigs.ContainsKey($"{item.Category}:{item.Name}")
                                ? _parentViewModel.ItemConfigs[$"{item.Category}:{item.Name}"]
                                : new ItemConfig();
                            var clause = _clauseConversionService.ConvertFilterItemToClause(item, config);
                            if (clause != null)
                            {
                                var clauseRow = _clauseConversionService.ConvertToClauseViewModel(clause, item.Category, 0);
                                if (clauseRow != null)
                                    MustClauses.Add(clauseRow);
                            }
                        }
                    }

                    // Convert Should items
                    foreach (var item in visualVm.SelectedShould)
                    {
                        var config = _parentViewModel.ItemConfigs.ContainsKey($"{item.Category}:{item.Name}")
                            ? _parentViewModel.ItemConfigs[$"{item.Category}:{item.Name}"]
                            : new ItemConfig();
                        var clause = _clauseConversionService.ConvertFilterItemToClause(item, config);
                        if (clause != null)
                        {
                            var clauseRow = _clauseConversionService.ConvertToClauseViewModel(clause, item.Category, 0);
                            if (clauseRow != null)
                                ShouldClauses.Add(clauseRow);
                        }
                    }
                }
                else
                {
                    // Fallback to parent's key-based collections
                    DebugLogger.Log("ValidateFilterTab",
                        $"⚠️ NO VisualBuilderTab found! Falling back to parent collections: {_parentViewModel.SelectedMust.Count()} must, {_parentViewModel.SelectedMustNot.Count()} mustNot");
                    DebugLogger.Log("ValidateFilterTab",
                        $"VisualBuilderTab is null: {_parentViewModel.VisualBuilderTab == null}, Type: {_parentViewModel.VisualBuilderTab?.GetType()?.Name ?? "null"}");

                    // Convert Must items from keys
                    foreach (var itemKey in _parentViewModel.SelectedMust)
                    {
                        if (_parentViewModel.ItemConfigs.TryGetValue(itemKey, out var config))
                        {
                            var clauseRow = ConvertItemConfigDirectly(config, itemKey, 0);
                            if (clauseRow != null)
                                MustClauses.Add(clauseRow);
                        }
                    }

                    // Convert MustNot items
                    foreach (var itemKey in _parentViewModel.SelectedMustNot)
                    {
                        if (_parentViewModel.ItemConfigs.TryGetValue(itemKey, out var config))
                        {
                            var clauseRow = ConvertItemConfigDirectly(config, itemKey, 0);
                            if (clauseRow != null)
                                MustNotClauses.Add(clauseRow);
                        }
                    }

                    // Convert Should items
                    foreach (var itemKey in _parentViewModel.SelectedShould)
                    {
                        if (_parentViewModel.ItemConfigs.TryGetValue(itemKey, out var config))
                        {
                            var clauseRow = ConvertItemConfigDirectly(config, itemKey, 0);
                            if (clauseRow != null)
                                ShouldClauses.Add(clauseRow);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                DebugLogger.LogError("ValidateFilterTab", $"Error refreshing clause display: {ex.Message}");
            }
        }


        private ClauseRowViewModel? ConvertItemConfigDirectly(ItemConfig config, string itemKey, int nestingLevel)
        {
            // Extract category and name from item key (format: "category:name")
            var parts = itemKey.Split(':');
            if (parts.Length != 2)
                return null;

            var category = parts[0];
            var name = parts[1];

            // Create a FilterItem to pass to the conversion service
            var filterItem = new FilterItem
            {
                Category = category,
                Name = name,
                ItemKey = itemKey
            };

            // Use the conversion service to create the clause
            var clause = _clauseConversionService.ConvertFilterItemToClause(filterItem, config);
            if (clause == null)
                return null;

            // Convert the clause to a view model
            var clauseRow = _clauseConversionService.ConvertToClauseViewModel(clause, category, nestingLevel);

            // Wire up commands if the row was created successfully
            if (clauseRow != null)
            {
                clauseRow.EditClauseCommand = new RelayCommand(() => EditClause(clauseRow));
                clauseRow.RemoveClauseCommand = new RelayCommand(() => RemoveClause(clauseRow));
            }

            return clauseRow;
        }

        private void EditClause(ClauseRowViewModel row)
        {
            // Find the ItemConfig
            if (_parentViewModel.ItemConfigs.TryGetValue(row.ItemKey, out var config))
            {
                // Create and show the ItemConfigPopup through the parent ViewModel
                var popupViewModel = new ItemConfigPopupViewModel(config);

                // Set item details for display
                popupViewModel.ItemName = row.DisplayText;
                // Note: ItemImage would need to be loaded from sprite service if needed

                // Subscribe to config applied event to refresh display
                popupViewModel.ConfigApplied += (updatedConfig) =>
                {
                    // Update the ItemConfig in parent's collection
                    _parentViewModel.ItemConfigs[row.ItemKey] = updatedConfig;

                    // Refresh the clause display when config is applied
                    RefreshClauseDisplay();
                    DebugLogger.Log("ValidateFilterTab", $"Updated config for clause: {row.ItemKey}");
                };

                // Show popup through parent's CurrentPopup property
                _parentViewModel.CurrentPopup = popupViewModel;

                DebugLogger.Log("ValidateFilterTab", $"Opened config popup for clause: {row.ItemKey}");
            }
            else
            {
                DebugLogger.LogError("ValidateFilterTab", $"ItemConfig not found for clause: {row.ItemKey}");
            }
        }

        private void RemoveClause(ClauseRowViewModel row)
        {
            // Check if we're removing from VisualBuilderTab's collections
            if (_parentViewModel.VisualBuilderTab is VisualBuilderTabViewModel visualVm)
            {
                // Remove from Must collection
                if (MustClauses.Contains(row))
                {
                    MustClauses.Remove(row);

                    // Find and remove from parent's SelectedMust
                    var itemToRemove = visualVm.SelectedMust.FirstOrDefault(item => item.ItemKey == row.ItemKey);
                    if (itemToRemove != null)
                    {
                        visualVm.SelectedMust.Remove(itemToRemove);
                        DebugLogger.Log("ValidateFilterTab", $"Removed clause from Must: {row.ItemKey}");
                    }
                }
                // Remove from MustNot collection
                else if (MustNotClauses.Contains(row))
                {
                    MustNotClauses.Remove(row);

                    // MustNot items are typically in BannedItems operator - need special handling
                    foreach (var item in visualVm.SelectedMust.OfType<Models.FilterOperatorItem>())
                    {
                        if (item.OperatorType == "BannedItems")
                        {
                            var childToRemove = item.Children.FirstOrDefault(child => child.ItemKey == row.ItemKey);
                            if (childToRemove != null)
                            {
                                item.Children.Remove(childToRemove);
                                DebugLogger.Log("ValidateFilterTab", $"Removed clause from BannedItems: {row.ItemKey}");
                                break;
                            }
                        }
                    }
                }
                // Remove from Should collection
                else if (ShouldClauses.Contains(row))
                {
                    ShouldClauses.Remove(row);

                    // Find and remove from parent's SelectedShould
                    var itemToRemove = visualVm.SelectedShould.FirstOrDefault(item => item.ItemKey == row.ItemKey);
                    if (itemToRemove != null)
                    {
                        visualVm.SelectedShould.Remove(itemToRemove);
                        DebugLogger.Log("ValidateFilterTab", $"Removed clause from Should: {row.ItemKey}");
                    }
                }
            }
            else
            {
                // Fallback for key-based collections
                if (MustClauses.Contains(row))
                {
                    MustClauses.Remove(row);
                    _parentViewModel.SelectedMust.Remove(row.ItemKey);
                    DebugLogger.Log("ValidateFilterTab", $"Removed clause from Must (key-based): {row.ItemKey}");
                }
                else if (MustNotClauses.Contains(row))
                {
                    MustNotClauses.Remove(row);
                    _parentViewModel.SelectedMustNot.Remove(row.ItemKey);
                    DebugLogger.Log("ValidateFilterTab", $"Removed clause from MustNot (key-based): {row.ItemKey}");
                }
                else if (ShouldClauses.Contains(row))
                {
                    ShouldClauses.Remove(row);
                    _parentViewModel.SelectedShould.Remove(row.ItemKey);
                    DebugLogger.Log("ValidateFilterTab", $"Removed clause from Should (key-based): {row.ItemKey}");
                }
            }

            DebugLogger.Log("ValidateFilterTab", $"Completed removal of clause: {row.ItemKey}");
        }

        #region Command Implementations

        [RelayCommand(CanExecute = nameof(CanSave))]
        private async Task SaveCurrentFilter()
        {
            try
            {
                // Check if filter has been validated
                if (!IsFilterVerified)
                {
                    var result = await ShowSaveWarningDialog();
                    if (!result) return; // User cancelled
                }

                if (string.IsNullOrWhiteSpace(FilterName))
                {
                    UpdateStatus("Please enter a filter name", true);
                    return;
                }

                var config = BuildConfigFromCurrentState();
                config.Name = FilterName;
                config.Description = FilterDescription;

                // Store example seed if we have one
                if (!string.IsNullOrEmpty(ExampleSeedForPreview))
                {
                    // TODO: Add ExampleSeed property to MotelyJsonConfig
                    // config.ExampleSeed = ExampleSeedForPreview;
                }

                // Generate proper filename in JsonItemFilters folder
                var filePath = _filterService.GenerateFilterFileName(FilterName);
                var success = await _configurationService.SaveFilterAsync(filePath, config);

                if (success)
                {
                    CurrentFileName = Path.GetFileName(filePath);
                    LastModified = DateTime.Now.ToString("yyyy-MM-dd HH:mm");
                    UpdateStatus($"✓ Filter saved: {CurrentFileName}", false);
                    DebugLogger.Log("ValidateFilterTab", $"Filter saved to: {filePath}");
                }
                else
                {
                    UpdateStatus("Failed to save filter", true);
                }
            }
            catch (Exception ex)
            {
                UpdateStatus($"Save error: {ex.Message}", true);
                DebugLogger.LogError("ValidateFilterTab", $"Error saving filter: {ex.Message}");
            }
        }

        private async Task<bool> ShowSaveWarningDialog()
        {
            // TODO: Implement proper dialog when modal system is available
            // For now, just return true to proceed
            HasValidationWarnings = true;
            ValidationWarningText = "This filter has not been tested. It may not match any seeds.";

            // Simulate user confirmation
            await Task.Delay(100);
            return true;
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
                }
                else
                {
                    UpdateStatus("Failed to save filter", true);
                }
            }
            catch (Exception ex)
            {
                UpdateStatus($"Save As error: {ex.Message}", true);
                DebugLogger.LogError("ValidateFilterTab", $"Error in Save As: {ex.Message}");
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

                // Export to desktop as JSON
                var desktopPath = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
                var exportFileName = $"{config.Name}_{DateTime.Now:yyyyMMdd_HHmmss}.json";
                var exportPath = Path.Combine(desktopPath, exportFileName);

                // Use custom serializer to include mode and preserve score formatting
                var userProfileService = ServiceHelper.GetService<UserProfileService>();
                var serializer = new FilterSerializationService(userProfileService!);
                var json = serializer.SerializeConfig(config);
                await File.WriteAllTextAsync(exportPath, json);

                UpdateStatus($"✅ Exported to Desktop: {exportFileName}", false);
                DebugLogger.Log("ValidateFilterTab", $"Filter exported to: {exportPath}");
            }
            catch (Exception ex)
            {
                UpdateStatus($"Export error: {ex.Message}", true);
                DebugLogger.LogError("ValidateFilterTab", $"Error exporting: {ex.Message}");
            }
        }

        private bool CanSave()
        {
            return !string.IsNullOrWhiteSpace(FilterName);
        }

        [RelayCommand]
        private async Task TestFilter()
        {
            try
            {
                // Update to live search state
                IsLiveSearchActive = true;
                LiveSearchProgress = 0;
                LiveSearchStatus = "Initializing search...";
                LiveSearchMatchCount = 0;

                // Reset test states
                IsTestRunning = true;
                ShowTestSuccess = false;
                ShowTestError = false;
                ShowFoundSeed = false;
                TestResultMessage = "";
                FoundSeed = "";
                SeedsChecked = 0;
                TestElapsedTime = 0.0;

                // Build the filter configuration from current selections
                var config = BuildConfigFromCurrentState();

                // Validate the filter name
                if (string.IsNullOrWhiteSpace(config?.Name))
                {
                    IsTestRunning = false;
                    IsLiveSearchActive = false;
                    ShowTestError = true;
                    TestResultMessage = "Please enter a filter name before testing";
                    UpdateStatus("Please enter a filter name before testing", true);
                    return;
                }

                // Derive deck/stake from parent selections
                var deckName = GetDeckName(_parentViewModel.SelectedDeckIndex);
                var stakeName = GetStakeName(_parentViewModel.SelectedStakeIndex);

                // Build test search criteria
                // NOTE: We search 1 BILLION seeds since the system can handle millions per second
                // BatchSize 3 = 35^3 = 42,875 seeds per batch
                // 25,000 batches = 1,071,875,000 seeds (over 1 billion!)
                var criteria = new BalatroSeedOracle.Models.SearchCriteria
                {
                    BatchSize = 3, // 35^3 = 42,875 seeds per batch
                    StartBatch = 0,
                    EndBatch = 25000, // 25K batches = ~1.07 BILLION seeds tested!
                    Deck = deckName,
                    Stake = stakeName,
                    MinScore = 0,
                    MaxResults = 10, // Find up to 10 matching seeds
                };

                // Create progress callback for live feedback
                var totalSeeds = 1_071_875_000; // 1.07 billion seeds
                var progress = new Progress<(int seedsChecked, int matchCount)>(p =>
                {
                    LiveSearchProgress = (p.seedsChecked / (double)totalSeeds) * 100;
                    LiveSearchStatus = $"Searched {p.seedsChecked:N0} / {totalSeeds:N0} seeds ({(p.seedsChecked / 1_000_000.0):F1}M seeds)";
                    LiveSearchMatchCount = p.matchCount;
                    SeedsChecked = p.seedsChecked;
                });

                // Start search via SearchManager
                var searchManager = ServiceHelper.GetService<BalatroSeedOracle.Services.SearchManager>();
                if (searchManager == null)
                {
                    IsTestRunning = false;
                    IsLiveSearchActive = false;
                    ShowTestError = true;
                    TestResultMessage = "SearchManager not available";
                    UpdateStatus("SearchManager not available", true);
                    return;
                }

                // Run quick search with in-memory config
                UpdateStatus($"Testing '{config.Name}' on {deckName} deck, {stakeName} stake...", false);
                DebugLogger.Log("ValidateFilterTab", $"🚀 STARTING MASSIVE SEARCH: Testing 1,071,875,000 seeds!");
                DebugLogger.Log("ValidateFilterTab", $"Search parameters: Deck={deckName}, Stake={stakeName}, Batches={criteria.EndBatch}");

                var results = await searchManager.RunQuickSearchAsync(criteria, config);

                DebugLogger.Log("ValidateFilterTab", $"✅ SEARCH COMPLETE: Checked {SeedsChecked:N0} seeds in {results.ElapsedTime:F1} seconds");
                DebugLogger.Log("ValidateFilterTab", $"Results: Found {results.Count} matching seeds");

                // Update UI with results
                IsTestRunning = false;
                IsLiveSearchActive = false;
                TestElapsedTime = results.ElapsedTime;

                if (results.Success && results.Count > 0)
                {
                    // SUCCESS STATE
                    ShowTestSuccess = true;
                    ShowFoundSeed = true;
                    IsFilterVerified = true;

                    // Extract and store the seed
                    if (results.Seeds != null && results.Seeds.Count > 0)
                    {
                        FoundSeed = results.Seeds[0].ToString() ?? "Unknown";
                        ExampleSeedForPreview = FoundSeed; // Store for preview
                    }

                    TestResultMessage = $"✓ VERIFIED - Found matching seed in {results.ElapsedTime:F1}s";
                    UpdateStatus($"✓ Filter verified: Found seed {FoundSeed}", false);

                    // Clear any validation warnings
                    HasValidationWarnings = false;
                    ValidationWarningText = "";
                }
                else if (results.Success && results.Count == 0)
                {
                    // NO MATCH STATE
                    ShowTestError = true;
                    TestResultMessage = $"⚠ NO MATCHES FOUND in {results.ElapsedTime:F1}s\nTry different deck/stake or wider search";
                    UpdateStatus($"⚠ No matching seeds found (searched {criteria.EndBatch} batches)", true);

                    HasValidationWarnings = true;
                    ValidationWarningText = "No matching seeds found. This filter may be too restrictive.";
                }
                else
                {
                    // ERROR STATE
                    ShowTestError = true;
                    TestResultMessage = $"❌ Test failed: {results.Error}";
                    UpdateStatus($"Test failed: {results.Error}", true);
                }
            }
            catch (Exception ex)
            {
                IsTestRunning = false;
                IsLiveSearchActive = false;
                ShowTestError = true;
                TestResultMessage = $"❌ Error: {ex.Message}";
                UpdateStatus($"Error testing filter: {ex.Message}", true);
                DebugLogger.LogError("ValidateFilterTab", $"Test filter error: {ex.Message}");
            }
        }

        [RelayCommand]
        private async Task CopySeed()
        {
            try
            {
                if (!string.IsNullOrWhiteSpace(FoundSeed))
                {
                    await ClipboardService.CopyToClipboardAsync(FoundSeed);
                    UpdateStatus($"✓ Copied seed {FoundSeed} to clipboard", false);
                    DebugLogger.Log("ValidateFilterTab", $"Copied seed to clipboard: {FoundSeed}");
                }
            }
            catch (Exception ex)
            {
                UpdateStatus($"Failed to copy seed: {ex.Message}", true);
                DebugLogger.LogError("ValidateFilterTab", $"Error copying seed: {ex.Message}");
            }
        }

        #endregion

        #region Helper Methods

        private MotelyJsonConfig BuildConfigFromCurrentState()
        {
            var userProfileService = ServiceHelper.GetService<UserProfileService>();
            var config = new MotelyJsonConfig
            {
                Deck = "Red", // Default deck
                Stake = "White", // Default stake
                Name = string.IsNullOrEmpty(FilterName) ? "Untitled Filter" : FilterName,
                Description = FilterDescription,
                DateCreated = DateTime.UtcNow,
                Author = userProfileService?.GetAuthorName() ?? "Unknown",
            };

            config.Must = new List<MotelyJsonConfig.MotleyJsonFilterClause>();
            config.Should = new List<MotelyJsonConfig.MotleyJsonFilterClause>();
            config.MustNot = new List<MotelyJsonConfig.MotleyJsonFilterClause>();

            // Build from VisualBuilderTab if available
            if (_parentViewModel.VisualBuilderTab is VisualBuilderTabViewModel visualVm)
            {
                // Convert FilterItem objects directly
                foreach (var item in visualVm.SelectedMust)
                {
                    // SPECIAL HANDLING: BannedItems operator → MustNot[]
                    if (item is Models.FilterOperatorItem operatorItem &&
                        operatorItem.OperatorType == "BannedItems")
                    {
                        // Add each child to MustNot array
                        foreach (var child in operatorItem.Children)
                        {
                            var itemConfig = _parentViewModel.ItemConfigs.ContainsKey($"{child.Category}:{child.Name}")
                                ? _parentViewModel.ItemConfigs[$"{child.Category}:{child.Name}"]
                                : new ItemConfig();
                            var clause = _clauseConversionService.ConvertFilterItemToClause(child, itemConfig);
                            if (clause != null)
                                config.MustNot.Add(clause);
                        }
                    }
                    else
                    {
                        var itemConfig = _parentViewModel.ItemConfigs.ContainsKey($"{item.Category}:{item.Name}")
                            ? _parentViewModel.ItemConfigs[$"{item.Category}:{item.Name}"]
                            : new ItemConfig();
                        var clause = _clauseConversionService.ConvertFilterItemToClause(item, itemConfig);
                        if (clause != null)
                            config.Must.Add(clause);
                    }
                }

                foreach (var item in visualVm.SelectedShould)
                {
                    var itemConfig = _parentViewModel.ItemConfigs.ContainsKey($"{item.Category}:{item.Name}")
                        ? _parentViewModel.ItemConfigs[$"{item.Category}:{item.Name}"]
                        : new ItemConfig();
                    var clause = _clauseConversionService.ConvertFilterItemToClause(item, itemConfig);
                    if (clause != null)
                        config.Should.Add(clause);
                }
            }
            else
            {
                // Fallback to parent's collections
                return _filterConfigurationService.BuildConfigFromSelections(
                    _parentViewModel.SelectedMust.ToList(),
                    _parentViewModel.SelectedShould.ToList(),
                    _parentViewModel.SelectedMustNot.ToList(),
                    _parentViewModel.ItemConfigs,
                    FilterName,
                    FilterDescription
                );
            }

            return config;
        }


        private string GetDeckName(int index)
        {
            var deckNames = new[]
            {
                "Red", "Blue", "Yellow", "Green", "Black", "Magic", "Nebula", "Ghost",
                "Abandoned", "Checkered", "Zodiac", "Painted", "Anaglyph", "Plasma", "Erratic",
            };
            return index >= 0 && index < deckNames.Length ? deckNames[index] : "Red";
        }

        private string GetStakeName(int index)
        {
            var stakeNames = new[]
            {
                "white", "red", "green", "black", "blue", "purple", "orange", "gold",
            };
            return index >= 0 && index < stakeNames.Length ? stakeNames[index] : "white";
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
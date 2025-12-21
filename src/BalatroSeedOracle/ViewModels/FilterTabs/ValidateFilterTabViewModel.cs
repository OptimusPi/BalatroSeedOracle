using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Avalonia.Media;
using Avalonia.Threading;
using Avalonia.Platform.Storage;
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

            // Refresh clause display when ViewModel is created (fire-and-forget in constructor)
            // Callers should explicitly await RefreshClauseDisplay() if they need synchronous initialization
            _ = RefreshClauseDisplay();
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
                    DebugLogger.Log(
                        "ValidateFilterTab",
                        $"Pre-filled from LoadedConfig: {FilterName}"
                    );
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

                    DebugLogger.Log(
                        "ValidateFilterTab",
                        $"Pre-filled from parent state: {FilterName}"
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
        public async Task RefreshClauseDisplay()
        {
            // Properly await InvokeAsync so exceptions propagate and callers can show loading indicators
            await Dispatcher.UIThread.InvokeAsync(() =>
            {
                try
                {
                    MustClauses.Clear();
                    MustNotClauses.Clear();
                    ShouldClauses.Clear();
                    // Get VisualBuilderTab if available
                    if (_parentViewModel.VisualBuilderTab is VisualBuilderTabViewModel visualVm)
                    {
                        DebugLogger.Log(
                            "ValidateFilterTab",
                            $"✅ FOUND VisualBuilderTab - Refreshing from VisualBuilder: {visualVm.SelectedMust.Count} must, {visualVm.SelectedShould.Count} should"
                        );

                        // Convert Must items
                        foreach (var item in visualVm.SelectedMust)
                        {
                            // Check for BannedItems operator
                            if (
                                item is Models.FilterOperatorItem operatorItem
                                && operatorItem.OperatorType == "BannedItems"
                            )
                            {
                                // Add children to MustNot
                                foreach (var child in operatorItem.Children)
                                {
                                    var config = _parentViewModel.ItemConfigs.ContainsKey(
                                        $"{child.Category}:{child.Name}"
                                    )
                                        ? _parentViewModel.ItemConfigs[
                                            $"{child.Category}:{child.Name}"
                                        ]
                                        : new ItemConfig();
                                    var clause = _clauseConversionService.ConvertFilterItemToClause(
                                        child,
                                        config
                                    );
                                    if (clause != null)
                                    {
                                        var clauseRow =
                                            _clauseConversionService.ConvertToClauseViewModel(
                                                clause,
                                                child.Category,
                                                0
                                            );
                                        if (clauseRow != null)
                                            MustNotClauses.Add(clauseRow);
                                    }
                                }
                            }
                            else
                            {
                                var config = _parentViewModel.ItemConfigs.ContainsKey(
                                    $"{item.Category}:{item.Name}"
                                )
                                    ? _parentViewModel.ItemConfigs[$"{item.Category}:{item.Name}"]
                                    : new ItemConfig();
                                var clause = _clauseConversionService.ConvertFilterItemToClause(
                                    item,
                                    config
                                );
                                if (clause != null)
                                {
                                    var clauseRow =
                                        _clauseConversionService.ConvertToClauseViewModel(
                                            clause,
                                            item.Category,
                                            0
                                        );
                                    if (clauseRow != null)
                                        MustClauses.Add(clauseRow);
                                }
                            }
                        }

                        // Convert Should items
                        foreach (var item in visualVm.SelectedShould)
                        {
                            var config = _parentViewModel.ItemConfigs.ContainsKey(
                                $"{item.Category}:{item.Name}"
                            )
                                ? _parentViewModel.ItemConfigs[$"{item.Category}:{item.Name}"]
                                : new ItemConfig();
                            var clause = _clauseConversionService.ConvertFilterItemToClause(
                                item,
                                config
                            );
                            if (clause != null)
                            {
                                var clauseRow = _clauseConversionService.ConvertToClauseViewModel(
                                    clause,
                                    item.Category,
                                    0
                                );
                                if (clauseRow != null)
                                    ShouldClauses.Add(clauseRow);
                            }
                        }
                    }
                    else
                    {
                        // Fallback to parent's key-based collections
                        DebugLogger.Log(
                            "ValidateFilterTab",
                            $"⚠️ NO VisualBuilderTab found! Falling back to parent collections: {_parentViewModel.SelectedMust.Count()} must, {_parentViewModel.SelectedMustNot.Count()} mustNot"
                        );
                        DebugLogger.Log(
                            "ValidateFilterTab",
                            $"VisualBuilderTab is null: {_parentViewModel.VisualBuilderTab == null}, Type: {_parentViewModel.VisualBuilderTab?.GetType()?.Name ?? "null"}"
                        );

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
                    // Log the error but don't crash the whole tab - let partial results show
                    var errorMsg =
                        $"❌ ERROR refreshing clause display (partial results may be shown):\n\n{ex.Message}\n\nStack Trace:\n{ex.StackTrace}";
                    DebugLogger.LogError("ValidateFilterTab", errorMsg);
                    // Don't re-throw - individual item errors are now handled in ConvertItemConfigDirectly
                }
            });
        }

        private ClauseRowViewModel? ConvertItemConfigDirectly(
            ItemConfig config,
            string itemKey,
            int nestingLevel
        )
        {
            try
            {
                // Extract category and name from item key (format: "category:name")
                var parts = itemKey.Split(':');
                if (parts.Length != 2)
                {
                    DebugLogger.LogError(
                        "ValidateFilterTab",
                        $"Invalid itemKey format '{itemKey}' - expected 'category:name'"
                    );
                    return null;
                }

                var category = parts[0];
                var name = parts[1];

                // Create a FilterItem to pass to the conversion service
                var filterItem = new FilterItem
                {
                    Category = category,
                    Name = name,
                    ItemKey = itemKey,
                };

                // Use the conversion service to create the clause
                var clause = _clauseConversionService.ConvertFilterItemToClause(filterItem, config);
                if (clause == null)
                {
                    DebugLogger.LogError(
                        "ValidateFilterTab",
                        $"ConvertFilterItemToClause returned null for itemKey '{itemKey}' (category: {category}, name: {name})"
                    );
                    return null;
                }

                // Convert the clause to a view model
                var clauseRow = _clauseConversionService.ConvertToClauseViewModel(
                    clause,
                    category,
                    nestingLevel
                );

                if (clauseRow == null)
                {
                    DebugLogger.LogError(
                        "ValidateFilterTab",
                        $"ConvertToClauseViewModel returned null for itemKey '{itemKey}'"
                    );
                    return null;
                }

                // Wire up commands if the row was created successfully
                clauseRow.EditClauseCommand = new RelayCommand(() => EditClause(clauseRow));
                clauseRow.RemoveClauseCommand = new RelayCommand(() => RemoveClause(clauseRow));

                return clauseRow;
            }
            catch (Exception ex)
            {
                // Don't crash the whole tab - just skip this item and log the error
                DebugLogger.LogError(
                    "ValidateFilterTab",
                    $"❌ Failed to convert itemKey '{itemKey}': {ex.Message}"
                );
                DebugLogger.LogError("ValidateFilterTab", $"Stack trace: {ex.StackTrace}");
                return null; // Skip this item, continue with others
            }
        }

        private void EditClause(ClauseRowViewModel row)
        {
            // Find the ItemConfig
            if (_parentViewModel.ItemConfigs.TryGetValue(row.ItemKey, out var config))
            {
                // Create and show the ItemConfigPopup through the parent ViewModel
                // TODO: Item config from ValidateFilterTab not yet implemented
                // Use VisualBuilderTab right-click for item configuration
                return;
            }
            else
            {
                DebugLogger.LogError(
                    "ValidateFilterTab",
                    $"ItemConfig not found for clause: {row.ItemKey}"
                );
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
                    var itemToRemove = visualVm.SelectedMust.FirstOrDefault(item =>
                        item.ItemKey == row.ItemKey
                    );
                    if (itemToRemove != null)
                    {
                        visualVm.SelectedMust.Remove(itemToRemove);
                        DebugLogger.Log(
                            "ValidateFilterTab",
                            $"Removed clause from Must: {row.ItemKey}"
                        );
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
                            var childToRemove = item.Children.FirstOrDefault(child =>
                                child.ItemKey == row.ItemKey
                            );
                            if (childToRemove != null)
                            {
                                item.Children.Remove(childToRemove);
                                DebugLogger.Log(
                                    "ValidateFilterTab",
                                    $"Removed clause from BannedItems: {row.ItemKey}"
                                );
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
                    var itemToRemove = visualVm.SelectedShould.FirstOrDefault(item =>
                        item.ItemKey == row.ItemKey
                    );
                    if (itemToRemove != null)
                    {
                        visualVm.SelectedShould.Remove(itemToRemove);
                        DebugLogger.Log(
                            "ValidateFilterTab",
                            $"Removed clause from Should: {row.ItemKey}"
                        );
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
                    DebugLogger.Log(
                        "ValidateFilterTab",
                        $"Removed clause from Must (key-based): {row.ItemKey}"
                    );
                }
                else if (MustNotClauses.Contains(row))
                {
                    MustNotClauses.Remove(row);
                    _parentViewModel.SelectedMustNot.Remove(row.ItemKey);
                    DebugLogger.Log(
                        "ValidateFilterTab",
                        $"Removed clause from MustNot (key-based): {row.ItemKey}"
                    );
                }
                else if (ShouldClauses.Contains(row))
                {
                    ShouldClauses.Remove(row);
                    _parentViewModel.SelectedShould.Remove(row.ItemKey);
                    DebugLogger.Log(
                        "ValidateFilterTab",
                        $"Removed clause from Should (key-based): {row.ItemKey}"
                    );
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
                    if (!result)
                        return; // User cancelled
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

                // Generate proper filename in JsonFilters folder
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

        [RelayCommand]
        private void FinishAndClose()
        {
            try
            {
                DebugLogger.Log("ValidateFilterTab", "Finish & Close button clicked");
                _parentViewModel.RequestClose?.Invoke();
            }
            catch (Exception ex)
            {
                DebugLogger.LogError("ValidateFilterTab", $"Error closing modal: {ex.Message}");
            }
        }

        [RelayCommand]
        private async Task CopyJson()
        {
            try
            {
                var config = BuildConfigFromCurrentState();
                if (config == null)
                {
                    UpdateStatus("Cannot copy JSON - invalid filter configuration", true);
                    return;
                }

                // Serialize to JSON
                var userProfileService = ServiceHelper.GetService<UserProfileService>();
                var serializer = new FilterSerializationService(userProfileService!);
                var json = serializer.SerializeConfig(config);

                // Copy to clipboard
                await Services.ClipboardService.CopyToClipboardAsync(json);

                UpdateStatus("✅ JSON copied to clipboard", false);
                DebugLogger.Log("ValidateFilterTab", "Filter JSON copied to clipboard");
            }
            catch (Exception ex)
            {
                UpdateStatus($"Copy error: {ex.Message}", true);
                DebugLogger.LogError("ValidateFilterTab", $"Error copying JSON: {ex.Message}");
            }
        }

        [RelayCommand(CanExecute = nameof(CanSave))]
        private async Task GoToSearch()
        {
            try
            {
                // Save the filter first
                await SaveCurrentFilter();

                // Close the filter modal
                _parentViewModel.RequestClose?.Invoke();

                // Open the search modal with this filter
                // Get the filter path from the saved filter name
                var filterPath = $"JsonFilters/{FilterName}.json";

                // TODO: Open search modal with this filter
                // For now, just show success message
                DebugLogger.Log(
                    "ValidateFilterTab",
                    $"Go to Search clicked for filter: {FilterName}"
                );

                // The main menu should open the search modal with the filter loaded
                // This will be handled by the parent when we request close
            }
            catch (Exception ex)
            {
                UpdateStatus($"Error: {ex.Message}", true);
                DebugLogger.LogError("ValidateFilterTab", $"Error going to search: {ex.Message}");
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

                var exportFileName = $"{config.Name}_{DateTime.Now:yyyyMMdd_HHmmss}.json";

#if BROWSER
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
#else
                // Desktop: Export to desktop folder
                var desktopPath = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
                var exportPath = Path.Combine(desktopPath, exportFileName);
                await File.WriteAllTextAsync(exportPath, json);

                UpdateStatus($"✅ Exported to Desktop: {exportFileName}", false);
                DebugLogger.Log("ValidateFilterTab", $"Filter exported to: {exportPath}");
#endif
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

                var searchManager =
                    ServiceHelper.GetService<BalatroSeedOracle.Services.SearchManager>();
                if (searchManager == null)
                {
                    IsTestRunning = false;
                    IsLiveSearchActive = false;
                    ShowTestError = true;
                    TestResultMessage = "SearchManager not available";
                    UpdateStatus("SearchManager not available", true);
                    return;
                }

                // PHASE 1: Ultra-quick sanity check (1 batch = 35 seeds)
                LiveSearchStatus = "Phase 1: Quick sanity check (35 seeds)...";
                var phase1Criteria = new BalatroSeedOracle.Models.SearchCriteria
                {
                    BatchSize = 1, // 1 character = 35 seeds
                    StartBatch = 0,
                    EndBatch = 1, // Search batches 0-1 (1 batch = 35 seeds)
                    Deck = deckName,
                    Stake = stakeName,
                    MinScore = 0,
                    MaxResults = 20, // Detect if too permissive (>10)
                };

                DebugLogger.Log("ValidateFilterTab", "🚀 PHASE 1: Testing 35 seeds (sanity check)");
                var phase1Results = await searchManager.RunQuickSearchAsync(phase1Criteria, config);

                if (phase1Results.Success && phase1Results.Count > 0)
                {
                    // Found seeds in first 35! Check if too permissive
                    if (phase1Results.Count > 10)
                    {
                        HasValidationWarnings = true;
                        ValidationWarningText =
                            $"⚠️ Filter found {phase1Results.Count} seeds in first 35! This filter is very permissive.";
                        DebugLogger.Log(
                            "ValidateFilterTab",
                            $"⚠️ PHASE 1: Too permissive - found {phase1Results.Count} seeds"
                        );
                    }

                    // Still mark as verified
                    IsTestRunning = false;
                    IsLiveSearchActive = false;
                    ShowTestSuccess = true;
                    ShowFoundSeed = true;
                    IsFilterVerified = true;

                    if (phase1Results.Seeds != null && phase1Results.Seeds.Count > 0)
                    {
                        FoundSeed = phase1Results.Seeds[0].ToString() ?? "Unknown";
                        ExampleSeedForPreview = FoundSeed;
                    }

                    TestResultMessage = $"✓ VERIFIED - Found {phase1Results.Count} seeds instantly";
                    UpdateStatus($"✓ Filter verified: Found seed {FoundSeed}", false);
                    return;
                }

                DebugLogger.Log(
                    "ValidateFilterTab",
                    "✅ PHASE 1: No seeds found (good - filter not too permissive)"
                );

                // PHASE 2: Progressive escalation with batch size 1, CPU-1 threads
                // Use CPU count - 1 for threads
                var cpuCount = Environment.ProcessorCount;
                var threadCount = Math.Max(1, cpuCount - 1);
                DebugLogger.Log(
                    "ValidateFilterTab",
                    $"Using {threadCount} threads (CPU count: {cpuCount})"
                );

                // Sub-phase 2a: Try 1,000 batches (35K seeds)
                LiveSearchStatus = "Phase 2a: Searching 1,000 batches (35K seeds)...";
                LiveSearchProgress = 10;
                var phase2aCriteria = new BalatroSeedOracle.Models.SearchCriteria
                {
                    BatchSize = 1,
                    StartBatch = 0,
                    EndBatch = 1000,
                    Deck = deckName,
                    Stake = stakeName,
                    MinScore = 0,
                    MaxResults = 1, // Stop at first match
                };

                DebugLogger.Log(
                    "ValidateFilterTab",
                    "🚀 PHASE 2a: Testing 1,000 batches (35K seeds)"
                );
                var phase2aResults = await searchManager.RunQuickSearchAsync(
                    phase2aCriteria,
                    config
                );

                if (phase2aResults.Success && phase2aResults.Count > 0)
                {
                    ShowTestSuccess = true;
                    ShowFoundSeed = true;
                    IsFilterVerified = true;
                    FoundSeed = phase2aResults.Seeds?[0].ToString() ?? "Unknown";
                    ExampleSeedForPreview = FoundSeed;
                    TestResultMessage =
                        $"✓ VERIFIED - Found seed in {phase2aResults.ElapsedTime:F1}s";
                    UpdateStatus($"✓ Filter verified: {FoundSeed}", false);
                    IsTestRunning = false;
                    IsLiveSearchActive = false;
                    return;
                }

                // Sub-phase 2b: Try 100,000 batches (3.5M seeds)
                LiveSearchStatus = "Phase 2b: Searching 100,000 batches (3.5M seeds)...";
                LiveSearchProgress = 30;
                var phase2bCriteria = new BalatroSeedOracle.Models.SearchCriteria
                {
                    BatchSize = 1,
                    StartBatch = 0,
                    EndBatch = 100000,
                    Deck = deckName,
                    Stake = stakeName,
                    MinScore = 0,
                    MaxResults = 1,
                };

                DebugLogger.Log(
                    "ValidateFilterTab",
                    "🚀 PHASE 2b: Testing 100,000 batches (3.5M seeds)"
                );
                var phase2bResults = await searchManager.RunQuickSearchAsync(
                    phase2bCriteria,
                    config
                );

                if (phase2bResults.Success && phase2bResults.Count > 0)
                {
                    ShowTestSuccess = true;
                    ShowFoundSeed = true;
                    IsFilterVerified = true;
                    FoundSeed = phase2bResults.Seeds?[0].ToString() ?? "Unknown";
                    ExampleSeedForPreview = FoundSeed;
                    TestResultMessage =
                        $"✓ VERIFIED - Found seed in {phase2bResults.ElapsedTime:F1}s";
                    UpdateStatus($"✓ Filter verified: {FoundSeed}", false);
                    IsTestRunning = false;
                    IsLiveSearchActive = false;
                    return;
                }

                // Sub-phase 2c: Continue from 100K + 250 more batches
                LiveSearchStatus = "Phase 2c: Searching batches 100K-100.25K...";
                LiveSearchProgress = 50;
                var phase2cCriteria = new BalatroSeedOracle.Models.SearchCriteria
                {
                    BatchSize = 1,
                    StartBatch = 100000,
                    EndBatch = 100250,
                    Deck = deckName,
                    Stake = stakeName,
                    MinScore = 0,
                    MaxResults = 1,
                };

                DebugLogger.Log("ValidateFilterTab", "🚀 PHASE 2c: Testing batches 100K-100.25K");
                var phase2cResults = await searchManager.RunQuickSearchAsync(
                    phase2cCriteria,
                    config
                );

                if (phase2cResults.Success && phase2cResults.Count > 0)
                {
                    ShowTestSuccess = true;
                    ShowFoundSeed = true;
                    IsFilterVerified = true;
                    FoundSeed = phase2cResults.Seeds?[0].ToString() ?? "Unknown";
                    ExampleSeedForPreview = FoundSeed;
                    TestResultMessage =
                        $"✓ VERIFIED - Found seed in {phase2cResults.ElapsedTime:F1}s";
                    UpdateStatus($"✓ Filter verified: {FoundSeed}", false);
                    IsTestRunning = false;
                    IsLiveSearchActive = false;
                    return;
                }

                // Sub-phase 2d: Final push - 1 million batches (35M seeds, 2-30 seconds)
                LiveSearchStatus = "Phase 2d: Searching 1M batches (35M seeds)...";
                LiveSearchProgress = 70;
                var phase2dCriteria = new BalatroSeedOracle.Models.SearchCriteria
                {
                    BatchSize = 1,
                    StartBatch = 0,
                    EndBatch = 1000000,
                    Deck = deckName,
                    Stake = stakeName,
                    MinScore = 0,
                    MaxResults = 1,
                };

                DebugLogger.Log(
                    "ValidateFilterTab",
                    "🚀 PHASE 2d: Testing 1M batches (35M seeds) - FINAL PUSH"
                );
                var phase2dResults = await searchManager.RunQuickSearchAsync(
                    phase2dCriteria,
                    config
                );

                LiveSearchProgress = 100;
                IsTestRunning = false;
                IsLiveSearchActive = false;

                if (phase2dResults.Success && phase2dResults.Count > 0)
                {
                    ShowTestSuccess = true;
                    ShowFoundSeed = true;
                    IsFilterVerified = true;
                    FoundSeed = phase2dResults.Seeds?[0].ToString() ?? "Unknown";
                    ExampleSeedForPreview = FoundSeed;
                    TestResultMessage =
                        $"✓ VERIFIED - Found seed in {phase2dResults.ElapsedTime:F1}s";
                    UpdateStatus($"✓ Filter verified: {FoundSeed}", false);
                }
                else if (phase2dResults.Success && phase2dResults.Count == 0)
                {
                    // NO MATCH after all phases
                    ShowTestError = true;
                    TestResultMessage =
                        $"⚠ NO SEEDS FOUND\nSearched 35M seeds in {phase2dResults.ElapsedTime:F1}s\nFilter may be too restrictive";
                    UpdateStatus($"⚠ No seeds found in 35M seeds", true);
                    HasValidationWarnings = true;
                    ValidationWarningText =
                        "No matching seeds found. Try making filter less restrictive.";
                }
                else
                {
                    // ERROR STATE
                    ShowTestError = true;
                    TestResultMessage = $"❌ Test failed: {phase2dResults.Error}";
                    UpdateStatus($"Test failed: {phase2dResults.Error}", true);
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
            // MUST run on UI thread to access ObservableCollections
            return Dispatcher.UIThread.Invoke(() =>
            {
                // Use the parent's robust implementation if possible
                var config = _parentViewModel.BuildConfigFromCurrentState();

                // Override name and description from this tab's inputs
                config.Name = string.IsNullOrEmpty(FilterName) ? "Untitled Filter" : FilterName;
                config.Description = FilterDescription;

                return config;
            });
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
            DebugLogger.Log("ValidateFilterTab", $"Status: {message} (Error: {isError})");
        }


        #endregion
    }
}

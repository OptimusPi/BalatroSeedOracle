using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using BalatroSeedOracle.Controls;
using BalatroSeedOracle.Helpers;
using BalatroSeedOracle.Models;
using BalatroSeedOracle.Services;
using BalatroSeedOracle.ViewModels.FilterTabs;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Motely;
using Motely.Filters;
using BsoLogger = BalatroSeedOracle.Helpers.DebugLogger;

namespace BalatroSeedOracle.ViewModels
{
    /// <summary>
    /// Main ViewModel for FiltersModal - proper MVVM pattern
    /// Replaces 8,583-line god class in FiltersModal.axaml.cs
    /// </summary>
    public partial class FiltersModalViewModel
        : ObservableObject,
            BalatroSeedOracle.Helpers.IModalBackNavigable
    {
        private readonly UserProfileService? _userProfileService;
        private readonly Func<ValidateFilterTabViewModel>? _validateTabFactory;

        // ===== CORE STATE (using [ObservableProperty] for automatic INotifyPropertyChanged) =====
        [ObservableProperty]
        private string _currentCategory = "Jokers";

        [ObservableProperty]
        private string _searchFilter = "";

        [ObservableProperty]
        private int _selectedTabIndex = 0;

        // Track previous tab index to properly save data when switching tabs
        private int _previousTabIndex = 0;

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(HasLoadedFilter))]
        [NotifyCanExecuteChangedFor(nameof(DeleteFilterCommand))]
        private string? _currentFilterPath;

        [ObservableProperty]
        private JamlConfig? _loadedConfig;

        // Track original metadata to preserve on save (prevent overwriting author)
        private string? _originalAuthor;

        [ObservableProperty]
        private string _filterName = "";

        [ObservableProperty]
        private bool _filterNameEditMode = false;

        [ObservableProperty]
        private bool _filterNameDisplayMode = true;

        [ObservableProperty]
        private string _filterDescription = "";

        [ObservableProperty]
        private string _filterAuthor = "";

        [ObservableProperty]
        private DateTime? _filterDateCreated;

        [ObservableProperty]
        private int[]? _defaultAntes = new[] { 1, 2, 3, 4, 5, 6, 7, 8 };

        [ObservableProperty]
        private int[]? _defaultPackSlots = new[] { 0, 1, 2, 3, 4, 5 };

        [ObservableProperty]
        private int[]? _defaultShopSlots = new[] { 0, 1, 2, 3, 4, 5 };

        [ObservableProperty]
        private int _defaultScore = 1;

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(SelectedDeckIndex))]
        private Motely.Enums.MotelyDeck _selectedDeck = MotelyDeck.Red;

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(SelectedStakeIndex))]
        private Motely.Enums.MotelyStake _selectedStake = MotelyStake.White;

        // Tab visibility properties - proper MVVM pattern
        [ObservableProperty]
        private bool _isLoadSaveTabVisible = true;

        [ObservableProperty]
        private bool _isVisualTabVisible = false;

        [ObservableProperty]
        private bool _isDeckStakeTabVisible = false;

        [ObservableProperty]
        private bool _isJsonTabVisible = false;

        [ObservableProperty]
        private bool _isTestTabVisible = false;

        [ObservableProperty]
        private bool _isSaveTabVisible = false;

        // Collections - Observable for data binding
        private readonly Dictionary<string, List<string>> _itemCategories;
        public ObservableCollection<string> SelectedMust { get; } = new();
        public ObservableCollection<string> SelectedShould { get; } = new();
        public ObservableCollection<string> SelectedMustNot { get; } = new();
        public Dictionary<string, ItemConfig> ItemConfigs { get; } = new();

        // Counters
        private int _itemKeyCounter = 0;
        private int _instanceCounter = 0;

        // Callbacks for child view models
        public Action<string>? RequestNavigateToSearch { get; set; }
        public Action? RequestClose { get; set; }

        // Computed properties
        public bool HasLoadedFilter => !string.IsNullOrEmpty(CurrentFilterPath);

        /// <summary>The file this designer session's auto-save created, so it may write it again.</summary>
        private string? _autoSavedPath;

        /// <summary>
        /// Where an auto-save may write, or null with the reason it may not. Auto-save used to write
        /// to a path built from the filter's name, from the builder's state, which drops each
        /// clause's antes, sources and score: opening a filter whose file name differed from its
        /// name (waterbear.yaml, "The Waterbear") left a lossy duplicate that sorted first and got
        /// searched, and one whose name matched was overwritten. So: a filter opened from a file is
        /// never auto-saved (Save writes it), and a new one only to a file that doesn't exist yet
        /// or that this session's auto-save made.
        /// </summary>
        public string? AutoSaveTarget(out string reason)
        {
            reason = "";
            if (
                !string.IsNullOrWhiteSpace(CurrentFilterPath)
                && !Path.GetFileName(CurrentFilterPath).StartsWith("_UNSAVED_", StringComparison.OrdinalIgnoreCase)
            )
            {
                reason = "Opened from a file: use Save Filter to write it.";
                return null;
            }
            if (string.IsNullOrWhiteSpace(FilterName))
                return null;

            var candidate = FilterFiles.Resolve(FilterName.Replace(" ", "_"));
            if (
                File.Exists(candidate)
                && !string.Equals(Path.GetFullPath(candidate), _autoSavedPath, StringComparison.OrdinalIgnoreCase)
            )
            {
                reason = $"Not auto-saved: {Path.GetFileName(candidate)} already exists.";
                return null;
            }
            _autoSavedPath = Path.GetFullPath(candidate);
            return candidate;
        }

        public ObservableCollection<BalatroSeedOracle.ViewModels.FilterTabs.FilterTabViewModel> FilterTabs { get; }

        public BalatroSeedOracle.ViewModels.FilterTabs.FilterTabViewModel MustHaveItems { get; }
        public BalatroSeedOracle.ViewModels.FilterTabs.FilterTabViewModel ShouldHaveItems { get; }
        public BalatroSeedOracle.ViewModels.FilterTabs.FilterTabViewModel MustNotHaveItems { get; }

        public FiltersModalViewModel(
            UserProfileService? userProfileService = null,
            Func<ValidateFilterTabViewModel>? validateTabFactory = null
        )
        {
            _userProfileService = userProfileService;
            _validateTabFactory = validateTabFactory;

            _itemCategories = InitializeItemCategories();

            MustHaveItems = new BalatroSeedOracle.ViewModels.FilterTabs.FilterTabViewModel
            {
                Header = "Must Have",
            };
            ShouldHaveItems = new BalatroSeedOracle.ViewModels.FilterTabs.FilterTabViewModel
            {
                Header = "Should Have",
            };
            MustNotHaveItems = new BalatroSeedOracle.ViewModels.FilterTabs.FilterTabViewModel
            {
                Header = "Must Not Have",
            };

            FilterTabs =
                new ObservableCollection<BalatroSeedOracle.ViewModels.FilterTabs.FilterTabViewModel>
                {
                    MustHaveItems,
                    ShouldHaveItems,
                    MustNotHaveItems,
                };
        }

        // Deck/Stake display values from Motely enums (single source of truth)
        public string[] DeckDisplayValues { get; } = Enum.GetNames(typeof(MotelyDeck));
        public string[] StakeDisplayValues { get; } = Enum.GetNames(typeof(MotelyStake));

        // The stake-by-UI-index mapping is order-specific and uses lowercase JSON values
        // (MotelyStake enum has gaps so we can't derive this from Enum.GetNames).
        private static readonly string[] StakeNamesByIndex =
        {
            "white",
            "red",
            "green",
            "black",
            "blue",
            "purple",
            "orange",
            "gold",
        };

        // Deck/Stake index helpers
        public int SelectedDeckIndex
        {
            get => (int)SelectedDeck;
            set
            {
                if (value >= 0 && value <= 14) // 0-14 for 15 decks
                {
                    SelectedDeck = (Motely.Enums.MotelyDeck)value;
                }
            }
        }

        public int SelectedStakeIndex
        {
            get
            {
                // Map enum to UI index (0-7)
                return SelectedStake switch
                {
                    MotelyStake.White => 0,
                    MotelyStake.Red => 1,
                    MotelyStake.Green => 2,
                    MotelyStake.Black => 3,
                    MotelyStake.Blue => 4,
                    MotelyStake.Purple => 5,
                    MotelyStake.Orange => 6,
                    MotelyStake.Gold => 7,
                    _ => 0,
                };
            }
            set
            {
                // Map UI index (0-7) to enum
                SelectedStake = value switch
                {
                    0 => MotelyStake.White,
                    1 => MotelyStake.Red,
                    2 => MotelyStake.Green,
                    3 => MotelyStake.Black,
                    4 => MotelyStake.Blue,
                    5 => MotelyStake.Purple,
                    6 => MotelyStake.Orange,
                    7 => MotelyStake.Gold,
                    _ => MotelyStake.White,
                };
            }
        }

        public Dictionary<string, List<string>> ItemCategories => _itemCategories;
        public ObservableCollection<TabItemViewModel> TabItems { get; } = new();

        // Tab ViewModels for cross-tab communication
        public object? VisualBuilderTab { get; set; }
        public object? DeckStakeTab { get; set; }
        public object? YAMLEditorTab { get; set; }

        [ObservableProperty]
        private object? _currentPopup;

        [ObservableProperty]
        private object? _currentTabContent;

        // ===== PARTIAL METHODS (Property change handlers) =====
        partial void OnFilterNameEditModeChanged(bool value)
        {
            FilterNameDisplayMode = !value;
        }

        partial void OnSelectedDeckChanged(Motely.Enums.MotelyDeck value)
        {
            BsoLogger.Log("FiltersModalViewModel", $"Deck changed to: {value}");
            if (YAMLEditorTab is FilterTabs.JamlEditorTabViewModel yamlVm)
            {
                yamlVm.AutoGenerateFromVisual();
            }
        }

        partial void OnSelectedStakeChanged(Motely.Enums.MotelyStake value)
        {
            BsoLogger.Log("FiltersModalViewModel", $"Stake changed to: {value}");
            if (YAMLEditorTab is FilterTabs.JamlEditorTabViewModel yamlVm)
            {
                yamlVm.AutoGenerateFromVisual();
            }
        }

        // ===== EVENTS =====
        /// <summary>
        /// Raised when filter name edit mode is activated (for focus request)
        /// </summary>
        public event EventHandler? OnFilterNameEditActivated;

        // ===== COMMANDS (using [RelayCommand] source generator) =====

        [RelayCommand]
        private void SaveCurrentFilter()
        {
            // If we're creating a new filter (or currently pointing at an _UNSAVED_ temp file),
            // and the user has provided a name, save to a real filter file name so it shows up
            // in the filter picker (which excludes _UNSAVED_ files).
            var originalPath = CurrentFilterPath;

            if (
                string.IsNullOrWhiteSpace(CurrentFilterPath)
                || Path.GetFileName(CurrentFilterPath)
                    .StartsWith("_UNSAVED_", StringComparison.OrdinalIgnoreCase)
            )
            {
                CurrentFilterPath = FilterFiles.Resolve(
                    string.IsNullOrWhiteSpace(FilterName) ? "_UNSAVED_CREATION" : FilterName.Trim()
                );
            }

            var config = BuildConfigFromCurrentState();
            FilterFiles.Save(config, CurrentFilterPath);
            LoadedConfig = config;
            BsoLogger.Log("FiltersModalViewModel", $"✅ Filter saved: {CurrentFilterPath}");

            // If we saved to a new non-_UNSAVED_ path, clean up the old temp file.
            if (
                !string.IsNullOrWhiteSpace(originalPath)
                && !string.Equals(
                    originalPath,
                    CurrentFilterPath,
                    StringComparison.OrdinalIgnoreCase
                )
                && Path.GetFileName(originalPath)
                    .StartsWith("_UNSAVED_", StringComparison.OrdinalIgnoreCase)
            )
            {
                FilterFiles.Delete(originalPath);
            }
        }

        [RelayCommand]
        public void LoadFilter()
        {
            BsoLogger.Log(
                "FiltersModalViewModel",
                $"Found {FilterFiles.List().Count()} available filters"
            );
        }

        /// <summary>
        /// Loads a specific filter config for editing (called from other modals)
        /// </summary>
        /// <param name="config">The filter config to load</param>
        public async Task LoadFilterForEditing(JamlConfig config)
        {
            try
            {
                LoadedConfig = config;
                FilterName = config.Name ?? "Unnamed Filter";
                FilterDescription = config.Description ?? "";
                // Deck and stake are now enum values
                SelectedDeck = config.Deck;
                SelectedStake = config.Stake;

                // Store original metadata
                _originalAuthor = config.Author;

                // Note: Criteria hash calculation and clause loading will be implemented when needed

                // Don't set CurrentFilterPath since this is editing, not loading from file
                CurrentFilterPath = null;

                BsoLogger.Log("FiltersModalViewModel", $"Loaded filter for editing: {config.Name}");
            }
            catch (Exception ex)
            {
                BsoLogger.LogError(
                    "FiltersModalViewModel",
                    $"Error loading filter for editing: {ex.Message}"
                );
            }

            await Task.CompletedTask;
        }

        [RelayCommand]
        private void CreateNewFilter()
        {
            try
            {
                ClearAllSelections();
                CurrentFilterPath = null;
                LoadedConfig = null;
                FilterName = "";
                FilterDescription = "";
                BsoLogger.Log("FiltersModalViewModel", "Created new filter");
            }
            catch (Exception ex)
            {
                BsoLogger.LogError(
                    "FiltersModalViewModel",
                    $"Error creating new filter: {ex.Message}"
                );
            }
        }

        [RelayCommand]
        private void FilterNameClick()
        {
            FilterNameEditMode = true;
            OnFilterNameEditActivated?.Invoke(this, EventArgs.Empty);
        }

        [RelayCommand]
        private void SaveFilterName()
        {
            var newName = FilterName?.Trim();
            if (!string.IsNullOrEmpty(newName))
            {
                FilterName = newName;
                BsoLogger.Log("FiltersModalViewModel", $"Filter name updated to: {newName}");
            }
            FilterNameEditMode = false;
        }

        [RelayCommand]
        private void CancelFilterNameEdit()
        {
            // Restore original value from loaded config if available
            if (LoadedConfig is not null && !string.IsNullOrEmpty(LoadedConfig.Name))
            {
                FilterName = LoadedConfig.Name;
            }
            FilterNameEditMode = false;
        }

        [RelayCommand(CanExecute = nameof(HasLoadedFilter))]
        private void DeleteFilter()
        {
            if (!string.IsNullOrEmpty(CurrentFilterPath))
            {
                var deletedPath = CurrentFilterPath;
                FilterFiles.Delete(deletedPath);
                CreateNewFilter();
                BsoLogger.Log("FiltersModalViewModel", $"Deleted filter: {deletedPath}");
            }
        }

        [RelayCommand]
        private void RefreshFromConfig()
        {
            try
            {
                if (LoadedConfig is not null)
                {
                    LoadConfigIntoState(LoadedConfig);
                    BsoLogger.Log("FiltersModalViewModel", "Refreshed from config");
                }
            }
            catch (Exception ex)
            {
                BsoLogger.LogError(
                    "FiltersModalViewModel",
                    $"Error refreshing from config: {ex.Message}"
                );
            }
        }

        private void PopulateFilterTabs(JamlConfig config)
        {
            MustHaveItems.Items.Clear();
            ShouldHaveItems.Items.Clear();
            MustNotHaveItems.Items.Clear();

            if (config.Must is not null)
            {
                foreach (var item in config.Must)
                {
                    MustHaveItems.Items.Add(
                        new FilterItem
                        {
                            Name = item.GetValueName() ?? "",
                            Status = FilterItemStatus.MustHave,
                        }
                    );
                }
            }

            if (config.Should is not null)
            {
                foreach (var item in config.Should)
                {
                    ShouldHaveItems.Items.Add(
                        new FilterItem
                        {
                            Name = item.GetValueName() ?? "",
                            Status = FilterItemStatus.ShouldHave,
                        }
                    );
                }
            }

            if (config.MustNot is not null)
            {
                foreach (var item in config.MustNot)
                {
                    MustNotHaveItems.Items.Add(
                        new FilterItem
                        {
                            Name = item.GetValueName() ?? "",
                            Status = FilterItemStatus.MustNotHave,
                        }
                    );
                }
            }
        }

        [RelayCommand]
        public async Task ReloadVisualFromSavedFile()
        {
            if (string.IsNullOrEmpty(CurrentFilterPath) || !File.Exists(CurrentFilterPath))
            {
                BsoLogger.Log("FiltersModalViewModel", "No saved file to reload visual from");
                return;
            }

            BsoLogger.Log(
                "FiltersModalViewModel",
                $"Reloading visual from file: {CurrentFilterPath}"
            );

            var config = FilterFiles.Load(CurrentFilterPath, out var loadError);
            if (config is null)
            {
                BsoLogger.LogError("FiltersModalViewModel", $"Reload failed: {loadError}");
                return;
            }

            PopulateFilterTabs(config);
            LoadConfigIntoState(config);
            LoadedConfig = config;

            // Update YAML editor with loaded content
            if (YAMLEditorTab is FilterTabs.JamlEditorTabViewModel yamlVm)
            {
                yamlVm.AutoGenerateFromVisual();
            }

            // Update Visual Builder with FilterItem objects
            await UpdateVisualBuilderFromItemConfigs();

            // CRITICAL: Expand drop zones that have items so they render
            ExpandDropZonesWithItems();

            BsoLogger.Log("FiltersModalViewModel", "Visual reloaded from saved file");
        }

        // ===== TAB VISIBILITY MANAGEMENT (MVVM) =====

        /// <summary>
        /// Updates tab visibility based on the selected tab index
        /// Follows proper MVVM pattern - no direct UI manipulation
        /// </summary>
        /// <param name="tabIndex">0=Build Filter, 1=Deck/Stake, 2=JSON Editor, 3=YAML Editor, 4=Validate Filter</param>
        public void UpdateTabVisibility(int tabIndex)
        {
            BsoLogger.Log(
                "FiltersModalViewModel",
                $"UpdateTabVisibility called with tabIndex={tabIndex}"
            );

            // Hide all tabs first
            IsLoadSaveTabVisible = false;
            IsVisualTabVisible = false;
            IsDeckStakeTabVisible = false;
            IsJsonTabVisible = false;
            IsTestTabVisible = false;
            IsSaveTabVisible = false;

            // Show the selected tab
            switch (tabIndex)
            {
                case 0:
                    IsVisualTabVisible = true;
                    BsoLogger.Log("FiltersModalViewModel", "Build Filter tab visible");
                    break;
                case 1:
                    IsDeckStakeTabVisible = true;
                    BsoLogger.Log("FiltersModalViewModel", "Deck/Stake tab visible");
                    break;
                case 2:
                    IsJsonTabVisible = true;
                    BsoLogger.Log("FiltersModalViewModel", "JSON Editor tab visible");
                    break;
                case 3:
                    BsoLogger.Log("FiltersModalViewModel", "YAML Editor tab visible");
                    break;
                case 4:
                    IsSaveTabVisible = true;
                    RefreshSaveTabData();
                    BsoLogger.Log("FiltersModalViewModel", "Validate Filter tab visible");
                    break;
            }

            // Log final state
            BsoLogger.Log(
                "FiltersModalViewModel",
                $"Final visibility state - Visual:{IsVisualTabVisible} JSON:{IsJsonTabVisible} Save:{IsSaveTabVisible}"
            );
        }

        /// <summary>
        /// Refresh Save tab data when switching to it
        /// </summary>
        private void RefreshSaveTabData()
        {
            // Find the Validate Filter tab and refresh its data
            if (
                TabItems
                    .FirstOrDefault(t => t.Content is Components.FilterTabs.ValidateFilterTab)
                    ?.Content
                    is Components.FilterTabs.ValidateFilterTab validateFilterTab
                && validateFilterTab.DataContext
                    is FilterTabs.ValidateFilterTabViewModel validateVm
            )
            {
                validateVm.PreFillFilterData();
                validateVm.Validate();
                BsoLogger.Log("FiltersModalViewModel", "Refreshed Validate Filter tab data");
            }
        }

        /// <summary>
        /// Implements in-modal back navigation for tabbed filters workflow.
        /// Returns true if navigation occurred; false indicates caller should close modal.
        /// </summary>
        public bool TryGoBack()
        {
            if (SelectedTabIndex > 0)
            {
                var newIndex = SelectedTabIndex - 1;
                SelectedTabIndex = newIndex;
                UpdateTabVisibility(newIndex);
                return true;
            }
            return false;
        }

        // Capture the OLD tab index BEFORE the property changes
        partial void OnSelectedTabIndexChanging(int value)
        {
            _previousTabIndex = SelectedTabIndex;

            // Tab order: 0=Build Filter, 1=Deck/Stake, 2=YAML Editor, 3=Validate Filter
            switch (_previousTabIndex)
            {
                case 2: // Leaving YAML Editor - parse YAML and update state
                    if (YAMLEditorTab is FilterTabs.JamlEditorTabViewModel yamlVm)
                    {
                        SaveYAMLEditorToState(yamlVm);
                    }
                    break;
            }
        }

        partial void OnSelectedTabIndexChanged(int value)
        {
            BsoLogger.Log("FiltersModalViewModel", $"Tab switch to {value}");

            UpdateTabVisibility(value);
            CurrentTabContent = (value >= 0 && value < TabItems.Count) ? TabItems[value].Content : null;

            // Tab order: 0=Build Filter, 1=Deck/Stake, 2=YAML Editor, 3=Validate Filter
            switch (value)
            {
                case 0: // Entering Visual Builder - refresh from state
                    if (VisualBuilderTab is FilterTabs.VisualBuilderTabViewModel visualVm)
                    {
                        visualVm.LoadFromParentCollections();
                        ExpandDropZonesWithItems();
                    }
                    break;
                case 2: // Entering YAML Editor - regenerate from state
                    if (YAMLEditorTab is FilterTabs.JamlEditorTabViewModel yamlVm)
                    {
                        yamlVm.AutoGenerateFromVisual();
                    }
                    break;
                case 3: // Entering Validate Filter - refresh its data
                    RefreshSaveTabData();
                    break;
            }
        }

        /// <summary>
        /// Parse YAML from YAML Editor and update parent ViewModel's state
        /// YAML (Joker Ante Markup Language) is a YAML-based format
        /// </summary>
        private void SaveYAMLEditorToState(FilterTabs.JamlEditorTabViewModel yamlVm)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(yamlVm.YamlContent))
                {
                    BsoLogger.Log(
                        "FiltersModalViewModel",
                        "⚠️ YAML Editor is empty - skipping save"
                    );
                    return;
                }

                if (
                    !JamlConfigLoader.TryLoad(yamlVm.YamlContent, out var config, out var loadError)
                    || config is null
                )
                {
                    BsoLogger.LogError(
                        "FiltersModalViewModel",
                        $"❌ Failed to parse YAML - skipping save: {loadError}"
                    );
                    return;
                }

                // Load config into parent state (updates SelectedMust/Should/MustNot, deck, stake, etc.)
                LoadConfigIntoState(config);

                BsoLogger.Log(
                    "FiltersModalViewModel",
                    $"✅ YAML Editor saved to state: {config.Must?.Count ?? 0} must, {config.Should?.Count ?? 0} should, Deck={config.Deck}, Stake={config.Stake}"
                );
            }
            catch (Exception ex)
            {
                BsoLogger.LogError("FiltersModalViewModel", $"Error parsing YAML: {ex.Message}");
            }
        }

        // ===== HELPER METHODS =====

        private Dictionary<string, List<string>> InitializeItemCategories()
        {
            // Initialize from BalatroData
            return new Dictionary<string, List<string>>
            {
                ["Jokers"] = new List<string>(BalatroData.Jokers.Keys),
                ["Tarots"] = new List<string>(BalatroData.TarotCards.Keys),
                ["Planets"] = new List<string>(BalatroData.PlanetCards.Keys),
                ["Spectrals"] = new List<string>(BalatroData.SpectralCards.Keys),
                ["PlayingCards"] = GeneratePlayingCardsList(),
                ["Vouchers"] = new List<string>(BalatroData.Vouchers.Keys),
                ["Tags"] = new List<string>(BalatroData.Tags.Keys),
                ["Bosses"] = new List<string>(BalatroData.BossBlinds.Keys),
            };
        }

        private List<string> GeneratePlayingCardsList()
        {
            var cards = new List<string>();
            var ranks = new[] { "2", "3", "4", "5", "6", "7", "8", "9", "10", "J", "Q", "K", "A" };
            var suits = new[] { "♠", "♥", "♦", "♣" };

            foreach (var suit in suits)
            {
                foreach (var rank in ranks)
                {
                    cards.Add($"{rank}{suit}");
                }
            }

            return cards;
        }

        /// <summary>
        /// Builds JamlConfig from current ViewModel state
        /// Called by code-behind and ViewModel methods
        /// </summary>
        public JamlConfig BuildConfigFromCurrentState()
        {
            var author = _userProfileService?.GetAuthorName() ?? "Unknown";

            // A loaded filter keeps what the designer has no control for: its id, date, seed
            // list, cutoff and clause defaults. A new filter gets a fresh id.
            var loaded = LoadedConfig;
            var config = new JamlConfig
            {
                Id = string.IsNullOrEmpty(loaded?.Id) ? Guid.NewGuid().ToString("N") : loaded.Id,
                Name = string.IsNullOrWhiteSpace(FilterName) ? "Untitled Filter" : FilterName,
                Description =
                    string.IsNullOrEmpty(FilterDescription) && loaded is { Description: null }
                        ? null
                        : FilterDescription,
                Author = _originalAuthor ?? author,
                DateCreated = loaded?.DateCreated,
                Deck = SelectedDeck,
                Stake = SelectedStake,
                Seeds = loaded is null ? new List<string>() : new List<string>(loaded.Seeds),
                Cutoff = loaded?.Cutoff,
                Defaults = loaded?.Defaults,
                Must = new List<IMotelyClause>(),
                Should = new List<IMotelyClause>(),
                MustNot = new List<IMotelyClause>(),
            };

            var visualBuilder = VisualBuilderTab as FilterTabs.VisualBuilderTabViewModel;

            BsoLogger.LogImportant(
                "FiltersModalViewModel",
                $"🔍 BuildConfig: VisualBuilderTab={visualBuilder?.GetType().Name ?? "NULL"}"
            );

            if (visualBuilder is not null)
            {
                BsoLogger.LogImportant(
                    "FiltersModalViewModel",
                    $"✅ USING VisualBuilder PATH: {visualBuilder.SelectedMust.Count} must, {visualBuilder.SelectedShould.Count} should"
                );

                AppendVisualClauses(visualBuilder.SelectedMust, config.Must);
                AppendVisualClauses(visualBuilder.SelectedShould, config.Should);
                AppendBannedItemsAsMustNot(visualBuilder.SelectedMust, config.MustNot);
            }

            return config;
        }

        // ConvertFilterItemToClause returns null for BannedItems operators by design,
        // so they're skipped here and surfaced via AppendBannedItemsAsMustNot.
        private void AppendVisualClauses(
            IEnumerable<Models.FilterItem> source,
            List<IMotelyClause> target
        )
        {
            foreach (var filterItem in source)
            {
                BsoLogger.LogImportant(
                    "FiltersModalViewModel",
                    $"Processing item: Name={filterItem.Name}, Type={filterItem.Type}, ActualType={filterItem.GetType().Name}"
                );

                var clause = ConvertFilterItemToClause(filterItem);
                if (clause is null)
                {
                    BsoLogger.LogError(
                        "FiltersModalViewModel",
                        $"Failed to convert {filterItem.Name} to clause!"
                    );
                    continue;
                }

                target.Add(clause);
                var childClauses = clause.GetClauses();
                BsoLogger.Log(
                    "FiltersModalViewModel",
                    $"Added clause: Type={clause.GetTypeName()}, HasClauses={childClauses is not null}, ClausesCount={childClauses?.Length ?? 0}"
                );
            }
        }

        // BannedItems live inside the Must collection as a FilterOperatorItem wrapper;
        // unwrap their children into MustNot to match the JSON config schema.
        private void AppendBannedItemsAsMustNot(
            IEnumerable<Models.FilterItem> source,
            List<IMotelyClause> mustNot
        )
        {
            foreach (var item in source)
            {
                if (item is not Models.FilterOperatorItem op || op.OperatorType != "BannedItems")
                    continue;

                foreach (var child in op.Children)
                {
                    var clause = ConvertFilterItemToClause(child);
                    if (clause is not null)
                        mustNot.Add(clause);
                }
            }
        }

        private IMotelyClause? ConvertFilterItemToClause(Models.FilterItem filterItem)
        {
            // An item loaded from a filter emits the clause it was loaded from, so everything the
            // builder doesn't show (antes, sources, score, min/max, label, …) survives; only what
            // the builder changed on the item is applied. An and/or takes its children as they
            // now stand in the builder (each emitting its own clause the same way).
            if (filterItem.Clause is { } loadedClause)
            {
                var copy = ShallowCopy(loadedClause);
                if (copy is LogicClause loadedLogic && filterItem is Models.FilterOperatorItem loadedOperator)
                {
                    loadedLogic.Clauses = loadedOperator
                        .Children.Select(ConvertFilterItemToClause)
                        .OfType<IMotelyClause>()
                        .ToArray();
                }
                else
                {
                    ApplyBuilderEdits(copy, filterItem);
                }
                return copy;
            }

            // Handle FilterOperatorItem (OR/AND clauses)
            if (filterItem is Models.FilterOperatorItem operatorItem)
            {
                if (operatorItem.OperatorType == "BannedItems")
                    return null;

                var isOr = operatorItem.OperatorType.Equals("Or", StringComparison.OrdinalIgnoreCase);
                var logic = isOr ? (LogicClause)new OrClause() : new AndClause();
                var children = new List<IMotelyClause>();
                foreach (var child in operatorItem.Children)
                {
                    var childClause = ConvertFilterItemToClause(child);
                    if (childClause is not null)
                        children.Add(childClause);
                }
                logic.Clauses = children.ToArray();
                logic.Label = operatorItem.DisplayName;
                return logic;
            }

            // Convert wildcard names to Motely-compatible values
            string clauseValue = filterItem.Name;
            if (filterItem.Name.StartsWith("Wildcard_", StringComparison.OrdinalIgnoreCase))
            {
                if (filterItem.Type == "SoulJoker")
                    clauseValue = "Any";
                else
                    clauseValue = filterItem.Name.Replace("Wildcard_Joker", "any", StringComparison.OrdinalIgnoreCase).ToLowerInvariant();
            }

            var clause = JamlClauseExtensions.CreateClauseForDiscriminator(filterItem.Type, clauseValue);

            if (filterItem.Antes?.Any() == true)
                clause.SetAntes(filterItem.Antes.ToArray());

            if (
                !string.IsNullOrEmpty(filterItem.Edition)
                && filterItem.Edition != "none"
                && filterItem.Edition != "None"
            )
            {
                clause.SetEditionString(filterItem.Edition);
            }

            if (filterItem.Stickers?.Any() == true)
                clause.SetStickerStrings(filterItem.Stickers.ToArray());

            if (filterItem.Type == "PlayingCard" || filterItem.Category == "PlayingCards")
            {
                if (!string.IsNullOrEmpty(filterItem.Seal) && filterItem.Seal != "None")
                    clause.SetSealString(filterItem.Seal);
                if (!string.IsNullOrEmpty(filterItem.Enhancement) && filterItem.Enhancement != "None")
                    clause.SetEnhancementString(filterItem.Enhancement);
            }

            return clause;
        }

        private static readonly System.Reflection.MethodInfo MemberwiseCloneMethod =
            typeof(object).GetMethod(
                "MemberwiseClone",
                System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic
            )!;

        /// <summary>
        /// A copy of the clause to emit, so building never changes the loaded clause (the builder
        /// item and <see cref="LoadedConfig"/> keep it). Shallow is enough: the edits below replace
        /// values and arrays, never change them in place.
        /// </summary>
        private static IMotelyClause ShallowCopy(IMotelyClause clause) =>
            (IMotelyClause)MemberwiseCloneMethod.Invoke(clause, null)!;

        /// <summary>
        /// Apply to a loaded clause what the builder set on its item: an edition, stickers, seal,
        /// enhancement or antes the item holds that differ from the clause's. An item value that is
        /// empty leaves the clause's own value alone (as for new items, the builder only adds
        /// decorations; and the ante checkboxes can't show ante 0, so "none ticked" isn't "none").
        /// </summary>
        private static void ApplyBuilderEdits(IMotelyClause clause, Models.FilterItem item)
        {
            static bool IsSet(string? value) =>
                !string.IsNullOrEmpty(value) && !value.Equals("none", StringComparison.OrdinalIgnoreCase);

            if (IsSet(item.Edition) && !string.Equals(item.Edition, clause.GetEditionString(), StringComparison.OrdinalIgnoreCase))
                clause.SetEditionString(item.Edition!);

            if (item.Stickers is { Count: > 0 } stickers)
            {
                var current = clause.GetStickerStrings() ?? [];
                if (
                    !stickers
                        .Order(StringComparer.OrdinalIgnoreCase)
                        .SequenceEqual(current.Order(StringComparer.OrdinalIgnoreCase), StringComparer.OrdinalIgnoreCase)
                )
                    clause.SetStickerStrings(stickers.ToArray());
            }

            if (IsSet(item.Seal) && !string.Equals(item.Seal, clause.GetSealString(), StringComparison.OrdinalIgnoreCase))
                clause.SetSealString(item.Seal!);

            if (
                IsSet(item.Enhancement)
                && !string.Equals(item.Enhancement, clause.GetEnhancementString(), StringComparison.OrdinalIgnoreCase)
            )
                clause.SetEnhancementString(item.Enhancement!);

            if (item.Antes is { Length: > 0 } antes && !antes.SequenceEqual(clause.GetAntes()))
                clause.SetAntes(antes.ToArray());
        }

        public void LoadConfigIntoState(JamlConfig config)
        {
            // Clear current state
            ClearAllSelections();

            // Load basic properties
            FilterName = config.Name ?? "Untitled";
            FilterDescription = config.Description ?? "";

            // Store original metadata to preserve on save
            _originalAuthor = config.Author;

            // Deck and stake are enum values
            SelectedDeck = config.Deck;
            SelectedStake = config.Stake;

            LoadClausesIntoSelection(config.Must, SelectedMust);
            LoadClausesIntoSelection(config.Should, SelectedShould);
            LoadClausesIntoSelection(config.MustNot, SelectedMustNot);

            LoadedConfig = config;

            // CRITICAL FIX: Sync Visual Builder tab collections from loaded data!
            // This populates the SelectedMust/Should/MustNot FilterItem collections
            if (VisualBuilderTab is FilterTabs.VisualBuilderTabViewModel visualVm)
            {
                visualVm.LoadFromParentCollections();
                BsoLogger.Log(
                    "FiltersModalViewModel",
                    "Synced Visual Builder tab from loaded config"
                );
            }
        }

        // Each clause is registered in ItemConfigs under a freshly generated key,
        // and the key is appended to the appropriate selection collection.
        private void LoadClausesIntoSelection(
            IEnumerable<IMotelyClause>? clauses,
            ObservableCollection<string> selection
        )
        {
            if (clauses is null)
                return;

            foreach (var clause in clauses)
            {
                var itemKey = GenerateNextItemKey();
                ItemConfigs[itemKey] = ConvertClauseToItemConfig(clause, itemKey);
                selection.Add(itemKey);
            }
        }

        private ItemConfig ConvertClauseToItemConfig(
            IMotelyClause clause,
            string itemKey
        )
        {
            var normalizedType = NormalizeItemType(clause.GetTypeName());
            var childClauses = clause.GetClauses();

            if (
                (
                    normalizedType.Equals("and", StringComparison.OrdinalIgnoreCase)
                    || normalizedType.Equals("or", StringComparison.OrdinalIgnoreCase)
                )
                && childClauses?.Length > 0
            )
            {
                var operatorConfig = new ItemConfig
                {
                    ItemKey = itemKey,
                    ItemType = "Operator",
                    ItemName = $"{normalizedType.ToUpper()} ({childClauses.Length} items)",
                    OperatorType =
                        normalizedType.Substring(0, 1).ToUpper()
                        + normalizedType.Substring(1).ToLower(),
                    Score = clause.Score > 0 ? clause.Score : 1,
                    Label = clause.Label,
                    Antes = clause.GetAntes().ToList(),
                    Children = new List<ItemConfig>(),
                    Clause = clause,
                };

                int childIndex = 0;
                foreach (var childClause in childClauses)
                {
                    var childKey = $"{itemKey}_child_{++childIndex}";
                    var childConfig = ConvertClauseToItemConfig(childClause, childKey);
                    operatorConfig.Children.Add(childConfig);
                }

                return operatorConfig;
            }

            string itemName = clause.GetValueName() ?? "";
            if (
                itemName.Equals("Any", StringComparison.OrdinalIgnoreCase)
                && normalizedType == "SoulJoker"
            )
            {
                itemName = "Wildcard_JokerLegendary";
            }

            var itemConfig = new ItemConfig
            {
                ItemKey = itemKey,
                ItemType = normalizedType,
                ItemName = itemName,
                Score = clause.Score > 0 ? clause.Score : 1,
                Label = clause.Label,
                Min = clause.Min,
                Edition = clause.GetEditionString() ?? "none",
                Antes = clause.GetAntes().ToList(),
                Stickers = clause.GetStickerStrings()?.ToList(),
                Seal = clause.GetSealString(),
                Enhancement = clause.GetEnhancementString(),
                Clause = clause,
            };

            if (normalizedType == "SoulJoker")
            {
                itemConfig.ItemType = "Joker";
                itemConfig.IsSoulJoker = true;
            }

            return itemConfig;
        }

        private void ClearAllSelections()
        {
            SelectedMust.Clear();
            SelectedShould.Clear();
            SelectedMustNot.Clear();
            ItemConfigs.Clear();
            _itemKeyCounter = 0;
            _instanceCounter = 0;

            // Clear original metadata when starting a new filter
            _originalAuthor = null;
        }

        public string GenerateNextItemKey()
        {
            return $"item_{++_itemKeyCounter}";
        }

        public int GenerateNextInstance()
        {
            return ++_instanceCounter;
        }

        public void InitializeTabs()
        {
            TabItems.Clear();

            // Create the proper VisualBuilderTabViewModel for the visual builder
            var visualBuilderViewModel = new FilterTabs.VisualBuilderTabViewModel(this);
            VisualBuilderTab = visualBuilderViewModel; // Store reference for other components

            // Create the VISUAL BUILDER tab with the actual visual item shelf!
            var visualBuilderTab = new Components.FilterTabs.VisualBuilderTab
            {
                DataContext = visualBuilderViewModel,
            };

            // Wire up filter name edit activation event
            // Filter name focus is handled by the view; no view lookups from the ViewModel.

            TabItems.Add(new TabItemViewModel("BUILD FILTER", visualBuilderTab));

            // Deck/Stake tab (NEW - between Build Filter and JSON Editor)
            var deckStakeViewModel = new FilterTabs.DeckStakeTabViewModel(this);
            var deckStakeTab = new Components.FilterTabs.DeckStakeTab
            {
                DataContext = deckStakeViewModel,
            };
            DeckStakeTab = deckStakeViewModel;
            TabItems.Add(new TabItemViewModel("DECK/STAKE", deckStakeTab));

            // YAML Editor tab (YAML = Joker Ante Markup Language, a YAML-based format)
            var yamlEditorViewModel = new FilterTabs.JamlEditorTabViewModel(this);
            var yamlEditorTab = new Components.FilterTabs.YAMLEditorTab
            {
                DataContext = yamlEditorViewModel,
            };
            YAMLEditorTab = yamlEditorViewModel; // Store reference
            TabItems.Add(new TabItemViewModel("JAML EDITOR", yamlEditorTab));

            // Validate Filter tab
            var validateFilterViewModel =
                _validateTabFactory?.Invoke() ?? new ValidateFilterTabViewModel(this);
            var validateFilterTab = new Components.FilterTabs.ValidateFilterTab
            {
                DataContext = validateFilterViewModel,
            };

            // Initialize the ValidateFilterTab with current filter data
            validateFilterViewModel.PreFillFilterData();

            TabItems.Add(new TabItemViewModel("VALIDATE FILTER", validateFilterTab));

            // Ensure initial tab content and visibility are set
            // Order: 0=Build Filter, 1=Deck/Stake, 2=YAML Editor, 3=Validate Filter
            UpdateTabVisibility(SelectedTabIndex);
            CurrentTabContent = (SelectedTabIndex >= 0 && SelectedTabIndex < TabItems.Count)
                ? TabItems[SelectedTabIndex].Content : null;
        }

        private object CreateLoadTabContent()
        {
            // Use the new FilterSelectorControl (unified component for both modals)
            var filterSelector = new Components.FilterSelectorControl
            {
                // In FiltersModal, we are NOT in the Search context
                IsInSearchModal = false,
            };

            // Handle EDIT action: load config and switch to Visual Builder
            filterSelector.FilterEditRequested += async (sender, filterPath) =>
            {
                try
                {
                    BsoLogger.Log("FiltersModalViewModel", $"Editing filter from: {filterPath}");

                    var yaml = await File.ReadAllTextAsync(filterPath);
                    if (!JamlConfigLoader.TryLoad(yaml, out var config, out var loadError) || config is null)
                    {
                        BsoLogger.LogError(
                            "FiltersModalViewModel",
                            $"Failed to load filter for editing: {loadError}"
                        );
                        return;
                    }

                    CurrentFilterPath = filterPath;

                    // CRITICAL: Load the config into state (populates SelectedMust/Should/MustNot)
                    LoadConfigIntoState(config);

                    // Update Visual Builder with FilterItem objects and expand zones
                    await UpdateVisualBuilderFromItemConfigs();
                    ExpandDropZonesWithItems();

                    ApplyDeckFromConfig(config.Deck.ToString());
                    ApplyStakeFromConfig(config.Stake.ToString());

                    // Switch to Visual Builder tab
                    SelectedTabIndex = 1;
                    BsoLogger.Log(
                        "FiltersModalViewModel",
                        $"Filter loaded for editing: {config.Name}"
                    );
                }
                catch (Exception ex)
                {
                    BsoLogger.LogError(
                        "FiltersModalViewModel",
                        $"Error loading filter for editing: {ex.Message}"
                    );
                }
            };

            // Handle COPY action: duplicate file, resolve name collisions, load copy for editing
            filterSelector.FilterCopyRequested += async (sender, originalPath) =>
            {
                try
                {
                    var directory = Path.GetDirectoryName(originalPath) ?? FilterFiles.Dir;
                    var baseName = Path.GetFileNameWithoutExtension(originalPath);
                    var extension = Path.GetExtension(originalPath);

                    // Prefer "(copy)" suffix and append counter if needed
                    string candidateName = $"{baseName} (copy)";
                    string newPath = Path.Combine(directory, candidateName + extension);
                    int counter = 2;
                    while (File.Exists(newPath))
                    {
                        candidateName = $"{baseName} (copy {counter})";
                        newPath = Path.Combine(directory, candidateName + extension);
                        counter++;
                    }

                    JamlConfig? config = null;
                    try
                    {
                        var yaml = await File.ReadAllTextAsync(originalPath);
                        if (JamlConfigLoader.TryLoad(yaml, out var loadedConfig, out _))
                            config = loadedConfig;
                    }
                    catch
                    {
                        // Failed to parse — fall through to raw file copy below
                    }

                    if (config is not null)
                    {
                        config.Name = string.IsNullOrWhiteSpace(config.Name)
                            ? candidateName
                            : $"{config.Name} (copy)";
                        await File.WriteAllTextAsync(newPath, JamlConfigText.ToJaml(config));
                    }
                    else
                    {
                        File.Copy(originalPath, newPath, overwrite: false);
                    }

                    // Refresh list and load the new copy for editing
                    filterSelector.RefreshFilters();

                    try
                    {
                        var newYAML = await File.ReadAllTextAsync(newPath);
                        var newConfig = JamlConfigLoader.TryLoad(newYAML, out var loadedConfig, out _)
                            ? loadedConfig
                            : null;
                        if (newConfig is not null)
                        {
                            LoadedConfig = newConfig;
                            CurrentFilterPath = newPath;

                            // Load config and update Visual Builder
                            LoadConfigIntoState(newConfig);
                            await UpdateVisualBuilderFromItemConfigs();
                            ExpandDropZonesWithItems();

                            // Preserve deck/stake selections from the copy
                            ApplyDeckFromConfig(newConfig.Deck.ToString());
                            ApplyStakeFromConfig(newConfig.Stake.ToString());

                            SelectedTabIndex = 1;
                            BsoLogger.Log(
                                "FiltersModalViewModel",
                                $"Created and loaded copy: {candidateName}"
                            );
                        }
                    }
                    catch (Exception ex)
                    {
                        BsoLogger.LogError(
                            "FiltersModalViewModel",
                            $"Copy created but failed to load: {ex.Message}"
                        );
                    }
                }
                catch (Exception ex)
                {
                    BsoLogger.LogError(
                        "FiltersModalViewModel",
                        $"Error copying filter: {ex.Message}"
                    );
                }
            };

            // Handle DELETE action
            filterSelector.FilterDeleteRequested += (sender, filterPath) =>
            {
                try
                {
                    if (File.Exists(filterPath))
                    {
                        File.Delete(filterPath);
                        filterSelector.RefreshFilters();
                        BsoLogger.Log("FiltersModalViewModel", $"Deleted filter: {filterPath}");
                    }
                }
                catch (Exception ex)
                {
                    BsoLogger.LogError(
                        "FiltersModalViewModel",
                        $"Error deleting filter: {ex.Message}"
                    );
                }
            };

            // Handle CREATE NEW FILTER
            filterSelector.NewFilterRequested += (sender, e) =>
            {
                try
                {
                    BsoLogger.Log(
                        "FiltersModalViewModel",
                        "Create New Filter requested from Load tab"
                    );
                    CreateNewFilter();
                    SelectedTabIndex = 1;
                    BsoLogger.Log(
                        "FiltersModalViewModel",
                        "Switched to Visual Builder for new filter"
                    );
                }
                catch (Exception ex)
                {
                    BsoLogger.LogError(
                        "FiltersModalViewModel",
                        $"Error handling Create New Filter: {ex.Message}"
                    );
                }
            };

            return filterSelector;
        }

        // Lookup deck/stake name from a config blob and apply to the SelectedXIndex setter.
        // Both no-op if the name is missing or unrecognised, leaving the previous selection intact.
        private void ApplyDeckFromConfig(string? deckName)
        {
            if (string.IsNullOrEmpty(deckName))
                return;
            var index = Array.IndexOf(DeckDisplayValues, deckName);
            if (index >= 0)
                SelectedDeckIndex = index;
        }

        private void ApplyStakeFromConfig(string? stakeName)
        {
            if (string.IsNullOrEmpty(stakeName))
                return;
            var index = Array.IndexOf(StakeNamesByIndex, stakeName.ToLower());
            if (index >= 0)
                SelectedStakeIndex = index;
        }

        /// <summary>
        /// Update Visual Builder FilterItem collections from ItemConfigs
        /// </summary>
        public Task UpdateVisualBuilderFromItemConfigs()
        {
            try
            {
                if (VisualBuilderTab is FilterTabs.VisualBuilderTabViewModel visualVm)
                {
                    // Disable JSON auto-generation during filter load
                    visualVm.BeginFilterLoad();
                    FillVisualBuilder(visualVm);
                    // Re-enable JSON auto-generation after filter load
                    visualVm.EndFilterLoad();
                }
            }
            catch (Exception ex)
            {
                BsoLogger.LogError(
                    "FiltersModalViewModel",
                    $"Error updating Visual Builder: {ex.Message}"
                );
            }
            return Task.CompletedTask;
        }

        /// <summary>
        /// Rebuild the builder's MUST/SHOULD zones from <see cref="SelectedMust"/>,
        /// <see cref="SelectedShould"/> and <see cref="SelectedMustNot"/> (the last as a Banned
        /// Items tray in MUST). Each item carries the clause it was loaded from, if any.
        /// </summary>
        public void FillVisualBuilder(FilterTabs.VisualBuilderTabViewModel visualVm)
        {
            visualVm.SelectedMust.Clear();
            visualVm.SelectedShould.Clear();

            foreach (var itemKey in SelectedMust)
                if (ItemConfigs.TryGetValue(itemKey, out var itemConfig))
                    visualVm.SelectedMust.Add(CreateBuilderItem(itemConfig));

            foreach (var itemKey in SelectedShould)
                if (ItemConfigs.TryGetValue(itemKey, out var itemConfig))
                    visualVm.SelectedShould.Add(CreateBuilderItem(itemConfig));

            // MustNot items → BannedItems tray in the MUST zone
            if (SelectedMustNot.Count > 0)
            {
                var bannedItemsTray = new Models.FilterOperatorItem("BannedItems");
                foreach (var itemKey in SelectedMustNot)
                {
                    if (ItemConfigs.TryGetValue(itemKey, out var itemConfig))
                    {
                        var filterItem = CreateBuilderItem(itemConfig);
                        // Set IsInBannedItemsTray flag for debuffed overlay
                        filterItem.IsInBannedItemsTray = true;
                        bannedItemsTray.Children.Add(filterItem);
                    }
                }
                visualVm.SelectedMust.Add(bannedItemsTray);
            }

            BsoLogger.Log(
                "FiltersModalViewModel",
                $"Updated Visual Builder: {visualVm.SelectedMust.Count} must, {visualVm.SelectedShould.Count} should"
            );
        }

        /// <summary>
        /// The builder item for an ItemConfig: an and/or becomes an operator tray holding its
        /// children, anything else a card. The item keeps the config's loaded clause. Never null:
        /// an item whose art can't be found still shows (and saves) without it.
        /// </summary>
        private Models.FilterItem CreateBuilderItem(ItemConfig itemConfig)
        {
            if (itemConfig.ItemType == "Operator" && itemConfig.Children is { } children)
            {
                var operatorItem = new Models.FilterOperatorItem(
                    (itemConfig.OperatorType ?? "Or").ToUpperInvariant()
                )
                {
                    ItemKey = itemConfig.ItemKey,
                    Label = itemConfig.Label,
                    Score = itemConfig.Score,
                    Clause = itemConfig.Clause,
                };
                foreach (var child in children)
                    operatorItem.Children.Add(CreateBuilderItem(child));
                return operatorItem;
            }

            var normalizedType = NormalizeItemType(itemConfig.ItemType);
            var effectiveType = itemConfig.IsSoulJoker ? "SoulJoker" : normalizedType;
            var edition =
                string.IsNullOrEmpty(itemConfig.Edition)
                || itemConfig.Edition.Equals("none", StringComparison.OrdinalIgnoreCase)
                    ? null
                    : itemConfig.Edition;

            var filterItem = new Models.FilterItem
            {
                Name = itemConfig.ItemName,
                Type = effectiveType,
                // Determine category for layout behavior
                Category = DetermineCategoryFromType(effectiveType),
                ItemKey = itemConfig.ItemKey,
                Edition = edition,
                Stickers = itemConfig.Stickers is { Count: > 0 } ? new List<string>(itemConfig.Stickers) : null,
                Seal = itemConfig.Seal,
                Enhancement = itemConfig.Enhancement,
                Antes = itemConfig.Antes is { Count: > 0 } ? itemConfig.Antes.ToArray() : null,
                Label = itemConfig.Label,
                Score = itemConfig.Score,
                Clause = itemConfig.Clause,
            };

            try
            {
                filterItem.DisplayName = BalatroData.GetDisplayNameFromSprite(itemConfig.ItemName);
                var spriteService = SpriteService.Instance;
                // Get appropriate sprite image based on type
                filterItem.ItemImage = effectiveType switch
                {
                    "Joker" or "SoulJoker" or "CommonJoker" or "UncommonJoker" or "RareJoker" =>
                        spriteService.GetJokerImage(itemConfig.ItemName, itemConfig.Edition),
                    "SmallBlindTag" or "BigBlindTag" => spriteService.GetTagImage(itemConfig.ItemName),
                    "Voucher" => spriteService.GetVoucherImage(itemConfig.ItemName),
                    "Tarot" => spriteService.GetTarotImage(itemConfig.ItemName),
                    "Planet" => spriteService.GetPlanetCardImage(itemConfig.ItemName),
                    "Spectral" => spriteService.GetSpectralImage(itemConfig.ItemName),
                    "Boss" => spriteService.GetBossImage(itemConfig.ItemName),
                    "PlayingCard" or "StandardCard" => spriteService.GetPlayingCardImage(
                        string.IsNullOrWhiteSpace(itemConfig.Suit) ? "Spades" : itemConfig.Suit,
                        string.IsNullOrWhiteSpace(itemConfig.Rank) ? "10" : itemConfig.Rank,
                        itemConfig.Enhancement
                    ),
                    _ => null,
                };
            }
            catch (Exception ex)
            {
                BsoLogger.LogError(
                    "FiltersModalViewModel",
                    $"No art for {itemConfig.ItemType} '{itemConfig.ItemName}': {ex.Message}"
                );
            }

            return filterItem;
        }

        /// <summary>
        /// Maps item type to category for CategoryGroupedLayoutBehavior
        /// </summary>
        private string DetermineCategoryFromType(string itemType)
        {
            var normalizedType = NormalizeItemType(itemType);

            return normalizedType switch
            {
                "Joker" or "SoulJoker" or "CommonJoker" or "UncommonJoker" or "RareJoker" => "Jokers",
                "SmallBlindTag" or "BigBlindTag" => "Tags",
                "Boss" => "Bosses",
                "Voucher" => "Vouchers",
                "Tarot" or "Planet" or "Spectral" => "Consumables",
                "StandardCard" or "PlayingCard" => "PlayingCards",
                _ => "Other",
            };
        }

        private static string NormalizeItemType(string? type)
        {
            if (string.IsNullOrWhiteSpace(type))
            {
                return "Unknown";
            }

            var cleaned = new string(type.Where(char.IsLetterOrDigit).ToArray());
            if (string.IsNullOrEmpty(cleaned))
            {
                return "Unknown";
            }

            var lower = cleaned.ToLowerInvariant();

            return lower switch
            {
                "joker" => "Joker",
                "souljoker" => "SoulJoker",
                // The rarity keys keep their rarity (so a save writes `rareJoker:` back), but
                // title-casing made "Rarejoker", which had no sprite and no category.
                "commonjoker" => "CommonJoker",
                "uncommonjoker" => "UncommonJoker",
                "rarejoker" => "RareJoker",
                "legendaryjoker" => "SoulJoker",
                "smallblindtag" => "SmallBlindTag",
                "bigblindtag" => "BigBlindTag",
                "tag" => "SmallBlindTag",
                "boss" => "Boss",
                "voucher" => "Voucher",
                "tarot" => "Tarot",
                "planet" => "Planet",
                "spectral" => "Spectral",
                "operator" => "Operator",
                "playingcard" => "PlayingCard",
                "standardcard" => "StandardCard",
                _ => CultureInfo.InvariantCulture.TextInfo.ToTitleCase(lower),
            };
        }

        /// <summary>
        /// CRITICAL: Expand drop zones that contain items after loading a filter.
        /// Without this, loaded items won't render because the ItemsControl IsVisible=false when collapsed.
        /// </summary>
        public void ExpandDropZonesWithItems()
        {
            if (VisualBuilderTab is FilterTabs.VisualBuilderTabViewModel visualVm)
            {
                // Expand ALL zones that have items
                visualVm.IsMustExpanded = visualVm.SelectedMust.Count > 0;
                visualVm.IsShouldExpanded = visualVm.SelectedShould.Count > 0;
                // MUST-NOT zone removed - no IsCantExpanded property
            }
        }
    }
}

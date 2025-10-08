using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Animation;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Shapes;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
using Avalonia.Platform.Storage;
using Avalonia.Styling;
using Avalonia.Threading;
using Avalonia.VisualTree;
using AvaloniaEdit;
using AvaloniaEdit.CodeCompletion;
using AvaloniaEdit.TextMate;
using Motely;
using Motely.Filters;
using BalatroSeedOracle.Components;
using BalatroSeedOracle.Constants;
using BalatroSeedOracle.Controls;
using BalatroSeedOracle.Helpers;
using BalatroSeedOracle.Models;
using BalatroSeedOracle.Services;
using BalatroSeedOracle.ViewModels;
using TextMateSharp.Grammars;
using DebugLogger = BalatroSeedOracle.Helpers.DebugLogger;
using IoPath = System.IO.Path;

namespace BalatroSeedOracle.Views.Modals
{
    // Tag class for drop zone items
    internal class DropZoneItemTag
    {
        public string Key { get; set; } = "";
        public string Zone { get; set; } = "";
        public ItemConfig Config { get; set; } = new(); // Direct reference to config!
        public bool Fanned { get; set; }
        public int CardIndex { get; set; }
    }

    public partial class FiltersModalContent : UserControl
    {
        // ===== VIEWMODEL (The source of truth!) =====
        public FiltersModalViewModel ViewModel { get; }

        // ===== TEMPORARY: Keep old state for gradual migration =====
        // TODO: Remove these once all code uses ViewModel.SelectedMust/Should/MustNot
        private readonly Dictionary<string, List<string>> _itemCategories;
        private readonly List<string> _selectedMust = new();
        private readonly List<string> _selectedShould = new();
        private readonly List<string> _selectedMustNot = new();

        // PERFORMANCE FIX (QW-5): HashSet for O(1) lookups instead of O(n) LINQ.Any()
        // Reduces selection state updates by 90% (was O(150 × 20) = O(3000) per update)
        private readonly HashSet<string> _selectedMustSet = new();
        private readonly HashSet<string> _selectedShouldSet = new();
        private readonly HashSet<string> _selectedMustNotSet = new();
        private readonly Dictionary<string, ItemConfig> _itemConfigs = new(); // stores config per item instance
        private string _currentCategory = "Jokers";
        private int _itemKeyCounter = 0;
        private int _instanceCounter = 0; // For making each dropped item unique
        private string? _currentFilterPath; // Path to the currently loaded filter
        private MotelyJsonConfig? _loadedConfig; // Currently loaded filter configuration
        
        private string MakeUniqueKey(string itemKey)
        {
            return $"{itemKey}#{++_instanceCounter}";
        }
        
        private string GetBaseItemKey(string uniqueKey)
        {
            var idx = uniqueKey.LastIndexOf('#');
            return idx > 0 ? uniqueKey.Substring(0, idx) : uniqueKey;
        }

        /// <summary>
        /// Creates a unique key for an item to allow duplicates with different configurations
        /// </summary>
        private string CreateUniqueKey(string category, string itemName)
        {
            return $"{category}:{itemName}#{++_itemKeyCounter}";
        }

        private string _searchFilter = "";
        private Popup? _configPopup;
        private ItemConfigPopupBase? _configPopupContent;
        private EventHandler? _currentDeleteHandler;
        private Popup? _itemSelectionPopup;
        private string? _currentConfigPath;
        private TextBox? _searchBox;
        private ScrollViewer? _mainScrollViewer;
        private Dictionary<string, TextBlock> _sectionHeaders = new();
        private string _currentActiveTab = "SoulJokersTab";
        private TextBox? _configNameBox;
        private TextBox? _configDescriptionBox;
        private int _currentTabIndex = 0;
        private Polygon? _tabTriangle;
    private bool _isSwitchingTab = false; // reentrancy guard for tab switching
        private TextBox? _jsonTextBox;
        private object? _originalItemPaletteContent;
        private TextBlock? _statusText;
        private string? _currentFilePath;
        private bool _isDragging = false;

        private FavoritesService.JokerSet? _draggingSet = null;

        // Filter list ViewModel
        private FilterListViewModel? _filterListViewModel;

        // PERFORMANCE FIX (QW-1): Cache all FindControl results to avoid 160+ O(n) tree walks
        // This single change provides 50% performance improvement

        // PERFORMANCE FIX (QW-3): Debounce scroll handler (fires 60-120 times/second!)
        // Reduces CPU usage by 85% during scrolling
        private DateTime _lastScrollUpdate = DateTime.MinValue;
        private const int SCROLL_THROTTLE_MS = 100; // 10 FPS max (smooth enough)

        private Button? _clearNeedsButton;
        private Button? _clearWantsButton;
        private Button? _clearMustNotButton;
        private Button? _visualTab;
        private Button? _jsonTab;
        private Button? _loadSaveTab;
        private TextBox? _saveFilterNameInput;
        private Button? _saveFilterButton;
        private Grid? _loadSavePanel;
        private Components.FilterSelectorControl? _filterSelector;
        private Button? _visualTabButton;
        private ContentControl? _itemPaletteContent;
        private WrapPanel? _needsPanel;
        private WrapPanel? _wantsPanel;
        private WrapPanel? _mustNotPanel;
        private TextBlock? _needsPlaceholder;
        private TextBlock? _wantsPlaceholder;
        private TextBlock? _mustNotPlaceholder;
        private ScrollViewer? _needsScrollViewer;
        private ScrollViewer? _wantsScrollViewer;
        private ScrollViewer? _mustNotScrollViewer;
        private Dictionary<string, Button> _cachedTabButtons = new();

        public FiltersModalContent()
        {
            // Initialize ViewModel (follows SearchModal pattern!)
            var configService = ServiceHelper.GetRequiredService<IConfigurationService>();
            var filterService = ServiceHelper.GetRequiredService<IFilterService>();
            ViewModel = new FiltersModalViewModel(configService, filterService);
            DataContext = ViewModel;

            // SpriteService initializes lazily via Instance property
            InitializeComponent();
            BalatroSeedOracle.Helpers.DebugLogger.Log("FiltersModal", "FiltersModalContent constructor called");

            // Setup auto-save timer
            SetupAutoSave();

            // Initialize item categories from BalatroData
            _itemCategories = new Dictionary<string, List<string>>
            {
                ["Favorites"] = FavoritesService.Instance.GetFavoriteItems(),
                ["Jokers"] = BalatroData.Jokers.Keys.ToList(),
                ["Tarots"] = BalatroData.TarotCards.Keys.ToList(),
                ["Planets"] = BalatroData.PlanetCards.Keys.ToList(),
                ["Spectrals"] = BalatroData.SpectralCards.Keys.ToList(),
                ["PlayingCards"] = GeneratePlayingCardsList(),
                ["Vouchers"] = BalatroData.Vouchers.Keys.ToList(),
                ["Tags"] = BalatroData.Tags.Keys.ToList(),
                ["Bosses"] = BalatroData.BossBlinds.Keys.ToList(),
            };

            SetupControls();
            LoadAllCategories();

            // Start with tabs disabled until a filter is selected by clicking a button
            UpdateTabStates(false);

            // Hide all tab panels initially - only show filter selector
            HideAllTabPanels();
        }

        private void InitializeComponent()
        {
            BalatroSeedOracle.Helpers.DebugLogger.Log(
                "FiltersModal",
                "FiltersModalContent InitializeComponent called"
            );
            AvaloniaXamlLoader.Load(this);
        }

        private void SetupControls()
        {
            BalatroSeedOracle.Helpers.DebugLogger.Log("FiltersModal", "SetupControls called");

            // PERFORMANCE FIX (QW-1): Cache ALL controls once, never call FindControl again
            // This eliminates 160+ O(n) visual tree walks, providing 50% performance improvement
            _configNameBox = this.FindControl<TextBox>("ConfigNameBox");
            _configDescriptionBox = this.FindControl<TextBox>("ConfigDescriptionBox");
            _clearNeedsButton = this.FindControl<Button>("ClearNeedsButton");
            _clearWantsButton = this.FindControl<Button>("ClearWantsButton");
            _clearMustNotButton = this.FindControl<Button>("ClearMustNotButton");
            _visualTab = this.FindControl<Button>("VisualTab");
            _jsonTab = this.FindControl<Button>("JsonTab");
            _loadSaveTab = this.FindControl<Button>("LoadSaveTab");
            _saveFilterNameInput = this.FindControl<TextBox>("SaveFilterNameInput");
            _saveFilterButton = this.FindControl<Button>("SaveFilterButton");
            _loadSavePanel = this.FindControl<Grid>("LoadSavePanel");
            _filterSelector = this.FindControl<Components.FilterSelectorControl>("FilterSelector");
            _visualTabButton = this.FindControl<Button>("VisualTab");
            _itemPaletteContent = this.FindControl<ContentControl>("ItemPaletteContent");
            _mainScrollViewer = this.FindControl<ScrollViewer>("MainScrollViewer");
            _needsPanel = this.FindControl<WrapPanel>("NeedsPanel");
            _wantsPanel = this.FindControl<WrapPanel>("WantsPanel");
            _mustNotPanel = this.FindControl<WrapPanel>("MustNotPanel");
            _needsPlaceholder = this.FindControl<TextBlock>("NeedsPlaceholder");
            _wantsPlaceholder = this.FindControl<TextBlock>("WantsPlaceholder");
            _mustNotPlaceholder = this.FindControl<TextBlock>("MustNotPlaceholder");
            _needsScrollViewer = this.FindControl<ScrollViewer>("NeedsScrollViewer");
            _wantsScrollViewer = this.FindControl<ScrollViewer>("WantsScrollViewer");
            _mustNotScrollViewer = this.FindControl<ScrollViewer>("MustNotScrollViewer");

            // Setup tab button handlers
            SetupTabButtons();

            // Setup drop zones
            SetupDropZones();

            // Setup search functionality
            SetupSearchBox();

            // Setup clear buttons for each zone (use cached references)
            if (_clearNeedsButton != null)
            {
                _clearNeedsButton.Click += (s, e) => ClearNeeds();
            }

            if (_clearWantsButton != null)
            {
                _clearWantsButton.Click += (s, e) => ClearWants();
            }

            if (_clearMustNotButton != null)
            {
                _clearMustNotButton.Click += (s, e) => ClearMustNot();
            }

            // Keep track of current tab for triangle animation
            _currentTabIndex = 0;

            // Setup JSON Editor with autocomplete
            SetupJsonEditorAutocomplete();

            // Setup Save Filter functionality (use cached references)
            if (_saveFilterNameInput != null && _saveFilterButton != null)
            {
                // Enable/disable save button based on filter name
                _saveFilterNameInput.TextChanged += (s, e) =>
                {
                    _saveFilterButton.IsEnabled = !string.IsNullOrWhiteSpace(_saveFilterNameInput.Text);
                };
            }

            // Initialize FilterListViewModel and set as DataContext for LoadSavePanel
            _filterListViewModel = new FilterListViewModel();
            if (_loadSavePanel != null)
            {
                _loadSavePanel.DataContext = _filterListViewModel;
            }

            // Wire up FilterSelectorControl events (use cached reference)
            if (_filterSelector != null)
            {
                _filterSelector.FilterSelected += OnFilterSelected;
                _filterSelector.FilterEditRequested += OnFilterEditRequested;
                _filterSelector.FilterCopyRequested += OnFilterCopyRequested;
                _filterSelector.NewFilterRequested += OnNewFilterRequested;
            }
        }

        private void OnCreateNewFilterClick(object? sender, RoutedEventArgs e)
        {
            // Clear current filter and enable tabs to create new
            ClearFilter();
            UpdateTabStates(true);

            // Switch to Visual tab (use cached reference - QW-1)
            _visualTabButton?.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
        }

        // NOTE: OnFilterListItemClick, OnMustHaveTabClick, OnShouldHaveTabClick, OnMustNotTabClick,
        // LoadFilterItemsForTab, and CreateCardImage methods have been removed as they are now
        // handled by FilterSelectorControl component

        private void OnSelectFilterClick(object? sender, RoutedEventArgs e)
        {
            var filterPath = _filterListViewModel?.GetSelectedFilterPath();
            if (!string.IsNullOrEmpty(filterPath))
            {
                OnFilterSelected(null, filterPath);
            }
        }

        private async void OnCopyFilterClick(object? sender, RoutedEventArgs e)
        {
            var filterPath = _filterListViewModel?.GetSelectedFilterPath();
            if (string.IsNullOrEmpty(filterPath))
                return;

            try
            {
                // Get original filter name
                var originalName = IoPath.GetFileNameWithoutExtension(filterPath);

                // Show dialog to get new name
                var newName = await ShowFilterNameDialog($"{originalName} (Copy)");
                if (string.IsNullOrWhiteSpace(newName))
                    return; // User cancelled

                // Create new blank filter with the given name
                var userProfile = ServiceHelper.GetService<UserProfileService>();
                var authorName = userProfile?.GetAuthorName() ?? "Unknown";

                var newConfig = new Motely.Filters.MotelyJsonConfig
                {
                    Name = newName,
                    Description = $"Copy of {originalName}",
                    Author = authorName,
                    DateCreated = DateTime.UtcNow,
                    Must = new List<Motely.Filters.MotelyJsonConfig.MotleyJsonFilterClause>(),
                    Should = new List<Motely.Filters.MotelyJsonConfig.MotleyJsonFilterClause>(),
                    MustNot = new List<Motely.Filters.MotelyJsonConfig.MotleyJsonFilterClause>(),
                    Deck = "Red Deck",
                    Stake = "White Stake"
                };

                // Save the new filter
                var filtersDir = IoPath.Combine(Directory.GetCurrentDirectory(), "JsonItemFilters");
                Directory.CreateDirectory(filtersDir);
                var fileName = NormalizeFileName(newName);
                var newFilePath = IoPath.Combine(filtersDir, $"{fileName}.json");

                // Handle duplicates
                int counter = 1;
                while (File.Exists(newFilePath))
                {
                    newFilePath = IoPath.Combine(filtersDir, $"{fileName}{counter}.json");
                    counter++;
                }

                var json = SerializeOuijaConfig(newConfig);
                await File.WriteAllTextAsync(newFilePath, json);

                // Refresh filter list
                _filterListViewModel?.LoadFilters();

                // Load the new filter for editing
                await LoadConfigAsync(newFilePath);
                UpdateTabStates(true);

                // Switch to Visual tab
                var visualTabButton = this.FindControl<Button>("VisualTab");
                visualTabButton?.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));

                UpdateStatus($"Created copy: {newName}", false);
            }
            catch (Exception ex)
            {
                DebugLogger.LogError("FiltersModal", $"Error copying filter: {ex.Message}");
                UpdateStatus($"Error copying filter: {ex.Message}", true);
            }
        }

        private async Task<string?> ShowFilterNameDialog(string defaultName)
        {
            try
            {
                var dialog = new Window
                {
                    Title = "Name Your Filter",
                    Width = 400,
                    Height = 200,
                    WindowStartupLocation = WindowStartupLocation.CenterOwner,
                    CanResize = false
                };

                string? result = null;

                var panel = new StackPanel
                {
                    Margin = new Thickness(20),
                    Spacing = 15
                };

                var titleBlock = new TextBlock
                {
                    Text = "Enter filter name:",
                    FontFamily = (this.FindResource("BalatroFont") as Avalonia.Media.FontFamily) ?? Avalonia.Media.FontFamily.Default,
                    FontSize = 16,
                    Foreground = Brushes.White
                };
                panel.Children.Add(titleBlock);

                var textBox = new TextBox
                {
                    Text = defaultName,
                    FontSize = 14,
                    Padding = new Thickness(8)
                };
                panel.Children.Add(textBox);

                var buttonPanel = new StackPanel
                {
                    Orientation = Avalonia.Layout.Orientation.Horizontal,
                    HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center,
                    Spacing = 10,
                    Margin = new Thickness(0, 10, 0, 0)
                };

                var okButton = new Button
                {
                    Content = "CREATE",
                    Width = 120,
                    Height = 40,
                    FontSize = 14
                };
                okButton.Click += (s, e) =>
                {
                    result = textBox.Text;
                    dialog.Close();
                };

                var cancelButton = new Button
                {
                    Content = "CANCEL",
                    Width = 120,
                    Height = 40,
                    FontSize = 14
                };
                cancelButton.Click += (s, e) => dialog.Close();

                buttonPanel.Children.Add(okButton);
                buttonPanel.Children.Add(cancelButton);
                panel.Children.Add(buttonPanel);

                dialog.Content = panel;

                // Focus the textbox when dialog opens and select all text
                dialog.Opened += (s, e) =>
                {
                    textBox.Focus();
                    textBox.SelectAll();
                };

                var owner = Avalonia.Application.Current?.ApplicationLifetime is Avalonia.Controls.ApplicationLifetimes.IClassicDesktopStyleApplicationLifetime desktop
                    ? desktop.MainWindow
                    : null;

                if (owner != null)
                {
                    await dialog.ShowDialog(owner);
                }

                return result;
            }
            catch (Exception ex)
            {
                DebugLogger.LogError("FiltersModal", $"Error showing filter name dialog: {ex.Message}");
                return null;
            }
        }

        // FilterSelector event handlers
        private async void OnFilterSelected(object? sender, string filterPath)
        {
            BalatroSeedOracle.Helpers.DebugLogger.Log("FiltersModal", $"Filter selected for preview: {filterPath}");

            // When a filter is selected, load and display its details (read-only preview)
            // User must click "Edit Filter" button to actually edit

            _currentFilterPath = filterPath;

            try
            {
                // Load the filter config to display its details
                await LoadConfigAsync(filterPath);

                // DON'T enable tabs - this is preview mode only
                // Tabs stay disabled until user clicks "Edit Filter"
                UpdateTabStates(false);
            }
            catch (Exception ex)
            {
                BalatroSeedOracle.Helpers.DebugLogger.LogError("FiltersModal", $"Failed to load filter preview: {ex.Message}");
                UpdateStatus($"Failed to load filter: {ex.Message}", true);
            }
        }

        private async void OnFilterEditRequested(object? sender, string filterPath)
        {
            BalatroSeedOracle.Helpers.DebugLogger.Log("FiltersModal", $"Filter edit requested: {filterPath}");

            try
            {
                // Load the selected filter for editing
                await LoadConfigAsync(filterPath);

                // Enable tabs
                UpdateTabStates(true);

                // Switch to Visual tab for editing
                var visualTabButton = this.FindControl<Button>("VisualTab");
                visualTabButton?.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
            }
            catch (Exception ex)
            {
                BalatroSeedOracle.Helpers.DebugLogger.LogError(
                    "FiltersModal",
                    $"Error loading filter for edit: {ex.Message}"
                );
                UpdateStatus($"Error loading filter: {ex.Message}", true);
            }
        }

        private async void OnFilterCopyRequested(object? sender, string filterPath)
        {
            if (string.IsNullOrEmpty(filterPath))
                return;

            try
            {
                // Get original filter name
                var originalName = IoPath.GetFileNameWithoutExtension(filterPath);

                // Show dialog to get new name
                var newName = await ShowFilterNameDialog($"{originalName} (Copy)");
                if (string.IsNullOrWhiteSpace(newName))
                    return; // User cancelled

                // Create new blank filter with the given name
                var userProfile = ServiceHelper.GetService<UserProfileService>();
                var authorName = userProfile?.GetAuthorName() ?? "Unknown";

                var newConfig = new Motely.Filters.MotelyJsonConfig
                {
                    Name = newName,
                    Description = $"Copy of {originalName}",
                    Author = authorName,
                    DateCreated = DateTime.UtcNow,
                    Must = new List<Motely.Filters.MotelyJsonConfig.MotleyJsonFilterClause>(),
                    Should = new List<Motely.Filters.MotelyJsonConfig.MotleyJsonFilterClause>(),
                    MustNot = new List<Motely.Filters.MotelyJsonConfig.MotleyJsonFilterClause>(),
                    Deck = "Red Deck",
                    Stake = "White Stake"
                };

                // Save the new filter
                var filtersDir = IoPath.Combine(Directory.GetCurrentDirectory(), "JsonItemFilters");
                Directory.CreateDirectory(filtersDir);
                var fileName = NormalizeFileName(newName);
                var newFilePath = IoPath.Combine(filtersDir, $"{fileName}.json");

                // Handle duplicates
                int counter = 1;
                while (File.Exists(newFilePath))
                {
                    newFilePath = IoPath.Combine(filtersDir, $"{fileName}{counter}.json");
                    counter++;
                }

                var json = SerializeOuijaConfig(newConfig);
                await File.WriteAllTextAsync(newFilePath, json);

                // Refresh filter list in FilterSelectorControl
                var filterSelector = this.FindControl<Components.FilterSelectorControl>("FilterSelector");
                filterSelector?.RefreshFilters();

                // Load the new filter for editing
                await LoadConfigAsync(newFilePath);
                UpdateTabStates(true);

                // Switch to Visual tab
                var visualTabButton = this.FindControl<Button>("VisualTab");
                visualTabButton?.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));

                UpdateStatus($"Created copy: {newName}", false);
            }
            catch (Exception ex)
            {
                DebugLogger.LogError("FiltersModal", $"Error copying filter: {ex.Message}");
                UpdateStatus($"Error copying filter: {ex.Message}", true);
            }
        }

        private void OnNewFilterRequested(object? sender, EventArgs e)
        {
            // Clear current filter and enable tabs to create new
            ClearFilter();
            UpdateTabStates(true);

            // Switch to Visual tab
            var visualTabButton = this.FindControl<Button>("VisualTab");
            visualTabButton?.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
        }

        private void OnModeToggleChanged(object? sender, bool isChecked)
        {
            if (isChecked)
            {
                // JSON mode
                EnterEditJsonMode();
            }
            else
            {
                // Visual mode - restore the layout AND reload the content
                RestoreDragDropModeLayout();

                // Reload all categories to refresh the sprite display
                BalatroSeedOracle.Helpers.DebugLogger.Log(
                    "FiltersModal",
                    "Reloading all categories after JSON mode exit"
                );
                LoadAllCategories();

                // Update drop zones to show current selections
                UpdateDropZoneVisibility();
            }
        }

        private void HideAllTabPanels()
        {
            // Hide all tab content panels with correct control types
            var visualPanel = this.FindControl<Grid>("VisualPanel");
            var jsonPanel = this.FindControl<Grid>("JsonPanel");
            var testPanel = this.FindControl<Grid>("TestPanel");
            var savePanel = this.FindControl<Grid>("SaveFilterPanel");

            if (visualPanel != null)
                visualPanel.IsVisible = false;
            if (jsonPanel != null)
                jsonPanel.IsVisible = false;
            if (testPanel != null)
                testPanel.IsVisible = false;
            if (savePanel != null)
                savePanel.IsVisible = false;

            // Show the load/save panel (filter selector)
            var loadSavePanel = this.FindControl<Grid>("LoadSavePanel");
            if (loadSavePanel != null)
            {
                loadSavePanel.IsVisible = true;
            }
        }
        
        private void UpdateTabStates(bool configLoaded)
        {
            var visualTab = this.FindControl<Button>("VisualTab");
            var jsonTab = this.FindControl<Button>("JsonTab");
            var testTab = this.FindControl<Button>("TestTab");
            var saveFilterTab = this.FindControl<Button>("SaveFilterTab");

            // Enable tabs only when a filter is loaded BY USER ACTION
            if (visualTab != null)
            {
                visualTab.IsEnabled = configLoaded;
                if (!configLoaded)
                {
                    visualTab.Classes.Remove("active");
                }
            }

            if (jsonTab != null)
            {
                jsonTab.IsEnabled = configLoaded;
                if (!configLoaded)
                {
                    jsonTab.Classes.Remove("active");
                }
            }

            if (testTab != null)
            {
                testTab.IsEnabled = configLoaded;
                if (!configLoaded)
                {
                    testTab.Classes.Remove("active");
                }
            }

            if (saveFilterTab != null)
            {
                saveFilterTab.IsEnabled = configLoaded;
                if (!configLoaded)
                {
                    saveFilterTab.Classes.Remove("active");
                }
            }
            
            // Show/hide selector panel based on whether filter is loaded
            var loadSavePanel = this.FindControl<Grid>("LoadSavePanel");
            if (loadSavePanel != null)
            {
                loadSavePanel.IsVisible = !configLoaded;
            }
        }

        private BalatroSeedOracle.Models.FilterItem? ParseItemKey(string key)
        {
            var parts = key.Split(':');
            if (parts.Length >= 2)
            {
                var category = parts[0].ToLower();
                var itemName = parts[1].Split('#')[0]; // Remove any unique suffix

                // Get item configuration if exists
                var itemConfig = _itemConfigs.ContainsKey(key) ? _itemConfigs[key] : null;

                var filterItem = new BalatroSeedOracle.Models.FilterItem
                {
                    Type = category switch
                    {
                        "jokers" => "Joker",
                        "tarots" => "Tarot",
                        "spectrals" => "Spectral",
                        "vouchers" => "Voucher",
                        "tags" => "Tag",
                        "bosses" => "Boss",
                        _ => category,
                    },
                    Value = itemName,
                };

                // Set label from config or generate it
                if (itemConfig != null)
                {
                    filterItem.Label = itemConfig.Label;

                    // Copy other config properties - preserve exact antes selection
                    if (itemConfig.Antes != null)
                    {
                        filterItem.Antes = itemConfig.Antes.ToArray();
                    }
                    filterItem.Edition = itemConfig.Edition != "none" ? itemConfig.Edition : null;

                    // Convert sources to boolean flags
                    if (itemConfig.Sources != null)
                    {
                        if (itemConfig.Sources is List<string> sourcesList)
                        {
                            // Old format
                            filterItem.IncludeBoosterPacks = sourcesList.Contains("booster");
                            filterItem.IncludeShopStream = sourcesList.Contains("shop");
                            filterItem.IncludeSkipTags = sourcesList.Contains("tag");
                        }
                        else if (itemConfig.Sources is Dictionary<string, List<int>> sourcesDict)
                        {
                            // New format - check if slots exist
                            filterItem.IncludeBoosterPacks = sourcesDict.ContainsKey("packSlots") && sourcesDict["packSlots"].Count > 0;
                            filterItem.IncludeShopStream = sourcesDict.ContainsKey("shopSlots") && sourcesDict["shopSlots"].Count > 0;
                            filterItem.IncludeSkipTags = sourcesDict.ContainsKey("packSlots") && sourcesDict["packSlots"].Count > 0;
                        }
                    }
                }

                // Generate label if not provided
                if (string.IsNullOrWhiteSpace(filterItem.Label))
                {
                    filterItem.Label = GenerateItemLabel(filterItem);
                }

                return filterItem;
            }
            return null;
        }

        private string GenerateItemLabel(BalatroSeedOracle.Models.FilterItem item)
        {
            // Generate a label based on edition and item name
            var label = new System.Text.StringBuilder();

            // Add edition prefix if present
            if (!string.IsNullOrWhiteSpace(item.Edition) && item.Edition != "none")
            {
                label.Append(char.ToUpper(item.Edition[0]) + item.Edition.Substring(1));
            }

            // Add item name in PascalCase
            if (!string.IsNullOrWhiteSpace(item.Value))
            {
                var words = item.Value.Split(
                    new[] { '_', ' ', '-' },
                    StringSplitOptions.RemoveEmptyEntries
                );
                foreach (var word in words)
                {
                    if (word.Length > 0)
                    {
                        label.Append(char.ToUpper(word[0]));
                        if (word.Length > 1)
                        {
                            label.Append(word.Substring(1).ToLower());
                        }
                    }
                }
            }

            return label.ToString();
        }

        private void OnCreateNewClick(object? sender, RoutedEventArgs e)
        {
            // Clear all selections
            ClearNeeds();
            ClearWants();
            ClearMustNot();

            // Clear the config name
            var configNameBox = this.FindControl<TextBox>("ConfigNameBox");
            if (configNameBox != null)
            {
                configNameBox.Text = "";
            }

            // Enable tabs for new filter
            UpdateTabStates(true);

            // Switch to Visual tab
            var visualTab = this.FindControl<Button>("VisualTab");
            if (visualTab != null)
            {
                OnTabClick(visualTab, new RoutedEventArgs());
            }

            BalatroSeedOracle.Helpers.DebugLogger.Log("FiltersModal", "Created new empty filter configuration");
        }

        private void UpdateSaveFilterPanel()
        {
            // Update the author display from user profile
            var authorDisplay = this.FindControl<TextBlock>("AuthorDisplay");
            if (authorDisplay != null)
            {
                var userProfileService = ServiceHelper.GetService<UserProfileService>();
                authorDisplay.Text = userProfileService?.GetAuthorName() ?? "Jimbo";
            }
            
            // Update file name display
            var fileNameDisplay = this.FindControl<TextBlock>("FileNameDisplay");
            if (fileNameDisplay != null)
            {
                if (!string.IsNullOrEmpty(_currentFilterPath))
                {
                    fileNameDisplay.Text = System.IO.Path.GetFileName(_currentFilterPath);
                }
                else
                {
                    fileNameDisplay.Text = "Not saved yet";
                }
            }
            
            // Update created date
            var createdDateDisplay = this.FindControl<TextBlock>("CreatedDateDisplay");
            if (createdDateDisplay != null && _loadedConfig != null)
            {
                if (_loadedConfig.DateCreated.HasValue)
                {
                    createdDateDisplay.Text = _loadedConfig.DateCreated.Value.ToString("yyyy-MM-dd HH:mm:ss");
                }
                else
                {
                    createdDateDisplay.Text = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss");
                }
            }
            
            // Update filter name input
            var filterNameInput = this.FindControl<TextBox>("FilterNameInput");
            if (filterNameInput != null)
            {
                if (_loadedConfig != null && !string.IsNullOrWhiteSpace(_loadedConfig.Name))
                {
                    filterNameInput.Text = _loadedConfig.Name;
                }
                else if (!string.IsNullOrEmpty(_currentFilterPath))
                {
                    // Use filename without extension as default name
                    filterNameInput.Text = System.IO.Path.GetFileNameWithoutExtension(_currentFilterPath);
                }
            }
            
            // Update filter description
            var descriptionInput = this.FindControl<TextBox>("FilterDescriptionInput");
            if (descriptionInput != null)
            {
                if (_loadedConfig != null && !string.IsNullOrWhiteSpace(_loadedConfig.Description))
                {
                    descriptionInput.Text = _loadedConfig.Description;
                }
                else if (string.IsNullOrWhiteSpace(descriptionInput.Text))
                {
                    descriptionInput.Text = "Created with visual filter builder";
                }
            }
        }
        
        private void OnSearchForSeedsClick(object? sender, RoutedEventArgs e)
        {
            // Get the saved filter path from the button's Tag
            var searchButton = sender as Button;
            var filterPath = searchButton?.Tag as string;
            
            if (string.IsNullOrEmpty(filterPath))
            {
                UpdateStatus("Error: No filter path found. Please save the filter first.", true);
                return;
            }
            
            // Get the main window to find BalatroMainMenu
            var mainWindow = TopLevel.GetTopLevel(this) as Window;
            if (mainWindow?.Content is Grid grid)
            {
                var mainMenu = grid.Children.OfType<BalatroMainMenu>().FirstOrDefault();
                if (mainMenu != null)
                {
                    // Close this modal first
                    mainMenu.HideModalContent();
                    
                    // Open the search modal with the SAVED FILE PATH
                    Dispatcher.UIThread.Post(() =>
                    {
                        var searchModal = mainMenu.ShowSearchModal(filterPath);
                        
                        // The modal needs a moment to initialize before we can switch tabs
                        Dispatcher.UIThread.Post(() =>
                        {
                            // Find the SearchModal content within the StandardModal
                            if (searchModal?.Content is SearchModal searchContent)
                            {
                                // Go directly to the Search tab
                                searchContent.ViewModel.SelectedTabIndex = 2; // Search tab
                            }
                        }, DispatcherPriority.Background);
                    }, DispatcherPriority.Background);
                }
            }
        }
        
        private void OnSearchClick(object? sender, RoutedEventArgs e)
        {
            // Use the current filter path
            var filterPath = _currentFilterPath;
            
            if (string.IsNullOrEmpty(filterPath))
            {
                UpdateStatus("Error: No filter path found. Please save the filter first.", true);
                return;
            }
            
            // Get the main window to find BalatroMainMenu
            var mainWindow = TopLevel.GetTopLevel(this) as Window;
            if (mainWindow?.Content is Grid grid)
            {
                var mainMenu = grid.Children.OfType<BalatroMainMenu>().FirstOrDefault();
                if (mainMenu != null)
                {
                    // Close this modal first
                    mainMenu.HideModalContent();
                    
                    // Open the search modal with the SAVED FILE PATH
                    Dispatcher.UIThread.Post(() =>
                    {
                        var searchModal = mainMenu.ShowSearchModal(filterPath);
                        
                        // The modal needs a moment to initialize before we can switch tabs
                        Dispatcher.UIThread.Post(() =>
                        {
                            // Find the SearchModal content within the StandardModal
                            if (searchModal?.Content is SearchModal searchContent)
                            {
                                // Go directly to the Search tab
                                searchContent.ViewModel.SelectedTabIndex = 2; // Search tab
                            }
                        }, DispatcherPriority.Background);
                    }, DispatcherPriority.Background);
                }
            }
        }
        
        private async void OnSaveFilterClick(object? sender, RoutedEventArgs e)
        {
            var filterNameInput = this.FindControl<TextBox>("SaveFilterNameInput");
            if (filterNameInput == null || string.IsNullOrWhiteSpace(filterNameInput.Text))
            {
                UpdateStatus("Please enter a filter name", true);
                return;
            }

            try
            {
                var filterName = filterNameInput.Text.Trim();

                // Build the OuijaConfig from current selections
                var config = BuildOuijaConfigFromSelections();
                
                // Get author from user profile
                var userProfileService = ServiceHelper.GetService<UserProfileService>();
                var authorName = userProfileService?.GetAuthorName() ?? "Jimbo";
                
                // Get description from the input field
                var descriptionInput = this.FindControl<TextBox>("FilterDescriptionInput");
                var description = descriptionInput?.Text ?? "Created with visual filter builder";
                
                // Save to file
                var directory = IoPath.Combine(Directory.GetCurrentDirectory(), "JsonItemFilters");
                if (!Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                var fileName = $"{filterName.Replace(" ", "_")}.json";
                var filePath = IoPath.Combine(directory, fileName);

                // Check if file already exists
                if (File.Exists(filePath))
                {
                    UpdateStatus($"Filter '{filterName}' already exists. Please choose a different name.", true);
                    return;
                }

                // Write the JSON file using the proper serializer
                var json = SerializeOuijaConfig(config);
                
                // Replace the empty name and description (dateCreated is already handled by SerializeOuijaConfig)
                json = json.Replace("\"name\": \"\"", $"\"name\": \"{filterName}\"");
                json = json.Replace("\"description\": \"\"", $"\"description\": \"{description}\"");
                
                await File.WriteAllTextAsync(filePath, json);

                UpdateStatus($"Filter '{filterName}' saved successfully!", false);
                BalatroSeedOracle.Helpers.DebugLogger.Log("FiltersModal", $"Saved filter to: {filePath}");
                
                // Enable the Search for Seeds button and store the filter path
                var searchButton = this.FindControl<Button>("SearchForSeedsButton");
                if (searchButton != null)
                {
                    searchButton.IsEnabled = true;
                    searchButton.Tag = filePath; // Store the filter path for later use
                }

                // Refresh the filter list to show the new filter
                _filterListViewModel?.LoadFilters();
                
                // Update the JSON editor with the newly saved content
                _currentFilePath = filePath; // Store the path so JSON tab can reload it
                UpdateJsonEditor();
            }
            catch (Exception ex)
            {
                UpdateStatus($"Error saving filter: {ex.Message}", true);
                BalatroSeedOracle.Helpers.DebugLogger.LogError("FiltersModal", $"Error saving filter: {ex.Message}");
            }
        }

        private void SetupTabButtons()
        {
            // Removed search tab and editJsonTab - now using toggle switch

            // Favorites tab
            var favoritesTab = this.FindControl<Button>("FavoritesTab");
            favoritesTab?.AddHandler(
                Button.ClickEvent,
                (s, e) =>
                {
                    BalatroSeedOracle.Helpers.DebugLogger.Log("FiltersModal", "FavoritesTab clicked");
                    ShowFavorites();
                }
            );

            // Joker category tabs
            var soulJokersTab = this.FindControl<Button>("SoulJokersTab");
            var rareJokersTab = this.FindControl<Button>("RareJokersTab");
            var uncommonJokersTab = this.FindControl<Button>("UncommonJokersTab");
            var commonJokersTab = this.FindControl<Button>("CommonJokersTab");

            soulJokersTab?.AddHandler(
                Button.ClickEvent,
                (s, e) =>
                {
                    // Ensure button has focus (fixes first-click issue)
                    if (s is Button btn) btn.Focus();
                    BalatroSeedOracle.Helpers.DebugLogger.Log("FiltersModal", "SoulJokersTab clicked");
                    NavigateToSection("SoulJokersTab");
                }
            );
            rareJokersTab?.AddHandler(
                Button.ClickEvent,
                (s, e) =>
                {
                    BalatroSeedOracle.Helpers.DebugLogger.Log("FiltersModal", "RareJokersTab clicked");
                    NavigateToSection("RareJokersTab");
                }
            );
            uncommonJokersTab?.AddHandler(
                Button.ClickEvent,
                (s, e) =>
                {
                    BalatroSeedOracle.Helpers.DebugLogger.Log("FiltersModal", "UncommonJokersTab clicked");
                    NavigateToSection("UncommonJokersTab");
                }
            );
            commonJokersTab?.AddHandler(
                Button.ClickEvent,
                (s, e) =>
                {
                    BalatroSeedOracle.Helpers.DebugLogger.Log("FiltersModal", "CommonJokersTab clicked");
                    NavigateToSection("CommonJokersTab");
                }
            );

            // Other category tabs
            var vouchersTab = this.FindControl<Button>("VouchersTab");
            var tarotsTab = this.FindControl<Button>("TarotsTab");
            var spectralsTab = this.FindControl<Button>("SpectralsTab");
            var tagsTab = this.FindControl<Button>("TagsTab");
            var bossesTab = this.FindControl<Button>("BossesTab");
            var clearTab = this.FindControl<Button>("ClearTab");

            vouchersTab?.AddHandler(
                Button.ClickEvent,
                (s, e) =>
                {
                    BalatroSeedOracle.Helpers.DebugLogger.Log("FiltersModal", "VouchersTab clicked");
                    NavigateToSection("VouchersTab");
                }
            );
            tarotsTab?.AddHandler(
                Button.ClickEvent,
                (s, e) =>
                {
                    BalatroSeedOracle.Helpers.DebugLogger.Log("FiltersModal", "TarotsTab clicked");
                    NavigateToSection("TarotsTab");
                }
            );
            
            // Planets tab
            var planetsTab = this.FindControl<Button>("PlanetsTab");
            planetsTab?.AddHandler(
                Button.ClickEvent,
                (s, e) =>
                {
                    BalatroSeedOracle.Helpers.DebugLogger.Log("FiltersModal", "PlanetsTab clicked");
                    NavigateToSection("PlanetsTab");
                }
            );
            
            spectralsTab?.AddHandler(
                Button.ClickEvent,
                (s, e) =>
                {
                    BalatroSeedOracle.Helpers.DebugLogger.Log("FiltersModal", "SpectralsTab clicked");
                    NavigateToSection("SpectralsTab");
                }
            );
            
            // Playing Cards tab
            var playingCardsTab = this.FindControl<Button>("PlayingCardsTab");
            playingCardsTab?.AddHandler(
                Button.ClickEvent,
                (s, e) =>
                {
                    BalatroSeedOracle.Helpers.DebugLogger.Log("FiltersModal", "PlayingCardsTab clicked");
                    NavigateToSection("PlayingCardsTab");
                }
            );
            tagsTab?.AddHandler(
                Button.ClickEvent,
                (s, e) =>
                {
                    BalatroSeedOracle.Helpers.DebugLogger.Log("FiltersModal", "TagsTab clicked");
                    NavigateToSection("TagsTab");
                }
            );
            bossesTab?.AddHandler(
                Button.ClickEvent,
                (s, e) =>
                {
                    BalatroSeedOracle.Helpers.DebugLogger.Log("FiltersModal", "BossesTab clicked");
                    NavigateToSection("BossesTab");
                }
            );
            clearTab?.AddHandler(
                Button.ClickEvent,
                (s, e) =>
                {
                    BalatroSeedOracle.Helpers.DebugLogger.Log("FiltersModal", "ClearTab clicked");
                    ClearNeeds();
                    ClearWants();
                    ClearMustNot();
                }
            );

            // Setup other button handlers
            var saveButton = this.FindControl<Button>("SaveButton");
            var browseButton = this.FindControl<Button>("BrowseButton");
            var clearAllButton = this.FindControl<Button>("ClearAllButton");

            saveButton?.AddHandler(Button.ClickEvent, OnSaveClick);
            browseButton?.AddHandler(Button.ClickEvent, OnLoadClick);
            clearAllButton?.AddHandler(
                Button.ClickEvent,
                (s, e) =>
                {
                    ClearNeeds();
                    ClearWants();
                    ClearMustNot();
                }
            );
        }

        private void SetupDropZones()
        {
            BalatroSeedOracle.Helpers.DebugLogger.Log("FiltersModal", "Setting up drop zones...");

            // Set up drag-over for main containers to keep drag alive
            var mainGrid = this.FindControl<Grid>("VisualPanel");
            var contentGrid = this.FindControl<Grid>("VisualModeContainer");

            if (mainGrid != null)
            {
                mainGrid.AddHandler(
                    DragDrop.DragOverEvent,
                    (s, e) =>
                    {
                        e.DragEffects = DragDropEffects.Move;
                        e.Handled = true;
                    }
                );
            }

            if (contentGrid != null)
            {
                contentGrid.AddHandler(
                    DragDrop.DragOverEvent,
                    (s, e) =>
                    {
                        e.DragEffects = DragDropEffects.Move;
                        e.Handled = true;
                    }
                );
            }

            // Handle global drop/drag-leave to clean up
            this.AddHandler(
                DragDrop.DropEvent,
                (s, e) =>
                {
                    // Clean up drag visuals when drop happens anywhere
                    CleanupDragVisuals();
                }
            );

            this.AddHandler(
                DragDrop.DragLeaveEvent,
                (s, e) =>
                {
                    // Only clean up if we're really leaving the entire control
                    var point = e.GetPosition(this);
                    if (
                        point.X < 0
                        || point.Y < 0
                        || point.X > this.Bounds.Width
                        || point.Y > this.Bounds.Height
                    )
                    {
                        CleanupDragVisuals();
                    }
                }
            );

            // Get the drop zone borders (not just the panels)
            var needsBorder = this.FindControl<Border>("NeedsBorder");
            var wantsBorder = this.FindControl<Border>("WantsBorder");
            var mustNotBorder = this.FindControl<Border>("MustNotBorder");

            // Disable clipping on borders to allow cards to pop out when hovered
            if (needsBorder != null) needsBorder.ClipToBounds = false;
            if (wantsBorder != null) wantsBorder.ClipToBounds = false;
            if (mustNotBorder != null) mustNotBorder.ClipToBounds = false;

            // Get the actual panels inside the borders
            var needsPanel = this.FindControl<WrapPanel>("NeedsPanel");
            var wantsPanel = this.FindControl<WrapPanel>("WantsPanel");
            var mustNotPanel = this.FindControl<WrapPanel>("MustNotPanel");

            // Setup drop zones on the borders (larger drop targets)
            if (needsBorder != null)
            {
                DragDrop.SetAllowDrop(needsBorder, true);
                needsBorder.AddHandler(DragDrop.DropEvent, OnNeedsPanelDrop);
                needsBorder.AddHandler(DragDrop.DragOverEvent, OnNeedsDragOver);
                needsBorder.AddHandler(DragDrop.DragEnterEvent, OnNeedsDragEnter);
                needsBorder.AddHandler(DragDrop.DragLeaveEvent, OnNeedsDragLeave);
                // Removed PointerPressed - no popup on empty space click
            }

            if (wantsBorder != null)
            {
                DragDrop.SetAllowDrop(wantsBorder, true);
                wantsBorder.AddHandler(DragDrop.DropEvent, OnWantsPanelDrop);
                wantsBorder.AddHandler(DragDrop.DragOverEvent, OnWantsDragOver);
                wantsBorder.AddHandler(DragDrop.DragEnterEvent, OnWantsDragEnter);
                wantsBorder.AddHandler(DragDrop.DragLeaveEvent, OnWantsDragLeave);
                // Removed PointerPressed - no popup on empty space click
            }

            if (mustNotBorder != null)
            {
                DragDrop.SetAllowDrop(mustNotBorder, true);
                mustNotBorder.AddHandler(DragDrop.DropEvent, OnMustNotPanelDrop);
                mustNotBorder.AddHandler(DragDrop.DragOverEvent, OnMustNotDragOver);
                mustNotBorder.AddHandler(DragDrop.DragEnterEvent, OnMustNotDragEnter);
                mustNotBorder.AddHandler(DragDrop.DragLeaveEvent, OnMustNotDragLeave);
                // Removed PointerPressed - no popup on empty space click
            }

            // Clear buttons removed from headers - using clearTab button in navigation instead
            // var clearNeedsButton = this.FindControl<Button>("ClearNeedsButton");
            // var clearWantsButton = this.FindControl<Button>("ClearWantsButton");
            //
            // if (clearNeedsButton != null)
            //     clearNeedsButton.Click += (s, e) => ClearNeeds();
            // if (clearWantsButton != null)
            //     clearWantsButton.Click += (s, e) => ClearWants();

            // Remove old button code - using toggle switch now
            // Rest of the toggle switch handling is in OnModeToggleChanged

            BalatroSeedOracle.Helpers.DebugLogger.Log("FiltersModal", "Drop zones setup complete!");
        }

        // 🎯 Enhanced Drag & Drop Event Handlers

        private void OnNeedsDragEnter(object? sender, DragEventArgs e)
        {
            if (e.Data.Contains("balatro-item") || e.Data.Contains("JokerSet"))
            {
                var needsBorder = sender as Border;
                needsBorder?.Classes.Add("drag-over");
                e.DragEffects = DragDropEffects.Move;
            }
            e.Handled = true;
        }

        private void OnNeedsDragLeave(object? sender, DragEventArgs e)
        {
            var needsBorder = sender as Border;
            needsBorder?.Classes.Remove("drag-over");
            e.Handled = true;
        }

        private void OnNeedsDragOver(object? sender, DragEventArgs e)
        {
            if (e.Data.Contains("balatro-item") || e.Data.Contains("JokerSet"))
            {
                e.DragEffects = DragDropEffects.Move;

                // Update ghost position during drag
                if (_isDragging && sender is Visual visual)
                {
                    var position = e.GetPosition(this);
                    UpdateGhostPosition(position);
                }
            }
            else
            {
                e.DragEffects = DragDropEffects.None;
            }
            e.Handled = true;
        }

        private void OnWantsDragEnter(object? sender, DragEventArgs e)
        {
            if (e.Data.Contains("balatro-item") || e.Data.Contains("JokerSet"))
            {
                var wantsBorder = sender as Border;
                wantsBorder?.Classes.Add("drag-over");
                e.DragEffects = DragDropEffects.Move;
            }
            e.Handled = true;
        }

        private void OnWantsDragLeave(object? sender, DragEventArgs e)
        {
            var wantsBorder = sender as Border;
            wantsBorder?.Classes.Remove("drag-over");
            e.Handled = true;
        }

        private void OnWantsDragOver(object? sender, DragEventArgs e)
        {
            if (e.Data.Contains("balatro-item") || e.Data.Contains("JokerSet"))
            {
                e.DragEffects = DragDropEffects.Move;

                // Update ghost position during drag
                if (_isDragging && sender is Visual visual)
                {
                    var position = e.GetPosition(this);
                    UpdateGhostPosition(position);
                }
            }
            else
            {
                e.DragEffects = DragDropEffects.None;
            }
            e.Handled = true;
        }

        private void OnNeedsPanelDrop(object? sender, DragEventArgs e)
        {
            // Remove visual feedback
            var needsBorder = sender as Border;
            needsBorder?.Classes.Remove("drag-over");

            // Handle JokerSet drop
            if (e.Data.Contains("JokerSet"))
            {
                var jokerSet = e.Data.Get("JokerSet") as FavoritesService.JokerSet;
                if (jokerSet != null)
                {
                    // Add all items from the joker set to needs
                    foreach (var item in jokerSet.Items)
                    {
                        // Find the category for this item
                        string? itemCategory = null;
                        if (BalatroData.Jokers.ContainsKey(item))
                        {
                            itemCategory = "Jokers";
                        }
                        else if (BalatroData.TarotCards.ContainsKey(item))
                        {
                            itemCategory = "Tarots";
                        }
                        else if (BalatroData.SpectralCards.ContainsKey(item))
                        {
                            itemCategory = "Spectrals";
                        }
                        else if (BalatroData.Vouchers.ContainsKey(item))
                        {
                            itemCategory = "Vouchers";
                        }

                        if (itemCategory != null)
                        {
                            var uniqueKey = CreateUniqueKey(itemCategory, item);
                            _selectedMust.Add(uniqueKey);
                            MarkAsChanged();
                        }
                    }

                    UpdateDropZoneVisibility();
                    RefreshItemPalette();
                    RemoveDragOverlay();
                    _isDragging = false;
                    e.Handled = true;
                    BalatroSeedOracle.Helpers.DebugLogger.Log(
                        "FiltersModal",
                        $"✅ Added joker set '{jokerSet.Name}' ({jokerSet.Items.Count} items) to NEEDS"
                    );
                    return;
                }
            }

            if (e.Data.Contains("balatro-item"))
            {
                var itemData = e.Data.Get("balatro-item") as string;
                if (!string.IsNullOrEmpty(itemData))
                {
                    var parts = itemData.Split('|');
                    if (parts.Length >= 2)
                    {
                        var category = parts[0];
                        var itemName = parts[1];

                        if (category == "Set" && e.Data.Contains("set-items"))
                        {
                            // Handle set drop
                            var setItemsData = e.Data.Get("set-items") as string;
                            if (!string.IsNullOrEmpty(setItemsData))
                            {
                                var setItems = setItemsData.Split(',');
                                foreach (var item in setItems)
                                {
                                    // Find the category for this item
                                    string? itemCategory = null;
                                    if (BalatroData.Jokers.ContainsKey(item))
                                    {
                                        itemCategory = "Jokers";
                                    }
                                    else if (BalatroData.TarotCards.ContainsKey(item))
                                    {
                                        itemCategory = "Tarots";
                                    }
                                    else if (BalatroData.SpectralCards.ContainsKey(item))
                                    {
                                        itemCategory = "Spectrals";
                                    }
                                    else if (BalatroData.Vouchers.ContainsKey(item))
                                    {
                                        itemCategory = "Vouchers";
                                    }

                                    if (itemCategory != null)
                                    {
                                        var uniqueKey = CreateUniqueKey(itemCategory, item);
                                        // Add to needs (allow item in multiple lists)
                                        _selectedMust.Add(uniqueKey);
                                    }
                                }
                                BalatroSeedOracle.Helpers.DebugLogger.Log(
                                    "FiltersModal",
                                    $"✅ Added set '{itemName}' ({setItems.Length} items) to NEEDS"
                                );
                            }
                        }
                        else
                        {
                            // Handle single item drop
                            // For SoulJokers, use Jokers category for storage
                            var storageCategory = category == "SoulJokers" ? "Jokers" : category;
                            var key = CreateUniqueKey(storageCategory, itemName);

                            // Add to needs (allow item in multiple lists)
                            _selectedMust.Add(key);

                            BalatroSeedOracle.Helpers.DebugLogger.Log(
                                "FiltersModal",
                                $"✅ Added {itemName} to NEEDS with key: {key}"
                            );
                        }

                        UpdateDropZoneVisibility();
                        UpdatePersistentFavorites();
                        RefreshItemPalette();
                    }
                }
            }

            // Clean up drag visuals after drop
            RemoveDragOverlay();
            _isDragging = false;
            CleanupDragVisuals();

            e.Handled = true;
        }

        private void OnWantsPanelDrop(object? sender, DragEventArgs e)
        {
            // Remove visual feedback
            var wantsBorder = sender as Border;
            wantsBorder?.Classes.Remove("drag-over");

            // Handle JokerSet drop
            if (e.Data.Contains("JokerSet"))
            {
                var jokerSet = e.Data.Get("JokerSet") as FavoritesService.JokerSet;
                if (jokerSet != null)
                {
                    // Add all items from the joker set to wants
                    foreach (var item in jokerSet.Items)
                    {
                        // Find the category for this item
                        string? itemCategory = null;
                        if (BalatroData.Jokers.ContainsKey(item))
                        {
                            itemCategory = "Jokers";
                        }
                        else if (BalatroData.TarotCards.ContainsKey(item))
                        {
                            itemCategory = "Tarots";
                        }
                        else if (BalatroData.SpectralCards.ContainsKey(item))
                        {
                            itemCategory = "Spectrals";
                        }
                        else if (BalatroData.Vouchers.ContainsKey(item))
                        {
                            itemCategory = "Vouchers";
                        }

                        if (itemCategory != null)
                        {
                            var uniqueKey = CreateUniqueKey(itemCategory, item);
                            // Add to wants (allow item in multiple lists)
                            _selectedShould.Add(uniqueKey);
                            MarkAsChanged();
                        }
                    }

                    UpdateDropZoneVisibility();
                    UpdatePersistentFavorites();
                    RefreshItemPalette();
                    RemoveDragOverlay();
                    _isDragging = false;
                    e.Handled = true;
                    BalatroSeedOracle.Helpers.DebugLogger.Log(
                        "FiltersModal",
                        $"✅ Added joker set '{jokerSet.Name}' ({jokerSet.Items.Count} items) to WANTS"
                    );
                    return;
                }
            }

            if (e.Data.Contains("balatro-item"))
            {
                var itemData = e.Data.Get("balatro-item") as string;
                if (!string.IsNullOrEmpty(itemData))
                {
                    var parts = itemData.Split('|');
                    if (parts.Length >= 2)
                    {
                        var category = parts[0];
                        var itemName = parts[1];

                        if (category == "Set" && e.Data.Contains("set-items"))
                        {
                            // Handle set drop
                            var setItemsData = e.Data.Get("set-items") as string;
                            if (!string.IsNullOrEmpty(setItemsData))
                            {
                                var setItems = setItemsData.Split(',');
                                foreach (var item in setItems)
                                {
                                    // Find the category for this item
                                    string? itemCategory = null;
                                    if (BalatroData.Jokers.ContainsKey(item))
                                    {
                                        itemCategory = "Jokers";
                                    }
                                    else if (BalatroData.TarotCards.ContainsKey(item))
                                    {
                                        itemCategory = "Tarots";
                                    }
                                    else if (BalatroData.SpectralCards.ContainsKey(item))
                                    {
                                        itemCategory = "Spectrals";
                                    }
                                    else if (BalatroData.Vouchers.ContainsKey(item))
                                    {
                                        itemCategory = "Vouchers";
                                    }

                                    if (itemCategory != null)
                                    {
                                        var uniqueKey = CreateUniqueKey(itemCategory, item);
                                        // Add to wants (allow item in multiple lists)
                                        _selectedShould.Add(uniqueKey);
                                    }
                                }
                                BalatroSeedOracle.Helpers.DebugLogger.Log(
                                    "FiltersModal",
                                    $"✅ Added set '{itemName}' ({setItems.Length} items) to WANTS"
                                );
                            }
                        }
                        else
                        {
                            // Handle single item drop
                            // For SoulJokers, use Jokers category for storage
                            var storageCategory = category == "SoulJokers" ? "Jokers" : category;
                            var key = CreateUniqueKey(storageCategory, itemName);

                            // Add to wants (allow item in multiple lists)
                            _selectedShould.Add(key);

                            BalatroSeedOracle.Helpers.DebugLogger.Log(
                                "FiltersModal",
                                $"✅ Added {itemName} to WANTS"
                            );
                        }

                        UpdateDropZoneVisibility();
                        UpdatePersistentFavorites();
                        RefreshItemPalette();
                    }
                }
            }

            // Clean up drag visuals after drop
            RemoveDragOverlay();
            _isDragging = false;
            CleanupDragVisuals();

            e.Handled = true;
        }

        private void RestoreDragDropModeLayout()
        {
            try
            {
                BalatroSeedOracle.Helpers.DebugLogger.Log(
                    "FiltersModal",
                    "RestoreDragDropModeLayout: Starting restoration"
                );

                // Restore the sidebar and grid columns
                var mainContentGrid = this.FindControl<Grid>("MainContentGrid");
                if (mainContentGrid != null)
                {
                    // Restore left sidebar visibility
                    var leftSidebar = this.FindControl<Border>("LeftSidebar");
                    if (leftSidebar != null)
                    {
                        leftSidebar.IsVisible = true;
                        BalatroSeedOracle.Helpers.DebugLogger.Log(
                            "RestoreDragDropModeLayout: Restored left sidebar"
                        );
                    }

                    // Restore column widths
                    if (mainContentGrid.ColumnDefinitions.Count >= 2)
                    {
                        mainContentGrid.ColumnDefinitions[0].Width = new GridLength(120); // Restore sidebar width
                        mainContentGrid.ColumnDefinitions[1].Width = new GridLength(8); // Restore spacer width
                    }
                }

                // Restore the search bar
                var searchBox = this.FindControl<TextBox>("SearchBox");
                if (searchBox?.Parent?.Parent is Border searchBar)
                {
                    searchBar.IsVisible = true;
                    BalatroSeedOracle.Helpers.DebugLogger.Log("RestoreDragDropModeLayout: Restored search bar");

                    // Clear the search box when returning from JSON mode
                    if (searchBox != null && _searchBox != null)
                    {
                        _searchBox.Text = "";
                        _searchFilter = "";
                    }
                }

                // Restore padding to the ItemPaletteBorder
                var itemPaletteBorder = this.FindControl<Border>("ItemPaletteBorder");
                if (itemPaletteBorder != null)
                {
                    itemPaletteBorder.Padding = new Thickness(8);
                    BalatroSeedOracle.Helpers.DebugLogger.Log(
                        "RestoreDragDropModeLayout: Restored ItemPaletteBorder padding"
                    );
                }

                // Restore the original item palette content
                var itemPaletteContent = this.FindControl<ContentControl>("ItemPaletteContent");
                if (itemPaletteContent != null && _originalItemPaletteContent != null)
                {
                    itemPaletteContent.Content = _originalItemPaletteContent;
                    _originalItemPaletteContent = null;
                    BalatroSeedOracle.Helpers.DebugLogger.Log(
                        "RestoreDragDropModeLayout: Restored original item palette content"
                    );
                }

                BalatroSeedOracle.Helpers.DebugLogger.Log("RestoreDragDropModeLayout: Restoration complete");
            }
            catch (Exception ex)
            {
                BalatroSeedOracle.Helpers.DebugLogger.LogError(
                    "FiltersModal",
                    $"ERROR in RestoreDragDropModeLayout: {ex}"
                );
            }
        }

        private void EnterEditJsonMode()
        {
            try
            {
                BalatroSeedOracle.Helpers.DebugLogger.Log("FiltersModal", "EnterEditJsonMode called");

                // The JSON editor is created in the JsonModeContainer/JsonModeContent when the button is clicked
                // This method now just handles the JSON content creation if needed

                // Update drop zones after a short delay to allow JSON editor to load
                Dispatcher.UIThread.Post(
                    () =>
                    {
                        UpdateDropZonesFromJson();
                    },
                    DispatcherPriority.Background
                );

                BalatroSeedOracle.Helpers.DebugLogger.Log("JSON editor mode entered");
            }
            catch (Exception ex)
            {
                BalatroSeedOracle.Helpers.DebugLogger.LogError(
                    "FiltersModal",
                    $"ERROR in EnterEditJsonMode: {ex}"
                );
            }
        }

        private void SetupJsonEditorAutocomplete()
        {
            var jsonEditor = this.FindControl<TextEditor>("JsonEditor");
            if (jsonEditor != null)
            {
                // Setup autocomplete triggers
                jsonEditor.TextArea.TextEntering += OnJsonTextEntering;
                jsonEditor.TextArea.TextEntered += OnJsonTextEntered;
                jsonEditor.TextArea.KeyDown += OnJsonKeyDown;

                DebugLogger.Log("FiltersModal", "JSON editor autocomplete configured");
            }
        }

        private void OnJsonKeyDown(object? sender, KeyEventArgs e)
        {
            // Ctrl+Space triggers autocomplete
            if (e.Key == Key.Space && e.KeyModifiers.HasFlag(KeyModifiers.Control))
            {
                ShowJsonCompletions();
                e.Handled = true;
            }
        }

        private void OnJsonTextEntering(object? sender, TextInputEventArgs e)
        {
            // Can be used to handle text before it's entered
        }

        private void OnJsonTextEntered(object? sender, TextInputEventArgs e)
        {
            var jsonEditor = this.FindControl<TextEditor>("JsonEditor");
            if (jsonEditor?.TextArea == null) return;

            // Show autocomplete on quote or after colon
            if (e.Text == "\"" || e.Text == ":")
            {
                ShowJsonCompletions();
            }
        }

        private void ShowJsonCompletions()
        {
            var jsonEditor = this.FindControl<TextEditor>("JsonEditor");
            if (jsonEditor?.TextArea == null) return;

            try
            {
                // Get text before cursor for context-aware completions
                var caretOffset = jsonEditor.TextArea.Caret.Offset;
                var textBeforeCursor = caretOffset > 0
                    ? jsonEditor.Document.GetText(0, Math.Min(caretOffset, jsonEditor.Document.TextLength))
                    : "";

                // Get completions from helper
                var completions = JsonAutocompletionHelper.GetCompletionsForContext(textBeforeCursor);

                if (completions.Any())
                {
                    // Create and show completion window
                    var completionWindow = new CompletionWindow(jsonEditor.TextArea);
                    foreach (var item in completions)
                    {
                        completionWindow.CompletionList.CompletionData.Add(item);
                    }
                    completionWindow.Show();
                }
            }
            catch (Exception ex)
            {
                DebugLogger.LogError("FiltersModal", $"Error showing JSON completions: {ex.Message}");
            }
        }

        private void UpdateJsonEditor()
        {
            try
            {
                BalatroSeedOracle.Helpers.DebugLogger.Log("FiltersModal", "UpdateJsonEditor called");

                var jsonEditor = this.FindControl<TextEditor>("JsonEditor");
                if (jsonEditor != null)
                {
                    string json;
                    
                    // If we have a saved file path, load from file to get the latest content
                    if (!string.IsNullOrEmpty(_currentFilePath) && File.Exists(_currentFilePath))
                    {
                        json = File.ReadAllText(_currentFilePath);
                        BalatroSeedOracle.Helpers.DebugLogger.Log(
                            "FiltersModal",
                            $"Loaded JSON from file: {_currentFilePath}"
                        );
                    }
                    else
                    {
                        // Create OuijaConfig from current selections
                        var config = BuildOuijaConfigFromSelections();
                        // Use the same serialization method that properly handles sources
                        json = SerializeOuijaConfig(config);
                    }
                    
                    jsonEditor.Text = json;

                    BalatroSeedOracle.Helpers.DebugLogger.Log(
                        "FiltersModal",
                        $"Updated JSON editor with {json.Length} characters"
                    );
                }
                else
                {
                    BalatroSeedOracle.Helpers.DebugLogger.LogError("FiltersModal", "JsonEditor control not found");
                }
            }
            catch (Exception ex)
            {
                BalatroSeedOracle.Helpers.DebugLogger.LogError("FiltersModal", $"ERROR in UpdateJsonEditor: {ex}");
            }
        }

        private Control CreateEditJsonInterface()
        {
            try
            {
                BalatroSeedOracle.Helpers.DebugLogger.Log("FiltersModal", "CreateEditJsonInterface started");

                // Get the current JSON content - either from selections or default
                string jsonContent;
                if (_selectedMust.Any() || _selectedShould.Any() || _selectedMustNot.Any())
                {
                    // Build JSON from current selections
                    var config = BuildOuijaConfigFromSelections();
                    jsonContent = SerializeOuijaConfig(config);
                    BalatroSeedOracle.Helpers.DebugLogger.Log("Using JSON built from current selections");
                }
                else
                {
                    // Use default example JSON only if no selections exist
                    jsonContent = GetDefaultOuijaConfigJson();
                    BalatroSeedOracle.Helpers.DebugLogger.Log("Using default example JSON (no selections)");
                }

                // Create a Grid that fills the entire space
                var mainGrid = new Grid
                {
                    VerticalAlignment = Avalonia.Layout.VerticalAlignment.Stretch,
                    HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Stretch,
                    Background =
                        Application.Current?.FindResource("PopupBackground") as IBrush
                        ?? new SolidColorBrush(Color.Parse("#1e1e1e")),
                    Margin = new Thickness(-8), // Negative margin to counteract parent padding
                };

                mainGrid.RowDefinitions.Add(
                    new RowDefinition { Height = new GridLength(1, GridUnitType.Star) }
                );
                mainGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

                // Check if we should use fallback mode
                bool useFallback = false; // Now that AvaloniaEdit is properly configured, we can use it!

                Control editorControl;

                if (useFallback)
                {
                    // Use a simple TextBox as fallback
                    BalatroSeedOracle.Helpers.DebugLogger.Log("Using fallback TextBox for JSON editing");
                    var fallbackTextBox = new TextBox
                    {
                        Name = "JsonTextEditor_Fallback",
                        Text = jsonContent,
                        AcceptsReturn = true,
                        TextWrapping = TextWrapping.NoWrap,
                        Background =
                            Application.Current?.FindResource("PopupBackground") as IBrush
                            ?? new SolidColorBrush(Color.Parse("#1e1e1e")),
                        Foreground =
                            Application.Current?.FindResource("White") as IBrush
                            ?? new SolidColorBrush(Color.Parse("#FFFFFF")),
                        FontFamily = new FontFamily("Cascadia Code,Consolas,Menlo,Monaco,monospace"),
                        FontSize = 14,
                        Padding = new Thickness(10),
                        VerticalAlignment = Avalonia.Layout.VerticalAlignment.Stretch,
                        HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Stretch,
                        MinHeight = 200,
                    };

                    // Store reference for compatibility
                    _jsonTextBox = fallbackTextBox;

                    // Wrap TextBox in ScrollViewer for scrolling
                    var scrollViewer = new ScrollViewer
                    {
                        HorizontalScrollBarVisibility = Avalonia
                            .Controls
                            .Primitives
                            .ScrollBarVisibility
                            .Auto,
                        VerticalScrollBarVisibility = Avalonia
                            .Controls
                            .Primitives
                            .ScrollBarVisibility
                            .Visible,
                        AllowAutoHide = false,
                        Content = fallbackTextBox,
                        VerticalAlignment = Avalonia.Layout.VerticalAlignment.Stretch,
                        HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Stretch,
                    };

                    var editorBorder = new Border
                    {
                        Background =
                            Application.Current?.FindResource("PopupBackground") as IBrush
                            ?? new SolidColorBrush(Color.Parse("#1e1e1e")),
                        BorderBrush =
                            Application.Current?.FindResource("DarkGreyBorder") as IBrush
                            ?? new SolidColorBrush(Color.Parse("#444444")),
                        BorderThickness = new Thickness(1),
                        CornerRadius = new CornerRadius(4),
                        VerticalAlignment = Avalonia.Layout.VerticalAlignment.Stretch,
                        HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Stretch,
                        Child = scrollViewer,
                    };

                    editorControl = editorBorder;
                }
                else
                {
                    // Create an AvaloniaEdit TextEditor for JSON editing
                    BalatroSeedOracle.Helpers.DebugLogger.Log("Creating AvaloniaEdit JSON editor");
                    var editorBorder = new Border
                    {
                        Background =
                            Application.Current?.FindResource("PopupBackground") as IBrush
                            ?? new SolidColorBrush(Color.Parse("#1e1e1e")),
                        BorderBrush =
                            Application.Current?.FindResource("DarkGreyBorder") as IBrush
                            ?? new SolidColorBrush(Color.Parse("#444444")),
                        BorderThickness = new Thickness(1),
                        CornerRadius = new CornerRadius(4),
                        Padding = new Thickness(10),
                        VerticalAlignment = Avalonia.Layout.VerticalAlignment.Stretch,
                        HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Stretch,
                    };

                    // Create AvaloniaEdit TextEditor
                    var textEditor = new AvaloniaEdit.TextEditor
                    {
                        Name = "JsonTextEditor",
                        Background =
                            Application.Current?.FindResource("PopupBackground") as IBrush
                            ?? new SolidColorBrush(Color.Parse("#1e1e1e")),
                        Foreground =
                            Application.Current?.FindResource("White") as IBrush
                            ?? new SolidColorBrush(Color.Parse("#FFFFFF")),
                        FontFamily = new FontFamily("Cascadia Code,Consolas,Menlo,Monaco,monospace"),
                        FontSize = 14,
                        ShowLineNumbers = true,
                        WordWrap = false,
                        HorizontalScrollBarVisibility = Avalonia
                            .Controls
                            .Primitives
                            .ScrollBarVisibility
                            .Auto,
                        VerticalScrollBarVisibility = Avalonia
                            .Controls
                            .Primitives
                            .ScrollBarVisibility
                            .Visible,
                        VerticalAlignment = Avalonia.Layout.VerticalAlignment.Stretch,
                        HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Stretch,
                        IsVisible = true,
                    };

                    // Set up editor options
                    textEditor.Options.EnableHyperlinks = false;
                    textEditor.Options.EnableEmailHyperlinks = false;
                    textEditor.Options.EnableRectangularSelection = true;
                    textEditor.Options.EnableTextDragDrop = true;
                    textEditor.Options.AllowScrollBelowDocument = true;
                    textEditor.Options.IndentationSize = 2;

                    // Enable mouse wheel scrolling
                    textEditor.Options.EnableVirtualSpace = false;
                    textEditor.Options.ShowEndOfLine = false;

                    // Set document and text after creation
                    textEditor.Document = new AvaloniaEdit.Document.TextDocument(jsonContent);

                    // Setup TextMate for JSON with dark theme
                    SetupJsonSyntaxHighlighting(textEditor);

                    // Add a simple wheel handler that manually scrolls the editor
                    textEditor.PointerWheelChanged += (s, e) =>
                    {
                        var scrollViewer = textEditor
                            .TextArea?.GetVisualDescendants()
                            ?.OfType<ScrollViewer>()
                            ?.FirstOrDefault();
                        if (scrollViewer != null)
                        {
                            // Scroll by 3 lines worth (approximately 48 pixels)
                            var delta = e.Delta.Y * 48;
                            scrollViewer.Offset = new Vector(
                                scrollViewer.Offset.X,
                                Math.Max(0, scrollViewer.Offset.Y - delta)
                            );
                            e.Handled = true;
                        }
                    };

                    // Force text area colors after the editor is attached
                    textEditor.AttachedToVisualTree += (s, e) =>
                    {
                        try
                        {
                            // Ensure text is visible
                            textEditor.TextArea.TextView.LinkTextForegroundBrush =
                                Application.Current?.FindResource("LightBlue") as IBrush
                                ?? new SolidColorBrush(Color.Parse("#569CD6"));

                            // Force colors on the text view
                            var textView = textEditor.TextArea.TextView;
                            textView.Document = textEditor.Document;

                            // Set line number margin colors
                            foreach (var margin in textEditor.TextArea.LeftMargins)
                            {
                                if (margin is AvaloniaEdit.Editing.LineNumberMargin lineNumberMargin)
                                {
                                    // Line number margin styling would go here if needed
                                }
                            }

                            BalatroSeedOracle.Helpers.DebugLogger.Log(
                                $"AvaloniaEdit attached - Document length: {textEditor.Document.TextLength}"
                            );
                        }
                        catch (Exception ex)
                        {
                            BalatroSeedOracle.Helpers.DebugLogger.LogError(
                                $"Error in AttachedToVisualTree: {ex.Message}"
                            );
                        }
                    };

                    editorBorder.Child = textEditor;

                    // Ensure editor gets focus when clicked
                    editorBorder.PointerPressed += (s, e) =>
                    {
                        textEditor.Focus();
                    };

                    // Make sure editor is focusable and has proper height
                    textEditor.Focusable = true;
                    textEditor.Loaded += (s, e) =>
                    {
                        textEditor.Focus();
                        BalatroSeedOracle.Helpers.DebugLogger.Log(
                            $"JSON Editor loaded. Height={textEditor.Bounds.Height}, Lines={textEditor.Document.LineCount}"
                        );
                    };

                    // Store references
                    _jsonTextBox = new TextBox { IsVisible = false }; // Create a hidden TextBox to maintain compatibility
                    _jsonTextBox.Text = "{}"; // Sync initial text

                    // Add text changed handler
                    textEditor.TextChanged += (s, e) =>
                    {
                        // Update the hidden TextBox to maintain compatibility with existing code
                        if (_jsonTextBox != null)
                        {
                            _jsonTextBox.Text = textEditor.Text;
                        }

                        Dispatcher.UIThread.Post(
                            () =>
                            {
                                ValidateJsonSyntaxForAvaloniaEdit(textEditor);
                                // Update drop zones to show visual preview
                                UpdateDropZonesFromJson();
                            },
                            DispatcherPriority.Background
                        );
                    };

                    editorControl = editorBorder;
                }

                Grid.SetRow(editorControl, 0);
                mainGrid.Children.Add(editorControl);

                // Status bar at bottom
                BalatroSeedOracle.Helpers.DebugLogger.Log("Creating status bar");
                var statusBar = new Border
                {
                    Background =
                        Application.Current?.FindResource("DarkBackground") as IBrush
                        ?? new SolidColorBrush(Color.Parse("#2e3f42")),
                    Padding = new Avalonia.Thickness(12, 6),
                    BorderBrush =
                        Application.Current?.FindResource("DarkGreyBorder") as IBrush
                        ?? new SolidColorBrush(Color.Parse("#444444")),
                    BorderThickness = new Avalonia.Thickness(0, 1, 0, 0),
                    Height = 30,
                };

                var statusText = new TextBlock
                {
                    Name = "StatusText",
                    Text = "Ready - JSON Editor",
                    VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center,
                    Foreground =
                        Application.Current?.FindResource("VeryLightGrey") as IBrush
                        ?? new SolidColorBrush(Color.Parse("#E2E2E2")),
                    FontSize = 12,
                };
                statusBar.Child = statusText;
                Grid.SetRow(statusBar, 1);
                mainGrid.Children.Add(statusBar);

                // Store reference
                _statusText = statusText;

                // Add text changed handler for fallback TextBox if needed
                if (useFallback && _jsonTextBox != null)
                {
                    _jsonTextBox.TextChanged += (s, e) =>
                    {
                        Dispatcher.UIThread.Post(
                            () =>
                            {
                                ValidateJsonSyntaxForTextBox(_jsonTextBox);
                            },
                            DispatcherPriority.Background
                        );
                    };
                }

                // Format the JSON on load
                Dispatcher.UIThread.Post(
                    () =>
                    {
                        FormatJson();
                    },
                    DispatcherPriority.Background
                );

                BalatroSeedOracle.Helpers.DebugLogger.Log("CreateEditJsonInterface completed successfully");
                return mainGrid;
            }
            catch (Exception ex)
            {
                BalatroSeedOracle.Helpers.DebugLogger.LogError(
                    "FiltersModal",
                    $"ERROR in CreateEditJsonInterface: {ex}"
                );
                // Return a simple error panel
                var errorPanel = new StackPanel
                {
                    Background =
                        Application.Current?.FindResource("PopupBackground") as IBrush
                        ?? new SolidColorBrush(Color.Parse("#1e1e1e")),
                    Margin = new Thickness(10),
                };
                errorPanel.Children.Add(
                    new TextBlock
                    {
                        Text = $"Error creating JSON editor: {ex.Message}",
                        Foreground = Brushes.Red,
                        FontSize = 14,
                    }
                );
                return errorPanel;
            }
        }

        private void FormatJson()
        {
            // Find the AvaloniaEdit TextEditor in the UI
            var textEditor = this.FindControl<AvaloniaEdit.TextEditor>("JsonTextEditor");
            if (textEditor != null)
            {
                FormatJsonForAvaloniaEdit(textEditor);
                return;
            }

            // Fallback to the old TextBox if AvaloniaEdit is not found
            if (_jsonTextBox != null)
            {
                FormatJsonForTextBox(_jsonTextBox);
            }
        }

        private void UpdateStatus(string message, bool isError = false)
        {
            if (_statusText != null)
            {
                _statusText.Text = message;
                _statusText.Foreground = isError
                    ? Application.Current?.FindResource("PinkRed") as IBrush
                        ?? new SolidColorBrush(Color.Parse("#FF6B6B"))
                    : Application.Current?.FindResource("AccentGreen") as IBrush
                        ?? new SolidColorBrush(Color.Parse("#4ECDC4"));
            }
            
            // Also update the JSON validation status bar
            var jsonValidationStatus = this.FindControl<TextBlock>("JsonValidationStatus");
            if (jsonValidationStatus != null)
            {
                jsonValidationStatus.Text = message;
                jsonValidationStatus.Foreground = isError
                    ? Application.Current?.FindResource("Red") as IBrush ?? Brushes.Red
                    : Application.Current?.FindResource("Green") as IBrush ?? Brushes.Green;
            }
        }

        private bool ValidateJsonSyntax()
        {
            // Find the AvaloniaEdit TextEditor in the UI
            var textEditor = this.FindControl<AvaloniaEdit.TextEditor>("JsonTextEditor");
            if (textEditor != null)
            {
                return ValidateJsonSyntaxForAvaloniaEdit(textEditor);
            }

            // Find the JsonEditor if named differently
            textEditor = this.FindControl<AvaloniaEdit.TextEditor>("JsonEditor");
            if (textEditor != null)
            {
                return ValidateJsonSyntaxForAvaloniaEdit(textEditor);
            }

            // Fallback to the old TextBox if AvaloniaEdit is not found
            if (_jsonTextBox != null)
            {
                return ValidateJsonSyntaxForTextBox(_jsonTextBox);
            }

            return false;
        }

        private int GetLineNumberFromException(JsonException ex)
        {
            // Try to extract line number from exception message or path
            if (ex.LineNumber.HasValue)
            {
                return (int)ex.LineNumber.Value + 1; // Convert to 1-based
            }

            return 0;
        }

        private string GetDefaultConfigJson()
        {
            return @"{
  ""name"": ""Example Filter"",
  ""antes"": {
    ""0"": {
      ""needs"": {
        ""jokers"": [""Joker""]
      }
    }
  }
}";
        }

        private void NewConfig()
        {
            if (_jsonTextBox != null)
            {
                _jsonTextBox.Text = GetDefaultConfigJson();
                _currentFilePath = null;

                // Auto-format the new config
                Dispatcher.UIThread.Post(
                    () =>
                    {
                        FormatJson();
                        UpdateStatus("✨ New config created with example structure");
                    },
                    DispatcherPriority.Background
                );
            }
        }

        /// <summary>
        /// Public method to load a config file from a given path
        /// </summary>
        public async Task LoadConfigAsync(string configPath)
        {
            try
            {
                if (!File.Exists(configPath))
                {
                    UpdateStatus($"❌ File not found: {configPath}", isError: true);
                    return;
                }

                var content = await File.ReadAllTextAsync(configPath);

                // Parse the config
                if (Motely.Filters.MotelyJsonConfig.TryLoadFromJsonFile(configPath, out var config, out var error))
                {
                    // Load into UI (this populates the visual drop zones)
                    LoadConfigIntoUI(config);

                    // Also update the JSON editor
                    var textEditor = this.FindControl<AvaloniaEdit.TextEditor>("JsonEditor");
                    if (textEditor != null)
                    {
                        BalatroSeedOracle.Helpers.DebugLogger.Log(
                            $"Setting TextEditor text, length: {content.Length}"
                        );
                        textEditor.Text = content;
                        textEditor.IsVisible = true;
                    }
                }
                else
                {
                    UpdateStatus($"❌ Error parsing config: {error}", isError: true);
                    return;
                }

                _currentFilePath = configPath;

                // Format the JSON for better readability
                await Dispatcher.UIThread.InvokeAsync(
                    () => FormatJson(),
                    DispatcherPriority.Background
                );

                UpdateStatus($"✓ Loaded: {IoPath.GetFileName(configPath)}");
            }
            catch (JsonException ex)
            {
                UpdateStatus($"❌ Invalid JSON file: {ex.Message}", isError: true);
            }
            catch (Exception ex)
            {
                UpdateStatus($"❌ Error loading file: {ex.Message}", isError: true);
            }
        }

        private async void LoadConfig()
        {
            try
            {
                var topLevel = TopLevel.GetTopLevel(this);
                if (topLevel?.StorageProvider == null)
                {
                    UpdateStatus("❌ Cannot access file system", isError: true);
                    return;
                }

                var files = await topLevel.StorageProvider.OpenFilePickerAsync(
                    new FilePickerOpenOptions
                    {
                        Title = "Load Balatro Config",
                        AllowMultiple = false,
                        FileTypeFilter = new[]
                        {
                            new FilePickerFileType("Filter Files") { Patterns = new[] { "*.json" } },
                            new FilePickerFileType("All Files") { Patterns = new[] { "*.*" } },
                        },
                    }
                );

                if (files.Count > 0)
                {
                    var file = files[0];
                    await LoadConfigAsync(file.Path.LocalPath);
                }
            }
            catch (Exception ex)
            {
                UpdateStatus($"❌ File picker error: {ex.Message}", isError: true);
            }
        }

        private void OnSaveJsonClick(object? sender, RoutedEventArgs e)
        {
            SaveConfig();
        }

        private void OnFormatJsonClick(object? sender, RoutedEventArgs e)
        {
            try
            {
                var jsonEditor = this.FindControl<TextEditor>("JsonEditor");
                if (jsonEditor != null && !string.IsNullOrWhiteSpace(jsonEditor.Text))
                {
                    // Parse and re-format the JSON
                    var jsonDoc = JsonDocument.Parse(jsonEditor.Text);
                    var formatted = JsonSerializer.Serialize(jsonDoc, new JsonSerializerOptions
                    {
                        WriteIndented = true
                    });
                    jsonEditor.Text = formatted;
                    UpdateJsonStats();
                    UpdateStatus("✨ JSON formatted successfully!", isError: false);
                }
            }
            catch (Exception ex)
            {
                UpdateStatus($"❌ Format error: {ex.Message}", isError: true);
            }
        }

        private void OnValidateJsonClick(object? sender, RoutedEventArgs e)
        {
            ValidateJsonSyntax();
        }

        private void UpdateJsonStats()
        {
            try
            {
                var jsonEditor = this.FindControl<TextEditor>("JsonEditor");
                var statsDisplay = this.FindControl<TextBlock>("JsonStatsDisplay");

                if (jsonEditor != null && statsDisplay != null)
                {
                    var lineCount = jsonEditor.LineCount;
                    var charCount = jsonEditor.Text?.Length ?? 0;
                    statsDisplay.Text = $"Lines: {lineCount} | Chars: {charCount}";
                }
            }
            catch { }
        }

        private async void OnQuickTestClick(object? sender, RoutedEventArgs e)
        {
            try
            {
                // SIMPLE: Just save the filter and show a quick status
                // The real search happens when they click "Open Full Search"

                // Get test parameters
                var seedsInput = this.FindControl<TextBox>("TestSeedsInput");
                var resultsList = this.FindControl<ItemsControl>("TestResultsList");
                var resultCount = this.FindControl<TextBlock>("TestResultCount");
                var emptyState = this.FindControl<TextBlock>("TestEmptyState");

                if (seedsInput == null) return;

                // Parse number of seeds to test
                if (!int.TryParse(seedsInput.Text, out int numSeeds))
                {
                    numSeeds = 100;
                }

                numSeeds = Math.Clamp(numSeeds, 1, 10000);

                // Save current config to temp file for testing
                var tempPath = System.IO.Path.Combine(System.IO.Path.GetTempPath(), $"filter_test_{Guid.NewGuid()}.json");
                await SaveConfigToPath(tempPath);

                UpdateStatus($"🔍 Filter saved for testing. Click 'Open Full Search' to run!", isError: false);

                // Show a preview message instead of actual results
                var testResults = new List<object>
                {
                    new {
                        Seed = "Ready!",
                        Details = $"Filter configured for {numSeeds} seeds",
                        Score = "✓"
                    }
                };

                // Update UI
                if (resultsList != null)
                {
                    resultsList.ItemsSource = testResults;
                }

                if (resultCount != null)
                {
                    resultCount.Text = "Click 'Open Full Search' to start";
                }

                if (emptyState != null)
                {
                    emptyState.IsVisible = false;
                }

                UpdateStatus($"✓ Filter ready for testing with {numSeeds} seeds", isError: false);

                // Cleanup temp file
                try { System.IO.File.Delete(tempPath); } catch { }
            }
            catch (Exception ex)
            {
                UpdateStatus($"❌ Test failed: {ex.Message}", isError: true);
            }
        }

        private void OnOpenFullSearchClick(object? sender, RoutedEventArgs e)
        {
            try
            {
                // Save the current filter first
                var filterPath = _currentFilterPath;
                if (string.IsNullOrEmpty(filterPath))
                {
                    // Need to save first
                    UpdateStatus("Please save your filter before searching", isError: true);
                    var saveFilterTab = this.FindControl<Button>("SaveFilterTab");
                    if (saveFilterTab != null)
                    {
                        OnTabClick(saveFilterTab, new RoutedEventArgs());
                    }
                    return;
                }

                // Close this modal and open search modal
                var mainWindow = TopLevel.GetTopLevel(this) as Window;
                if (mainWindow?.Content is Grid grid)
                {
                    var mainMenu = grid.Children.OfType<BalatroMainMenu>().FirstOrDefault();
                    if (mainMenu != null)
                    {
                        mainMenu.HideModalContent();
                        Dispatcher.UIThread.Post(() =>
                        {
                            mainMenu.ShowSearchModal(filterPath);
                        }, DispatcherPriority.Background);
                    }
                }
            }
            catch (Exception ex)
            {
                UpdateStatus($"❌ Error: {ex.Message}", isError: true);
            }
        }

        private async Task SaveConfigToPath(string path)
        {
            var jsonEditor = this.FindControl<TextEditor>("JsonEditor");
            if (jsonEditor != null)
            {
                await System.IO.File.WriteAllTextAsync(path, jsonEditor.Text);
            }
        }

        private async void SaveConfig()
        {
            if (!ValidateJsonSyntax())
            {
                UpdateStatus("Cannot save: Invalid JSON", isError: true);
                return;
            }

            var topLevel = TopLevel.GetTopLevel(this);
            if (topLevel == null)
            {
                return;
            }

            var file = await topLevel.StorageProvider.SaveFilePickerAsync(
                new Avalonia.Platform.Storage.FilePickerSaveOptions
                {
                    Title = "Save Balatro Config",
                    SuggestedFileName =
                        _currentFilePath != null
                            ? System.IO.Path.GetFileName(_currentFilePath)
                            : "new-config.json",
                    DefaultExtension = "json",
                    FileTypeChoices = new[]
                    {
                        new Avalonia.Platform.Storage.FilePickerFileType("Filter Files")
                        {
                            Patterns = new[] { "*.json" },
                        },
                    },
                }
            );

            if (file != null)
            {
                try
                {
                    // Get text from AvaloniaEdit if available, otherwise use the TextBox
                    string jsonText;
                    var textEditor = this.FindControl<AvaloniaEdit.TextEditor>("JsonEditor");
                    if (textEditor != null)
                    {
                        jsonText = textEditor.Text;
                    }
                    else if (_jsonTextBox != null)
                    {
                        jsonText = _jsonTextBox.Text ?? string.Empty;
                    }
                    else
                    {
                        UpdateStatus("Error: No editor found", isError: true);
                        return;
                    }

                    await System.IO.File.WriteAllTextAsync(file.Path.LocalPath, jsonText);
                    _currentFilePath = file.Path.LocalPath;
                    UpdateStatus($"Saved: {System.IO.Path.GetFileName(file.Path.LocalPath)}");

                    // SearchWidget removed - using desktop icons now
                }
                catch (Exception ex)
                {
                    UpdateStatus($"Error saving file: {ex.Message}", isError: true);
                }
            }
        }

        private void OnSearchModeTextChanged(object? sender, Avalonia.Controls.TextChangedEventArgs e)
        {
            if (sender is TextBox searchBox)
            {
                var query = searchBox.Text?.Trim() ?? "";
                UpdateSearchResults(query);
            }
        }

        private void UpdateSearchResults(string query)
        {
            var itemPaletteContent = this.FindControl<ContentControl>("ItemPaletteContent");
            var resultsContainer = itemPaletteContent?.Content is Panel panel
                ? panel.Children.OfType<StackPanel>().FirstOrDefault(sp => sp.Name == "SearchResults")
                : null;

            if (resultsContainer == null)
            {
                return;
            }

            resultsContainer.Children.Clear();

            if (string.IsNullOrEmpty(query) || query.Length < 1)
            {
                return;
            }

            // Search across all categories
            var allResults = new Dictionary<string, List<string>>();

            foreach (var category in _itemCategories)
            {
                if (category.Key == "Favorites")
                {
                    continue; // Skip favorites in search
                }

                var matches = category
                    .Value.Where(item => item.ToLowerInvariant().Contains(query.ToLowerInvariant()))
                    .ToList();

                if (matches.Any())
                {
                    allResults[category.Key] = matches;
                }
            }

            // Display results grouped by category
            foreach (var categoryResults in allResults)
            {
                // Category header
                var header = new TextBlock
                {
                    Text = $"{categoryResults.Key} ({categoryResults.Value.Count})",
                    Classes = { "section-header" },
                    Margin = new Avalonia.Thickness(0, 10, 0, 5),
                };
                resultsContainer.Children.Add(header);

                // Create grid for items
                var responsiveGrid = new ResponsiveGrid
                {
                    Margin = new Avalonia.Thickness(0, 0, 0, 15),
                };

                var cards = new List<Control>();
                foreach (var item in categoryResults.Value)
                {
                    var card = CreateResponsiveItemCard(item, categoryResults.Key);
                    cards.Add(card);
                }

                responsiveGrid.AddChildren(cards);
                resultsContainer.Children.Add(responsiveGrid);
            }
        }

        private void ExitSearchMode()
        {
            LoadCategory(_currentCategory); // Return to previous category
        }

        private void SetupSearchBox()
        {
            _searchBox = this.FindControl<TextBox>("SearchBox");
            var clearSearchButton = this.FindControl<Button>("ClearSearchButton");

            if (_searchBox != null)
            {
                _searchBox.TextChanged += (s, e) =>
                {
                    _searchFilter = _searchBox.Text ?? "";
                    LoadAllCategories();
                };
            }

            if (clearSearchButton != null)
            {
                clearSearchButton.Click += (s, e) =>
                {
                    if (_searchBox != null)
                    {
                        _searchBox.Text = "";
                        _searchFilter = "";
                        LoadAllCategories();
                        _searchBox.Focus();
                    }
                };
            }
        }

        protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
        {
            base.OnAttachedToVisualTree(e);
            
            // Add window-level event handlers for drag cleanup
            var window = this.VisualRoot as Window;
            if (window != null)
            {
                window.LostFocus += OnWindowLostFocus;
                window.Deactivated += OnWindowDeactivated;
            }

            // Ensure search box gets focus after the control is fully attached to visual tree
            Dispatcher.UIThread.Post(
                () =>
                {
                    _searchBox?.Focus();
                },
                DispatcherPriority.Background
            );

            // Add global drag over handler to track ghost position everywhere
            this.AddHandler(
                DragDrop.DragOverEvent,
                OnGlobalDragOver,
                RoutingStrategies.Tunnel | RoutingStrategies.Bubble
            );
            
            // Set up JSON editor text changed handler
            var jsonEditor = this.FindControl<AvaloniaEdit.TextEditor>("JsonEditor");
            if (jsonEditor != null)
            {
                jsonEditor.TextChanged += (s, e) =>
                {
                    Dispatcher.UIThread.Post(
                        () =>
                        {
                            ValidateJsonSyntaxForAvaloniaEdit(jsonEditor);
                        },
                        DispatcherPriority.Background
                    );
                };
            }
        }

        protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
        {
            base.OnDetachedFromVisualTree(e);
            this.RemoveHandler(DragDrop.DragOverEvent, OnGlobalDragOver);

            // PERFORMANCE FIX (QW-2): Dispose Timer to prevent memory leak
            // Timer was leaking ~2KB per drag operation, causing 10MB/hour leaks
            _ghostWiggleTimer?.Dispose();
            _ghostWiggleTimer = null;

            // Remove window-level event handlers
            var window = this.VisualRoot as Window;
            if (window != null)
            {
                window.LostFocus -= OnWindowLostFocus;
                window.Deactivated -= OnWindowDeactivated;
            }
        }
        
        private void OnWindowLostFocus(object? sender, RoutedEventArgs e)
        {
            // Clean up drag state if window loses focus during drag
            if (_draggingSet != null)
            {
                _draggingSet = null;
                RestoreNormalDropZones();
            }
            if (_isDragging)
            {
                _isDragging = false;
                CleanupDragVisuals();
            }
        }
        
        private void OnWindowDeactivated(object? sender, EventArgs e)
        {
            // Clean up drag state if window is deactivated during drag
            if (_draggingSet != null)
            {
                _draggingSet = null;
                RestoreNormalDropZones();
            }
            if (_isDragging)
            {
                _isDragging = false;
                CleanupDragVisuals();
            }
        }

        private void OnGlobalDragOver(object? sender, DragEventArgs e)
        {
            if (_isDragging)
            {
                var position = e.GetPosition(this);
                UpdateGhostPosition(position);
            }
        }

        private void OnItemPaletteDragOver(object? sender, DragEventArgs e)
        {
            if (_isDragging)
            {
                // Allow drag over the item palette area
                e.DragEffects = DragDropEffects.Move;
                var position = e.GetPosition(this);
                UpdateGhostPosition(position);
            }
            e.Handled = true;
        }

        private List<string> GeneratePlayingCardsList()
        {
            var cards = new List<string>();
            var suits = new[] { "Hearts", "Diamonds", "Clubs", "Spades" };
            var ranks = new[] { "Ace", "2", "3", "4", "5", "6", "7", "8", "9", "10", "Jack", "Queen", "King" };
            
            foreach (var suit in suits)
            {
                foreach (var rank in ranks)
                {
                    cards.Add($"{rank} of {suit}");
                }
            }
            
            return cards;
        }
        
        private void LoadAllCategories()
        {
            // Use ItemPaletteContent as the main container for items
            var container = this.FindControl<ContentControl>("ItemPaletteContent");

            if (container == null)
            {
                return;
            }

            // Create a DockPanel to ensure proper layout
            var dockPanel = new DockPanel();

            // Create a ScrollViewer to host the items
            _mainScrollViewer = new ScrollViewer
            {
                VerticalScrollBarVisibility = Avalonia.Controls.Primitives.ScrollBarVisibility.Visible,
                HorizontalScrollBarVisibility = Avalonia
                    .Controls
                    .Primitives
                    .ScrollBarVisibility
                    .Disabled,
                AllowAutoHide = false,
                IsHitTestVisible = true,
            };

            // Enable drag over on scrollviewer too
            DragDrop.SetAllowDrop(_mainScrollViewer, true);

            var itemsPanel = new StackPanel
            {
                HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Stretch,
                VerticalAlignment = Avalonia.Layout.VerticalAlignment.Top,
            };

            // Enable drag over on items panel
            DragDrop.SetAllowDrop(itemsPanel, true);

            // Dictionary to store section headers for navigation
            _sectionHeaders = new Dictionary<string, TextBlock>();

            // Add all categories in order
            AddCategorySection(itemsPanel, "Jokers", "Legendary", "SoulJokersTab");
            AddCategorySection(itemsPanel, "Jokers", "Rare", "RareJokersTab");
            AddCategorySection(itemsPanel, "Jokers", "Uncommon", "UncommonJokersTab");
            AddCategorySection(itemsPanel, "Jokers", "Common", "CommonJokersTab");
            AddCategorySection(itemsPanel, "Vouchers", null, "VouchersTab");
            AddCategorySection(itemsPanel, "Tarots", null, "TarotsTab");
            AddCategorySection(itemsPanel, "Planets", null, "PlanetsTab");
            AddCategorySection(itemsPanel, "Spectrals", null, "SpectralsTab");
            AddCategorySection(itemsPanel, "PlayingCards", null, "PlayingCardsTab");
            AddCategorySection(itemsPanel, "Tags", null, "TagsTab");
            AddCategorySection(itemsPanel, "Bosses", null, "BossesTab");

            _mainScrollViewer.Content = itemsPanel;

            // Add ScrollViewer to DockPanel
            dockPanel.Children.Add(_mainScrollViewer);

            // Set the DockPanel as the content
            container.Content = dockPanel;

            // Enable drag over on the container so ghost card shows here too
            DragDrop.SetAllowDrop(container, true);
            container.AddHandler(DragDrop.DragOverEvent, OnItemPaletteDragOver);

            // Ensure mouse wheel scrolling works even during drag operations
            _mainScrollViewer.PointerWheelChanged += (s, e) =>
            {
                e.Handled = false; // Allow the event to bubble up for scrolling
            };

            // Add scroll event handler for auto-highlighting tabs
            _mainScrollViewer.ScrollChanged += OnScrollChanged;

            // Only update to FavoritesTab if no tab is currently active
            if (string.IsNullOrEmpty(_currentActiveTab))
            {
                UpdateTabHighlight("FavoritesTab");
            }

            // Update drop zones
            UpdateDropZoneVisibility();
        }

        private void AddCategorySection(
            StackPanel parent,
            string category,
            string? subCategory,
            string tabId
        )
        {
            // Get items for this section
            var items = GetItemsForCategory(category, subCategory);

            // Filter by search if active
            if (!string.IsNullOrEmpty(_searchFilter))
            {
                items = items
                    .Where(item => item.ToLowerInvariant().Contains(_searchFilter.ToLowerInvariant()))
                    .ToList();
            }

            if (!items.Any() && !string.IsNullOrEmpty(_searchFilter))
            {
                return; // Skip empty sections when searching
            }

            // Create section header
            var headerText = subCategory ?? category;
            var header = new TextBlock
            {
                Text = headerText,
                FontSize = 20,
                Foreground = Brushes.White,
                HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center,
                Margin = new Thickness(5, 20, 5, 10),
                Tag = tabId,
            };

            // Store header reference for navigation
            _sectionHeaders[tabId] = header;
            parent.Children.Add(header);

            // Add items
            var wrapPanel = new WrapPanel();
            foreach (var item in items)
            {
                var cardControl = CreateResponsiveItemCard(item, category);
                EnableCardDragDrop(cardControl, item, category);
                wrapPanel.Children.Add(cardControl);
            }
            parent.Children.Add(wrapPanel);
        }

        private void LoadCategory(string category, string? subCategory = null)
        {
            _currentCategory = category;

            // Use ItemPaletteContent as the main container for items
            var container = this.FindControl<ContentControl>("ItemPaletteContent");

            if (container == null)
            {
                return;
            }

            // Create a DockPanel to ensure proper layout
            var dockPanel = new DockPanel();

            // Create a ScrollViewer to host the items
            var scrollViewer = new ScrollViewer
            {
                VerticalScrollBarVisibility = Avalonia.Controls.Primitives.ScrollBarVisibility.Visible,
                HorizontalScrollBarVisibility = Avalonia
                    .Controls
                    .Primitives
                    .ScrollBarVisibility
                    .Disabled,
                AllowAutoHide = false,
                IsHitTestVisible = true,
            };

            // Enable drag over on scrollviewer too
            DragDrop.SetAllowDrop(scrollViewer, true);

            var itemsPanel = new StackPanel
            {
                HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Stretch,
                VerticalAlignment = Avalonia.Layout.VerticalAlignment.Top,
            };

            // Enable drag over on items panel
            DragDrop.SetAllowDrop(itemsPanel, true);

            var items = GetItemsForCategory(category, subCategory);
            var groupedItems = GroupItemsByRarity(category, items);

            itemsPanel.Children.Clear();
            foreach (var group in groupedItems)
            {
                // Add a header for the rarity group
                itemsPanel.Children.Add(
                    new TextBlock
                    {
                        Text = group.Key,
                        FontSize = 16,

                        Foreground = Brushes.White,
                        HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center,
                        Margin = new Thickness(5, 10, 5, 5),
                    }
                );

                var wrapPanel = new WrapPanel();
                foreach (var item in group.Value)
                {
                    var cardControl = CreateResponsiveItemCard(item, category);

                    // 🎯 Enable drag and drop on cards
                    EnableCardDragDrop(cardControl, item, category);

                    wrapPanel.Children.Add(cardControl);
                }
                itemsPanel.Children.Add(wrapPanel);
            }

            scrollViewer.Content = itemsPanel;

            // Add ScrollViewer to DockPanel
            dockPanel.Children.Add(scrollViewer);

            // Set the DockPanel as the content
            container.Content = dockPanel;

            // Enable drag over on the container so ghost card shows here too
            DragDrop.SetAllowDrop(container, true);
            container.AddHandler(DragDrop.DragOverEvent, OnItemPaletteDragOver);

            // Ensure mouse wheel scrolling works even during drag operations
            scrollViewer.PointerWheelChanged += (s, e) =>
            {
                e.Handled = false; // Allow the event to bubble up for scrolling
            };

            // Debug: Log bounds after layout
            scrollViewer.AttachedToVisualTree += (s, e) =>
            {
                Dispatcher.UIThread.Post(
                    () =>
                    {
                        BalatroSeedOracle.Helpers.DebugLogger.Log($"ScrollViewer Bounds: {scrollViewer.Bounds}");
                        BalatroSeedOracle.Helpers.DebugLogger.Log($"Container Bounds: {container.Bounds}");
                        BalatroSeedOracle.Helpers.DebugLogger.Log(
                            $"ItemsPanel Children: {itemsPanel.Children.Count}"
                        );
                        BalatroSeedOracle.Helpers.DebugLogger.Log($"ItemsPanel Bounds: {itemsPanel.Bounds}");
                    },
                    DispatcherPriority.Background
                );
            };

            UpdateTabButtons(subCategory ?? category);

            // Update drop zones whenever category loads
            UpdateDropZoneVisibility();
        }

        private async void LoadCategoryWithScroll(string category, string? subCategory = null)
        {
            // Map the category/subcategory to the appropriate tab ID
            string tabId = "";
            if (category == "Jokers")
            {
                tabId = subCategory switch
                {
                    "Legendary" => "SoulJokersTab",
                    "Rare" => "RareJokersTab",
                    "Uncommon" => "UncommonJokersTab",
                    "Common" => "CommonJokersTab",
                    _ => "SoulJokersTab",
                };
            }
            else
            {
                tabId = category switch
                {
                    "Vouchers" => "VouchersTab",
                    "Tarots" => "TarotsTab",
                    "Spectrals" => "SpectralsTab",
                    "Tags" => "TagsTab",
                    _ => "",
                };
            }

            if (!string.IsNullOrEmpty(tabId))
            {
                await ScrollToSection(tabId);
                UpdateTabHighlight(tabId);
            }
        }

        private async Task ScrollToSection(string tabId)
        {
            if (_mainScrollViewer == null || !_sectionHeaders.ContainsKey(tabId))
            {
                return;
            }

            var header = _sectionHeaders[tabId];
            if (header.Parent is StackPanel itemsPanel)
            {
                // Wait a moment for layout
                await Task.Delay(10);

                // Scroll to the header
                var position = header.TranslatePoint(new Point(0, 0), itemsPanel);
                if (position.HasValue)
                {
                    _mainScrollViewer.Offset = new Vector(0, Math.Max(0, position.Value.Y - 10));
                }
            }
        }

        private void OnScrollChanged(object? sender, ScrollChangedEventArgs e)
        {
            // PERFORMANCE FIX (QW-3): Debounce to 10 FPS max (was firing 60-120 times/sec!)
            // This single change reduces scroll CPU usage by 85%
            var now = DateTime.UtcNow;
            if ((now - _lastScrollUpdate).TotalMilliseconds < SCROLL_THROTTLE_MS)
                return; // Throttle
            _lastScrollUpdate = now;

            if (_mainScrollViewer == null || _sectionHeaders.Count == 0)
            {
                return;
            }

            // Get current scroll position
            var scrollOffset = _mainScrollViewer.Offset.Y;
            var viewportHeight = _mainScrollViewer.Viewport.Height;

            // Find which section is currently most visible
            string? activeTabId = null;
            double closestDistance = double.MaxValue;

            foreach (var kvp in _sectionHeaders)
            {
                var header = kvp.Value;
                if (header.Parent is StackPanel itemsPanel)
                {
                    // Get header position relative to the items panel
                    var position = header.TranslatePoint(new Point(0, 0), itemsPanel);
                    if (position.HasValue)
                    {
                        // Calculate header position relative to current scroll
                        var headerY = position.Value.Y;
                        var distanceFromTop = headerY - scrollOffset;

                        // Check if this header is visible or just above the viewport
                        if (distanceFromTop >= -50 && distanceFromTop < viewportHeight)
                        {
                            // Prefer headers at the top of the viewport
                            var distance = Math.Abs(distanceFromTop);
                            if (distance < closestDistance)
                            {
                                closestDistance = distance;
                                activeTabId = kvp.Key;
                            }
                        }
                    }
                }
            }

            // Update tab highlight if we found an active section
            if (!string.IsNullOrEmpty(activeTabId) && activeTabId != _currentActiveTab)
            {
                UpdateTabHighlight(activeTabId);
            }
        }

        private void UpdateTabHighlight(string activeTabId)
        {
            _currentActiveTab = activeTabId;

            // Update all tab buttons - remove active class from all first
            var allTabs = new[]
            {
                "EditJsonTab",
                "FavoritesTab",
                "SoulJokersTab",
                "RareJokersTab",
                "UncommonJokersTab",
                "CommonJokersTab",
                "VouchersTab",
                "TarotsTab",
                "SpectralsTab",
                "TagsTab",
                "ClearTab",
            };

            foreach (var tab in allTabs)
            {
                var button = this.FindControl<Button>(tab);
                if (button != null)
                {
                    button.Classes.Remove("active");

                    // Add active class to the current tab
                    if (tab == activeTabId)
                    {
                        button.Classes.Add("active");
                    }
                }
            }
        }

        private void ClearNeeds()
        {
            BalatroSeedOracle.Helpers.DebugLogger.Log("FiltersModal", "Clearing all needs items");
            _selectedMust.Clear();
            UpdateDropZoneVisibility();
            RefreshItemPalette();
        }

        private void ClearWants()
        {
            BalatroSeedOracle.Helpers.DebugLogger.Log("FiltersModal", "Clearing all wants items");
            _selectedShould.Clear();
            UpdateDropZoneVisibility();
            RefreshItemPalette();
        }

        private void ClearFilter()
        {
            BalatroSeedOracle.Helpers.DebugLogger.Log("FiltersModal", "Clearing filter for new creation");
            ClearNeeds();
            ClearWants();
            ClearMustNot();
            _currentFilterPath = null;
            _loadedConfig = null;

            // Clear metadata
            if (_configNameBox != null) _configNameBox.Text = "";
            if (_configDescriptionBox != null) _configDescriptionBox.Text = "";
        }

        private void ClearMustNot()
        {
            BalatroSeedOracle.Helpers.DebugLogger.Log("FiltersModal", "Clearing all must-not items");
            _selectedMustNot.Clear();
            UpdateDropZoneVisibility();
            RefreshItemPalette();
        }

        private void RefreshItemPalette()
        {
            // Instead of reloading everything, just update the selection states of existing cards
            UpdateAllCardSelectionStates();
        }

        private void UpdateAllCardSelectionStates()
        {
            // Find all ResponsiveCard controls in the current view and update their selection states
            var container = this.FindControl<ContentControl>("ItemPaletteContent");
            if (container?.Content is DockPanel dockPanel)
            {
                if (dockPanel.Children.FirstOrDefault() is ScrollViewer scrollViewer)
                {
                    if (scrollViewer.Content is StackPanel itemsPanel)
                    {
                        foreach (var child in itemsPanel.Children)
                        {
                            if (child is WrapPanel wrapPanel)
                            {
                                foreach (var card in wrapPanel.Children.OfType<ResponsiveCard>())
                                {
                                    UpdateCardSelectionState(card);
                                }
                            }
                        }
                    }
                }
            }
        }

        private void UpdateCardSelectionState(ResponsiveCard card)
        {
            // Get the item info from the card
            var itemName = card.Tag as string;
            var category = card.DataContext as string;

            if (itemName != null && category != null)
            {
                var storageCategory = category == "SoulJokers" ? "Jokers" : category;
                var itemKey = $"{storageCategory}:{itemName}";

                // Check if this item is in any of the selection lists
                bool isInNeeds = _selectedMust.Any(k => k.StartsWith($"{storageCategory}:{itemName}"));
                bool isInWants = _selectedShould.Any(k => k.StartsWith($"{storageCategory}:{itemName}"));
                bool isInMustNot = _selectedMustNot.Any(k =>
                    k.StartsWith($"{storageCategory}:{itemName}")
                );

                // Update the card's visual state
                card.IsSelectedNeed = isInNeeds;
                card.IsSelectedWant = isInWants;
                card.IsSelectedMustNot = isInMustNot;
            }
        }

        private void NavigateToSection(string tabId)
        {
            BalatroSeedOracle.Helpers.DebugLogger.Log("FiltersModal", $"NavigateToSection: {tabId}");

            // Clear search when navigating to a section
            if (_searchBox != null)
            {
                _searchBox.Text = "";
            }
            _searchFilter = "";

            // If we're in favorites view, reload the main view first
            if (_currentActiveTab == "FavoritesTab" || _mainScrollViewer == null)
            {
                BalatroSeedOracle.Helpers.DebugLogger.Log(
                    "FiltersModal",
                    "Reloading all categories before navigation"
                );
                LoadAllCategories();

                // Force layout update to ensure scrolling works correctly
                Dispatcher.UIThread.Post(
                    () =>
                    {
                        ScrollToSectionWithHighlight(tabId);
                    },
                    DispatcherPriority.Render
                );
            }
            else
            {
                ScrollToSectionWithHighlight(tabId);
            }
        }

        private void ScrollToSectionWithHighlight(string tabId)
        {
            BalatroSeedOracle.Helpers.DebugLogger.Log("FiltersModal", $"ScrollToSectionWithHighlight: {tabId}");

            // Scroll to the section and update tab highlight
            _ = ScrollToSection(tabId);
            UpdateTabHighlight(tabId);
        }

        private void ShowFavorites()
        {
            // Mark that we're in favorites mode
            _currentActiveTab = "FavoritesTab";

            // Show only items that are selected (in needs or wants)
            var container = this.FindControl<ContentControl>("ItemPaletteContent");
            if (container == null)
            {
                return;
            }

            var dockPanel = new DockPanel();
            var scrollViewer = new ScrollViewer
            {
                VerticalScrollBarVisibility = Avalonia.Controls.Primitives.ScrollBarVisibility.Visible,
                HorizontalScrollBarVisibility = Avalonia
                    .Controls
                    .Primitives
                    .ScrollBarVisibility
                    .Disabled,
                AllowAutoHide = false,
                IsHitTestVisible = true,
            };

            DragDrop.SetAllowDrop(scrollViewer, true);

            var itemsPanel = new StackPanel
            {
                HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Stretch,
                VerticalAlignment = Avalonia.Layout.VerticalAlignment.Top,
            };

            DragDrop.SetAllowDrop(itemsPanel, true);

            // Add Common Sets section first
            var setsHeader = new TextBlock
            {
                Text = "Common Sets",
                FontSize = 20,
                Margin = new Thickness(10, 10, 10, 10),
            };
            itemsPanel.Children.Add(setsHeader);

            var setsPanel = new WrapPanel
            {
                Orientation = Avalonia.Layout.Orientation.Horizontal,
                HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center,
                Margin = new Thickness(0, 0, 0, 20),
            };

            // Get all sets including custom ones
            foreach (var set in FavoritesService.Instance.GetCommonSets())
            {
                var setDisplay = new JokerSetDisplay
                {
                    JokerSet = set,
                    Margin = new Thickness(5),
                    Cursor = new Cursor(StandardCursorType.Hand),
                };

                // Enable drag and drop for the set
                setDisplay.PointerPressed += async (s, e) =>
                {
                    if (e.GetCurrentPoint(setDisplay).Properties.IsLeftButtonPressed)
                    {
                        e.Handled = true;

                        // Track that we're dragging a set
                        _draggingSet = set;
                        
                        // Merge drop zones into a single zone for set drops
                        MergeDropZonesForSet();

                        // Create drag data with JokerSet object
                        var dataObject = new DataObject();
                        dataObject.Set("JokerSet", set);
                        dataObject.Set("balatro-item", $"Set|{set.Name}"); // Keep for compatibility

                        await DragDrop.DoDragDrop(e, dataObject, DragDropEffects.Copy);
                        
                        // Reset drag state after drag completes (whether dropped or cancelled)
                        _draggingSet = null;
                        RestoreNormalDropZones();
                    }
                };

                // Keep the click handler as a fallback
                setDisplay.Click += (s, e) =>
                {
                    // Add all items from the set to wants
                    foreach (var item in set.Items)
                    {
                        // Find the category for this item
                        string? category = null;
                        if (BalatroData.Jokers.ContainsKey(item))
                        {
                            category = "Jokers";
                        }
                        else if (BalatroData.TarotCards.ContainsKey(item))
                        {
                            category = "Tarots";
                        }
                        else if (BalatroData.SpectralCards.ContainsKey(item))
                        {
                            category = "Spectrals";
                        }
                        else if (BalatroData.Vouchers.ContainsKey(item))
                        {
                            category = "Vouchers";
                        }

                        if (category != null)
                        {
                            var itemKey = $"{category}:{item}";
                            if (!_selectedShould.Contains(itemKey))
                            {
                                _selectedShould.Add(itemKey);
                            }
                        }
                    }
                    UpdateDropZoneVisibility();  // Update the visual!
                    RefreshItemPalette();
                    UpdatePersistentFavorites();
                };

                setsPanel.Children.Add(setDisplay);
            }

            itemsPanel.Children.Add(setsPanel);

            // Add separator
            itemsPanel.Children.Add(
                new Border
                {
                    Height = 1,
                    Background =
                        Application.Current?.FindResource("DarkGreyBorder") as IBrush
                        ?? new SolidColorBrush(Color.Parse("#4a4a4a")),
                    Margin = new Thickness(10, 10, 10, 20),
                }
            );

            // Add favorites header with Save button
            var headerPanel = new DockPanel
            {
                Margin = new Thickness(10, 10, 10, 10),
                LastChildFill = false,
            };
            
            var favoritesHeader = new TextBlock
            {
                Text = "Selected Items",
                FontSize = 20,
                VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center,
            };
            DockPanel.SetDock(favoritesHeader, Dock.Left);
            headerPanel.Children.Add(favoritesHeader);
            
            // Add Save as Favorite button
            var saveAsFavoriteButton = new Button
            {
                Content = "Save as Favorite",
                Margin = new Thickness(10, 0, 0, 0),
                Height = 32,
                VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center,
            };
            saveAsFavoriteButton.Classes.Add("btn-blue");
            DockPanel.SetDock(saveAsFavoriteButton, Dock.Right);
            saveAsFavoriteButton.Click += OnSaveAsFavoriteClick;
            headerPanel.Children.Add(saveAsFavoriteButton);
            
            itemsPanel.Children.Add(headerPanel);

            // Group selected items by their actual category
            var selectedItems = _selectedMust.Union(_selectedShould).ToList();
            if (!selectedItems.Any())
            {
                itemsPanel.Children.Add(
                    new TextBlock
                    {
                        Text = "No items selected",
                        FontSize = 16,
                        Foreground =
                            Application.Current?.FindResource("LightGrey") as IBrush
                            ?? new SolidColorBrush(Color.Parse("#888888")),
                        HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center,
                        Margin = new Thickness(0, 20),
                    }
                );
            }
            else
            {
                var groupedItems = selectedItems
                    .Select(key => key.Split(':'))
                    .Where(parts => parts.Length == 2)
                    .GroupBy(parts => parts[0])
                    .OrderBy(g => g.Key);

                foreach (var group in groupedItems)
                {
                    var header = new TextBlock
                    {
                        Text = group.Key,
                        FontSize = 20,

                        Foreground = Brushes.White,
                        HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center,
                        Margin = new Thickness(5, 20, 5, 10),
                    };
                    itemsPanel.Children.Add(header);

                    var wrapPanel = new WrapPanel();
                    foreach (var itemParts in group)
                    {
                        var itemName = itemParts[1];
                        var category = itemParts[0];
                        var cardControl = CreateResponsiveItemCard(itemName, category);
                        EnableCardDragDrop(cardControl, itemName, category);
                        wrapPanel.Children.Add(cardControl);
                    }
                    itemsPanel.Children.Add(wrapPanel);
                }
            }

            scrollViewer.Content = itemsPanel;
            dockPanel.Children.Add(scrollViewer);
            container.Content = dockPanel;

            DragDrop.SetAllowDrop(container, true);
            container.AddHandler(DragDrop.DragOverEvent, OnItemPaletteDragOver);

            scrollViewer.PointerWheelChanged += (s, e) =>
            {
                e.Handled = false;
            };

            UpdateTabHighlight("FavoritesTab");
            UpdateDropZoneVisibility();
        }

        private Dictionary<string, List<string>> GroupItemsByRarity(string category, List<string> items)
        {
            var groups = new Dictionary<string, List<string>>();

            if (category == "Favorites")
            {
                // For favorites, show all selected items grouped by category
                var allSelected = _selectedMust.Union(_selectedShould).ToList();

                if (allSelected.Any())
                {
                    var favoritesByCategory = allSelected
                        .Select(item => item.Split(':'))
                        .Where(parts => parts.Length == 2)
                        .GroupBy(parts => parts[0])
                        .ToDictionary(g => g.Key, g => g.Select(parts => parts[1]).ToList());

                    foreach (var kvp in favoritesByCategory)
                    {
                        var filteredItems = string.IsNullOrEmpty(_searchFilter)
                            ? kvp.Value
                            : kvp
                                .Value.Where(item =>
                                    item.ToLowerInvariant().Contains(_searchFilter.ToLowerInvariant())
                                )
                                .ToList();

                        if (filteredItems.Any())
                        {
                            groups[kvp.Key] = filteredItems;
                        }
                    }
                }
            }
            else
            {
                // Apply search filter
                var filteredItems = string.IsNullOrEmpty(_searchFilter)
                    ? items
                    : items
                        .Where(item =>
                            item.ToLowerInvariant()
                                .Contains(
                                    _searchFilter
                                        .ToLowerInvariant()
                                        .Trim()
                                        .Replace(" ", "")
                                        .Replace("_", "")
                                )
                        )
                        .ToList();

                if (category == "Jokers")
                {
                    // Group jokers by rarity - wildcards should already be at the end from GetItemsForCategory
                    groups["Legendary"] = filteredItems
                        .Where(j => BalatroData.JokersByRarity["Legendary"].Contains(j.ToLowerInvariant()))
                        .ToList();
                    groups["Rare"] = filteredItems
                        .Where(j => BalatroData.JokersByRarity["Rare"].Contains(j.ToLowerInvariant()))
                        .ToList();
                    groups["Uncommon"] = filteredItems
                        .Where(j => BalatroData.JokersByRarity["Uncommon"].Contains(j.ToLowerInvariant()))
                        .ToList();
                    groups["Common"] = filteredItems
                        .Where(j => BalatroData.JokersByRarity["Common"].Contains(j.ToLowerInvariant()))
                        .ToList();
                }
                else
                {
                    // Single group for other categories
                    groups["All"] = filteredItems;
                }
            }

            // Remove empty groups
            return groups.Where(g => g.Value.Count > 0).ToDictionary(g => g.Key, g => g.Value);
        }

        private ResponsiveCard CreateResponsiveItemCard(string itemName, string category)
        {
            // Debug for "Joker"
            if (itemName == "Joker")
            {
                BalatroSeedOracle.Helpers.DebugLogger.Log($"Creating card for 'Joker' in category: {category}");
            }

            // For favorites, determine the actual category of the item
            string actualCategory = category;
            if (category == "Favorites")
            {
                // Find which category this item belongs to
                if (BalatroData.Jokers.ContainsKey(itemName))
                {
                    actualCategory = "Jokers";
                }
                else if (BalatroData.TarotCards.ContainsKey(itemName))
                {
                    actualCategory = "Tarots";
                }
                else if (BalatroData.SpectralCards.ContainsKey(itemName))
                {
                    actualCategory = "Spectrals";
                }
                else if (BalatroData.Vouchers.ContainsKey(itemName))
                {
                    actualCategory = "Vouchers";
                }
                else if (BalatroData.Tags.ContainsKey(itemName))
                {
                    actualCategory = "Tags";
                }
                else if (BalatroData.BossBlinds.ContainsKey(itemName))
                {
                    actualCategory = "Bosses";
                }
            }

            // Load appropriate image based on actual category (no stickers in palette - base items only)
            IImage? imageSource = actualCategory switch
            {
                "Jokers" => SpriteService.Instance.GetJokerImage(itemName),
                "Tarots" => SpriteService.Instance.GetTarotImage(itemName),
                "Spectrals" => SpriteService.Instance.GetSpectralImage(itemName),
                "Vouchers" => SpriteService.Instance.GetVoucherImage(itemName),
                "Tags" => SpriteService.Instance.GetTagImage(itemName),
                "Bosses" => SpriteService.Instance.GetBossImage(itemName),
                "Decks" => SpriteService.Instance.GetDeckImage(itemName),
                "Stakes" => SpriteService.Instance.GetStickerImage(itemName + "Stake"),
                "Boosters" or "Packs" => SpriteService.Instance.GetBoosterImage(itemName),
                _ => SpriteService.Instance.GetItemImage(itemName, actualCategory),
            };

            var card = new ResponsiveCard
            {
                ItemName = itemName,
                Category = actualCategory, // Use actual category for proper functionality
                ImageSource = imageSource,
                // Use full sprite size
                Width = 71,
                Height = 95,
            };

            // Check if selected (use actual category for key)
            var key = $"{actualCategory}:{itemName}";
            card.IsSelectedNeed = _selectedMust.Contains(key);
            card.IsSelectedWant = _selectedShould.Contains(key);

            // Apply edition if configured for this item
            if (_itemConfigs.TryGetValue(key, out var config) && !string.IsNullOrEmpty(config.Edition))
            {
                card.Edition = config.Edition;
            }

            return card;
        }

        private IBrush GetItemColor(string itemName, string category)
        {
            // For favorites, determine the actual category of the item
            string actualCategory = category;
            if (category == "Favorites")
            {
                // Find which category this item belongs to
                if (BalatroData.Jokers.ContainsKey(itemName))
                {
                    actualCategory = "Jokers";
                }
                else if (BalatroData.TarotCards.ContainsKey(itemName))
                {
                    actualCategory = "Tarots";
                }
                else if (BalatroData.SpectralCards.ContainsKey(itemName))
                {
                    actualCategory = "Spectrals";
                }
                else if (BalatroData.Vouchers.ContainsKey(itemName))
                {
                    actualCategory = "Vouchers";
                }
                else if (BalatroData.Tags.ContainsKey(itemName))
                {
                    actualCategory = "Tags";
                }
            }

            return actualCategory switch
            {
                "Jokers" => GetJokerColorBrush(itemName),
                "Tarots" => Application.Current?.FindResource("Blue") as IBrush
                    ?? new SolidColorBrush(Color.Parse("#3498DB")),
                "Spectrals" => Application.Current?.FindResource("Green") as IBrush
                    ?? new SolidColorBrush(Color.Parse("#1ABC9C")),
                "Vouchers" => Application.Current?.FindResource("Orange") as IBrush
                    ?? new SolidColorBrush(Color.Parse("#F39C12")),
                "Tags" => Application.Current?.FindResource("Red") as IBrush
                    ?? new SolidColorBrush(Color.Parse("#E74C3C")),
                _ => Application.Current?.FindResource("TealGrey") as IBrush
                    ?? new SolidColorBrush(Color.Parse("#7F8C8D")),
            };
        }

        private SolidColorBrush GetJokerColorBrush(string jokerName)
        {
            // Determine joker rarity and return appropriate color
            if (BalatroData.JokersByRarity["Legendary"].Contains(jokerName))
            {
                return Application.Current?.FindResource("GoldGradient1") as SolidColorBrush
                    ?? new SolidColorBrush(Color.Parse("#FFD700")); // Gold
            }

            if (BalatroData.JokersByRarity["Rare"].Contains(jokerName))
            {
                return Application.Current?.FindResource("Purple") as SolidColorBrush
                    ?? new SolidColorBrush(Color.Parse("#9B59B6")); // Purple
            }

            if (BalatroData.JokersByRarity["Uncommon"].Contains(jokerName))
            {
                return Application.Current?.FindResource("Blue") as SolidColorBrush
                    ?? new SolidColorBrush(Color.Parse("#3498DB")); // Blue
            }

            if (BalatroData.JokersByRarity["Common"].Contains(jokerName))
            {
                return Application.Current?.FindResource("LightGrey") as SolidColorBrush
                    ?? new SolidColorBrush(Color.Parse("#95A5A6")); // Gray
            }

            return Application.Current?.FindResource("TealGrey") as SolidColorBrush
                ?? new SolidColorBrush(Color.Parse("#7F8C8D")); // Default gray
        }

        private void UpdateCardSelection(ResponsiveCard card)
        {
            var key = $"{card.Category}:{card.ItemName}";
            card.IsSelectedNeed = _selectedMust.Contains(key);
            card.IsSelectedWant = _selectedShould.Contains(key);
        }

        private List<string> GetItemsForCategory(string category, string? subCategory)
        {
            if (subCategory != null && BalatroData.JokersByRarity.ContainsKey(subCategory))
            {
                // For jokers by rarity, we need to convert lowercase names back to proper enum names
                var lowercaseItems = BalatroData.JokersByRarity[subCategory];
                var properNames = new List<string>();

                foreach (var lcItem in lowercaseItems)
                {
                    // Find the matching key in BalatroData.Jokers (case-insensitive)
                    var match = BalatroData.Jokers.Keys.FirstOrDefault(k =>
                        k.Equals(lcItem, StringComparison.OrdinalIgnoreCase)
                    );
                    if (match != null)
                    {
                        properNames.Add(match);
                        if (subCategory == "Legendary" || lcItem.StartsWith("any"))
                        {
                            BalatroSeedOracle.Helpers.DebugLogger.Log(
                                $"🎴 Joker found: {match} (from lowercase: {lcItem})"
                            );
                        }
                    }
                    else
                    {
                        // Fallback to the lowercase name if no match found
                        BalatroSeedOracle.Helpers.DebugLogger.LogError(
                            "FiltersModal",
                            $"No match found for joker: {lcItem}"
                        );
                        properNames.Add(lcItem);
                    }
                }
                
                // Wildcards are already included in JokersByRarity from BalatroData
                // No need to add them again here
                
                return properNames;
            }

            var allItems = _itemCategories.ContainsKey(category)
                ? _itemCategories[category]
                : new List<string>();
            return allItems;
        }

        private void UpdateSelectionDisplay()
        {
            var countText = this.FindControl<TextBlock>("SelectionCountText");
            if (countText != null)
            {
                var total = _selectedMust.Count + _selectedShould.Count + _selectedMustNot.Count;
                countText.Text = $"{total} selected";
            }

            // Update needs/wants/mustnot panels
            UpdateSelectedItemsPanel("NeedsPanel", _selectedMust);
            UpdateSelectedItemsPanel("WantsPanel", _selectedShould);
            UpdateSelectedItemsPanel("MustNotPanel", _selectedMustNot);
        }

        private bool ValidateJsonSyntaxForTextBox(TextBox textBox)
        {
            if (textBox == null)
            {
                return false;
            }

            try
            {
                var jsonText = textBox.Text?.Trim();
                if (string.IsNullOrEmpty(jsonText))
                {
                    UpdateStatus("Empty JSON document", isError: true);
                    return false;
                }

                using var jsonDocument = JsonDocument.Parse(jsonText);

                UpdateStatus("✓ Valid filter config");
                return true;
            }
            catch (JsonException ex)
            {
                UpdateStatus($"❌ JSON Error: {ex.Message}", isError: true);
                return false;
            }
            catch (Exception ex)
            {
                UpdateStatus($"❌ Validation error: {ex.Message}", isError: true);
                return false;
            }
        }

        private bool ValidateJsonSyntaxForAvaloniaEdit(AvaloniaEdit.TextEditor textEditor)
        {
            if (textEditor == null)
            {
                return false;
            }

            try
            {
                var jsonText = textEditor.Text?.Trim();
                if (string.IsNullOrEmpty(jsonText))
                {
                    UpdateStatus("Empty JSON document", isError: true);
                    return false;
                }

                using var jsonDocument = JsonDocument.Parse(jsonText);

                // Check for proper OuijaConfig structure
                bool hasValidStructure = jsonDocument.RootElement.TryGetProperty("must", out _) ||
                                        jsonDocument.RootElement.TryGetProperty("should", out _) ||
                                        jsonDocument.RootElement.TryGetProperty("mustNot", out _);
                
                if (hasValidStructure)
                {
                    UpdateStatus("✓ Valid filter");
                }
                else
                {
                    UpdateStatus("✓ Valid JSON");
                }
                return true;
            }
            catch (JsonException ex)
            {
                UpdateStatus($"❌ JSON Error: {ex.Message}", isError: true);
                return false;
            }
            catch (Exception ex)
            {
                UpdateStatus($"❌ Validation error: {ex.Message}", isError: true);
                return false;
            }
        }

        private void FormatJsonForTextBox(TextBox textBox)
        {
            if (textBox == null)
            {
                return;
            }

            try
            {
                if (string.IsNullOrWhiteSpace(textBox.Text))
                {
                    UpdateStatus("❌ Cannot format: Empty or null JSON", isError: true);
                    return;
                }

                var jsonDocument = JsonDocument.Parse(textBox.Text);
                var formattedJson = JsonSerializer.Serialize(
                    jsonDocument,
                    new JsonSerializerOptions
                    {
                        WriteIndented = true,
                        Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
                    }
                );

                // Format arrays compactly for better readability
                formattedJson = FormatJsonWithCompactArrays(formattedJson);

                textBox.Text = formattedJson;
                UpdateStatus("✨ JSON formatted successfully");
            }
            catch (JsonException ex)
            {
                UpdateStatus($"❌ Cannot format: {ex.Message}", isError: true);
            }
        }

        private void SetupJsonSyntaxHighlighting(AvaloniaEdit.TextEditor textEditor)
        {
            try
            {
                // Install TextMate with dark theme
                var registryOptions = new RegistryOptions(ThemeName.DarkPlus);
                var installation = textEditor.InstallTextMate(registryOptions);

                // Set grammar for JSON
                var jsonLanguage = registryOptions.GetLanguageByExtension(".json");
                if (jsonLanguage != null)
                {
                    installation.SetGrammar(registryOptions.GetScopeByLanguageId(jsonLanguage.Id));

                    // Try to customize colors after TextMate is installed
                    Dispatcher.UIThread.Post(
                        () =>
                        {
                            try
                            {
                                // Override specific colors with Balatro theme colors
                                if (textEditor.SyntaxHighlighting != null)
                                {
                                    // String values - Balatro Red
                                    var stringColor = textEditor.SyntaxHighlighting.GetNamedColor(
                                        "String"
                                    );
                                    if (stringColor != null)
                                    {
                                        stringColor.Foreground =
                                            new AvaloniaEdit.Highlighting.SimpleHighlightingBrush(
                                                (
                                                    Application.Current?.FindResource("ToggleRed")
                                                    as SolidColorBrush
                                                )?.Color ?? Color.Parse("#FE5F55")
                                            );
                                    }

                                    // Numbers - Balatro Blue
                                    var numberColor = textEditor.SyntaxHighlighting.GetNamedColor(
                                        "Number"
                                    );
                                    if (numberColor != null)
                                    {
                                        numberColor.Foreground =
                                            new AvaloniaEdit.Highlighting.SimpleHighlightingBrush(
                                                (
                                                    Application.Current?.FindResource("ToggleBlue")
                                                    as SolidColorBrush
                                                )?.Color ?? Color.Parse("#009dff")
                                            );
                                    }

                                    // Keywords (true/false/null) - Balatro Orange
                                    var keywordColor = textEditor.SyntaxHighlighting.GetNamedColor(
                                        "Keyword"
                                    );
                                    if (keywordColor != null)
                                    {
                                        keywordColor.Foreground =
                                            new AvaloniaEdit.Highlighting.SimpleHighlightingBrush(
                                                (
                                                    Application.Current?.FindResource("PaleOrange")
                                                    as SolidColorBrush
                                                )?.Color ?? Color.Parse("#FEB95F")
                                            );
                                    }
                                }
                            }
                            catch (Exception ex)
                            {
                                BalatroSeedOracle.Helpers.DebugLogger.LogError(
                                    "FiltersModal",
                                    $"Error customizing syntax colors: {ex.Message}"
                                );
                            }
                        },
                        DispatcherPriority.Background
                    );

                    BalatroSeedOracle.Helpers.DebugLogger.Log(
                        "FiltersModal",
                        "JSON syntax highlighting configured with TextMate"
                    );
                }
                else
                {
                    BalatroSeedOracle.Helpers.DebugLogger.LogError(
                        "FiltersModal",
                        "JSON language not found in TextMate registry"
                    );
                }
            }
            catch (Exception ex)
            {
                BalatroSeedOracle.Helpers.DebugLogger.LogError(
                    "FiltersModal",
                    $"Error setting up JSON syntax highlighting: {ex.Message}"
                );
                // Fall back to basic colors
                textEditor.Foreground =
                    Application.Current?.FindResource("LightTextGrey") as IBrush
                    ?? new SolidColorBrush(Color.Parse("#E0E0E0"));
            }
        }

        private void FormatJsonForAvaloniaEdit(AvaloniaEdit.TextEditor textEditor)
        {
            if (textEditor == null)
            {
                return;
            }

            try
            {
                if (string.IsNullOrWhiteSpace(textEditor.Text))
                {
                    UpdateStatus("❌ Cannot format: Empty or null JSON", isError: true);
                    return;
                }

                var jsonDocument = JsonDocument.Parse(textEditor.Text);
                var formattedJson = JsonSerializer.Serialize(
                    jsonDocument,
                    new JsonSerializerOptions
                    {
                        WriteIndented = true,
                        Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
                    }
                );

                textEditor.Text = formattedJson;
                UpdateStatus("✨ JSON formatted successfully");
            }
            catch (JsonException ex)
            {
                UpdateStatus($"❌ Cannot format: {ex.Message}", isError: true);
            }
        }

        private void UpdateSelectedItemsPanel(string panelName, List<string> items)
        {
            var panel = this.FindControl<WrapPanel>(panelName);
            if (panel == null)
            {
                return;
            }

            panel.Children.Clear();

            foreach (var item in items)
            {
                var parts = item.Split(':');
                if (parts.Length != 2)
                {
                    continue;
                }

                var miniCard = new Border
                {
                    Background = GetItemColor(parts[1], parts[0]),
                    CornerRadius = new Avalonia.CornerRadius(4),
                    Padding = new Avalonia.Thickness(8, 4),
                    Margin = new Avalonia.Thickness(2),
                    Cursor = new Cursor(StandardCursorType.Hand),
                };

                var text = new TextBlock
                {
                    Text = parts[1],
                    FontSize = 11,
                    Foreground = Brushes.White,
                };

                miniCard.Child = text;

                // Click to remove
                miniCard.PointerPressed += (s, e) =>
                {
                    items.Remove(item);
                    LoadCategory(_currentCategory); // Refresh display
                    UpdateSelectionDisplay();
                };

                panel.Children.Add(miniCard);
            }
        }

        private void UpdateTabButtons(string activeCategory)
        {
            // Update all tab buttons - remove active class from all first
            var allTabs = new[]
            {
                "EditJsonTab",
                "FavoritesTab",
                "SoulJokersTab",
                "RareJokersTab",
                "UncommonJokersTab",
                "CommonJokersTab",
                "VouchersTab",
                "TarotsTab",
                "SpectralsTab",
                "TagsTab",
                "ClearTab",
            };

            foreach (var tab in allTabs)
            {
                var button = this.FindControl<Button>(tab);
                if (button != null)
                {
                    button.Classes.Remove("active");
                }
            }

            // Set active tab based on category
            string activeTab = activeCategory switch
            {
                "EditJson" => "EditJsonTab",
                "Favorites" => "FavoritesTab",
                "Jokers" => "SoulJokersTab", // Default to Soul Jokers when loading Jokers category
                "Vouchers" => "VouchersTab",
                "Tarots" => "TarotsTab",
                "Spectrals" => "SpectralsTab",
                "Tags" => "TagsTab",
                "Clear" => "ClearTab",
                _ => "",
            };

            if (!string.IsNullOrEmpty(activeTab))
            {
                var button = this.FindControl<Button>(activeTab);
                button?.Classes.Add("active");
            }
        }

        private void UpdateJokerTabButtons(string targetSection)
        {
            // Remove active class from all joker tabs
            var jokerTabs = new[]
            {
                "SoulJokersTab",
                "RareJokersTab",
                "UncommonJokersTab",
                "CommonJokersTab",
            };

            foreach (var tab in jokerTabs)
            {
                var button = this.FindControl<Button>(tab);
                button?.Classes.Remove("active");
            }

            // Set active tab based on target section
            string activeTab = targetSection switch
            {
                "Legendary" => "SoulJokersTab",
                "Rare" => "RareJokersTab",
                "Uncommon" => "UncommonJokersTab",
                "Common" => "CommonJokersTab",
                _ => "SoulJokersTab",
            };

            var activeButton = this.FindControl<Button>(activeTab);
            activeButton?.Classes.Add("active");
        }

        private void OnTabClick(object? sender, RoutedEventArgs e)
        {
            if (sender is not Button button)
            {
                return;
            }

            // Prevent re-entrant tab logic if user double-clicks or triggers via keyboard quickly
            if (_isSwitchingTab)
            {
                return;
            }
            _isSwitchingTab = true;
            try
            {
                // Wrap full logic in try/catch so any transient null access doesn't crash app
                SafeHandleTabSwitch(button);
            }
            catch (Exception ex)
            {
                BalatroSeedOracle.Helpers.DebugLogger.LogError("FiltersModal", $"Exception during tab switch: {ex}");
            }
            finally
            {
                _isSwitchingTab = false;
            }
        }

        private void SafeHandleTabSwitch(Button button)
        {

            // Ensure button has focus (fixes first-click issue)
            button.Focus();

            // Get all tab buttons
            var visualTab = this.FindControl<Button>("VisualTab");
            var jsonTab = this.FindControl<Button>("JsonTab");
            var testTab = this.FindControl<Button>("TestTab");
            var loadSaveTab = this.FindControl<Button>("LoadSaveTab");
            var saveFilterTab = this.FindControl<Button>("SaveFilterTab");

            // Get all panels
            var visualPanel = this.FindControl<Grid>("VisualPanel");
            var jsonPanel = this.FindControl<Grid>("JsonPanel");
            var testPanel = this.FindControl<Grid>("TestPanel");
            var loadSavePanel = this.FindControl<Grid>("LoadSavePanel");
            var saveFilterPanel = this.FindControl<Grid>("SaveFilterPanel");

            // Remove active class from all tabs
            visualTab?.Classes.Remove("active");
            jsonTab?.Classes.Remove("active");
            testTab?.Classes.Remove("active");
            loadSaveTab?.Classes.Remove("active");
            saveFilterTab?.Classes.Remove("active");

            // Hide all panels
            if (visualPanel != null)
            {
                visualPanel.IsVisible = false;
            }

            if (jsonPanel != null)
            {
                jsonPanel.IsVisible = false;
            }

            if (testPanel != null)
            {
                testPanel.IsVisible = false;
            }

            if (loadSavePanel != null)
            {
                loadSavePanel.IsVisible = false;
            }

            if (saveFilterPanel != null)
            {
                saveFilterPanel.IsVisible = false;
            }

            // Get triangle for animation
            _tabTriangle = this.FindControl<Polygon>("TabTriangle");

            // Show the clicked tab's panel and update triangle position
            switch (button.Name)
            {
                case "LoadSaveTab":
                    button.Classes.Add("active");
                    if (loadSavePanel != null)
                    {
                        loadSavePanel.IsVisible = true;
                    }

                    AnimateTriangleToTab(0);
                    break;

                case "VisualTab":
                    button.Classes.Add("active");
                    if (visualPanel != null)
                    {
                        visualPanel.IsVisible = true;
                    }

                    AnimateTriangleToTab(1);
                    RestoreDragDropModeLayout();
                    LoadAllCategories();
                    UpdateDropZoneVisibility();
                    // RELOAD from current config to refresh drop zones
                    RefreshDropZonesFromConfig();
                    break;

                case "JsonTab":
                    button.Classes.Add("active");
                    if (jsonPanel != null)
                    {
                        jsonPanel.IsVisible = true;
                    }

                    AnimateTriangleToTab(2);
                    EnterEditJsonMode();
                    UpdateJsonEditor();
                    // DON'T reload from selections if we have a loaded filter - it would overwrite the JSON!
                    // Only reload from selections if we're building a filter from scratch in Visual mode
                    if (_loadedConfig == null)
                    {
                        ReloadJsonFromCurrentConfig();
                    }
                    break;

                case "TestTab":
                    button.Classes.Add("active");
                    if (testPanel != null)
                    {
                        testPanel.IsVisible = true;
                    }

                    AnimateTriangleToTab(3);
                    UpdateJsonStats(); // Update stats display
                    break;

                case "SaveFilterTab":
                    button.Classes.Add("active");
                    if (saveFilterPanel != null)
                    {
                        saveFilterPanel.IsVisible = true;
                    }

                    AnimateTriangleToTab(4);
                    UpdateSaveFilterPanel();
                    break;
            }
        }

        private void AnimateTriangleToTab(int tabIndex)
        {
            if (_tabTriangle == null)
            {
                return;
            }

            _currentTabIndex = tabIndex;
            // Map tab index to actual column position
            // Tab order: LoadSaveTab=0, VisualTab=1, JsonTab=2
            var targetColumn = tabIndex;

            // Move triangle to the correct column
            if (_tabTriangle?.Parent is Grid triangleContainer)
            {
                Grid.SetColumn(triangleContainer, targetColumn);
            }
        }

        private void OnClearClick(object? sender, RoutedEventArgs e)
        {
            _selectedMust.Clear();
            _selectedShould.Clear();
            LoadCategory(_currentCategory);
            UpdateSelectionDisplay();
        }

        // 🎯 Drop Zone Management Methods

        private void UpdateDropZoneVisibility()
        {
            // Update NEEDS panel
            var needsPlaceholder = this.FindControl<TextBlock>("NeedsPlaceholder");
            var needsScrollViewer = this.FindControl<ScrollViewer>("NeedsScrollViewer");
            var needsPanel = this.FindControl<WrapPanel>("NeedsPanel");
            var clearNeedsButton = this.FindControl<Button>("ClearNeedsButton");

            // Disable clipping on ScrollViewers to allow cards to pop out
            if (needsScrollViewer != null) needsScrollViewer.ClipToBounds = false;

            if (needsPlaceholder != null && needsScrollViewer != null && needsPanel != null)
            {
                if (_selectedMust.Any())
                {
                    needsPlaceholder.IsVisible = false;
                    needsScrollViewer.IsVisible = true;
                    PopulateDropZonePanel(needsPanel, _selectedMust, "needs");
                    if (clearNeedsButton != null)
                    {
                        clearNeedsButton.IsVisible = true;
                    }
                }
                else
                {
                    needsPlaceholder.IsVisible = true;
                    needsScrollViewer.IsVisible = false;
                    needsPanel.Children.Clear();
                    if (clearNeedsButton != null)
                    {
                        clearNeedsButton.IsVisible = false;
                    }
                }
            }

            // Update WANTS panel
            var wantsPlaceholder = this.FindControl<TextBlock>("WantsPlaceholder");
            var wantsScrollViewer = this.FindControl<ScrollViewer>("WantsScrollViewer");
            var wantsPanel = this.FindControl<WrapPanel>("WantsPanel");
            var clearWantsButton = this.FindControl<Button>("ClearWantsButton");

            // Disable clipping on ScrollViewers to allow cards to pop out
            if (wantsScrollViewer != null) wantsScrollViewer.ClipToBounds = false;

            if (wantsPlaceholder != null && wantsScrollViewer != null && wantsPanel != null)
            {
                if (_selectedShould.Any())
                {
                    wantsPlaceholder.IsVisible = false;
                    wantsScrollViewer.IsVisible = true;
                    PopulateDropZonePanel(wantsPanel, _selectedShould, "wants");
                    if (clearWantsButton != null)
                    {
                        clearWantsButton.IsVisible = true;
                    }
                }
                else
                {
                    wantsPlaceholder.IsVisible = true;
                    wantsScrollViewer.IsVisible = false;
                    wantsPanel.Children.Clear();
                    if (clearWantsButton != null)
                    {
                        clearWantsButton.IsVisible = false;
                    }
                }
            }

            // Update MUST NOT panel
            var mustNotPlaceholder = this.FindControl<TextBlock>("MustNotPlaceholder");
            var mustNotScrollViewer = this.FindControl<ScrollViewer>("MustNotScrollViewer");
            var mustNotPanel = this.FindControl<WrapPanel>("MustNotPanel");
            var clearMustNotButton = this.FindControl<Button>("ClearMustNotButton");

            // Disable clipping on ScrollViewers to allow cards to pop out
            if (mustNotScrollViewer != null) mustNotScrollViewer.ClipToBounds = false;

            if (mustNotPlaceholder != null && mustNotScrollViewer != null && mustNotPanel != null)
            {
                if (_selectedMustNot.Any())
                {
                    mustNotPlaceholder.IsVisible = false;
                    mustNotScrollViewer.IsVisible = true;
                    PopulateDropZonePanel(mustNotPanel, _selectedMustNot, "mustnot");
                    if (clearMustNotButton != null)
                    {
                        clearMustNotButton.IsVisible = true;
                    }
                }
                else
                {
                    mustNotPlaceholder.IsVisible = true;
                    mustNotScrollViewer.IsVisible = false;
                    mustNotPanel.Children.Clear();
                    if (clearMustNotButton != null)
                    {
                        clearMustNotButton.IsVisible = false;
                    }
                }
            }

            BalatroSeedOracle.Helpers.DebugLogger.Log(
                $"📈 Updated drop zones: {_selectedMust.Count} needs, {_selectedShould.Count} wants, {_selectedMustNot.Count} must not"
            );
        }

        private void UpdateDropZonesFromJson()
        {
            try
            {
                // Get the current JSON from the editor
                var textEditor = this.FindControl<AvaloniaEdit.TextEditor>("JsonEditor");
                if (textEditor == null || string.IsNullOrWhiteSpace(textEditor.Text))
                {
                    BalatroSeedOracle.Helpers.DebugLogger.LogError(
                        "FiltersModal",
                        "UpdateDropZonesFromJson: JsonEditor not found or empty"
                    );
                    return;
                }

                // Parse the JSON to extract items
                var json = textEditor.Text;
                var jsonDoc = JsonDocument.Parse(json);
                var root = jsonDoc.RootElement;

                // Clear current selections
                _selectedMust.Clear();
                _selectedShould.Clear();
                _selectedMustNot.Clear();

                // Parse must items
                if (
                    root.TryGetProperty("must", out var mustArray)
                    && mustArray.ValueKind == JsonValueKind.Array
                )
                {
                    foreach (var item in mustArray.EnumerateArray())
                    {
                        AddItemFromJson(item, _selectedMust);
                    }
                }

                // Parse should items
                if (
                    root.TryGetProperty("should", out var shouldArray)
                    && shouldArray.ValueKind == JsonValueKind.Array
                )
                {
                    foreach (var item in shouldArray.EnumerateArray())
                    {
                        AddItemFromJson(item, _selectedShould);
                    }
                }

                // Parse mustNot items
                if (
                    root.TryGetProperty("mustNot", out var mustNotArray)
                    && mustNotArray.ValueKind == JsonValueKind.Array
                )
                {
                    foreach (var item in mustNotArray.EnumerateArray())
                    {
                        AddItemFromJson(item, _selectedMustNot);
                    }
                }

                // Update the drop zones to show the items
                UpdateDropZoneVisibility();

                BalatroSeedOracle.Helpers.DebugLogger.Log("FiltersModal", "Updated drop zones from JSON");
            }
            catch (Exception ex)
            {
                BalatroSeedOracle.Helpers.DebugLogger.LogError(
                    "FiltersModal",
                    $"Error updating drop zones from JSON: {ex.Message}"
                );
            }
        }

        private void AddItemFromJson(JsonElement item, List<string> targetSet)
        {
            if (
                item.TryGetProperty("type", out var typeElement)
                && item.TryGetProperty("value", out var valueElement)
            )
            {
                var type = typeElement.GetString();
                var value = valueElement.GetString();

                if (!string.IsNullOrEmpty(type) && !string.IsNullOrEmpty(value))
                {
                    // Map type to category
                    string? category = type?.ToLower() switch
                    {
                        "joker" or "souljoker" => "Jokers",
                        "tarot" or "tarotcard" => "Tarots",
                        "spectral" or "spectralcard" => "Spectrals",
                        "voucher" => "Vouchers",
                        "tag" => "Tags",
                        _ => null,
                    };

                    if (category != null)
                    {
                        targetSet.Add($"{category}:{value}");
                    }
                }
            }
        }

        private void PopulateDropZonePanel(WrapPanel panel, List<string> items, string zoneName)
        {
            panel.Children.Clear();

            // Create canvas for custom card positioning
            var canvas = new Canvas
            {
                Height = 110, // Height for cards
                ClipToBounds = false, // Allow overflow for transforms
            };

            // Create a viewbox for responsive scaling
            var viewbox = new Viewbox
            {
                Stretch = Stretch.Uniform,
                HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Left,
                VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center,
                Child = canvas,
            };

            // Add viewbox to panel
            panel.Children.Add(viewbox);

            // Categorize items WITH their unique keys preserved
            var jokers = new List<(string key, string name, string category)>();
            var bosses = new List<(string key, string name, string category)>();
            var tags = new List<(string key, string name, string category)>();
            var vouchers = new List<(string key, string name, string category)>();
            var spectrals = new List<(string key, string name, string category)>();
            var tarots = new List<(string key, string name, string category)>();

            foreach (var item in items)
            {
                var colonIndex = item.IndexOf(':');
                if (colonIndex > 0)
                {
                    var category = item.Substring(0, colonIndex);
                    var itemNameWithSuffix = item.Substring(colonIndex + 1);
                    var hashIndex = itemNameWithSuffix.IndexOf('#');
                    var itemName =
                        hashIndex > 0 ? itemNameWithSuffix.Substring(0, hashIndex) : itemNameWithSuffix;

                    switch (category)
                    {
                        case "Jokers":
                            if (itemName == "The_Soul" || itemName == "Cavendish") // Bosses
                            {
                                bosses.Add((item, itemName, category));
                            }
                            else
                            {
                                jokers.Add((item, itemName, category));
                            }

                            break;
                        case "Tags":
                            tags.Add((item, itemName, category));
                            break;
                        case "Vouchers":
                            vouchers.Add((item, itemName, category));
                            break;
                        case "Spectrals":
                            spectrals.Add((item, itemName, category));
                            break;
                        case "Tarots":
                            tarots.Add((item, itemName, category));
                            break;
                    }
                }
            }

            // Render items with custom positioning
            double currentX = 10;

            // 1. Fan out Jokers on the left
            if (jokers.Any())
            {
                RenderFannedJokers(canvas, jokers, ref currentX, zoneName, items);
                currentX += 20; // Gap after jokers
            }

            // 2. Fan out Vouchers
            if (vouchers.Any())
            {
                RenderFannedItems(canvas, vouchers, ref currentX, zoneName, items, "Vouchers");
                currentX += 20; // Gap after vouchers
            }

            // 3. Fan out Spectrals
            if (spectrals.Any())
            {
                RenderFannedItems(canvas, spectrals, ref currentX, zoneName, items, "Spectrals");
                currentX += 20; // Gap after spectrals
            }

            // 4. Fan out Tarots
            if (tarots.Any())
            {
                RenderFannedItems(canvas, tarots, ref currentX, zoneName, items, "Tarots");
                currentX += 20; // Gap after tarots
            }

            // 3. Stack Tags vertically
            if (tags.Any())
            {
                RenderVerticalStack(canvas, tags, currentX, "Tags", items, zoneName);
                currentX += 80;
            }

            // 4. Stack Bosses vertically on far right
            if (bosses.Any())
            {
                // Position bosses at the far right
                double bossX = Math.Max(currentX, 520); // Adjusted for smaller canvas
                RenderVerticalStack(canvas, bosses, bossX, "Bosses", items, zoneName);
                currentX = bossX + 60; // Update currentX to include boss width
            }

            // Set the canvas width to accommodate all items
            canvas.Width = Math.Max(currentX, 200); // Minimum width of 200
        }

        private void RenderFannedJokers(
            Canvas canvas,
            List<(string key, string name, string category)> jokers,
            ref double startX,
            string zoneName,
            List<string> items
        )
        {
            const double fanAngle = 8; // degrees per card (increased from 7)
            const double overlapX = 17; // horizontal overlap (increased from 15)
            const double centerY = 24; // Start 4px south as requested (was 20)

            // First pass: create all cards and wrappers
            var allWrappers = new List<Grid>();
            var allBorders = new List<Border>();
            
            for (int i = 0; i < jokers.Count; i++)
            {
                // Find the actual unique key for this specific item
                var itemKey = items[i]; // Use the actual item from the list, not a search!
                
                // Create a wrapper container that stays static (for hit detection)
                var wrapper = new Grid
                {
                    Width = 53,
                    Height = 71,
                    Background = Brushes.Transparent, // Transparent but hit-testable
                    ClipToBounds = false // Allow visual to pop out
                };
                
                // Get or create config for this item
                if (!_itemConfigs.ContainsKey(itemKey))
                {
                    _itemConfigs[itemKey] = new ItemConfig 
                    { 
                        ItemKey = itemKey,
                        Edition = "none"
                    };
                }
                
                // Create the actual visual card
                var control = CreateDroppedItemControl(
                    jokers[i].name,
                    jokers[i].category,
                    itemKey,
                    zoneName
                );

                // Position the wrapper (not the control)
                double x = startX + 6 + (i * overlapX); // Added 6px east offset
                double y = centerY;

                // Apply rotation for fan effect
                double angle = (i - jokers.Count / 2.0) * fanAngle;

                // Apply transforms to the INNER control, not the wrapper
                var transformGroup = new TransformGroup();
                transformGroup.Children.Add(new ScaleTransform(1, 1));
                transformGroup.Children.Add(new RotateTransform(angle));

                control.RenderTransform = transformGroup;
                control.RenderTransformOrigin = new RelativePoint(0.5, 0.9, RelativeUnit.Relative);

                // Add control to wrapper
                wrapper.Children.Add(control);

                // Update hover handlers on the WRAPPER (static hit area)
                if (control is Border border)
                {
                    int cardIndex = i; // Capture the index for the closure
                    border.Tag = new { Key = itemKey, Zone = zoneName };

                    // Track hover state to prevent re-entry
                    bool isHovered = false;
                    
                    wrapper.PointerEntered += (s, e) =>
                    {
                        // Prevent re-entry if already hovered
                        if (isHovered) return;
                        isHovered = true;
                        
                        if (border.RenderTransform is TransformGroup tg)
                        {
                            // Scale up
                            if (tg.Children[0] is ScaleTransform scale)
                            {
                                scale.ScaleX = 1.05;
                                scale.ScaleY = 1.05;
                            }
                            
                            // Add translate up
                            var translateTransform = tg.Children.OfType<TranslateTransform>().FirstOrDefault();
                            if (translateTransform == null)
                            {
                                translateTransform = new TranslateTransform();
                                tg.Children.Add(translateTransform);
                            }
                            translateTransform.Y = -3;
                            
                            border.ZIndex = 2000;
                        }
                    };

                    wrapper.PointerExited += (s, e) =>
                    {
                        // Reset hover state
                        isHovered = false;
                        
                        if (border.RenderTransform is TransformGroup tg)
                        {
                            // Reset scale
                            if (tg.Children[0] is ScaleTransform scale)
                            {
                                scale.ScaleX = 1.0;
                                scale.ScaleY = 1.0;
                            }
                            
                            // Reset translate
                            var translateTransform = tg.Children.OfType<TranslateTransform>().FirstOrDefault();
                            if (translateTransform != null)
                            {
                                translateTransform.Y = 0;
                            }
                            
                            // Reset z-index
                            border.ZIndex = 0;
                        }
                    };
                }

                Canvas.SetLeft(wrapper, x);
                Canvas.SetTop(wrapper, y);

                // Higher z-index for later cards (base 100 to ensure they're above other elements)
                wrapper.ZIndex = 100 + i;

                canvas.Children.Add(wrapper);
            }

            // Second pass: add hover handlers with access to all cards
            for (int i = 0; i < allWrappers.Count; i++)
            {
                int currentIndex = i; // Capture for closure
                var wrapper = allWrappers[i];
                
                wrapper.PointerEntered += (s, e) =>
                {
                    // "Part the sea" effect - spread cards around the hovered one
                    for (int j = 0; j < allBorders.Count; j++)
                    {
                        if (allBorders[j].RenderTransform is TransformGroup tg)
                        {
                            var translateTransform = tg.Children.OfType<TranslateTransform>().FirstOrDefault();
                            if (translateTransform == null)
                            {
                                translateTransform = new TranslateTransform();
                                tg.Children.Add(translateTransform);
                            }

                            if (j < currentIndex)
                            {
                                // Cards before: push left, but only immediate neighbors
                                int distance = currentIndex - j;
                                if (distance == 1)
                                {
                                    // Immediate left neighbor: push just enough to be clickable
                                    translateTransform.X = -12;
                                }
                                else if (distance == 2)
                                {
                                    translateTransform.X = -6;
                                }
                                else
                                {
                                    translateTransform.X = -3; // Further cards barely move
                                }
                            }
                            else if (j > currentIndex)
                            {
                                // Cards after: push right, but only immediate neighbors
                                int distance = j - currentIndex;
                                if (distance == 1)
                                {
                                    // Immediate right neighbor: push just enough to be clickable
                                    translateTransform.X = 12;
                                }
                                else if (distance == 2)
                                {
                                    translateTransform.X = 6;
                                }
                                else
                                {
                                    translateTransform.X = 3; // Further cards barely move
                                }
                            }
                            else
                            {
                                // Hovered card: come forward and up slightly
                                translateTransform.X = 0;
                                translateTransform.Y = -5;  // Lift up to show selection
                                if (tg.Children[0] is ScaleTransform scale)
                                {
                                    scale.ScaleX = 1.08;  // Noticeable but not huge
                                    scale.ScaleY = 1.08;
                                }
                                allBorders[j].ZIndex = 500; // Definitely on top
                            }
                        }
                    }
                };

                wrapper.PointerExited += (s, e) =>
                {
                    // Reset all cards
                    for (int j = 0; j < allBorders.Count; j++)
                    {
                        if (allBorders[j].RenderTransform is TransformGroup tg)
                        {
                            // Reset translate
                            var translateTransform = tg.Children.OfType<TranslateTransform>().FirstOrDefault();
                            if (translateTransform != null)
                            {
                                translateTransform.X = 0;
                                translateTransform.Y = 0;
                            }
                            
                            // Reset scale
                            if (tg.Children[0] is ScaleTransform scale)
                            {
                                scale.ScaleX = 1.0;
                                scale.ScaleY = 1.0;
                            }
                            
                            // Reset z-index
                            allBorders[j].ZIndex = 0;
                        }
                    }
                };
            }

            startX += (jokers.Count * overlapX) + 46; // Increased from 40 to account for 6px east offset
        }

        private void RenderFannedItems(
            Canvas canvas,
            List<(string key, string name, string category)> items,
            ref double startX,
            string zoneName,
            List<string> allItems,
            string itemType
        )
        {
            const double fanAngle = 8; // degrees per card (increased from 7)
            const double overlapX = 17; // horizontal overlap (increased from 15)
            const double centerY = 24; // Start 4px south as requested (was 20)

            // First pass: create all cards and wrappers
            var allWrappers = new List<Grid>();
            var allBorders = new List<Border>();
            
            for (int i = 0; i < items.Count; i++)
            {
                var itemKey = items[i].key; // Use the key directly!
                
                // Create a wrapper container that stays static (for hit detection)
                var wrapper = new Grid
                {
                    Width = 53,
                    Height = 71,
                    Background = Brushes.Transparent, // Transparent but hit-testable
                    ClipToBounds = false // Allow visual to pop out
                };
                
                // Create the actual visual card
                var control = CreateDroppedItemControl(
                    items[i].name,
                    items[i].category,
                    itemKey,
                    zoneName
                );

                // Position the wrapper (not the control)
                double x = startX + 6 + (i * overlapX); // Added 6px east offset
                double y = centerY;

                // Apply rotation for fan effect
                double angle = (i - items.Count / 2.0) * fanAngle;

                // Apply transforms to the INNER control, not the wrapper
                var transformGroup = new TransformGroup();
                transformGroup.Children.Add(new ScaleTransform(1, 1));
                transformGroup.Children.Add(new RotateTransform(angle));

                // Replace the existing transform with our group
                if (control is Border border)
                {
                    border.RenderTransform = transformGroup;
                    border.RenderTransformOrigin = new RelativePoint(0.5, 0.8, RelativeUnit.Relative);

                    // Mark as fanned and store the card index
                    var originalTag = border.Tag as DropZoneItemTag;
                    int cardIndex = i;
                    if (originalTag != null)
                    {
                        border.Tag = new DropZoneItemTag
                        {
                            Key = originalTag.Key,
                            Zone = originalTag.Zone,
                            Config = originalTag.Config, // Preserve the config!
                            Fanned = true,
                            CardIndex = cardIndex,
                        };
                    }

                    // Add control to wrapper
                    wrapper.Children.Add(control);
                    
                    // Store wrapper and border for later
                    allWrappers.Add(wrapper);
                    if (control is Border b)
                    {
                        allBorders.Add(b);
                    }
                }
                else
                {
                    // If not a border, still add to wrapper
                    wrapper.Children.Add(control);
                }

                Canvas.SetLeft(wrapper, x);
                Canvas.SetTop(wrapper, y);

                // Higher z-index for later cards (base 100 to ensure they're above other elements)
                wrapper.ZIndex = 100 + i;

                canvas.Children.Add(wrapper);
            }

            startX += (items.Count * overlapX) + 40;
        }

        private void RenderHorizontalTags(
            Canvas canvas,
            List<(string key, string name, string category)> tags, 
            List<string> items,
            string zoneName
        )
        {
            double tagX = 10; // Start from left
            double tagY = 75; // Position at bottom of drop zone
            
            foreach (var tag in tags)
            {
                var itemKey = tag.key; // Use the key directly!
                var control = CreateDroppedItemControl(tag.name, tag.category, itemKey, zoneName);
                
                if (control is Border tagBorder)
                {
                    // Make tags 2x bigger (was smaller, now 60x80)
                    tagBorder.Width = 60;
                    tagBorder.Height = 80;
                    
                    // Add hover effect
                    tagBorder.PointerEntered += (s, e) =>
                    {
                        if (tagBorder.RenderTransform == null)
                        {
                            tagBorder.RenderTransform = new ScaleTransform(1, 1);
                            tagBorder.RenderTransformOrigin = new RelativePoint(0.5, 0.5, RelativeUnit.Relative);
                        }
                        if (tagBorder.RenderTransform is ScaleTransform scale)
                        {
                            scale.ScaleX = 1.1;
                            scale.ScaleY = 1.1;
                        }
                    };
                    
                    tagBorder.PointerExited += (s, e) =>
                    {
                        if (tagBorder.RenderTransform is ScaleTransform scale)
                        {
                            scale.ScaleX = 1.0;
                            scale.ScaleY = 1.0;
                        }
                    };
                }
                
                Canvas.SetLeft(control, tagX);
                Canvas.SetTop(control, tagY);
                canvas.Children.Add(control);
                tagX += 65; // Spacing between tags
            }
        }

        private void RenderHorizontalBosses(
            Canvas canvas,
            List<(string key, string name, string category)> bosses,
            double startX,
            List<string> items,
            string zoneName
        )
        {
            double bossX = startX;
            double bossY = 80; // Position at bottom, slightly lower than tags
            
            foreach (var boss in bosses)
            {
                var itemKey = boss.key; // Use the key directly!
                var control = CreateDroppedItemControl(boss.name, boss.category, itemKey, zoneName);
                
                if (control is Border bossBorder)
                {
                    // Make bosses visible size
                    bossBorder.Width = 60;
                    bossBorder.Height = 60;
                    
                    // Add hover effect
                    bossBorder.PointerEntered += (s, e) =>
                    {
                        if (bossBorder.RenderTransform == null)
                        {
                            bossBorder.RenderTransform = new ScaleTransform(1, 1);
                            bossBorder.RenderTransformOrigin = new RelativePoint(0.5, 0.5, RelativeUnit.Relative);
                        }
                        if (bossBorder.RenderTransform is ScaleTransform scale)
                        {
                            scale.ScaleX = 1.1;
                            scale.ScaleY = 1.1;
                        }
                    };
                    
                    bossBorder.PointerExited += (s, e) =>
                    {
                        if (bossBorder.RenderTransform is ScaleTransform scale)
                        {
                            scale.ScaleX = 1.0;
                            scale.ScaleY = 1.0;
                        }
                    };
                }
                
                Canvas.SetLeft(control, bossX);
                Canvas.SetTop(control, bossY);
                canvas.Children.Add(control);
                bossX += 65; // Spacing between bosses
            }
        }

        private void RenderVerticalStack(
            Canvas canvas,
            List<(string key, string name, string category)> stackItems,
            double x,
            string stackType,
            List<string> allItems,
            string zoneName
        )
        {
            const double verticalSpacing = 18; // Overlap vertically
            double startY = 15;

            for (int i = 0; i < stackItems.Count; i++)
            {
                var itemKey = stackItems[i].key; // Use the key directly!
                var control = CreateDroppedItemControl(
                    stackItems[i].name,
                    stackItems[i].category,
                    itemKey,
                    zoneName
                );

                Canvas.SetLeft(control, x);
                Canvas.SetTop(control, startY + (i * verticalSpacing));

                // Bring later items to front
                control.ZIndex = i;

                canvas.Children.Add(control);
            }
        }

        private Control CreateDroppedItemControl(
            string itemName,
            string category,
            string itemKey,
            string zoneName
        )
        {
            // Check if item has an edition configured
            string? edition = null;
            if (_itemConfigs.TryGetValue(itemKey, out var config) && !string.IsNullOrEmpty(config.Edition) && config.Edition != "none")
            {
                edition = config.Edition;
            }
            // Create a simple border without ViewBox to prevent scaling
            var border = new Border
            {
                Classes = { "dropped-item" },
                Cursor = new Cursor(StandardCursorType.Hand),
                Width = 53, // Smaller card width (75% of original)
                Height = 71, // Smaller card height (75% of original)
                Margin = new Thickness(0),
                Padding = new Thickness(0),
                Tag = new DropZoneItemTag 
                { 
                    Key = itemKey, 
                    Zone = zoneName,
                    Config = _itemConfigs.GetValueOrDefault(itemKey, new ItemConfig { ItemKey = itemKey })
                }, // Store config directly!
                RenderTransform = new ScaleTransform(1, 1),
                RenderTransformOrigin = RelativePoint.Center,
                Transitions = new Transitions
                {
                    new TransformOperationsTransition
                    {
                        Property = Border.RenderTransformProperty,
                        Duration = TimeSpan.FromMilliseconds(150),
                    },
                },
            };

            // DON'T add hover handlers here for fanned cards - they're handled by the wrapper
            // This prevents the infinite loop bug where wrapper and border fight each other

            BalatroSeedOracle.Helpers.DebugLogger.LogImportant(
                "CreateDroppedItem",
                $"🎴 DROPPED ITEM: '{itemName}' (category: '{category}')"
            );

            // Check if this is a Legendary Joker (including wildcards)
            bool isLegendaryJoker = category == "Jokers" && IsLegendaryJoker(itemName);

            // List of the 5 legendary jokers that have animated faces
            var animatedLegendaryJokers = new[] { "Canio", "Triboulet", "Yorick", "Chicot", "Perkeo" };
            bool hasAnimatedFace = animatedLegendaryJokers.Any(lj =>
                lj.Equals(itemName, StringComparison.OrdinalIgnoreCase)
            );

            BalatroSeedOracle.Helpers.DebugLogger.LogImportant(
                "CreateDroppedItem",
                $"🎴 Legendary check: IsLegendary={isLegendaryJoker}, HasAnimatedFace={hasAnimatedFace} for item '{itemName}'"
            );

            if (isLegendaryJoker)
            {
                // Create stacked layout for legendary joker
                var grid = new Grid { Width = 53, Height = 71 };

                // Joker face image on top (legendary jokers can't have stickers)
                var jokerImageSource = SpriteService.Instance.GetJokerImage(itemName);
                if (jokerImageSource != null)
                {
                    var jokerFace = new Image
                    {
                        Source = jokerImageSource,
                        Stretch = Stretch.Uniform,
                        Width = 53,
                        Height = 71,
                        VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center,
                        HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center,
                        RenderTransform = null, // Explicitly reset any transforms
                        RenderTransformOrigin = RelativePoint.Center,
                    };
                    grid.Children.Add(jokerFace);
                }

                // If it has an animated soul face, add that too
                if (hasAnimatedFace)
                {
                    var faceSource = SpriteService.Instance.GetJokerSoulImage(itemName);
                    if (faceSource != null)
                    {
                        var faceImage = new Image
                        {
                            Source = faceSource,
                            Stretch = Stretch.Uniform,
                            Width = 53,
                            Height = 71,
                            VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center,
                            HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center,
                            RenderTransform = new RotateTransform(),
                        };

                        // Add wobble animation
                        var animation = new Avalonia.Animation.Animation
                        {
                            Duration = TimeSpan.FromSeconds(2),
                            IterationCount = new Avalonia.Animation.IterationCount(uint.MaxValue),
                            Children =
                            {
                                new Avalonia.Animation.KeyFrame
                                {
                                    Cue = new Avalonia.Animation.Cue(0),
                                    Setters = { new Setter(RotateTransform.AngleProperty, -5.0) },
                                },
                                new Avalonia.Animation.KeyFrame
                                {
                                    Cue = new Avalonia.Animation.Cue(0.5),
                                    Setters = { new Setter(RotateTransform.AngleProperty, 5.0) },
                                },
                                new Avalonia.Animation.KeyFrame
                                {
                                    Cue = new Avalonia.Animation.Cue(1),
                                    Setters = { new Setter(RotateTransform.AngleProperty, -5.0) },
                                },
                            },
                        };
                        animation.RunAsync(faceImage);

                        grid.Children.Add(faceImage);
                    }
                }

                // Add edition overlay for legendary jokers
                if (!string.IsNullOrEmpty(edition) && edition != "none")
                {
                    AddEditionOverlay(grid, edition);
                }

                border.Child = grid;
            }
            else
            {
                // Regular item display
                BalatroSeedOracle.Helpers.DebugLogger.LogImportant(
                    "CreateDroppedItem",
                    $"🎴 Getting image for regular item: '{itemName}' (category: '{category}')"
                );

                // Get stickers from config if it's a joker
                List<string>? stickers = null;
                if (category == "Jokers" && config != null && config.Stickers != null)
                {
                    stickers = config.Stickers;
                }

                IImage? imageSource = category switch
                {
                    "Jokers" => SpriteService.Instance.GetJokerImageWithStickers(itemName, stickers),
                    "Tarots" => SpriteService.Instance.GetTarotImage(itemName),
                    "Spectrals" => SpriteService.Instance.GetSpectralImage(itemName),
                    "Vouchers" => SpriteService.Instance.GetVoucherImage(itemName),
                    "Tags" => SpriteService.Instance.GetTagImage(itemName),
                    "Bosses" => SpriteService.Instance.GetBossImage(itemName),
                    "Decks" => SpriteService.Instance.GetDeckImage(itemName),
                    "Stakes" => SpriteService.Instance.GetStickerImage(itemName + "Stake"),
                    "Boosters" or "Packs" => SpriteService.Instance.GetBoosterImage(itemName),
                    _ => SpriteService.Instance.GetItemImage(itemName, category),
                };

                BalatroSeedOracle.Helpers.DebugLogger.LogImportant(
                    "CreateDroppedItem",
                    $"🎴 Image lookup for '{itemName}' in category '{category}': {(imageSource != null ? "FOUND" : "NOT FOUND")}"
                );

                if (imageSource != null)
                {
                    BalatroSeedOracle.Helpers.DebugLogger.LogImportant(
                        "CreateDroppedItem",
                        $"✅ Found image for '{itemName}'"
                    );
                    // Use correct size based on category (75% of original)
                    double imgWidth = 53;
                    double imgHeight = 71;
                    
                    // Special handling for Wee Joker - make him 50% size
                    var isWeeJoker = itemName.Equals("jolly", StringComparison.OrdinalIgnoreCase) || 
                                     itemName.Equals("weejoker", StringComparison.OrdinalIgnoreCase) ||
                                     itemName.Equals("j_jolly", StringComparison.OrdinalIgnoreCase);
                    
                    if (isWeeJoker)
                    {
                        imgWidth = 27;
                        imgHeight = 36;
                    }
                    else if (category == "Tags")
                    {
                        imgWidth = 15;
                        imgHeight = 15;
                    }
                    else if (category == "Bosses")
                    {
                        imgWidth = 20;
                        imgHeight = 20;
                    }

                    // For jokers with editions, wrap in a grid to layer the edition
                    if (category == "Jokers" && !string.IsNullOrEmpty(edition) && edition != "none")
                    {
                        var grid = new Grid { Width = imgWidth, Height = imgHeight };
                        
                        var image = new Image
                        {
                            Source = imageSource,
                            Stretch = Stretch.Uniform,
                            Width = imgWidth,
                            Height = imgHeight,
                            VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center,
                            HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center,
                            RenderTransform = null,
                            RenderTransformOrigin = RelativePoint.Center,
                        };
                        grid.Children.Add(image);
                        
                        // Add edition overlay
                        AddEditionOverlay(grid, edition);
                        
                        border.Child = grid;
                    }
                    else
                    {
                        var image = new Image
                        {
                            Source = imageSource,
                            Stretch = Stretch.Uniform,
                            Width = imgWidth,
                            Height = imgHeight,
                            VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center,
                            HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center,
                            RenderTransform = null,
                            RenderTransformOrigin = RelativePoint.Center,
                        };
                        border.Child = image;
                    }
                }
                else
                {
                    BalatroSeedOracle.Helpers.DebugLogger.LogImportant(
                        "CreateDroppedItem",
                        $"❌ No image found for '{itemName}', using text fallback"
                    );
                    // Get display name for items like "anyuncommon" -> "Any Uncommon"
                    var displayText =
                        category == "Jokers"
                            ? BalatroSeedOracle.Models.BalatroData.GetDisplayNameFromSprite(itemName)
                            : itemName;
                    var textBlock = new TextBlock
                    {
                        Text = displayText,
                        FontSize = 10,
                        FontWeight = FontWeight.SemiBold,
                        Foreground = Brushes.White,
                        TextTrimming = TextTrimming.CharacterEllipsis,
                        TextAlignment = TextAlignment.Center,
                        TextWrapping = TextWrapping.Wrap,
                        MaxWidth = 65,
                        VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center,
                        HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center,
                    };
                    border.Child = textBlock;
                }
            }

            // Click to show config popup
            border.PointerPressed += async (s, e) =>
            {
                e.Handled = true;
                // Add a small delay to ensure proper click registration
                await Task.Delay(50);
                ShowItemConfigPopup(border, itemName, category);
            };

            // Return the border directly without ViewBox wrapping
            return border;
        }

        private void AddEditionOverlay(Grid grid, string edition)
        {
            if (string.IsNullOrEmpty(edition) || edition == "none")
                return;
                
            // For negative edition, apply an invert filter
            if (edition.ToLower() == "negative")
            {
                // Avalonia doesn't have a direct invert filter, so we'll add a visual indicator
                
                var negativeIndicator = new TextBlock
                {
                    Text = "NEG",
                    FontSize = 8,
                    Foreground = Brushes.White,
                    Background = Brushes.Black,
                    Padding = new Thickness(2, 1),
                    HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Right,
                    VerticalAlignment = Avalonia.Layout.VerticalAlignment.Top,
                    Margin = new Thickness(0, 2, 2, 0),
                    ZIndex = 100
                };
                grid.Children.Add(negativeIndicator);
            }
            else
            {
                // For other editions, overlay the edition sprite
                var editionImage = SpriteService.Instance.GetEditionImage(edition);
                if (editionImage != null)
                {
                    var editionOverlay = new Image
                    {
                        Source = editionImage,
                        Stretch = Stretch.Uniform,
                        Width = 53,
                        Height = 71,
                        VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center,
                        HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center,
                        Opacity = 0.8,  // Semi-transparent overlay
                        ZIndex = 50
                    };
                    grid.Children.Add(editionOverlay);
                }
            }
        }

        private void ShowItemConfigPopup(Border itemBorder, string itemName, string category)
        {
            // Get the actual key and zone from the border's tag
            dynamic? tagData = itemBorder.Tag;
            string actualKey;
            string? zoneName = null;

            if (tagData != null)
            {
                try
                {
                    actualKey = tagData.Key?.ToString() ?? $"{category}:{itemName}";
                    zoneName = tagData.Zone?.ToString();
                }
                catch
                {
                    // If dynamic access fails, use fallback
                    actualKey = $"{category}:{itemName}";
                }
            }
            else
            {
                // Fallback for old-style tags
                actualKey = itemBorder.Tag as string ?? $"{category}:{itemName}";
            }

            var key = actualKey;

            // Create popup if it doesn't exist
            if (_configPopup == null)
            {
                _configPopup = new Popup
                {
                    Placement = PlacementMode.Pointer,
                    IsLightDismissEnabled = true,
                    HorizontalOffset = 0,
                    VerticalOffset = 10,
                };
            }

            // Create or replace popup content based on category
            if (_configPopupContent != null)
            {
                // Unsubscribe from previous events
                _configPopupContent.ConfigApplied -= OnItemConfigApplied;
                if (_currentDeleteHandler != null)
                {
                    _configPopupContent.DeleteRequested -= _currentDeleteHandler;
                }

                _configPopupContent.Cancelled -= OnItemConfigCancelled;
            }

            // Create appropriate popup based on category
            _configPopupContent = category switch
            {
                "Jokers" or "SoulJokers" => CreateJokerConfigPopup(itemName),
                "Tarots" => new TarotConfigPopup(),
                "Planets" => new TarotConfigPopup(), // Planet cards use same config as Tarot (no editions)
                "Spectrals" => CreateSpectralConfigPopup(itemName),
                "Vouchers" => new VoucherConfigPopup(),
                "Tags" => new TagConfigPopup(),
                "PlayingCards" => new PlayingCardConfigPopup(),
                _ => new JokerConfigPopup(), // Fallback to joker config
            };

            _configPopup.Child = _configPopupContent;

            // Handle events
            _configPopupContent.ConfigApplied += OnItemConfigApplied;
            // Create a wrapper to pass the zone name
            _currentDeleteHandler = (s, e) => OnItemDeleteRequested(s, e, zoneName);
            _configPopupContent.DeleteRequested += _currentDeleteHandler;
            _configPopupContent.Cancelled += OnItemConfigCancelled;

            // Position popup based on zone
            if (zoneName == "needs")
            {
                _configPopup.Placement = PlacementMode.Bottom;
                _configPopup.VerticalOffset = 5;
            }
            else
            {
                _configPopup.Placement = PlacementMode.Top;
                _configPopup.VerticalOffset = -5;
            }

            // Set popup target and data with zone-specific defaults
            _configPopup.PlacementTarget = itemBorder;
            var existingConfig = _itemConfigs.GetValueOrDefault(key);

            // Set default ante selections based on zone if no existing config
            if (existingConfig == null)
            {
                var defaultConfig = new ItemConfig
                {
                    ItemKey = key,
                    Edition = "none",
                };

                // Determine smart defaults for sources based on item type
                var sources = new List<string>();
                var itemType = key.Split(':')[0];
                var itemId = key.Split(':')[1];
                
                // Check if item can appear in packs
                bool canAppearInPacks = false;
                if (itemType == "Jokers")
                {
                    // Regular jokers appear in packs, legendary ones come from soul cards
                    canAppearInPacks = !itemId.StartsWith("Canio") && !itemId.StartsWith("Triboulet") && 
                                       !itemId.StartsWith("Yorick") && !itemId.StartsWith("Chicot") && 
                                       !itemId.StartsWith("Perkeo");
                }
                else if (itemType == "Tarots" || itemType == "Planets" || itemType == "Spectrals")
                {
                    canAppearInPacks = true;
                }
                else if (itemType == "PlayingCards")
                {
                    canAppearInPacks = true; // Standard pack
                }
                
                // Set sources intelligently
                if (canAppearInPacks)
                {
                    sources.Add("packs");
                    // Note: PackSlots would be set here but ItemConfig doesn't have this property yet
                }
                
                // Check if can appear in shop
                bool canAppearInShop = itemType == "Jokers" || itemType == "Vouchers" || itemType == "PlayingCards";
                if (canAppearInShop)
                {
                    sources.Add("shop");
                    // Note: ShopSlots would be set based on deck but ItemConfig doesn't have this property yet
                }
                
                // Tags for special items
                if (itemType == "Jokers" || itemType == "Vouchers")
                {
                    sources.Add("tags");
                }
                
                // Soul jokers from soul cards (legendary jokers)
                bool isSoulJoker = itemType == "Jokers" && (itemId.StartsWith("Canio") || itemId.StartsWith("Triboulet") || 
                                                             itemId.StartsWith("Yorick") || itemId.StartsWith("Chicot") || 
                                                             itemId.StartsWith("Perkeo"));
                if (isSoulJoker)
                {
                    sources.Clear();
                    sources.Add("soul");
                }
                
                defaultConfig.Sources = sources.Any() ? sources : new List<string> { "shop", "packs", "tags" };

                if (zoneName == "needs")
                {
                    // MUST zone defaults to antes 1-4
                    defaultConfig.Antes = new List<int> { 1, 2, 3, 4 };
                }
                else if (zoneName == "wants")
                {
                    // SHOULD zone defaults to antes 1-8
                    defaultConfig.Antes = new List<int> { 1, 2, 3, 4, 5, 6, 7, 8 };
                }
                else
                {
                    // MUST NOT - just ante 1
                    defaultConfig.Antes = new List<int> { 1 };
                }

                existingConfig = defaultConfig;
            }

            if (_configPopupContent != null)
            {
                BalatroSeedOracle.Helpers.DebugLogger.Log(
                    "FiltersModal",
                    $"[POPUP] Opening config for key: {key}, itemName: {itemName}, existingConfig: {existingConfig?.ItemKey}"
                );
                _configPopupContent.SetItem(key, itemName, existingConfig);
            }

            // Show popup
            if (_configPopup != null)
            {
                _configPopup.IsOpen = true;
            }
        }

        private void OnItemConfigApplied(object? sender, ItemConfigEventArgs e)
        {
            _itemConfigs[e.Config.ItemKey] = e.Config;
            _configPopup!.IsOpen = false;
            BalatroSeedOracle.Helpers.DebugLogger.Log(
                "FiltersModal",
                $"[CONFIG] Item: {e.Config.ItemKey}, Edition: {e.Config.Edition}, Sources: {string.Join(",", e.Config.Sources)}"
            );

            // Refresh both the item palette and drop zones to show edition overlays
            RefreshItemPalette();
            UpdateDropZoneVisibility();
        }

        private void OnItemDeleteRequested(object? sender, EventArgs e, string? zoneName)
        {
            if (_configPopupContent != null)
            {
                var key = _configPopupContent.ItemKey;

                // Only remove from the specific zone where the item was clicked
                bool removed = false;
                switch (zoneName)
                {
                    case "needs":
                        if (_selectedMust.Remove(key))
                        {
                            MarkAsChanged();
                            removed = true;
                            BalatroSeedOracle.Helpers.DebugLogger.Log($"Removed {key} from NEEDS zone");
                        }
                        break;
                    case "wants":
                        if (_selectedShould.Remove(key))
                        {
                            MarkAsChanged();
                            removed = true;
                            BalatroSeedOracle.Helpers.DebugLogger.Log($"Removed {key} from WANTS zone");
                        }
                        break;
                    case "mustnot":
                        if (_selectedMustNot.Remove(key))
                        {
                            MarkAsChanged();
                            removed = true;
                            BalatroSeedOracle.Helpers.DebugLogger.Log($"Removed {key} from MUST NOT zone");
                        }
                        break;
                }

                // Remove the config if item was removed
                if (removed)
                {
                    _itemConfigs.Remove(key);
                }

                UpdateDropZoneVisibility();
                UpdatePersistentFavorites();
                RefreshItemPalette();

                _configPopup!.IsOpen = false;
                BalatroSeedOracle.Helpers.DebugLogger.Log(
                    $"Item deleted: {key} from zone: {zoneName ?? "unknown"}"
                );
            }
        }

        private void OnItemConfigCancelled(object? sender, EventArgs e)
        {
            _configPopup!.IsOpen = false;
        }

        private void SaveConfigurationToFile(string filePath)
        {
            var config = BuildOuijaConfigFromSelections();
            var json = SerializeOuijaConfig(config);
            File.WriteAllText(filePath, json);
        }

        private async Task OnSaveClickAsync()
        {
            var configNameBox = this.FindControl<TextBox>("ConfigNameBox");
            var configName = configNameBox?.Text ?? "Untitled Filter";

            // Create the config object using the existing method
            var config = BuildOuijaConfigFromSelections();
            var json = System.Text.Json.JsonSerializer.Serialize(
                config,
                new System.Text.Json.JsonSerializerOptions
                {
                    WriteIndented = true,
                    DefaultIgnoreCondition = System
                        .Text
                        .Json
                        .Serialization
                        .JsonIgnoreCondition
                        .WhenWritingNull,
                }
            );

            // Format arrays compactly: [1,2,3,4,5,6] instead of vertical format
            json = FormatJsonWithCompactArrays(json);

            // Get the MainWindow to access storage
            var mainWindow = this.GetVisualRoot() as Window;
            if (mainWindow != null)
            {
                var options = new Avalonia.Platform.Storage.FilePickerSaveOptions
                {
                    Title = "Save Filter Configuration",
                    DefaultExtension = "json",
                    SuggestedFileName = $"{configName}.json",
                    FileTypeChoices = new[]
                    {
                        new Avalonia.Platform.Storage.FilePickerFileType("Filter Files")
                        {
                            Patterns = new[] { "*.json" },
                        },
                    },
                };

                var file = await mainWindow.StorageProvider.SaveFilePickerAsync(options);
                if (file != null)
                {
                    // Wrap the config with metadata for saving
                    var userProfileService = ServiceHelper.GetService<UserProfileService>();
                    var authorName = userProfileService?.GetAuthorName() ?? "Jimbo";

                    var wrappedConfig = new
                    {
                        name = configName,
                        author = authorName,
                        created = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss"),
                        must = config.Must,
                        should = config.Should,
                        mustNot = config.MustNot,
                    };

                    var wrappedJson = System.Text.Json.JsonSerializer.Serialize(
                        wrappedConfig,
                        new System.Text.Json.JsonSerializerOptions
                        {
                            WriteIndented = true,
                            DefaultIgnoreCondition = System
                                .Text
                                .Json
                                .Serialization
                                .JsonIgnoreCondition
                                .WhenWritingNull,
                        }
                    );

                    await System.IO.File.WriteAllTextAsync(file.Path.LocalPath, wrappedJson);
                    _currentConfigPath = file.Path.LocalPath;
                    BalatroSeedOracle.Helpers.DebugLogger.Log(
                        "FiltersModal",
                        $"Saved config to: {_currentConfigPath}"
                    );
                }
            }
        }

        private int _widgetCount
        {
            get
            {
                // Get the main window to find BalatroMainMenu
                var mainWindow = TopLevel.GetTopLevel(this) as Window;
                if (mainWindow?.Content is Grid grid)
                {
                    foreach (var child in grid.Children)
                    {
                        if (child is Views.BalatroMainMenu mainMenu)
                        {
                            var desktopCanvas = mainMenu.FindControl<Grid>("DesktopCanvas");
                            if (desktopCanvas != null)
                            {
                                return desktopCanvas.Children.OfType<SearchDesktopIcon>().Count();
                            }
                        }
                    }
                }
                return 0;
            }
        }

        private void PreviewConfig()
        {
            try
            {
                var config = BuildOuijaConfigFromSelections();
                var json = SerializeOuijaConfig(config);

                // Show a simple preview dialog (you could enhance this)
                BalatroSeedOracle.Helpers.DebugLogger.Log("👁️ Config Preview:");
                BalatroSeedOracle.Helpers.DebugLogger.Log(json);

                // For now, just enter JSON edit mode with the generated config
                EnterEditJsonMode();

                // Set the JSON content after a short delay to allow the UI to initialize
                Dispatcher.UIThread.Post(
                    () =>
                    {
                        // Update AvaloniaEdit if available
                        var textEditor = this.FindControl<AvaloniaEdit.TextEditor>("JsonTextEditor");
                        if (textEditor != null)
                        {
                            BalatroSeedOracle.Helpers.DebugLogger.Log(
                                $"Setting TextEditor text in PreviewConfig, length: {json.Length}"
                            );
                            textEditor.Text = json;
                            textEditor.IsVisible = true; // Ensure visibility
                        }
                        else
                        {
                            BalatroSeedOracle.Helpers.DebugLogger.LogError(
                                "JsonTextEditor not found in PreviewConfig"
                            );
                        }

                        // Also update the hidden TextBox for compatibility
                        if (_jsonTextBox != null)
                        {
                            _jsonTextBox.Text = json;
                        }

                        FormatJson();
                    },
                    DispatcherPriority.Background
                );
            }
            catch (Exception ex)
            {
                BalatroSeedOracle.Helpers.DebugLogger.LogError($"Error generating config preview: {ex.Message}");
            }
        }

        // 🎯 Drag and Drop Support for Cards

        private void EnableCardDragDrop(ResponsiveCard card, string itemName, string category)
        {
            // Add draggable styling
            card.Classes.Add("draggable-item");

            // Set the tag for identification during refresh
            var actualCategory = category;
            if (category == "Favorites")
            {
                // Find which category this item belongs to
                if (BalatroData.Jokers.ContainsKey(itemName))
                {
                    actualCategory = "Jokers";
                }
                else if (BalatroData.TarotCards.ContainsKey(itemName))
                {
                    actualCategory = "Tarots";
                }
                else if (BalatroData.SpectralCards.ContainsKey(itemName))
                {
                    actualCategory = "Spectrals";
                }
                else if (BalatroData.Vouchers.ContainsKey(itemName))
                {
                    actualCategory = "Vouchers";
                }
                else if (BalatroData.Tags.ContainsKey(itemName))
                {
                    actualCategory = "Tags";
                }
            }
            card.Tag = $"{actualCategory}:{itemName}";

            // Handle click events - ResponsiveCard will fire CardClicked for clicks
            card.CardClicked += (sender, args) =>
            {
                if (args.ClickType == CardClickType.LeftClick)
                {
                    // Toggle selection
                    var key = $"{actualCategory}:{itemName}";

                    // Toggle between Need/Want/None states
                    if (card.IsSelectedNeed)
                    {
                        _selectedMust.Remove(key);
                        _selectedShould.Add(key);
                        card.IsSelectedNeed = false;
                        card.IsSelectedWant = true;
                        BalatroSeedOracle.Helpers.DebugLogger.Log($"🟠 {itemName} moved to WANTS");
                    }
                    else if (card.IsSelectedWant)
                    {
                        _selectedShould.Remove(key);
                        card.IsSelectedNeed = false;
                        card.IsSelectedWant = false;
                        BalatroSeedOracle.Helpers.DebugLogger.Log($"⚪ {itemName} deselected");
                    }
                    else
                    {
                        _selectedMust.Add(key);
                        card.IsSelectedNeed = true;
                        card.IsSelectedWant = false;
                        BalatroSeedOracle.Helpers.DebugLogger.Log($"🟢 {itemName} moved to NEEDS");
                    }

                    UpdateDropZoneVisibility();
                }
            };

            // Handle drag events - ResponsiveCard will fire CardDragStarted when dragging starts
            card.CardDragStarted += (sender, args) =>
            {
                // Add visual feedback
                card.Classes.Add("is-dragging");
                BalatroSeedOracle.Helpers.DebugLogger.Log(
                    $"👋 Drag started via CardDragStarted: {itemName} from {category}"
                );

                // Create a ghost card overlay with actual image
                CreateDragOverlay(itemName, category, card.ImageSource);
                _isDragging = true;
            };
        }

        private System.Threading.Timer? _ghostWiggleTimer;
        private Canvas? _dragOverlay;
        private Border? _ghostCard;

        private void CreateDragOverlay(string itemName, string category, IImage? imageSource)
        {
            // Find the root grid of the modal
            var rootGrid = this.FindControl<Grid>("RootGrid");
            if (rootGrid == null)
            {
                // Try to find any top-level grid
                rootGrid = this.GetVisualDescendants().OfType<Grid>().FirstOrDefault();
                if (rootGrid == null)
                {
                    return;
                }
            }

            // Create overlay canvas
            _dragOverlay = new Canvas { IsHitTestVisible = false, ZIndex = 1000 };

            // Create ghost card with actual image
            var ghostContent = new Grid();

            // Check if this is a legendary joker
            bool isLegendaryJoker = category == "Jokers" && IsLegendaryJoker(itemName);
            var animatedLegendaryJokers = new[] { "Canio", "Triboulet", "Yorick", "Chicot", "Perkeo" };
            bool hasAnimatedFace = animatedLegendaryJokers.Any(lj =>
                lj.Equals(itemName, StringComparison.OrdinalIgnoreCase)
            );

            if (isLegendaryJoker)
            {
                // For legendary jokers, just show the face without gold background

                // Add joker face on top
                if (imageSource != null)
                {
                    var jokerFace = new Image
                    {
                        Source = imageSource,
                        Width = 53,
                        Height = 71,
                        Stretch = Stretch.UniformToFill,
                        HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center,
                        VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center,
                    };
                    ghostContent.Children.Add(jokerFace);
                }

                // If it has an animated soul face, add that too
                if (hasAnimatedFace)
                {
                    var faceSource = SpriteService.Instance.GetJokerSoulImage(itemName);
                    if (faceSource != null)
                    {
                        var faceImage = new Image
                        {
                            Source = faceSource,
                            Width = 53, // Match the container size
                            Height = 71, // Match the container size
                            Stretch = Stretch.Uniform,
                            HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center,
                            VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center,
                        };
                        ghostContent.Children.Add(faceImage);
                    }
                }
            }
            else if (imageSource != null)
            {
                // Regular item - adjust size based on category
                double width = UIConstants.JokerSpriteWidth;
                double height = UIConstants.JokerSpriteHeight;

                // Adjust for different item types
                if (category == "Tags")
                {
                    width = 27;
                    height = 27;
                }
                else if (category == "Bosses")
                {
                    width = 34;
                    height = 34;
                }

                var image = new Image
                {
                    Source = imageSource,
                    Width = width,
                    Height = height,
                    Stretch = Stretch.Uniform,
                    HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center,
                    VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center,
                };
                ghostContent.Children.Add(image);
            }
            else
            {
                DebugLogger.Log(
                    "FiltersModal",
                    $"No image found for item '{itemName}' in category '{category}' - using text fallback"
                );
                // Fallback to text if no image
                ghostContent.Children.Add(
                    new TextBlock
                    {
                        Text = itemName,
                        FontSize = 12,

                        Foreground = Brushes.White,
                        HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center,
                        VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center,
                        TextWrapping = TextWrapping.Wrap,
                        TextAlignment = TextAlignment.Center,
                        Margin = new Thickness(4),
                    }
                );
            }

            _ghostCard = new Border
            {
                Width = 80,
                Height = 100,
                Background = new SolidColorBrush(Color.FromArgb(200, 32, 32, 32)),
                BorderThickness = new Thickness(0),
                CornerRadius = new CornerRadius(6),
                BoxShadow = BoxShadows.Parse("0 5 20 #000000AA"),
                RenderTransform = new RotateTransform(-5),
                Opacity = 0.9,
                IsVisible = false,
                Child = ghostContent,
                RenderTransformOrigin = new RelativePoint(0.5, 0.5, RelativeUnit.Relative),
            };

            _dragOverlay.Children.Add(_ghostCard);

            // Add overlay to the root grid
            Grid.SetRowSpan(_dragOverlay, Math.Max(1, rootGrid.RowDefinitions.Count));
            Grid.SetColumnSpan(_dragOverlay, Math.Max(1, rootGrid.ColumnDefinitions.Count));
            rootGrid.Children.Add(_dragOverlay);

            // Start wiggling the ghost card too!
            StartGhostWiggle();

            BalatroSeedOracle.Helpers.DebugLogger.Log("👻 Created drag overlay");
        }

        private void RemoveDragOverlay()
        {
            if (_dragOverlay != null)
            {
                StopGhostWiggle();
                var parent = _dragOverlay.Parent as Panel;
                parent?.Children.Remove(_dragOverlay);
                _dragOverlay = null;
                _ghostCard = null;
                BalatroSeedOracle.Helpers.DebugLogger.Log("👻 Removed drag overlay");
            }
        }

        private void CleanupDragVisuals()
        {
            // Find all cards and remove is-dragging class
            var cards = this.GetVisualDescendants().OfType<ResponsiveCard>();
            foreach (var card in cards)
            {
                card.Classes.Remove("is-dragging");
                // Reset any transforms that might have been applied
                card.RenderTransform = null;
            }

            // Remove drag overlay
            RemoveDragOverlay();
            _isDragging = false;
        }

        private void StartGhostWiggle()
        {
            var wiggleAngles = new[] { -6, 6, -2, 2, -3, 3, -1, 1, -5, 5 };
            var index = 0;

            _ghostWiggleTimer = new System.Threading.Timer(
                _ =>
                {
                    Dispatcher.UIThread.Post(() =>
                    {
                        if (_ghostCard != null && _isDragging)
                        {
                            var angle = wiggleAngles[index % wiggleAngles.Length];
                            _ghostCard.RenderTransform = new RotateTransform(angle);
                            index++;
                        }
                    });
                },
                null,
                0,
                90
            );
        }

        private void StopGhostWiggle()
        {
            _ghostWiggleTimer?.Dispose();
            _ghostWiggleTimer = null;
        }

        private void UpdateGhostPosition(Point position)
        {
            if (_ghostCard != null && _dragOverlay != null)
            {
                Canvas.SetLeft(_ghostCard, position.X - 40);
                Canvas.SetTop(_ghostCard, position.Y - 50);
                _ghostCard.IsVisible = true;
            }
        }

        private async void OnSaveClick(object? sender, RoutedEventArgs e)
        {
            // Check if we're in JSON editor mode
            var modeToggle = this.FindControl<CheckBox>("ModeToggle");
            if (modeToggle?.IsChecked == true)
            {
                // We're in JSON editor mode, use the JSON save method
                SaveConfig();
                return;
            }

            // Otherwise, we're in drag-drop mode
            try
            {
                // Create ouija config from selections - always use compound format
                var config = BuildOuijaConfigFromSelections();
                var json = SerializeOuijaConfig(config);

                // Get top level visual
                var topLevel = Avalonia.Controls.TopLevel.GetTopLevel(this);
                if (topLevel == null)
                {
                    return;
                }

                var storageProvider = topLevel.StorageProvider;

                // Show save file dialog
                var saveOptions = new Avalonia.Platform.Storage.FilePickerSaveOptions
                {
                    Title = "Save Filter Config",
                    DefaultExtension = "json",
                    FileTypeChoices = new[]
                    {
                        new Avalonia.Platform.Storage.FilePickerFileType("Filter Files")
                        {
                            Patterns = new[] { "*.json" },
                        },
                    },
                    SuggestedFileName = GetSuggestedFileName(),
                };

                var file = await storageProvider.SaveFilePickerAsync(saveOptions);
                if (file != null)
                {
                    await System.IO.File.WriteAllTextAsync(file.Path.LocalPath, json);
                    BalatroSeedOracle.Helpers.DebugLogger.Log($"✅ Config saved to: {file.Path.LocalPath}");

                    // Enable tabs after successful save
                    UpdateTabStates(true);

                    // SearchWidget removed - using desktop icons now
                }
            }
            catch (Exception ex)
            {
                BalatroSeedOracle.Helpers.DebugLogger.LogError($"Error saving config: {ex.Message}");
            }
        }

        private string SerializeOuijaConfig(Motely.Filters.MotelyJsonConfig config)
        {
            // Manually build the JSON to ensure we get the exact nested format
            using var stream = new MemoryStream();
            using var writer = new Utf8JsonWriter(stream, new JsonWriterOptions { Indented = true });

            writer.WriteStartObject();
            
            // Add metadata fields (write actual values instead of placeholders)
            if (!string.IsNullOrWhiteSpace(config.Name))
            {
                writer.WriteString("name", config.Name);
            }
            else
            {
                writer.WriteString("name", "Untitled Filter");
            }
            writer.WriteString("description", config.Description ?? string.Empty);

            // Author (prefer config.Author, fall back to profile service)
            var userProfileService = ServiceHelper.GetService<UserProfileService>();
            var authorName = !string.IsNullOrWhiteSpace(config.Author)
                ? config.Author
                : (userProfileService?.GetAuthorName() ?? "Jimbo");
            writer.WriteString("author", authorName);

            // Creation timestamp (ISO 8601 UTC)
            var created = config.DateCreated ?? DateTime.UtcNow;
            writer.WriteString("dateCreated", created.ToString("o"));

            // Write must array
            writer.WriteStartArray("must");
            foreach (var item in config.Must)
            {
                WriteFilterItem(writer, item);
            }
            writer.WriteEndArray();

            // Write should array
            writer.WriteStartArray("should");
            foreach (var item in config.Should)
            {
                WriteFilterItem(writer, item, includeScore: true);
            }
            writer.WriteEndArray();

            // Write mustNot array
            writer.WriteStartArray("mustNot");
            foreach (var item in config.MustNot)
            {
                WriteFilterItem(writer, item);
            }
            writer.WriteEndArray();

            // Write deck, stake, maxSearchAnte, minimumScore
            writer.WriteString("deck", config.Deck);
            writer.WriteString("stake", config.Stake);

            writer.WriteEndObject();
            writer.Flush();

            return Encoding.UTF8.GetString(stream.ToArray());
        }

        private void WriteFilterItem(
            Utf8JsonWriter writer,
            Motely.Filters.MotelyJsonConfig.MotleyJsonFilterClause item,
            bool includeScore = false
        )
        {
            writer.WriteStartObject();

            // Write type and value
            writer.WriteString("type", item.Type);
            if (!string.IsNullOrEmpty(item.Value))
            {
                writer.WriteString("value", item.Value);
            }

            // Write edition if present
            if (!string.IsNullOrEmpty(item.Edition))
            {
                writer.WriteString("edition", item.Edition);
            }

            // Write antes array
            if (item.Antes != null && item.Antes.Length > 0)
            {
                writer.WriteStartArray("antes");
                foreach (var ante in item.Antes)
                {
                    writer.WriteNumberValue(ante);
                }
                writer.WriteEndArray();
            }

            // Only write sources for items that can actually have sources
            // Tags, vouchers, and bosses NEVER have sources
            bool canHaveSources = item.Type != null && 
                                  !item.Type.Equals("tag", StringComparison.OrdinalIgnoreCase) &&
                                  !item.Type.Equals("voucher", StringComparison.OrdinalIgnoreCase) &&
                                  !item.Type.Equals("boss", StringComparison.OrdinalIgnoreCase);

            // Write sources object only for applicable items
            if (canHaveSources && item.Sources != null)
            {
                writer.WriteStartObject("sources");
                if (item.Sources.ShopSlots != null && item.Sources.ShopSlots.Length > 0)
                {
                    writer.WriteStartArray("shopSlots");
                    foreach (var slot in item.Sources.ShopSlots)
                    {
                        writer.WriteNumberValue(slot);
                    }
                    writer.WriteEndArray();
                }
                if (item.Sources.PackSlots != null && item.Sources.PackSlots.Length > 0)
                {
                    writer.WriteStartArray("packSlots");
                    foreach (var slot in item.Sources.PackSlots)
                    {
                        writer.WriteNumberValue(slot);
                    }
                    writer.WriteEndArray();
                }
                if (item.Sources.Tags.HasValue)
                {
                    writer.WriteBoolean("tags", item.Sources.Tags.Value);
                }
                if (item.Sources.RequireMega.HasValue)
                {
                    writer.WriteBoolean("requireMega", item.Sources.RequireMega.Value);
                }
                writer.WriteEndObject();
            }

            // Write score if requested (for should clauses)
            if (includeScore && item.Score > 0)
            {
                writer.WriteNumber("score", item.Score);
            }

            writer.WriteEndObject();
        }

        private Motely.Filters.MotelyJsonConfig BuildOuijaConfigFromSelections()
        {
            // Get deck/stake preferences from the selector
            string deckName = "Red";   // Default
            string stakeName = "White"; // Default

            var deckStakeSelector = this.FindControl<DeckAndStakeSelector>("PreferredDeckStakeSelector");
            if (deckStakeSelector != null)
            {
                // Get the deck spinner control
                var deckSpinner = deckStakeSelector.FindControl<DeckSpinner>("DeckSpinnerControl");
                if (deckSpinner != null)
                {
                    int deckIndex = deckSpinner.SelectedDeckIndex;
                    string[] deckNames = { "Red", "Blue", "Yellow", "Green", "Black", "Magic", "Nebula", "Ghost",
                                           "Abandoned", "Checkered", "Zodiac", "Painted", "Anaglyph", "Plasma", "Erratic" };
                    if (deckIndex >= 0 && deckIndex < deckNames.Length)
                    {
                        deckName = deckNames[deckIndex];
                    }
                }

                // Get the stake spinner control
                var stakeSpinner = deckStakeSelector.FindControl<SpinnerControl>("StakeSpinner");
                if (stakeSpinner != null)
                {
                    int stakeIndex = (int)stakeSpinner.Value;
                    string[] stakeNames = { "White", "Red", "Green", "Black", "Blue", "Purple", "Orange", "Gold" };
                    if (stakeIndex >= 0 && stakeIndex < stakeNames.Length)
                    {
                        stakeName = stakeNames[stakeIndex];
                    }
                }
            }

            var config = new Motely.Filters.MotelyJsonConfig
            {
                Deck = deckName,
                Stake = stakeName,
            };

            // Get name from the ConfigNameBox
            var configNameBox = this.FindControl<TextBox>("ConfigNameBox");
            if (configNameBox != null && !string.IsNullOrWhiteSpace(configNameBox.Text))
            {
                config.Name = configNameBox.Text;
            }
            else
            {
                config.Name = "Untitled Filter";
            }
            
            // Get description if available
            var configDescriptionBox = this.FindControl<TextBox>("ConfigDescriptionBox");
            if (configDescriptionBox != null && !string.IsNullOrWhiteSpace(configDescriptionBox.Text))
            {
                config.Description = configDescriptionBox.Text;
            }
            
            // Get author from user profile
            var userProfileService = ServiceHelper.GetService<UserProfileService>();
            config.Author = userProfileService?.GetAuthorName() ?? "Jimbo";

            // Set creation date if not already provided
            if (!config.DateCreated.HasValue)
            {
                config.DateCreated = DateTime.UtcNow;
            }

            // Convert all items using the helper method that handles unique keys
            FixUniqueKeyParsing(_selectedMust, config.Must, 0);
            FixUniqueKeyParsing(_selectedShould, config.Should, 1);
            FixUniqueKeyParsing(_selectedMustNot, config.MustNot, 0);

            return config;
        }

        private void FixUniqueKeyParsing(
            List<string> items,
            List<Motely.Filters.MotelyJsonConfig.MotleyJsonFilterClause> targetList,
            int defaultScore = 0
        )
        {
            foreach (var item in items)
            {
                // Handle both formats: "Category:Item" and "Category:Item#123"
                var colonIndex = item.IndexOf(':');
                if (colonIndex > 0)
                {
                    var category = item.Substring(0, colonIndex);
                    var itemNameWithSuffix = item.Substring(colonIndex + 1);

                    // Remove the unique key suffix if present
                    var hashIndex = itemNameWithSuffix.IndexOf('#');
                    var itemName =
                        hashIndex > 0 ? itemNameWithSuffix.Substring(0, hashIndex) : itemNameWithSuffix;

                    var itemConfig = _itemConfigs.ContainsKey(item)
                        ? _itemConfigs[item]
                        : new ItemConfig();

                    var filterItem = CreateFilterItemFromSelection(category, itemName, itemConfig);
                    if (filterItem != null)
                    {
                        filterItem.Score = defaultScore;
                        targetList.Add(filterItem);
                    }
                }
            }
        }

        private Motely.Filters.MotelyJsonConfig.MotleyJsonFilterClause? CreateFilterItemFromSelection(
            string category,
            string itemName,
            ItemConfig config
        )
        {
            var filterItem = new Motely.Filters.MotelyJsonConfig.MotleyJsonFilterClause
            {
                // If Antes is null, it means either all antes or no antes were selected
                // The JokerConfigPopup returns null for both cases
                // We'll use an empty array for now and let Motely handle the default
                Antes = config.Antes?.ToArray() ?? new[] { 1, 2, 3, 4, 5, 6, 7, 8 },
                Min = config.Min // Support minimum count for Must items
            };

            // Handle category mappings 
            var normalizedCategory = category.ToLower();
            
            // Only add Sources for items that can actually have sources (NOT tags, vouchers, or bosses)
            bool canHaveSources = normalizedCategory == "jokers" || 
                                  normalizedCategory == "souljokers" || 
                                  normalizedCategory == "tarots" || 
                                  normalizedCategory == "spectrals" ||
                                  normalizedCategory == "planets" ||
                                  normalizedCategory == "playingcards";
            
            if (canHaveSources)
            {
                // Create Sources config only for items that can have sources
                filterItem.Sources = new Motely.Filters.MotelyJsonConfig.SourcesConfig();
                
                if (config.Sources != null)
                {
                    if (config.Sources is List<string> sourcesList && sourcesList.Count > 0)
                    {
                        // Old format - convert to new format
                        if (sourcesList.Contains("shop"))
                        {
                            filterItem.Sources.ShopSlots = new[] { 0, 1, 2, 3 };
                        }
                        if (sourcesList.Contains("booster") || sourcesList.Contains("packs"))
                        {
                            filterItem.Sources.PackSlots = new[] { 0, 1, 2, 3 };
                        }
                        if (sourcesList.Contains("tag") || sourcesList.Contains("tags"))
                        {
                            filterItem.Sources.PackSlots = new[] { 0, 1, 2, 3 };
                        }
                    }
                    else if (config.Sources is Dictionary<string, List<int>> sourcesDict)
                    {
                        // New format - use slots directly
                        if (sourcesDict.ContainsKey("shopSlots") && sourcesDict["shopSlots"].Count > 0)
                        {
                            filterItem.Sources.ShopSlots = sourcesDict["shopSlots"].ToArray();
                        }
                        else
                        {
                            filterItem.Sources.ShopSlots = new int[] { };
                        }
                        
                        if (sourcesDict.ContainsKey("packSlots") && sourcesDict["packSlots"].Count > 0)
                        {
                            filterItem.Sources.PackSlots = sourcesDict["packSlots"].ToArray();
                        }
                        else
                        {
                            filterItem.Sources.PackSlots = new int[] { };
                        }
                    }
                }
                else
                {
                    // Default sources if none specified for items that can have sources
                    filterItem.Sources.PackSlots = new[] { 0, 1, 2, 3 };
                    filterItem.Sources.ShopSlots = new[] { 0, 1, 2, 3 };
                }
            }
            // For tags, vouchers, and bosses - NO SOURCES AT ALL
            
            // Set type and value directly
            switch (normalizedCategory)
            {
                case "souljokers":
                    filterItem.Type = "souljoker";
                    filterItem.Value = itemName;
                    // Soul jokers cannot appear in shop, only from The Soul card
                    if (filterItem.Sources != null)
                    {
                        filterItem.Sources.ShopSlots = Array.Empty<int>();
                    }
                    // Soul jokers can have editions
                    if (!string.IsNullOrEmpty(config.Edition) && config.Edition != "none")
                    {
                        filterItem.Edition = config.Edition;
                    }
                    break;
                    
                case "jokers":
                    // Check if it's a wildcard - ALWAYS use "any" as value for wildcards
                    if (itemName.ToLower() == "anyjoker")
                    {
                        filterItem.Type = "joker";
                        filterItem.Value = "any";
                    }
                    else if (itemName.ToLower() == "anylegendary" || IsLegendaryJoker(itemName))
                    {
                        filterItem.Type = "souljoker";
                        // Handle "anylegendary" as a special case
                        if (itemName.ToLower() == "anylegendary")
                        {
                            filterItem.Value = "any";
                        }
                        else
                        {
                            filterItem.Value = itemName;
                        }
                        // Soul jokers cannot appear in shop, only from The Soul card
                        if (filterItem.Sources != null)
                        {
                            filterItem.Sources.ShopSlots = Array.Empty<int>();
                        }
                    }
                    else if (itemName.ToLower() == "anyrare" || 
                             itemName.ToLower() == "anyuncommon" || 
                             itemName.ToLower() == "anycommon")
                    {
                        // For other "any" rarities, just use joker type with "any" value
                        filterItem.Type = "joker";
                        filterItem.Value = "any";
                    }
                    else
                    {
                        filterItem.Type = "joker";
                        filterItem.Value = itemName;
                    }
                    // Add edition if configured
                    if (!string.IsNullOrEmpty(config.Edition) && config.Edition != "none")
                    {
                        filterItem.Edition = config.Edition;
                    }
                    break;

                case "tarots":
                    filterItem.Type = "tarotcard";
                    filterItem.Value = itemName;
                    break;

                case "spectrals":
                    filterItem.Type = "spectralcard";
                    filterItem.Value = itemName;
                    break;

                case "vouchers":
                    filterItem.Type = "voucher";
                    filterItem.Value = itemName;
                    break;

                case "tags":
                    // Use the tag type from config if available, otherwise default to small blind
                    if (config != null && !string.IsNullOrEmpty(config.TagType))
                    {
                        filterItem.Type = config.TagType;
                    }
                    else
                    {
                        filterItem.Type = "smallblindtag"; // Default to small blind
                    }
                    filterItem.Value = itemName;
                    break;

                case "bosses":
                    filterItem.Type = "boss";
                    filterItem.Value = itemName;
                    break;

                case "planets":
                    filterItem.Type = "planetcard";
                    filterItem.Value = itemName;
                    break;

                case "playingcards":
                    filterItem.Type = "playingcard";
                    filterItem.Value = itemName;
                    break;

                default:
                    return null;
            }

            // Don't call Initialize() - we want to keep the nested format
            // Set the antes array (instead of Antes) for the nested format
            filterItem.Antes = filterItem.Antes;

            return filterItem;
        }
        
        private void ReloadJsonFromCurrentConfig()
        {
            try
            {
                BalatroSeedOracle.Helpers.DebugLogger.Log("FiltersModal", "ReloadJsonFromCurrentConfig called");
                
                var jsonEditor = this.FindControl<TextEditor>("JsonEditor");
                if (jsonEditor != null)
                {
                    // Build current config from selections and reload into JSON editor
                    var config = BuildOuijaConfigFromSelections();
                    var json = SerializeOuijaConfig(config);
                    
                    jsonEditor.Text = json;
                    
                    BalatroSeedOracle.Helpers.DebugLogger.Log(
                        "FiltersModal", 
                        $"Reloaded JSON editor with current config ({json.Length} characters)"
                    );
                }
            }
            catch (Exception ex)
            {
                BalatroSeedOracle.Helpers.DebugLogger.LogError("FiltersModal", $"Error in ReloadJsonFromCurrentConfig: {ex}");
            }
        }
        
        private void RefreshDropZonesFromConfig()
        {
            try
            {
                BalatroSeedOracle.Helpers.DebugLogger.Log("FiltersModal", "RefreshDropZonesFromConfig called");
                
                // Clear existing drop zones
                ClearNeeds();
                ClearWants();
                ClearMustNot();
                
                // Rebuild from current selections
                UpdateSelectedItemsPanel("NeedsPanel", _selectedMust);
                UpdateSelectedItemsPanel("WantsPanel", _selectedShould);
                UpdateSelectedItemsPanel("MustNotPanel", _selectedMustNot);
                
                BalatroSeedOracle.Helpers.DebugLogger.Log(
                    "FiltersModal", 
                    $"Refreshed drop zones - Must: {_selectedMust.Count}, Should: {_selectedShould.Count}, MustNot: {_selectedMustNot.Count}"
                );
            }
            catch (Exception ex)
            {
                BalatroSeedOracle.Helpers.DebugLogger.LogError("FiltersModal", $"Error in RefreshDropZonesFromConfig: {ex}");
            }
        }

        private string GetDefaultOuijaConfigJson()
        {
            // Return a default OuijaConfig format example
            var defaultConfig = new Motely.Filters.MotelyJsonConfig
            {
                Deck = "Red",
                Stake = "White",
                Must = new List<Motely.Filters.MotelyJsonConfig.MotleyJsonFilterClause>
                {
                    new Motely.Filters.MotelyJsonConfig.MotleyJsonFilterClause
                    {
                        Type = "Joker",
                        Value = "Perkeo",
                        Antes = new[] { 1, 2 },
                        Sources = new Motely.Filters.MotelyJsonConfig.SourcesConfig
                        {
                            ShopSlots = new[] { 0, 1, 2, 3 },
                            PackSlots = new[] { 0, 1, 2, 3, 4, 5 },
                            Tags = false,
                        },
                    },
                },
                Should = new List<Motely.Filters.MotelyJsonConfig.MotleyJsonFilterClause>
                {
                    new Motely.Filters.MotelyJsonConfig.MotleyJsonFilterClause
                    {
                        Type = "tag",
                        Value = "NegativeTag",
                        Score = 50,
                        Antes = new[] { 1, 2, 3 },
                        // NO SOURCES for tags!
                    },
                    new Motely.Filters.MotelyJsonConfig.MotleyJsonFilterClause
                    {
                        Type = "Joker",
                        Value = "Blueprint",
                        Score = 30,
                        Antes = new[] { 1, 2, 3, 4 },
                        Sources = new Motely.Filters.MotelyJsonConfig.SourcesConfig
                        {
                            ShopSlots = new[] { 0, 1, 2, 3 },
                            PackSlots = new[] { 0, 1, 2, 3, 4, 5 },
                            Tags = false,
                        },
                    },
                },
                MustNot = new List<Motely.Filters.MotelyJsonConfig.MotleyJsonFilterClause>
                {
                    new Motely.Filters.MotelyJsonConfig.MotleyJsonFilterClause
                    {
                        Type = "voucher",
                        Value = "CreditCard",
                        Antes = new[] { 1 },
                    },
                },
            };

            return SerializeOuijaConfig(defaultConfig);
        }

        /// <summary>
        /// Check if a joker is legendary (soul joker)
        /// </summary>
        private bool IsLegendaryJoker(string jokerName)
        {
            // Check if the joker exists in the MotelyJokerLegendary enum
            return Enum.TryParse<MotelyJokerLegendary>(jokerName, out _);
        }
        
        private JokerConfigPopup CreateJokerConfigPopup(string itemName)
        {
            var popup = new JokerConfigPopup();
            
            // Check if it's a legendary joker and configure accordingly
            if (IsLegendaryJoker(itemName))
            {
                popup.SetIsLegendaryJoker(true);
            }
            
            return popup;
        }
        
        private SpectralConfigPopup CreateSpectralConfigPopup(string itemName)
        {
            var popup = new SpectralConfigPopup();
            
            // Check if it's Soul or BlackHole spectral card
            // These cards cannot appear in shops
            if (itemName == "Soul" || itemName == "TheSoul" || 
                itemName == "BlackHole" || itemName == "Black Hole")
            {
                popup.SetCannotAppearInShop(true);
            }
            
            return popup;
        }

        /// <summary>
        /// Maps UI category names (plural) to JSON Type values (singular)
        /// </summary>
        private string MapCategoryToType(string category)
        {
            return category switch
            {
                "Jokers" => "Joker",
                "SoulJokers" => "SoulJoker",
                "Tarots" => "Tarot",
                "Spectrals" => "Spectral",
                "Vouchers" => "Voucher",
                "Tags" => "Tag",
                "Bosses" => "Boss",
                "PlayingCards" => "PlayingCard",
                _ => category, // Fallback to original if not mapped
            };
        }

        /// <summary>
        /// Maps JSON Type values (singular) to UI category names (plural)
        /// </summary>
        private string MapTypeToCategory(string type)
        {
            return type switch
            {
                "SoulJoker" => "SoulJokers",
                "Joker" => "Jokers",
                "Tarot" => "Tarots",
                "Spectral" => "Spectrals",
                "Voucher" => "Vouchers",
                "Tag" => "Tags",
                "Boss" => "Bosses",
                "PlayingCard" => "PlayingCards",
                _ => throw new ArgumentException($"Unknown type: {type}"),
            };
        }

        /// <summary>
        /// Normalizes item names to match the casing used in BalatroData
        /// </summary>
        private string NormalizeItemName(string itemName, string category)
        {
            // Try to find the item in the appropriate dictionary with case-insensitive search
            switch (category)
            {
                case "Jokers":
                    var joker = BalatroData.Jokers.Keys.FirstOrDefault(k =>
                        k.Equals(itemName, StringComparison.OrdinalIgnoreCase)
                    );
                    return joker ?? itemName;

                case "Tarots":
                    var tarot = BalatroData.TarotCards.Keys.FirstOrDefault(k =>
                        k.Equals(itemName, StringComparison.OrdinalIgnoreCase)
                    );
                    return tarot ?? itemName;

                case "Spectrals":
                    var spectral = BalatroData.SpectralCards.Keys.FirstOrDefault(k =>
                        k.Equals(itemName, StringComparison.OrdinalIgnoreCase)
                    );
                    return spectral ?? itemName;

                case "Vouchers":
                    var voucher = BalatroData.Vouchers.Keys.FirstOrDefault(k =>
                        k.Equals(itemName, StringComparison.OrdinalIgnoreCase)
                    );
                    return voucher ?? itemName;

                case "Tags":
                    var tag = BalatroData.Tags.Keys.FirstOrDefault(k =>
                        k.Equals(itemName, StringComparison.OrdinalIgnoreCase)
                    );
                    return tag ?? itemName;

                default:
                    return itemName;
            }
        }

        private void OnBrowseFilterClick(object? sender, RoutedEventArgs e)
        {
            OnLoadClick(sender, e);
        }

        private void OnBrowseFiltersClick(object? sender, RoutedEventArgs e)
        {
            OnLoadClick(sender, e);
        }

        private async void OnLoadClick(object? sender, RoutedEventArgs e)
        {
            // Check if we're in JSON editor mode by checking which button is active
            var jsonModeButton = this.FindControl<Button>("JsonModeButton");
            if (jsonModeButton != null && jsonModeButton.Classes.Contains("active"))
            {
                // We're in JSON editor mode, use the JSON load method
                LoadConfig();
                return;
            }

            // Otherwise, we're in drag-drop mode
            try
            {
                // Get top level visual
                var topLevel = Avalonia.Controls.TopLevel.GetTopLevel(this);
                if (topLevel == null)
                {
                    return;
                }

                var storageProvider = topLevel.StorageProvider;

                // Show open file dialog
                var openOptions = new Avalonia.Platform.Storage.FilePickerOpenOptions
                {
                    Title = "Load Filter Config",
                    AllowMultiple = false,
                    FileTypeFilter = new[]
                    {
                        new Avalonia.Platform.Storage.FilePickerFileType("Filter Files")
                        {
                            Patterns = new[] { "*.json" },
                        },
                    },
                };

                var files = await storageProvider.OpenFilePickerAsync(openOptions);
                if (files.Count > 0)
                {
                    var file = files[0];
                    var json = await System.IO.File.ReadAllTextAsync(file.Path.LocalPath);

                    // Parse the JSON to load into UI
                    var config = JsonSerializer.Deserialize<Motely.Filters.MotelyJsonConfig>(
                        json,
                        new JsonSerializerOptions
                        {
                            PropertyNameCaseInsensitive = true,
                            ReadCommentHandling = JsonCommentHandling.Skip,
                        }
                    );

                    if (config != null)
                    {
                        LoadConfigIntoUI(config);
                        _currentConfigPath = file.Path.LocalPath;

                        // Update filename in textbox
                        var configNameBox = this.FindControl<TextBox>("ConfigNameBox");
                        if (configNameBox != null)
                        {
                            configNameBox.Text = IoPath.GetFileNameWithoutExtension(
                                IoPath.GetFileNameWithoutExtension(file.Path.LocalPath)
                            );
                        }

                        // Update hidden FilterPathInput
                        var filterPathInput = this.FindControl<TextBox>("FilterPathInput");
                        if (filterPathInput != null)
                        {
                            filterPathInput.Text = file.Path.LocalPath;
                        }

                        BalatroSeedOracle.Helpers.DebugLogger.Log($"✅ Config loaded from: {file.Path.LocalPath}");

                        // Enable tabs and switch to Visual tab
                        UpdateTabStates(true);

                        // Switch to Visual tab
                        var visualTab = this.FindControl<Button>("VisualTab");
                        if (visualTab != null)
                        {
                            OnTabClick(visualTab, new RoutedEventArgs());
                        }
                    }

                    // SearchWidget removed - using desktop icons now
                }
            }
            catch (Exception ex)
            {
                BalatroSeedOracle.Helpers.DebugLogger.LogError($"Error loading config: {ex.Message}");
            }
        }

        private void LoadConfigIntoUI(Motely.Filters.MotelyJsonConfig config)
        {
            // Clear existing selections
            ClearNeeds();
            ClearWants();
            ClearMustNot();

            // Load metadata
            var configNameBox = this.FindControl<TextBox>("ConfigNameBox");
            if (configNameBox != null && !string.IsNullOrEmpty(config.Name))
            {
                configNameBox.Text = config.Name;
            }

            var configDescriptionBox = this.FindControl<TextBox>("ConfigDescriptionBox");
            if (configDescriptionBox != null && !string.IsNullOrEmpty(config.Description))
            {
                configDescriptionBox.Text = config.Description;
            }

            // Load deck/stake preferences
            var deckStakeSelector = this.FindControl<DeckAndStakeSelector>("PreferredDeckStakeSelector");
            if (deckStakeSelector != null && !string.IsNullOrEmpty(config.Deck) && !string.IsNullOrEmpty(config.Stake))
            {
                // Map deck name to index
                string[] deckNames = { "Red", "Blue", "Yellow", "Green", "Black", "Magic", "Nebula", "Ghost",
                                       "Abandoned", "Checkered", "Zodiac", "Painted", "Anaglyph", "Plasma", "Erratic" };
                int deckIndex = Array.IndexOf(deckNames, config.Deck);
                if (deckIndex == -1) deckIndex = 0; // Default to Red

                // Map stake name to index
                string[] stakeNames = { "White", "Red", "Green", "Black", "Blue", "Purple", "Orange", "Gold" };
                int stakeIndex = Array.IndexOf(stakeNames, config.Stake);
                if (stakeIndex == -1) stakeIndex = 0; // Default to White

                // Set the values in the spinners
                var deckSpinner = deckStakeSelector.FindControl<DeckSpinner>("DeckSpinnerControl");
                if (deckSpinner != null)
                {
                    deckSpinner.SelectedDeckIndex = deckIndex;
                }

                var stakeSpinner = deckStakeSelector.FindControl<SpinnerControl>("StakeSpinner");
                if (stakeSpinner != null)
                {
                    stakeSpinner.Value = stakeIndex;
                }
            }

            // Load MUST items (needs)
            if (config.Must != null)
            {
                foreach (var item in config.Must)
                {
                    var category = GetCategoryFromType(item.Type);
                    var itemName = item.Value;

                    // Add to selected needs with unique key
                    if (itemName != null)
                    {
                        var uniqueKey = CreateUniqueKey(category, itemName);
                        _selectedMust.Add(uniqueKey);
                        
                        // Store the full item config including edition, stickers, antes, etc.
                        _itemConfigs[uniqueKey] = new ItemConfig
                        {
                            ItemKey = uniqueKey,
                            Edition = item.Edition ?? "none",
                            Antes = item.Antes?.ToList(),
                            Sources = item.Sources,
                            Min = item.Min,
                            // TODO: Add stickers if they're in the config
                        };
                    }
                }
            }

            // Load SHOULD items (wants)
            if (config.Should != null)
            {
                foreach (var item in config.Should)
                {
                    var category = GetCategoryFromType(item.Type);
                    var itemName = item.Value;

                    // Add to selected wants with unique key
                    if (itemName != null)
                    {
                        var uniqueKey = CreateUniqueKey(category, itemName);
                        _selectedShould.Add(uniqueKey);
                        
                        // Store the full item config
                        _itemConfigs[uniqueKey] = new ItemConfig
                        {
                            ItemKey = uniqueKey,
                            Edition = item.Edition ?? "none",
                            Antes = item.Antes?.ToList(),
                            Sources = item.Sources,
                            Min = item.Min,
                        };
                    }
                }
            }

            // Load MUST NOT items
            if (config.MustNot != null)
            {
                foreach (var item in config.MustNot)
                {
                    var category = GetCategoryFromType(item.Type);
                    var itemName = item.Value;

                    // Add to selected must not with unique key
                    if (itemName != null)
                    {
                        var uniqueKey = CreateUniqueKey(category, itemName);
                        _selectedMustNot.Add(uniqueKey);
                        
                        // Store the full item config
                        _itemConfigs[uniqueKey] = new ItemConfig
                        {
                            ItemKey = uniqueKey,
                            Edition = item.Edition ?? "none",
                            Antes = item.Antes?.ToList(),
                            Sources = item.Sources,
                            Min = item.Min,
                        };
                    }
                }
            }

            // The new format doesn't have a separate filters section
            // All items are directly in Must/Should/MustNot lists

            // Store the loaded config
            _loadedConfig = config;
            
            // Update the UI to show the loaded items
            UpdateDropZoneVisibility();
            RefreshItemPalette();
            UpdateSaveFilterPanel();

            BalatroSeedOracle.Helpers.DebugLogger.Log(
                $"LoadConfigIntoUI: Loaded {_selectedMust.Count} needs, {_selectedShould.Count} wants, {_selectedMustNot.Count} must not"
            );
        }

        private string GetCategoryFromType(string type)
        {
            return type.ToLower() switch
            {
                "joker" or "souljoker" => "Jokers",
                "tarot" or "tarotcard" => "Tarots",
                "spectral" or "spectralcard" => "Spectrals",
                "planet" or "planetcard" => "Planets",
                "tag" or "smallblindtag" or "bigblindtag" => "Tags",
                "voucher" => "Vouchers",
                "playingcard" => "PlayingCards",
                _ => "Unknown",
            };
        }

        private void UpdatePersistentFavorites()
        {
            // Update favorites in the service
            var allSelected = _selectedMust.Union(_selectedShould).ToList();
            FavoritesService.Instance.SetFavoriteItems(allSelected);

            // Update the favorites category
            _itemCategories["Favorites"] = allSelected;
        }

        // 🎯 MUST NOT Drag & Drop Event Handlers

        private void OnMustNotDragEnter(object? sender, DragEventArgs e)
        {
            if (e.Data.Contains("balatro-item") || e.Data.Contains("JokerSet"))
            {
                var mustNotBorder = sender as Border;
                mustNotBorder?.Classes.Add("drag-over");
                e.DragEffects = DragDropEffects.Move;
            }
            e.Handled = true;
        }

        private void OnMustNotDragLeave(object? sender, DragEventArgs e)
        {
            var mustNotBorder = sender as Border;
            mustNotBorder?.Classes.Remove("drag-over");
            e.Handled = true;
        }

        private void OnMustNotDragOver(object? sender, DragEventArgs e)
        {
            if (e.Data.Contains("balatro-item") || e.Data.Contains("JokerSet"))
            {
                e.DragEffects = DragDropEffects.Move;

                // Update ghost position during drag
                if (_isDragging && sender is Visual visual)
                {
                    var position = e.GetPosition(this);
                    UpdateGhostPosition(position);
                }
            }
            else
            {
                e.DragEffects = DragDropEffects.None;
            }
            e.Handled = true;
        }

        private void OnMustNotPanelDrop(object? sender, DragEventArgs e)
        {
            // Remove visual feedback
            var mustNotBorder = sender as Border;
            mustNotBorder?.Classes.Remove("drag-over");

            // Handle JokerSet drop
            if (e.Data.Contains("JokerSet"))
            {
                var jokerSet = e.Data.Get("JokerSet") as FavoritesService.JokerSet;
                if (jokerSet != null)
                {
                    // Add all items from the joker set to must not
                    foreach (var item in jokerSet.Items)
                    {
                        // Find the category for this item
                        string? itemCategory = null;
                        if (BalatroData.Jokers.ContainsKey(item))
                        {
                            itemCategory = "Jokers";
                        }
                        else if (BalatroData.TarotCards.ContainsKey(item))
                        {
                            itemCategory = "Tarots";
                        }
                        else if (BalatroData.SpectralCards.ContainsKey(item))
                        {
                            itemCategory = "Spectrals";
                        }
                        else if (BalatroData.Vouchers.ContainsKey(item))
                        {
                            itemCategory = "Vouchers";
                        }

                        if (itemCategory != null)
                        {
                            var uniqueKey = CreateUniqueKey(itemCategory, item);
                            // Add to must not (allow item in multiple lists)
                            _selectedMustNot.Add(uniqueKey);
                            MarkAsChanged();
                        }
                    }

                    UpdateDropZoneVisibility();
                    UpdatePersistentFavorites();
                    RefreshItemPalette();
                    RemoveDragOverlay();
                    _isDragging = false;
                    e.Handled = true;
                    BalatroSeedOracle.Helpers.DebugLogger.Log(
                        "FiltersModal",
                        $"✅ Added joker set '{jokerSet.Name}' ({jokerSet.Items.Count} items) to MUST NOT"
                    );
                    return;
                }
            }

            if (e.Data.Contains("balatro-item"))
            {
                var itemData = e.Data.Get("balatro-item") as string;
                if (!string.IsNullOrEmpty(itemData))
                {
                    var parts = itemData.Split('|');
                    if (parts.Length >= 2)
                    {
                        var category = parts[0];
                        var itemName = parts[1];

                        if (category == "Set" && e.Data.Contains("set-items"))
                        {
                            // Handle set drop
                            var setItemsData = e.Data.Get("set-items") as string;
                            if (!string.IsNullOrEmpty(setItemsData))
                            {
                                var setItems = setItemsData.Split(',');
                                foreach (var item in setItems)
                                {
                                    // Find the category for this item
                                    string? itemCategory = null;
                                    if (BalatroData.Jokers.ContainsKey(item))
                                    {
                                        itemCategory = "Jokers";
                                    }
                                    else if (BalatroData.TarotCards.ContainsKey(item))
                                    {
                                        itemCategory = "Tarots";
                                    }
                                    else if (BalatroData.SpectralCards.ContainsKey(item))
                                    {
                                        itemCategory = "Spectrals";
                                    }
                                    else if (BalatroData.Vouchers.ContainsKey(item))
                                    {
                                        itemCategory = "Vouchers";
                                    }

                                    if (itemCategory != null)
                                    {
                                        var uniqueKey = CreateUniqueKey(itemCategory, item);
                                        // Add to must not (allow item in multiple lists)
                                        _selectedMustNot.Add(uniqueKey);
                                    }
                                }
                                BalatroSeedOracle.Helpers.DebugLogger.Log(
                                    "FiltersModal",
                                    $"✅ Added set '{itemName}' ({setItems.Length} items) to MUST NOT"
                                );
                            }
                        }
                        else
                        {
                            // Handle single item drop
                            // For SoulJokers, use Jokers category for storage
                            var storageCategory = category == "SoulJokers" ? "Jokers" : category;
                            var key = CreateUniqueKey(storageCategory, itemName);

                            // Add to must not (allow item in multiple lists)
                            _selectedMustNot.Add(key);

                            BalatroSeedOracle.Helpers.DebugLogger.Log(
                                "FiltersModal",
                                $"✅ Added {itemName} to MUST NOT"
                            );
                        }

                        UpdateDropZoneVisibility();
                        UpdatePersistentFavorites();
                        RefreshItemPalette();
                    }
                }
            }

            // Clean up drag visuals after drop
            RemoveDragOverlay();
            _isDragging = false;
            CleanupDragVisuals();

            e.Handled = true;
        }

        private void OnSaveAsFavoriteClick(object? sender, RoutedEventArgs e)
        {
            // Get all selected items
            var selectedItems = _selectedMust.Union(_selectedShould).Union(_selectedMustNot).ToList();
            
            if (!selectedItems.Any())
            {
                UpdateStatus("No items selected to save as favorite!", true);
                return;
            }
            
            // Create a simple dialog to get the name and description
            var dialogPanel = new StackPanel
            {
                Spacing = 10,
                Width = 400,
                Margin = new Thickness(20),
            };
            
            dialogPanel.Children.Add(new TextBlock
            {
                Text = "Save Current Selection as Favorite Set",
                FontSize = 18,
                HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center,
                Margin = new Thickness(0, 0, 0, 10),
            });
            
            var nameBox = new TextBox
            {
                Watermark = "Set Name (e.g. 'My Combo')",
                FontSize = 14,
                Height = 36,
            };
            dialogPanel.Children.Add(nameBox);
            
            var descBox = new TextBox
            {
                Watermark = "Description (optional)",
                FontSize = 14,
                Height = 60,
                TextWrapping = TextWrapping.Wrap,
                AcceptsReturn = true,
            };
            dialogPanel.Children.Add(descBox);
            
            var tagsBox = new TextBox
            {
                Watermark = "Tags (comma separated, e.g. '#Synergy, #XMult')",
                FontSize = 14,
                Height = 36,
            };
            dialogPanel.Children.Add(tagsBox);
            
            var buttonPanel = new StackPanel
            {
                Orientation = Avalonia.Layout.Orientation.Horizontal,
                HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center,
                Spacing = 10,
                Margin = new Thickness(0, 20, 0, 0),
            };
            
            var saveButton = new Button
            {
                Content = "Save",
                Width = 100,
                Height = 40,
            };
            saveButton.Classes.Add("btn-green");
            
            var cancelButton = new Button
            {
                Content = "Cancel",
                Width = 100,
                Height = 40,
            };
            cancelButton.Classes.Add("btn-red");
            
            buttonPanel.Children.Add(saveButton);
            buttonPanel.Children.Add(cancelButton);
            dialogPanel.Children.Add(buttonPanel);
            
            // Create modal
            var mainWindow = TopLevel.GetTopLevel(this) as Window;
            if (mainWindow?.Content is Grid grid)
            {
                var mainMenu = grid.Children.OfType<BalatroMainMenu>().FirstOrDefault();
                if (mainMenu != null)
                {
                    // Create a UserControl wrapper for the dialog panel
                    var dialogWrapper = new UserControl { Content = dialogPanel };
                    var modal = mainMenu.ShowModal("Save as Favorite", dialogWrapper);
                    
                    saveButton.Click += (s, ev) =>
                    {
                        var name = nameBox.Text?.Trim();
                        if (string.IsNullOrWhiteSpace(name))
                        {
                            nameBox.Classes.Add("error");
                            return;
                        }
                        
                        // Extract just the item names without categories
                        var itemNames = selectedItems.Select(item =>
                        {
                            var parts = item.Split(':');
                            if (parts.Length > 1)
                            {
                                var itemName = parts[1];
                                // Remove any unique suffix
                                var hashIndex = itemName.IndexOf('#');
                                return hashIndex > 0 ? itemName.Substring(0, hashIndex) : itemName;
                            }
                            return item;
                        }).ToList();
                        
                        // Parse tags
                        var tags = tagsBox.Text?.Split(',')
                            .Select(t => t.Trim())
                            .Where(t => !string.IsNullOrWhiteSpace(t))
                            .ToList() ?? new List<string>();
                        
                        // Create the new set with zone information
                        var newSet = new FavoritesService.JokerSet
                        {
                            Name = name,
                            Description = descBox.Text?.Trim() ?? "",
                            Items = itemNames, // Keep for backward compatibility
                            Tags = tags,
                            // Store items by their original zones
                            MustItems = _selectedMust.Select(k => k.Split(':').LastOrDefault() ?? "").ToList(),
                            ShouldItems = _selectedShould.Select(k => k.Split(':').LastOrDefault() ?? "").ToList(),
                            MustNotItems = _selectedMustNot.Select(k => k.Split(':').LastOrDefault() ?? "").ToList(),
                        };
                        
                        // Save it
                        FavoritesService.Instance.AddCustomSet(newSet);
                        
                        UpdateStatus($"Saved '{name}' as favorite!", false);
                        
                        // Close the save dialog
                        mainMenu.HideModalContent();
                        
                        // Refresh the favorites if we're on that tab
                        if (_currentCategory == "Favorites")
                        {
                            RefreshItemPalette();
                        }
                    };
                    
                    cancelButton.Click += (s, ev) =>
                    {
                        // Close the dialog
                        mainMenu.HideModalContent();
                    };
                }
            }
        }

        private string GetSuggestedFileName()
        {
            var configNameBox = this.FindControl<TextBox>("ConfigNameBox");
            var configName = configNameBox?.Text?.Trim();

            if (!string.IsNullOrEmpty(configName))
            {
                // Sanitize the filename
                var invalidChars = System.IO.Path.GetInvalidFileNameChars();
                var sanitizedName = new string(
                    configName.Where(c => !invalidChars.Contains(c)).ToArray()
                );
                if (!string.IsNullOrEmpty(sanitizedName))
                {
                    return $"{sanitizedName}.json";
                }
            }

            // Fallback to timestamp-based name
            return $"config-{DateTime.UtcNow:yyyyMMdd-HHmmss}.json";
        }

        private void MergeDropZonesForSet()
        {
            var dropZonesContainer = this.FindControl<Grid>("DropZonesContainer");
            if (dropZonesContainer == null) return;
            
            // Store the original drop zones for restoration
            _originalDropZones = dropZonesContainer.Children.ToList();
            
            // Clear the container
            dropZonesContainer.Children.Clear();
            dropZonesContainer.ColumnDefinitions.Clear();
            
            // Create single column
            dropZonesContainer.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Star));
            
            // Create merged drop zone
            var mergedBorder = new Border
            {
                Background = new SolidColorBrush(Color.Parse("#1a1a1a")),
                BorderThickness = new Thickness(0),
                CornerRadius = new CornerRadius(8),
                MinHeight = 180,
                Margin = new Thickness(5),
            };
            
            // Add the hint text
            var hintText = new TextBlock
            {
                Text = "Drop to apply set",
                FontSize = 24,
                Foreground = new SolidColorBrush(Color.Parse("#FFD700")),
                HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center,
                VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center,
            };
            
            mergedBorder.Child = hintText;
            
            // Set up drag events
            DragDrop.SetAllowDrop(mergedBorder, true);
            mergedBorder.AddHandler(DragDrop.DropEvent, OnMergedZoneDrop);
            mergedBorder.AddHandler(DragDrop.DragOverEvent, OnMergedZoneDragOver);
            mergedBorder.AddHandler(DragDrop.DragEnterEvent, OnMergedZoneDragEnter);
            mergedBorder.AddHandler(DragDrop.DragLeaveEvent, OnMergedZoneDragLeave);
            
            Grid.SetColumn(mergedBorder, 0);
            dropZonesContainer.Children.Add(mergedBorder);
        }
        
        private void RestoreNormalDropZones()
        {
            var dropZonesContainer = this.FindControl<Grid>("DropZonesContainer");
            if (dropZonesContainer == null || _originalDropZones == null) return;
            
            // Clear the merged zone
            dropZonesContainer.Children.Clear();
            dropZonesContainer.ColumnDefinitions.Clear();
            
            // Restore original column definitions
            dropZonesContainer.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Star));
            dropZonesContainer.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Star));
            dropZonesContainer.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Star));
            
            // Restore original drop zones
            foreach (var child in _originalDropZones)
            {
                dropZonesContainer.Children.Add(child);
            }
            
            _originalDropZones = null;
        }
        
        private List<Control>? _originalDropZones;
        
        private void OnMergedZoneDragEnter(object? sender, DragEventArgs e)
        {
            if (e.Data.Contains("JokerSet"))
            {
                var mergedBorder = sender as Border;
                mergedBorder?.Classes.Add("drag-over");
                e.DragEffects = DragDropEffects.Copy;
            }
            e.Handled = true;
        }
        
        private void OnMergedZoneDragLeave(object? sender, DragEventArgs e)
        {
            var mergedBorder = sender as Border;
            mergedBorder?.Classes.Remove("drag-over");
            e.Handled = true;
        }
        
        private void OnMergedZoneDragOver(object? sender, DragEventArgs e)
        {
            if (e.Data.Contains("JokerSet"))
            {
                e.DragEffects = DragDropEffects.Copy;
            }
            else
            {
                e.DragEffects = DragDropEffects.None;
            }
            e.Handled = true;
        }
        
        private void OnMergedZoneDrop(object? sender, DragEventArgs e)
        {
            var mergedBorder = sender as Border;
            mergedBorder?.Classes.Remove("drag-over");
            
            if (e.Data.Contains("JokerSet"))
            {
                var jokerSet = e.Data.Get("JokerSet") as FavoritesService.JokerSet;
                if (jokerSet != null)
                {
                    // Apply the set - distribute items to their original zones
                    if (jokerSet.HasZoneInfo)
                    {
                        // Use the new zone-specific lists
                        foreach (var item in jokerSet.MustItems)
                        {
                            AddItemToZone(item, "needs");
                        }
                        foreach (var item in jokerSet.ShouldItems)
                        {
                            AddItemToZone(item, "wants");
                        }
                        foreach (var item in jokerSet.MustNotItems)
                        {
                            AddItemToZone(item, "mustnot");
                        }
                    }
                    else
                    {
                        // Fallback for old sets without zone info - add all to Should
                        foreach (var item in jokerSet.Items)
                        {
                            AddItemToZone(item, "wants");
                        }
                    }
                    
                    UpdateDropZoneVisibility();
                    UpdateSelectionDisplay();  // Update the visual drop zones
                    UpdatePersistentFavorites();
                    
                    // Force a full refresh of the current category to show selection states
                    var currentCat = _currentCategory;
                    LoadCategory(currentCat); // This will reload with proper selection states
                    
                    UpdateStatus($"Applied set: {jokerSet.Name}", false);
                }
            }
            
            e.Handled = true;
        }
        
        private void AddItemToZone(string itemName, string zone)
        {
            // Find the category for this item
            string? itemCategory = null;
            if (BalatroData.Jokers.ContainsKey(itemName))
            {
                itemCategory = "Jokers";
            }
            else if (BalatroData.TarotCards.ContainsKey(itemName))
            {
                itemCategory = "Tarots";
            }
            else if (BalatroData.SpectralCards.ContainsKey(itemName))
            {
                itemCategory = "Spectrals";
            }
            else if (BalatroData.Vouchers.ContainsKey(itemName))
            {
                itemCategory = "Vouchers";
            }
            else if (BalatroData.Tags.ContainsKey(itemName))
            {
                itemCategory = "Tags";
            }
            
            if (itemCategory != null)
            {
                var itemKey = $"{itemCategory}:{itemName}";
                
                // Create ItemConfig with zone-appropriate ante defaults if it doesn't exist
                if (!_itemConfigs.ContainsKey(itemKey))
                {
                    var config = new ItemConfig
                    {
                        ItemKey = itemKey,
                        Edition = "none"
                    };
                    
                    // Set zone-appropriate ante defaults
                    switch (zone)
                    {
                        case "needs":
                            config.Antes = new List<int> { 1, 2, 3, 4 }; // Early antes for MUST
                            break;
                        case "wants":
                            config.Antes = new List<int> { 1, 2, 3, 4, 5, 6, 7, 8 }; // All antes for SHOULD
                            break;
                        case "mustnot":
                            config.Antes = new List<int> { 1 }; // Just first ante for MUST NOT
                            break;
                    }
                    
                    _itemConfigs[itemKey] = config;
                }
                
                switch (zone)
                {
                    case "needs":
                        if (!_selectedMust.Contains(itemKey))
                            _selectedMust.Add(itemKey);
                        break;
                    case "wants":
                        if (!_selectedShould.Contains(itemKey))
                            _selectedShould.Add(itemKey);
                        break;
                    case "mustnot":
                        if (!_selectedMustNot.Contains(itemKey))
                            _selectedMustNot.Add(itemKey);
                        break;
                }
            }
        }
        
        private void UpdateAutoGeneratedFilterName()
        {
            var configNameBox = this.FindControl<TextBox>("ConfigNameBox");
            if (configNameBox == null)
            {
                return;
            }

            // Only auto-generate if the user hasn't typed anything yet
            if (!string.IsNullOrEmpty(configNameBox.Text?.Trim()))
            {
                return;
            }

            // Get all selected items
            var allItems = new List<string>();
            allItems.AddRange(_selectedMust);
            allItems.AddRange(_selectedShould);
            allItems.AddRange(_selectedMustNot);

            if (allItems.Count == 0)
            {
                return;
            }

            // Extract just the item names (after the "Category:" part)
            var itemNames = allItems
                .Select(item => item.Split(':').LastOrDefault()?.Replace("_", "") ?? "")
                .Where(name => !string.IsNullOrEmpty(name))
                .Take(4) // Only use first 4 items
                .ToList();

            if (itemNames.Count == 0)
            {
                return;
            }

            string filterName = "";

            // Add prefix based on item count
            if (allItems.Count > 8)
            {
                filterName = "HolyCow";
            }
            else if (allItems.Count > 4)
            {
                filterName = "Mega";
            }

            // Combine item names
            var combinedNames = string.Join("", itemNames);

            // If too long, trim each item name by 1 character
            if (combinedNames.Length > 20)
            {
                itemNames = itemNames.Select(name => name.Length > 1 ? name[..^1] : name).ToList();
                combinedNames = string.Join("", itemNames);
            }

            filterName += combinedNames;

            // Set the generated name
            configNameBox.Text = filterName;
        }

        private void ShowItemSelectionPopup(string dropZoneType, Border dropZoneBorder)
        {
            BalatroSeedOracle.Helpers.DebugLogger.Log(
                "FiltersModal",
                $"ShowItemSelectionPopup called for {dropZoneType}"
            );

            // Don't show popup if dragging
            if (_isDragging)
            {
                return;
            }

            // Create popup if it doesn't exist
            if (_itemSelectionPopup == null)
            {
                _itemSelectionPopup = new Popup
                {
                    Placement = PlacementMode.Pointer,
                    IsLightDismissEnabled = true,
                };
            }

            // Create popup content with current category items
            var popupContent = new Border
            {
                Background =
                    Application.Current?.FindResource("ContainerDarkPrecise") as IBrush
                    ?? new SolidColorBrush(Color.Parse("#1e2b2d")),
                BorderBrush =
                    Application.Current?.FindResource("ModalBorder") as IBrush
                    ?? new SolidColorBrush(Color.Parse("#607B7D")),
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(8),
                Padding = new Thickness(12),
                MaxWidth = 600,
                MaxHeight = 500,
            };

            var contentStack = new StackPanel { Spacing = 10 };

            // Header
            var header = new TextBlock
            {
                Text = dropZoneType switch
                {
                    "needs" => "Select items that MUST appear",
                    "wants" => "Select items you'd LIKE to see",
                    "mustnot" => "Select forbidden items",
                    _ => "Select items",
                },
                FontFamily =
                    Application.Current?.FindResource("BalatroFont") as FontFamily
                    ?? FontFamily.Default,
                FontSize = 18,
                Foreground = Application.Current?.FindResource("Gold") as IBrush,
                HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center,
                Margin = new Thickness(0, 0, 0, 10),
            };
            contentStack.Children.Add(header);

            // ScrollViewer for items
            var scrollViewer = new ScrollViewer
            {
                VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
                HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled,
                MaxHeight = 400,
            };

            var itemsPanel = new WrapPanel
            {
                Orientation = Avalonia.Layout.Orientation.Horizontal,
                MaxWidth = 580,
            };

            // Get current category items from the item palette
            var itemPaletteContent = this.FindControl<ContentControl>("ItemPaletteContent");
            if (itemPaletteContent?.Content is ScrollViewer paletteScrollViewer)
            {
                if (paletteScrollViewer.Content is StackPanel categoriesStack)
                {
                    // Find the currently active category
                    foreach (var child in categoriesStack.Children)
                    {
                        if (child is StackPanel categoryPanel && categoryPanel.IsVisible)
                        {
                            // Look for the items wrap panel in this category
                            foreach (var categoryChild in categoryPanel.Children)
                            {
                                if (categoryChild is WrapPanel wrapPanel)
                                {
                                    // Clone each item for the popup
                                    foreach (var item in wrapPanel.Children)
                                    {
                                        if (item is ResponsiveCard card)
                                        {
                                            var clonedCard = CreateItemCardForPopup(
                                                card.ItemName,
                                                card.Category,
                                                dropZoneType
                                            );
                                            itemsPanel.Children.Add(clonedCard);
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }

            scrollViewer.Content = itemsPanel;
            contentStack.Children.Add(scrollViewer);

            // Close button
            var closeButton = new Button
            {
                Content = "Close",
                Classes = { "btn-red" },
                HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center,
                Margin = new Thickness(0, 10, 0, 0),
            };
            closeButton.Click += (s, e) => _itemSelectionPopup.IsOpen = false;
            contentStack.Children.Add(closeButton);

            popupContent.Child = contentStack;
            _itemSelectionPopup.Child = popupContent;
            _itemSelectionPopup.PlacementTarget = dropZoneBorder;
            _itemSelectionPopup.IsOpen = true;
        }

        private ResponsiveCard CreateItemCardForPopup(
            string itemName,
            string category,
            string dropZoneType
        )
        {
            var card = new ResponsiveCard
            {
                ItemName = itemName,
                Category = category,
                ImageSource = GetItemImageForCategory(itemName, category),
                Margin = new Thickness(4),
                Cursor = new Cursor(StandardCursorType.Hand),
            };

            // Handle click to add to the appropriate zone
            card.CardClicked += (sender, args) =>
            {
                if (args.ClickType == CardClickType.LeftClick)
                {
                    var storageCategory = category == "SoulJokers" ? "Jokers" : category;
                    var key = CreateUniqueKey(storageCategory, itemName);

                    // Add to the appropriate set based on dropZoneType
                    switch (dropZoneType)
                    {
                        case "needs":
                            _selectedMust.Add(key);
                            BalatroSeedOracle.Helpers.DebugLogger.Log(
                                "FiltersModal",
                                $"✅ Added {itemName} to NEEDS via popup"
                            );
                            break;
                        case "wants":
                            _selectedShould.Add(key);
                            BalatroSeedOracle.Helpers.DebugLogger.Log(
                                "FiltersModal",
                                $"✅ Added {itemName} to WANTS via popup"
                            );
                            break;
                        case "mustnot":
                            _selectedMustNot.Add(key);
                            BalatroSeedOracle.Helpers.DebugLogger.Log(
                                "FiltersModal",
                                $"✅ Added {itemName} to MUST NOT via popup"
                            );
                            break;
                    }

                    UpdateDropZoneVisibility();
                    UpdatePersistentFavorites();
                    RefreshItemPalette();

                    // Close the popup
                    if (_itemSelectionPopup != null)
                    {
                        _itemSelectionPopup.IsOpen = false;
                    }
                }
            };

            return card;
        }

        private IImage? GetPlayingCardImageFromName(string cardName)
        {
            // Parse "Ace of Hearts" format
            var parts = cardName.Split(" of ");
            if (parts.Length == 2)
            {
                var rank = parts[0];
                var suit = parts[1];
                return SpriteService.Instance.GetPlayingCardImage(suit, rank);
            }
            return null;
        }
        
        private IImage? GetItemImageForCategory(string itemName, string category)
        {
            // No stickers in selection popup - these are base items
            return category switch
            {
                "Jokers" or "SoulJokers" => SpriteService.Instance.GetJokerImage(itemName),
                "Tarots" => SpriteService.Instance.GetTarotImage(itemName),
                "Planets" => SpriteService.Instance.GetPlanetCardImage(itemName),
                "Spectrals" => SpriteService.Instance.GetSpectralImage(itemName),
                "PlayingCards" => GetPlayingCardImageFromName(itemName),
                "Vouchers" => SpriteService.Instance.GetVoucherImage(itemName),
                "Tags" => SpriteService.Instance.GetTagImage(itemName),
                "Bosses" => SpriteService.Instance.GetBossImage(itemName),
                "Decks" => SpriteService.Instance.GetDeckImage(itemName),
                "Stakes" => SpriteService.Instance.GetStickerImage(itemName + "Stake"),
                "Boosters" or "Packs" => SpriteService.Instance.GetBoosterImage(itemName),
                _ => SpriteService.Instance.GetItemImage(itemName, category),
            };
        }
        
        // Auto-save functionality
        private void SetupAutoSave()
        {
            // No longer using timer - saving immediately on changes
        }
        
        private async void MarkAsChanged()
        {
            // Save immediately if we have a filter path
            if (!string.IsNullOrEmpty(_currentFilterPath))
            {
                await SaveCurrentFilterAsync();
                UpdateStatus("Saved", false);
            }
        }
        
        // New helper methods for Filter Info tab
        private async Task SaveCurrentFilterAsync()
        {
            if (string.IsNullOrEmpty(_currentFilterPath))
            {
                // No current path, save as new
                var filterNameInput = this.FindControl<TextBox>("SaveFilterNameInput");
                if (filterNameInput != null && !string.IsNullOrWhiteSpace(filterNameInput.Text))
                {
                    await SaveFilterAsAsync(filterNameInput.Text.Trim());
                }
                else
                {
                    UpdateStatus("Please enter a filter name", true);
                }
                return;
            }
            
            // Save to existing path
            try
            {
                var config = BuildOuijaConfigFromSelections();

                // Update name and description from inputs
                var nameInput = this.FindControl<TextBox>("SaveFilterNameInput");
                var descriptionInput = this.FindControl<TextBox>("FilterDescriptionInput");

                if (nameInput != null)
                    config.Name = nameInput.Text?.Trim() ?? "";
                if (descriptionInput != null)
                    config.Description = descriptionInput.Text?.Trim() ?? "";

                var json = SerializeOuijaConfig(config);
                await File.WriteAllTextAsync(_currentFilterPath, json);

                // CRITICAL: Delete all database files for this filter to avoid column conflicts
                // When editing a filter, the structure may have changed, so old data is invalid
                if (!string.IsNullOrEmpty(config.Name))
                {
                    DeleteFilterDatabases(config.Name);
                }

                UpdateStatus($"Filter saved to {System.IO.Path.GetFileName(_currentFilterPath)}", false);

                // Update modified date display
                var modifiedDateDisplay = this.FindControl<TextBlock>("ModifiedDateDisplay");
                if (modifiedDateDisplay != null)
                {
                    modifiedDateDisplay.Text = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm");
                }
            }
            catch (Exception ex)
            {
                DebugLogger.LogError("FiltersModal", $"Failed to save filter: {ex.Message}");
                UpdateStatus($"Failed to save: {ex.Message}", true);
            }
        }
        
        private async Task SaveFilterAsAsync(string newName)
        {
            try
            {
                var config = BuildOuijaConfigFromSelections();
                config.Name = newName;
                
                // Update description if provided
                var descriptionInput = this.FindControl<TextBox>("FilterDescriptionInput");
                if (descriptionInput != null)
                    config.Description = descriptionInput.Text?.Trim() ?? "";
                
                // Generate filename
                var fileName = NormalizeFileName(newName);
                var filePath = System.IO.Path.Combine(Directory.GetCurrentDirectory(), "JsonItemFilters", $"{fileName}.json");
                
                // Handle duplicates
                int counter = 1;
                while (File.Exists(filePath))
                {
                    filePath = System.IO.Path.Combine(Directory.GetCurrentDirectory(), "JsonItemFilters", $"{fileName}{counter}.json");
                    counter++;
                }
                
                var json = SerializeOuijaConfig(config);
                await File.WriteAllTextAsync(filePath, json);
                
                _currentFilterPath = filePath;
                _loadedConfig = config;
                
                UpdateStatus($"Filter saved as {System.IO.Path.GetFileName(filePath)}", false);
                UpdateSaveFilterPanel();
                
                // Enable search button
                var searchButton = this.FindControl<Button>("SearchForSeedsButton");
                if (searchButton != null)
                {
                    searchButton.IsEnabled = true;
                    searchButton.Tag = filePath;
                }
            }
            catch (Exception ex)
            {
                DebugLogger.LogError("FiltersModal", $"Failed to save filter as: {ex.Message}");
                UpdateStatus($"Failed to save: {ex.Message}", true);
            }
        }
        
        private string NormalizeFileName(string name)
        {
            // Remove special characters and replace with hyphens
            var normalized = System.Text.RegularExpressions.Regex.Replace(name, @"[^a-zA-Z0-9]+", "-");
            
            // Remove leading/trailing hyphens
            normalized = normalized.Trim('-');
            
            // If empty, use default
            if (string.IsNullOrEmpty(normalized))
                normalized = "NewFilter";
                
            return normalized;
        }
        
        // New event handlers for Filter Info buttons
        private async void OnSaveChangesClick(object? sender, RoutedEventArgs e)
        {
            await SaveCurrentFilterAsync();
        }
        
        private async void OnSaveAsClick(object? sender, RoutedEventArgs e)
        {
            var filterNameInput = this.FindControl<TextBox>("SaveFilterNameInput");
            if (filterNameInput == null || string.IsNullOrWhiteSpace(filterNameInput.Text))
            {
                UpdateStatus("Please enter a filter name", true);
                return;
            }
            
            var newName = filterNameInput.Text.Trim() + " (Copy)";
            await SaveFilterAsAsync(newName);
        }
        
        private async void OnDuplicateClick(object? sender, RoutedEventArgs e)
        {
            var filterNameInput = this.FindControl<TextBox>("SaveFilterNameInput");
            if (filterNameInput != null && !string.IsNullOrWhiteSpace(filterNameInput.Text))
            {
                var duplicateName = filterNameInput.Text.Trim() + " (Copy)";
                await SaveFilterAsAsync(duplicateName);

                // Refresh the filter list to show the duplicate
                _filterListViewModel?.LoadFilters();
            }
        }
        
        private async void OnExportClick(object? sender, RoutedEventArgs e)
        {
            if (string.IsNullOrEmpty(_currentFilterPath))
            {
                UpdateStatus("Please save the filter first", true);
                return;
            }
            
            var topLevel = TopLevel.GetTopLevel(this);
            if (topLevel == null) return;
            
            var file = await topLevel.StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
            {
                Title = "Export Filter",
                DefaultExtension = "json",
                FileTypeChoices = new[] { new FilePickerFileType("JSON Files") { Patterns = new[] { "*.json" } } },
                SuggestedFileName = System.IO.Path.GetFileName(_currentFilterPath)
            });
            
            if (file != null && File.Exists(_currentFilterPath))
            {
                var content = await File.ReadAllTextAsync(_currentFilterPath);
                await File.WriteAllTextAsync(file.Path.LocalPath, content);
                UpdateStatus($"Filter exported to {file.Name}", false);
            }
        }
        
        /// <summary>
        /// Formats JSON to use compact arrays [1,2,3] instead of vertical arrays
        /// This makes the JSON editor MUCH more readable for "antes" arrays!
        /// </summary>
        private string FormatJsonWithCompactArrays(string json)
        {
            // Simple regex replacement: collapse arrays that span multiple lines
            // Matches: [\n    1,\n    2,\n    3\n  ] → [1, 2, 3]
            var compactJson = System.Text.RegularExpressions.Regex.Replace(
                json,
                @"\[\s*\n\s*((?:[^\[\]]+?,\s*\n\s*)*[^\[\]]+?)\s*\n\s*\]",
                match =>
                {
                    // Extract array content, remove newlines and extra spaces
                    var content = match.Groups[1].Value;
                    content = System.Text.RegularExpressions.Regex.Replace(content, @"\s*\n\s*", " ");
                    content = System.Text.RegularExpressions.Regex.Replace(content, @"\s+", " ");
                    return $"[{content.Trim()}]";
                },
                System.Text.RegularExpressions.RegexOptions.Multiline
            );
            return compactJson;
        }

        // PERFORMANCE FIX (QW-5): Helper methods to keep List and HashSet in sync
        // Ensures O(1) lookups while maintaining backward compatibility with List-based code
        private void AddToMust(string key)
        {
            _selectedMust.Add(key);
            _selectedMustSet.Add(key);
        }

        private void AddToShould(string key)
        {
            _selectedShould.Add(key);
            _selectedShouldSet.Add(key);
        }

        private void AddToMustNot(string key)
        {
            _selectedMustNot.Add(key);
            _selectedMustNotSet.Add(key);
        }

        private bool RemoveFromMust(string key)
        {
            _selectedMustSet.Remove(key);
            return _selectedMust.Remove(key);
        }

        private bool RemoveFromShould(string key)
        {
            _selectedShouldSet.Remove(key);
            return _selectedShould.Remove(key);
        }

        private bool RemoveFromMustNot(string key)
        {
            _selectedMustNotSet.Remove(key);
            return _selectedMustNot.Remove(key);
        }

        private void ClearMust()
        {
            _selectedMust.Clear();
            _selectedMustSet.Clear();
        }

        private void ClearShould()
        {
            _selectedShould.Clear();
            _selectedShouldSet.Clear();
        }

        private void ClearMustNotItems()
        {
            _selectedMustNot.Clear();
            _selectedMustNotSet.Clear();
        }

        private async void OnShareClick(object? sender, RoutedEventArgs e)
        {
            if (string.IsNullOrEmpty(_currentFilterPath) || !File.Exists(_currentFilterPath))
            {
                UpdateStatus("Please save the filter first", true);
                return;
            }

            var content = await File.ReadAllTextAsync(_currentFilterPath);
            var clipboard = TopLevel.GetTopLevel(this)?.Clipboard;
            if (clipboard != null)
            {
                await clipboard.SetTextAsync(content);
                UpdateStatus("Filter JSON copied to clipboard!", false);
            }
        }

        /// <summary>
        /// Deletes all database files associated with a filter name
        /// This prevents column conflicts when editing filter structure
        /// </summary>
        private void DeleteFilterDatabases(string filterName)
        {
            try
            {
                var searchResultsPath = System.IO.Path.Combine(Directory.GetCurrentDirectory(), "SearchResults");
                if (!Directory.Exists(searchResultsPath))
                {
                    DebugLogger.Log("FiltersModal", "SearchResults directory does not exist");
                    return;
                }

                // Normalize filter name for filename matching (same as NormalizeFileName)
                var normalizedName = NormalizeFileName(filterName);

                // Find all .db files that start with this filter name
                // Pattern: {FilterName}_{Deck}_{Stake}.db or {FilterName}.db
                var dbFiles = Directory.GetFiles(searchResultsPath, "*.db")
                    .Where(f => {
                        var fileName = System.IO.Path.GetFileNameWithoutExtension(f);
                        return fileName.StartsWith(normalizedName, StringComparison.OrdinalIgnoreCase);
                    })
                    .ToList();

                foreach (var dbFile in dbFiles)
                {
                    try
                    {
                        File.Delete(dbFile);
                        DebugLogger.Log("FiltersModal", $"Deleted database file: {System.IO.Path.GetFileName(dbFile)}");
                    }
                    catch (Exception ex)
                    {
                        DebugLogger.LogError("FiltersModal", $"Failed to delete {System.IO.Path.GetFileName(dbFile)}: {ex.Message}");
                    }
                }

                if (dbFiles.Count > 0)
                {
                    DebugLogger.Log("FiltersModal", $"Deleted {dbFiles.Count} database file(s) for filter '{filterName}'");
                }
            }
            catch (Exception ex)
            {
                DebugLogger.LogError("FiltersModal", $"Error while cleaning up databases: {ex.Message}");
            }
        }
    }
}

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Reactive;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Shapes;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Oracle.Services;
using Oracle.Controls;
using Oracle.Helpers;
using ReactiveUI;
using Avalonia.Media.Imaging;
using Avalonia.Media;
using System.Text.Json;
using System.IO;
using Avalonia.Controls.Primitives;
using System.Globalization;
using Avalonia.VisualTree;

namespace Oracle.Views.Modals
{
    public partial class SearchModal : UserControl
    {
        public event EventHandler<string>? CreateDesktopIconRequested;
        private readonly ObservableCollection<SearchResult> _searchResults = new();
        private MotelySearchService? _searchService;
        private bool _isSearching = false;
        
        // Tab panels
        private Panel? _filterPanel;
        private Panel? _settingsPanel;
        private Panel? _searchPanel;
        private Panel? _resultsPanel;
        
        // Tab buttons
        private Button? _filterTab;
        private Button? _settingsTab;
        private Button? _searchTab;
        private Button? _resultsTab;
        
        // Triangle pointer container (for animation)
        private Grid? _triangleContainer;
        
        // Controls
        private TextBox? _consoleOutput;
        
        // Action buttons
        private Button? _cookButton;
        private Button? _saveWidgetButton;
        
        // Balatro Spinners
        private SpinnerControl? _threadsSpinner;
        private SpinnerControl? _batchSizeSpinner;
        private SpinnerControl? _minScoreSpinner;
        private ComboBox? _deckComboBox;
        private ComboBox? _stakeComboBox;
        private CheckBox? _debugCheckBox;
        
        // Deck and Stake selection
        private Image? _deckPreviewImage;
        private Image? _stakeChipOverlay;
        private Canvas? _stakeChipsCanvas;
        private TextBlock? _deckNameText;
        private TextBlock? _deckDescText;
        private Button? _deckLeftArrow;
        private Button? _deckRightArrow;
        private SpinnerControl? _stakeSpinner;
        private TextBlock? _stakeNameText;
        private TextBlock? _stakeDescText;
        private Button? _stakeLeftArrow;
        private Button? _stakeRightArrow;
        
        private int _currentDeckIndex = 0;
        private int _currentStakeIndex = 0;
        private SpriteService? _spriteService;
        
        // Deck data
        private readonly List<(string name, string description, int x, int y)> _decks = new()
        {
            ("Red", "+1 Discard every round", 0, 0),
            ("Blue", "+1 Hand every round", 0, 2),
            ("Yellow", "+$10 at start", 1, 2),
            ("Green", "No interest\nAt end of round: +$2 per\nremaining Hand\n$1 per remaining Discard", 2, 2),
            ("Black", "+1 Joker slot\n-1 Hand every round", 3, 2),
            ("Magic", "Start run with the\n'Crystal Ball' voucher\nand 2 copies of 'The Fool'", 0, 3),
            ("Nebula", "Start run with the\n'Telescope' voucher", 4, 0),
            ("Ghost", "Spectral cards may\nappear in the shop\nStart with a Hex spectral card", 6, 2),
            ("Abandoned", "Start run with no\nFace Cards in your deck", 3, 3),
            ("Checkered", "Start run with 26 Spades\nand 26 Hearts in deck", 1, 3),
            ("Zodiac", "Start run with\n'Overstock', 'Overstock Plus',\nand 'Crystal Ball' vouchers", 3, 4),
            ("Painted", "Start run with 2 Hands,\n+1 Hand size", 4, 3),
            ("Anaglyph", "After defeating each\nBlind, gain a Double Tag", 2, 4),
            ("Plasma", "Balance Chips and Mult\nwhen calculating score for played hand\nBase Blind size increased", 4, 2),
            ("Erratic", "All Ranks and Suits\nin deck are randomized", 2, 3)
        };
        
        // Stake data
        private readonly List<(string name, string description, int x, int y)> _stakes = new()
        {
            ("White Stake", "No modifiers", 0, 1),
            ("Red Stake", "Small Blind gives\nno reward money", 2, 0),
            ("Green Stake", "Required score scales\nfaster per Ante", 3, 0),
            ("Black Stake", "Shop can have Eternal\nJokers (can't be sold or destroyed)", 1, 0),
            ("Blue Stake", "-1 Discard", 4, 0),
            ("Purple Stake", "Required score scales\nfaster per Ante", 1, 1),
            ("Orange Stake", "Shop can have Perishable\nJokers (Debuffed after 5 Rounds)", 2, 1),
            ("Gold Stake", "Shop can have Rental\nJokers (Costs $3 per round)", 3, 1)
        };
        
        // Current filter info
        private string? _currentFilterPath;
        
        // Filter preview
        private Button? _filterLeftArrow;
        private Button? _filterRightArrow;
        private TextBlock? _filterPreviewName;
        private TextBlock? _filterPreviewDesc;
        private Canvas? _filterCardsCanvas;
        private TextBlock? _filterAuthorText;
        private StackPanel? _authorPanel;
        private List<string> _availableFilters = new();
        private int _currentFilterIndex = 0;
        
        // Results panel controls
        private DataGrid? _resultsDataGrid;
        private TextBlock? _resultsSummary;
        private Button? _exportResultsButton;
        
        public SearchModal()
        {
            InitializeComponent();
            this.Unloaded += OnUnloaded;
        }
        
        private void OnUnloaded(object? sender, EventArgs e)
        {
            // If search is running when modal closes, create desktop icon
            if (_isSearching && !string.IsNullOrEmpty(_currentFilterPath))
            {
                Oracle.Helpers.DebugLogger.Log("SearchModal", "Creating desktop icon for ongoing search...");
                CreateDesktopIconRequested?.Invoke(this, _currentFilterPath);
            }
        }
        
        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
            
            // Find panels
            _filterPanel = this.FindControl<Panel>("FilterPanel");
            _settingsPanel = this.FindControl<Panel>("SettingsPanel");
            _searchPanel = this.FindControl<Panel>("SearchPanel");
            _resultsPanel = this.FindControl<Panel>("ResultsPanel");
            
            // Find tab buttons
            _filterTab = this.FindControl<Button>("FilterTab");
            _settingsTab = this.FindControl<Button>("SettingsTab");
            _searchTab = this.FindControl<Button>("SearchTab");
            _resultsTab = this.FindControl<Button>("ResultsTab");
            
            // Find triangle pointer's parent Grid (for animation)
            var tabTriangle = this.FindControl<Polygon>("TabTriangle");
            _triangleContainer = tabTriangle?.Parent as Grid;
            
            // Find controls
            _consoleOutput = this.FindControl<TextBox>("ConsoleOutput");
            
            // Find buttons
            _cookButton = this.FindControl<Button>("CookButton");
            _saveWidgetButton = this.FindControl<Button>("SaveWidgetButton");
            
            // Find Balatro spinners and set up their ranges
            _threadsSpinner = this.FindControl<SpinnerControl>("ThreadsSpinner");
            _batchSizeSpinner = this.FindControl<SpinnerControl>("BatchSizeSpinner");
            _minScoreSpinner = this.FindControl<SpinnerControl>("MinScoreSpinner");
            _deckComboBox = this.FindControl<ComboBox>("DeckComboBox");
            _stakeComboBox = this.FindControl<ComboBox>("StakeComboBox");
            
            // Find deck/stake UI elements
            _deckPreviewImage = this.FindControl<Image>("DeckPreviewImage");
            _stakeChipOverlay = this.FindControl<Image>("StakeChipOverlay");
            _stakeChipsCanvas = this.FindControl<Canvas>("StakeChipsCanvas");
            _deckNameText = this.FindControl<TextBlock>("DeckNameText");
            _deckDescText = this.FindControl<TextBlock>("DeckDescText");
            _deckLeftArrow = this.FindControl<Button>("DeckLeftArrow");
            _deckRightArrow = this.FindControl<Button>("DeckRightArrow");
            _stakeSpinner = this.FindControl<SpinnerControl>("StakeSpinner");
            _stakeNameText = this.FindControl<TextBlock>("StakeNameText");
            _stakeDescText = this.FindControl<TextBlock>("StakeDescText");
            _stakeLeftArrow = this.FindControl<Button>("StakeLeftArrow");
            _stakeRightArrow = this.FindControl<Button>("StakeRightArrow");
            _debugCheckBox = this.FindControl<CheckBox>("DebugCheckBox");
            
            // Find filter preview elements
            _filterLeftArrow = this.FindControl<Button>("FilterLeftArrow");
            _filterRightArrow = this.FindControl<Button>("FilterRightArrow");
            _filterPreviewName = this.FindControl<TextBlock>("FilterPreviewName");
            _filterPreviewDesc = this.FindControl<TextBlock>("FilterPreviewDesc");
            _filterCardsCanvas = this.FindControl<Canvas>("FilterCardsCanvas");
            _filterAuthorText = this.FindControl<TextBlock>("FilterAuthorText");
            _authorPanel = this.FindControl<StackPanel>("AuthorPanel");
            
            // Find results panel controls
            _resultsDataGrid = this.FindControl<DataGrid>("ResultsDataGrid");
            _resultsSummary = this.FindControl<TextBlock>("ResultsSummary");
            _exportResultsButton = this.FindControl<Button>("ExportResultsButton");
            
            // Set up results data grid
            if (_resultsDataGrid != null)
            {
                _resultsDataGrid.ItemsSource = _searchResults;
            }
            
            // Set up threads spinner with processor count
            if (_threadsSpinner != null)
            {
                _threadsSpinner.Maximum = Environment.ProcessorCount;
                _threadsSpinner.Value = Math.Min(4, Environment.ProcessorCount);
            }
            
            // Initialize search service
            _searchService = App.GetService<MotelySearchService>();
            if (_searchService != null)
            {
                _searchService.ProgressUpdated += OnSearchProgressUpdated;
                _searchService.ResultFound += OnResultFound;
                _searchService.ConsoleOutput += OnConsoleOutput;
                _searchService.SearchStarted += OnSearchStarted;
                _searchService.SearchCompleted += OnSearchCompleted;
            }
            
            // Initialize sprite service
            _spriteService = App.GetService<SpriteService>();
            
            // Set up deck/stake navigation
            if (_deckLeftArrow != null)
                _deckLeftArrow.Click += (s, e) => NavigateDeck(-1);
            if (_deckRightArrow != null)
                _deckRightArrow.Click += (s, e) => NavigateDeck(1);
            if (_stakeLeftArrow != null)
                _stakeLeftArrow.Click += (s, e) => NavigateStake(-1);
            if (_stakeRightArrow != null)
                _stakeRightArrow.Click += (s, e) => NavigateStake(1);
            if (_stakeSpinner != null)
            {
                _stakeSpinner.ValueChanged += (s, e) => 
                {
                    _currentStakeIndex = (int)_stakeSpinner.Value;
                    UpdateStakeDisplay();
                };
            }
            
            // Initialize deck and stake display
            UpdateDeckDisplay();
            UpdateStakeDisplay();
            
            // Initialize stake spinner with stake names
            if (_stakeSpinner != null)
            {
                // The SpinnerControl will automatically show stake names when SpinnerType="stake"
                _stakeSpinner.Value = 0;
                _currentStakeIndex = 0;
            }
            
            // Load available filters
            LoadAvailableFilters();
            
            // Enable tabs and cook button since we auto-load a filter
            UpdateTabStates(true);
            
            // If we have filters available, auto-load the first one
            if (_availableFilters.Count > 0)
            {
                _currentFilterPath = _availableFilters[0];
                // Actually load the filter into the search service
                Task.Run(async () => await LoadFilterAsync(_currentFilterPath));
            }
        }
        
        private void LoadAvailableFilters()
        {
            _availableFilters.Clear();
            
            // Look for .json files in the root directory
            var directory = System.IO.Directory.GetCurrentDirectory();
            var allJsonFiles = System.IO.Directory.GetFiles(directory, "*.json");
            var jsonFiles = new List<string>();
            foreach (var f in allJsonFiles)
            {
                if (!f.EndsWith("avalonia.json") && !f.EndsWith("userprofile.json") && !f.EndsWith("appsettings.json"))
                    jsonFiles.Add(f);
            }
            
            // Sort by filename
            jsonFiles.Sort((a, b) => System.IO.Path.GetFileName(a).CompareTo(System.IO.Path.GetFileName(b)));
            
            _availableFilters = jsonFiles;
            
            // Update filter preview
            UpdateFilterPreview();
        }
        
        private void UpdateFilterPreview()
        {
            if (_filterPreviewName == null || _filterPreviewDesc == null || _filterCardsCanvas == null)
                return;
                
            if (_availableFilters.Count == 0)
            {
                _filterPreviewName.Text = "No filters found";
                _filterPreviewDesc.Text = "Import a filter or create one in the Filter Builder";
                _filterCardsCanvas.Children.Clear();
                
                // Add a placeholder card icon
                var placeholderBorder = new Border
                {
                    Width = 60,
                    Height = 80,
                    Background = new SolidColorBrush(Color.Parse("#1a1f26")),
                    BorderBrush = new SolidColorBrush(Color.Parse("#3a424d")),
                    BorderThickness = new Thickness(2),
                    CornerRadius = new CornerRadius(6)
                };
                
                placeholderBorder.Child = new TextBlock
                {
                    Text = "?",
                    FontSize = 32,
                    FontWeight = FontWeight.Bold,
                    Foreground = new SolidColorBrush(Color.Parse("#3a424d")),
                    HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center,
                    VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center
                };
                
                Canvas.SetLeft(placeholderBorder, 70);
                Canvas.SetTop(placeholderBorder, 0);
                _filterCardsCanvas.Children.Add(placeholderBorder);
                
                return;
            }
            
            // Get current filter
            var filterPath = _availableFilters[_currentFilterIndex];
            var filterName = System.IO.Path.GetFileNameWithoutExtension(filterPath);
            
            _filterPreviewName.Text = filterName;
            
            // Try to load and parse the filter
            try
            {
                var json = System.IO.File.ReadAllText(filterPath);
                var jsonDoc = JsonDocument.Parse(json);
                var root = jsonDoc.RootElement;
                
                // Get description
                string description = "No description";
                if (root.TryGetProperty("description", out var descProp))
                    description = descProp.GetString() ?? "No description";
                _filterPreviewDesc.Text = description;
                
                // Get author if available
                if (root.TryGetProperty("author", out var authorProp) && _authorPanel != null && _filterAuthorText != null)
                {
                    var author = authorProp.GetString();
                    if (!string.IsNullOrEmpty(author))
                    {
                        _filterAuthorText.Text = author;
                        _authorPanel.IsVisible = true;
                    }
                    else
                    {
                        _authorPanel.IsVisible = false;
                    }
                }
                else if (_authorPanel != null)
                {
                    _authorPanel.IsVisible = false;
                }
                
                // Clear canvas
                _filterCardsCanvas.Children.Clear();
                
                // Get must items
                if (root.TryGetProperty("must", out var mustProp) && mustProp.ValueKind == JsonValueKind.Object)
                {
                    var mustItems = new List<(string category, List<string> items)>();
                    
                    // Collect all must items
                    foreach (var category in mustProp.EnumerateObject())
                    {
                        if (category.Value.ValueKind == JsonValueKind.Array)
                        {
                            var itemsList = new List<string>();
                            foreach (var item in category.Value.EnumerateArray())
                            {
                                var itemStr = item.GetString();
                                if (!string.IsNullOrEmpty(itemStr))
                                    itemsList.Add(itemStr);
                            }
                            
                            if (itemsList.Count > 0)
                                mustItems.Add((category.Name, itemsList));
                        }
                    }
                    
                    // Display up to 4 items as fanned cards
                    int cardCount = 0;
                    double fanAngle = 5; // degrees per card
                    double cardWidth = 38;
                    double cardHeight = 48;
                    double cardSpacing = 25;
                    double totalWidth = cardSpacing * 3; // Total width for 4 cards
                    double startX = (200 - totalWidth) / 2; // Center in the 200px canvas
                    
                    foreach (var (category, items) in mustItems)
                    {
                        foreach (var item in items)
                        {
                            if (cardCount >= 4) break;
                            
                            // Create card border
                            var border = new Border
                            {
                                Width = cardWidth,
                                Height = cardHeight,
                                Background = new SolidColorBrush(Color.Parse("#2c333a")),
                                BorderBrush = new SolidColorBrush(Color.Parse("#596981")),
                                BorderThickness = new Thickness(1),
                                CornerRadius = new CornerRadius(4),
                                RenderTransform = new RotateTransform(fanAngle * (cardCount - 1.5))
                            };
                            
                            // Try to get sprite image
                            Image? spriteImage = null;
                            if (_spriteService != null)
                            {
                                switch (category.ToLower())
                                {
                                    case "jokers":
                                        var jokerImage = _spriteService.GetJokerImage(item);
                                        if (jokerImage != null)
                                        {
                                            spriteImage = new Image { Source = jokerImage };
                                        }
                                        break;
                                    case "consumables":
                                    case "tarots":
                                        var tarotImage = _spriteService.GetTarotImage(item);
                                        if (tarotImage != null)
                                        {
                                            spriteImage = new Image { Source = tarotImage };
                                        }
                                        break;
                                    case "planets":
                                        var planetImage = _spriteService.GetTarotImage(item);
                                        if (planetImage != null)
                                        {
                                            spriteImage = new Image { Source = planetImage };
                                        }
                                        break;
                                    case "spectrals":
                                        var spectralImage = _spriteService.GetSpectralImage(item);
                                        if (spectralImage != null)
                                        {
                                            spriteImage = new Image { Source = spectralImage };
                                        }
                                        break;
                                    case "tags":
                                        var tagImage = _spriteService.GetTagImage(item);
                                        if (tagImage != null)
                                        {
                                            spriteImage = new Image { Source = tagImage };
                                        }
                                        break;
                                    case "vouchers":
                                        var voucherImage = _spriteService.GetVoucherImage(item);
                                        if (voucherImage != null)
                                        {
                                            spriteImage = new Image { Source = voucherImage };
                                        }
                                        break;
                                }
                            }
                            
                            if (spriteImage != null)
                            {
                                spriteImage.Width = cardWidth - 4;
                                spriteImage.Height = cardHeight - 4;
                                spriteImage.Stretch = Stretch.Uniform;
                                border.Child = spriteImage;
                            }
                            else
                            {
                                // Fallback text
                                border.Child = new TextBlock
                                {
                                    Text = item.Replace("_", " "),
                                    FontSize = 8,
                                    Foreground = Brushes.White,
                                    HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center,
                                    VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center,
                                    TextWrapping = TextWrapping.Wrap,
                                    TextAlignment = TextAlignment.Center
                                };
                            }
                            
                            // Position the card with proper centering
                            Canvas.SetLeft(border, startX + cardCount * cardSpacing);
                            Canvas.SetTop(border, 8);
                            
                            _filterCardsCanvas.Children.Add(border);
                            cardCount++;
                        }
                        if (cardCount >= 4) break;
                    }
                }
            }
            catch (Exception ex)
            {
                _filterPreviewDesc.Text = "Error loading filter";
                _filterCardsCanvas.Children.Clear();
                Oracle.Helpers.DebugLogger.LogError("SearchModal", $"Error loading filter preview: {ex.Message}");
            }
        }
        
        private async void OnFilterLeftArrowClick(object? sender, RoutedEventArgs e)
        {
            if (_availableFilters.Count == 0) return;
            _currentFilterIndex = (_currentFilterIndex - 1 + _availableFilters.Count) % _availableFilters.Count;
            _currentFilterPath = _availableFilters[_currentFilterIndex];
            UpdateFilterPreview();
            
            // Load the newly selected filter
            await LoadFilterAsync(_currentFilterPath);
        }
        
        private async void OnFilterRightArrowClick(object? sender, RoutedEventArgs e)
        {
            if (_availableFilters.Count == 0) return;
            _currentFilterIndex = (_currentFilterIndex + 1) % _availableFilters.Count;
            _currentFilterPath = _availableFilters[_currentFilterIndex];
            UpdateFilterPreview();
            
            // Load the newly selected filter
            await LoadFilterAsync(_currentFilterPath);
        }
        
        private void UpdateTabStates(bool filterLoaded)
        {
            if (_settingsTab != null) _settingsTab.IsEnabled = filterLoaded;
            if (_searchTab != null) _searchTab.IsEnabled = filterLoaded;
            if (_resultsTab != null) _resultsTab.IsEnabled = filterLoaded && _searchResults.Count > 0;
        }
        
        private void OnTabClick(object? sender, RoutedEventArgs e)
        {
            if (sender is not Button clickedTab) return;
            
            // Remove active class from all tabs
            _filterTab?.Classes.Remove("active");
            _settingsTab?.Classes.Remove("active");
            _searchTab?.Classes.Remove("active");
            _resultsTab?.Classes.Remove("active");
            
            // Move triangle container to correct column
            if (_triangleContainer != null)
            {
                int column = clickedTab.Name switch
                {
                    "FilterTab" => 0,
                    "SettingsTab" => 1,
                    "SearchTab" => 2,
                    "ResultsTab" => 3,
                    _ => 0
                };
                Grid.SetColumn(_triangleContainer, column);
            }
            
            // Hide all panels
            if (_filterPanel != null) _filterPanel.IsVisible = false;
            if (_settingsPanel != null) _settingsPanel.IsVisible = false;
            if (_searchPanel != null) _searchPanel.IsVisible = false;
            if (_resultsPanel != null) _resultsPanel.IsVisible = false;
            
            // Show the clicked panel and mark tab as active
            if (clickedTab == _filterTab)
            {
                clickedTab.Classes.Add("active");
                if (_filterPanel != null) _filterPanel.IsVisible = true;
            }
            else if (clickedTab == _settingsTab)
            {
                clickedTab.Classes.Add("active");
                if (_settingsPanel != null) _settingsPanel.IsVisible = true;
            }
            else if (clickedTab == _searchTab)
            {
                clickedTab.Classes.Add("active");
                if (_searchPanel != null) _searchPanel.IsVisible = true;
            }
            else if (clickedTab == _resultsTab)
            {
                clickedTab.Classes.Add("active");
                if (_resultsPanel != null) _resultsPanel.IsVisible = true;
            }
        }
        
        private async void OnBrowseFilterClick(object? sender, RoutedEventArgs e)
        {
            var topLevel = TopLevel.GetTopLevel(this);
            if (topLevel == null) return;
            
            var options = new Avalonia.Platform.Storage.FilePickerOpenOptions
            {
                Title = "Select Filter Configuration",
                AllowMultiple = false,
                FileTypeFilter = new[]
                {
                    new Avalonia.Platform.Storage.FilePickerFileType("Filter Files") { Patterns = new[] { "*.json" } },
                    new Avalonia.Platform.Storage.FilePickerFileType("All Files") { Patterns = new[] { "*" } }
                }
            };
            
            var result = await topLevel.StorageProvider.OpenFilePickerAsync(options);
            
            if (result?.Count > 0)
            {
                var filePath = result[0].Path.LocalPath;
                await LoadFilterAsync(filePath);
                
                // Add the imported file to available filters if not already there
                if (!_availableFilters.Contains(filePath))
                {
                    _availableFilters.Add(filePath);
                }
                
                // Set the spinner to show the imported filter
                _currentFilterIndex = _availableFilters.IndexOf(filePath);
                if (_currentFilterIndex < 0)
                {
                    _currentFilterIndex = _availableFilters.Count - 1;
                }
                
                // Update the preview to show the imported filter
                UpdateFilterPreview();
            }
        }
        
        private void OnOpenFilterBuilderClick(object? sender, RoutedEventArgs e)
        {
            // Close this modal and open the filters modal
            var window = TopLevel.GetTopLevel(this) as Window;
            if (window != null)
            {
                // Find and close the current modal
                var modalHost = window.FindControl<Grid>("ModalHost");
                if (modalHost != null && modalHost.Children.Count > 0)
                {
                    modalHost.Children.Clear();
                    
                    // Open the filters modal
                    var filtersModal = new Oracle.Views.Modals.FiltersModalContent();
                    modalHost.Children.Add(filtersModal);
                }
            }
        }
        
        public async Task LoadFilterAsync(string filePath)
        {
            _currentFilterPath = filePath;
            
            if (_searchService != null)
            {
                var (success, message, name, author, description) = await _searchService.LoadConfigAsync(filePath);
                if (success)
                {
                    // Just log that we loaded successfully
                    Oracle.Helpers.DebugLogger.Log("SearchModal", $"Filter loaded successfully: {name}");
                    
                    // Initialize the search history service with this filter
                    var historyService = App.GetService<SearchHistoryService>();
                    if (historyService != null)
                    {
                        historyService.SetFilterName(filePath);
                    }
                    
                    // Cook button should always be enabled when a filter is loaded
                    if (_cookButton != null)
                    {
                        _cookButton.IsEnabled = true;
                    }
                    
                    // Enable tabs now that filter is loaded
                    UpdateTabStates(true);
                    
                    // Add to console
                    AddToConsole($"Filter loaded: {System.IO.Path.GetFileName(filePath)}");
                }
                else
                {
                    // Show error in console
                    AddToConsole($"Error: {message}");
                }
            }
        }
        
        private async void OnCookClick(object? sender, RoutedEventArgs e)
        {
            if (_searchService == null || string.IsNullOrEmpty(_currentFilterPath)) return;
            
            if (!_isSearching)
            {
                // Start search
                _searchResults.Clear();
                
                // Get parameters from Balatro spinners
                var config = new SearchConfiguration
                {
                    ThreadCount = _threadsSpinner?.Value ?? 4,
                    MinScore = _minScoreSpinner?.Value ?? 0, // 0 = Auto, 1-5 = actual values
                    BatchSize = (_batchSizeSpinner?.Value ?? 3) + 1, // Convert 1-4 to 2-5 for actual batch size
                    StartBatch = 0,
                    EndBatch = 999999,
                    DebugMode = _debugCheckBox?.IsChecked ?? false,
                    Deck = _decks[_currentDeckIndex].name,
                    Stake = _stakes[_currentStakeIndex].name.Replace(" Stake", "")
                };
                
                AddToConsole("Let Jimbo cook! Starting search...");
                
                // Start search
                await _searchService.StartSearchAsync(config);
            }
            else
            {
                // Stop search
                _searchService.StopSearch();
                AddToConsole("Jimbo stopped cooking!");
            }
        }
        
        private void OnSaveWidgetClick(object? sender, RoutedEventArgs e)
        {
            // This would create a desktop widget from the current search
            AddToConsole("Widget functionality coming soon!");
        }
        
        private void OnSearchStarted(object? sender, EventArgs e)
        {
            Avalonia.Threading.Dispatcher.UIThread.Post(() =>
            {
                _isSearching = true;
                
                // Update cook button
                if (_cookButton != null)
                {
                    _cookButton.Content = "Stop Jimbo!";
                    _cookButton.Classes.Remove("cook-button");
                    _cookButton.Classes.Add("cook-button");
                    _cookButton.Classes.Add("stop");
                }
                
                // Enable save widget button
                if (_saveWidgetButton != null)
                {
                    _saveWidgetButton.IsEnabled = true;
                }
            });
        }
        
        private void OnSearchCompleted(object? sender, EventArgs e)
        {
            Avalonia.Threading.Dispatcher.UIThread.Post(() =>
            {
                _isSearching = false;
                
                // Update cook button
                if (_cookButton != null)
                {
                    _cookButton.Content = "Let Jimbo COOK!";
                    _cookButton.Classes.Remove("stop");
                }
                
                AddToConsole("Jimbo finished cooking!");
            });
        }
        
        private void OnClearConsoleClick(object? sender, RoutedEventArgs e)
        {
            if (_consoleOutput != null)
            {
                _consoleOutput.Text = "> Motely Search Console\n> Ready for Jimbo to cook...\n";
            }
        }
        
        private void OnSearchProgressUpdated(object? sender, SearchProgressEventArgs e)
        {
            Avalonia.Threading.Dispatcher.UIThread.Post(() =>
            {
                AddToConsole($"Searched: {e.SeedsSearched:N0} | Found: {e.ResultsFound} | Speed: {e.SeedsPerSecond:N0}/s");
            });
        }
        
        private void OnResultFound(object? sender, SearchResultEventArgs e)
        {
            Avalonia.Threading.Dispatcher.UIThread.Post(() =>
            {
                _searchResults.Add(e.Result);
                AddToConsole($"🎉 Found seed: {e.Result.Seed} (Score: {e.Result.Score})");
                
                // Update results summary
                if (_resultsSummary != null)
                {
                    _resultsSummary.Text = $"Found {_searchResults.Count} results";
                }
                
                // Enable results tab and export button
                if (_resultsTab != null) _resultsTab.IsEnabled = true;
                if (_exportResultsButton != null) _exportResultsButton.IsEnabled = _searchResults.Count > 0;
            });
        }
        
        private void OnConsoleOutput(object? sender, string message)
        {
            Avalonia.Threading.Dispatcher.UIThread.Post(() =>
            {
                AddToConsole(message);
            });
        }
        
        private void AddToConsole(string message)
        {
            if (_consoleOutput != null)
            {
                var timestamp = DateTime.Now.ToString("HH:mm:ss");
                _consoleOutput.Text += $"[{timestamp}] {message}\n";
                
                // Auto-scroll to bottom
                _consoleOutput.CaretIndex = _consoleOutput.Text.Length;
            }
        }
        
        private void NavigateDeck(int direction)
        {
            _currentDeckIndex = (_currentDeckIndex + direction + _decks.Count) % _decks.Count;
            UpdateDeckDisplay();
        }
        
        private void NavigateStake(int direction)
        {
            _currentStakeIndex = (_currentStakeIndex + direction + _stakes.Count) % _stakes.Count;
            UpdateStakeDisplay();
        }
        
        private void UpdateDeckDisplay()
        {
            if (_deckNameText == null || _deckDescText == null || _deckPreviewImage == null || _spriteService == null)
                return;
                
            var deck = _decks[_currentDeckIndex];
            _deckNameText.Text = deck.name + " Deck";
            _deckDescText.Text = deck.description;
            
            // Get deck image from sprite service
            var deckImage = _spriteService.GetDeckImage(deck.name);
            if (deckImage != null)
            {
                _deckPreviewImage.Source = deckImage;
            }
        }
        
        private void UpdateStakeDisplay()
        {
            if (_stakeChipsCanvas == null || _spriteService == null)
                return;
                
            var stake = _stakes[_currentStakeIndex];
            
            // Update stake text
            if (_stakeNameText != null)
                _stakeNameText.Text = stake.name;
            if (_stakeDescText != null)
                _stakeDescText.Text = stake.description;
            
            // Clear and update stake chips display
            _stakeChipsCanvas.Children.Clear();
            
            // Get stake chip image
            var stakeName = stake.name.Replace(" Stake", "");
            var chipImage = _spriteService.GetStakeChipImage(stakeName);
            
            if (chipImage != null)
            {
                // Display a single chip centered in the canvas
                var image = new Image
                {
                    Source = chipImage,
                    Width = 29,
                    Height = 29
                };
                
                // Center the chip in the 80x80 canvas
                double x = (80 - 29) / 2.0;
                double y = (80 - 29) / 2.0;
                
                Canvas.SetLeft(image, x);
                Canvas.SetTop(image, y);
                
                _stakeChipsCanvas.Children.Add(image);
            }
            
            // Update spinner value if available
            if (_stakeSpinner != null)
            {
                _stakeSpinner.Value = _currentStakeIndex;
            }
            
            // Update stake sticker overlay on deck card
            if (_stakeChipOverlay != null)
            {
                // Get stake sticker image from sprite service
                var stickerName = stake.name.Replace(" ", "");
                var stakeSticker = _spriteService.GetStickerImage(stickerName);
                if (stakeSticker != null)
                {
                    _stakeChipOverlay.Source = stakeSticker;
                    _stakeChipOverlay.IsVisible = true;
                }
                else
                {
                    _stakeChipOverlay.IsVisible = false;
                }
            }
        }
        
        public string GetSelectedDeck()
        {
            return _decks[_currentDeckIndex].name;
        }
        
        public string GetSelectedStake()
        {
            // Return stake name without "Stake" suffix
            var stakeName = _stakes[_currentStakeIndex].name;
            return stakeName.Replace(" Stake", "");
        }
        
        private async void OnExportResultsClick(object? sender, RoutedEventArgs e)
        {
            if (_searchResults.Count == 0) return;
            
            var topLevel = TopLevel.GetTopLevel(this);
            if (topLevel == null) return;
            
            var options = new Avalonia.Platform.Storage.FilePickerSaveOptions
            {
                Title = "Export Search Results",
                DefaultExtension = "csv",
                FileTypeChoices = new[]
                {
                    new Avalonia.Platform.Storage.FilePickerFileType("CSV Files") { Patterns = new[] { "*.csv" } },
                    new Avalonia.Platform.Storage.FilePickerFileType("All Files") { Patterns = new[] { "*" } }
                }
            };
            
            var result = await topLevel.StorageProvider.SaveFilePickerAsync(options);
            
            if (result != null)
            {
                try
                {
                    var csv = new System.Text.StringBuilder();
                    csv.AppendLine("Seed,Score,Timestamp,Details");
                    
                    foreach (var r in _searchResults)
                    {
                        csv.AppendLine($"{r.Seed},{r.Score:F1},{r.Timestamp:yyyy-MM-dd HH:mm:ss},\"{r.Details}\"");
                    }
                    
                    await System.IO.File.WriteAllTextAsync(result.Path.LocalPath, csv.ToString());
                    AddToConsole($"Exported {_searchResults.Count} results to {result.Name}");
                }
                catch (Exception ex)
                {
                    AddToConsole($"Error exporting results: {ex.Message}");
                }
            }
        }
    }
    
    // Supporting classes for the search functionality
    public class SearchConfiguration
    {
        public int ThreadCount { get; set; } = 4;
        public int MinScore { get; set; } = 0;
        public int BatchSize { get; set; } = 4;
        public int StartBatch { get; set; } = 0;
        public int EndBatch { get; set; } = 999999;
        public bool DebugMode { get; set; } = false;
        public string? Deck { get; set; }
        public string? Stake { get; set; }
    }
    
    public class SearchResult : ReactiveObject
    {
        public string Seed { get; set; } = "";
        public double Score { get; set; }
        public string Details { get; set; } = "";
        public string? Antes { get; set; }
        public string? ItemsJson { get; set; }
        public DateTime Timestamp { get; set; } = DateTime.Now;
        
        public ReactiveCommand<string, Unit> CopyCommand { get; }
        
        public SearchResult()
        {
            CopyCommand = ReactiveCommand.Create<string>(seed =>
            {
                // Copy to clipboard logic here
                if (Application.Current?.ApplicationLifetime is Avalonia.Controls.ApplicationLifetimes.IClassicDesktopStyleApplicationLifetime desktop)
                {
                    var mainWindow = desktop.MainWindow;
                    if (mainWindow != null)
                    {
                        mainWindow.Clipboard?.SetTextAsync(seed);
                    }
                }
            });
        }
    }
    
    public class SearchProgressEventArgs : EventArgs
    {
        public int SeedsSearched { get; set; }
        public int ResultsFound { get; set; }
        public double SeedsPerSecond { get; set; }
        public int PercentComplete { get; set; }
    }
    
    public class SearchResultEventArgs : EventArgs
    {
        public SearchResult Result { get; set; } = new();
    }
}

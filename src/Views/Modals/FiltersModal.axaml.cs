using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Input;
using Oracle.Constants;
using Oracle.Helpers;
using Oracle.Models;
using Oracle.Components;
using Oracle.Controls;
using Oracle.Services;
using Avalonia.Markup.Xaml;
using System.IO;
using Avalonia.Threading;
using Avalonia.Platform.Storage;
using Avalonia.VisualTree;
using Motely;

namespace Oracle.Views.Modals;

public partial class FiltersModalContent : UserControl
{
    
    private readonly Dictionary<string, List<string>> _itemCategories;
    private readonly HashSet<string> _selectedNeeds = new();
    private readonly HashSet<string> _selectedWants = new();
    private string _currentCategory = "Jokers";
    private string _searchFilter = "";
    private TextBox? _searchBox;
    private ScrollViewer? _mainScrollViewer;
    private Dictionary<string, TextBlock> _sectionHeaders = new();
    private string _currentActiveTab = "SoulJokersTab";
    private TextBox? _configNameBox;
    private TextBox? _configDescriptionBox;
    private TextBox? _jsonTextBox;
    private TextBlock? _statusText;
    private string? _currentFilePath;


    public FiltersModalContent()
    {
        // SpriteService initializes lazily via Instance property
        InitializeComponent();
        Oracle.Helpers.DebugLogger.Log("FiltersModal", "FiltersModalContent constructor called");
        
        // Initialize item categories from BalatroData
        _itemCategories = new Dictionary<string, List<string>>
        {
            ["Jokers"] = BalatroData.Jokers.Keys.ToList(),
            ["Tarots"] = BalatroData.TarotCards.Keys.ToList(),
            ["Spectrals"] = BalatroData.SpectralCards.Keys.ToList(),
            ["Vouchers"] = BalatroData.Vouchers.Keys.ToList(),
            ["Tags"] = BalatroData.Tags.Keys.ToList(),
            ["Favorites"] = FavoritesService.Instance.GetFavoriteItems() // Load persistent favorites
        };
        
        SetupControls();
        LoadAllCategories();
    }
    
    private void InitializeComponent()
    {
        Oracle.Helpers.DebugLogger.Log("FiltersModal", "FiltersModalContent InitializeComponent called");
        AvaloniaXamlLoader.Load(this);
    }
    
    private void SetupControls()
    {
        Oracle.Helpers.DebugLogger.Log("FiltersModal", "SetupControls called");
        
        // Get metadata controls
        _configNameBox = this.FindControl<TextBox>("ConfigNameBox");
        _configDescriptionBox = this.FindControl<TextBox>("ConfigDescriptionBox");
        
        // Setup tab button handlers
        SetupTabButtons();
        
        // Setup drop zones
        SetupDropZones();
        
        // Setup search functionality
        SetupSearchBox();
    }
    
    private void SetupTabButtons()
    {
        // Removed search tab - now integrated into main area
        
        var editJsonTab = this.FindControl<Button>("EditJsonTab");
        editJsonTab?.AddHandler(Button.ClickEvent, (s, e) => EnterEditJsonMode());
        
        // Favorites tab
        var favoritesTab = this.FindControl<Button>("FavoritesTab");
        favoritesTab?.AddHandler(Button.ClickEvent, (s, e) => ShowFavorites());
        
        // Joker category tabs
        var soulJokersTab = this.FindControl<Button>("SoulJokersTab");
        var rareJokersTab = this.FindControl<Button>("RareJokersTab");
        var uncommonJokersTab = this.FindControl<Button>("UncommonJokersTab");
        var commonJokersTab = this.FindControl<Button>("CommonJokersTab");
        
        soulJokersTab?.AddHandler(Button.ClickEvent, (s, e) => NavigateToSection("SoulJokersTab"));
        rareJokersTab?.AddHandler(Button.ClickEvent, (s, e) => NavigateToSection("RareJokersTab"));
        uncommonJokersTab?.AddHandler(Button.ClickEvent, (s, e) => NavigateToSection("UncommonJokersTab"));
        commonJokersTab?.AddHandler(Button.ClickEvent, (s, e) => NavigateToSection("CommonJokersTab"));
        
        // Other category tabs
        var vouchersTab = this.FindControl<Button>("VouchersTab");
        var tarotsTab = this.FindControl<Button>("TarotsTab");
        var spectralsTab = this.FindControl<Button>("SpectralsTab");
        var tagsTab = this.FindControl<Button>("TagsTab");
        var clearTab = this.FindControl<Button>("ClearTab");
        
        vouchersTab?.AddHandler(Button.ClickEvent, (s, e) => NavigateToSection("VouchersTab"));
        tarotsTab?.AddHandler(Button.ClickEvent, (s, e) => NavigateToSection("TarotsTab"));
        spectralsTab?.AddHandler(Button.ClickEvent, (s, e) => NavigateToSection("SpectralsTab"));
        tagsTab?.AddHandler(Button.ClickEvent, (s, e) => NavigateToSection("TagsTab"));
        clearTab?.AddHandler(Button.ClickEvent, (s, e) => { ClearNeeds(); ClearWants(); });
        
        // Setup other button handlers
        var saveButton = this.FindControl<Button>("SaveButton");
        var loadButton = this.FindControl<Button>("LoadButton");
        var clearAllButton = this.FindControl<Button>("ClearAllButton");
        
        saveButton?.AddHandler(Button.ClickEvent, OnSaveClick);
        loadButton?.AddHandler(Button.ClickEvent, OnLoadClick);
        clearAllButton?.AddHandler(Button.ClickEvent, (s, e) => { ClearNeeds(); ClearWants(); });
        
        var searchButton = this.FindControl<Button>("SearchButton");
        searchButton?.AddHandler(Button.ClickEvent, OnLaunchSearchClick);
    }
    
    private void SetupDropZones()
    {
        Oracle.Helpers.DebugLogger.Log("FiltersModal", "Setting up drop zones...");
        
        // Get the drop zone borders (not just the panels)
        var needsBorder = this.FindControl<Border>("NeedsBorder");
        var wantsBorder = this.FindControl<Border>("WantsBorder");
        
        // Get the actual panels inside the borders
        var needsPanel = this.FindControl<WrapPanel>("NeedsPanel");
        var wantsPanel = this.FindControl<WrapPanel>("WantsPanel");
        
        // Setup drop zones on the borders (larger drop targets)
        if (needsBorder != null)
        {
            DragDrop.SetAllowDrop(needsBorder, true);
            needsBorder.AddHandler(DragDrop.DropEvent, OnNeedsPanelDrop);
            needsBorder.AddHandler(DragDrop.DragOverEvent, OnNeedsDragOver);
            needsBorder.AddHandler(DragDrop.DragEnterEvent, OnNeedsDragEnter);
            needsBorder.AddHandler(DragDrop.DragLeaveEvent, OnNeedsDragLeave);
        }
        
        if (wantsBorder != null)
        {
            DragDrop.SetAllowDrop(wantsBorder, true);
            wantsBorder.AddHandler(DragDrop.DropEvent, OnWantsPanelDrop);
            wantsBorder.AddHandler(DragDrop.DragOverEvent, OnWantsDragOver);
            wantsBorder.AddHandler(DragDrop.DragEnterEvent, OnWantsDragEnter);
            wantsBorder.AddHandler(DragDrop.DragLeaveEvent, OnWantsDragLeave);
        }
        
        // Setup clear buttons (now integrated in the headers)
        var clearNeedsButton = this.FindControl<Button>("ClearNeedsButton");
        var clearWantsButton = this.FindControl<Button>("ClearWantsButton");
        
        if (clearNeedsButton != null)
            clearNeedsButton.Click += (s, e) => ClearNeeds();
        if (clearWantsButton != null)
            clearWantsButton.Click += (s, e) => ClearWants();
            
        // Setup mode toggle buttons
        var dragDropModeButton = this.FindControl<Border>("DragDropModeButton");
        var jsonEditorModeButton = this.FindControl<Border>("JsonEditorModeButton");
        
        if (dragDropModeButton != null)
            dragDropModeButton.PointerPressed += (s, e) => {
                // Switch to drag-drop mode
                dragDropModeButton.Classes.Remove("mode-inactive");
                dragDropModeButton.Classes.Add("mode-active");
                jsonEditorModeButton?.Classes.Remove("mode-active");
                jsonEditorModeButton?.Classes.Add("mode-inactive");
                
                // Restore visibility of hidden elements
                RestoreDragDropModeLayout();
                
                LoadCategory(_currentCategory);
            };
        if (jsonEditorModeButton != null)
            jsonEditorModeButton.PointerPressed += (s, e) => {
                // Switch to JSON editor mode
                jsonEditorModeButton.Classes.Remove("mode-inactive");
                jsonEditorModeButton.Classes.Add("mode-active");
                dragDropModeButton?.Classes.Remove("mode-active");
                dragDropModeButton?.Classes.Add("mode-inactive");
                EnterEditJsonMode();
            };
        
        Oracle.Helpers.DebugLogger.Log("FiltersModal", "Drop zones setup complete!");
    }
    
    // 🎯 Enhanced Drag & Drop Event Handlers
    
    private void OnNeedsDragEnter(object? sender, DragEventArgs e)
    {
        if (e.Data.Contains("balatro-item"))
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
        if (e.Data.Contains("balatro-item"))
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
        if (e.Data.Contains("balatro-item"))
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
        if (e.Data.Contains("balatro-item"))
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
                    var key = $"{category}:{itemName}";
                    
                    // Move from wants to needs
                    _selectedWants.Remove(key);
                    _selectedNeeds.Add(key);
                    
                    UpdateDropZoneVisibility();
                    UpdatePersistentFavorites();
                    RefreshItemPalette();
                    
                    Oracle.Helpers.DebugLogger.Log("FiltersModal", $"✅ Added {itemName} to NEEDS");
                }
            }
        }
        e.Handled = true;
    }
    
    private void OnWantsPanelDrop(object? sender, DragEventArgs e)
    {
        // Remove visual feedback
        var wantsBorder = sender as Border;
        wantsBorder?.Classes.Remove("drag-over");
        
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
                    var key = $"{category}:{itemName}";
                    
                    // Move from needs to wants
                    _selectedNeeds.Remove(key);
                    _selectedWants.Add(key);
                    
                    UpdateDropZoneVisibility();
                    UpdatePersistentFavorites();
                    RefreshItemPalette();
                    
                    Oracle.Helpers.DebugLogger.Log("FiltersModal", $"✅ Added {itemName} to WANTS");
                }
            }
        }
        e.Handled = true;
    }
    
    private void RestoreDragDropModeLayout()
    {
        try
        {
            Oracle.Helpers.DebugLogger.Log("FiltersModal", "RestoreDragDropModeLayout: Starting restoration");
            
            // Find the root grid first
            var rootGrid = this.FindControl<Grid>("RootGrid");
            if (rootGrid == null)
            {
                Oracle.Helpers.DebugLogger.Log("FiltersModal", "RestoreDragDropModeLayout: Could not find RootGrid");
                return;
            }
            
            // Navigate to the main content area (Grid.Row="1")
            var mainContentArea = rootGrid.Children.OfType<Grid>()
                .FirstOrDefault(g => Grid.GetRow(g) == 1);
            
            if (mainContentArea == null)
            {
                Oracle.Helpers.DebugLogger.Log("FiltersModal", "RestoreDragDropModeLayout: Could not find main content area");
                return;
            }
            
            Oracle.Helpers.DebugLogger.Log("FiltersModal", $"RestoreDragDropModeLayout: Found main content area with {mainContentArea.ColumnDefinitions.Count} columns");
            
            // This should be the grid with columns "160, 12, *"
            if (mainContentArea.ColumnDefinitions.Count == 3)
            {
                // Restore the left sidebar (Column 0)
                var leftSidebar = mainContentArea.Children.OfType<Border>()
                    .FirstOrDefault(b => Grid.GetColumn(b) == 0);
                if (leftSidebar != null)
                {
                    Oracle.Helpers.DebugLogger.Log("RestoreDragDropModeLayout: Found left sidebar, making visible");
                    leftSidebar.IsVisible = true;
                }
                
                // Find the item palette grid (Column 2)
                var rightContentGrid = mainContentArea.Children.OfType<Grid>()
                    .FirstOrDefault(g => Grid.GetColumn(g) == 2);
                    
                if (rightContentGrid != null)
                {
                    Oracle.Helpers.DebugLogger.Log("RestoreDragDropModeLayout: Found right content grid");
                    
                    // Find the left side (item palette) within the right content grid
                    var itemPaletteGrid = rightContentGrid.Children.OfType<Grid>()
                        .FirstOrDefault(g => Grid.GetColumn(g) == 0);
                        
                    if (itemPaletteGrid != null)
                    {
                        // Find and show the search bar container
                        var searchBarContainer = itemPaletteGrid.Children.OfType<Border>()
                            .FirstOrDefault(b => Grid.GetRow(b) == 0);
                        if (searchBarContainer != null)
                        {
                            Oracle.Helpers.DebugLogger.Log("RestoreDragDropModeLayout: Found search bar container, making visible");
                            searchBarContainer.IsVisible = true;
                        }
                        
                        // Show the item palette container
                        var itemPaletteContainer = itemPaletteGrid.Children.OfType<Border>()
                            .FirstOrDefault(b => Grid.GetRow(b) == 1);
                        if (itemPaletteContainer != null)
                        {
                            Oracle.Helpers.DebugLogger.Log("RestoreDragDropModeLayout: Found item palette container, making visible");
                            itemPaletteContainer.IsVisible = true;
                        }
                    }
                    
                    // Find the drop zones grid (Column 2 of the right content grid)
                    var dropZonesGrid = rightContentGrid.Children.OfType<Grid>()
                        .FirstOrDefault(g => Grid.GetColumn(g) == 2);
                        
                    if (dropZonesGrid != null)
                    {
                        Oracle.Helpers.DebugLogger.Log("RestoreDragDropModeLayout: Found drop zones grid, making visible");
                        dropZonesGrid.IsVisible = true;
                        
                        // Also make sure the individual drop zones are visible
                        var needsBorder = dropZonesGrid.Children.OfType<Border>()
                            .FirstOrDefault(b => b.Name == "NeedsBorder");
                        var wantsBorder = dropZonesGrid.Children.OfType<Border>()
                            .FirstOrDefault(b => b.Name == "WantsBorder");
                            
                        if (needsBorder != null)
                        {
                            Oracle.Helpers.DebugLogger.Log("RestoreDragDropModeLayout: Found NeedsBorder, making visible");
                            needsBorder.IsVisible = true;
                        }
                        
                        if (wantsBorder != null)
                        {
                            Oracle.Helpers.DebugLogger.Log("RestoreDragDropModeLayout: Found WantsBorder, making visible");
                            wantsBorder.IsVisible = true;
                        }
                    }
                    
                    // Restore the column widths for the right content grid
                    if (rightContentGrid.ColumnDefinitions.Count == 3)
                    {
                        Oracle.Helpers.DebugLogger.Log("RestoreDragDropModeLayout: Restoring right content grid column widths");
                        rightContentGrid.ColumnDefinitions[0].Width = new GridLength(1, GridUnitType.Star);
                        rightContentGrid.ColumnDefinitions[1].Width = new GridLength(15);
                        rightContentGrid.ColumnDefinitions[2].Width = new GridLength(1, GridUnitType.Star);
                    }
                }
                
                // Restore main content area column widths
                Oracle.Helpers.DebugLogger.Log("RestoreDragDropModeLayout: Restoring main content area column widths");
                mainContentArea.ColumnDefinitions[0].Width = new GridLength(160);
                mainContentArea.ColumnDefinitions[1].Width = new GridLength(12);
                mainContentArea.ColumnDefinitions[2].Width = new GridLength(1, GridUnitType.Star);
            }
            
            Oracle.Helpers.DebugLogger.Log("RestoreDragDropModeLayout: Restoration complete");
        }
        catch (Exception ex)
        {
            Oracle.Helpers.DebugLogger.LogError("FiltersModal", $"ERROR in RestoreDragDropModeLayout: {ex}");
        }
    }
    
    private void EnterEditJsonMode()
    {
        try
        {
            Oracle.Helpers.DebugLogger.Log("FiltersModal", "EnterEditJsonMode called");
            
            // Hide the left sidebar
            var leftSidebar = this.GetVisualDescendants().OfType<Border>()
                .FirstOrDefault(b => Grid.GetColumn(b) == 0 && b.Background?.ToString() == "#FF2A2A2A");
            if (leftSidebar != null)
            {
                leftSidebar.IsVisible = false;
                Oracle.Helpers.DebugLogger.Log("Hidden left sidebar");
            }
            
            // Hide the search bar
            var searchBox = this.FindControl<TextBox>("SearchBox");
            var searchBar = searchBox?.Parent?.Parent as Border;
            if (searchBar != null)
            {
                searchBar.IsVisible = false;
                Oracle.Helpers.DebugLogger.Log("Hidden search bar");
            }
            
            // Hide the drop zones (NEEDS and WANTS panels)
            var needsBorder = this.FindControl<Border>("NeedsBorder");
            var wantsBorder = this.FindControl<Border>("WantsBorder");
            if (needsBorder != null) needsBorder.IsVisible = false;
            if (wantsBorder != null) wantsBorder.IsVisible = false;
            Oracle.Helpers.DebugLogger.Log("Hidden drop zones");
            
            // Find the main content area that contains the columns
            var mainContentArea = this.GetVisualDescendants().OfType<Grid>()
                .FirstOrDefault(g => g.ColumnDefinitions.Count == 3 && g.ColumnDefinitions[0].Width.Value == 160);
            
            if (mainContentArea != null)
            {
                // Hide only column 0 (sidebar) - don't change column widths yet
                var sidebarInColumn0 = mainContentArea.Children
                    .OfType<Border>()
                    .FirstOrDefault(b => Grid.GetColumn(b) == 0);
                if (sidebarInColumn0 != null)
                {
                    sidebarInColumn0.IsVisible = false;
                }
                
                // The JSON editor is in the Grid at column 2, which contains both the item palette (col 0) and drop zones (col 2)
                // We need to find the inner grid that has the item palette and drop zones
                var innerContentGrid = mainContentArea.Children
                    .OfType<Grid>()
                    .FirstOrDefault(g => Grid.GetColumn(g) == 2 && g.ColumnDefinitions.Count == 3);
                    
                if (innerContentGrid != null)
                {
                    // Hide the drop zones grid (column 2 of inner grid)
                    var dropZonesGrid = innerContentGrid.Children
                        .OfType<Grid>()
                        .FirstOrDefault(g => Grid.GetColumn(g) == 2);
                    if (dropZonesGrid != null)
                    {
                        dropZonesGrid.IsVisible = false;
                    }
                    
                    // Make the item palette column take full width
                    innerContentGrid.ColumnDefinitions[0].Width = new GridLength(1, GridUnitType.Star);
                    innerContentGrid.ColumnDefinitions[1].Width = new GridLength(0);
                    innerContentGrid.ColumnDefinitions[2].Width = new GridLength(0);
                }
                
                // Also hide the left sidebar by adjusting outer grid
                mainContentArea.ColumnDefinitions[0].Width = new GridLength(0);
                mainContentArea.ColumnDefinitions[1].Width = new GridLength(0);
                Oracle.Helpers.DebugLogger.Log("Adjusted layout for JSON editor");
            }
            
            var container = this.FindControl<ContentControl>("ItemPaletteContent");
            if (container == null) 
            {
                Oracle.Helpers.DebugLogger.LogError("ItemPaletteContent not found!");
                return;
            }

            // Update tab appearance
            Oracle.Helpers.DebugLogger.Log("Updating tab buttons");
            UpdateTabButtons("EditJson");

            // Create JSON editor interface
            Oracle.Helpers.DebugLogger.Log("Creating edit JSON interface");
            var editJsonInterface = CreateEditJsonInterface();
            Oracle.Helpers.DebugLogger.Log("Adding interface to container");
            container.Content = editJsonInterface;
            Oracle.Helpers.DebugLogger.Log("JSON editor interface added successfully");
            
            Oracle.Helpers.DebugLogger.Log("JSON editor mode entered");
        }
        catch (Exception ex)
        {
            Oracle.Helpers.DebugLogger.LogError("FiltersModal", $"ERROR in EnterEditJsonMode: {ex}");
        }
    }

    private Control CreateEditJsonInterface()
    {
        try
        {
            Oracle.Helpers.DebugLogger.Log("FiltersModal", "CreateEditJsonInterface started");
            
            // Get the current JSON content - either from selections or default
            string jsonContent;
            if (_selectedNeeds.Any() || _selectedWants.Any())
            {
                // Build JSON from current selections
                var config = BuildConfigFromSelections();
                jsonContent = JsonSerializer.Serialize(config, new JsonSerializerOptions 
                { 
                    WriteIndented = true,
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                });
                Oracle.Helpers.DebugLogger.Log("Using JSON built from current selections");
            }
            else
            {
                // Use default example JSON only if no selections exist
                jsonContent = GetDefaultConfigJson();
                Oracle.Helpers.DebugLogger.Log("Using default example JSON (no selections)");
            }
            
            // Create a simple Grid layout
            var mainGrid = new Grid
            {
                VerticalAlignment = Avalonia.Layout.VerticalAlignment.Stretch,
                HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Stretch,
                Background = new SolidColorBrush(Color.Parse("#1e1e1e"))
                // Remove MinHeight to let it use all available space
            };
            
            mainGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
            mainGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            
            // Check if we should use fallback mode
            bool useFallback = false; // Now that AvaloniaEdit is properly configured, we can use it!

            Control editorControl;
            
            if (useFallback)
            {
                // Use a simple TextBox as fallback
                Oracle.Helpers.DebugLogger.Log("Using fallback TextBox for JSON editing");
                var fallbackTextBox = new TextBox
                {
                    Name = "JsonTextEditor_Fallback",
                    Text = jsonContent,
                    AcceptsReturn = true,
                    TextWrapping = TextWrapping.NoWrap,
                    Background = new SolidColorBrush(Color.Parse("#1e1e1e")),
                    Foreground = new SolidColorBrush(Color.Parse("#FFFFFF")),
                    FontFamily = new FontFamily("Cascadia Code,Consolas,Menlo,Monaco,monospace"),
                    FontSize = 14,
                    Padding = new Thickness(10),
                    VerticalAlignment = Avalonia.Layout.VerticalAlignment.Stretch,
                    HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Stretch,
                    MinHeight = 200
                };
                
                // Store reference for compatibility
                _jsonTextBox = fallbackTextBox;
                
                // Wrap TextBox in ScrollViewer for scrolling
                var scrollViewer = new ScrollViewer
                {
                    HorizontalScrollBarVisibility = Avalonia.Controls.Primitives.ScrollBarVisibility.Auto,
                    VerticalScrollBarVisibility = Avalonia.Controls.Primitives.ScrollBarVisibility.Visible,
                    AllowAutoHide = false,
                    Content = fallbackTextBox,
                    VerticalAlignment = Avalonia.Layout.VerticalAlignment.Stretch,
                    HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Stretch
                };
                
                var editorBorder = new Border
                {
                    Background = new SolidColorBrush(Color.Parse("#1e1e1e")),
                    BorderBrush = new SolidColorBrush(Color.Parse("#444444")),
                    BorderThickness = new Thickness(1),
                    CornerRadius = new CornerRadius(4),
                    VerticalAlignment = Avalonia.Layout.VerticalAlignment.Stretch,
                    HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Stretch,
                    Child = scrollViewer
                };
                
                editorControl = editorBorder;
            }
            else
            {
                // Create an AvaloniaEdit TextEditor for JSON editing
                Oracle.Helpers.DebugLogger.Log("Creating AvaloniaEdit JSON editor");
                var editorBorder = new Border
                {
                    Background = new SolidColorBrush(Color.Parse("#1e1e1e")),
                    BorderBrush = new SolidColorBrush(Color.Parse("#444444")),
                    BorderThickness = new Thickness(1),
                    CornerRadius = new CornerRadius(4),
                    Padding = new Thickness(10),
                    VerticalAlignment = Avalonia.Layout.VerticalAlignment.Stretch,
                    HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Stretch
                    // Let it stretch to fill available space in the grid
                };
                
                // Create AvaloniaEdit TextEditor
                var textEditor = new AvaloniaEdit.TextEditor
                {
                    Name = "JsonTextEditor",
                    Background = new SolidColorBrush(Color.Parse("#1e1e1e")),
                    Foreground = new SolidColorBrush(Color.Parse("#FFFFFF")),
                    FontFamily = new FontFamily("Cascadia Code,Consolas,Menlo,Monaco,monospace"),
                    FontSize = 14,
                    ShowLineNumbers = true,
                    WordWrap = false,
                    HorizontalScrollBarVisibility = Avalonia.Controls.Primitives.ScrollBarVisibility.Auto,
                    VerticalScrollBarVisibility = Avalonia.Controls.Primitives.ScrollBarVisibility.Visible,
                    VerticalAlignment = Avalonia.Layout.VerticalAlignment.Stretch,
                    HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Stretch,
                    IsVisible = true,
                    // Height will be managed by the container
                    // Don't set Text here - we'll set it after the control is loaded
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
                
                // Add a simple wheel handler that manually scrolls the editor
                textEditor.PointerWheelChanged += (s, e) => {
                    var scrollViewer = textEditor.TextArea?.GetVisualDescendants()?.OfType<ScrollViewer>()?.FirstOrDefault();
                    if (scrollViewer != null)
                    {
                        // Scroll by 3 lines worth (approximately 48 pixels)
                        var delta = e.Delta.Y * 48;
                        scrollViewer.Offset = new Vector(scrollViewer.Offset.X, Math.Max(0, scrollViewer.Offset.Y - delta));
                        e.Handled = true;
                    }
                };
                
                // Force text area colors after the editor is attached
                textEditor.AttachedToVisualTree += (s, e) => {
                    try
                    {
                        // Ensure text is visible
                        textEditor.TextArea.TextView.LinkTextForegroundBrush = new SolidColorBrush(Color.Parse("#569CD6"));
                        
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
                        
                        Oracle.Helpers.DebugLogger.Log($"AvaloniaEdit attached - Document length: {textEditor.Document.TextLength}");
                    }
                    catch (Exception ex)
                    {
                        Oracle.Helpers.DebugLogger.LogError($"Error in AttachedToVisualTree: {ex.Message}");
                    }
                };
                
                editorBorder.Child = textEditor;
                
                // Ensure editor gets focus when clicked
                editorBorder.PointerPressed += (s, e) => {
                    textEditor.Focus();
                };
                
                // Make sure editor is focusable and has proper height
                textEditor.Focusable = true;
                textEditor.Loaded += (s, e) => {
                    textEditor.Focus();
                    Oracle.Helpers.DebugLogger.Log($"JSON Editor loaded. Height={textEditor.Bounds.Height}, Lines={textEditor.Document.LineCount}");
                };
                
                // Store references
                _jsonTextBox = new TextBox { IsVisible = false }; // Create a hidden TextBox to maintain compatibility
                _jsonTextBox.Text = "{}"; // Sync initial text
                
                // Add text changed handler
                textEditor.TextChanged += (s, e) => {
                    // Update the hidden TextBox to maintain compatibility with existing code
                    if (_jsonTextBox != null)
                    {
                        _jsonTextBox.Text = textEditor.Text;
                    }
                    
                    Dispatcher.UIThread.Post(() => {
                        ValidateJsonSyntaxForAvaloniaEdit(textEditor);
                    }, DispatcherPriority.Background);
                };
                
                editorControl = editorBorder;
            }
            
            Grid.SetRow(editorControl, 0);
            mainGrid.Children.Add(editorControl);

            // Status bar at bottom
            Oracle.Helpers.DebugLogger.Log("Creating status bar");
            var statusBar = new Border
            {
                Background = new SolidColorBrush(Color.Parse("#1a1a1a")),
                Padding = new Avalonia.Thickness(12, 6),
                BorderBrush = new SolidColorBrush(Color.Parse("#444444")),
                BorderThickness = new Avalonia.Thickness(0, 1, 0, 0),
                Height = 30
            };
            
            var statusText = new TextBlock 
            { 
                Name = "StatusText", 
                Text = "Ready - JSON Editor", 
                VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center,
                Foreground = new SolidColorBrush(Color.Parse("#E2E2E2")),
                FontSize = 12
            };
            statusBar.Child = statusText;
            Grid.SetRow(statusBar, 1);
            mainGrid.Children.Add(statusBar);
            
            // Store reference
            _statusText = statusText;
            
            // Add text changed handler for fallback TextBox if needed
            if (useFallback && _jsonTextBox != null)
            {
                _jsonTextBox.TextChanged += (s, e) => {
                    Dispatcher.UIThread.Post(() => {
                        ValidateJsonSyntaxForTextBox(_jsonTextBox);
                    }, DispatcherPriority.Background);
                };
            }

            // Format the JSON on load
            Dispatcher.UIThread.Post(() => {
                FormatJson();
            }, DispatcherPriority.Background);
            
            Oracle.Helpers.DebugLogger.Log("CreateEditJsonInterface completed successfully");
            return mainGrid;
        }
        catch (Exception ex)
        {
            Oracle.Helpers.DebugLogger.LogError("FiltersModal", $"ERROR in CreateEditJsonInterface: {ex}");
            // Return a simple error panel
            var errorPanel = new StackPanel
            {
                Background = new SolidColorBrush(Color.Parse("#1e1e1e")),
                Margin = new Thickness(10)
            };
            errorPanel.Children.Add(new TextBlock 
            { 
                Text = $"Error creating JSON editor: {ex.Message}",
                Foreground = Brushes.Red,
                FontSize = 14
            });
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
                ? new SolidColorBrush(Color.Parse("#FF6B6B")) 
                : new SolidColorBrush(Color.Parse("#4ECDC4"));
        }
    }

    private bool ValidateJsonSyntax()
    {
        // Find the AvaloniaEdit TextEditor in the UI
        var textEditor = this.FindControl<AvaloniaEdit.TextEditor>("JsonTextEditor");
        if (textEditor != null)
        {
            ValidateJsonSyntaxForAvaloniaEdit(textEditor);
            return true; // We don't know the result from the validation
        }
        
        // Fallback to the old TextBox if AvaloniaEdit is not found
        if (_jsonTextBox != null)
        {
            ValidateJsonSyntaxForTextBox(_jsonTextBox);
            return true; // We don't know the result from the validation
        }
        
        return false;
    }

    private int GetLineNumberFromException(JsonException ex)
    {
        // Try to extract line number from exception message or path
        if (ex.LineNumber.HasValue)
            return (int)ex.LineNumber.Value + 1; // Convert to 1-based
        return 0;
    }

    private void NewConfig()
    {
        if (_jsonTextBox != null)
        {
            _jsonTextBox.Text = GetDefaultConfigJson();
            _currentFilePath = null;
            
            // Auto-format the new config
            Dispatcher.UIThread.Post(() => {
                FormatJson();
                UpdateStatus("✨ New config created with example structure");
            }, DispatcherPriority.Background);
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

            var files = await topLevel.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
            {
                Title = "Load Balatro Config",
                AllowMultiple = false,
                FileTypeFilter = new[]
                {
                    new FilePickerFileType("Ouija Config Files")
                    {
                        Patterns = new[] { "*.ouija.json" }
                    },
                    new FilePickerFileType("JSON Files")
                    {
                        Patterns = new[] { "*.json" }
                    },
                    new FilePickerFileType("All Files")
                    {
                        Patterns = new[] { "*.*" }
                    }
                }
            });

            if (files.Count > 0)
            {
                var file = files[0];
                try
                {
                    var content = await File.ReadAllTextAsync(file.Path.LocalPath);
                    
                    // Validate before loading
                    using var testDoc = JsonDocument.Parse(content);
                    
                    // Update AvaloniaEdit if available
                    var textEditor = this.FindControl<AvaloniaEdit.TextEditor>("JsonTextEditor");
                    if (textEditor != null)
                    {
                        Oracle.Helpers.DebugLogger.Log($"Setting TextEditor text, length: {content.Length}");
                        textEditor.Text = content;
                        textEditor.IsVisible = true; // Ensure visibility
                    }
                    else
                    {
                        Oracle.Helpers.DebugLogger.Log("FiltersModal", "WARNING: JsonTextEditor not found in LoadConfig");
                    }
                    
                    // Also update the hidden TextBox for compatibility
                    if (_jsonTextBox != null)
                    {
                        _jsonTextBox.Text = content;
                    }
                    
                    _currentFilePath = file.Path.LocalPath;
                    
                    // Format the JSON for better readability
                    Dispatcher.UIThread.Post(() => FormatJson(), DispatcherPriority.Background);
                    
                    UpdateStatus($"✓ Loaded: {Path.GetFileName(file.Path.LocalPath)}");
                    
                    // Update the search widget to use this loaded config
                    UpdateSearchWidgetConfig(file.Path.LocalPath);
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
        }
        catch (Exception ex)
        {
            UpdateStatus($"❌ File picker error: {ex.Message}", isError: true);
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
        if (topLevel == null) return;

        var file = await topLevel.StorageProvider.SaveFilePickerAsync(new Avalonia.Platform.Storage.FilePickerSaveOptions
        {
            Title = "Save Balatro Config",
            SuggestedFileName = _currentFilePath != null ? System.IO.Path.GetFileName(_currentFilePath) : "new-config.ouija.json",
            DefaultExtension = "ouija.json",
            FileTypeChoices = new[]
            {
                new Avalonia.Platform.Storage.FilePickerFileType("Ouija Config Files")
                {
                    Patterns = new[] { "*.ouija.json" }
                }
            }
        });

        if (file != null)
        {
            try
            {
                // Get text from AvaloniaEdit if available, otherwise use the TextBox
                string jsonText;
                var textEditor = this.FindControl<AvaloniaEdit.TextEditor>("JsonTextEditor");
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
                
                // Update the search widget to use this saved config
                UpdateSearchWidgetConfig(file.Path.LocalPath);
            }
            catch (Exception ex)
            {
                UpdateStatus($"Error saving file: {ex.Message}", isError: true);
            }
        }
    }

    private string GetDefaultConfigJson()
    {
        // Create an impressive demo config that showcases the JSON editor
        return @"{
  ""name"": ""Professional Balatro Seed Config"",
  ""description"": ""Comprehensive filter for finding optimal Balatro seeds with specific requirements."",
  ""author"": ""Oracle User"",
  ""version"": ""1.0.0"",
  ""keywords"": [
    ""joker"",
    ""negative"",
    ""voucher"",
    ""tarot"",
    ""spectral""
  ],
  ""filter_config"": {
    ""numNeeds"": 2,
    ""numWants"": 3,
    ""Needs"": [
      {
        ""Type"": ""Jokers"",
        ""Value"": ""Joker"",
        ""SearchAntes"": [1, 2, 3, 4],
        ""Score"": 5
      },
      {
        ""Type"": ""Vouchers"",
        ""Value"": ""Overstock"",
        ""SearchAntes"": [1, 2],
        ""Score"": 3
      }
    ],
    ""Wants"": [
      {
        ""Type"": ""Jokers"",
        ""Value"": ""Blueprint"",
        ""SearchAntes"": [2, 3, 4, 5],
        ""Score"": 2
      },
      {
        ""Type"": ""Tarots"",
        ""Value"": ""The Fool"",
        ""SearchAntes"": [1, 2, 3],
        ""Score"": 1
      },
      {
        ""Type"": ""Tags"",
        ""Value"": ""Negative Tag"",
        ""SearchAntes"": [2, 3, 4],
        ""Score"": 4
      }
    ],
    ""ScoreNaturalNegatives"": true,
    ""ScoreDesiredNegatives"": false,
    ""MinimumScore"": 8,
    ""MaxSearchTime"": 300,
    ""EnableAdvancedFiltering"": true
  },
  ""search_parameters"": {
    ""max_ante"": 8,
    ""min_ante"": 1,
    ""stake_level"": ""Gold"",
    ""deck_type"": ""Red Deck"",
    ""enable_skips"": true,
    ""priority_mode"": ""balanced""
  },
  ""metadata"": {
    ""created_date"": ""2025-01-20"",
    ""last_modified"": ""2025-01-20"",
    ""estimated_seeds_per_hour"": 50000,
    ""compatibility_version"": ""3.1.0""
  }
}";
    }

    private Control CreateSearchInterface()
    {
        var mainPanel = new StackPanel { Spacing = 15 };
        
        // Large search box
        var searchBox = new TextBox
        {
            Name = "SearchModeBox",
            Watermark = "🔍 Type to search all items...",
            FontSize = 16,
            Padding = new Avalonia.Thickness(12, 8),
            Background = new SolidColorBrush(Color.Parse("#1a1a1a")),
            Foreground = Brushes.White,
            Margin = new Avalonia.Thickness(0, 0, 0, 10)
        };
        
        // Search results container
        var resultsContainer = new StackPanel
        {
            Name = "SearchResults"
        };
        
        // Clear search button
        var clearButton = new Button
        {
            Content = "Clear Search",
            Classes = { "oracle-btn", "button-color-yellow-orange" },
            HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center,
            FontSize = 14,
            Padding = new Avalonia.Thickness(16, 8),
            Margin = new Avalonia.Thickness(0, 10, 0, 0)
        };
        
        // Wire up events
        searchBox.TextChanged += OnSearchModeTextChanged;
        clearButton.Click += (s, e) => ExitSearchMode();
        
        mainPanel.Children.Add(searchBox);
        mainPanel.Children.Add(resultsContainer);
        mainPanel.Children.Add(clearButton);
        
        // Focus the search box
        searchBox.Focus();
        
        return mainPanel;
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
        var resultsContainer = itemPaletteContent?.Content is Panel panel ? panel.Children.OfType<StackPanel>().FirstOrDefault(sp => sp.Name == "SearchResults") : null;
        
        if (resultsContainer == null) return;
        
        resultsContainer.Children.Clear();
        
        if (string.IsNullOrEmpty(query) || query.Length < 2)
        {
            var hintText = new TextBlock
            {
                Text = "Type at least 2 characters to search...",
                FontSize = 14,
                Foreground = new SolidColorBrush(Color.Parse("#888888")),
                Background = Brushes.Transparent,
                HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center,
                Margin = new Avalonia.Thickness(0, 20)
            };
            resultsContainer.Children.Add(hintText);
            return;
        }
        
        // Search across all categories
        var allResults = new Dictionary<string, List<string>>();
        
        foreach (var category in _itemCategories)
        {
            if (category.Key == "Favorites") continue; // Skip favorites in search
            
            var matches = category.Value
                .Where(item => item.ToLowerInvariant().Contains(query.ToLowerInvariant()))
                .ToList();
            
            if (matches.Any())
            {
                allResults[category.Key] = matches;
            }
        }
        
        if (!allResults.Any())
        {
            var noResultsText = new TextBlock
            {
                Text = $"No items found matching '{query}'",
                FontSize = 14,
                Foreground = new SolidColorBrush(Color.Parse("#888888")),
                HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center,
                Margin = new Avalonia.Thickness(0, 20)
            };
            resultsContainer.Children.Add(noResultsText);
            return;
        }
        
        // Display results grouped by category
        foreach (var categoryResults in allResults)
        {
            // Category header
            var header = new TextBlock
            {
                Text = $"{categoryResults.Key} ({categoryResults.Value.Count})",
                Classes = { "section-header" },
                Margin = new Avalonia.Thickness(0, 10, 0, 5)
            };
            resultsContainer.Children.Add(header);
            
            // Create grid for items
            var responsiveGrid = new ResponsiveGrid
            {
                Margin = new Avalonia.Thickness(0, 0, 0, 15)
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
        
        // Ensure search box gets focus after the control is fully attached to visual tree
        Dispatcher.UIThread.Post(() => {
            _searchBox?.Focus();
        }, DispatcherPriority.Background);
        
        // Add global drag over handler to track ghost position everywhere
        this.AddHandler(DragDrop.DragOverEvent, OnGlobalDragOver, RoutingStrategies.Tunnel | RoutingStrategies.Bubble);
    }
    
    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnDetachedFromVisualTree(e);
        this.RemoveHandler(DragDrop.DragOverEvent, OnGlobalDragOver);
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

    private void LoadAllCategories()
    {
        // Use ItemPaletteContent as the main container for items
        var container = this.FindControl<ContentControl>("ItemPaletteContent");
        
        if (container == null) return;
        
        // Create a DockPanel to ensure proper layout
        var dockPanel = new DockPanel();
        
        // Create a ScrollViewer to host the items
        _mainScrollViewer = new ScrollViewer
        {
            VerticalScrollBarVisibility = Avalonia.Controls.Primitives.ScrollBarVisibility.Visible,
            HorizontalScrollBarVisibility = Avalonia.Controls.Primitives.ScrollBarVisibility.Disabled,
            AllowAutoHide = false,
            IsHitTestVisible = true
        };
        
        // Enable drag over on scrollviewer too
        DragDrop.SetAllowDrop(_mainScrollViewer, true);
        
        var itemsPanel = new StackPanel
        {
            HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Stretch,
            VerticalAlignment = Avalonia.Layout.VerticalAlignment.Top
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
        AddCategorySection(itemsPanel, "Spectrals", null, "SpectralsTab");
        AddCategorySection(itemsPanel, "Tags", null, "TagsTab");
        
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
        
        // Update initial tab highlight
        UpdateTabHighlight("SoulJokersTab");
        
        // Update drop zones
        UpdateDropZoneVisibility();
    }
    
    private void AddCategorySection(StackPanel parent, string category, string? subCategory, string tabId)
    {
        // Get items for this section
        var items = GetItemsForCategory(category, subCategory);
        
        // Filter by search if active
        if (!string.IsNullOrEmpty(_searchFilter))
        {
            items = items.Where(item => item.ToLowerInvariant().Contains(_searchFilter.ToLowerInvariant())).ToList();
        }
        
        if (!items.Any() && !string.IsNullOrEmpty(_searchFilter))
            return; // Skip empty sections when searching
        
        // Create section header
        var headerText = subCategory ?? category;
        var header = new TextBlock
        {
            Text = headerText,
            FontSize = 20,
            FontWeight = FontWeight.Bold,
            Foreground = Brushes.White,
            HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center,
            Margin = new Thickness(5, 20, 5, 10),
            Tag = tabId
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
        
        if (container == null) return;

        // Create a DockPanel to ensure proper layout
        var dockPanel = new DockPanel();

        // Create a ScrollViewer to host the items
        var scrollViewer = new ScrollViewer
        {
            VerticalScrollBarVisibility = Avalonia.Controls.Primitives.ScrollBarVisibility.Visible,
            HorizontalScrollBarVisibility = Avalonia.Controls.Primitives.ScrollBarVisibility.Disabled,
            AllowAutoHide = false,
            IsHitTestVisible = true
        };
        
        // Enable drag over on scrollviewer too
        DragDrop.SetAllowDrop(scrollViewer, true);

        var itemsPanel = new StackPanel
        {
            HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Stretch,
            VerticalAlignment = Avalonia.Layout.VerticalAlignment.Top
        };
        
        // Enable drag over on items panel
        DragDrop.SetAllowDrop(itemsPanel, true);

        var items = GetItemsForCategory(category, subCategory);
        var groupedItems = GroupItemsByRarity(category, items);

        itemsPanel.Children.Clear();
        foreach (var group in groupedItems)
        {
            // Add a header for the rarity group
            itemsPanel.Children.Add(new TextBlock
            {
                Text = group.Key,
                FontSize = 16,
                FontWeight = FontWeight.Bold,
                Foreground = Brushes.White,
                HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center,
                Margin = new Thickness(5, 10, 5, 5)
            });

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
            Dispatcher.UIThread.Post(() =>
            {
                Oracle.Helpers.DebugLogger.Log($"ScrollViewer Bounds: {scrollViewer.Bounds}");
            Oracle.Helpers.DebugLogger.Log($"Container Bounds: {container.Bounds}");
            Oracle.Helpers.DebugLogger.Log($"ItemsPanel Children: {itemsPanel.Children.Count}");
            Oracle.Helpers.DebugLogger.Log($"ItemsPanel Bounds: {itemsPanel.Bounds}");
            }, DispatcherPriority.Background);
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
                _ => "SoulJokersTab"
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
                _ => ""
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
            return;
            
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
        if (_mainScrollViewer == null || _sectionHeaders.Count == 0)
            return;
            
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
        var allTabs = new[] { 
            "EditJsonTab", "FavoritesTab", "SoulJokersTab", "RareJokersTab", "UncommonJokersTab", "CommonJokersTab",
            "VouchersTab", "TarotsTab", "SpectralsTab", "TagsTab", "ClearTab"
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
    
    private void NavigateToSection(string tabId)
    {
        // If we're in favorites view, reload the main view first
        if (_currentActiveTab == "FavoritesTab" || _mainScrollViewer == null)
        {
            LoadAllCategories();
        }
        
        // Then scroll to the section
        _ = ScrollToSection(tabId);
        UpdateTabHighlight(tabId);
    }
    
    private void ShowFavorites()
    {
        // Mark that we're in favorites mode
        _currentActiveTab = "FavoritesTab";
        
        // Show only items that are selected (in needs or wants)
        var container = this.FindControl<ContentControl>("ItemPaletteContent");
        if (container == null) return;
        
        var dockPanel = new DockPanel();
        var scrollViewer = new ScrollViewer
        {
            VerticalScrollBarVisibility = Avalonia.Controls.Primitives.ScrollBarVisibility.Visible,
            HorizontalScrollBarVisibility = Avalonia.Controls.Primitives.ScrollBarVisibility.Disabled,
            AllowAutoHide = false,
            IsHitTestVisible = true
        };
        
        DragDrop.SetAllowDrop(scrollViewer, true);
        
        var itemsPanel = new StackPanel
        {
            HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Stretch,
            VerticalAlignment = Avalonia.Layout.VerticalAlignment.Top
        };
        
        DragDrop.SetAllowDrop(itemsPanel, true);
        
        // Add Common Sets section first
        var setsHeader = new TextBlock
        {
            Text = "Common Sets",
            FontSize = 20,
            FontWeight = FontWeight.Bold,
            Margin = new Thickness(10, 10, 10, 10)
        };
        itemsPanel.Children.Add(setsHeader);
        
        var setsPanel = new WrapPanel
        {
            Orientation = Avalonia.Layout.Orientation.Horizontal,
            HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center,
            Margin = new Thickness(0, 0, 0, 20)
        };
        
        foreach (var set in FavoritesService.Instance.GetCommonSets())
        {
            var setDisplay = new JokerSetDisplay
            {
                JokerSet = set,
                Margin = new Thickness(5)
            };
            
            setDisplay.Click += (s, e) => 
            {
                // Add all items from the set to wants
                foreach (var item in set.Items)
                {
                    // Find the category for this item
                    string? category = null;
                    if (BalatroData.Jokers.ContainsKey(item)) category = "Jokers";
                    else if (BalatroData.TarotCards.ContainsKey(item)) category = "Tarots";
                    else if (BalatroData.SpectralCards.ContainsKey(item)) category = "Spectrals";
                    else if (BalatroData.Vouchers.ContainsKey(item)) category = "Vouchers";
                    
                    if (category != null)
                    {
                        var itemKey = $"{category}:{item}";
                        if (!_selectedWants.Contains(itemKey))
                        {
                            _selectedWants.Add(itemKey);
                        }
                    }
                }
                RefreshItemPalette();
                UpdatePersistentFavorites();
            };
            
            setsPanel.Children.Add(setDisplay);
        }
        
        itemsPanel.Children.Add(setsPanel);
        
        // Add separator
        itemsPanel.Children.Add(new Border
        {
            Height = 1,
            Background = new SolidColorBrush(Color.Parse("#4a4a4a")),
            Margin = new Thickness(10, 10, 10, 20)
        });
        
        // Add favorites header
        var favoritesHeader = new TextBlock
        {
            Text = "Selected Items",
            FontSize = 20,
            FontWeight = FontWeight.Bold,
            Margin = new Thickness(10, 10, 10, 10)
        };
        itemsPanel.Children.Add(favoritesHeader);
        
        // Group selected items by their actual category
        var selectedItems = _selectedNeeds.Union(_selectedWants).ToList();
        if (!selectedItems.Any())
        {
            itemsPanel.Children.Add(new TextBlock
            {
                Text = "No items selected",
                FontSize = 16,
                Foreground = new SolidColorBrush(Color.Parse("#888888")),
                HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center,
                Margin = new Thickness(0, 20)
            });
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
                    FontWeight = FontWeight.Bold,
                    Foreground = Brushes.White,
                    HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center,
                    Margin = new Thickness(5, 20, 5, 10)
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
        
        scrollViewer.PointerWheelChanged += (s, e) => { e.Handled = false; };
        
        UpdateTabHighlight("FavoritesTab");
        UpdateDropZoneVisibility();
    }
    
    private Dictionary<string, List<string>> GroupItemsByRarity(string category, List<string> items)
    {
        var groups = new Dictionary<string, List<string>>();
        
        if (category == "Favorites")
        {
            // For favorites, show all selected items grouped by category
            var allSelected = _selectedNeeds.Union(_selectedWants).ToList();
            
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
                        : kvp.Value.Where(item => item.ToLowerInvariant().Contains(_searchFilter.ToLowerInvariant())).ToList();
                    
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
                : items.Where(item => item.ToLowerInvariant().Contains(_searchFilter.ToLowerInvariant().Trim().Replace(" ", "").Replace("_", ""))).ToList();
            
            if (category == "Jokers")
            {
                // Group jokers by rarity
                groups["Legendary"] = filteredItems.Where(j => BalatroData.JokersByRarity["Legendary"].Contains(j)).ToList();
                groups["Rare"] = filteredItems.Where(j => BalatroData.JokersByRarity["Rare"].Contains(j)).ToList();
                groups["Uncommon"] = filteredItems.Where(j => BalatroData.JokersByRarity["Uncommon"].Contains(j)).ToList();
                groups["Common"] = filteredItems.Where(j => BalatroData.JokersByRarity["Common"].Contains(j)).ToList();
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
            Oracle.Helpers.DebugLogger.Log($"Creating card for 'Joker' in category: {category}");
        }
        
        // For favorites, determine the actual category of the item
        string actualCategory = category;
        if (category == "Favorites")
        {
            // Find which category this item belongs to
            if (BalatroData.Jokers.ContainsKey(itemName)) actualCategory = "Jokers";
            else if (BalatroData.TarotCards.ContainsKey(itemName)) actualCategory = "Tarots";
            else if (BalatroData.SpectralCards.ContainsKey(itemName)) actualCategory = "Spectrals";
            else if (BalatroData.Vouchers.ContainsKey(itemName)) actualCategory = "Vouchers";
            else if (BalatroData.Tags.ContainsKey(itemName)) actualCategory = "Tags";
        }
        
        // Load appropriate image based on actual category
        IImage? imageSource = actualCategory switch
        {
            "Jokers" => SpriteService.Instance.GetJokerImage(itemName),
            "Tarots" => SpriteService.Instance.GetTarotImage(itemName),
            "Spectrals" => SpriteService.Instance.GetSpectralImage(itemName),
            "Vouchers" => SpriteService.Instance.GetVoucherImage(itemName),
            "Tags" => SpriteService.Instance.GetTagImage(itemName),
            _ => null
        };
        
        var card = new ResponsiveCard
        {
            ItemName = itemName,
            Category = actualCategory, // Use actual category for proper functionality
            ImageSource = imageSource
        };
        
        // Check if selected (use actual category for key)
        var key = $"{actualCategory}:{itemName}";
        card.IsSelectedNeed = _selectedNeeds.Contains(key);
        card.IsSelectedWant = _selectedWants.Contains(key);
        
        return card;
    }
    
    private IBrush GetItemColor(string itemName, string category)
    {
        // For favorites, determine the actual category of the item
        string actualCategory = category;
        if (category == "Favorites")
        {
            // Find which category this item belongs to
            if (BalatroData.Jokers.ContainsKey(itemName)) actualCategory = "Jokers";
            else if (BalatroData.TarotCards.ContainsKey(itemName)) actualCategory = "Tarots";
            else if (BalatroData.SpectralCards.ContainsKey(itemName)) actualCategory = "Spectrals";
            else if (BalatroData.Vouchers.ContainsKey(itemName)) actualCategory = "Vouchers";
            else if (BalatroData.Tags.ContainsKey(itemName)) actualCategory = "Tags";
        }
        
        return actualCategory switch
        {
            "Jokers" => GetJokerColorBrush(itemName),
            "Tarots" => new SolidColorBrush(Color.Parse("#3498DB")),
            "Spectrals" => new SolidColorBrush(Color.Parse("#1ABC9C")),
            "Vouchers" => new SolidColorBrush(Color.Parse("#F39C12")),
            "Tags" => new SolidColorBrush(Color.Parse("#E74C3C")),
            _ => new SolidColorBrush(Color.Parse("#7F8C8D"))
        };
    }
    
    private SolidColorBrush GetJokerColorBrush(string jokerName)
    {
        // Determine joker rarity and return appropriate color
        if (BalatroData.JokersByRarity["Legendary"].Contains(jokerName))
            return new SolidColorBrush(Color.Parse("#FFD700")); // Gold
        if (BalatroData.JokersByRarity["Rare"].Contains(jokerName))
            return new SolidColorBrush(Color.Parse("#9B59B6")); // Purple
        if (BalatroData.JokersByRarity["Uncommon"].Contains(jokerName))
            return new SolidColorBrush(Color.Parse("#3498DB")); // Blue
        if (BalatroData.JokersByRarity["Common"].Contains(jokerName))
            return new SolidColorBrush(Color.Parse("#95A5A6")); // Gray
        
        return new SolidColorBrush(Color.Parse("#7F8C8D")); // Default gray
    }

    private void UpdateCardSelection(ResponsiveCard card)
    {
        var key = $"{card.Category}:{card.ItemName}";
        card.IsSelectedNeed = _selectedNeeds.Contains(key);
        card.IsSelectedWant = _selectedWants.Contains(key);
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
                var match = BalatroData.Jokers.Keys.FirstOrDefault(k => k.Equals(lcItem, StringComparison.OrdinalIgnoreCase));
                if (match != null)
                {
                    properNames.Add(match);
                }
                else
                {
                    // Fallback to the lowercase name if no match found
                    properNames.Add(lcItem);
                }
            }
            
            return properNames;
        }
        
        var allItems = _itemCategories.ContainsKey(category) ? _itemCategories[category] : new List<string>();
        return allItems;
    }

    private void UpdateSelectionDisplay()
    {
        var countText = this.FindControl<TextBlock>("SelectionCountText");
        if (countText != null)
        {
            var total = _selectedNeeds.Count + _selectedWants.Count;
            countText.Text = $"{total} selected";
        }

        // Update needs/wants panels
        UpdateSelectedItemsPanel("NeedsPanel", _selectedNeeds);
        UpdateSelectedItemsPanel("WantsPanel", _selectedWants);
    }
    
    private void ValidateJsonSyntaxForTextBox(TextBox textBox)
    {
        if (textBox == null) return;

        try
        {
            var jsonText = textBox.Text?.Trim();
            if (string.IsNullOrEmpty(jsonText))
            {
                UpdateStatus("Empty JSON document", isError: true);
                return;
            }

            using var jsonDocument = JsonDocument.Parse(jsonText);
            
            // Additional validation for Ouija config structure
            if (jsonDocument.RootElement.TryGetProperty("filter_config", out var filterConfig))
            {
                UpdateStatus("✓ Valid Ouija JSON config");
            }
            else
            {
                UpdateStatus("✓ Valid JSON (no filter_config found)");
            }
        }
        catch (JsonException ex)
        {
            UpdateStatus($"❌ JSON Error: {ex.Message}", isError: true);
        }
        catch (Exception ex)
        {
            UpdateStatus($"❌ Validation error: {ex.Message}", isError: true);
        }
    }
    
    private void ValidateJsonSyntaxForAvaloniaEdit(AvaloniaEdit.TextEditor textEditor)
    {
        if (textEditor == null) return;

        try
        {
            var jsonText = textEditor.Text?.Trim();
            if (string.IsNullOrEmpty(jsonText))
            {
                UpdateStatus("Empty JSON document", isError: true);
                return;
            }

            using var jsonDocument = JsonDocument.Parse(jsonText);
            
            // Additional validation for Ouija config structure
            if (jsonDocument.RootElement.TryGetProperty("filter_config", out var filterConfig))
            {
                UpdateStatus("✓ Valid Ouija JSON config");
            }
            else
            {
                UpdateStatus("✓ Valid JSON (no filter_config found)");
            }
        }
        catch (JsonException ex)
        {
            UpdateStatus($"❌ JSON Error: {ex.Message}", isError: true);
        }
        catch (Exception ex)
        {
            UpdateStatus($"❌ Validation error: {ex.Message}", isError: true);
        }
    }
    
    private void FormatJsonForTextBox(TextBox textBox)
    {
        if (textBox == null) return;

        try
        {
            if (string.IsNullOrWhiteSpace(textBox.Text))
            {
                UpdateStatus("❌ Cannot format: Empty or null JSON", isError: true);
                return;
            }
            
            var jsonDocument = JsonDocument.Parse(textBox.Text);
            var formattedJson = JsonSerializer.Serialize(jsonDocument, new JsonSerializerOptions 
            { 
                WriteIndented = true,
                Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
            });
            
            textBox.Text = formattedJson;
            UpdateStatus("✨ JSON formatted successfully");
        }
        catch (JsonException ex)
        {
            UpdateStatus($"❌ Cannot format: {ex.Message}", isError: true);
        }
    }
    
    private void FormatJsonForAvaloniaEdit(AvaloniaEdit.TextEditor textEditor)
    {
        if (textEditor == null) return;

        try
        {
            if (string.IsNullOrWhiteSpace(textEditor.Text))
            {
                UpdateStatus("❌ Cannot format: Empty or null JSON", isError: true);
                return;
            }
            
            var jsonDocument = JsonDocument.Parse(textEditor.Text);
            var formattedJson = JsonSerializer.Serialize(jsonDocument, new JsonSerializerOptions 
            { 
                WriteIndented = true,
                Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
            });
            
            textEditor.Text = formattedJson;
            UpdateStatus("✨ JSON formatted successfully");
        }
        catch (JsonException ex)
        {
            UpdateStatus($"❌ Cannot format: {ex.Message}", isError: true);
        }
    }
    
    private void UpdateSelectedItemsPanel(string panelName, HashSet<string> items)
    {
        var panel = this.FindControl<WrapPanel>(panelName);
        if (panel == null) return;
        
        panel.Children.Clear();
        
        foreach (var item in items)
        {
            var parts = item.Split(':');
            if (parts.Length != 2) continue;
            
            var miniCard = new Border
            {
                Background = GetItemColor(parts[1], parts[0]),
                CornerRadius = new Avalonia.CornerRadius(4),
                Padding = new Avalonia.Thickness(8, 4),
                Margin = new Avalonia.Thickness(2),
                Cursor = new Cursor(StandardCursorType.Hand)
            };
            
            var text = new TextBlock
            {
                Text = parts[1],
                FontSize = 11,
                Foreground = Brushes.White
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
        var allTabs = new[] { 
            "EditJsonTab", "FavoritesTab", "SoulJokersTab", "RareJokersTab", "UncommonJokersTab", "CommonJokersTab",
            "VouchersTab", "TarotsTab", "SpectralsTab", "TagsTab", "ClearTab"
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
            _ => ""
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
        var jokerTabs = new[] { "SoulJokersTab", "RareJokersTab", "UncommonJokersTab", "CommonJokersTab" };
        
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
            _ => "SoulJokersTab"
        };
        
        var activeButton = this.FindControl<Button>(activeTab);
        activeButton?.Classes.Add("active");
    }
    
    private void OnClearClick(object? sender, RoutedEventArgs e)
    {
        _selectedNeeds.Clear();
        _selectedWants.Clear();
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
        
        if (needsPlaceholder != null && needsScrollViewer != null && needsPanel != null)
        {
            if (_selectedNeeds.Any())
            {
                needsPlaceholder.IsVisible = false;
                needsScrollViewer.IsVisible = true;
                PopulateDropZonePanel(needsPanel, _selectedNeeds);
            }
            else
            {
                needsPlaceholder.IsVisible = true;
                needsScrollViewer.IsVisible = false;
                needsPanel.Children.Clear();
            }
        }
        
        // Update WANTS panel
        var wantsPlaceholder = this.FindControl<TextBlock>("WantsPlaceholder");
        var wantsScrollViewer = this.FindControl<ScrollViewer>("WantsScrollViewer");
        var wantsPanel = this.FindControl<WrapPanel>("WantsPanel");
        
        if (wantsPlaceholder != null && wantsScrollViewer != null && wantsPanel != null)
        {
            if (_selectedWants.Any())
            {
                wantsPlaceholder.IsVisible = false;
                wantsScrollViewer.IsVisible = true;
                PopulateDropZonePanel(wantsPanel, _selectedWants);
            }
            else
            {
                wantsPlaceholder.IsVisible = true;
                wantsScrollViewer.IsVisible = false;
                wantsPanel.Children.Clear();
            }
        }
        
        Oracle.Helpers.DebugLogger.Log($"📈 Updated drop zones: {_selectedNeeds.Count} needs, {_selectedWants.Count} wants");
    }
    
    private void PopulateDropZonePanel(WrapPanel panel, HashSet<string> items)
    {
        panel.Children.Clear();
        
        foreach (var item in items)
        {
            var parts = item.Split(':');
            if (parts.Length == 2)
            {
                var category = parts[0];
                var itemName = parts[1];
                var droppedItem = CreateDroppedItemControl(itemName, category);
                panel.Children.Add(droppedItem);
            }
        }
    }
    
    private Control CreateDroppedItemControl(string itemName, string category)
    {
        var border = new Border
        {
            Classes = { "dropped-item" },
            Cursor = new Cursor(StandardCursorType.Hand),
            Width = 50,
            Height = 65,
            Padding = new Thickness(2)
        };
        
        Oracle.Helpers.DebugLogger.Log($"🎴 Creating dropped item: {itemName} (category: {category})");
        
        // Get the appropriate image for the item
        IImage? imageSource = category switch
        {
            "Jokers" => SpriteService.Instance.GetJokerImage(itemName),
            "Tarots" => SpriteService.Instance.GetTarotImage(itemName),
            "Spectrals" => SpriteService.Instance.GetSpectralImage(itemName),
            "Vouchers" => SpriteService.Instance.GetVoucherImage(itemName),
            "Tags" => SpriteService.Instance.GetTagImage(itemName),
            _ => null
        };
        
        Oracle.Helpers.DebugLogger.Log($"🎴 Image found: {imageSource != null}");
        
        if (imageSource != null)
        {
            // Show the actual card image
            var image = new Image
            {
                Source = imageSource,
                Stretch = Stretch.Uniform,
                VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center,
                HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center
            };
            border.Child = image;
        }
        else
        {
            // Fallback to text if no image
            var textBlock = new TextBlock
            {
                Text = itemName,
                FontSize = 8,
                FontWeight = FontWeight.SemiBold,
                Foreground = Brushes.White,
                TextTrimming = TextTrimming.CharacterEllipsis,
                TextAlignment = TextAlignment.Center,
                VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center,
                HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center
            };
            border.Child = textBlock;
        }
        
        // Click to remove item
        border.PointerPressed += (s, e) =>
        {
            var key = $"{category}:{itemName}";
            _selectedNeeds.Remove(key);
            _selectedWants.Remove(key);
            
            UpdateDropZoneVisibility();
            UpdatePersistentFavorites();
            RefreshItemPalette();
            
            Oracle.Helpers.DebugLogger.Log($"❌ Removed {itemName} from selections");
        };
        
        return border;
    }
    
    private void RefreshItemPalette()
    {
        Oracle.Helpers.DebugLogger.Log("FiltersModal", "RefreshItemPalette called - updating selection states only");
        
        // If we're in favorites view, refresh it
        if (_currentActiveTab == "FavoritesTab")
        {
            ShowFavorites();
            return;
        }
        
        // Instead of reloading the entire category, just update the selection states
        var container = this.FindControl<ContentControl>("ItemPaletteContent");
        if (container?.Content is DockPanel dockPanel)
        {
            var scrollViewer = dockPanel.Children.OfType<ScrollViewer>().FirstOrDefault();
            if (scrollViewer?.Content is StackPanel itemsPanel)
            {
                // Update selection states for all visible items
                foreach (var child in itemsPanel.Children)
                {
                    if (child is WrapPanel wrapPanel)
                    {
                        foreach (var item in wrapPanel.Children)
                        {
                            if (item is ResponsiveCard card && card.Tag is string itemKey)
                            {
                                // Update the selection state
                                card.IsSelectedNeed = _selectedNeeds.Contains(itemKey);
                                card.IsSelectedWant = _selectedWants.Contains(itemKey);
                            }
                            else if (item is Border border && border.Tag is string borderKey)
                            {
                                // Fallback for any border-based items
                                bool isSelected = _selectedNeeds.Contains(borderKey) || _selectedWants.Contains(borderKey);
                                if (isSelected)
                                {
                                    border.Classes.Add("selected");
                                }
                                else
                                {
                                    border.Classes.Remove("selected");
                                }
                            }
                        }
                    }
                }
                
                Oracle.Helpers.DebugLogger.Log("FiltersModal", "Selection states updated without reload");
                return;
            }
        }
        
        // Fallback to full reload if structure not found
        Oracle.Helpers.DebugLogger.Log("FiltersModal", "Falling back to full reload");
        LoadAllCategories();
    }
    
    private void ClearNeeds()
    {
        _selectedNeeds.Clear();
        UpdateDropZoneVisibility();
        RefreshItemPalette();
        Oracle.Helpers.DebugLogger.Log("🗑️ Cleared all NEEDS");
    }
    
    private void ClearWants()
    {
        _selectedWants.Clear();
        UpdateDropZoneVisibility();
        RefreshItemPalette();
        Oracle.Helpers.DebugLogger.Log("🗑️ Cleared all WANTS");
    }
    
    private void PreviewConfig()
    {
        try
        {
            var config = BuildConfigFromSelections();
            var json = config.ToJson();
            
            // Show a simple preview dialog (you could enhance this)
            Oracle.Helpers.DebugLogger.Log("👁️ Config Preview:");
        Oracle.Helpers.DebugLogger.Log(json);
            
            // For now, just enter JSON edit mode with the generated config
            EnterEditJsonMode();
            
            // Set the JSON content after a short delay to allow the UI to initialize
            Dispatcher.UIThread.Post(() =>
            {
                // Update AvaloniaEdit if available
                var textEditor = this.FindControl<AvaloniaEdit.TextEditor>("JsonTextEditor");
                if (textEditor != null)
                {
                    Oracle.Helpers.DebugLogger.Log($"Setting TextEditor text in PreviewConfig, length: {json.Length}");
                    textEditor.Text = json;
                    textEditor.IsVisible = true; // Ensure visibility
                }
                else
                {
                    Oracle.Helpers.DebugLogger.LogError("JsonTextEditor not found in PreviewConfig");
                }
                
                // Also update the hidden TextBox for compatibility
                if (_jsonTextBox != null)
                {
                    _jsonTextBox.Text = json;
                }
                
                FormatJson();
            }, DispatcherPriority.Background);
        }
        catch (Exception ex)
        {
            Oracle.Helpers.DebugLogger.LogError($"Error generating config preview: {ex.Message}");
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
            if (BalatroData.Jokers.ContainsKey(itemName)) actualCategory = "Jokers";
            else if (BalatroData.TarotCards.ContainsKey(itemName)) actualCategory = "Tarots";
            else if (BalatroData.SpectralCards.ContainsKey(itemName)) actualCategory = "Spectrals";
            else if (BalatroData.Vouchers.ContainsKey(itemName)) actualCategory = "Vouchers";
            else if (BalatroData.Tags.ContainsKey(itemName)) actualCategory = "Tags";
        }
        card.Tag = $"{actualCategory}:{itemName}";
        
        // Single event handler for drag initiation - use PointerPressed!
        card.PointerPressed += async (sender, e) =>
        {
            // Only start drag on left mouse button
            var properties = e.GetCurrentPoint(card).Properties;
            if (!properties.IsLeftButtonPressed) return;
            
            // For jokers, check if it's a legendary/soul joker
            var dragCategory = category;
            if (category == "Jokers" && IsLegendaryJoker(itemName))
            {
                dragCategory = "SoulJokers";
            }
            
            // Create drag data
            var dataObject = new DataObject();
            dataObject.Set("balatro-item", $"{dragCategory}|{itemName}");
            
            // Visual feedback
            card.Classes.Add("is-dragging");
            Oracle.Helpers.DebugLogger.Log($"👋 Started dragging {itemName} from {category}");
            
            // Create a ghost card overlay with actual image
            CreateDragOverlay(itemName, category, card.ImageSource);
            _isDragging = true;
            
            try
            {
                // Do the drag operation
                // Note: Avalonia doesn't have built-in support for custom drag visuals
                // The wiggle animation on the source card will have to suffice for now
                var result = await DragDrop.DoDragDrop(e, dataObject, DragDropEffects.Move);
                Oracle.Helpers.DebugLogger.Log($"✅ Drag complete: {itemName} - Result: {result}");
            }
            catch (Exception ex)
            {
                Oracle.Helpers.DebugLogger.LogError($"Drag failed: {ex.Message}");
            }
            finally
            {
                // Always clean up visual feedback
                card.Classes.Remove("is-dragging");
                RemoveDragOverlay();
                _isDragging = false;
            }
        };
    }
    
    private System.Threading.Timer? _ghostWiggleTimer;
    private Canvas? _dragOverlay;
    private Border? _ghostCard;
    private bool _isDragging = false;
    
    private void CreateDragOverlay(string itemName, string category, IImage? imageSource)
    {
        // Find the root grid of the modal
        var rootGrid = this.FindControl<Grid>("RootGrid");
        if (rootGrid == null)
        {
            // Try to find any top-level grid
            rootGrid = this.GetVisualDescendants().OfType<Grid>().FirstOrDefault();
            if (rootGrid == null) return;
        }
        
        // Create overlay canvas
        _dragOverlay = new Canvas
        {
            IsHitTestVisible = false,
            ZIndex = 1000
        };
        
        // Create ghost card with actual image
        var ghostContent = new Grid();
        
        // Add the actual card image if available
        if (imageSource != null)
        {
            var image = new Image
            {
                Source = imageSource,
                Width = UIConstants.JokerSpriteWidth,
                Height = UIConstants.JokerSpriteHeight,
                Stretch = Stretch.Uniform
            };
            ghostContent.Children.Add(image);
        }
        else
        {
            // Fallback to text if no image
            ghostContent.Children.Add(new TextBlock
            {
                Text = itemName,
                FontSize = UIConstants.SmallFontSize,
                FontWeight = FontWeight.Bold,
                Foreground = Brushes.White,
                HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center,
                VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center,
                TextWrapping = TextWrapping.Wrap,
                TextAlignment = TextAlignment.Center,
                Margin = new Thickness(4)
            });
        }
        
        _ghostCard = new Border
        {
            Width = 80,
            Height = 100,
            Background = new SolidColorBrush(Color.FromArgb(200, 32, 32, 32)),
            BorderBrush = new SolidColorBrush(Color.Parse(UIConstants.BorderGold)),
            BorderThickness = new Thickness(2),
            CornerRadius = new CornerRadius(8),
            BoxShadow = BoxShadows.Parse("0 5 20 #000000AA"),
            RenderTransform = new RotateTransform(-5),
            Opacity = 0.9,
            IsVisible = false,
            Child = ghostContent,
            RenderTransformOrigin = new RelativePoint(0.5, 0.5, RelativeUnit.Relative)
        };
        
        _dragOverlay.Children.Add(_ghostCard);
        
        // Add overlay to the root grid
        Grid.SetRowSpan(_dragOverlay, Math.Max(1, rootGrid.RowDefinitions.Count));
        Grid.SetColumnSpan(_dragOverlay, Math.Max(1, rootGrid.ColumnDefinitions.Count));
        rootGrid.Children.Add(_dragOverlay);
        
        // Start wiggling the ghost card too!
        StartGhostWiggle();
        
        Oracle.Helpers.DebugLogger.Log("👻 Created drag overlay");
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
            Oracle.Helpers.DebugLogger.Log("👻 Removed drag overlay");
        }
    }
    
    private void StartGhostWiggle()
    {
        var wiggleAngles = new[] { -6, 6, -2, 2, -3, 3, -1, 1, -5, 5 };
        var index = 0;
        
        _ghostWiggleTimer = new System.Threading.Timer(_ =>
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
        }, null, 0, 90);
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
        var jsonEditorModeButton = this.FindControl<Border>("JsonEditorModeButton");
        if (jsonEditorModeButton != null && jsonEditorModeButton.Classes.Contains("mode-active"))
        {
            // We're in JSON editor mode, use the JSON save method
            SaveConfig();
            return;
        }
        
        // Otherwise, we're in drag-drop mode
        try
        {
            // Create ouija config from selections
            var config = BuildConfigFromSelections();
            
            // Get top level visual
            var topLevel = Avalonia.Controls.TopLevel.GetTopLevel(this);
            if (topLevel == null) return;
            
            var storageProvider = topLevel.StorageProvider;
            
            // Show save file dialog
            var saveOptions = new Avalonia.Platform.Storage.FilePickerSaveOptions
            {
                Title = "Save Ouija Config",
                DefaultExtension = "ouija.json",
                FileTypeChoices = new[]
                {
                    new Avalonia.Platform.Storage.FilePickerFileType("Ouija Config Files")
                    {
                        Patterns = new[] { "*.ouija.json" }
                    }
                },
                SuggestedFileName = $"config-{DateTime.Now:yyyyMMdd-HHmmss}.ouija.json"
            };
            
            var file = await storageProvider.SaveFilePickerAsync(saveOptions);
            if (file != null)
            {
                var json = config.ToJson();
                await System.IO.File.WriteAllTextAsync(file.Path.LocalPath, json);
                Oracle.Helpers.DebugLogger.Log($"✅ Config saved to: {file.Path.LocalPath}");
                
                // Update the search widget to use this saved config
                UpdateSearchWidgetConfig(file.Path.LocalPath);
            }
        }
        catch (Exception ex)
        {
            Oracle.Helpers.DebugLogger.LogError($"Error saving config: {ex.Message}");
        }
    }
    
    private OuijaConfig BuildConfigFromSelections()
    {
        // Get ante selection
        var anteSelector = this.FindControl<ComboBox>("AnteSelector");
        var selectedAnteIndex = anteSelector?.SelectedIndex ?? 0;
        int[]? searchAntes = null;
        if (selectedAnteIndex > 0) // Not "Any Ante"
        {
            searchAntes = new int[] { selectedAnteIndex };
        }
        
        // Get edition selection
        var editionType = GetSelectedEdition();
        
        // Get source selections
        var sources = GetSelectedSources();
        
        var config = new OuijaConfig
        {
            name = _configNameBox?.Text ?? "Filter Configuration",
            description = _configDescriptionBox?.Text ?? $"Search for {_selectedNeeds.Count} required items and {_selectedWants.Count} optional items",
            author = "Jimbo",
            keywords = new List<string>(),
            sources = sources,
            filter_config = new FilterConfig
            {
                Needs = new List<FilterItem>(),
                Wants = new List<FilterItem>(),

                ScoreNaturalNegatives = editionType == "negative",
                ScoreDesiredNegatives = false
            }
        };
        
        // Add needs (required items)
        foreach (var need in _selectedNeeds)
        {
            var parts = need.Split(':');
            if (parts.Length == 2)
            {
                // Add keywords based on item names
                if (!config.keywords.Contains(parts[1].ToLower()))
                    config.keywords.Add(parts[1].ToLower());
                
                config.filter_config.Needs.Add(new FilterItem
                {
                    Type = MapCategoryToType(parts[0]),
                    Value = parts[1],
                    SearchAntes = searchAntes ?? new List<int> { 1, 2, 3, 4, 5, 6, 7, 8 },
                    Score = 1,
                    Edition = editionType == "negative" ? "negative" : "NoEdition"
                });
            }
        }
        
        // Add wants (optional items)
        foreach (var want in _selectedWants)
        {
            var parts = want.Split(':');
            if (parts.Length == 2)
            {
                // Add keywords based on item names
                if (!config.keywords.Contains(parts[1].ToLower()))
                    config.keywords.Add(parts[1].ToLower());
                
                config.filter_config.Wants.Add(new FilterItem
                {
                    Type = MapCategoryToType(parts[0]),
                    Value = parts[1],
                    SearchAntes = searchAntes ?? new List<int> { 1, 2, 3, 4, 5, 6, 7, 8 },
                    Score = 1,
                    Edition = editionType == "negative" ? "negative" : "NoEdition"
                });
            }
        }
        
        return config;
    }
    
    private string GetSelectedEdition()
    {
        var normalRadio = this.FindControl<RadioButton>("EditionNormal");
        var foilRadio = this.FindControl<RadioButton>("EditionFoil");
        var holoRadio = this.FindControl<RadioButton>("EditionHolo");
        var polyRadio = this.FindControl<RadioButton>("EditionPoly");
        var negativeRadio = this.FindControl<RadioButton>("EditionNegative");
        
        if (foilRadio?.IsChecked == true) return "foil";
        if (holoRadio?.IsChecked == true) return "holographic";
        if (polyRadio?.IsChecked == true) return "polychrome";
        if (negativeRadio?.IsChecked == true) return "negative";
        
        return "normal";
    }
    
    private List<string> GetSelectedSources()
    {
        var sources = new List<string>();
        
        var tagsCheck = this.FindControl<CheckBox>("SourceTags");
        var packsCheck = this.FindControl<CheckBox>("SourcePacks");
        var shopCheck = this.FindControl<CheckBox>("SourceShop");
        var jimboCheck = this.FindControl<CheckBox>("SourceJimbo");
        var bigBlindCheck = this.FindControl<CheckBox>("SourceBigBlind");
        var smallBlindCheck = this.FindControl<CheckBox>("SourceSmallBlind");
        
        if (tagsCheck?.IsChecked == true) sources.Add("tag");
        if (packsCheck?.IsChecked == true) sources.Add("booster");
        if (shopCheck?.IsChecked == true) sources.Add("shop");
        if (jimboCheck?.IsChecked == true) sources.Add("jimbo");
        if (bigBlindCheck?.IsChecked == true) sources.Add("big_blind");
        if (smallBlindCheck?.IsChecked == true) sources.Add("small_blind");
        
        // If no sources selected, default to all main sources
        if (!sources.Any())
        {
            sources.AddRange(new[] { "tag", "booster", "shop" });
        }
        
        return sources;
    }
    
    /// <summary>
    /// Check if a joker is legendary (soul joker)
    /// </summary>
    private bool IsLegendaryJoker(string jokerName)
    {
        // Check if the joker exists in the MotelyJokerLegendary enum
        return Enum.TryParse<MotelyJokerLegendary>(jokerName, out _);
    }
    
    /// <summary>
    /// Maps UI category names (plural) to JSON Type values (singular)
    /// </summary>
    private string MapCategoryToType(string category)
    {
        return category switch
        {
            "Jokers" => "Joker",
            "SoulJokers" => "SoulJoker",  // Soul/Legendary jokers for Ouija
            "Tarots" => "Tarot",
            "Spectrals" => "Spectral",
            "Vouchers" => "Voucher",
            "Tags" => "Tag",
            "Bosses" => "Boss",
            "PlayingCards" => "PlayingCard",
            _ => category // Fallback to original if not mapped
        };
    }
    
    /// <summary>
    /// Maps JSON Type values (singular) to UI category names (plural)
    /// </summary>
    private string MapTypeToCategory(string type)
    {
        return type switch
        {
            "Joker" => "Jokers",
            "Tarot" => "Tarots", 
            "Spectral" => "Spectrals",
            "Voucher" => "Vouchers",
            "Tag" => "Tags",
            "Boss" => "Bosses",
            "PlayingCard" => "PlayingCards",
            "SoulJoker" => "Jokers", // Handle legacy SoulJoker type
            _ => type + "s" // Default: add 's' for unknown types
        };
    }
    


    private async void OnLoadClick(object? sender, RoutedEventArgs e)
    {
        // Check if we're in JSON editor mode
        var jsonEditorModeButton = this.FindControl<Border>("JsonEditorModeButton");
        if (jsonEditorModeButton != null && jsonEditorModeButton.Classes.Contains("mode-active"))
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
            if (topLevel == null) return;
            
            var storageProvider = topLevel.StorageProvider;
            
            // Show open file dialog
            var openOptions = new Avalonia.Platform.Storage.FilePickerOpenOptions
            {
                Title = "Load Ouija Config",
                AllowMultiple = false,
                FileTypeFilter = new[]
                {
                    new Avalonia.Platform.Storage.FilePickerFileType("Ouija Config Files")
                    {
                        Patterns = new[] { "*.ouija.json", "*.json" }
                    }
                }
            };
            
            var files = await storageProvider.OpenFilePickerAsync(openOptions);
            if (files.Count > 0)
            {
                var file = files[0];
                var json = await System.IO.File.ReadAllTextAsync(file.Path.LocalPath);
                
                // Parse the config
                var config = JsonSerializer.Deserialize<OuijaConfig>(json);
                if (config != null)
                {
                    LoadConfigIntoUI(config);
                    Oracle.Helpers.DebugLogger.Log($"✅ Config loaded from: {file.Path.LocalPath}");
                    
                    // Update the search widget to use this loaded config
                    UpdateSearchWidgetConfig(file.Path.LocalPath);
                }
            }
        }
        catch (Exception ex)
        {
            Oracle.Helpers.DebugLogger.LogError($"Error loading config: {ex.Message}");
        }
    }
    
    private void UpdateSearchWidgetConfig(string configPath)
    {
        try
        {
            // Get the main window and menu
            var window = TopLevel.GetTopLevel(this) as Window;
            var mainMenu = window?.Content as Views.BalatroMainMenu;
            
            if (mainMenu != null)
            {
                // Get the search widget
                var searchWidget = mainMenu.FindControl<Components.SearchWidget>("SearchWidget");
                if (searchWidget != null && searchWidget.IsVisible)
                {
                    Oracle.Helpers.DebugLogger.Log("FiltersModal", $"Updating SearchWidget with config: {Path.GetFileName(configPath)}");
                    
                    // Load the config in the search widget
                    Dispatcher.UIThread.Post(async () =>
                    {
                        await searchWidget.LoadConfig(configPath);
                    });
                }
            }
        }
        catch (Exception ex)
        {
            Oracle.Helpers.DebugLogger.LogError("FiltersModal", $"Error updating search widget config: {ex.Message}");
        }
    }
    
    private void LoadConfigIntoUI(OuijaConfig config)
    {
        // Clear current selections
        _selectedNeeds.Clear();
        _selectedWants.Clear();
        
        // Update metadata fields
        if (_configNameBox != null)
            _configNameBox.Text = config.name;
        if (_configDescriptionBox != null)
            _configDescriptionBox.Text = config.description;
            

        
        // Update item selections
        if (config.filter_config?.Needs != null)
        {
            foreach (var need in config.filter_config.Needs)
            {
                var category = MapTypeToCategory(need.Type);
                var key = $"{category}:{need.Value}";
                _selectedNeeds.Add(key);
            }
        }
        
        if (config.filter_config?.Wants != null)
        {
            foreach (var want in config.filter_config.Wants)
            {
                var category = MapTypeToCategory(want.Type);
                var key = $"{category}:{want.Value}";
                _selectedWants.Add(key);
            }
        }
        
        // Refresh the UI
        LoadCategory(_currentCategory);
        UpdateSelectionDisplay();
    }
    
    private async void OnLaunchSearchClick(object? sender, RoutedEventArgs e)
    {
        Oracle.Helpers.DebugLogger.Log("FiltersModal: OnLaunchSearchClick called");
        try
        {
            // Build config from current selections
            var config = BuildConfigFromSelections();
            
            // Save to temp file
            var tempPath = Path.Combine(Path.GetTempPath(), $"temp_search_{Guid.NewGuid()}.ouija.json");
            var json = JsonSerializer.Serialize(config, new JsonSerializerOptions 
            { 
                WriteIndented = true,
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
            });
            await File.WriteAllTextAsync(tempPath, json);
            
            Oracle.Helpers.DebugLogger.Log($"FiltersModal: Created temp config at {tempPath}");
            
            // Get the main menu reference
            var mainWindow = TopLevel.GetTopLevel(this) as Window;
            Oracle.Helpers.DebugLogger.Log($"FiltersModal: Got window: {mainWindow != null}");
            
            if (mainWindow != null)
            {
                // The window content is a Grid, and BalatroMainMenu is inside it
                var grid = mainWindow.Content as Grid;
                Oracle.Helpers.DebugLogger.Log($"FiltersModal: Got grid: {grid != null}");
                
                if (grid != null)
                {
                    // Find BalatroMainMenu in the grid's children
                    BalatroMainMenu? mainMenu = null;
                    foreach (var child in grid.Children)
                    {
                        if (child is BalatroMainMenu menu)
                        {
                            mainMenu = menu;
                            break;
                        }
                    }
                    
                    Oracle.Helpers.DebugLogger.Log($"FiltersModal: Got mainMenu: {mainMenu != null}");
                    
                    if (mainMenu != null)
                    {
                        // Close this modal properly
                        Oracle.Helpers.DebugLogger.Log("FiltersModal: Hiding modal content");
                        mainMenu.HideModalContent();
                        
                        // Show the search widget with the temp config
                        Oracle.Helpers.DebugLogger.Log("FiltersModal: Showing search widget");
                        mainMenu.ShowSearchWidget(tempPath);
                    }
                }
            }
        }
        catch (Exception ex)
        {
            Oracle.Helpers.DebugLogger.LogError($"Error launching search: {ex.Message}");
        }
    }
    
    // Removed custom scroll handler - AvaloniaEdit handles scrolling natively
    // private void OnEditorPointerWheelChanged(object? sender, PointerWheelEventArgs e)
    // {
    //     // AvaloniaEdit handles scrolling automatically
    // }
    
    private void UpdatePersistentFavorites()
    {
        // Update favorites in the service
        var allSelected = _selectedNeeds.Union(_selectedWants).ToList();
        FavoritesService.Instance.SetFavoriteItems(allSelected);
        
        // Update the favorites category
        _itemCategories["Favorites"] = allSelected;
    }
}
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using Avalonia.Platform.Storage;
using Avalonia.Markup.Xaml;
using Oracle.Helpers;
using Oracle.Services;

namespace Oracle.Components
{
    public partial class FilterSelector : UserControl
    {
        // Events
        public event EventHandler<string>? FilterSelected;
        public event EventHandler<string>? FilterLoaded;
        public event EventHandler? NewFilterRequested;
        
        // Properties
        private List<string> _availableFilters = new();
        private int _currentFilterIndex = 0;
        private string? _currentFilterPath = null;
        private bool _autoLoadEnabled = true;
        private bool _showCreateButton = true;
        
        // Controls
        private TextBlock? _filterName;
        private TextBlock? _filterDescription;
        private TextBlock? _authorText;
        private StackPanel? _authorPanel;
        private Canvas? _cardsCanvas;
        private SpriteService? _spriteService;
        
        public FilterSelector()
        {
            InitializeComponent();
        }
        
        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
            
            // Get controls
            _filterName = this.FindControl<TextBlock>("FilterName");
            _filterDescription = this.FindControl<TextBlock>("FilterDescription");
            _authorText = this.FindControl<TextBlock>("AuthorText");
            _authorPanel = this.FindControl<StackPanel>("AuthorPanel");
            _cardsCanvas = this.FindControl<Canvas>("CardsCanvas");
            
            // Initialize sprite service
            _spriteService = App.GetService<SpriteService>();
        }
        
        protected override void OnLoaded(RoutedEventArgs e)
        {
            base.OnLoaded(e);
            LoadAvailableFilters();
        }
        
        private void LoadAvailableFilters()
        {
            try
            {
                _availableFilters.Clear();
                
                // Look for .json files in the root directory
                var directory = Directory.GetCurrentDirectory();
                var rootJsonFiles = Directory.GetFiles(directory, "*.json");
                _availableFilters.AddRange(rootJsonFiles);
                
                // Also look for .json files in JsonItemConfigs directory
                var jsonConfigsDir = Path.Combine(directory, "JsonItemConfigs");
                if (Directory.Exists(jsonConfigsDir))
                {
                    var files = Directory.GetFiles(jsonConfigsDir, "*.json");
                    _availableFilters.AddRange(files);
                }
                
                DebugLogger.Log("FilterSelector", $"Found {_availableFilters.Count} filters");
                
                if (_availableFilters.Count == 0)
                {
                    if (_filterName != null) _filterName.Text = "No filters found";
                    if (_filterDescription != null) _filterDescription.Text = "Import a filter or create a new one";
                }
                else
                {
                    _currentFilterIndex = 0;
                    UpdateFilterPreview();
                    // The first filter will auto-load via UpdateFilterPreview
                }
            }
            catch (Exception ex)
            {
                DebugLogger.LogError("FilterSelector", $"Error loading filters: {ex.Message}");
            }
        }
        
        private void UpdateFilterPreview()
        {
            if (_availableFilters.Count == 0 || _currentFilterIndex < 0 || _currentFilterIndex >= _availableFilters.Count)
                return;
            
            try
            {
                var filterPath = _availableFilters[_currentFilterIndex];
                _currentFilterPath = filterPath;
                var filterContent = File.ReadAllText(filterPath);
                
                // Parse the filter
                using var doc = JsonDocument.Parse(filterContent);
                
                // Get filter name - skip if no name property
                if (!doc.RootElement.TryGetProperty("name", out var nameElement) || string.IsNullOrEmpty(nameElement.GetString()))
                {
                    Console.WriteLine($"Warning: Filter '{filterPath}' has no 'name' property - skipping");
                    _currentFilterIndex = (_currentFilterIndex + 1) % _availableFilters.Count;
                    UpdateFilterPreview();
                    return;
                }
                
                var filterName = nameElement.GetString()!;
                if (_filterName != null) _filterName.Text = filterName;
                
                // Get description
                string description = "No description";
                if (doc.RootElement.TryGetProperty("description", out var descElement))
                {
                    description = descElement.GetString() ?? "No description";
                }
                if (_filterDescription != null) _filterDescription.Text = description;
                
                // Get author
                if (doc.RootElement.TryGetProperty("author", out var authorElement) && _authorPanel != null && _authorText != null)
                {
                    var author = authorElement.GetString();
                    if (!string.IsNullOrEmpty(author))
                    {
                        _authorText.Text = author;
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
                
                
                // Update cards preview
                UpdateCardsPreview(doc.RootElement);
                
                // Fire selection event
                FilterSelected?.Invoke(this, filterPath);
                
                // AUTO-LOAD THE FILTER only if enabled (not in FiltersModal)
                if (_autoLoadEnabled)
                {
                    DebugLogger.Log("FilterSelector", $"Auto-loading filter: {filterPath}");
                    FilterLoaded?.Invoke(this, filterPath);
                }
            }
            catch (Exception ex)
            {
                DebugLogger.LogError("FilterSelector", $"Error updating filter preview: {ex.Message}");
            }
        }
        
        private void UpdateCardsPreview(JsonElement filterRoot)
        {
            if (_cardsCanvas == null || _spriteService == null) return;
            _cardsCanvas.Children.Clear();
            
            // Collect items from different categories
            var mustItems = new List<(string value, string? type)>();
            var shouldItems = new List<(string value, string? type)>();
            var mustNotItems = new List<(string value, string? type)>();
            
            // Debug: Log all root properties
            DebugLogger.Log("FilterSelector", $"Root properties in filter:");
            foreach (var prop in filterRoot.EnumerateObject())
            {
                DebugLogger.Log("FilterSelector", $"  - {prop.Name}: {prop.Value.ValueKind}");
            }
            
            // Check for the new filter format
            if (filterRoot.TryGetProperty("filter_config", out var filterConfig))
            {
                // New format with Must/Should/MustNot
                if (filterConfig.TryGetProperty("Must", out var must))
                {
                    foreach (var item in must.EnumerateArray())
                    {
                        if (item.TryGetProperty("Value", out var value))
                        {
                            var val = value.GetString() ?? "";
                            var type = item.TryGetProperty("Type", out var typeEl) ? typeEl.GetString() : null;
                            mustItems.Add((val, type));
                        }
                    }
                }
                
                if (filterConfig.TryGetProperty("Should", out var should))
                {
                    foreach (var item in should.EnumerateArray())
                    {
                        if (item.TryGetProperty("Value", out var value))
                        {
                            var val = value.GetString() ?? "";
                            var type = item.TryGetProperty("Type", out var typeEl) ? typeEl.GetString() : null;
                            shouldItems.Add((val, type));
                        }
                    }
                }
                
                if (filterConfig.TryGetProperty("MustNot", out var mustNot))
                {
                    foreach (var item in mustNot.EnumerateArray())
                    {
                        if (item.TryGetProperty("Value", out var value))
                        {
                            var val = value.GetString() ?? "";
                            var type = item.TryGetProperty("Type", out var typeEl) ? typeEl.GetString() : null;
                            mustNotItems.Add((val, type));
                        }
                    }
                }
            }
            else if (filterRoot.TryGetProperty("must", out var oldMust))
            {
                // Old format with must/wants/mustNot
                void ExtractItems(JsonElement element, List<(string, string?)> targetList, string? defaultType = null)
                {
                    if (element.ValueKind == JsonValueKind.Object)
                    {
                        foreach (var category in element.EnumerateObject())
                        {
                            var categoryType = category.Name;
                            if (category.Value.ValueKind == JsonValueKind.Array)
                            {
                                foreach (var item in category.Value.EnumerateArray())
                                {
                                    targetList.Add((item.GetString() ?? "", categoryType));
                                }
                            }
                        }
                    }
                    else if (element.ValueKind == JsonValueKind.Array)
                    {
                        foreach (var item in element.EnumerateArray())
                        {
                            if (item.ValueKind == JsonValueKind.String)
                            {
                                targetList.Add((item.GetString() ?? "", defaultType));
                            }
                            else if (item.ValueKind == JsonValueKind.Object && item.TryGetProperty("Value", out var valueElement))
                            {
                                var type = item.TryGetProperty("Type", out var typeEl) ? typeEl.GetString() : defaultType;
                                targetList.Add((valueElement.GetString() ?? "", type));
                            }
                        }
                    }
                }
                
                ExtractItems(oldMust, mustItems);
                
                if (filterRoot.TryGetProperty("wants", out var oldWants))
                    ExtractItems(oldWants, shouldItems);
                    
                if (filterRoot.TryGetProperty("mustNot", out var oldMustNot))
                    ExtractItems(oldMustNot, mustNotItems);
            }
            else if (filterRoot.TryGetProperty("Must", out var rootMust))
            {
                // Direct Motely format with Must/Should/MustNot at root
                DebugLogger.Log("FilterSelector", "Found direct Motely format");
                
                foreach (var item in rootMust.EnumerateArray())
                {
                    if (item.TryGetProperty("Value", out var value))
                    {
                        var val = value.GetString() ?? "";
                        var type = item.TryGetProperty("Type", out var typeEl) ? typeEl.GetString() : null;
                        mustItems.Add((val, type));
                        DebugLogger.Log("FilterSelector", $"  Must item: {val} (type: {type})");
                    }
                }
                
                if (filterRoot.TryGetProperty("Should", out var rootShould))
                {
                    foreach (var item in rootShould.EnumerateArray())
                    {
                        if (item.TryGetProperty("Value", out var value))
                        {
                            var val = value.GetString() ?? "";
                            var type = item.TryGetProperty("Type", out var typeEl) ? typeEl.GetString() : null;
                            shouldItems.Add((val, type));
                            DebugLogger.Log("FilterSelector", $"  Should item: {val} (type: {type})");
                        }
                    }
                }
                
                if (filterRoot.TryGetProperty("MustNot", out var rootMustNot))
                {
                    foreach (var item in rootMustNot.EnumerateArray())
                    {
                        if (item.TryGetProperty("Value", out var value))
                        {
                            var val = value.GetString() ?? "";
                            var type = item.TryGetProperty("Type", out var typeEl) ? typeEl.GetString() : null;
                            mustNotItems.Add((val, type));
                            DebugLogger.Log("FilterSelector", $"  MustNot item: {val} (type: {type})");
                        }
                    }
                }
            }
            
            // Debug: Log what we found
            DebugLogger.Log("FilterSelector", $"Found {mustItems.Count} must items, {shouldItems.Count} should items, {mustNotItems.Count} mustNot items");
            
            // Build the cards to display
            var cardsToDisplay = new List<(string value, string? type, bool isMustNot)>();
            
            // De-duplicate MUST items, skip "any" and "*"
            var seenValues = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var (value, type) in mustItems)
            {
                if (string.IsNullOrWhiteSpace(value) || 
                    value.Equals("any", StringComparison.OrdinalIgnoreCase) || 
                    value.Equals("*", StringComparison.OrdinalIgnoreCase))
                    continue;
                    
                if (seenValues.Add(value))
                {
                    cardsToDisplay.Add((value, type, false));
                }
            }
            
            // If less than 3 MUST items, add some SHOULD items
            if (cardsToDisplay.Count < 3)
            {
                foreach (var (value, type) in shouldItems)
                {
                    if (string.IsNullOrWhiteSpace(value) || 
                        value.Equals("any", StringComparison.OrdinalIgnoreCase) || 
                        value.Equals("*", StringComparison.OrdinalIgnoreCase))
                        continue;
                        
                    if (seenValues.Add(value))
                    {
                        cardsToDisplay.Add((value, type, false));
                        if (cardsToDisplay.Count >= 3) break;
                    }
                }
            }
            
            // Add some MUST-NOT items (but limit total to 5)
            int mustNotCount = 0;
            foreach (var (value, type) in mustNotItems)
            {
                if (cardsToDisplay.Count >= 5) break;
                if (string.IsNullOrWhiteSpace(value)) continue;
                
                if (seenValues.Add(value))
                {
                    cardsToDisplay.Add((value, type, true));
                    mustNotCount++;
                    if (mustNotCount >= 2) break; // Limit MUST-NOT items to 2
                }
            }
            
            // Create fanned card display
            for (int i = 0; i < Math.Min(cardsToDisplay.Count, 5); i++)
            {
                var (itemName, itemType, isMustNot) = cardsToDisplay[i];
                
                // Try to get the sprite for this item
                var sprite = _spriteService.GetItemImage(itemName, itemType);
                
                var cardContainer = new Grid
                {
                    Width = 50,
                    Height = 70
                };
                
                if (sprite != null)
                {
                    // Use actual sprite
                    var image = new Image
                    {
                        Source = sprite,
                        Width = 50,
                        Height = 70,
                        Stretch = Stretch.Uniform
                    };
                    cardContainer.Children.Add(image);
                }
                else
                {
                    // Fallback to text display
                    var card = new Border
                    {
                        Width = 50,
                        Height = 70,
                        Background = Application.Current?.FindResource("VeryDarkBackground") as IBrush ?? new SolidColorBrush(Color.Parse("#2a2a2a")),
                        BorderBrush = Application.Current?.FindResource("DarkGreyBorder") as IBrush ?? new SolidColorBrush(Color.Parse("#444444")),
                        BorderThickness = new Thickness(2),
                        CornerRadius = new CornerRadius(4)
                    };
                    
                    var textBlock = new TextBlock
                    {
                        Text = itemName.Length > 10 ? itemName.Substring(0, 10) + "..." : itemName,
                        FontSize = 9,
                        Foreground = Brushes.White,
                        HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center,
                        VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center,
                        TextTrimming = TextTrimming.CharacterEllipsis,
                        TextWrapping = TextWrapping.Wrap
                    };
                    card.Child = textBlock;
                    cardContainer.Children.Add(card);
                }
                
                // Add debuff X overlay for MUST-NOT items
                if (isMustNot)
                {
                    var debuffOverlay = GetDebuffXOverlay();
                    if (debuffOverlay != null)
                    {
                        var overlayImage = new Image
                        {
                            Source = debuffOverlay,
                            Width = 50,
                            Height = 70,
                            Stretch = Stretch.Uniform,
                            IsHitTestVisible = false
                        };
                        cardContainer.Children.Add(overlayImage);
                    }
                }
                
                // Position cards in a fan
                Canvas.SetLeft(cardContainer, 60 + i * 22);
                Canvas.SetTop(cardContainer, Math.Abs(i - 2) * 4); // Create arc effect
                
                // Rotate cards
                cardContainer.RenderTransform = new RotateTransform((i - 2) * 5);
                cardContainer.RenderTransformOrigin = new RelativePoint(0.5, 1, RelativeUnit.Relative);
                
                _cardsCanvas.Children.Add(cardContainer);
            }
            
            // If no items, show placeholder
            if (cardsToDisplay.Count == 0)
            {
                var placeholder = new TextBlock
                {
                    Text = "Empty filter",
                    FontSize = 12,
                    Foreground = Application.Current?.FindResource("MediumDarkGrey") as IBrush ?? new SolidColorBrush(Color.Parse("#666666")),
                    HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center,
                    VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center
                };
                Canvas.SetLeft(placeholder, 70);
                Canvas.SetTop(placeholder, 25);
                _cardsCanvas.Children.Add(placeholder);
            }
        }
        
        private IImage? GetDebuffXOverlay()
        {
            try
            {
                // The debuff X is at position 5 (index 4) in the Editions.png sprite sheet
                var editionsUri = new Uri("avares://Oracle/Assets/Jokers/Editions.png");
                using var stream = AssetLoader.Open(editionsUri);
                var editionsSheet = new Bitmap(stream);
                
                // Each edition is 71x94 pixels
                int spriteWidth = 71;
                int spriteHeight = 94;
                int xPosition = 4 * spriteWidth; // Position 5 (0-indexed as 4)
                
                return new CroppedBitmap(editionsSheet, new PixelRect(xPosition, 0, spriteWidth, spriteHeight));
            }
            catch (Exception ex)
            {
                DebugLogger.LogError("FilterSelector", $"Failed to load debuff X overlay: {ex.Message}");
                return null;
            }
        }
        
        private void OnLeftArrowClick(object? sender, RoutedEventArgs e)
        {
            if (_availableFilters.Count == 0)
            {
                LoadAvailableFilters();
                return;
            }
            
            if (_availableFilters.Count > 0)
            {
                _currentFilterIndex--;
                if (_currentFilterIndex < 0)
                    _currentFilterIndex = _availableFilters.Count - 1;
                
                UpdateFilterPreview();
            }
        }
        
        private void OnRightArrowClick(object? sender, RoutedEventArgs e)
        {
            if (_availableFilters.Count == 0)
            {
                LoadAvailableFilters();
                return;
            }
            
            if (_availableFilters.Count > 0)
            {
                _currentFilterIndex++;
                if (_currentFilterIndex >= _availableFilters.Count)
                    _currentFilterIndex = 0;
                
                UpdateFilterPreview();
            }
        }
        
        
        private async void OnImportClick(object? sender, RoutedEventArgs e)
        {
            var topLevel = TopLevel.GetTopLevel(this);
            if (topLevel == null) return;
            
            var files = await topLevel.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
            {
                Title = "Import Filter Configuration",
                AllowMultiple = false,
                FileTypeFilter = new[]
                {
                    new FilePickerFileType("JSON Files") { Patterns = new[] { "*.json" } },
                    new FilePickerFileType("All Files") { Patterns = new[] { "*" } }
                }
            });
            
            if (files.Count > 0)
            {
                try
                {
                    var importedFilePath = files[0].Path.LocalPath;
                    
                    // Create JsonItemConfigs directory if it doesn't exist
                    var jsonConfigsDir = Path.Combine(Directory.GetCurrentDirectory(), "JsonItemConfigs");
                    if (!Directory.Exists(jsonConfigsDir))
                    {
                        Directory.CreateDirectory(jsonConfigsDir);
                    }
                    
                    // Copy the imported file
                    var fileName = Path.GetFileName(importedFilePath);
                    var destinationPath = Path.Combine(jsonConfigsDir, fileName);
                    
                    // Handle duplicate names
                    if (File.Exists(destinationPath))
                    {
                        var baseName = Path.GetFileNameWithoutExtension(fileName);
                        var extension = Path.GetExtension(fileName);
                        var counter = 1;
                        do
                        {
                            fileName = $"{baseName}_{counter}{extension}";
                            destinationPath = Path.Combine(jsonConfigsDir, fileName);
                            counter++;
                        } while (File.Exists(destinationPath));
                    }
                    
                    File.Copy(importedFilePath, destinationPath, overwrite: false);
                    
                    // Reload and select the new filter
                    LoadAvailableFilters();
                    
                    // Find and select the newly imported filter
                    var index = _availableFilters.FindIndex(f => f.Equals(destinationPath, StringComparison.OrdinalIgnoreCase));
                    if (index >= 0)
                    {
                        _currentFilterIndex = index;
                        UpdateFilterPreview();
                    }
                    
                    DebugLogger.Log("FilterSelector", $"Imported filter: {fileName}");
                }
                catch (Exception ex)
                {
                    DebugLogger.LogError("FilterSelector", $"Error importing filter: {ex.Message}");
                }
            }
        }
        
        private void OnNewFilterClick(object? sender, RoutedEventArgs e)
        {
            NewFilterRequested?.Invoke(this, EventArgs.Empty);
        }
        
        // Public methods
        public void RefreshFilters()
        {
            LoadAvailableFilters();
        }
        
        public string? GetCurrentFilterPath()
        {
            return _currentFilterPath;
        }
        
        public bool AutoLoadEnabled
        {
            get => _autoLoadEnabled;
            set => _autoLoadEnabled = value;
        }
        
        public bool ShowCreateButton
        {
            get => _showCreateButton;
            set
            {
                _showCreateButton = value;
                // Update visibility of the New Filter button
                var newFilterButton = this.FindControl<Button>("NewFilterButton");
                if (newFilterButton != null)
                {
                    newFilterButton.IsVisible = value;
                }
            }
        }
    }
}
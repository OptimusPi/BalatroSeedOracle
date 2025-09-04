using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Platform.Storage;
using Avalonia.VisualTree;
using BalatroSeedOracle.Constants;
using BalatroSeedOracle.Controls;
using BalatroSeedOracle.Helpers;
using BalatroSeedOracle.Services;
using BalatroSeedOracle.Views.Modals;

namespace BalatroSeedOracle.Components
{
    public partial class FilterSelector : UserControl
    {
        // Events
        public event EventHandler<string>? FilterSelected;
        public event EventHandler<string>? FilterLoaded;
        public event EventHandler<string>? FilterCopyRequested;
        public event EventHandler<string>? FilterEditRequested;
        public event EventHandler<string>? FilterDeleteRequested;
        public event EventHandler? NewFilterRequested;

        // Properties
        private bool _autoLoadEnabled = true;
        public bool AutoLoadEnabled
        {
            get => _autoLoadEnabled;
            set => _autoLoadEnabled = value;
        }
        

        // Services
        private readonly SpriteService _spriteService;

        public bool ShowCreateButton { get; set; } = true;
        public bool ShouldSwitchToVisualTab { get; set; } = false;
        public bool IsInSearchModal { get; set; } = false;
        public bool ShowSelectButton { get; set; } = true;
        public bool ShowActionButtons { get; set; } = true;
        
        public static readonly StyledProperty<string> TitleProperty = 
            AvaloniaProperty.Register<FilterSelector, string>(nameof(Title), "Select Filter");
            
        public string Title
        {
            get => GetValue(TitleProperty);
            set => SetValue(TitleProperty, value);
        }

        // Controls
        private PanelSpinner? _filterSpinner;
        private Button? _selectButton;
        private bool _hasFilters = false;

        public FilterSelector()
        {
            InitializeComponent();
            _spriteService = ServiceHelper.GetRequiredService<SpriteService>();
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);

            // Get controls
            _filterSpinner = this.FindControl<PanelSpinner>("FilterSpinner");
            _selectButton = this.FindControl<Button>("SelectButton");

            // Setup panel spinner
            if (_filterSpinner != null)
            {
                _filterSpinner.SelectionChanged += OnFilterSelectionChanged;
            }
            
            // CRITICAL FIX: Wire up the Select button!
            if (_selectButton != null)
            {
                _selectButton.Click += OnSelectClick;
            }
        }

        protected override void OnLoaded(RoutedEventArgs e)
        {
            base.OnLoaded(e);
            LoadAvailableFilters();

            // Set button text based on context
            if (_selectButton != null)
            {
                if (IsInSearchModal)
                {
                    _selectButton.Content = "SEARCH WITH THIS FILTER";
                }
                else
                {
                    _selectButton.Content = "LOAD THIS FILTER";
                }
            }
        }

        private void LoadAvailableFilters()
        {
            try
            {
                var filterItems = new List<(PanelItem? item, DateTime? dateCreated)>();

                // Look for .json files in JsonItemFilters directory
                var directory = Path.Combine(Directory.GetCurrentDirectory(), "JsonItemFilters");

                // Create directory if it doesn't exist
                if (!Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                var rootJsonFiles = Directory.GetFiles(directory, "*.json");

                foreach (var file in rootJsonFiles)
                {
                    var result = CreateFilterPanelItemWithDate(file);
                    if (result.item != null)
                    {
                        filterItems.Add(result);
                    }
                }

                // Sort by DateCreated DESC (newest first)
                var sortedItems = filterItems
                    .Where(x => x.item != null)
                    .OrderByDescending(x => x.dateCreated ?? DateTime.MinValue)
                    .Select(x => x.item!)
                    .ToList();

                DebugLogger.Log("FilterSelector", $"Found {sortedItems.Count} filters");
                _hasFilters = sortedItems.Count > 0;

                // Create filter option removed - handled by dedicated FilterCreationModal
                
                // Control button visibility and state
                if (_selectButton != null)
                {
                    _selectButton.IsEnabled = true;
                    _selectButton.IsVisible = ShowSelectButton;
                }
                
                // Control action buttons visibility
                var copyButton = this.FindControl<Button>("CopyFilterButton");
                var editButton = this.FindControl<Button>("EditFilterButton");
                var deleteButton = this.FindControl<Button>("DeleteFilterButton");
                var createButton = this.FindControl<Button>("CreateNewFilterButton");
                
                if (copyButton != null) copyButton.IsVisible = ShowActionButtons;
                if (editButton != null) editButton.IsVisible = ShowActionButtons;
                if (deleteButton != null) deleteButton.IsVisible = ShowActionButtons;
                if (createButton != null) createButton.IsVisible = ShowActionButtons;

                if (_filterSpinner != null)
                    _filterSpinner.Items = sortedItems;
            }
            catch (Exception ex)
            {
                DebugLogger.LogError("FilterSelector", $"Error loading filters: {ex.Message}");
            }
        }

        private (PanelItem? item, DateTime? dateCreated) CreateFilterPanelItemWithDate(string filterPath)
        {
            try
            {
                var filterContent = File.ReadAllText(filterPath);
                
                // Skip empty files
                if (string.IsNullOrWhiteSpace(filterContent))
                {
                    DebugLogger.Log("FilterSelector", $"Skipping empty filter file: {filterPath}");
                    return (null, null);
                }
                
                var options = new JsonDocumentOptions
                {
                    AllowTrailingCommas = true,
                    CommentHandling = System.Text.Json.JsonCommentHandling.Skip
                };
                using var doc = JsonDocument.Parse(filterContent, options);

                // Get filter name - skip if no name property
                if (
                    !doc.RootElement.TryGetProperty("name", out var nameElement)
                    || string.IsNullOrEmpty(nameElement.GetString())
                )
                {
                    DebugLogger.Log(
                        "FilterSelector",
                        $"Filter '{filterPath}' has no 'name' property - skipping"
                    );
                    return (null, null);
                }

                var filterName = nameElement.GetString()!;

                // Get description
                string description = "No description";
                if (doc.RootElement.TryGetProperty("description", out var descElement))
                {
                    description = descElement.GetString() ?? "No description";
                }

                // Get author if available
                string? author = null;
                if (doc.RootElement.TryGetProperty("author", out var authorElement))
                {
                    author = authorElement.GetString();
                }

                if (!string.IsNullOrEmpty(author))
                {
                    description = $"by {author}\n{description}";
                }

                // Get dateCreated if available
                DateTime? dateCreated = null;
                if (doc.RootElement.TryGetProperty("dateCreated", out var dateElement))
                {
                    if (DateTime.TryParse(dateElement.GetString(), out var parsedDate))
                    {
                        dateCreated = parsedDate;
                    }
                }

                // Clone the root element to use after the document is disposed
                var clonedRoot = doc.RootElement.Clone();

                var item = new PanelItem
                {
                    Title = filterName,
                    Description = description,
                    Value = filterPath,
                    GetImage = () => CreateFannedPreviewImage(clonedRoot),
                };

                return (item, dateCreated);
            }
            catch (Exception ex)
            {
                DebugLogger.LogError(
                    "FilterSelector",
                    $"Error parsing filter {filterPath}: {ex.Message}"
                );
                return (null, null);
            }
        }

        private PanelItem? CreateFilterPanelItem(string filterPath)
        {
            var result = CreateFilterPanelItemWithDate(filterPath);
            return result.item;
        }

        private IImage? CreateFannedPreviewImage(JsonElement filterRoot)
        {
            try
            {
                DebugLogger.Log("FilterSelector", "Creating fanned preview image");
                
                // Collect items from all categories
                var previewItems = new List<(string value, string? type)>();

                // Check must items first
                if (filterRoot.TryGetProperty("must", out var mustItems))
                {
                    foreach (var item in mustItems.EnumerateArray())
                    {
                        if (item.TryGetProperty("value", out var value) && 
                            item.TryGetProperty("type", out var type))
                        {
                            previewItems.Add((value.GetString() ?? "", type.GetString()));
                            if (previewItems.Count >= 4) break;
                        }
                    }
                }

                // Add should items if we have space
                if (previewItems.Count < 4 && filterRoot.TryGetProperty("should", out var shouldItems))
                {
                    foreach (var item in shouldItems.EnumerateArray())
                    {
                        if (item.TryGetProperty("value", out var value) && 
                            item.TryGetProperty("type", out var type))
                        {
                            previewItems.Add((value.GetString() ?? "", type.GetString()));
                            if (previewItems.Count >= 4) break;
                        }
                    }
                }

                if (previewItems.Count == 0)
                {
                    DebugLogger.Log("FilterSelector", "No preview items found, showing MysteryJoker");
                    // Return a MysteryJoker image for empty filters
                    return _spriteService.GetJokerImage("j_joker");
                }
                
                DebugLogger.Log("FilterSelector", $"Found {previewItems.Count} preview items");

                // Create a render target bitmap for the composite image
                var pixelSize = new PixelSize(400, 200);
                var dpi = new Vector(96, 96);
                var renderBitmap = new RenderTargetBitmap(pixelSize, dpi);

                // Create a canvas to arrange the cards
                var canvas = new Canvas
                {
                    Width = pixelSize.Width,
                    Height = pixelSize.Height,
                    Background = Brushes.Transparent,
                    ClipToBounds = true,
                };

                // Add cards in a fanned arrangement
                int cardIndex = 0;
                int totalCards = Math.Min(previewItems.Count, 4);
                double startX = 50;
                double cardSpacing = 60;
                
                foreach (var (value, type) in previewItems.Take(totalCards))
                {
                    var image = GetItemImage(value, type);
                    if (image != null)
                    {
                        DebugLogger.Log("FilterSelector", $"Got image for {value} (type: {type})");
                        var imgControl = new Image
                        {
                            Source = image,
                            Width = 110,
                            Height = 150,
                            Stretch = Stretch.Uniform,
                        };

                        // Calculate position and rotation
                        double rotation = (cardIndex - totalCards / 2.0 + 0.5) * 8;
                        double xPos = startX + (cardIndex * cardSpacing);
                        double yPos = 20 + Math.Abs(cardIndex - totalCards / 2.0 + 0.5) * 8;

                        var transformGroup = new TransformGroup();
                        transformGroup.Children.Add(new RotateTransform(rotation, 55, 75));
                        transformGroup.Children.Add(new TranslateTransform(xPos, yPos));
                        
                        imgControl.RenderTransform = transformGroup;
                        imgControl.ZIndex = cardIndex;

                        canvas.Children.Add(imgControl);
                        cardIndex++;
                    }
                }

                // Measure and arrange the canvas
                canvas.Measure(new Size(pixelSize.Width, pixelSize.Height));
                canvas.Arrange(new Rect(0, 0, pixelSize.Width, pixelSize.Height));
                
                // Force layout update
                canvas.UpdateLayout();

                // Render to bitmap
                renderBitmap.Render(canvas);
                
                DebugLogger.Log("FilterSelector", $"Successfully created fanned preview with {cardIndex} cards");

                return renderBitmap;
            }
            catch (Exception ex)
            {
                DebugLogger.LogError("FilterSelector", $"Error creating fanned preview: {ex.Message}");
                // Fallback to single image
                return GetFilterPreviewImage(filterRoot);
            }
        }

        private Control? GetFilterPreviewControl(JsonElement filterRoot)
        {
            try
            {
                // Collect items from all categories
                var previewItems = new List<(string value, string? type)>();

                // Check must items first
                if (filterRoot.TryGetProperty("must", out var mustItems))
                {
                    foreach (var item in mustItems.EnumerateArray())
                    {
                        if (item.TryGetProperty("value", out var value) && 
                            item.TryGetProperty("type", out var type))
                        {
                            previewItems.Add((value.GetString() ?? "", type.GetString()));
                            if (previewItems.Count >= 5) break;
                        }
                    }
                }

                // Add should items if we have space
                if (previewItems.Count < 5 && filterRoot.TryGetProperty("should", out var shouldItems))
                {
                    foreach (var item in shouldItems.EnumerateArray())
                    {
                        if (item.TryGetProperty("value", out var value) && 
                            item.TryGetProperty("type", out var type))
                        {
                            previewItems.Add((value.GetString() ?? "", type.GetString()));
                            if (previewItems.Count >= 5) break;
                        }
                    }
                }

                if (previewItems.Count == 0)
                {
                    return null;
                }

                // Create a canvas to hold the fanned cards
                var canvas = new Canvas
                {
                    Width = 200,
                    Height = 100,
                    HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center,
                    VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center,
                };

                // Create fanned display
                int cardIndex = 0;
                foreach (var (value, type) in previewItems.Take(5))
                {
                    var image = GetItemImage(value, type);
                    if (image != null)
                    {
                        var imgControl = new Image
                        {
                            Source = image,
                            Width = 60,
                            Height = 80,
                        };

                        // Fan out the cards
                        var rotation = (cardIndex - 2) * 5; // -10, -5, 0, 5, 10 degrees
                        var xOffset = cardIndex * 25 + 20;
                        var yOffset = Math.Abs(cardIndex - 2) * 3; // Slight Y offset for depth

                        imgControl.RenderTransform = new Avalonia.Media.RotateTransform(rotation);
                        Canvas.SetLeft(imgControl, xOffset);
                        Canvas.SetTop(imgControl, yOffset);
                        imgControl.ZIndex = cardIndex;

                        canvas.Children.Add(imgControl);
                        cardIndex++;
                    }
                }

                return canvas;
            }
            catch (Exception ex)
            {
                DebugLogger.LogError("FilterSelector", $"Error creating preview: {ex.Message}");
                return null;
            }
        }

        private IImage? GetFilterPreviewImage(JsonElement filterRoot)
        {
            // Fallback for single image if needed
            try
            {
                if (filterRoot.TryGetProperty("must", out var items))
                {
                    foreach (var item in items.EnumerateArray())
                    {
                        if (item.TryGetProperty("value", out var value) && 
                            item.TryGetProperty("type", out var type))
                        {
                            return GetItemImage(value.GetString() ?? "", type.GetString());
                        }
                    }
                }
                return null;
            }
            catch
            {
                return null;
            }
        }

        private IImage? GetItemImage(string value, string? type)
        {
            // Get image based on type
            var lowerType = type?.ToLower();
            
            // Special handling for souljoker - create composite with card base and face overlay
            if (lowerType == "souljoker")
            {
                var cardBase = _spriteService.GetJokerImage(value);
                var jokerFace = _spriteService.GetJokerSoulImage(value);
                
                if (cardBase != null && jokerFace != null)
                {
                    // Create a composite image with card base and face overlay
                    var pixelSize = new PixelSize(UIConstants.JokerSpriteWidth, UIConstants.JokerSpriteHeight);
                    var dpi = new Vector(96, 96);
                    
                    try
                    {
                        var renderBitmap = new RenderTargetBitmap(pixelSize, dpi);
                        
                        using (var context = renderBitmap.CreateDrawingContext())
                        {
                            // Draw card base first
                            var cardRect = new Rect(0, 0, pixelSize.Width, pixelSize.Height);
                            context.DrawImage(cardBase, cardRect);
                            
                            // Draw face overlay on top
                            context.DrawImage(jokerFace, cardRect);
                        }
                        
                        return renderBitmap;
                    }
                    catch (Exception ex)
                    {
                        DebugLogger.LogError("FilterSelector", $"Failed to create composite image: {ex.Message}");
                        // Fallback to just the face if composite fails
                        return jokerFace ?? cardBase;
                    }
                }
                else if (jokerFace != null)
                {
                    // If we only have the face, use that
                    return jokerFace;
                }
                else if (cardBase != null)
                {
                    // Fallback to just the card if we can't get the face
                    return cardBase;
                }
            }
            
            return lowerType switch
            {
                "joker" => _spriteService.GetJokerImage(value),
                "voucher" => _spriteService.GetVoucherImage(value),
                "tag" => _spriteService.GetTagImage(value),
                "boss" => _spriteService.GetBossImage(value),
                "spectral" => _spriteService.GetSpectralImage(value),
                "tarot" => _spriteService.GetTarotImage(value),
                _ => null,
            };
        }

        private void OnFilterSelectionChanged(object? sender, PanelItem? item)
        {
            if (item?.Value != null && !string.IsNullOrEmpty(item.Value))
            {
                // Regular filter selection
                FilterSelected?.Invoke(this, item.Value);
                
                // Update button text based on context
                if (_selectButton != null)
                {
                    if (IsInSearchModal)
                        _selectButton.Content = "USE THIS FILTER";
                    else
                        _selectButton.Content = "LOAD THIS FILTER";
                        
                    _selectButton.IsEnabled = true;
                    _selectButton.IsVisible = true;
                }

                // Auto-load if enabled
                if (_autoLoadEnabled)
                {
                    DebugLogger.Log("FilterSelector", $"Auto-loading filter: {item.Value} (AutoLoadEnabled={_autoLoadEnabled})");
                    FilterLoaded?.Invoke(this, item.Value);
                }
                else
                {
                    DebugLogger.Log("FilterSelector", $"Filter selected but NOT auto-loading: {item.Value} (AutoLoadEnabled={_autoLoadEnabled})");
                }
            }
        }

        private void OnSelectClick(object? sender, RoutedEventArgs e)
        {
            var selectedItem = _filterSpinner?.SelectedItem;
            
            if (selectedItem?.Value != null && !string.IsNullOrEmpty(selectedItem.Value))
            {
                // Load the selected filter
                FilterLoaded?.Invoke(this, selectedItem.Value);
            }
        }

        private void OnCopyFilterClick(object? sender, RoutedEventArgs e)
        {
            var selectedItem = _filterSpinner?.SelectedItem;
            if (selectedItem?.Value != null)
            {
                FilterCopyRequested?.Invoke(this, selectedItem.Value);
            }
        }

        private void OnEditFilterClick(object? sender, RoutedEventArgs e)
        {
            var selectedItem = _filterSpinner?.SelectedItem;
            if (selectedItem?.Value != null)
            {
                FilterEditRequested?.Invoke(this, selectedItem.Value);
            }
        }

        private void OnDeleteFilterClick(object? sender, RoutedEventArgs e)
        {
            var selectedItem = _filterSpinner?.SelectedItem;
            if (selectedItem?.Value != null)
            {
                FilterDeleteRequested?.Invoke(this, selectedItem.Value);
            }
        }

        private void OnCreateNewFilterClick(object? sender, RoutedEventArgs e)
        {
            NewFilterRequested?.Invoke(this, EventArgs.Empty);
        }

        public void RefreshFilters()
        {
            LoadAvailableFilters();
        }

    }
}

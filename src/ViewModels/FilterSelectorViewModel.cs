using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using BalatroSeedOracle.Constants;
using BalatroSeedOracle.Controls;
using BalatroSeedOracle.Helpers;
using BalatroSeedOracle.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace BalatroSeedOracle.ViewModels
{
    /// <summary>
    /// ViewModel for FilterSelector component - handles filter loading, selection, and management
    /// </summary>
    public partial class FilterSelectorViewModel : ObservableObject
    {
        private readonly SpriteService _spriteService;

        [ObservableProperty]
        private bool _autoLoadEnabled = true;

        [ObservableProperty]
        private bool _showCreateButton = true;

        [ObservableProperty]
        private bool _shouldSwitchToVisualTab = false;

        [ObservableProperty]
        private bool _isInSearchModal = false;

        [ObservableProperty]
        private bool _showSelectButton = true;

        [ObservableProperty]
        private bool _showActionButtons = true;

        [ObservableProperty]
        private string _title = "Select Filter";

        [ObservableProperty]
        private string _selectButtonText = "LOAD THIS FILTER";

        [ObservableProperty]
        private bool _selectButtonEnabled = false;

        [ObservableProperty]
        private bool _hasFilters = false;

        [ObservableProperty]
        private List<PanelItem> _filterItems = new();

        [ObservableProperty]
        private PanelItem? _selectedFilter;

        [ObservableProperty]
        private int _selectedFilterIndex = 0;

        // Events
        public event EventHandler<string>? FilterSelected;
        public event EventHandler<string>? FilterLoaded;
        public event EventHandler<string>? FilterCopyRequested;
        public event EventHandler<string>? FilterEditRequested;
        public event EventHandler<string>? FilterDeleteRequested;
        public event EventHandler? NewFilterRequested;

        private readonly IConfigurationService _configurationService;

        public FilterSelectorViewModel(SpriteService spriteService, IConfigurationService configurationService)
        {
            _spriteService = spriteService ?? throw new ArgumentNullException(nameof(spriteService));
            _configurationService = configurationService ?? throw new ArgumentNullException(nameof(configurationService));
        }

        #region Property Changed Handlers

        partial void OnIsInSearchModalChanged(bool value)
        {
            UpdateSelectButtonText();
        }

        partial void OnSelectedFilterChanged(PanelItem? value)
        {
            OnFilterSelectionChanged();
        }

        #endregion

        /// <summary>
        /// Initialize the ViewModel - loads available filters
        /// </summary>
        public async Task InitializeAsync()
        {
            await LoadAvailableFiltersAsync();
            UpdateSelectButtonText();
            // Disable select until a specific filter is chosen
            SelectButtonEnabled = SelectedFilter != null && !string.IsNullOrEmpty(SelectedFilter?.Value);
            UpdateCommandStates();
        }

        /// <summary>
        /// Loads all available filter files from the JsonItemFilters directory
        /// </summary>
        public async Task LoadAvailableFiltersAsync()
        {
            try
            {
                var filterItems = new List<(PanelItem? item, DateTime? dateCreated)>();

                // Look for .json files in filters directory via configuration service
                var directory = _configurationService.GetFiltersDirectory();

                // Ensure directory exists via configuration service
                _configurationService.EnsureDirectoryExists(directory);

                var rootJsonFiles = Directory.GetFiles(directory, "*.json");

                foreach (var file in rootJsonFiles)
                {
                    var result = await CreateFilterPanelItemWithDateAsync(file);
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

                DebugLogger.Log("FilterSelectorViewModel", $"Found {sortedItems.Count} filters");

                FilterItems = sortedItems;
                HasFilters = sortedItems.Count > 0;
                // Default-select the first filter so actions are immediately available
                if (HasFilters && SelectedFilter == null)
                {
                    SelectedFilterIndex = 0;
                    SelectedFilter = sortedItems[0];
                }

                // Enable when a selection is present
                SelectButtonEnabled = SelectedFilter != null && !string.IsNullOrEmpty(SelectedFilter?.Value);

                // Update command can execute states
                UpdateCommandStates();
            }
            catch (Exception ex)
            {
                DebugLogger.LogError("FilterSelectorViewModel", $"Error loading filters: {ex.Message}");
            }
        }

        /// <summary>
        /// Creates a PanelItem from a filter file path with date metadata
        /// </summary>
        private async Task<(PanelItem? item, DateTime? dateCreated)> CreateFilterPanelItemWithDateAsync(string filterPath)
        {
            try
            {
                var filterContent = await File.ReadAllTextAsync(filterPath);

                // Skip empty files
                if (string.IsNullOrWhiteSpace(filterContent))
                {
                    DebugLogger.Log("FilterSelectorViewModel", $"Skipping empty filter file: {filterPath}");
                    return (null, null);
                }

                var options = new JsonDocumentOptions
                {
                    AllowTrailingCommas = true,
                    CommentHandling = JsonCommentHandling.Skip
                };

                using var doc = JsonDocument.Parse(filterContent, options);

                // Get filter name - skip if no name property
                if (!doc.RootElement.TryGetProperty("name", out var nameElement)
                    || string.IsNullOrEmpty(nameElement.GetString()))
                {
                    DebugLogger.Log("FilterSelectorViewModel", $"Filter '{filterPath}' has no 'name' property - skipping");
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
                DebugLogger.LogError("FilterSelectorViewModel", $"Error parsing filter {filterPath}: {ex.Message}");
                return (null, null);
            }
        }

        /// <summary>
        /// Creates a fanned preview image showing multiple cards from the filter
        /// </summary>
        private IImage? CreateFannedPreviewImage(JsonElement filterRoot)
        {
            try
            {
                DebugLogger.Log("FilterSelectorViewModel", "Creating fanned preview image");

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
                    DebugLogger.Log("FilterSelectorViewModel", "No preview items found, showing MysteryJoker");
                    // Return a MysteryJoker image for empty filters
                    return _spriteService.GetJokerImage("j_joker");
                }

                DebugLogger.Log("FilterSelectorViewModel", $"Found {previewItems.Count} preview items");

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
                        DebugLogger.Log("FilterSelectorViewModel", $"Got image for {value} (type: {type})");
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

                DebugLogger.Log("FilterSelectorViewModel", $"Successfully created fanned preview with {cardIndex} cards");

                return renderBitmap;
            }
            catch (Exception ex)
            {
                DebugLogger.LogError("FilterSelectorViewModel", $"Error creating fanned preview: {ex.Message}");
                // Fallback to single image
                return GetFilterPreviewImage(filterRoot);
            }
        }

        /// <summary>
        /// Gets a fallback single image preview from the filter
        /// </summary>
        private IImage? GetFilterPreviewImage(JsonElement filterRoot)
        {
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

        /// <summary>
        /// Gets the appropriate sprite image based on item type
        /// </summary>
        private IImage? GetItemImage(string value, string? type)
        {
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
                        DebugLogger.LogError("FilterSelectorViewModel", $"Failed to create composite image: {ex.Message}");
                        // Fallback to just the face if composite fails
                        return jokerFace ?? cardBase;
                    }
                }
                else if (jokerFace != null)
                {
                    return jokerFace;
                }
                else if (cardBase != null)
                {
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

        /// <summary>
        /// Updates the select button text based on context
        /// </summary>
        private void UpdateSelectButtonText()
        {
            if (IsInSearchModal)
            {
                SelectButtonText = SelectedFilter != null ? "USE THIS FILTER" : "SEARCH WITH THIS FILTER";
            }
            else
            {
                SelectButtonText = "LOAD THIS FILTER";
            }
        }

        /// <summary>
        /// Updates all command can execute states
        /// </summary>
        private void UpdateCommandStates()
        {
            SelectFilterCommand.NotifyCanExecuteChanged();
            CopyFilterCommand.NotifyCanExecuteChanged();
            EditFilterCommand.NotifyCanExecuteChanged();
            DeleteFilterCommand.NotifyCanExecuteChanged();
        }

        /// <summary>
        /// Handles filter selection changes
        /// </summary>
        private void OnFilterSelectionChanged()
        {
            if (SelectedFilter?.Value != null && !string.IsNullOrEmpty(SelectedFilter.Value))
            {
                // Regular filter selection
                FilterSelected?.Invoke(this, SelectedFilter.Value);

                UpdateSelectButtonText();
                SelectButtonEnabled = true;

                // Update command states
                UpdateCommandStates();

                // Auto-load if enabled
                if (AutoLoadEnabled)
                {
                    DebugLogger.Log("FilterSelectorViewModel", $"Auto-loading filter: {SelectedFilter.Value} (AutoLoadEnabled={AutoLoadEnabled})");
                    FilterLoaded?.Invoke(this, SelectedFilter.Value);
                }
                else
                {
                    DebugLogger.Log("FilterSelectorViewModel", $"Filter selected but NOT auto-loading: {SelectedFilter.Value} (AutoLoadEnabled={AutoLoadEnabled})");
                }
            }
            else
            {
                // No selection – keep actions disabled and update text accordingly
                SelectButtonEnabled = false;
                UpdateSelectButtonText();
                UpdateCommandStates();
            }
        }

        #region Command Implementations

        private bool CanSelectFilter() => SelectedFilter != null && !string.IsNullOrEmpty(SelectedFilter.Value);

        private bool CanExecuteFilterAction() => SelectedFilter != null && !string.IsNullOrEmpty(SelectedFilter.Value);

        [RelayCommand(CanExecute = nameof(CanSelectFilter))]
        private void SelectFilter()
        {
            if (SelectedFilter?.Value != null && !string.IsNullOrEmpty(SelectedFilter.Value))
            {
                DebugLogger.Log("FilterSelectorViewModel", $"Loading filter: {SelectedFilter.Value}");
                FilterLoaded?.Invoke(this, SelectedFilter.Value);
            }
        }

        [RelayCommand(CanExecute = nameof(CanExecuteFilterAction))]
        private void CopyFilter()
        {
            if (SelectedFilter?.Value != null)
            {
                DebugLogger.Log("FilterSelectorViewModel", $"Copy requested for filter: {SelectedFilter.Value}");
                FilterCopyRequested?.Invoke(this, SelectedFilter.Value);
            }
        }

        [RelayCommand(CanExecute = nameof(CanExecuteFilterAction))]
        private void EditFilter()
        {
            if (SelectedFilter?.Value != null)
            {
                DebugLogger.Log("FilterSelectorViewModel", $"Edit requested for filter: {SelectedFilter.Value}");
                FilterEditRequested?.Invoke(this, SelectedFilter.Value);
            }
        }

        [RelayCommand(CanExecute = nameof(CanExecuteFilterAction))]
        private void DeleteFilter()
        {
            if (SelectedFilter?.Value != null)
            {
                DebugLogger.Log("FilterSelectorViewModel", $"Delete requested for filter: {SelectedFilter.Value}");
                FilterDeleteRequested?.Invoke(this, SelectedFilter.Value);
            }
        }

        [RelayCommand]
        private void CreateNewFilter()
        {
            DebugLogger.Log("FilterSelectorViewModel", "Create new filter requested");
            NewFilterRequested?.Invoke(this, EventArgs.Empty);
        }

        [RelayCommand]
        private async Task RefreshFilters()
        {
            await LoadAvailableFiltersAsync();
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Public method to refresh the filter list
        /// </summary>
        public async Task RefreshFiltersAsync()
        {
            await LoadAvailableFiltersAsync();
        }

        #endregion
    }
}

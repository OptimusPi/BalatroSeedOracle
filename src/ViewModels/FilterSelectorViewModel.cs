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
        private readonly IFilterCacheService _filterCacheService;

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

        public FilterSelectorViewModel(
            SpriteService spriteService,
            IConfigurationService configurationService,
            IFilterCacheService filterCacheService
        )
        {
            _spriteService =
                spriteService ?? throw new ArgumentNullException(nameof(spriteService));
            _configurationService =
                configurationService
                ?? throw new ArgumentNullException(nameof(configurationService));
            _filterCacheService =
                filterCacheService ?? throw new ArgumentNullException(nameof(filterCacheService));
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
            SelectButtonEnabled =
                SelectedFilter != null && !string.IsNullOrEmpty(SelectedFilter?.Value);
            UpdateCommandStates();
        }

        /// <summary>
        /// Loads all available filter files from the FilterCacheService
        /// </summary>
        public async Task LoadAvailableFiltersAsync()
        {
            try
            {
                // Use the FilterCacheService instead of reading files manually
                var allCachedFilters = _filterCacheService.GetAllFilters();

                var filterItems = new List<PanelItem>();

                foreach (var cached in allCachedFilters)
                {
                    var item = await CreateFilterPanelItemFromCacheAsync(cached);
                    if (item != null)
                    {
                        filterItems.Add(item);
                    }
                }

                DebugLogger.Log(
                    "FilterSelectorViewModel",
                    $"Found {filterItems.Count} filters from cache"
                );

                FilterItems = filterItems;
                HasFilters = filterItems.Count > 0;
                // Default-select the first filter so actions are immediately available
                if (HasFilters && SelectedFilter == null)
                {
                    SelectedFilterIndex = 0;
                    SelectedFilter = filterItems[0];
                }

                // Enable when a selection is present
                SelectButtonEnabled =
                    SelectedFilter != null && !string.IsNullOrEmpty(SelectedFilter?.Value);

                // Update command can execute states
                UpdateCommandStates();
            }
            catch (Exception ex)
            {
                DebugLogger.LogError(
                    "FilterSelectorViewModel",
                    $"Error loading filters: {ex.Message}"
                );
            }
        }

        /// <summary>
        /// Creates a PanelItem from a cached filter
        /// </summary>
        private Task<PanelItem?> CreateFilterPanelItemFromCacheAsync(
            Services.CachedFilter cached
        )
        {
            try
            {
                var config = cached.Config;
                if (config == null || string.IsNullOrEmpty(config.Name))
                {
                    DebugLogger.Log(
                        "FilterSelectorViewModel",
                        $"Skipping filter with no name: {cached.FilterId}"
                    );
                    return Task.FromResult<PanelItem?>(null);
                }

                // Build description
                string description = config.Description ?? "No description";
                if (!string.IsNullOrEmpty(config.Author))
                {
                    description = $"by {config.Author}\n{description}";
                }

                var item = new PanelItem
                {
                    Title = config.Name,
                    Description = description,
                    Value = cached.FilePath,
                    GetImage = () => CreateFannedPreviewImageFromConfig(config),
                };

                return Task.FromResult<PanelItem?>(item);
            }
            catch (Exception ex)
            {
                DebugLogger.LogError(
                    "FilterSelectorViewModel",
                    $"Error creating panel item from cache for {cached.FilterId}: {ex.Message}"
                );
                return Task.FromResult<PanelItem?>(null);
            }
        }

        /// <summary>
        /// Creates a fanned preview image showing multiple cards from the filter config
        /// </summary>
        private IImage? CreateFannedPreviewImageFromConfig(Motely.Filters.MotelyJsonConfig config)
        {
            try
            {
                DebugLogger.Log("FilterSelectorViewModel", "Creating fanned preview image");

                // Collect items from all categories
                var previewItems = new List<(string value, string? type)>();

                // Check must items first
                if (config.Must != null)
                {
                    foreach (var item in config.Must.Take(4))
                    {
                        if (!string.IsNullOrEmpty(item.Value))
                        {
                            previewItems.Add((item.Value, item.Type));
                        }
                    }
                }

                foreach (var item in config.Should)
                {
                    if (!string.IsNullOrEmpty(item.Value))
                    {
                        previewItems.Add((item.Value, item.Type));
                    }
                }

                if (previewItems.Count == 0)
                {
                    DebugLogger.Log(
                        "FilterSelectorViewModel",
                        "No preview items found - returning null"
                    );
                    return null;
                }

                DebugLogger.Log(
                    "FilterSelectorViewModel",
                    $"Found {previewItems.Count} preview items"
                );

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
                        DebugLogger.Log(
                            "FilterSelectorViewModel",
                            $"Got image for {value} (type: {type})"
                        );
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

                DebugLogger.Log(
                    "FilterSelectorViewModel",
                    $"Successfully created fanned preview with {cardIndex} cards"
                );

                return renderBitmap;
            }
            catch (Exception ex)
            {
                DebugLogger.LogError(
                    "FilterSelectorViewModel",
                    $"Error creating fanned preview: {ex.Message}"
                );
                // Fallback to single image
                return GetFilterPreviewImageFromConfig(config);
            }
        }

        /// <summary>
        /// Gets a fallback single image preview from the filter config
        /// </summary>
        private IImage? GetFilterPreviewImageFromConfig(Motely.Filters.MotelyJsonConfig config)
        {
            try
            {
                if (config.Must != null && config.Must.Count > 0)
                {
                    var firstItem = config.Must[0];
                    if (!string.IsNullOrEmpty(firstItem.Value))
                    {
                        return GetItemImage(firstItem.Value, firstItem.Type);
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
                    var pixelSize = new PixelSize(
                        UIConstants.JokerSpriteWidth,
                        UIConstants.JokerSpriteHeight
                    );
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
                        DebugLogger.LogError(
                            "FilterSelectorViewModel",
                            $"Failed to create composite image: {ex.Message}"
                        );
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

            // Special handling for "The Soul" spectral card - composite with card base + gem overlay
            if (lowerType == "spectral" && value.Equals("soul", StringComparison.OrdinalIgnoreCase))
            {
                var cardBase = _spriteService.GetSpectralImage(value);
                var soulGem = _spriteService.GetSoulGemImage();

                if (cardBase != null && soulGem != null)
                {
                    var pixelSize = new PixelSize(
                        UIConstants.SpectralSpriteWidth,
                        UIConstants.SpectralSpriteHeight
                    );
                    var dpi = new Vector(96, 96);

                    try
                    {
                        var renderBitmap = new RenderTargetBitmap(pixelSize, dpi);

                        using (var context = renderBitmap.CreateDrawingContext())
                        {
                            // Draw card base first
                            var cardRect = new Rect(0, 0, pixelSize.Width, pixelSize.Height);
                            context.DrawImage(cardBase, cardRect);

                            // Draw soul gem overlay on top
                            context.DrawImage(soulGem, cardRect);
                        }

                        return renderBitmap;
                    }
                    catch (Exception ex)
                    {
                        DebugLogger.LogError(
                            "FilterSelectorViewModel",
                            $"Failed to create Soul composite image: {ex.Message}"
                        );
                        return soulGem ?? cardBase;
                    }
                }
                else
                {
                    return soulGem ?? cardBase;
                }
            }

            // Special handling for wildcard jokers - composite with base + mystery face overlay
            if ((lowerType == "joker" || lowerType == "souljoker") &&
                value.StartsWith("Wildcard_", StringComparison.OrdinalIgnoreCase))
            {
                var cardBase = _spriteService.GetJokerImage(value);
                var mysteryFace = _spriteService.GetMysteryJokerFaceImage();

                if (cardBase != null && mysteryFace != null)
                {
                    var pixelSize = new PixelSize(
                        UIConstants.JokerSpriteWidth,
                        UIConstants.JokerSpriteHeight
                    );
                    var dpi = new Vector(96, 96);

                    try
                    {
                        var renderBitmap = new RenderTargetBitmap(pixelSize, dpi);

                        using (var context = renderBitmap.CreateDrawingContext())
                        {
                            // Draw card base first
                            var cardRect = new Rect(0, 0, pixelSize.Width, pixelSize.Height);
                            context.DrawImage(cardBase, cardRect);

                            // Draw mystery face overlay on top
                            context.DrawImage(mysteryFace, cardRect);
                        }

                        return renderBitmap;
                    }
                    catch (Exception ex)
                    {
                        DebugLogger.LogError(
                            "FilterSelectorViewModel",
                            $"Failed to create wildcard joker composite image: {ex.Message}"
                        );
                        return mysteryFace ?? cardBase;
                    }
                }
                else
                {
                    return mysteryFace ?? cardBase;
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
                SelectButtonText =
                    SelectedFilter != null ? "USE THIS FILTER" : "SEARCH WITH THIS FILTER";
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
                    DebugLogger.Log(
                        "FilterSelectorViewModel",
                        $"Auto-loading filter: {SelectedFilter.Value} (AutoLoadEnabled={AutoLoadEnabled})"
                    );
                    FilterLoaded?.Invoke(this, SelectedFilter.Value);
                }
                else
                {
                    DebugLogger.Log(
                        "FilterSelectorViewModel",
                        $"Filter selected but NOT auto-loading: {SelectedFilter.Value} (AutoLoadEnabled={AutoLoadEnabled})"
                    );
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

        private bool CanSelectFilter() =>
            SelectedFilter != null && !string.IsNullOrEmpty(SelectedFilter.Value);

        private bool CanExecuteFilterAction() =>
            SelectedFilter != null && !string.IsNullOrEmpty(SelectedFilter.Value);

        [RelayCommand(CanExecute = nameof(CanSelectFilter))]
        private void SelectFilter()
        {
            if (SelectedFilter?.Value != null && !string.IsNullOrEmpty(SelectedFilter.Value))
            {
                DebugLogger.Log(
                    "FilterSelectorViewModel",
                    $"Loading filter: {SelectedFilter.Value}"
                );
                FilterLoaded?.Invoke(this, SelectedFilter.Value);
            }
        }

        [RelayCommand(CanExecute = nameof(CanExecuteFilterAction))]
        private void CopyFilter()
        {
            if (SelectedFilter?.Value != null)
            {
                DebugLogger.Log(
                    "FilterSelectorViewModel",
                    $"Copy requested for filter: {SelectedFilter.Value}"
                );
                FilterCopyRequested?.Invoke(this, SelectedFilter.Value);
            }
        }

        [RelayCommand(CanExecute = nameof(CanExecuteFilterAction))]
        private void EditFilter()
        {
            if (SelectedFilter?.Value != null)
            {
                DebugLogger.Log(
                    "FilterSelectorViewModel",
                    $"Edit requested for filter: {SelectedFilter.Value}"
                );
                FilterEditRequested?.Invoke(this, SelectedFilter.Value);
            }
        }

        [RelayCommand(CanExecute = nameof(CanExecuteFilterAction))]
        private void DeleteFilter()
        {
            if (SelectedFilter?.Value != null)
            {
                DebugLogger.Log(
                    "FilterSelectorViewModel",
                    $"Delete requested for filter: {SelectedFilter.Value}"
                );
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

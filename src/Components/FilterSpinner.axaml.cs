using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Oracle.Controls;
using Oracle.Services;
using Oracle.Helpers;

namespace Oracle.Components
{
    public partial class FilterSpinner : UserControl
    {
        private PanelSpinner? _panelSpinner;
        private readonly SpriteService _spriteService;
        private List<string> _availableFilters = new();
    
        public event EventHandler<string>? FilterSelected;
        public event EventHandler<string>? FilterLoaded;
    
        public bool AutoLoadEnabled { get; set; } = true;
    
        public FilterSpinner()
        {
            InitializeComponent();
            _spriteService = ServiceHelper.GetRequiredService<SpriteService>();
        }
    
        protected override void OnLoaded(RoutedEventArgs e)
        {
            base.OnLoaded(e);
        
            _panelSpinner = this.FindControl<PanelSpinner>("InnerPanelSpinner");
            if (_panelSpinner != null)
            {
                _panelSpinner.SelectionChanged += OnFilterSelectionChanged;
                LoadAvailableFilters();
            }
        }
    
        private void LoadAvailableFilters()
        {
            try
            {
                _availableFilters.Clear();

                // Look for .json files in the root directory
                var directory =
                    $"{Directory.GetCurrentDirectory()}/JsonItemFilters"
                    .Replace("//", "/");

                if (Directory.Exists(directory))
                {
                    var rootJsonFiles = Directory.GetFiles(directory, "*.json");
                    _availableFilters.AddRange(rootJsonFiles);
                }
            
                DebugLogger.Log("FilterSpinner", $"Found {_availableFilters.Count} filters");
            
                if (_panelSpinner == null)
                {
                    return;
                }

                if (_availableFilters.Count == 0)
                {
                    // Create a "no filters" item
                    var noFiltersItem = new PanelItem
                    {
                        Title = "No filters found",
                        Description = "Import a filter or create a new one",
                        Value = "",
                        GetImage = () => null
                    };
                    _panelSpinner.Items = new List<PanelItem> { noFiltersItem };
                }
                else
                {
                    // Create panel items from filters
                    var items = new List<PanelItem>();
                    foreach (var filterPath in _availableFilters)
                    {
                        var item = CreatePanelItemFromFilter(filterPath);
                        if (item != null)
                        {
                            items.Add(item);
                        }
                    }
                
                    if (items.Count > 0)
                    {
                        _panelSpinner.Items = items;
                    }
                }
            }
            catch (Exception ex)
            {
                DebugLogger.LogError("FilterSpinner", $"Error loading filters: {ex.Message}");
            }
        }
    
        private PanelItem? CreatePanelItemFromFilter(string filterPath)
        {
            try
            {
                var filterContent = File.ReadAllText(filterPath);
                using var doc = JsonDocument.Parse(filterContent);
            
                // Get filter name - skip if no name property
                if (!doc.RootElement.TryGetProperty("name", out var nameElement) || string.IsNullOrEmpty(nameElement.GetString()))
                {
                    DebugLogger.Log("FilterSpinner", $"Filter '{filterPath}' has no 'name' property - skipping");
                    return null;
                }
            
                var filterName = nameElement.GetString()!;
            
                // Get description
                string description = "No description";
                if (doc.RootElement.TryGetProperty("description", out var descElement))
                {
                    description = descElement.GetString() ?? "No description";
                }
            
                // Get author
                string? author = null;
                if (doc.RootElement.TryGetProperty("author", out var authorElement))
                {
                    author = authorElement.GetString();
                }
            
                // If author exists, prepend "by " to description
                if (!string.IsNullOrEmpty(author))
                {
                    description = $"by {author}\n{description}";
                }
            
                return new PanelItem
                {
                    Title = filterName,
                    Description = description,
                    Value = filterPath,
                    GetImage = () => CreateFilterPreviewImage(doc.RootElement)
                };
            }
            catch (Exception ex)
            {
                DebugLogger.LogError("FilterSpinner", $"Error parsing filter {filterPath}: {ex.Message}");
                return null;
            }
        }
    
        private IImage? CreateFilterPreviewImage(JsonElement filterRoot)
        {
            try
            {
                // Create a canvas to render the preview
                var canvas = new Canvas
                {
                    Width = 220,
                    Height = 65,
                    Background = Brushes.Transparent
                };
            
                // Collect items from different categories (simplified for now)
                var previewItems = new List<(string value, string? type)>();
            
                // Check for different filter formats and extract items
                if (filterRoot.TryGetProperty("filter_config", out var filterConfig))
                {
                    if (filterConfig.TryGetProperty("Must", out var must))
                    {
                        foreach (var item in must.EnumerateArray())
                        {
                            if (item.TryGetProperty("Value", out var value))
                            {
                                var val = value.GetString() ?? "";
                                var type = item.TryGetProperty("Type", out var typeEl) ? typeEl.GetString() : null;
                                previewItems.Add((val, type));
                                if (previewItems.Count >= 3)
                                {
                                    break; // Limit preview
                                }
                            }
                        }
                    }
                }
            
                // Add preview images to canvas
                int xOffset = 0;
                foreach (var (value, type) in previewItems.Take(3))
                {
                    var image = GetItemImage(value, type);
                    if (image != null)
                    {
                        var imageControl = new Image
                        {
                            Source = image,
                            Width = 64,
                            Height = 64
                        };
                        Canvas.SetLeft(imageControl, xOffset);
                        Canvas.SetTop(imageControl, 0);
                        canvas.Children.Add(imageControl);
                        xOffset += 70;
                    }
                }
            
                // For now, return null as we can't easily convert Canvas to IImage
                // In a real implementation, we'd render this to a bitmap
                return null;
            }
            catch (Exception ex)
            {
                DebugLogger.LogError("FilterSpinner", $"Error creating preview image: {ex.Message}");
                return null;
            }
        }
    
        private IImage? GetItemImage(string value, string? type)
        {
            // Simplified image loading based on type
            return type?.ToLower() switch
            {
                "jokers" => _spriteService.GetJokerImage(value),
                "vouchers" => _spriteService.GetVoucherImage(value),
                "tags" => _spriteService.GetTagImage(value),
                "bosses" => _spriteService.GetBossImage(value),
                _ => null
            };
        }
    
        private void OnFilterSelectionChanged(object? sender, PanelItem? item)
        {
            if (item?.Value is string filterPath && !string.IsNullOrEmpty(filterPath))
            {
                FilterSelected?.Invoke(this, filterPath);
            
                if (AutoLoadEnabled)
                {
                    DebugLogger.Log("FilterSpinner", $"Auto-loading filter: {filterPath}");
                    FilterLoaded?.Invoke(this, filterPath);
                }
            }
        }
    
        public void RefreshFilters()
        {
            LoadAvailableFilters();
        }
    
        public string? SelectedFilterPath => (_panelSpinner?.SelectedItem as PanelItem)?.Value as string;
    }
}
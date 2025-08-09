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
using Avalonia.Platform.Storage;
using Avalonia.Markup.Xaml;
using Oracle.Controls;
using Oracle.Helpers;
using Oracle.Services;
using Oracle.Views.Modals;

namespace Oracle.Components
{
    public partial class FilterSelector : UserControl
    {
        // Events
        public event EventHandler<string>? FilterSelected;
        public event EventHandler<string>? FilterLoaded;
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
        
        // Controls
        private PanelSpinner? _filterSpinner;
        private Button? _selectButton;
        private TextBox? _filterNameInput;
        private Button? _createFilterButton;
        
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
            _filterNameInput = this.FindControl<TextBox>("FilterNameInput");
            _createFilterButton = this.FindControl<Button>("CreateFilterButton");
            
            // Setup panel spinner
            if (_filterSpinner != null)
            {
                _filterSpinner.SelectionChanged += OnFilterSelectionChanged;
            }
        }
        
        protected override void OnLoaded(RoutedEventArgs e)
        {
            base.OnLoaded(e);
            LoadAvailableFilters();
            
            // Hide create section if not needed
            if (!ShowCreateButton && _filterNameInput != null && _createFilterButton != null)
            {
                _filterNameInput.IsVisible = false;
                _createFilterButton.IsVisible = false;
                
                // Also hide the separator
                var parent = _filterNameInput.Parent as StackPanel;
                if (parent?.Parent is StackPanel grandParent && grandParent.Children.Count > 2)
                {
                    // Hide the separator (should be the 3rd child)
                    if (grandParent.Children[2] is Border separator)
                    {
                        separator.IsVisible = false;
                    }
                }
            }
        }
        
        private void LoadAvailableFilters()
        {
            try
            {
                var filterItems = new List<PanelItem>();
                
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
                    var item = CreateFilterPanelItem(file);
                    if (item != null)
                        filterItems.Add(item);
                }
                
                DebugLogger.Log("FilterSelector", $"Found {filterItems.Count} filters");
                
                if (filterItems.Count == 0)
                {
                    // Add a placeholder item
                    filterItems.Add(new PanelItem
                    {
                        Title = "No filters found",
                        Description = "Import a filter or create a new one",
                        Value = ""
                    });
                }
                
                if (_filterSpinner != null)
                {
                    _filterSpinner.Items = filterItems;
                }
            }
            catch (Exception ex)
            {
                DebugLogger.LogError("FilterSelector", $"Error loading filters: {ex.Message}");
            }
        }
        
        private PanelItem? CreateFilterPanelItem(string filterPath)
        {
            try
            {
                var filterContent = File.ReadAllText(filterPath);
                using var doc = JsonDocument.Parse(filterContent);
                
                // Get filter name - skip if no name property
                if (!doc.RootElement.TryGetProperty("name", out var nameElement) || string.IsNullOrEmpty(nameElement.GetString()))
                {
                    DebugLogger.Log("FilterSelector", $"Filter '{filterPath}' has no 'name' property - skipping");
                    return null;
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
                
                // Clone the root element to use after the document is disposed
                var clonedRoot = doc.RootElement.Clone();
                
                return new PanelItem
                {
                    Title = filterName,
                    Description = description,
                    Value = filterPath,
                    GetImage = () => GetFilterPreviewImage(clonedRoot)
                };
            }
            catch (Exception ex)
            {
                DebugLogger.LogError("FilterSelector", $"Error parsing filter {filterPath}: {ex.Message}");
                return null;
            }
        }
        
        private IImage? GetFilterPreviewImage(JsonElement filterRoot)
        {
            try
            {
                // Collect items from different categories
                var previewItems = new List<(string value, string? type)>();
                
                // Check for items in the filter
                if (filterRoot.TryGetProperty("items", out var items))
                {
                    foreach (var item in items.EnumerateArray())
                    {
                        if (item.TryGetProperty("value", out var value) && 
                            item.TryGetProperty("type", out var type))
                        {
                            var val = value.GetString() ?? "";
                            var typeStr = type.GetString();
                            previewItems.Add((val, typeStr));
                            if (previewItems.Count >= 3) break; // Limit preview to 3 items
                        }
                    }
                }
                
                // If no items found, return null
                if (previewItems.Count == 0)
                    return null;
                
                // Get the first item's image as the preview
                var (firstValue, firstType) = previewItems[0];
                return GetItemImage(firstValue, firstType);
            }
            catch (Exception ex)
            {
                DebugLogger.LogError("FilterSelector", $"Error creating preview image: {ex.Message}");
                return null;
            }
        }
        
        private IImage? GetItemImage(string value, string? type)
        {
            // Get image based on type
            return type?.ToLower() switch
            {
                "joker" => _spriteService.GetJokerImage(value),
                "voucher" => _spriteService.GetVoucherImage(value),
                "tag" => _spriteService.GetTagImage(value),
                "boss" => _spriteService.GetBossImage(value),
                "spectral" => _spriteService.GetSpectralImage(value),
                "tarot" => _spriteService.GetTarotImage(value),
                _ => null
            };
        }
        
        private void OnFilterSelectionChanged(object? sender, PanelItem? item)
        {
            if (item?.Value != null && !string.IsNullOrEmpty(item.Value))
            {
                FilterSelected?.Invoke(this, item.Value);
                
                // Auto-load if enabled
                if (_autoLoadEnabled)
                {
                    DebugLogger.Log("FilterSelector", $"Auto-loading filter: {item.Value}");
                    FilterLoaded?.Invoke(this, item.Value);
                }
            }
        }
        
        private void OnSelectClick(object? sender, RoutedEventArgs e)
        {
            var selectedItem = _filterSpinner?.SelectedItem;
            if (selectedItem?.Value != null && !string.IsNullOrEmpty(selectedItem.Value))
            {
                FilterLoaded?.Invoke(this, selectedItem.Value);
            }
        }
        
        private void OnFilterNameTextChanged(object? sender, TextChangedEventArgs e)
        {
            if (_createFilterButton != null && _filterNameInput != null)
            {
                _createFilterButton.IsEnabled = !string.IsNullOrWhiteSpace(_filterNameInput.Text);
            }
        }
        
        private void OnCreateFilterClick(object? sender, RoutedEventArgs e)
        {
            if (!string.IsNullOrWhiteSpace(_filterNameInput?.Text))
            {
                ShouldSwitchToVisualTab = true;
                NewFilterRequested?.Invoke(this, EventArgs.Empty);
            }
        }
        
        public void RefreshFilters()
        {
            LoadAvailableFilters();
        }
        
        public string? GetNewFilterName()
        {
            return _filterNameInput?.Text;
        }
    }
}
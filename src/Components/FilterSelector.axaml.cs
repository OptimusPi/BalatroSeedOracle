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
                
                // Look for .json files in the root directory
                var directory = Directory.GetCurrentDirectory();
                var rootJsonFiles = Directory.GetFiles(directory, "*.json");
                
                foreach (var file in rootJsonFiles)
                {
                    var item = CreateFilterPanelItem(file);
                    if (item != null)
                        filterItems.Add(item);
                }
                
                // Also look for .json files in JsonItemConfigs directory
                var jsonConfigsDir = Path.Combine(directory, "JsonItemConfigs");
                if (Directory.Exists(jsonConfigsDir))
                {
                    var files = Directory.GetFiles(jsonConfigsDir, "*.json");
                    foreach (var file in files)
                    {
                        var item = CreateFilterPanelItem(file);
                        if (item != null)
                            filterItems.Add(item);
                    }
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
                
                return new PanelItem
                {
                    Title = filterName,
                    Description = description,
                    Value = filterPath,
                    GetImage = () => GetFilterPreviewImage(doc.RootElement)
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
            // For now, return null - we could generate a preview image later
            // showing the first few items from the filter
            return null;
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
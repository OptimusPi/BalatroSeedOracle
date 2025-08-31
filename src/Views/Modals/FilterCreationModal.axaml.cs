using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Avalonia.Media.Imaging;
using BalatroSeedOracle.Controls;
using BalatroSeedOracle.Helpers;
using BalatroSeedOracle.Models;
using BalatroSeedOracle.Services;

namespace BalatroSeedOracle.Views.Modals
{
    public partial class FilterCreationModal : UserControl
    {
        public event EventHandler? CloseRequested;
        
        private PanelSpinner? _filterSpinner;
        private string? _selectedFilterPath;

        public FilterCreationModal()
        {
            InitializeComponent();

            // Wire up events
            var closeButton = this.FindControl<Button>("CloseButton");
            if (closeButton != null)
            {
                closeButton.Click += OnCloseButtonClick;
            }
            
            var createCopyButton = this.FindControl<Button>("CreateCopyButton");
            if (createCopyButton != null)
            {
                createCopyButton.Click += OnCreateCopyClick;
            }
            
            var startBlankButton = this.FindControl<Button>("StartBlankButton");
            if (startBlankButton != null)
            {
                startBlankButton.Click += OnStartBlankClick;
            }
            
            _filterSpinner = this.FindControl<PanelSpinner>("FilterSpinner");
            if (_filterSpinner != null)
            {
                _filterSpinner.SelectionChanged += OnFilterSelectionChanged;
                _filterSpinner.PageIndicatorLabel = "Filter";
            }
            
            // Load available filters
            LoadFilters();
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }

        private void LoadFilters()
        {
            try
            {
                var items = new List<PanelItem>();
                
                // Find the filters directory
                var possiblePaths = new[]
                {
                    Path.Combine(Directory.GetCurrentDirectory(), "JsonItemFilters"),
                    Path.Combine(Directory.GetCurrentDirectory(), "external", "Motely", "Motely", "JsonItemFilters"),
                    Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "JsonItemFilters"),
                    Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "external", "Motely", "Motely", "JsonItemFilters")
                };

                string? filterDirectory = null;
                foreach (var path in possiblePaths)
                {
                    if (Directory.Exists(path))
                    {
                        filterDirectory = path;
                        break;
                    }
                }

                if (string.IsNullOrEmpty(filterDirectory))
                {
                    DebugLogger.LogError("FilterCreationModal", "No filter directory found");
                    
                    // Add a default "no filters" item
                    items.Add(new PanelItem
                    {
                        Title = "No Filters Found",
                        Description = "Create your first filter to get started!",
                        Value = "",
                        GetImage = () => null
                    });
                }
                else
                {
                    // Load all JSON files
                    var jsonFiles = Directory.GetFiles(filterDirectory, "*.json").OrderBy(f => Path.GetFileName(f)).ToArray();
                    
                    if (jsonFiles.Length == 0)
                    {
                        // No filters found, add a placeholder
                        items.Add(new PanelItem
                        {
                            Title = "No Filters Found", 
                            Description = "Start by creating a new filter!",
                            Value = "",
                            GetImage = () => null
                        });
                    }
                    else
                    {
                        foreach (var file in jsonFiles)
                        {
                            try
                            {
                                var content = File.ReadAllText(file);
                                using var doc = JsonDocument.Parse(content);
                                
                                var title = Path.GetFileNameWithoutExtension(file);
                                var description = "Custom filter";
                                string? firstJoker = null;
                                
                                // Try to get metadata from JSON
                                if (doc.RootElement.TryGetProperty("name", out var nameElement))
                                {
                                    title = nameElement.GetString() ?? title;
                                }
                                
                                if (doc.RootElement.TryGetProperty("description", out var descElement))
                                {
                                    description = descElement.GetString() ?? description;
                                }
                                
                                // Try to find first joker for preview image
                                firstJoker = GetFirstJokerFromFilter(doc.RootElement);
                                
                                var filePath = file; // Capture for closure
                                items.Add(new PanelItem
                                {
                                    Title = title,
                                    Description = description,
                                    Value = filePath,
                                    GetImage = () => 
                                    {
                                        if (!string.IsNullOrEmpty(firstJoker))
                                        {
                                            return SpriteService.Instance.GetJokerImage(firstJoker);
                                        }
                                        return SpriteService.Instance.GetJokerImage("Joker"); // Default joker image
                                    }
                                });
                            }
                            catch (Exception ex)
                            {
                                DebugLogger.LogError("FilterCreationModal", $"Error loading filter {file}: {ex.Message}");
                            }
                        }
                    }
                }
                
                // Set the items on the spinner
                if (_filterSpinner != null)
                {
                    _filterSpinner.Items = items;
                    if (items.Count > 0)
                    {
                        _filterSpinner.SelectedIndex = 0;
                        _selectedFilterPath = items[0].Value;
                    }
                }
                
                DebugLogger.Log("FilterCreationModal", $"Loaded {items.Count} filters");
            }
            catch (Exception ex)
            {
                DebugLogger.LogError("FilterCreationModal", $"Error loading filters: {ex.Message}");
            }
        }

        private string? GetFirstJokerFromFilter(JsonElement filterRoot)
        {
            try
            {
                // Check for needs/wants sections
                if (filterRoot.TryGetProperty("needs", out var needs))
                {
                    var joker = GetFirstJokerFromSection(needs);
                    if (joker != null) return joker;
                }
                
                if (filterRoot.TryGetProperty("wants", out var wants))
                {
                    var joker = GetFirstJokerFromSection(wants);
                    if (joker != null) return joker;
                }
                
                // Check for antes structure (older format)
                if (filterRoot.TryGetProperty("antes", out var antes))
                {
                    foreach (var ante in antes.EnumerateObject())
                    {
                        if (ante.Value.TryGetProperty("needs", out var anteNeeds))
                        {
                            var joker = GetFirstJokerFromSection(anteNeeds);
                            if (joker != null) return joker;
                        }
                        if (ante.Value.TryGetProperty("wants", out var anteWants))
                        {
                            var joker = GetFirstJokerFromSection(anteWants);
                            if (joker != null) return joker;
                        }
                    }
                }
            }
            catch
            {
                // Ignore errors
            }
            
            return null;
        }

        private string? GetFirstJokerFromSection(JsonElement section)
        {
            try
            {
                // Look for jokers array
                if (section.TryGetProperty("jokers", out var jokers) && jokers.ValueKind == JsonValueKind.Array)
                {
                    foreach (var joker in jokers.EnumerateArray())
                    {
                        var jokerName = joker.GetString();
                        if (!string.IsNullOrEmpty(jokerName))
                        {
                            return jokerName;
                        }
                    }
                }
                
                // Look for items array (newer format)
                if (section.ValueKind == JsonValueKind.Array)
                {
                    foreach (var item in section.EnumerateArray())
                    {
                        if (item.TryGetProperty("type", out var type) && 
                            type.GetString()?.ToLower() == "joker" &&
                            item.TryGetProperty("value", out var value))
                        {
                            var jokerName = value.GetString();
                            if (!string.IsNullOrEmpty(jokerName))
                            {
                                return jokerName;
                            }
                        }
                    }
                }
            }
            catch
            {
                // Ignore errors
            }
            
            return null;
        }

        private void OnFilterSelectionChanged(object? sender, PanelItem? item)
        {
            if (item != null)
            {
                _selectedFilterPath = item.Value;
                DebugLogger.Log("FilterCreationModal", $"Selected filter: {item.Title}");
            }
        }

        private void OnCreateCopyClick(object? sender, RoutedEventArgs e)
        {
            if (string.IsNullOrEmpty(_selectedFilterPath))
            {
                DebugLogger.LogError("FilterCreationModal", "No filter selected");
                return;
            }
            
            // TODO: Open the filter editor with the selected filter as template
            DebugLogger.Log("FilterCreationModal", $"Creating copy of: {_selectedFilterPath}");
            
            // Close this modal
            CloseRequested?.Invoke(this, EventArgs.Empty);
        }

        private void OnStartBlankClick(object? sender, RoutedEventArgs e)
        {
            // TODO: Open the filter editor with a blank filter
            DebugLogger.Log("FilterCreationModal", "Starting with blank filter");
            
            // Close this modal
            CloseRequested?.Invoke(this, EventArgs.Empty);
        }

        private void OnCloseButtonClick(object? sender, RoutedEventArgs e)
        {
            CloseRequested?.Invoke(this, EventArgs.Empty);
        }
    }
}
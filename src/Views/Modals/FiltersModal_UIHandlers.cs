using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Threading;

namespace Oracle.Views.Modals;

// UI Handlers for FiltersModal - part of the FiltersModalContent partial class
public partial class FiltersModalContent
{
    // Add these methods to FiltersModal.axaml.cs to support the new UI

    private List<string> _availableFilters = new();
    private int _currentFilterIndex = 0;

    private void OnFilterLeftArrowClick(object? sender, RoutedEventArgs e)
{
    if (_availableFilters.Count == 0)
    {
        LoadAvailableFilters();
    }
    
    if (_availableFilters.Count > 0)
    {
        _currentFilterIndex--;
        if (_currentFilterIndex < 0)
            _currentFilterIndex = _availableFilters.Count - 1;
        
        UpdateFilterPreview();
    }
}

private void OnFilterRightArrowClick(object? sender, RoutedEventArgs e)
{
    if (_availableFilters.Count == 0)
    {
        LoadAvailableFilters();
    }
    
    if (_availableFilters.Count > 0)
    {
        _currentFilterIndex++;
        if (_currentFilterIndex >= _availableFilters.Count)
            _currentFilterIndex = 0;
        
        UpdateFilterPreview();
    }
}

private void LoadAvailableFilters()
{
    try
    {
        _availableFilters.Clear();
        
        // Check local filters directory
        var filtersDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "filters");
        if (Directory.Exists(filtersDir))
        {
            var files = Directory.GetFiles(filtersDir, "*.json");
            _availableFilters.AddRange(files);
        }
        
        // If no filters found, show placeholder
        if (_availableFilters.Count == 0)
        {
            var filterNameText = this.FindControl<TextBlock>("FilterPreviewName");
            var filterDescText = this.FindControl<TextBlock>("FilterPreviewDesc");
            
            if (filterNameText != null)
                filterNameText.Text = "No filters found";
            if (filterDescText != null)
                filterDescText.Text = "Create a new filter or import an existing one";
            
            // Disable the Use Selected button
            var useButton = this.FindControl<Button>("UseSelectedFilterButton");
            if (useButton != null)
                useButton.IsEnabled = false;
        }
        else
        {
            _currentFilterIndex = 0;
            UpdateFilterPreview();
        }
    }
    catch (Exception ex)
    {
        Oracle.Helpers.DebugLogger.LogError("FiltersModal", $"Error loading filters: {ex.Message}");
    }
}

private void UpdateFilterPreview()
{
    if (_availableFilters.Count == 0 || _currentFilterIndex < 0 || _currentFilterIndex >= _availableFilters.Count)
        return;
    
    try
    {
        var filterPath = _availableFilters[_currentFilterIndex];
        var filterContent = File.ReadAllText(filterPath);
        
        // Parse the filter to get name and description
        using var doc = JsonDocument.Parse(filterContent);
        
        var filterNameText = this.FindControl<TextBlock>("FilterPreviewName");
        var filterDescText = this.FindControl<TextBlock>("FilterPreviewDesc");
        var useButton = this.FindControl<Button>("UseSelectedFilterButton");
        
        // Get filter name
        string filterName = Path.GetFileNameWithoutExtension(filterPath);
        if (doc.RootElement.TryGetProperty("name", out var nameElement))
        {
            filterName = nameElement.GetString() ?? filterName;
        }
        
        // Get filter description
        string filterDesc = "No description";
        if (doc.RootElement.TryGetProperty("description", out var descElement))
        {
            filterDesc = descElement.GetString() ?? "No description";
        }
        
        // Count items in filter
        int itemCount = 0;
        if (doc.RootElement.TryGetProperty("filter_config", out var config))
        {
            if (config.TryGetProperty("Must", out var must))
                itemCount += must.GetArrayLength();
            if (config.TryGetProperty("Should", out var should))
                itemCount += should.GetArrayLength();
            if (config.TryGetProperty("MustNot", out var mustNot))
                itemCount += mustNot.GetArrayLength();
        }
        
        // Update UI
        if (filterNameText != null)
            filterNameText.Text = filterName;
        
        if (filterDescText != null)
            filterDescText.Text = $"{filterDesc} ({itemCount} items)";
        
        if (useButton != null)
            useButton.IsEnabled = true;
        
        // Store the current filter path
        _currentConfigPath = filterPath;
        
        // TODO: Update the fanned cards display (FilterCardsCanvas) with item previews
        UpdateFilterCardsPreview(doc.RootElement);
    }
    catch (Exception ex)
    {
        Oracle.Helpers.DebugLogger.LogError("FiltersModal", $"Error updating filter preview: {ex.Message}");
    }
}

private void UpdateFilterCardsPreview(JsonElement filterRoot)
{
    var canvas = this.FindControl<Canvas>("FilterCardsCanvas");
    if (canvas == null) return;
    
    canvas.Children.Clear();
    
    // Get first few items from the filter to show as cards
    var items = new List<string>();
    
    if (filterRoot.TryGetProperty("filter_config", out var config))
    {
        // Get up to 5 items total from Must/Should lists
        if (config.TryGetProperty("Must", out var must))
        {
            foreach (var item in must.EnumerateArray())
            {
                if (item.TryGetProperty("Value", out var value))
                {
                    items.Add(value.GetString() ?? "");
                    if (items.Count >= 5) break;
                }
            }
        }
        
        if (items.Count < 5 && config.TryGetProperty("Should", out var should))
        {
            foreach (var item in should.EnumerateArray())
            {
                if (item.TryGetProperty("Value", out var value))
                {
                    items.Add(value.GetString() ?? "");
                    if (items.Count >= 5) break;
                }
            }
        }
    }
    
    // Create fanned card display
    for (int i = 0; i < Math.Min(items.Count, 5); i++)
    {
        var card = new Border
        {
            Width = 50,
            Height = 70,
            Background = new SolidColorBrush(Color.Parse("#2a2a2a")),
            BorderBrush = new SolidColorBrush(Color.Parse("#444444")),
            BorderThickness = new Thickness(2),
            CornerRadius = new CornerRadius(4)
        };
        
        // Position cards in a fan
        Canvas.SetLeft(card, 75 + i * 15);
        Canvas.SetTop(card, Math.Abs(i - 2) * 5); // Create arc effect
        
        // Rotate cards
        card.RenderTransform = new RotateTransform((i - 2) * 5);
        card.RenderTransformOrigin = new RelativePoint(0.5, 1, RelativeUnit.Relative);
        
        canvas.Children.Add(card);
    }
}

private void OnImportFilterClick(object? sender, RoutedEventArgs e)
{
    // Same as OnLoadClick but specifically for importing
    OnLoadClick(sender, e);
}

private async void OnUseSelectedFilterClick(object? sender, RoutedEventArgs e)
{
    if (string.IsNullOrEmpty(_currentConfigPath) || !File.Exists(_currentConfigPath))
    {
        UpdateStatus("No filter selected", true);
        return;
    }
    
    try
    {
        // Load the selected filter
        await LoadConfigAsync(_currentConfigPath);
        
        // Enable tabs and switch to Visual tab
        UpdateTabStates(true);
        
        var visualTab = this.FindControl<Button>("VisualTab");
        if (visualTab != null)
        {
            OnTabClick(visualTab, new RoutedEventArgs());
        }
        
        Oracle.Helpers.DebugLogger.Log("FiltersModal", $"Loaded filter: {_currentConfigPath}");
    }
    catch (Exception ex)
    {
        Oracle.Helpers.DebugLogger.LogError("FiltersModal", $"Error using selected filter: {ex.Message}");
        UpdateStatus($"Error loading filter: {ex.Message}", true);
    }
}

// Initialize the filter list when the modal opens
protected override void OnLoaded(RoutedEventArgs e)
{
    base.OnLoaded(e);
    
    // Load available filters when the modal opens
    Dispatcher.UIThread.Post(() =>
    {
        LoadAvailableFilters();
    }, DispatcherPriority.Background);
}

}

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Animation;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Media.Transformation;
using BalatroSeedOracle.Services;
using BalatroSeedOracle.Helpers;

namespace BalatroSeedOracle.Components;

public partial class ChallengesFilterSelector : UserControl
{
    private const int ITEMS_PER_PAGE = 10;
    
    private List<string> _availableFilters = new();
    private List<string> _filteredFilters = new();
    private string _searchQuery = string.Empty;
    private int _currentPage = 0;
    private int _selectedIndex = -1;
    private string? _selectedFilterPath;
    
    private StackPanel? _filterListPanel;
    private TextBlock? _selectedFilterName;
    private StackPanel? _mustHavePanel;
    private StackPanel? _shouldHavePanel;
    private StackPanel? _mustNotHavePanel;
    private TextBlock? _pageIndicator;
    private TextBlock? _statusText;
    private Button? _prevPageButton;
    private Button? _nextPageButton;
    private Button? _selectButton;
    private TextBox? _searchBox;
    private TextBlock? _mustCountText;
    private TextBlock? _shouldCountText;
    private TextBlock? _mustNotCountText;
    
    public event EventHandler<string?>? FilterSelected;
    
    public ChallengesFilterSelector()
    {
        InitializeComponent();
        
        // Enable keyboard navigation
        this.Focusable = true;
        this.KeyDown += OnKeyDown;
    }
    
    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
        
        _filterListPanel = this.FindControl<StackPanel>("FilterListPanel");
        _selectedFilterName = this.FindControl<TextBlock>("SelectedFilterName");
        _mustHavePanel = this.FindControl<StackPanel>("MustHavePanel");
        _shouldHavePanel = this.FindControl<StackPanel>("ShouldHavePanel");
        _mustNotHavePanel = this.FindControl<StackPanel>("MustNotHavePanel");
        _pageIndicator = this.FindControl<TextBlock>("PageIndicator");
        _statusText = this.FindControl<TextBlock>("StatusText");
        _prevPageButton = this.FindControl<Button>("PrevPageButton");
        _nextPageButton = this.FindControl<Button>("NextPageButton");
        _selectButton = this.FindControl<Button>("SelectButton");
        _searchBox = this.FindControl<TextBox>("SearchBox");
        _mustCountText = this.FindControl<TextBlock>("MustCountText");
        _shouldCountText = this.FindControl<TextBlock>("ShouldCountText");
        _mustNotCountText = this.FindControl<TextBlock>("MustNotCountText");
        
        if (_searchBox != null)
        {
            _searchBox.TextChanged += OnSearchTextChanged;
        }
        
        Loaded += OnLoaded;
    }
    
    private async void OnLoaded(object? _, RoutedEventArgs __)
    {
        await RefreshFilters();
    }
    
    public async Task RefreshFilters()
    {
        try
        {
            _availableFilters.Clear();
            
            var filterDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "JsonItemFilters");
            if (Directory.Exists(filterDir))
            {
                var files = Directory.GetFiles(filterDir, "*.json")
                    .Select(Path.GetFileNameWithoutExtension)
                    .Where(name => !string.IsNullOrEmpty(name))
                    .OrderBy(name => name)
                    .ToList();
                
                _availableFilters = files!;
            }
            
            // Add "Create New" option at the beginning
            _availableFilters.Insert(0, "📝 Create New Filter");
            
            _currentPage = 0;
            UpdateFilterList();
            UpdateStatus();
        }
        catch (Exception ex)
        {
            DebugLogger.LogError($"Failed to refresh filters: {ex.Message}");
        }
    }
    
    private void OnSearchTextChanged(object? sender, TextChangedEventArgs e)
    {
        if (_searchBox == null) return;
        
        _searchQuery = _searchBox.Text ?? string.Empty;
        FilterAndUpdateList();
    }
    
    private void FilterAndUpdateList()
    {
        if (string.IsNullOrWhiteSpace(_searchQuery))
        {
            _filteredFilters = new List<string>(_availableFilters);
        }
        else
        {
            _filteredFilters = _availableFilters
                .Where(f => f.Contains(_searchQuery, StringComparison.OrdinalIgnoreCase))
                .ToList();
                
            // Always keep "Create New" at the top
            if (!_filteredFilters.Contains("📝 Create New Filter") && 
                _availableFilters.Contains("📝 Create New Filter"))
            {
                _filteredFilters.Insert(0, "📝 Create New Filter");
            }
        }
        
        _currentPage = 0;
        UpdateFilterList();
        UpdateStatus();
    }
    
    private void UpdateFilterList()
    {
        if (_filterListPanel == null) return;
        
        _filterListPanel.Children.Clear();
        
        int startIndex = _currentPage * ITEMS_PER_PAGE;
        int endIndex = Math.Min(startIndex + ITEMS_PER_PAGE, _filteredFilters.Count);
        
        for (int i = startIndex; i < endIndex; i++)
        {
            var filterName = _filteredFilters[i];
            var actualIndex = _availableFilters.IndexOf(filterName);
            var button = new Button
            {
                Classes = { "filter-item" },
                Tag = actualIndex
            };
            
            // Special styling for "Create New"
            if (actualIndex == 0)
            {
                button.Classes.Add("create-new");
            }
            
            // Check if selected
            if (actualIndex == _selectedIndex)
            {
                button.Classes.Add("selected");
            }
            
            // Create button content
            var grid = new Grid();
            grid.ColumnDefinitions.Add(new ColumnDefinition(30, GridUnitType.Pixel));
            grid.ColumnDefinitions.Add(new ColumnDefinition(1, GridUnitType.Star));
            grid.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Auto));
            
            // Item number (skip for Create New)
            if (actualIndex > 0)
            {
                System.Diagnostics.Debug.Assert(Application.Current != null, "Application.Current should not be null when building filter list");
                var fontFam = Application.Current?.FindResource("BalatroFont") as Avalonia.Media.FontFamily;
                var whiteBrush = Application.Current?.FindResource("White") as Avalonia.Media.IBrush;
                System.Diagnostics.Debug.Assert(fontFam != null, "BalatroFont resource missing");
                System.Diagnostics.Debug.Assert(whiteBrush != null, "White brush resource missing");
                var numberText = new TextBlock
                {
                    Text = actualIndex.ToString(),
                    FontFamily = fontFam,
                    FontSize = 14,
                    Foreground = whiteBrush,
                    VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center
                };
                Grid.SetColumn(numberText, 0);
                grid.Children.Add(numberText);
            }
            
            // Filter name
            System.Diagnostics.Debug.Assert(Application.Current != null);
            var fontFam2 = Application.Current?.FindResource("BalatroFont") as Avalonia.Media.FontFamily;
            var whiteBrush2 = Application.Current?.FindResource("White") as Avalonia.Media.IBrush;
            System.Diagnostics.Debug.Assert(fontFam2 != null && whiteBrush2 != null);
            var nameText = new TextBlock
            {
                Text = filterName,
                FontFamily = fontFam2,
                FontSize = 14,
                Foreground = whiteBrush2,
                VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center
            };
            Grid.SetColumn(nameText, 1);
            grid.Children.Add(nameText);
            
            // Selection arrow (visible when selected)
            if (actualIndex == _selectedIndex)
            {
                var arrow = new TextBlock
                {
                    Text = "▶",
                    FontSize = 12,
                    Foreground = Application.Current.FindResource("Gold") as Avalonia.Media.IBrush,
                    VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center
                };
                Grid.SetColumn(arrow, 2);
                grid.Children.Add(arrow);
            }
            
            button.Content = grid;
            button.Click += OnFilterItemClick;
            
            _filterListPanel.Children.Add(button);
        }
        
        UpdatePageIndicator();
    }
    
    private void OnFilterItemClick(object? sender, RoutedEventArgs e)
    {
        if (sender is Button button && button.Tag is int index)
        {
            _selectedIndex = index;
            
            if (index == 0)
            {
                // Create new filter
                _selectedFilterPath = null;
                UpdatePreview(null);
            }
            else
            {
                // Load existing filter
                var filterName = _availableFilters[index];
                _selectedFilterPath = Path.Combine(
                    AppDomain.CurrentDomain.BaseDirectory, 
                    "JsonItemFilters", 
                    $"{filterName}.json"
                );
                UpdatePreview(_selectedFilterPath);
            }
            
            UpdateFilterList();
            
            if (_selectButton is not null)
                _selectButton.IsEnabled = true;
        }
    }
    
    private async void UpdatePreview(string? filterPath)
    {
        if (_selectedFilterName == null) return;
        
        // Clear preview panels
        _mustHavePanel?.Children.Clear();
        _shouldHavePanel?.Children.Clear();
        _mustNotHavePanel?.Children.Clear();
        
        if (filterPath == null)
        {
            _selectedFilterName.Text = "Create New Filter";
            return;
        }
        
        try
        {
            if (File.Exists(filterPath))
            {
                var filterName = Path.GetFileNameWithoutExtension(filterPath);
                _selectedFilterName.Text = filterName;
                
                // Load and parse filter JSON
                var json = await File.ReadAllTextAsync(filterPath);
                using var doc = System.Text.Json.JsonDocument.Parse(json);
                var root = doc.RootElement;
                
                // Parse and display must items
                if (root.TryGetProperty("must", out var mustArray))
                {
                    DisplayFilterItems(_mustHavePanel, mustArray);
                }
                
                // Parse and display should items
                if (root.TryGetProperty("should", out var shouldArray))
                {
                    DisplayFilterItems(_shouldHavePanel, shouldArray);
                }
                
                // Parse and display mustNot items
                if (root.TryGetProperty("mustNot", out var mustNotArray))
                {
                    DisplayFilterItems(_mustNotHavePanel, mustNotArray);
                }
            }
        }
        catch (Exception ex)
        {
            DebugLogger.LogError($"Failed to load filter preview: {ex.Message}");
        }
    }
    
    private void DisplayFilterItems(StackPanel? panel, System.Text.Json.JsonElement items)
    {
        if (panel == null) return;
        
        try
        {
            int itemCount = 0;
            
            if (items.ValueKind == System.Text.Json.JsonValueKind.Array)
            {
                foreach (var item in items.EnumerateArray())
                {
                    if (!item.TryGetProperty("type", out var typeElement) ||
                        !item.TryGetProperty("value", out var valueElement))
                        continue;
                    
                    var type = typeElement.GetString()?.ToLowerInvariant();
                    var value = valueElement.GetString();
                    
                    if (string.IsNullOrEmpty(type) || string.IsNullOrEmpty(value))
                        continue;
                    
                    itemCount++;
                    
                    // Create container for sprite with hover effect
                    var container = new Border
                    {
                        Background = Brushes.Transparent,
                        CornerRadius = new CornerRadius(4),
                        Padding = new Thickness(2),
                        Margin = new Thickness(2),
                        Cursor = new Avalonia.Input.Cursor(Avalonia.Input.StandardCursorType.Hand),
                        Transitions = new Transitions
                        {
                            new DoubleTransition { Property = Border.OpacityProperty, Duration = TimeSpan.FromMilliseconds(150) }
                        }
                    };
                    
                    // Create image based on type
                    IImage? sprite = null;
                    var spriteService = SpriteService.Instance;
                    
                    switch (type)
                    {
                        case "joker":
                            sprite = spriteService.GetJokerImage(value);
                            break;
                        case "souljoker":
                            sprite = spriteService.GetJokerSoulImage(value);
                            break;
                        case "tag":
                        case "smallblindtag":
                        case "bigblindtag":
                            sprite = spriteService.GetTagImage(value);
                            break;
                        case "tarotcard":
                            sprite = spriteService.GetTarotImage(value);
                            break;
                        case "spectralcard":
                            sprite = spriteService.GetSpectralImage(value);
                            break;
                        case "planetcard":
                            sprite = spriteService.GetPlanetCardImage(value);
                            break;
                        case "voucher":
                            sprite = spriteService.GetVoucherImage(value);
                            break;
                        case "boss":
                            sprite = spriteService.GetBossImage(value);
                            break;
                        case "playingcard":
                            // Playing cards need special handling for rank/suit
                            if (value.Length >= 2)
                            {
                                sprite = spriteService.GetPlayingCardImage(value, null, null, null);
                            }
                            break;
                    }
                    
                    if (sprite != null)
                    {
                        var image = new Image
                        {
                            Source = sprite,
                            Width = type == "tag" ? 36 : 56,  // Slightly bigger
                            Height = type == "tag" ? 36 : 75,
                            Stretch = Avalonia.Media.Stretch.Uniform
                        };
                        
                        container.Child = image;
                        
                        // Add hover effect
                        container.PointerEntered += (s, e) => 
                        {
                            container.RenderTransform = new ScaleTransform(1.1, 1.1);
                            container.Background = Application.Current!.FindResource("BlueDarker") as IBrush;
                        };
                        container.PointerExited += (s, e) => 
                        {
                            container.RenderTransform = new ScaleTransform(1.0, 1.0);
                            container.Background = Brushes.Transparent;
                        };
                        
                        // Add tooltip with item name
                        var displayType = type switch
                        {
                            "joker" => "Joker",
                            "souljoker" => "Soul Joker",
                            "tag" => "Tag",
                            "smallblindtag" => "Small Blind Tag",
                            "bigblindtag" => "Big Blind Tag",
                            "tarotcard" => "Tarot",
                            "spectralcard" => "Spectral",
                            "planetcard" => "Planet",
                            "voucher" => "Voucher",
                            "boss" => "Boss",
                            "playingcard" => "Playing Card",
                            _ => type
                        };
                        
                        var tooltip = $"{displayType}: {value}";
                        if (item.TryGetProperty("antes", out var antes))
                        {
                            tooltip += $" (Ante {antes})";
                        }
                        ToolTip.SetTip(container, tooltip);
                        
                        panel.Children.Add(container);
                    }
                    else
                    {
                        // Fallback text for items without sprites
                        var text = new Border
                        {
                            Background = Application.Current!.FindResource("GreyDarker") as Avalonia.Media.IBrush,
                            CornerRadius = new CornerRadius(4),
                            Padding = new Thickness(8, 4),
                            Margin = new Thickness(2),
                            Child = new TextBlock
                            {
                                Text = value,
                                FontFamily = Application.Current.FindResource("BalatroFont") as Avalonia.Media.FontFamily,
                                FontSize = 11,
                                Foreground = Application.Current.FindResource("Silver") as Avalonia.Media.IBrush
                            }
                        };
                        panel.Children.Add(text);
                    }
                }
            }
            
            // Update count badges
            if (panel.Name == "MustHavePanel" && _mustCountText != null)
            {
                _mustCountText.Text = itemCount.ToString();
            }
            else if (panel.Name == "ShouldHavePanel" && _shouldCountText != null)
            {
                _shouldCountText.Text = itemCount.ToString();
            }
            else if (panel.Name == "MustNotHavePanel" && _mustNotCountText != null)
            {
                _mustNotCountText.Text = itemCount.ToString();
            }
            
            // If no items, show empty message with icon
            if (panel.Children.Count == 0)
            {
                var emptyContainer = new StackPanel
                {
                    Orientation = Orientation.Horizontal,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Center,
                    Spacing = 6
                };
                
                var emptyIcon = new TextBlock
                {
                    Text = "📦",
                    FontSize = 24,
                    Opacity = 0.5
                };
                
                var emptyText = new TextBlock
                {
                    Text = "Empty",
                    FontFamily = Application.Current!.FindResource("BalatroFont") as Avalonia.Media.FontFamily,
                    FontSize = 12,
                    Foreground = Application.Current.FindResource("SilverDarker") as Avalonia.Media.IBrush,
                    VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center
                };
                
                emptyContainer.Children.Add(emptyIcon);
                emptyContainer.Children.Add(emptyText);
                panel.Children.Add(emptyContainer);
            }
        }
        catch (Exception ex)
        {
            DebugLogger.LogError($"Failed to display filter items: {ex.Message}");
        }
    }
    
    private void UpdatePageIndicator()
    {
        if (_pageIndicator is null) return;
        
        int totalPages = (_filteredFilters.Count + ITEMS_PER_PAGE - 1) / ITEMS_PER_PAGE;
        _pageIndicator.Text = $"Page {_currentPage + 1}/{Math.Max(1, totalPages)}";
        
        if (_prevPageButton is not null)
            _prevPageButton.IsEnabled = _currentPage > 0;
            
        if (_nextPageButton is not null)
            _nextPageButton.IsEnabled = _currentPage < totalPages - 1;
    }
    
    private void UpdateStatus()
    {
        if (_statusText == null) return;
        
        int filterCount = Math.Max(0, _availableFilters.Count - 1); // Subtract "Create New"
        _statusText.Text = $"{filterCount} Filter{(filterCount != 1 ? "s" : "")} Available";
    }
    
    private void OnPrevPageClick(object? _, RoutedEventArgs __)
    {
        if (_currentPage > 0)
        {
            _currentPage--;
            UpdateFilterList();
        }
    }
    
    private void OnNextPageClick(object? _, RoutedEventArgs __)
    {
        int totalPages = (_filteredFilters.Count + ITEMS_PER_PAGE - 1) / ITEMS_PER_PAGE;
        if (_currentPage < totalPages - 1)
        {
            _currentPage++;
            UpdateFilterList();
        }
    }
    
    private void OnSelectClick(object? sender, RoutedEventArgs e)
    {
        if (_selectedIndex >= 0)
        {
            if (_selectedIndex == 0)
            {
                // Create new filter
                FilterSelected?.Invoke(this, null);
            }
            else
            {
                // Select existing filter
                FilterSelected?.Invoke(this, _selectedFilterPath);
            }
        }
    }
    
    private void OnKeyDown(object? sender, KeyEventArgs e)
    {
        switch (e.Key)
        {
            // Up/Down arrow keys to navigate filter list
            case Key.Up:
                SelectPreviousFilter();
                e.Handled = true;
                break;
                
            case Key.Down:
                SelectNextFilter();
                e.Handled = true;
                break;
            
            // Page Up/Page Down for pagination
            case Key.PageUp:
                OnPrevPageClick(null, null!);
                e.Handled = true;
                break;
                
            case Key.PageDown:
                OnNextPageClick(null, null!);
                e.Handled = true;
                break;
            
            // Enter to select current filter
            case Key.Enter:
                if (_selectedIndex >= 0 && _selectButton?.IsEnabled == true)
                {
                    OnSelectClick(null, null!);
                    e.Handled = true;
                }
                break;
            
            // Home/End to jump to first/last page
            case Key.Home:
                if (_currentPage != 0)
                {
                    _currentPage = 0;
                    UpdateFilterList();
                    e.Handled = true;
                }
                break;
                
            case Key.End:
                int totalPages = (_filteredFilters.Count + ITEMS_PER_PAGE - 1) / ITEMS_PER_PAGE;
                if (_currentPage != totalPages - 1)
                {
                    _currentPage = Math.Max(0, totalPages - 1);
                    UpdateFilterList();
                    e.Handled = true;
                }
                break;
        }
    }
    
    private void SelectPreviousFilter()
    {
        if (_filteredFilters.Count == 0) return;
        
        if (_selectedIndex > 0)
        {
            // Check if we need to go to previous page
            int startIndex = _currentPage * ITEMS_PER_PAGE;
            if (_selectedIndex == startIndex && _currentPage > 0)
            {
                _currentPage--;
                _selectedIndex--;
            }
            else if (_selectedIndex > startIndex)
            {
                _selectedIndex--;
            }
        }
        else if (_selectedIndex == -1 && _filteredFilters.Count > 0)
        {
            // No selection yet, select first item
            _selectedIndex = 0;
        }
        
        UpdateFilterList();
        
        // Update preview
        if (_selectedIndex >= 0)
        {
            if (_selectedIndex == 0)
            {
                _selectedFilterPath = null;
                UpdatePreview(null);
            }
            else
            {
                var filterName = _availableFilters[_selectedIndex];
                _selectedFilterPath = Path.Combine(
                    AppDomain.CurrentDomain.BaseDirectory,
                    "JsonItemFilters",
                    $"{filterName}.json"
                );
                UpdatePreview(_selectedFilterPath);
            }
            
            if (_selectButton is not null)
                _selectButton.IsEnabled = true;
        }
    }
    
    private void SelectNextFilter()
    {
        if (_filteredFilters.Count == 0) return;
        
        if (_selectedIndex < _availableFilters.Count - 1)
        {
            // Check if we need to go to next page
            int endIndex = Math.Min((_currentPage + 1) * ITEMS_PER_PAGE, _filteredFilters.Count) - 1;
            if (_selectedIndex == endIndex && _currentPage < (_filteredFilters.Count + ITEMS_PER_PAGE - 1) / ITEMS_PER_PAGE - 1)
            {
                _currentPage++;
                _selectedIndex++;
            }
            else if (_selectedIndex < endIndex)
            {
                _selectedIndex++;
            }
        }
        else if (_selectedIndex == -1 && _filteredFilters.Count > 0)
        {
            // No selection yet, select first item
            _selectedIndex = 0;
        }
        
        UpdateFilterList();
        
        // Update preview
        if (_selectedIndex >= 0)
        {
            if (_selectedIndex == 0)
            {
                _selectedFilterPath = null;
                UpdatePreview(null);
            }
            else
            {
                var filterName = _availableFilters[_selectedIndex];
                _selectedFilterPath = Path.Combine(
                    AppDomain.CurrentDomain.BaseDirectory,
                    "JsonItemFilters",
                    $"{filterName}.json"
                );
                UpdatePreview(_selectedFilterPath);
            }
            
            if (_selectButton is not null)
                _selectButton.IsEnabled = true;
        }
    }
}
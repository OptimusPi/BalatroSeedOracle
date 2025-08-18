using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Animation;
using Avalonia.Animation.Easings;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
using Avalonia.Styling;
using Avalonia.Threading;
using BalatroSeedOracle.Helpers;
using BalatroSeedOracle.Services;

namespace BalatroSeedOracle.Components;

public partial class ChallengesStyleFilterGrid : UserControl
{
    private const int ITEMS_PER_PAGE = 12; // 3x4 grid
    private const int ITEMS_PER_ROW = 3;
    
    private class FilterInfo
    {
        public string Name { get; set; } = "";
        public string Path { get; set; } = "";
        public string Description { get; set; } = "";
        public int SearchCount { get; set; }
        public DateTime LastUsed { get; set; }
        public bool IsCompleted { get; set; } // Has found results
        public bool IsNew { get; set; }
    }
    
    private List<FilterInfo> _filters = new();
    private int _currentPage = 0;
    private int _selectedIndex = -1;
    private FilterInfo? _selectedFilter;
    
    private ItemsControl? _filterGrid;
    private TextBlock? _filterCountText;
    private TextBlock? _completedCountText;
    private TextBlock? _pageIndicator;
    private Button? _prevPageButton;
    private Button? _nextPageButton;
    private Button? _selectButton;
    
    public event EventHandler<string?>? FilterSelected;
    
    public ChallengesStyleFilterGrid()
    {
        InitializeComponent();
    }
    
    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
        
        _filterGrid = this.FindControl<ItemsControl>("FilterGrid");
        _filterCountText = this.FindControl<TextBlock>("FilterCountText");
        _completedCountText = this.FindControl<TextBlock>("CompletedCountText");
        _pageIndicator = this.FindControl<TextBlock>("PageIndicator");
        _prevPageButton = this.FindControl<Button>("PrevPageButton");
        _nextPageButton = this.FindControl<Button>("NextPageButton");
        _selectButton = this.FindControl<Button>("SelectButton");
        
        if (_prevPageButton != null)
            _prevPageButton.Click += OnPrevPageClick;
        if (_nextPageButton != null)
            _nextPageButton.Click += OnNextPageClick;
        if (_selectButton != null)
            _selectButton.Click += OnSelectClick;
        
        // Enable keyboard navigation
        this.Focusable = true;
        this.KeyDown += OnKeyDown;
        
        Loaded += OnLoaded;
    }
    
    private async void OnLoaded(object? sender, RoutedEventArgs e)
    {
        await LoadFilters();
        UpdateDisplay();
        
        // Focus for keyboard nav
        this.Focus();
    }
    
    private async Task LoadFilters()
    {
        try
        {
            _filters.Clear();
            
            // Add "Create New" option first
            _filters.Add(new FilterInfo
            {
                Name = "✨ Create New Filter",
                Description = "Start fresh with a blank filter",
                IsNew = true
            });
            
            // Load existing filters
            var filterDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "JsonItemFilters");
            if (Directory.Exists(filterDir))
            {
                var files = Directory.GetFiles(filterDir, "*.json");
                
                foreach (var file in files.OrderBy(f => f))
                {
                    try
                    {
                        var json = await File.ReadAllTextAsync(file);
                        using var doc = JsonDocument.Parse(json);
                        var root = doc.RootElement;
                        
                        var filterInfo = new FilterInfo
                        {
                            Name = Path.GetFileNameWithoutExtension(file),
                            Path = file,
                            Description = root.TryGetProperty("description", out var desc) 
                                ? desc.GetString() ?? "" 
                                : "No description",
                            LastUsed = File.GetLastWriteTime(file)
                        };
                        
                        // Check if filter has been used (has results)
                        var resultsDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "SearchResults");
                        if (Directory.Exists(resultsDir))
                        {
                            var resultFiles = Directory.GetFiles(resultsDir, $"*{filterInfo.Name}*.duckdb");
                            filterInfo.SearchCount = resultFiles.Length;
                            filterInfo.IsCompleted = resultFiles.Length > 0;
                        }
                        
                        _filters.Add(filterInfo);
                    }
                    catch (Exception ex)
                    {
                        DebugLogger.LogError($"Failed to load filter {file}: {ex.Message}");
                    }
                }
            }
            
            UpdateStats();
        }
        catch (Exception ex)
        {
            DebugLogger.LogError($"Failed to load filters: {ex.Message}");
        }
    }
    
    private void UpdateStats()
    {
        if (_filterCountText != null)
            _filterCountText.Text = (_filters.Count - 1).ToString(); // Exclude "Create New"
        
        if (_completedCountText != null)
        {
            var completed = _filters.Count(f => f.IsCompleted && !f.IsNew);
            var total = _filters.Count - 1; // Exclude "Create New"
            _completedCountText.Text = $"{completed}/{total}";
        }
    }
    
    private void UpdateDisplay()
    {
        if (_filterGrid == null) return;
        
        _filterGrid.ItemsSource = null;
        var items = new List<Control>();
        
        int startIndex = _currentPage * ITEMS_PER_PAGE;
        int endIndex = Math.Min(startIndex + ITEMS_PER_PAGE, _filters.Count);
        
        for (int i = startIndex; i < endIndex; i++)
        {
            var filter = _filters[i];
            var index = i;
            
            var button = new Button
            {
                Classes = { "challenge-card" }
            };
            
            if (filter.IsNew)
            {
                button.Classes.Add("new-filter");
            }
            else if (filter.IsCompleted)
            {
                button.Classes.Add("selected");
            }
            
            if (i == _selectedIndex)
            {
                button.Classes.Add("selected");
            }
            
            // Create custom content
            var grid = new Grid();
            grid.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Auto));
            grid.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Star));
            
            // Number badge
            if (!filter.IsNew)
            {
                var app = Application.Current;
                var green = app?.FindResource("Green") as IBrush;
                var gold = app?.FindResource("Gold") as IBrush;
                var black = app?.FindResource("Black") as IBrush;
                var balatroFont = app?.FindResource("BalatroFont") as FontFamily;
                if (green == null || gold == null || black == null || balatroFont == null)
                    return; // resources not ready yet

                var numberBorder = new Border
                {
                    Background = filter.IsCompleted ? green : gold,
                    Width = 32,
                    Height = 32,
                    CornerRadius = new CornerRadius(16),
                    Margin = new Thickness(0, 0, 8, 0),
                    VerticalAlignment = VerticalAlignment.Center
                };
                
                var numberText = new TextBlock
                {
                    Text = i.ToString(),
                    FontFamily = balatroFont,
                    FontSize = 16,
                    FontWeight = FontWeight.Bold,
                    Foreground = Application.Current!.FindResource("Black") as IBrush,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Center
                };
                
                numberBorder.Child = numberText;
                Grid.SetColumn(numberBorder, 0);
                grid.Children.Add(numberBorder);
            }
            else
            {
                // Special icon for "Create New"
                var app2 = Application.Current;
                var gold2 = app2?.FindResource("Gold") as IBrush;
                var black2 = app2?.FindResource("Black") as IBrush;
                var font2 = app2?.FindResource("BalatroFont") as FontFamily;
                if (gold2 == null || black2 == null || font2 == null)
                    return;
                var iconBorder = new Border
                {
                    Background = gold2,
                    Width = 32,
                    Height = 32,
                    CornerRadius = new CornerRadius(16),
                    Margin = new Thickness(0, 0, 8, 0),
                    VerticalAlignment = VerticalAlignment.Center
                };
                
                var iconText = new TextBlock
                {
                    Text = "+",
                    FontFamily = font2,
                    FontSize = 20,
                    FontWeight = FontWeight.Bold,
                    Foreground = Application.Current!.FindResource("Black") as IBrush,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Center
                };
                
                iconBorder.Child = iconText;
                Grid.SetColumn(iconBorder, 0);
                grid.Children.Add(iconBorder);
            }
            
            // Filter info
            var infoPanel = new StackPanel
            {
                VerticalAlignment = VerticalAlignment.Center
            };
            
                var app3 = Application.Current;
                var white3 = app3?.FindResource("White") as IBrush;
                var balatro3 = app3?.FindResource("BalatroFont") as FontFamily;
                if (white3 == null || balatro3 == null) return;
                var nameText = new TextBlock
            {
                Text = filter.Name,
                    FontFamily = balatro3,
                FontSize = 14,
                FontWeight = FontWeight.Bold,
                    Foreground = white3,
                TextWrapping = TextWrapping.Wrap
            };
            infoPanel.Children.Add(nameText);
            
            if (!string.IsNullOrEmpty(filter.Description))
            {
                var silver = app3?.FindResource("Silver") as IBrush;
                if (silver == null) return;
                var descText = new TextBlock
                {
                    Text = filter.Description,
                    FontFamily = balatro3,
                    FontSize = 10,
                    Foreground = silver,
                    TextWrapping = TextWrapping.Wrap,
                    MaxWidth = 160
                };
                infoPanel.Children.Add(descText);
            }
            
            if (filter.SearchCount > 0)
            {
                var green4 = app3?.FindResource("Green") as IBrush;
                if (green4 == null) return;
                var statsText = new TextBlock
                {
                    Text = $"🔍 {filter.SearchCount} searches",
                    FontFamily = balatro3,
                    FontSize = 9,
                    Foreground = green4,
                    Margin = new Thickness(0, 2, 0, 0)
                };
                infoPanel.Children.Add(statsText);
            }
            
            Grid.SetColumn(infoPanel, 1);
            grid.Children.Add(infoPanel);
            
            button.Content = grid;
            button.Click += (s, e) => OnFilterClick(index);
            
            // Add hover animation
            button.PointerEntered += async (s, e) =>
            {
                await AnimateCardHover(button, true);
            };
            
            button.PointerExited += async (s, e) =>
            {
                await AnimateCardHover(button, false);
            };
            
            items.Add(button);
        }
        
        _filterGrid.ItemsSource = items;
        UpdatePageIndicator();
    }
    
    private async Task AnimateCardHover(Button card, bool isHovering)
    {
        var transform = new ScaleTransform();
        card.RenderTransform = transform;
        card.RenderTransformOrigin = new RelativePoint(0.5, 0.5, RelativeUnit.Relative);
        
        var animation = new Animation
        {
            Duration = TimeSpan.FromMilliseconds(100),
            Easing = new CubicEaseOut(),
            Children =
            {
                new KeyFrame
                {
                    Setters = 
                    { 
                        new Avalonia.Styling.Setter(ScaleTransform.ScaleXProperty, isHovering ? 1.05 : 1.0),
                        new Avalonia.Styling.Setter(ScaleTransform.ScaleYProperty, isHovering ? 1.05 : 1.0)
                    },
                    Cue = new Cue(1)
                }
            }
        };
        
        await animation.RunAsync(transform);
    }
    
    private void OnFilterClick(int index)
    {
        _selectedIndex = index;
        _selectedFilter = _filters[index];
        
        if (_selectButton != null)
            _selectButton.IsEnabled = true;
        
        UpdateDisplay();
        
        // Auto-select if double-clicked
        if (_lastClickIndex == index && (DateTime.Now - _lastClickTime).TotalMilliseconds < 500)
        {
            OnSelectClick(null, null!);
        }
        
        _lastClickIndex = index;
        _lastClickTime = DateTime.Now;
    }
    
    private int _lastClickIndex = -1;
    private DateTime _lastClickTime = DateTime.MinValue;
    
    private void UpdatePageIndicator()
    {
        int totalPages = (_filters.Count + ITEMS_PER_PAGE - 1) / ITEMS_PER_PAGE;
        
        if (_pageIndicator != null)
            _pageIndicator.Text = $"{_currentPage + 1}/{Math.Max(1, totalPages)}";
        
        if (_prevPageButton != null)
            _prevPageButton.IsEnabled = _currentPage > 0;
        
        if (_nextPageButton != null)
            _nextPageButton.IsEnabled = _currentPage < totalPages - 1;
    }
    
    private void OnPrevPageClick(object? sender, RoutedEventArgs e)
    {
        if (_currentPage > 0)
        {
            _currentPage--;
            AnimatePageTransition(-1);
        }
    }
    
    private void OnNextPageClick(object? sender, RoutedEventArgs e)
    {
        int totalPages = (_filters.Count + ITEMS_PER_PAGE - 1) / ITEMS_PER_PAGE;
        if (_currentPage < totalPages - 1)
        {
            _currentPage++;
            AnimatePageTransition(1);
        }
    }
    
    private async void AnimatePageTransition(int direction)
    {
        if (_filterGrid == null) return;
        
        // Fade out
        var fadeOut = new Animation
        {
            Duration = TimeSpan.FromMilliseconds(150),
            Children =
            {
                new KeyFrame
                {
                    Setters = { new Avalonia.Styling.Setter(Visual.OpacityProperty, 0.0) },
                    Cue = new Cue(1)
                }
            }
        };
        
        await fadeOut.RunAsync(_filterGrid);
        
        // Update content
        UpdateDisplay();
        
        // Fade in
        var fadeIn = new Animation
        {
            Duration = TimeSpan.FromMilliseconds(150),
            Children =
            {
                new KeyFrame
                {
                    Setters = { new Avalonia.Styling.Setter(Visual.OpacityProperty, 1.0) },
                    Cue = new Cue(1)
                }
            }
        };
        
        await fadeIn.RunAsync(_filterGrid);
    }
    
    private void OnSelectClick(object? sender, RoutedEventArgs e)
    {
        if (_selectedFilter != null)
        {
            if (_selectedFilter.IsNew)
            {
                FilterSelected?.Invoke(this, null);
            }
            else
            {
                FilterSelected?.Invoke(this, _selectedFilter.Path);
            }
        }
    }
    
    private void OnKeyDown(object? sender, KeyEventArgs e)
    {
        int totalItems = _filters.Count;
        int totalPages = (totalItems + ITEMS_PER_PAGE - 1) / ITEMS_PER_PAGE;
        
        switch (e.Key)
        {
            case Key.Left:
                if (_selectedIndex > 0)
                {
                    // Move to previous item
                    _selectedIndex--;
                    
                    // Check if we need to go to previous page
                    int pageOfSelected = _selectedIndex / ITEMS_PER_PAGE;
                    if (pageOfSelected < _currentPage)
                    {
                        _currentPage = pageOfSelected;
                    }
                    
                    _selectedFilter = _filters[_selectedIndex];
                    UpdateDisplay();
                }
                e.Handled = true;
                break;
                
            case Key.Right:
                if (_selectedIndex < totalItems - 1)
                {
                    // Move to next item
                    _selectedIndex++;
                    
                    // Check if we need to go to next page
                    int pageOfSelected = _selectedIndex / ITEMS_PER_PAGE;
                    if (pageOfSelected > _currentPage)
                    {
                        _currentPage = pageOfSelected;
                    }
                    
                    _selectedFilter = _filters[_selectedIndex];
                    UpdateDisplay();
                }
                e.Handled = true;
                break;
                
            case Key.Up:
                if (_selectedIndex >= ITEMS_PER_ROW)
                {
                    _selectedIndex -= ITEMS_PER_ROW;
                    
                    // Check if we need to go to previous page
                    int pageOfSelected = _selectedIndex / ITEMS_PER_PAGE;
                    if (pageOfSelected < _currentPage)
                    {
                        _currentPage = pageOfSelected;
                    }
                    
                    _selectedFilter = _filters[_selectedIndex];
                    UpdateDisplay();
                }
                e.Handled = true;
                break;
                
            case Key.Down:
                if (_selectedIndex + ITEMS_PER_ROW < totalItems)
                {
                    _selectedIndex += ITEMS_PER_ROW;
                    
                    // Check if we need to go to next page
                    int pageOfSelected = _selectedIndex / ITEMS_PER_PAGE;
                    if (pageOfSelected > _currentPage)
                    {
                        _currentPage = pageOfSelected;
                    }
                    
                    _selectedFilter = _filters[_selectedIndex];
                    UpdateDisplay();
                }
                e.Handled = true;
                break;
                
            case Key.PageUp:
                OnPrevPageClick(null, null!);
                e.Handled = true;
                break;
                
            case Key.PageDown:
                OnNextPageClick(null, null!);
                e.Handled = true;
                break;
                
            case Key.Enter:
            case Key.Space:
                if (_selectedFilter != null)
                {
                    OnSelectClick(null, null!);
                }
                e.Handled = true;
                break;
                
            case Key.Home:
                _currentPage = 0;
                _selectedIndex = 0;
                _selectedFilter = _filters.FirstOrDefault();
                UpdateDisplay();
                e.Handled = true;
                break;
                
            case Key.End:
                _currentPage = Math.Max(0, totalPages - 1);
                _selectedIndex = totalItems - 1;
                _selectedFilter = _filters.LastOrDefault();
                UpdateDisplay();
                e.Handled = true;
                break;
                
            // Number keys for quick selection
            case Key.D1:
            case Key.NumPad1:
                SelectByNumber(1);
                e.Handled = true;
                break;
            case Key.D2:
            case Key.NumPad2:
                SelectByNumber(2);
                e.Handled = true;
                break;
            case Key.D3:
            case Key.NumPad3:
                SelectByNumber(3);
                e.Handled = true;
                break;
            case Key.D4:
            case Key.NumPad4:
                SelectByNumber(4);
                e.Handled = true;
                break;
            case Key.D5:
            case Key.NumPad5:
                SelectByNumber(5);
                e.Handled = true;
                break;
            case Key.D6:
            case Key.NumPad6:
                SelectByNumber(6);
                e.Handled = true;
                break;
            case Key.D7:
            case Key.NumPad7:
                SelectByNumber(7);
                e.Handled = true;
                break;
            case Key.D8:
            case Key.NumPad8:
                SelectByNumber(8);
                e.Handled = true;
                break;
            case Key.D9:
            case Key.NumPad9:
                SelectByNumber(9);
                e.Handled = true;
                break;
        }
    }
    
    private void SelectByNumber(int number)
    {
        int targetIndex = (_currentPage * ITEMS_PER_PAGE) + number - 1;
        if (targetIndex >= 0 && targetIndex < _filters.Count)
        {
            _selectedIndex = targetIndex;
            _selectedFilter = _filters[targetIndex];
            UpdateDisplay();
            
            if (_selectButton != null)
                _selectButton.IsEnabled = true;
        }
    }
    
    public async Task RefreshFilters()
    {
        await LoadFilters();
        UpdateDisplay();
    }
}
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
using BalatroSeedOracle.Views.Modals;

namespace BalatroSeedOracle.Components;

public partial class FilterSelector : UserControl
{
    private const int ITEMS_PER_PAGE = 12;
    
    private List<FilterInfo> _availableFilters = new();
    private List<FilterInfo> _filteredFilters = new();
    private string _searchQuery = string.Empty;
    private int _currentPage = 0;
    private int _selectedIndex = -1;
    private FilterInfo? _selectedFilter;
    private FileSystemWatcher? _watcher;
    private DateTime _lastFsEventUtc = DateTime.MinValue;
    private bool _pendingRefresh;
    private readonly TimeSpan _fsDebounce = TimeSpan.FromMilliseconds(250);
    private readonly object _refreshLock = new();
    private bool _disposed;

    public bool AutoFireSelection { get; set; } = false;
    
    public static readonly StyledProperty<string> TitleProperty = 
        AvaloniaProperty.Register<FilterSelector, string>(nameof(Title), "Select Filter");
        
    public string Title
    {
        get => GetValue(TitleProperty);
        set => SetValue(TitleProperty, value);
    }
    
    private ItemsControl? _filterGrid;
    private TextBlock? _selectedFilterName;
    private TextBlock? _pageIndicator;
    private TextBlock? _statusText;
    private Button? _prevPageButton;
    private Button? _nextPageButton;
    private Button? _selectButton;
    private Button? _createButton;
    private TextBox? _searchBox;
    
    public event EventHandler<string?>? FilterSelected;
    
    public FilterSelector()
    {
        InitializeComponent();
        
        this.Focusable = true;
        this.KeyDown += OnKeyDown;
        SetupFileWatcher();
    }
    
    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
        
        _filterGrid = this.FindControl<ItemsControl>("FilterGrid");
        _selectedFilterName = this.FindControl<TextBlock>("SelectedFilterName");
        _pageIndicator = this.FindControl<TextBlock>("PageIndicator");
        _statusText = this.FindControl<TextBlock>("StatusText");
        _prevPageButton = this.FindControl<Button>("PrevPageButton");
        _nextPageButton = this.FindControl<Button>("NextPageButton");
        _selectButton = this.FindControl<Button>("SelectButton");
        _createButton = this.FindControl<Button>("CreateButton");
        _searchBox = this.FindControl<TextBox>("SearchBox");
        
        if (_searchBox != null)
        {
            _searchBox.TextChanged += OnSearchTextChanged;
        }
        
        if (_prevPageButton != null)
        {
            _prevPageButton.Click += OnPrevPageClick;
        }
        
        if (_nextPageButton != null)
        {
            _nextPageButton.Click += OnNextPageClick;
        }
        
        if (_selectButton != null)
        {
            _selectButton.Click += OnSelectClick;
        }
        
        if (_createButton != null)
        {
            _createButton.Click += OnCreateClick;
        }
        
        Loaded += OnLoaded;
    }
    
    private void OnLoaded(object? _, RoutedEventArgs __)
    {
        RefreshFilters();
    }
    
    private void SetupFileWatcher()
    {
        try
        {
            // Look in project root for filters, not working directory
            var projectRoot = Path.GetDirectoryName(Path.GetDirectoryName(Directory.GetCurrentDirectory())) ?? Directory.GetCurrentDirectory();
            var directory = Path.Combine(projectRoot, "JsonItemFilters");
            if (!Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }
            
            _watcher = new FileSystemWatcher(directory)
            {
                Filter = "*.json",
                NotifyFilter = NotifyFilters.FileName | NotifyFilters.LastWrite | NotifyFilters.CreationTime,
                EnableRaisingEvents = true
            };
            
            _watcher.Created += OnFileSystemEvent;
            _watcher.Changed += OnFileSystemEvent;
            _watcher.Deleted += OnFileSystemEvent;
            _watcher.Renamed += OnFileSystemEvent;
        }
        catch (Exception ex)
        {
            DebugLogger.LogError("FilterSelector", $"Failed to setup file watcher: {ex.Message}");
        }
    }
    
    private void OnFileSystemEvent(object sender, FileSystemEventArgs e)
    {
        lock (_refreshLock)
        {
            _lastFsEventUtc = DateTime.UtcNow;
            if (!_pendingRefresh)
            {
                _pendingRefresh = true;
                Task.Delay(_fsDebounce).ContinueWith(_ => DebouncedRefresh());
            }
        }
    }
    
    private void DebouncedRefresh()
    {
        lock (_refreshLock)
        {
            if (DateTime.UtcNow - _lastFsEventUtc >= _fsDebounce)
            {
                _pendingRefresh = false;
                Avalonia.Threading.Dispatcher.UIThread.Post(RefreshFilters);
            }
            else
            {
                Task.Delay(_fsDebounce).ContinueWith(_ => DebouncedRefresh());
            }
        }
    }
    
    private void RefreshFilters()
    {
        try
        {
            // Look in project root for filters, not working directory
            var projectRoot = Path.GetDirectoryName(Path.GetDirectoryName(Directory.GetCurrentDirectory())) ?? Directory.GetCurrentDirectory();
            var directory = Path.Combine(projectRoot, "JsonItemFilters");
            if (!Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }
            
            var filters = new List<FilterInfo>();
            var jsonFiles = Directory.GetFiles(directory, "*.json");
            
            foreach (var file in jsonFiles)
            {
                try
                {
                    var content = File.ReadAllText(file);
                    using var doc = JsonDocument.Parse(content);
                    
                    if (!doc.RootElement.TryGetProperty("name", out var nameElement) ||
                        string.IsNullOrEmpty(nameElement.GetString()))
                    {
                        continue;
                    }
                    
                    var name = nameElement.GetString()!;
                    var description = "No description";
                    var author = "";
                    var dateCreated = File.GetCreationTime(file);
                    
                    if (doc.RootElement.TryGetProperty("description", out var descElement))
                    {
                        description = descElement.GetString() ?? "No description";
                    }
                    
                    if (doc.RootElement.TryGetProperty("author", out var authorElement))
                    {
                        author = authorElement.GetString() ?? "";
                    }
                    
                    if (doc.RootElement.TryGetProperty("dateCreated", out var dateElement))
                    {
                        if (DateTime.TryParse(dateElement.GetString(), out var parsedDate))
                        {
                            dateCreated = parsedDate;
                        }
                    }
                    
                    filters.Add(new FilterInfo
                    {
                        Name = name,
                        Description = description,
                        Author = author,
                        FilePath = file,
                        DateCreated = dateCreated
                    });
                }
                catch (Exception ex)
                {
                    DebugLogger.LogError("FilterSelector", $"Error parsing filter {file}: {ex.Message}");
                }
            }
            
            _availableFilters = filters.OrderByDescending(f => f.DateCreated).ToList();
            ApplySearchFilter();
            UpdateUI();
        }
        catch (Exception ex)
        {
            DebugLogger.LogError("FilterSelector", $"Error refreshing filters: {ex.Message}");
        }
    }
    
    private void ApplySearchFilter()
    {
        if (string.IsNullOrWhiteSpace(_searchQuery))
        {
            _filteredFilters = _availableFilters.ToList();
        }
        else
        {
            var query = _searchQuery.ToLowerInvariant();
            _filteredFilters = _availableFilters.Where(f => 
                f.Name.ToLowerInvariant().Contains(query) ||
                f.Description.ToLowerInvariant().Contains(query) ||
                f.Author.ToLowerInvariant().Contains(query)
            ).ToList();
        }
        
        _currentPage = 0;
        if (_selectedFilter != null && !_filteredFilters.Contains(_selectedFilter))
        {
            _selectedFilter = null;
            _selectedIndex = -1;
        }
    }
    
    private void UpdateUI()
    {
        UpdateFilterGrid();
        UpdatePagination();
        UpdateStatus();
        UpdateSelection();
    }
    
    private void UpdateFilterGrid()
    {
        if (_filterGrid == null) return;
        
        var startIndex = _currentPage * ITEMS_PER_PAGE;
        var pageFilters = _filteredFilters.Skip(startIndex).Take(ITEMS_PER_PAGE).ToList();
        
        var buttons = new List<Button>();
        
        // Add "Create New" button if on first page
        if (_currentPage == 0)
        {
            var createButton = new Button
            {
                Classes = { "filter-card", "create-filter-card" },
                Content = new StackPanel
                {
                    Orientation = Orientation.Vertical,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Center,
                    Children =
                    {
                        new TextBlock
                        {
                            Text = "➕",
                            FontSize = 32,
                            HorizontalAlignment = HorizontalAlignment.Center,
                            Foreground = Brushes.White
                        },
                        new TextBlock
                        {
                            Text = "Create New",
                            FontFamily = this.FindResource("BalatroFont") as FontFamily ?? FontFamily.Default,
                            FontSize = 12,
                            HorizontalAlignment = HorizontalAlignment.Center,
                            Foreground = Brushes.White,
                            Margin = new Thickness(0, 4, 0, 0)
                        }
                    }
                }
            };
            createButton.Click += OnCreateClick;
            buttons.Add(createButton);
        }
        
        // Add filter buttons
        for (int i = 0; i < pageFilters.Count; i++)
        {
            var filter = pageFilters[i];
            var globalIndex = startIndex + i;
            var isSelected = _selectedFilter == filter;
            
            var button = new Button
            {
                Classes = { "filter-card" },
                Tag = filter,
                Content = new StackPanel
                {
                    Orientation = Orientation.Vertical,
                    Children =
                    {
                        new TextBlock
                        {
                            Text = filter.Name,
                            FontFamily = this.FindResource("BalatroFont") as FontFamily ?? FontFamily.Default,
                            FontSize = 14,
                            FontWeight = FontWeight.Bold,
                            Foreground = Brushes.White,
                            TextWrapping = TextWrapping.Wrap,
                            HorizontalAlignment = HorizontalAlignment.Center,
                            Margin = new Thickness(0, 0, 0, 4)
                        },
                        new TextBlock
                        {
                            Text = string.IsNullOrEmpty(filter.Author) ? filter.Description : $"by {filter.Author}",
                            FontFamily = this.FindResource("BalatroFont") as FontFamily ?? FontFamily.Default,
                            FontSize = 10,
                            Foreground = Brushes.LightGray,
                            TextWrapping = TextWrapping.Wrap,
                            HorizontalAlignment = HorizontalAlignment.Center,
                            MaxHeight = 40
                        }
                    }
                }
            };
            
            if (isSelected)
            {
                button.Classes.Add("selected");
            }
            
            button.Click += OnFilterClick;
            buttons.Add(button);
        }
        
        _filterGrid.ItemsSource = buttons;
    }
    
    private void UpdatePagination()
    {
        var totalPages = Math.Max(1, (int)Math.Ceiling((double)_filteredFilters.Count / ITEMS_PER_PAGE));
        
        if (_pageIndicator != null)
        {
            _pageIndicator.Text = $"{_currentPage + 1} / {totalPages}";
        }
        
        if (_prevPageButton != null)
        {
            _prevPageButton.IsEnabled = _currentPage > 0;
        }
        
        if (_nextPageButton != null)
        {
            _nextPageButton.IsEnabled = _currentPage < totalPages - 1;
        }
    }
    
    private void UpdateStatus()
    {
        if (_statusText != null)
        {
            var total = _availableFilters.Count;
            var filtered = _filteredFilters.Count;
            
            if (total == 0)
            {
                _statusText.Text = "No filters found";
            }
            else if (filtered == total)
            {
                _statusText.Text = $"{total} filter{(total == 1 ? "" : "s")} available";
            }
            else
            {
                _statusText.Text = $"{filtered} of {total} filter{(total == 1 ? "" : "s")}";
            }
        }
    }
    
    private void UpdateSelection()
    {
        if (_selectedFilterName != null)
        {
            _selectedFilterName.Text = _selectedFilter?.Name ?? "No filter selected";
        }
        
        if (_selectButton != null)
        {
            _selectButton.IsEnabled = _selectedFilter != null;
        }
    }
    
    private void OnSearchTextChanged(object? sender, TextChangedEventArgs e)
    {
        if (sender is TextBox textBox)
        {
            _searchQuery = textBox.Text ?? string.Empty;
            ApplySearchFilter();
            UpdateUI();
        }
    }
    
    private void OnPrevPageClick(object? sender, RoutedEventArgs e)
    {
        if (_currentPage > 0)
        {
            _currentPage--;
            UpdateUI();
        }
    }
    
    private void OnNextPageClick(object? sender, RoutedEventArgs e)
    {
        var totalPages = Math.Max(1, (int)Math.Ceiling((double)_filteredFilters.Count / ITEMS_PER_PAGE));
        if (_currentPage < totalPages - 1)
        {
            _currentPage++;
            UpdateUI();
        }
    }
    
    private void OnFilterClick(object? sender, RoutedEventArgs e)
    {
        if (sender is Button button && button.Tag is FilterInfo filter)
        {
            _selectedFilter = filter;
            _selectedIndex = _filteredFilters.IndexOf(filter);
            UpdateUI();
            
            if (AutoFireSelection)
            {
                FilterSelected?.Invoke(this, filter.FilePath);
            }
        }
    }
    
    private void OnSelectClick(object? sender, RoutedEventArgs e)
    {
        if (_selectedFilter != null)
        {
            FilterSelected?.Invoke(this, _selectedFilter.FilePath);
        }
    }
    
    private void OnCreateClick(object? sender, RoutedEventArgs e)
    {
        try
        {
            // For now, just log that the feature is not yet implemented
            DebugLogger.Log("FilterSelector", "Filter creation feature requested - not yet implemented");
            
            // TODO: Implement filter creation modal
            // This would require access to the main window/menu to properly show the modal
        }
        catch (Exception ex)
        {
            DebugLogger.LogError("FilterSelector", $"Error handling create click: {ex.Message}");
        }
    }
    
    private void OnKeyDown(object? sender, KeyEventArgs e)
    {
        switch (e.Key)
        {
            case Key.Enter:
                if (_selectedFilter != null)
                {
                    FilterSelected?.Invoke(this, _selectedFilter.FilePath);
                    e.Handled = true;
                }
                break;
                
            case Key.Left:
                OnPrevPageClick(null, new RoutedEventArgs());
                e.Handled = true;
                break;
                
            case Key.Right:
                OnNextPageClick(null, new RoutedEventArgs());
                e.Handled = true;
                break;
        }
    }
    
    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnDetachedFromVisualTree(e);
        
        if (!_disposed)
        {
            _disposed = true;
            _watcher?.Dispose();
        }
    }
    
    private class FilterInfo
    {
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Author { get; set; } = string.Empty;
        public string FilePath { get; set; } = string.Empty;
        public DateTime DateCreated { get; set; }
    }
}

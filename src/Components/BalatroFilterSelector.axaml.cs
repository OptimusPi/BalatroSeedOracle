using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using BalatroSeedOracle.Helpers;
using BalatroSeedOracle.Services;

namespace BalatroSeedOracle.Components
{
    public partial class BalatroFilterSelector : UserControl
    {
        private const int FILTERS_PER_PAGE = 10; // Like Balatro challenges
        
        private List<FilterInfo> _allFilters = new();
        private int _currentPage = 0;
        private int _selectedFilterIndex = -1;
        private FilterInfo? _selectedFilter;
        private string _currentTab = "overview";
        
        // UI Controls
        private StackPanel? _filterListPanel;
        private StackPanel? _jokerPreviewPanel;
        private StackPanel? _bottomPanelContent;
        private TextBlock? _pageIndicatorText;
        private TextBlock? _statusText;
        private TextBlock? _topPanelHeader;
        private TextBlock? _topPanelContent;
        private TextBlock? _bottomPanelHeader;
        private Button? _prevPageButton;
        private Button? _nextPageButton;
        private Button? _editInDesignerButton;
        private Button? _cloneFilterButton;
        private Button? _overviewTab;
        private Button? _filterRulesTab;
        private Button? _scoringRulesTab;
        
        public event EventHandler<FilterInfo?>? FilterSelected;
        public event EventHandler? CreateNewFilterRequested;
        
        public BalatroFilterSelector()
        {
            InitializeComponent();
            LoadFilters();
        }
        
        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
            
            // Find controls
            _filterListPanel = this.FindControl<StackPanel>("FilterListPanel");
            _jokerPreviewPanel = this.FindControl<StackPanel>("JokerPreviewPanel");
            _bottomPanelContent = this.FindControl<StackPanel>("BottomPanelContent");
            _pageIndicatorText = this.FindControl<TextBlock>("PageIndicatorText");
            _statusText = this.FindControl<TextBlock>("StatusText");
            _topPanelHeader = this.FindControl<TextBlock>("TopPanelHeader");
            _topPanelContent = this.FindControl<TextBlock>("TopPanelContent");
            _bottomPanelHeader = this.FindControl<TextBlock>("BottomPanelHeader");
            _prevPageButton = this.FindControl<Button>("PrevPageButton");
            _nextPageButton = this.FindControl<Button>("NextPageButton");
            _editInDesignerButton = this.FindControl<Button>("EditInDesignerButton");
            _cloneFilterButton = this.FindControl<Button>("CloneFilterButton");
            _overviewTab = this.FindControl<Button>("OverviewTab");
            _filterRulesTab = this.FindControl<Button>("FilterRulesTab");
            _scoringRulesTab = this.FindControl<Button>("ScoringRulesTab");
        }
        
        private void LoadFilters()
        {
            try
            {
                _allFilters.Clear();
                
                // Look in multiple possible directories
                var possiblePaths = new[]
                {
                    Path.Combine(Directory.GetCurrentDirectory(), "JsonItemFilters"),
                    Path.Combine(Directory.GetCurrentDirectory(), "..", "JsonItemFilters"),
                    Path.Combine(Directory.GetCurrentDirectory(), "external", "Motely", "Motely", "JsonItemFilters"),
                    Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "JsonItemFilters")
                };

                string? filterDirectory = null;
                foreach (var path in possiblePaths)
                {
                    if (Directory.Exists(path))
                    {
                        filterDirectory = path;
                        DebugLogger.Log("BalatroFilterSelector", $"Found filter directory: {path}");
                        break;
                    }
                }

                if (string.IsNullOrEmpty(filterDirectory))
                {
                    DebugLogger.LogError("BalatroFilterSelector", "No filter directory found");
                    UpdateUI();
                    return;
                }

                var jsonFiles = Directory.GetFiles(filterDirectory, "*.json").OrderBy(f => Path.GetFileName(f)).ToArray();
                
                for (int i = 0; i < jsonFiles.Length; i++)
                {
                    var file = jsonFiles[i];
                    try
                    {
                        var content = File.ReadAllText(file);
                        using var doc = JsonDocument.Parse(content);
                        
                        var filter = new FilterInfo
                        {
                            Index = i + 1,
                            Name = GetFilterName(doc.RootElement, file),
                            Description = GetFilterDescription(doc.RootElement),
                            Author = GetFilterAuthor(doc.RootElement),
                            FilePath = file,
                            JsonContent = doc.RootElement.Clone(),
                            RequiredJokers = GetRequiredJokers(doc.RootElement),
                            WantedItems = GetWantedItems(doc.RootElement),
                            Deck = GetDeck(doc.RootElement),
                            Stake = GetStake(doc.RootElement)
                        };
                        
                        _allFilters.Add(filter);
                    }
                    catch (Exception ex)
                    {
                        DebugLogger.LogError("BalatroFilterSelector", $"Error parsing filter {file}: {ex.Message}");
                    }
                }
                
                DebugLogger.Log("BalatroFilterSelector", $"Loaded {_allFilters.Count} filters");
                UpdateUI();
            }
            catch (Exception ex)
            {
                DebugLogger.LogError("BalatroFilterSelector", $"Error loading filters: {ex.Message}");
            }
        }
        
        private string GetFilterName(JsonElement root, string filePath)
        {
            if (root.TryGetProperty("name", out var nameElement))
            {
                return nameElement.GetString() ?? Path.GetFileNameWithoutExtension(filePath);
            }
            return Path.GetFileNameWithoutExtension(filePath);
        }
        
        private string GetFilterDescription(JsonElement root)
        {
            if (root.TryGetProperty("description", out var descElement))
            {
                return descElement.GetString() ?? "No description";
            }
            return "No description";
        }
        
        private string GetFilterAuthor(JsonElement root)
        {
            if (root.TryGetProperty("author", out var authorElement))
            {
                return authorElement.GetString() ?? "";
            }
            return "";
        }
        
        private List<string> GetRequiredJokers(JsonElement root)
        {
            var jokers = new List<string>();
            
            // Check "must" array
            if (root.TryGetProperty("must", out var mustArray) && mustArray.ValueKind == JsonValueKind.Array)
            {
                foreach (var item in mustArray.EnumerateArray())
                {
                    if (item.TryGetProperty("type", out var type) && 
                        (type.GetString()?.ToLower() == "joker" || type.GetString()?.ToLower() == "souljoker"))
                    {
                        if (item.TryGetProperty("value", out var value))
                        {
                            var jokerName = value.GetString();
                            if (!string.IsNullOrEmpty(jokerName))
                            {
                                jokers.Add(jokerName);
                            }
                        }
                    }
                }
            }
            
            // Check legacy "needs" format
            if (root.TryGetProperty("needs", out var needs) && needs.ValueKind == JsonValueKind.Array)
            {
                foreach (var item in needs.EnumerateArray())
                {
                    if (item.TryGetProperty("Type", out var type) && 
                        (type.GetString() == "Joker" || type.GetString() == "SoulJoker"))
                    {
                        if (item.TryGetProperty("Value", out var value))
                        {
                            var jokerName = value.GetString();
                            if (!string.IsNullOrEmpty(jokerName))
                            {
                                jokers.Add(jokerName);
                            }
                        }
                    }
                }
            }
            
            return jokers.Take(6).ToList(); // Limit to 6 for display
        }
        
        private List<string> GetWantedItems(JsonElement root)
        {
            var items = new List<string>();
            
            if (root.TryGetProperty("should", out var shouldArray) && shouldArray.ValueKind == JsonValueKind.Array)
            {
                foreach (var item in shouldArray.EnumerateArray())
                {
                    if (item.TryGetProperty("value", out var value))
                    {
                        var itemName = value.GetString();
                        if (!string.IsNullOrEmpty(itemName))
                        {
                            items.Add(itemName);
                        }
                    }
                }
            }
            
            return items;
        }
        
        private string GetDeck(JsonElement root)
        {
            if (root.TryGetProperty("deck", out var deckElement))
            {
                return deckElement.GetString() ?? "Red";
            }
            return "Red";
        }
        
        private string GetStake(JsonElement root)
        {
            if (root.TryGetProperty("stake", out var stakeElement))
            {
                return stakeElement.GetString() ?? "White";
            }
            return "White";
        }
        
        private string TruncateFilterName(string name, int maxLength)
        {
            if (string.IsNullOrEmpty(name) || name.Length <= maxLength)
                return name;
            
            return name.Substring(0, maxLength - 3) + "...";
        }
        
        private void UpdateUI()
        {
            UpdateFilterList();
            UpdatePagination();
            UpdateStatus();
            UpdateFilterDetails();
        }
        
        private void UpdateFilterList()
        {
            if (_filterListPanel == null) return;
            
            _filterListPanel.Children.Clear();
            
            // Add "Create New Filter" button at the top
            var createContainer = new StackPanel
            {
                Orientation = Avalonia.Layout.Orientation.Horizontal,
                Margin = new Thickness(0, 2)
            };
            
            // "Plus" symbol instead of number
            var plusText = new TextBlock
            {
                Text = "+",
                Classes = { "filter-number" },
                FontSize = 20,
                FontWeight = FontWeight.Bold
            };
            createContainer.Children.Add(plusText);
            
            // Create new filter button
            var createButton = new Button
            {
                Content = "Create New Filter",
                Classes = { "filter-item", "create-filter-button" },
                Tag = -1, // Special tag for create button
                Width = 196,
                MinWidth = 196,
                MaxWidth = 196
            };
            
            createButton.Click += OnCreateFilterClick;
            createContainer.Children.Add(createButton);
            
            // Add create icon/indicator
            var createIcon = new Border
            {
                Width = 12,
                Height = 12,
                CornerRadius = new CornerRadius(6),
                Background = new SolidColorBrush(Color.Parse("#00ff00")), // Green for "new"
                Margin = new Thickness(8, 0, 0, 0),
                VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center
            };
            createContainer.Children.Add(createIcon);
            
            _filterListPanel.Children.Add(createContainer);
            
            // Add separator if there are existing filters
            if (_allFilters.Count > 0)
            {
                var separator = new Border
                {
                    Height = 1,
                    Background = new SolidColorBrush(Color.Parse("#444")),
                    Margin = new Thickness(20, 8, 20, 8)
                };
                _filterListPanel.Children.Add(separator);
            }
            
            if (_allFilters.Count == 0)
            {
                var noFiltersText = new TextBlock
                {
                    Text = "No existing filters found\nCreate your first filter above!",
                    FontFamily = this.FindResource("BalatroFont") as FontFamily,
                    FontSize = 12,
                    Foreground = Brushes.Gray,
                    HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center,
                    TextAlignment = TextAlignment.Center,
                    Margin = new Thickness(0, 20)
                };
                _filterListPanel.Children.Add(noFiltersText);
                return;
            }
            
            var startIndex = _currentPage * FILTERS_PER_PAGE;
            var endIndex = Math.Min(startIndex + FILTERS_PER_PAGE, _allFilters.Count);
            
            for (int i = startIndex; i < endIndex; i++)
            {
                var filter = _allFilters[i];
                var isSelected = i == _selectedFilterIndex;
                
                var container = new StackPanel
                {
                    Orientation = Avalonia.Layout.Orientation.Horizontal,
                    Margin = new Thickness(0, 2)
                };
                
                // Filter number
                var numberText = new TextBlock
                {
                    Text = filter.Index.ToString(),
                    Classes = { "filter-number" }
                };
                container.Children.Add(numberText);
                
                // Filter button
                var truncatedName = TruncateFilterName(filter.Name, 18);
                var filterButton = new Button
                {
                    Content = truncatedName,
                    Classes = { "filter-item" },
                    Tag = i,
                    Width = 196, // Fixed width for all filter buttons
                    MinWidth = 196,
                    MaxWidth = 196
                };
                
                // Add tooltip if name was truncated
                if (truncatedName != filter.Name)
                {
                    ToolTip.SetTip(filterButton, filter.Name);
                }
                
                if (isSelected)
                {
                    filterButton.Classes.Add("selected");
                }
                
                filterButton.Click += OnFilterClick;
                container.Children.Add(filterButton);
                
                // Completion indicator (small circle)
                var indicator = new Border
                {
                    Width = 12,
                    Height = 12,
                    CornerRadius = new CornerRadius(6),
                    Background = Brushes.Gray, // Could be green for "completed" filters
                    Margin = new Thickness(8, 0, 0, 0),
                    VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center
                };
                container.Children.Add(indicator);
                
                _filterListPanel.Children.Add(container);
            }
        }
        
        private void UpdatePagination()
        {
            var totalPages = Math.Max(1, (int)Math.Ceiling((double)_allFilters.Count / FILTERS_PER_PAGE));
            var currentPageDisplay = _allFilters.Count > 0 ? _currentPage + 1 : 1;
            
            if (_pageIndicatorText != null)
            {
                _pageIndicatorText.Text = $"Page {currentPageDisplay}/{totalPages}";
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
                _statusText.Text = $"Available {_allFilters.Count}/{_allFilters.Count} Filters";
            }
        }
        
        private void UpdateFilterDetails()
        {
            // Clear joker preview
            if (_jokerPreviewPanel != null)
            {
                _jokerPreviewPanel.Children.Clear();
                
                if (_selectedFilter != null)
                {
                    // Collect all items (both required and wanted)
                    var allItems = new List<(string name, bool isRequired)>();
                    
                    // Add required jokers (Must[])
                    foreach (var joker in _selectedFilter.RequiredJokers)
                    {
                        allItems.Add((joker, true));
                    }
                    
                    // Add wanted items (Should[]) - for now just show first few
                    foreach (var item in _selectedFilter.WantedItems.Take(3))
                    {
                        allItems.Add((item, false));
                    }
                    
                    if (allItems.Count > 0)
                    {
                        foreach (var (itemName, isRequired) in allItems)
                        {
                            // Determine opacity based on current tab
                            double opacity = 1.0;
                            if (_currentTab == "filter-rules" && !isRequired)
                            {
                                opacity = 0.4; // Grey out Should[] items in Filter Rules tab
                            }
                            else if (_currentTab == "scoring-rules" && isRequired)
                            {
                                opacity = 0.4; // Grey out Must[] items in Scoring Rules tab
                            }
                            
                            // Create a card container
                            var cardContainer = new Border
                            {
                                Width = 71,
                                Height = 95,
                                CornerRadius = new CornerRadius(8),
                                ClipToBounds = true,
                                Margin = new Thickness(4, 2),
                                Background = Brushes.White,
                                BorderBrush = new SolidColorBrush(Color.Parse("#444")),
                                BorderThickness = new Thickness(1),
                                Opacity = opacity
                            };
                            
                            // Create a grid for layering card base and joker face
                            var cardGrid = new Grid();
                            
                            // Add card base (background)
                            var cardBase = new Border
                            {
                                Background = new SolidColorBrush(Color.Parse("#f8f8f8")),
                                CornerRadius = new CornerRadius(6),
                                Margin = new Thickness(2)
                            };
                            cardGrid.Children.Add(cardBase);
                            
                            // Add joker face image
                            var jokerImage = SpriteService.Instance.GetJokerImage(itemName);
                            if (jokerImage != null)
                            {
                                var imageControl = new Image
                                {
                                    Source = jokerImage,
                                    Stretch = Avalonia.Media.Stretch.UniformToFill,
                                    VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center,
                                    HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center,
                                    Width = 65,
                                    Height = 89,
                                    Margin = new Thickness(3)
                                };
                                cardGrid.Children.Add(imageControl);
                            }
                            else
                            {
                                // Fallback: show item name text
                                var nameText = new TextBlock
                                {
                                    Text = itemName,
                                    FontSize = 8,
                                    Foreground = Brushes.Black,
                                    HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center,
                                    VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center,
                                    TextAlignment = TextAlignment.Center,
                                    TextWrapping = TextWrapping.Wrap,
                                    MaxWidth = 60
                                };
                                cardGrid.Children.Add(nameText);
                            }
                            
                            // Add indicator for Must vs Should
                            if (_currentTab != "overview")
                            {
                                var indicator = new Border
                                {
                                    Width = 16,
                                    Height = 16,
                                    CornerRadius = new CornerRadius(8),
                                    Background = isRequired ? 
                                        new SolidColorBrush(Color.Parse("#ff4444")) :  // Red for Must
                                        new SolidColorBrush(Color.Parse("#4444ff")),   // Blue for Should
                                    HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Right,
                                    VerticalAlignment = Avalonia.Layout.VerticalAlignment.Top,
                                    Margin = new Thickness(0, 4, 4, 0)
                                };
                                
                                var indicatorText = new TextBlock
                                {
                                    Text = isRequired ? "M" : "S",
                                    FontSize = 10,
                                    FontWeight = FontWeight.Bold,
                                    Foreground = Brushes.White,
                                    HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center,
                                    VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center
                                };
                                indicator.Child = indicatorText;
                                cardGrid.Children.Add(indicator);
                            }
                            
                            cardContainer.Child = cardGrid;
                            _jokerPreviewPanel.Children.Add(cardContainer);
                        }
                    }
                    else
                    {
                        // Show "No items" message
                        var noItemsText = new TextBlock
                        {
                            Text = "No specific items defined",
                            FontFamily = this.FindResource("BalatroFont") as FontFamily,
                            FontSize = 12,
                            Foreground = Brushes.Gray,
                            HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center,
                            VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center
                        };
                        _jokerPreviewPanel.Children.Add(noItemsText);
                    }
                }
                else
                {
                    // No filter selected
                    var selectText = new TextBlock
                    {
                        Text = "Select a filter to view items",
                        FontFamily = this.FindResource("BalatroFont") as FontFamily,
                        FontSize = 12,
                        Foreground = Brushes.Gray,
                        HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center,
                        VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center
                    };
                    _jokerPreviewPanel.Children.Add(selectText);
                }
            }
            
            // Update info panels based on current tab
            UpdateInfoPanels();
            
            // Update action buttons
            if (_editInDesignerButton != null)
            {
                _editInDesignerButton.IsEnabled = _selectedFilter != null;
            }
            
            if (_cloneFilterButton != null)
            {
                _cloneFilterButton.IsEnabled = _selectedFilter != null;
            }
        }
        
        private void UpdateInfoPanels()
        {
            if (_selectedFilter == null)
            {
                if (_topPanelContent != null) _topPanelContent.Text = "Select a filter to view details";
                if (_bottomPanelContent != null) _bottomPanelContent.Children.Clear();
                return;
            }
            
            switch (_currentTab)
            {
                case "overview":
                    UpdateOverviewPanel();
                    break;
                case "filter-rules":
                    UpdateFilterRulesPanel();
                    break;
                case "scoring-rules":
                    UpdateScoringRulesPanel();
                    break;
                default:
                    UpdateOverviewPanel();
                    break;
            }
        }
        
        private void UpdateOverviewPanel()
        {
            if (_topPanelHeader != null) _topPanelHeader.Text = "Filter Info";
            if (_topPanelContent != null)
            {
                var filterInfo = _selectedFilter!.Name;
                if (!string.IsNullOrEmpty(_selectedFilter.Author))
                {
                    filterInfo += $"\nby {_selectedFilter.Author}";
                }
                _topPanelContent.Text = filterInfo;
            }
            
            if (_bottomPanelHeader != null) _bottomPanelHeader.Text = "Description / Notes";
            if (_bottomPanelContent != null)
            {
                _bottomPanelContent.Children.Clear();
                
                var descriptionText = new TextBlock
                {
                    Text = _selectedFilter!.Description,
                    FontFamily = this.FindResource("BalatroFont") as FontFamily,
                    FontSize = 12,
                    Foreground = new SolidColorBrush(Color.Parse("#666")),
                    TextWrapping = TextWrapping.Wrap
                };
                _bottomPanelContent.Children.Add(descriptionText);
            }
        }
        
        private void UpdateFilterRulesPanel()
        {
            if (_topPanelHeader != null) _topPanelHeader.Text = "Required Items (Must Have)";
            if (_topPanelContent != null)
            {
                if (_selectedFilter!.RequiredJokers.Count > 0)
                {
                    _topPanelContent.Text = string.Join(", ", _selectedFilter.RequiredJokers) + " - These items are REQUIRED for the filter to match.";
                }
                else
                {
                    _topPanelContent.Text = "No specific required items - this is an open filter.";
                }
            }
            
            if (_bottomPanelHeader != null) _bottomPanelHeader.Text = "Optional Scoring Items (Should Have)";
            if (_bottomPanelContent != null)
            {
                _bottomPanelContent.Children.Clear();
                if (_selectedFilter!.WantedItems.Count > 0)
                {
                    var headerText = new TextBlock
                    {
                        Text = "These items provide bonus scoring but are not required:",
                        FontFamily = this.FindResource("BalatroFont") as FontFamily,
                        FontSize = 12,
                        Foreground = new SolidColorBrush(Color.Parse("#666")),
                        Margin = new Thickness(0, 0, 0, 4)
                    };
                    _bottomPanelContent.Children.Add(headerText);
                    
                    foreach (var item in _selectedFilter.WantedItems.Take(5))
                    {
                        var itemText = new TextBlock
                        {
                            Text = $"• {item}",
                            FontFamily = this.FindResource("BalatroFont") as FontFamily,
                            FontSize = 11,
                            Foreground = new SolidColorBrush(Color.Parse("#888"))
                        };
                        _bottomPanelContent.Children.Add(itemText);
                    }
                }
                else
                {
                    var noneText = new TextBlock
                    {
                        Text = "No optional scoring items defined.",
                        FontFamily = this.FindResource("BalatroFont") as FontFamily,
                        FontSize = 12,
                        Foreground = new SolidColorBrush(Color.Parse("#666"))
                    };
                    _bottomPanelContent.Children.Add(noneText);
                }
            }
        }
        
        private void UpdateScoringRulesPanel()
        {
            if (_topPanelHeader != null) _topPanelHeader.Text = "Scoring Items (Should Have)";
            if (_topPanelContent != null)
            {
                if (_selectedFilter!.WantedItems.Count > 0)
                {
                    _topPanelContent.Text = string.Join(", ", _selectedFilter.WantedItems) + " - These items provide bonus points when found.";
                }
                else
                {
                    _topPanelContent.Text = "No scoring items defined - filter uses basic matching only.";
                }
            }
            
            if (_bottomPanelHeader != null) _bottomPanelHeader.Text = "Base Requirements (Must Have)";
            if (_bottomPanelContent != null)
            {
                _bottomPanelContent.Children.Clear();
                if (_selectedFilter!.RequiredJokers.Count > 0)
                {
                    var headerText = new TextBlock
                    {
                        Text = "These items must be present for the filter to match:",
                        FontFamily = this.FindResource("BalatroFont") as FontFamily,
                        FontSize = 12,
                        Foreground = new SolidColorBrush(Color.Parse("#666")),
                        Margin = new Thickness(0, 0, 0, 4)
                    };
                    _bottomPanelContent.Children.Add(headerText);
                    
                    foreach (var joker in _selectedFilter.RequiredJokers)
                    {
                        var jokerText = new TextBlock
                        {
                            Text = $"• {joker}",
                            FontFamily = this.FindResource("BalatroFont") as FontFamily,
                            FontSize = 11,
                            Foreground = new SolidColorBrush(Color.Parse("#888"))
                        };
                        _bottomPanelContent.Children.Add(jokerText);
                    }
                }
                else
                {
                    var noneText = new TextBlock
                    {
                        Text = "No base requirements - this is an open scoring filter.",
                        FontFamily = this.FindResource("BalatroFont") as FontFamily,
                        FontSize = 12,
                        Foreground = new SolidColorBrush(Color.Parse("#666"))
                    };
                    _bottomPanelContent.Children.Add(noneText);
                }
            }
        }
        
        // Event handlers
        private void OnFilterClick(object? sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is int index)
            {
                _selectedFilterIndex = index;
                _selectedFilter = _allFilters[index];
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
            var totalPages = (int)Math.Ceiling((double)_allFilters.Count / FILTERS_PER_PAGE);
            if (_currentPage < totalPages - 1)
            {
                _currentPage++;
                UpdateUI();
            }
        }
        
        private void OnTabClick(object? sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is string tab)
            {
                _currentTab = tab;
                
                // Update tab appearance
                if (_overviewTab != null) _overviewTab.Classes.Remove("active");
                if (_filterRulesTab != null) _filterRulesTab.Classes.Remove("active");
                if (_scoringRulesTab != null) _scoringRulesTab.Classes.Remove("active");
                
                button.Classes.Add("active");
                
                // Update both the joker preview and info panels
                UpdateFilterDetails();
            }
        }
        
        private void OnEditInDesignerClick(object? sender, RoutedEventArgs e)
        {
            if (_selectedFilter != null)
            {
                // Fire the FilterSelected event to indicate editing is requested
                FilterSelected?.Invoke(this, _selectedFilter);
            }
        }
        
        private void OnCloneFilterClick(object? sender, RoutedEventArgs e)
        {
            if (_selectedFilter == null) return;
            
            try
            {
                // Generate clone filename
                var originalName = Path.GetFileNameWithoutExtension(_selectedFilter.FilePath);
                var cloneFileName = $"{originalName}-CLONE.json";
                var originalDir = Path.GetDirectoryName(_selectedFilter.FilePath);
                if (string.IsNullOrEmpty(originalDir))
                {
                    DebugLogger.LogError("BalatroFilterSelector", "Could not determine directory for original filter");
                    return;
                }
                var clonePath = Path.Combine(originalDir, cloneFileName);
                
                // Handle name collisions
                int counter = 2;
                while (File.Exists(clonePath))
                {
                    cloneFileName = $"{originalName}-CLONE{counter}.json";
                    clonePath = Path.Combine(originalDir, cloneFileName);
                    counter++;
                }
                
                // Read the original filter content
                var originalContent = File.ReadAllText(_selectedFilter.FilePath);
                
                // Modify the JSON to update the name (if it exists)
                using var doc = JsonDocument.Parse(originalContent);
                var root = doc.RootElement;
                
                // Create a dictionary to build the new JSON
                var newFilter = new Dictionary<string, object>();
                
                // Copy all properties from the original
                foreach (var property in root.EnumerateObject())
                {
                    object? value = property.Value.ValueKind switch
                    {
                        JsonValueKind.String => (object?)property.Value.GetString(),
                        JsonValueKind.Number => (object?)property.Value.GetDouble(),
                        JsonValueKind.True => (object?)true,
                        JsonValueKind.False => (object?)false,
                        JsonValueKind.Array => (object?)JsonSerializer.Deserialize<object[]>(property.Value.GetRawText()),
                        JsonValueKind.Object => (object?)JsonSerializer.Deserialize<Dictionary<string, object>>(property.Value.GetRawText()),
                        _ => (object?)property.Value.GetRawText()
                    };
                    
                    if (value != null)
                    {
                        newFilter[property.Name] = value;
                    }
                }
                
                // Update the name to indicate it's a clone
                if (newFilter.ContainsKey("name"))
                {
                    newFilter["name"] = $"{newFilter["name"]} (Clone)";
                }
                else
                {
                    newFilter["name"] = $"{originalName} (Clone)";
                }
                
                // Write the cloned filter
                var jsonOptions = new JsonSerializerOptions { WriteIndented = true };
                var clonedJson = JsonSerializer.Serialize(newFilter, jsonOptions);
                File.WriteAllText(clonePath, clonedJson);
                
                DebugLogger.Log("BalatroFilterSelector", $"Cloned filter: {_selectedFilter.Name} -> {cloneFileName}");
                
                // Reload the filter list
                LoadFilters();
                
                // Find and select the newly created clone
                for (int i = 0; i < _allFilters.Count; i++)
                {
                    if (_allFilters[i].FilePath == clonePath)
                    {
                        _selectedFilterIndex = i;
                        _selectedFilter = _allFilters[i];
                        
                        // Make sure the clone is visible on the current page
                        var targetPage = i / FILTERS_PER_PAGE;
                        if (targetPage != _currentPage)
                        {
                            _currentPage = targetPage;
                        }
                        
                        break;
                    }
                }
                
                // Update the UI to show the selected clone
                UpdateUI();
            }
            catch (Exception ex)
            {
                DebugLogger.LogError("BalatroFilterSelector", $"Error cloning filter: {ex.Message}");
            }
        }
        
        private void OnCreateFilterClick(object? sender, RoutedEventArgs e)
        {
            // Trigger the create new filter event
            CreateNewFilterRequested?.Invoke(this, EventArgs.Empty);
        }
    }
    
    public class FilterInfo
    {
        public int Index { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Author { get; set; } = string.Empty;
        public string FilePath { get; set; } = string.Empty;
        public JsonElement JsonContent { get; set; }
        public List<string> RequiredJokers { get; set; } = new();
        public List<string> WantedItems { get; set; } = new();
        public string Deck { get; set; } = "Red";
        public string Stake { get; set; } = "White";
    }
}
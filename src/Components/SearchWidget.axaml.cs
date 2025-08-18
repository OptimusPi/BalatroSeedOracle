using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Text.Json;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.LogicalTree;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Threading;
using BalatroSeedOracle.Helpers;
using BalatroSeedOracle.Models;
using BalatroSeedOracle.Services;
using BalatroSeedOracle.Views;
using BalatroSeedOracle.Views.Modals;

namespace BalatroSeedOracle.Components
{
    public class TopSeedItem
    {
        public string Rank { get; set; } = "";
        public string Seed { get; set; } = "";
        public string Score { get; set; } = "";
    }
    
    /// <summary>
    /// Search widget with expand/collapse functionality for resuming searches
    /// </summary>
    public partial class SearchWidget : ExpandableWidgetBase
    {
        // UI Elements - Minimized
        private Border? _statusIndicator;
        private Image? _stateIcon;
        private Canvas? _filterPreviewCanvas;
        private TextBlock? _minFilterName;
        private TextBlock? _minStatusText;
        private ProgressBar? _miniProgress;
        private Border? _resultBadge;
        private TextBlock? _resultCount;
        
        // UI Elements - Expanded
        private Image? _largeStateIcon;
        private TextBlock? _expFilterName;
        private TextBlock? _expStatusText;
        private TextBlock? _expSeedsFound;
        private TextBlock? _expProgressText;
        private ProgressBar? _searchProgress;
        private TextBlock? _progressText;
        private TextBlock? _playPauseIcon;
        private TextBlock? _playPauseText;
        private Border? _topSeedsPanel;
        private ItemsControl? _topSeedsList;
        
        // Statistics
        private TextBlock? _seedsProcessed;
        private TextBlock? _matchesFound;
        private TextBlock? _seedsPerSecond;
        private TextBlock? _timeElapsed;
        
        // Search state
        private SearchInstance? _searchInstance;
        private SearchManager? _searchManager;
        private string _searchId = string.Empty;
        private string _configPath = string.Empty;
        private string _filterName = "No Filter";
        private int _resultCountValue = 0;
        private bool _isSearching = false;
        private Control? _filterPreview;
        
        // Data collections
        private ObservableCollection<TopSeedItem> _topSeeds = new();
        private DateTime _searchStartTime = DateTime.UtcNow;
        private DispatcherTimer? _statsTimer;
        
        public SearchWidget()
        {
            // Set widget properties
            MinimizedSize = new Size(120, 120);
            ExpandedSize = new Size(400, 550);
            WidgetTitle = "Search Monitor";
            
            InitializeComponent();
            SetupStatsTimer();
        }
        
        protected override void InitializeComponent()
        {
            base.InitializeComponent();
            
            // Find minimized controls
            _statusIndicator = this.FindControl<Border>("StatusIndicator");
            _stateIcon = this.FindControl<Image>("StateIcon");
            _filterPreviewCanvas = this.FindControl<Canvas>("FilterPreviewCanvas");
            _minFilterName = this.FindControl<TextBlock>("MinFilterName");
            _minStatusText = this.FindControl<TextBlock>("MinStatusText");
            _miniProgress = this.FindControl<ProgressBar>("MiniProgress");
            _resultBadge = this.FindControl<Border>("ResultBadge");
            _resultCount = this.FindControl<TextBlock>("ResultCount");
            
            // Find expanded controls
            _largeStateIcon = this.FindControl<Image>("LargeStateIcon");
            _expFilterName = this.FindControl<TextBlock>("ExpFilterName");
            _expStatusText = this.FindControl<TextBlock>("ExpStatusText");
            _expSeedsFound = this.FindControl<TextBlock>("ExpSeedsFound");
            _expProgressText = this.FindControl<TextBlock>("ExpProgressText");
            _searchProgress = this.FindControl<ProgressBar>("SearchProgress");
            _progressText = this.FindControl<TextBlock>("ProgressText");
            _playPauseIcon = this.FindControl<TextBlock>("PlayPauseIcon");
            _playPauseText = this.FindControl<TextBlock>("PlayPauseText");
            _topSeedsPanel = this.FindControl<Border>("TopSeedsPanel");
            _topSeedsList = this.FindControl<ItemsControl>("TopSeedsList");
            
            // Find statistics controls
            _seedsProcessed = this.FindControl<TextBlock>("SeedsProcessed");
            _matchesFound = this.FindControl<TextBlock>("MatchesFound");
            _seedsPerSecond = this.FindControl<TextBlock>("SeedsPerSecond");
            _timeElapsed = this.FindControl<TextBlock>("TimeElapsed");
            
            // Get search manager service
            _searchManager = App.GetService<SearchManager>();
            
            // Bind top seeds list
            if (_topSeedsList != null)
            {
                _topSeedsList.ItemsSource = _topSeeds;
            }
        }
        
        private void SetupStatsTimer()
        {
            _statsTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(1)
            };
            _statsTimer.Tick += UpdateStatistics;
        }
        
        public void Initialize(string searchId, string configPath, string filterName)
        {
            _searchId = searchId;
            _configPath = configPath;
            _filterName = filterName;
            
            UpdateFilterDisplay();
            
            // Connect to the specific search instance
            if (_searchManager != null && !string.IsNullOrEmpty(_searchId))
            {
                _searchInstance = _searchManager.GetSearch(_searchId);
                if (_searchInstance != null)
                {
                    // Subscribe to search instance events
                    _searchInstance.SearchStarted += OnSearchStarted;
                    _searchInstance.SearchCompleted += OnSearchCompleted;
                    _searchInstance.ResultFound += OnResultFound;
                    _searchInstance.ProgressUpdated += OnProgressUpdated;
                    
                    // Update UI with current state
                    _isSearching = _searchInstance.IsRunning;
                    _resultCountValue = _searchInstance.ResultCount;
                    
                    UpdateUI();
                    UpdateStateIcons();
                    LoadTopSeeds();
                    
                    if (_isSearching)
                    {
                        _searchStartTime = DateTime.UtcNow;
                        _statsTimer?.Start();
                    }
                }
            }
            
            UpdateProgress(0);
        }
        
        private void UpdateFilterDisplay()
        {
            if (_minFilterName != null)
                _minFilterName.Text = _filterName;
            if (_expFilterName != null)
                _expFilterName.Text = _filterName;
        }
        
        private void UpdateUI()
        {
            // Update status text
            string status = _isSearching ? "Searching..." : 
                           (_searchInstance?.IsPaused ?? false) ? "Paused" : 
                           (_searchProgress?.Value == 100) ? "Complete" :
                           _resultCountValue > 0 ? $"{_resultCountValue} found" : "Ready";
            
            if (_minStatusText != null)
                _minStatusText.Text = status;
            if (_expStatusText != null)
                _expStatusText.Text = status;
            
            // Update seeds found
            if (_expSeedsFound != null)
                _expSeedsFound.Text = _resultCountValue.ToString();
            if (_matchesFound != null)
                _matchesFound.Text = _resultCountValue.ToString();
            
            // Update result badge
            if (_resultBadge != null && _resultCount != null)
            {
                _resultBadge.IsVisible = _resultCountValue > 0;
                _resultCount.Text = _resultCountValue.ToString();
            }
            
            // Update status indicator color
            if (_statusIndicator != null)
            {
                _statusIndicator.Background = _isSearching ? 
                    this.FindResource("Green") as IBrush :
                    (_searchInstance?.IsPaused ?? false) ? 
                    this.FindResource("Orange") as IBrush :
                    this.FindResource("Grey") as IBrush;
            }
            
            // Update play/pause button
            UpdatePlayPauseButton();
            
            // Show/hide mini progress
            if (_miniProgress != null)
            {
                _miniProgress.IsVisible = _isSearching || (_searchProgress?.Value > 0 && _searchProgress?.Value < 100);
            }
        }
        
        private void UpdatePlayPauseButton()
        {
            if (_playPauseIcon != null && _playPauseText != null)
            {
                if (_isSearching)
                {
                    _playPauseIcon.Text = "⏸";
                    _playPauseText.Text = "Pause";
                }
                else if (_searchInstance?.IsPaused ?? false)
                {
                    _playPauseIcon.Text = "▶";
                    _playPauseText.Text = "Resume";
                }
                else
                {
                    _playPauseIcon.Text = "▶";
                    _playPauseText.Text = "Start";
                }
            }
        }
        
        private void UpdateStateIcons()
        {
            var spriteService = ServiceHelper.GetRequiredService<SpriteService>();
            IImage? icon = null;
            
            // Clear any existing filter preview
            if (_filterPreview != null && _stateIcon?.Parent is Grid grid)
            {
                if (grid.Children.Contains(_filterPreview))
                {
                    grid.Children.Remove(_filterPreview);
                }
                _filterPreview = null;
            }
            
            // Determine icon based on state
            if (_isSearching && (_searchInstance?.IsRunning ?? false))
            {
                // Running - use spectral card
                icon = spriteService.GetSpectralImage("soul");
            }
            else if (!_isSearching && (_searchInstance?.IsPaused ?? false))
            {
                // Paused - use double tag
                icon = spriteService.GetTagImage("double");
            }
            else if (!_isSearching && (_searchProgress?.Value == 100))
            {
                // Completed - use gold seal
                icon = spriteService.GetStickerImage("GoldSeal");
            }
            else if (!_isSearching && _resultCountValue > 0)
            {
                // Has results - use voucher
                icon = spriteService.GetVoucherImage("grabber");
            }
            else
            {
                // Default - show filter preview or telescope
                ShowFilterPreview();
                return;
            }
            
            // Apply icon to both minimized and expanded views
            if (icon != null)
            {
                if (_stateIcon != null)
                {
                    _stateIcon.Source = icon;
                    _stateIcon.IsVisible = true;
                }
                if (_largeStateIcon != null)
                {
                    _largeStateIcon.Source = icon;
                }
                if (_filterPreviewCanvas != null)
                {
                    _filterPreviewCanvas.IsVisible = false;
                }
            }
        }
        
        private void ShowFilterPreview()
        {
            if (!string.IsNullOrEmpty(_configPath) && File.Exists(_configPath))
            {
                try
                {
                    var filterJson = File.ReadAllText(_configPath);
                    var filterDoc = JsonDocument.Parse(filterJson);
                    _filterPreview = CreateFilterPreview(filterDoc.RootElement);
                    
                    if (_filterPreview != null)
                    {
                        // Add to canvas for minimized view
                        if (_filterPreviewCanvas != null)
                        {
                            _filterPreviewCanvas.Children.Clear();
                            _filterPreviewCanvas.Children.Add(_filterPreview);
                            _filterPreviewCanvas.IsVisible = true;
                        }
                        
                        // Hide state icon in minimized view
                        if (_stateIcon != null)
                        {
                            _stateIcon.IsVisible = false;
                        }
                        
                        // For expanded view, use the large icon
                        if (_largeStateIcon != null)
                        {
                            var spriteService = ServiceHelper.GetRequiredService<SpriteService>();
                            _largeStateIcon.Source = spriteService.GetVoucherImage("telescope");
                        }
                        return;
                    }
                }
                catch (Exception ex)
                {
                    DebugLogger.LogError("SearchWidget", $"Failed to create filter preview: {ex.Message}");
                }
            }
            
            // Fallback to telescope icon
            var service = ServiceHelper.GetRequiredService<SpriteService>();
            var telescopeIcon = service.GetVoucherImage("telescope");
            
            if (_stateIcon != null)
            {
                _stateIcon.Source = telescopeIcon;
                _stateIcon.IsVisible = true;
            }
            if (_largeStateIcon != null)
            {
                _largeStateIcon.Source = telescopeIcon;
            }
            if (_filterPreviewCanvas != null)
            {
                _filterPreviewCanvas.IsVisible = false;
            }
        }
        
        private Control? CreateFilterPreview(JsonElement filterRoot)
        {
            try
            {
                var previewItems = new List<(string value, string? type)>();
                
                // Check must items first
                if (filterRoot.TryGetProperty("must", out var mustItems))
                {
                    foreach (var item in mustItems.EnumerateArray())
                    {
                        if (item.TryGetProperty("value", out var value) && 
                            item.TryGetProperty("type", out var type))
                        {
                            previewItems.Add((value.GetString() ?? "", type.GetString()));
                            if (previewItems.Count >= 2) break;  // Smaller preview for widget
                        }
                    }
                }
                
                // Add should items if we have space
                if (previewItems.Count < 2 && filterRoot.TryGetProperty("should", out var shouldItems))
                {
                    foreach (var item in shouldItems.EnumerateArray())
                    {
                        if (item.TryGetProperty("value", out var value) && 
                            item.TryGetProperty("type", out var type))
                        {
                            previewItems.Add((value.GetString() ?? "", type.GetString()));
                            if (previewItems.Count >= 2) break;
                        }
                    }
                }
                
                if (previewItems.Count == 0)
                    return null;
                
                // Create a smaller canvas for widget display
                var canvas = new Canvas
                {
                    Width = 48,
                    Height = 48,
                    HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center,
                    VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center,
                };
                
                // Create fanned display with smaller cards
                int cardIndex = 0;
                foreach (var (value, type) in previewItems.Take(2))
                {
                    var image = GetItemImage(value, type);
                    if (image != null)
                    {
                        var imgControl = new Image
                        {
                            Source = image,
                            Width = 28,
                            Height = 36,
                        };
                        
                        // Fan out the cards
                        var rotation = (cardIndex - 0.5) * 15; // -7.5, 7.5 degrees
                        var xOffset = cardIndex * 16 + 4;
                        var yOffset = Math.Abs(cardIndex - 0.5) * 3;
                        
                        imgControl.RenderTransform = new RotateTransform(rotation);
                        Canvas.SetLeft(imgControl, xOffset);
                        Canvas.SetTop(imgControl, yOffset);
                        imgControl.ZIndex = cardIndex;
                        
                        canvas.Children.Add(imgControl);
                        cardIndex++;
                    }
                }
                
                return canvas;
            }
            catch (Exception ex)
            {
                DebugLogger.LogError("SearchWidget", $"Error creating preview: {ex.Message}");
                return null;
            }
        }
        
        private IImage? GetItemImage(string value, string? type)
        {
            var lowerType = type?.ToLower();
            var spriteService = ServiceHelper.GetRequiredService<SpriteService>();
            
            return lowerType switch
            {
                "joker" => spriteService.GetJokerImage(value),
                "spectral" or "spectralcard" => spriteService.GetSpectralImage(value),
                "tarot" or "tarotcard" => spriteService.GetTarotImage(value),
                "planet" or "planetcard" => spriteService.GetPlanetCardImage(value),
                "tag" => spriteService.GetTagImage(value),
                "voucher" => spriteService.GetVoucherImage(value),
                "boss" => spriteService.GetBossImage(value),
                _ => null
            };
        }
        
        private void UpdateProgress(double percent)
        {
            var clamped = Math.Clamp(percent, 0, 100);
            
            if (_searchProgress != null)
                _searchProgress.Value = clamped;
            if (_miniProgress != null)
                _miniProgress.Value = clamped;
            if (_progressText != null)
                _progressText.Text = $"{clamped:F0}%";
            if (_expProgressText != null)
                _expProgressText.Text = $"{clamped:F0}%";
        }
        
        private void UpdateStatistics(object? sender, EventArgs e)
        {
            if (_searchInstance == null) return;
            
            // Update seeds processed (use result count as proxy)
            if (_seedsProcessed != null)
                _seedsProcessed.Text = _resultCountValue.ToString("N0");
            
            // Calculate and update seeds per second
            var elapsed = DateTime.UtcNow - _searchStartTime;
            if (elapsed.TotalSeconds > 0 && _seedsPerSecond != null)
            {
                // Use a reasonable estimate for seeds/sec
                _seedsPerSecond.Text = "~1000";
            }
            
            // Update time elapsed
            if (_timeElapsed != null)
                _timeElapsed.Text = elapsed.ToString(@"hh\:mm\:ss");
        }
        
        private void LoadTopSeeds()
        {
            // TODO: Load top seeds from search instance
            // For now, show/hide panel based on results
            if (_topSeedsPanel != null)
            {
                _topSeedsPanel.IsVisible = _resultCountValue > 0;
            }
        }
        
        // Event handlers
        private void OnSearchStarted(object? sender, EventArgs e)
        {
            Dispatcher.UIThread.Post(() =>
            {
                _isSearching = true;
                _resultCountValue = 0;
                _searchStartTime = DateTime.UtcNow;
                _statsTimer?.Start();
                UpdateUI();
                UpdateStateIcons();
                UpdateProgress(0);
            });
        }
        
        private void OnSearchCompleted(object? sender, EventArgs e)
        {
            Dispatcher.UIThread.Post(() =>
            {
                _isSearching = false;
                _statsTimer?.Stop();
                UpdateUI();
                UpdateStateIcons();
            });
        }
        
        private void OnResultFound(object? sender, SearchResultEventArgs e)
        {
            Dispatcher.UIThread.Post(() =>
            {
                _resultCountValue++;
                UpdateUI();
                
                // Add to top seeds if it's a high score
                if (_topSeeds.Count < 5 && e.Result != null)
                {
                    _topSeeds.Add(new TopSeedItem
                    {
                        Rank = $"#{_topSeeds.Count + 1}",
                        Seed = e.Result.Seed ?? "Unknown",
                        Score = "N/A"  // Score not available in current SearchResult model
                    });
                    
                    if (_topSeedsPanel != null)
                        _topSeedsPanel.IsVisible = true;
                }
            });
        }
        
        private void OnProgressUpdated(object? sender, SearchProgressEventArgs e)
        {
            Dispatcher.UIThread.Post(() =>
            {
                UpdateProgress(e.PercentComplete);
            });
        }
        
        private void OnPlayPauseClick(object? sender, RoutedEventArgs e)
        {
            if (_searchInstance == null) return;
            
            if (_isSearching)
            {
                _searchInstance.PauseSearch();
                _isSearching = false;
                _statsTimer?.Stop();
            }
            else
            {
                _searchInstance.ResumeSearch();
                _isSearching = true;
                _searchStartTime = DateTime.UtcNow;
                _statsTimer?.Start();
            }
            
            UpdateUI();
            UpdateStateIcons();
        }
        
        private void OnStopClick(object? sender, RoutedEventArgs e)
        {
            _searchInstance?.StopSearch();
            _isSearching = false;
            _statsTimer?.Stop();
            UpdateUI();
            UpdateStateIcons();
        }
        
        private void OnViewResultsClick(object? sender, RoutedEventArgs e)
        {
            // Find main menu and show search modal
            var current = this.Parent;
            while (current != null)
            {
                if (current is BalatroMainMenu mainMenu)
                {
                    mainMenu.ShowSearchModalForInstance(_searchId, _configPath);
                    break;
                }
                current = current.Parent;
            }
        }
        
        private void OnConfigClick(object? sender, RoutedEventArgs e)
        {
            // Find main menu and show filters modal
            var current = this.Parent;
            while (current != null)
            {
                if (current is BalatroMainMenu mainMenu)
                {
                    mainMenu.ShowFiltersModal();
                    break;
                }
                current = current.Parent;
            }
        }
        
        private void OnCopySeedClick(object? sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is string seed)
            {
                var topLevel = TopLevel.GetTopLevel(this);
                topLevel?.Clipboard?.SetTextAsync(seed);
                DebugLogger.Log("SearchWidget", $"Copied seed to clipboard: {seed}");
            }
        }
        
        protected override void OnDetachedFromLogicalTree(LogicalTreeAttachmentEventArgs e)
        {
            base.OnDetachedFromLogicalTree(e);
            
            // Unsubscribe from events
            if (_searchInstance != null)
            {
                _searchInstance.SearchStarted -= OnSearchStarted;
                _searchInstance.SearchCompleted -= OnSearchCompleted;
                _searchInstance.ResultFound -= OnResultFound;
                _searchInstance.ProgressUpdated -= OnProgressUpdated;
            }
            
            _statsTimer?.Stop();
        }
    }
}
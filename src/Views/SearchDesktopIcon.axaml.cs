using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Linq;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Shapes;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
using Avalonia.VisualTree;
using BalatroSeedOracle.Helpers;
using BalatroSeedOracle.Models;
using BalatroSeedOracle.Services;
using BalatroSeedOracle.Views.Modals;

namespace BalatroSeedOracle.Views
{
    public partial class SearchDesktopIcon : BalatroSeedOracle.Components.CollapsibleWidgetBase
    {
        private Border? _notificationBadge;
        private TextBlock? _badgeText;
        private TextBlock? _filterNameText;
        private TextBlock? _progressText;
        private ProgressBar? _searchProgress;
    private Image? _stateIcon;
    private Control? _filterPreview;
    private Border? _iconBorder;
    // Quick view references
    private Border? _expandedView;
    private Border? _minimizedRoot;
    private TextBlock? _quickFilterName;
    private TextBlock? _quickResultCount;
    private TextBlock? _quickProgressText;
    private TextBlock? _quickStateText;
    private ProgressBar? _quickProgressBar;
    private ItemsControl? _quickResultsList;
    private Button? _quickStopButton;
    private Button? _quickResumeButton;
    private readonly System.Collections.ObjectModel.ObservableCollection<BalatroSeedOracle.Models.SearchResult> _quickRecentResults = new();
        private SearchInstance? _searchInstance;
        private SearchManager? _searchManager;
        private string _searchId = string.Empty;
        private string _configPath = string.Empty;
        private string _filterName = "No Filter";
        private int _resultCount = 0;
        private bool _isSearching = false;

        public SearchDesktopIcon()
        {
            InitializeComponent();
            InitializeCollapsibleParts();
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);

            _notificationBadge = this.FindControl<Border>("NotificationBadge");
            _badgeText = this.FindControl<TextBlock>("BadgeText");
            _filterNameText = this.FindControl<TextBlock>("FilterNameText");
            _progressText = this.FindControl<TextBlock>("ProgressText");
            _searchProgress = this.FindControl<ProgressBar>("SearchProgress");
            _stateIcon = this.FindControl<Image>("StateIcon");
            _iconBorder = this.FindControl<Border>("IconBorder");
            _expandedView = this.FindControl<Border>("ExpandedView");
            _minimizedRoot = this.FindControl<Border>("MinimizedRoot");
            _quickFilterName = this.FindControl<TextBlock>("QuickFilterName");
            _quickResultCount = this.FindControl<TextBlock>("QuickResultCount");
            _quickProgressText = this.FindControl<TextBlock>("QuickProgressText");
            _quickStateText = this.FindControl<TextBlock>("QuickStateText");
            _quickProgressBar = this.FindControl<ProgressBar>("QuickProgressBar");
            _quickResultsList = this.FindControl<ItemsControl>("QuickResultsList");
            _quickStopButton = this.FindControl<Button>("QuickStopButton");
            _quickResumeButton = this.FindControl<Button>("QuickResumeButton");

            if (_quickResultsList != null)
            {
                _quickResultsList.ItemsSource = _quickRecentResults;
            }

            // Get search manager service
            _searchManager = App.GetService<SearchManager>();
        }

        public void Initialize(string searchId, string configPath, string filterName)
        {
            _searchId = searchId;
            _configPath = configPath;
            _filterName = filterName;

            if (_filterNameText != null)
            {
                _filterNameText.Text = _filterName;
            }

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
                    _resultCount = _searchInstance.ResultCount;
                    UpdateBadge();
                    UpdateStateIcon();
                }
            }

            UpdateProgress(0);
        }

        private void OnIconClick(object? sender, RoutedEventArgs e)
        {
            DebugLogger.Log("SearchDesktopIcon", $"OnIconClick called - SearchId: {_searchId}, ConfigPath: {_configPath}");
            
            // Find the BalatroMainMenu by traversing up the visual tree
            BalatroMainMenu? mainMenu = null;
            var current = this as Visual;
            
            while (current != null && mainMenu == null)
            {
                current = current.GetVisualParent<Visual>();
                mainMenu = current as BalatroMainMenu;
            }
            
            DebugLogger.Log("SearchDesktopIcon", $"MainMenu found via parent traversal: {mainMenu != null}");

            if (mainMenu == null)
            {
                // Try window content as fallback
                var window = TopLevel.GetTopLevel(this) as Window;
                mainMenu = window?.Content as BalatroMainMenu;
                DebugLogger.Log("SearchDesktopIcon", $"MainMenu found via window content: {mainMenu != null}");
            }

            if (mainMenu == null)
            {
                DebugLogger.LogError("SearchDesktopIcon", "Could not find BalatroMainMenu to show search modal");
                return;
            }

            try
            {
                // Show search modal with current search instance
                mainMenu.ShowSearchModalForInstance(_searchId, _configPath);
                DebugLogger.Log("SearchDesktopIcon", "ShowSearchModalForInstance called successfully");
                // Collapse back to minimized after opening full modal
                Collapse();
            }
            catch (Exception ex)
            {
                DebugLogger.LogError("SearchDesktopIcon", $"Error showing search modal: {ex}");
            }
        }

        private void OnMinimizedClick(object? sender, PointerPressedEventArgs e)
        {
            Expand();
            UpdateQuickView();
        }

        private void OnMinimizeClick(object? sender, RoutedEventArgs e)
        {
            Collapse();
        }

        private void OnOpenFullSearch(object? sender, RoutedEventArgs e)
        {
            OnIconClick(sender, e);
        }

        private void OnQuickStop(object? sender, RoutedEventArgs e)
        {
            OnStopSearch(sender, e);
        }

        private void OnQuickResume(object? sender, RoutedEventArgs e)
        {
            OnResumeSearch(sender, e);
        }

        private void OnContextRequested(object? sender, ContextRequestedEventArgs e)
        {
            var contextMenu = new ContextMenu();

            var viewResultsItem = new MenuItem { Header = "View Results" };
            viewResultsItem.Click += (s, ev) => OnIconClick(s, ev);
            contextMenu.Items.Add(viewResultsItem);

            contextMenu.Items.Add(new Separator());

            if (_isSearching)
            {
                var pauseItem = new MenuItem { Header = "Pause Search" };
                pauseItem.Click += OnPauseSearch;
                contextMenu.Items.Add(pauseItem);

                var stopItem = new MenuItem { Header = "Stop Search" };
                stopItem.Click += OnStopSearch;
                contextMenu.Items.Add(stopItem);
            }
            else
            {
                var resumeItem = new MenuItem { Header = "Resume Search" };
                resumeItem.Click += OnResumeSearch;
                contextMenu.Items.Add(resumeItem);
            }

            contextMenu.Items.Add(new Separator());

            var deleteItem = new MenuItem { Header = "Remove Icon" };
            deleteItem.Click += OnDeleteIcon;
            contextMenu.Items.Add(deleteItem);

            contextMenu.Open(sender as Control);
            e.Handled = true;
        }

        private void OnSearchStarted(object? sender, EventArgs e)
        {
            Avalonia.Threading.Dispatcher.UIThread.Post(() =>
            {
                _isSearching = true;
                _resultCount = 0;
                UpdateBadge();
                UpdateProgress(0);
                UpdateStateIcon();
                if (_iconBorder != null && !_iconBorder.Classes.Contains("searching"))
                    _iconBorder.Classes.Add("searching");
                UpdateQuickView();
            });
        }

        private void OnSearchCompleted(object? sender, EventArgs e)
        {
            Avalonia.Threading.Dispatcher.UIThread.Post(() =>
            {
                _isSearching = false;
                // Don't update progress to 100% - keep current progress
                // Search may have been stopped/cancelled early
                UpdateStateIcon();
                if (_iconBorder != null && _iconBorder.Classes.Contains("searching"))
                    _iconBorder.Classes.Remove("searching");
                UpdateQuickView();
            });
        }

    private void OnResultFound(object? sender, BalatroSeedOracle.Models.SearchResultEventArgs e)
        {
            Avalonia.Threading.Dispatcher.UIThread.Post(() =>
            {
                _resultCount++;
                UpdateBadge();
                // Keep running icon; no change needed here.
                if (_searchInstance != null && e?.Result != null)
                {
                    // Store up to last 15
                    _quickRecentResults.Insert(0, e.Result);
                    while (_quickRecentResults.Count > 15)
                        _quickRecentResults.RemoveAt(_quickRecentResults.Count - 1);
                }
                UpdateQuickView();
            });
        }

        private void OnProgressUpdated(object? sender, SearchProgressEventArgs e)
        {
            Avalonia.Threading.Dispatcher.UIThread.Post(() =>
            {
                UpdateProgress((int)e.PercentComplete);
                UpdateQuickView();
            });
        }

        private void UpdateBadge()
        {
            if (_notificationBadge != null && _badgeText != null)
            {
                if (_resultCount > 0)
                {
                    _notificationBadge.IsVisible = true;
                    _badgeText.Text = _resultCount.ToString(
                        System.Globalization.CultureInfo.InvariantCulture
                    );
                }
                else
                {
                    _notificationBadge.IsVisible = false;
                }
            }
        }

        private void UpdateProgress(double percent)
        {
            if (_searchProgress != null && _progressText != null)
            {
                var clamped = (int)Math.Clamp(percent, 0, 100);
                _searchProgress.Value = clamped;
                _progressText.Text = $"{clamped}%";
            }
        }

        private void OnPauseSearch(object? sender, RoutedEventArgs e)
        {
            _searchInstance?.PauseSearch();
            _isSearching = false;
            UpdateStateIcon();
            UpdateQuickView();
        }

        private void OnResumeSearch(object? sender, RoutedEventArgs e)
        {
            _searchInstance?.ResumeSearch();
            _isSearching = true;
            UpdateStateIcon();
            UpdateQuickView();
        }

        private void OnStopSearch(object? sender, RoutedEventArgs e)
        {
            _searchInstance?.StopSearch();
            _isSearching = false;
            // Don't update progress - keep current progress when stopped
            UpdateStateIcon();
            UpdateQuickView();
        }

        private void OnDismissClick(object? sender, RoutedEventArgs e)
        {
            OnDeleteIcon(sender, e);
        }

        private void OnDeleteIcon(object? sender, RoutedEventArgs e)
        {
            DebugLogger.Log("SearchDesktopIcon", $"OnDeleteIcon called for search {_searchId}, isSearching={_isSearching}");
            
            // FIRST stop the search BEFORE clearing state!
            // Otherwise the search will save its state again while stopping
            if (_searchInstance != null)
            {
                DebugLogger.Log("SearchDesktopIcon", "Stopping search instance without saving state");
                // Stop the search instance WITHOUT saving state
                _searchInstance.StopSearch(true);
            }
            else
            {
                DebugLogger.Log("SearchDesktopIcon", "No search instance to stop (placeholder icon)");
            }
            
            // ALWAYS clear the saved search state, even for placeholder icons!
            var userProfileService = ServiceHelper.GetService<UserProfileService>();
            if (userProfileService != null)
            {
                DebugLogger.Log("SearchDesktopIcon", "Clearing saved search state from user profile");
                userProfileService.ClearSearchState();
                
                // Force save the profile immediately to persist the deletion
                DebugLogger.Log("SearchDesktopIcon", "Flushing profile to disk");
                userProfileService.FlushProfile();
            }

            // Remove search from manager if it exists
            if (!string.IsNullOrEmpty(_searchId) && _searchManager != null)
            {
                DebugLogger.Log("SearchDesktopIcon", $"Removing search {_searchId} from manager");
                _searchManager.RemoveSearch(_searchId);
            }

            // Remove this icon from parent
            if (this.Parent is Panel parent)
            {
                DebugLogger.Log("SearchDesktopIcon", "Removing icon from parent");
                parent.Children.Remove(this);
            }
            
            DebugLogger.Log("SearchDesktopIcon", "Icon deletion complete");
        }

        private void UpdateStateIcon()
        {
            if (_stateIcon == null)
                return;

            var spriteService = ServiceHelper.GetRequiredService<SpriteService>();
            
            // Clear any existing filter preview when showing state icons
            if (_filterPreview != null && _stateIcon.Parent is Grid grid)
            {
                if (grid.Children.Contains(_filterPreview))
                {
                    grid.Children.Remove(_filterPreview);
                }
                _filterPreview = null;
            }
            
            // Priority order: running, paused, completed (finished with progress 100), idle
            if (_isSearching && (_searchInstance?.IsRunning ?? false))
            {
                // Running - use a spectral card (The Soul)
                _stateIcon.IsVisible = true;
                _stateIcon.Source = spriteService.GetSpectralImage("soul");
                if (_iconBorder != null && !_iconBorder.Classes.Contains("searching"))
                    _iconBorder.Classes.Add("searching");
            }
            else if (!_isSearching && (_searchInstance?.IsPaused ?? false))
            {
                // Paused - use a tag (double tag for pause symbol)
                _stateIcon.IsVisible = true;
                _stateIcon.Source = spriteService.GetTagImage("double");
                if (_iconBorder != null && _iconBorder.Classes.Contains("searching"))
                    _iconBorder.Classes.Remove("searching");
            }
            else if (!_isSearching && (_searchProgress?.Value == 100))
            {
                // Completed - use gold seal
                _stateIcon.IsVisible = true;
                _stateIcon.Source = spriteService.GetStickerImage("GoldSeal");
                if (_iconBorder != null && _iconBorder.Classes.Contains("searching"))
                    _iconBorder.Classes.Remove("searching");
            }
            else if (!_isSearching && _resultCount > 0)
            {
                // Has results - use a voucher
                _stateIcon.IsVisible = true;
                _stateIcon.Source = spriteService.GetVoucherImage("grabber");
                if (_iconBorder != null && _iconBorder.Classes.Contains("searching"))
                    _iconBorder.Classes.Remove("searching");
            }
            else
            {
                // Idle / default - show the filter preview if possible
                if (_iconBorder != null && _iconBorder.Classes.Contains("searching"))
                    _iconBorder.Classes.Remove("searching");
                ShowFilterPreview();
            }
            UpdateQuickView();
        }

        private void UpdateQuickView()
        {
            if (_quickFilterName != null)
                _quickFilterName.Text = _filterName;
            if (_quickResultCount != null)
                _quickResultCount.Text = _resultCount.ToString(System.Globalization.CultureInfo.InvariantCulture);
            if (_quickProgressText != null && _searchProgress != null)
                _quickProgressText.Text = _searchProgress.Value.ToString(System.Globalization.CultureInfo.InvariantCulture) + "%";
            if (_quickProgressBar != null && _searchProgress != null)
                _quickProgressBar.Value = _searchProgress.Value;
            if (_quickStateText != null)
            {
                if (_isSearching && (_searchInstance?.IsRunning ?? false)) _quickStateText.Text = "Running";
                else if (_searchInstance?.IsPaused ?? false) _quickStateText.Text = "Paused";
                else if (_searchProgress?.Value == 100) _quickStateText.Text = "Complete";
                else _quickStateText.Text = "Idle";
            }
            if (_quickStopButton != null)
                _quickStopButton.IsVisible = _isSearching && (_searchInstance?.IsRunning ?? false);
            if (_quickResumeButton != null)
                _quickResumeButton.IsVisible = !_isSearching && (_searchInstance?.IsPaused ?? false);
        }

    // (Handlers defined earlier in file retained)
        
        private void ShowFilterPreview()
        {
            if (_filterPreview == null && !string.IsNullOrEmpty(_configPath) && File.Exists(_configPath))
            {
                try
                {
                    var filterJson = File.ReadAllText(_configPath);
                    var filterDoc = JsonDocument.Parse(filterJson);
                    _filterPreview = CreateFilterPreview(filterDoc.RootElement);
                    
                    if (_filterPreview != null && _stateIcon != null && _stateIcon.Parent is Grid parentGrid)
                    {
                        // Hide the image and show the preview control
                        _stateIcon.IsVisible = false;
                        Grid.SetRow(_filterPreview, Grid.GetRow(_stateIcon));
                        Grid.SetColumn(_filterPreview, Grid.GetColumn(_stateIcon));
                        parentGrid.Children.Add(_filterPreview);
                    }
                    else if (_stateIcon != null)
                    {
                        // Fallback to telescope icon if preview failed
                        _stateIcon.IsVisible = true;
                        var spriteService = ServiceHelper.GetRequiredService<SpriteService>();
                        _stateIcon.Source = spriteService.GetVoucherImage("telescope");
                    }
                }
                catch (Exception ex)
                {
                    DebugLogger.LogError("SearchDesktopIcon", $"Failed to create filter preview: {ex.Message}");
                    // Fallback to telescope icon
                    if (_stateIcon != null)
                    {
                        _stateIcon.IsVisible = true;
                        var spriteService = ServiceHelper.GetRequiredService<SpriteService>();
                        _stateIcon.Source = spriteService.GetVoucherImage("telescope");
                    }
                }
            }
            else if (_stateIcon != null)
            {
                // Default to telescope icon
                _stateIcon.IsVisible = true;
                var spriteService = ServiceHelper.GetRequiredService<SpriteService>();
                _stateIcon.Source = spriteService.GetVoucherImage("telescope");
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
                            if (previewItems.Count() >= 3) break;  // Smaller preview for icon
                        }
                    }
                }

                // Add should items if we have space
                if (previewItems.Count() < 3 && filterRoot.TryGetProperty("should", out var shouldItems))
                {
                    foreach (var item in shouldItems.EnumerateArray())
                    {
                        if (item.TryGetProperty("value", out var value) && 
                            item.TryGetProperty("type", out var type))
                        {
                            previewItems.Add((value.GetString() ?? "", type.GetString()));
                            if (previewItems.Count >= 3) break;
                        }
                    }
                }

                if (previewItems.Count() == 0)
                {
                    return null;
                }

                // Create a smaller canvas for icon display
                var canvas = new Canvas
                {
                    Width = 80,
                    Height = 50,
                    HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center,
                    VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center,
                };

                // Create fanned display with smaller cards
                int cardIndex = 0;
                foreach (var (value, type) in previewItems.Take(3))
                {
                    var image = GetItemImage(value, type);
                    if (image != null)
                    {
                        var imgControl = new Image
                        {
                            Source = image,
                            Width = 30,
                            Height = 40,
                        };

                        // Fan out the cards
                        var rotation = (cardIndex - 1) * 8; // -8, 0, 8 degrees
                        var xOffset = cardIndex * 18 + 5;
                        var yOffset = Math.Abs(cardIndex - 1) * 2; // Slight Y offset

                        imgControl.RenderTransform = new Avalonia.Media.RotateTransform(rotation);
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
                DebugLogger.LogError("SearchDesktopIcon", $"Error creating preview: {ex.Message}");
                return null;
            }
        }
        
        private IImage? GetItemImage(string value, string? type)
        {
            // Get image based on type
            var lowerType = type?.ToLower();
            var spriteService = ServiceHelper.GetRequiredService<SpriteService>();
            
            switch (lowerType)
            {
                case "joker":
                    return spriteService.GetJokerImage(value);
                case "spectral":
                    return spriteService.GetSpectralImage(value);
                case "tarot":
                    return spriteService.GetTarotImage(value);
                case "planet":
                    return spriteService.GetPlanetCardImage(value);
                case "tag":
                    return spriteService.GetTagImage(value);
                case "voucher":
                    return spriteService.GetVoucherImage(value);
                case "booster":
                    return spriteService.GetBoosterImage(value);
                case "deck":
                    return spriteService.GetDeckImage(value);
                case "consumable":
                    // Could be tarot, planet, or spectral
                    var tarot = spriteService.GetTarotImage(value);
                    if (tarot != null) return tarot;
                    var planet = spriteService.GetPlanetCardImage(value);
                    if (planet != null) return planet;
                    var spectral = spriteService.GetSpectralImage(value);
                    return spectral;
                case "playingcard":
                    // Parse playing card format
                    if (value.Contains("_"))
                    {
                        var parts = value.Split('_');
                        if (parts.Length == 2)
                        {
                            return spriteService.GetPlayingCardImage(parts[0], parts[1]);
                        }
                    }
                    break;
            }
            return null;
        }
    }
}

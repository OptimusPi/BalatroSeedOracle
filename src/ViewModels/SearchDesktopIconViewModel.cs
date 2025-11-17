using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Threading;
using BalatroSeedOracle.Helpers;
using BalatroSeedOracle.Models;
using BalatroSeedOracle.Services;
using CommunityToolkit.Mvvm.Input;

namespace BalatroSeedOracle.ViewModels
{
    /// <summary>
    /// ViewModel for the SearchDesktopIcon
    /// Manages state, business logic, and commands for the desktop search icon widget
    /// </summary>
    public partial class SearchDesktopIconViewModel : BaseWidgetViewModel, IDisposable
    {
        #region Services (Injected)

        private readonly SearchManager? _searchManager;
        private readonly UserProfileService _userProfileService;
        private readonly SpriteService _spriteService;

        #endregion

        #region Private Fields

        private SearchInstance? _searchInstance;
        private string _searchId = string.Empty;
        private string _configPath = string.Empty;

        #endregion

        #region Observable Properties

        private string _filterName = "No Filter";

        /// <summary>
        /// Display name of the filter
        /// </summary>
        public string FilterName
        {
            get => _filterName;
            set => SetProperty(ref _filterName, value);
        }

        private int _resultCount = 0;

        /// <summary>
        /// Number of results found
        /// </summary>
        public int ResultCount
        {
            get => _resultCount;
            set
            {
                if (SetProperty(ref _resultCount, value))
                {
                    UpdateBadgeNotification();
                }
            }
        }

        private bool _isSearching = false;

        /// <summary>
        /// Whether search is currently running
        /// </summary>
        public bool IsSearching
        {
            get => _isSearching;
            set
            {
                if (SetProperty(ref _isSearching, value))
                {
                    UpdateStateIcon();
                }
            }
        }

        private double _searchProgress = 0;

        /// <summary>
        /// Search progress percentage (0-100)
        /// </summary>
        public double SearchProgress
        {
            get => _searchProgress;
            set
            {
                if (SetProperty(ref _searchProgress, value))
                {
                    OnPropertyChanged(nameof(ProgressText));
                    UpdateStateIcon();
                }
            }
        }

        private IImage? _stateIcon;

        /// <summary>
        /// State icon image (running, paused, completed, etc.)
        /// </summary>
        public IImage? StateIcon
        {
            get => _stateIcon;
            set => SetProperty(ref _stateIcon, value);
        }

        private Control? _filterPreview;

        /// <summary>
        /// Filter preview control (fanned cards)
        /// </summary>
        public Control? FilterPreview
        {
            get => _filterPreview;
            set => SetProperty(ref _filterPreview, value);
        }

        private bool _showStateIcon = true;

        /// <summary>
        /// Whether to show state icon (hide when showing preview)
        /// </summary>
        public bool ShowStateIcon
        {
            get => _showStateIcon;
            set => SetProperty(ref _showStateIcon, value);
        }

        /// <summary>
        /// Formatted progress text
        /// </summary>
        public string ProgressText => $"{(int)Math.Clamp(SearchProgress, 0, 100)}%";

        #endregion

        #region Commands

        public IAsyncRelayCommand ViewResultsCommand { get; }
        public IRelayCommand PauseSearchCommand { get; }
        public IRelayCommand ResumeSearchCommand { get; }
        public IRelayCommand StopSearchCommand { get; }
        public IRelayCommand DeleteIconCommand { get; }

        #endregion

        #region Events

        /// <summary>
        /// Raised when user requests to view results in search modal
        /// Passes the search ID and config path
        /// </summary>
        public event EventHandler<(string searchId, string configPath)>? ViewResultsRequested;

        #endregion

        #region Constructor

        /// <summary>
        /// Constructor with dependency injection
        /// </summary>
        public SearchDesktopIconViewModel(
            SearchManager? searchManager,
            UserProfileService userProfileService,
            SpriteService spriteService
        )
        {
            _searchManager = searchManager;
            _userProfileService =
                userProfileService ?? throw new ArgumentNullException(nameof(userProfileService));
            _spriteService =
                spriteService ?? throw new ArgumentNullException(nameof(spriteService));

            // Initialize widget properties
            WidgetTitle = "Search";
            WidgetIcon = "🔍";

            // Initialize commands
            ViewResultsCommand = new AsyncRelayCommand(OnViewResultsAsync);
            PauseSearchCommand = new RelayCommand(OnPauseSearch, CanPauseSearch);
            ResumeSearchCommand = new RelayCommand(OnResumeSearch, CanResumeSearch);
            StopSearchCommand = new RelayCommand(OnStopSearch, CanStopSearch);
            DeleteIconCommand = new RelayCommand(OnDeleteIcon);
        }

        /// <summary>
        /// Parameterless constructor for design-time support
        /// </summary>
        public SearchDesktopIconViewModel()
            : this(
                App.GetService<SearchManager>(),
                new UserProfileService(),
                ServiceHelper.GetRequiredService<SpriteService>()
            )
        { }

        #endregion

        #region Public Methods

        /// <summary>
        /// Initialize the icon with search details
        /// </summary>
        public void Initialize(string searchId, string configPath, string filterName)
        {
            _searchId = searchId;
            _configPath = configPath;
            FilterName = filterName;
            // Reflect filter name in widget title for BaseWidget minimized state
            WidgetTitle = filterName;

            // Connect to the specific search instance
            if (_searchManager != null && !string.IsNullOrEmpty(_searchId))
            {
                _searchInstance = _searchManager.GetSearch(_searchId);
                if (_searchInstance != null)
                {
                    // Subscribe to search instance events
                    _searchInstance.SearchStarted += OnSearchStarted;
                    _searchInstance.SearchCompleted += OnSearchCompleted;
                    _searchInstance.ProgressUpdated += OnProgressUpdated;

                    // Update UI with current state
                    IsSearching = _searchInstance.IsRunning;
                    ResultCount = _searchInstance.ResultCount;
                    UpdateBadgeNotification();
                    UpdateStateIcon();
                }
            }

            SearchProgress = 0;
        }

        /// <summary>
        /// Get the search ID (public accessor to avoid reflection)
        /// </summary>
        public string GetSearchId() => _searchId;

        #endregion

        #region Private Methods - Business Logic

        /// <summary>
        /// Update notification badge based on result count
        /// </summary>
        private void UpdateBadgeNotification()
        {
            if (ResultCount > 0)
            {
                SetNotification(ResultCount);
            }
            else
            {
                ClearNotification();
            }
        }

        /// <summary>
        /// Update the state icon based on search state
        /// </summary>
        private void UpdateStateIcon()
        {
            // Priority order: running, paused, completed (finished with progress 100), idle
            if (IsSearching && (_searchInstance?.IsRunning ?? false))
            {
                // Running - use a spectral card (The Soul)
                ShowStateIcon = true;
                FilterPreview = null;
                StateIcon = _spriteService.GetSpectralImage("soul");
            }
            else if (!IsSearching && (_searchInstance?.IsPaused ?? false))
            {
                // Paused - use a tag (double tag for pause symbol)
                ShowStateIcon = true;
                FilterPreview = null;
                StateIcon = _spriteService.GetTagImage("double");
            }
            else if (!IsSearching && SearchProgress >= 100)
            {
                // Completed - use gold seal
                ShowStateIcon = true;
                FilterPreview = null;
                StateIcon = _spriteService.GetStickerImage("GoldSeal");
            }
            else if (!IsSearching && ResultCount > 0)
            {
                // Has results - use a voucher
                ShowStateIcon = true;
                FilterPreview = null;
                StateIcon = _spriteService.GetVoucherImage("grabber");
            }
            else
            {
                // Idle / default - show the filter preview if possible
                ShowFilterPreview();
            }
        }

        /// <summary>
        /// Show filter preview (fanned cards)
        /// </summary>
        private void ShowFilterPreview()
        {
            if (!string.IsNullOrEmpty(_configPath) && File.Exists(_configPath))
            {
                try
                {
                    var filterJson = File.ReadAllText(_configPath);
                    var filterDoc = JsonDocument.Parse(filterJson);
                    FilterPreview = CreateFilterPreview(filterDoc.RootElement);
                    ShowStateIcon = false;
                }
                catch (Exception ex)
                {
                    DebugLogger.LogError(
                        "SearchDesktopIconViewModel",
                        $"Failed to create filter preview: {ex.Message}"
                    );
                    // Fallback to telescope icon
                    ShowStateIcon = true;
                    FilterPreview = null;
                    StateIcon = _spriteService.GetVoucherImage("telescope");
                }
            }
            else
            {
                // Default to telescope icon
                ShowStateIcon = true;
                FilterPreview = null;
                StateIcon = _spriteService.GetVoucherImage("telescope");
            }
        }

        /// <summary>
        /// Create filter preview control with fanned cards
        /// </summary>
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
                        if (
                            item.TryGetProperty("value", out var value)
                            && item.TryGetProperty("type", out var type)
                        )
                        {
                            previewItems.Add((value.GetString() ?? "", type.GetString()));
                            if (previewItems.Count >= 3)
                                break;
                        }
                    }
                }

                // Add should items if we have space
                if (
                    previewItems.Count < 3
                    && filterRoot.TryGetProperty("should", out var shouldItems)
                )
                {
                    foreach (var item in shouldItems.EnumerateArray())
                    {
                        if (
                            item.TryGetProperty("value", out var value)
                            && item.TryGetProperty("type", out var type)
                        )
                        {
                            previewItems.Add((value.GetString() ?? "", type.GetString()));
                            if (previewItems.Count >= 3)
                                break;
                        }
                    }
                }

                if (previewItems.Count == 0)
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
                DebugLogger.LogError(
                    "SearchDesktopIconViewModel",
                    $"Error creating preview: {ex.Message}"
                );
                return null;
            }
        }

        /// <summary>
        /// Get image for a filter item based on type
        /// </summary>
        private IImage? GetItemImage(string value, string? type)
        {
            var lowerType = type?.ToLower();

            switch (lowerType)
            {
                case "joker":
                    return _spriteService.GetJokerImage(value);
                case "spectral":
                    return _spriteService.GetSpectralImage(value);
                case "tarot":
                    return _spriteService.GetTarotImage(value);
                case "planet":
                    return _spriteService.GetPlanetCardImage(value);
                case "tag":
                    return _spriteService.GetTagImage(value);
                case "voucher":
                    return _spriteService.GetVoucherImage(value);
                case "booster":
                    return _spriteService.GetBoosterImage(value);
                case "deck":
                    return _spriteService.GetDeckImage(value);
                case "consumable":
                    // Could be tarot, planet, or spectral
                    var tarot = _spriteService.GetTarotImage(value);
                    if (tarot != null)
                        return tarot;
                    var planet = _spriteService.GetPlanetCardImage(value);
                    if (planet != null)
                        return planet;
                    var spectral = _spriteService.GetSpectralImage(value);
                    return spectral;
                case "playingcard":
                    // Parse playing card format
                    if (value.Contains("_"))
                    {
                        var parts = value.Split('_');
                        if (parts.Length == 2)
                        {
                            return _spriteService.GetPlayingCardImage(parts[0], parts[1]);
                        }
                    }
                    break;
            }
            return null;
        }

        #endregion

        #region Event Handlers

        /// <summary>
        /// Called when search starts
        /// </summary>
        private void OnSearchStarted(object? sender, EventArgs e)
        {
            Dispatcher.UIThread.Post(() =>
            {
                IsSearching = true;
                ResultCount = 0;
                SearchProgress = 0;
                UpdateStateIcon();
            });
        }

        /// <summary>
        /// Called when search completes
        /// </summary>
        private void OnSearchCompleted(object? sender, EventArgs e)
        {
            Dispatcher.UIThread.Post(() =>
            {
                IsSearching = false;
                UpdateStateIcon();
            });
        }

        /// <summary>
        /// Called when search progress updates
        /// </summary>
        private void OnProgressUpdated(object? sender, SearchProgress progress)
        {
            Dispatcher.UIThread.Post(() =>
            {
                SearchProgress = progress.PercentComplete;
                ResultCount = progress.ResultsFound;
                UpdateBadgeNotification();
            });
        }

        #endregion

        #region Command Handlers

        /// <summary>
        /// View results in search modal
        /// </summary>
        private async Task OnViewResultsAsync()
        {
            DebugLogger.Log(
                "SearchDesktopIconViewModel",
                $"ViewResults called - SearchId: {_searchId}, ConfigPath: {_configPath}"
            );

            // Raise event for view to handle
            ViewResultsRequested?.Invoke(this, (_searchId, _configPath));

            await Task.CompletedTask;
        }

        /// <summary>
        /// Check if search can be paused
        /// </summary>
        private bool CanPauseSearch() => IsSearching && (_searchInstance?.IsRunning ?? false);

        /// <summary>
        /// Pause the search
        /// </summary>
        private void OnPauseSearch()
        {
            _searchInstance?.PauseSearch();
            IsSearching = false;
            UpdateStateIcon();
        }

        /// <summary>
        /// Check if search can be resumed
        /// </summary>
        private bool CanResumeSearch() => !IsSearching && (_searchInstance?.IsPaused ?? false);

        /// <summary>
        /// Resume the search
        /// </summary>
        private void OnResumeSearch()
        {
            _searchInstance?.ResumeSearch();
            IsSearching = true;
            UpdateStateIcon();
        }

        /// <summary>
        /// Check if search can be stopped
        /// </summary>
        private bool CanStopSearch() => _searchInstance != null;

        /// <summary>
        /// Stop the search
        /// </summary>
        private void OnStopSearch()
        {
            _searchInstance?.StopSearch();
            IsSearching = false;
            UpdateStateIcon();
        }

        /// <summary>
        /// Delete the desktop icon and clean up
        /// </summary>
        private void OnDeleteIcon()
        {
            DebugLogger.Log(
                "SearchDesktopIconViewModel",
                $"DeleteIcon called for search {_searchId}, isSearching={IsSearching}"
            );

            // FIRST stop the search BEFORE clearing state!
            if (_searchInstance != null)
            {
                DebugLogger.Log(
                    "SearchDesktopIconViewModel",
                    "Stopping search instance without saving state"
                );
                _searchInstance.StopSearch(true); // Stop without saving state
            }

            // Remove search from manager if it exists
            if (!string.IsNullOrEmpty(_searchId) && _searchManager != null)
            {
                DebugLogger.Log(
                    "SearchDesktopIconViewModel",
                    $"Removing search {_searchId} from manager"
                );
                _searchManager.RemoveSearch(_searchId);
            }

            // Clear the saved search state
            DebugLogger.Log(
                "SearchDesktopIconViewModel",
                "Clearing saved search state from user profile"
            );
            _userProfileService.ClearSearchState();
            _userProfileService.FlushProfile();

            DebugLogger.Log("SearchDesktopIconViewModel", "Icon deletion complete");
        }

        #endregion

        #region IDisposable

        private bool _disposed;

        public void Dispose()
        {
            if (_disposed)
                return;

            // Unsubscribe from events
            if (_searchInstance != null)
            {
                _searchInstance.SearchStarted -= OnSearchStarted;
                _searchInstance.SearchCompleted -= OnSearchCompleted;
                _searchInstance.ProgressUpdated -= OnProgressUpdated;
            }

            _disposed = true;
        }

        #endregion
    }
}

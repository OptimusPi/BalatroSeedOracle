using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Media.Imaging;
using BalatroSeedOracle.Helpers;
using BalatroSeedOracle.Models;
using BalatroSeedOracle.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Motely.Filters;

namespace BalatroSeedOracle.ViewModels
{
    /// <summary>
    /// ViewModel for SearchWidget - minimized search progress indicator
    /// Shows filter icon, name, and progress bar for an active SearchInstance
    /// </summary>
    public partial class SearchWidgetViewModel : BaseWidgetViewModel, IDisposable
    {
        private bool _disposed;
        private readonly ActiveSearchContext _searchInstance;
        private readonly SpriteService _spriteService;
        private CancellationTokenSource? _saveDebounceToken;

        /// <summary>
        /// SearchInstance ID for identifying this widget
        /// </summary>
        public string SearchInstanceId => _searchInstance.SearchId ?? "";

        [ObservableProperty]
        private bool _isHovered;

        [ObservableProperty]
        private string _filterName = "Unknown";

        [ObservableProperty]
        private Bitmap? _filterIcon;

        [ObservableProperty]
        private double _progressBarWidth = 0.0; // 0-56 pixels (fits in 64px card)

        [ObservableProperty]
        private string _progressText = "0%";

        private readonly NotificationService? _notificationService;
        private readonly UserProfileService? _userProfileService;

        public SearchWidgetViewModel(
            ActiveSearchContext searchInstance,
            SpriteService spriteService,
            WidgetPositionService? widgetPositionService = null,
            NotificationService? notificationService = null,
            UserProfileService? userProfileService = null
        )
            : base(widgetPositionService)
        {
            _searchInstance =
                searchInstance ?? throw new ArgumentNullException(nameof(searchInstance));
            _spriteService =
                spriteService ?? throw new ArgumentNullException(nameof(spriteService));
            _notificationService = notificationService;
            _userProfileService = userProfileService;

            // Initialize from SearchInstance
            FilterName = _searchInstance.FilterName ?? "Search";
            LoadFilterIcon();

            // Subscribe to progress updates
            _searchInstance.ProgressUpdated += OnSearchProgressUpdated;

            // Subscribe to property changes for auto-save
            PropertyChanged += (s, e) =>
            {
                if (e.PropertyName == nameof(PositionX) || e.PropertyName == nameof(PositionY))
                {
                    _ = DebouncedSavePositionAsync();
                }
                else if (e.PropertyName == nameof(IsMinimized))
                {
                    SaveWidgetState(); // Immediate save for minimize
                }
            };
        }

        /// <summary>
        /// Load the filter's icon from the first Must clause item
        /// </summary>
        private void LoadFilterIcon()
        {
            try
            {
                var config = _searchInstance.GetFilterConfig();

                // Extract first Must clause that has a Value (skip And/Or groupings)
                if (config?.Must is not null && config.Must.Count > 0)
                {
                    foreach (var clause in config.Must)
                    {
                        var clauseValue = clause.GetValueName();
                        var clauseType = clause.GetTypeName();
                        if (
                            !string.IsNullOrEmpty(clauseValue)
                            && !string.IsNullOrEmpty(clauseType)
                        )
                        {
                            FilterIcon = clauseType.ToLowerInvariant() switch
                            {
                                "joker" or "commonjoker" or "uncommonjoker" or "rarejoker" or "legendaryjoker"
                                    => _spriteService.GetJokerImage(clauseValue) as Bitmap,
                                "tarotcard" => _spriteService.GetTarotImage(clauseValue) as Bitmap,
                                "planetcard" => _spriteService.GetPlanetCardImage(clauseValue) as Bitmap,
                                "spectralcard" => _spriteService.GetSpectralImage(clauseValue) as Bitmap,
                                "voucher" => _spriteService.GetVoucherImage(clauseValue) as Bitmap,
                                "tag" or "smallblindtag" or "bigblindtag"
                                    => _spriteService.GetTagImage(clauseValue) as Bitmap,
                                "boss" => _spriteService.GetBossImage(clauseValue) as Bitmap,
                                _ => _spriteService.GetItemImage(clauseValue, clauseType) as Bitmap,
                            };

                            if (FilterIcon is not null)
                            {
                                DebugLogger.Log(
                                    "SearchWidgetViewModel",
                                    $"Loaded icon for {clauseType}:{clauseValue}"
                                );
                                return;
                            }
                        }
                    }
                }

                // Fallback to default Joker icon
                FilterIcon = _spriteService.GetJokerImage("Joker") as Bitmap;
                DebugLogger.Log(
                    "SearchWidgetViewModel",
                    "Using default Joker icon (no Must clauses with Value found)"
                );
            }
            catch (Exception ex)
            {
                DebugLogger.LogError(
                    "SearchWidgetViewModel",
                    $"Failed to load filter icon: {ex.Message}"
                );
                FilterIcon = _spriteService.GetJokerImage("Joker") as Bitmap;
            }
        }

        /// <summary>
        /// Handle search progress updates
        /// </summary>
        private void OnSearchProgressUpdated(object? sender, SearchProgress progress)
        {
            Avalonia.Threading.Dispatcher.UIThread.Post(() =>
            {
                UpdateProgress(progress);
            });
        }

        /// <summary>
        /// Update progress bar and text from SearchProgress
        /// </summary>
        private void UpdateProgress(SearchProgress? progress)
        {
            if (progress is null)
            {
                ProgressBarWidth = 0.0;
                ProgressText = "0%";
                return;
            }

            // Calculate progress bar width (0-56 pixels for 64px card with margins)
            var progressFraction = progress.PercentComplete / 100.0;
            ProgressBarWidth = progressFraction * 56.0;

            // Format progress text
            ProgressText = $"{(int)progress.PercentComplete}%";

            // Show notification for completion
            if (progress.PercentComplete >= 100.0 && _notificationService is not null)
            {
                _notificationService.ShowSuccess(
                    "Search Complete",
                    $"Search finished: {FilterName}",
                    TimeSpan.FromSeconds(3)
                );
            }
        }

        /// <summary>
        /// Open SearchModal with this SearchInstance when widget is clicked
        /// </summary>
        [RelayCommand]
        private void OpenSearchModal()
        {
            try
            {
                DebugLogger.Log(
                    "SearchWidgetViewModel",
                    $"Opening SearchModal for search: {_searchInstance.SearchId}"
                );

                // Request to open SearchModal with this SearchInstance
                // This will be handled by the View (BalatroMainMenu)
                SearchModalOpenRequested?.Invoke(this, _searchInstance.SearchId);
            }
            catch (Exception ex)
            {
                DebugLogger.LogError(
                    "SearchWidgetViewModel",
                    $"Failed to open SearchModal: {ex.Message}"
                );
            }
        }

        /// <summary>
        /// Event raised when user clicks widget to reopen SearchModal
        /// </summary>
        public event EventHandler<string>? SearchModalOpenRequested;

        /// <summary>
        /// Debounced save for position changes (1 second delay).
        /// </summary>
        private async Task DebouncedSavePositionAsync()
        {
            _saveDebounceToken?.Cancel();
            _saveDebounceToken = new CancellationTokenSource();

            try
            {
                await Task.Delay(1000, _saveDebounceToken.Token).ConfigureAwait(false);
                SaveWidgetState();
            }
            catch (TaskCanceledException)
            {
                // Debounce was canceled
            }
        }

        /// <summary>
        /// Save widget state to UserProfile
        /// </summary>
        private void SaveWidgetState()
        {
            try
            {
                if (_userProfileService is null)
                {
                    DebugLogger.LogError(
                        "SearchWidgetViewModel",
                        "UserProfileService not available for save"
                    );
                    return;
                }

                var profile = _userProfileService.GetProfile();

                // Find existing saved widget or create new
                var saved = profile.SavedSearchWidgets.FirstOrDefault(w =>
                    w.SearchInstanceId == SearchInstanceId
                );
                if (saved is null)
                {
                    saved = new SavedSearchWidget();
                    profile.SavedSearchWidgets.Add(saved);
                }

                // Update state
                saved.SearchInstanceId = SearchInstanceId;
                saved.PositionX = PositionX;
                saved.PositionY = PositionY;
                saved.IsMinimized = IsMinimized;
                saved.ZIndexOffset = 0; // Will use BaseWidgetViewModel's _zIndexOffset if needed
                saved.LastUsed = DateTime.UtcNow;

                _userProfileService.SaveProfile(profile);

                DebugLogger.Log(
                    "SearchWidgetViewModel",
                    $"Saved widget state for {SearchInstanceId}"
                );
            }
            catch (Exception ex)
            {
                DebugLogger.LogError(
                    "SearchWidgetViewModel",
                    $"Failed to save widget state: {ex.Message}"
                );
            }
        }

        /// <summary>
        /// Override OnClosed to remove widget from saved widgets
        /// </summary>
        protected override void OnClosed()
        {
            try
            {
                // Remove from saved widgets
                if (_userProfileService is not null)
                {
                    var profile = _userProfileService.GetProfile();
                    profile.SavedSearchWidgets.RemoveAll(w =>
                        w.SearchInstanceId == SearchInstanceId
                    );
                    _userProfileService.SaveProfile(profile);

                    DebugLogger.Log(
                        "SearchWidgetViewModel",
                        $"Removed saved widget state for {SearchInstanceId}"
                    );
                }
            }
            catch (Exception ex)
            {
                DebugLogger.LogError(
                    "SearchWidgetViewModel",
                    $"Failed to remove saved widget: {ex.Message}"
                );
            }

            base.OnClosed();
        }

        public void Dispose()
        {
            if (_disposed)
                return;

            // Cancel any pending save operations
            _saveDebounceToken?.Cancel();
            _saveDebounceToken?.Dispose();

            // Unsubscribe from SearchInstance events
            _searchInstance.ProgressUpdated -= OnSearchProgressUpdated;

            _disposed = true;
        }
    }
}

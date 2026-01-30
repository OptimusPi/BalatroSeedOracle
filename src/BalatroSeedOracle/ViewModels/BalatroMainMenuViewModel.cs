using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using Avalonia.Controls;
using BalatroSeedOracle.Controls;
using BalatroSeedOracle.Helpers;
using BalatroSeedOracle.Models;
using BalatroSeedOracle.Services;
using BalatroSeedOracle.Views.Modals;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using IconPacks.Avalonia.Material;

namespace BalatroSeedOracle.ViewModels
{
    /// <summary>
    /// ViewModel for the BalatroMainMenu view
    /// Handles all state management, commands, and business logic
    /// </summary>
    public partial class BalatroMainMenuViewModel : ObservableObject
    {
        private readonly UserProfileService _userProfileService;
        private readonly Func<AnalyzeModalViewModel> _analyzeModalFactory;
        private readonly IAudioManager? _audioManager;
        private readonly EventFXService? _eventFXService;
        private readonly IApiHostService? _apiHostService;
        private readonly IPlatformServices? _platformServices;
        private Action<float, float, float, float>? _audioAnalysisHandler;

        /// <summary>
        /// ViewModel for the API Host widget - owned by parent, bound via XAML DataContext
        /// </summary>
        public ApiHostWidgetViewModel? ApiHostWidgetViewModel { get; }

        // Effect source tracking
        private int _shadowFlickerSource = 0;
        private int _spinSource = 0;
        private int _twirlSource = 0;
        private int _zoomThumpSource = 0;
        private int _colorSaturationSource = 0;

        /// <summary>
        /// Expose audio manager for widgets to access frequency data
        /// </summary>
        public IAudioManager? AudioManager => _audioManager;

        [ObservableProperty]
        private string _mainTitle = "";

        [ObservableProperty]
        private bool _isAnimating = true;

        [ObservableProperty]
        private PackIconMaterialKind _animationIcon = PackIconMaterialKind.Pause;

        [ObservableProperty]
        private bool _isMusicPlaying = true;

        [ObservableProperty]
        private double _volume = 70;

        [ObservableProperty]
        private string _volumePercentText = "70%";

        [ObservableProperty]
        private PackIconMaterialKind _musicIcon = PackIconMaterialKind.VolumeHigh;

        [ObservableProperty]
        private PackIconMaterialKind _searchWidgetsIcon = PackIconMaterialKind.Magnify;

        [ObservableProperty]
        private PackIconMaterialKind _toggleAllWidgetsIcon = PackIconMaterialKind.Widgets;

        [ObservableProperty]
        private string _muteButtonText = "MUTE";

        [ObservableProperty]
        private string _authorName = "Author";

        [ObservableProperty]
        private bool _authorEditMode = false;

        [ObservableProperty]
        private bool _authorDisplayMode = true;

        [ObservableProperty]
        private bool _isVolumePopupOpen = false;

        [ObservableProperty]
        [NotifyCanExecuteChangedFor(nameof(SeedSearchCommand))]
        [NotifyCanExecuteChangedFor(nameof(EditorCommand))]
        [NotifyCanExecuteChangedFor(nameof(AnalyzeCommand))]
        [NotifyCanExecuteChangedFor(nameof(ToolCommand))]
        private bool _isModalVisible = false;

        /// <summary>
        /// The currently active modal content (ViewModel-driven)
        /// </summary>
        [ObservableProperty]
        private object? _activeModal;

        partial void OnIsModalVisibleChanged(bool value)
        {
            if (!value)
            {
                ActiveModal = null;
            }
            else
            {
                // Close widget dock when modal opens
                IsWidgetDockVisible = false;
            }
        }

        partial void OnIsSearchWidgetsVisibleChanged(bool value)
        {
            SearchWidgetsIcon = value
                ? PackIconMaterialKind.Magnify
                : PackIconMaterialKind.MagnifyPlus;
            UpdateToggleAllWidgetsIcon();
        }

        private void UpdateToggleAllWidgetsIcon()
        {
            // Check if all enabled widgets are visible
            var allVisible =
                (IsMusicMixerWidgetVisible == IsMusicMixerWidgetEnabled)
                && (IsVisualizerWidgetVisible == IsVisualizerWidgetEnabled)
                && (IsTransitionDesignerWidgetVisible == IsTransitionDesignerWidgetEnabled)
                && (IsFertilizerWidgetVisible == IsFertilizerWidgetEnabled)
                && (IsHostApiWidgetVisible == IsHostApiWidgetEnabled)
                && (IsEventFXWidgetVisible == IsEventFXWidgetEnabled)
                && IsSearchWidgetsVisible;

            ToggleAllWidgetsIcon = allVisible
                ? PackIconMaterialKind.Widgets
                : PackIconMaterialKind.WidgetsOutline;
        }

        [ObservableProperty]
        private int _widgetCounter = 0;

        // Widget enabled state (from FeatureToggles - source of truth)
        [ObservableProperty]
        private bool _isMusicMixerWidgetEnabled = false;

        [ObservableProperty]
        private bool _isVisualizerWidgetEnabled = false;

        [ObservableProperty]
        private bool _isTransitionDesignerWidgetEnabled = false;

        [ObservableProperty]
        private bool _isFertilizerWidgetEnabled = false;

        [ObservableProperty]
        private bool _isHostApiWidgetEnabled = false;

        [ObservableProperty]
        private bool _isEventFXWidgetEnabled = false;

        // Widget visibility state (can be toggled via dock)
        [ObservableProperty]
        private bool _isVisualizerWidgetVisible = false;

        [ObservableProperty]
        private bool _isTransitionDesignerWidgetVisible = false;

        [ObservableProperty]
        private bool _isMusicMixerWidgetVisible = false;

        [ObservableProperty]
        private bool _isFertilizerWidgetVisible = false;

        [ObservableProperty]
        private bool _isHostApiWidgetVisible = false;

        [ObservableProperty]
        private bool _isEventFXWidgetVisible = false;

        [ObservableProperty]
        private bool _isSearchWidgetsVisible = true; // Search widgets visible by default

        [ObservableProperty]
        private bool _isVibeOutMode = false;

        // Widget dock popup visibility
        [ObservableProperty]
        private bool _isWidgetDockVisible = false;

        [RelayCommand]
        private void ToggleWidgetDock()
        {
            IsWidgetDockVisible = !IsWidgetDockVisible;
            PlayButtonClickSound();
        }

        private double _previousVolume = 70;

        /// <summary>
        /// Singleton SearchModalViewModel - injected, used by ModalHelper to create SearchModal.
        /// </summary>
        public SearchModalViewModel SearchModalViewModel { get; }

        /// <summary>
        /// Singleton FiltersModalViewModel - injected, used by ModalHelper to create FiltersModal.
        /// </summary>
        public FiltersModalViewModel FiltersModalViewModel { get; }

        /// <summary>
        /// CreditsModalViewModel - injected, used by ModalHelper to create CreditsModal (creator passes VM to View).
        /// </summary>
        public CreditsModalViewModel CreditsModalViewModel { get; }

        public BalatroMainMenuViewModel(
            UserProfileService userProfileService,
            SearchModalViewModel searchModalViewModel,
            FiltersModalViewModel filtersModalViewModel,
            CreditsModalViewModel creditsModalViewModel,
            Func<AnalyzeModalViewModel> analyzeModalFactory,
            IApiHostService? apiHostService = null,
            IAudioManager? audioManager = null,
            EventFXService? eventFXService = null,
            WidgetPositionService? widgetPositionService = null,
            IPlatformServices? platformServices = null
        )
        {
            // Store injected services
            _userProfileService = userProfileService;
            _analyzeModalFactory = analyzeModalFactory ?? throw new ArgumentNullException(nameof(analyzeModalFactory));
            SearchModalViewModel = searchModalViewModel;
            FiltersModalViewModel = filtersModalViewModel;
            CreditsModalViewModel = creditsModalViewModel;
            _apiHostService = apiHostService;
            _audioManager = audioManager;
            _eventFXService = eventFXService;
            _platformServices = platformServices;

            // Create child widget ViewModels (owned by parent, bound via XAML)
            if (_apiHostService != null)
            {
                ApiHostWidgetViewModel = new ApiHostWidgetViewModel(
                    _apiHostService,
                    widgetPositionService
                );
            }

            // Load settings
            LoadSettings();
        }

        /// <summary>Creates an AnalyzeModalViewModel via DI factory (no ServiceHelper). Used by View to show analyze modal.</summary>
        public AnalyzeModalViewModel CreateAnalyzeModalViewModel() => _analyzeModalFactory();

        /// <summary>Exposed so parent View can pass factory to child widgets (e.g. DayLatroWidget) that cannot get it from DI.</summary>
        public Func<AnalyzeModalViewModel> AnalyzeModalFactory => _analyzeModalFactory;

        partial void OnIsMusicMixerWidgetVisibleChanged(bool value)
        {
            UpdateToggleAllWidgetsIcon();
        }

        partial void OnIsVisualizerWidgetVisibleChanged(bool value)
        {
            UpdateToggleAllWidgetsIcon();
        }

        partial void OnIsTransitionDesignerWidgetVisibleChanged(bool value)
        {
            UpdateToggleAllWidgetsIcon();
        }

        partial void OnIsFertilizerWidgetVisibleChanged(bool value)
        {
            UpdateToggleAllWidgetsIcon();
        }

        partial void OnIsHostApiWidgetVisibleChanged(bool value)
        {
            UpdateToggleAllWidgetsIcon();
        }

        partial void OnIsEventFXWidgetVisibleChanged(bool value)
        {
            UpdateToggleAllWidgetsIcon();
        }

        partial void OnIsAnimatingChanged(bool value)
        {
            AnimationIcon = value ? PackIconMaterialKind.Pause : PackIconMaterialKind.Play;
            OnIsAnimatingChangedEvent?.Invoke(this, value);
        }

        partial void OnVolumeChanged(double value)
        {
            VolumePercentText = $"{(int)value}%";
            MusicIcon =
                value > 0 ? PackIconMaterialKind.VolumeHigh : PackIconMaterialKind.VolumeOff;
            MuteButtonText = value > 0 ? "MUTE" : "UNMUTE";
            IsMusicPlaying = value > 0;
            ApplyVolumeChange(value);

            // Save volume to user profile
            SaveVolumeToProfile();
        }

        partial void OnAuthorNameChanged(string value)
        {
            // Auto-save when changed
            if (!AuthorEditMode && !string.IsNullOrWhiteSpace(value))
            {
                _userProfileService.SetAuthorName(value.Trim());
            }
        }

        partial void OnAuthorEditModeChanged(bool value)
        {
            AuthorDisplayMode = !value;
        }

        #region Events

        /// <summary>
        /// Raised when a modal should be shown
        /// </summary>
        public event EventHandler<ModalRequestedEventArgs>? ModalRequested;

        /// <summary>
        /// Raised when the modal should be hidden
        /// </summary>
        public event EventHandler? HideModalRequested;

        /// <summary>
        /// Raised when animation state changes (for background control)
        /// </summary>
        public event EventHandler<bool>? OnIsAnimatingChangedEvent;

        /// <summary>
        /// Raised when volume popup visibility should change
        /// </summary>
        public event EventHandler<bool>? OnVolumePopupToggle;

        /// <summary>
        /// Raised when author edit mode is activated (for focus request)
        /// </summary>
        public event EventHandler? OnAuthorEditActivated;

        /// <summary>
        /// Raised when window state should change (for fullscreen vibe mode)
        /// </summary>
        public event EventHandler<bool>? WindowStateChangeRequested;

        #endregion

        #region Command Implementations

        [RelayCommand(CanExecute = nameof(CanOpenModal))]
        private void SeedSearch()
        {
            PlayButtonClickSound();
            _eventFXService?.TriggerEvent(EventFXType.SearchLaunchModal);
            try
            {
                var filterSelectionVM = new FilterSelectionModalViewModel(
                    enableSearch: true,
                    enableEdit: true,
                    enableCopy: false,
                    enableDelete: false,
                    enableAnalyze: false
                );

                filterSelectionVM.ModalCloseRequested += (s, e) =>
                {
                    var result = filterSelectionVM.Result;
                    if (result.Cancelled)
                    {
                        HideModal();
                        return;
                    }

                    if (result.Action == FilterAction.Search && !string.IsNullOrEmpty(result.FilterId))
                    {
                        // Transition to SearchModal
                        var searchVM = ServiceHelper.GetRequiredService<SearchModalViewModel>();
                        searchVM.MainMenu = null; // We'll handle navigation via ActiveModal
                        
                        // Load filter and show
                        var filtersDir = AppPaths.FiltersDir;
                        var configPath = Path.Combine(filtersDir, result.FilterId + ".jaml");
                        if (!File.Exists(configPath))
                            configPath = Path.Combine(filtersDir, result.FilterId + ".json");

                        _ = searchVM.LoadFilterAsync(configPath);
                        ActiveModal = searchVM;
                        MainTitle = "🎰 SEED SEARCH";
                    }
                };

                ActiveModal = filterSelectionVM;
                IsModalVisible = true;
                MainTitle = "🔍 SELECT FILTER";
            }
            catch (Exception ex)
            {
                IsModalVisible = false;
                ActiveModal = null;
                DebugLogger.LogError(
                    "BalatroMainMenuViewModel",
                    $"Failed to open search modal: {ex}"
                );
                ShowErrorModal(
                    $"Failed to open Search Modal:\n\n{ex.Message}\n\nPlease check the logs for details."
                );
            }
        }

        private bool CanOpenModal() => !IsModalVisible;

        /// <summary>
        /// Resolves the filter config file path for a filter ID (.jaml first, then .json).
        /// MVVM: path resolution and file checks belong in ViewModel, not View.
        /// </summary>
        public async Task<string?> GetFilterConfigPathAsync(string filterId)
        {
            if (string.IsNullOrWhiteSpace(filterId))
                return null;

            var isBrowser = _platformServices != null && !_platformServices.SupportsFileSystem;
            if (isBrowser)
            {
                var jamlPath = $"Filters/{filterId}.jaml";
                var jsonPath = $"Filters/{filterId}.json";
                if (await _platformServices!.FileExistsAsync(jamlPath).ConfigureAwait(false))
                    return jamlPath;
                if (await _platformServices.FileExistsAsync(jsonPath).ConfigureAwait(false))
                    return jsonPath;
                return null;
            }

            var filtersDir = AppPaths.FiltersDir;
            var jamlFull = Path.Combine(filtersDir, filterId + ".jaml");
            var jsonFull = Path.Combine(filtersDir, filterId + ".json");
            if (File.Exists(jamlFull))
                return jamlFull;
            if (File.Exists(jsonFull))
                return jsonFull;
            return null;
        }

        [RelayCommand(CanExecute = nameof(CanOpenModal))]
        private void Editor()
        {
            PlayButtonClickSound();
            _eventFXService?.TriggerEvent(EventFXType.DesignerLaunchModal);
            try
            {
                IsModalVisible = true;
                ModalRequested?.Invoke(this, new ModalRequestedEventArgs(ModalType.Filters));
            }
            catch (Exception ex)
            {
                IsModalVisible = false;
                DebugLogger.LogError(
                    "BalatroMainMenuViewModel",
                    $"Failed to open filters modal: {ex}"
                );
                ShowErrorModal(
                    $"Failed to open Designer Modal:\n\n{ex.Message}\n\nPlease check the logs for details."
                );
            }
        }

        [RelayCommand(CanExecute = nameof(CanOpenModal))]
        private void Analyze()
        {
            PlayButtonClickSound();
            _eventFXService?.TriggerEvent(EventFXType.AnalyzerLaunchModal);
            try
            {
                IsModalVisible = true;
                ModalRequested?.Invoke(this, new ModalRequestedEventArgs(ModalType.Analyze));
            }
            catch (Exception ex)
            {
                IsModalVisible = false;
                DebugLogger.LogError(
                    "BalatroMainMenuViewModel",
                    $"Failed to open analyze modal: {ex}"
                );
                ShowErrorModal(
                    $"Failed to open Analyzer Modal:\n\n{ex.Message}\n\nPlease check the logs for details."
                );
            }
        }

        [RelayCommand(CanExecute = nameof(CanOpenModal))]
        private void Tool()
        {
            PlayButtonClickSound();
            _eventFXService?.TriggerEvent(EventFXType.SettingsLaunchModal);
            try
            {
                IsModalVisible = true;
                ModalRequested?.Invoke(this, new ModalRequestedEventArgs(ModalType.Settings));
            }
            catch (Exception ex)
            {
                IsModalVisible = false;
                DebugLogger.LogError(
                    "BalatroMainMenuViewModel",
                    $"Failed to open settings modal: {ex}"
                );
                ShowErrorModal(
                    $"Failed to open Settings Modal:\n\n{ex.Message}\n\nPlease check the logs for details."
                );
            }
        }

        [RelayCommand]
        private void Settings()
        {
            PlayButtonClickSound();
            // Settings now opens SettingsModal via ModalRequested event
        }

        [RelayCommand]
        private void ToggleMusicMixerWidget()
        {
            if (!IsMusicMixerWidgetEnabled)
                return; // Can't toggle if not enabled
            PlayButtonClickSound();
            IsMusicMixerWidgetVisible = !IsMusicMixerWidgetVisible;
        }

        [RelayCommand]
        private void ToggleVisualizerWidget()
        {
            if (!IsVisualizerWidgetEnabled)
                return;
            PlayButtonClickSound();
            IsVisualizerWidgetVisible = !IsVisualizerWidgetVisible;
        }

        [RelayCommand]
        private void ToggleTransitionDesignerWidget()
        {
            if (!IsTransitionDesignerWidgetEnabled)
                return;
            PlayButtonClickSound();
            IsTransitionDesignerWidgetVisible = !IsTransitionDesignerWidgetVisible;
        }

        [RelayCommand]
        private void ToggleFertilizerWidget()
        {
            if (!IsFertilizerWidgetEnabled)
                return;
            PlayButtonClickSound();
            IsFertilizerWidgetVisible = !IsFertilizerWidgetVisible;
        }

        [RelayCommand]
        private void ToggleHostApiWidget()
        {
            if (!IsHostApiWidgetEnabled)
                return;
            PlayButtonClickSound();
            IsHostApiWidgetVisible = !IsHostApiWidgetVisible;
        }

        [RelayCommand]
        private void ToggleEventFXWidget()
        {
            if (!IsEventFXWidgetEnabled)
                return;
            PlayButtonClickSound();
            IsEventFXWidgetVisible = !IsEventFXWidgetVisible;
        }

        [RelayCommand]
        private void ToggleAllWidgets()
        {
            PlayButtonClickSound();

            // Check if any widgets are currently visible
            var anyVisible =
                IsMusicMixerWidgetVisible
                || IsVisualizerWidgetVisible
                || IsTransitionDesignerWidgetVisible
                || IsFertilizerWidgetVisible
                || IsHostApiWidgetVisible
                || IsEventFXWidgetVisible
                || IsSearchWidgetsVisible;

            // Toggle all widgets to the opposite state
            var newState = !anyVisible;

            IsMusicMixerWidgetVisible = newState && IsMusicMixerWidgetEnabled;
            IsVisualizerWidgetVisible = newState && IsVisualizerWidgetEnabled;
            IsTransitionDesignerWidgetVisible = newState && IsTransitionDesignerWidgetEnabled;
            IsFertilizerWidgetVisible = newState && IsFertilizerWidgetEnabled;
            IsHostApiWidgetVisible = newState && IsHostApiWidgetEnabled;
            IsEventFXWidgetVisible = newState && IsEventFXWidgetEnabled;
            IsSearchWidgetsVisible = newState;

            // Show/hide search widgets via window manager
            var widgetManager = Services.WidgetWindowManager.Instance;
            if (newState)
            {
                widgetManager.ShowAllWidgets();
            }
            else
            {
                widgetManager.HideAllWidgets();
            }
        }

        [RelayCommand]
        private void ToggleSearchWidgets()
        {
            PlayButtonClickSound();
            IsSearchWidgetsVisible = !IsSearchWidgetsVisible;

            // Show/hide all search widgets via window manager
            var widgetManager = Services.WidgetWindowManager.Instance;
            if (IsSearchWidgetsVisible)
            {
                widgetManager.ShowAllWidgets();
            }
            else
            {
                widgetManager.HideAllWidgets();
            }
        }

        [RelayCommand]
        private void AnimationToggle()
        {
            PlayButtonClickSound();
            IsAnimating = !IsAnimating;
        }

        [RelayCommand]
        private void MusicToggle()
        {
            PlayButtonClickSound();
            IsVolumePopupOpen = !IsVolumePopupOpen;
            OnVolumePopupToggle?.Invoke(this, IsVolumePopupOpen);
        }

        [RelayCommand]
        private void Mute()
        {
            PlayButtonClickSound();
            if (Volume > 0)
            {
                _previousVolume = Volume;
                Volume = 0;
            }
            else
            {
                Volume = _previousVolume > 0 ? _previousVolume : 70;
            }
        }

        [RelayCommand]
        private void ToggleVibeOutMode()
        {
            IsVibeOutMode = !IsVibeOutMode;

            // In vibe out mode: hide all widgets
            if (IsVibeOutMode)
            {
                IsWidgetDockVisible = false;
                IsSearchWidgetsVisible = false;

                // Hide search widgets via window manager
                var widgetManager = Services.WidgetWindowManager.Instance;
                widgetManager.HideAllWidgets();
            }
            else
            {
                IsSearchWidgetsVisible = true;

                // Show search widgets via window manager
                var widgetManager = Services.WidgetWindowManager.Instance;
                widgetManager.ShowAllWidgets();
            }

            DebugLogger.Log(
                "BalatroMainMenu",
                $"Vibe Out Mode: {(IsVibeOutMode ? "ON" : "OFF")}, Widgets hidden: {!IsWidgetDockVisible}"
            );

            // Request window state change (true = fullscreen, false = normal)
            WindowStateChangeRequested?.Invoke(this, IsVibeOutMode);
        }

        [RelayCommand]
        private void AuthorClick()
        {
            PlayButtonClickSound();
            _eventFXService?.TriggerEvent(EventFXType.AuthorLaunchEdit);
            AuthorEditMode = true;
            OnAuthorEditActivated?.Invoke(this, EventArgs.Empty);
        }

        [RelayCommand]
        private void SaveAuthor()
        {
            var newName = AuthorName?.Trim();
            if (!string.IsNullOrEmpty(newName))
            {
                _userProfileService.SetAuthorName(newName);
                AuthorName = newName;
                DebugLogger.Log("BalatroMainMenuViewModel", $"Author name updated to: {newName}");
            }
            AuthorEditMode = false;
        }

        [RelayCommand]
        private void CancelAuthorEdit()
        {
            // Restore original value
            AuthorName = _userProfileService.GetAuthorName();
            AuthorEditMode = false;
        }

        [RelayCommand]
        private void BuyBalatro()
        {
            try
            {
                var url = "https://playbalatro.com/";
                Process.Start(new ProcessStartInfo { FileName = url, UseShellExecute = true });
            }
            catch (Exception ex)
            {
                DebugLogger.LogError(
                    "BalatroMainMenuViewModel",
                    $"Error opening Balatro website: {ex.Message}"
                );
            }
        }

        #endregion

        #region Modal Management

        /// <summary>
        /// Shows a modal with the specified title
        /// </summary>
        public void ShowModal(string title, UserControl content)
        {
            MainTitle = title;
            IsModalVisible = true;
            ModalRequested?.Invoke(this, new ModalRequestedEventArgs(content, title));
        }

        /// <summary>
        /// Hides the current modal
        /// </summary>
        public void HideModal()
        {
            MainTitle = "";
            IsModalVisible = false;
            HideModalRequested?.Invoke(this, EventArgs.Empty);
        }

        /// <summary>
        /// Shows an error modal
        /// </summary>
        private void ShowErrorModal(string errorMessage)
        {
            var errorModal = new StandardModal("ERROR");
            var errorText = new TextBlock
            {
                Text = errorMessage,
                Margin = new Avalonia.Thickness(20),
                TextWrapping = Avalonia.Media.TextWrapping.Wrap,
            };
            errorModal.SetContent(errorText);
            errorModal.BackClicked += (s, ev) => HideModal();
            ShowModal("ERROR", errorModal);
        }

        #endregion

        #region Settings Management

        /// <summary>
        /// Loads settings from user profile
        /// </summary>
        private void LoadSettings()
        {
            try
            {
                // Load author name
                AuthorName = _userProfileService.GetAuthorName();

                // Load visualizer settings
                var profile = _userProfileService.GetProfile();

                // Load volume and mute state
                Volume = profile.MusicVolume * 100; // Convert 0-1 to 0-100
                if (profile.IsMusicMuted)
                {
                    _previousVolume = Volume;
                    Volume = 0;
                }

                // Load feature toggles - these control which widgets are enabled (exist)
                // Combine user preferences with platform capabilities
                var toggles = profile.FeatureToggles;
                IsMusicMixerWidgetEnabled = (_platformServices?.SupportsAudioWidgets ?? true) && (toggles?.ShowMusicMixer ?? false);
                IsVisualizerWidgetEnabled = (_platformServices?.SupportsAudioWidgets ?? true) && (toggles?.ShowVisualizer ?? false);
                IsTransitionDesignerWidgetEnabled = (_platformServices?.SupportsTransitionDesigner ?? true) && (toggles?.ShowTransitionDesigner ?? false);
                IsFertilizerWidgetEnabled = toggles?.ShowFertilizer ?? false;
                IsHostApiWidgetEnabled = (_platformServices?.SupportsApiHostWidget ?? true) && (toggles?.ShowHostServer ?? false);
                IsEventFXWidgetEnabled = toggles?.ShowEventFX ?? false;

                // Initialize visibility to match enabled state
                IsMusicMixerWidgetVisible = IsMusicMixerWidgetEnabled;
                IsVisualizerWidgetVisible = IsVisualizerWidgetEnabled;
                IsTransitionDesignerWidgetVisible = IsTransitionDesignerWidgetEnabled;
                IsFertilizerWidgetVisible = IsFertilizerWidgetEnabled;
                IsHostApiWidgetVisible = IsHostApiWidgetEnabled;
                IsEventFXWidgetVisible = IsEventFXWidgetEnabled;
                // Search widgets are always enabled but visibility is controlled separately
                IsSearchWidgetsVisible = true;

                DebugLogger.Log(
                    "BalatroMainMenuViewModel",
                    $"Settings loaded: Volume={profile.MusicVolume}, Muted={profile.IsMusicMuted}, "
                        + $"Features: Mixer={IsMusicMixerWidgetVisible}, Viz={IsVisualizerWidgetVisible}, Trans={IsTransitionDesignerWidgetVisible}, Fert={IsFertilizerWidgetVisible}, Host={IsHostApiWidgetVisible}, EventFX={IsEventFXWidgetVisible}"
                );
            }
            catch (Exception ex)
            {
                DebugLogger.LogError(
                    "BalatroMainMenuViewModel",
                    $"Error loading settings: {ex.Message}"
                );
            }
        }

        /// <summary>
        /// Refreshes feature toggle visibility from profile. Call this when settings change.
        /// </summary>
        public void RefreshFeatureToggles()
        {
            var profile = _userProfileService.GetProfile();
            var toggles = profile.FeatureToggles;

            // Update enabled state (source of truth)
            // Combine user preferences with platform capabilities
            IsMusicMixerWidgetEnabled = (_platformServices?.SupportsAudioWidgets ?? true) && (toggles?.ShowMusicMixer ?? false);
            IsVisualizerWidgetEnabled = (_platformServices?.SupportsAudioWidgets ?? true) && (toggles?.ShowVisualizer ?? false);
            IsTransitionDesignerWidgetEnabled = (_platformServices?.SupportsTransitionDesigner ?? true) && (toggles?.ShowTransitionDesigner ?? false);
            IsFertilizerWidgetEnabled = toggles?.ShowFertilizer ?? false;
            IsHostApiWidgetEnabled = (_platformServices?.SupportsApiHostWidget ?? true) && (toggles?.ShowHostServer ?? false);
            IsEventFXWidgetEnabled = toggles?.ShowEventFX ?? false;

            // If a widget is disabled, hide it. If enabled, keep current visibility.
            if (!IsMusicMixerWidgetEnabled)
                IsMusicMixerWidgetVisible = false;
            if (!IsVisualizerWidgetEnabled)
                IsVisualizerWidgetVisible = false;
            if (!IsTransitionDesignerWidgetEnabled)
                IsTransitionDesignerWidgetVisible = false;
            if (!IsFertilizerWidgetEnabled)
                IsFertilizerWidgetVisible = false;
            if (!IsHostApiWidgetEnabled)
                IsHostApiWidgetVisible = false;
            if (!IsEventFXWidgetEnabled)
                IsEventFXWidgetVisible = false;
            DebugLogger.Log(
                "BalatroMainMenuViewModel",
                $"Feature toggles refreshed: Mixer={IsMusicMixerWidgetVisible}, Viz={IsVisualizerWidgetVisible}, Trans={IsTransitionDesignerWidgetVisible}, Fert={IsFertilizerWidgetVisible}, Host={IsHostApiWidgetVisible}, EventFX={IsEventFXWidgetVisible}"
            );
        }

        private void SaveVolumeToProfile()
        {
            try
            {
                var profile = _userProfileService.GetProfile();
                profile.MusicVolume = (float)(Volume / 100.0); // Convert 0-100 to 0-1
                profile.IsMusicMuted = Volume == 0;
                _userProfileService.SaveProfile();
                DebugLogger.Log(
                    "BalatroMainMenuViewModel",
                    $"Volume saved: {profile.MusicVolume}, Muted: {profile.IsMusicMuted}"
                );
            }
            catch (Exception ex)
            {
                DebugLogger.LogError(
                    "BalatroMainMenuViewModel",
                    $"Error saving volume: {ex.Message}"
                );
            }
        }

        /// <summary>
        /// Loads and applies visualizer settings
        /// </summary>
        public void LoadAndApplyVisualizerSettings(BalatroShaderBackground? shader)
        {
            if (shader == null)
                return;

            try
            {
                var profile = _userProfileService.GetProfile();
                var settings = profile.VisualizerSettings ?? new VisualizerSettings();

                if (profile.VisualizerSettings == null)
                {
                    profile.VisualizerSettings = settings;
                    _userProfileService.SaveProfile(profile);
                }

                // Color selections (themes removed - using direct color control)
                var mainColorIndex = Math.Clamp(settings.MainColor, 0, 8);
                ApplyMainColor(shader, mainColorIndex);

                var accentColorIndex = Math.Clamp(settings.AccentColor, 0, 8);
                ApplyAccentColor(shader, accentColorIndex);

                // Shader parameter preferences
                var timeSpeed = Math.Clamp(settings.TimeSpeed, 0f, 3f);
                ApplyTimeSpeed(shader, timeSpeed);

                // Audio source bindings for effects
                ApplyShadowFlickerSource(shader, Math.Clamp(settings.ShadowFlickerSource, 0, 4));
                ApplySpinSource(shader, Math.Clamp(settings.SpinSource, 0, 4));
                ApplyTwirlSource(shader, Math.Clamp(settings.TwirlSource, 0, 4));
                ApplyZoomThumpSource(shader, Math.Clamp(settings.ZoomThumpSource, 0, 4));
                ApplyColorSaturationSource(
                    shader,
                    Math.Clamp(settings.ColorSaturationSource, 0, 4)
                );

                // Per-track volume balancing for SoundFlow audio stems
                if (_audioManager != null)
                {
                    SetTrackVolume("Drums1", Math.Clamp(settings.Drums1Volume, 0f, 1f));
                    SetTrackVolume("Drums2", Math.Clamp(settings.Drums2Volume, 0f, 1f));
                    SetTrackVolume("Bass1", Math.Clamp(settings.Bass1Volume, 0f, 1f));
                    SetTrackVolume("Bass2", Math.Clamp(settings.Bass2Volume, 0f, 1f));
                    SetTrackVolume("Chords1", Math.Clamp(settings.Chords1Volume, 0f, 1f));
                    SetTrackVolume("Chords2", Math.Clamp(settings.Chords2Volume, 0f, 1f));
                    SetTrackVolume("Melody1", Math.Clamp(settings.Melody1Volume, 0f, 1f));
                    SetTrackVolume("Melody2", Math.Clamp(settings.Melody2Volume, 0f, 1f));
                }

                DebugLogger.Log(
                    "BalatroMainMenuViewModel",
                    $"Visualizer settings applied (MainColor={mainColorIndex}, AccentColor={accentColorIndex}, TimeSpeed={timeSpeed:F2})"
                );
            }
            catch (Exception ex)
            {
                DebugLogger.LogError(
                    "BalatroMainMenuViewModel",
                    $"Error loading visualizer settings: {ex.Message}"
                );
            }
        }

        /// <summary>
        /// Checks for resumable search and restores if needed
        /// </summary>
        public void CheckAndRestoreSearchIcon(Action<string, string?> showIconCallback)
        {
            try
            {
                if (_userProfileService.GetSearchState() is { } resumeState)
                {
                    // User will close searches they don't want - no need for auto-cleanup!
                    var timeSinceSearch = DateTime.UtcNow - resumeState.LastActiveTime;
                    DebugLogger.Log(
                        "BalatroMainMenuViewModel",
                        $"Found resumable search state from {timeSinceSearch.TotalMinutes:F0} minutes ago"
                    );

                    if (
                        !string.IsNullOrEmpty(resumeState.ConfigPath)
                        && File.Exists(resumeState.ConfigPath)
                    )
                    {
                        var placeholderSearchId = Guid.NewGuid().ToString();
                        showIconCallback?.Invoke(placeholderSearchId, resumeState.ConfigPath);

                        DebugLogger.Log(
                            "BalatroMainMenuViewModel",
                            $"Restored desktop icon for search (not started yet): {resumeState.ConfigPath}"
                        );
                    }
                    else
                    {
                        DebugLogger.Log(
                            "BalatroMainMenuViewModel",
                            $"Skipping desktop icon for resumable search - invalid config path: {resumeState.ConfigPath}"
                        );
                        _userProfileService.ClearSearchState();
                    }
                }
            }
            catch (Exception ex)
            {
                DebugLogger.LogError(
                    "BalatroMainMenuViewModel",
                    $"Error checking for resumable search: {ex.Message}"
                );
            }
        }

        #endregion

        #region Audio Management

        /// <summary>
        /// Plays button click sound
        /// </summary>
        private void PlayButtonClickSound()
        {
            try
            {
                var audioManager = ServiceHelper.GetService<IAudioManager>();
                audioManager?.PlaySfx("button", 1.0f);
            }
            catch (Exception ex)
            {
                DebugLogger.LogError(
                    "BalatroMainMenuViewModel",
                    $"Failed to play button click sound: {ex.Message}"
                );
            }
        }

        /// <summary>
        /// Applies volume change to audio manager
        /// </summary>
        private void ApplyVolumeChange(double volume)
        {
            try
            {
                float volumeFloat = (float)(volume / 100.0);
                if (_audioManager != null)
                {
                    _audioManager.MasterVolume = volumeFloat;
                }
            }
            catch (Exception ex)
            {
                DebugLogger.LogError(
                    "BalatroMainMenuViewModel",
                    $"Failed to apply volume: {ex.Message}"
                );
            }
        }

        /// <summary>
        /// Set volume of a specific audio track (only works with IAudioManager)
        /// </summary>
        public void SetTrackVolume(string trackName, float volume)
        {
            try
            {
                _audioManager?.SetTrackVolume(trackName, volume);
            }
            catch (Exception ex)
            {
                DebugLogger.LogError(
                    "BalatroMainMenuViewModel",
                    $"Failed to set track volume: {ex.Message}"
                );
            }
        }

        /// <summary>
        /// Wire up audio analysis to shader (called by View when shader is ready)
        /// </summary>
        public void WireAudioAnalysisToShader(BalatroShaderBackground shader)
        {
            try
            {
                if (_audioManager != null)
                {
                    _audioAnalysisHandler = (bass, mid, treble, peak) => {
                        // Audio reactivity handled by effect binding system
                    };

                    _audioManager.AudioAnalysisUpdated += _audioAnalysisHandler;
                    DebugLogger.Log(
                        "ViewModel",
                        "✅ Audio analysis handler connected (awaiting effect binding system)"
                    );
                }
            }
            catch (Exception ex)
            {
                DebugLogger.LogError(
                    "BalatroMainMenuViewModel",
                    $"Failed to wire audio analysis: {ex.Message}"
                );
            }
        }

        #endregion

        #region Shader Management

        /// <summary>
        /// Apply main color to shader
        /// </summary>
        public void ApplyMainColor(BalatroShaderBackground? shader, int colorIndex)
        {
            var color = IndexToSKColor(colorIndex);
            shader?.SetMainColor(color);
        }

        public void ApplyMainColor(BalatroShaderBackground? shader, SkiaSharp.SKColor color)
        {
            shader?.SetMainColor(color);
        }

        /// <summary>
        /// Apply accent color to shader
        /// </summary>
        public void ApplyAccentColor(BalatroShaderBackground? shader, int colorIndex)
        {
            var color = IndexToSKColor(colorIndex);
            shader?.SetAccentColor(color);
        }

        public void ApplyAccentColor(BalatroShaderBackground? shader, SkiaSharp.SKColor color)
        {
            shader?.SetAccentColor(color);
        }

        /// <summary>
        /// Convert color dropdown index to SKColor
        /// </summary>
        private SkiaSharp.SKColor IndexToSKColor(int index)
        {
            return index switch
            {
                0 => new SkiaSharp.SKColor(255, 76, 64), // Red (Balatro Red)
                1 => new SkiaSharp.SKColor(255, 165, 0), // Orange
                2 => new SkiaSharp.SKColor(255, 215, 0), // Yellow (Gold)
                3 => new SkiaSharp.SKColor(0, 255, 127), // Green (Spring Green)
                4 => new SkiaSharp.SKColor(0, 147, 255), // Blue (Balatro Blue)
                5 => new SkiaSharp.SKColor(147, 51, 234), // Purple
                6 => new SkiaSharp.SKColor(139, 69, 19), // Brown (Saddle Brown)
                7 => new SkiaSharp.SKColor(255, 255, 255), // White
                8 => new SkiaSharp.SKColor(30, 43, 45), // None (Dark background)
                _ => new SkiaSharp.SKColor(255, 76, 64), // Default to Red
            };
        }

        /// <summary>
        /// Apply time speed to shader (animation speed multiplier)
        /// </summary>
        public void ApplyTimeSpeed(BalatroShaderBackground? shader, float speed)
        {
            shader?.SetTime(speed); // Now controls animation speed
        }

        /// <summary>
        /// Apply contrast to shader
        /// </summary>
        public void ApplyShaderContrast(BalatroShaderBackground? shader, float contrast)
        {
            shader?.SetContrast(contrast);
        }

        /// <summary>
        /// Apply spin amount to shader
        /// </summary>
        public void ApplyShaderSpinAmount(BalatroShaderBackground? shader, float spinAmount)
        {
            shader?.SetSpinAmount(spinAmount);
        }

        /// <summary>
        /// Apply zoom scale to shader
        /// </summary>
        public void ApplyShaderZoomPunch(BalatroShaderBackground? shader, float zoom)
        {
            shader?.SetZoomScale(zoom);
        }

        /// <summary>
        /// Apply saturation to shader
        /// </summary>
        public void ApplyShaderMelodySaturation(BalatroShaderBackground? shader, float saturation)
        {
            shader?.SetSaturationAmount(saturation);
        }

        public void ApplyShaderPixelSize(BalatroShaderBackground? shader, float pixelSize)
        {
            shader?.SetPixelSize(pixelSize);
        }

        public void ApplyShaderSpinEase(BalatroShaderBackground? shader, float spinEase)
        {
            shader?.SetSpinEase(spinEase);
        }

        public void ApplyShaderLoopCount(BalatroShaderBackground? shader, float loopCount)
        {
            shader?.SetLoopCount(loopCount);
        }

        /// <summary>
        /// Apply shadow flicker source to shader
        /// </summary>
        public void ApplyShadowFlickerSource(BalatroShaderBackground? shader, int sourceIndex)
        {
            // Store for update loop to use
            _shadowFlickerSource = sourceIndex;
        }

        /// <summary>
        /// Apply spin source to shader
        /// </summary>
        public void ApplySpinSource(BalatroShaderBackground? shader, int sourceIndex)
        {
            _spinSource = sourceIndex;
        }

        /// <summary>
        /// Apply twirl source to shader
        /// </summary>
        public void ApplyTwirlSource(BalatroShaderBackground? shader, int sourceIndex)
        {
            _twirlSource = sourceIndex;
        }

        /// <summary>
        /// Apply zoom thump source to shader
        /// </summary>
        public void ApplyZoomThumpSource(BalatroShaderBackground? shader, int sourceIndex)
        {
            _zoomThumpSource = sourceIndex;
        }

        /// <summary>
        /// Apply color saturation source to shader
        /// </summary>
        public void ApplyColorSaturationSource(BalatroShaderBackground? shader, int sourceIndex)
        {
            _colorSaturationSource = sourceIndex;
        }

        #endregion
    }

    #region Event Args

    /// <summary>
    /// Event args for modal requests
    /// </summary>
    public class ModalRequestedEventArgs : EventArgs
    {
        public ModalType ModalType { get; }
        public UserControl? CustomContent { get; }
        public string? CustomTitle { get; }

        public ModalRequestedEventArgs(ModalType modalType)
        {
            ModalType = modalType;
        }

        public ModalRequestedEventArgs(UserControl customContent, string customTitle)
        {
            ModalType = ModalType.Custom;
            CustomContent = customContent;
            CustomTitle = customTitle;
        }
    }

    /// <summary>
    /// Types of modals
    /// </summary>
    public enum ModalType
    {
        Search,
        Filters,
        Analyze,
        Tools,
        Settings,
        Custom,
    }

    #endregion
}

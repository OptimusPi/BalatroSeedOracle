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
        private readonly Func<FilterSelectionModalViewModel>? _filterSelectionFactory;
        private readonly IAudioManager? _audioManager;
        private readonly EventFXService? _eventFXService;
        private readonly IPlatformServices? _platformServices;
        private readonly IModalHost? _modalHost;
        private Action<float, float, float, float>? _audioAnalysisHandler;

        // Effect source tracking
        private int _shadowFlickerSource = 0;
        private int _spinSource = 0;
        private int _twirlSource = 0;
        private int _zoomThumpSource = 0;
        private int _colorSaturationSource = 0;

        // --- Music reactivity bridge ---
        // The shader the audio analysis drives, plus resting baselines captured at
        // wire-up so reactivity modulates *around* the configured values instead of
        // overwriting them, and smoothed deltas so the background pulses (not strobes).
        private BalatroShaderBackground? _reactiveShader;
        private float _baseZoom;
        private float _baseContrast;
        private float _baseSpinAmount;
        private float _baseSpinTime;
        private float _baseSaturation;
        private float _smZoom;
        private float _smContrast;
        private float _smSpinAmount;
        private float _smSpinTime;
        private float _smSaturation;

        /// <summary>
        /// Expose audio manager for widgets to access frequency data
        /// </summary>
        public IAudioManager? AudioManager => _audioManager;

        /// <summary>
        /// Active search "desktop icons" — searches that keep running after their
        /// modal was closed. Click one to reconnect without losing your spot.
        /// </summary>
        public System.Collections.ObjectModel.ObservableCollection<SearchWidgetIconViewModel> ActiveSearchWidgets { get; } = new();

        [ObservableProperty]
        private string _mainTitle = "";

        [ObservableProperty]
        private bool _isAnimating = true;

        [ObservableProperty]
        private string _animationIcon = "Pause";

        [ObservableProperty]
        private bool _isMusicPlaying = true;

        [ObservableProperty]
        private double _volume = 70;

        [ObservableProperty]
        private string _volumePercentText = "70%";

        [ObservableProperty]
        private string _musicIcon = "VolumeHigh";

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
        }

        [ObservableProperty]
        private bool _isVibeOutMode = false;

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
            IAudioManager? audioManager = null,
            EventFXService? eventFXService = null,
            IModalHost? modalHost = null,
            IPlatformServices? platformServices = null,
            Func<FilterSelectionModalViewModel>? filterSelectionFactory = null
        )
        {
            // Store injected services
            _userProfileService = userProfileService;
            _analyzeModalFactory =
                analyzeModalFactory ?? throw new ArgumentNullException(nameof(analyzeModalFactory));
            _filterSelectionFactory = filterSelectionFactory;
            SearchModalViewModel = searchModalViewModel;
            FiltersModalViewModel = filtersModalViewModel;
            CreditsModalViewModel = creditsModalViewModel;
            _audioManager = audioManager;
            _eventFXService = eventFXService;
            _platformServices = platformServices;
            _modalHost = modalHost;

            // Load settings
            LoadSettings();
        }

        /// <summary>Creates an AnalyzeModalViewModel via DI factory (no ServiceHelper). Used by View to show analyze modal.</summary>
        public AnalyzeModalViewModel CreateAnalyzeModalViewModel() => _analyzeModalFactory();

        /// <summary>Exposed so parent View can pass factory to child widgets (e.g. DayLatroWidget) that cannot get it from DI.</summary>
        public Func<AnalyzeModalViewModel> AnalyzeModalFactory => _analyzeModalFactory;

        partial void OnIsAnimatingChanged(bool value)
        {
            AnimationIcon = value ? "Pause" : "Play";
            OnIsAnimatingChangedEvent?.Invoke(this, value);
        }

        partial void OnVolumeChanged(double value)
        {
            VolumePercentText = $"{(int)value}%";
            MusicIcon = value > 0 ? "VolumeHigh" : "VolumeOff";
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
                IsModalVisible = true;
                ModalRequested?.Invoke(this, new ModalRequestedEventArgs(ModalType.Search));
            }
            catch (Exception ex)
            {
                HandleModalOpenError("search", "Search", ex);
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

            var isBrowser = _platformServices is not null && !_platformServices.SupportsFileSystem;
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
                HandleModalOpenError("filters", "Designer", ex);
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
                HandleModalOpenError("analyze", "Analyzer", ex);
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
                HandleModalOpenError("settings", "Settings", ex);
            }
        }

        [RelayCommand]
        private void Settings()
        {
            PlayButtonClickSound();
            // Settings now opens SettingsModal via ModalRequested event
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

            DebugLogger.Log(
                "BalatroMainMenu",
                $"Vibe Out Mode: {(IsVibeOutMode ? "ON" : "OFF")}"
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

        // Common error path for the modal-launching commands. logName is the
        // lowercase name used in DebugLogger; displayName is the user-facing
        // name shown in the error modal title text.
        private void HandleModalOpenError(string logName, string displayName, Exception ex)
        {
            IsModalVisible = false;
            DebugLogger.LogError("BalatroMainMenuViewModel", $"Failed to open {logName} modal: {ex}");
            ShowErrorModal(
                $"Failed to open {displayName} Modal:\n\n{ex.Message}\n\nPlease check the logs for details."
            );
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

                DebugLogger.Log(
                    "BalatroMainMenuViewModel",
                    $"Settings loaded: Volume={profile.MusicVolume}, Muted={profile.IsMusicMuted}"
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
            if (shader is null)
                return;

            try
            {
                var profile = _userProfileService.GetProfile();
                var settings = profile.VisualizerSettings ?? new VisualizerSettings();

                if (profile.VisualizerSettings is null)
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
                if (_audioManager is not null)
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
                _audioManager?.PlaySfx("button", 1.0f);
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
                if (_audioManager is not null)
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
                if (_audioManager is null)
                    return;

                _reactiveShader = shader;

                // Capture resting baselines so reactivity adds motion on top of the
                // user's/theme's configured values and decays back to them when quiet.
                _baseZoom = shader.GetZoomScale();
                _baseContrast = shader.GetContrast();
                _baseSpinAmount = shader.GetSpinAmount();
                _baseSpinTime = shader.GetSpinTimeSpeed();
                _baseSaturation = shader.GetSaturationAmount();

                // Re-wiring must never double-subscribe.
                if (_audioAnalysisHandler is not null)
                    _audioManager.AudioAnalysisUpdated -= _audioAnalysisHandler;

                // Event payload is (BassIntensity, ChordsIntensity, MelodyIntensity, peak);
                // Drums is read live from the manager inside the handler.
                _audioAnalysisHandler = (bass, chords, melody, _) =>
                    ApplyAudioReactivity(bass, chords, melody);
                _audioManager.AudioAnalysisUpdated += _audioAnalysisHandler;

                DebugLogger.Log(
                    "ViewModel",
                    "Audio reactivity bridge connected (FFT stems -> shader)"
                );
            }
            catch (Exception ex)
            {
                DebugLogger.LogError(
                    "BalatroMainMenuViewModel",
                    $"Failed to wire audio analysis: {ex.Message}"
                );
            }
        }

        /// <summary>
        /// Detach the audio reactivity bridge (called by View on teardown).
        /// </summary>
        public void UnwireAudioAnalysisFromShader()
        {
            try
            {
                if (_audioManager is not null && _audioAnalysisHandler is not null)
                {
                    _audioManager.AudioAnalysisUpdated -= _audioAnalysisHandler;
                }
            }
            catch (Exception ex)
            {
                DebugLogger.LogError(
                    "BalatroMainMenuViewModel",
                    $"Failed to unwire audio analysis: {ex.Message}"
                );
            }
            finally
            {
                _audioAnalysisHandler = null;
                _reactiveShader = null;
            }
        }

        /// <summary>
        /// Routes live FFT stem levels onto shader effects per the user's visualizer
        /// settings. Source index: 0=None, 1=Drums, 2=Bass, 3=Chords, 4=Melody.
        /// Runs on the audio analysis thread; only writes plain shader uniform fields.
        /// </summary>
        private void ApplyAudioReactivity(float bass, float chords, float melody)
        {
            var shader = _reactiveShader;
            var audio = _audioManager;
            if (shader is null || audio is null)
                return;

            try
            {
                var vibe = _userProfileService.GetProfile().VisualizerSettings;
                float globalIntensity = Math.Clamp(vibe.AudioIntensity, 0f, 2f);
                float drums = audio.DrumsIntensity;

                float Level(int source) =>
                    source switch
                    {
                        1 => drums,
                        2 => bass,
                        3 => chords,
                        4 => melody,
                        _ => 0f,
                    };

                static float Pct(float v) => Math.Clamp(v, 0f, 100f) / 100f;

                // Target additive deltas per shader channel.
                float zoomTarget =
                    (
                        Level(vibe.ZoomThumpSource) * Pct(vibe.ZoomThumpIntensity) * 0.6f
                        + Level(vibe.BeatPulseSource) * Pct(vibe.BeatPulseIntensity) * 0.4f
                    ) * globalIntensity;
                float contrastTarget =
                    Level(vibe.ShadowFlickerSource) * Pct(vibe.ShadowFlickerIntensity) * 1.5f * globalIntensity;
                float spinAmountTarget =
                    Level(vibe.SpinSource) * Pct(vibe.SpinIntensity) * 0.5f * globalIntensity;
                float spinTimeTarget =
                    Level(vibe.TwirlSource) * Pct(vibe.TwirlIntensity) * 2.0f * globalIntensity;
                float saturationTarget =
                    Level(vibe.ColorSaturationSource) * Pct(vibe.ColorSaturationIntensity) * 0.8f * globalIntensity;

                // Smooth (attack/decay) so the background pulses instead of strobing.
                const float smooth = 0.35f;
                _smZoom += (zoomTarget - _smZoom) * smooth;
                _smContrast += (contrastTarget - _smContrast) * smooth;
                _smSpinAmount += (spinAmountTarget - _smSpinAmount) * smooth;
                _smSpinTime += (spinTimeTarget - _smSpinTime) * smooth;
                _smSaturation += (saturationTarget - _smSaturation) * smooth;

                shader.SetZoomScale(_baseZoom + _smZoom);
                shader.SetContrast(_baseContrast + _smContrast);
                shader.SetSpinAmount(_baseSpinAmount + _smSpinAmount);
                shader.SetSpinTime(_baseSpinTime + _smSpinTime);
                shader.SetSaturationAmount(_baseSaturation + _smSaturation);
            }
            catch
            {
                // A reactive frame must never take down audio playback.
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

        #region Desktop Icon Management

        /// <summary>
        /// Adds a search "desktop icon" to the main menu for a search that keeps
        /// running after its modal was closed. Clicking it reconnects to the
        /// running instance (or reopens the filter if the search isn't started yet)
        /// so the user never loses their spot.
        /// </summary>
        public void ShowSearchDesktopIcon(string searchId, string? configPath = null)
        {
            try
            {
                // Dedupe: one icon per search
                foreach (var existing in ActiveSearchWidgets)
                {
                    if (existing.SearchId == searchId)
                        return;
                }

                var searchManager = ServiceHelper.GetService<SearchManager>();
                var context = searchManager?.GetSearch(searchId);

                var icon = new SearchWidgetIconViewModel(
                    searchId,
                    configPath,
                    context,
                    OnSearchWidgetIconClicked
                );
                ActiveSearchWidgets.Add(icon);

                DebugLogger.Log(
                    "BalatroMainMenuViewModel",
                    $"Search desktop icon created for searchId: {searchId} (live instance: {context != null})"
                );
            }
            catch (Exception ex)
            {
                DebugLogger.LogError(
                    "BalatroMainMenuViewModel",
                    $"Failed to create search desktop icon: {ex.Message}"
                );
            }
        }

        /// <summary>
        /// Removes a search desktop icon by search ID
        /// </summary>
        public void RemoveSearchDesktopIcon(string searchId)
        {
            try
            {
                for (int i = ActiveSearchWidgets.Count - 1; i >= 0; i--)
                {
                    if (ActiveSearchWidgets[i].SearchId == searchId)
                    {
                        ActiveSearchWidgets[i].Dispose();
                        ActiveSearchWidgets.RemoveAt(i);
                        DebugLogger.Log(
                            "BalatroMainMenuViewModel",
                            $"Search desktop icon removed for searchId: {searchId}"
                        );
                    }
                }
            }
            catch (Exception ex)
            {
                DebugLogger.LogError(
                    "BalatroMainMenuViewModel",
                    $"Failed to remove search desktop icon: {ex.Message}"
                );
            }
        }

        /// <summary>
        /// Handles search widget icon click - removes icon and shows appropriate modal
        /// </summary>
        private async void OnSearchWidgetIconClicked(SearchWidgetIconViewModel icon)
        {
            try
            {
                var searchManager = ServiceHelper.GetService<SearchManager>();
                var context = searchManager?.GetSearch(icon.SearchId);

                RemoveSearchDesktopIcon(icon.SearchId);

                // Use IModalHost to show the appropriate search modal
                // Note: IModalHost.ShowSearchModal() shows filter selection, not the specific search instance
                // For now, we delegate to the view via a callback since showing specific search instances
                // requires access to the view's ShowSearchModalForInstanceAsync method
                if (_modalHost is Views.BalatroMainMenu mainMenu)
                {
                    if (context is not null)
                    {
                        // Live instance: reconnect with full state
                        await mainMenu.ShowSearchModalForInstanceAsync(icon.SearchId, icon.ConfigPath);
                    }
                    else if (!string.IsNullOrEmpty(icon.ConfigPath))
                    {
                        // Restored-from-disk: reopen search modal with filter loaded
                        await mainMenu.ShowSearchModalWithFilterAsync(icon.ConfigPath);
                    }
                    else
                    {
                        mainMenu.ShowSearchModal();
                    }
                }
                else
                {
                    DebugLogger.LogError(
                        "BalatroMainMenuViewModel",
                        "IModalHost is not BalatroMainMenu - cannot show search modal for desktop icon"
                    );
                }
            }
            catch (Exception ex)
            {
                DebugLogger.LogError(
                    "BalatroMainMenuViewModel",
                    $"Failed to handle search widget icon click: {ex.Message}"
                );
            }
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

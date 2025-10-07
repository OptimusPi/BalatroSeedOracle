using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using System.Windows.Input;
using Avalonia.Controls;
using CommunityToolkit.Mvvm.Input;
using BalatroSeedOracle.Controls;
using BalatroSeedOracle.Helpers;
using BalatroSeedOracle.Services;
using BalatroSeedOracle.Views.Modals;

namespace BalatroSeedOracle.ViewModels
{
    /// <summary>
    /// ViewModel for the BalatroMainMenu view
    /// Handles all state management, commands, and business logic
    /// </summary>
    public class BalatroMainMenuViewModel : BaseViewModel
    {
        private readonly UserProfileService _userProfileService;
        private readonly VibeAudioManager? _audioManager;

        // State fields
        private bool _isAnimating = true;
        private bool _isMusicPlaying = true;
        private int _widgetCounter = 0;
        private bool _isVibeOutMode = false;
        private string _mainTitle = "Welcome!";
        private string _authorName = "Author";
        private bool _authorEditMode = false;
        private double _volume = 70;
        private string _volumePercentText = "70%";
        private string _musicIconText = "🔊";
        private string _muteButtonText = "MUTE";
        private string _animationIconText = "⏸";
        private bool _isSettingsPopupOpen = false;
        private bool _isVolumePopupOpen = false;
        private bool _isModalVisible = false;

        public BalatroMainMenuViewModel()
        {
            // Initialize services
            _userProfileService = App.GetService<UserProfileService>()
                ?? throw new InvalidOperationException("UserProfileService not available");
            _audioManager = ServiceHelper.GetService<VibeAudioManager>();

            // Initialize commands
            SeedSearchCommand = new RelayCommand(OnSeedSearch);
            EditorCommand = new RelayCommand(OnEditor);
            AnalyzeCommand = new RelayCommand(OnAnalyze);
            ToolCommand = new RelayCommand(OnTool);
            SettingsCommand = new RelayCommand(OnSettings);
            AnimationToggleCommand = new RelayCommand(OnAnimationToggle);
            MusicToggleCommand = new RelayCommand(OnMusicToggle);
            MuteCommand = new RelayCommand(OnMute);
            AuthorClickCommand = new RelayCommand(OnAuthorClick);
            SaveAuthorCommand = new RelayCommand(SaveAuthor);
            CancelAuthorEditCommand = new RelayCommand(CancelAuthorEdit);
            ExitVibeOutCommand = new RelayCommand(ExitVibeOutMode);
            BuyBalatroCommand = new RelayCommand(OnBuyBalatro);

            // Load settings
            LoadSettings();
        }

        #region Properties

        /// <summary>
        /// Main title text displayed at the top
        /// </summary>
        public string MainTitle
        {
            get => _mainTitle;
            set => SetProperty(ref _mainTitle, value);
        }

        /// <summary>
        /// Whether background animation is enabled
        /// </summary>
        public bool IsAnimating
        {
            get => _isAnimating;
            set
            {
                if (SetProperty(ref _isAnimating, value))
                {
                    AnimationIconText = value ? "⏸" : "▶";
                    OnIsAnimatingChanged?.Invoke(this, value);
                }
            }
        }

        /// <summary>
        /// Animation button icon text
        /// </summary>
        public string AnimationIconText
        {
            get => _animationIconText;
            set => SetProperty(ref _animationIconText, value);
        }

        /// <summary>
        /// Whether music is playing
        /// </summary>
        public bool IsMusicPlaying
        {
            get => _isMusicPlaying;
            set => SetProperty(ref _isMusicPlaying, value);
        }

        /// <summary>
        /// Volume level (0-100)
        /// </summary>
        public double Volume
        {
            get => _volume;
            set
            {
                if (SetProperty(ref _volume, value))
                {
                    VolumePercentText = $"{(int)value}%";
                    MusicIconText = value > 0 ? "🔊" : "🔇";
                    MuteButtonText = value > 0 ? "MUTE" : "UNMUTE";
                    IsMusicPlaying = value > 0;
                    ApplyVolumeChange(value);

                    // Save volume to user profile
                    SaveVolumeToProfile();
                }
            }
        }

        /// <summary>
        /// Volume percentage display text
        /// </summary>
        public string VolumePercentText
        {
            get => _volumePercentText;
            set => SetProperty(ref _volumePercentText, value);
        }

        /// <summary>
        /// Music icon text
        /// </summary>
        public string MusicIconText
        {
            get => _musicIconText;
            set => SetProperty(ref _musicIconText, value);
        }

        /// <summary>
        /// Mute button text
        /// </summary>
        public string MuteButtonText
        {
            get => _muteButtonText;
            set => SetProperty(ref _muteButtonText, value);
        }

        /// <summary>
        /// Author name
        /// </summary>
        public string AuthorName
        {
            get => _authorName;
            set
            {
                if (SetProperty(ref _authorName, value))
                {
                    // Auto-save when changed
                    if (!AuthorEditMode && !string.IsNullOrWhiteSpace(value))
                    {
                        _userProfileService.SetAuthorName(value.Trim());
                    }
                }
            }
        }

        /// <summary>
        /// Whether author edit mode is active
        /// </summary>
        public bool AuthorEditMode
        {
            get => _authorEditMode;
            set
            {
                if (SetProperty(ref _authorEditMode, value))
                {
                    AuthorDisplayMode = !value;
                }
            }
        }

        private bool _authorDisplayMode = true;
        /// <summary>
        /// Whether author display mode is active (inverse of edit mode)
        /// </summary>
        public bool AuthorDisplayMode
        {
            get => _authorDisplayMode;
            set => SetProperty(ref _authorDisplayMode, value);
        }

        /// <summary>
        /// Whether VibeOut mode is active
        /// </summary>
        public bool IsVibeOutMode
        {
            get => _isVibeOutMode;
            set
            {
                if (SetProperty(ref _isVibeOutMode, value))
                {
                    OnVibeOutModeChanged?.Invoke(this, value);
                }
            }
        }

        /// <summary>
        /// Whether settings popup is open
        /// </summary>
        public bool IsSettingsPopupOpen
        {
            get => _isSettingsPopupOpen;
            set => SetProperty(ref _isSettingsPopupOpen, value);
        }

        /// <summary>
        /// Whether volume popup is open
        /// </summary>
        public bool IsVolumePopupOpen
        {
            get => _isVolumePopupOpen;
            set => SetProperty(ref _isVolumePopupOpen, value);
        }

        /// <summary>
        /// Whether modal is visible
        /// </summary>
        public bool IsModalVisible
        {
            get => _isModalVisible;
            set => SetProperty(ref _isModalVisible, value);
        }

        /// <summary>
        /// Widget counter for positioning desktop icons
        /// </summary>
        public int WidgetCounter
        {
            get => _widgetCounter;
            set => SetProperty(ref _widgetCounter, value);
        }

        #endregion

        #region Commands

        public ICommand SeedSearchCommand { get; }
        public ICommand EditorCommand { get; }
        public ICommand AnalyzeCommand { get; }
        public ICommand ToolCommand { get; }
        public ICommand SettingsCommand { get; }
        public ICommand AnimationToggleCommand { get; }
        public ICommand MusicToggleCommand { get; }
        public ICommand MuteCommand { get; }
        public ICommand AuthorClickCommand { get; }
        public ICommand SaveAuthorCommand { get; }
        public ICommand CancelAuthorEditCommand { get; }
        public ICommand ExitVibeOutCommand { get; }
        public ICommand BuyBalatroCommand { get; }

        #endregion

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
        public event EventHandler<bool>? OnIsAnimatingChanged;

        /// <summary>
        /// Raised when VibeOut mode changes
        /// </summary>
        public event EventHandler<bool>? OnVibeOutModeChanged;

        /// <summary>
        /// Raised when settings popup visibility should change
        /// </summary>
        public event EventHandler<bool>? OnSettingsPopupToggle;

        /// <summary>
        /// Raised when volume popup visibility should change
        /// </summary>
        public event EventHandler<bool>? OnVolumePopupToggle;

        /// <summary>
        /// Raised when author edit mode is activated (for focus request)
        /// </summary>
        public event EventHandler? OnAuthorEditActivated;

        #endregion

        #region Command Implementations

        private void OnSeedSearch()
        {
            PlayButtonClickSound();
            try
            {
                ModalRequested?.Invoke(this, new ModalRequestedEventArgs(ModalType.Search));
            }
            catch (Exception ex)
            {
                DebugLogger.LogError("BalatroMainMenuViewModel", $"Failed to open search modal: {ex}");
                ShowErrorModal($"Failed to open Search Modal:\n\n{ex.Message}\n\nPlease check the logs for details.");
            }
        }

        private void OnEditor()
        {
            PlayButtonClickSound();
            ModalRequested?.Invoke(this, new ModalRequestedEventArgs(ModalType.Filters));
        }

        private void OnAnalyze()
        {
            PlayButtonClickSound();
            ModalRequested?.Invoke(this, new ModalRequestedEventArgs(ModalType.Analyze));
        }

        private void OnTool()
        {
            PlayButtonClickSound();
            ModalRequested?.Invoke(this, new ModalRequestedEventArgs(ModalType.Settings));
        }

        private void OnSettings()
        {
            PlayButtonClickSound();
            IsSettingsPopupOpen = !IsSettingsPopupOpen;
            OnSettingsPopupToggle?.Invoke(this, IsSettingsPopupOpen);
        }

        private void OnAnimationToggle()
        {
            PlayButtonClickSound();
            IsAnimating = !IsAnimating;
        }

        private void OnMusicToggle()
        {
            PlayButtonClickSound();
            IsVolumePopupOpen = !IsVolumePopupOpen;
            OnVolumePopupToggle?.Invoke(this, IsVolumePopupOpen);
        }

        private double _previousVolume = 70;
        private void OnMute()
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

        private void OnAuthorClick()
        {
            PlayButtonClickSound();
            AuthorEditMode = true;
            OnAuthorEditActivated?.Invoke(this, EventArgs.Empty);
        }

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

        private void CancelAuthorEdit()
        {
            // Restore original value
            AuthorName = _userProfileService.GetAuthorName();
            AuthorEditMode = false;
        }

        public void ExitVibeOutMode()
        {
            IsVibeOutMode = false;
        }

        public void EnterVibeOutMode()
        {
            IsVibeOutMode = true;
        }

        private void OnBuyBalatro()
        {
            try
            {
                var url = "https://playbalatro.com/";
                Process.Start(new ProcessStartInfo { FileName = url, UseShellExecute = true });
            }
            catch (Exception ex)
            {
                DebugLogger.LogError("BalatroMainMenuViewModel", $"Error opening Balatro website: {ex.Message}");
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
            MainTitle = "Welcome!";
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

                DebugLogger.Log("BalatroMainMenuViewModel", $"Settings loaded: Volume={profile.MusicVolume}, Muted={profile.IsMusicMuted}");
            }
            catch (Exception ex)
            {
                DebugLogger.LogError("BalatroMainMenuViewModel", $"Error loading settings: {ex.Message}");
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
                DebugLogger.Log("BalatroMainMenuViewModel", $"Volume saved: {profile.MusicVolume}, Muted: {profile.IsMusicMuted}");
            }
            catch (Exception ex)
            {
                DebugLogger.LogError("BalatroMainMenuViewModel", $"Error saving volume: {ex.Message}");
            }
        }

        /// <summary>
        /// Loads and applies visualizer settings
        /// </summary>
        public void LoadAndApplyVisualizerSettings(BalatroShaderBackground? shader)
        {
            if (shader == null) return;

            try
            {
                var profile = _userProfileService.GetProfile();
                var settings = profile.VibeOutSettings;

                DebugLogger.Log("BalatroMainMenuViewModel", "Loading visualizer settings on startup...");

                // Apply theme
                shader.SetTheme(settings.ThemeIndex);

                // Apply intensity settings
                shader.UpdateVibeIntensity(settings.AudioIntensity);
                shader.SetBaseTimeSpeed(settings.TimeSpeed);
                shader.SetParallaxStrength(settings.ParallaxStrength);

                // Apply custom colors if CUSTOMIZE theme is selected (index 8)
                if (settings.ThemeIndex == 8)
                {
                    shader.SetMainColor(settings.MainColor);
                    shader.SetAccentColor(settings.AccentColor);
                }

                // Apply shader effect audio sources
                shader.SetShadowFlickerSource((AudioSource)settings.ShadowFlickerSource);
                shader.SetSpinSource((AudioSource)settings.SpinSource);
                shader.SetTwirlSource((AudioSource)settings.TwirlSource);
                shader.SetZoomThumpSource((AudioSource)settings.ZoomThumpSource);
                shader.SetColorSaturationSource((AudioSource)settings.ColorSaturationSource);
                shader.SetBeatPulseSource((AudioSource)settings.BeatPulseSource);

                DebugLogger.Log("BalatroMainMenuViewModel", $"Visualizer settings loaded - Theme: {settings.ThemeIndex}, Intensity: {settings.AudioIntensity}");
            }
            catch (Exception ex)
            {
                DebugLogger.LogError("BalatroMainMenuViewModel", $"Error loading visualizer settings: {ex.Message}");
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
                    // Check if the search is recent (within last 24 hours)
                    var timeSinceSearch = DateTime.UtcNow - resumeState.LastActiveTime;
                    if (timeSinceSearch.TotalHours > 24)
                    {
                        _userProfileService.ClearSearchState();
                        return;
                    }

                    DebugLogger.Log("BalatroMainMenuViewModel", $"Found resumable search state from {timeSinceSearch.TotalMinutes:F0} minutes ago");

                    if (!string.IsNullOrEmpty(resumeState.ConfigPath) && File.Exists(resumeState.ConfigPath))
                    {
                        var placeholderSearchId = Guid.NewGuid().ToString();
                        showIconCallback?.Invoke(placeholderSearchId, resumeState.ConfigPath);

                        DebugLogger.Log("BalatroMainMenuViewModel", $"Restored desktop icon for search (not started yet): {resumeState.ConfigPath}");
                    }
                    else
                    {
                        DebugLogger.Log("BalatroMainMenuViewModel", $"Skipping desktop icon for resumable search - invalid config path: {resumeState.ConfigPath}");
                        _userProfileService.ClearSearchState();
                    }
                }
            }
            catch (Exception ex)
            {
                DebugLogger.LogError("BalatroMainMenuViewModel", $"Error checking for resumable search: {ex.Message}");
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
                _audioManager?.PlayClickSound();
            }
            catch (Exception ex)
            {
                DebugLogger.LogError("BalatroMainMenuViewModel", $"Failed to play button click sound: {ex.Message}");
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
                _audioManager?.SetMasterVolume(volumeFloat);
                _audioManager?.SetSfxVolume(volumeFloat);
            }
            catch (Exception ex)
            {
                DebugLogger.LogError("BalatroMainMenuViewModel", $"Failed to apply volume: {ex.Message}");
            }
        }

        #endregion

        #region Shader Management

        /// <summary>
        /// Apply visualizer theme change to shader
        /// </summary>
        public void ApplyVisualizerTheme(BalatroShaderBackground? shader, int themeIndex)
        {
            shader?.SetTheme(themeIndex);
        }

        /// <summary>
        /// Apply main color to shader
        /// </summary>
        public void ApplyMainColor(BalatroShaderBackground? shader, int colorIndex)
        {
            shader?.SetMainColor(colorIndex);
        }

        /// <summary>
        /// Apply accent color to shader
        /// </summary>
        public void ApplyAccentColor(BalatroShaderBackground? shader, int colorIndex)
        {
            shader?.SetAccentColor(colorIndex);
        }

        /// <summary>
        /// Apply audio intensity to shader
        /// </summary>
        public void ApplyAudioIntensity(BalatroShaderBackground? shader, float intensity)
        {
            shader?.SetAudioReactivityIntensity(intensity);
        }

        /// <summary>
        /// Apply parallax strength to shader
        /// </summary>
        public void ApplyParallaxStrength(BalatroShaderBackground? shader, float strength)
        {
            shader?.SetParallaxStrength(strength);
        }

        /// <summary>
        /// Apply time speed to shader
        /// </summary>
        public void ApplyTimeSpeed(BalatroShaderBackground? shader, float speed)
        {
            shader?.SetBaseTimeSpeed(speed);
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
        /// Apply zoom punch to shader
        /// </summary>
        public void ApplyShaderZoomPunch(BalatroShaderBackground? shader, float zoom)
        {
            shader?.SetZoomPunch(zoom);
        }

        /// <summary>
        /// Apply melody saturation to shader
        /// </summary>
        public void ApplyShaderMelodySaturation(BalatroShaderBackground? shader, float saturation)
        {
            shader?.SetMelodySaturation(saturation);
        }

        /// <summary>
        /// Apply shadow flicker source to shader
        /// </summary>
        public void ApplyShadowFlickerSource(BalatroShaderBackground? shader, int sourceIndex)
        {
            shader?.SetShadowFlickerSource((AudioSource)sourceIndex);
        }

        /// <summary>
        /// Apply spin source to shader
        /// </summary>
        public void ApplySpinSource(BalatroShaderBackground? shader, int sourceIndex)
        {
            shader?.SetSpinSource((AudioSource)sourceIndex);
        }

        /// <summary>
        /// Apply twirl source to shader
        /// </summary>
        public void ApplyTwirlSource(BalatroShaderBackground? shader, int sourceIndex)
        {
            shader?.SetTwirlSource((AudioSource)sourceIndex);
        }

        /// <summary>
        /// Apply zoom thump source to shader
        /// </summary>
        public void ApplyZoomThumpSource(BalatroShaderBackground? shader, int sourceIndex)
        {
            shader?.SetZoomThumpSource((AudioSource)sourceIndex);
        }

        /// <summary>
        /// Apply color saturation source to shader
        /// </summary>
        public void ApplyColorSaturationSource(BalatroShaderBackground? shader, int sourceIndex)
        {
            shader?.SetColorSaturationSource((AudioSource)sourceIndex);
        }

        /// <summary>
        /// Apply beat pulse source to shader
        /// </summary>
        public void ApplyBeatPulseSource(BalatroShaderBackground? shader, int sourceIndex)
        {
            shader?.SetBeatPulseSource((AudioSource)sourceIndex);
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
        Custom
    }

    #endregion
}

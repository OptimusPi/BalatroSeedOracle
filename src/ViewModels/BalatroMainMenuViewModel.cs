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
using AudioSource = BalatroSeedOracle.Controls.BalatroShaderBackground.AudioSource;

namespace BalatroSeedOracle.ViewModels
{
    /// <summary>
    /// ViewModel for the BalatroMainMenu view
    /// Handles all state management, commands, and business logic
    /// </summary>
    public partial class BalatroMainMenuViewModel : ObservableObject
    {
        private readonly UserProfileService _userProfileService;
        private readonly SoundFlowAudioManager? _soundFlowAudioManager;
        private Action<float, float, float, float>? _audioAnalysisHandler;

        // Effect source tracking
        private int _shadowFlickerSource = 0;
        private int _spinSource = 0;
        private int _twirlSource = 0;
        private int _zoomThumpSource = 0;
        private int _colorSaturationSource = 0;

        // Effect state for decay (reserved for future audio-reactive implementation)
        // private float _zoomThumpDecay = 0f;
        // private float _spinDecay = 0f;
        // private float _twirlDecay = 0f;
        // private float _shadowFlickerDecay = 0f;

        /// <summary>
        /// Expose audio manager for widgets to access frequency data
        /// </summary>
        public SoundFlowAudioManager? AudioManager => _soundFlowAudioManager;

        [ObservableProperty]
        private string _mainTitle = "Welcome!";

        [ObservableProperty]
        private bool _isAnimating = true;

        [ObservableProperty]
        private string _animationIconText = "⏸";

        [ObservableProperty]
        private bool _isMusicPlaying = true;

        [ObservableProperty]
        private double _volume = 70;

        [ObservableProperty]
        private string _volumePercentText = "70%";

        [ObservableProperty]
        private string _musicIconText = "🔊";

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

        [ObservableProperty]
        private int _widgetCounter = 0;

        [ObservableProperty]
        private bool _isVisualizerWidgetVisible = false;

        private double _previousVolume = 70;

        public BalatroMainMenuViewModel()
        {
            // Initialize services
            _userProfileService =
                App.GetService<UserProfileService>()
                ?? throw new InvalidOperationException("UserProfileService not available");

            // Get SoundFlow audio manager (8 independent tracks)
            _soundFlowAudioManager = ServiceHelper.GetService<SoundFlowAudioManager>();
            if (_soundFlowAudioManager != null)
            {
                Console.WriteLine(
                    "[ViewModel] 🎵 Using SoundFlowAudioManager (8 independent tracks)"
                );
            }

            // Load settings
            LoadSettings();
        }

        partial void OnIsAnimatingChanged(bool value)
        {
            AnimationIconText = value ? "⏸" : "▶";
            OnIsAnimatingChangedEvent?.Invoke(this, value);
        }

        partial void OnVolumeChanged(double value)
        {
            VolumePercentText = $"{(int)value}%";
            MusicIconText = value > 0 ? "🔊" : "🔇";
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

        #endregion

        #region Command Implementations

        [RelayCommand(CanExecute = nameof(CanOpenModal))]
        private void SeedSearch()
        {
            PlayButtonClickSound();
            try
            {
                IsModalVisible = true;
                ModalRequested?.Invoke(this, new ModalRequestedEventArgs(ModalType.Search));
            }
            catch (Exception ex)
            {
                IsModalVisible = false;
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

        [RelayCommand(CanExecute = nameof(CanOpenModal))]
        private void Editor()
        {
            PlayButtonClickSound();
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
        private void ToggleVisualizerWidget()
        {
            PlayButtonClickSound();
            IsVisualizerWidgetVisible = !IsVisualizerWidgetVisible;
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
        private void AuthorClick()
        {
            PlayButtonClickSound();
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

                // Theme + color selections
                var themeIndex = Math.Clamp(settings.ThemeIndex, 0, 8);
                ApplyVisualizerTheme(shader, themeIndex);

                var mainColorIndex = Math.Clamp(settings.MainColor, 0, 8);
                ApplyMainColor(shader, mainColorIndex);

                var accentColorIndex = Math.Clamp(settings.AccentColor, 0, 8);
                ApplyAccentColor(shader, accentColorIndex);

                // Shader parameter preferences
                var timeSpeed = Math.Clamp(settings.TimeSpeed, 0f, 3f);
                ApplyTimeSpeed(shader, timeSpeed);

                var audioIntensity = Math.Clamp(settings.AudioIntensity, 0f, 2f);
                ApplyAudioIntensity(shader, audioIntensity);

                var parallaxStrength = Math.Clamp(settings.ParallaxStrength, 0f, 2f);
                ApplyParallaxStrength(shader, parallaxStrength);

                // Audio effect source mappings (effect binding stubs for future work)
                ApplyShadowFlickerSource(shader, Math.Clamp(settings.ShadowFlickerSource, 0, 4));
                ApplySpinSource(shader, Math.Clamp(settings.SpinSource, 0, 4));
                ApplyTwirlSource(shader, Math.Clamp(settings.TwirlSource, 0, 4));
                ApplyZoomThumpSource(shader, Math.Clamp(settings.ZoomThumpSource, 0, 4));
                ApplyColorSaturationSource(
                    shader,
                    Math.Clamp(settings.ColorSaturationSource, 0, 4)
                );
                ApplyBeatPulseSource(shader, Math.Clamp(settings.BeatPulseSource, 0, 4));

                // Per-track volume balancing for SoundFlow audio stems
                if (_soundFlowAudioManager != null)
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
                    $"Visualizer settings applied (Theme={themeIndex}, MainColor={mainColorIndex}, AccentColor={accentColorIndex}, TimeSpeed={timeSpeed:F2}, AudioIntensity={audioIntensity:F2})"
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
                // No click sounds with SoundFlow for now
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

                if (_soundFlowAudioManager != null)
                {
                    _soundFlowAudioManager.MasterVolume = volumeFloat;
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
        /// Set volume of a specific audio track (only works with SoundFlowAudioManager)
        /// </summary>
        public void SetTrackVolume(string trackName, float volume)
        {
            try
            {
                _soundFlowAudioManager?.SetTrackVolume(trackName, volume);
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
                if (_soundFlowAudioManager != null)
                {
                    _audioAnalysisHandler = (bass, mid, treble, peak) => {
                        // Audio reactivity will be handled by proper effect binding system
                        // TODO: Implement effect bindings that map tracks to shader parameters
                    };

                    _soundFlowAudioManager.AudioAnalysisUpdated += _audioAnalysisHandler;
                    Console.WriteLine(
                        "[ViewModel] ✅ Audio analysis handler connected (awaiting effect binding system)"
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
        /// Apply visualizer theme change to shader
        /// </summary>
        public void ApplyVisualizerTheme(BalatroShaderBackground? shader, int themeIndex)
        {
            // Removed - themes replaced by direct color control
        }

        /// <summary>
        /// Apply main color to shader
        /// </summary>
        public void ApplyMainColor(BalatroShaderBackground? shader, int colorIndex)
        {
            var color = IndexToSKColor(colorIndex);
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
        /// Apply audio intensity to shader
        /// </summary>
        public void ApplyAudioIntensity(BalatroShaderBackground? shader, float intensity)
        {
            // Removed - will be handled by effect binding system
        }

        /// <summary>
        /// Apply parallax strength to shader
        /// </summary>
        public void ApplyParallaxStrength(BalatroShaderBackground? shader, float strength)
        {
            // Removed - use SetParallax() directly for now
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

        /// <summary>
        /// Apply beat pulse source to shader
        /// </summary>
        public void ApplyBeatPulseSource(BalatroShaderBackground? shader, int sourceIndex)
        {
            // DELETED - not used
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

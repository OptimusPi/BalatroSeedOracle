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
using BalatroSeedOracle.ViewModels.Widgets;
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
        private readonly SoundFlowAudioManager? _soundFlowAudioManager;
        private readonly EventFXService? _eventFXService;
        private Action<float, float, float, float>? _audioAnalysisHandler;
        private IWidgetRegistry? _widgetRegistry;

        /// <summary>
        /// Widget container for the new interface system
        /// </summary>
        [ObservableProperty]
        private WidgetContainerViewModel? _widgetContainer;

        // Effect source tracking
        private int _shadowFlickerSource = 0;
        private int _spinSource = 0;
        private int _twirlSource = 0;
        private int _zoomThumpSource = 0;
        private int _colorSaturationSource = 0;

        /// <summary>
        /// Expose audio manager for widgets to access frequency data
        /// </summary>
        public SoundFlowAudioManager? AudioManager => _soundFlowAudioManager;

        [ObservableProperty]
        private string _mainTitle = "";

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

        partial void OnIsModalVisibleChanged(bool value)
        {
            // Hide widgets when a modal is open, restore previous state when closed
            if (value)
            {
                AreWidgetsHidden = true;
            }
            else
            {
                AreWidgetsHidden = false;
            }
        }

        [ObservableProperty]
        private int _widgetCounter = 0;

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
        private bool _isVibeOutMode = false;

        // New: Hide/show all widgets
        [ObservableProperty]
        private bool _areWidgetsHidden = false;

        [RelayCommand]
        private void ToggleWidgetsVisibility()
        {
            AreWidgetsHidden = !AreWidgetsHidden;
        }

        private double _previousVolume = 70;

        public BalatroMainMenuViewModel()
        {
            // Initialize services (temporary fallback until proper DI)
            _userProfileService = App.GetService<UserProfileService>() 
                ?? throw new InvalidOperationException("UserProfileService not available");

            // Initialize widget system
            WidgetContainer = App.GetService<WidgetContainerViewModel>();
            _widgetRegistry = App.GetService<IWidgetRegistry>();
            
            if (WidgetContainer != null && _widgetRegistry != null)
            {
                InitializeWidgetSystem();
            }



            // Get SoundFlow audio manager (8 independent tracks)
            _soundFlowAudioManager = ServiceHelper.GetService<SoundFlowAudioManager>();
            if (_soundFlowAudioManager != null)
            {
                Console.WriteLine(
                    "[ViewModel] Using SoundFlowAudioManager (8 independent tracks)"
                );
            }

            // Get EventFX service for triggering configured animations
            _eventFXService = ServiceHelper.GetService<EventFXService>();

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
        private void ToggleVisualizerWidget()
        {
            PlayButtonClickSound();
            IsVisualizerWidgetVisible = !IsVisualizerWidgetVisible;
        }

        [RelayCommand]
        private void ToggleTransitionDesignerWidget()
        {
            PlayButtonClickSound();
            IsTransitionDesignerWidgetVisible = !IsTransitionDesignerWidgetVisible;
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

            // In vibe out mode: hide dock buttons and widgets, keep only modal visible
            AreWidgetsHidden = IsVibeOutMode;

            DebugLogger.Log(
                "BalatroMainMenu",
                $"Vibe Out Mode: {(IsVibeOutMode ? "ON" : "OFF")}, Widgets hidden: {AreWidgetsHidden}"
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

                // Load feature toggles for widget visibility
                var toggles = profile.FeatureToggles;
                IsMusicMixerWidgetVisible = toggles?.ShowMusicMixer ?? false;
                IsVisualizerWidgetVisible = toggles?.ShowVisualizer ?? false;
                IsTransitionDesignerWidgetVisible = toggles?.ShowTransitionDesigner ?? false;
                IsFertilizerWidgetVisible = toggles?.ShowFertilizer ?? false;
                IsHostApiWidgetVisible = toggles?.ShowHostServer ?? false;
                IsEventFXWidgetVisible = toggles?.ShowEventFX ?? false;

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
            IsMusicMixerWidgetVisible = toggles?.ShowMusicMixer ?? false;
            IsVisualizerWidgetVisible = toggles?.ShowVisualizer ?? false;
            IsTransitionDesignerWidgetVisible = toggles?.ShowTransitionDesigner ?? false;
            IsFertilizerWidgetVisible = toggles?.ShowFertilizer ?? false;
            IsHostApiWidgetVisible = toggles?.ShowHostServer ?? false;
            IsEventFXWidgetVisible = toggles?.ShowEventFX ?? false;
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
                SoundFlowAudioManager.Instance.PlaySfx("button", 1.0f);
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
                        // Audio reactivity handled by effect binding system
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

        private void InitializeWidgetSystem()
        {
            try
            {
                if (_widgetRegistry != null)
                {
                    // Register DayLatro widget
                    _widgetRegistry.RegisterWidget(new WidgetMetadata
                    {
                        Id = "daylatro",
                        Title = "DayLatro", 
                        IconResource = "Calendar",
                        WidgetType = typeof(DayLatroWidgetViewModel),
                        ViewModelType = typeof(DayLatroWidgetViewModel),
                        AllowClose = false,
                        AllowPopOut = false,
                        DefaultSize = new Avalonia.Size(400, 500),
                        Description = "Daily Balatro challenge tracking",
                        Category = "Core"
                    });

                    // Register TransitionDesigner widget
                    _widgetRegistry.RegisterWidget(new WidgetMetadata
                    {
                        Id = "transition-designer", 
                        Title = "Transition Designer",
                        IconResource = "MovieFilter",
                        WidgetType = typeof(TransitionDesignerWidgetViewModel),
                        ViewModelType = typeof(TransitionDesignerWidgetViewModel),
                        AllowClose = false,
                        AllowPopOut = false,
                        DefaultSize = new Avalonia.Size(420, 600),
                        Description = "Design and test audio/visual transitions",
                        Category = "Effects"
                    });

                    // Create widget instances using the interface system
                    _ = WidgetContainer?.CreateWidgetAsync("daylatro");
                    _ = WidgetContainer?.CreateWidgetAsync("transition-designer");
                }
            }
            catch (Exception ex)
            {
                DebugLogger.Log("BalatroMainMenu", $"Failed to initialize widget system: {ex.Message}");
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

    #endregion

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
}

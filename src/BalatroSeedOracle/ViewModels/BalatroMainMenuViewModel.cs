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

        public BalatroMainMenuViewModel(
            UserProfileService userProfileService,
            SearchModalViewModel searchModalViewModel,
            FiltersModalViewModel filtersModalViewModel,
            Func<AnalyzeModalViewModel> analyzeModalFactory
        )
        {
            _userProfileService = userProfileService;
            _analyzeModalFactory =
                analyzeModalFactory ?? throw new ArgumentNullException(nameof(analyzeModalFactory));
            SearchModalViewModel = searchModalViewModel;
            FiltersModalViewModel = filtersModalViewModel;

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
        /// Resolves the filter config file path for a filter ID (.jaml in JamlFilters/).
        /// MVVM: path resolution and file checks belong in ViewModel, not View.
        /// </summary>
        public string? GetFilterConfigPath(string filterId)
        {
            if (string.IsNullOrWhiteSpace(filterId))
                return null;

            var path = FilterFiles.Resolve(filterId);
            return File.Exists(path) ? path : null;
        }

        [RelayCommand(CanExecute = nameof(CanOpenModal))]
        private void Editor()
        {
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
            // Settings now opens SettingsModal via ModalRequested event
        }

        [RelayCommand]
        private void AnimationToggle()
        {
            IsAnimating = !IsAnimating;
        }

        [RelayCommand]
        private void MusicToggle()
        {
            IsVolumePopupOpen = !IsVolumePopupOpen;
            OnVolumePopupToggle?.Invoke(this, IsVolumePopupOpen);
        }

        [RelayCommand]
        private void Mute()
        {
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
                    _userProfileService.SaveProfile();
                }

                // Color selections (themes removed - using direct color control)
                var mainColorIndex = Math.Clamp(settings.MainColor, 0, 8);
                ApplyMainColor(shader, mainColorIndex);

                var accentColorIndex = Math.Clamp(settings.AccentColor, 0, 8);
                ApplyAccentColor(shader, accentColorIndex);

                // Shader parameter preferences
                var timeSpeed = Math.Clamp(settings.TimeSpeed, 0f, 3f);
                ApplyTimeSpeed(shader, timeSpeed);

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

                var icon = new SearchWidgetIconViewModel(
                    searchId,
                    configPath,
                    OnSearchWidgetIconClicked
                );
                ActiveSearchWidgets.Add(icon);

                DebugLogger.Log(
                    "BalatroMainMenuViewModel",
                    $"Search desktop icon created for searchId: {searchId}"
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
                RemoveSearchDesktopIcon(icon.SearchId);

                if (App.GetService<Views.BalatroMainMenu>() is { } mainMenu)
                {
                    if (!string.IsNullOrEmpty(icon.ConfigPath))
                    {
                        // Reopen search modal with filter loaded
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
                        "BalatroMainMenu not available - cannot show search modal for desktop icon"
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

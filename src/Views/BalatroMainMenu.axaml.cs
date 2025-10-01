using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Presenters;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.LogicalTree;
using Avalonia.Markup.Xaml;
using Avalonia.VisualTree;
using BalatroSeedOracle.Controls;
using BalatroSeedOracle.Helpers;
using BalatroSeedOracle.Models;
using BalatroSeedOracle.Services;
using BalatroSeedOracle.Views.Modals;

namespace BalatroSeedOracle.Views
{
    public partial class BalatroMainMenu : UserControl
    {
        private Grid? _modalContainer;
        private Control? _background;
        private Button? _animationToggleButton;
        private TextBlock? _animationButtonText;
        private Button? _musicToggleButton;
        private TextBlock? _musicToggleIcon;
        private SearchDesktopIcon? _searchDesktopIcon;
        private bool _isAnimating = true;
        private bool _isMusicPlaying = true;
        private int _widgetCounter = 0;
        private int _minimizedWidgetCount = 0;
        private UserProfileService? _userProfileService;

        // VibeOut mode
        private Grid? _vibeOutOverlay;
        private Grid? _mainContent;
        private bool _isVibeOutMode = false;

        // Event handler references for proper cleanup
        private Action<float>? _beatDetectedHandler;
        private Action<float, float, float, float>? _audioAnalysisHandler;

        /// <summary>
        /// Callback to request main content swap (set by MainWindow)
        /// </summary>
        public Action<UserControl>? RequestContentSwap { get; set; }

        public BalatroMainMenu()
        {
            InitializeComponent();

            // Defer service initialization to OnLoaded to ensure services are ready
            this.Loaded += OnLoaded;
            this.Loaded += InitializeVibeAudio;
        }
        
        private void InitializeVibeAudio(object? sender, RoutedEventArgs e)
        {
            // Unsubscribe after first run to prevent multiple subscriptions
            this.Loaded -= InitializeVibeAudio;

            try
            {
                // Start subtle background music on main menu
                var audioManager = ServiceHelper.GetService<VibeAudioManager>();

                if (audioManager == null)
                    return;

                audioManager.TransitionTo(AudioState.MainMenu);

                // Store handler references for cleanup
                _beatDetectedHandler = (beatIntensity) =>
                {
                    if (_background is BalatroShaderBackground shader)
                    {
                        // Amplify beat intensity for dramatic effect
                        shader.OnBeatDetected(beatIntensity * 3.0f);
                    }
                };

                _audioAnalysisHandler = (bass, mid, treble, peak) =>
                {
                    if (_background is BalatroShaderBackground shader)
                    {
                        // Use overall peak energy as vibe intensity (smooth, not beat-reactive)
                        shader.UpdateVibeIntensity(peak * 0.05f);

                        // Pass melodic FFT values directly to shader
                        shader.UpdateMelodicFFT(mid, treble, peak);

                        // Pass individual track intensities
                        shader.UpdateTrackIntensities(
                            audioManager.MelodyIntensity,
                            audioManager.ChordsIntensity,
                            audioManager.BassTrackIntensity
                        );
                    }
                };

                // Hook up beat detection to background shader
                audioManager.BeatDetected += _beatDetectedHandler;

                // Hook up audio analysis for vibe intensity and melodic FFT
                audioManager.AudioAnalysisUpdated += _audioAnalysisHandler;
            }
            catch (Exception ex)
            {
                DebugLogger.LogError("BalatroMainMenu", $"Failed to start vibe audio: {ex.Message}");
            }
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);

            _modalContainer = this.FindControl<Grid>("ModalContainer");
            _background = this.FindControl<Control>("BackgroundControl");
            _animationToggleButton = this.FindControl<Button>("AnimationToggleButton");
            _musicToggleButton = this.FindControl<Button>("MusicToggleButton");
            _searchDesktopIcon = this.FindControl<SearchDesktopIcon>("SearchDesktopIcon");
            _vibeOutOverlay = this.FindControl<Grid>("VibeOutOverlay");
            _mainContent = this.FindControl<Grid>("MainContent");

            if (_animationToggleButton != null)
            {
                // Find the TextBlock inside the button using logical tree traversal
                _animationButtonText = LogicalExtensions
                    .GetLogicalChildren(_animationToggleButton)
                    .OfType<TextBlock>()
                    .FirstOrDefault();
            }

            if (_musicToggleButton != null)
            {
                // Find the music icon TextBlock
                _musicToggleIcon = LogicalExtensions
                    .GetLogicalChildren(_musicToggleButton)
                    .OfType<TextBlock>()
                    .FirstOrDefault();
            }

            // Don't initialize service here - wait for OnLoaded
        }

        private UserControl? _activeModalContent;
        private TextBlock? _mainTitleText;

        /// <summary>
        /// Updates the main title text
        /// </summary>
        public void SetTitle(string title)
        {
            if (_mainTitleText == null)
            {
                _mainTitleText = this.FindControl<TextBlock>("MainTitleText");
            }

            if (_mainTitleText != null)
            {
                _mainTitleText.Text = title;
            }
        }

        /// <summary>
        /// Show a UserControl as a modal overlay in the main menu, styled as a Balatro card
        /// </summary>
        public void ShowModalContent(UserControl content, string? title = null)
        {
            if (_modalContainer == null)
                return;

            // Remove previous content
            _modalContainer.Children.Clear();

            // Add the modal directly - it already has its own sizing grid
            _modalContainer.Children.Add(content);
            _modalContainer.IsVisible = true;
            _activeModalContent = content;

            // Update title if provided
            if (!string.IsNullOrEmpty(title))
            {
                SetTitle(title);
            }
            
            // Transition audio to modal open state
            var audioManager = App.GetService<Services.VibeAudioManager>();
            audioManager?.TransitionTo(Services.AudioState.ModalOpen);
        }

        /// <summary>
        /// Hide the modal overlay
        /// </summary>
        public void HideModalContent()
        {
            if (_modalContainer == null)
                return;

            _modalContainer.Children.Clear();
            _modalContainer.IsVisible = false;
            _activeModalContent = null;

            // Reset title to Welcome!
            SetTitle("Welcome!");
            
            // Transition audio back to main menu state
            var audioManager = App.GetService<Services.VibeAudioManager>();
            audioManager?.TransitionTo(Services.AudioState.MainMenu);
        }

        /// <summary>
        /// Plays a click sound effect for button presses
        /// </summary>
        private void PlayButtonClickSound()
        {
            try
            {
                var audioManager = ServiceHelper.GetService<VibeAudioManager>();
                audioManager?.PlayClickSound();
            }
            catch (Exception ex)
            {
                // Silently fail - don't let SFX errors crash the app
                DebugLogger.LogError("BalatroMainMenu", $"Failed to play button click sound: {ex.Message}");
            }
        }

        // Main menu button event handlers
        private void OnSeedSearchClick(object? sender, RoutedEventArgs e)
        {
            PlayButtonClickSound();
            try
            {
                // Open the search modal
                this.ShowSearchModal();
            }
            catch (Exception ex)
            {
                BalatroSeedOracle.Helpers.DebugLogger.LogError(
                    "BalatroMainMenu",
                    $"Failed to open search modal: {ex}"
                );
                // Show error in UI
                var errorModal = new StandardModal("ERROR");
                var errorText = new TextBlock
                {
                    Text =
                        $"Failed to open Search Modal:\n\n{ex.Message}\n\nPlease check the logs for details.",
                    Margin = new Avalonia.Thickness(20),
                    TextWrapping = Avalonia.Media.TextWrapping.Wrap,
                };
                errorModal.SetContent(errorText);
                errorModal.BackClicked += (s, ev) => HideModalContent();
                ShowModalContent(errorModal, "ERROR");
            }
        }

        private void OnEditorClick(object? sender, RoutedEventArgs e)
        {
            PlayButtonClickSound();
            // Open filters modal with blank/new filter
            this.ShowFiltersModal();
        }

        private void OnAnalyzeClick(object? sender, RoutedEventArgs e)
        {
            PlayButtonClickSound();
            // Show the analyze modal
            var analyzeModal = new AnalyzeModal();
            var modal = new StandardModal("ANALYZE");
            modal.SetContent(analyzeModal);
            modal.BackClicked += (s, ev) => HideModalContent();
            ShowModalContent(modal, "SEED ANALYZER");
        }

        private void OnToolClick(object? sender, RoutedEventArgs e)
        {
            PlayButtonClickSound();
            // Use the modal helper extension method
            this.ShowToolsModal();
        }

        /// <summary>
        /// Proper cleanup when the view is disposed
        /// </summary>
        public void Dispose()
        {
            try
            {
                CleanupEventHandlers();
            }
            catch (Exception ex)
            {
                BalatroSeedOracle.Helpers.DebugLogger.LogError(
                    "BalatroMainMenu",
                    $"⚠️  Error during disposal: {ex.Message}"
                );
            }
        }

        protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
        {
            CleanupEventHandlers();
            base.OnDetachedFromVisualTree(e);
        }

        private void CleanupEventHandlers()
        {
            try
            {
                // Unsubscribe from audio manager events
                var audioManager = ServiceHelper.GetService<VibeAudioManager>();
                if (audioManager != null)
                {
                    if (_beatDetectedHandler != null)
                    {
                        audioManager.BeatDetected -= _beatDetectedHandler;
                        _beatDetectedHandler = null;
                    }

                    if (_audioAnalysisHandler != null)
                    {
                        audioManager.AudioAnalysisUpdated -= _audioAnalysisHandler;
                        _audioAnalysisHandler = null;
                    }
                }

                DebugLogger.Log("BalatroMainMenu", "Event handlers cleaned up successfully");
            }
            catch (Exception ex)
            {
                DebugLogger.LogError("BalatroMainMenu", $"Error cleaning up event handlers: {ex.Message}");
            }
        }

        private void OnBuyBalatroClick(object? sender, PointerPressedEventArgs e)
        {
            try
            {
                // Open the Balatro website in the default browser
                var url = "https://playbalatro.com/";
                Process.Start(new ProcessStartInfo { FileName = url, UseShellExecute = true });
            }
            catch (Exception ex)
            {
                BalatroSeedOracle.Helpers.DebugLogger.LogError(
                    "BalatroMainMenu",
                    $"Error opening Balatro website: {ex.Message}"
                );
            }
        }

        /// <summary>
        /// Opens settings modal when settings button clicked
        /// </summary>
        private void OnSettingsClick(object? sender, RoutedEventArgs e)
        {
            PlayButtonClickSound();
            var popup = this.FindControl<Popup>("SettingsPopup");
            if (popup != null)
            {
                popup.IsOpen = !popup.IsOpen;

                // Wire up settings modal events if opening
                if (popup.IsOpen)
                {
                    var modal = this.FindControl<SettingsModal>("SettingsModal");
                    if (modal != null)
                    {
                        // Unwire old events to prevent duplicates
                        modal.CloseRequested -= OnSettingsClose;
                        modal.ThemeChanged -= OnThemeChanged;
                        modal.MusicVolumeChanged -= OnMusicVolumeChanged;
                        modal.SfxVolumeChanged -= OnSfxVolumeChanged;
                        modal.ContrastChanged -= OnContrastChanged;
                        modal.SpinChanged -= OnSpinChanged;
                        modal.SpeedChanged -= OnSpeedChanged;

                        // Wire new events
                        modal.CloseRequested += OnSettingsClose;
                        modal.ThemeChanged += OnThemeChanged;
                        modal.MusicVolumeChanged += OnMusicVolumeChanged;
                        modal.SfxVolumeChanged += OnSfxVolumeChanged;
                        modal.ContrastChanged += OnContrastChanged;
                        modal.SpinChanged += OnSpinChanged;
                        modal.SpeedChanged += OnSpeedChanged;
                    }
                }
            }
        }

        private void OnSettingsClose(object? sender, EventArgs e)
        {
            PlayButtonClickSound();
            var popup = this.FindControl<Popup>("SettingsPopup");
            if (popup != null) popup.IsOpen = false;
        }

        private void OnThemeChanged(object? sender, int themeIndex)
        {
            if (_background is BalatroShaderBackground shader)
            {
                shader.SetTheme(themeIndex);
            }
        }

        private void OnMusicVolumeChanged(object? sender, double volume)
        {
            var audioManager = ServiceHelper.GetService<VibeAudioManager>();
            audioManager?.SetMusicVolume((float)(volume / 100.0));
        }

        private void OnSfxVolumeChanged(object? sender, double volume)
        {
            var audioManager = ServiceHelper.GetService<VibeAudioManager>();
            audioManager?.SetSfxVolume((float)(volume / 100.0));
        }

        private void OnContrastChanged(object? sender, double contrast)
        {
            if (_background is BalatroShaderBackground shader)
            {
                shader.SetContrast((float)contrast);
            }
        }

        private void OnSpinChanged(object? sender, double spin)
        {
            if (_background is BalatroShaderBackground shader)
            {
                shader.SetSpinAmount((float)spin);
            }
        }

        private void OnSpeedChanged(object? sender, double speed)
        {
            if (_background is BalatroShaderBackground shader)
            {
                shader.SetSpeed((float)speed);
            }
        }

        /// <summary>
        /// Toggles the background animation on/off when the animation button is clicked
        /// </summary>
        private void OnAnimationToggleClick(object? sender, RoutedEventArgs e)
        {
            PlayButtonClickSound();
            // Toggle animation state
            _isAnimating = !_isAnimating;

            if (_background is BalatroShaderBackground bg)
            {
                bg.IsAnimating = _isAnimating;
            }
            else if (_background is BalatroStyleBackground bg2)
            {
                bg2.IsAnimating = _isAnimating;
            }

            // Update button icon based on animation state
            if (_animationButtonText != null)
            {
                _animationButtonText.Text = _isAnimating ? "⏸" : "▶";
            }
        }

        /// <summary>
        /// Opens volume slider popup
        /// </summary>
        private void OnMusicToggleClick(object? sender, RoutedEventArgs e)
        {
            PlayButtonClickSound();

            var popup = this.FindControl<Popup>("VolumePopup");
            if (popup != null)
            {
                popup.IsOpen = !popup.IsOpen;
            }
        }

        /// <summary>
        /// Volume slider changed - updates both music and SFX
        /// </summary>
        private void OnVolumeSliderChanged(object? sender, Avalonia.Controls.Primitives.RangeBaseValueChangedEventArgs e)
        {
            var slider = sender as Slider;
            if (slider == null) return;

            float volume = (float)(slider.Value / 100.0);

            var audioManager = ServiceHelper.GetService<VibeAudioManager>();
            if (audioManager != null)
            {
                audioManager.SetMasterVolume(volume);
                audioManager.SetSfxVolume(volume);
            }

            // Update percentage display
            var percentText = this.FindControl<TextBlock>("VolumePercentText");
            if (percentText != null)
            {
                percentText.Text = $"{(int)slider.Value}%";
            }

            // Update music button icon
            if (_musicToggleIcon != null)
            {
                _musicToggleIcon.Text = slider.Value > 0 ? "🔊" : "🔇";
            }

            // Update mute button text
            var muteButton = this.FindControl<Button>("MuteButton");
            if (muteButton != null)
            {
                muteButton.Content = slider.Value > 0 ? "MUTE" : "UNMUTE";
            }

            _isMusicPlaying = slider.Value > 0;
        }

        /// <summary>
        /// Mute button clicked - toggles between mute and previous volume
        /// </summary>
        private void OnMuteButtonClick(object? sender, RoutedEventArgs e)
        {
            PlayButtonClickSound();

            var slider = this.FindControl<Slider>("VolumeSlider");
            if (slider == null) return;

            if (slider.Value > 0)
            {
                // Store current volume and mute
                slider.Tag = slider.Value;
                slider.Value = 0;
            }
            else
            {
                // Restore previous volume or default to 70%
                slider.Value = slider.Tag is double storedValue ? storedValue : 70;
            }
        }

        /// <summary>
        /// Switches to edit mode for author name
        /// </summary>
        private void OnAuthorClick(object? sender, RoutedEventArgs e)
        {
            PlayButtonClickSound();
            var authorDisplay = this.FindControl<TextBlock>("AuthorDisplay");
            var authorEdit = this.FindControl<TextBox>("AuthorEdit");

            if (authorDisplay != null && authorEdit != null)
            {
                // Switch to edit mode
                authorDisplay.IsVisible = false;
                authorEdit.IsVisible = true;

                // Focus and select all text
                authorEdit.Focus();
                authorEdit.SelectAll();
            }

            e.Handled = true;
        }

        /// <summary>
        /// Save author name when edit loses focus
        /// </summary>
        private void OnAuthorEditLostFocus(object? sender, RoutedEventArgs e)
        {
            SaveAuthorName();
        }

        /// <summary>
        /// Handle Enter/Tab keys in author edit
        /// </summary>
        private void OnAuthorEditKeyDown(object? sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter || e.Key == Key.Tab)
            {
                SaveAuthorName();
                e.Handled = true;
            }
            else if (e.Key == Key.Escape)
            {
                // Cancel edit
                var authorDisplay = this.FindControl<TextBlock>("AuthorDisplay");
                var authorEdit = this.FindControl<TextBox>("AuthorEdit");

                if (authorDisplay != null && authorEdit != null && _userProfileService != null)
                {
                    // Restore original value
                    authorEdit.Text = _userProfileService.GetAuthorName();
                    authorDisplay.IsVisible = true;
                    authorEdit.IsVisible = false;
                }
                e.Handled = true;
            }
        }

        /// <summary>
        /// Save the author name and switch back to display mode
        /// </summary>
        private void SaveAuthorName()
        {
            var authorDisplay = this.FindControl<TextBlock>("AuthorDisplay");
            var authorEdit = this.FindControl<TextBox>("AuthorEdit");

            if (authorDisplay != null && authorEdit != null && _userProfileService != null)
            {
                var newName = authorEdit.Text?.Trim();
                if (!string.IsNullOrEmpty(newName))
                {
                    _userProfileService.SetAuthorName(newName);
                    authorDisplay.Text = newName;
                    BalatroSeedOracle.Helpers.DebugLogger.Log(
                        "BalatroMainMenu",
                        $"Author name updated to: {newName}"
                    );
                }

                // Switch back to display mode
                authorDisplay.IsVisible = true;
                authorEdit.IsVisible = false;
            }
        }

        private void OnLoaded(object? sender, RoutedEventArgs e)
        {
            // Initialize user profile service when control is loaded
            if (_userProfileService == null)
            {
                _userProfileService = ServiceHelper.GetService<UserProfileService>();
                if (_userProfileService == null)
                {
                    BalatroSeedOracle.Helpers.DebugLogger.LogError(
                        "BalatroMainMenu",
                        "UserProfileService is null after initialization attempt"
                    );
                    return;
                }
            }

            // Load and display current author name
            var authorDisplay = this.FindControl<TextBlock>("AuthorDisplay");
            var authorEdit = this.FindControl<TextBox>("AuthorEdit");
            if (authorDisplay != null && authorEdit != null)
            {
                var authorName = _userProfileService.GetAuthorName();
                BalatroSeedOracle.Helpers.DebugLogger.Log(
                    "BalatroMainMenu",
                    $"Setting author display to: '{authorName}'"
                );
                authorDisplay.Text = authorName;
                authorEdit.Text = authorName;
            }
            else
            {
                BalatroSeedOracle.Helpers.DebugLogger.LogError(
                    "BalatroMainMenu",
                    "Could not find AuthorDisplay or AuthorEdit controls"
                );
            }
            
            // Check for resumable search and restore desktop icon if needed
            CheckAndRestoreSearchIcon();
        }

        /// <summary>
        /// Check for resumable search state and restore desktop icon if needed
        /// </summary>
        private void CheckAndRestoreSearchIcon()
        {
            try
            {
                if (_userProfileService?.GetSearchState() is { } resumeState)
                {
                    // Check if the search is recent (within last 24 hours)
                    var timeSinceSearch = DateTime.UtcNow - resumeState.LastActiveTime;
                    if (timeSinceSearch.TotalHours > 24)
                    {
                        // Too old, clear it
                        _userProfileService.ClearSearchState();
                        return;
                    }

                    BalatroSeedOracle.Helpers.DebugLogger.Log(
                        "BalatroMainMenu",
                        $"Found resumable search state from {timeSinceSearch.TotalMinutes:F0} minutes ago"
                    );

                    // DON'T create a search instance here! Just show the icon
                    // The search will be created when the user actually clicks the icon
                    if (!string.IsNullOrEmpty(resumeState.ConfigPath) && File.Exists(resumeState.ConfigPath))
                    {
                        // Create a placeholder search ID for the icon
                        // The actual search will be created when the icon is clicked
                        var placeholderSearchId = Guid.NewGuid().ToString();
                        ShowSearchDesktopIcon(placeholderSearchId, resumeState.ConfigPath);
                        
                        BalatroSeedOracle.Helpers.DebugLogger.Log(
                            "BalatroMainMenu",
                            $"Restored desktop icon for search (not started yet): {resumeState.ConfigPath}"
                        );
                    }
                    else
                    {
                        BalatroSeedOracle.Helpers.DebugLogger.Log(
                            "BalatroMainMenu",
                            $"Skipping desktop icon for resumable search - invalid config path: {resumeState.ConfigPath}"
                        );
                        _userProfileService.ClearSearchState();
                    }
                    
                }
            }
            catch (Exception ex)
            {
                BalatroSeedOracle.Helpers.DebugLogger.LogError(
                    "BalatroMainMenu",
                    $"Error checking for resumable search: {ex.Message}"
                );
            }
        }

        /// <summary>
        /// Shows a search desktop icon on the desktop
        /// </summary>
        /// <summary>
        /// Shows the search modal for an existing search instance (opened from desktop icon)
        /// </summary>
        public void ShowSearchModalForInstance(string searchId, string? configPath = null)
        {
            try
            {
                DebugLogger.Log("BalatroMainMenu", $"ShowSearchModalForInstance called - SearchId: {searchId}, ConfigPath: {configPath}");
                
                var searchContent = new SearchModal();
                
                // Connect to the existing search instance
                searchContent.ConnectToExistingSearch(searchId);
                
                // If we have a config path, load it
                if (!string.IsNullOrEmpty(configPath))
                {
                    _ = searchContent.LoadFilterAsync(configPath);
                }
                
                // Handle desktop icon creation when modal closes with active search
                searchContent.CreateShortcutRequested += (sender, cfgPath) => 
                {
                    DebugLogger.Log("BalatroMainMenu", $"Desktop icon requested for config: {cfgPath}");
                    // Get the search ID from the modal
                    var modalSearchId = searchContent.GetCurrentSearchId();
                    if (!string.IsNullOrEmpty(modalSearchId))
                    {
                        ShowSearchDesktopIcon(modalSearchId, cfgPath);
                    }
                };
                
                this.ShowModal("MOTELY SEARCH", searchContent);
            }
            catch (Exception ex)
            {
                DebugLogger.LogError("BalatroMainMenu", $"Failed to show search modal for instance: {ex}");
            }
        }
        
        public void ShowSearchDesktopIcon(string searchId, string? configPath = null)
        {
            BalatroSeedOracle.Helpers.DebugLogger.Log(
                "BalatroMainMenu",
                $"ShowSearchDesktopIcon called with searchId: {searchId}, config: {configPath}"
            );

            // Get the desktop canvas
            var desktopCanvas = this.FindControl<Grid>("DesktopCanvas");
            if (desktopCanvas == null)
            {
                BalatroSeedOracle.Helpers.DebugLogger.Log("BalatroMainMenu", "DesktopCanvas not found!");
                return;
            }

            // Create a new SearchDesktopIcon instance
            var searchIcon = new SearchDesktopIcon();

            // Get filter name from config path
            string filterName = "Unknown Filter";
            if (!string.IsNullOrEmpty(configPath) && File.Exists(configPath))
            {
                filterName = Path.GetFileNameWithoutExtension(configPath);
            }
            else
            {
                // Don't create desktop icons for unknown filters
                BalatroSeedOracle.Helpers.DebugLogger.Log(
                    "BalatroMainMenu",
                    $"Skipping desktop icon creation - no valid config path"
                );
                return;
            }

            searchIcon.Initialize(searchId, configPath ?? string.Empty, filterName);

            // Calculate position based on existing icons
            var leftMargin = 20 + (_widgetCounter % 8) * 120; // 8 icons per row, 120px apart
            var topMargin = 20 + (_widgetCounter / 8) * 140; // Stack rows, 140px apart

            searchIcon.HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Left;
            searchIcon.VerticalAlignment = Avalonia.Layout.VerticalAlignment.Top;
            searchIcon.Margin = new Thickness(leftMargin, topMargin, 0, 0);
            searchIcon.IsVisible = true;

            // Add to the desktop canvas
            desktopCanvas.Children.Add(searchIcon);
            _widgetCounter++;

            BalatroSeedOracle.Helpers.DebugLogger.Log(
                "BalatroMainMenu",
                $"Created SearchDesktopIcon #{_widgetCounter} at position ({leftMargin}, {topMargin})"
            );

            // DON'T auto-start searches! The user will start them manually
            // The icon is just a placeholder for resuming later
        }

        /// <summary>
        /// Updates the visibility of the search desktop icon based on minimized widget count
        /// </summary>
        private void UpdateSearchDesktopIconVisibility()
        {
            if (_searchDesktopIcon != null)
            {
                _searchDesktopIcon.IsVisible = _minimizedWidgetCount > 0;
            }
        }

        /// <summary>
        /// Removes a search desktop icon from the desktop
        /// </summary>
        public void RemoveSearchDesktopIcon(string searchId)
        {
            BalatroSeedOracle.Helpers.DebugLogger.Log(
                "BalatroMainMenu",
                $"RemoveSearchDesktopIcon called for searchId: {searchId}"
            );

            var desktopCanvas = this.FindControl<Grid>("DesktopCanvas");
            if (desktopCanvas == null)
            {
                BalatroSeedOracle.Helpers.DebugLogger.Log("BalatroMainMenu", "DesktopCanvas not found!");
                return;
            }

            // Find and remove the icon with matching searchId
            SearchDesktopIcon? iconToRemove = null;
            foreach (var child in desktopCanvas.Children)
            {
                if (child is SearchDesktopIcon icon)
                {
                    // Check if this icon matches the searchId
                    // We'll need to add a property to SearchDesktopIcon to get its searchId
                    var searchIdProperty = icon.GetType().GetField("_searchId", 
                        System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                    if (searchIdProperty != null)
                    {
                        var iconSearchId = searchIdProperty.GetValue(icon) as string;
                        if (iconSearchId == searchId)
                        {
                            iconToRemove = icon;
                            break;
                        }
                    }
                }
            }

            if (iconToRemove != null)
            {
                desktopCanvas.Children.Remove(iconToRemove);
                BalatroSeedOracle.Helpers.DebugLogger.Log(
                    "BalatroMainMenu",
                    $"Removed SearchDesktopIcon for searchId: {searchId}"
                );
            }
            else
            {
                BalatroSeedOracle.Helpers.DebugLogger.Log(
                    "BalatroMainMenu",
                    $"No SearchDesktopIcon found for searchId: {searchId}"
                );
            }
        }

        /// <summary>
        /// Stops all running searches - called during application shutdown
        /// </summary>
        public Task StopAllSearchesAsync()
        {
            DebugLogger.LogImportant("BalatroMainMenu", "Stopping all searches...");

            // Search widgets removed - using desktop icons now

            // Check if there's a filters modal open with a search running
            if (_modalContainer != null && _modalContainer.Children.Count > 0)
            {
                var modal = _modalContainer.Children[0] as StandardModal;
                if (modal != null)
                {
                    // Find the ModalContent presenter inside StandardModal
                    var modalContent = modal.FindControl<ContentPresenter>("ModalContent");
                    var filtersModal = modalContent?.Content as FiltersModal;
                    if (filtersModal != null)
                    {
                        // FiltersModal may have active searches to stop
                        DebugLogger.LogImportant(
                            "BalatroMainMenu",
                            "Checking FiltersModal for active searches..."
                        );
                        // Note: FiltersModal doesn't have a StopSearch method
                        // but we keep this structure in case it's needed later
                    }
                }
            }

            DebugLogger.LogImportant("BalatroMainMenu", "All searches stopped");
            return Task.CompletedTask;
        }

        /// <summary>
        /// Enter VibeOut mode - fullscreen background with ESC overlay
        /// </summary>
        public void EnterVibeOutMode()
        {
            if (_isVibeOutMode) return;

            _isVibeOutMode = true;

            // Hide main menu UI
            if (_mainContent != null) _mainContent.IsVisible = false;
            if (_modalContainer != null) _modalContainer.IsVisible = false;

            // Show VibeOut overlay
            if (_vibeOutOverlay != null) _vibeOutOverlay.IsVisible = true;

            // Set background to VibeOut theme
            if (_background is BalatroShaderBackground shader)
            {
                shader.Theme = BalatroShaderBackground.BackgroundTheme.VibeOut;
            }

            // Optional: Request window maximize (if we have access to window)
            var window = this.VisualRoot as Window;
            if (window != null)
            {
                window.WindowState = WindowState.Maximized;
            }

            DebugLogger.Log("BalatroMainMenu", "🎵 Entered VibeOut mode");
        }

        /// <summary>
        /// Exit VibeOut mode
        /// </summary>
        public void ExitVibeOutMode()
        {
            if (!_isVibeOutMode) return;

            _isVibeOutMode = false;

            // Show main menu UI
            if (_mainContent != null) _mainContent.IsVisible = true;

            // Hide VibeOut overlay
            if (_vibeOutOverlay != null) _vibeOutOverlay.IsVisible = false;

            // Restore default theme
            if (_background is BalatroShaderBackground shader)
            {
                shader.Theme = BalatroShaderBackground.BackgroundTheme.Default;
            }

            DebugLogger.Log("BalatroMainMenu", "👋 Exited VibeOut mode");
        }

        private void OnExitVibeOut(object? sender, RoutedEventArgs e)
        {
            ExitVibeOutMode();
        }

        protected override void OnKeyDown(KeyEventArgs e)
        {
            base.OnKeyDown(e);

            // ESC to exit VibeOut mode
            if (e.Key == Key.Escape && _isVibeOutMode)
            {
                ExitVibeOutMode();
                e.Handled = true;
            }
        }
    }
}

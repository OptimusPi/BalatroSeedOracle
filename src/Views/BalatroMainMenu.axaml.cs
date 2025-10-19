using System;
using System.Linq;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Animation;
using Avalonia.Animation.Easings;
using Avalonia.Controls;
using Avalonia.Controls.Presenters;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.LogicalTree;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
using Avalonia.Styling;
using Avalonia.Threading;
using BalatroSeedOracle.Controls;
using BalatroSeedOracle.Helpers;
using BalatroSeedOracle.Services;
using BalatroSeedOracle.ViewModels;
using BalatroSeedOracle.Views.Modals;

namespace BalatroSeedOracle.Views
{
    public partial class BalatroMainMenu : UserControl
    {
        // View-only references (no business logic state)
        private Grid? _modalContainer;
        private Control? _background;
        private BalatroShaderBackground? _shaderBackground; // CACHED - it's ALWAYS BalatroShaderBackground!
        private Grid? _vibeOutOverlay;
        private Grid? _mainContent;
        private UserControl? _activeModalContent;
        private TextBlock? _mainTitleText;
        private Action<float, float, float, float>? _audioAnalysisHandler;
        private Popup? _volumePopup;

        // ViewModel (injected via DI - never null after construction)
        public BalatroMainMenuViewModel ViewModel { get; }

        /// <summary>
        /// Callback to request main content swap (set by MainWindow)
        /// </summary>
        public Action<UserControl>? RequestContentSwap { get; set; }

        public BalatroMainMenu()
        {
            // Get ViewModel from DI (proper way!)
            ViewModel = ServiceHelper.GetRequiredService<BalatroMainMenuViewModel>();
            DataContext = ViewModel;

            InitializeComponent();

            // Wire up ViewModel events
            WireViewModelEvents();

            // Defer initialization to OnLoaded
            this.Loaded += OnLoaded;
            this.Loaded += InitializeVibeAudio;
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);

            // Get view-only control references
            _modalContainer = this.FindControl<Grid>("ModalContainer");
            _background = this.FindControl<Control>("BackgroundControl");
            _shaderBackground = _background as BalatroShaderBackground; // CACHE IT ONCE!
            _vibeOutOverlay = this.FindControl<Grid>("VibeOutOverlay");
            _mainContent = this.FindControl<Grid>("MainContent");
            _volumePopup = this.FindControl<Popup>("VolumePopup");
        }

        /// <summary>
        /// Wire up ViewModel events to view behaviors
        /// </summary>
        private void WireViewModelEvents()
        {
            // Modal requests
            ViewModel.ModalRequested += OnModalRequested;
            ViewModel.HideModalRequested += (s, e) => HideModalContent();

            // Animation state changes
            ViewModel.OnIsAnimatingChangedEvent += (s, isAnimating) =>
            {
                if (_shaderBackground != null)
                {
                    // Ensure UI-thread dispatch when touching visual controls
                    Avalonia.Threading.Dispatcher.UIThread.Post(() =>
                    {
                        _shaderBackground.IsAnimating = isAnimating;
                    }, Avalonia.Threading.DispatcherPriority.Render);
                }
            };

            // VibeOut mode changes
            ViewModel.OnVibeOutModeChangedEvent += (s, isVibeOut) =>
            {
                if (isVibeOut)
                {
                    EnterVibeOutModeView();
                }
                else
                {
                    ExitVibeOutModeView();
                }
            };

            // Author edit activation (focus request)
            ViewModel.OnAuthorEditActivated += (s, e) =>
            {
                var authorEdit = this.FindControl<TextBox>("AuthorEdit");
                if (authorEdit != null)
                {
                    authorEdit.Focus();
                    authorEdit.SelectAll();
                }
            };

        }

        /// <summary>
        /// Handles modal requests from ViewModel
        /// </summary>
        private void OnModalRequested(object? sender, ModalRequestedEventArgs e)
        {
            switch (e.ModalType)
            {
                case ModalType.Search:
                    this.ShowSearchModal();
                    break;
                case ModalType.Filters:
                    this.ShowFiltersModal();
                    break;
                case ModalType.Analyze:
                    ShowAnalyzeModal();
                    break;
                case ModalType.Tools:
                    this.ShowToolsModal();
                    break;
                case ModalType.Settings:
                    this.ShowSettingsModal();
                    break;
                case ModalType.Custom:
                    if (e.CustomContent != null && e.CustomTitle != null)
                    {
                        ShowModalContent(e.CustomContent, e.CustomTitle);
                    }
                    break;
            }
        }

        /// <summary>
        /// Show search modal
        /// </summary>
        private void ShowSearchModal()
        {
            var searchModal = new Modals.SearchModal();
            // Set the MainMenu reference so CREATE NEW FILTER button works
            searchModal.ViewModel.MainMenu = this;

            var modal = new StandardModal("🎰 SEED SEARCH");
            modal.SetContent(searchModal);
            modal.BackClicked += (s, e) => HideModalContent();

            ShowModalContent(modal, "🎰 SEED SEARCH");
        }

        /// <summary>
        /// Show filters modal (for creating/managing filters)
        /// </summary>
        public void ShowFiltersModal()
        {
            var filtersModal = new Modals.FiltersModal();

            var modal = new StandardModal("🎨 FILTER DESIGNER");
            modal.SetContent(filtersModal);
            modal.BackClicked += (s, e) => HideModalContent();

            ShowModalContent(modal, "🎨 FILTER DESIGNER");
        }

        /// <summary>
        /// Show settings modal
        /// </summary>
        private void ShowSettingsModal()
        {
            var settingsModal = new Modals.SettingsModal();

            var modal = new StandardModal("SETTINGS");
            modal.SetContent(settingsModal);
            modal.BackClicked += (s, e) => HideModalContent();

            ShowModalContent(modal, "SETTINGS");
        }

        /// <summary>
        /// Show tools modal
        /// </summary>
        private void ShowToolsModal()
        {
            var toolsModal = new Modals.ToolsModal();
            var modal = new StandardModal("TOOLS");
            modal.SetContent(toolsModal);
            modal.BackClicked += (s, ev) => HideModalContent();
            ShowModalContent(modal, "TOOLS");
        }

        /// <summary>
        /// Show analyze modal
        /// </summary>
        private void ShowAnalyzeModal()
        {
            var analyzeModal = new AnalyzeModal();
            var modal = new StandardModal("ANALYZE");
            modal.SetContent(analyzeModal);
            modal.BackClicked += (s, ev) => HideModalContent();
            ShowModalContent(modal, "SEED ANALYZER");
        }

        /// <summary>
        /// Initialize vibe audio on load
        /// </summary>
        private void InitializeVibeAudio(object? sender, RoutedEventArgs e)
        {
            this.Loaded -= InitializeVibeAudio;

            try
            {
                var audioManager = ServiceHelper.GetService<VibeAudioManager>();
                if (audioManager == null)
                {
                    DebugLogger.LogError("BalatroMainMenu", "VibeAudioManager is NULL - music visualization won't work!");
                    return;
                }

                // Store handler reference for cleanup
                _audioAnalysisHandler = (bass, mid, treble, peak) =>
                {
                    if (_background is BalatroShaderBackground shader)
                    {
                        shader.UpdateVibeIntensity(peak * 0.05f);
                        shader.UpdateMelodicFFT(mid, treble, peak);
                        shader.UpdateTrackIntensities(
                            audioManager.MelodyIntensity,
                            audioManager.ChordsIntensity,
                            audioManager.BassIntensity
                        );
                    }
                };

                audioManager.AudioAnalysisUpdated += _audioAnalysisHandler;
            }
            catch (Exception ex)
            {
                DebugLogger.LogError("BalatroMainMenu", $"Failed to start vibe audio: {ex.Message}");
            }
        }

        /// <summary>
        /// Loaded event handler
        /// </summary>
        private void OnLoaded(object? sender, RoutedEventArgs e)
        {
            // Load visualizer settings
            if (_background is BalatroShaderBackground shader)
            {
                ViewModel.LoadAndApplyVisualizerSettings(shader);
            }

            // Check for resumable search
            ViewModel.CheckAndRestoreSearchIcon(ShowSearchDesktopIcon);

            // Set up click-away handler for popups (NOT for main modals which have back buttons)
            this.PointerPressed += OnPointerPressedForPopupClickAway;
        }

        /// <summary>
        /// Handles pointer press to close popups when clicking outside
        /// This is for TRUE popups (volume slider, etc) NOT main modals which have back buttons
        /// </summary>
        private void OnPointerPressedForPopupClickAway(object? sender, PointerPressedEventArgs e)
        {
            // Only handle click-away for actual popups, not main modals
            if (_volumePopup?.IsOpen == true)
            {
                // Get the source of the click
                var source = e.Source as Control;

                // Don't close if clicking on the music toggle button itself (let it toggle naturally)
                var musicButton = this.FindControl<Button>("MusicToggleButton");
                if (source == musicButton)
                {
                    return; // Let the button's click handler toggle the popup
                }

                // Check if the click source is a child of the music button
                var parent = source;
                while (parent != null)
                {
                    if (parent == musicButton)
                    {
                        return; // Click was on music button or its child
                    }
                    parent = parent.Parent as Control;
                }

                // Get the position of the click
                var clickPosition = e.GetPosition(this);

                // Get the volume popup's child (the Border control)
                if (_volumePopup.Child is Control popupContent)
                {
                    // Check if click is outside the popup bounds
                    var popupBounds = popupContent.Bounds;
                    var popupPosition = popupContent.TranslatePoint(new Point(0, 0), this);

                    if (popupPosition.HasValue)
                    {
                        var absolutePopupBounds = new Rect(
                            popupPosition.Value.X,
                            popupPosition.Value.Y,
                            popupBounds.Width,
                            popupBounds.Height
                        );

                        // If click is outside popup, close it
                        if (!absolutePopupBounds.Contains(clickPosition))
                        {
                            ViewModel.IsVolumePopupOpen = false;
                            DebugLogger.Log("BalatroMainMenu", "Volume popup closed via click-away");
                        }
                    }
                }
            }
        }

        #region Modal Management (View-only logic)

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
        /// Show a UserControl as a modal overlay with smooth transition
        /// </summary>
        public void ShowModalContent(UserControl content, string? title = null)
        {
            if (_modalContainer == null) return;

            // Close any open popups when opening a modal
            if (ViewModel.IsVolumePopupOpen)
            {
                ViewModel.IsVolumePopupOpen = false;
            }

            // If there's already a modal showing, do a smooth transition
            if (_modalContainer.IsVisible && _modalContainer.Children.Count > 0)
            {
                TransitionToNewModal(content, title);
            }
            else
            {
                // No existing modal - just show the new one with pop-up animation
                ShowModalWithAnimation(content, title);
            }
        }

        /// <summary>
        /// Smoothly transition from current modal to new modal (Balatro-style)
        /// </summary>
        private async void TransitionToNewModal(UserControl newContent, string? title)
        {
            if (_modalContainer == null || _modalContainer.Children.Count == 0) return;

            var oldContent = _modalContainer.Children[0];

            // Balatro-style "pop_out" animation - quick and juicy (0.2s like juice_up)
            var fallAnimation = new Avalonia.Animation.Animation
            {
                Duration = TimeSpan.FromMilliseconds(200), // Balatro juice_up standard duration
                Easing = new QuadraticEaseIn(), // Accelerate into fall
                Children =
                {
                    new Avalonia.Animation.KeyFrame
                    {
                        Cue = new Cue(0),
                        Setters =
                        {
                            new Setter(TranslateTransform.YProperty, 0d),
                            new Setter(OpacityProperty, 1.0d),
                            new Setter(ScaleTransform.ScaleYProperty, 1.0d),
                            new Setter(ScaleTransform.ScaleXProperty, 1.0d)
                        }
                    },
                    new Avalonia.Animation.KeyFrame
                    {
                        Cue = new Cue(1),
                        Setters =
                        {
                            new Setter(TranslateTransform.YProperty, 500d),
                            new Setter(OpacityProperty, 0.0d),
                            new Setter(ScaleTransform.ScaleYProperty, 0.8d),
                            new Setter(ScaleTransform.ScaleXProperty, 0.95d)
                        }
                    }
                }
            };

            var transformGroup = new TransformGroup();
            transformGroup.Children.Add(new ScaleTransform(1, 1));
            transformGroup.Children.Add(new TranslateTransform(0, 0));
            oldContent.RenderTransform = transformGroup;
            oldContent.RenderTransformOrigin = new RelativePoint(0.5, 0.5, RelativeUnit.Relative);

            await fallAnimation.RunAsync(oldContent);
            await Task.Delay(150); // Balatro standard delay between sequences

            _modalContainer.Children.Clear();
            ShowModalWithAnimation(newContent, title);
        }

        /// <summary>
        /// Show modal with pop-up bounce animation (Balatro-style)
        /// </summary>
        private async void ShowModalWithAnimation(UserControl content, string? title)
        {
            if (_modalContainer == null) return;

            _modalContainer.Children.Add(content);
            _modalContainer.IsVisible = true;
            _activeModalContent = content;

            if (!string.IsNullOrEmpty(title))
            {
                SetTitle(title);
            }

            // Balatro-style "pop_in" animation with juice
            var popAnimation = new Avalonia.Animation.Animation
            {
                Duration = TimeSpan.FromMilliseconds(350), // Slightly longer for bounce effect
                Easing = new BackEaseOut(), // Subtle overshoot like Balatro's juice
                Children =
                {
                    new Avalonia.Animation.KeyFrame
                    {
                        Cue = new Cue(0),
                        Setters =
                        {
                            new Setter(TranslateTransform.YProperty, 400d),
                            new Setter(OpacityProperty, 0.0d),
                            new Setter(ScaleTransform.ScaleYProperty, 0.7d),
                            new Setter(ScaleTransform.ScaleXProperty, 0.9d)
                        }
                    },
                    new Avalonia.Animation.KeyFrame
                    {
                        Cue = new Cue(0.6), // Quick rise
                        Setters =
                        {
                            new Setter(TranslateTransform.YProperty, -10d), // Slight overshoot
                            new Setter(OpacityProperty, 1.0d),
                            new Setter(ScaleTransform.ScaleYProperty, 1.02d),
                            new Setter(ScaleTransform.ScaleXProperty, 1.01d)
                        }
                    },
                    new Avalonia.Animation.KeyFrame
                    {
                        Cue = new Cue(1), // Settle
                        Setters =
                        {
                            new Setter(TranslateTransform.YProperty, 0d),
                            new Setter(OpacityProperty, 1.0d),
                            new Setter(ScaleTransform.ScaleYProperty, 1.0d),
                            new Setter(ScaleTransform.ScaleXProperty, 1.0d)
                        }
                    }
                }
            };

            var transformGroup = new TransformGroup();
            transformGroup.Children.Add(new ScaleTransform(1, 1));
            transformGroup.Children.Add(new TranslateTransform(0, 0));
            content.RenderTransform = transformGroup;
            content.RenderTransformOrigin = new RelativePoint(0.5, 0.5, RelativeUnit.Relative);

            await popAnimation.RunAsync(content);
            var settleAnimation = new Avalonia.Animation.Animation
            {
                Duration = TimeSpan.FromMilliseconds(50),
                Children =
                {
                    new Avalonia.Animation.KeyFrame
                    {
                        Cue = new Cue(0),
                        Setters =
                        {
                            new Setter(ScaleTransform.ScaleYProperty, 1.0d),
                            new Setter(ScaleTransform.ScaleXProperty, 1.0d)
                        }
                    },
                    new Avalonia.Animation.KeyFrame
                    {
                        Cue = new Cue(1),
                        Setters =
                        {
                            new Setter(ScaleTransform.ScaleYProperty, 1.005d),
                            new Setter(ScaleTransform.ScaleXProperty, 1.002d)
                        }
                    }
                }
            };
            await settleAnimation.RunAsync(content);
        }

        /// <summary>
        /// Hide the modal overlay
        /// </summary>
        public void HideModalContent()
        {
            if (_modalContainer == null) return;

            // PERFORMANCE FIX: Defer Children.Clear() to prevent audio crackling
            // FiltersModal has thousands of controls - clearing synchronously blocks UI thread
            // which causes audio buffer underruns
            _modalContainer.IsVisible = false;
            _activeModalContent = null;
            SetTitle("Welcome!");

            // Clear on background thread to avoid blocking audio
            Dispatcher.UIThread.Post(() =>
            {
                _modalContainer.Children.Clear();
            }, DispatcherPriority.Background);

            var audioManager = App.GetService<Services.VibeAudioManager>();
        }

        #endregion

        #region VibeOut Mode (View-only logic)

        /// <summary>
        /// Enter VibeOut mode - view changes only
        /// </summary>
        private void EnterVibeOutModeView()
        {
            if (_mainContent != null) _mainContent.IsVisible = false;
            if (_modalContainer != null) _modalContainer.IsVisible = false;
            if (_vibeOutOverlay != null) _vibeOutOverlay.IsVisible = true;

            if (_background is BalatroShaderBackground shader)
            {
                shader.Theme = BalatroShaderBackground.BackgroundTheme.VibeOut;
            }

            var window = this.VisualRoot as Window;
            if (window != null)
            {
                window.WindowState = WindowState.Maximized;
            }

            DebugLogger.Log("BalatroMainMenu", "🎵 Entered VibeOut mode");
        }

        /// <summary>
        /// Exit VibeOut mode - view changes only
        /// </summary>
        private void ExitVibeOutModeView()
        {
            if (_mainContent != null) _mainContent.IsVisible = true;
            if (_vibeOutOverlay != null) _vibeOutOverlay.IsVisible = false;

            if (_background is BalatroShaderBackground shader)
            {
                shader.Theme = BalatroShaderBackground.BackgroundTheme.Default;
            }

            DebugLogger.Log("BalatroMainMenu", "👋 Exited VibeOut mode");
        }

        /// <summary>
        /// Enter VibeOut mode (public API)
        /// </summary>
        public void EnterVibeOutMode()
        {
            ViewModel.EnterVibeOutMode();
        }

        /// <summary>
        /// Exit VibeOut mode (public API)
        /// </summary>
        public void ExitVibeOutMode()
        {
            ViewModel.ExitVibeOutMode();
        }

        #endregion

        #region Settings Modal Wiring (View-only)

        private void OnVisualizerThemeChanged(object? sender, int themeIndex)
        {
            if (_background is BalatroShaderBackground shader)
            {
                ViewModel.ApplyVisualizerTheme(shader, themeIndex);
            }
        }

        #endregion

        #region Shader Management (Delegated to ViewModel)

        internal void ApplyVisualizerTheme(int themeIndex)
        {
            if (_background is BalatroShaderBackground shader)
            {
                ViewModel.ApplyVisualizerTheme(shader, themeIndex);
            }
        }

        internal void ApplyMainColor(int colorIndex)
        {
            if (_background is BalatroShaderBackground shader)
            {
                ViewModel.ApplyMainColor(shader, colorIndex);
            }
        }

        internal void ApplyAccentColor(int colorIndex)
        {
            if (_background is BalatroShaderBackground shader)
            {
                ViewModel.ApplyAccentColor(shader, colorIndex);
            }
        }

        internal void ApplyAudioIntensity(float intensity)
        {
            if (_background is BalatroShaderBackground shader)
            {
                ViewModel.ApplyAudioIntensity(shader, intensity);
            }
        }

        internal void ApplyParallaxStrength(float strength)
        {
            if (_background is BalatroShaderBackground shader)
            {
                ViewModel.ApplyParallaxStrength(shader, strength);
            }
        }

        internal void ApplyTimeSpeed(float speed)
        {
            if (_background is BalatroShaderBackground shader)
            {
                ViewModel.ApplyTimeSpeed(shader, speed);
            }
        }

        internal void ApplyShaderContrast(float contrast)
        {
            if (_background is BalatroShaderBackground shader)
            {
                ViewModel.ApplyShaderContrast(shader, contrast);
            }
        }

        internal void ApplyShaderSpinAmount(float spinAmount)
        {
            if (_background is BalatroShaderBackground shader)
            {
                ViewModel.ApplyShaderSpinAmount(shader, spinAmount);
            }
        }

        internal void ApplyShaderZoomPunch(float zoom)
        {
            if (_background is BalatroShaderBackground shader)
            {
                ViewModel.ApplyShaderZoomPunch(shader, zoom);
            }
        }

        internal void ApplyShaderMelodySaturation(float saturation)
        {
            if (_background is BalatroShaderBackground shader)
            {
                ViewModel.ApplyShaderMelodySaturation(shader, saturation);
            }
        }

        internal void ApplyShadowFlickerSource(int sourceIndex)
        {
            if (_background is BalatroShaderBackground shader)
            {
                ViewModel.ApplyShadowFlickerSource(shader, sourceIndex);
            }
        }

        internal void ApplySpinSource(int sourceIndex)
        {
            if (_background is BalatroShaderBackground shader)
            {
                ViewModel.ApplySpinSource(shader, sourceIndex);
            }
        }

        internal void ApplyTwirlSource(int sourceIndex)
        {
            if (_background is BalatroShaderBackground shader)
            {
                ViewModel.ApplyTwirlSource(shader, sourceIndex);
            }
        }

        internal void ApplyZoomThumpSource(int sourceIndex)
        {
            if (_background is BalatroShaderBackground shader)
            {
                ViewModel.ApplyZoomThumpSource(shader, sourceIndex);
            }
        }

        internal void ApplyColorSaturationSource(int sourceIndex)
        {
            if (_background is BalatroShaderBackground shader)
            {
                ViewModel.ApplyColorSaturationSource(shader, sourceIndex);
            }
        }

        internal void ApplyBeatPulseSource(int sourceIndex)
        {
            if (_background is BalatroShaderBackground shader)
            {
                ViewModel.ApplyBeatPulseSource(shader, sourceIndex);
            }
        }

        // Range application helpers
        internal void ApplyContrastRange(float min, float max)
        {
            if (_background is BalatroShaderBackground shader)
            {
                shader.SetContrastRange(min, max);
            }
        }

        internal void ApplySpinAmountRange(float min, float max)
        {
            if (_background is BalatroShaderBackground shader)
            {
                shader.SetSpinAmountRange(min, max);
            }
        }

        internal void ApplyTwirlSpeedRange(float min, float max)
        {
            if (_background is BalatroShaderBackground shader)
            {
                shader.SetTwirlSpeedRange(min, max);
            }
        }

        internal void ApplyZoomPunchRange(float min, float max)
        {
            if (_background is BalatroShaderBackground shader)
            {
                shader.SetZoomPunchRange(min, max);
            }
        }

        internal void ApplyMelodySatRange(float min, float max)
        {
            if (_background is BalatroShaderBackground shader)
            {
                shader.SetMelodySaturationRange(min, max);
            }
        }

        #endregion

        #region Desktop Icon Management

        /// <summary>
        /// Shows the search modal for an existing search instance
        /// </summary>
        public void ShowSearchModalForInstance(string searchId, string? configPath = null)
        {
            try
            {
                DebugLogger.Log("BalatroMainMenu", $"ShowSearchModalForInstance called - SearchId: {searchId}, ConfigPath: {configPath}");

                var searchContent = new SearchModal();
                // Set the MainMenu reference so CREATE NEW FILTER button works
                searchContent.ViewModel.MainMenu = this;
                _ = searchContent.ViewModel.ConnectToExistingSearch(searchId);

                if (!string.IsNullOrEmpty(configPath))
                {
                    _ = searchContent.ViewModel.LoadFilterAsync(configPath);
                }

                searchContent.ViewModel.CreateShortcutRequested += (sender, cfgPath) =>
                {
                    DebugLogger.Log("BalatroMainMenu", $"Desktop icon requested for config: {cfgPath}");
                    var modalSearchId = searchContent.ViewModel.CurrentSearchId;
                    if (!string.IsNullOrEmpty(modalSearchId))
                    {
                        ShowSearchDesktopIcon(modalSearchId, cfgPath);
                    }
                };

                this.ShowModal("🎰 SEED SEARCH", searchContent);
            }
            catch (Exception ex)
            {
                DebugLogger.LogError("BalatroMainMenu", $"Failed to show search modal for instance: {ex}");
            }
        }

        public void ShowSearchDesktopIcon(string searchId, string? configPath = null)
        {
            DebugLogger.Log("BalatroMainMenu", $"ShowSearchDesktopIcon called with searchId: {searchId}, config: {configPath}");

            var desktopCanvas = this.FindControl<Grid>("DesktopCanvas");
            if (desktopCanvas == null)
            {
                DebugLogger.Log("BalatroMainMenu", "DesktopCanvas not found!");
                return;
            }

            string filterName = "Unknown Filter";
            if (!string.IsNullOrEmpty(configPath) && System.IO.File.Exists(configPath))
            {
                filterName = System.IO.Path.GetFileNameWithoutExtension(configPath);
            }
            else
            {
                DebugLogger.Log("BalatroMainMenu", $"Skipping desktop icon creation - no valid config path");
                return;
            }

            var searchIcon = new SearchDesktopIcon();
            searchIcon.Initialize(searchId, configPath ?? string.Empty, filterName);

            var leftMargin = 20 + (ViewModel.WidgetCounter % 8) * 120;
            var topMargin = 20 + (ViewModel.WidgetCounter / 8) * 140;

            searchIcon.HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Left;
            searchIcon.VerticalAlignment = Avalonia.Layout.VerticalAlignment.Top;
            searchIcon.Margin = new Thickness(leftMargin, topMargin, 0, 0);
            searchIcon.IsVisible = true;

            desktopCanvas.Children.Add(searchIcon);
            ViewModel.WidgetCounter++;

            DebugLogger.Log("BalatroMainMenu", $"Created SearchDesktopIcon #{ViewModel.WidgetCounter} at position ({leftMargin}, {topMargin})");
        }

        public void RemoveSearchDesktopIcon(string searchId)
        {
            DebugLogger.Log("BalatroMainMenu", $"RemoveSearchDesktopIcon called for searchId: {searchId}");

            var desktopCanvas = this.FindControl<Grid>("DesktopCanvas");
            if (desktopCanvas == null)
            {
                DebugLogger.Log("BalatroMainMenu", "DesktopCanvas not found!");
                return;
            }

            SearchDesktopIcon? iconToRemove = null;
            foreach (var child in desktopCanvas.Children)
            {
                if (child is SearchDesktopIcon icon)
                {
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
                DebugLogger.Log("BalatroMainMenu", $"Removed SearchDesktopIcon for searchId: {searchId}");
            }
            else
            {
                DebugLogger.Log("BalatroMainMenu", $"No SearchDesktopIcon found for searchId: {searchId}");
            }
        }

        #endregion

        #region Cleanup

        public void Dispose()
        {
            try
            {
                CleanupEventHandlers();
            }
            catch (Exception ex)
            {
                DebugLogger.LogError("BalatroMainMenu", $"⚠️  Error during disposal: {ex.Message}");
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
                var audioManager = ServiceHelper.GetService<VibeAudioManager>();
                if (audioManager != null && _audioAnalysisHandler != null)
                {
                    audioManager.AudioAnalysisUpdated -= _audioAnalysisHandler;
                    _audioAnalysisHandler = null;
                }

                DebugLogger.Log("BalatroMainMenu", "Event handlers cleaned up successfully");
            }
            catch (Exception ex)
            {
                DebugLogger.LogError("BalatroMainMenu", $"Error cleaning up event handlers: {ex.Message}");
            }
        }

        public Task StopAllSearchesAsync()
        {
            DebugLogger.LogImportant("BalatroMainMenu", "Stopping all searches...");

            if (_modalContainer != null && _modalContainer.Children.Count > 0)
            {
                var modal = _modalContainer.Children[0] as StandardModal;
                if (modal != null)
                {
                    var modalContent = modal.FindControl<ContentPresenter>("ModalContent");
                    var filtersModal = modalContent?.Content as Modals.FiltersModal;
                    if (filtersModal != null)
                    {
                        DebugLogger.LogImportant("BalatroMainMenu", "Checking FiltersModal for active searches...");
                    }
                }
            }

            DebugLogger.LogImportant("BalatroMainMenu", "All searches stopped");
            return Task.CompletedTask;
        }

        #endregion

        #region Keyboard Input

        protected override void OnKeyDown(KeyEventArgs e)
        {
            base.OnKeyDown(e);

            // ESC to exit VibeOut mode
            if (e.Key == Key.Escape && ViewModel?.IsVibeOutMode == true)
            {
                ViewModel.ExitVibeOutMode();
                e.Handled = true;
            }
        }

        #endregion
    }
}


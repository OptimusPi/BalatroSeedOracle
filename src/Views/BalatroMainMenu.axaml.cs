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

        // private Grid? _vibeOutOverlay; // REMOVED: VibeOut feature removed
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
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);

            // Get view-only control references
            _modalContainer = this.FindControl<Grid>("ModalContainer");
            _background = this.FindControl<Control>("BackgroundControl");
            _shaderBackground = _background as BalatroShaderBackground; // CACHE IT ONCE!
            // _vibeOutOverlay = this.FindControl<Grid>("VibeOutOverlay"); // REMOVED: VibeOut feature removed
            _mainContent = this.FindControl<Grid>("MainContent");
            _volumePopup = this.FindControl<Popup>("VolumePopup");

            // CRITICAL FIX: Add drag event handlers to ModalContainer to allow events to pass through
            // Without this, drag events stop at ModalContainer and never reach modal content
            if (_modalContainer != null)
            {
                _modalContainer.AddHandler(
                    DragDrop.DragOverEvent,
                    OnModalContainerDragOver,
                    RoutingStrategies.Tunnel | RoutingStrategies.Bubble,
                    true
                );
                _modalContainer.AddHandler(
                    DragDrop.DropEvent,
                    OnModalContainerDrop,
                    RoutingStrategies.Tunnel | RoutingStrategies.Bubble,
                    true
                );
                DebugLogger.Log(
                    "BalatroMainMenu",
                    "✅ ModalContainer drag event handlers installed"
                );
            }
        }

        /// <summary>
        /// Allows drag events to pass through ModalContainer to modal content
        /// </summary>
        private void OnModalContainerDragOver(object? sender, DragEventArgs e)
        {
            // Allow ALL drag effects - let the actual drop zones decide
            e.DragEffects = DragDropEffects.Copy | DragDropEffects.Move | DragDropEffects.Link;
            // DON'T set e.Handled - let event continue to modal content
            DebugLogger.Log("BalatroMainMenu", $"ModalContainer DragOver - allowing passthrough");
        }

        /// <summary>
        /// Allows drop events to pass through ModalContainer to modal content
        /// </summary>
        private void OnModalContainerDrop(object? sender, DragEventArgs e)
        {
            // DON'T set e.Handled - let event continue to modal content drop zones
            DebugLogger.Log("BalatroMainMenu", "ModalContainer Drop - allowing passthrough");
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
                    Avalonia.Threading.Dispatcher.UIThread.Post(
                        () =>
                        {
                            _shaderBackground.IsAnimating = isAnimating;
                        },
                        Avalonia.Threading.DispatcherPriority.Render
                    );
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
        /// Show search modal - now uses FilterSelectionModal as gateway
        /// </summary>
        private async void ShowSearchModal()
        {
            var result = await this.ShowFilterSelectionModal(enableSearch: true, enableEdit: true);

            // CRITICAL: Reset IsModalVisible when FilterSelectionModal closes, even if cancelled
            if (ViewModel != null)
            {
                ViewModel.IsModalVisible = false;
            }

            if (result.Cancelled)
                return;

            switch (result.Action)
            {
                case Models.FilterAction.Search:
                    if (result.FilterId != null)
                    {
                        // Resolve filter id (filename without extension) to full path in JsonItemFilters
                        var filtersDir = System.IO.Path.Combine(
                            System.IO.Directory.GetCurrentDirectory(),
                            "JsonItemFilters"
                        );
                        var configPath = System.IO.Path.Combine(
                            filtersDir,
                            result.FilterId + ".json"
                        );
                        this.ShowSearchModal(configPath);
                    }
                    break;

                case Models.FilterAction.Edit:
                    // User clicked "Edit this Filter" from search modal - open designer
                    if (result.FilterId != null)
                        ShowFiltersModalDirect(result.FilterId);
                    break;

                case Models.FilterAction.CreateNew:
                    // User clicked "Create New Filter" from search modal - open designer
                    ShowFiltersModalDirect();
                    break;
            }
        }

        /// <summary>
        /// Show search modal with a specific filter loaded (overload for FilterSelectionModal flow)
        /// </summary>
        private void ShowSearchModal(string configPath)
        {
            try
            {
                Helpers.DebugLogger.Log(
                    "BalatroMainMenu",
                    $"ShowSearchModal called with filter: {configPath}"
                );

                var searchContent = new SearchModal();
                // Set the MainMenu reference so CREATE NEW FILTER button works
                searchContent.ViewModel.MainMenu = this;

                // CRITICAL: Load the selected filter immediately!
                if (!string.IsNullOrEmpty(configPath) && System.IO.File.Exists(configPath))
                {
                    _ = searchContent.ViewModel.LoadFilterAsync(configPath);
                    Helpers.DebugLogger.Log(
                        "BalatroMainMenu",
                        $"✅ Filter loaded: {System.IO.Path.GetFileName(configPath)}"
                    );
                }
                else
                {
                    Helpers.DebugLogger.LogError(
                        "BalatroMainMenu",
                        $"❌ Filter not found: {configPath}"
                    );
                }

                // Wire up desktop icon creation
                searchContent.ViewModel.CreateShortcutRequested += (sender, cfgPath) =>
                {
                    Helpers.DebugLogger.Log(
                        "BalatroMainMenu",
                        $"Desktop icon requested for config: {cfgPath}"
                    );
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
                Helpers.DebugLogger.LogError(
                    "BalatroMainMenu",
                    $"Failed to show search modal with filter: {ex.Message}"
                );
            }
        }

        /// <summary>
        /// Show filters modal (for creating/managing filters) - now uses FilterSelectionModal as gateway
        /// </summary>
        public async void ShowFiltersModal()
        {
            var result = await this.ShowFilterSelectionModal(
                enableEdit: true,
                enableCopy: true,
                enableDelete: true
            );

            // CRITICAL: Reset IsModalVisible when FilterSelectionModal closes, even if cancelled
            if (ViewModel != null)
            {
                ViewModel.IsModalVisible = false;
            }

            if (result.Cancelled)
                return;

            switch (result.Action)
            {
                case Models.FilterAction.CreateNew:
                    // Open designer with blank filter
                    ShowFiltersModalDirect();
                    break;
                case Models.FilterAction.Edit:
                    // Open designer with selected filter loaded
                    if (result.FilterId != null)
                        ShowFiltersModalDirect(result.FilterId);
                    break;
                case Models.FilterAction.Copy:
                    // Clone the filter and open designer
                    if (result.FilterId != null)
                    {
                        var clonedId = await CloneFilter(result.FilterId);
                        if (!string.IsNullOrEmpty(clonedId))
                            ShowFiltersModalDirect(clonedId);
                    }
                    break;
                case Models.FilterAction.Delete:
                    // Delete the filter
                    if (result.FilterId != null)
                        await DeleteFilter(result.FilterId);
                    break;
            }
        }

        /// <summary>
        /// Show filters modal directly (internal use - called after filter selection)
        /// </summary>
        private void ShowFiltersModalDirect(string? filterId = null)
        {
            var filtersModal = new Modals.FiltersModal();

            if (!string.IsNullOrEmpty(filterId) && filtersModal.ViewModel != null)
            {
                var filtersDir = System.IO.Path.Combine(
                    System.IO.Directory.GetCurrentDirectory(),
                    "JsonItemFilters"
                );
                var filterPath = System.IO.Path.Combine(filtersDir, filterId + ".json");

                filtersModal.ViewModel.CurrentFilterPath = filterPath;
                _ = filtersModal.ViewModel.ReloadVisualFromSavedFileCommand.ExecuteAsync(null);
            }

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
        /// Show analyze modal - skips FilterSelectionModal and goes straight to analyzer
        /// </summary>
        private void ShowAnalyzeModal()
        {
            OpenAnalyzer(null);
        }

        /// <summary>
        /// Open analyzer with the specified filter (filter selection happens inside analyzer)
        /// </summary>
        private void OpenAnalyzer(string? filterId)
        {
            var analyzeModal = new AnalyzeModal();
            var modal = new StandardModal("ANALYZE");
            modal.SetContent(analyzeModal);
            modal.BackClicked += (s, ev) => HideModalContent();
            ShowModalContent(modal, "SEED ANALYZER");
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
                            DebugLogger.Log(
                                "BalatroMainMenu",
                                "Volume popup closed via click-away"
                            );
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
            if (_modalContainer == null)
                return;

            // Close any open popups when opening a modal
            if (ViewModel.IsVolumePopupOpen)
            {
                ViewModel.IsVolumePopupOpen = false;
            }

            // IMPORTANT: Only clear if we're showing a fresh modal (container was hidden)
            // If transitioning between modals, just replace the content to avoid flicker
            if (!_modalContainer.IsVisible || _modalContainer.Children.Count == 0)
            {
                _modalContainer.Children.Clear();
            }
            else
            {
                // Replace existing modal content (no flicker!)
                _modalContainer.Children.Clear();
            }

            _modalContainer.Children.Add(content);
            _modalContainer.IsVisible = true;
        }

        /// <summary>
        /// Smoothly transition from current modal to new modal (Balatro-style)
        /// </summary>
        private async void TransitionToNewModal(UserControl newContent, string? title)
        {
            if (_modalContainer == null || _modalContainer.Children.Count == 0)
                return;

            var oldContent = _modalContainer.Children[0];

            // Gravity fall with bounce - modal falls completely out of view
            var fallAnimation = new Avalonia.Animation.Animation
            {
                Duration = TimeSpan.FromMilliseconds(800), // Smooth gravity fall
                Easing = new ExponentialEaseIn(), // Gravity acceleration
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
                            new Setter(ScaleTransform.ScaleXProperty, 1.0d),
                            new Setter(RotateTransform.AngleProperty, 0d),
                        },
                    },
                    new Avalonia.Animation.KeyFrame
                    {
                        Cue = new Cue(0.3), // Start rotating as it falls
                        Setters =
                        {
                            new Setter(TranslateTransform.YProperty, 100d),
                            new Setter(OpacityProperty, 0.9d),
                            new Setter(ScaleTransform.ScaleYProperty, 0.98d),
                            new Setter(RotateTransform.AngleProperty, 2d),
                        },
                    },
                    new Avalonia.Animation.KeyFrame
                    {
                        Cue = new Cue(0.7), // Accelerating
                        Setters =
                        {
                            new Setter(TranslateTransform.YProperty, 400d),
                            new Setter(OpacityProperty, 0.5d),
                            new Setter(ScaleTransform.ScaleYProperty, 0.9d),
                            new Setter(ScaleTransform.ScaleXProperty, 0.95d),
                            new Setter(RotateTransform.AngleProperty, 5d),
                        },
                    },
                    new Avalonia.Animation.KeyFrame
                    {
                        Cue = new Cue(1), // Completely out of view
                        Setters =
                        {
                            new Setter(TranslateTransform.YProperty, 1200d), // Way off screen
                            new Setter(OpacityProperty, 0.0d),
                            new Setter(ScaleTransform.ScaleYProperty, 0.7d),
                            new Setter(ScaleTransform.ScaleXProperty, 0.85d),
                            new Setter(RotateTransform.AngleProperty, 8d),
                        },
                    },
                },
            };

            var transformGroup = new TransformGroup();
            transformGroup.Children.Add(new ScaleTransform(1, 1));
            transformGroup.Children.Add(new RotateTransform(0));
            transformGroup.Children.Add(new TranslateTransform(0, 0));
            oldContent.RenderTransform = transformGroup;
            oldContent.RenderTransformOrigin = new RelativePoint(0.5, 0.5, RelativeUnit.Relative);

            await fallAnimation.RunAsync(oldContent);
            await Task.Delay(200); // Brief pause before new modal appears

            _modalContainer.Children.Clear();
            ShowModalWithAnimation(newContent, title);
        }

        /// <summary>
        /// Show modal with pop-up bounce animation (Balatro-style)
        /// </summary>
        private async void ShowModalWithAnimation(UserControl content, string? title)
        {
            if (_modalContainer == null)
                return;

            _modalContainer.Children.Add(content);
            _modalContainer.IsVisible = true;
            _activeModalContent = content;

            if (!string.IsNullOrEmpty(title))
            {
                SetTitle(title);
            }

            // Smooth gravity bounce - rises from below with elastic bounce
            var popAnimation = new Avalonia.Animation.Animation
            {
                Duration = TimeSpan.FromMilliseconds(600), // Smooth rise with bounce
                Easing = new ElasticEaseOut(), // Bouncy landing
                Children =
                {
                    new Avalonia.Animation.KeyFrame
                    {
                        Cue = new Cue(0),
                        Setters =
                        {
                            new Setter(TranslateTransform.YProperty, 800d), // Start from below
                            new Setter(OpacityProperty, 0.0d),
                            new Setter(ScaleTransform.ScaleYProperty, 0.5d),
                            new Setter(ScaleTransform.ScaleXProperty, 0.8d),
                        },
                    },
                    new Avalonia.Animation.KeyFrame
                    {
                        Cue = new Cue(0.4), // Rising up
                        Setters =
                        {
                            new Setter(TranslateTransform.YProperty, 200d),
                            new Setter(OpacityProperty, 0.8d),
                            new Setter(ScaleTransform.ScaleYProperty, 0.95d),
                            new Setter(ScaleTransform.ScaleXProperty, 0.98d),
                        },
                    },
                    new Avalonia.Animation.KeyFrame
                    {
                        Cue = new Cue(1), // Final position with elastic bounce
                        Setters =
                        {
                            new Setter(TranslateTransform.YProperty, 0d),
                            new Setter(OpacityProperty, 1.0d),
                            new Setter(ScaleTransform.ScaleYProperty, 1.0d),
                            new Setter(ScaleTransform.ScaleXProperty, 1.0d),
                        },
                    },
                },
            };

            var transformGroup = new TransformGroup();
            transformGroup.Children.Add(new ScaleTransform(1, 1));
            transformGroup.Children.Add(new TranslateTransform(0, 0));
            content.RenderTransform = transformGroup;
            content.RenderTransformOrigin = new RelativePoint(0.5, 0.5, RelativeUnit.Relative);

            await popAnimation.RunAsync(content);
        }

        /// <summary>
        /// Hide the modal overlay
        /// </summary>
        public void HideModalContent()
        {
            if (_modalContainer == null)
                return;

            // PERFORMANCE FIX: Defer Children.Clear() to prevent audio crackling
            // FiltersModal has thousands of controls - clearing synchronously blocks UI thread
            // which causes audio buffer underruns
            _modalContainer.IsVisible = false;
            _activeModalContent = null;
            SetTitle("Welcome!");

            // Re-enable buttons by clearing modal state
            if (ViewModel != null)
            {
                ViewModel.IsModalVisible = false;
            }

            // Clear on background thread to avoid blocking audio
            Dispatcher.UIThread.Post(
                () =>
                {
                    _modalContainer.Children.Clear();
                },
                DispatcherPriority.Background
            );

            var audioManager = App.GetService<Services.SoundFlowAudioManager>();
        }

        #endregion

        #region VibeOut Mode - REMOVED
        // REMOVED: VibeOut feature has been removed from the application
        /*
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
        */
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

        internal void ApplyShaderPixelSize(float pixelSize)
        {
            if (_background is BalatroShaderBackground shader)
            {
                ViewModel.ApplyShaderPixelSize(shader, pixelSize);
            }
        }

        internal void ApplyShaderSpinEase(float spinEase)
        {
            if (_background is BalatroShaderBackground shader)
            {
                ViewModel.ApplyShaderSpinEase(shader, spinEase);
            }
        }

        // New shader parameter methods that call shader directly
        internal void ApplyShaderTime(float time)
        {
            if (_background is BalatroShaderBackground shader)
            {
                shader.SetTime(time);
            }
        }

        internal void ApplyShaderSpinTime(float spinTime)
        {
            if (_background is BalatroShaderBackground shader)
            {
                shader.SetSpinTime(spinTime);
            }
        }

        internal void ApplyShaderParallaxX(float parallaxX)
        {
            if (_background is BalatroShaderBackground shader)
            {
                shader.SetParallaxX(parallaxX);
            }
        }

        internal void ApplyShaderParallaxY(float parallaxY)
        {
            if (_background is BalatroShaderBackground shader)
            {
                shader.SetParallaxY(parallaxY);
            }
        }

        internal void ApplyShaderLoopCount(float loopCount)
        {
            if (_background is BalatroShaderBackground shader)
            {
                shader.SetLoopCount(loopCount);
            }
        }

        /// <summary>
        /// Set the volume for a specific track in the audio manager
        /// </summary>
        internal void SetTrackVolume(string trackName, float volume)
        {
            // TODO: Implement track volume control when audio manager supports it
            // This is a stub for now to fix build errors
            DebugLogger.Log("BalatroMainMenu", $"SetTrackVolume called: {trackName} = {volume}");
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

        // Range application helpers - REMOVED (VibeOut feature)
        // REMOVED: These methods no longer exist on BalatroShaderBackground
        /*
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
        */

        #endregion

        #region Desktop Icon Management

        /// <summary>
        /// Shows the search modal for an existing search instance
        /// </summary>
        public void ShowSearchModalForInstance(string searchId, string? configPath = null)
        {
            try
            {
                DebugLogger.Log(
                    "BalatroMainMenu",
                    $"ShowSearchModalForInstance called - SearchId: {searchId}, ConfigPath: {configPath}"
                );

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
                    DebugLogger.Log(
                        "BalatroMainMenu",
                        $"Desktop icon requested for config: {cfgPath}"
                    );
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
                DebugLogger.LogError(
                    "BalatroMainMenu",
                    $"Failed to show search modal for instance: {ex}"
                );
            }
        }

        public void ShowSearchDesktopIcon(string searchId, string? configPath = null)
        {
            DebugLogger.Log(
                "BalatroMainMenu",
                $"ShowSearchDesktopIcon called with searchId: {searchId}, config: {configPath}"
            );

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
                DebugLogger.Log(
                    "BalatroMainMenu",
                    $"Skipping desktop icon creation - no valid config path"
                );
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

            DebugLogger.Log(
                "BalatroMainMenu",
                $"Created SearchDesktopIcon #{ViewModel.WidgetCounter} at position ({leftMargin}, {topMargin})"
            );
        }

        public void RemoveSearchDesktopIcon(string searchId)
        {
            DebugLogger.Log(
                "BalatroMainMenu",
                $"RemoveSearchDesktopIcon called for searchId: {searchId}"
            );

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
                    var searchIdProperty = icon.GetType()
                        .GetField(
                            "_searchId",
                            System.Reflection.BindingFlags.NonPublic
                                | System.Reflection.BindingFlags.Instance
                        );
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
                DebugLogger.Log(
                    "BalatroMainMenu",
                    $"Removed SearchDesktopIcon for searchId: {searchId}"
                );
            }
            else
            {
                DebugLogger.Log(
                    "BalatroMainMenu",
                    $"No SearchDesktopIcon found for searchId: {searchId}"
                );
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
                var audioManager = ServiceHelper.GetService<SoundFlowAudioManager>();
                if (audioManager != null && _audioAnalysisHandler != null)
                {
                    audioManager.AudioAnalysisUpdated -= _audioAnalysisHandler;
                    _audioAnalysisHandler = null;
                }

                DebugLogger.Log("BalatroMainMenu", "Event handlers cleaned up successfully");
            }
            catch (Exception ex)
            {
                DebugLogger.LogError(
                    "BalatroMainMenu",
                    $"Error cleaning up event handlers: {ex.Message}"
                );
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
                        DebugLogger.LogImportant(
                            "BalatroMainMenu",
                            "Checking FiltersModal for active searches..."
                        );
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

            // REMOVED: VibeOut mode ESC key handler
            /*
            // ESC to exit VibeOut mode
            if (e.Key == Key.Escape && ViewModel?.IsVibeOutMode == true)
            {
                ViewModel.ExitVibeOutMode();
                e.Handled = true;
            }
            */
        }

        #endregion

        #region Filter Management Helpers

        /// <summary>
        /// Get the parent window (needed for modal dialogs)
        /// </summary>
        public Window GetWindow()
        {
            return TopLevel.GetTopLevel(this) as Window
                ?? throw new InvalidOperationException("Could not find parent window");
        }

        /// <summary>
        /// Clone a filter and return the new filter ID
        /// </summary>
        private async Task<string> CloneFilter(string filterId)
        {
            try
            {
                var baseDir =
                    System.IO.Path.GetDirectoryName(
                        System.Reflection.Assembly.GetExecutingAssembly().Location
                    ) ?? System.AppDomain.CurrentDomain.BaseDirectory;
                var filtersDir = System.IO.Path.Combine(baseDir, "JsonItemFilters");
                var filterPath = System.IO.Path.Combine(filtersDir, $"{filterId}.json");

                if (!System.IO.File.Exists(filterPath))
                {
                    DebugLogger.LogError("BalatroMainMenu", $"Filter not found: {filterPath}");
                    return string.Empty;
                }

                var json = await System.IO.File.ReadAllTextAsync(filterPath);
                var config =
                    System.Text.Json.JsonSerializer.Deserialize<Motely.Filters.MotelyJsonConfig>(
                        json
                    );

                if (config != null)
                {
                    config.Name = $"{config.Name} (Copy)";
                    config.DateCreated = DateTime.UtcNow;
                    config.Author =
                        ServiceHelper.GetService<UserProfileService>()?.GetAuthorName()
                        ?? "Unknown";

                    var newId = $"{filterId}_copy_{DateTime.UtcNow.Ticks}";
                    var newPath = System.IO.Path.Combine(filtersDir, $"{newId}.json");

                    var newJson = System.Text.Json.JsonSerializer.Serialize(
                        config,
                        new System.Text.Json.JsonSerializerOptions { WriteIndented = true }
                    );
                    await System.IO.File.WriteAllTextAsync(newPath, newJson);

                    DebugLogger.Log("BalatroMainMenu", $"Filter cloned: {filterId} -> {newId}");
                    return newId;
                }
            }
            catch (Exception ex)
            {
                DebugLogger.LogError("BalatroMainMenu", $"Error cloning filter: {ex.Message}");
            }

            return string.Empty;
        }

        /// <summary>
        /// Delete a filter
        /// </summary>
        private async Task DeleteFilter(string filterId)
        {
            try
            {
                var baseDir =
                    System.IO.Path.GetDirectoryName(
                        System.Reflection.Assembly.GetExecutingAssembly().Location
                    ) ?? System.AppDomain.CurrentDomain.BaseDirectory;
                var filtersDir = System.IO.Path.Combine(baseDir, "JsonItemFilters");
                var filterPath = System.IO.Path.Combine(filtersDir, $"{filterId}.json");

                if (System.IO.File.Exists(filterPath))
                {
                    System.IO.File.Delete(filterPath);
                    DebugLogger.Log("BalatroMainMenu", $"Filter deleted: {filterId}");
                }
                else
                {
                    DebugLogger.Log(
                        "BalatroMainMenu",
                        $"Filter not found for deletion: {filterPath}"
                    );
                }
            }
            catch (Exception ex)
            {
                DebugLogger.LogError("BalatroMainMenu", $"Error deleting filter: {ex.Message}");
            }

            await Task.CompletedTask;
        }

        #endregion

        #region Author TextBox Handlers

        private void OnAuthorTextBoxKeyDown(object? sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                // Save and exit edit mode
                ViewModel.SaveAuthorCommand.Execute(null);
                e.Handled = true;

                // Remove focus from textbox
                if (sender is TextBox textBox)
                {
                    var focusManager = TopLevel.GetTopLevel(this)?.FocusManager;
                    focusManager?.ClearFocus();
                }
            }
            else if (e.Key == Key.Escape)
            {
                // Cancel editing
                ViewModel.CancelAuthorEditCommand.Execute(null);
                e.Handled = true;
            }
        }

        private void OnAuthorTextBoxLostFocus(object? sender, RoutedEventArgs e)
        {
            // Auto-save when losing focus
            ViewModel.SaveAuthorCommand.Execute(null);
        }

        #endregion
    }
}

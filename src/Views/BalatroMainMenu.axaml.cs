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
using Avalonia.Layout;
using Avalonia.LogicalTree;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
using Avalonia.Styling;
using Avalonia.Threading;
using BalatroSeedOracle.Constants;
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
        private Border? _modalOverlay;
        private ContentControl? _modalContentWrapper;
        private Control? _background;
        private BalatroShaderBackground? _shaderBackground; // CACHED - it's ALWAYS BalatroShaderBackground!

        // private Grid? _vibeOutOverlay; // REMOVED: VibeOut feature removed
        private Grid? _mainContent;
        private UserControl? _activeModalContent;
        private TextBlock? _mainTitleText;
        private Action<float, float, float, float>? _audioAnalysisHandler;
        private Popup? _volumePopup;

        // Modal navigation stack (for Back button support)
        private UserControl? _previousModalContent;
        private string? _previousModalTitle;

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
            _modalOverlay = this.FindControl<Border>("ModalOverlay");
            _modalContentWrapper = this.FindControl<ContentControl>("ModalContentWrapper");
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

            // Window state change requests (for fullscreen vibe mode)
            ViewModel.WindowStateChangeRequested += OnWindowStateChangeRequested;
        }

        /// <summary>
        /// Handles window state change requests from the ViewModel (for fullscreen vibe mode)
        /// </summary>
        private void OnWindowStateChangeRequested(object? sender, bool enterFullscreen)
        {
            var window = this.VisualRoot as Window;
            if (window != null)
            {
                Avalonia.Threading.Dispatcher.UIThread.Post(
                    () =>
                    {
                        if (enterFullscreen)
                        {
                            window.WindowState = WindowState.FullScreen;
                            DebugLogger.Log(
                                "BalatroMainMenu",
                                "Entered fullscreen for Vibe Out Mode"
                            );
                        }
                        else
                        {
                            window.WindowState = WindowState.Normal;
                            DebugLogger.Log(
                                "BalatroMainMenu",
                                "Exited fullscreen from Vibe Out Mode"
                            );
                        }
                    },
                    Avalonia.Threading.DispatcherPriority.Render
                );
            }
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
        private void ShowSearchModal()
        {
            // Clear modal stack when starting fresh from main menu
            _previousModalContent = null;
            _previousModalTitle = null;

            // Create FilterSelectionModal and show it as main modal content (NOT as a dialog)
            var filterSelectionModal = new FilterSelectionModal();
            var filterSelectionVM = new FilterSelectionModalViewModel(
                enableSearch: true,
                enableEdit: true,
                enableCopy: false,
                enableDelete: false,
                enableAnalyze: false
            );
            filterSelectionModal.DataContext = filterSelectionVM;

            // Wire up ModalCloseRequested to handle user actions
            filterSelectionVM.ModalCloseRequested += (s, e) =>
            {
                DebugLogger.Log(
                    "BalatroMainMenu",
                    "🔵 ShowSearchModal: ModalCloseRequested event FIRED!"
                );
                var result = filterSelectionVM.Result;

                if (result.Cancelled)
                {
                    // User hit back/cancel - close modal
                    HideModalContent();
                    if (ViewModel != null)
                    {
                        ViewModel.IsModalVisible = false;
                    }
                    return;
                }

                switch (result.Action)
                {
                    case Models.FilterAction.Search:
                        if (result.FilterId != null)
                        {
                            // Save FilterSelectionModal for back navigation
                            _previousModalContent = filterSelectionModal;
                            _previousModalTitle = "🔍 SELECT FILTER";

                            // Resolve filter id to full path
                            var filtersDir = System.IO.Path.Combine(
                                System.IO.Directory.GetCurrentDirectory(),
                                "JsonItemFilters"
                            );
                            var configPath = System.IO.Path.Combine(
                                filtersDir,
                                result.FilterId + ".json"
                            );
                            // TRANSITION to SearchModal (no flicker - just content swap)
                            _ = ShowSearchModalWithFilterAsync(configPath);
                        }
                        break;

                    case Models.FilterAction.Edit:
                        if (result.FilterId != null)
                            _ = ShowFiltersModalDirectAsync(result.FilterId);
                        else
                            _ = ShowFiltersModalDirectAsync(); // CreateNew
                        break;

                    case Models.FilterAction.CreateNew:
                        _ = ShowFiltersModalDirectAsync();
                        break;
                }
            };

            // Show FilterSelectionModal as main modal content (NOT wrapped in StandardModal - it has its own Back button!)
            ShowModalContent(filterSelectionModal, "🔍 SELECT FILTER");
        }

        /// <summary>
        /// Show search modal with a specific filter loaded (overload for FilterSelectionModal flow)
        /// </summary>
        private async Task ShowSearchModalWithFilterAsync(string configPath)
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

                // CRITICAL: Load the selected filter immediately AND WAIT FOR IT!
                if (!string.IsNullOrEmpty(configPath) && System.IO.File.Exists(configPath))
                {
                    await searchContent.ViewModel.LoadFilterAsync(configPath);

                    // DEBUG ASSERT: Filter MUST be loaded
                    if (string.IsNullOrEmpty(searchContent.ViewModel.CurrentFilterPath))
                    {
                        throw new InvalidOperationException(
                            $"ASSERT FAILED: Filter did not load! Path: {configPath}"
                        );
                    }

                    // Open directly to Search tab (index 0 since Preferred Deck tab was removed)
                    searchContent.ViewModel.SelectedTabIndex = 0;

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
                    throw new InvalidOperationException(
                        $"ASSERT FAILED: Filter file does not exist! Path: {configPath}"
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

                // Show SearchModal as main modal content (replaces FilterSelectionModal)
                var modal = new StandardModal("🎰 SEED SEARCH");
                modal.SetContent(searchContent);
                modal.BackClicked += (s, e) =>
                {
                    // Check if we have a previous modal to return to
                    if (_previousModalContent != null && _previousModalTitle != null)
                    {
                        // Return to FilterSelectionModal
                        var previousContent = _previousModalContent;
                        var previousTitle = _previousModalTitle;

                        // Clear stack for next navigation
                        _previousModalContent = null;
                        _previousModalTitle = null;

                        // Restore previous modal
                        ShowModalContent(previousContent, previousTitle, keepBackdrop: true);
                    }
                    else
                    {
                        // No previous modal - close entirely
                        HideModalContent();
                    }
                };
                // Keep backdrop visible during transition to prevent flicker
                ShowModalContent(modal, "🎰 SEED SEARCH", keepBackdrop: true);
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
        public void ShowFiltersModal()
        {
            // Create FilterSelectionModal with Edit/Copy/Delete actions enabled
            var filterSelectionModal = new FilterSelectionModal();
            var filterSelectionVM = new FilterSelectionModalViewModel(
                enableSearch: false,
                enableEdit: true,
                enableCopy: true,
                enableDelete: true,
                enableAnalyze: false
            );
            filterSelectionModal.DataContext = filterSelectionVM;

            // Handle modal close event
            filterSelectionVM.ModalCloseRequested += async (s, e) =>
            {
                var result = filterSelectionVM.Result;

                if (result.Cancelled)
                {
                    // Reset IsModalVisible and hide modal
                    if (ViewModel != null)
                    {
                        ViewModel.IsModalVisible = false;
                    }
                    HideModalContent();
                    return;
                }

                // Handle different actions - ShowFiltersModalDirect will replace the modal content directly
                switch (result.Action)
                {
                    case Models.FilterAction.CreateNew:
                        // Show filter name input dialog first
                        var filterName = await ShowFilterNameInputDialog();
                        if (!string.IsNullOrWhiteSpace(filterName))
                        {
                            // Create new filter with the given name
                            var newFilterId = await CreateNewFilterWithName(filterName);
                            if (!string.IsNullOrEmpty(newFilterId))
                            {
                                await Dispatcher.UIThread.InvokeAsync(() =>
                                    ShowFiltersModalDirectAsync(newFilterId)
                                );
                            }
                        }
                        else
                        {
                            // User cancelled - stay in FilterSelectionModal (don't close entire modal)
                            Helpers.DebugLogger.Log(
                                "BalatroMainMenu",
                                "Create filter cancelled - staying in filter selection"
                            );
                            // Do nothing - just let the dialog close and stay in FilterSelectionModal
                        }
                        break;
                    case Models.FilterAction.Edit:
                        // Open designer with selected filter loaded (replaces current modal)
                        if (result.FilterId != null)
                        {
                            await Dispatcher.UIThread.InvokeAsync(() =>
                                ShowFiltersModalDirectAsync(result.FilterId)
                            );
                        }
                        break;
                    case Models.FilterAction.Copy:
                        // Show dialog to get copy name, then clone and open designer
                        if (result.FilterId != null)
                        {
                            // Get original filter name for default (using FilterService)
                            var filterService =
                                Helpers.ServiceHelper.GetRequiredService<IFilterService>();
                            var originalName = await filterService.GetFilterNameAsync(
                                result.FilterId
                            );
                            var defaultCopyName = string.IsNullOrEmpty(originalName)
                                ? "Filter Copy"
                                : $"{originalName} Copy";

                            var copyName = await ShowFilterNameInputDialog(defaultCopyName);
                            if (!string.IsNullOrWhiteSpace(copyName))
                            {
                                var clonedId = await filterService.CloneFilterAsync(
                                    result.FilterId,
                                    copyName
                                );
                                if (!string.IsNullOrEmpty(clonedId))
                                {
                                    await Dispatcher.UIThread.InvokeAsync(() =>
                                        ShowFiltersModalDirectAsync(clonedId)
                                    );
                                }
                            }
                            else
                            {
                                // User cancelled - stay in FilterSelectionModal (don't close entire modal)
                                Helpers.DebugLogger.Log(
                                    "BalatroMainMenu",
                                    "Copy filter cancelled - staying in filter selection"
                                );
                                // Do nothing - just let the dialog close and stay in FilterSelectionModal
                            }
                        }
                        break;
                    case Models.FilterAction.Delete:
                        // NOTE: Delete is now handled entirely in FilterSelectionModalViewModel.ConfirmDelete()
                        // This case should never be reached since ConfirmDelete() doesn't invoke ModalCloseRequested anymore
                        // Kept for backwards compatibility in case of refactoring
                        Helpers.DebugLogger.Log(
                            "BalatroMainMenu",
                            "Delete action reached ModalCloseRequested - this should not happen. Delete is handled in ViewModel."
                        );
                        break;
                }
            };

            // Show FilterSelectionModal as main modal content (NOT wrapped in StandardModal)
            ShowModalContent(filterSelectionModal, "🎨 SELECT FILTER");
        }

        /// <summary>
        /// Show filters modal directly (internal use - called after filter selection)
        /// </summary>
        private async Task ShowFiltersModalDirectAsync(string? filterId = null)
        {
            try
            {
                // If no filterId provided (Create New), prompt for filter name first
                if (string.IsNullOrEmpty(filterId))
                {
                    var filterName = await ShowFilterNameInputDialog();
                    if (string.IsNullOrEmpty(filterName))
                    {
                        Helpers.DebugLogger.Log(
                            "BalatroMainMenu",
                            "Filter creation cancelled by user"
                        );
                        return; // User cancelled
                    }

                    // Create new filter file with user's chosen name
                    var filtersDir = System.IO.Path.Combine(
                        System.IO.Directory.GetCurrentDirectory(),
                        "JsonItemFilters"
                    );
                    System.IO.Directory.CreateDirectory(filtersDir);

                    // Sanitize filename
                    var sanitizedName = string.Join(
                        "_",
                        filterName.Split(System.IO.Path.GetInvalidFileNameChars())
                    );
                    filterId = sanitizedName;
                    var filterPath = System.IO.Path.Combine(filtersDir, filterId + ".json");

                    // Create default empty filter in NEW MotelyJsonConfig format
                    var defaultFilter = new
                    {
                        name = filterName,
                        description = "Created with visual filter builder",
                        author = "pifreak",
                        dateCreated = DateTime.UtcNow.ToString("O"),
                        deck = "Red",
                        stake = "White",
                        must = new object[] { },
                        should = new object[] { },
                        mustNot = new object[] { },
                    };
                    var jsonContent = System.Text.Json.JsonSerializer.Serialize(
                        defaultFilter,
                        new System.Text.Json.JsonSerializerOptions { WriteIndented = true }
                    );
                    System.IO.File.WriteAllText(
                        filterPath,
                        Helpers.CompactJsonFormatter.Format(jsonContent)
                    );

                    Helpers.DebugLogger.Log(
                        "BalatroMainMenu",
                        $"✅ Created new filter: {filterName} ({filterId}.json)"
                    );
                }

                // Back to the REAL FiltersModal with visual item shelf and card display!
                var filtersModal = new Modals.FiltersModal();

                // Load the filter data FIRST if provided
                if (!string.IsNullOrEmpty(filterId) && filtersModal.ViewModel != null)
                {
                    var filtersDir = System.IO.Path.Combine(
                        System.IO.Directory.GetCurrentDirectory(),
                        "JsonItemFilters"
                    );
                    var filterPath = System.IO.Path.Combine(filtersDir, filterId + ".json");

                    filtersModal.ViewModel.CurrentFilterPath = filterPath;

                    Helpers.DebugLogger.Log(
                        "BalatroMainMenu",
                        $"🔄 Loading filter for editing: {filterPath}"
                    );

                    await filtersModal.ViewModel.ReloadVisualFromSavedFileCommand.ExecuteAsync(
                        null
                    );

                    Helpers.DebugLogger.Log(
                        "BalatroMainMenu",
                        $"✅ Filter loaded for editing: {filterId}"
                    );
                }

                // DEBUG ASSERT: Filter must ALWAYS be loaded when showing designer
                System.Diagnostics.Debug.Assert(
                    filtersModal.ViewModel != null && !string.IsNullOrEmpty(filterId),
                    "Filter Designer opened without a valid filter! FilterId must be provided."
                );

                // THEN show the modal with loaded content
                var modal = new StandardModal("🎨 FILTER DESIGNER");
                modal.SetContent(filtersModal);
                modal.BackClicked += (s, e) => HideModalContent();
                // Keep backdrop visible during transition to prevent flicker
                ShowModalContent(modal, keepBackdrop: true);
            }
            catch (Exception ex)
            {
                Helpers.DebugLogger.LogError(
                    "BalatroMainMenu",
                    $"ShowFiltersModalDirectAsync failed: {ex.Message}"
                );
                throw;
            }
        }

        /// <summary>
        /// Show Avalonia input dialog for filter name
        /// </summary>
        private async Task<string?> ShowFilterNameInputDialog(string? defaultName = null)
        {
            var dialog = new Window
            {
                Title = defaultName == null ? "Create New Filter" : "Copy Filter",
                Width = 400,
                Height = 200,
                CanResize = false,
                WindowStartupLocation = WindowStartupLocation.CenterOwner,
                SystemDecorations = SystemDecorations.BorderOnly,
            };

            // Use provided default name, or generate a fun random filter name
            string defaultText;
            if (!string.IsNullOrEmpty(defaultName))
            {
                defaultText = defaultName;
            }
            else
            {
                var randomNames = new[]
                {
                    "Epic Filter",
                    "Lucky Find",
                    "Mega Search",
                    "Sweet Combo",
                    "Power Play",
                    "Golden Run",
                    "Chaos Filter",
                    "Master Build",
                    "Pro Strat",
                    "Wildcard Hunt",
                    "Dream Seed",
                    "Perfect Setup",
                    "Boss Crusher",
                    "Money Maker",
                    "Victory Path",
                };
                defaultText = randomNames[new Random().Next(randomNames.Length)];
            }

            string? result = null;
            var textBox = new TextBox
            {
                Text = defaultText, // Pre-fill with default name
                Watermark = "Enter filter name...",
                Margin = new Thickness(20, 10),
            };

            var okButton = new Button
            {
                Content = "CREATE",
                Width = 100,
                Margin = new Thickness(5),
            };

            var cancelButton = new Button
            {
                Content = "CANCEL",
                Width = 100,
                Margin = new Thickness(5),
            };

            okButton.Click += (s, e) =>
            {
                result = textBox.Text;
                dialog.Close();
            };

            cancelButton.Click += (s, e) =>
            {
                result = null;
                dialog.Close();
            };

            var layout = new StackPanel { Spacing = 10, Margin = new Thickness(20) };

            layout.Children.Add(
                new TextBlock { Text = "Filter Name:", FontWeight = FontWeight.Bold }
            );
            layout.Children.Add(textBox);

            var buttonPanel = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                HorizontalAlignment = HorizontalAlignment.Center,
                Spacing = 10,
            };
            buttonPanel.Children.Add(okButton);
            buttonPanel.Children.Add(cancelButton);
            layout.Children.Add(buttonPanel);

            dialog.Content = layout;

            await dialog.ShowDialog((Window)this.VisualRoot!);
            return result;
        }

        /// <summary>
        /// Create a new filter with the given name
        /// </summary>
        private async Task<string?> CreateNewFilterWithName(string filterName)
        {
            try
            {
                var filtersDir = System.IO.Path.Combine(
                    System.IO.Directory.GetCurrentDirectory(),
                    "JsonItemFilters"
                );

                System.IO.Directory.CreateDirectory(filtersDir);

                // Generate unique ID
                var filterId = $"{filterName.Replace(" ", "").ToLower()}_{Guid.NewGuid():N}";
                var filterPath = System.IO.Path.Combine(filtersDir, filterId + ".json");

                // Create minimal valid filter JSON in NEW MotelyJsonConfig format
                var minimalFilter = new
                {
                    name = filterName,
                    description = "Created with visual filter builder",
                    author = "pifreak",
                    dateCreated = DateTime.UtcNow.ToString("O"),
                    deck = "Red",
                    stake = "White",
                    must = new object[] { },
                    should = new object[] { },
                    mustNot = new object[] { },
                };

                var json = System.Text.Json.JsonSerializer.Serialize(
                    minimalFilter,
                    new System.Text.Json.JsonSerializerOptions { WriteIndented = true }
                );

                await System.IO.File.WriteAllTextAsync(filterPath, json);

                Helpers.DebugLogger.Log("BalatroMainMenu", $"✅ Created new filter: {filterId}");
                return filterId;
            }
            catch (Exception ex)
            {
                Helpers.DebugLogger.LogError(
                    "BalatroMainMenu",
                    $"Failed to create filter: {ex.Message}"
                );
                return null;
            }
        }

        /// <summary>
        /// Show settings modal
        /// </summary>
        private void ShowSettingsModal()
        {
            var settingsModal = new Modals.SettingsModal();

            var modal = new StandardModal("SETTINGS");
            modal.Squeeze = true; // Use compact sizing for settings
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

            // Request focus so keyboard events work (F11, ESC)
            this.Focus();
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
        /// Show a UserControl as a modal overlay - Balatro style (NO wimpy fades, just POP!)
        /// </summary>
        public void ShowModalContent(
            UserControl content,
            string? title = null,
            bool keepBackdrop = false
        )
        {
            if (_modalContainer == null || _modalOverlay == null || _modalContentWrapper == null)
                return;

            // Close any open popups when opening a modal
            if (ViewModel.IsVolumePopupOpen)
            {
                ViewModel.IsVolumePopupOpen = false;
            }

            // Update title immediately
            if (!string.IsNullOrEmpty(title))
            {
                SetTitle(title);
            }

            // Store active content reference
            _activeModalContent = content;

            // CRITICAL: Calculate window height and set initial transform FIRST
            var windowHeight = this.Bounds.Height;

            // Set initial states BEFORE making visible or adding content
            // Backdrop appears INSTANTLY (no fade) - Balatro style
            if (!keepBackdrop)
            {
                _modalOverlay.Opacity = UIConstants.FullOpacity;
            }

            // CRITICAL: Keep content wrapper invisible until transform is applied
            _modalContentWrapper.Opacity = UIConstants.InvisibleOpacity;

            // Get the existing TranslateTransform from XAML (which has transitions attached!)
            var translateTransform = _modalContentWrapper.RenderTransform as TranslateTransform;
            if (translateTransform == null)
            {
                // Fallback: create new transform if somehow missing from XAML
                translateTransform = new TranslateTransform();
                _modalContentWrapper.RenderTransform = translateTransform;
            }

            // Set initial Y position to be off-screen at the bottom
            translateTransform.Y = windowHeight;
            translateTransform.X = 0;

            // Set the content in the wrapper
            _modalContentWrapper.Content = content;

            // Make container visible
            _modalContainer.IsVisible = true;

            // Enable modal state
            if (ViewModel != null)
            {
                ViewModel.IsModalVisible = true;
            }

            // Trigger animations by changing properties after a delay
            // The transitions defined in XAML will handle the smooth animation
            Dispatcher.UIThread.Post(
                () =>
                {
                    // Make content wrapper visible now that transform is set
                    _modalContentWrapper.Opacity = UIConstants.FullOpacity;

                    // Backdrop is already instant - no fade animation

                    // Slide up content from below screen (translateY: windowHeight → 0)
                    translateTransform.Y = 0;
                },
                DispatcherPriority.Render
            );
        }

        /// <summary>
        /// Smoothly transition from current modal to new modal (Balatro-style)
        /// </summary>
        private async Task TransitionToNewModalAsync(UserControl newContent, string? title)
        {
            try
            {
                if (
                    _modalContainer == null
                    || _modalContentWrapper == null
                    || _modalContentWrapper.Content == null
                )
                    return;

                var oldContent = _modalContentWrapper.Content as Control;
                if (oldContent == null)
                    return;

                // Gravity fall with bounce - modal falls completely out of view
                var fallAnimation = new Avalonia.Animation.Animation
                {
                    Duration = TimeSpan.FromMilliseconds(UIConstants.GravityAnimationDurationMs), // Smooth gravity fall
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
                oldContent.RenderTransformOrigin = new RelativePoint(
                    0.5,
                    0.5,
                    RelativeUnit.Relative
                );

                await fallAnimation.RunAsync(oldContent);

                _modalContentWrapper.Content = null;
                await ShowModalWithAnimationAsync(newContent, title);
            }
            catch (Exception ex)
            {
                Helpers.DebugLogger.LogError(
                    "BalatroMainMenu",
                    $"TransitionToNewModalAsync failed: {ex.Message}"
                );
                throw;
            }
        }

        /// <summary>
        /// Show modal with pop-up bounce animation (Balatro-style)
        /// </summary>
        private async Task ShowModalWithAnimationAsync(UserControl content, string? title)
        {
            try
            {
                if (
                    _modalContainer == null
                    || _modalContentWrapper == null
                    || _modalOverlay == null
                )
                    return;

                _modalContentWrapper.Content = content;
                _modalContainer.IsVisible = true;
                _activeModalContent = content;
                _modalOverlay.Opacity = UIConstants.FullOpacity; // Keep overlay visible during transition

                if (!string.IsNullOrEmpty(title))
                {
                    SetTitle(title);
                }

                // Smooth gravity bounce - rises from below with elastic bounce
                var popAnimation = new Avalonia.Animation.Animation
                {
                    Duration = TimeSpan.FromMilliseconds(UIConstants.BounceAnimationDurationMs), // Smooth rise with bounce
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
            catch (Exception ex)
            {
                Helpers.DebugLogger.LogError(
                    "BalatroMainMenu",
                    $"ShowModalWithAnimationAsync failed: {ex.Message}"
                );
                throw;
            }
        }

        /// <summary>
        /// Hide the modal overlay
        /// </summary>
        public void HideModalContent()
        {
            if (_modalContainer == null || _modalContentWrapper == null)
                return;

            // PERFORMANCE FIX: Defer content clearing to prevent audio crackling
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

            // Clear content on background thread to avoid blocking audio
            Dispatcher.UIThread.Post(
                () =>
                {
                    _modalContentWrapper.Content = null;
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
        public async Task ShowSearchModalForInstanceAsync(
            string searchId,
            string? configPath = null
        )
        {
            try
            {
                DebugLogger.Log(
                    "BalatroMainMenu",
                    $"ShowSearchModalForInstanceAsync called - SearchId: {searchId}, ConfigPath: {configPath}"
                );

                var searchContent = new SearchModal();
                // Set the MainMenu reference so CREATE NEW FILTER button works
                searchContent.ViewModel.MainMenu = this;
                await searchContent.ViewModel.ConnectToExistingSearch(searchId);

                if (!string.IsNullOrEmpty(configPath))
                {
                    await searchContent.ViewModel.LoadFilterAsync(configPath);
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
                    $"ShowSearchModalForInstanceAsync failed: {ex.Message}"
                );
                throw;
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
                    // Use public SearchId property instead of reflection
                    if (icon.SearchId == searchId)
                    {
                        iconToRemove = icon;
                        break;
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

            if (_modalContentWrapper != null && _modalContentWrapper.Content != null)
            {
                var modal = _modalContentWrapper.Content as StandardModal;
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

            // F11 to toggle Vibe Out Mode (fullscreen visualizer)
            if (e.Key == Key.F11)
            {
                ViewModel?.ToggleVibeOutModeCommand.Execute(null);
                e.Handled = true;
                return;
            }

            // ESC to exit Vibe Out Mode
            if (e.Key == Key.Escape && ViewModel?.IsVibeOutMode == true)
            {
                ViewModel.ToggleVibeOutModeCommand.Execute(null);
                e.Handled = true;
                return;
            }
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
        /// <summary>
        /// Get filter name from filter ID
        /// </summary>
        // REMOVED: GetFilterName, CloneFilterWithName, DeleteFilter
        // These methods have been moved to FilterService for proper MVVM separation
        // Use IFilterService.GetFilterNameAsync(), IFilterService.CloneFilterAsync(), IFilterService.DeleteFilterAsync()

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

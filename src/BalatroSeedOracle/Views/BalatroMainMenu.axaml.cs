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
using Motely.Filters.Jaml;

namespace BalatroSeedOracle.Views
{
    public partial class BalatroMainMenu : UserControl, IModalHost
    {
        private Grid? _modalContainer;
        private Border? _modalOverlay;
        private ContentControl? _modalContentWrapper;
        private Control? _background;
        private BalatroShaderBackground? _shaderBackground;

        /// <summary>
        /// Public accessor for the shader background control (for transition effects)
        /// </summary>
        public BalatroShaderBackground? ShaderBackground => _shaderBackground;

        private Grid? _mainContent;
        private UserControl? _activeModalContent;
        private TextBlock? _mainTitleText;
        private Action<float, float, float, float>? _audioAnalysisHandler;
        private Popup? _volumePopup;

        // Modal navigation stack
        private UserControl? _previousModalContent;
        private string? _previousModalTitle;

        public BalatroMainMenuViewModel ViewModel { get; }

        public Action<UserControl>? RequestContentSwap { get; set; }

        /// <summary>
        /// Parameterless constructor required by Avalonia XAML compiler (AVLN3000).
        /// Must not be used at runtime. Resolve BalatroMainMenu from DI (constructor injection).
        /// </summary>
        public BalatroMainMenu()
            : this(throwForDesignTimeOnly: true) { }

        private BalatroMainMenu(bool throwForDesignTimeOnly)
        {
            if (throwForDesignTimeOnly)
                throw new InvalidOperationException(
                    "Do not instantiate BalatroMainMenu() with no args. Resolve from DI: GetRequiredService<BalatroMainMenu>() (ViewModel is injected by the container)."
                );
            ViewModel = null!;
            DataContext = null;
            InitializeComponent(); // generated: loads XAML + assigns x:Name fields
            WireUpControls();
            WireViewModelEvents();
            this.Loaded += OnLoaded;
        }

        public BalatroMainMenu(BalatroMainMenuViewModel viewModel)
        {
            ViewModel = viewModel;
            DataContext = ViewModel;

            InitializeComponent(); // generated: loads XAML + assigns x:Name fields
            WireUpControls();
            WireViewModelEvents();

            this.Loaded += OnLoaded;
        }

        /// <summary>
        /// Caches strongly-typed references to x:Name controls and wires container handlers.
        /// MUST run AFTER the generated InitializeComponent() (which loads the XAML and assigns
        /// the x:Name fields). This used to be a hand-written InitializeComponent() override,
        /// but that shadowed the source-generated InitializeComponent(bool) so the x:Name fields
        /// were never assigned (all named controls came back null → modals never showed content).
        /// </summary>
        private void WireUpControls()
        {
            // Direct field access from x:Name - assigned by the generated InitializeComponent().
            _modalContainer = ModalContainer;
            _modalOverlay = ModalOverlay;
            _modalContentWrapper = ModalContentWrapper;
            _background = BackgroundControl;
            _shaderBackground = _background as BalatroShaderBackground;
            _mainContent = MainContent;
            _volumePopup = VolumePopup;

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
            }
        }

        private void OnModalContainerDragOver(object? sender, DragEventArgs e)
        {
            e.DragEffects = DragDropEffects.Copy | DragDropEffects.Move | DragDropEffects.Link;
        }

        private void OnModalContainerDrop(object? sender, DragEventArgs e) { }

        private void WireViewModelEvents()
        {
            // ModalRequested and HideModalRequested are no longer needed for MVVM-driven modals
            // but we keep them for legacy code-behind modals if any remain
            ViewModel.ModalRequested += OnModalRequested;
            ViewModel.HideModalRequested += (s, e) => HideModalContent();

            ViewModel.OnIsAnimatingChangedEvent += (s, isAnimating) =>
            {
                if (_shaderBackground != null)
                {
                    Dispatcher.UIThread.Post(
                        () =>
                        {
                            _shaderBackground.AnimationEnabled = isAnimating;
                        },
                        DispatcherPriority.Render
                    );
                }
            };

            ViewModel.OnAuthorEditActivated += (s, e) =>
            {
                // Direct field access from x:Name
                if (AuthorEdit != null)
                {
                    AuthorEdit.Focus();
                    AuthorEdit.SelectAll();
                }
            };

            ViewModel.WindowStateChangeRequested += OnWindowStateChangeRequested;
        }

        private void OnWindowStateChangeRequested(object? sender, bool enterFullscreen)
        {
            var window = this.VisualRoot as Window;
            if (window != null)
            {
                Dispatcher.UIThread.Post(
                    () =>
                    {
                        if (enterFullscreen)
                        {
                            window.WindowState = WindowState.FullScreen;
                        }
                        else
                        {
                            window.WindowState = WindowState.Normal;
                        }
                    },
                    DispatcherPriority.Render
                );
            }
        }

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
        /// MIXED: Creates FilterSelectionModal UserControl, wires up ViewModel events, handles modal close logic
        /// VIEW: UserControl creation and presentation via ShowModalContent
        /// BUSINESS: Service resolution, ViewModel configuration, filter selection flow
        /// </summary>
        public void ShowSearchModal()
        {
            _previousModalContent = null;
            _previousModalTitle = null;

            var configurationService = App.GetService<IConfigurationService>()
                ?? throw new InvalidOperationException("IConfigurationService not registered");
            var filterService = App.GetService<IFilterService>()
                ?? throw new InvalidOperationException("IFilterService not registered");
            var filterSelectionVM = App.GetService<FilterSelectionModalViewModel>()
                ?? throw new InvalidOperationException("FilterSelectionModalViewModel not registered");
            filterSelectionVM.Configure(
                enableSearch: true,
                enableEdit: true,
                enableCopy: false,
                enableDelete: false,
                enableAnalyze: false
            );
            var filterSelectionModal = new FilterSelectionModal(
                filterSelectionVM,
                configurationService,
                filterService
            );

            filterSelectionVM.ModalCloseRequested += async (s, e) =>
            {
                if (filterSelectionVM.Result == null)
                {
                    return;
                }

                var result = filterSelectionVM.Result;

                if (result.Cancelled)
                {
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
                        if (result.FilterId != null && ViewModel != null)
                        {
                            _previousModalContent = filterSelectionModal;
                            _previousModalTitle = "🔍 Select Filter";

                            try
                            {
                                var configPath = await ViewModel.GetFilterConfigPathAsync(
                                    result.FilterId
                                );
                                if (configPath != null)
                                {
                                    HideModalContent();
                                    this.ShowSearchModal(configPath);
                                }
                            }
                            catch (Exception ex)
                            {
                                if (ViewModel != null)
                                    ViewModel.IsModalVisible = false;
                                HideModalContent();

                                DebugLogger.LogError(
                                    "BalatroMainMenu",
                                    $"Failed to show search modal with filter: {ex.Message}"
                                );

                                // Show error using existing modal system
                                var errorText = $"Failed to load filter:\n\n{ex.Message}";
                                var errorContent = new StackPanel
                                {
                                    Margin = new Thickness(20),
                                    Children =
                                    {
                                        new TextBlock
                                        {
                                            Text = "Error",
                                            FontSize = 24,
                                            FontWeight = FontWeight.Normal,
                                            Margin = new Thickness(0, 0, 0, 10),
                                        },
                                        new TextBlock
                                        {
                                            Text = errorText,
                                            TextWrapping = TextWrapping.Wrap,
                                        },
                                    },
                                };
                                var errorControl = new UserControl { Content = errorContent };
                                ShowModalContent(errorControl, "❌ ERROR");
                                return;
                            }
                        }
                        break;

                    case Models.FilterAction.Edit:
                        if (result.FilterId != null)
                        {
                            await Dispatcher.UIThread.InvokeAsync(() =>
                                ShowFiltersModalDirectAsync(result.FilterId)
                            );
                        }
                        break;

                    case Models.FilterAction.CreateNew:
                        var filterName = await ShowFilterNameInputDialog();
                        if (!string.IsNullOrWhiteSpace(filterName))
                        {
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
                            DebugLogger.Log(
                                "BalatroMainMenu",
                                "Create filter cancelled - staying in filter selection"
                            );
                        }
                        break;
                }
            };

            ShowModalContent(filterSelectionModal, "🔍 Select Filter");
        }

        /// <summary>
        /// Show search modal with specific filter loaded
        /// MIXED: Creates SearchModal UserControl, loads filter, wires up desktop icon creation
        /// VIEW: UserControl creation and presentation via ShowModalContent
        /// BUSINESS: Filter loading, search state management, desktop icon registration
        /// </summary>
        internal async Task ShowSearchModalWithFilterAsync(string configPath)
        {
            try
            {
                var searchContent = new Modals.SearchModal(ViewModel.SearchModalViewModel);
                searchContent.ViewModel.MainMenu = this;

                if (!string.IsNullOrEmpty(configPath))
                {
                    var platformServices = ServiceHelper.GetRequiredService<IPlatformServices>();
                    bool exists = await platformServices.FileExistsAsync(configPath);

                    if (!exists)
                    {
                        throw new InvalidOperationException($"Filter file not found: {configPath}");
                    }

                    await searchContent.ViewModel.LoadFilterAsync(configPath);

                    if (string.IsNullOrEmpty(searchContent.ViewModel.CurrentFilterPath))
                    {
                        throw new InvalidOperationException(
                            $"ASSERT FAILED: Filter did not load! Path: {configPath}"
                        );
                    }

                    searchContent.ViewModel.SelectedTabIndex = 0;
                }
                else
                {
                    throw new InvalidOperationException(
                        $"ASSERT FAILED: Filter file does not exist! Path: {configPath}"
                    );
                }

                searchContent.ViewModel.CreateShortcutRequested += (sender, cfgPath) =>
                {
                    var modalSearchId = searchContent.ViewModel.CurrentSearchId;
                    if (!string.IsNullOrEmpty(modalSearchId))
                    {
                        ShowSearchDesktopIcon(modalSearchId, cfgPath);
                    }
                };

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
                DebugLogger.LogError(
                    "BalatroMainMenu",
                    $"Failed to show search modal with filter: {ex.Message}"
                );
            }
        }

        /// <summary>
        /// Show filters modal (for creating/managing filters) - now uses FilterSelectionModal as gateway
        /// MIXED: Creates FilterSelectionModal UserControl, wires up ViewModel events, handles filter actions
        /// VIEW: UserControl creation and presentation via ShowModalContent
        /// BUSINESS: Service resolution, ViewModel configuration, filter creation/edit/copy flow
        /// </summary>
        public void ShowFiltersModal()
        {
            _previousModalContent = null;
            _previousModalTitle = null;

            var configurationService = App.GetService<IConfigurationService>()
                ?? throw new InvalidOperationException("IConfigurationService not registered");
            var filterService = App.GetService<IFilterService>()
                ?? throw new InvalidOperationException("IFilterService not registered");
            var filterSelectionVM = App.GetService<FilterSelectionModalViewModel>()
                ?? throw new InvalidOperationException("FilterSelectionModalViewModel not registered");
            filterSelectionVM.Configure(
                enableSearch: false,
                enableEdit: true,
                enableCopy: true,
                enableDelete: true,
                enableAnalyze: false
            );
            var filterSelectionModal = new FilterSelectionModal(
                filterSelectionVM,
                configurationService,
                filterService
            );

            filterSelectionVM.ModalCloseRequested += async (s, e) =>
            {
                var result = filterSelectionVM.Result;

                if (result.Cancelled)
                {
                    if (ViewModel != null)
                    {
                        ViewModel.IsModalVisible = false;
                    }
                    HideModalContent();
                    return;
                }

                switch (result.Action)
                {
                    case Models.FilterAction.CreateNew:
                        var filterName = await ShowFilterNameInputDialog();
                        if (!string.IsNullOrWhiteSpace(filterName))
                        {
                            var newFilterId = await CreateNewFilterWithName(filterName);
                            if (!string.IsNullOrEmpty(newFilterId))
                            {
                                await Dispatcher.UIThread.InvokeAsync(() =>
                                    ShowFiltersModalDirectAsync(newFilterId)
                                );
                            }
                        }
                        break;
                    case Models.FilterAction.Edit:
                        if (result.FilterId != null)
                        {
                            await Dispatcher.UIThread.InvokeAsync(() =>
                                ShowFiltersModalDirectAsync(result.FilterId)
                            );
                        }
                        break;
                    case Models.FilterAction.Copy:
                        if (result.FilterId != null)
                        {
                            var filterService = ServiceHelper.GetRequiredService<IFilterService>();
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
                        }
                        break;
                }
            };

            ShowModalContent(filterSelectionModal, "🎨 Select Filter");
        }

        /// <summary>
        /// Show filters modal directly (internal use - called after filter selection)
        /// MIXED: Creates FiltersModal UserControl, loads filter data, wires up close callback
        /// VIEW: UserControl creation and presentation via ShowModalContent
        /// BUSINESS: Filter file loading, ViewModel configuration, filter reload
        /// </summary>
        public async Task ShowFiltersModalDirectAsync(string filterId)
        {
            if (string.IsNullOrEmpty(filterId))
            {
                throw new ArgumentException("FilterId cannot be null or empty.", nameof(filterId));
            }

            try
            {
                // Back to the REAL FiltersModal with visual item shelf and card display!
                var filtersModal = new Modals.FiltersModal(ViewModel.FiltersModalViewModel);

                // Check if initialization succeeded
                if (filtersModal.ViewModel == null)
                {
                    DebugLogger.LogError(
                        "BalatroMainMenu",
                        "FiltersModal initialization failed - ViewModel is null"
                    );
                    HideModalContent(); // Clean up modal state
                    return;
                }

                // Wire up RequestClose callback so Finish & Close button works
                filtersModal.ViewModel.RequestClose = () =>
                {
                    DebugLogger.Log("BalatroMainMenu", "FiltersModal RequestClose invoked");
                    HideModalContent();
                };

                // Load the filter data
                var filtersDir = AppPaths.FiltersDir;
                // Try .jaml first, then .json as fallback
                var filterPath = System.IO.Path.Combine(filtersDir, filterId + ".jaml");

                if (!System.IO.File.Exists(filterPath))
                {
                    filterPath = System.IO.Path.Combine(filtersDir, filterId + ".json");
                }

                filtersModal.ViewModel.CurrentFilterPath = filterPath;

                DebugLogger.Log(
                    "BalatroMainMenu",
                    $"🔄 Loading filter for editing: {filterPath}"
                );

                await filtersModal.ViewModel.ReloadVisualFromSavedFileCommand.ExecuteAsync(
                    null
                );

                DebugLogger.Log("BalatroMainMenu", $"✅ Filter loaded for editing: {filterId}");

                // THEN show the modal with loaded content
                var modal = new StandardModal("🎨 Filter Designer");
                modal.SetContent(filtersModal);
                modal.BackClicked += (s, e) => HideModalContent();
                // Keep backdrop visible during transition to prevent flicker
                ShowModalContent(modal, keepBackdrop: true);
            }
            catch (Exception ex)
            {
                DebugLogger.LogError(
                    "BalatroMainMenu",
                    $"ShowFiltersModalDirectAsync failed: {ex.Message}"
                );
                throw;
            }
        }

        /// <summary>
        /// Show Avalonia input dialog for filter name (Browser-compatible)
        /// MIXED: Creates FilterNameInputDialog UserControl, manages modal stack for cancel/restore
        /// VIEW: UserControl creation and presentation via ShowModalContent
        /// BUSINESS: Modal stack management, default name generation, dialog result handling
        /// </summary>
        private async Task<string?> ShowFilterNameInputDialog(string? defaultName = null)
        {
            // Save current modal state to restore if cancelled
            var previousContent = _activeModalContent;
            var previousTitle = _mainTitleText?.Text;

            // Generate default name if not provided
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

            var dialog = new FilterNameInputDialog(
                defaultName == null ? "Create New Filter" : "Copy Filter",
                defaultName == null ? "CREATE" : "COPY",
                defaultText
            );

            // Show it
            ShowModalContent(dialog, defaultName == null ? "Create Filter" : "Copy Filter");

            // Wait for user input
            var result = await dialog.GetResultAsync();

            // If cancelled, restore previous modal
            if (result == null && previousContent != null)
            {
                ShowModalContent(previousContent, previousTitle, keepBackdrop: true);
            }

            return result;
        }

        /// <summary>
        /// Create a new filter with the given name
        /// BUSINESS: Filter file creation, JAML generation, file I/O
        /// NOTE: This is business logic that could be moved to a service or ViewModel
        /// </summary>
        private async Task<string?> CreateNewFilterWithName(string filterName)
        {
            try
            {
                var filtersDir = AppPaths.FiltersDir;

                System.IO.Directory.CreateDirectory(filtersDir);

                // Generate unique ID
                var filterId = $"{filterName.Replace(" ", "").ToLower()}_{Guid.NewGuid():N}";
                var filterPath = System.IO.Path.Combine(filtersDir, filterId + ".jaml");

                // JAML is the source format — write it literally instead of building a
                // JamlConfig just to serialize it back out. JamlConfigLoader reads .jaml.
                var safeName = filterName.Replace("\"", "\\\"");
                var jaml =
                    $"name: \"{safeName}\"\n"
                    + "description: Created with visual filter builder\n"
                    + "author: pifreak\n"
                    + $"dateCreated: \"{DateTime.UtcNow:o}\"\n"
                    + "deck: Red\n"
                    + "stake: White\n"
                    + "must: []\n"
                    + "should: []\n"
                    + "mustNot: []\n";

                await System.IO.File.WriteAllTextAsync(filterPath, jaml);

                DebugLogger.Log("BalatroMainMenu", $"✅ Created new filter: {filterId}");
                return filterId;
            }
            catch (Exception ex)
            {
                DebugLogger.LogError("BalatroMainMenu", $"Failed to create filter: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Show settings modal
        /// VIEW-ONLY: Creates SettingsModal UserControl and presents it via ShowModalContent
        /// </summary>
        public void ShowSettingsModal()
        {
            // Clear modal stack when starting fresh from main menu
            _previousModalContent = null;
            _previousModalTitle = null;

            var settingsModal = new Modals.SettingsModal();

            var modal = new StandardModal("SETTINGS");
            modal.Squeeze = true; // Use compact sizing for settings
            modal.SetContent(settingsModal);
            modal.BackClicked += (s, e) => HideModalContent();

            ShowModalContent(modal, "SETTINGS");
        }

        /// <summary>
        /// Show word lists modal from Settings (with back navigation to Settings)
        /// VIEW-ONLY: Creates WordListsModal UserControl and presents it via ShowModalContent
        /// </summary>
        public void ShowWordListsModal()
        {
            // Clear modal stack when starting fresh
            _previousModalContent = null;
            _previousModalTitle = null;

            var wordListsModal = new Modals.WordListsModal();
            var modal = new StandardModal("WORD LISTS");
            modal.SetContent(wordListsModal);
            modal.BackClicked += (s, e) => HideModalContent();
            ShowModalContent(modal, "WORD LISTS");
        }

        /// <summary>
        /// Show word lists modal from Settings (with back navigation to Settings)
        /// VIEW-ONLY: Creates WordListsModal UserControl with back navigation stack management
        /// </summary>
        public void ShowWordListsModalFromSettings()
        {
            // Save Settings modal for back navigation
            if (_activeModalContent != null)
            {
                _previousModalContent = _activeModalContent;
                _previousModalTitle = "SETTINGS";
            }

            var wordListsModal = new Modals.WordListsModal();
            var modal = new StandardModal("WORD LISTS");
            modal.SetContent(wordListsModal);
            modal.BackClicked += (s, e) =>
            {
                // Check if we have a previous modal to return to
                if (_previousModalContent != null && _previousModalTitle != null)
                {
                    // Return to Settings modal
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
            ShowModalContent(modal, "WORD LISTS", keepBackdrop: true);
        }

        /// <summary>
        /// Show credits modal from Settings (with back navigation to Settings)
        /// VIEW-ONLY: Creates CreditsModal UserControl with back navigation stack management
        /// </summary>
        public void ShowCreditsModal()
        {
            // Save Settings modal for back navigation
            if (_activeModalContent != null)
            {
                _previousModalContent = _activeModalContent;
                _previousModalTitle = "SETTINGS";
            }

            var creditsModal = new Modals.CreditsModal(new ViewModels.CreditsModalViewModel());
            var modal = new StandardModal("CREDITS");
            modal.SetContent(creditsModal);
            modal.BackClicked += (s, e) =>
            {
                // Check if we have a previous modal to return to
                if (_previousModalContent != null && _previousModalTitle != null)
                {
                    // Return to Settings modal
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
            ShowModalContent(modal, "CREDITS", keepBackdrop: true);
        }

        /// <summary>
        /// Show tools modal
        /// VIEW-ONLY: Resolves ToolsModal from DI and presents it via ShowModalContent
        /// </summary>
        public void ShowToolsModal()
        {
            var toolsModal = App.GetService<Modals.ToolsModal>();
            if (toolsModal == null)
            {
                DebugLogger.LogError("BalatroMainMenu", "ToolsModal could not be resolved from services");
                return;
            }
            var modal = new StandardModal("TOOLS");
            modal.SetContent(toolsModal);
            modal.BackClicked += (s, ev) => HideModalContent();
            ShowModalContent(modal, "TOOLS");
        }

        /// <summary>
        /// Show analyze modal - skips FilterSelectionModal and goes straight to analyzer
        /// VIEW-ONLY: Delegates to OpenAnalyzer which creates AnalyzeModal UserControl
        /// </summary>
        public void ShowAnalyzeModal()
        {
            OpenAnalyzer(null);
        }

        /// <summary>
        /// Show Audio Visualizer Settings modal
        /// VIEW-ONLY: Creates AudioVisualizerSettingsModal UserControl with ViewModel and presents it via ShowModalContent
        /// </summary>
        public void ShowAudioVisualizerSettingsModal()
        {
            _previousModalContent = null;
            _previousModalTitle = null;

            var userProfileService = App.GetService<UserProfileService>()
                ?? throw new InvalidOperationException("UserProfileService not registered");
            var viewModel = new ViewModels.AudioVisualizerSettingsModalViewModel(userProfileService);
            var audioVisualizerSettingsModal = new Modals.AudioVisualizerSettingsModal();
            audioVisualizerSettingsModal.DataContext = viewModel;
            var modal = new StandardModal("AUDIO VISUALIZER");
            modal.SetContent(audioVisualizerSettingsModal);
            modal.BackClicked += (s, ev) => HideModalContent();
            ShowModalContent(modal, "AUDIO VISUALIZER");
        }

        /// <summary>
        /// Show Filter Selection modal
        /// VIEW-ONLY: Delegates to ShowSearchModal (FilterSelectionModal is part of Search flow)
        /// </summary>
        public void ShowFilterSelectionModal()
        {
            // FilterSelectionModal is part of the Search flow
            ShowSearchModal();
        }

        /// <summary>
        /// Open analyzer with the specified filter (filter selection happens inside analyzer)
        /// MIXED: Creates AnalyzeModal UserControl via ViewModel factory
        /// VIEW: UserControl creation and presentation via ShowModalContent
        /// BUSINESS: ViewModel creation via factory, analyzer initialization
        /// </summary>
        private void OpenAnalyzer(string? filterId)
        {
            var analyzeVm =
                ViewModel?.CreateAnalyzeModalViewModel()
                ?? throw new InvalidOperationException(
                    "BalatroMainMenu requires ViewModel with CreateAnalyzeModalViewModel."
                );
            var analyzeModal = new AnalyzeModal(analyzeVm);
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
            // Wire the event-FX system to the live shader, then trigger the intro animation.
            // Connect must come first: TriggerEvent is a no-op until the shader is reachable.
            var eventFXService = ServiceHelper.GetService<EventFXService>();
            eventFXService?.Connect(() => CurrentShaderParameters, ApplyShaderParameters);
            eventFXService?.TriggerEvent(EventFXType.IntroAnimation, 5.0);

            // Load visualizer settings
            if (_background is BalatroShaderBackground shader)
            {
                ViewModel.LoadAndApplyVisualizerSettings(shader);
                // Connect the FFT stem levels to the shader so the background
                // reacts to the music (settings choose which stem drives what).
                ViewModel.WireAudioAnalysisToShader(shader);
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
                // Direct field access from x:Name
                if (source == MusicToggleButton)
                {
                    return; // Let the button's click handler toggle the popup
                }

                // Check if the click source is a child of the music button
                var parent = source;
                while (parent != null)
                {
                    if (parent == MusicToggleButton)
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

        #region Modal Hosting (View-only layer - manages UserControl presentation, animations, transitions)

        /// <summary>
        /// Updates the main title text
        /// VIEW-ONLY: Updates TextBlock text property
        /// </summary>
        public void SetTitle(string title)
        {
            // Direct field access from x:Name
            _mainTitleText = MainTitleText;
            if (_mainTitleText != null)
            {
                _mainTitleText.Text = title;
            }
        }

        /// <summary>
        /// Show a UserControl as a modal overlay - Balatro style (NO wimpy fades, just POP!)
        /// VIEW-ONLY: Delegates to ShowModalContent for presentation
        /// </summary>
        public void ShowModal(UserControl content, string title)
        {
            ShowModalContent(content, title);
        }

        /// <summary>
        /// Show a UserControl as a modal overlay - Balatro style (NO wimpy fades, just POP!)
        /// VIEW-ONLY: Core modal presentation logic - manages overlay, content wrapper, transitions
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
            DebugLogger.LogImportant("BalatroMainMenu", $"[ShowModalContent] Attempting to set wrapper content to: {content?.GetType().FullName ?? "NULL"}");
            _modalContentWrapper.Content = content;
            DebugLogger.LogImportant("BalatroMainMenu", $"[ShowModalContent] Wrapper content is now: {_modalContentWrapper.Content?.GetType().FullName ?? "NULL"}");

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
        /// VIEW-ONLY: Animation-only transition logic
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
                DebugLogger.LogError(
                    "BalatroMainMenu",
                    $"TransitionToNewModalAsync failed: {ex.Message}"
                );
                throw;
            }
        }

        /// <summary>
        /// Show modal with pop-up bounce animation (Balatro-style)
        /// VIEW-ONLY: Animation-only presentation logic
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
                DebugLogger.LogError(
                    "BalatroMainMenu",
                    $"ShowModalWithAnimationAsync failed: {ex.Message}"
                );
                throw;
            }
        }

        /// <summary>
        /// Hide the modal overlay
        /// VIEW-ONLY: Delegates to HideModalContent
        /// </summary>
        public void HideModal()
        {
            HideModalContent();
        }

        /// <summary>
        /// Hide the modal overlay (internal)
        /// VIEW-ONLY: Core modal hiding logic - manages overlay visibility, content clearing
        /// </summary>
        public void HideModalContent()
        {
            if (_modalContainer == null || _modalContentWrapper == null)
                return;

            // Capture the content we are hiding so we only clear it if no new modal has opened since.
            var contentToClear = _modalContentWrapper.Content;

            // NEVER lose your spot: if a search modal is closing (any path - Back
            // button, minimize, etc.) while its search is still running, drop a
            // desktop icon so the user can jump right back in.
            var searchModal = FindSearchModalIn(contentToClear);
            if (searchModal?.ViewModel is { } searchVm
                && !string.IsNullOrEmpty(searchVm.CurrentSearchId)
                && searchVm.IsSearching)
            {
                ShowSearchDesktopIcon(searchVm.CurrentSearchId, searchVm.CurrentFilterPath);
            }

            // PERFORMANCE FIX: Defer content clearing to prevent audio crackling
            // FiltersModal has thousands of controls - clearing synchronously blocks UI thread
            // which causes audio buffer underruns
            _modalContainer.IsVisible = false;
            _activeModalContent = null;
            SetTitle("");

            // Re-enable buttons by clearing modal state
            if (ViewModel != null)
            {
                ViewModel.IsModalVisible = false;
            }

            // Clear content on background thread to avoid blocking audio
            Dispatcher.UIThread.Post(
                () =>
                {
                    if (_modalContentWrapper.Content == contentToClear)
                    {
                        _modalContentWrapper.Content = null;
                    }
                },
                DispatcherPriority.Background
            );

            // Audio manager cleanup handled via IAudioManager interface
        }

        /// <summary>
        /// Digs a SearchModal out of closing modal content (it may be wrapped in a
        /// StandardModal or be the content directly).
        /// VIEW-ONLY: Helper for visual tree inspection
        /// </summary>
        private static Modals.SearchModal? FindSearchModalIn(object? content)
        {
            return content switch
            {
                Modals.SearchModal direct => direct,
                Modals.StandardModal standard => standard.GetContent() as Modals.SearchModal,
                _ => null,
            };
        }

        #endregion


        #region Shader Management

        /// <summary>
        /// Apply all shader parameters at once via ShaderParameters model
        /// VIEW-ONLY: Delegates to shader background control
        /// </summary>
        /// <summary>
        /// The last parameters pushed through <see cref="ApplyShaderParameters"/> — the
        /// starting point for event-FX transitions so they blend from wherever the shader
        /// actually is instead of a hardcoded state.
        /// </summary>
        public Models.ShaderParameters CurrentShaderParameters { get; private set; } =
            Extensions.VisualizerPresetExtensions.CreateDefaultNormalParameters();

        public void ApplyShaderParameters(Models.ShaderParameters parameters)
        {
            try
            {
                CurrentShaderParameters = parameters;
                if (_shaderBackground != null)
                {
                    _shaderBackground.SetTime(parameters.TimeSpeed);
                    _shaderBackground.SetSpinTime(parameters.SpinTimeSpeed);
                    _shaderBackground.SetMainColor(parameters.MainColor);
                    _shaderBackground.SetAccentColor(parameters.AccentColor);
                    _shaderBackground.SetBackgroundColor(parameters.BackgroundColor);
                    _shaderBackground.SetContrast(parameters.Contrast);
                    _shaderBackground.SetSpinAmount(parameters.SpinAmount);
                    _shaderBackground.SetParallax(parameters.ParallaxX, parameters.ParallaxY);
                    _shaderBackground.SetZoomScale(parameters.ZoomScale);
                    _shaderBackground.SetSaturationAmount(parameters.SaturationAmount);
                    _shaderBackground.SetSaturationAmount2(parameters.SaturationAmount2);
                    _shaderBackground.SetPixelSize(parameters.PixelSize);
                    _shaderBackground.SetSpinEase(parameters.SpinEase);
                    _shaderBackground.SetLoopCount(parameters.LoopCount);
                }
            }
            catch (Exception ex)
            {
                DebugLogger.LogError(
                    "BalatroMainMenu",
                    $"Failed to apply shader parameters: {ex.Message}"
                );
            }
        }

        /// <summary>
        /// Get the BalatroShaderBackground control for direct shader access
        /// Used by ViewModel for direct shader parameter manipulation
        /// </summary>
        public BalatroShaderBackground? GetShaderBackground()
        {
            return _background as BalatroShaderBackground;
        }

        /// <summary>
        /// Set the volume for a specific track in the audio manager
        /// VIEW-ONLY: Audio routing logic
        /// </summary>
        internal void SetTrackVolume(string trackName, float volume)
        {
            DebugLogger.Log("BalatroMainMenu", $"SetTrackVolume called: {trackName} = {volume}");
        }

        #endregion

        #region Desktop Icon Management

        /// <summary>
        /// Shows the search modal for an existing search instance
        /// VIEW-ONLY: Creates SearchModal UserControl and presents it via ShowModalContent
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

                var searchContent = new Modals.SearchModal(ViewModel.SearchModalViewModel);
                // Set the MainMenu reference so CREATE NEW FILTER button works
                searchContent.ViewModel.MainMenu = this;
                searchContent.CloseRequested += (s, e) => HideModalContent();
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
                        ViewModel.ShowSearchDesktopIcon(modalSearchId, cfgPath);
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

        /// <summary>
        /// Adds a search "desktop icon" - delegated to ViewModel
        /// VIEW-ONLY: Wrapper for ViewModel method
        /// </summary>
        public void ShowSearchDesktopIcon(string searchId, string? configPath = null)
        {
            ViewModel.ShowSearchDesktopIcon(searchId, configPath);
        }

        /// <summary>
        /// Removes a search desktop icon - delegated to ViewModel
        /// VIEW-ONLY: Wrapper for ViewModel method
        /// </summary>
        public void RemoveSearchDesktopIcon(string searchId)
        {
            ViewModel.RemoveSearchDesktopIcon(searchId);
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
                var audioManager = ServiceHelper.GetService<IAudioManager>();
                if (audioManager != null && _audioAnalysisHandler != null)
                {
                    audioManager.AudioAnalysisUpdated -= _audioAnalysisHandler;
                    _audioAnalysisHandler = null;
                }

                // Detach the music-reactivity bridge owned by the ViewModel.
                ViewModel.UnwireAudioAnalysisFromShader();

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
                    // Direct field access from x:Name in StandardModal
                    var filtersModal = modal.ModalContent?.Content as Modals.FiltersModal;
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
                    TopLevel.GetTopLevel(this)?.Focus();
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

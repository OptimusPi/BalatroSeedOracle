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
        private Grid? _modalContainer;
        private Border? _modalOverlay;
        private ContentControl? _modalContentWrapper;
        private Control? _background;
        private BalatroShaderBackground? _shaderBackground;

        /// <summary>
        /// Public accessor for the shader background control (for transition effects)
        /// </summary>
        public BalatroShaderBackground? ShaderBackground => _shaderBackground;

        /// <summary>
        /// Public accessor for the desktop widget canvas (x:Name="DesktopCanvas").
        /// Used by Desktop project to add platform-specific widgets; shared code uses direct field.
        /// </summary>
        public Grid? DesktopCanvasHost => DesktopCanvas;

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
            : this(throwForDesignTimeOnly: true)
        {
        }

        private BalatroMainMenu(bool throwForDesignTimeOnly)
        {
            if (throwForDesignTimeOnly)
                throw new InvalidOperationException(
                    "Do not instantiate BalatroMainMenu() with no args. Resolve from DI: GetRequiredService<BalatroMainMenu>() (ViewModel is injected by the container).");
            ViewModel = null!;
            DataContext = null;
            InitializeComponent();
            WireViewModelEvents();
            this.Loaded += OnLoaded;
        }

        public BalatroMainMenu(BalatroMainMenuViewModel viewModel)
        {
            ViewModel = viewModel;
            DataContext = ViewModel;

            InitializeComponent();
            WireViewModelEvents();

            this.Loaded += OnLoaded;
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);

            // Direct field access from x:Name - no FindControl anti-pattern!
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
                    Avalonia.Threading.Dispatcher.UIThread.Post(
                        () =>
                        {
                            _shaderBackground.AnimationEnabled = isAnimating;
                        },
                        Avalonia.Threading.DispatcherPriority.Render
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
                Avalonia.Threading.Dispatcher.UIThread.Post(
                    () =>
                    {
                        if (enterFullscreen)
                        {
                            window.WindowState = WindowState.FullScreen;

                            // Direct field access from x:Name
                            if (DesktopCanvas != null)
                            {
                                DesktopCanvas.Margin = new Thickness(0, -30, 0, 0);
                            }
                        }
                        else
                        {
                            window.WindowState = WindowState.Normal;

                            // Direct field access from x:Name
                            if (DesktopCanvas != null)
                            {
                                DesktopCanvas.Margin = new Thickness(0);
                            }
                        }
                    },
                    Avalonia.Threading.DispatcherPriority.Render
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

        private void ShowSearchModal()
        {
            _previousModalContent = null;
            _previousModalTitle = null;

            var filterSelectionVM = new FilterSelectionModalViewModel(
                enableSearch: true,
                enableEdit: true,
                enableCopy: false,
                enableDelete: false,
                enableAnalyze: false
            );
            var configurationService = ServiceHelper.GetRequiredService<IConfigurationService>();
            var filterService = ServiceHelper.GetRequiredService<IFilterService>();
            var filterSelectionModal = new FilterSelectionModal(filterSelectionVM, configurationService, filterService);

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
                            _previousModalTitle = "🔍 SELECT FILTER";

                            try
                            {
                                var configPath = await ViewModel.GetFilterConfigPathAsync(result.FilterId);
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

                                Helpers.DebugLogger.LogError(
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
                                            FontWeight = FontWeight.Bold,
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
                            _ = ShowFiltersModalDirectAsync(result.FilterId);
                        else
                            _ = ShowFiltersModalDirectAsync();
                        break;

                    case Models.FilterAction.CreateNew:
                        _ = ShowFiltersModalDirectAsync();
                        break;
                }
            };

            ShowModalContent(filterSelectionModal, "🔍 SELECT FILTER");
        }

        private async Task ShowSearchModalWithFilterAsync(string configPath)
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
            var filterSelectionVM = new FilterSelectionModalViewModel(
                enableSearch: false,
                enableEdit: true,
                enableCopy: true,
                enableDelete: true,
                enableAnalyze: false
            );
            var configurationService = ServiceHelper.GetRequiredService<IConfigurationService>();
            var filterService = ServiceHelper.GetRequiredService<IFilterService>();
            var filterSelectionModal = new FilterSelectionModal(filterSelectionVM, configurationService, filterService);

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
        public async Task ShowFiltersModalDirectAsync(string? filterId = null)
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
                    var filtersDir = AppPaths.FiltersDir;

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
                var filtersModal = new Modals.FiltersModal(ViewModel.FiltersModalViewModel);

                // Check if initialization succeeded
                if (filtersModal.ViewModel == null)
                {
                    Helpers.DebugLogger.LogError(
                        "BalatroMainMenu",
                        "FiltersModal initialization failed - ViewModel is null"
                    );
                    HideModalContent(); // Clean up modal state
                    return;
                }

                // Wire up RequestClose callback so Finish & Close button works
                filtersModal.ViewModel.RequestClose = () =>
                {
                    Helpers.DebugLogger.Log("BalatroMainMenu", "FiltersModal RequestClose invoked");
                    HideModalContent();
                };

                // Load the filter data FIRST if provided
                if (!string.IsNullOrEmpty(filterId))
                {
                    var filtersDir = AppPaths.FiltersDir;
                    // Try .jaml first, then .json as fallback
                    var filterPath = System.IO.Path.Combine(filtersDir, filterId + ".jaml");

                    if (!System.IO.File.Exists(filterPath))
                    {
                        filterPath = System.IO.Path.Combine(filtersDir, filterId + ".json");
                    }

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

                // Verify filter was loaded properly
                if (string.IsNullOrEmpty(filterId))
                {
                    Helpers.DebugLogger.LogError(
                        "BalatroMainMenu",
                        "Filter Designer opened without a valid filter! FilterId must be provided."
                    );
                    HideModalContent();
                    return;
                }

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
        /// Show Avalonia input dialog for filter name (Browser-compatible)
        /// </summary>
        private async Task<string?> ShowFilterNameInputDialog(string? defaultName = null)
        {
            // Save current modal state to restore if cancelled
            var previousContent = _activeModalContent;
            var previousTitle = _mainTitleText?.Text;

            var tcs = new TaskCompletionSource<string?>();

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

            var textBox = new TextBox
            {
                Text = defaultText,
                Watermark = "Enter filter name...",
                Margin = new Thickness(0, 10, 0, 0),
                FontSize = 18,
                Padding = new Thickness(12, 8),
                MinHeight = 45,
            };

            var okButton = new Button
            {
                Content = "CREATE",
                Classes = { "btn-blue" },
                MinWidth = 120,
                Height = 45,
            };

            var cancelButton = new Button
            {
                Content = "CANCEL",
                Classes = { "btn-red" },
                MinWidth = 120,
                Height = 45,
            };

            okButton.Click += (s, e) =>
            {
                tcs.TrySetResult(textBox.Text);
            };

            cancelButton.Click += (s, e) =>
            {
                tcs.TrySetResult(null);
            };

            // Main container with border and rounded corners
            var mainBorder = new Border
            {
                Background = this.FindResource("DarkBorder") as Avalonia.Media.IBrush,
                BorderBrush = this.FindResource("LightGrey") as Avalonia.Media.IBrush,
                BorderThickness = new Thickness(3),
                CornerRadius = new CornerRadius(16),
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
                Width = 450,
                Height = 250,
            };

            var mainGrid = new Grid { RowDefinitions = new RowDefinitions("Auto,*,Auto") };

            // Title bar
            var titleBar = new Border
            {
                [Grid.RowProperty] = 0,
                Background = this.FindResource("ModalGrey") as Avalonia.Media.IBrush,
                CornerRadius = new CornerRadius(14, 14, 0, 0),
                Padding = new Thickness(20, 12),
            };

            var titleText = new TextBlock
            {
                Text = defaultName == null ? "Create New Filter" : "Copy Filter",
                FontSize = 24,
                Foreground = this.FindResource("White") as Avalonia.Media.IBrush,
                HorizontalAlignment = HorizontalAlignment.Center,
            };

            titleBar.Child = titleText;
            mainGrid.Children.Add(titleBar);

            // Content area
            var contentBorder = new Border
            {
                [Grid.RowProperty] = 1,
                Background = this.FindResource("DarkBackground") as Avalonia.Media.IBrush,
                Padding = new Thickness(24),
            };

            var contentStack = new StackPanel { Spacing = 8 };
            contentStack.Children.Add(
                new TextBlock
                {
                    Text = "Filter Name:",
                    FontSize = 16,
                    Foreground = this.FindResource("White") as Avalonia.Media.IBrush,
                }
            );
            contentStack.Children.Add(textBox);

            contentBorder.Child = contentStack;
            mainGrid.Children.Add(contentBorder);

            // Button area
            var buttonBorder = new Border
            {
                [Grid.RowProperty] = 2,
                Background = this.FindResource("DarkBackground") as Avalonia.Media.IBrush,
                CornerRadius = new CornerRadius(0, 0, 14, 14),
                Padding = new Thickness(20, 12, 20, 20),
            };

            var buttonPanel = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                HorizontalAlignment = HorizontalAlignment.Center,
                Spacing = 12,
            };
            buttonPanel.Children.Add(okButton);
            buttonPanel.Children.Add(cancelButton);

            buttonBorder.Child = buttonPanel;
            mainGrid.Children.Add(buttonBorder);

            mainBorder.Child = mainGrid;

            // Wrap in UserControl for ShowModalContent
            var contentControl = new UserControl { Content = mainBorder };

            // Show it
            ShowModalContent(contentControl, defaultName == null ? "CREATE FILTER" : "COPY FILTER");

            // Focus textbox after a short delay
            Dispatcher.UIThread.Post(async () =>
            {
                await Task.Delay(100);
                textBox.Focus();
                textBox.SelectAll();
            });

            // Wait for user input
            var result = await tcs.Task;

            // If cancelled, restore previous modal
            if (result == null && previousContent != null)
            {
                ShowModalContent(previousContent, previousTitle, keepBackdrop: true);
            }

            return result;
        }

        /// <summary>
        /// Create a new filter with the given name
        /// </summary>
        private async Task<string?> CreateNewFilterWithName(string filterName)
        {
            try
            {
                var filtersDir = AppPaths.FiltersDir;

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
        /// </summary>
        public void ShowCreditsModalFromSettings()
        {
            // Save Settings modal for back navigation
            if (_activeModalContent != null)
            {
                _previousModalContent = _activeModalContent;
                _previousModalTitle = "SETTINGS";
            }

            var creditsModal = ServiceHelper.GetRequiredService<Modals.CreditsModal>();
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
        /// Show widget picker from Settings (with back navigation to Settings)
        /// </summary>
        public void ShowWidgetPickerFromSettings()
        {
            // Save Settings modal for back navigation
            if (_activeModalContent != null)
            {
                _previousModalContent = _activeModalContent;
                _previousModalTitle = "SETTINGS";
            }

            var widgetPicker = new Modals.WidgetPickerModal();
            var modal = new StandardModal("ADD WIDGETS");
            modal.Squeeze = true;
            modal.SetContent(widgetPicker);
            modal.BackClicked += (s, e) =>
            {
                if (_previousModalContent != null && _previousModalTitle != null)
                {
                    var previousContent = _previousModalContent;
                    var previousTitle = _previousModalTitle;
                    _previousModalContent = null;
                    _previousModalTitle = null;
                    ShowModalContent(previousContent, previousTitle, keepBackdrop: true);
                }
                else
                {
                    HideModalContent();
                }
            };
            ShowModalContent(modal, "ADD WIDGETS", keepBackdrop: true);
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

        public void ApplyShaderParameters(Models.ShaderParameters parameters)
        {
            try
            {
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
        /// Open analyzer with the specified filter (filter selection happens inside analyzer)
        /// </summary>
        private void OpenAnalyzer(string? filterId)
        {
            var analyzeVm = ViewModel?.CreateAnalyzeModalViewModel()
                ?? throw new InvalidOperationException("BalatroMainMenu requires ViewModel with CreateAnalyzeModalViewModel.");
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
            // Trigger intro animation event
            var eventFXService = ServiceHelper.GetService<EventFXService>();
            eventFXService?.TriggerEvent(EventFXType.IntroAnimation, 5.0);

            // Load visualizer settings
            if (_background is BalatroShaderBackground shader)
            {
                ViewModel.LoadAndApplyVisualizerSettings(shader);
            }

            // Check for resumable search
            ViewModel.CheckAndRestoreSearchIcon(ShowSearchDesktopIcon);

            // Restore saved search widgets
            RestoreSavedWidgets();

            // Set up click-away handler for popups (NOT for main modals which have back buttons)
            this.PointerPressed += OnPointerPressedForPopupClickAway;

            // Request focus so keyboard events work (F11, ESC)
            this.Focus();

            // Pass analyze modal factory to DayLatroWidget so it can show analyze modal without ServiceHelper
            if (DayLatroWidget?.ViewModel is DayLatroWidgetViewModel dwvm && ViewModel != null)
                dwvm.SetAnalyzeModalFactory(ViewModel.AnalyzeModalFactory);
        }

        /// <summary>
        /// Restore saved search widgets from UserProfile
        /// </summary>
        private void RestoreSavedWidgets()
        {
            try
            {
                var profileService = ServiceHelper.GetService<UserProfileService>();
                var searchManager = ServiceHelper.GetService<SearchManager>();

                if (profileService == null || searchManager == null)
                {
                    DebugLogger.LogError(
                        "BalatroMainMenu",
                        "Services not available for widget restoration"
                    );
                    return;
                }

                var profile = profileService.GetProfile();
                // Direct field access from x:Name
                if (DesktopCanvas == null)
                {
                    DebugLogger.LogError(
                        "BalatroMainMenu",
                        "DesktopCanvas not found for widget restoration"
                    );
                    return;
                }

                // Track widgets to remove (orphaned searches)
                var widgetsToRemove =
                    new System.Collections.Generic.List<Models.SavedSearchWidget>();

                // Restore each saved widget
                foreach (var savedWidget in profile.SavedSearchWidgets.ToList())
                {
                    // Try to get or restore the search instance
                    var searchInstance = searchManager.GetOrRestoreSearch(
                        savedWidget.SearchInstanceId
                    );

                    if (searchInstance == null)
                    {
                        // Search doesn't exist, mark for cleanup
                        widgetsToRemove.Add(savedWidget);
                        DebugLogger.Log(
                            "BalatroMainMenu",
                            $"Orphaned widget removed: {savedWidget.SearchInstanceId}"
                        );
                        continue;
                    }

                    // Check if search instance has valid filter data (not just "Unknown")
                    if (
                        searchInstance.FilterName == "Unknown"
                        || searchInstance.GetFilterConfig() == null
                    )
                    {
                        // Search exists but has no valid config - stale data, remove it
                        widgetsToRemove.Add(savedWidget);
                        DebugLogger.Log(
                            "BalatroMainMenu",
                            $"Stale widget removed (no filter config): {savedWidget.SearchInstanceId}"
                        );
                        continue;
                    }

                    // Create SearchWidget at saved position
                    var spriteService = Services.SpriteService.Instance;
                    // SearchWidget is desktop-only - registered in Desktop Program.cs
                    DebugLogger.Log("BalatroMainMenu", "SearchWidget is desktop-only feature");
                    return;
                    // Commented out - SearchWidget is desktop-only
                    /*
                    var viewModel = new SearchWidgetViewModel(searchInstance, spriteService);
                    var searchWidget = new Components.Widgets.SearchWidget
                    {
                        DataContext = viewModel,
                    };

                    // Restore position and state
                    viewModel.PositionX = savedWidget.PositionX;
                    viewModel.PositionY = savedWidget.PositionY;
                    viewModel.IsMinimized = savedWidget.IsMinimized;

                    // Set widget content for window system
                    viewModel.WidgetContent = searchWidget;
                    viewModel.WidgetTitle = $"Search #{savedWidget.SearchInstanceId}";

                    // Register with position service for collision avoidance
                    var positionService = Helpers.ServiceHelper.GetService<Services.WidgetPositionService>();
                    positionService?.RegisterWidget(viewModel);

                    // Wire up event to reopen SearchModal when widget is clicked
                    viewModel.SearchModalOpenRequested += async (s, sid) =>
                    {
                        await ShowSearchModalForInstanceAsync(sid);
                    };

                    // Use the new window manager instead of desktop canvas
                    var widgetManager = Services.WidgetWindowManager.Instance;
                    widgetManager.CreateWidget(viewModel);

                    DebugLogger.Log(
                        "BalatroMainMenu",
                        $"Restored widget window: {savedWidget.SearchInstanceId} at ({savedWidget.PositionX}, {savedWidget.PositionY})"
                    );
                    */
                }

                // Clean up orphaned widgets
                if (widgetsToRemove.Count > 0)
                {
                    foreach (var widget in widgetsToRemove)
                    {
                        profile.SavedSearchWidgets.Remove(widget);
                    }
                    profileService.SaveProfile(profile);
                    DebugLogger.Log(
                        "BalatroMainMenu",
                        $"Cleaned up {widgetsToRemove.Count} orphaned widgets"
                    );
                }

                if (profile.SavedSearchWidgets.Count > 0)
                {
                    DebugLogger.Log(
                        "BalatroMainMenu",
                        $"Restored {profile.SavedSearchWidgets.Count} search widgets"
                    );
                }
            }
            catch (Exception ex)
            {
                DebugLogger.LogError(
                    "BalatroMainMenu",
                    $"Failed to restore saved widgets: {ex.Message}"
                );
            }
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

        #region Modal Management (View-only logic)

        /// <summary>
        /// Updates the main title text
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
                    _modalContentWrapper.Content = null;
                },
                DispatcherPriority.Background
            );

            // Audio manager cleanup handled via IAudioManager interface
        }

        #endregion


        #region Shader Management (Delegated to ViewModel)

        // ApplyVisualizerTheme removed - was empty stub

        public void ApplyMainColor(int colorIndex)
        {
            if (_background is BalatroShaderBackground shader)
            {
                ViewModel.ApplyMainColor(shader, colorIndex);
            }
        }

        public void ApplyMainColor(SkiaSharp.SKColor color)
        {
            if (_background is BalatroShaderBackground shader)
            {
                ViewModel.ApplyMainColor(shader, color);
            }
        }

        public void ApplyAccentColor(int colorIndex)
        {
            if (_background is BalatroShaderBackground shader)
            {
                ViewModel.ApplyAccentColor(shader, colorIndex);
            }
        }

        public void ApplyAccentColor(SkiaSharp.SKColor color)
        {
            if (_background is BalatroShaderBackground shader)
            {
                ViewModel.ApplyAccentColor(shader, color);
            }
        }

        // ApplyAudioIntensity removed - was empty stub
        // ApplyParallaxStrength removed - was empty stub

        public void ApplyTimeSpeed(float speed)
        {
            if (_background is BalatroShaderBackground shader)
            {
                ViewModel.ApplyTimeSpeed(shader, speed);
            }
        }

        public void ApplyShaderContrast(float contrast)
        {
            if (_background is BalatroShaderBackground shader)
            {
                ViewModel.ApplyShaderContrast(shader, contrast);
            }
        }

        public void ApplyShaderSpinAmount(float spinAmount)
        {
            if (_background is BalatroShaderBackground shader)
            {
                ViewModel.ApplyShaderSpinAmount(shader, spinAmount);
            }
        }

        public void ApplyShaderZoomPunch(float zoom)
        {
            if (_background is BalatroShaderBackground shader)
            {
                ViewModel.ApplyShaderZoomPunch(shader, zoom);
            }
        }

        public void ApplyShaderMelodySaturation(float saturation)
        {
            if (_background is BalatroShaderBackground shader)
            {
                ViewModel.ApplyShaderMelodySaturation(shader, saturation);
            }
        }

        public void ApplyShaderPixelSize(float pixelSize)
        {
            if (_background is BalatroShaderBackground shader)
            {
                ViewModel.ApplyShaderPixelSize(shader, pixelSize);
            }
        }

        public void ApplyShaderSpinEase(float spinEase)
        {
            if (_background is BalatroShaderBackground shader)
            {
                ViewModel.ApplyShaderSpinEase(shader, spinEase);
            }
        }

        // New shader parameter methods that call shader directly
        public void ApplyShaderTime(float time)
        {
            if (_background is BalatroShaderBackground shader)
            {
                shader.SetTime(time);
            }
        }

        public float GetTimeSpeed()
        {
            if (_background is BalatroShaderBackground shader)
            {
                return shader.GetTimeSpeed();
            }
            return 1f;
        }

        public float GetSpinTimeSpeed()
        {
            if (_background is BalatroShaderBackground shader)
            {
                return shader.GetSpinTimeSpeed();
            }
            return 1f;
        }

        public void ApplyShaderSpinTime(float spinTime)
        {
            if (_background is BalatroShaderBackground shader)
            {
                shader.SetSpinTime(spinTime);
            }
        }

        public void ApplyShaderParallaxX(float parallaxX)
        {
            if (_background is BalatroShaderBackground shader)
            {
                shader.SetParallaxX(parallaxX);
            }
        }

        public void ApplyShaderParallaxY(float parallaxY)
        {
            if (_background is BalatroShaderBackground shader)
            {
                shader.SetParallaxY(parallaxY);
            }
        }

        public void ApplyShaderLoopCount(float loopCount)
        {
            if (_background is BalatroShaderBackground shader)
            {
                shader.SetLoopCount(loopCount);
            }
        }

        public void ApplyPsychedelicBlend(float blend)
        {
            if (_background is BalatroShaderBackground shader)
            {
                shader.SetPsychedelicBlend(blend);
            }
        }

        public void ApplyPsychedelicSpeed(float speed)
        {
            if (_background is BalatroShaderBackground shader)
            {
                shader.SetPsychedelicSpeed(speed);
            }
        }

        public void ApplyPsychedelicComplexity(float value)
        {
            if (_background is BalatroShaderBackground shader)
            {
                shader.SetPsychedelicComplexity(value);
            }
        }

        public void ApplyPsychedelicColorCycle(float value)
        {
            if (_background is BalatroShaderBackground shader)
            {
                shader.SetPsychedelicColorCycle(value);
            }
        }

        public void ApplyPsychedelicKaleidoscope(float value)
        {
            if (_background is BalatroShaderBackground shader)
            {
                shader.SetPsychedelicKaleidoscope(value);
            }
        }

        public void ApplyPsychedelicFluidFlow(float value)
        {
            if (_background is BalatroShaderBackground shader)
            {
                shader.SetPsychedelicFluidFlow(value);
            }
        }

        /// <summary>
        /// Set the volume for a specific track in the audio manager
        /// </summary>
        internal void SetTrackVolume(string trackName, float volume)
        {
            DebugLogger.Log("BalatroMainMenu", $"SetTrackVolume called: {trackName} = {volume}");
        }

        public void ApplyShadowFlickerSource(int sourceIndex)
        {
            if (_background is BalatroShaderBackground shader)
            {
                ViewModel.ApplyShadowFlickerSource(shader, sourceIndex);
            }
        }

        public void ApplySpinSource(int sourceIndex)
        {
            if (_background is BalatroShaderBackground shader)
            {
                ViewModel.ApplySpinSource(shader, sourceIndex);
            }
        }

        public void ApplyTwirlSource(int sourceIndex)
        {
            if (_background is BalatroShaderBackground shader)
            {
                ViewModel.ApplyTwirlSource(shader, sourceIndex);
            }
        }

        public void ApplyZoomThumpSource(int sourceIndex)
        {
            if (_background is BalatroShaderBackground shader)
            {
                ViewModel.ApplyZoomThumpSource(shader, sourceIndex);
            }
        }

        public void ApplyColorSaturationSource(int sourceIndex)
        {
            if (_background is BalatroShaderBackground shader)
            {
                ViewModel.ApplyColorSaturationSource(shader, sourceIndex);
            }
        }

        // ApplyBeatPulseSource removed - was empty stub

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

                var searchContent = new Modals.SearchModal(ViewModel.SearchModalViewModel);
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

            try
            {
                // Get search instance from service
                var searchManager = Helpers.ServiceHelper.GetService<SearchManager>();
                var searchInstance = searchManager?.GetSearch(searchId);

                if (searchInstance == null)
                {
                    DebugLogger.LogError(
                        "BalatroMainMenu",
                        $"Search instance not found: {searchId}"
                    );
                    return;
                }

                var platformServices = ServiceHelper.GetService<IPlatformServices>();
                if (platformServices?.SupportsResultsGrid == true)
                {
                    // Create SearchWidget with proper ViewModel (works on all platforms)
                    var spriteService = Services.SpriteService.Instance;
                    var notificationService = ServiceHelper.GetService<NotificationService>();
                    var viewModel = new SearchWidgetViewModel(
                        searchInstance,
                        spriteService,
                        null,
                        notificationService
                    );
                    var searchWidget = new Components.Widgets.SearchWidget
                    {
                        DataContext = viewModel,
                    };

                    // Wire up event to reopen SearchModal when widget is clicked
                    viewModel.SearchModalOpenRequested += async (s, sid) =>
                    {
                        await ShowSearchModalForInstanceAsync(sid);
                    };

                    // Set widget content for window system
                    viewModel.WidgetContent = searchWidget;
                    viewModel.WidgetTitle = $"Search #{searchId}";
                    viewModel.IsMinimized = true;

                    // Use the new window manager instead of desktop canvas
                    var widgetManager = Services.WidgetWindowManager.Instance;
                    widgetManager.CreateWidget(viewModel);
                }

                DebugLogger.Log(
                    "BalatroMainMenu",
                    $"Created SearchWidget window for searchId: {searchId}"
                );
            }
            catch (Exception ex)
            {
                DebugLogger.LogError(
                    "BalatroMainMenu",
                    $"Failed to create SearchWidget window: {ex.Message}"
                );
            }
        }

        /// <summary>
        /// Remove Search Widget using window manager
        /// </summary>
        public void RemoveSearchDesktopIcon(string searchId)
        {
            DebugLogger.Log(
                "BalatroMainMenu",
                $"RemoveSearchDesktopIcon called for searchId: {searchId}"
            );

            try
            {
                // Find the widget in the window manager (works on all platforms)
                var widgetManager = Services.WidgetWindowManager.Instance;
                var activeWidgets = widgetManager.GetActiveWidgets();

                var widgetToRemove = activeWidgets.FirstOrDefault(w =>
                    w is SearchWidgetViewModel searchVm && searchVm.SearchInstanceId == searchId
                );

                if (widgetToRemove != null)
                {
                    widgetManager.CloseWidget(widgetToRemove);
                    DebugLogger.Log(
                        "BalatroMainMenu",
                        $"Removed SearchWidget window for searchId: {searchId}"
                    );
                }
                else
                {
                    DebugLogger.Log(
                        "BalatroMainMenu",
                        $"No SearchWidget window found for searchId: {searchId}"
                    );
                }
            }
            catch (Exception ex)
            {
                DebugLogger.LogError(
                    "BalatroMainMenu",
                    $"Failed to remove SearchWidget window: {ex.Message}"
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
                var audioManager = ServiceHelper.GetService<IAudioManager>();
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

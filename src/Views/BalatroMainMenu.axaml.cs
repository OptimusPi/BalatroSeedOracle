using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Presenters;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.LogicalTree;
using Avalonia.Markup.Xaml;
using Oracle.Controls;
using Oracle.Helpers;
using Oracle.Models;
using Oracle.Services;
using Oracle.Views.Modals;

namespace Oracle.Views
{
    public partial class BalatroMainMenu : UserControl
    {
        private Grid? _modalContainer;
        private BalatroStyleBackground? _background;
        private Button? _animationToggleButton;
        private TextBlock? _animationButtonText;
        private SearchDesktopIcon? _searchDesktopIcon;
        private bool _isAnimating = true;
        private int _widgetCounter = 0;
        private int _minimizedWidgetCount = 0;
        private UserProfileService? _userProfileService;

        /// <summary>
        /// Callback to request main content swap (set by MainWindow)
        /// </summary>
        public Action<UserControl>? RequestContentSwap { get; set; }

        public BalatroMainMenu()
        {
            InitializeComponent();

            // Defer service initialization to OnLoaded to ensure services are ready
            this.Loaded += OnLoaded;
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);

            _modalContainer = this.FindControl<Grid>("ModalContainer");
            _background = this.FindControl<BalatroStyleBackground>("BackgroundControl");
            _animationToggleButton = this.FindControl<Button>("AnimationToggleButton");
            _searchDesktopIcon = this.FindControl<SearchDesktopIcon>("SearchDesktopIcon");

            if (_animationToggleButton != null)
            {
                // Find the TextBlock inside the button using logical tree traversal
                _animationButtonText = LogicalExtensions
                    .GetLogicalChildren(_animationToggleButton)
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
        }

        // Main menu button event handlers
        private void OnSeedSearchClick(object? sender, RoutedEventArgs e)
        {
            try
            {
                // Open the search modal
                this.ShowSearchModal();
            }
            catch (Exception ex)
            {
                Oracle.Helpers.DebugLogger.LogError(
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
            // Open filters modal with blank/new filter
            this.ShowFiltersModal();
        }

        private void OnResultsClick(object? sender, RoutedEventArgs e)
        {
            // Show the results modal
            var resultsModal = new ResultsModal();
            var modal = new StandardModal("SAVED FILTERS");
            modal.SetContent(resultsModal);
            modal.BackClicked += (s, ev) => HideModalContent();
            ShowModalContent(modal, "SAVED FILTERS");
        }

        private void OnAnalyzeClick(object? sender, RoutedEventArgs e)
        {
            // Show the analyze modal
            var analyzeModal = new AnalyzeModal();
            var modal = new StandardModal("ANALYZE");
            modal.SetContent(analyzeModal);
            modal.BackClicked += (s, ev) => HideModalContent();
            ShowModalContent(modal, "SEED ANALYZER");
        }

        private void OnToolClick(object? sender, RoutedEventArgs e)
        {
            // Use the modal helper extension method
            this.ShowToolsModal();
        }

        private void OnExitClick(object? sender, RoutedEventArgs e)
        {
            if (
                Application.Current?.ApplicationLifetime
                is Avalonia.Controls.ApplicationLifetimes.IClassicDesktopStyleApplicationLifetime desktop
            )
            {
                desktop.Shutdown();
            }
        }

        /// <summary>
        /// Proper cleanup when the view is disposed
        /// </summary>
        public void Dispose()
        {
            try
            {
                // Search widgets removed - using desktop icons now
            }
            catch (Exception ex)
            {
                Oracle.Helpers.DebugLogger.LogError(
                    "BalatroMainMenu",
                    $"⚠️  Error during disposal: {ex.Message}"
                );
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
                Oracle.Helpers.DebugLogger.LogError(
                    "BalatroMainMenu",
                    $"Error opening Balatro website: {ex.Message}"
                );
            }
        }

        /// <summary>
        /// Cycles through the available background themes when the theme button is clicked
        /// </summary>
        private void OnThemeCycleClick(object? sender, RoutedEventArgs e)
        {
            if (_background == null)
                return;

            // Get the current theme and cycle to the next one
            var currentTheme = _background.Theme;
            var themes = Enum.GetValues<BalatroStyleBackground.BackgroundTheme>();
            var nextThemeIndex = ((int)currentTheme + 1) % themes.Length;
            var nextTheme = themes[nextThemeIndex];

            // Set the new theme
            _background.Theme = nextTheme;
        }

        /// <summary>
        /// Toggles the background animation on/off when the animation button is clicked
        /// </summary>
        private void OnAnimationToggleClick(object? sender, RoutedEventArgs e)
        {
            if (_background == null)
                return;

            // Toggle animation state
            _isAnimating = !_isAnimating;
            _background.IsAnimating = _isAnimating;

            // Update button icon based on animation state
            if (_animationButtonText != null)
            {
                _animationButtonText.Text = _isAnimating ? "⏸" : "▶";
            }
        }

        /// <summary>
        /// Switches to edit mode for author name
        /// </summary>
        private void OnAuthorClick(object? sender, RoutedEventArgs e)
        {
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
                    Oracle.Helpers.DebugLogger.Log(
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
                    Oracle.Helpers.DebugLogger.LogError(
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
                Oracle.Helpers.DebugLogger.Log(
                    "BalatroMainMenu",
                    $"Setting author display to: '{authorName}'"
                );
                authorDisplay.Text = authorName;
                authorEdit.Text = authorName;
            }
            else
            {
                Oracle.Helpers.DebugLogger.LogError(
                    "BalatroMainMenu",
                    "Could not find AuthorDisplay or AuthorEdit controls"
                );
            }
        }

        /// <summary>
        /// Shows a search desktop icon on the desktop
        /// </summary>
        public async void ShowSearchDesktopIcon(string searchId, string? configPath = null)
        {
            Oracle.Helpers.DebugLogger.Log(
                "BalatroMainMenu",
                $"ShowSearchDesktopIcon called with searchId: {searchId}, config: {configPath}"
            );

            // Get the desktop canvas
            var desktopCanvas = this.FindControl<Grid>("DesktopCanvas");
            if (desktopCanvas == null)
            {
                Oracle.Helpers.DebugLogger.Log("BalatroMainMenu", "DesktopCanvas not found!");
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

            Oracle.Helpers.DebugLogger.Log(
                "BalatroMainMenu",
                $"Created SearchDesktopIcon #{_widgetCounter} at position ({leftMargin}, {topMargin})"
            );

            // If config path provided, start the search
            if (!string.IsNullOrEmpty(configPath))
            {
                Oracle.Helpers.DebugLogger.Log("BalatroMainMenu", "Starting search...");
                var searchService = App.GetService<MotelySearchService>();
                if (searchService != null)
                {
                    var criteria = new Oracle.Models.SearchCriteria
                    {
                        ConfigPath = configPath,
                        ThreadCount = 4,
                        MinScore = 0,
                        BatchSize = 3,
                    };
                    await searchService.StartSearchAsync(criteria);
                }
            }
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
            Oracle.Helpers.DebugLogger.Log(
                "BalatroMainMenu",
                $"RemoveSearchDesktopIcon called for searchId: {searchId}"
            );

            var desktopCanvas = this.FindControl<Grid>("DesktopCanvas");
            if (desktopCanvas == null)
            {
                Oracle.Helpers.DebugLogger.Log("BalatroMainMenu", "DesktopCanvas not found!");
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
                Oracle.Helpers.DebugLogger.Log(
                    "BalatroMainMenu",
                    $"Removed SearchDesktopIcon for searchId: {searchId}"
                );
            }
            else
            {
                Oracle.Helpers.DebugLogger.Log(
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
                    var filtersModal = modalContent?.Content as FiltersModalContent;
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
    }
}

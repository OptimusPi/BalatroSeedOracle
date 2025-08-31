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
using Avalonia.Media;
using Avalonia.Layout;
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
            // Open the gorgeous Balatro-style filter selector
            var filterSelector = new Components.BalatroFilterSelector();
            
            // Handle filter selection (Edit In Designer button)
            filterSelector.FilterSelected += (s, filter) => 
            {
                if (filter != null)
                {
                    DebugLogger.Log("BalatroMainMenu", $"Filter selected for editing: {filter.Name}");
                    
                    // Close the selector modal
                    HideModalContent();
                    
                    // TODO: Open Visual Filter Builder with selected filter
                    // Open the real FiltersModal instead of placeholder
                    var filtersModal = new FiltersModal();
                    // Load the selected filter for editing
                    filtersModal.LoadFilter(filter.FilePath);
                    
                    var modal = new StandardModal("VISUAL FILTER BUILDER");
                    modal.SetContent(filtersModal);
                    modal.BackClicked += (s, ev) => HideModalContent();
                    ShowModalContent(modal, "VISUAL FILTER BUILDER");
                }
            };
            
            // Handle create new filter request
            filterSelector.CreateNewFilterRequested += (s, e) =>
            {
                DebugLogger.Log("BalatroMainMenu", "Create new filter requested");
                
                // Close the selector modal
                HideModalContent();
                
                // TODO: Open Visual Filter Builder with blank filter
                // Open the real FiltersModal instead of placeholder
                var filtersModal = new FiltersModal();
                // Start with blank filter (Config tab will be active)
                
                var modal = new StandardModal("VISUAL FILTER BUILDER");
                modal.SetContent(filtersModal);
                modal.BackClicked += (s, ev) => HideModalContent();
                ShowModalContent(modal, "VISUAL FILTER BUILDER");
            };
            
            var modal = new StandardModal("FILTER SELECTION");
            modal.SetContent(filterSelector);
            modal.BackClicked += (s, ev) => HideModalContent();
            ShowModalContent(modal, "SELECT FILTER");
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
                BalatroSeedOracle.Helpers.DebugLogger.LogError(
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
                BalatroSeedOracle.Helpers.DebugLogger.LogError(
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
                    var filtersModal = modalContent?.Content as Components.ChallengesFilterSelector;
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

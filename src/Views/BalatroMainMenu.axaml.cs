using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Presenters;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Avalonia.LogicalTree;
using Oracle.Controls;
using Oracle.Helpers;
using Oracle.Models;
using Oracle.Services;
using Oracle.Views.Modals;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;

namespace Oracle.Views
{
    public partial class BalatroMainMenu : UserControl
    {
        private Grid? _modalContainer;
        private BalatroStyleBackground? _background;
        private Button? _animationToggleButton;
        private TextBlock? _animationButtonText;
        private bool _isAnimating = true;
        private readonly List<Components.SearchWidget> _searchWidgets = new();
        private int _widgetCounter = 0;
        private UserProfileService? _userProfileService;

        /// <summary>
        /// Callback to request main content swap (set by MainWindow)
        /// </summary>
        public Action<UserControl>? RequestContentSwap { get; set; }

        public BalatroMainMenu()
        {
            InitializeComponent();
            
            // Initialize user profile service
            _userProfileService = ServiceHelper.GetService<UserProfileService>();
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
            
            _modalContainer = this.FindControl<Grid>("ModalContainer");
            _background = this.FindControl<BalatroStyleBackground>("BackgroundControl");
            _animationToggleButton = this.FindControl<Button>("AnimationToggleButton");
            
            if (_animationToggleButton != null)
            {
                // Find the TextBlock inside the button using logical tree traversal
                _animationButtonText = LogicalExtensions.GetLogicalChildren(_animationToggleButton).OfType<TextBlock>().FirstOrDefault();
            }
            
            
            // Load and display current author name
            if (_userProfileService != null)
            {
                var authorDisplay = this.FindControl<TextBlock>("AuthorDisplay");
                var authorEdit = this.FindControl<TextBox>("AuthorEdit");
                if (authorDisplay != null && authorEdit != null)
                {
                    var authorName = _userProfileService.GetAuthorName();
                    authorDisplay.Text = authorName;
                    authorEdit.Text = authorName;
                }
            }
        }
        
        private UserControl? _activeModalContent;

        /// <summary>
        /// Show a UserControl as a modal overlay in the main menu, styled as a Balatro card
        /// </summary>
        public void ShowModalContent(UserControl content)
        {
            if (_modalContainer == null)
                return;

            // Remove previous content
            _modalContainer.Children.Clear();

            // Add the modal directly - it already has its own sizing grid
            _modalContainer.Children.Add(content);
            _modalContainer.IsVisible = true;
            _activeModalContent = content;
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
        }

        // Main menu button event handlers
        private void OnNewFilterClick(object? sender, RoutedEventArgs e)
        {
            // Open filters modal with blank/new filter
            this.ShowFiltersModal();
        }
        
        private void OnLoadClick(object? sender, RoutedEventArgs e)
        {
            // Show the browse filters modal
            this.ShowBrowseFiltersModal();
        }

        private void OnResultsClick(object? sender, RoutedEventArgs e)
        {
            // Show the results modal
            var resultsModal = new ResultsModal();
            var modal = new StandardModal("SAVED FILTERS");
            modal.SetContent(resultsModal);
            modal.BackClicked += (s, ev) => HideModalContent();
            ShowModalContent(modal);
        }
        

        private void OnFunRunClick(object? sender, RoutedEventArgs e)
        {
            // Use the modal helper extension method
            this.ShowFunRunsModal();
        }

        private void OnExitClick(object? sender, RoutedEventArgs e)
        {
            if (Application.Current?.ApplicationLifetime is Avalonia.Controls.ApplicationLifetimes.IClassicDesktopStyleApplicationLifetime desktop)
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
                // Stop all search widgets
                foreach (var searchWidget in _searchWidgets)
                {
                    Oracle.Helpers.DebugLogger.Log("BalatroMainMenu", $"Stopping search widget #{_searchWidgets.IndexOf(searchWidget) + 1}...");
                    searchWidget.StopSearch();
                }
            }
            catch (Exception ex)
            {
                Oracle.Helpers.DebugLogger.LogError("BalatroMainMenu", $"⚠️  Error during disposal: {ex.Message}");
            }
        }
        
        private void OnBuyBalatroClick(object? sender, PointerPressedEventArgs e)
        {
            try
            {
                // Open the Balatro website in the default browser
                var url = "https://playbalatro.com/";
                Process.Start(new ProcessStartInfo
                {
                    FileName = url,
                    UseShellExecute = true
                });
            }
            catch (Exception ex)
            {
                Oracle.Helpers.DebugLogger.LogError("BalatroMainMenu", $"Error opening Balatro website: {ex.Message}");
            }
        }

        /// <summary>
        /// Cycles through the available background themes when the theme button is clicked
        /// </summary>
        private void OnThemeCycleClick(object? sender, RoutedEventArgs e)
        {
            if (_background == null) return;
            
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
            if (_background == null) return;
            
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
                    Oracle.Helpers.DebugLogger.Log("BalatroMainMenu", $"Author name updated to: {newName}");
                }
                
                // Switch back to display mode
                authorDisplay.IsVisible = true;
                authorEdit.IsVisible = false;
            }
        }
        
        
        
        
        /// <summary>
        /// Shows the search widget on the desktop
        /// </summary>
        public async void ShowSearchWidget(string? configPath = null)
        {
            Oracle.Helpers.DebugLogger.Log("BalatroMainMenu", $"ShowSearchWidget called with config: {configPath}");
            
            // Get the desktop canvas
            var desktopCanvas = this.FindControl<Grid>("DesktopCanvas");
            if (desktopCanvas == null)
            {
                Oracle.Helpers.DebugLogger.Log("BalatroMainMenu", "DesktopCanvas not found!");
                return;
            }
            
            // Create a new SearchWidget instance
            var searchWidget = new Components.SearchWidget();
            
            // Calculate position based on existing widgets
            var leftMargin = 20 + (_widgetCounter % 3) * 400; // 3 widgets per row
            var topMargin = 80 + (_widgetCounter / 3) * 300; // Stack rows
            
            searchWidget.HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Left;
            searchWidget.VerticalAlignment = Avalonia.Layout.VerticalAlignment.Top;
            searchWidget.Margin = new Thickness(leftMargin, topMargin, 0, 0);
            searchWidget.IsVisible = true;
            
            // Add to the desktop canvas
            desktopCanvas.Children.Add(searchWidget);
            _searchWidgets.Add(searchWidget);
            _widgetCounter++;
            
            Oracle.Helpers.DebugLogger.Log("BalatroMainMenu", $"Created SearchWidget #{_widgetCounter} at position ({leftMargin}, {topMargin})");
            
            // If config path provided, load it
            if (!string.IsNullOrEmpty(configPath))
            {
                Oracle.Helpers.DebugLogger.Log("BalatroMainMenu", "Loading config...");
                await searchWidget.LoadConfig(configPath);
            }
        }
        
        /// <summary>
        /// Stops all running searches - called during application shutdown
        /// </summary>
        public async Task StopAllSearchesAsync()
        {
            DebugLogger.LogImportant("BalatroMainMenu", "Stopping all searches...");
            
            // Stop all search widgets
            foreach (var searchWidget in _searchWidgets)
            {
                if (searchWidget.IsRunning)
                {
                    DebugLogger.LogImportant("BalatroMainMenu", $"Stopping SearchWidget #{_searchWidgets.IndexOf(searchWidget) + 1}...");
                    searchWidget.StopSearch();
                }
            }
            
            // Wait a bit for the searches to stop
            if (_searchWidgets.Any(w => w.IsRunning))
            {
                await Task.Delay(500);
            }
            
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
                        DebugLogger.LogImportant("BalatroMainMenu", "Checking FiltersModal for active searches...");
                        // Note: FiltersModal doesn't have a StopSearch method currently
                        // but we keep this structure in case it's needed later
                    }
                }
            }
            
            DebugLogger.LogImportant("BalatroMainMenu", "All searches stopped");
        }
    }
}
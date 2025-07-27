using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Presenters;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Avalonia.LogicalTree;
using Oracle.Controls;
using Oracle.Helpers;
using Oracle.Services;
using Oracle.Views.Modals;
using System;
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
        private bool _isMusicEnabled = true;
        private int _volumeLevel = 2; // Default to medium volume

        /// <summary>
        /// Callback to request main content swap (set by MainWindow)
        /// </summary>
        public Action<UserControl>? RequestContentSwap { get; set; }

        public BalatroMainMenu()
        {
            InitializeComponent();
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
        private void OnSearchClick(object? sender, RoutedEventArgs e)
        {
            OpenSearchModal();
        }
        
        private void OpenSearchModal(string? configPath = null)
        {
            // Create a new search modal
            var searchModal = new SearchModal();
            searchModal.SetSearchService(ServiceHelper.GetService<MotelySearchService>() ?? new MotelySearchService());
            
            // If a config path is provided, set it
            if (!string.IsNullOrEmpty(configPath))
            {
                searchModal.SetConfigPath(configPath);
            }
            
            // Wrap in standard modal frame
            var modal = new StandardModal("SEED SEARCH");
            modal.SetContent(searchModal);
            modal.BackClicked += (s, ev) => HideModalContent();
            ShowModalContent(modal);
        }
        
        public void OpenSearchModalWithConfig(string configPath)
        {
            OpenSearchModal(configPath);
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
        
        private void OnFiltersClick(object? sender, RoutedEventArgs e)
        {
            // Use the modal helper extension method
            this.ShowFiltersModal();
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
                // Stop any running search in the search widget
                var searchWidget = this.FindControl<Components.SearchWidget>("SearchWidget");
                if (searchWidget != null)
                {
                    Oracle.Helpers.DebugLogger.Log("BalatroMainMenu", "Stopping search widget...");
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
        /// Toggles music playback when the music button is clicked
        /// </summary>
        private void OnMusicToggleClick(object? sender, RoutedEventArgs e)
        {
            // Toggle music on/off (placeholder implementation)
            _isMusicEnabled = !_isMusicEnabled;
            Oracle.Helpers.DebugLogger.Log("BalatroMainMenu", $"Music toggled: {(_isMusicEnabled ? "ON" : "OFF")}");
            
            // Update button text to reflect state
            if (sender is Button button)
            {
                button.Content = _isMusicEnabled ? "🔊 MUSIC" : "🔇 MUSIC";
            }
        }
        
        /// <summary>
        /// Shows volume control when the volume button is clicked
        /// </summary>
        private void OnVolumeClick(object? sender, RoutedEventArgs e)
        {
            // Cycle through volume levels (placeholder implementation)
            _volumeLevel = (_volumeLevel + 1) % 4; // 0-3 levels
            var volumeText = _volumeLevel switch
            {
                0 => "🔇 MUTE",
                1 => "🔈 LOW",
                2 => "🔉 MED",
                3 => "🔊 HIGH",
                _ => "🔊 HIGH"
            };
            
            Oracle.Helpers.DebugLogger.Log("BalatroMainMenu", $"Volume changed to level {_volumeLevel}: {volumeText}");
            
            // Update button text to reflect volume level
            if (sender is Button button)
            {
                button.Content = volumeText;
            }
        }
        
        /// <summary>
        /// Shows the search modal with current search results from widget
        /// </summary>
        public void ShowSearchModal(Components.SearchWidget searchWidget)
        {
            var searchModal = new SearchModal();
            searchModal.SetSearchService(ServiceHelper.GetService<MotelySearchService>() ?? new MotelySearchService());
            searchModal.SetConfigPath(searchWidget.ConfigPath);
            searchModal.SetResults(searchWidget.Results);
            searchModal.SetSearchState(searchWidget.IsRunning, searchWidget.FoundCount);
            
            var modal = new StandardModal("SEARCH RESULTS");
            modal.SetContent(searchModal);
            modal.BackClicked += (s, ev) => HideModalContent();
            ShowModalContent(modal);
        }
        
        /// <summary>
        /// Shows the search widget on the desktop
        /// </summary>
        public async void ShowSearchWidget(string? configPath = null)
        {
            Oracle.Helpers.DebugLogger.Log("BalatroMainMenu", $"ShowSearchWidget called with config: {configPath}");
            
            var searchWidget = this.FindControl<Components.SearchWidget>("SearchWidget");
            if (searchWidget != null)
            {
                Oracle.Helpers.DebugLogger.Log("BalatroMainMenu", "Found SearchWidget control");
                searchWidget.IsVisible = true;
                Oracle.Helpers.DebugLogger.Log("BalatroMainMenu", $"SearchWidget.IsVisible set to: {searchWidget.IsVisible}");
                
                // If config path provided, load it
                if (!string.IsNullOrEmpty(configPath))
                {
                    Oracle.Helpers.DebugLogger.Log("BalatroMainMenu", "Loading config...");
                    await searchWidget.LoadConfig(configPath);
                }
            }
            else
            {
                Oracle.Helpers.DebugLogger.Log("BalatroMainMenu", "SearchWidget control not found!");
            }
        }
        
        /// <summary>
        /// Stops all running searches - called during application shutdown
        /// </summary>
        public async Task StopAllSearchesAsync()
        {
            DebugLogger.LogImportant("BalatroMainMenu", "Stopping all searches...");
            
            // Find and stop the search widget
            var searchWidget = this.FindControl<Components.SearchWidget>("SearchWidget");
            if (searchWidget != null && searchWidget.IsRunning)
            {
                DebugLogger.LogImportant("BalatroMainMenu", "Stopping SearchWidget search...");
                searchWidget.StopSearch();
                
                // Wait a bit for the search to stop
                await Task.Delay(500);
            }
            
            // Also check if there's a search modal open
            if (_modalContainer != null && _modalContainer.Children.Count > 0)
            {
                var modal = _modalContainer.Children[0] as StandardModal;
                if (modal != null)
                {
                    // Find the ModalContent presenter inside StandardModal
                    var modalContent = modal.FindControl<ContentPresenter>("ModalContent");
                    var searchModal = modalContent?.Content as SearchModal;
                    if (searchModal != null)
                    {
                        DebugLogger.LogImportant("BalatroMainMenu", "Stopping SearchModal search...");
                        searchModal.StopSearch();
                        await Task.Delay(500);
                    }
                }
            }
            
            DebugLogger.LogImportant("BalatroMainMenu", "All searches stopped");
        }
    }
}
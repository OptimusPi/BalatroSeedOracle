using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
using Avalonia.VisualTree;
using BalatroSeedOracle.Helpers;
using BalatroSeedOracle.Services;
using BalatroSeedOracle.Views.Modals;

namespace BalatroSeedOracle.Views
{
    public partial class SearchDesktopIcon : UserControl
    {
        private Border? _notificationBadge;
        private TextBlock? _badgeText;
        private TextBlock? _filterNameText;
        private TextBlock? _progressText;
        private ProgressBar? _searchProgress;
        private SearchInstance? _searchInstance;
        private SearchManager? _searchManager;
        private string _searchId = string.Empty;
        private string _configPath = string.Empty;
        private string _filterName = "No Filter";
        private int _resultCount = 0;
        private bool _isSearching = false;

        public SearchDesktopIcon()
        {
            InitializeComponent();
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);

            _notificationBadge = this.FindControl<Border>("NotificationBadge");
            _badgeText = this.FindControl<TextBlock>("BadgeText");
            _filterNameText = this.FindControl<TextBlock>("FilterNameText");
            _progressText = this.FindControl<TextBlock>("ProgressText");
            _searchProgress = this.FindControl<ProgressBar>("SearchProgress");

            // Get search manager service
            _searchManager = App.GetService<SearchManager>();
        }

        public void Initialize(string searchId, string configPath, string filterName)
        {
            _searchId = searchId;
            _configPath = configPath;
            _filterName = filterName;

            if (_filterNameText != null)
            {
                _filterNameText.Text = _filterName;
            }

            // Connect to the specific search instance
            if (_searchManager != null && !string.IsNullOrEmpty(_searchId))
            {
                _searchInstance = _searchManager.GetSearch(_searchId);
                if (_searchInstance != null)
                {
                    // Subscribe to search instance events
                    _searchInstance.SearchStarted += OnSearchStarted;
                    _searchInstance.SearchCompleted += OnSearchCompleted;
                    _searchInstance.ResultFound += OnResultFound;
                    _searchInstance.ProgressUpdated += OnProgressUpdated;

                    // Update UI with current state
                    _isSearching = _searchInstance.IsRunning;
                    _resultCount = _searchInstance.ResultCount;
                    UpdateBadge();
                }
            }

            UpdateProgress(0);
        }

        private void OnIconClick(object? sender, RoutedEventArgs e)
        {
            DebugLogger.Log("SearchDesktopIcon", $"OnIconClick called - SearchId: {_searchId}, ConfigPath: {_configPath}");
            
            // Find the BalatroMainMenu by traversing up the visual tree
            BalatroMainMenu? mainMenu = null;
            var current = this as Visual;
            
            while (current != null && mainMenu == null)
            {
                current = current.GetVisualParent<Visual>();
                mainMenu = current as BalatroMainMenu;
            }
            
            DebugLogger.Log("SearchDesktopIcon", $"MainMenu found via parent traversal: {mainMenu != null}");

            if (mainMenu == null)
            {
                // Try window content as fallback
                var window = TopLevel.GetTopLevel(this) as Window;
                mainMenu = window?.Content as BalatroMainMenu;
                DebugLogger.Log("SearchDesktopIcon", $"MainMenu found via window content: {mainMenu != null}");
            }

            if (mainMenu == null)
            {
                DebugLogger.LogError("SearchDesktopIcon", "Could not find BalatroMainMenu to show search modal");
                return;
            }

            try
            {
                // Show search modal with current search instance
                mainMenu.ShowSearchModalForInstance(_searchId, _configPath);
                DebugLogger.Log("SearchDesktopIcon", "ShowSearchModalForInstance called successfully");
            }
            catch (Exception ex)
            {
                DebugLogger.LogError("SearchDesktopIcon", $"Error showing search modal: {ex}");
            }
        }

        private void OnContextRequested(object? sender, ContextRequestedEventArgs e)
        {
            var contextMenu = new ContextMenu();

            var viewResultsItem = new MenuItem { Header = "View Results" };
            viewResultsItem.Click += (s, ev) => OnIconClick(s, ev);
            contextMenu.Items.Add(viewResultsItem);

            contextMenu.Items.Add(new Separator());

            if (_isSearching)
            {
                var pauseItem = new MenuItem { Header = "Pause Search" };
                pauseItem.Click += OnPauseSearch;
                contextMenu.Items.Add(pauseItem);

                var stopItem = new MenuItem { Header = "Stop Search" };
                stopItem.Click += OnStopSearch;
                contextMenu.Items.Add(stopItem);
            }
            else
            {
                var resumeItem = new MenuItem { Header = "Resume Search" };
                resumeItem.Click += OnResumeSearch;
                contextMenu.Items.Add(resumeItem);
            }

            contextMenu.Items.Add(new Separator());

            var deleteItem = new MenuItem { Header = "Remove Icon" };
            deleteItem.Click += OnDeleteIcon;
            contextMenu.Items.Add(deleteItem);

            contextMenu.Open(sender as Control);
            e.Handled = true;
        }

        private void OnSearchStarted(object? sender, EventArgs e)
        {
            Avalonia.Threading.Dispatcher.UIThread.Post(() =>
            {
                _isSearching = true;
                _resultCount = 0;
                UpdateBadge();
                UpdateProgress(0);
            });
        }

        private void OnSearchCompleted(object? sender, EventArgs e)
        {
            Avalonia.Threading.Dispatcher.UIThread.Post(() =>
            {
                _isSearching = false;
                // Don't update progress to 100% - keep current progress
                // Search may have been stopped/cancelled early
            });
        }

        private void OnResultFound(object? sender, SearchResultEventArgs e)
        {
            Avalonia.Threading.Dispatcher.UIThread.Post(() =>
            {
                _resultCount++;
                UpdateBadge();
            });
        }

        private void OnProgressUpdated(object? sender, SearchProgressEventArgs e)
        {
            Avalonia.Threading.Dispatcher.UIThread.Post(() =>
            {
                UpdateProgress(e.PercentComplete);
            });
        }

        private void UpdateBadge()
        {
            if (_notificationBadge != null && _badgeText != null)
            {
                if (_resultCount > 0)
                {
                    _notificationBadge.IsVisible = true;
                    _badgeText.Text = _resultCount.ToString(
                        System.Globalization.CultureInfo.InvariantCulture
                    );
                }
                else
                {
                    _notificationBadge.IsVisible = false;
                }
            }
        }

        private void UpdateProgress(int percent)
        {
            if (_searchProgress != null && _progressText != null)
            {
                _searchProgress.Value = percent;
                _progressText.Text = $"{percent}%";
            }
        }

        private void OnPauseSearch(object? sender, RoutedEventArgs e)
        {
            _searchInstance?.PauseSearch();
            _isSearching = false;
        }

        private void OnResumeSearch(object? sender, RoutedEventArgs e)
        {
            _searchInstance?.ResumeSearch();
            _isSearching = true;
        }

        private void OnStopSearch(object? sender, RoutedEventArgs e)
        {
            _searchInstance?.StopSearch();
            _isSearching = false;
            // Don't update progress - keep current progress when stopped
        }

        private void OnDeleteIcon(object? sender, RoutedEventArgs e)
        {
            // FIRST stop the search BEFORE clearing state!
            // Otherwise the search will save its state again while stopping
            if (_isSearching && _searchInstance != null)
            {
                // Stop the search instance WITHOUT saving state
                _searchInstance.StopSearch(true);
            }
            
            // NOW clear the saved search state AFTER stopping the search
            var userProfileService = ServiceHelper.GetService<UserProfileService>();
            if (userProfileService != null)
            {
                userProfileService.ClearSearchState();
                DebugLogger.Log("SearchDesktopIcon", "Cleared saved search state from user profile");
                
                // Force save the profile immediately to persist the deletion
                userProfileService.FlushProfile();
            }

            // Remove search from manager
            if (!string.IsNullOrEmpty(_searchId) && _searchManager != null)
            {
                _searchManager.RemoveSearch(_searchId);
            }

            // Remove this icon from parent
            if (this.Parent is Panel parent)
            {
                parent.Children.Remove(this);
            }
        }
    }
}

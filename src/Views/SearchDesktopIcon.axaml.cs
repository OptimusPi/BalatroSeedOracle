using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Oracle.Services;
using Oracle.Views.Modals;
using Oracle.Helpers;
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Avalonia.Input;
using Avalonia.Media;

namespace Oracle.Views
{
    public partial class SearchDesktopIcon : UserControl
    {
        private Border? _notificationBadge;
        private TextBlock? _badgeText;
        private TextBlock? _filterNameText;
        private TextBlock? _progressText;
        private ProgressBar? _searchProgress;
        private MotelySearchService? _searchService;
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
            
            // Subscribe to search service events
            _searchService = App.GetService<MotelySearchService>();
            if (_searchService != null)
            {
                _searchService.SearchStarted += OnSearchStarted;
                _searchService.SearchCompleted += OnSearchCompleted;
                _searchService.ResultFound += OnResultFound;
                // ProgressUpdated event needs different handler
            }
        }
        
        public void Initialize(string configPath, string filterName)
        {
            _configPath = configPath;
            _filterName = filterName;
            
            if (_filterNameText != null)
            {
                _filterNameText.Text = _filterName;
            }
            
            UpdateProgress(0);
        }
        
        private void OnIconClick(object? sender, RoutedEventArgs e)
        {
            var window = TopLevel.GetTopLevel(this) as Window;
            var mainMenu = window?.Content as BalatroMainMenu;
            
            if (mainMenu != null)
            {
                // Show search modal with current results
                mainMenu.ShowSearchModal(_configPath);
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
                UpdateProgress(100);
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
                    _badgeText.Text = _resultCount.ToString();
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
            _searchService?.PauseSearch();
            _isSearching = false;
        }
        
        private void OnResumeSearch(object? sender, RoutedEventArgs e)
        {
            _searchService?.ResumeSearch();
            _isSearching = true;
        }
        
        private void OnStopSearch(object? sender, RoutedEventArgs e)
        {
            _searchService?.StopSearch();
            _isSearching = false;
            UpdateProgress(100);
        }
        
        private void OnDeleteIcon(object? sender, RoutedEventArgs e)
        {
            // Stop any running search
            if (_isSearching)
            {
                _searchService?.StopSearch();
            }
            
            // Remove this icon from parent
            if (this.Parent is Panel parent)
            {
                parent.Children.Remove(this);
            }
        }
    }
}
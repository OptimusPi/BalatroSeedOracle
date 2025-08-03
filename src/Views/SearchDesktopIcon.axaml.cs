using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Oracle.Services;
using Oracle.Views.Modals;
using System;
using System.Threading.Tasks;

namespace Oracle.Views
{
    public partial class SearchDesktopIcon : UserControl
    {
        private Border? _notificationBadge;
        private TextBlock? _badgeText;
        private MotelySearchService? _searchService;
        private int _activeSearchCount = 0;
        
        public SearchDesktopIcon()
        {
            InitializeComponent();
        }
        
        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
            
            _notificationBadge = this.FindControl<Border>("NotificationBadge");
            _badgeText = this.FindControl<TextBlock>("BadgeText");
            
            // Subscribe to search service events
            _searchService = App.GetService<MotelySearchService>();
            if (_searchService != null)
            {
                _searchService.SearchStarted += OnSearchStarted;
                _searchService.SearchCompleted += OnSearchCompleted;
                _searchService.ResultFound += OnResultFound;
            }
        }
        
        private async void OnIconClick(object? sender, RoutedEventArgs e)
        {
            var window = TopLevel.GetTopLevel(this) as Window;
            if (window != null)
            {
                var searchModal = new SearchModal();
                await StandardModal.ShowModal(window, "MOTELY SEARCH", searchModal);
            }
        }
        
        private void OnSearchStarted(object? sender, EventArgs e)
        {
            Avalonia.Threading.Dispatcher.UIThread.Post(() =>
            {
                _activeSearchCount++;
                UpdateBadge();
            });
        }
        
        private void OnSearchCompleted(object? sender, EventArgs e)
        {
            Avalonia.Threading.Dispatcher.UIThread.Post(() =>
            {
                _activeSearchCount = Math.Max(0, _activeSearchCount - 1);
                UpdateBadge();
            });
        }
        
        private void OnResultFound(object? sender, SearchResultEventArgs e)
        {
            // You can update the badge to show result count if desired
            // For now, we just show active search count
        }
        
        private void UpdateBadge()
        {
            if (_notificationBadge != null && _badgeText != null)
            {
                if (_activeSearchCount > 0)
                {
                    _notificationBadge.IsVisible = true;
                    _badgeText.Text = _activeSearchCount.ToString();
                }
                else
                {
                    _notificationBadge.IsVisible = false;
                }
            }
        }
    }
}
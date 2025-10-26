using System;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using BalatroSeedOracle.Services;
using BalatroSeedOracle.Helpers;

namespace BalatroSeedOracle.ViewModels
{
    /// <summary>
    /// Base ViewModel for all desktop widgets (DayLatro, Search icons, etc.)
    /// Provides common functionality: minimize/expand, dragging, notifications
    /// </summary>
    public partial class BaseWidgetViewModel : ObservableObject
    {
        // Static counter to ensure unique Z-indexes for proper layering
        private static int _nextZIndexCounter = 0;

        public BaseWidgetViewModel()
        {
            // Register with position service when created
            RegisterWithPositionService();
            
            // Initialize with base Z-index offset (will be updated when brought to front)
            _zIndexOffset = 0;
        }

        private void RegisterWithPositionService()
        {
            try
            {
                var positionService = ServiceHelper.GetService<WidgetPositionService>();
                positionService?.RegisterWidget(this);
            }
            catch
            {
                // Ignore if service is not available (e.g., during testing)
            }
        }
        [ObservableProperty]
        private bool _isMinimized = true;

        [ObservableProperty]
        private bool _showNotificationBadge = false;

        [ObservableProperty]
        private string _notificationText = "0";

        [ObservableProperty]
        private string _widgetTitle = "Widget";

        [ObservableProperty]
        private string _widgetIcon = "📦";

        [ObservableProperty]
        private double _positionX = 10;

        [ObservableProperty]
        private double _positionY = 10;

        [ObservableProperty]
        private double _width = 350;

        [ObservableProperty]
        private double _height = 450;

        /// <summary>
        /// Z-index offset for this specific widget instance
        /// </summary>
        private int _zIndexOffset;

        /// <summary>
        /// Dynamic Z-index: minimized = 1, expanded = 100 + unique offset
        /// Each widget gets unique Z-index to prevent XAML order conflicts
        /// </summary>
        public int WidgetZIndex 
        { 
            get 
            {
                var zIndex = IsMinimized ? 1 : (100 + _zIndexOffset);
                #if DEBUG
                Console.WriteLine($"[{WidgetTitle}] ZIndex: {zIndex} (IsMinimized: {IsMinimized}, Offset: {_zIndexOffset})");
                #endif
                return zIndex;
            }
        }

        /// <summary>
        /// Brings this widget to front by giving it the highest Z-index
        /// </summary>
        public void BringToFront()
        {
            _zIndexOffset = ++_nextZIndexCounter;
            OnPropertyChanged(nameof(WidgetZIndex));
        }

        partial void OnIsMinimizedChanged(bool value)
        {
            // Notify that WidgetZIndex has changed when IsMinimized changes
            OnPropertyChanged(nameof(WidgetZIndex));
        }

        [RelayCommand]
        private void Expand()
        {
            IsMinimized = false;
            BringToFront(); // Ensure expanded widget comes to front
            OnExpanded();
        }

        [RelayCommand]
        private void Minimize()
        {
            IsMinimized = true;
            OnMinimized();
        }

        [RelayCommand]
        private void Maximize()
        {
            // Toggle between normal and maximized size
            if (Width < 800)
            {
                Width = 900;
                Height = 700;
            }
            else
            {
                Width = 350;
                Height = 450;
            }
            OnMaximized();
        }

        [RelayCommand]
        private void Close()
        {
            OnClosed();
        }

        /// <summary>
        /// Called when widget is expanded - override in derived classes
        /// </summary>
        protected virtual void OnExpanded() { }

        /// <summary>
        /// Called when widget is maximized - override in derived classes
        /// </summary>
        protected virtual void OnMaximized() { }

        /// <summary>
        /// Called when widget is minimized - override in derived classes
        /// </summary>
        protected virtual void OnMinimized() { }

        /// <summary>
        /// Called when widget is closed - override in derived classes
        /// </summary>
        protected virtual void OnClosed() 
        {
            // Unregister from position service when closed
            try
            {
                var positionService = ServiceHelper.GetService<WidgetPositionService>();
                positionService?.UnregisterWidget(this);
            }
            catch
            {
                // Ignore if service is not available
            }
        }

        /// <summary>
        /// Set notification badge
        /// </summary>
        public void SetNotification(int count)
        {
            if (count > 0)
            {
                ShowNotificationBadge = true;
                NotificationText = count > 99 ? "99+" : count.ToString();
            }
            else
            {
                ShowNotificationBadge = false;
            }
        }

        /// <summary>
        /// Clear notification badge
        /// </summary>
        public void ClearNotification()
        {
            ShowNotificationBadge = false;
            NotificationText = "0";
        }
    }
}

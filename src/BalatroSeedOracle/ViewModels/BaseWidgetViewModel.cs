using System;
using Avalonia;
using Avalonia.Controls;
using BalatroSeedOracle.Helpers;
using BalatroSeedOracle.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace BalatroSeedOracle.ViewModels
{
    /// <summary>
    /// Base ViewModel for all desktop widgets (DayLatro, Search icons, etc.)
    /// Provides common functionality: minimize/expand, window management, notifications
    /// Now uses proper Avalonia Window system instead of custom desktop canvas
    /// </summary>
    public partial class BaseWidgetViewModel : ObservableObject
    {
        // Static counter to ensure unique Z-indexes for proper layering
        private static int _nextZIndexCounter = 0;
        private readonly WidgetPositionService? _widgetPositionService;
        private Window? _widgetWindow;

        public BaseWidgetViewModel(WidgetPositionService? widgetPositionService = null)
        {
            _widgetPositionService = widgetPositionService;

            // Register with position service when created
            RegisterWithPositionService();

            // Initialize with base Z-index offset (will be updated when brought to front)
            _zIndexOffset = 0;
        }

        /// <summary>
        /// The actual widget content to display in the window
        /// </summary>
        public Control? WidgetContent { get; set; }

        /// <summary>
        /// The window that hosts this widget
        /// </summary>
        public Window? WidgetWindow
        {
            get => _widgetWindow;
            set
            {
                _widgetWindow = value;
                OnPropertyChanged();
            }
        }

        private void RegisterWithPositionService()
        {
            try
            {
                _widgetPositionService?.RegisterWidget(this);
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
        private string _widgetIcon = "Widgets";

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
                DebugLogger.Log(WidgetTitle, $"ZIndex: {zIndex} (IsMinimized: {IsMinimized}, Offset: {_zIndexOffset})");
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
            // DON'T call BringToFront here - it causes pointer capture loss during drag events
            // Widgets will naturally come to front when dragged (in OnPointerReleased)
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
                _widgetPositionService?.UnregisterWidget(this);
            }
            catch
            {
                // Ignore if service is not available
            }
        }

        /// <summary>
        /// Show the widget window (non-modal, stays open)
        /// </summary>
        [RelayCommand]
        private void Show()
        {
            if (WidgetWindow == null && WidgetContent != null)
            {
                var window = new Windows.WidgetWindow();
                window.DataContext = this;
                WidgetWindow = window;

                // Set initial position using position service
                var positionService = Helpers.ServiceHelper.GetService<Services.WidgetPositionService>();
                if (positionService != null)
                {
                    var (x, y) = positionService.FindNextAvailablePosition(this, IsMinimized);
                    window.Position = new PixelPoint((int)x, (int)y);
                    positionService.RegisterWidget(this);
                }

                window.Show();
            }
            else if (WidgetWindow != null)
            {
                WidgetWindow.WindowState = WindowState.Normal;
                WidgetWindow.Show();
                WidgetWindow.Activate();
            }
        }

        /// <summary>
        /// Hide the widget window
        /// </summary>
        [RelayCommand]
        private void Hide()
        {
            if (WidgetWindow != null)
            {
                WidgetWindow.Hide();
            }
        }

        /// <summary>
        /// Expand the widget
        /// </summary>
        [RelayCommand]
        private void ExpandWidget()
        {
            IsMinimized = false;
        }

        /// <summary>
        /// Close the widget window and cleanup
        /// </summary>
        [RelayCommand]
        private void CloseWidget()
        {
            // Unregister from position service
            var positionService = Helpers.ServiceHelper.GetService<Services.WidgetPositionService>();
            positionService?.UnregisterWidget(this);

            // Close and cleanup window
            if (WidgetWindow != null)
            {
                WidgetWindow.Close();
                WidgetWindow = null;
            }
        }

        /// <summary>
        /// Toggle between minimized and expanded states
        /// </summary>
        [RelayCommand]
        private void ToggleMinimize()
        {
            IsMinimized = !IsMinimized;
        }

        /// <summary>
        /// Bring widget to front (update Z-index)
        /// </summary>
        [RelayCommand]
        private void BringWidgetToFront()
        {
            _nextZIndexCounter++;
            OnPropertyChanged(nameof(WidgetZIndex));

            if (WidgetWindow != null)
            {
                WidgetWindow.Topmost = true;
                WidgetWindow.Topmost = false; // Reset topmost after bringing to front
            }
        }

        /// <summary>
        /// Set notification badge with smart formatting (K/M suffixes for large numbers)
        /// Examples: 1, 52, 1.52k, 12.3k, 121K, 1.72M
        /// </summary>
        public void SetNotification(int count)
        {
            if (count > 0)
            {
                ShowNotificationBadge = true;
                NotificationText = FormatNotificationCount(count);
            }
            else
            {
                ShowNotificationBadge = false;
            }
        }

        /// <summary>
        /// Set notification badge with long count (for large seed counts)
        /// </summary>
        public void SetNotification(long count)
        {
            if (count > 0)
            {
                ShowNotificationBadge = true;
                NotificationText = FormatNotificationCount(count);
            }
            else
            {
                ShowNotificationBadge = false;
            }
        }

        /// <summary>
        /// Format large numbers with K/M suffixes (3 significant figures)
        /// Examples: 1, 52, 1.52k, 12.3k, 121K, 1.72M
        /// </summary>
        private static string FormatNotificationCount(long count)
        {
            if (count < 1000)
                return count.ToString();

            if (count < 10_000)
                return $"{count / 1000.0:0.00}k"; // 1.52k

            if (count < 100_000)
                return $"{count / 1000.0:0.0}k"; // 12.3k

            if (count < 1_000_000)
                return $"{count / 1000}K"; // 121K

            if (count < 10_000_000)
                return $"{count / 1_000_000.0:0.00}M"; // 1.72M

            if (count < 100_000_000)
                return $"{count / 1_000_000.0:0.0}M"; // 12.3M

            return $"{count / 1_000_000}M"; // 121M
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

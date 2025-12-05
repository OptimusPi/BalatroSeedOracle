using System;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using BalatroSeedOracle.Models;
using BalatroSeedOracle.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace BalatroSeedOracle.ViewModels.Widgets
{
    /// <summary>
    /// Base MVVM ViewModel for all widgets with observable properties and commands
    /// </summary>
    public abstract partial class WidgetViewModel : ObservableObject, IWidget
    {
        private readonly IWidgetLayoutService _layoutService;
        private readonly IDockingService _dockingService;

        /// <summary>
        /// Unique identifier for the widget instance
        /// </summary>
        [ObservableProperty]
        private string _id = Guid.NewGuid().ToString();

        /// <summary>
        /// Display title shown in minimized state and title bar
        /// </summary>
        [ObservableProperty]
        private string _title = string.Empty;

        /// <summary>
        /// Resource path to the widget's icon
        /// </summary>
        [ObservableProperty]
        private string _iconResource = "WidgetIcon";

        /// <summary>
        /// Current widget state (Minimized/Open)
        /// </summary>
        [ObservableProperty]
        private WidgetState _state = WidgetState.Minimized;

        /// <summary>
        /// Number of notifications for badge display
        /// </summary>
        [ObservableProperty]
        private int _notificationCount = 0;

        /// <summary>
        /// Progress value between 0.0 and 1.0 for progress bar
        /// </summary>
        [ObservableProperty]
        private double _progressValue = 0.0;

        /// <summary>
        /// Whether the close button is visible when open
        /// </summary>
        [ObservableProperty]
        private bool _showCloseButton = true;

        /// <summary>
        /// Whether the pop-out button is visible when open
        /// </summary>
        [ObservableProperty]
        private bool _showPopOutButton = false;

        /// <summary>
        /// Widget position in grid coordinates
        /// </summary>
        [ObservableProperty]
        private Point _position = new Point(0, 0);

        /// <summary>
        /// Widget size when in open state
        /// </summary>
        [ObservableProperty]
        private Size _size = new Size(400, 300);

        /// <summary>
        /// Whether the widget is currently docked
        /// </summary>
        [ObservableProperty]
        private bool _isDocked = false;

        /// <summary>
        /// Current dock position if docked
        /// </summary>
        [ObservableProperty]
        private DockPosition _dockPosition = DockPosition.None;

        /// <summary>
        /// Widget-specific state that survives sessions
        /// </summary>
        [ObservableProperty]
        private object? _persistedState = null;

        protected WidgetViewModel(IWidgetLayoutService layoutService, IDockingService dockingService)
        {
            _layoutService = layoutService ?? throw new ArgumentNullException(nameof(layoutService));
            _dockingService = dockingService ?? throw new ArgumentNullException(nameof(dockingService));
        }

        /// <summary>
        /// Event fired when widget state changes
        /// </summary>
        public event EventHandler<WidgetStateChangedEventArgs>? StateChanged;

        /// <summary>
        /// Event fired when widget needs to be closed
        /// </summary>
        public event EventHandler<EventArgs>? CloseRequested;

        /// <summary>
        /// Transition widget to open state
        /// </summary>
        [RelayCommand]
        public async Task OpenAsync()
        {
            var oldState = State;
            State = WidgetState.Transitioning;
            
            // Perform any opening logic
            await OnOpenAsync();
            
            State = WidgetState.Open;
            StateChanged?.Invoke(this, new WidgetStateChangedEventArgs(Id, oldState, State));
        }

        /// <summary>
        /// Transition widget to minimized state
        /// </summary>
        [RelayCommand]
        public async Task MinimizeAsync()
        {
            var oldState = State;
            State = WidgetState.Transitioning;
            
            // Perform any minimizing logic
            await OnMinimizeAsync();
            
            State = WidgetState.Minimized;
            StateChanged?.Invoke(this, new WidgetStateChangedEventArgs(Id, oldState, State));
        }

        /// <summary>
        /// Request widget to be closed completely
        /// </summary>
        [RelayCommand]
        public async Task CloseAsync()
        {
            // Perform any cleanup logic
            await OnCloseAsync();
            
            CloseRequested?.Invoke(this, EventArgs.Empty);
        }

        /// <summary>
        /// Pop out widget (if supported)
        /// </summary>
        [RelayCommand]
        public async Task PopOutAsync()
        {
            if (ShowPopOutButton)
            {
                await OnPopOutAsync();
            }
        }

        /// <summary>
        /// Get the content view for this widget
        /// </summary>
        public abstract UserControl GetContentView();

        /// <summary>
        /// Update notification count and refresh badge
        /// </summary>
        /// <param name="count">New notification count</param>
        public virtual void UpdateNotifications(int count)
        {
            NotificationCount = Math.Max(0, count);
        }

        /// <summary>
        /// Update progress value and refresh progress bar
        /// </summary>
        /// <param name="value">Progress value between 0.0 and 1.0</param>
        public virtual void UpdateProgress(double value)
        {
            ProgressValue = Math.Clamp(value, 0.0, 1.0);
        }

        /// <summary>
        /// Save widget-specific state for persistence
        /// </summary>
        public virtual async Task<object?> SaveStateAsync()
        {
            return await Task.FromResult(PersistedState);
        }

        /// <summary>
        /// Load widget-specific state from persistence
        /// </summary>
        public virtual async Task LoadStateAsync(object? state)
        {
            PersistedState = state;
            await Task.CompletedTask;
        }

        /// <summary>
        /// Called when widget is opening - override for custom logic
        /// </summary>
        protected virtual async Task OnOpenAsync()
        {
            await Task.CompletedTask;
        }

        /// <summary>
        /// Called when widget is minimizing - override for custom logic
        /// </summary>
        protected virtual async Task OnMinimizeAsync()
        {
            await Task.CompletedTask;
        }

        /// <summary>
        /// Called when widget is closing - override for cleanup
        /// </summary>
        protected virtual async Task OnCloseAsync()
        {
            await Task.CompletedTask;
        }

        /// <summary>
        /// Called when widget is popped out - override for custom logic
        /// </summary>
        protected virtual async Task OnPopOutAsync()
        {
            await Task.CompletedTask;
        }
    }
}
using System;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;

namespace BalatroSeedOracle.Models.Widgets
{
    /// <summary>
    /// Contract defining widget behavior and properties for the widget interface system
    /// </summary>
    public interface IWidget
    {
        /// <summary>
        /// Unique identifier for the widget instance
        /// </summary>
        string Id { get; }

        /// <summary>
        /// Display title shown in minimized state and title bar
        /// </summary>
        string Title { get; set; }

        /// <summary>
        /// Resource path to the widget's icon
        /// </summary>
        string IconResource { get; set; }

        /// <summary>
        /// Current widget state (Minimized/Open)
        /// </summary>
        WidgetState State { get; }

        /// <summary>
        /// Number of notifications for badge display
        /// </summary>
        int NotificationCount { get; set; }

        /// <summary>
        /// Progress value between 0.0 and 1.0 for progress bar
        /// </summary>
        double ProgressValue { get; set; }

        /// <summary>
        /// Whether the close button is visible when open
        /// </summary>
        bool ShowCloseButton { get; set; }

        /// <summary>
        /// Whether the pop-out button is visible when open
        /// </summary>
        bool ShowPopOutButton { get; set; }

        /// <summary>
        /// Widget position in grid coordinates
        /// </summary>
        Point Position { get; set; }

        /// <summary>
        /// Widget size when in open state
        /// </summary>
        Size Size { get; set; }

        /// <summary>
        /// Whether the widget is currently docked
        /// </summary>
        bool IsDocked { get; }

        /// <summary>
        /// Current dock position if docked
        /// </summary>
        DockPosition DockPosition { get; set; }

        /// <summary>
        /// Event fired when widget state changes
        /// </summary>
        event EventHandler<WidgetStateChangedEventArgs> StateChanged;

        /// <summary>
        /// Event fired when widget needs to be closed
        /// </summary>
        event EventHandler<EventArgs> CloseRequested;

        /// <summary>
        /// Transition widget to open state
        /// </summary>
        Task OpenAsync();

        /// <summary>
        /// Transition widget to minimized state
        /// </summary>
        Task MinimizeAsync();

        /// <summary>
        /// Request widget to be closed completely
        /// </summary>
        Task CloseAsync();

        /// <summary>
        /// Get the content view for this widget
        /// </summary>
        UserControl GetContentView();

        /// <summary>
        /// Update notification count and refresh badge
        /// </summary>
        /// <param name="count">New notification count</param>
        void UpdateNotifications(int count);

        /// <summary>
        /// Update progress value and refresh progress bar
        /// </summary>
        /// <param name="value">Progress value between 0.0 and 1.0</param>
        void UpdateProgress(double value);
    }

    /// <summary>
    /// Event arguments for widget state changes
    /// </summary>
    public class WidgetStateChangedEventArgs : EventArgs
    {
        public WidgetState OldState { get; }
        public WidgetState NewState { get; }
        public string WidgetId { get; }

        public WidgetStateChangedEventArgs(string widgetId, WidgetState oldState, WidgetState newState)
        {
            WidgetId = widgetId;
            OldState = oldState;
            NewState = newState;
        }
    }
}
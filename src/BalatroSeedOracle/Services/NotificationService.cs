using System;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Controls.Notifications;
using Avalonia.Threading;
using BalatroSeedOracle.Helpers;

namespace BalatroSeedOracle.Services
{
    /// <summary>
    /// Service for displaying toast notifications to users.
    /// Uses Avalonia's WindowNotificationManager for cross-platform toast support.
    /// </summary>
    public class NotificationService
    {
        private WindowNotificationManager? _notificationManager;
        private Window? _hostWindow;

        /// <summary>
        /// Initialize the notification service with a host window.
        /// Must be called after the main window is created.
        /// </summary>
        public void Initialize(Window hostWindow)
        {
            _hostWindow = hostWindow;
            _notificationManager = new WindowNotificationManager(hostWindow)
            {
                Position = NotificationPosition.TopRight,
                MaxItems = 5,
            };
        }

        /// <summary>
        /// Show a success notification
        /// </summary>
        public void ShowSuccess(string title, string message, TimeSpan? expiration = null)
        {
            ShowNotification(title, message, NotificationType.Success, expiration);
        }

        /// <summary>
        /// Show an error notification
        /// </summary>
        public void ShowError(string title, string message, TimeSpan? expiration = null)
        {
            ShowNotification(title, message, NotificationType.Error, expiration);
        }

        /// <summary>
        /// Show a warning notification
        /// </summary>
        public void ShowWarning(string title, string message, TimeSpan? expiration = null)
        {
            ShowNotification(title, message, NotificationType.Warning, expiration);
        }

        /// <summary>
        /// Show an informational notification
        /// </summary>
        public void ShowInfo(string title, string message, TimeSpan? expiration = null)
        {
            ShowNotification(title, message, NotificationType.Information, expiration);
        }

        /// <summary>
        /// Show a notification with custom type
        /// </summary>
        public void ShowNotification(
            string title,
            string message,
            NotificationType type,
            TimeSpan? expiration = null
        )
        {
            if (_notificationManager == null)
            {
                DebugLogger.LogError(
                    "NotificationService",
                    "NotificationManager not initialized. Call Initialize() first."
                );
                return;
            }

            // Ensure we're on the UI thread
            if (Dispatcher.UIThread.CheckAccess())
            {
                ShowNotificationInternal(title, message, type, expiration);
            }
            else
            {
                Dispatcher.UIThread.Post(
                    () => ShowNotificationInternal(title, message, type, expiration)
                );
            }
        }

        private void ShowNotificationInternal(
            string title,
            string message,
            NotificationType type,
            TimeSpan? expiration
        )
        {
            try
            {
                var notification = new Notification(
                    title,
                    message,
                    type,
                    expiration ?? TimeSpan.FromSeconds(5)
                );

                _notificationManager?.Show(notification);
            }
            catch (Exception ex)
            {
                DebugLogger.LogError(
                    "NotificationService",
                    $"Failed to show notification: {ex.Message}"
                );
            }
        }

        /// <summary>
        /// Show a notification asynchronously (for use in async methods)
        /// </summary>
        public Task ShowNotificationAsync(
            string title,
            string message,
            NotificationType type,
            TimeSpan? expiration = null
        )
        {
            ShowNotification(title, message, type, expiration);
            return Task.CompletedTask;
        }
    }
}

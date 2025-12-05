using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using BalatroSeedOracle.Models;

namespace BalatroSeedOracle.Services
{
    /// <summary>
    /// Service contract for widget registration and discovery
    /// </summary>
    public interface IWidgetRegistry
    {
        /// <summary>
        /// Event fired when a new widget type is registered
        /// </summary>
        event EventHandler<WidgetRegisteredEventArgs>? WidgetRegistered;

        /// <summary>
        /// Register a widget type with the system
        /// </summary>
        /// <param name="metadata">Widget metadata</param>
        void RegisterWidget(WidgetMetadata metadata);

        /// <summary>
        /// Unregister a widget type
        /// </summary>
        /// <param name="widgetId">Widget type ID to unregister</param>
        void UnregisterWidget(string widgetId);

        /// <summary>
        /// Get all registered widget types
        /// </summary>
        /// <returns>Collection of widget metadata</returns>
        IReadOnlyCollection<WidgetMetadata> GetRegisteredWidgets();

        /// <summary>
        /// Get metadata for a specific widget type
        /// </summary>
        /// <param name="widgetId">Widget type ID</param>
        /// <returns>Widget metadata or null if not found</returns>
        WidgetMetadata? GetWidgetMetadata(string widgetId);

        /// <summary>
        /// Create a new widget instance
        /// </summary>
        /// <param name="widgetId">Widget type ID</param>
        /// <returns>New widget instance</returns>
        Task<IWidget?> CreateWidgetAsync(string widgetId);

        /// <summary>
        /// Check if a widget type is registered
        /// </summary>
        /// <param name="widgetId">Widget type ID</param>
        /// <returns>True if registered, false otherwise</returns>
        bool IsWidgetRegistered(string widgetId);
    }

    /// <summary>
    /// Event arguments for widget registration
    /// </summary>
    public class WidgetRegisteredEventArgs : EventArgs
    {
        public WidgetMetadata Metadata { get; }

        public WidgetRegisteredEventArgs(WidgetMetadata metadata)
        {
            Metadata = metadata;
        }
    }
}
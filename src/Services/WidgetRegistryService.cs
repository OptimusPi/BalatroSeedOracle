using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BalatroSeedOracle.Models;

namespace BalatroSeedOracle.Services
{
    /// <summary>
    /// Service implementation for widget registration and discovery
    /// </summary>
    public class WidgetRegistryService : IWidgetRegistry
    {
        private readonly ConcurrentDictionary<string, WidgetMetadata> _registeredWidgets = new();

        /// <summary>
        /// Event fired when a new widget type is registered
        /// </summary>
        public event EventHandler<WidgetRegisteredEventArgs>? WidgetRegistered;

        /// <summary>
        /// Register a widget type with the system
        /// </summary>
        /// <param name="metadata">Widget metadata</param>
        public void RegisterWidget(WidgetMetadata metadata)
        {
            if (string.IsNullOrWhiteSpace(metadata.Id))
                throw new ArgumentException("Widget ID cannot be null or empty", nameof(metadata));

            if (string.IsNullOrWhiteSpace(metadata.Title))
                throw new ArgumentException("Widget title cannot be null or empty", nameof(metadata));

            _registeredWidgets.AddOrUpdate(metadata.Id, metadata, (key, existing) => metadata);
            WidgetRegistered?.Invoke(this, new WidgetRegisteredEventArgs(metadata));
        }

        /// <summary>
        /// Unregister a widget type
        /// </summary>
        /// <param name="widgetId">Widget type ID to unregister</param>
        public void UnregisterWidget(string widgetId)
        {
            if (string.IsNullOrWhiteSpace(widgetId))
                return;

            _registeredWidgets.TryRemove(widgetId, out _);
        }

        /// <summary>
        /// Get all registered widget types
        /// </summary>
        /// <returns>Collection of widget metadata</returns>
        public IReadOnlyCollection<WidgetMetadata> GetRegisteredWidgets()
        {
            return _registeredWidgets.Values.ToList().AsReadOnly();
        }

        /// <summary>
        /// Get metadata for a specific widget type
        /// </summary>
        /// <param name="widgetId">Widget type ID</param>
        /// <returns>Widget metadata or null if not found</returns>
        public WidgetMetadata? GetWidgetMetadata(string widgetId)
        {
            if (string.IsNullOrWhiteSpace(widgetId))
                return null;

            _registeredWidgets.TryGetValue(widgetId, out var metadata);
            return metadata;
        }

        /// <summary>
        /// Create a new widget instance
        /// </summary>
        /// <param name="widgetId">Widget type ID</param>
        /// <returns>New widget instance</returns>
        public async Task<IWidget?> CreateWidgetAsync(string widgetId)
        {
            var metadata = GetWidgetMetadata(widgetId);
            if (metadata?.WidgetType == null)
                return null;

            try
            {
                // Create widget instance using reflection
                var widget = Activator.CreateInstance(metadata.WidgetType) as IWidget;
                if (widget != null)
                {
                    // Initialize basic properties from metadata
                    widget.Title = metadata.Title;
                    widget.IconResource = metadata.IconResource;
                    widget.ShowCloseButton = metadata.AllowClose;
                    widget.ShowPopOutButton = metadata.AllowPopOut;
                    widget.Size = metadata.DefaultSize;
                }
                
                return widget;
            }
            catch (Exception ex)
            {
                // Log error (would use actual logging in real implementation)
                Console.WriteLine($"Failed to create widget {widgetId}: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Check if a widget type is registered
        /// </summary>
        /// <param name="widgetId">Widget type ID</param>
        /// <returns>True if registered, false otherwise</returns>
        public bool IsWidgetRegistered(string widgetId)
        {
            if (string.IsNullOrWhiteSpace(widgetId))
                return false;

            return _registeredWidgets.ContainsKey(widgetId);
        }
    }
}
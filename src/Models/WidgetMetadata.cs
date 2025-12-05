using System;
using Avalonia;

namespace BalatroSeedOracle.Models
{
    /// <summary>
    /// Store widget metadata and configuration
    /// </summary>
    public class WidgetMetadata
    {
        /// <summary>
        /// Unique identifier
        /// </summary>
        public string Id { get; set; } = string.Empty;

        /// <summary>
        /// Widget display title
        /// </summary>
        public string Title { get; set; } = string.Empty;

        /// <summary>
        /// Icon resource path
        /// </summary>
        public string IconResource { get; set; } = string.Empty;

        /// <summary>
        /// Type of widget implementation
        /// </summary>
        public Type? WidgetType { get; set; }

        /// <summary>
        /// Type of associated ViewModel
        /// </summary>
        public Type? ViewModelType { get; set; }

        /// <summary>
        /// Whether widget can be closed
        /// </summary>
        public bool AllowClose { get; set; } = true;

        /// <summary>
        /// Whether widget can be popped out
        /// </summary>
        public bool AllowPopOut { get; set; } = false;

        /// <summary>
        /// Default size when opened
        /// </summary>
        public Size DefaultSize { get; set; } = new Size(400, 300);

        /// <summary>
        /// Widget description
        /// </summary>
        public string Description { get; set; } = string.Empty;

        /// <summary>
        /// Widget category for grouping
        /// </summary>
        public string Category { get; set; } = string.Empty;
    }
}
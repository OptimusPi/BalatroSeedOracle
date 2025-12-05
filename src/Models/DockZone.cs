using Avalonia;

namespace BalatroSeedOracle.Models
{
    /// <summary>
    /// Define docking zones and their properties
    /// </summary>
    public class DockZone
    {
        /// <summary>
        /// Zone position
        /// </summary>
        public DockPosition Position { get; set; }

        /// <summary>
        /// Zone boundaries
        /// </summary>
        public Rect Bounds { get; set; }

        /// <summary>
        /// Whether zone is currently active
        /// </summary>
        public bool IsActive { get; set; }

        /// <summary>
        /// Whether zone is highlighted during drag
        /// </summary>
        public bool IsHighlighted { get; set; }

        /// <summary>
        /// Text to show in drop zone
        /// </summary>
        public string DisplayText { get; set; } = "DOCK WIDGET";

        /// <summary>
        /// Check if point is within zone
        /// </summary>
        public bool ContainsPoint(Point point)
        {
            return Bounds.Contains(point);
        }

        /// <summary>
        /// Activate zone during drag operation
        /// </summary>
        public void Activate()
        {
            IsActive = true;
        }

        /// <summary>
        /// Deactivate zone
        /// </summary>
        public void Deactivate()
        {
            IsActive = false;
            IsHighlighted = false;
        }

        /// <summary>
        /// Highlight zone during hover
        /// </summary>
        public void Highlight()
        {
            IsHighlighted = true;
        }

        /// <summary>
        /// Clear highlight
        /// </summary>
        public void ClearHighlight()
        {
            IsHighlighted = false;
        }
    }
}
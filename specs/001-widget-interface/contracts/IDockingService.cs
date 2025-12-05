using System;
using System.Collections.Generic;
using Avalonia;

namespace BalatroSeedOracle.Services.Widgets
{
    /// <summary>
    /// Service contract for widget docking functionality
    /// </summary>
    public interface IDockingService
    {
        /// <summary>
        /// Event fired when dock zones should be shown
        /// </summary>
        event EventHandler<DockZonesEventArgs> DockZonesRequested;

        /// <summary>
        /// Event fired when dock zones should be hidden
        /// </summary>
        event EventHandler<EventArgs> DockZonesHidden;

        /// <summary>
        /// Event fired when a widget is docked
        /// </summary>
        event EventHandler<WidgetDockedEventArgs> WidgetDocked;

        /// <summary>
        /// Create dock zones for the current container size
        /// </summary>
        /// <param name="containerSize">Container size</param>
        /// <returns>Collection of dock zones</returns>
        IReadOnlyCollection<DockZone> CreateDockZones(Size containerSize);

        /// <summary>
        /// Find dock zone at specified point
        /// </summary>
        /// <param name="point">Point to check</param>
        /// <param name="dockZones">Available dock zones</param>
        /// <returns>Dock zone at point or null</returns>
        DockZone? FindDockZoneAtPoint(Point point, IEnumerable<DockZone> dockZones);

        /// <summary>
        /// Calculate docked widget bounds
        /// </summary>
        /// <param name="position">Dock position</param>
        /// <param name="containerSize">Container size</param>
        /// <returns>Widget bounds when docked</returns>
        Rect CalculateDockedBounds(DockPosition position, Size containerSize);

        /// <summary>
        /// Start drag operation and show dock zones
        /// </summary>
        /// <param name="widget">Widget being dragged</param>
        /// <param name="containerSize">Container size</param>
        void StartDockingOperation(IWidget widget, Size containerSize);

        /// <summary>
        /// Update drag operation with current mouse position
        /// </summary>
        /// <param name="mousePosition">Current mouse position</param>
        void UpdateDragPosition(Point mousePosition);

        /// <summary>
        /// End drag operation and dock widget if in valid zone
        /// </summary>
        /// <param name="widget">Widget being dragged</param>
        /// <param name="dropPosition">Drop position</param>
        /// <returns>True if widget was docked, false otherwise</returns>
        bool EndDockingOperation(IWidget widget, Point dropPosition);

        /// <summary>
        /// Undock a widget and return to grid layout
        /// </summary>
        /// <param name="widget">Widget to undock</param>
        void UndockWidget(IWidget widget);

        /// <summary>
        /// Check if a dock position is available
        /// </summary>
        /// <param name="position">Dock position to check</param>
        /// <param name="dockedWidgets">Currently docked widgets</param>
        /// <returns>True if position is available</returns>
        bool IsDockPositionAvailable(DockPosition position, IEnumerable<IWidget> dockedWidgets);
    }

    /// <summary>
    /// Event arguments for dock zones display
    /// </summary>
    public class DockZonesEventArgs : EventArgs
    {
        public IReadOnlyCollection<DockZone> DockZones { get; }

        public DockZonesEventArgs(IReadOnlyCollection<DockZone> dockZones)
        {
            DockZones = dockZones;
        }
    }

    /// <summary>
    /// Event arguments for widget docking
    /// </summary>
    public class WidgetDockedEventArgs : EventArgs
    {
        public IWidget Widget { get; }
        public DockPosition Position { get; }
        public Rect Bounds { get; }

        public WidgetDockedEventArgs(IWidget widget, DockPosition position, Rect bounds)
        {
            Widget = widget;
            Position = position;
            Bounds = bounds;
        }
    }
}
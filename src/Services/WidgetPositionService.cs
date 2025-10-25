using System;
using System.Collections.Generic;
using System.Linq;
using BalatroSeedOracle.ViewModels;

namespace BalatroSeedOracle.Services
{
    /// <summary>
    /// Service to manage widget positioning and prevent overlaps
    /// Tracks widget positions and provides collision detection for proper placement
    /// </summary>
    public class WidgetPositionService
    {
        private readonly Dictionary<BaseWidgetViewModel, (double X, double Y)> _widgetPositions = new();
        private const double GridSize = 90.0; // Match the yGridSize from DraggableWidgetBehavior
        private const double MinimizedWidgetSize = 64.0; // Size of minimized widget icons
        private const double LeftEdgeX = 20.0; // Standard left edge position for minimized widgets
        private const double StartingY = 20.0; // Starting Y position

        /// <summary>
        /// Register a widget with its current position
        /// </summary>
        public void RegisterWidget(BaseWidgetViewModel? widget)
        {
            if (widget == null) return;
            
            _widgetPositions[widget] = (widget.PositionX, widget.PositionY);
            
            // Subscribe to position changes to keep track
            widget.PropertyChanged += (s, e) =>
            {
                if (e.PropertyName == nameof(widget.PositionX) || e.PropertyName == nameof(widget.PositionY))
                {
                    _widgetPositions[widget] = (widget.PositionX, widget.PositionY);
                }
            };
        }

        /// <summary>
        /// Unregister a widget (when it's closed/destroyed)
        /// </summary>
        public void UnregisterWidget(BaseWidgetViewModel? widget)
        {
            if (widget == null) return;
            _widgetPositions.Remove(widget);
        }

        /// <summary>
        /// Check if a position would collide with any existing minimized widgets
        /// </summary>
        public bool IsPositionOccupied(double x, double y, BaseWidgetViewModel? excludeWidget = null)
        {
            const double tolerance = 40.0; // Allow some overlap tolerance for spacing

            foreach (var kvp in _widgetPositions)
            {
                var widget = kvp.Key;
                var (widgetX, widgetY) = kvp.Value;

                // Skip the widget we're currently positioning
                if (excludeWidget != null && widget == excludeWidget)
                    continue;

                // Only check minimized widgets on the left edge
                if (!widget.IsMinimized || Math.Abs(widgetX - LeftEdgeX) > 10)
                    continue;

                // Check if positions are too close (within tolerance)
                if (Math.Abs(x - widgetX) < tolerance && Math.Abs(y - widgetY) < tolerance)
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Find the next available grid position for a minimized widget
        /// </summary>
        public (double X, double Y) FindNextAvailablePosition(BaseWidgetViewModel? excludeWidget = null)
        {
            var x = LeftEdgeX;
            var y = StartingY;

            // Keep checking grid positions until we find an empty spot
            while (IsPositionOccupied(x, y, excludeWidget))
            {
                y += GridSize;
                
                // Prevent going too far down the screen
                if (y > 800) // Reasonable screen height limit
                {
                    // If we've gone too far down, start a new column
                    x += 100; // Move to next column
                    y = StartingY;
                    
                    // If we've tried multiple columns, give up and return the requested position
                    if (x > 300)
                    {
                        return (LeftEdgeX, StartingY);
                    }
                }
            }

            return (x, y);
        }

        /// <summary>
        /// Snap a position to the grid while avoiding collisions
        /// </summary>
        public (double X, double Y) SnapToGridWithCollisionAvoidance(double currentX, double currentY, BaseWidgetViewModel widget)
        {
            // For minimized widgets, snap to left edge and find available Y position
            if (widget.IsMinimized)
            {
                var snappedY = StartingY + Math.Round((currentY - StartingY) / GridSize) * GridSize;
                
                // If the snapped position is occupied, find the next available position
                if (IsPositionOccupied(LeftEdgeX, snappedY, widget))
                {
                    return FindNextAvailablePosition(widget);
                }
                
                return (LeftEdgeX, snappedY);
            }
            
            // For expanded widgets, just return the current position (no grid snapping needed)
            return (currentX, currentY);
        }

        /// <summary>
        /// Get all registered widget positions (for debugging)
        /// </summary>
        public IEnumerable<(BaseWidgetViewModel Widget, double X, double Y)> GetAllPositions()
        {
            return _widgetPositions.Select(kvp => (kvp.Key, kvp.Value.X, kvp.Value.Y));
        }
    }
}
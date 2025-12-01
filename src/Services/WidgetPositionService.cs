using System;
using System.Collections.Generic;
using System.Linq;
using BalatroSeedOracle.ViewModels;

namespace BalatroSeedOracle.Services
{
    /// <summary>
    /// Service to manage widget positioning and prevent overlaps
    /// Supports full-screen grid positioning for flexible widget placement
    /// </summary>
    public class WidgetPositionService
    {
        private readonly Dictionary<BaseWidgetViewModel, (double X, double Y)> _widgetPositions =
            new();
        private double _lastKnownParentWidth = 1200.0; // Track parent window dimensions
        private double _lastKnownParentHeight = 700.0;
        private readonly DateTime _startupTime = DateTime.Now; // Track startup time
        private const double GridSpacingX = 90.0; // Horizontal grid spacing (widget width + spacing)
        private const double GridSpacingY = 90.0; // Vertical grid spacing (widget height + spacing)
        private const double GridOriginX = 0.0; // Grid starts at left edge (no offset needed)
        private const double GridOriginY = 45.0; // Grid starts at y=45 (no more Welcome text, allows stacking)
        private const double MinimizedWidgetSize = 80.0; // Size of minimized widget icons (with padding)
        private const double ExpandedWidgetWidth = 350.0; // Standard expanded widget width
        private const double ExpandedWidgetHeight = 450.0; // Standard expanded widget height

        /// <summary>
        /// Check if we're still in startup mode (first 3 seconds after service creation)
        /// </summary>
        private bool IsStartupMode => (DateTime.Now - _startupTime).TotalSeconds < 3.0;

        /// <summary>
        /// Calculate dynamic exclusion zones based on current parent window dimensions
        /// </summary>
        private List<(double X, double Y, double Width, double Height)> GetDynamicExclusionZones()
        {
            // Use the last known parent dimensions from the drag behavior
            var parentWidth = _lastKnownParentWidth;
            var parentHeight = _lastKnownParentHeight;

            return new List<(double X, double Y, double Width, double Height)>
            {
                // Top area - just title bar (top 40px only, not Welcome text)
                (0, 0, parentWidth, 40),
                // Bottom area - menu buttons (bottom 15% of window, minimum 100px)
                (
                    0,
                    parentHeight - Math.Max(100, parentHeight * 0.15),
                    parentWidth,
                    Math.Max(100, parentHeight * 0.15)
                ),
            };
        }

        /// <summary>
        /// Check if a position would overlap with UI exclusion zones
        /// </summary>
        private bool IsInExclusionZone(double x, double y, bool isMinimized = true)
        {
            var widgetWidth = isMinimized ? MinimizedWidgetSize : ExpandedWidgetWidth;
            var widgetHeight = isMinimized ? MinimizedWidgetSize : ExpandedWidgetHeight;

            // Get current dynamic exclusion zones based on screen size
            var exclusionZones = GetDynamicExclusionZones();

            foreach (var (zoneX, zoneY, zoneWidth, zoneHeight) in exclusionZones)
            {
                // Check if widget rectangle overlaps with exclusion zone rectangle
                if (
                    x < zoneX + zoneWidth
                    && x + widgetWidth > zoneX
                    && y < zoneY + zoneHeight
                    && y + widgetHeight > zoneY
                )
                {
#if DEBUG
                    Console.WriteLine(
                        $"Widget blocked by dynamic exclusion zone at ({x}, {y}) - zone: ({zoneX}, {zoneY}, {zoneWidth}x{zoneHeight})"
                    );
#endif
                    return true; // Widget overlaps with exclusion zone
                }
            }

            return false;
        }

        /// <summary>
        /// Force repositioning of any widgets that are currently in exclusion zones
        /// Call this after window resize or during startup cleanup
        /// </summary>
        public void CleanupWidgetsInExclusionZones()
        {
            foreach (var kvp in _widgetPositions.ToList())
            {
                var widget = kvp.Key;
                var (currentX, currentY) = kvp.Value;

                if (IsInExclusionZone(currentX, currentY, widget.IsMinimized))
                {
#if DEBUG
                    Console.WriteLine(
                        $"Cleanup: Moving widget {widget.WidgetTitle} from exclusion zone ({currentX}, {currentY})"
                    );
#endif

                    // Find a safe position in the left column
                    var safeX = GridOriginX;
                    var safeY = GridOriginY;

                    // Find the first available position in the starting column
                    for (int i = 0; i < 10; i++)
                    {
                        var testY = GridOriginY + (i * GridSpacingY);
                        var exclusionZones = GetDynamicExclusionZones();
                        var bottomExclusionStart = exclusionZones
                            .Where(z => z.Y > _lastKnownParentHeight / 2)
                            .FirstOrDefault()
                            .Y;

                        if (
                            testY + MinimizedWidgetSize < bottomExclusionStart - 20
                            && !IsPositionOccupied(safeX, testY, widget, widget.IsMinimized)
                        )
                        {
                            safeY = testY;
                            break;
                        }
                    }

#if DEBUG
                    Console.WriteLine(
                        $"Cleanup: Moving widget {widget.WidgetTitle} to safe position ({safeX}, {safeY})"
                    );
#endif

                    widget.PositionX = safeX;
                    widget.PositionY = safeY;
                    _widgetPositions[widget] = (safeX, safeY);
                }
            }
        }

        /// <summary>
        /// Register a widget with its current position
        /// </summary>
        public void RegisterWidget(BaseWidgetViewModel? widget)
        {
            if (widget == null)
                return;

            _widgetPositions[widget] = (widget.PositionX, widget.PositionY);

            // Subscribe to position changes to keep track
            widget.PropertyChanged += (s, e) =>
            {
                if (
                    e.PropertyName == nameof(widget.PositionX)
                    || e.PropertyName == nameof(widget.PositionY)
                )
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
            if (widget == null)
                return;
            _widgetPositions.Remove(widget);
        }

        /// <summary>
        /// Snap any position to the global grid starting at (20, 20) with proper widget spacing
        /// </summary>
        public (double X, double Y) SnapToGrid(double x, double y)
        {
            // Offset from grid origin, snap to grid, then add origin back
            var offsetX = x - GridOriginX;
            var offsetY = y - GridOriginY;
            var snappedOffsetX = Math.Round(offsetX / GridSpacingX) * GridSpacingX;
            var snappedOffsetY = Math.Round(offsetY / GridSpacingY) * GridSpacingY;
            var snappedX = snappedOffsetX + GridOriginX;
            var snappedY = snappedOffsetY + GridOriginY;
            return (snappedX, snappedY);
        }

        /// <summary>
        /// Check if a position would collide with any existing widgets or UI exclusion zones
        /// </summary>
        public bool IsPositionOccupied(
            double x,
            double y,
            BaseWidgetViewModel? excludeWidget = null,
            bool isMinimized = true
        )
        {
            // First check if position is in a UI exclusion zone
            if (IsInExclusionZone(x, y, isMinimized))
            {
                return true;
            }

            // During startup (when we have few widgets), be more lenient with collision detection
            // to prevent false positives during initialization
            if (_widgetPositions.Count < 5)
            {
                var tolerance = 45.0; // Much stricter tolerance during startup

                foreach (var kvp in _widgetPositions)
                {
                    var widget = kvp.Key;
                    var (widgetX, widgetY) = kvp.Value;

                    // Skip the widget we're currently positioning
                    if (excludeWidget != null && widget == excludeWidget)
                        continue;

                    // Only detect collision if widgets are very close (less than 45 pixels apart)
                    if (Math.Abs(x - widgetX) < tolerance && Math.Abs(y - widgetY) < tolerance)
                    {
                        return true;
                    }
                }
                return false;
            }

            // Normal collision detection using proper bounding box overlap
            // Calculate the size of the widget we're trying to place
            var widget1Width = isMinimized ? MinimizedWidgetSize : ExpandedWidgetWidth;
            var widget1Height = isMinimized ? MinimizedWidgetSize : ExpandedWidgetHeight;

            foreach (var kvp in _widgetPositions)
            {
                var widget = kvp.Key;
                var (widgetX, widgetY) = kvp.Value;

                // Skip the widget we're currently positioning
                if (excludeWidget != null && widget == excludeWidget)
                    continue;

                // Get the size of the existing widget
                var widget2Width = widget.IsMinimized ? MinimizedWidgetSize : ExpandedWidgetWidth;
                var widget2Height = widget.IsMinimized ? MinimizedWidgetSize : ExpandedWidgetHeight;

                // Add small padding between widgets (10px)
                var padding = 10.0;

                // Check if rectangles overlap using proper AABB collision
                bool overlapsX =
                    x < widgetX + widget2Width + padding && x + widget1Width + padding > widgetX;
                bool overlapsY =
                    y < widgetY + widget2Height + padding && y + widget1Height + padding > widgetY;

                if (overlapsX && overlapsY)
                {
                    return true; // Widgets actually overlap!
                }
            }

            return false;
        }

        /// <summary>
        /// Find the next available grid position for a widget anywhere on screen
        /// Searches in a spiral pattern from the grid origin (20, 20) with proper widget spacing
        /// </summary>
        public (double X, double Y) FindNextAvailablePosition(
            BaseWidgetViewModel? excludeWidget = null,
            bool isMinimized = true
        )
        {
            var startX = GridOriginX;
            var startY = GridOriginY;

            // For initial positioning, search the grid with proper spacing
            for (int row = 0; row < 15; row++) // Try up to 15 rows
            {
                var y = startY + (row * GridSpacingY); // Use consistent grid spacing

                for (int col = 0; col < 20; col++) // Try up to 20 columns across the screen
                {
                    var x = startX + (col * GridSpacingX); // Use consistent grid spacing

                    if (!IsPositionOccupied(x, y, excludeWidget, isMinimized))
                    {
                        return SnapToGrid(x, y);
                    }
                }
            }

            // Fallback to the grid origin if everything is occupied
            return SnapToGrid(startX, startY);
        }


        /// <summary>
        /// Snap a position to the grid with collision avoidance
        /// Now supports full-screen positioning for both minimized and expanded widgets
        /// </summary>
        public (double X, double Y) SnapToGridWithCollisionAvoidance(
            double currentX,
            double currentY,
            BaseWidgetViewModel widget,
            double parentWidth = 0,
            double parentHeight = 0
        )
        {
            // Update exclusion zones with current parent bounds if provided
            if (parentWidth > 0 && parentHeight > 0)
            {
                _lastKnownParentWidth = parentWidth;
                _lastKnownParentHeight = parentHeight;
            }

            // Snap to the global grid first
            var (snappedX, snappedY) = SnapToGrid(currentX, currentY);

            // During startup mode, skip collision avoidance entirely
            // to allow widgets to position at their intended locations
            if (IsStartupMode)
            {
                Console.WriteLine(
                    $"[WidgetPosition] Startup mode - widget at ({snappedX}, {snappedY})"
                );
                return (snappedX, snappedY);
            }

            // Check if the snapped position is occupied
            if (IsPositionOccupied(snappedX, snappedY, widget, widget.IsMinimized))
            {
                Console.WriteLine(
                    $"[WidgetPosition] Position ({snappedX}, {snappedY}) occupied, finding alternative"
                );
                // Find the nearest available position
                var (nearestX, nearestY) = FindNearestAvailablePosition(snappedX, snappedY, widget);
                Console.WriteLine(
                    $"[WidgetPosition] Alternative position: ({nearestX}, {nearestY})"
                );

                return (nearestX, nearestY);
            }

            Console.WriteLine($"[WidgetPosition] Position ({snappedX}, {snappedY}) available");
            return (snappedX, snappedY);
        }

        /// <summary>
        /// Find the nearest available position to a given coordinate using proper grid spacing
        /// Uses actual distance calculation to find the truly closest available spot
        /// </summary>
        private (double X, double Y) FindNearestAvailablePosition(
            double targetX,
            double targetY,
            BaseWidgetViewModel widget
        )
        {
            var candidates = new List<(double X, double Y, double Distance)>();
            var maxSearchRadius = 8; // Search up to 8 grid spaces away

            // Get dynamic exclusion zones to determine safe bounds
            var exclusionZones = GetDynamicExclusionZones();
            var bottomExclusionStart = exclusionZones
                .Where(z => z.Y > _lastKnownParentHeight / 2)
                .FirstOrDefault()
                .Y;
            var topExclusionEnd = exclusionZones.Where(z => z.Y == 0).FirstOrDefault().Height;

            // Set dynamic bounds based on actual exclusion zones
            var minX = GridOriginX;
            var maxX = _lastKnownParentWidth - 200; // Leave some margin for widget width
            var minY = Math.Max(GridOriginY, topExclusionEnd);
            var maxY = bottomExclusionStart - 100; // Stop before bottom exclusion zone

            // Generate all possible positions within search radius and calculate their distances
            for (int dx = -maxSearchRadius; dx <= maxSearchRadius; dx++)
            {
                for (int dy = -maxSearchRadius; dy <= maxSearchRadius; dy++)
                {
                    // Skip the center position (it's already occupied)
                    if (dx == 0 && dy == 0)
                        continue;

                    var testX = targetX + (dx * GridSpacingX);
                    var testY = targetY + (dy * GridSpacingY);

                    // Ensure position is within dynamic bounds that respect exclusion zones
                    if (testX < minX || testY < minY || testX > maxX || testY > maxY)
                        continue;

                    // Check if this position is available
                    if (!IsPositionOccupied(testX, testY, widget, widget.IsMinimized))
                    {
                        // Calculate actual distance (not just grid steps)
                        var distance = Math.Sqrt(
                            (testX - targetX) * (testX - targetX)
                                + (testY - targetY) * (testY - targetY)
                        );
                        candidates.Add((testX, testY, distance));
                    }
                }
            }

            // Sort by distance and return the closest available position
            if (candidates.Count > 0)
            {
                var closest = candidates.OrderBy(c => c.Distance).First();
                return (closest.X, closest.Y);
            }

            // Fallback to a safe position within bounds
            return (GridOriginX, Math.Max(GridOriginY, topExclusionEnd));
        }

        /// <summary>
        /// Handle window resize by repositioning widgets that are now out of bounds
        /// </summary>
        public void HandleWindowResize(double newWidth, double newHeight)
        {
            // Skip resize handling during startup to prevent widgets from being moved from their intended positions
            if (IsStartupMode)
            {
                return;
            }

            // Update our tracked dimensions
            _lastKnownParentWidth = newWidth;
            _lastKnownParentHeight = newHeight;

            // Get the new exclusion zones based on updated dimensions
            var exclusionZones = GetDynamicExclusionZones();
            var bottomExclusionStart = exclusionZones
                .Where(z => z.Y > newHeight / 2)
                .FirstOrDefault()
                .Y;
            var topExclusionEnd = exclusionZones.Where(z => z.Y == 0).FirstOrDefault().Height;

            // Calculate safe bounds
            var maxSafeX = newWidth - ExpandedWidgetWidth - 50; // Leave margin for widget width
            var maxSafeY = bottomExclusionStart - 100; // Stay above bottom exclusion zone
            var minSafeY = Math.Max(GridOriginY, topExclusionEnd + 20); // Stay below top exclusion zone

            // Check and reposition widgets that are now out of bounds
            // Process them one at a time to avoid collisions between repositioned widgets
            foreach (var kvp in _widgetPositions.ToList()) // ToList to avoid collection modification issues
            {
                var widget = kvp.Key;
                var (currentX, currentY) = kvp.Value;

                // Check if widget is now out of bounds or in exclusion zones
                bool needsRepositioning = false;
                double newX = currentX;
                double newY = currentY;

                // Check if current position is in exclusion zone (this is the most important check)
                if (IsInExclusionZone(currentX, currentY, widget.IsMinimized))
                {
                    needsRepositioning = true;
                }

                // Check horizontal bounds
                if (currentX > maxSafeX)
                {
                    newX = Math.Max(GridOriginX, maxSafeX);
                    needsRepositioning = true;
                }

                // Check vertical bounds
                if (currentY > maxSafeY || currentY < minSafeY)
                {
                    newY = Math.Clamp(currentY, minSafeY, maxSafeY);
                    needsRepositioning = true;
                }

                // Check if current position is now in exclusion zone
                if (IsInExclusionZone(currentX, currentY, widget.IsMinimized))
                {
                    needsRepositioning = true;
                }

                if (needsRepositioning)
                {
                    // Try to keep widgets in their current positions, but move them to nearest safe location if needed
                    var (gridX, gridY) = SnapToGrid(newX, newY);

                    // If this position is still in an exclusion zone or occupied, find the nearest alternative
                    if (
                        IsInExclusionZone(gridX, gridY, widget.IsMinimized)
                        || IsPositionOccupied(gridX, gridY, widget, widget.IsMinimized)
                    )
                    {
                        var nearestPosition = FindNearestAvailablePosition(gridX, gridY, widget);
                        gridX = nearestPosition.X;
                        gridY = nearestPosition.Y;
                    }

#if DEBUG
                    Console.WriteLine(
                        $"Window resize: Moving widget from ({currentX}, {currentY}) to ({gridX}, {gridY}) - Safe Y range: {minSafeY}-{maxSafeY}"
                    );
#endif
                    // Immediately update the widget's position and our tracking
                    widget.PositionX = gridX;
                    widget.PositionY = gridY;
                    _widgetPositions[widget] = (gridX, gridY);
                }
            }

            // Final cleanup: ensure no widgets ended up in exclusion zones
            CleanupWidgetsInExclusionZones();
        }

        /// <summary>
        /// Find the next available position in a specific column, maintaining vertical alignment
        /// </summary>
        private (double X, double Y) FindNextAvailablePositionInColumn(
            BaseWidgetViewModel? excludeWidget,
            double columnX,
            double minY,
            double maxY
        )
        {
            // Start from the top of the safe area and work down
            for (double y = minY; y <= maxY; y += GridSpacingY)
            {
                var snappedPos = SnapToGrid(columnX, y);
                if (
                    !IsPositionOccupied(
                        snappedPos.X,
                        snappedPos.Y,
                        excludeWidget,
                        excludeWidget?.IsMinimized ?? true
                    )
                    && !IsInExclusionZone(
                        snappedPos.X,
                        snappedPos.Y,
                        excludeWidget?.IsMinimized ?? true
                    )
                )
                {
                    return snappedPos;
                }
            }

            // If no position in the preferred column, fall back to any available position
            return FindNextAvailablePosition(excludeWidget, excludeWidget?.IsMinimized ?? true);
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

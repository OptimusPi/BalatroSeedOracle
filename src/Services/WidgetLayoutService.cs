using System;
using System.Collections.Generic;
using System.Linq;
using Avalonia;
using BalatroSeedOracle.Models;

namespace BalatroSeedOracle.Services
{
    /// <summary>
    /// Service implementation for managing widget layout and positioning
    /// </summary>
    public class WidgetLayoutService : IWidgetLayoutService
    {
        /// <summary>
        /// Grid cell size in pixels (100x100px per clarifications)
        /// </summary>
        public Size GridCellSize { get; } = new Size(100, 100);

        /// <summary>
        /// Padding from window edges in pixels
        /// </summary>
        public Thickness WindowPadding { get; } = new Thickness(10);

        /// <summary>
        /// Event fired when grid layout changes
        /// </summary>
        public event EventHandler<LayoutChangedEventArgs>? LayoutChanged;

        /// <summary>
        /// Calculate grid position from pixel coordinates
        /// </summary>
        /// <param name="pixelPosition">Pixel coordinates</param>
        /// <returns>Grid coordinates</returns>
        public Point CalculateGridPosition(Point pixelPosition)
        {
            var adjustedX = Math.Max(0, pixelPosition.X - WindowPadding.Left);
            var adjustedY = Math.Max(0, pixelPosition.Y - WindowPadding.Top);
            
            return new Point(
                Math.Floor(adjustedX / GridCellSize.Width),
                Math.Floor(adjustedY / GridCellSize.Height)
            );
        }

        /// <summary>
        /// Calculate pixel position from grid coordinates
        /// </summary>
        /// <param name="gridPosition">Grid coordinates</param>
        /// <returns>Pixel coordinates</returns>
        public Point CalculatePixelPosition(Point gridPosition)
        {
            return new Point(
                gridPosition.X * GridCellSize.Width + WindowPadding.Left,
                gridPosition.Y * GridCellSize.Height + WindowPadding.Top
            );
        }

        /// <summary>
        /// Find nearest available grid position
        /// </summary>
        /// <param name="preferredPosition">Preferred grid position</param>
        /// <param name="occupiedPositions">Currently occupied positions</param>
        /// <returns>Nearest available grid position</returns>
        public Point FindNearestAvailablePosition(Point preferredPosition, IEnumerable<Point> occupiedPositions)
        {
            var occupied = new HashSet<Point>(occupiedPositions);
            
            // If preferred position is available, use it
            if (!occupied.Contains(preferredPosition))
                return preferredPosition;

            // Search in expanding spiral pattern
            var searchRadius = 1;
            var maxRadius = 10; // Reasonable limit
            
            while (searchRadius <= maxRadius)
            {
                for (var dx = -searchRadius; dx <= searchRadius; dx++)
                {
                    for (var dy = -searchRadius; dy <= searchRadius; dy++)
                    {
                        // Only check positions on the current radius boundary
                        if (Math.Abs(dx) != searchRadius && Math.Abs(dy) != searchRadius)
                            continue;

                        var candidate = new Point(
                            Math.Max(0, preferredPosition.X + dx),
                            Math.Max(0, preferredPosition.Y + dy)
                        );

                        if (!occupied.Contains(candidate))
                            return candidate;
                    }
                }
                searchRadius++;
            }

            // Fallback to any available position
            return GetNextDefaultPosition(occupiedPositions);
        }

        /// <summary>
        /// Get next default position for new widget
        /// </summary>
        /// <param name="occupiedPositions">Currently occupied positions</param>
        /// <returns>Default position (top-left, then row-wise)</returns>
        public Point GetNextDefaultPosition(IEnumerable<Point> occupiedPositions)
        {
            var occupied = new HashSet<Point>(occupiedPositions);
            
            // Start from top-left (0,0) and go row by row
            for (var row = 0; row < 20; row++) // Reasonable limit
            {
                for (var col = 0; col < 20; col++) // Reasonable limit
                {
                    var position = new Point(col, row);
                    if (!occupied.Contains(position))
                        return position;
                }
            }

            // Fallback if somehow everything is occupied
            return new Point(0, 0);
        }

        /// <summary>
        /// Check if a position is within valid bounds
        /// </summary>
        /// <param name="gridPosition">Grid position to check</param>
        /// <param name="containerSize">Container size</param>
        /// <returns>True if position is valid</returns>
        public bool IsPositionValid(Point gridPosition, Size containerSize)
        {
            if (gridPosition.X < 0 || gridPosition.Y < 0)
                return false;

            var gridDimensions = CalculateGridDimensions(containerSize);
            return gridPosition.X < gridDimensions.Width && gridPosition.Y < gridDimensions.Height;
        }

        /// <summary>
        /// Snap position to grid if outside bounds
        /// </summary>
        /// <param name="position">Position to snap</param>
        /// <param name="containerSize">Container size</param>
        /// <returns>Snapped position within bounds</returns>
        public Point SnapToValidPosition(Point position, Size containerSize)
        {
            var gridDimensions = CalculateGridDimensions(containerSize);
            
            return new Point(
                Math.Max(0, Math.Min(position.X, gridDimensions.Width - 1)),
                Math.Max(0, Math.Min(position.Y, gridDimensions.Height - 1))
            );
        }

        /// <summary>
        /// Calculate container grid dimensions
        /// </summary>
        /// <param name="containerSize">Container size in pixels</param>
        /// <returns>Grid dimensions (columns, rows)</returns>
        public Size CalculateGridDimensions(Size containerSize)
        {
            var availableWidth = containerSize.Width - WindowPadding.Left - WindowPadding.Right;
            var availableHeight = containerSize.Height - WindowPadding.Top - WindowPadding.Bottom;
            
            return new Size(
                Math.Max(1, Math.Floor(availableWidth / GridCellSize.Width)),
                Math.Max(1, Math.Floor(availableHeight / GridCellSize.Height))
            );
        }

        /// <summary>
        /// Notify when layout changes
        /// </summary>
        /// <param name="containerSize">New container size</param>
        public void NotifyLayoutChanged(Size containerSize)
        {
            var gridDimensions = CalculateGridDimensions(containerSize);
            LayoutChanged?.Invoke(this, new LayoutChangedEventArgs(containerSize, gridDimensions));
        }
    }
}
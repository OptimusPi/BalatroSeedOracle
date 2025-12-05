using System;
using System.Collections.Generic;
using Avalonia;

namespace BalatroSeedOracle.Services
{
    /// <summary>
    /// Service contract for managing widget layout and positioning
    /// </summary>
    public interface IWidgetLayoutService
    {
        /// <summary>
        /// Grid cell size in pixels (100x100px per clarifications)
        /// </summary>
        Size GridCellSize { get; }

        /// <summary>
        /// Padding from window edges in pixels
        /// </summary>
        Thickness WindowPadding { get; }

        /// <summary>
        /// Event fired when grid layout changes
        /// </summary>
        event EventHandler<LayoutChangedEventArgs>? LayoutChanged;

        /// <summary>
        /// Calculate grid position from pixel coordinates
        /// </summary>
        /// <param name="pixelPosition">Pixel coordinates</param>
        /// <returns>Grid coordinates</returns>
        Point CalculateGridPosition(Point pixelPosition);

        /// <summary>
        /// Calculate pixel position from grid coordinates
        /// </summary>
        /// <param name="gridPosition">Grid coordinates</param>
        /// <returns>Pixel coordinates</returns>
        Point CalculatePixelPosition(Point gridPosition);

        /// <summary>
        /// Find nearest available grid position
        /// </summary>
        /// <param name="preferredPosition">Preferred grid position</param>
        /// <param name="occupiedPositions">Currently occupied positions</param>
        /// <returns>Nearest available grid position</returns>
        Point FindNearestAvailablePosition(Point preferredPosition, IEnumerable<Point> occupiedPositions);

        /// <summary>
        /// Get next default position for new widget
        /// </summary>
        /// <param name="occupiedPositions">Currently occupied positions</param>
        /// <returns>Default position (top-left, then row-wise)</returns>
        Point GetNextDefaultPosition(IEnumerable<Point> occupiedPositions);

        /// <summary>
        /// Check if a position is within valid bounds
        /// </summary>
        /// <param name="gridPosition">Grid position to check</param>
        /// <param name="containerSize">Container size</param>
        /// <returns>True if position is valid</returns>
        bool IsPositionValid(Point gridPosition, Size containerSize);

        /// <summary>
        /// Snap position to grid if outside bounds
        /// </summary>
        /// <param name="position">Position to snap</param>
        /// <param name="containerSize">Container size</param>
        /// <returns>Snapped position within bounds</returns>
        Point SnapToValidPosition(Point position, Size containerSize);

        /// <summary>
        /// Calculate container grid dimensions
        /// </summary>
        /// <param name="containerSize">Container size in pixels</param>
        /// <returns>Grid dimensions (columns, rows)</returns>
        Size CalculateGridDimensions(Size containerSize);

        /// <summary>
        /// Notify when layout changes
        /// </summary>
        /// <param name="containerSize">New container size</param>
        void NotifyLayoutChanged(Size containerSize);
    }

    /// <summary>
    /// Event arguments for layout changes
    /// </summary>
    public class LayoutChangedEventArgs : EventArgs
    {
        public Size NewContainerSize { get; }
        public Size NewGridDimensions { get; }

        public LayoutChangedEventArgs(Size containerSize, Size gridDimensions)
        {
            NewContainerSize = containerSize;
            NewGridDimensions = gridDimensions;
        }
    }
}
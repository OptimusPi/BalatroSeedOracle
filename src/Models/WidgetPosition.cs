using Avalonia;

namespace BalatroSeedOracle.Models
{
    /// <summary>
    /// Manage widget positioning and grid coordinates
    /// </summary>
    public class WidgetPosition
    {
        /// <summary>
        /// Grid cell size (100x100px per clarifications)
        /// </summary>
        public static readonly Size GridCellSize = new Size(100, 100);

        /// <summary>
        /// X coordinate in grid
        /// </summary>
        public int GridX { get; set; }

        /// <summary>
        /// Y coordinate in grid
        /// </summary>
        public int GridY { get; set; }

        /// <summary>
        /// Actual pixel X position
        /// </summary>
        public double PixelX { get; set; }

        /// <summary>
        /// Actual pixel Y position
        /// </summary>
        public double PixelY { get; set; }

        /// <summary>
        /// Whether position is valid
        /// </summary>
        public bool IsValidPosition { get; set; } = true;

        /// <summary>
        /// Whether grid position is occupied
        /// </summary>
        public bool IsOccupied { get; set; } = false;

        /// <summary>
        /// Create WidgetPosition from grid coordinates
        /// </summary>
        public static WidgetPosition FromGrid(int x, int y)
        {
            return new WidgetPosition
            {
                GridX = x,
                GridY = y,
                PixelX = x * GridCellSize.Width,
                PixelY = y * GridCellSize.Height
            };
        }

        /// <summary>
        /// Create WidgetPosition from pixel coordinates
        /// </summary>
        public static WidgetPosition FromPixels(double x, double y)
        {
            return new WidgetPosition
            {
                GridX = (int)(x / GridCellSize.Width),
                GridY = (int)(y / GridCellSize.Height),
                PixelX = x,
                PixelY = y
            };
        }

        /// <summary>
        /// Convert to grid coordinates
        /// </summary>
        public Point ToGrid()
        {
            return new Point(GridX, GridY);
        }

        /// <summary>
        /// Convert to pixel coordinates
        /// </summary>
        public Point ToPixels()
        {
            return new Point(PixelX, PixelY);
        }
    }
}
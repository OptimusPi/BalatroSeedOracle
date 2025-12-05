namespace BalatroSeedOracle.Models
{
    /// <summary>
    /// Defines the available docking positions for widgets
    /// </summary>
    public enum DockPosition
    {
        /// <summary>
        /// Widget is not docked
        /// </summary>
        None,
        
        /// <summary>
        /// Widget is docked to the left side, taking full height
        /// </summary>
        LeftFull,
        
        /// <summary>
        /// Widget is docked to the right side, taking full height
        /// </summary>
        RightFull,
        
        /// <summary>
        /// Widget is docked to the top-left quarter position
        /// </summary>
        TopLeft,
        
        /// <summary>
        /// Widget is docked to the top-right quarter position
        /// </summary>
        TopRight,
        
        /// <summary>
        /// Widget is docked to the bottom-left quarter position
        /// </summary>
        BottomLeft,
        
        /// <summary>
        /// Widget is docked to the bottom-right quarter position
        /// </summary>
        BottomRight
    }
}
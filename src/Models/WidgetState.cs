namespace BalatroSeedOracle.Models
{
    /// <summary>
    /// Defines the possible states for a widget
    /// </summary>
    public enum WidgetState
    {
        /// <summary>
        /// Widget is displayed as a minimized square button
        /// </summary>
        Minimized,
        
        /// <summary>
        /// Widget is displayed in full functionality mode
        /// </summary>
        Open,
        
        /// <summary>
        /// Widget is transitioning between states (temporary state)
        /// </summary>
        Transitioning
    }
}
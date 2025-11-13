using Avalonia.Controls;

namespace BalatroSeedOracle.Components
{
    /// <summary>
    /// SELF-CONTAINED Balatro card component with magnetic tilt behavior.
    /// NO manual behavior attachment needed in parent - just set DataContext!
    ///
    /// Features:
    /// - MagneticTiltBehavior for authentic hover effects
    /// - All visual overlays (Edition, Stickers, Soul Face)
    /// - Drag opacity feedback
    ///
    /// Usage: <components:FilterItemCard DataContext="{Binding}"/>
    /// </summary>
    public partial class FilterItemCard : UserControl
    {
        public FilterItemCard()
        {
            InitializeComponent();
        }
    }
}

using Avalonia.Controls;

namespace BalatroSeedOracle.Components
{
    /// <summary>
    /// SELF-CONTAINED Balatro card component with all animations baked in.
    ///
    /// Features:
    /// - CardDragBehavior for drag, magnetic tilt, ambient sway, and juice animations
    /// - All visual overlays (Edition, Stickers, Soul Face, Debuff)
    /// - Card name label
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

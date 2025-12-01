using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace BalatroSeedOracle.Components
{
    /// <summary>
    /// Shared widget header component with minimize button and title.
    /// Used by all widgets for consistency.
    /// </summary>
    public partial class WidgetHeader : UserControl
    {
        public WidgetHeader()
        {
            InitializeComponent();
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }
    }
}

using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace BalatroSeedOracle.Components.FilterTabs
{
    /// <summary>
    /// UI control for displaying a single clause row in the Validate Filter tab
    /// </summary>
    public partial class ClauseRowControl : UserControl
    {
        public ClauseRowControl()
        {
            InitializeComponent();
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }
    }
}
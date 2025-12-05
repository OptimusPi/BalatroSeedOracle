using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Markup.Xaml;
using BalatroSeedOracle.ViewModels.Widgets;

namespace BalatroSeedOracle.Views.Widgets
{
    /// <summary>
    /// Open widget view - full functionality window
    /// </summary>
    public partial class WidgetView : UserControl
    {
        public WidgetView()
        {
            InitializeComponent();
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }
    }
}
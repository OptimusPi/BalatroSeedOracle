using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Markup.Xaml;
using BalatroSeedOracle.ViewModels;

namespace BalatroSeedOracle.Components.Widgets
{
    public partial class SearchWidget : UserControl
    {
        public SearchWidgetViewModel ViewModel => (SearchWidgetViewModel)DataContext!;

        public SearchWidget()
        {
            InitializeComponent();

            // Handle pointer enter/leave for hover state
            PointerEntered += (s, e) => ViewModel.IsHovered = true;
            PointerExited += (s, e) => ViewModel.IsHovered = false;
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }
    }
}

using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using BalatroSeedOracle.ViewModels;

namespace BalatroSeedOracle.Views.Modals
{
    public partial class CreditsModal : UserControl
    {
        public CreditsModal()
        {
            InitializeComponent();
            DataContext = new CreditsModalViewModel();
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }
    }
}

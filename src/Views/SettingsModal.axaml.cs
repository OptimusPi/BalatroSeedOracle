using Avalonia.Controls;
using BalatroSeedOracle.ViewModels;

namespace BalatroSeedOracle.Views
{
    public partial class SettingsModal : UserControl
    {
        public SettingsModal()
        {
            InitializeComponent();
            DataContext = new SettingsModalViewModel();
        }

        public SettingsModalViewModel? ViewModel => DataContext as SettingsModalViewModel;
    }
}

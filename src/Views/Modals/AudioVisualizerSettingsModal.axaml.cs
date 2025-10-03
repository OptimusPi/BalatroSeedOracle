using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using BalatroSeedOracle.ViewModels;

namespace BalatroSeedOracle.Views.Modals
{
    public partial class AudioVisualizerSettingsModal : UserControl
    {
        public AudioVisualizerSettingsModal()
        {
            InitializeComponent();
            DataContext = new AudioVisualizerSettingsModalViewModel();
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }

        public AudioVisualizerSettingsModalViewModel? ViewModel => DataContext as AudioVisualizerSettingsModalViewModel;
    }
}

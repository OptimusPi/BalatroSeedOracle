using System;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using BalatroSeedOracle.ViewModels;

namespace BalatroSeedOracle.Views.Modals
{
    public partial class AudioVisualizerSettingsModal : UserControl
    {
        private AudioVisualizerSettingsModalViewModel? _subscribedVm;

        public AudioVisualizerSettingsModal()
        {
            AvaloniaXamlLoader.Load(this);
            DataContextChanged += OnDataContextChanged;
        }

        private void OnDataContextChanged(object? sender, EventArgs e)
        {
            if (_subscribedVm != null)
            {
                _subscribedVm.RequestClose -= OnRequestClose;
                _subscribedVm = null;
            }

            if (DataContext is AudioVisualizerSettingsModalViewModel vm)
            {
                vm.RequestClose += OnRequestClose;
                _subscribedVm = vm;
            }
        }

        private void OnRequestClose(object? sender, EventArgs e)
        {
            var mainMenu = App.GetService<BalatroMainMenu>();
            if (mainMenu != null)
            {
                mainMenu.HideModalContent();
            }
            else if (TopLevel.GetTopLevel(this) is Window window && window is not MainWindow)
            {
                window.Close();
            }
        }
    }
}

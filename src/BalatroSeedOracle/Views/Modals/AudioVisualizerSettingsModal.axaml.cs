using System;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using BalatroSeedOracle.ViewModels;

namespace BalatroSeedOracle.Views.Modals
{
    public partial class AudioVisualizerSettingsModal : UserControl
    {
        public AudioVisualizerSettingsModal()
        {
            AvaloniaXamlLoader.Load(this);
            DataContextChanged += OnDataContextChanged;
        }

        private void OnDataContextChanged(object? sender, EventArgs e)
        {
            if (DataContext is AudioVisualizerSettingsModalViewModel vm)
            {
                vm.RequestClose += (_, _) =>
                {
                    if (TopLevel.GetTopLevel(this) is Window window)
                        window.Close();
                };
            }
        }
    }
}

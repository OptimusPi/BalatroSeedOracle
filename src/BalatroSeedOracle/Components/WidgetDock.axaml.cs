using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using BalatroSeedOracle.ViewModels;

namespace BalatroSeedOracle.Components
{
    public partial class WidgetDock : UserControl
    {
        public WidgetDock()
        {
            InitializeComponent();
        }

        private void InitializeComponent()
        {
            Avalonia.Markup.Xaml.AvaloniaXamlLoader.Load(this);
        }

        private void OnMusicMixerClick(object? sender, PointerPressedEventArgs e)
        {
            if (DataContext is BalatroMainMenuViewModel vm)
            {
                vm.ToggleMusicMixerWidgetCommand.Execute(null);
            }
            e.Handled = true;
        }

        private void OnVisualizerClick(object? sender, PointerPressedEventArgs e)
        {
            if (DataContext is BalatroMainMenuViewModel vm)
            {
                vm.ToggleVisualizerWidgetCommand.Execute(null);
            }
            e.Handled = true;
        }

        private void OnTransitionDesignerClick(object? sender, PointerPressedEventArgs e)
        {
            if (DataContext is BalatroMainMenuViewModel vm)
            {
                vm.ToggleTransitionDesignerWidgetCommand.Execute(null);
            }
            e.Handled = true;
        }

        private void OnFertilizerClick(object? sender, PointerPressedEventArgs e)
        {
            if (DataContext is BalatroMainMenuViewModel vm)
            {
                vm.ToggleFertilizerWidgetCommand.Execute(null);
            }
            e.Handled = true;
        }

        private void OnHostApiClick(object? sender, PointerPressedEventArgs e)
        {
            if (DataContext is BalatroMainMenuViewModel vm)
            {
                vm.ToggleHostApiWidgetCommand.Execute(null);
            }
            e.Handled = true;
        }

        private void OnEventFXClick(object? sender, PointerPressedEventArgs e)
        {
            if (DataContext is BalatroMainMenuViewModel vm)
            {
                vm.ToggleEventFXWidgetCommand.Execute(null);
            }
            e.Handled = true;
        }
    }
}

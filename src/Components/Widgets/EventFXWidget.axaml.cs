using Avalonia.Controls;
using Avalonia.Input;
using BalatroSeedOracle.ViewModels;

namespace BalatroSeedOracle.Components;

public partial class EventFXWidget : UserControl
{
    public EventFXWidget()
    {
        InitializeComponent();
    }

    private void OnMinimizedIconPressed(object? sender, PointerPressedEventArgs e)
    {
    }

    private void OnMinimizedIconReleased(object? sender, PointerReleasedEventArgs e)
    {
        if (DataContext is EventFXWidgetViewModel vm)
        {
            vm.IsMinimized = false;
        }
    }
}

using Avalonia.Markup.Xaml;
using BalatroSeedOracle.Components;
using BalatroSeedOracle.ViewModels;

namespace BalatroSeedOracle.Desktop.Components.Widgets;

/// <summary>
/// ApiHostWidget - Hosts the Motely API server within BSO
/// DataContext is bound via XAML from parent ViewModel (the Avalonia way)
/// </summary>
public partial class ApiHostWidget : BaseWidgetControl
{
    public ApiHostWidget()
    {
        InitializeComponent();
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }

    // Cleanup when widget is unloaded
    protected override void OnUnloaded(Avalonia.Interactivity.RoutedEventArgs e)
    {
        base.OnUnloaded(e);
        if (DataContext is ApiHostWidgetViewModel vm)
        {
            _ = vm.CleanupAsync();
        }
    }
}

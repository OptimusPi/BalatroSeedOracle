using Avalonia.Markup.Xaml;

namespace BalatroSeedOracle.Components;

/// <summary>
/// Browser stub for HostApiWidget.
/// The actual HostApiWidget requires MotelyApiServer which uses HttpListener
/// and other desktop-only APIs. This stub satisfies XAML references in browser builds.
/// </summary>
public partial class HostApiWidgetBrowser : BaseWidgetControl
{
    public HostApiWidgetBrowser()
    {
        InitializeComponent();
        // Hide by default since this is just a placeholder
        IsVisible = false;
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }
}

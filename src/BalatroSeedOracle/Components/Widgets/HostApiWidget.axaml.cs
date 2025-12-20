using System;
using Avalonia.Markup.Xaml;
using BalatroSeedOracle.ViewModels;

namespace BalatroSeedOracle.Components;

/// <summary>
/// Host API Widget - controls the MotelyApiServer, shows status and request log.
/// </summary>
public partial class HostApiWidget : BaseWidgetControl
{
    public HostApiWidgetViewModel? ViewModel { get; }

    public HostApiWidget()
    {
        ViewModel = new HostApiWidgetViewModel();
        DataContext = ViewModel;

        InitializeComponent();

        this.DetachedFromVisualTree += OnDetachedFromVisualTree;
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }

    private void OnDetachedFromVisualTree(object? sender, EventArgs e)
    {
        ViewModel?.Dispose();
    }

    private async void CopyLog_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        if (ViewModel == null) return;
        
        var topLevel = Avalonia.Controls.TopLevel.GetTopLevel(this);
        if (topLevel?.Clipboard != null)
        {
            await topLevel.Clipboard.SetTextAsync(ViewModel.LogText);
            // Optional: ViewModel.AddLog("Log copied to clipboard");
        }
    }
}

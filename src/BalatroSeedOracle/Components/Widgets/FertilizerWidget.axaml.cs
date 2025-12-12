using System;
using Avalonia.Markup.Xaml;
using BalatroSeedOracle.Helpers;
using BalatroSeedOracle.Services;
using BalatroSeedOracle.ViewModels;

namespace BalatroSeedOracle.Components;

/// <summary>
/// Fertilizer Widget - displays seed count and allows clearing the global seed pile.
/// </summary>
public partial class FertilizerWidget : BaseWidgetControl
{
    public FertilizerWidgetViewModel? ViewModel { get; }

    public FertilizerWidget()
    {
        // Get FertilizerService from DI
        var fertilizerService = ServiceHelper.GetService<FertilizerService>();
        if (fertilizerService != null)
        {
            ViewModel = new FertilizerWidgetViewModel(fertilizerService);
            DataContext = ViewModel;
        }

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
}

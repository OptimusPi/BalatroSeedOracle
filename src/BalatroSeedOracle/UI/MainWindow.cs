using System;
using Avalonia.Controls;
using Avalonia.Media;
using BalatroSeedOracle.Theme;

namespace BalatroSeedOracle.UI;

public class MainWindow : Window
{
    private readonly IServiceProvider _services;

    public MainWindow(IServiceProvider services)
    {
        _services = services;

        Title = "Balatro Seed Oracle";
        Width = 1280;
        Height = 800;
        MinWidth = 800;
        MinHeight = 600;
        Background = new SolidColorBrush(BalatroColors.DarkGrey);
        FontFamily = BalatroFonts.Primary;

        Content = new MainMenuComponent(services);
    }
}

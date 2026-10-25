using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;

namespace BalatroSeedOracle.Helpers;

/// <summary>
/// Resolves the application's <see cref="TopLevel"/> (main window) for pickers/clipboard.
/// The app is desktop-only (classic desktop lifetime), so the main window is the TopLevel.
/// </summary>
public static class TopLevelHelper
{
    public static TopLevel? GetTopLevel()
    {
        if (Avalonia.Application.Current?.ApplicationLifetime
            is IClassicDesktopStyleApplicationLifetime desktop)
        {
            return desktop.MainWindow;
        }
        return null;
    }
}

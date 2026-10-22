using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;

namespace BalatroSeedOracle.Helpers;

/// <summary>
/// Resolves the application's <see cref="TopLevel"/> (main window) for pickers/clipboard.
/// TIER0-STUB: replace in Tier 1 once modal host abstraction lands.
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

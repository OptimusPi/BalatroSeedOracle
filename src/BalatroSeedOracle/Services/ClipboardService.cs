using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input.Platform;
using BalatroSeedOracle.Helpers;

namespace BalatroSeedOracle.Services
{
    public static class ClipboardService
    {
        public static async Task CopyToClipboardAsync(string text)
        {
            try
            {
                IClipboard? clipboard = null;

                if (
                    Application.Current?.ApplicationLifetime
                    is Avalonia.Controls.ApplicationLifetimes.IClassicDesktopStyleApplicationLifetime desktop
                )
                {
                    clipboard = desktop.MainWindow?.Clipboard;
                }
                else if (
                    Application.Current?.ApplicationLifetime
                    is Avalonia.Controls.ApplicationLifetimes.ISingleViewApplicationLifetime singleView
                )
                {
                    var topLevel = TopLevel.GetTopLevel(singleView.MainView);
                    clipboard = topLevel?.Clipboard;
                }

                if (clipboard != null)
                {
                    await clipboard.SetTextAsync(text);
                    DebugLogger.Log("ClipboardService", $"Copied to clipboard: {text}");
                }
            }
            catch (System.Exception ex)
            {
                DebugLogger.LogError(
                    "ClipboardService",
                    $"Failed to copy to clipboard: {ex.Message}"
                );
            }
        }
    }
}

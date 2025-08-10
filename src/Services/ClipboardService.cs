using System.Threading.Tasks;
using Avalonia;
using Avalonia.Input.Platform;
using Oracle.Helpers;

namespace Oracle.Services
{
    public static class ClipboardService
    {
        public static async Task CopyToClipboardAsync(string text)
        {
            try
            {
                if (
                    Application.Current?.ApplicationLifetime
                    is Avalonia.Controls.ApplicationLifetimes.IClassicDesktopStyleApplicationLifetime desktop
                )
                {
                    var clipboard = desktop.MainWindow?.Clipboard;
                    if (clipboard != null)
                    {
                        await clipboard.SetTextAsync(text);
                        DebugLogger.Log("ClipboardService", $"Copied to clipboard: {text}");
                    }
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

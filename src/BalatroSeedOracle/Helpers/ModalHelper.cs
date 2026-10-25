using System;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Threading;
using MsBox.Avalonia;
using MsBox.Avalonia.Base;
using MsBox.Avalonia.Enums;

namespace BalatroSeedOracle.Helpers;

/// <summary>
/// Simple message dialogs for destructive-action confirmation and operation summaries
/// (MessageBox.Avalonia). Dialogs are owned by the main window, so they are truly modal,
/// centered on the app and cannot fall behind it; without a main window they fall back to
/// a standalone window. Calls from background threads are marshalled to the UI thread.
/// </summary>
public static class ModalHelper
{
    public static Task ShowSuccessAsync(string title, string message) =>
        ShowAsync(title, message, ButtonEnum.Ok, Icon.None);

    public static async Task<bool> ShowConfirmationAsync(string title, string message) =>
        await ShowAsync(title, message, ButtonEnum.YesNo, Icon.Warning) == ButtonResult.Yes;

    private static Task<ButtonResult> ShowAsync(string title, string message, ButtonEnum buttons, Icon icon)
    {
        if (!Dispatcher.UIThread.CheckAccess())
            return Dispatcher.UIThread.InvokeAsync(() => ShowAsync(title, message, buttons, icon));

        try
        {
            IMsBox<ButtonResult> box = MessageBoxManager.GetMessageBoxStandard(title, message, buttons, icon);
            return TopLevelHelper.GetTopLevel() is Window { IsVisible: true } owner
                ? box.ShowWindowDialogAsync(owner)
                : box.ShowAsync();
        }
        catch (Exception ex)
        {
            DebugLogger.LogError(nameof(ModalHelper), $"Cannot show dialog '{title}': {ex.Message}");
            return Task.FromResult(ButtonResult.None);
        }
    }
}

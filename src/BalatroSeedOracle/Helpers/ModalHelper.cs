using System.Threading.Tasks;
using MsBox.Avalonia;
using MsBox.Avalonia.Enums;

namespace BalatroSeedOracle.Helpers;

/// <summary>
/// Simple message dialogs for destructive-action confirmation and operation summaries.
/// TIER0-STUB: replace in Tier 1 with the app's styled Balatro modal once it is unified
/// (these use MessageBox.Avalonia, already referenced by the project).
/// </summary>
public static class ModalHelper
{
    public static async Task ShowSuccessAsync(string title, string message)
    {
        await MessageBoxManager.GetMessageBoxStandard(
            title,
            message,
            ButtonEnum.Ok,
            Icon.None
        ).ShowAsync();
    }

    public static async Task<bool> ShowConfirmationAsync(string title, string message)
    {
        var result = await MessageBoxManager.GetMessageBoxStandard(
            title,
            message,
            ButtonEnum.YesNo,
            Icon.Warning
        ).ShowAsync();
        return result == ButtonResult.Yes;
    }
}

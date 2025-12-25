using System;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using BalatroSeedOracle.Helpers;
using BalatroSeedOracle.ViewModels.FilterTabs;

namespace BalatroSeedOracle.Components.FilterTabs
{
    /// <summary>
    /// MVVM Save Filter Tab - replaces save logic from original FiltersModal
    /// Minimal code-behind, all logic in SaveFilterTabViewModel
    /// </summary>
    public partial class SaveFilterTab : UserControl
    {
        public SaveFilterTabViewModel? ViewModel => DataContext as SaveFilterTabViewModel;

        public SaveFilterTab()
        {
            InitializeComponent();
            
            if (ViewModel != null)
            {
                ViewModel.CopyToClipboardRequested += async (s, text) => await CopyToClipboardAsync(text);
            }
        }

        public async Task CopyToClipboardAsync(string text)
        {
            try
            {
                var topLevel = TopLevel.GetTopLevel(this);
                if (topLevel?.Clipboard != null)
                {
                    await topLevel.Clipboard.SetTextAsync(text);
                    DebugLogger.Log("SaveFilterTab", $"Copied to clipboard: {text}");
                }
            }
            catch (Exception ex)
            {
                DebugLogger.LogError("SaveFilterTab", $"Failed to copy to clipboard: {ex.Message}");
            }
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }
    }
}

using System;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using BalatroSeedOracle.Helpers;
using BalatroSeedOracle.ViewModels.FilterTabs;

namespace BalatroSeedOracle.Components.FilterTabs
{
    public partial class ValidateFilterTab : UserControl
    {
        public ValidateFilterTabViewModel? ViewModel => DataContext as ValidateFilterTabViewModel;

        public ValidateFilterTab()
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
                    DebugLogger.Log("ValidateFilterTab", $"Copied to clipboard: {text}");
                }
            }
            catch (Exception ex)
            {
                DebugLogger.LogError("ValidateFilterTab", $"Failed to copy to clipboard: {ex.Message}");
            }
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }
    }
}

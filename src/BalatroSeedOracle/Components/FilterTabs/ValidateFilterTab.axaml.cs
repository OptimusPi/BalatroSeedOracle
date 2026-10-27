using System;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Input.Platform;
using Avalonia.Markup.Xaml;
using BalatroSeedOracle.Helpers;
using BalatroSeedOracle.ViewModels.FilterTabs;

namespace BalatroSeedOracle.Components.FilterTabs
{
    public partial class ValidateFilterTab : UserControl
    {
        private ValidateFilterTabViewModel? _subscribedViewModel;

        public ValidateFilterTabViewModel? ViewModel => DataContext as ValidateFilterTabViewModel;

        public ValidateFilterTab()
        {
            InitializeComponent();

            DataContextChanged += OnDataContextChanged;
        }

        private void OnDataContextChanged(object? sender, EventArgs e)
        {
            if (_subscribedViewModel != null)
            {
                _subscribedViewModel.CopyToClipboardRequested -= OnCopyToClipboardRequested;
                _subscribedViewModel = null;
            }

            if (ViewModel != null)
            {
                ViewModel.CopyToClipboardRequested += OnCopyToClipboardRequested;
                _subscribedViewModel = ViewModel;
            }
        }

        private async void OnCopyToClipboardRequested(object? sender, string text)
        {
            await CopyToClipboardAsync(text);
        }

        protected override void OnAttachedToVisualTree(Avalonia.VisualTreeAttachmentEventArgs e)
        {
            base.OnAttachedToVisualTree(e);
            if (_subscribedViewModel == null && ViewModel != null)
            {
                ViewModel.CopyToClipboardRequested += OnCopyToClipboardRequested;
                _subscribedViewModel = ViewModel;
            }
        }

        protected override void OnDetachedFromVisualTree(Avalonia.VisualTreeAttachmentEventArgs e)
        {
            if (_subscribedViewModel != null)
            {
                _subscribedViewModel.CopyToClipboardRequested -= OnCopyToClipboardRequested;
                _subscribedViewModel = null;
            }
            base.OnDetachedFromVisualTree(e);
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
                DebugLogger.LogError(
                    "ValidateFilterTab",
                    $"Failed to copy to clipboard: {ex.Message}"
                );
            }
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }
    }
}

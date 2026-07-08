using Avalonia.Controls;
using Avalonia.Input.Platform;
using Avalonia.Interactivity;
using BalatroSeedOracle.ViewModels;

namespace BalatroSeedOracle.Views.Modals
{
    /// <summary>
    /// Word lists modal - view wiring only. All list/file logic lives in
    /// WordListsModalViewModel. Code-behind owns only clipboard access (a view concern).
    /// </summary>
    public partial class WordListsModal : UserControl
    {
        private WordListsModalViewModel? Vm => DataContext as WordListsModalViewModel;

        public WordListsModal()
        {
            InitializeComponent();
            DataContext = new WordListsModalViewModel();
        }

        // view-only: OK — clipboard requires TopLevel; text is handed straight to the VM
        private async void OnPasteClick(object? sender, RoutedEventArgs e)
        {
            if (Vm is not { } vm)
                return;

            var topLevel = TopLevel.GetTopLevel(this);
            if (topLevel?.Clipboard == null)
            {
                vm.ReportStatus("Clipboard not available");
                return;
            }

            var clipboardText = await topLevel.Clipboard.TryGetTextAsync();
            if (!string.IsNullOrEmpty(clipboardText))
            {
                vm.PasteText(clipboardText);
            }
            else
            {
                vm.ReportStatus("Clipboard is empty");
            }
        }
    }
}

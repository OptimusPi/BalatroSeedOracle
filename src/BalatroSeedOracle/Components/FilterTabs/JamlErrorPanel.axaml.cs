using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Markup.Xaml;
using BalatroSeedOracle.ViewModels.FilterTabs;

namespace BalatroSeedOracle.Components.FilterTabs
{
    public partial class JamlErrorPanel : UserControl
    {
        public JamlErrorPanel()
        {
            InitializeComponent();
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }

        private void OnErrorItemClick(object? sender, PointerPressedEventArgs e)
        {
            if (sender is Border border && border.DataContext is ValidationErrorItem error)
            {
                // Notify parent to jump to error location
                if (DataContext is JamlEditorTabViewModel viewModel)
                {
                    viewModel.JumpToError?.Invoke(error.LineNumber, error.Column);
                }
            }
        }
    }
}

using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Markup.Xaml;
using BalatroSeedOracle.ViewModels.FilterTabs;

namespace BalatroSeedOracle.Components.FilterTabs
{
    public partial class YAMLErrorPanel : UserControl
    {
        public YAMLErrorPanel()
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
                if (DataContext is YAMLEditorTabViewModel viewModel)
                {
                    viewModel.RequestJumpToError(error.LineNumber, error.Column);
                }
            }
        }
    }
}

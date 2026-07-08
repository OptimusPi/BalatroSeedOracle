using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using BalatroSeedOracle.ViewModels;

namespace BalatroSeedOracle.Views.Modals
{
    public partial class ToolsModal : UserControl
    {
        public ToolsModalViewModel? ViewModel { get; }

        /// <summary>Parameterless ctor for XAML loader only.</summary>
        public ToolsModal()
        {
            InitializeComponent();
        }

        public ToolsModal(ToolsModalViewModel viewModel)
        {
            ViewModel = viewModel;
            InitializeComponent();
            DataContext = ViewModel;
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }
    }
}

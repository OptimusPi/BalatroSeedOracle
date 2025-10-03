using System;
using Avalonia.Controls;
using BalatroSeedOracle.ViewModels;

namespace BalatroSeedOracle.Controls
{
    public partial class AnteSelector : UserControl
    {
        private AnteSelectorViewModel? _viewModel;

        public event EventHandler<int[]>? SelectedAntesChanged;

        public AnteSelector()
        {
            InitializeComponent();
            InitializeViewModel();
        }

        private void InitializeViewModel()
        {
            _viewModel = new AnteSelectorViewModel();
            DataContext = _viewModel;

            // Forward ViewModel events to maintain API compatibility
            _viewModel.SelectedAntesChanged += (s, e) => SelectedAntesChanged?.Invoke(this, e);
        }

        // Public API - delegates to ViewModel
        public int[] GetSelectedAntes()
        {
            return _viewModel?.GetSelectedAntes() ?? Array.Empty<int>();
        }

        public void SetSelectedAntes(int[] antes)
        {
            _viewModel?.SetSelectedAntes(antes);
        }
    }
}

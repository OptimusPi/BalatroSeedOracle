using System;
using Avalonia.Controls;
using BalatroSeedOracle.ViewModels;

namespace BalatroSeedOracle.Controls
{
    public partial class EditionSelector : UserControl
    {
        private EditionSelectorViewModel? _viewModel;

        public event EventHandler<string>? EditionChanged;

        public EditionSelector()
        {
            InitializeComponent();
            InitializeViewModel();
        }

        private void InitializeViewModel()
        {
            _viewModel = new EditionSelectorViewModel();
            DataContext = _viewModel;

            // Forward ViewModel events to maintain API compatibility
            _viewModel.EditionChanged += (s, e) => EditionChanged?.Invoke(this, e);
        }

        // Public API - delegates to ViewModel
        public string GetSelectedEdition()
        {
            return _viewModel?.GetSelectedEdition() ?? "";
        }

        public void SetSelectedEdition(string edition)
        {
            _viewModel?.SetSelectedEdition(edition);
        }

        // Static helper methods
        public static string GetEditionDisplayName(string edition)
        {
            return EditionSelectorViewModel.GetEditionDisplayName(edition);
        }

        public static string[] GetAllEditions()
        {
            return EditionSelectorViewModel.GetAllEditions();
        }

        public static int GetEditionPowerLevel(string edition)
        {
            return EditionSelectorViewModel.GetEditionPowerLevel(edition);
        }
    }
}

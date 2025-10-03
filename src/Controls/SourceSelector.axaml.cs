using System;
using Avalonia.Controls;
using BalatroSeedOracle.ViewModels;

namespace BalatroSeedOracle.Controls
{
    public partial class SourceSelector : UserControl
    {
        private SourceSelectorViewModel? _viewModel;

        public event EventHandler<string>? SourceChanged;

        public SourceSelector()
        {
            InitializeComponent();
            InitializeViewModel();
        }

        private void InitializeViewModel()
        {
            _viewModel = new SourceSelectorViewModel();
            DataContext = _viewModel;

            // Forward ViewModel events to maintain API compatibility
            _viewModel.SourceChanged += (s, e) => SourceChanged?.Invoke(this, e);
        }

        // Public API - delegates to ViewModel
        public string GetSelectedSource()
        {
            return _viewModel?.GetSelectedSource() ?? "";
        }

        public void SetSelectedSource(string source)
        {
            _viewModel?.SetSelectedSource(source);
        }

        // Static helper methods
        public static string GetSourceDisplayName(string source)
        {
            return SourceSelectorViewModel.GetSourceDisplayName(source);
        }

        public static string[] GetAllSources()
        {
            return SourceSelectorViewModel.GetAllSources();
        }
    }
}

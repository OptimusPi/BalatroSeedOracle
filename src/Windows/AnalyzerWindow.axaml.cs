using System.Collections.Generic;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using BalatroSeedOracle.Features.Analyzer;
using BalatroSeedOracle.Helpers;
using BalatroSeedOracle.ViewModels;

namespace BalatroSeedOracle.Windows
{
    public partial class AnalyzerWindow : Window
    {
        private AnalyzerView? _analyzerView;
        private readonly AnalyzerViewModel _viewModel;

        public AnalyzerWindow()
            : this(new AnalyzerViewModel()) { }

        public AnalyzerWindow(AnalyzerViewModel viewModel)
        {
            _viewModel = viewModel;
            InitializeComponent();
            _analyzerView = this.FindControl<AnalyzerView>("AnalyzerViewContent");

            DataContext = _viewModel;

            if (_analyzerView != null)
            {
                _analyzerView.DataContext = _viewModel;
            }
        }

        public AnalyzerWindow(AnalyzerViewModel viewModel, string seed)
            : this(viewModel)
        {
            // Set the seed after initialization
            SetSeedAndAnalyze(seed);
        }

        public AnalyzerWindow(AnalyzerViewModel viewModel, IEnumerable<string> seeds)
            : this(viewModel)
        {
            // Set multiple seeds for navigation
            _viewModel.SetSeeds(seeds);
            Title = $"Balatro Seed Analyzer - {_viewModel.TotalResults} seeds";
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }

        /// <summary>
        /// Set the seed and start analysis
        /// </summary>
        public void SetSeedAndAnalyze(string seed)
        {
            _viewModel.SetSeeds(new[] { seed });
            Title = $"Balatro Seed Analyzer - {seed}";
        }
    }
}

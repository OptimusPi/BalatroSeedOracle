using System.Collections.Generic;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using BalatroSeedOracle.Features.Analyzer;
using BalatroSeedOracle.Helpers;
using BalatroSeedOracle.ViewModels;

namespace BalatroSeedOracle.Windows
{
    /// <summary>
    /// Analyzer window for seed analysis.
    /// Uses direct x:Name field access (no FindControl anti-pattern).
    /// </summary>
    public partial class AnalyzerWindow : Window
    {
        private readonly AnalyzerViewModel _viewModel;

        public AnalyzerWindow()
            : this(new AnalyzerViewModel()) { }

        public AnalyzerWindow(AnalyzerViewModel viewModel)
        {
            _viewModel = viewModel;
            InitializeComponent();

            DataContext = _viewModel;

            // Direct x:Name field access - no FindControl!
            if (AnalyzerViewContent != null)
            {
                AnalyzerViewContent.DataContext = _viewModel;
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

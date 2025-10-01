using System.Collections.Generic;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using BalatroSeedOracle.Features.Analyzer;
using BalatroSeedOracle.ViewModels;
using BalatroSeedOracle.Helpers;

namespace BalatroSeedOracle.Windows
{
    public partial class AnalyzerWindow : Window
    {
        private AnalyzerView? _analyzerView;
        private AnalyzerViewModel? _viewModel;

        public AnalyzerWindow()
        {
            InitializeComponent();
            _analyzerView = this.FindControl<AnalyzerView>("AnalyzerViewContent");
            
            // Get ViewModel from DI
            _viewModel = ServiceHelper.GetRequiredService<AnalyzerViewModel>();
            DataContext = _viewModel;
            
            if (_analyzerView != null)
            {
                _analyzerView.DataContext = _viewModel;
            }
        }

        public AnalyzerWindow(string seed) : this()
        {
            // Set the seed after initialization
            SetSeedAndAnalyze(seed);
        }

        public AnalyzerWindow(IEnumerable<string> seeds) : this()
        {
            // Set multiple seeds for navigation
            _viewModel?.SetSeeds(seeds);
            Title = $"Balatro Seed Analyzer - {_viewModel?.TotalResults ?? 0} seeds";
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
            _viewModel?.SetSeeds(new[] { seed });
            Title = $"Balatro Seed Analyzer - {seed}";
        }
    }
}

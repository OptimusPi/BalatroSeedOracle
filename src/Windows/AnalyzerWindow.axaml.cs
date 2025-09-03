using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using BalatroSeedOracle.Views.Modals;

namespace BalatroSeedOracle.Windows
{
    public partial class AnalyzerWindow : Window
    {
        private AnalyzeModal? _analyzeModalContent;

        public AnalyzerWindow()
        {
            InitializeComponent();
            _analyzeModalContent = this.FindControl<AnalyzeModal>("AnalyzeModalContent");
        }

        public AnalyzerWindow(string seed) : this()
        {
            // Set the seed after initialization
            SetSeedAndAnalyze(seed);
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
            _analyzeModalContent?.SetSeedAndAnalyze(seed);
            
            // Update window title to include the seed
            Title = $"Seed Analyzer - {seed}";
        }
    }
}
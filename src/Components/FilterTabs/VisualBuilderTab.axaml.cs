using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using BalatroSeedOracle.ViewModels.FilterTabs;
using BalatroSeedOracle.Helpers;

namespace BalatroSeedOracle.Components.FilterTabs
{
    /// <summary>
    /// MVVM Visual Builder Tab - replaces drag/drop logic from original FiltersModal
    /// Minimal code-behind, all logic in VisualBuilderTabViewModel
    /// </summary>
    public partial class VisualBuilderTab : UserControl
    {
        public VisualBuilderTabViewModel? ViewModel => DataContext as VisualBuilderTabViewModel;

        public VisualBuilderTab()
        {
            InitializeComponent();
            
            // Set up ViewModel
            DataContext = new VisualBuilderTabViewModel();
            
            DebugLogger.Log("VisualBuilderTab", "MVVM Visual Builder Tab created");
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }
    }
}
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using BalatroSeedOracle.ViewModels;
using BalatroSeedOracle.Helpers;

namespace BalatroSeedOracle.Views.Modals
{
    /// <summary>
    /// CLEAN MVVM FiltersModal - demonstrates proper separation of concerns
    /// Minimal code-behind, all logic in ViewModel, proper data binding
    /// </summary>
    public partial class ComprehensiveFiltersModal : UserControl
    {
        public ComprehensiveFiltersModalViewModel? ViewModel => DataContext as ComprehensiveFiltersModalViewModel;

        public ComprehensiveFiltersModal()
        {
            InitializeComponent();
            
            // Set up MVVM ViewModel
            DataContext = ServiceHelper.GetRequiredService<ComprehensiveFiltersModalViewModel>();
            
            DebugLogger.Log("ComprehensiveFiltersModal", "Clean MVVM FiltersModal created - code-behind minimal!");
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }
    }
}
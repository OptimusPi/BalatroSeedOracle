using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using BalatroSeedOracle.ViewModels;
using BalatroSeedOracle.Helpers;

namespace BalatroSeedOracle.Views.Modals
{
    /// <summary>
    /// MODULAR FiltersModal using component composition
    /// Replaces 8000-line god class with clean MVVM tab components:
    /// - VisualBuilderTab: Drag/drop filter building
    /// - JsonEditorTab: JSON editing with syntax highlighting  
    /// - SaveFilterTab: Save/export functionality
    /// - LoadFilterTab: Filter loading/browsing
    /// 
    /// Each component has its own ViewModel and minimal code-behind
    /// </summary>
    public partial class ModularFiltersModal : UserControl
    {
        public FiltersModalViewModel? ViewModel => DataContext as FiltersModalViewModel;

        public ModularFiltersModal()
        {
            InitializeComponent();
            
            // Set up MVVM ViewModel
            DataContext = ServiceHelper.GetRequiredService<FiltersModalViewModel>();
            
            DebugLogger.Log("ModularFiltersModal", " Modular MVVM FiltersModal created!");
            DebugLogger.Log("ModularFiltersModal", "🎯 8000-line god class ELIMINATED!");
            DebugLogger.Log("ModularFiltersModal", "🏗️ Clean component architecture achieved!");
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }
    }
}
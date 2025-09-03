using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using BalatroSeedOracle.ViewModels.FilterTabs;
using BalatroSeedOracle.Helpers;

namespace BalatroSeedOracle.Components.FilterTabs
{
    /// <summary>
    /// MVVM Save Filter Tab - replaces save logic from original FiltersModal
    /// Minimal code-behind, all logic in SaveFilterTabViewModel
    /// </summary>
    public partial class SaveFilterTab : UserControl
    {
        public SaveFilterTabViewModel? ViewModel => DataContext as SaveFilterTabViewModel;

        public SaveFilterTab()
        {
            InitializeComponent();
            
            // Set up ViewModel
            DataContext = ServiceHelper.GetRequiredService<SaveFilterTabViewModel>();
            
            DebugLogger.Log("SaveFilterTab", "MVVM Save Filter Tab created");
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }
    }
}
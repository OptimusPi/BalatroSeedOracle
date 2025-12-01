using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using BalatroSeedOracle.Helpers;
using BalatroSeedOracle.ViewModels.FilterTabs;

namespace BalatroSeedOracle.Components.FilterTabs
{
    /// <summary>
    /// MVVM Validate Filter Tab - replaces confusing save interface with clear validation UI
    /// Uses row-based Expanders for clause display
    /// </summary>
    public partial class ValidateFilterTab : UserControl
    {
        public ValidateFilterTabViewModel? ViewModel => DataContext as ValidateFilterTabViewModel;

        public ValidateFilterTab()
        {
            InitializeComponent();
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }
    }
}

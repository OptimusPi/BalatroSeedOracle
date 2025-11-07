using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using BalatroSeedOracle.ViewModels.FilterTabs;

namespace BalatroSeedOracle.Components.Shared
{
    public partial class FilterCategoryNav : UserControl
    {
        public FilterCategoryNav()
        {
            InitializeComponent();
        }

        private void OnCategoryClick(object? sender, RoutedEventArgs e)
        {
            if (sender is not Button button) return;
            if (button.Tag is not string category) return;
            if (DataContext is not FilterTabViewModelBase viewModel) return;

            viewModel.SetCategory(category);
        }
    }
}

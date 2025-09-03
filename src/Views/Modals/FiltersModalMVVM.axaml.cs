using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using BalatroSeedOracle.ViewModels;
using BalatroSeedOracle.Helpers;

namespace BalatroSeedOracle.Views.Modals
{
    public partial class FiltersModalMVVM : UserControl
    {
        public FiltersModalViewModel? ViewModel => DataContext as FiltersModalViewModel;

        public FiltersModalMVVM()
        {
            InitializeComponent();
            
            // Set up MVVM ViewModel
            DataContext = ServiceHelper.GetRequiredService<FiltersModalViewModel>();
            
            DebugLogger.Log("FiltersModalMVVM", "Clean MVVM FiltersModal created");
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }
    }
}
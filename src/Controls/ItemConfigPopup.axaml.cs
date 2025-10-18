using Avalonia.Controls;
using Avalonia.Interactivity;
using BalatroSeedOracle.ViewModels;

namespace BalatroSeedOracle.Controls
{
    public partial class ItemConfigPopup : UserControl
    {
        public ItemConfigPopup()
        {
            InitializeComponent();
            Loaded += OnLoaded;
        }

        private void OnLoaded(object? sender, RoutedEventArgs e)
        {
            // Wire SourceSelector to ViewModel property for SelectedSource
            var vm = DataContext as ItemConfigPopupViewModel;
            var sourceSelector = this.FindControl<SourceSelector>("SourceSelector");
            if (vm != null && sourceSelector != null)
            {
                // Initialize selector from current VM value
                if (!string.IsNullOrEmpty(vm.SelectedSource))
                {
                    sourceSelector.SetSelectedSource(vm.SelectedSource);
                }

                // Keep VM in sync when user changes selection
                sourceSelector.SourceChanged += (s, selected) =>
                {
                    vm.SelectedSource = selected ?? "";
                };
            }
        }
    }
}

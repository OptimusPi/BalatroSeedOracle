using Avalonia.Controls;
using BalatroSeedOracle.Components;
using BalatroSeedOracle.ViewModels;

namespace BalatroSeedOracle.Views.SearchModalTabs
{
    public partial class SettingsTab : UserControl
    {
        private DeckAndStakeSelector? _deckStakeSelector;

        public SettingsTab()
        {
            InitializeComponent();
            Loaded += OnLoaded;
        }

        private void OnLoaded(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
        {
            _deckStakeSelector = this.FindControl<DeckAndStakeSelector>("DeckStakeSelector");

            if (_deckStakeSelector != null && DataContext is SearchModalViewModel vm)
            {
                _deckStakeSelector.DeckSelected += (s, _) =>
                {
                    // Save deck/stake selection to ViewModel
                    vm.DeckIndex = _deckStakeSelector.DeckIndex;
                    vm.StakeIndex = _deckStakeSelector.StakeIndex;

                    // Switch to next tab (Search tab)
                    vm.SelectedTabIndex = 1;
                };
            }
        }
    }
}

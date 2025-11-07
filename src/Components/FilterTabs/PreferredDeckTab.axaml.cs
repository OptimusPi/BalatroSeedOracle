using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using BalatroSeedOracle.ViewModels;

namespace BalatroSeedOracle.Components.FilterTabs
{
    public partial class PreferredDeckTab : UserControl
    {
        public PreferredDeckTab()
        {
            InitializeComponent();
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }

        protected override void OnLoaded(RoutedEventArgs e)
        {
            base.OnLoaded(e);

            // Wire up the DeckAndStakeSelector with FiltersModalViewModel's deck/stake properties
            var deckAndStakeSelector = this.FindControl<DeckAndStakeSelector>("DeckAndStakeSelector");
            if (deckAndStakeSelector != null && DataContext is FiltersModalViewModel filtersVm)
            {
                // Initialize selector with current values from FiltersModalViewModel
                deckAndStakeSelector.DeckIndex = filtersVm.SelectedDeckIndex;
                deckAndStakeSelector.StakeIndex = filtersVm.SelectedStakeIndex;

                // Subscribe to selection changes to update FiltersModalViewModel
                deckAndStakeSelector.SelectionChanged += (sender, selection) =>
                {
                    filtersVm.SelectedDeckIndex = selection.deckIndex;
                    filtersVm.SelectedStakeIndex = selection.stakeIndex;

                    // Trigger auto-save when deck/stake changes
                    filtersVm.TriggerAutoSave();
                };

                // The CONTINUE button in DeckAndStakeSelector can move to the next tab
                deckAndStakeSelector.DeckSelected += (sender, e) =>
                {
                    // Move to JSON Editor tab (index 2) when user clicks CONTINUE
                    filtersVm.SelectedTabIndex = 2;
                };
            }
        }
    }
}

using System.Linq;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Avalonia.VisualTree;
using BalatroSeedOracle.Components;
using BalatroSeedOracle.Controls;
using BalatroSeedOracle.ViewModels;

namespace BalatroSeedOracle.Components.FilterTabs
{
    public partial class DeckStakeTab : UserControl
    {
        private DeckAndStakeSelector? _deckAndStakeSelector;

        public DeckStakeTab()
        {
            InitializeComponent();
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);

            // Find the DeckAndStakeSelector in the visual tree
            _deckAndStakeSelector = this.FindControl<DeckAndStakeSelector>("DeckAndStakeSelector");
        }

        protected override void OnLoaded(RoutedEventArgs e)
        {
            base.OnLoaded(e);

            // Subscribe to events from the DeckAndStakeSelector
            if (_deckAndStakeSelector != null)
            {
                _deckAndStakeSelector.DeckSelected += OnDeckSelected;
                _deckAndStakeSelector.SelectionChanged += OnSelectionChanged;
            }
        }

        protected override void OnUnloaded(RoutedEventArgs e)
        {
            base.OnUnloaded(e);

            // Unsubscribe to prevent memory leaks
            if (_deckAndStakeSelector != null)
            {
                _deckAndStakeSelector.DeckSelected -= OnDeckSelected;
                _deckAndStakeSelector.SelectionChanged -= OnSelectionChanged;
            }
        }

        private void OnSelectionChanged(object? sender, (int deckIndex, int stakeIndex) selection)
        {
            // Update the parent ViewModel when selection changes
            if (DataContext is ViewModels.FilterTabs.DeckStakeTabViewModel vm)
            {
                vm.SelectedDeckIndex = selection.deckIndex;
                vm.SelectedStakeIndex = selection.stakeIndex;
            }
        }

        private void OnDeckSelected(object? sender, System.EventArgs e)
        {
            // Find the parent FiltersModal's ViewModel and advance to next tab
            var parent = this.GetVisualAncestors()
                .OfType<UserControl>()
                .FirstOrDefault(uc => uc.DataContext is FiltersModalViewModel);

            if (parent?.DataContext is FiltersModalViewModel vm)
            {
                // Advance to next tab (JSON Editor is index 2)
                if (vm.SelectedTabIndex < 3)
                {
                    vm.SelectedTabIndex = 2; // Move to JSON Editor tab
                }
            }
        }
    }
}

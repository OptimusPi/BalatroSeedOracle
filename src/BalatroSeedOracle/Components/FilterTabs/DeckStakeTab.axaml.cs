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
        private ViewModels.FilterTabs.DeckStakeTabViewModel? _subscribedViewModel;

        public DeckStakeTab()
        {
            InitializeComponent();
        }

        private void AttachSelectorAndViewModel()
        {
            _deckAndStakeSelector = DeckAndStakeSelector;

            if (_subscribedViewModel != null)
            {
                _subscribedViewModel.PropertyChanged -= OnViewModelPropertyChanged;
                _subscribedViewModel = null;
            }

            if (DataContext is ViewModels.FilterTabs.DeckStakeTabViewModel vm)
            {
                _subscribedViewModel = vm;
                _subscribedViewModel.PropertyChanged += OnViewModelPropertyChanged;
                if (_deckAndStakeSelector != null)
                {
                    _deckAndStakeSelector.DeckIndex = vm.SelectedDeckIndex;
                    _deckAndStakeSelector.StakeIndex = vm.SelectedStakeIndex;
                }
            }

            if (_deckAndStakeSelector != null)
            {
                _deckAndStakeSelector.DeckSelected -= OnDeckSelected;
                _deckAndStakeSelector.SelectionChanged -= OnSelectionChanged;

                _deckAndStakeSelector.DeckSelected += OnDeckSelected;
                _deckAndStakeSelector.SelectionChanged += OnSelectionChanged;
            }
        }

        private void DetachSelectorAndViewModel()
        {
            if (_subscribedViewModel != null)
            {
                _subscribedViewModel.PropertyChanged -= OnViewModelPropertyChanged;
                _subscribedViewModel = null;
            }

            if (_deckAndStakeSelector != null)
            {
                _deckAndStakeSelector.DeckSelected -= OnDeckSelected;
                _deckAndStakeSelector.SelectionChanged -= OnSelectionChanged;
            }
        }

        private void OnViewModelPropertyChanged(
            object? sender,
            System.ComponentModel.PropertyChangedEventArgs e
        )
        {
            if (
                _deckAndStakeSelector == null
                || DataContext is not ViewModels.FilterTabs.DeckStakeTabViewModel vm
            )
            {
                return;
            }

            if (
                e.PropertyName == nameof(ViewModels.FilterTabs.DeckStakeTabViewModel.SelectedDeckIndex)
                && _deckAndStakeSelector.DeckIndex != vm.SelectedDeckIndex
            )
            {
                _deckAndStakeSelector.DeckIndex = vm.SelectedDeckIndex;
            }
            else if (
                e.PropertyName == nameof(ViewModels.FilterTabs.DeckStakeTabViewModel.SelectedStakeIndex)
                && _deckAndStakeSelector.StakeIndex != vm.SelectedStakeIndex
            )
            {
                _deckAndStakeSelector.StakeIndex = vm.SelectedStakeIndex;
            }
        }

        protected override void OnAttachedToVisualTree(Avalonia.VisualTreeAttachmentEventArgs e)
        {
            base.OnAttachedToVisualTree(e);
            AttachSelectorAndViewModel();
        }

        protected override void OnDetachedFromVisualTree(Avalonia.VisualTreeAttachmentEventArgs e)
        {
            DetachSelectorAndViewModel();
            base.OnDetachedFromVisualTree(e);
        }

        protected override void OnLoaded(RoutedEventArgs e)
        {
            base.OnLoaded(e);
            AttachSelectorAndViewModel();
        }

        protected override void OnUnloaded(RoutedEventArgs e)
        {
            base.OnUnloaded(e);
            DetachSelectorAndViewModel();
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

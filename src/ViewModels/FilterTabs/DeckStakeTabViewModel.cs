using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;

namespace BalatroSeedOracle.ViewModels.FilterTabs
{
    /// <summary>
    /// ViewModel for the Deck/Stake selection tab in the Filter Builder Modal.
    /// Provides a dedicated interface for selecting deck and stake preferences.
    /// </summary>
    public partial class DeckStakeTabViewModel : ObservableObject
    {
        private readonly FiltersModalViewModel _parentViewModel;

        public DeckStakeTabViewModel(FiltersModalViewModel parentViewModel)
        {
            _parentViewModel = parentViewModel;
        }

        // Deck selection (0-14 index for 15 decks)
        public int SelectedDeckIndex
        {
            get => _parentViewModel.SelectedDeckIndex;
            set
            {
                if (_parentViewModel.SelectedDeckIndex != value)
                {
                    Helpers.DebugLogger.LogImportant("DeckStakeTab", $"🎯🎯🎯 DECK CHANGED: index {_parentViewModel.SelectedDeckIndex} → {value}");
                    _parentViewModel.SelectedDeckIndex = value;
                    Helpers.DebugLogger.LogImportant("DeckStakeTab", $"🎯🎯🎯 Parent SelectedDeck is now: {_parentViewModel.SelectedDeck}");
                    OnPropertyChanged(nameof(SelectedDeckIndex));
                }
            }
        }

        // Stake selection (0-7 index for 8 stakes)
        public int SelectedStakeIndex
        {
            get => _parentViewModel.SelectedStakeIndex;
            set
            {
                if (_parentViewModel.SelectedStakeIndex != value)
                {
                    Helpers.DebugLogger.LogImportant("DeckStakeTab", $"🎯🎯🎯 STAKE CHANGED: index {_parentViewModel.SelectedStakeIndex} → {value}");
                    _parentViewModel.SelectedStakeIndex = value;
                    Helpers.DebugLogger.LogImportant("DeckStakeTab", $"🎯🎯🎯 Parent SelectedStake is now: {_parentViewModel.SelectedStake}");
                    OnPropertyChanged(nameof(SelectedStakeIndex));
                }
            }
        }

        // Display values for deck spinner (Red, Blue, Yellow, etc.)
        public string[] DeckDisplayValues => _parentViewModel.DeckDisplayValues;

        // Display values for stake spinner (White, Red, Green, etc.)
        public string[] StakeDisplayValues => _parentViewModel.StakeDisplayValues;
    }
}

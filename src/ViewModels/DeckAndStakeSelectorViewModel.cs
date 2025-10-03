using System;
using System.Collections.Generic;
using System.Windows.Input;
using CommunityToolkit.Mvvm.Input;

namespace BalatroSeedOracle.ViewModels
{
    /// <summary>
    /// ViewModel for the DeckAndStakeSelector control.
    /// Manages deck and stake selection state with proper MVVM pattern.
    /// </summary>
    public class DeckAndStakeSelectorViewModel : BaseViewModel
    {
        private int _deckIndex;
        private int _stakeIndex;
        private string[] _stakeDisplayValues;

        public DeckAndStakeSelectorViewModel()
        {
            _stakeDisplayValues = new[]
            {
                "White Stake",
                "Red Stake",
                "Green Stake",
                "Black Stake",
                "Blue Stake",
                "Purple Stake",
                "Orange Stake",
                "Gold Stake"
            };

            SelectCommand = new RelayCommand(OnSelect);
        }

        #region Properties

        /// <summary>
        /// The selected deck index (0-14)
        /// </summary>
        public int DeckIndex
        {
            get => _deckIndex;
            set
            {
                if (SetProperty(ref _deckIndex, value))
                {
                    OnPropertyChanged(nameof(SelectedDeckName));
                    RaiseSelectionChanged();
                }
            }
        }

        /// <summary>
        /// The selected stake index (0-7)
        /// </summary>
        public int StakeIndex
        {
            get => _stakeIndex;
            set
            {
                if (SetProperty(ref _stakeIndex, value))
                {
                    OnPropertyChanged(nameof(SelectedStakeName));
                    RaiseSelectionChanged();
                }
            }
        }

        /// <summary>
        /// Display values for stake spinner
        /// </summary>
        public string[] StakeDisplayValues
        {
            get => _stakeDisplayValues;
            set => SetProperty(ref _stakeDisplayValues, value);
        }

        /// <summary>
        /// The name of the selected deck
        /// </summary>
        public string SelectedDeckName => GetDeckName(_deckIndex);

        /// <summary>
        /// The name of the selected stake
        /// </summary>
        public string SelectedStakeName => GetStakeName(_stakeIndex);

        #endregion

        #region Commands

        public ICommand SelectCommand { get; }

        #endregion

        #region Events

        /// <summary>
        /// Raised when the deck or stake selection changes
        /// </summary>
        public event EventHandler<(int deckIndex, int stakeIndex)>? SelectionChanged;

        /// <summary>
        /// Raised when the Select button is clicked
        /// </summary>
        public event EventHandler? DeckSelected;

        #endregion

        #region Private Methods

        private void OnSelect()
        {
            DeckSelected?.Invoke(this, EventArgs.Empty);
        }

        private void RaiseSelectionChanged()
        {
            SelectionChanged?.Invoke(this, (_deckIndex, _stakeIndex));
        }

        private string GetDeckName(int index)
        {
            return index switch
            {
                0 => "Red",
                1 => "Blue",
                2 => "Yellow",
                3 => "Green",
                4 => "Black",
                5 => "Magic",
                6 => "Nebula",
                7 => "Ghost",
                8 => "Abandoned",
                9 => "Checkered",
                10 => "Zodiac",
                11 => "Painted",
                12 => "Anaglyph",
                13 => "Plasma",
                14 => "Erratic",
                _ => "Red"
            };
        }

        private string GetStakeName(int index)
        {
            return index switch
            {
                0 => "White",
                1 => "Red",
                2 => "Green",
                3 => "Black",
                4 => "Blue",
                5 => "Purple",
                6 => "Orange",
                7 => "Gold",
                _ => "White"
            };
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Sets the deck by name
        /// </summary>
        public void SetDeck(string deckName)
        {
            int index = deckName?.ToLower() switch
            {
                "red" => 0,
                "blue" => 1,
                "yellow" => 2,
                "green" => 3,
                "black" => 4,
                "magic" => 5,
                "nebula" => 6,
                "ghost" => 7,
                "abandoned" => 8,
                "checkered" => 9,
                "zodiac" => 10,
                "painted" => 11,
                "anaglyph" => 12,
                "plasma" => 13,
                "erratic" => 14,
                _ => 0
            };
            DeckIndex = index;
        }

        /// <summary>
        /// Sets the stake by name
        /// </summary>
        public void SetStake(string stakeName)
        {
            int index = stakeName?.ToLower() switch
            {
                "white" => 0,
                "red" => 1,
                "green" => 2,
                "black" => 3,
                "blue" => 4,
                "purple" => 5,
                "orange" => 6,
                "gold" => 7,
                _ => 0
            };
            StakeIndex = index;
        }

        #endregion
    }
}

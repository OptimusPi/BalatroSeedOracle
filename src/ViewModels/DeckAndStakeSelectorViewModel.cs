using System;
using System.Collections.Generic;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace BalatroSeedOracle.ViewModels
{
    /// <summary>
    /// ViewModel for the DeckAndStakeSelector control.
    /// Manages deck and stake selection state with proper MVVM pattern.
    /// </summary>
    public partial class DeckAndStakeSelectorViewModel : ObservableObject
    {
        [ObservableProperty]
        private int _deckIndex;

        [ObservableProperty]
        private int _stakeIndex;

        [ObservableProperty]
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
        }

        #region Properties

        /// <summary>
        /// The name of the selected deck
        /// </summary>
        public string SelectedDeckName => GetDeckName(DeckIndex);

        /// <summary>
        /// The name of the selected stake
        /// </summary>
        public string SelectedStakeName => GetStakeName(StakeIndex);

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

        #region Property Changed Handlers

        partial void OnDeckIndexChanged(int value)
        {
            OnPropertyChanged(nameof(SelectedDeckName));
            RaiseSelectionChanged();
        }

        partial void OnStakeIndexChanged(int value)
        {
            OnPropertyChanged(nameof(SelectedStakeName));
            RaiseSelectionChanged();
        }

        #endregion

        #region Command Implementations

        [RelayCommand]
        private void Select()
        {
            DeckSelected?.Invoke(this, EventArgs.Empty);
        }

        #endregion

        #region Private Methods

        private void RaiseSelectionChanged()
        {
            SelectionChanged?.Invoke(this, (DeckIndex, StakeIndex));
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

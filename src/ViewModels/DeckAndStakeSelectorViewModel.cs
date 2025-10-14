using System;
using System.Collections.Generic;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using BalatroSeedOracle.Models;

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

        /// <summary>
        /// Display name for selected deck (e.g., "Red Deck")
        /// </summary>
        public string SelectedDeckDisplayName => $"{GetDeckName(DeckIndex)} Deck";

        /// <summary>
        /// Description of the selected deck
        /// </summary>
        public string SelectedDeckDescription
        {
            get
            {
                var deckName = GetDeckName(DeckIndex);
                return BalatroData.DeckDescriptions.TryGetValue(deckName, out var desc) ? desc : "";
            }
        }

        /// <summary>
        /// Display name for selected stake (e.g., "White Stake")
        /// </summary>
        public string SelectedStakeDisplayName => StakeDisplayValues[StakeIndex];

        /// <summary>
        /// Description of the selected stake
        /// </summary>
        public string SelectedStakeDescription
        {
            get
            {
                return StakeIndex switch
                {
                    0 => "Base Difficulty",
                    1 => "Small Blind gives no money reward",
                    2 => "Required score scales faster for each Ante",
                    3 => "Shop can have Eternal Jokers (Can't be sold or destroyed)",
                    4 => "-1 Discard",
                    5 => "Required score scales faster for each Ante",
                    6 => "Shop can have Perishable Jokers (Debuffed after 5 rounds)",
                    7 => "Shop can have Rental Jokers (Costs $3 per round)",
                    _ => "Base Difficulty"
                };
            }
        }

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
            OnPropertyChanged(nameof(SelectedDeckDisplayName));
            OnPropertyChanged(nameof(SelectedDeckDescription));
            RaiseSelectionChanged();
        }

        partial void OnStakeIndexChanged(int value)
        {
            OnPropertyChanged(nameof(SelectedStakeName));
            OnPropertyChanged(nameof(SelectedStakeDisplayName));
            OnPropertyChanged(nameof(SelectedStakeDescription));
            RaiseSelectionChanged();
        }

        #endregion

        #region Command Implementations

        [RelayCommand]
        private void Select()
        {
            DeckSelected?.Invoke(this, EventArgs.Empty);
        }

        // Navigate decks
        [RelayCommand]
        private void NextDeck()
        {
            var next = DeckIndex + 1;
            if (next > 14) next = 0; // wrap
            DeckIndex = next;
        }

        [RelayCommand]
        private void PreviousDeck()
        {
            var prev = DeckIndex - 1;
            if (prev < 0) prev = 14; // wrap
            DeckIndex = prev;
        }

        // Navigate stakes
        [RelayCommand]
        private void NextStake()
        {
            var next = StakeIndex + 1;
            if (next > 7) next = 0; // wrap
            StakeIndex = next;
        }

        [RelayCommand]
        private void PreviousStake()
        {
            var prev = StakeIndex - 1;
            if (prev < 0) prev = 7; // wrap
            StakeIndex = prev;
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

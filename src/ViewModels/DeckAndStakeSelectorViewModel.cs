using System;
using System.Collections.Generic;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using BalatroSeedOracle.Models;
using BalatroSeedOracle.Services;
using Avalonia.Media;
using Motely;

namespace BalatroSeedOracle.ViewModels
{
    /// <summary>
    /// ViewModel for the DeckAndStakeSelector control.
    /// Manages deck and stake selection state with proper MVVM pattern.
    /// </summary>
    public partial class DeckAndStakeSelectorViewModel : ObservableObject
    {
        private readonly SpriteService _spriteService;

        [ObservableProperty]
        private int _deckIndex;

        [ObservableProperty]
        private int _stakeIndex;

        [ObservableProperty]
        private string[] _stakeDisplayValues;

        [ObservableProperty]
        private IImage? _deckImage;

        [ObservableProperty]
        private IImage? _stakeImage;

        // Strongly-typed selections for deck and stake
        [ObservableProperty]
        private MotelyDeck _selectedDeck;

        [ObservableProperty]
        private MotelyStake _selectedStake;

        public DeckAndStakeSelectorViewModel(SpriteService spriteService)
        {
            _spriteService = spriteService;
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

            // Initialize enum selections to defaults
            SelectedDeck = MotelyDeck.Red;
            SelectedStake = MotelyStake.White;

            // Ensure images initialize
            UpdateDeckImage();
            UpdateStakeImage();
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
            // keep enum in sync
            SelectedDeck = GetDeckEnum(value);
            OnPropertyChanged(nameof(SelectedDeckName));
            OnPropertyChanged(nameof(SelectedDeckDisplayName));
            OnPropertyChanged(nameof(SelectedDeckDescription));
            UpdateDeckImage();
            RaiseSelectionChanged();
        }

        partial void OnStakeIndexChanged(int value)
        {
            // keep enum in sync
            SelectedStake = GetStakeEnum(value);
            OnPropertyChanged(nameof(SelectedStakeName));
            OnPropertyChanged(nameof(SelectedStakeDisplayName));
            OnPropertyChanged(nameof(SelectedStakeDescription));
            UpdateStakeImage();
            RaiseSelectionChanged();
        }

        // When enum selection changes directly, update indices
        partial void OnSelectedDeckChanged(MotelyDeck value)
        {
            var idx = GetDeckIndex(value);
            if (DeckIndex != idx)
            {
                DeckIndex = idx;
            }
            else
            {
                // refresh dependent state if index unchanged
                OnPropertyChanged(nameof(SelectedDeckName));
                OnPropertyChanged(nameof(SelectedDeckDisplayName));
                OnPropertyChanged(nameof(SelectedDeckDescription));
                UpdateDeckImage();
                RaiseSelectionChanged();
            }
        }

        partial void OnSelectedStakeChanged(MotelyStake value)
        {
            var idx = GetStakeIndex(value);
            if (StakeIndex != idx)
            {
                StakeIndex = idx;
            }
            else
            {
                // refresh dependent state if index unchanged
                OnPropertyChanged(nameof(SelectedStakeName));
                OnPropertyChanged(nameof(SelectedStakeDisplayName));
                OnPropertyChanged(nameof(SelectedStakeDescription));
                UpdateStakeImage();
                RaiseSelectionChanged();
            }
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

        private void UpdateDeckImage()
        {
            DeckImage = _spriteService.GetDeckImage(SelectedDeckName);
        }

        private void UpdateStakeImage()
        {
            StakeImage = _spriteService.GetStakeImage(SelectedStakeName);
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

        private MotelyDeck GetDeckEnum(int index)
        {
            return index switch
            {
                0 => MotelyDeck.Red,
                1 => MotelyDeck.Blue,
                2 => MotelyDeck.Yellow,
                3 => MotelyDeck.Green,
                4 => MotelyDeck.Black,
                5 => MotelyDeck.Magic,
                6 => MotelyDeck.Nebula,
                7 => MotelyDeck.Ghost,
                8 => MotelyDeck.Abandoned,
                9 => MotelyDeck.Checkered,
                10 => MotelyDeck.Zodiac,
                11 => MotelyDeck.Painted,
                12 => MotelyDeck.Anaglyph,
                13 => MotelyDeck.Plasma,
                14 => MotelyDeck.Erratic,
                _ => MotelyDeck.Red,
            };
        }

        private int GetDeckIndex(MotelyDeck deck)
        {
            return deck switch
            {
                MotelyDeck.Red => 0,
                MotelyDeck.Blue => 1,
                MotelyDeck.Yellow => 2,
                MotelyDeck.Green => 3,
                MotelyDeck.Black => 4,
                MotelyDeck.Magic => 5,
                MotelyDeck.Nebula => 6,
                MotelyDeck.Ghost => 7,
                MotelyDeck.Abandoned => 8,
                MotelyDeck.Checkered => 9,
                MotelyDeck.Zodiac => 10,
                MotelyDeck.Painted => 11,
                MotelyDeck.Anaglyph => 12,
                MotelyDeck.Plasma => 13,
                MotelyDeck.Erratic => 14,
                _ => 0,
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

        private MotelyStake GetStakeEnum(int index)
        {
            return index switch
            {
                0 => MotelyStake.White,
                1 => MotelyStake.Red,
                2 => MotelyStake.Green,
                3 => MotelyStake.Black,
                4 => MotelyStake.Blue,
                5 => MotelyStake.Purple,
                6 => MotelyStake.Orange,
                7 => MotelyStake.Gold,
                _ => MotelyStake.White,
            };
        }

        private int GetStakeIndex(MotelyStake stake)
        {
            return stake switch
            {
                MotelyStake.White => 0,
                MotelyStake.Red => 1,
                MotelyStake.Green => 2,
                MotelyStake.Black => 3,
                MotelyStake.Blue => 4,
                MotelyStake.Purple => 5,
                MotelyStake.Orange => 6,
                MotelyStake.Gold => 7,
                _ => 0,
            };
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Sets the deck by name
        /// </summary>
        public void SetDeck(string deckName)
        {
            SelectedDeck = (deckName?.ToLower()) switch
            {
                "red" => MotelyDeck.Red,
                "blue" => MotelyDeck.Blue,
                "yellow" => MotelyDeck.Yellow,
                "green" => MotelyDeck.Green,
                "black" => MotelyDeck.Black,
                "magic" => MotelyDeck.Magic,
                "nebula" => MotelyDeck.Nebula,
                "ghost" => MotelyDeck.Ghost,
                "abandoned" => MotelyDeck.Abandoned,
                "checkered" => MotelyDeck.Checkered,
                "zodiac" => MotelyDeck.Zodiac,
                "painted" => MotelyDeck.Painted,
                "anaglyph" => MotelyDeck.Anaglyph,
                "plasma" => MotelyDeck.Plasma,
                "erratic" => MotelyDeck.Erratic,
                _ => MotelyDeck.Red,
            };
        }

        /// <summary>
        /// Sets the stake by name
        /// </summary>
        public void SetStake(string stakeName)
        {
            SelectedStake = (stakeName?.ToLower()) switch
            {
                "white" => MotelyStake.White,
                "red" => MotelyStake.Red,
                "green" => MotelyStake.Green,
                "black" => MotelyStake.Black,
                "blue" => MotelyStake.Blue,
                "purple" => MotelyStake.Purple,
                "orange" => MotelyStake.Orange,
                "gold" => MotelyStake.Gold,
                _ => MotelyStake.White,
            };
        }

        #endregion
    }
}

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace BalatroSeedOracle.ViewModels
{
    /// <summary>
    /// ViewModel for the PlayingCardSelector control.
    /// Manages card selection state and provides commands for bulk selection operations.
    /// </summary>
    public partial class PlayingCardSelectorViewModel : ObservableObject
    {
        private readonly HashSet<string> _selectedCardKeys = new();

        [ObservableProperty]
        private string _selectionSummary = "0 cards selected";

        public PlayingCardSelectorViewModel()
        {
            InitializeCards();
        }

        #region Properties

        /// <summary>
        /// Collection of all playing cards organized by suit
        /// </summary>
        public ObservableCollection<CardSuitGroup> CardSuits { get; } = new();

        #endregion

        #region Events

        /// <summary>
        /// Raised when the selection changes. Provides list of selected card keys.
        /// </summary>
        public event EventHandler<PlayingCardSelectionEventArgs>? SelectionChanged;

        #endregion

        #region Initialization

        private void InitializeCards()
        {
            string[] ranks = { "A", "2", "3", "4", "5", "6", "7", "8", "9", "10", "J", "Q", "K" };

            // Create card groups for each suit
            CardSuits.Add(CreateSuitGroup("Spades", "♠", "#000000", ranks));
            CardSuits.Add(CreateSuitGroup("Hearts", "♥", "#FF0000", ranks));
            CardSuits.Add(CreateSuitGroup("Diamonds", "♦", "#FF0000", ranks));
            CardSuits.Add(CreateSuitGroup("Clubs", "♣", "#000000", ranks));
        }

        private CardSuitGroup CreateSuitGroup(
            string suit,
            string suitSymbol,
            string colorHex,
            string[] ranks
        )
        {
            var cards = new ObservableCollection<PlayingCardViewModel>();

            foreach (var rank in ranks)
            {
                var card = new PlayingCardViewModel(rank, suit, suitSymbol, colorHex);
                card.IsSelectedChanged += OnCardIsSelectedChanged;
                cards.Add(card);
            }

            return new CardSuitGroup(suit, suitSymbol, cards);
        }

        #endregion

        #region Command Implementations

        [RelayCommand]
        private void SelectAll()
        {
            foreach (var suitGroup in CardSuits)
            {
                foreach (var card in suitGroup.Cards)
                {
                    card.IsSelected = true;
                }
            }
        }

        [RelayCommand]
        private void ClearAll()
        {
            foreach (var suitGroup in CardSuits)
            {
                foreach (var card in suitGroup.Cards)
                {
                    card.IsSelected = false;
                }
            }
        }

        [RelayCommand]
        private void SelectSuit(string? suit)
        {
            if (string.IsNullOrEmpty(suit))
                return;

            var suitGroup = CardSuits.FirstOrDefault(g => g.Suit == suit);
            if (suitGroup != null)
            {
                foreach (var card in suitGroup.Cards)
                {
                    card.IsSelected = true;
                }
            }
        }

        [RelayCommand]
        private void SelectRank(string? rank)
        {
            if (string.IsNullOrEmpty(rank))
                return;

            foreach (var suitGroup in CardSuits)
            {
                var card = suitGroup.Cards.FirstOrDefault(c => c.Rank == rank);
                if (card != null)
                {
                    card.IsSelected = true;
                }
            }
        }

        [RelayCommand]
        private void SelectFaceCards()
        {
            string[] faceRanks = { "J", "Q", "K" };

            foreach (var suitGroup in CardSuits)
            {
                foreach (var card in suitGroup.Cards.Where(c => faceRanks.Contains(c.Rank)))
                {
                    card.IsSelected = true;
                }
            }
        }

        [RelayCommand]
        private void SelectNumberCards()
        {
            string[] numberRanks = { "2", "3", "4", "5", "6", "7", "8", "9", "10" };

            foreach (var suitGroup in CardSuits)
            {
                foreach (var card in suitGroup.Cards.Where(c => numberRanks.Contains(c.Rank)))
                {
                    card.IsSelected = true;
                }
            }
        }

        [RelayCommand]
        private void ToggleCard(PlayingCardViewModel? card)
        {
            if (card != null)
            {
                card.IsSelected = !card.IsSelected;
            }
        }

        #endregion

        #region Selection Management

        private void OnCardIsSelectedChanged(object? sender, EventArgs e)
        {
            if (sender is PlayingCardViewModel card)
            {
                var key = card.CardKey;

                if (card.IsSelected)
                {
                    _selectedCardKeys.Add(key);
                }
                else
                {
                    _selectedCardKeys.Remove(key);
                }

                UpdateSelectionSummary();
                RaiseSelectionChanged();
            }
        }

        private void UpdateSelectionSummary()
        {
            var count = _selectedCardKeys.Count;
            SelectionSummary = count == 1 ? "1 card selected" : $"{count} cards selected";
        }

        private void RaiseSelectionChanged()
        {
            SelectionChanged?.Invoke(
                this,
                new PlayingCardSelectionEventArgs(_selectedCardKeys.ToList())
            );
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Gets the list of currently selected card keys (format: "Rank_Suit")
        /// </summary>
        public List<string> GetSelectedCards()
        {
            return _selectedCardKeys.ToList();
        }

        /// <summary>
        /// Sets the selected cards from a list of card keys (format: "Rank_Suit")
        /// </summary>
        public void SetSelectedCards(List<string> cardKeys)
        {
            // Clear current selection
            ClearAll();

            // Select specified cards
            foreach (var key in cardKeys)
            {
                var parts = key.Split('_');
                if (parts.Length == 2)
                {
                    var rank = parts[0];
                    var suit = parts[1];

                    var suitGroup = CardSuits.FirstOrDefault(g => g.Suit == suit);
                    var card = suitGroup?.Cards.FirstOrDefault(c => c.Rank == rank);

                    if (card != null)
                    {
                        card.IsSelected = true;
                    }
                }
            }
        }

        #endregion
    }

    /// <summary>
    /// ViewModel for an individual playing card
    /// </summary>
    public partial class PlayingCardViewModel : ObservableObject
    {
        [ObservableProperty]
        private bool _isSelected;

        public PlayingCardViewModel(string rank, string suit, string suitSymbol, string colorHex)
        {
            Rank = rank;
            Suit = suit;
            SuitSymbol = suitSymbol;
            SuitColorHex = colorHex;
            CardKey = $"{rank}_{suit}";
        }

        public string Rank { get; }
        public string Suit { get; }
        public string SuitSymbol { get; }
        public string SuitColorHex { get; }
        public string CardKey { get; }

        partial void OnIsSelectedChanged(bool value)
        {
            IsSelectedChanged?.Invoke(this, EventArgs.Empty);
        }

        public event EventHandler? IsSelectedChanged;
    }

    /// <summary>
    /// Groups playing cards by suit
    /// </summary>
    public class CardSuitGroup
    {
        public CardSuitGroup(
            string suit,
            string suitSymbol,
            ObservableCollection<PlayingCardViewModel> cards
        )
        {
            Suit = suit;
            SuitSymbol = suitSymbol;
            Cards = cards;
        }

        public string Suit { get; }
        public string SuitSymbol { get; }
        public ObservableCollection<PlayingCardViewModel> Cards { get; }
    }

    /// <summary>
    /// Event args for selection changed events
    /// </summary>
    public class PlayingCardSelectionEventArgs : EventArgs
    {
        public PlayingCardSelectionEventArgs(List<string> selectedCards)
        {
            SelectedCards = selectedCards;
        }

        public List<string> SelectedCards { get; }
    }
}

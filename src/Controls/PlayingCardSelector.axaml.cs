using System;
using System.Collections.Generic;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using BalatroSeedOracle.ViewModels;

namespace BalatroSeedOracle.Controls
{
    /// <summary>
    /// User control for selecting playing cards from a standard 52-card deck.
    /// Uses MVVM pattern with PlayingCardSelectorViewModel for all state management and business logic.
    /// </summary>
    public partial class PlayingCardSelector : UserControl
    {
        private readonly PlayingCardSelectorViewModel _viewModel;

        public PlayingCardSelector()
        {
            _viewModel = new PlayingCardSelectorViewModel();
            DataContext = _viewModel;

            InitializeComponent();

            // Forward ViewModel events to control events for backward compatibility
            _viewModel.SelectionChanged += OnViewModelSelectionChanged;
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }

        #region Events

        /// <summary>
        /// Raised when the selection changes. Provides list of selected card keys.
        /// </summary>
        public event EventHandler<PlayingCardSelectionEventArgs>? SelectionChanged;

        #endregion

        #region Public Methods

        /// <summary>
        /// Gets the list of currently selected card keys (format: "Rank_Suit")
        /// </summary>
        public List<string> GetSelectedCards()
        {
            return _viewModel.GetSelectedCards();
        }

        /// <summary>
        /// Sets the selected cards from a list of card keys (format: "Rank_Suit")
        /// </summary>
        public void SetSelectedCards(List<string> cards)
        {
            _viewModel.SetSelectedCards(cards);
        }

        #endregion

        #region Private Methods

        private void OnViewModelSelectionChanged(object? sender, PlayingCardSelectionEventArgs e)
        {
            SelectionChanged?.Invoke(this, e);
        }

        #endregion
    }
}

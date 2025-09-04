using System;
using System.Collections.Generic;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using BalatroSeedOracle.Services;

namespace BalatroSeedOracle.Controls
{
    public partial class PlayingCardSelector : UserControl
    {
        private readonly Dictionary<string, PlayingCardTile> _cardTiles = new();
        private readonly HashSet<string> _selectedCards = new();
        
        public event EventHandler<PlayingCardSelectionEventArgs>? SelectionChanged;
        
        public PlayingCardSelector()
        {
            InitializeComponent();
            InitializeCards();
        }
        
        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
            
            // Get references
            var selectAllButton = this.FindControl<Button>("SelectAllButton");
            var clearAllButton = this.FindControl<Button>("ClearAllButton");
            var selectSpadesButton = this.FindControl<Button>("SelectSpadesButton");
            var selectHeartsButton = this.FindControl<Button>("SelectHeartsButton");
            var selectDiamondsButton = this.FindControl<Button>("SelectDiamondsButton");
            var selectClubsButton = this.FindControl<Button>("SelectClubsButton");
            var selectAcesButton = this.FindControl<Button>("SelectAcesButton");
            var selectFaceButton = this.FindControl<Button>("SelectFaceButton");
            var selectNumberButton = this.FindControl<Button>("SelectNumberButton");
            
            // Wire up events
            if (selectAllButton != null) selectAllButton.Click += (s, e) => SelectAll();
            if (clearAllButton != null) clearAllButton.Click += (s, e) => ClearAll();
            if (selectSpadesButton != null) selectSpadesButton.Click += (s, e) => SelectSuit("Spades");
            if (selectHeartsButton != null) selectHeartsButton.Click += (s, e) => SelectSuit("Hearts");
            if (selectDiamondsButton != null) selectDiamondsButton.Click += (s, e) => SelectSuit("Diamonds");
            if (selectClubsButton != null) selectClubsButton.Click += (s, e) => SelectSuit("Clubs");
            if (selectAcesButton != null) selectAcesButton.Click += (s, e) => SelectRank("A");
            if (selectFaceButton != null) selectFaceButton.Click += (s, e) => SelectFaceCards();
            if (selectNumberButton != null) selectNumberButton.Click += (s, e) => SelectNumberCards();
        }
        
        private void InitializeCards()
        {
            var spadesRow = this.FindControl<WrapPanel>("SpadesRow");
            var heartsRow = this.FindControl<WrapPanel>("HeartsRow");
            var diamondsRow = this.FindControl<WrapPanel>("DiamondsRow");
            var clubsRow = this.FindControl<WrapPanel>("ClubsRow");
            
            if (spadesRow == null || heartsRow == null || diamondsRow == null || clubsRow == null)
                return;
            
            string[] ranks = { "A", "2", "3", "4", "5", "6", "7", "8", "9", "10", "J", "Q", "K" };
            
            // Create spades
            foreach (var rank in ranks)
            {
                var tile = CreateCardTile(rank, "Spades", "♠", Brushes.Black);
                spadesRow.Children.Add(tile);
            }
            
            // Create hearts
            foreach (var rank in ranks)
            {
                var tile = CreateCardTile(rank, "Hearts", "♥", Brushes.Red);
                heartsRow.Children.Add(tile);
            }
            
            // Create diamonds
            foreach (var rank in ranks)
            {
                var tile = CreateCardTile(rank, "Diamonds", "♦", Brushes.Red);
                diamondsRow.Children.Add(tile);
            }
            
            // Create clubs
            foreach (var rank in ranks)
            {
                var tile = CreateCardTile(rank, "Clubs", "♣", Brushes.Black);
                clubsRow.Children.Add(tile);
            }
            
            UpdateSelectionSummary();
        }
        
        private PlayingCardTile CreateCardTile(string rank, string suit, string suitSymbol, IBrush suitColor)
        {
            var key = $"{rank}_{suit}";
            var tile = new PlayingCardTile(rank, suit, suitSymbol, suitColor);
            tile.Click += OnCardTileClick;
            _cardTiles[key] = tile;
            return tile;
        }
        
        private void OnCardTileClick(object? sender, RoutedEventArgs e)
        {
            if (sender is PlayingCardTile tile)
            {
                var key = $"{tile.Rank}_{tile.Suit}";
                
                if (_selectedCards.Contains(key))
                {
                    _selectedCards.Remove(key);
                    tile.IsSelected = false;
                }
                else
                {
                    _selectedCards.Add(key);
                    tile.IsSelected = true;
                }
                
                UpdateSelectionSummary();
                SelectionChanged?.Invoke(this, new PlayingCardSelectionEventArgs(_selectedCards.ToList()));
            }
        }
        
        private void SelectAll()
        {
            foreach (var kvp in _cardTiles)
            {
                _selectedCards.Add(kvp.Key);
                kvp.Value.IsSelected = true;
            }
            UpdateSelectionSummary();
            SelectionChanged?.Invoke(this, new PlayingCardSelectionEventArgs(_selectedCards.ToList()));
        }
        
        private void ClearAll()
        {
            _selectedCards.Clear();
            foreach (var tile in _cardTiles.Values)
            {
                tile.IsSelected = false;
            }
            UpdateSelectionSummary();
            SelectionChanged?.Invoke(this, new PlayingCardSelectionEventArgs(_selectedCards.ToList()));
        }
        
        private void SelectSuit(string suit)
        {
            foreach (var kvp in _cardTiles)
            {
                if (kvp.Value.Suit == suit)
                {
                    _selectedCards.Add(kvp.Key);
                    kvp.Value.IsSelected = true;
                }
            }
            UpdateSelectionSummary();
            SelectionChanged?.Invoke(this, new PlayingCardSelectionEventArgs(_selectedCards.ToList()));
        }
        
        private void SelectRank(string rank)
        {
            foreach (var kvp in _cardTiles)
            {
                if (kvp.Value.Rank == rank)
                {
                    _selectedCards.Add(kvp.Key);
                    kvp.Value.IsSelected = true;
                }
            }
            UpdateSelectionSummary();
            SelectionChanged?.Invoke(this, new PlayingCardSelectionEventArgs(_selectedCards.ToList()));
        }
        
        private void SelectFaceCards()
        {
            string[] faceRanks = { "J", "Q", "K" };
            foreach (var kvp in _cardTiles)
            {
                if (faceRanks.Contains(kvp.Value.Rank))
                {
                    _selectedCards.Add(kvp.Key);
                    kvp.Value.IsSelected = true;
                }
            }
            UpdateSelectionSummary();
            SelectionChanged?.Invoke(this, new PlayingCardSelectionEventArgs(_selectedCards.ToList()));
        }
        
        private void SelectNumberCards()
        {
            string[] numberRanks = { "2", "3", "4", "5", "6", "7", "8", "9", "10" };
            foreach (var kvp in _cardTiles)
            {
                if (numberRanks.Contains(kvp.Value.Rank))
                {
                    _selectedCards.Add(kvp.Key);
                    kvp.Value.IsSelected = true;
                }
            }
            UpdateSelectionSummary();
            SelectionChanged?.Invoke(this, new PlayingCardSelectionEventArgs(_selectedCards.ToList()));
        }
        
        private void UpdateSelectionSummary()
        {
            var summary = this.FindControl<TextBlock>("SelectionSummary");
            if (summary != null)
            {
                var count = _selectedCards.Count;
                summary.Text = count == 1 ? "1 card selected" : $"{count} cards selected";
            }
        }
        
        public List<string> GetSelectedCards()
        {
            return _selectedCards.ToList();
        }
        
        public void SetSelectedCards(List<string> cards)
        {
            ClearAll();
            foreach (var card in cards)
            {
                if (_cardTiles.ContainsKey(card))
                {
                    _selectedCards.Add(card);
                    _cardTiles[card].IsSelected = true;
                }
            }
            UpdateSelectionSummary();
        }
    }
    
    public class PlayingCardSelectionEventArgs : EventArgs
    {
        public List<string> SelectedCards { get; }
        
        public PlayingCardSelectionEventArgs(List<string> selectedCards)
        {
            SelectedCards = selectedCards;
        }
    }
    
    public class PlayingCardTile : Button
    {
        public string Rank { get; }
        public string Suit { get; }
        
        private bool _isSelected;
        public bool IsSelected
        {
            get => _isSelected;
            set
            {
                _isSelected = value;
                UpdateVisualState();
            }
        }
        
        public PlayingCardTile(string rank, string suit, string suitSymbol, IBrush suitColor)
        {
            Rank = rank;
            Suit = suit;
            
            Width = 60;
            Height = 84;
            Margin = new Thickness(3);
            Background = Brushes.White;
            BorderBrush = Application.Current?.FindResource("DarkerGrey") as IBrush ?? Brushes.Gray;
            BorderThickness = new Thickness(2);
            CornerRadius = new CornerRadius(4);
            Cursor = new Cursor(StandardCursorType.Hand);
            
            // Create card content
            var grid = new Grid();
            grid.RowDefinitions = new RowDefinitions("*,*");
            
            var rankText = new TextBlock
            {
                Text = rank,
                FontSize = 18,
                Foreground = suitColor,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            };
            Grid.SetRow(rankText, 0);
            grid.Children.Add(rankText);
            
            var suitText = new TextBlock
            {
                Text = suitSymbol,
                FontSize = 24,
                Foreground = suitColor,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            };
            Grid.SetRow(suitText, 1);
            grid.Children.Add(suitText);
            
            Content = grid;
            
            UpdateVisualState();
        }
        
        private void UpdateVisualState()
        {
            if (_isSelected)
            {
                BorderBrush = Application.Current?.FindResource("AccentBlue") as IBrush ?? Brushes.Blue;
                BorderThickness = new Thickness(3);
                Background = new SolidColorBrush(Color.Parse("#E6F3FF"));
            }
            else
            {
                BorderBrush = Application.Current?.FindResource("DarkerGrey") as IBrush ?? Brushes.Gray;
                BorderThickness = new Thickness(2);
                Background = Brushes.White;
            }
        }
        
        protected override void OnPointerEntered(PointerEventArgs e)
        {
            base.OnPointerEntered(e);
            if (!_isSelected)
            {
                Background = new SolidColorBrush(Color.Parse("#F0F0F0"));
            }
        }
        
        protected override void OnPointerExited(PointerEventArgs e)
        {
            base.OnPointerExited(e);
            UpdateVisualState();
        }
    }
}
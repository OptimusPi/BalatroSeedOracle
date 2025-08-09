using System;
using System.Collections.Generic;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Markup.Xaml;
using Oracle.Services;
using Oracle.Controls;
using Oracle.Helpers;

namespace Oracle.Components
{
    public partial class DeckStakeSelector : UserControl
    {
        private readonly SpriteService _spriteService;
        
        // UI Elements
        private Image? _deckPreviewImage;
        private Image? _stakeChipOverlay;
        private TextBlock? _deckNameText;
        private TextBlock? _deckDescText;
        private Button? _deckLeftArrow;
        private Button? _deckRightArrow;
        private SpinnerControl? _stakeSpinner;
        
        private int _currentDeckIndex = 0;
        private int _currentStakeIndex = 0;
        
        // Properties
        public static readonly StyledProperty<bool> UseSpinnerForStakeProperty = 
            AvaloniaProperty.Register<DeckStakeSelector, bool>(nameof(UseSpinnerForStake), false);
        
        public bool UseSpinnerForStake
        {
            get => GetValue(UseSpinnerForStakeProperty);
            set => SetValue(UseSpinnerForStakeProperty, value);
        }
        
        // Events
        public event EventHandler<DeckChangedEventArgs>? DeckChanged;
        public event EventHandler<StakeChangedEventArgs>? StakeChanged;
        
        // Deck data
        private readonly List<(string name, string description, string spriteName)> _decks = new()
        {
            ("Red", "+1 Discard\nevery round", "red"),
            ("Blue", "+1 Hand\nevery round", "blue"),
            ("Yellow", "+$10 at\nstart of run", "yellow"),
            ("Green", "At end of each Round:\n+$1 interest per $5\n(max $5 interest)", "green"),
            ("Black", "+1 Joker slot\n-1 Hand every round", "black"),
            ("Magic", "Start run with the\nCrystal Ball voucher\nand 2 copies of The Fool", "magic"),
            ("Nebula", "Start run with the\nTelescope voucher\n-1 consumable slot", "nebula"),
            ("Ghost", "Spectral cards may\nappear in the shop\nstart with a Hex", "ghost"),
            ("Abandoned", "Start with no\nFace Cards\nin your deck", "abandoned"),
            ("Checkered", "Start with 26 Spades\nand 26 Hearts\nin deck", "checkered"),
            ("Zodiac", "Start run with\nTarot Merchant,\nPlanet Merchant,\nand Overstock", "zodiac"),
            ("Painted", "+2 Hand Size\n-1 Joker Slot", "painted"),
            ("Anaglyph", "After defeating each\nBoss Blind, gain a\nDouble Tag", "anaglyph"),
            ("Plasma", "Balance Chips and\nMult when calculating\nscore for played hand", "plasma"),
            ("Erratic", "All Ranks and Suits\nin deck are randomized", "erratic")
        };
        
        // Stake data
        private readonly List<(string name, string description, string spriteName)> _stakes = new()
        {
            ("White Stake", "Base Difficulty", "white"),
            ("Red Stake", "Small Blind gives\nno reward money", "red"),
            ("Green Stake", "Required score scales\nfaster for each Ante", "green"),
            ("Black Stake", "Shop can have\nEternal Jokers\n(Can't be sold or destroyed)", "black"),
            ("Blue Stake", "-1 Discard", "blue"),
            ("Purple Stake", "Required score scales\nfaster for each Ante", "purple"),
            ("Orange Stake", "Shop can have\nPerishable Jokers\n(Debuffed after 5 Rounds)", "orange"),
            ("Gold Stake", "Shop can have\nRental Jokers\n(Costs $1 per round)", "gold")
        };
        
        public DeckStakeSelector()
        {
            InitializeComponent();
            _spriteService = ServiceHelper.GetRequiredService<SpriteService>();
        }
        
        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
            
            // Find UI elements
            _deckPreviewImage = this.FindControl<Image>("DeckPreviewImage");
            _stakeChipOverlay = this.FindControl<Image>("StakeChipOverlay");
            _deckNameText = this.FindControl<TextBlock>("DeckNameText");
            _deckDescText = this.FindControl<TextBlock>("DeckDescText");
            _deckLeftArrow = this.FindControl<Button>("DeckLeftArrow");
            _deckRightArrow = this.FindControl<Button>("DeckRightArrow");
            _stakeSpinner = this.FindControl<SpinnerControl>("StakeSpinner");
            
            Oracle.Helpers.DebugLogger.Log("DeckStakeSelector", $"Controls found - DeckImage: {_deckPreviewImage != null}, StakeOverlay: {_stakeChipOverlay != null}, Spinner: {_stakeSpinner != null}");
            
            // Wire up event handlers
            if (_deckLeftArrow != null)
                _deckLeftArrow.Click += (s, e) => NavigateDeck(-1);
            if (_deckRightArrow != null)
                _deckRightArrow.Click += (s, e) => NavigateDeck(1);
            
            if (_stakeSpinner != null)
            {
                _stakeSpinner.ValueChanged += (s, e) => 
                {
                    _currentStakeIndex = (int)_stakeSpinner.Value;
                    Oracle.Helpers.DebugLogger.Log("DeckStakeSelector", $"Stake spinner changed to index: {_currentStakeIndex}");
                    UpdateStakeDisplay();
                };
            }
        }
        
        protected override void OnLoaded(Avalonia.Interactivity.RoutedEventArgs e)
        {
            base.OnLoaded(e);
            
            // Initialize display after control is loaded
            UpdateDeckDisplay();
            UpdateStakeDisplay();
        }
        
        
        private void NavigateDeck(int direction)
        {
            _currentDeckIndex = (_currentDeckIndex + direction + _decks.Count) % _decks.Count;
            UpdateDeckDisplay();
            
            var deck = _decks[_currentDeckIndex];
            DeckChanged?.Invoke(this, new DeckChangedEventArgs 
            { 
                DeckIndex = _currentDeckIndex,
                DeckName = deck.name,
                DeckDescription = deck.description
            });
        }
        
        
        private void UpdateDeckDisplay()
        {
            if (_deckNameText == null || _deckDescText == null || _deckPreviewImage == null || _spriteService == null)
                return;
            
            // Safety check for list bounds
            if (_decks == null || _decks.Count == 0 || _currentDeckIndex < 0 || _currentDeckIndex >= _decks.Count)
                return;
                
            var deck = _decks[_currentDeckIndex];
            _deckNameText.Text = deck.name + " Deck";
            _deckDescText.Text = deck.description;
            
            // Update the deck image with stake sticker
            UpdateDeckImageWithStake();
        }
        
        private void UpdateStakeDisplay()
        {
            // Update the deck image with new stake
            UpdateDeckImageWithStake();
            
            // Fire stake changed event
            if (_currentStakeIndex >= 0 && _currentStakeIndex < _stakes.Count)
            {
                var stake = _stakes[_currentStakeIndex];
                StakeChanged?.Invoke(this, new StakeChangedEventArgs 
                { 
                    StakeIndex = _currentStakeIndex,
                    StakeName = stake.name.Replace(" Stake", ""),
                    StakeDescription = stake.description
                });
            }
        }
        
        private void UpdateDeckImageWithStake()
        {
            if (_deckPreviewImage == null || _spriteService == null)
                return;
                
            if (_currentDeckIndex < 0 || _currentDeckIndex >= _decks.Count)
                return;
                
            if (_currentStakeIndex < 0 || _currentStakeIndex >= _stakes.Count)
                return;
                
            var deck = _decks[_currentDeckIndex];
            var stake = _stakes[_currentStakeIndex];
            
            try
            {
                // Get composite image with deck and stake sticker
                var compositeImage = _spriteService.GetDeckWithStakeSticker(deck.spriteName, stake.spriteName);
                if (compositeImage != null)
                {
                    _deckPreviewImage.Source = compositeImage;
                    Oracle.Helpers.DebugLogger.Log("DeckStakeSelector", $"✅ Deck with stake sticker set: {deck.spriteName} + {stake.spriteName}");
                }
                else
                {
                    // Fallback to just deck image
                    var deckImage = _spriteService.GetDeckImage(deck.spriteName);
                    if (deckImage != null)
                    {
                        _deckPreviewImage.Source = deckImage;
                    }
                }
                
                // Hide the overlay since we're using a composite image now
                if (_stakeChipOverlay != null)
                {
                    _stakeChipOverlay.IsVisible = false;
                }
            }
            catch (Exception ex)
            {
                Oracle.Helpers.DebugLogger.LogError("DeckStakeSelector", $"Failed to update deck image: {ex.Message}");
            }
        }
        
        // Public methods to get/set values
        public string GetSelectedDeck()
        {
            return _decks[_currentDeckIndex].name;
        }
        
        public string GetSelectedStake()
        {
            return _stakes[_currentStakeIndex].name.Replace(" Stake", "");
        }
        
        public int GetDeckIndex()
        {
            return _currentDeckIndex;
        }
        
        public int GetStakeIndex()
        {
            return _currentStakeIndex;
        }
        
        public void SetDeckIndex(int index)
        {
            if (index >= 0 && index < _decks.Count)
            {
                _currentDeckIndex = index;
                UpdateDeckDisplay();
            }
        }
        
        public void SetStakeIndex(int index)
        {
            if (index >= 0 && index < _stakes.Count)
            {
                _currentStakeIndex = index;
                Oracle.Helpers.DebugLogger.Log("DeckStakeSelector", $"SetStakeIndex called with index: {index}");
                
                // Update the spinner value if it exists
                if (_stakeSpinner != null)
                {
                    _stakeSpinner.Value = index;
                }
                
                UpdateStakeDisplay();
            }
        }
    }
    
    // Event args classes
    public class DeckChangedEventArgs : EventArgs
    {
        public int DeckIndex { get; set; }
        public string DeckName { get; set; } = string.Empty;
        public string DeckDescription { get; set; } = string.Empty;
    }
    
    public class StakeChangedEventArgs : EventArgs
    {
        public int StakeIndex { get; set; }
        public string StakeName { get; set; } = string.Empty;
        public string StakeDescription { get; set; } = string.Empty;
    }
}
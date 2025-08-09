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
        private TextBlock? _stakeNameText;
        private TextBlock? _stakeDescText;
        private Canvas? _stakeChipsCanvas;
        private Button? _deckLeftArrow;
        private Button? _deckRightArrow;
        private Button? _stakeLeftArrow;
        private Button? _stakeRightArrow;
        private SpinnerControl? _stakeSpinner;
        private Grid? _arrowNavigation;
        
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
            _stakeNameText = this.FindControl<TextBlock>("StakeNameText");
            _stakeDescText = this.FindControl<TextBlock>("StakeDescText");
            _stakeChipsCanvas = this.FindControl<Canvas>("StakeChipsCanvas");
            _deckLeftArrow = this.FindControl<Button>("DeckLeftArrow");
            _deckRightArrow = this.FindControl<Button>("DeckRightArrow");
            _stakeLeftArrow = this.FindControl<Button>("StakeLeftArrow");
            _stakeRightArrow = this.FindControl<Button>("StakeRightArrow");
            _stakeSpinner = this.FindControl<SpinnerControl>("StakeSpinner");
            _arrowNavigation = this.FindControl<Grid>("ArrowNavigation");
            
            // Wire up event handlers
            if (_deckLeftArrow != null)
                _deckLeftArrow.Click += (s, e) => NavigateDeck(-1);
            if (_deckRightArrow != null)
                _deckRightArrow.Click += (s, e) => NavigateDeck(1);
            if (_stakeLeftArrow != null)
                _stakeLeftArrow.Click += (s, e) => NavigateStake(-1);
            if (_stakeRightArrow != null)
                _stakeRightArrow.Click += (s, e) => NavigateStake(1);
            
            if (_stakeSpinner != null)
            {
                _stakeSpinner.ValueChanged += (s, e) => 
                {
                    _currentStakeIndex = (int)_stakeSpinner.Value;
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
        
        protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
        {
            base.OnPropertyChanged(change);
            
            if (change.Property == UseSpinnerForStakeProperty)
            {
                UpdateStakeNavigationMode();
            }
        }
        
        private void UpdateStakeNavigationMode()
        {
            if (_arrowNavigation != null && _stakeSpinner != null)
            {
                _arrowNavigation.IsVisible = !UseSpinnerForStake;
                _stakeSpinner.IsVisible = UseSpinnerForStake;
            }
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
        
        private void NavigateStake(int direction)
        {
            _currentStakeIndex = (_currentStakeIndex + direction + _stakes.Count) % _stakes.Count;
            UpdateStakeDisplay();
            
            var stake = _stakes[_currentStakeIndex];
            StakeChanged?.Invoke(this, new StakeChangedEventArgs 
            { 
                StakeIndex = _currentStakeIndex,
                StakeName = stake.name.Replace(" Stake", ""),
                StakeDescription = stake.description
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
            
            // Get deck image from sprite service
            try
            {
                var deckImage = _spriteService.GetDeckImage(deck.spriteName);
                if (deckImage != null)
                {
                    _deckPreviewImage.Source = deckImage;
                }
            }
            catch (Exception ex)
            {
                Oracle.Helpers.DebugLogger.LogError("DeckStakeSelector", $"Failed to load deck image: {ex.Message}");
            }
            
            // Update stake overlay
            UpdateStakeOverlay();
        }
        
        private void UpdateStakeDisplay()
        {
            if (_stakeChipsCanvas == null || _spriteService == null)
                return;
            
            // Safety check for list bounds
            if (_stakes == null || _stakes.Count == 0 || _currentStakeIndex < 0 || _currentStakeIndex >= _stakes.Count)
                return;
                
            var stake = _stakes[_currentStakeIndex];
            
            // Update stake text
            if (_stakeNameText != null)
                _stakeNameText.Text = stake.name;
            if (_stakeDescText != null)
                _stakeDescText.Text = stake.description;
            
            // Clear and update stake chips display
            _stakeChipsCanvas.Children.Clear();
            
            try
            {
                // Get stake chip image
                var chipImage = _spriteService.GetStakeChipImage(stake.spriteName);
                
                if (chipImage != null)
                {
                    // Create stacked chips effect
                    for (int i = 0; i <= _currentStakeIndex; i++)
                    {
                        var chip = new Image
                        {
                            Source = chipImage,
                            Width = 29,
                            Height = 29,
                            Stretch = Stretch.Uniform
                        };
                        
                        Canvas.SetLeft(chip, 25 + (i % 4) * 6);
                        Canvas.SetTop(chip, 25 - (i / 4) * 6);
                        chip.ZIndex = i;
                        
                        _stakeChipsCanvas.Children.Add(chip);
                    }
                }
            }
            catch (Exception ex)
            {
                Oracle.Helpers.DebugLogger.LogError("DeckStakeSelector", $"Failed to load stake chip image: {ex.Message}");
            }
            
            // Update spinner value if using spinner
            if (_stakeSpinner != null && UseSpinnerForStake)
            {
                _stakeSpinner.Value = _currentStakeIndex;
            }
            
            // Update stake overlay on deck
            UpdateStakeOverlay();
        }
        
        private void UpdateStakeOverlay()
        {
            if (_stakeChipOverlay != null && _currentStakeIndex > 0 && _spriteService != null)
            {
                try
                {
                    var stake = _stakes[_currentStakeIndex];
                    var overlaySprite = _spriteService.GetStakeChipImage(stake.spriteName);
                    _stakeChipOverlay.Source = overlaySprite;
                    _stakeChipOverlay.IsVisible = true;
                }
                catch (Exception ex)
                {
                    Oracle.Helpers.DebugLogger.LogError("DeckStakeSelector", $"Failed to load stake overlay: {ex.Message}");
                    _stakeChipOverlay.IsVisible = false;
                }
            }
            else if (_stakeChipOverlay != null)
            {
                _stakeChipOverlay.IsVisible = false;
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
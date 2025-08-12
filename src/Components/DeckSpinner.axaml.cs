using System;
using System.Collections.Generic;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using BalatroSeedOracle.Controls;
using BalatroSeedOracle.Helpers;
using BalatroSeedOracle.Services;

namespace BalatroSeedOracle.Components
{
    public partial class DeckSpinner : UserControl
    {
        private PanelSpinner? _panelSpinner;
        private readonly SpriteService _spriteService;

        // Deck data - same as DeckStakeSelector
        private readonly List<(string name, string description, string spriteName)> _decks = new()
        {
            ("Red", "+1 Discard every round", "red"),
            ("Blue", "+1 Hand every round", "blue"),
            ("Yellow", "+$10 at start of run", "yellow"),
            ("Green", "At end of each Round: +$1 interest per $5 (max $5 interest)", "green"),
            ("Black", "+1 Joker slot -1 Hand every round", "black"),
            ("Magic", "Start run with the Crystal Ball voucher and 2 copies of The Fool", "magic"),
            ("Nebula", "Start run with the Telescope voucher -1 consumable slot", "nebula"),
            ("Ghost", "Spectral cards may appear in the shop start with a Hex", "ghost"),
            ("Abandoned", "Start with no Face Cards in your deck", "abandoned"),
            ("Checkered", "Start with 26 Spades and 26 Hearts in deck", "checkered"),
            ("Zodiac", "Start run with Tarot Merchant, Planet Merchant, and Overstock", "zodiac"),
            ("Painted", "+2 Hand Size -1 Joker Slot", "painted"),
            ("Anaglyph", "After defeating each Boss Blind, gain a Double Tag", "anaglyph"),
            ("Plasma", "Balance Chips and Mult when calculating score for played hand", "plasma"),
            ("Erratic", "All Ranks and Suits in deck are randomized", "erratic"),
        };

        public event EventHandler<int>? DeckChanged;

        private int _currentStakeIndex = 0;

        public DeckSpinner()
        {
            InitializeComponent();
            _spriteService = ServiceHelper.GetRequiredService<SpriteService>();
        }

        protected override void OnLoaded(RoutedEventArgs e)
        {
            base.OnLoaded(e);

            _panelSpinner = this.FindControl<PanelSpinner>("InnerPanelSpinner");
            if (_panelSpinner != null)
            {
                // Create panel items from deck data
                var items = _decks
                    .Select(
                        (deck, index) =>
                            new PanelItem
                            {
                                Title = deck.name + " Deck",
                                Description = deck.description,
                                Value = deck.spriteName,
                                GetImage = () => GetDeckImageWithStake(deck.spriteName),
                            }
                    )
                    .ToList();

                _panelSpinner.Items = items;
                _panelSpinner.SelectionChanged += OnDeckSelectionChanged;
            }
        }

        private void OnDeckSelectionChanged(object? sender, PanelItem? item)
        {
            if (_panelSpinner != null)
            {
                DeckChanged?.Invoke(this, _panelSpinner.SelectedIndex);
            }
        }

        public int SelectedDeckIndex
        {
            get => _panelSpinner?.SelectedIndex ?? 0;
            set
            {
                if (_panelSpinner != null)
                {
                    _panelSpinner.SelectedIndex = value;
                }
            }
        }

        public string SelectedDeckName => _decks[SelectedDeckIndex].name;
        public string SelectedDeckSpriteName => _decks[SelectedDeckIndex].spriteName;

        public void SetStakeIndex(int stakeIndex)
        {
            _currentStakeIndex = stakeIndex;
            // Refresh the current deck image to show with new stake
            if (_panelSpinner != null)
            {
                _panelSpinner.RefreshCurrentImage();
            }
        }

        private IImage? GetDeckImageWithStake(string deckSpriteName)
        {
            string stakeName = GetStakeName(_currentStakeIndex);
            var compositeImage = _spriteService.GetDeckWithStakeSticker(deckSpriteName, stakeName);
            return compositeImage ?? _spriteService.GetDeckImage(deckSpriteName);
        }

        private string GetStakeName(int index)
        {
            return index switch
            {
                0 => "white",
                1 => "red",
                2 => "green",
                3 => "black",
                4 => "blue",
                5 => "purple",
                6 => "orange",
                7 => "gold",
                _ => "white",
            };
        }
    }
}

using Avalonia.Controls;
using Avalonia.Media.Imaging;
using BalatroSeedOracle.Components;
using BalatroSeedOracle.Models;
using BalatroSeedOracle.Services;
using BalatroSeedOracle.ViewModels;

namespace BalatroSeedOracle.Views.SearchModalTabs
{
    public partial class SettingsTab : UserControl
    {
        private Image? _deckImage;
        private Image? _stakeOverlayImage;
        private TextBlock? _deckDescriptionText;

        public SettingsTab()
        {
            InitializeComponent();
            DataContextChanged += OnDataContextChanged;
        }

        private void OnDataContextChanged(object? sender, System.EventArgs e)
        {
            if (DataContext is SearchModalViewModel vm)
            {
                // Find controls
                _deckImage = this.FindControl<Image>("DeckImage");
                _stakeOverlayImage = this.FindControl<Image>("StakeOverlayImage");
                _deckDescriptionText = this.FindControl<TextBlock>("DeckDescriptionText");

                // Subscribe to deck/stake changes
                vm.PropertyChanged += OnViewModelPropertyChanged;

                // Initial load
                UpdateDeckAndStakeDisplay(vm);
            }
        }

        private void OnViewModelPropertyChanged(
            object? sender,
            System.ComponentModel.PropertyChangedEventArgs e
        )
        {
            if (
                e.PropertyName == nameof(SearchModalViewModel.SelectedDeckIndex)
                || e.PropertyName == nameof(SearchModalViewModel.SelectedStakeIndex)
                || e.PropertyName == nameof(SearchModalViewModel.LoadedConfig)
            )
            {
                if (DataContext is SearchModalViewModel vm)
                {
                    UpdateDeckAndStakeDisplay(vm);
                }
            }
        }

        private void UpdateDeckAndStakeDisplay(SearchModalViewModel vm)
        {
            if (_deckImage == null || _stakeOverlayImage == null)
                return;

            // Get deck and stake names
            var deckName = vm.DeckDisplayValues[vm.SelectedDeckIndex];
            var stakeName = System.Linq.Enumerable.ToArray(BalatroData.Stakes.Values)[
                vm.SelectedStakeIndex
            ];

            // Load deck image
            var deckImage = SpriteService.Instance.GetDeckImage(deckName);
            if (deckImage != null)
            {
                _deckImage.Source = deckImage;
            }

            // Load stake overlay image
            var stakeImage = SpriteService.Instance.GetStakeImage(stakeName);
            if (stakeImage != null)
            {
                _stakeOverlayImage.Source = stakeImage;
            }

            // Update deck description
            if (
                _deckDescriptionText != null
                && BalatroData.Decks.TryGetValue(deckName, out var deckFullName)
            )
            {
                var description = GetDeckDescription(deckName);
                _deckDescriptionText.Text = description;
            }
        }

        private string GetDeckDescription(string deckName)
        {
            // Deck descriptions from Balatro
            return deckName switch
            {
                "Red Deck" => "Start with +1 Discard",
                "Blue Deck" => "Start with +1 Hand",
                "Yellow Deck" => "Start with +$10",
                "Green Deck" => "Start with +$5 at end of each round",
                "Black Deck" => "Start with +1 Joker slot, +1 Hand, -1 Discard",
                "Magic Deck" => "Start with 2 Crystal Balls and 1 extra Consumable slot",
                "Nebula Deck" => "Start with Voucher [Telescope]. +3 Consumable slots",
                "Ghost Deck" =>
                    "Spectral cards and Hex shop vouchers appear 2X more often. Start with 1 Hex",
                "Abandoned Deck" => "Start with no Face Cards in deck",
                "Checkered Deck" => "Start with 26 Spades and 26 Hearts in deck",
                "Zodiac Deck" =>
                    "Start with Voucher [Tarot Merchant], Voucher [Planet Merchant], Voucher [Overstock]",
                "Painted Deck" => "All cards start with a random enhancement. +1 Hand Size",
                "Anaglyph Deck" => "After defeating each Boss Blind, gain a Double Tag",
                "Plasma Deck" =>
                    "Balance Chips and Mult when calculating score for played hand. Base Chips and Mult set to stake Ante × 4",
                "Erratic Deck" =>
                    "All ranks and suits are unknown until card is played. Random starting deck.",
                _ => "",
            };
        }
    }
}

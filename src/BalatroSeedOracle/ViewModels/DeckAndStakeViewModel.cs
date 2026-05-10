using System;
using System.Collections.Generic;
using Avalonia.Media;
using BalatroSeedOracle.Components;
using BalatroSeedOracle.Helpers;
using BalatroSeedOracle.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace BalatroSeedOracle.ViewModels
{
    public partial class DeckAndStakeViewModel : ObservableObject
    {
        private readonly DeckAndStakeSelectorViewModel _inner;

        public DeckAndStakeViewModel(SpriteService spriteService)
        {
            _inner = new DeckAndStakeSelectorViewModel(spriteService);
            UpdateImages();
        }

        [ObservableProperty]
        private int deckIndex;

        [ObservableProperty]
        private int stakeIndex;

        [ObservableProperty]
        private IImage? deckImage;

        [ObservableProperty]
        private IImage? stakeImage;

        public IRelayCommand PreviousDeckCommand => _inner.PreviousDeckCommand;
        public IRelayCommand NextDeckCommand => _inner.NextDeckCommand;
        public IRelayCommand PreviousStakeCommand => _inner.PreviousStakeCommand;
        public IRelayCommand NextStakeCommand => _inner.NextStakeCommand;
        public IRelayCommand SelectCommand => _inner.SelectCommand;

        private void UpdateImages()
        {
            // Use generated observable properties rather than referencing backing fields
            DeckImage = _inner.DeckImage;
            StakeImage = _inner.StakeImage;
        }
    }
}

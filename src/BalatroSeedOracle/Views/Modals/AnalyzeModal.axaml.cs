using System;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using BalatroSeedOracle.Components;
using BalatroSeedOracle.Helpers;
using BalatroSeedOracle.Services;
using BalatroSeedOracle.ViewModels;

namespace BalatroSeedOracle.Views.Modals
{
    /// <summary>
    /// AnalyzeModal view - handles seed analysis with deck/stake selection.
    /// All business logic is in AnalyzeModalViewModel.
    /// </summary>
    public partial class AnalyzeModal : UserControl
    {
        private AnalyzeModalViewModel? ViewModel => DataContext as AnalyzeModalViewModel;
        private DeckAndStakeSelector? _deckAndStakeSelector;
        private AnalyzeModalViewModel? _subscribedViewModel;

        /// <summary>Parameterless ctor for XAML loader only. Throws at runtime. Creator must pass ViewModel.</summary>
        public AnalyzeModal()
            : this(throwForDesignTimeOnly: true) { }

        private AnalyzeModal(bool throwForDesignTimeOnly)
        {
            if (throwForDesignTimeOnly)
                throw new InvalidOperationException(
                    "Do not use AnalyzeModal(). Creator must pass AnalyzeModalViewModel."
                );
            InitializeComponent();
        }

        public AnalyzeModal(AnalyzeModalViewModel viewModel)
        {
            DataContext = viewModel ?? throw new ArgumentNullException(nameof(viewModel));
            InitializeComponent();
        }

        private void AttachSelectorAndViewModel()
        {
            _deckAndStakeSelector = DeckAndStakeSelector;

            if (_subscribedViewModel != null)
            {
                _subscribedViewModel.PropertyChanged -= OnViewModelPropertyChanged;
                _subscribedViewModel = null;
            }

            if (ViewModel != null)
            {
                _subscribedViewModel = ViewModel;
                _subscribedViewModel.PropertyChanged += OnViewModelPropertyChanged;
                if (_deckAndStakeSelector != null)
                {
                    _deckAndStakeSelector.DeckIndex = ViewModel.DeckIndex;
                    _deckAndStakeSelector.StakeIndex = ViewModel.StakeIndex;
                }
            }

            if (_deckAndStakeSelector != null)
            {
                _deckAndStakeSelector.SelectionChanged -= OnDeckAndStakeSelectionChanged;
                _deckAndStakeSelector.DeckSelected -= OnDeckSelected;

                _deckAndStakeSelector.SelectionChanged += OnDeckAndStakeSelectionChanged;
                _deckAndStakeSelector.DeckSelected += OnDeckSelected;
            }
        }

        private void DetachSelectorAndViewModel()
        {
            if (_subscribedViewModel != null)
            {
                _subscribedViewModel.PropertyChanged -= OnViewModelPropertyChanged;
                _subscribedViewModel = null;
            }

            if (_deckAndStakeSelector != null)
            {
                _deckAndStakeSelector.SelectionChanged -= OnDeckAndStakeSelectionChanged;
                _deckAndStakeSelector.DeckSelected -= OnDeckSelected;
            }
        }

        private void OnViewModelPropertyChanged(
            object? sender,
            System.ComponentModel.PropertyChangedEventArgs e
        )
        {
            if (_deckAndStakeSelector == null || ViewModel == null)
                return;

            if (
                e.PropertyName == nameof(AnalyzeModalViewModel.DeckIndex)
                && _deckAndStakeSelector.DeckIndex != ViewModel.DeckIndex
            )
            {
                _deckAndStakeSelector.DeckIndex = ViewModel.DeckIndex;
            }
            else if (
                e.PropertyName == nameof(AnalyzeModalViewModel.StakeIndex)
                && _deckAndStakeSelector.StakeIndex != ViewModel.StakeIndex
            )
            {
                _deckAndStakeSelector.StakeIndex = ViewModel.StakeIndex;
            }
        }

        protected override void OnAttachedToVisualTree(Avalonia.VisualTreeAttachmentEventArgs e)
        {
            base.OnAttachedToVisualTree(e);
            AttachSelectorAndViewModel();
        }

        protected override void OnDetachedFromVisualTree(Avalonia.VisualTreeAttachmentEventArgs e)
        {
            DetachSelectorAndViewModel();
            base.OnDetachedFromVisualTree(e);
        }

        protected override void OnLoaded(RoutedEventArgs e)
        {
            base.OnLoaded(e);
            AttachSelectorAndViewModel();
        }

        protected override void OnUnloaded(RoutedEventArgs e)
        {
            base.OnUnloaded(e);
            DetachSelectorAndViewModel();
        }

        private void OnDeckAndStakeSelectionChanged(
            object? sender,
            (int deckIndex, int stakeIndex) selection
        )
        {
            if (ViewModel != null)
            {
                ViewModel.DeckIndex = selection.deckIndex;
                ViewModel.StakeIndex = selection.stakeIndex;
            }
        }

        private void OnDeckSelected(object? sender, EventArgs e)
        {
            if (_deckAndStakeSelector != null && ViewModel != null)
            {
                // Update ViewModel with selected deck/stake indices
                ViewModel.DeckIndex = _deckAndStakeSelector.DeckIndex;
                ViewModel.StakeIndex = _deckAndStakeSelector.StakeIndex;

                // Notify ViewModel that deck was selected (switches to analyzer tab)
                ViewModel.OnDeckSelected();
            }
        }

        /// <summary>
        /// Programmatically set the seed, switch to analyzer tab, and run analysis.
        /// Safe to call after control construction; will defer execution until Loaded if needed.
        /// </summary>
        public void SetSeedAndAnalyze(string seed)
        {
            void Execute()
            {
                if (ViewModel != null)
                {
                    ViewModel.SetSeedAndAnalyze(seed);
                }
                else
                {
                    DebugLogger.LogError(
                        "AnalyzeModal",
                        "ViewModel is null, cannot set seed and analyze"
                    );
                }
            }

            if (this.IsLoaded)
            {
                Execute();
            }
            else
            {
                // Defer until loaded
                this.Loaded += (s, _) => Execute();
            }
        }
    }
}

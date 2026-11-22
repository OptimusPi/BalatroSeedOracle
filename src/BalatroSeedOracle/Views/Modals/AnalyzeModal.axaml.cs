using System;
using System.Collections.Generic;
using Avalonia.Controls;
using Avalonia.Input;
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
            Focusable = true;
            KeyDown += OnKeyDown;
        }

        // Left/Right step through the result list the modal was opened from. A focused TextBox
        // handles its own arrow keys first, so typing a seed still moves the caret.
        private void OnKeyDown(object? sender, KeyEventArgs e)
        {
            if (ViewModel is not { HasSeedList: true } vm || e.KeyModifiers != KeyModifiers.None)
                return;

            var delta = e.Key switch
            {
                Key.Left => -1,
                Key.Right => 1,
                _ => 0,
            };
            if (delta == 0)
                return;

            vm.StepSeed(delta);
            e.Handled = true;
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
            Focus();
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
        public void SetSeedAndAnalyze(string seed) =>
            RunWhenLoaded(vm => vm.SetSeedAndAnalyze(seed));

        /// <summary>
        /// Like <see cref="SetSeedAndAnalyze"/>, and remembers the result list (in on-screen order)
        /// so Left/Right step through it.
        /// </summary>
        public void SetSeedsAndAnalyze(string seed, IReadOnlyList<string> seeds) =>
            RunWhenLoaded(vm => vm.SetSeedsAndAnalyze(seed, seeds));

        private void RunWhenLoaded(Action<AnalyzeModalViewModel> action)
        {
            void Execute()
            {
                if (ViewModel != null)
                {
                    action(ViewModel);
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

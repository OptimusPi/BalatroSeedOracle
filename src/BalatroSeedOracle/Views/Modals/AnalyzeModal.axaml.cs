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

        /// <summary>Parameterless ctor for XAML loader only. Throws at runtime. Creator must pass ViewModel.</summary>
        public AnalyzeModal()
            : this(throwForDesignTimeOnly: true)
        {
        }

        private AnalyzeModal(bool throwForDesignTimeOnly)
        {
            if (throwForDesignTimeOnly)
                throw new InvalidOperationException("Do not use AnalyzeModal(). Creator must pass AnalyzeModalViewModel.");
            InitializeComponent();
        }

        public AnalyzeModal(AnalyzeModalViewModel viewModel)
        {
            DataContext = viewModel ?? throw new ArgumentNullException(nameof(viewModel));
            InitializeComponent();
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);

            // Get deck/stake selector component
            _deckAndStakeSelector = DeckAndStakeSelector;
        }

        protected override void OnLoaded(RoutedEventArgs e)
        {
            base.OnLoaded(e);

            // Subscribe to DeckSelected event to switch to analyzer tab
            if (_deckAndStakeSelector != null && ViewModel != null)
            {
                _deckAndStakeSelector.DeckSelected += (s, _) =>
                {
                    // Update ViewModel with selected deck/stake indices
                    ViewModel.DeckIndex = _deckAndStakeSelector.DeckIndex;
                    ViewModel.StakeIndex = _deckAndStakeSelector.StakeIndex;

                    // Notify ViewModel that deck was selected (switches to analyzer tab)
                    ViewModel.OnDeckSelected();
                };
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

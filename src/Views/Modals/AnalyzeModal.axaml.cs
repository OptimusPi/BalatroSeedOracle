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
        private Components.BalatroTabControl? _tabHeader;
        private bool _suppressHeaderSync = false;

        public AnalyzeModal()
        {
            // Initialize ViewModel with required services FIRST (consistency with other modals)
            var spriteService = ServiceHelper.GetRequiredService<SpriteService>();
            var userProfileService = ServiceHelper.GetRequiredService<UserProfileService>();
            DataContext = new AnalyzeModalViewModel(spriteService, userProfileService);

            InitializeComponent();
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);

            // Get deck/stake selector component
            _deckAndStakeSelector = this.FindControl<DeckAndStakeSelector>("DeckAndStakeSelector");

            // Wire up Balatro-style tab header
            _tabHeader = this.FindControl<Components.BalatroTabControl>("TabHeader");
            if (_tabHeader != null)
            {
                // Initialize header tabs
                _tabHeader.SetTabs("DECK/STAKE", "ANALYZE");

                // Forward header changes to ViewModel
                _tabHeader.TabChanged += (s, tabIndex) =>
                {
                    if (ViewModel == null)
                        return;
                    _suppressHeaderSync = true;
                    ViewModel.ActiveTab = (Models.AnalyzeModalTab)tabIndex;
                    _suppressHeaderSync = false;
                };

                // Sync header when ViewModel changes ActiveTab programmatically
                if (ViewModel != null)
                {
                    ViewModel.PropertyChanged += (s, e) =>
                    {
                        if (!_suppressHeaderSync && e.PropertyName == nameof(ViewModel.ActiveTab))
                        {
                            _tabHeader.SwitchToTab((int)ViewModel.ActiveTab);
                        }
                    };

                    // Ensure initial selection matches ViewModel
                    _tabHeader.SwitchToTab((int)ViewModel.ActiveTab);
                }
            }
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

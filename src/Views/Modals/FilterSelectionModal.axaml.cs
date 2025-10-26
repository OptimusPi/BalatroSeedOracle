using System;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Avalonia.Media.Imaging;
using BalatroSeedOracle.ViewModels;
using BalatroSeedOracle.Helpers;
using BalatroSeedOracle.Controls;

namespace BalatroSeedOracle.Views.Modals
{
    public partial class FilterSelectionModal : UserControl
    {
        public FilterSelectionModalViewModel? ViewModel => DataContext as FilterSelectionModalViewModel;

        public event EventHandler? CloseRequested;

        private Image? _deckImage;
        private Image? _stakeOverlayImage;

        public FilterSelectionModal()
        {
            InitializeComponent();

            // Subscribe to DataContext changes to wire up the ViewModel event
            this.DataContextChanged += OnDataContextChanged;

            // Wire up deck/stake images when loaded
            this.Loaded += OnLoaded;
        }

        private void OnLoaded(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
        {
            // Find the deck and stake images in the XAML
            _deckImage = this.FindControl<Image>("DeckImage");
            _stakeOverlayImage = this.FindControl<Image>("StakeOverlayImage");

            // Load default deck (Red Deck with White Stake)
            LoadDeckAndStake("Red Deck", "White");
        }

        private void LoadDeckAndStake(string deckName, string stakeName)
        {
            // Get deck items with stake overlay from factory
            var deckItems = PanelItemFactory.CreateDeckItemsWithStake(stakeName);

            // Find the requested deck
            var deckItem = deckItems.Find(item => item.Title == deckName);
            if (deckItem != null && deckItem.GetImage != null)
            {
                var image = deckItem.GetImage();
                if (_deckImage != null)
                {
                    _deckImage.Source = image;
                }
                if (_stakeOverlayImage != null)
                {
                    _stakeOverlayImage.Source = image;
                }
            }
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }

        private void OnDataContextChanged(object? sender, EventArgs e)
        {
            // Unsubscribe from old ViewModel if any
            if (sender is FilterSelectionModal modal)
            {
                var oldVm = modal.DataContext as FilterSelectionModalViewModel;
                if (oldVm != null)
                {
                    oldVm.ModalCloseRequested -= OnModalCloseRequested;
                }

                // Subscribe to new ViewModel
                var newVm = modal.DataContext as FilterSelectionModalViewModel;
                if (newVm != null)
                {
                    newVm.ModalCloseRequested += OnModalCloseRequested;
                }
            }
        }

        private void OnModalCloseRequested(object? sender, EventArgs e)
        {
            DebugLogger.Log("FilterSelectionModal", "Close requested from ViewModel");
            CloseRequested?.Invoke(this, EventArgs.Empty);
        }

        /// <summary>
        /// Shows the modal as a dialog and returns the result
        /// </summary>
        /// <param name="parent">Parent window</param>
        /// <returns>True if confirmed, false if cancelled</returns>
        public async System.Threading.Tasks.Task<bool> ShowDialog(Window parent)
        {
            var tcs = new System.Threading.Tasks.TaskCompletionSource<bool>();

            // Add to parent window
            if (parent.Content is Panel panel)
            {
                panel.Children.Add(this);

                void OnClose(object? s, EventArgs e)
                {
                    panel.Children.Remove(this);
                    CloseRequested -= OnClose;
                    tcs.SetResult(true);
                }

                CloseRequested += OnClose;
            }
            else
            {
                DebugLogger.LogError("FilterSelectionModal", "Parent window content is not a Panel");
                tcs.SetResult(false);
            }

            return await tcs.Task;
        }
    }
}

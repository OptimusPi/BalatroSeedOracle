using System;
using System.ComponentModel;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Avalonia.Media.Imaging;
using BalatroSeedOracle.Controls;
using BalatroSeedOracle.Helpers;
using BalatroSeedOracle.Services;
using BalatroSeedOracle.ViewModels;
using MsBox.Avalonia;
using MsBox.Avalonia.Enums;

namespace BalatroSeedOracle.Views.Modals
{
    public partial class FilterSelectionModal : UserControl
    {
        public FilterSelectionModalViewModel? ViewModel =>
            DataContext as FilterSelectionModalViewModel;

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

            // Load default deck (Red with White Stake) - SpriteService expects SHORT deck names!
            LoadDeckAndStake("Red", "White");
        }

        private void LoadDeckAndStake(string deckName, string stakeName)
        {
            // Use SpriteService to get the composite deck + stake sticker image
            var spriteService = SpriteService.Instance;

            // Get the deck with stake sticker already composited
            var compositedImage = spriteService.GetDeckWithStakeSticker(deckName, stakeName);
            if (compositedImage != null && _deckImage != null)
            {
                _deckImage.Source = compositedImage;
            }

            // Hide the overlay image since we're using the composited version
            if (_stakeOverlayImage != null)
            {
                _stakeOverlayImage.IsVisible = false;
            }

            DebugLogger.Log(
                "FilterSelectionModal",
                $"Loaded composited deck image: {deckName} with {stakeName} stake sticker"
            );
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }

        private void OnDataContextChanged(object? sender, EventArgs e)
        {
            DebugLogger.Log("FilterSelectionModal", "🔵 DataContext changed!");

            // Unsubscribe from old ViewModel if any
            if (sender is FilterSelectionModal modal)
            {
                var oldVm = modal.DataContext as FilterSelectionModalViewModel;
                if (oldVm != null)
                {
                    DebugLogger.Log("FilterSelectionModal", "  Unsubscribing from old ViewModel");
                    oldVm.ModalCloseRequested -= OnModalCloseRequested;
                    oldVm.PropertyChanged -= OnViewModelPropertyChanged;
                    oldVm.DeleteConfirmationRequested -= OnDeleteConfirmationRequested;
                }

                // Subscribe to new ViewModel
                var newVm = modal.DataContext as FilterSelectionModalViewModel;
                if (newVm != null)
                {
                    DebugLogger.Log(
                        "FilterSelectionModal",
                        $"  Subscribing to new ViewModel - EnableSearch={newVm.EnableSearch}"
                    );
                    newVm.ModalCloseRequested += OnModalCloseRequested;
                    newVm.PropertyChanged += OnViewModelPropertyChanged;
                    newVm.DeleteConfirmationRequested += OnDeleteConfirmationRequested;

                    // Load initial deck/stake if filter is already selected
                    if (newVm.SelectedFilter != null)
                    {
                        UpdateDeckAndStake(newVm.SelectedFilter);
                    }
                }
                else
                {
                    DebugLogger.LogError(
                        "FilterSelectionModal",
                        "  ❌ NEW DATACONTEXT IS NOT FilterSelectionModalViewModel!"
                    );
                }
            }
        }

        private async void OnDeleteConfirmationRequested(object? sender, string filterName)
        {
            var result = await MessageBoxManager
                .GetMessageBoxStandard(
                    "Delete Filter?",
                    $"Are you sure you want to delete '{filterName}'?\n\nThis cannot be undone.",
                    ButtonEnum.YesNo,
                    Icon.Warning
                )
                .ShowAsync();

            if (result == ButtonResult.Yes && ViewModel != null)
            {
                await ViewModel.ConfirmDeleteAsync();
            }
        }

        private void OnViewModelPropertyChanged(
            object? sender,
            System.ComponentModel.PropertyChangedEventArgs e
        )
        {
            if (e.PropertyName == nameof(FilterSelectionModalViewModel.SelectedFilter))
            {
                var vm = sender as FilterSelectionModalViewModel;
                if (vm?.SelectedFilter != null)
                {
                    UpdateDeckAndStake(vm.SelectedFilter);
                }
            }
        }

        private void UpdateDeckAndStake(FilterBrowserItem filter)
        {
            // Extract deck and stake names with fallbacks
            var deckName = filter.DeckName ?? "Red";
            var stakeName = filter.StakeName ?? "White";

            // SpriteService expects SHORT deck names (just "Red", not "Red Deck")
            // Filter JSON stores short names like "Red", "Anaglyph", etc.
            // NO need to add " Deck" suffix!

            DebugLogger.Log(
                "FilterSelectionModal",
                $"Loading deck: {deckName}, stake: {stakeName}"
            );
            LoadDeckAndStake(deckName, stakeName);
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
                DebugLogger.LogError(
                    "FilterSelectionModal",
                    "Parent window content is not a Panel"
                );
                tcs.SetResult(false);
            }

            return await tcs.Task;
        }
    }
}

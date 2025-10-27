using System;
using System.ComponentModel;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Avalonia.Media.Imaging;
using BalatroSeedOracle.ViewModels;
using BalatroSeedOracle.Helpers;
using BalatroSeedOracle.Controls;
using MsBox.Avalonia;
using MsBox.Avalonia.Enums;

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
                    oldVm.PropertyChanged -= OnViewModelPropertyChanged;
                    oldVm.DeleteConfirmationRequested -= OnDeleteConfirmationRequested;
                }

                // Subscribe to new ViewModel
                var newVm = modal.DataContext as FilterSelectionModalViewModel;
                if (newVm != null)
                {
                    newVm.ModalCloseRequested += OnModalCloseRequested;
                    newVm.PropertyChanged += OnViewModelPropertyChanged;
                    newVm.DeleteConfirmationRequested += OnDeleteConfirmationRequested;

                    // Load initial deck/stake if filter is already selected
                    if (newVm.SelectedFilter != null)
                    {
                        UpdateDeckAndStake(newVm.SelectedFilter);
                    }
                }
            }
        }

        private async void OnDeleteConfirmationRequested(object? sender, string filterName)
        {
            var result = await MessageBoxManager
                .GetMessageBoxStandard("Delete Filter?",
                    $"Are you sure you want to delete '{filterName}'?\n\nThis cannot be undone.",
                    ButtonEnum.YesNo,
                    Icon.Warning)
                .ShowAsync();

            if (result == ButtonResult.Yes && ViewModel != null)
            {
                ViewModel.ConfirmDelete();
            }
        }

        private void OnViewModelPropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
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

            // Convert short deck names to full display names (e.g., "Red" -> "Red Deck")
            // PanelItemFactory expects full names like "Red Deck", but config stores "Red"
            if (!deckName.EndsWith(" Deck", StringComparison.OrdinalIgnoreCase))
            {
                deckName = $"{deckName} Deck";
            }

            DebugLogger.Log("FilterSelectionModal", $"Loading deck: {deckName}, stake: {stakeName}");
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
                DebugLogger.LogError("FilterSelectionModal", "Parent window content is not a Panel");
                tcs.SetResult(false);
            }

            return await tcs.Task;
        }
    }
}

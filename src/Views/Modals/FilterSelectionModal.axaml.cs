using System;
using System.ComponentModel;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
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
            // Get the parent window
            var parentWindow = Avalonia.Controls.TopLevel.GetTopLevel(this) as Window;

            if (parentWindow == null)
            {
                throw new InvalidOperationException(
                    "FilterSelectionModal must be shown from a Window context!"
                );
            }

            // Create styled confirmation dialog
            var dialog = new Window
            {
                Width = 450,
                Height = 220,
                CanResize = false,
                WindowStartupLocation = WindowStartupLocation.CenterOwner,
                SystemDecorations = SystemDecorations.None,
                Background = Avalonia.Media.Brushes.Transparent,
                TransparencyLevelHint = new[] { WindowTransparencyLevel.Transparent }
            };

            bool? result = null;

            var yesButton = new Button
            {
                Content = "Yes",
                Classes = { "btn-red" },
                MinWidth = 120,
                Height = 45
            };

            var noButton = new Button
            {
                Content = "No",
                Classes = { "btn-blue" },
                MinWidth = 120,
                Height = 45
            };

            yesButton.Click += (s, ev) =>
            {
                result = true;
                dialog.Close();
            };

            noButton.Click += (s, ev) =>
            {
                result = false;
                dialog.Close();
            };

            // Main container
            var mainBorder = new Border
            {
                Background = this.FindResource("DarkBorder") as Avalonia.Media.IBrush,
                BorderBrush = this.FindResource("LightGrey") as Avalonia.Media.IBrush,
                BorderThickness = new Thickness(3),
                CornerRadius = new CornerRadius(16)
            };

            var mainGrid = new Grid
            {
                RowDefinitions = new RowDefinitions("Auto,*,Auto")
            };

            // Title bar
            var titleBar = new Border
            {
                [Grid.RowProperty] = 0,
                Background = this.FindResource("ModalGrey") as Avalonia.Media.IBrush,
                CornerRadius = new CornerRadius(14, 14, 0, 0),
                Padding = new Thickness(20, 12)
            };

            titleBar.Child = new TextBlock
            {
                Text = "Delete Filter?",
                FontSize = 24,
                Foreground = this.FindResource("White") as Avalonia.Media.IBrush,
                HorizontalAlignment = HorizontalAlignment.Center
            };

            mainGrid.Children.Add(titleBar);

            // Content area
            var contentBorder = new Border
            {
                [Grid.RowProperty] = 1,
                Background = this.FindResource("DarkBackground") as Avalonia.Media.IBrush,
                Padding = new Thickness(24)
            };

            var contentStack = new StackPanel { Spacing = 12 };

            // Warning message with icon
            var warningPanel = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                Spacing = 12,
                MaxWidth = 380 // Constrain width to prevent overflow
            };

            var warningIcon = new TextBlock
            {
                Text = "⚠",
                FontSize = 32,
                Foreground = new Avalonia.Media.SolidColorBrush(Avalonia.Media.Color.Parse("#FF6B6B")),
                VerticalAlignment = VerticalAlignment.Top,
                Margin = new Thickness(0, 0, 0, 0)
            };

            var messageText = new TextBlock
            {
                Text = $"Are you sure you want to delete '{filterName}'?",
                FontSize = 16,
                Foreground = this.FindResource("White") as Avalonia.Media.IBrush,
                TextWrapping = TextWrapping.Wrap,
                MaxWidth = 320 // Leave room for icon
            };

            warningPanel.Children.Add(warningIcon);
            warningPanel.Children.Add(messageText);
            contentStack.Children.Add(warningPanel);

            contentStack.Children.Add(new TextBlock
            {
                Text = "This cannot be undone.",
                FontSize = 14,
                Foreground = this.FindResource("LightGrey") as Avalonia.Media.IBrush,
                FontStyle = FontStyle.Italic
            });

            contentBorder.Child = contentStack;
            mainGrid.Children.Add(contentBorder);

            // Button area
            var buttonBorder = new Border
            {
                [Grid.RowProperty] = 2,
                Background = this.FindResource("DarkBackground") as Avalonia.Media.IBrush,
                CornerRadius = new CornerRadius(0, 0, 14, 14),
                Padding = new Thickness(20, 12, 20, 20)
            };

            var buttonPanel = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                HorizontalAlignment = HorizontalAlignment.Center,
                Spacing = 12
            };
            buttonPanel.Children.Add(yesButton);
            buttonPanel.Children.Add(noButton);

            buttonBorder.Child = buttonPanel;
            mainGrid.Children.Add(buttonBorder);

            mainBorder.Child = mainGrid;
            dialog.Content = mainBorder;

            await dialog.ShowDialog(parentWindow);

            if (result == true && ViewModel != null)
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

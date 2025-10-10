using System;
using System.Linq;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Media;
using BalatroSeedOracle.Helpers;
using BalatroSeedOracle.Services;
using BalatroSeedOracle.ViewModels;
using Motely;

namespace BalatroSeedOracle.Features.Analyzer;

public partial class AnalyzerView : UserControl
{
    private AnalyzerViewModel? ViewModel => DataContext as AnalyzerViewModel;

    public AnalyzerView()
    {
        Helpers.DebugLogger.Log("AnalyzerView", "Constructor called!");
        InitializeComponent();

        // Set up hotkeys
        this.KeyDown += OnKeyDown;

        // Wire up deck/stake selectors
        var deckCombo = this.FindControl<ComboBox>("DeckComboBox");
        var stakeCombo = this.FindControl<ComboBox>("StakeComboBox");

        if (deckCombo != null)
        {
            deckCombo.SelectionChanged += DeckComboBox_SelectionChanged;
        }

        if (stakeCombo != null)
        {
            stakeCombo.SelectionChanged += StakeComboBox_SelectionChanged;
        }

        // Make control focusable for hotkeys
        this.Focusable = true;
        this.Loaded += (s, e) => this.Focus();

        // Wire up data context change to render images
        this.DataContextChanged += OnDataContextChanged;
    }

    private void OnDataContextChanged(object? sender, EventArgs e)
    {
        Helpers.DebugLogger.Log("AnalyzerView", $"DataContext changed, ViewModel is {(ViewModel == null ? "NULL" : "SET")}");
        if (ViewModel != null)
        {
            ViewModel.PropertyChanged += ViewModel_PropertyChanged;
            Helpers.DebugLogger.Log("AnalyzerView", "Subscribed to ViewModel.PropertyChanged");
        }
    }

    private void ViewModel_PropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        Helpers.DebugLogger.Log("AnalyzerView", $"Property changed: {e.PropertyName}");
        // Re-render images when ante changes or analysis updates
        if (e.PropertyName == nameof(AnalyzerViewModel.CurrentBoss) ||
            e.PropertyName == nameof(AnalyzerViewModel.CurrentVoucher) ||
            e.PropertyName == nameof(AnalyzerViewModel.CurrentShopItemsRaw) ||
            e.PropertyName == nameof(AnalyzerViewModel.CurrentPacks))
        {
            Helpers.DebugLogger.Log("AnalyzerView", "Triggering RenderImages()");
            RenderImages();
        }
    }

    private void RenderImages()
    {
        Helpers.DebugLogger.Log("AnalyzerView", "RenderImages() called");
        RenderBossImage();
        RenderVoucherImage();
        RenderShopImages();
        RenderPackImages();
    }

    private void RenderBossImage()
    {
        var bossImage = this.FindControl<Image>("BossImage");
        var bossText = this.FindControl<TextBlock>("BossText");

        if (bossImage == null || bossText == null || ViewModel?.CurrentBoss == null)
            return;

        var bossName = ViewModel.CurrentBoss.Value.ToString();
        var sprite = SpriteService.Instance.GetBossImage(bossName);
        if (sprite != null)
        {
            bossImage.Source = sprite;
            bossImage.IsVisible = true;
            bossText.IsVisible = false;
        }
        else
        {
            bossImage.IsVisible = false;
            bossText.IsVisible = true;
        }
    }

    private void RenderVoucherImage()
    {
        var voucherImage = this.FindControl<Image>("VoucherImage");
        var voucherText = this.FindControl<TextBlock>("VoucherText");

        if (voucherImage == null || voucherText == null || ViewModel?.CurrentVoucher == null)
            return;

        var voucherStr = ViewModel.CurrentVoucher.Value.ToString();
        if (voucherStr != "None")
        {
            var sprite = SpriteService.Instance.GetVoucherImage(voucherStr);
            if (sprite != null)
            {
                voucherImage.Source = sprite;
                voucherImage.IsVisible = true;
                voucherText.IsVisible = false;
            }
            else
            {
                voucherImage.IsVisible = false;
                voucherText.IsVisible = true;
            }
        }
        else
        {
            voucherImage.IsVisible = false;
            voucherText.IsVisible = true;
        }
    }

    private void RenderShopImages()
    {
        var shopContainer = this.FindControl<ItemsControl>("ShopItemsContainer");
        if (shopContainer == null)
        {
            Helpers.DebugLogger.LogError("AnalyzerView", "ShopItemsContainer control not found!");
            return;
        }

        if (ViewModel == null)
        {
            Helpers.DebugLogger.LogError("AnalyzerView", "ViewModel is null!");
            return;
        }

        var items = ViewModel.CurrentShopItemsRaw;
        Helpers.DebugLogger.Log("AnalyzerView", $"RenderShopImages: {items?.Count ?? 0} items");
        var itemElements = new System.Collections.Generic.List<StackPanel>();

        for (int i = 0; i < items.Count; i++)
        {
            var item = items[i];
            var itemPanel = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                Spacing = 10,
                Margin = new Avalonia.Thickness(0, 2)
            };

            // Add index number
            itemPanel.Children.Add(new TextBlock
            {
                Text = $"{i + 1})",
                Foreground = Brushes.White,
                VerticalAlignment = VerticalAlignment.Center,
                MinWidth = 30
            });

            // Add item image
            var sprite = SpriteService.Instance.GetItemImage(item.Type.ToString());
            if (sprite != null)
            {
                itemPanel.Children.Add(new Image
                {
                    Source = sprite,
                    Width = 35.5,
                    Height = 47.5,
                    Stretch = Stretch.Uniform,
                    VerticalAlignment = VerticalAlignment.Center
                });
            }

            // Add item name
            itemPanel.Children.Add(new TextBlock
            {
                Text = FormatUtils.FormatItem(item),
                Foreground = Brushes.White,
                VerticalAlignment = VerticalAlignment.Center
            });

            itemElements.Add(itemPanel);
        }

        shopContainer.ItemsSource = itemElements;
    }

    private void RenderPackImages()
    {
        var packsContainer = this.FindControl<ItemsControl>("PacksContainer");
        if (packsContainer == null || ViewModel == null)
            return;

        var packs = ViewModel.CurrentPacks;
        var packElements = new System.Collections.Generic.List<Border>();

        foreach (var pack in packs)
        {
            var packBorder = new Border
            {
                Background = new SolidColorBrush(Color.Parse("#252525")),
                Padding = new Avalonia.Thickness(15),
                CornerRadius = new Avalonia.CornerRadius(4),
                Margin = new Avalonia.Thickness(0, 0, 10, 10),
                MinWidth = 200
            };

            var packStack = new StackPanel { Spacing = 8 };

            // Pack name
            packStack.Children.Add(new TextBlock
            {
                Text = pack.Name,
                FontWeight = FontWeight.Bold,
                Foreground = new SolidColorBrush(Color.Parse(pack.PackColor))
            });

            // Pack items with images
            for (int i = 0; i < pack.RawItems.Count; i++)
            {
                var item = pack.RawItems[i];
                var itemPanel = new StackPanel
                {
                    Orientation = Orientation.Horizontal,
                    Spacing = 8
                };

                var sprite = SpriteService.Instance.GetItemImage(item.Type.ToString());
                if (sprite != null)
                {
                    itemPanel.Children.Add(new Image
                    {
                        Source = sprite,
                        Width = 28.4,
                        Height = 38,
                        Stretch = Stretch.Uniform,
                        VerticalAlignment = VerticalAlignment.Center
                    });
                }

                itemPanel.Children.Add(new TextBlock
                {
                    Text = FormatUtils.FormatItem(item),
                    Foreground = Brushes.White,
                    FontSize = 12,
                    VerticalAlignment = VerticalAlignment.Center
                });

                packStack.Children.Add(itemPanel);
            }

            packBorder.Child = packStack;
            packElements.Add(packBorder);
        }

        packsContainer.ItemsSource = packElements;
    }

    private void OnKeyDown(object? sender, KeyEventArgs e)
    {
        if (ViewModel == null) return;

        switch (e.Key)
        {
            case Key.PageUp:
                ViewModel.PreviousResultCommand.Execute(null);
                e.Handled = true;
                break;

            case Key.PageDown:
                ViewModel.NextResultCommand.Execute(null);
                e.Handled = true;
                break;

            case Key.Up:
                ViewModel.ScrollUpAnteCommand.Execute(null);
                e.Handled = true;
                break;

            case Key.Down:
                ViewModel.ScrollDownAnteCommand.Execute(null);
                e.Handled = true;
                break;
        }
    }

    private void DeckComboBox_SelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (ViewModel == null || e.AddedItems.Count == 0) return;

        var deckCombo = sender as ComboBox;
        if (deckCombo == null) return;

        // Map combo box index to MotelyDeck enum
        var deckNames = new[]
        {
            MotelyDeck.Red,
            MotelyDeck.Blue,
            MotelyDeck.Yellow,
            MotelyDeck.Green,
            MotelyDeck.Black,
            MotelyDeck.Magic,
            MotelyDeck.Nebula,
            MotelyDeck.Ghost,
            MotelyDeck.Abandoned,
            MotelyDeck.Checkered,
            MotelyDeck.Zodiac,
            MotelyDeck.Painted,
            MotelyDeck.Anaglyph,
            MotelyDeck.Plasma,
            MotelyDeck.Erratic
        };

        if (deckCombo.SelectedIndex >= 0 && deckCombo.SelectedIndex < deckNames.Length)
        {
            ViewModel.SelectedDeck = deckNames[deckCombo.SelectedIndex];
        }
    }

    private void StakeComboBox_SelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (ViewModel == null || e.AddedItems.Count == 0) return;

        var stakeCombo = sender as ComboBox;
        if (stakeCombo == null) return;

        // Map combo box index to MotelyStake enum
        var stakeNames = new[]
        {
            MotelyStake.White,
            MotelyStake.Red,
            MotelyStake.Green,
            MotelyStake.Black,
            MotelyStake.Blue,
            MotelyStake.Purple,
            MotelyStake.Orange,
            MotelyStake.Gold
        };

        if (stakeCombo.SelectedIndex >= 0 && stakeCombo.SelectedIndex < stakeNames.Length)
        {
            ViewModel.SelectedStake = stakeNames[stakeCombo.SelectedIndex];
        }
    }
}

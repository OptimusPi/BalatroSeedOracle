using System;
using System.Collections.Generic;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Media;
using BalatroSeedOracle.Helpers;
using BalatroSeedOracle.Models;
using BalatroSeedOracle.Services;

namespace BalatroSeedOracle.Controls;

/// <summary>
/// Panel spinner control for deck/stake selection.
/// Uses direct x:Name field access (no FindControl anti-pattern).
/// </summary>
public partial class PanelSpinner : UserControl
{
    private int _currentIndex = 0;
    private List<PanelItem> _items = new();

    public static readonly DirectProperty<PanelSpinner, List<PanelItem>> ItemsProperty =
        AvaloniaProperty.RegisterDirect<PanelSpinner, List<PanelItem>>(
            nameof(Items),
            o => o.Items,
            (o, v) => o.Items = v
        );

    public static readonly DirectProperty<PanelSpinner, int> SelectedIndexProperty =
        AvaloniaProperty.RegisterDirect<PanelSpinner, int>(
            nameof(SelectedIndex),
            o => o.SelectedIndex,
            (o, v) => o.SelectedIndex = v
        );

    public static readonly DirectProperty<PanelSpinner, PanelItem?> SelectedItemProperty =
        AvaloniaProperty.RegisterDirect<PanelSpinner, PanelItem?>(
            nameof(SelectedItem),
            o => o.SelectedItem
        );

    public static readonly StyledProperty<bool> ShowArrowsProperty = AvaloniaProperty.Register<
        PanelSpinner,
        bool
    >(nameof(ShowArrows), defaultValue: true);

    public bool ShowArrows
    {
        get => GetValue(ShowArrowsProperty);
        set => SetValue(ShowArrowsProperty, value);
    }

    public List<PanelItem> Items
    {
        get => _items;
        set
        {
            SetAndRaise(ItemsProperty, ref _items, value);
            UpdateDisplay();
            UpdateDots();
        }
    }

    public int SelectedIndex
    {
        get => _currentIndex;
        set
        {
            if (value >= 0 && value < _items.Count)
            {
                SetAndRaise(SelectedIndexProperty, ref _currentIndex, value);
                UpdateDisplay();
                UpdateDots();
                RaisePropertyChanged(SelectedItemProperty, null, SelectedItem);
            }
        }
    }

    public PanelItem? SelectedItem =>
        _currentIndex >= 0 && _currentIndex < _items.Count ? _items[_currentIndex] : null;

    public event EventHandler<PanelItem?>? SelectionChanged;

    public PanelSpinner()
    {
        InitializeComponent();

        // Wire up ShowArrows property to button visibility (using direct x:Name fields)
        this.PropertyChanged += (s, e) =>
        {
            if (e.Property == ShowArrowsProperty)
            {
                PrevButton.IsVisible = ShowArrows;
                NextButton.IsVisible = ShowArrows;
            }
        };

        // Set initial visibility
        PrevButton.IsVisible = ShowArrows;
        NextButton.IsVisible = ShowArrows;
    }

    private void OnPrevClick(object? sender, RoutedEventArgs e)
    {
        if (_items.Count == 0)
            return;

        // Play filter switch sound - disabled (NAudio removed)
        // SoundEffectService.Instance.PlayFilterSwitch();

        // Circular navigation: if at beginning, wrap to end
        if (_currentIndex > 0)
        {
            SelectedIndex = _currentIndex - 1;
        }
        else
        {
            SelectedIndex = _items.Count - 1; // Wrap to last item
        }
        SelectionChanged?.Invoke(this, SelectedItem);
    }

    private void OnNextClick(object? sender, RoutedEventArgs e)
    {
        if (_items.Count == 0)
            return;

        // Play filter switch sound - disabled (NAudio removed)
        // SoundEffectService.Instance.PlayFilterSwitch();

        // Circular navigation: if at end, wrap to beginning
        if (_currentIndex < _items.Count - 1)
        {
            SelectedIndex = _currentIndex + 1;
        }
        else
        {
            SelectedIndex = 0; // Wrap to first item
        }
        SelectionChanged?.Invoke(this, SelectedItem);
    }

    private void UpdateDisplay()
    {
        if (_items.Count == 0)
            return;

        var item = _items[_currentIndex];

        TitleText.Text = item.Title;
        DescriptionText.Text = item.Description;

        // Check if we should show a custom control or an image
        if (item.GetImage != null)
        {
            var image = item.GetImage();
            SpriteImage.Source = image;
        }
        else if (item.GetControl != null && item.Value == "__CREATE_NEW__")
        {
            // Custom control path (for filter modal)
            var viewbox = SpriteImage.Parent as Viewbox;
            if (viewbox?.Parent is Grid grid)
            {
                var customControl = item.GetControl();
                if (customControl != null)
                {
                    viewbox.IsVisible = false;
                    Grid.SetRow(customControl, 1);
                    grid.Children.Add(customControl);
                }
            }
        }
        else
        {
            DebugLogger.LogError("PanelSpinner", $"GetImage is NULL for item: {item.Title}");
            SpriteImage.Source = null;
        }

        // Update button states - with circular navigation, buttons are always enabled if we have items
        PrevButton.IsEnabled = _items.Count > 1;
        NextButton.IsEnabled = _items.Count > 1;
    }

    private void UpdateDots()
    {
        // Clear existing dots
        DotsPanel.Children.Clear();

        // If more than 8 items, show page counter instead of dots
        if (_items.Count > 8)
        {
            var pageIndicator = new TextBlock
            {
                Text = $"{_currentIndex + 1}/{_items.Count}",
                FontFamily =
                    Application.Current?.Resources["BalatroFont"] as FontFamily
                    ?? FontFamily.Default,
                FontSize = 14,
                Foreground = Brushes.White,
                HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center,
            };
            DotsPanel.Children.Add(pageIndicator);
        }
        else
        {
            // Show dots for 8 or fewer items
            DotsPanel.Spacing = 4;

            for (int i = 0; i < _items.Count; i++)
            {
                var dot = new TextBlock();
                dot.Classes.Add("position-dot");

                if (i == _currentIndex)
                {
                    dot.Classes.Add("active");
                }

                DotsPanel.Children.Add(dot);
            }
        }
    }

    public void RefreshCurrentImage()
    {
        UpdateDisplay();
    }
}

public class PanelItem
{
    public required string Title { get; set; }
    public required string Description { get; set; }
    public required string Value { get; set; } // The actual value (e.g., "Red_Deck", "White_Stake")
    public Func<IImage?>? GetImage { get; set; }
    public Func<Control?>? GetControl { get; set; } // Optional custom control instead of image
}

// Helper factory for creating deck panel items
public static class PanelItemFactory
{
    public static List<PanelItem> CreateDeckItems()
    {
        var items = new List<PanelItem>();

        foreach (var deck in BalatroData.Decks)
        {
            var deckKey = deck.Key;
            items.Add(
                new PanelItem
                {
                    Title = deck.Value,
                    Description = BalatroData.DeckDescriptions.TryGetValue(deckKey, out var desc)
                        ? desc
                        : "",
                    Value = $"{deckKey}_Deck",
                    GetImage = () => SpriteService.Instance.GetDeckImage(deckKey),
                }
            );
        }

        return items;
    }

    // Create deck items with stake sticker composited on the card
    public static List<PanelItem> CreateDeckItemsWithStake(string stakeName)
    {
        ArgumentNullException.ThrowIfNull(stakeName);
        var items = new List<PanelItem>();

        foreach (var deck in BalatroData.Decks)
        {
            var deckKey = deck.Key;
            items.Add(
                new PanelItem
                {
                    Title = deck.Value,
                    Description = BalatroData.DeckDescriptions.TryGetValue(deckKey, out var desc)
                        ? desc
                        : "",
                    Value = $"{deckKey}_Deck",
                    GetImage = () =>
                        SpriteService.Instance.GetDeckWithStakeSticker(deckKey, stakeName),
                }
            );
        }

        return items;
    }

    public static List<PanelItem> CreateStakeItems()
    {
        var items = new List<PanelItem>
        {
            new PanelItem
            {
                Title = "White Stake",
                Description = "Base Difficulty",
                Value = "White_Stake",
                GetImage = () => SpriteService.Instance.GetStickerImage("WhiteStake"),
            },
            new PanelItem
            {
                Title = "Red Stake",
                Description = "Small Blind gives no money reward",
                Value = "Red_Stake",
                GetImage = () => SpriteService.Instance.GetStickerImage("RedStake"),
            },
            new PanelItem
            {
                Title = "Green Stake",
                Description = "Required score scales faster for each Ante",
                Value = "Green_Stake",
                GetImage = () => SpriteService.Instance.GetStickerImage("GreenStake"),
            },
            new PanelItem
            {
                Title = "Black Stake",
                Description = "Shop can have Eternal Jokers (Can't be sold or destroyed)",
                Value = "Black_Stake",
                GetImage = () => SpriteService.Instance.GetStickerImage("BlackStake"),
            },
            new PanelItem
            {
                Title = "Blue Stake",
                Description = "-1 Discard",
                Value = "Blue_Stake",
                GetImage = () => SpriteService.Instance.GetStickerImage("BlueStake"),
            },
            new PanelItem
            {
                Title = "Purple Stake",
                Description = "Required score scales faster for each Ante",
                Value = "Purple_Stake",
                GetImage = () => SpriteService.Instance.GetStickerImage("PurpleStake"),
            },
            new PanelItem
            {
                Title = "Orange Stake",
                Description = "Shop can have Perishable Jokers (Debuffed after 5 rounds)",
                Value = "Orange_Stake",
                GetImage = () => SpriteService.Instance.GetStickerImage("OrangeStake"),
            },
            new PanelItem
            {
                Title = "Gold Stake",
                Description = "Shop can have Rental Jokers (Costs $3 per round)",
                Value = "Gold_Stake",
                GetImage = () => SpriteService.Instance.GetStickerImage("GoldStake"),
            },
        };

        return items;
    }
}

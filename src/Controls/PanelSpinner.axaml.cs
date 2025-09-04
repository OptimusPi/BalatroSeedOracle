using System;
using System.Collections.Generic;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.LogicalTree;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using BalatroSeedOracle.Models;
using BalatroSeedOracle.Services;

namespace BalatroSeedOracle.Controls;

public partial class PanelSpinner : UserControl
{
    private Button? _prevButton;
    private Button? _nextButton;
    private TextBlock? _titleText;
    private TextBlock? _descriptionText;
    private Image? _spriteImage;
    private StackPanel? _dotsPanel;

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
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);

        _prevButton = this.FindControl<Button>("PrevButton");
        _nextButton = this.FindControl<Button>("NextButton");
        _titleText = this.FindControl<TextBlock>("TitleText");
        _descriptionText = this.FindControl<TextBlock>("DescriptionText");
        _spriteImage = this.FindControl<Image>("SpriteImage");
        _dotsPanel = this.FindControl<StackPanel>("DotsPanel");
    }

    private void OnPrevClick(object? sender, RoutedEventArgs e)
    {
        if (_items.Count == 0) return;
        
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
        if (_items.Count == 0) return;
        
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

        if (_titleText != null)
            _titleText.Text = item.Title;

        if (_descriptionText != null)
            _descriptionText.Text = item.Description;

        // Check if we should show a custom control or an image
        if (_spriteImage != null)
        {
            var viewbox = _spriteImage.Parent as Viewbox;
            if (viewbox?.Parent is Grid grid)
            {
                var customControlsInImageRow = grid.Children
                    .Where(child => Grid.GetRow(child) == 1 && child != viewbox)
                    .ToList();
                
                foreach (var child in customControlsInImageRow)
                {
                    grid.Children.Remove(child);
                }
                
                // Only show custom control if this specific item has one AND it's the special create item
                if (item.GetControl != null && item.Value == "__CREATE_NEW__")
                {
                    // Show custom control instead of image
                    var customControl = item.GetControl();
                    if (customControl != null)
                    {
                        viewbox.IsVisible = false;
                        Grid.SetRow(customControl, 1);
                        grid.Children.Add(customControl);
                    }
                }
                else
                {
                    // Show image for regular filters
                    viewbox.IsVisible = true;
                    if (item.GetImage != null)
                    {
                        var image = item.GetImage();
                        _spriteImage.Source = image;
                    }
                    else
                    {
                        _spriteImage.Source = null;
                    }
                }
            }
        }

        // Update button states - with circular navigation, buttons are always enabled if we have items
        if (_prevButton != null)
            _prevButton.IsEnabled = _items.Count > 1;

        if (_nextButton != null)
            _nextButton.IsEnabled = _items.Count > 1;
    }

    private void UpdateDots()
    {
        if (_dotsPanel == null)
            return;

        // Clear existing dots
        _dotsPanel.Children.Clear();

        // Adjust dot spacing based on number of items
        bool manyItems = _items.Count > 10;
        if (manyItems)
        {
            _dotsPanel.Spacing = 2; // Closer together
        }
        else
        {
            _dotsPanel.Spacing = 4; // Normal spacing
        }

        // Create new dots
        for (int i = 0; i < _items.Count; i++)
        {
            var dot = new TextBlock();
            dot.Classes.Add("position-dot");
            
            // Make dots smaller for many items
            if (manyItems)
            {
                dot.Classes.Add("compact");
            }

            if (i == _currentIndex)
            {
                dot.Classes.Add("active");
            }

            _dotsPanel.Children.Add(dot);
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

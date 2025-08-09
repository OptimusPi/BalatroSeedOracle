using System;
using System.Collections.Generic;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Oracle.Controls;
using Oracle.Services;
using Oracle.Helpers;

namespace Oracle.Components;

public partial class DeckSpinner : UserControl
{
    private PanelSpinner? _panelSpinner;
    private readonly SpriteService _spriteService;
    
    // Deck data - same as DeckStakeSelector
    private readonly List<(string name, string description, string spriteName)> _decks = new()
    {
        ("Red", "+1 Discard\nevery round", "red"),
        ("Blue", "+1 Hand\nevery round", "blue"),
        ("Yellow", "+$10 at\nstart of run", "yellow"),
        ("Green", "At end of each Round:\n+$1 interest per $5\n(max $5 interest)", "green"),
        ("Black", "+1 Joker slot\n-1 Hand every round", "black"),
        ("Magic", "Start run with the\nCrystal Ball voucher\nand 2 copies of The Fool", "magic"),
        ("Nebula", "Start run with the\nTelescope voucher\n-1 consumable slot", "nebula"),
        ("Ghost", "Spectral cards may\nappear in the shop\nstart with a Hex", "ghost"),
        ("Abandoned", "Start with no\nFace Cards\nin your deck", "abandoned"),
        ("Checkered", "Start with 26 Spades\nand 26 Hearts\nin deck", "checkered"),
        ("Zodiac", "Start run with\nTarot Merchant,\nPlanet Merchant,\nand Overstock", "zodiac"),
        ("Painted", "+2 Hand Size\n-1 Joker Slot", "painted"),
        ("Anaglyph", "After defeating each\nBoss Blind, gain a\nDouble Tag", "anaglyph"),
        ("Plasma", "Balance Chips and\nMult when calculating\nscore for played hand", "plasma"),
        ("Erratic", "All Ranks and Suits\nin deck are randomized", "erratic")
    };
    
    public event EventHandler<int>? DeckChanged;
    
    public DeckSpinner()
    {
        InitializeComponent();
        _spriteService = ServiceHelper.GetRequiredService<SpriteService>();
    }
    
    protected override void OnLoaded(RoutedEventArgs e)
    {
        base.OnLoaded(e);
        
        _panelSpinner = this.FindControl<PanelSpinner>("InnerPanelSpinner");
        if (_panelSpinner != null)
        {
            // Create panel items from deck data
            var items = _decks.Select((deck, index) => new PanelItem
            {
                Title = deck.name + " Deck",
                Description = deck.description,
                Value = deck.spriteName,
                GetImage = () => _spriteService.GetDeckImage(deck.spriteName)
            }).ToList();
            
            _panelSpinner.Items = items;
            _panelSpinner.SelectionChanged += OnDeckSelectionChanged;
        }
    }
    
    private void OnDeckSelectionChanged(object? sender, PanelItem? item)
    {
        if (_panelSpinner != null)
        {
            DeckChanged?.Invoke(this, _panelSpinner.SelectedIndex);
        }
    }
    
    public int SelectedDeckIndex
    {
        get => _panelSpinner?.SelectedIndex ?? 0;
        set
        {
            if (_panelSpinner != null)
            {
                _panelSpinner.SelectedIndex = value;
            }
        }
    }
    
    public string SelectedDeckName => _decks[SelectedDeckIndex].name;
    public string SelectedDeckSpriteName => _decks[SelectedDeckIndex].spriteName;
}
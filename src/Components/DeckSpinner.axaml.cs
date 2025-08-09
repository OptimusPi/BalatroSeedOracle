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
        ("Red", "+1 Discardevery round", "red"),
        ("Blue", "+1 Handevery round", "blue"),
        ("Yellow", "+$10 atstart of run", "yellow"),
        ("Green", "At end of each Round:+$1 interest per $5(max $5 interest)", "green"),
        ("Black", "+1 Joker slot-1 Hand every round", "black"),
        ("Magic", "Start run with theCrystal Ball voucherand 2 copies of The Fool", "magic"),
        ("Nebula", "Start run with theTelescope voucher-1 consumable slot", "nebula"),
        ("Ghost", "Spectral cards mayappear in the shopstart with a Hex", "ghost"),
        ("Abandoned", "Start with noFace Cardsin your deck", "abandoned"),
        ("Checkered", "Start with 26 Spadesand 26 Heartsin deck", "checkered"),
        ("Zodiac", "Start run withTarot Merchant,Planet Merchant,and Overstock", "zodiac"),
        ("Painted", "+2 Hand Size-1 Joker Slot", "painted"),
        ("Anaglyph", "After defeating eachBoss Blind, gain aDouble Tag", "anaglyph"),
        ("Plasma", "Balance Chips andMult when calculatingscore for played hand", "plasma"),
        ("Erratic", "All Ranks and Suitsin deck are randomized", "erratic")
    };
    
    public event EventHandler<int>? DeckChanged;
    
    private int _currentStakeIndex = 0;
    
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
                GetImage = () => GetDeckImageWithStake(deck.spriteName)
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
    
    public void SetStakeIndex(int stakeIndex)
    {
        _currentStakeIndex = stakeIndex;
        // Refresh the current deck image to show with new stake
        if (_panelSpinner != null)
        {
            _panelSpinner.RefreshCurrentImage();
        }
    }
    
    private IImage? GetDeckImageWithStake(string deckSpriteName)
    {
        string stakeName = GetStakeName(_currentStakeIndex);
        var compositeImage = _spriteService.GetDeckWithStakeSticker(deckSpriteName, stakeName);
        return compositeImage ?? _spriteService.GetDeckImage(deckSpriteName);
    }
    
    private string GetStakeName(int index)
    {
        return index switch
        {
            0 => "white",
            1 => "red",
            2 => "green", 
            3 => "black",
            4 => "blue",
            5 => "purple",
            6 => "orange",
            7 => "gold",
            _ => "white"
        };
    }
}
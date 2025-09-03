using System;
using System.Collections.Generic;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;

namespace BalatroSeedOracle.Components;

public partial class BalatroStyleDeckStakeSelector : UserControl
{
    private readonly List<string> _deckNames = new()
    {
        "Red Deck", "Blue Deck", "Yellow Deck", "Green Deck", "Black Deck",
        "Magic Deck", "Nebula Deck", "Ghost Deck", "Abandoned Deck", 
        "Checkered Deck", "Zodiac Deck", "Painted Deck", "Anaglyph Deck", 
        "Plasma Deck", "Erratic Deck"
    };
    
    private readonly List<string> _deckEffects = new()
    {
        "+1 Discard every round",
        "+1 Hand every round", 
        "Start with extra $10",
        "+1 Hand size",
        "+1 Joker slot",
        "Start with The Fool",
        "Start with 8 Planets",
        "No face cards in deck",
        "Start with no shop",
        "Start with Checkered deck",
        "Start with Tarot Merchant",
        "Start with Painted deck",
        "Start with Anaglyph deck",
        "Start with Plasma deck", 
        "Start with Erratic deck"
    };
    
    private readonly List<string> _stakeNames = new()
    {
        "White Stake", "Red Stake", "Green Stake", "Black Stake",
        "Blue Stake", "Purple Stake", "Orange Stake", "Gold Stake"
    };

    private readonly List<string> _stakeEffects = new()
    {
        "Base Difficulty",
        "Small Blind gives no reward money",
        "Required score scales faster",
        "Shop has extra card slot", 
        "Discards cost $1 each",
        "Required score scales faster",
        "Eternal Jokers can appear",
        "Store prices are doubled"
    };

    private int _currentDeckIndex = 0;
    private int _currentStakeIndex = 0;

    // Control references
    private TextBlock? _deckNameText;
    private TextBlock? _deckEffectText;
    private Image? _deckImage;
    private StackPanel? _deckPaginationPanel;
    private Button? _deckPrevButton;
    private Button? _deckNextButton;
    
    private TextBlock? _stakeNameText;
    private StackPanel? _stakePaginationPanel;
    private Button? _stakePrevButton;
    private Button? _stakeNextButton;
    
    private Button? _selectButton;

    public event EventHandler<(int deckIndex, int stakeIndex)>? SelectionChanged;
    public event EventHandler? DeckSelected;

    public BalatroStyleDeckStakeSelector()
    {
        InitializeComponent();
        this.Focusable = true;
        this.KeyDown += OnKeyDown;
    }
    
    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
        
        // Find controls
        _deckNameText = this.FindControl<TextBlock>("DeckNameText");
        _deckEffectText = this.FindControl<TextBlock>("DeckEffectText");
        _deckImage = this.FindControl<Image>("DeckImage");
        _deckPaginationPanel = this.FindControl<StackPanel>("DeckPaginationPanel");
        _deckPrevButton = this.FindControl<Button>("DeckPrevButton");
        _deckNextButton = this.FindControl<Button>("DeckNextButton");
        
        _stakeNameText = this.FindControl<TextBlock>("StakeNameText");
        _stakePaginationPanel = this.FindControl<StackPanel>("StakePaginationPanel");
        _stakePrevButton = this.FindControl<Button>("StakePrevButton");
        _stakeNextButton = this.FindControl<Button>("StakeNextButton");
        
        _selectButton = this.FindControl<Button>("SelectButton");

        Loaded += OnLoaded;
    }

    private void OnLoaded(object? _, RoutedEventArgs __)
    {
        UpdateDisplay();
    }

    private void UpdateDisplay()
    {
        // Update deck display
        if (_deckNameText != null)
            _deckNameText.Text = _deckNames[_currentDeckIndex];
            
        if (_deckEffectText != null)
            _deckEffectText.Text = _deckEffects[_currentDeckIndex];
            
        // Update stake display  
        if (_stakeNameText != null)
            _stakeNameText.Text = _stakeNames[_currentStakeIndex];
            
        UpdatePagination();
        UpdateButtons();
        
        // Notify of selection change
        SelectionChanged?.Invoke(this, (_currentDeckIndex, _currentStakeIndex));
    }
    
    private void UpdatePagination()
    {
        // Update deck pagination
        if (_deckPaginationPanel != null)
        {
            _deckPaginationPanel.Children.Clear();
            
            for (int i = 0; i < _deckNames.Count; i++)
            {
                var dot = new Border
                {
                    Classes = { "pagination-dot" }
                };
                
                if (i == _currentDeckIndex)
                    dot.Classes.Add("active");
                    
                _deckPaginationPanel.Children.Add(dot);
            }
        }
        
        // Update stake pagination
        if (_stakePaginationPanel != null)
        {
            _stakePaginationPanel.Children.Clear();
            
            for (int i = 0; i < _stakeNames.Count; i++)
            {
                var dot = new Border
                {
                    Classes = { "pagination-dot" }
                };
                
                if (i == _currentStakeIndex)
                    dot.Classes.Add("active");
                    
                _stakePaginationPanel.Children.Add(dot);
            }
        }
    }
    
    private void UpdateButtons()
    {
        if (_deckPrevButton != null)
            _deckPrevButton.IsEnabled = _currentDeckIndex > 0;
            
        if (_deckNextButton != null)
            _deckNextButton.IsEnabled = _currentDeckIndex < _deckNames.Count - 1;
            
        if (_stakePrevButton != null)
            _stakePrevButton.IsEnabled = _currentStakeIndex > 0;
            
        if (_stakeNextButton != null)
            _stakeNextButton.IsEnabled = _currentStakeIndex < _stakeNames.Count - 1;
    }
    
    // Event handlers
    private void OnDeckPrevClick(object? _, RoutedEventArgs __)
    {
        if (_currentDeckIndex > 0)
        {
            _currentDeckIndex--;
            UpdateDisplay();
        }
    }
    
    private void OnDeckNextClick(object? _, RoutedEventArgs __)
    {
        if (_currentDeckIndex < _deckNames.Count - 1)
        {
            _currentDeckIndex++;
            UpdateDisplay();
        }
    }
    
    private void OnStakePrevClick(object? _, RoutedEventArgs __)
    {
        if (_currentStakeIndex > 0)
        {
            _currentStakeIndex--;
            UpdateDisplay();
        }
    }
    
    private void OnStakeNextClick(object? _, RoutedEventArgs __)
    {
        if (_currentStakeIndex < _stakeNames.Count - 1)
        {
            _currentStakeIndex++;
            UpdateDisplay();
        }
    }
    
    private void OnSelectClick(object? sender, RoutedEventArgs e)
    {
        DeckSelected?.Invoke(this, EventArgs.Empty);
    }
    
    private void OnKeyDown(object? sender, KeyEventArgs e)
    {
        switch (e.Key)
        {
            case Key.Left:
                if (e.KeyModifiers == KeyModifiers.Control)
                    OnStakePrevClick(null, null!);
                else
                    OnDeckPrevClick(null, null!);
                e.Handled = true;
                break;
                
            case Key.Right:
                if (e.KeyModifiers == KeyModifiers.Control)
                    OnStakeNextClick(null, null!);
                else
                    OnDeckNextClick(null, null!);
                e.Handled = true;
                break;
            
            case Key.Enter:
                OnSelectClick(null, null!);
                e.Handled = true;
                break;
        }
    }

    // Public properties for external access
    public int DeckIndex => _currentDeckIndex;
    public int StakeIndex => _currentStakeIndex;
    public string SelectedDeckName => _deckNames[_currentDeckIndex];
    public string SelectedStakeName => _stakeNames[_currentStakeIndex];

    public void SetDeck(int index)
    {
        if (index >= 0 && index < _deckNames.Count)
        {
            _currentDeckIndex = index;
            UpdateDisplay();
        }
    }

    public void SetDeck(string deckName)
    {
        var index = GetDeckIndex(deckName);
        if (index >= 0)
        {
            _currentDeckIndex = index;
            UpdateDisplay();
        }
    }

    public void SetStake(int index)
    {
        if (index >= 0 && index < _stakeNames.Count)
        {
            _currentStakeIndex = index;
            UpdateDisplay();
        }
    }

    public void SetStake(string stakeName)
    {
        var index = GetStakeIndex(stakeName);
        if (index >= 0)
        {
            _currentStakeIndex = index;
            UpdateDisplay();
        }
    }

    private int GetDeckIndex(string deckName)
    {
        // Handle various deck name formats
        var cleanName = deckName?.Trim().Replace(" Deck", "").Replace("deck", "").Trim();
        
        for (int i = 0; i < _deckNames.Count; i++)
        {
            var deck = _deckNames[i].Replace(" Deck", "").Trim();
            if (string.Equals(deck, cleanName, StringComparison.OrdinalIgnoreCase))
                return i;
        }
        
        // Default to Red deck if not found
        return 0;
    }
    
    private int GetStakeIndex(string stakeName)
    {
        // Handle various stake name formats  
        var cleanName = stakeName?.Trim().Replace(" Stake", "").Replace("stake", "").Trim();
        
        for (int i = 0; i < _stakeNames.Count; i++)
        {
            var stake = _stakeNames[i].Replace(" Stake", "").Trim();
            if (string.Equals(stake, cleanName, StringComparison.OrdinalIgnoreCase))
                return i;
        }
        
        // Default to White stake if not found
        return 0;
    }
}

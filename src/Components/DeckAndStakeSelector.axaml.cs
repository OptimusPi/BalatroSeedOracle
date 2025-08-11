using System;
using Avalonia.Controls;
using Avalonia.Interactivity;
using BalatroSeedOracle.Controls;

namespace BalatroSeedOracle.Components;

public partial class DeckAndStakeSelector : UserControl
{
    private DeckSpinner? _deckSpinner;
    private SpinnerControl? _stakeSpinner;
    private Button? _selectButton;

    public event EventHandler<(int deckIndex, int stakeIndex)>? SelectionChanged;
    public event EventHandler? DeckSelected;

    public DeckAndStakeSelector()
    {
        InitializeComponent();
    }

    protected override void OnLoaded(RoutedEventArgs e)
    {
        base.OnLoaded(e);

        _deckSpinner = this.FindControl<DeckSpinner>("DeckSpinnerControl");
        _stakeSpinner = this.FindControl<SpinnerControl>("StakeSpinner");
        _selectButton = this.FindControl<Button>("SelectButton");
        
        // Configure stake spinner display values
        if (_stakeSpinner != null)
        {
            _stakeSpinner.DisplayValues = new[] 
            { 
                "White Stake",
                "Red Stake",
                "Green Stake", 
                "Black Stake",
                "Blue Stake",
                "Purple Stake",
                "Orange Stake",
                "Gold Stake"
            };
        }

        if (_deckSpinner != null)
        {
            _deckSpinner.DeckChanged += (s, deckIndex) =>
            {
                SelectionChanged?.Invoke(this, (deckIndex, StakeIndex));
            };
        }

        if (_stakeSpinner != null)
        {
            _stakeSpinner.ValueChanged += (s, e) =>
            {
                // Update deck spinner to show new stake
                _deckSpinner?.SetStakeIndex(StakeIndex);
                SelectionChanged?.Invoke(this, (DeckIndex, StakeIndex));
            };
        }
    }

    private void OnSelectClick(object? sender, RoutedEventArgs e)
    {
        // Fire the DeckSelected event to notify the parent modal
        DeckSelected?.Invoke(this, EventArgs.Empty);
    }

    public int DeckIndex
    {
        get => _deckSpinner?.SelectedDeckIndex ?? 0;
        set
        {
            if (_deckSpinner != null)
            {
                _deckSpinner.SelectedDeckIndex = value;
            }
        }
    }

    public int StakeIndex
    {
        get => (int)(_stakeSpinner?.Value ?? 0);
        set
        {
            if (_stakeSpinner != null)
            {
                _stakeSpinner.Value = value;
            }
        }
    }

    public string SelectedDeckName => _deckSpinner?.SelectedDeckName ?? "Red";
    public string SelectedStakeName => GetStakeName(StakeIndex);

    private string GetStakeName(int index)
    {
        return index switch
        {
            0 => "White",
            1 => "Red",
            2 => "Green",
            3 => "Black",
            4 => "Blue",
            5 => "Purple",
            6 => "Orange",
            7 => "Gold",
            _ => "White",
        };
    }

    public void SetDeck(string deckName)
    {
        int index = deckName?.ToLower() switch
        {
            "red" => 0,
            "blue" => 1,
            "yellow" => 2,
            "green" => 3,
            "black" => 4,
            "magic" => 5,
            "nebula" => 6,
            "ghost" => 7,
            "abandoned" => 8,
            "checkered" => 9,
            "zodiac" => 10,
            "painted" => 11,
            "anaglyph" => 12,
            "plasma" => 13,
            "erratic" => 14,
            _ => 0, // Default to Red
        };
        DeckIndex = index;
    }

    public void SetStake(string stakeName)
    {
        int index = stakeName?.ToLower() switch
        {
            "white" => 0,
            "red" => 1,
            "green" => 2,
            "black" => 3,
            "blue" => 4,
            "purple" => 5,
            "orange" => 6,
            "gold" => 7,
            _ => 0, // Default to White
        };
        StakeIndex = index;
    }
}

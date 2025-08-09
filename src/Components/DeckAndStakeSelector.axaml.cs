using System;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Oracle.Controls;

namespace Oracle.Components;

public partial class DeckAndStakeSelector : UserControl
{
    private DeckSpinner? _deckSpinner;
    private SpinnerControl? _stakeSpinner;
    
    public event EventHandler<(int deckIndex, int stakeIndex)>? SelectionChanged;
    
    public DeckAndStakeSelector()
    {
        InitializeComponent();
    }
    
    protected override void OnLoaded(RoutedEventArgs e)
    {
        base.OnLoaded(e);
        
        _deckSpinner = this.FindControl<DeckSpinner>("DeckSpinnerControl");
        _stakeSpinner = this.FindControl<SpinnerControl>("StakeSpinner");
        
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
                SelectionChanged?.Invoke(this, (DeckIndex, StakeIndex));
            };
        }
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
            _ => "White"
        };
    }
}
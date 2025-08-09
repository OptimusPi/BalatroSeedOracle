using System;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Oracle.Controls;

namespace Oracle.Components;

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
            _ => "White"
        };
    }
}
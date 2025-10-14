using System;
using Avalonia.Controls;
using Avalonia.Interactivity;
using BalatroSeedOracle.Controls;
using BalatroSeedOracle.ViewModels;

namespace BalatroSeedOracle.Components;

public partial class DeckAndStakeSelector : UserControl
{
    private DeckSpinner? _deckSpinner;
    private SpinnerControl? _stakeSpinner;
    private DeckAndStakeSelectorViewModel? _viewModel;

    public event EventHandler<(int deckIndex, int stakeIndex)>? SelectionChanged;
    public event EventHandler? DeckSelected;

    public DeckAndStakeSelector()
    {
        InitializeComponent();
        InitializeViewModel();
    }

    private void InitializeViewModel()
    {
        _viewModel = new DeckAndStakeSelectorViewModel();
        DataContext = _viewModel;

        // Forward ViewModel events to maintain API compatibility
        _viewModel.SelectionChanged += (s, e) => SelectionChanged?.Invoke(this, e);
        _viewModel.DeckSelected += (s, e) => DeckSelected?.Invoke(this, e);
    }

    protected override void OnLoaded(RoutedEventArgs e)
    {
        base.OnLoaded(e);

        _deckSpinner = this.FindControl<DeckSpinner>("DeckSpinnerControl");
        _stakeSpinner = this.FindControl<SpinnerControl>("StakeSpinner");
        // Ensure sane initial indices
        if (_viewModel != null)
        {
            if (_viewModel.DeckIndex < 0) _viewModel.DeckIndex = 0;
            if (_viewModel.StakeIndex < 0) _viewModel.StakeIndex = 0;
        }
        // Pure MVVM: bindings handle synchronization; no manual event hookups required
    }

    // Public API - delegates to ViewModel
    public int DeckIndex
    {
        get => _viewModel?.DeckIndex ?? 0;
        set
        {
            if (_viewModel != null)
            {
                _viewModel.DeckIndex = value;
            }
        }
    }

    public int StakeIndex
    {
        get => _viewModel?.StakeIndex ?? 0;
        set
        {
            if (_viewModel != null)
            {
                _viewModel.StakeIndex = value;
            }
        }
    }

    public string SelectedDeckName => _viewModel?.SelectedDeckName ?? "Red";
    public string SelectedStakeName => _viewModel?.SelectedStakeName ?? "White";

    public void SetDeck(string deckName)
    {
        _viewModel?.SetDeck(deckName);
    }

    public void SetStake(string stakeName)
    {
        _viewModel?.SetStake(stakeName);
    }
}

using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Media;
using BalatroSeedOracle.Controls;
using BalatroSeedOracle.Helpers;
using BalatroSeedOracle.Services;
using BalatroSeedOracle.ViewModels;
using CommunityToolkit.Mvvm.Input;
using Motely;

namespace BalatroSeedOracle.Components;

public partial class DeckAndStakeSelector : UserControl
{
    private readonly DeckAndStakeSelectorViewModel _viewModel;

    // Events mirror the inner ViewModel for external subscribers
    public event EventHandler<(int deckIndex, int stakeIndex)>? SelectionChanged;
    public event EventHandler? DeckSelected;

    public DeckAndStakeSelector()
    {
        InitializeComponent();
        // Create internal ViewModel and set DataContext to the ViewModel for pure MVVM binding
        _viewModel = new DeckAndStakeSelectorViewModel(ServiceHelper.GetRequiredService<SpriteService>());
        DataContext = _viewModel;
        // Initialize styled properties from ViewModel
        DeckIndex = _viewModel.DeckIndex;
        StakeIndex = _viewModel.StakeIndex;
        DeckImage = _viewModel.DeckImage;
        StakeImage = _viewModel.StakeImage;
        // Subscribe to ViewModel property changes to keep exposed styled properties in sync (compatibility)
        // Use SetCurrentValue to properly notify TwoWay bindings when ViewModel changes
        _viewModel.PropertyChanged += (s, e) =>
        {
            if (e.PropertyName == nameof(_viewModel.DeckIndex))
            {
                // Clamp to valid range before setting
                var clampedValue = Math.Max(0, Math.Min(14, _viewModel.DeckIndex));
                SetCurrentValue(DeckIndexProperty, clampedValue);
            }
            if (e.PropertyName == nameof(_viewModel.StakeIndex))
            {
                // Clamp to valid range before setting
                var clampedValue = Math.Max(0, Math.Min(7, _viewModel.StakeIndex));
                SetCurrentValue(StakeIndexProperty, clampedValue);
            }
            if (e.PropertyName == nameof(_viewModel.DeckImage))
                DeckImage = _viewModel.DeckImage;
            if (e.PropertyName == nameof(_viewModel.StakeImage))
                StakeImage = _viewModel.StakeImage;
        };

        // Forward inner ViewModel events to control-level events for compatibility
        _viewModel.SelectionChanged += (s, selection) => SelectionChanged?.Invoke(this, selection);
        _viewModel.DeckSelected += (s, ea) => DeckSelected?.Invoke(this, ea);
    }

    // InitializeViewModel removed; initialization happens in constructor to avoid reassigning readonly field

    protected override void OnLoaded(RoutedEventArgs e)
    {
        base.OnLoaded(e);

        // Ensure sane initial indices
        if (_viewModel != null)
        {
            if (_viewModel.DeckIndex < 0)
                _viewModel.DeckIndex = 0;
            if (_viewModel.StakeIndex < 0)
                _viewModel.StakeIndex = 0;
        }
        // Pure MVVM: bindings handle synchronization; no manual event hookups required
    }

    // DeckIndex property for MVVM two-way binding
    public static readonly StyledProperty<int> DeckIndexProperty = AvaloniaProperty.Register<DeckAndStakeSelector, int>(
        nameof(DeckIndex),
        defaultBindingMode: Avalonia.Data.BindingMode.TwoWay
    );
    public int DeckIndex
    {
        get => GetValue(DeckIndexProperty);
        set
        {
            // Clamp to valid range (0-14 for decks)
            var clampedValue = Math.Max(0, Math.Min(14, value));
            // CRITICAL FIX: Use SetCurrentValue to properly notify TwoWay bindings
            SetCurrentValue(DeckIndexProperty, clampedValue);
            _viewModel.DeckIndex = clampedValue;
        }
    }

    // StakeIndex property for MVVM two-way binding
    public static readonly StyledProperty<int> StakeIndexProperty = AvaloniaProperty.Register<
        DeckAndStakeSelector,
        int
    >(nameof(StakeIndex), defaultBindingMode: Avalonia.Data.BindingMode.TwoWay);
    public int StakeIndex
    {
        get => GetValue(StakeIndexProperty);
        set
        {
            // Clamp to valid range (0-7 for stakes)
            var clampedValue = Math.Max(0, Math.Min(7, value));
            // CRITICAL FIX: Use SetCurrentValue to properly notify TwoWay bindings
            SetCurrentValue(StakeIndexProperty, clampedValue);
            _viewModel.StakeIndex = clampedValue;
        }
    }

    // Expose ViewModel-selected image paths as IImage for binding to Image.Source
    public static readonly StyledProperty<IImage?> DeckImageProperty = AvaloniaProperty.Register<
        DeckAndStakeSelector,
        IImage?
    >(nameof(DeckImage));
    public IImage? DeckImage
    {
        get => GetValue(DeckImageProperty);
        private set => SetValue(DeckImageProperty, value);
    }
    public static readonly StyledProperty<IImage?> StakeImageProperty = AvaloniaProperty.Register<
        DeckAndStakeSelector,
        IImage?
    >(nameof(StakeImage));
    public IImage? StakeImage
    {
        get => GetValue(StakeImageProperty);
        private set => SetValue(StakeImageProperty, value);
    }

    // Commands for navigation and selection (expose as ICommand for broader compatibility)
    public System.Windows.Input.ICommand PreviousDeckCommand => _viewModel.PreviousDeckCommand;
    public System.Windows.Input.ICommand NextDeckCommand => _viewModel.NextDeckCommand;
    public System.Windows.Input.ICommand PreviousStakeCommand => _viewModel.PreviousStakeCommand;
    public System.Windows.Input.ICommand NextStakeCommand => _viewModel.NextStakeCommand;
    public System.Windows.Input.ICommand SelectCommand => _viewModel.SelectCommand;

    public void SetStake(string stakeName)
    {
        _viewModel?.SetStake(stakeName);
    }

    public void SetDeck(string deckName)
    {
        _viewModel?.SetDeck(deckName);
    }

    // Forward selected names
    public string SelectedDeckName => _viewModel.SelectedDeckName;
    public string SelectedStakeName => _viewModel.SelectedStakeName;

    // Forward strongly-typed enum selections
    public MotelyDeck SelectedDeck => _viewModel.SelectedDeck;
    public MotelyStake SelectedStake => _viewModel.SelectedStake;
}

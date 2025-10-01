using System;
using System.Linq;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using BalatroSeedOracle.ViewModels;
using Motely;

namespace BalatroSeedOracle.Features.Analyzer;

public partial class AnalyzerView : UserControl
{
    private AnalyzerViewModel? ViewModel => DataContext as AnalyzerViewModel;

    public AnalyzerView()
    {
        InitializeComponent();

        // Set up hotkeys
        this.KeyDown += OnKeyDown;

        // Wire up deck/stake selectors
        var deckCombo = this.FindControl<ComboBox>("DeckComboBox");
        var stakeCombo = this.FindControl<ComboBox>("StakeComboBox");

        if (deckCombo != null)
        {
            deckCombo.SelectionChanged += DeckComboBox_SelectionChanged;
        }

        if (stakeCombo != null)
        {
            stakeCombo.SelectionChanged += StakeComboBox_SelectionChanged;
        }

        // Make control focusable for hotkeys
        this.Focusable = true;
        this.Loaded += (s, e) => this.Focus();
    }

    private void OnKeyDown(object? sender, KeyEventArgs e)
    {
        if (ViewModel == null) return;

        switch (e.Key)
        {
            case Key.PageUp:
                ViewModel.PreviousResultCommand.Execute(null);
                e.Handled = true;
                break;

            case Key.PageDown:
                ViewModel.NextResultCommand.Execute(null);
                e.Handled = true;
                break;

            case Key.Up:
                ViewModel.ScrollUpAnteCommand.Execute(null);
                e.Handled = true;
                break;

            case Key.Down:
                ViewModel.ScrollDownAnteCommand.Execute(null);
                e.Handled = true;
                break;
        }
    }

    private void DeckComboBox_SelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (ViewModel == null || e.AddedItems.Count == 0) return;

        var deckCombo = sender as ComboBox;
        if (deckCombo == null) return;

        // Map combo box index to MotelyDeck enum
        var deckNames = new[]
        {
            MotelyDeck.Red,
            MotelyDeck.Blue,
            MotelyDeck.Yellow,
            MotelyDeck.Green,
            MotelyDeck.Black,
            MotelyDeck.Magic,
            MotelyDeck.Nebula,
            MotelyDeck.Ghost,
            MotelyDeck.Abandoned,
            MotelyDeck.Checkered,
            MotelyDeck.Zodiac,
            MotelyDeck.Painted,
            MotelyDeck.Anaglyph,
            MotelyDeck.Plasma,
            MotelyDeck.Erratic
        };

        if (deckCombo.SelectedIndex >= 0 && deckCombo.SelectedIndex < deckNames.Length)
        {
            ViewModel.SelectedDeck = deckNames[deckCombo.SelectedIndex];
        }
    }

    private void StakeComboBox_SelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (ViewModel == null || e.AddedItems.Count == 0) return;

        var stakeCombo = sender as ComboBox;
        if (stakeCombo == null) return;

        // Map combo box index to MotelyStake enum
        var stakeNames = new[]
        {
            MotelyStake.White,
            MotelyStake.Red,
            MotelyStake.Green,
            MotelyStake.Black,
            MotelyStake.Blue,
            MotelyStake.Purple,
            MotelyStake.Orange,
            MotelyStake.Gold
        };

        if (stakeCombo.SelectedIndex >= 0 && stakeCombo.SelectedIndex < stakeNames.Length)
        {
            ViewModel.SelectedStake = stakeNames[stakeCombo.SelectedIndex];
        }
    }
}

using System;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Input;
using BalatroSeedOracle.Helpers;
using BalatroSeedOracle.ViewModels;

namespace BalatroSeedOracle.Features.Analyzer;

/// <summary>
/// Analyzer view - displays seed analysis details.
/// All rendering via compiled XAML bindings to AnalyzerViewModel.
/// </summary>
public partial class AnalyzerView : UserControl
{
    private AnalyzerViewModel? ViewModel => DataContext as AnalyzerViewModel;

    public AnalyzerView()
    {
        DebugLogger.Log("AnalyzerView", "Constructor called!");
        InitializeComponent();

        // Set up hotkeys
        this.KeyDown += OnKeyDown;

        // Make control focusable for hotkeys
        this.Focusable = true;
        this.Loaded += (s, e) => this.Focus();

        // Wire up clipboard event
        this.DataContextChanged += OnDataContextChanged;
    }

    private void OnDataContextChanged(object? sender, EventArgs e)
    {
        if (ViewModel != null)
        {
            ViewModel.CopyToClipboardRequested += async (s, text) =>
                await CopyToClipboardAsync(text);
        }
    }

    public async Task CopyToClipboardAsync(string text)
    {
        try
        {
            var topLevel = TopLevel.GetTopLevel(this);
            if (topLevel?.Clipboard != null)
            {
                await topLevel.Clipboard.SetTextAsync(text);
                DebugLogger.Log("AnalyzerView", $"Copied to clipboard: {text}");
            }
        }
        catch (Exception ex)
        {
            DebugLogger.LogError("AnalyzerView", $"Failed to copy to clipboard: {ex.Message}");
        }
    }

    private void OnKeyDown(object? sender, KeyEventArgs e)
    {
        if (ViewModel == null)
            return;

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
}

using System;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Input;
using BalatroSeedOracle.Helpers;
using BalatroSeedOracle.Services;
using BalatroSeedOracle.ViewModels;
using Motely;

namespace BalatroSeedOracle.Features.Analyzer;

public partial class AnalyzerView : UserControl
{
    private AnalyzerViewModel? ViewModel => DataContext as AnalyzerViewModel;

    public AnalyzerView()
    {
        Helpers.DebugLogger.Log("AnalyzerView", "Constructor called!");
        InitializeComponent();

        // Set up hotkeys
        this.KeyDown += OnKeyDown;

        // Make control focusable for hotkeys
        this.Focusable = true;
        this.Loaded += (s, e) => this.Focus();

        // Wire up data context change to render images
        this.DataContextChanged += OnDataContextChanged;

        // Wire up clipboard event
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
                Helpers.DebugLogger.Log("AnalyzerView", $"Copied to clipboard: {text}");
            }
        }
        catch (Exception ex)
        {
            Helpers.DebugLogger.LogError(
                "AnalyzerView",
                $"Failed to copy to clipboard: {ex.Message}"
            );
        }
    }

    private void OnDataContextChanged(object? sender, EventArgs e)
    {
        Helpers.DebugLogger.Log(
            "AnalyzerView",
            $"DataContext changed, ViewModel is {(ViewModel == null ? "NULL" : "SET")}"
        );
        if (ViewModel != null)
        {
            ViewModel.PropertyChanged += ViewModel_PropertyChanged;
            Helpers.DebugLogger.Log("AnalyzerView", "Subscribed to ViewModel.PropertyChanged");
        }
    }

    private void ViewModel_PropertyChanged(
        object? sender,
        System.ComponentModel.PropertyChangedEventArgs e
    )
    {
        Helpers.DebugLogger.Log("AnalyzerView", $"Property changed: {e.PropertyName}");
        // Re-render images when ante changes or analysis updates
        if (
            e.PropertyName == nameof(AnalyzerViewModel.CurrentBoss)
            || e.PropertyName == nameof(AnalyzerViewModel.CurrentVoucher)
        )
        {
            Helpers.DebugLogger.Log("AnalyzerView", "Triggering RenderImages()");
            RenderImages();
        }
    }

    private void RenderImages()
    {
        Helpers.DebugLogger.Log("AnalyzerView", "RenderImages() called");
        RenderBossImage();
        RenderVoucherImage();
    }

    private void RenderBossImage()
    {
        var bossImage = this.FindControl<Image>("BossImage");
        var bossText = this.FindControl<TextBlock>("BossText");

        if (bossImage == null || bossText == null || ViewModel?.CurrentBoss == null)
            return;

        var bossName = ViewModel.CurrentBoss.Value.ToString();
        var sprite = SpriteService.Instance.GetBossImage(bossName);
        if (sprite != null)
        {
            bossImage.Source = sprite;
            bossImage.IsVisible = true;
            bossText.IsVisible = false;
        }
        else
        {
            bossImage.IsVisible = false;
            bossText.IsVisible = true;
        }
    }

    private void RenderVoucherImage()
    {
        var voucherImage = this.FindControl<Image>("VoucherImage");
        var voucherText = this.FindControl<TextBlock>("VoucherText");

        if (voucherImage == null || voucherText == null || ViewModel?.CurrentVoucher == null)
            return;

        var voucherStr = ViewModel.CurrentVoucher.Value.ToString();
        if (voucherStr != "None")
        {
            var sprite = SpriteService.Instance.GetVoucherImage(voucherStr);
            if (sprite != null)
            {
                voucherImage.Source = sprite;
                voucherImage.IsVisible = true;
                voucherText.IsVisible = false;
            }
            else
            {
                voucherImage.IsVisible = false;
                voucherText.IsVisible = true;
            }
        }
        else
        {
            voucherImage.IsVisible = false;
            voucherText.IsVisible = true;
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

    // Legacy ComboBox handlers removed in favor of DeckAndStakeSelector
}

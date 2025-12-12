using System;
using System.Threading.Tasks;
using System.Windows.Input;
using Avalonia.Threading;
using BalatroSeedOracle.Helpers;
using BalatroSeedOracle.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace BalatroSeedOracle.ViewModels;

/// <summary>
/// ViewModel for the Fertilizer Widget.
/// Displays seed count with K/M badge, allows clearing the fertilizer pile.
/// </summary>
public partial class FertilizerWidgetViewModel : BaseWidgetViewModel, IDisposable
{
    private readonly FertilizerService _fertilizerService;
    private bool _disposed;

    [ObservableProperty]
    private long _seedCount;

    [ObservableProperty]
    private string _seedCountDisplay = "0";

    [ObservableProperty]
    private bool _isClearing;

    [ObservableProperty]
    private bool _showConfirmClear;

    [ObservableProperty]
    private string _statusMessage = "";

    public ICommand ClearCommand { get; }
    public ICommand ConfirmClearCommand { get; }
    public ICommand CancelClearCommand { get; }
    public ICommand ExportCommand { get; }

    public FertilizerWidgetViewModel(FertilizerService fertilizerService)
    {
        _fertilizerService = fertilizerService;

        // Widget settings
        WidgetTitle = "Fertilizer";
        WidgetIcon = "\U0001F331"; // Seedling emoji
        Width = 320;
        Height = 280;
        PositionX = 20;
        PositionY = 380;

        // Commands
        ClearCommand = new RelayCommand(OnClear);
        ConfirmClearCommand = new AsyncRelayCommand(OnConfirmClearAsync);
        CancelClearCommand = new RelayCommand(OnCancelClear);
        ExportCommand = new AsyncRelayCommand(OnExportAsync);

        // Subscribe to seed count changes
        _fertilizerService.SeedCountChanged += OnSeedCountChanged;

        // Initialize count
        UpdateSeedCount(_fertilizerService.SeedCount);
    }

    private void OnSeedCountChanged(object? sender, long count)
    {
        Dispatcher.UIThread.Post(() => UpdateSeedCount(count));
    }

    private void UpdateSeedCount(long count)
    {
        SeedCount = count;
        SeedCountDisplay = FormatSeedCount(count);
        SetNotification(count);
    }

    private static string FormatSeedCount(long count)
    {
        if (count < 1000)
            return count.ToString("N0");

        if (count < 1_000_000)
            return $"{count / 1000.0:0.##}K";

        return $"{count / 1_000_000.0:0.##}M";
    }

    private void OnClear()
    {
        ShowConfirmClear = true;
    }

    private async Task OnConfirmClearAsync()
    {
        ShowConfirmClear = false;
        IsClearing = true;
        StatusMessage = "Clearing...";

        try
        {
            await _fertilizerService.ClearAsync();
            StatusMessage = "Fertilizer cleared!";
            DebugLogger.Log("FertilizerWidget", "Fertilizer pile cleared by user");

            // Auto-hide message after 3 seconds
            await Task.Delay(3000);
            StatusMessage = "";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error: {ex.Message}";
            DebugLogger.LogError("FertilizerWidget", $"Clear failed: {ex.Message}");
        }
        finally
        {
            IsClearing = false;
        }
    }

    private void OnCancelClear()
    {
        ShowConfirmClear = false;
    }

    private async Task OnExportAsync()
    {
        StatusMessage = "Exporting...";

        try
        {
            await _fertilizerService.ExportToTxtAsync();
            StatusMessage = "Exported to fertilizer.txt!";

            await Task.Delay(3000);
            StatusMessage = "";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error: {ex.Message}";
        }
    }

    protected override void OnExpanded()
    {
        // Refresh count when expanded
        UpdateSeedCount(_fertilizerService.SeedCount);
    }

    public void Dispose()
    {
        if (_disposed) return;

        _fertilizerService.SeedCountChanged -= OnSeedCountChanged;
        _disposed = true;
    }
}

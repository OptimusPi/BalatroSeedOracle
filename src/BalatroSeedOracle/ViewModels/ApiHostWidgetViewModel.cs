using System;
using System.Threading.Tasks;
using Avalonia.Threading;
using BalatroSeedOracle.Helpers;
using BalatroSeedOracle.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace BalatroSeedOracle.ViewModels;

/// <summary>
/// Server status for styling via pseudo-classes
/// </summary>
public enum ServerStatus
{
    Stopped,
    Starting,
    Running,
    Stopping,
    Failed,
    Unsupported,
}

/// <summary>
/// ViewModel for the API Host Widget - hosts the Motely API server within BSO.
/// Uses IApiHostService for platform abstraction. Service is null on unsupported platforms (e.g., Browser).
/// </summary>
public partial class ApiHostWidgetViewModel : BaseWidgetViewModel
{
    private readonly IApiHostService? _apiHostService;

    [ObservableProperty]
    private bool _isServerRunning;

    [ObservableProperty]
    private ServerStatus _currentStatus = ServerStatus.Stopped;

    [ObservableProperty]
    private string _serverStatusText = "Stopped";

    [ObservableProperty]
    private string _serverUrl = "http://localhost:3141/";

    [ObservableProperty]
    private string _logText = "";

    [ObservableProperty]
    private int _port = 3141;

    [ObservableProperty]
    private bool _isSupported = true;

    public ApiHostWidgetViewModel(
        IApiHostService? apiHostService = null,
        WidgetPositionService? positionService = null
    )
        : base(positionService)
    {
        _apiHostService = apiHostService;

        WidgetTitle = "API Host";
        WidgetIcon = "ServerNetwork";
        IsMinimized = true;

        // Service null = platform doesn't support API hosting
        IsSupported = _apiHostService?.IsSupported ?? false;

        if (!IsSupported)
        {
            CurrentStatus = ServerStatus.Unsupported;
            ServerStatusText = "N/A (Browser)";
            LogMessage("API hosting not available on this platform. Use Desktop version.");
        }

        // Subscribe to service events (only if service exists)
        if (_apiHostService != null)
        {
            _apiHostService.LogMessage += OnServiceLogMessage;
            _apiHostService.StatusChanged += OnServiceStatusChanged;
        }
    }

    private void OnServiceLogMessage(string message)
    {
        LogMessage(message);
    }

    private void OnServiceStatusChanged(bool isRunning)
    {
        Dispatcher.UIThread.Post(() =>
        {
            IsServerRunning = isRunning;
            CurrentStatus = isRunning ? ServerStatus.Running : ServerStatus.Stopped;
            ServerStatusText = isRunning ? "Running" : "Stopped";
            ServerUrl = _apiHostService?.ServerUrl ?? "";
        });
    }

    [RelayCommand]
    private async Task StartServerAsync()
    {
        if (!IsSupported || IsServerRunning || _apiHostService == null)
            return;

        try
        {
            CurrentStatus = ServerStatus.Starting;
            ServerStatusText = "Starting...";
            ServerUrl = $"http://localhost:{Port}/";

            await _apiHostService.StartAsync(Port);
        }
        catch (Exception ex)
        {
            CurrentStatus = ServerStatus.Failed;
            ServerStatusText = "Failed";
            IsServerRunning = false;
            LogMessage($"Failed to start: {ex.Message}");
            DebugLogger.LogError($"[ApiHost] Failed to start API server: {ex.Message}");
        }
    }

    [RelayCommand]
    private async Task StopServerAsync()
    {
        if (!IsSupported || !IsServerRunning || _apiHostService == null)
            return;

        try
        {
            CurrentStatus = ServerStatus.Stopping;
            ServerStatusText = "Stopping...";

            await _apiHostService.StopAsync();
        }
        catch (Exception ex)
        {
            LogMessage($"Error stopping: {ex.Message}");
            CurrentStatus = ServerStatus.Stopped;
            ServerStatusText = "Stopped";
            IsServerRunning = false;
        }
    }

    [RelayCommand]
    private void OpenInBrowser()
    {
        if (!IsServerRunning)
            return;

        try
        {
            var psi = new System.Diagnostics.ProcessStartInfo(ServerUrl) { UseShellExecute = true };
            System.Diagnostics.Process.Start(psi);
            LogMessage($"Opened browser: {ServerUrl}");
        }
        catch (Exception ex)
        {
            LogMessage($"Failed to open browser: {ex.Message}");
        }
    }

    [RelayCommand]
    private async Task CopyUrlAsync()
    {
        try
        {
            var topLevel = Avalonia.Application.Current?.ApplicationLifetime
                is Avalonia.Controls.ApplicationLifetimes.IClassicDesktopStyleApplicationLifetime desktop
                ? desktop.MainWindow
                : null;

            if (topLevel?.Clipboard is { } clipboard)
            {
                await clipboard.SetTextAsync(ServerUrl);
                LogMessage("Copied URL to clipboard");
            }
        }
        catch (Exception ex)
        {
            LogMessage($"Failed to copy: {ex.Message}");
        }
    }

    [RelayCommand]
    private void ClearLog()
    {
        LogText = "";
    }

    [RelayCommand]
    private void ToggleMinimized()
    {
        IsMinimized = !IsMinimized;
    }

    private void LogMessage(string message)
    {
        var timestamp = DateTime.Now.ToString("HH:mm:ss");
        var line = $"[{timestamp}] {message}\n";

        Dispatcher.UIThread.Post(() =>
        {
            LogText += line;

            // Trim log if it gets too long
            if (LogText.Length > 5000)
            {
                LogText = LogText.Substring(LogText.Length - 4000);
            }
        });

        DebugLogger.Log($"[ApiHost] {message}");
    }

    public async Task CleanupAsync()
    {
        if (_apiHostService != null)
        {
            _apiHostService.LogMessage -= OnServiceLogMessage;
            _apiHostService.StatusChanged -= OnServiceStatusChanged;
        }

        if (IsServerRunning)
        {
            await StopServerAsync();
        }
    }
}

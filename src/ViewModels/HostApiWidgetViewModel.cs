using System;
using System.Collections.ObjectModel;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;
using Avalonia.Threading;
using BalatroSeedOracle.Helpers;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Motely.API;

namespace BalatroSeedOracle.ViewModels;

/// <summary>
/// ViewModel for the Host API Widget.
/// Controls the MotelyApiServer, shows status, request log, and running background searches.
/// </summary>
public partial class HostApiWidgetViewModel : BaseWidgetViewModel, IDisposable
{
    private MotelyApiServer? _server;
    private CancellationTokenSource? _serverCts;
    private bool _disposed;
    private int _backgroundSearchCount;

    [ObservableProperty]
    private bool _isServerRunning;

    [ObservableProperty]
    private string _serverStatus = "OFFLINE";

    [ObservableProperty]
    private string _serverStatusColor = "#FF4444"; // Red

    [ObservableProperty]
    private string _host = "localhost";

    [ObservableProperty]
    private int _port = 3141;

    [ObservableProperty]
    private string _serverUrl = "http://localhost:3141/";

    [ObservableProperty]
    private ObservableCollection<string> _requestLog = new();

    [ObservableProperty]
    private int _requestCount;

    public ICommand StartServerCommand { get; }
    public ICommand StopServerCommand { get; }
    public ICommand OpenWebUiCommand { get; }
    public ICommand ClearLogCommand { get; }

    public HostApiWidgetViewModel()
    {
        // Widget settings
        WidgetTitle = "Host API";
        WidgetIcon = "\U0001F310"; // Globe emoji
        Width = 400;
        Height = 450;
        PositionX = 100;
        PositionY = 380;

        // Commands
        StartServerCommand = new AsyncRelayCommand(StartServerAsync, () => !IsServerRunning);
        StopServerCommand = new RelayCommand(StopServer, () => IsServerRunning);
        OpenWebUiCommand = new RelayCommand(OpenWebUi, () => IsServerRunning);
        ClearLogCommand = new RelayCommand(ClearLog);

        UpdateServerUrl();
    }

    partial void OnHostChanged(string value)
    {
        UpdateServerUrl();
    }

    partial void OnPortChanged(int value)
    {
        UpdateServerUrl();
    }

    private void UpdateServerUrl()
    {
        ServerUrl = $"http://{Host}:{Port}/";
    }

    private async Task StartServerAsync()
    {
        if (IsServerRunning) return;

        try
        {
            AddLog("Starting server...");

            _serverCts = new CancellationTokenSource();
            _server = new MotelyApiServer(Host, Port, OnServerLog);

            // Start server in background
            _ = Task.Run(async () =>
            {
                try
                {
                    await _server.StartAsync(_serverCts.Token);
                }
                catch (OperationCanceledException)
                {
                    // Expected when stopping
                }
                catch (Exception ex)
                {
                    Dispatcher.UIThread.Post(() =>
                    {
                        AddLog($"Server error: {ex.Message}");
                        UpdateServerStatus(false);
                    });
                }
            });

            // Give it a moment to start
            await Task.Delay(500);

            if (_server.IsRunning)
            {
                UpdateServerStatus(true);
                AddLog($"Server started on {ServerUrl}");
            }
            else
            {
                AddLog("Failed to start server");
            }
        }
        catch (Exception ex)
        {
            AddLog($"Error: {ex.Message}");
            DebugLogger.LogError("HostApiWidget", $"Failed to start server: {ex.Message}");
        }
    }

    private void StopServer()
    {
        if (!IsServerRunning) return;

        try
        {
            AddLog("Stopping server...");
            _serverCts?.Cancel();
            _server?.Stop();
            _server = null;
            _serverCts?.Dispose();
            _serverCts = null;

            UpdateServerStatus(false);
            AddLog("Server stopped");
        }
        catch (Exception ex)
        {
            AddLog($"Error stopping: {ex.Message}");
            DebugLogger.LogError("HostApiWidget", $"Failed to stop server: {ex.Message}");
        }
    }

    private void UpdateServerStatus(bool isRunning)
    {
        IsServerRunning = isRunning;
        ServerStatus = isRunning ? "ONLINE" : "OFFLINE";
        ServerStatusColor = isRunning ? "#44FF44" : "#FF4444"; // Green or Red

        // Update notification badge based on background searches
        if (isRunning)
        {
            if (_backgroundSearchCount > 0)
            {
                SetNotification(_backgroundSearchCount);
            }
            else
            {
                ClearNotification();
            }
        }
        else
        {
            ClearNotification();
        }

        // Notify commands
        ((AsyncRelayCommand)StartServerCommand).NotifyCanExecuteChanged();
        ((RelayCommand)StopServerCommand).NotifyCanExecuteChanged();
        ((RelayCommand)OpenWebUiCommand).NotifyCanExecuteChanged();
    }

    private void OnServerLog(string message)
    {
        Dispatcher.UIThread.Post(() =>
        {
            AddLog(message);

            // Track background searches for badge
            if (message.Contains("Background search started"))
            {
                _backgroundSearchCount++;
                SetNotification(_backgroundSearchCount);
            }
            else if (message.Contains("Background done") || message.Contains("Background error"))
            {
                _backgroundSearchCount = Math.Max(0, _backgroundSearchCount - 1);
                if (_backgroundSearchCount > 0)
                    SetNotification(_backgroundSearchCount);
                else
                    ClearNotification();
            }
        });
    }

    private void AddLog(string message)
    {
        // Keep only last 100 entries
        while (RequestLog.Count > 100)
        {
            RequestLog.RemoveAt(0);
        }

        RequestLog.Add($"[{DateTime.Now:HH:mm:ss}] {message}");
        RequestCount = RequestLog.Count;
    }

    private void ClearLog()
    {
        RequestLog.Clear();
        RequestCount = 0;
    }

    private void OpenWebUi()
    {
        if (!IsServerRunning) return;

        try
        {
            var psi = new System.Diagnostics.ProcessStartInfo
            {
                FileName = ServerUrl,
                UseShellExecute = true
            };
            System.Diagnostics.Process.Start(psi);
            AddLog("Opened Web UI in browser");
        }
        catch (Exception ex)
        {
            AddLog($"Failed to open browser: {ex.Message}");
            DebugLogger.LogError("HostApiWidget", $"Failed to open browser: {ex.Message}");
        }
    }

    protected override void OnExpanded()
    {
        // Nothing special needed
    }

    protected override void OnClosed()
    {
        // Stop server when widget closes
        StopServer();
        base.OnClosed();
    }

    public void Dispose()
    {
        if (_disposed) return;

        StopServer();
        _disposed = true;
    }
}

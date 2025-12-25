using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;
using Avalonia.Threading;
using BalatroSeedOracle.Helpers;
using BalatroSeedOracle.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace BalatroSeedOracle.ViewModels;

/// <summary>
/// ViewModel for the Host API Widget.
/// Controls the clean API server, shows status, request log, and running background searches.
/// </summary>
public partial class HostApiWidgetViewModel : BaseWidgetViewModel, IDisposable
{
    private Process? _server;
    private CancellationTokenSource? _serverCts;
    private CancellationTokenSource? _serverMonitorCts;
    private bool _disposed;
    private int _backgroundSearchCount;
    private readonly UserProfileService? _userProfileService;

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
    private int _threadCount = Environment.ProcessorCount;

    [ObservableProperty]
    private string _serverUrl = "http://localhost:3141/";

    [ObservableProperty]
    private ObservableCollection<string> _requestLog = new();

    [ObservableProperty]
    private int _requestCount;

    [ObservableProperty]
    private string _logText = "";

    public int MaxThreads => Environment.ProcessorCount;

    public ICommand StartServerCommand { get; }
    public ICommand StopServerCommand { get; }
    public ICommand OpenWebUiCommand { get; }
    public ICommand StartTunnelCommand { get; }
    public ICommand ClearLogCommand { get; }

    public HostApiWidgetViewModel()
    {
        // Widget settings
        WidgetTitle = "Host API";
        WidgetIcon = "🌐"; // Globe icon
        Width = 500;
        Height = 600;
        PositionX = 100;
        PositionY = 380;

        // Commands
        StartServerCommand = new AsyncRelayCommand(StartServerAsync, () => !IsServerRunning);
        StopServerCommand = new RelayCommand(StopServer, () => IsServerRunning);
        OpenWebUiCommand = new RelayCommand(OpenWebUi, () => IsServerRunning);
        StartTunnelCommand = new RelayCommand(StartTunnel, () => IsServerRunning);
        ClearLogCommand = new RelayCommand(ClearLog);
        
        // Load saved settings
        _userProfileService = App.GetService<UserProfileService>();
        LoadSettings();

        UpdateServerUrl();
    }

    private void LoadSettings()
    {
        if (_userProfileService == null) return;
        var profile = _userProfileService.GetProfile();
        var settings = profile.HostApiSettings;
        Host = settings.Host;
        Port = settings.Port;
        ThreadCount = settings.ThreadCount;
    }

    private void SaveSettings()
    {
        if (_userProfileService == null) return;
        var profile = _userProfileService.GetProfile();
        profile.HostApiSettings.Host = Host;
        profile.HostApiSettings.Port = Port;
        profile.HostApiSettings.ThreadCount = ThreadCount;
        _userProfileService.SaveProfile(profile);
    }

    partial void OnHostChanged(string value)
    {
        UpdateServerUrl();
        SaveSettings();
    }

    partial void OnPortChanged(int value)
    {
        UpdateServerUrl();
        SaveSettings();
    }

    partial void OnThreadCountChanged(int value)
    {
        SaveSettings();
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
            AddLog("Starting clean API server...");

            _serverCts = new CancellationTokenSource();
            
            // Launch the clean API instead of the old MotelyApiServer
            var apiProjectPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..", "..", "..", "..", "src", "BalatroSeedOracle.API");
            var startInfo = new ProcessStartInfo
            {
                FileName = "dotnet",
                Arguments = $"run --project \"{apiProjectPath}\" --urls \"http://{Host}:{Port}\"",
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true
            };

            _server = new Process { StartInfo = startInfo };
            _server.OutputDataReceived += (s, e) => {
                if (!string.IsNullOrEmpty(e.Data)) {
                    AddLog($"[API] {e.Data}");
                }
            };
            _server.ErrorDataReceived += (s, e) => {
                if (!string.IsNullOrEmpty(e.Data)) {
                    AddLog($"[ERROR] {e.Data}");
                }
            };

            _server.Start();
            _server.BeginOutputReadLine();
            _server.BeginErrorReadLine();

            // Set up proper async server exit handling
            _serverMonitorCts = new CancellationTokenSource();
            _server.EnableRaisingEvents = true;
            _server.Exited += async (s, e) => {
                await HandleServerExit();
            };

            // Wait for server to start with proper timeout
            var startupTimeout = TimeSpan.FromSeconds(10);
            var startTime = DateTime.UtcNow;
            
            while (DateTime.UtcNow - startTime < startupTimeout)
            {
                if (_server.HasExited)
                {
                    AddLog("Server exited immediately during startup");
                    return;
                }
                
                // Check if server is responding (could add health check here)
                await Task.Delay(100);
                
                // For now, just wait a reasonable startup time
                if (DateTime.UtcNow - startTime > TimeSpan.FromSeconds(2))
                {
                    break;
                }
            }

            if (_server != null && !_server.HasExited)
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

            if (_server != null)
            {
                try
                {
                    if (!_server.HasExited)
                    {
                        // Try graceful close first; if the process has no window this will be a no-op.
                        _server.CloseMainWindow();
                        if (!_server.WaitForExit(200))
                        {
                            _server.Kill(entireProcessTree: true);
                            _server.WaitForExit(200);
                        }
                    }
                }
                catch (Exception ex)
                {
                    // Log shutdown error but continue cleanup
                    DebugLogger.LogError("HostApiWidget", $"Server shutdown error: {ex.Message}");
                }
                finally
                {
                    _server?.Dispose();
                }
            }
            _server = null;
            _serverMonitorCts?.Cancel();
            _serverMonitorCts?.Dispose();
            _serverMonitorCts = null;
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
        DebugLogger.Log("HostApi", message);
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
        var line = $"[{DateTime.Now:HH:mm:ss}] {message}";
        RequestLog.Add(line);
        
        // Keep only last 100 entries
        while (RequestLog.Count > 100)
        {
            RequestLog.RemoveAt(0);
        }

        RequestCount = RequestLog.Count;
        LogText = string.Join("\n", RequestLog);
    }

    private void ClearLog()
    {
        RequestLog.Clear();
        RequestCount = 0;
        LogText = "";
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

    private async Task HandleServerExit()
    {
        if (_serverMonitorCts?.IsCancellationRequested == true) return;
        
        await Dispatcher.UIThread.InvokeAsync(() => {
            AddLog("Server process exited");
            UpdateServerStatus(false);
        });
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

    private void StartTunnel()
    {
        if (!IsServerRunning) return;

        try
        {
            AddLog("Starting tunnel...");
            AddLog("Tunnel functionality not yet implemented");
        }
        catch (Exception ex)
        {
            AddLog($"Failed to start tunnel: {ex.Message}");
            DebugLogger.LogError("HostApiWidget", $"Failed to start tunnel: {ex.Message}");
        }
    }

    public void Dispose()
    {
        if (_disposed) return;

        StopServer();
        _disposed = true;
    }
}

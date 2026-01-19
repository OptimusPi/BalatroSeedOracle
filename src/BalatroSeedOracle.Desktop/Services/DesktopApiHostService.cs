using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Hosting;
using Motely.API;
using BalatroSeedOracle.Services;

namespace BalatroSeedOracle.Desktop.Services;

/// <summary>
/// Desktop implementation of IApiHostService using Motely.API
/// </summary>
public class DesktopApiHostService : IApiHostService
{
    private WebApplication? _server;
    private CancellationTokenSource? _cts;
    
    public bool IsSupported => true;
    public bool IsRunning { get; private set; }
    public string ServerUrl { get; private set; } = "http://localhost:3141/";
    
    public event Action<string>? LogMessage;
    public event Action<bool>? StatusChanged;

    public async Task StartAsync(int port)
    {
        if (IsRunning) return;

        try
        {
            ServerUrl = $"http://localhost:{port}/";
            var args = new[] { "--urls", ServerUrl };

            Log($"Starting Motely API on {ServerUrl}");

            _cts = new CancellationTokenSource();
            _server = MotelyApiHost.CreateHost(args);

            // Set thread budget - use fully qualified name to avoid ambiguity
            var threadCount = Environment.ProcessorCount;
            Motely.API.SearchManager.Instance.SetThreadBudget(threadCount);
            Log($"Thread budget: {threadCount}");

            IsRunning = true;
            StatusChanged?.Invoke(true);

            Log("Server started successfully");
            Log($"Web UI: {ServerUrl}");
            Log($"Health: {ServerUrl}health");
            Log($"SignalR Hub: {ServerUrl}searchHub");

            // Run server in background
            _ = Task.Run(async () =>
            {
                try
                {
                    await _server.RunAsync(_cts.Token);
                }
                catch (OperationCanceledException)
                {
                    // Expected during stop
                }
                catch (Exception ex)
                {
                    Log($"Server error: {ex.Message}");
                    await StopInternalAsync();
                }
            });
        }
        catch (Exception ex)
        {
            Log($"Failed to start: {ex.Message}");
            IsRunning = false;
            StatusChanged?.Invoke(false);
            throw;
        }
    }

    public async Task StopAsync()
    {
        if (!IsRunning) return;

        Log("Stopping server...");

        // Stop all searches first - use fully qualified name
        try
        {
            await Motely.API.SearchManager.Instance.StopAllSearchesAsync();
            Log("Stopped all active searches");
        }
        catch (Exception ex)
        {
            Log($"Warning stopping searches: {ex.Message}");
        }

        await StopInternalAsync();
        Log("Server stopped");
    }

    private async Task StopInternalAsync()
    {
        _cts?.Cancel();

        if (_server is not null)
        {
            try
            {
                using var stopCts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
                await _server.StopAsync(stopCts.Token);
            }
            catch { }

            try
            {
                await _server.DisposeAsync();
            }
            catch { }
        }

        _server = null;
        _cts?.Dispose();
        _cts = null;

        IsRunning = false;
        StatusChanged?.Invoke(false);
    }

    private void Log(string message)
    {
        LogMessage?.Invoke(message);
    }
}

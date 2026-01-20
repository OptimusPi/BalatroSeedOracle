using System;
using System.Threading.Tasks;
using BalatroSeedOracle.Services;

namespace BalatroSeedOracle.Browser.Services;

/// <summary>
/// Browser stub implementation of IApiHostService - API hosting not supported in WASM
/// </summary>
public class BrowserApiHostService : IApiHostService
{
    public bool IsSupported => false;
    public bool IsRunning => false;
    public string ServerUrl => "";

    public event Action<string>? LogMessage;
    public event Action<bool>? StatusChanged;

    public Task StartAsync(int port)
    {
        LogMessage?.Invoke("API hosting is not available in the browser version.");
        LogMessage?.Invoke("Please use the Desktop version to host the API server.");
        return Task.CompletedTask;
    }

    public Task StopAsync()
    {
        return Task.CompletedTask;
    }
}

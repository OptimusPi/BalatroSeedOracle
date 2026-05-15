using System;
using System.Threading.Tasks;
using BalatroSeedOracle.Services;

namespace BalatroSeedOracle.Desktop.Services;

public class DesktopApiHostService : IApiHostService
{
    public bool IsSupported => false;
    public bool IsRunning => false;
    public string ServerUrl => "";

    public event Action<string>? LogMessage;
    public event Action<bool>? StatusChanged;

    public Task StartAsync(int port)
    {
        LogMessage?.Invoke("API host is not available in this build.");
        StatusChanged?.Invoke(false);
        return Task.CompletedTask;
    }

    public Task StopAsync()
    {
        StatusChanged?.Invoke(false);
        return Task.CompletedTask;
    }
}

using System;
using System.Threading.Tasks;

namespace BalatroSeedOracle.Services;

/// <summary>
/// Platform-agnostic interface for hosting the Motely API server.
/// Desktop provides real implementation, Browser provides no-op stub.
/// </summary>
public interface IApiHostService
{
    /// <summary>
    /// Whether API hosting is supported on this platform
    /// </summary>
    bool IsSupported { get; }

    /// <summary>
    /// Whether the server is currently running
    /// </summary>
    bool IsRunning { get; }

    /// <summary>
    /// Current server URL when running
    /// </summary>
    string ServerUrl { get; }

    /// <summary>
    /// Event fired when log messages are generated
    /// </summary>
    event Action<string>? LogMessage;

    /// <summary>
    /// Event fired when server status changes
    /// </summary>
    event Action<bool>? StatusChanged;

    /// <summary>
    /// Start the API server on the specified port
    /// </summary>
    Task StartAsync(int port);

    /// <summary>
    /// Stop the API server
    /// </summary>
    Task StopAsync();
}

using System;
using System.Threading.Tasks;
using Motely.Gpu;

namespace BalatroSeedOracle.Services;

/// <summary>
/// Live status snapshot of the host GPU compute device and search activity.
/// </summary>
public sealed record GpuDeviceStatus(
    bool IsAvailable,
    bool IsActive,
    string DeviceName,
    string Vendor,
    uint ComputeUnits,
    uint ClockMHz,
    ulong MemoryBytes,
    double CurrentSeedsPerSecond
);

/// <summary>
/// Service providing host GPU device discovery, status tracking, and live search telemetry.
/// </summary>
public interface IGpuDeviceService
{
    /// <summary>Current GPU device and search status.</summary>
    GpuDeviceStatus GetStatus();

    /// <summary>Fired whenever GPU search activity or telemetry changes.</summary>
    event EventHandler<GpuDeviceStatus>? StatusChanged;

    /// <summary>Notifies the service when a search begins or ends, and updates live throughput.</summary>
    void SetSearchActive(bool active, double seedsPerSecond = 0);

    /// <summary>Executes a live hardware benchmark on the host GPU.</summary>
    Task<GpuBenchmarkResult> RunBenchmarkAsync(int batchChars = 3, int batchCount = 64);
}

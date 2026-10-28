using System;
using System.Threading.Tasks;
using BalatroSeedOracle.Helpers;
using Motely.Gpu;

namespace BalatroSeedOracle.Services;

/// <summary>
/// Discovers and tracks host GPU hardware (e.g. NVIDIA GeForce RTX 4070 SUPER),
/// providing live search telemetry and status notifications.
/// </summary>
public sealed class GpuDeviceService : IGpuDeviceService
{
    private readonly object _lock = new();
    private readonly GpuDeviceInfo? _device;
    private bool _isActive;
    private double _currentSeedsPerSecond;

    public GpuDeviceService(GpuDeviceInfo? device = null)
    {
        _device = device ?? GpuDeviceInfo.GetBestDevice();
        if (_device is not null)
        {
            DebugLogger.Log(
                nameof(GpuDeviceService),
                $"Host GPU discovered: {_device.Name} ({_device.Vendor}, {_device.ComputeUnits} CUs, FP64: {_device.SupportsFp64})"
            );
        }
        else
        {
            DebugLogger.Log(
                nameof(GpuDeviceService),
                "No OpenCL FP64 GPU device available on this host."
            );
        }
    }

    /// <inheritdoc/>
    public event EventHandler<GpuDeviceStatus>? StatusChanged;

    /// <inheritdoc/>
    public GpuDeviceStatus GetStatus()
    {
        lock (_lock)
        {
            return BuildStatus();
        }
    }

    /// <inheritdoc/>
    public void SetSearchActive(bool active, double seedsPerSecond = 0)
    {
        GpuDeviceStatus status;
        lock (_lock)
        {
            _isActive = active;
            _currentSeedsPerSecond = active ? seedsPerSecond : 0;
            status = BuildStatus();
        }

        StatusChanged?.Invoke(this, status);
    }

    /// <inheritdoc/>
    public Task<GpuBenchmarkResult> RunBenchmarkAsync(int batchChars = 3, int batchCount = 64)
    {
        return Task.Run(() => GpuSeedSearcher.Benchmark(batchChars, batchCount));
    }

    private GpuDeviceStatus BuildStatus()
    {
        bool isAvail = _device is { SupportsFp64: true };
        return new GpuDeviceStatus(
            IsAvailable: isAvail,
            IsActive: _isActive && isAvail,
            DeviceName: _device?.Name ?? (OperatingSystem.IsBrowser() ? "Browser WebGL (No OpenCL)" : "No OpenCL FP64 Device"),
            Vendor: _device?.Vendor ?? "None",
            ComputeUnits: _device?.ComputeUnits ?? 0,
            ClockMHz: _device?.MaxClockFrequencyMHz ?? 0,
            MemoryBytes: _device?.GlobalMemoryBytes ?? 0,
            CurrentSeedsPerSecond: _currentSeedsPerSecond
        );
    }
}

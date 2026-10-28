using System;
using System.Threading;
using System.Threading.Tasks;

namespace BalatroSeedOracle.Services;

/// <summary>
/// Real-time progress snapshot of the Community Party Search worker.
/// </summary>
public sealed record PartySearchProgress(
    string PartyId,
    string Status,
    int LeasesCompleted,
    long BlocksCompleted,
    long SeedsSearched,
    long MatchesFound,
    long ConfirmedByServer,
    double SeedsPerSecond,
    TimeSpan Elapsed
);

/// <summary>
/// Service managing the background distributed Community Party search worker.
/// </summary>
public interface IPartySearchService
{
    /// <summary>True if a party worker is actively running in the background.</summary>
    bool IsWorking { get; }

    /// <summary>ID of the currently running or last joined party.</summary>
    string? CurrentPartyId { get; }

    /// <summary>Latest progress telemetry snapshot.</summary>
    PartySearchProgress? CurrentProgress { get; }

    /// <summary>Fired on worker telemetry updates.</summary>
    event EventHandler<PartySearchProgress>? ProgressUpdated;

    /// <summary>Fired when the worker emits a log or status message.</summary>
    event EventHandler<string>? LogMessageReceived;

    /// <summary>Starts the background worker connected to the given coordinator.</summary>
    Task StartPartyWorkerAsync(string coordinatorUrl, string partyId, int threads, bool useGpu, CancellationToken ct);

    /// <summary>Stops the background party worker.</summary>
    Task StopPartyWorkerAsync();
}

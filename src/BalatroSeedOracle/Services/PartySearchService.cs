using System;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using BalatroSeedOracle.Helpers;
using Motely.Party;

namespace BalatroSeedOracle.Services;

/// <summary>
/// Manages a distributed Search Party worker in the background, communicating with
/// the seedfinder.app coordinator or a local Motely.MCP coordinator.
/// </summary>
public sealed class PartySearchService : IPartySearchService, IDisposable
{
    private readonly Func<Uri, PartyClient> _clientFactory;
    private readonly HttpClient _defaultHttp;
    private CancellationTokenSource? _workerCts;
    private Task? _workerTask;
    private readonly object _sync = new();

    public PartySearchService(Func<Uri, PartyClient>? clientFactory = null, HttpClient? httpClient = null)
    {
        _defaultHttp = httpClient ?? new HttpClient();
        _clientFactory = clientFactory ?? (uri => new PartyClient(_defaultHttp, uri));
    }

    /// <inheritdoc/>
    public bool IsWorking { get; private set; }

    /// <inheritdoc/>
    public string? CurrentPartyId { get; private set; }

    /// <inheritdoc/>
    public PartySearchProgress? CurrentProgress { get; private set; }

    /// <inheritdoc/>
    public event EventHandler<PartySearchProgress>? ProgressUpdated;

    /// <inheritdoc/>
    public event EventHandler<string>? LogMessageReceived;

    /// <inheritdoc/>
    public async Task StartPartyWorkerAsync(
        string coordinatorUrl,
        string partyId,
        int threads,
        bool useGpu,
        CancellationToken ct
    )
    {
        if (string.IsNullOrWhiteSpace(partyId))
            throw new ArgumentException("Party ID must not be empty.", nameof(partyId));

        await StopPartyWorkerAsync().ConfigureAwait(false);

        lock (_sync)
        {
            CurrentPartyId = partyId.Trim();
            IsWorking = true;
            _workerCts = CancellationTokenSource.CreateLinkedTokenSource(ct);
        }

        var uri = new Uri(coordinatorUrl.Trim().TrimEnd('/'));
        var client = _clientFactory(uri);

        var token = _workerCts.Token;
        _workerTask = Task.Run(() => RunWorkerLoopAsync(client, partyId, threads, useGpu, token), token);
    }

    /// <inheritdoc/>
    public async Task StopPartyWorkerAsync()
    {
        Task? taskToWait = null;
        lock (_sync)
        {
            if (!IsWorking && _workerTask == null)
                return;

            _workerCts?.Cancel();
            taskToWait = _workerTask;
        }

        if (taskToWait != null)
        {
            try
            {
                await taskToWait.ConfigureAwait(false);
            }
            catch (OperationCanceledException) { }
            catch (Exception ex)
            {
                DebugLogger.LogError(nameof(PartySearchService), $"Worker stopped with exception: {ex.Message}");
            }
        }

        lock (_sync)
        {
            _workerCts?.Dispose();
            _workerCts = null;
            _workerTask = null;
            IsWorking = false;
        }
    }

    private async Task RunWorkerLoopAsync(
        PartyClient client,
        string partyId,
        int threads,
        bool useGpu,
        CancellationToken ct
    )
    {
        var clock = Stopwatch.StartNew();
        int leasesCompleted = 0;
        long blocksCompleted = 0;
        long seedsSearched = 0;
        long matchesFound = 0;
        long confirmedByServer = 0;

        void PublishProgress(string status)
        {
            double elapsedSec = Math.Max(0.001, clock.Elapsed.TotalSeconds);
            double speed = seedsSearched / elapsedSec;

            var progress = new PartySearchProgress(
                PartyId: partyId,
                Status: status,
                LeasesCompleted: leasesCompleted,
                BlocksCompleted: blocksCompleted,
                SeedsSearched: seedsSearched,
                MatchesFound: matchesFound,
                ConfirmedByServer: confirmedByServer,
                SeedsPerSecond: speed,
                Elapsed: clock.Elapsed
            );

            CurrentProgress = progress;
            ProgressUpdated?.Invoke(this, progress);
        }

        PublishProgress("Connecting...");
        var logWriter = new PartyLogWriter(line =>
        {
            LogMessageReceived?.Invoke(this, line);
            DebugLogger.Log(nameof(PartySearchService), $"[Party:{partyId}] {line}");
        });

        var options = new PartyWorkerOptions
        {
            Threads = threads,
            UseGpu = useGpu,
            Sweep = async (lease, th, sweepCt) =>
            {
                PublishProgress($"Sweeping blocks {lease.StartBlock}..{lease.EndBlock - 1}");
                var sweepResult = await PartySweep.RunAsync(lease, th, useGpu, sweepCt).ConfigureAwait(false);
                leasesCompleted++;
                blocksCompleted += lease.BlockCount;
                seedsSearched += sweepResult.SeedsSearched;
                matchesFound += sweepResult.Matched;
                PublishProgress("Reporting to coordinator...");
                return sweepResult;
            }
        };

        var worker = new PartyWorker(client, partyId, options, logWriter);

        try
        {
            var summary = await worker.RunAsync(ct).ConfigureAwait(false);
            confirmedByServer = summary.Confirmed;
            PublishProgress($"Finished: {summary.Outcome} ({summary.Reason})");
            DebugLogger.Log(
                nameof(PartySearchService),
                $"Worker finished: {summary.Outcome}, Leases: {summary.Leases}, Seeds: {summary.SeedsSearched}"
            );
        }
        catch (OperationCanceledException)
        {
            PublishProgress("Stopped by user");
        }
        catch (Exception ex)
        {
            PublishProgress($"Error: {ex.Message}");
            DebugLogger.LogError(nameof(PartySearchService), $"Worker error: {ex}");
        }
        finally
        {
            lock (_sync)
            {
                IsWorking = false;
            }
        }
    }

    public void Dispose()
    {
        _workerCts?.Cancel();
        _workerCts?.Dispose();
        _defaultHttp.Dispose();
    }

    private sealed class PartyLogWriter(Action<string> onLine) : TextWriter
    {
        private readonly StringBuilder _sb = new();
        public override Encoding Encoding => Encoding.UTF8;

        public override void Write(char value)
        {
            if (value == '\n')
            {
                FlushLine();
            }
            else if (value != '\r')
            {
                _sb.Append(value);
            }
        }

        public override void Write(string? value)
        {
            if (value is null) return;
            foreach (char c in value)
                Write(c);
        }

        public override void Flush()
        {
            if (_sb.Length > 0)
                FlushLine();
        }

        private void FlushLine()
        {
            string line = _sb.ToString().Trim();
            _sb.Clear();
            if (!string.IsNullOrWhiteSpace(line))
                onLine(line);
        }
    }
}

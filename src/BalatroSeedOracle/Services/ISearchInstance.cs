using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using BalatroSeedOracle.Models;
using Motely.Filters;

namespace BalatroSeedOracle.Services;

/// <summary>
/// Interface for search instance operations - platform-agnostic abstraction
/// Following Avalonia UI pattern: interface in shared project, implementation in platform head projects
/// </summary>
public interface ISearchInstance : IDisposable
{
    string SearchId { get; }
    bool IsRunning { get; }
    bool IsPaused { get; }
    TimeSpan SearchDuration { get; }
    DateTime SearchStartTime { get; }
    string ConfigPath { get; }
    string FilterName { get; }
    MotelyJsonConfig? GetFilterConfig();
    int ResultCount { get; }
    IReadOnlyList<string> ColumnNames { get; }
    string DatabasePath { get; }
    bool IsDatabaseInitialized { get; }
    bool HasNewResultsSinceLastQuery { get; }
    
    event EventHandler? SearchStarted;
    event EventHandler? SearchCompleted;
    event EventHandler<SearchProgress>? ProgressUpdated;
    event EventHandler<int>? NewHighScoreFound;
    
    Task StartSearchAsync(
        SearchCriteria criteria,
        MotelyJsonConfig config,
        IProgress<SearchProgress>? progress = null,
        CancellationToken cancellationToken = default
    );

    Task<List<SearchResult>> GetResultsPageAsync(int offset, int count);
    Task<List<SearchResult>> GetTopResultsAsync(string orderBy, bool ascending, int limit = 1000);
    void AcknowledgeResultsQueried();
    void StopSearch();
    void PauseSearch();
    List<string> GetConsoleHistory();
}

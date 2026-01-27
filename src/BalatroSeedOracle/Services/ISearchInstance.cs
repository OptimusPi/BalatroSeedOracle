using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using BalatroSeedOracle.Models;
using Motely.Filters;

namespace BalatroSeedOracle.Services;

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

    event EventHandler<SearchResultEventArgs>? SearchStarted;
    event EventHandler<SearchResultEventArgs>? SearchCompleted;
    event EventHandler<SearchProgress>? ProgressUpdated;

    Task<List<SearchResult>> GetResultsPageAsync(int offset, int count);
    Task<int> GetResultCountAsync();
    Task<List<SearchResult>> GetTopResultsAsync(int count);
    Task<List<SearchResult>> GetTopResultsAsync(string orderBy, bool ascending, int limit = 1000);
    Task StartSearchAsync(SearchCriteria criteria);
    Task StartSearchAsync(
        SearchCriteria criteria,
        MotelyJsonConfig config,
        IProgress<SearchProgress>? progress = null,
        CancellationToken cancellationToken = default
    );
    void AcknowledgeResultsQueried();
    void StopSearch();
    void PauseSearch();
    List<string> GetConsoleHistory();
    event EventHandler<int>? NewHighScoreFound;
}

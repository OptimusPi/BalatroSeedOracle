# DuckDB Integration Plan for BalatroSeedOracle

## Overview
This plan outlines the integration of DuckDB for real-time search result storage, capturing data directly from Motely's result queue instead of CSV output.

## Current State
- **SearchHistoryService**: Already exists with basic DuckDB schema
- **DataGrid**: Already implemented in SearchModal.axaml with sortable columns
- **Result Queue**: `OuijaJsonFilterDesc.OuijaJsonFilter.ResultsQueue` contains results during search

## Phase 1: Enhanced DuckDB Schema

### Updated Schema Design
```sql
-- Main search table (existing, enhanced)
CREATE TABLE IF NOT EXISTS searches (
    search_id INTEGER PRIMARY KEY,
    config_path VARCHAR,
    config_hash VARCHAR,  -- NEW: hash of config for deduplication
    search_date TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    thread_count INTEGER,
    min_score INTEGER,
    batch_size INTEGER,
    deck VARCHAR,
    stake VARCHAR,
    max_ante INTEGER,     -- NEW: from config
    total_seeds_searched BIGINT,
    results_found INTEGER,
    duration_seconds DOUBLE,
    search_status VARCHAR DEFAULT 'running'  -- NEW: running/completed/cancelled
);

-- Enhanced results table
CREATE TABLE IF NOT EXISTS search_results (
    result_id INTEGER PRIMARY KEY,
    search_id INTEGER,
    seed VARCHAR,
    score INTEGER,
    details TEXT,
    ante INTEGER,
    found_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    -- NEW: Breakdown columns for scoring
    score_breakdown TEXT,  -- JSON array of individual scores
    FOREIGN KEY (search_id) REFERENCES searches(search_id)
);

-- NEW: Filter items table for reconstructing searches
CREATE TABLE IF NOT EXISTS filter_items (
    item_id INTEGER PRIMARY KEY,
    search_id INTEGER,
    filter_type VARCHAR,  -- must/should/mustnot
    item_type VARCHAR,    -- joker/tarot/tag/etc
    item_value VARCHAR,   -- Perkeo/TheFool/etc
    edition VARCHAR,      -- Negative/Foil/etc
    score INTEGER,        -- for should items
    antes TEXT,          -- JSON array of antes
    FOREIGN KEY (search_id) REFERENCES searches(search_id)
);

-- Performance indexes
CREATE INDEX IF NOT EXISTS idx_search_date ON searches(search_date);
CREATE INDEX IF NOT EXISTS idx_seed ON search_results(seed);
CREATE INDEX IF NOT EXISTS idx_score ON search_results(score DESC);
CREATE INDEX IF NOT EXISTS idx_search_id ON search_results(search_id);
CREATE INDEX IF NOT EXISTS idx_config_hash ON searches(config_hash);
```

## Phase 2: Real-time Result Capture Service

### New Service: MotelyResultCapture
```csharp
public class MotelyResultCapture : IDisposable
{
    private readonly SearchHistoryService _searchHistory;
    private CancellationTokenSource? _cts;
    private Task? _captureTask;
    private long _currentSearchId = -1;
    
    public async Task StartCapture(long searchId)
    {
        _currentSearchId = searchId;
        _cts = new CancellationTokenSource();
        
        _captureTask = Task.Run(async () =>
        {
            while (!_cts.Token.IsCancellationRequested)
            {
                if (OuijaJsonFilterDesc.OuijaJsonFilter.ResultsQueue.TryDequeue(out var result))
                {
                    await _searchHistory.AddSearchResultAsync(searchId, new SearchResult
                    {
                        Seed = result.Seed,
                        Score = result.TotalScore,
                        Details = GenerateDetails(result),
                        ScoreBreakdown = result.ScoreWants
                    });
                }
                else
                {
                    await Task.Delay(10, _cts.Token);
                }
            }
        }, _cts.Token);
    }
    
    public async Task StopCapture()
    {
        _cts?.Cancel();
        if (_captureTask != null)
            await _captureTask;
    }
}
```

## Phase 3: SearchModal Integration

### Modifications to SearchModal.axaml.cs:
1. **Initialize DuckDB on search start**
2. **Real-time DataGrid updates** as results come in
3. **Direct binding** to DuckDB query results
4. **Sorting/filtering** handled by DuckDB queries

### Key Implementation Points:
```csharp
// In SearchModal.axaml.cs
private async Task StartSearch()
{
    // 1. Start new search in DuckDB
    var searchId = await _searchHistory.StartNewSearchAsync(
        configPath, threads, minScore, batchSize, deck, stake);
    
    // 2. Start result capture service
    _resultCapture = new MotelyResultCapture(_searchHistory);
    await _resultCapture.StartCapture(searchId);
    
    // 3. Launch Motely search process
    var process = LaunchMotelySearch(...);
    
    // 4. Bind DataGrid to live query
    ResultsGrid.ItemsSource = await _searchHistory.GetLiveResultsObservable(searchId);
}
```

## Phase 4: DataGrid Enhancements

### Current DataGrid Features:
- ✅ Sortable columns
- ✅ Score formatting
- ✅ Copy/View actions
- ✅ Alternating row colors
- ✅ Selection support

### Recommended Enhancements:
1. **Virtual Scrolling**: Already supported by Avalonia DataGrid
2. **Live Updates**: Use ObservableCollection bound to DuckDB
3. **Advanced Filtering**: Add filter row with per-column filters
4. **Score Breakdown**: Tooltip or expandable row for score details

### DataGrid Binding Model:
```csharp
public class SearchResultViewModel : INotifyPropertyChanged
{
    public int Index { get; set; }
    public string Seed { get; set; }
    public int Score { get; set; }
    public string ScoreFormatted => Score.ToString("N0");
    public string Details { get; set; }
    public int[] ScoreBreakdown { get; set; }
    
    // Tooltip for score breakdown
    public string ScoreTooltip => GenerateScoreTooltip();
    
    // Commands
    public ICommand CopyCommand { get; }
    public ICommand ViewCommand { get; }
}
```

## Phase 5: Query Interface

### SearchHistoryService Enhancements:
```csharp
// Live results with automatic updates
public async Task<ObservableCollection<SearchResultViewModel>> GetLiveResultsObservable(long searchId)
{
    var collection = new ObservableCollection<SearchResultViewModel>();
    
    // Initial load
    var results = await GetSearchResultsAsync(searchId);
    foreach (var result in results)
        collection.Add(MapToViewModel(result));
    
    // Set up live updates using DuckDB APPENDER or polling
    StartLiveUpdates(searchId, collection);
    
    return collection;
}

// Advanced queries
public async Task<List<SearchResult>> QueryResultsAsync(
    string? seedPattern = null,
    int? minScore = null,
    DateTime? startDate = null,
    DateTime? endDate = null,
    string? configHash = null)
{
    // Build dynamic SQL with parameters
    var sql = BuildQuerySql(seedPattern, minScore, startDate, endDate, configHash);
    // Execute and return results
}
```

## Implementation Timeline

### Week 1:
- ✅ Fix DuckDB parameter error (DONE)
- Update schema with new tables and columns
- Create MotelyResultCapture service
- Test direct queue integration

### Week 2:
- Integrate capture service with SearchModal
- Implement live DataGrid updates
- Add progress tracking to UI
- Test with large result sets

### Week 3:
- Add advanced query interface
- Implement filter row for DataGrid
- Add score breakdown tooltips
- Performance optimization

## Benefits

1. **Real-time Results**: See results as they're found, not after search completes
2. **Persistent Storage**: All results saved in .duckdb files per filter
3. **Fast Queries**: DuckDB's columnar storage for analytics
4. **Resume Capability**: Can query previous search results
5. **Export Flexibility**: DuckDB can export to CSV, Parquet, JSON

## Migration Notes

- Keep CSV export as backup option
- SearchResults folder structure: `SearchResults/{filter_name}.ouija.duckdb`
- Each filter gets its own database file
- Can query across databases using DuckDB's multi-database support

## Performance Considerations

1. **Batch Inserts**: Queue results and insert in batches of 100-1000
2. **Connection Pooling**: Keep connection open during active search only
3. **Index Strategy**: Index on seed, score, and search_id for fast lookups
4. **Vacuum**: Run VACUUM after large searches to optimize storage

## Error Handling

1. **Queue Overflow**: Implement backpressure if DB can't keep up
2. **Connection Loss**: Auto-reconnect with exponential backoff
3. **Disk Space**: Monitor available space, warn user if low
4. **Corrupted DB**: Automatic backup before each search session
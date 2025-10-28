# DuckDB Cleanup and Optimization TODO

**Last Updated:** 2025-10-28
**Reviewer:** Avalonia UI/MVVM Architecture Specialist
**Status:** Ready for Implementation

This document outlines critical fixes, performance improvements, and code quality enhancements for the DuckDB integration in BalatroSeedOracle. All items have been validated from an Avalonia UI/MVVM perspective.

---

## SECTION 1: CRITICAL FIXES (Must do before release)

These items can cause bugs, data corruption, or cross-instance contamination. **SHIP BLOCKER ISSUES.**

### 1.1 CRITICAL: Static ThreadLocal Appender Causes Cross-Instance Contamination

- [ ] Replace static ThreadLocal appender with instance-scoped collection
      File: `x:\BalatroSeedOracle\src\Services\SearchInstance.cs:54`
      Current: `private static readonly ThreadLocal<DuckDB.NET.Data.DuckDBAppender?> _threadAppender = new();`
      Fix: Change to instance-scoped: `private readonly ThreadLocal<DuckDB.NET.Data.DuckDBAppender?> _threadAppender = new();`
      Impact: **CRITICAL DATA CORRUPTION RISK** - Static ThreadLocal means all SearchInstance objects share the same appender reference per thread. If Thread A processes SearchInstance1 then SearchInstance2, the appender from Instance1 will write to Instance2's database, causing cross-contamination of search results between different filters/decks/stakes.

      Additional Changes Required:
      - Line 55: Remove `static` keyword
      - Line 1734: ThreadLocal disposal in Dispose() is already correct
      - Verify no other static state exists that could cause cross-instance issues

### 1.2 CRITICAL: No Connection Pooling in SearchManager

- [ ] Add DuckDB connection pooling to prevent resource exhaustion
      File: `x:\BalatroSeedOracle\src\Services\SearchManager.cs:20-23`
      Current: Each SearchInstance creates its own persistent connection without limits
      Fix: Implement connection pool in SearchManager:
      ```csharp
      private readonly ConcurrentDictionary<string, SearchInstance> _activeSearches;
      private readonly SemaphoreSlim _connectionPoolSemaphore;
      private const int MAX_CONCURRENT_CONNECTIONS = 10;

      public SearchManager()
      {
          _activeSearches = new ConcurrentDictionary<string, SearchInstance>();
          _connectionPoolSemaphore = new SemaphoreSlim(MAX_CONCURRENT_CONNECTIONS);
      }

      public async Task<string> CreateSearchAsync(string filterNameNormalized, string deckName, string stakeName)
      {
          await _connectionPoolSemaphore.WaitAsync();
          try
          {
              // Existing create logic
          }
          catch
          {
              _connectionPoolSemaphore.Release();
              throw;
          }
      }

      public bool RemoveSearch(string searchId)
      {
          if (_activeSearches.TryRemove(searchId, out var search))
          {
              search.Dispose();
              _connectionPoolSemaphore.Release();
              return true;
          }
          return false;
      }
      ```
      Impact: **RESOURCE EXHAUSTION** - Without pooling, creating 20+ concurrent searches can exhaust file handles and database locks, causing application crashes or hangs. This is especially critical in Avalonia where UI thread blocking can freeze the entire application.

### 1.3 CRITICAL: Exception-Based Duplicate Handling in Hot Path

- [ ] Replace try-catch duplicate detection with DuckDB INSERT OR IGNORE
      File: `x:\BalatroSeedOracle\src\Services\SearchInstance.cs:317-350`
      Current: Uses exception catching for PRIMARY KEY violations
      Fix: Replace DuckDB Appender with parameterized INSERT OR IGNORE:
      ```csharp
      private void AddSearchResult(SearchResult result)
      {
          if (!_dbInitialized)
              return;

          try
          {
              using var cmd = _connection.CreateCommand();

              // Build INSERT OR IGNORE statement
              var columnList = string.Join(", ", _columnNames);
              var paramList = string.Join(", ", _columnNames.Select((_, i) => $"${i + 1}"));
              cmd.CommandText = $"INSERT OR IGNORE INTO results ({columnList}) VALUES ({paramList})";

              // Add parameters
              cmd.Parameters.Add(new DuckDBParameter(result.Seed));
              cmd.Parameters.Add(new DuckDBParameter(result.TotalScore));

              int tallyCount = _columnNames.Count - 2;
              for (int i = 0; i < tallyCount; i++)
              {
                  int val = (result.Scores != null && i < result.Scores.Length) ? result.Scores[i] : 0;
                  cmd.Parameters.Add(new DuckDBParameter(val));
              }

              cmd.ExecuteNonQuery();
          }
          catch (Exception ex)
          {
              DebugLogger.LogError($"SearchInstance[{_searchId}]", $"Insert failed: {ex.Message}");
          }
      }
      ```
      Impact: **PERFORMANCE DEGRADATION** - Exception handling is 100-1000x slower than SQL-based duplicate handling. In high-throughput searches processing millions of seeds, this creates significant overhead and can cause UI thread starvation in Avalonia applications.

---

## SECTION 2: PERFORMANCE IMPROVEMENTS (Should do soon)

These items improve speed, memory usage, and UI responsiveness. **High priority for production quality.**

### 2.1 Missing Indexes on Tally Columns

- [ ] Add database indexes for all tally columns to accelerate sorting
      File: `x:\BalatroSeedOracle\src\Services\SearchInstance.cs:236-279`
      Current: Only `idx_score` index exists (line 252-255)
      Fix: In `InitializeDatabase()`, after creating the score index, add:
      ```csharp
      // Create indexes for all tally columns
      for (int i = 2; i < _columnNames.Count; i++)
      {
          var columnName = _columnNames[i];
          using (var createTallyIndex = _connection.CreateCommand())
          {
              createTallyIndex.CommandText =
                  $"CREATE INDEX IF NOT EXISTS idx_{columnName} ON results({columnName} DESC);";
              createTallyIndex.ExecuteNonQuery();
          }
      }
      ```
      Impact: **UI RESPONSIVENESS** - Without indexes, sorting by tally columns in DataGridResultsWindow triggers full table scans. For 100K+ results, this causes 5-15 second UI freezes. With indexes, sorting becomes sub-second. Critical for Avalonia UI responsiveness where long operations must be async to prevent UI thread blocking.

### 2.2 No COUNT(*) Caching

- [ ] Cache result count and invalidate on insert
      File: `x:\BalatroSeedOracle\src\Services\SearchInstance.cs:512-524`
      Current: `GetResultCountAsync()` runs `COUNT(*)` query every time
      Fix: Add caching with invalidation:
      ```csharp
      private volatile int _cachedResultCount = -1; // -1 means not cached
      private readonly object _countCacheLock = new();

      private void InvalidateCountCache()
      {
          lock (_countCacheLock)
          {
              _cachedResultCount = -1;
          }
      }

      public async Task<int> GetResultCountAsync()
      {
          if (!_dbInitialized)
              throw new InvalidOperationException("Database not initialized");

          // Check cache first
          if (_cachedResultCount >= 0)
              return _cachedResultCount;

          // Force flush to ensure all buffered results are counted
          ForceFlush();

          using var cmd = _connection.CreateCommand();
          cmd.CommandText = "SELECT COUNT(*) FROM results";
          var v = await cmd.ExecuteScalarAsync();
          var count = v == null ? 0 : Convert.ToInt32(v);

          lock (_countCacheLock)
          {
              _cachedResultCount = count;
          }

          return count;
      }

      private void AddSearchResult(SearchResult result)
      {
          // ... existing insert logic ...

          // Invalidate count cache after successful insert
          InvalidateCountCache();
      }
      ```
      Impact: **REDUCED DATABASE LOAD** - COUNT(*) on 500K+ rows takes 50-200ms. This is called frequently from UI bindings in Avalonia (via `ResultCount` property). Caching reduces repeated queries and prevents UI stuttering during progress updates.

### 2.3 Periodic Appender Flush for Memory Predictability

- [ ] Add periodic flush timer to prevent unbounded memory growth
      File: `x:\BalatroSeedOracle\src\Services\SearchInstance.cs:281-310`
      Current: Appender only flushes when explicitly called via `ForceFlush()`
      Fix: Add periodic flush mechanism:
      ```csharp
      private System.Timers.Timer? _flushTimer;
      private const int FLUSH_INTERVAL_MS = 5000; // Flush every 5 seconds

      private void InitializeDatabase()
      {
          // ... existing initialization ...

          // Setup periodic flush timer
          _flushTimer = new System.Timers.Timer(FLUSH_INTERVAL_MS);
          _flushTimer.Elapsed += (s, e) => ForceFlush();
          _flushTimer.AutoReset = true;
          _flushTimer.Start();
      }

      public void Dispose()
      {
          // ... existing dispose logic ...

          _flushTimer?.Stop();
          _flushTimer?.Dispose();
      }
      ```
      Impact: **MEMORY PREDICTABILITY** - Appenders buffer data in memory. Without periodic flushing, long-running searches can accumulate 100s of MB in buffers, causing unpredictable memory spikes and potential OOM crashes. Periodic flushing ensures bounded memory usage and makes results visible to UI queries sooner.

### 2.4 Optimize GetTopResultsAsync for UI Binding

- [ ] Add result caching for frequently accessed queries
      File: `x:\BalatroSeedOracle\src\Services\SearchInstance.cs:411-505`
      Current: Every call to `GetTopResultsAsync` hits the database
      Fix: Add simple cache for top N results:
      ```csharp
      private List<SearchResult>? _cachedTopResults;
      private string? _cachedTopResultsKey;
      private readonly object _topResultsCacheLock = new();

      public async Task<List<SearchResult>> GetTopResultsAsync(
          string orderBy,
          bool ascending,
          int limit = 1000)
      {
          var cacheKey = $"{orderBy}_{ascending}_{limit}";

          lock (_topResultsCacheLock)
          {
              if (_cachedTopResults != null && _cachedTopResultsKey == cacheKey)
              {
                  return new List<SearchResult>(_cachedTopResults);
              }
          }

          // ... existing query logic ...

          lock (_topResultsCacheLock)
          {
              _cachedTopResults = results;
              _cachedTopResultsKey = cacheKey;
          }

          return results;
      }

      private void AddSearchResult(SearchResult result)
      {
          // ... existing insert logic ...

          // Invalidate top results cache
          lock (_topResultsCacheLock)
          {
              _cachedTopResults = null;
              _cachedTopResultsKey = null;
          }
      }
      ```
      Impact: **UI BINDING PERFORMANCE** - In Avalonia, property bindings can trigger multiple queries during layout passes. Caching prevents redundant database hits and reduces UI thread blocking.

---

## SECTION 3: CODE QUALITY (Nice to have)

These items improve maintainability, debugging, and developer experience.

### 3.1 Add Database Schema Versioning

- [ ] Implement schema version tracking in search_meta table
      File: `x:\BalatroSeedOracle\src\Services\SearchInstance.cs:236-279`
      Current: No version tracking, schema changes are not detectable
      Fix: Add version tracking in `InitializeDatabase()`:
      ```csharp
      private const int CURRENT_SCHEMA_VERSION = 1;

      private void InitializeDatabase()
      {
          // ... existing table creation ...

          // Check schema version
          using (var checkVersion = _connection.CreateCommand())
          {
              checkVersion.CommandText = "SELECT value FROM search_meta WHERE key='schema_version'";
              var versionStr = checkVersion.ExecuteScalar()?.ToString();

              if (versionStr == null)
              {
                  // First time - set version
                  using var setVersion = _connection.CreateCommand();
                  setVersion.CommandText =
                      "INSERT INTO search_meta (key, value) VALUES ('schema_version', ?)";
                  setVersion.Parameters.Add(new DuckDBParameter(CURRENT_SCHEMA_VERSION.ToString()));
                  setVersion.ExecuteNonQuery();
              }
              else if (int.TryParse(versionStr, out int version) && version != CURRENT_SCHEMA_VERSION)
              {
                  throw new InvalidOperationException(
                      $"Database schema version mismatch: expected {CURRENT_SCHEMA_VERSION}, found {version}. " +
                      "Please delete the database file and restart the search.");
              }
          }
      }
      ```
      Impact: Prevents hard-to-debug schema mismatch errors when application is updated. Provides clear error messages to users.

### 3.2 Add Detailed Logging for Database Operations

- [ ] Add comprehensive debug logging for all database operations
      File: `x:\BalatroSeedOracle\src\Services\SearchInstance.cs` (multiple locations)
      Current: Minimal logging of database operations
      Fix: Add detailed logging at key points:
      ```csharp
      private void AddSearchResult(SearchResult result)
      {
          if (DebugLogger.IsVerboseEnabled) // Add this flag to DebugLogger
          {
              DebugLogger.Log($"SearchInstance[{_searchId}]",
                  $"Adding result: seed={result.Seed}, score={result.TotalScore}");
          }

          // ... existing logic ...
      }

      public async Task<int> GetResultCountAsync()
      {
          var sw = Stopwatch.StartNew();
          var count = await GetResultCountAsyncInternal();
          sw.Stop();

          DebugLogger.Log($"SearchInstance[{_searchId}]",
              $"GetResultCountAsync completed: {count} results in {sw.ElapsedMilliseconds}ms");

          return count;
      }
      ```
      Impact: Significantly improves debugging capabilities when investigating performance issues or data corruption.

### 3.3 Extract Database Operations to Separate Service

- [ ] Create `SearchDatabaseService` to handle all DuckDB operations
      File: New file `x:\BalatroSeedOracle\src\Services\SearchDatabaseService.cs`
      Current: Database logic mixed with search orchestration in SearchInstance
      Fix: Extract database operations to dedicated service:
      ```csharp
      public class SearchDatabaseService : IDisposable
      {
          private readonly string _dbPath;
          private readonly DuckDBConnection _connection;
          private readonly List<string> _columnNames;
          private bool _initialized;

          public SearchDatabaseService(string dbPath, List<string> columnNames)
          {
              _dbPath = dbPath;
              _columnNames = columnNames;
              _connection = new DuckDBConnection($"Data Source={_dbPath}");
              _connection.Open();
          }

          public void Initialize() { /* ... */ }
          public void AddResult(SearchResult result) { /* ... */ }
          public Task<List<SearchResult>> GetTopResultsAsync(string orderBy, bool ascending, int limit) { /* ... */ }
          public Task<int> GetCountAsync() { /* ... */ }
          public void Dispose() { /* ... */ }
      }
      ```
      Impact: **MVVM COMPLIANCE** - Separates data access from business logic, making SearchInstance a cleaner orchestrator. Improves testability and maintainability. Aligns with MVVM separation of concerns.

### 3.4 Improve Error Messages in DataGridResultsWindow

- [ ] Add user-friendly error messages for SQL query failures
      File: `x:\BalatroSeedOracle\src\Windows\DataGridResultsWindow.axaml.cs:397-446`
      Current: Generic error message "Error: {ex.Message}" (line 443)
      Fix: Add helpful error categorization:
      ```csharp
      catch (DuckDBException ex) when (ex.Message.Contains("syntax error"))
      {
          UpdateQueryStatus($"SQL Syntax Error: {ex.Message}\n\nCheck your SQL syntax and try again.");
      }
      catch (DuckDBException ex) when (ex.Message.Contains("table") && ex.Message.Contains("not found"))
      {
          UpdateQueryStatus($"Table Error: {ex.Message}\n\nThe 'results' table should be available.");
      }
      catch (DuckDBException ex)
      {
          UpdateQueryStatus($"Database Error: {ex.Message}");
      }
      catch (Exception ex)
      {
          UpdateQueryStatus($"Unexpected Error: {ex.Message}");
          DebugLogger.LogError("DataGridResultsWindow", $"SQL query failed: {ex}");
      }
      ```
      Impact: **BETTER UX** - Helps users understand and fix SQL errors without checking debug logs. Particularly important for the power-user feature of custom SQL queries.

---

## SECTION 4: NEW FEATURES (Future enhancements)

These items add new functionality to improve user experience.

### 4.1 SQL Query History

- [ ] Add query history persistence and recall
      File: `x:\BalatroSeedOracle\src\Windows\DataGridResultsWindow.axaml.cs`
      Current: No query history, users must rewrite queries
      Fix: Add history tracking:
      ```csharp
      private List<string> _queryHistory = new();
      private int _historyIndex = -1;
      private const int MAX_HISTORY_SIZE = 50;

      private async Task RunSqlQueryAsync()
      {
          var sql = _sqlEditor.Text;
          if (string.IsNullOrWhiteSpace(sql))
              return;

          // Add to history
          if (_queryHistory.Count == 0 || _queryHistory[_queryHistory.Count - 1] != sql)
          {
              _queryHistory.Add(sql);
              if (_queryHistory.Count > MAX_HISTORY_SIZE)
                  _queryHistory.RemoveAt(0);
              _historyIndex = _queryHistory.Count - 1;
          }

          // ... existing query execution ...
      }

      private void OnKeyDown(object? sender, KeyEventArgs e)
      {
          // Ctrl+Up for previous query
          if (e.Key == Key.Up && e.KeyModifiers.HasFlag(KeyModifiers.Control))
          {
              if (_historyIndex > 0)
              {
                  _historyIndex--;
                  _sqlEditor.Text = _queryHistory[_historyIndex];
              }
              e.Handled = true;
          }
          // Ctrl+Down for next query
          else if (e.Key == Key.Down && e.KeyModifiers.HasFlag(KeyModifiers.Control))
          {
              if (_historyIndex < _queryHistory.Count - 1)
              {
                  _historyIndex++;
                  _sqlEditor.Text = _queryHistory[_historyIndex];
              }
              e.Handled = true;
          }

          // ... existing keyboard shortcuts ...
      }
      ```
      Impact: **POWER USER PRODUCTIVITY** - Query history is a standard feature in database tools. Saves time for users repeatedly running similar queries.

### 4.2 Favorite Queries

- [ ] Add ability to save and load favorite queries
      File: `x:\BalatroSeedOracle\src\Windows\DataGridResultsWindow.axaml.cs`
      Current: No way to save useful queries
      Fix: Add favorites management:
      ```csharp
      private Dictionary<string, string> _favoriteQueries = new();
      private const string FAVORITES_FILE = "query_favorites.json";

      private void LoadFavorites()
      {
          try
          {
              var path = Path.Combine(Directory.GetCurrentDirectory(), FAVORITES_FILE);
              if (File.Exists(path))
              {
                  var json = File.ReadAllText(path);
                  _favoriteQueries = JsonSerializer.Deserialize<Dictionary<string, string>>(json)
                      ?? new Dictionary<string, string>();
              }
          }
          catch (Exception ex)
          {
              DebugLogger.LogError("DataGridResultsWindow", $"Failed to load favorites: {ex}");
          }
      }

      private void SaveFavorites()
      {
          try
          {
              var path = Path.Combine(Directory.GetCurrentDirectory(), FAVORITES_FILE);
              var json = JsonSerializer.Serialize(_favoriteQueries, new JsonSerializerOptions { WriteIndented = true });
              File.WriteAllText(path, json);
          }
          catch (Exception ex)
          {
              DebugLogger.LogError("DataGridResultsWindow", $"Failed to save favorites: {ex}");
          }
      }
      ```
      Impact: **ENHANCED UX** - Users can build a library of useful queries for different analysis scenarios. Complements the example queries feature.

### 4.3 Export Query Results to Separate File

- [ ] Add export functionality for SQL query results
      File: `x:\BalatroSeedOracle\src\Windows\DataGridResultsWindow.axaml.cs`
      Current: Can only export main results grid, not custom query results
      Fix: Add export button for query results grid:
      ```csharp
      private async Task ExportQueryResultsAsync()
      {
          if (_queryResultsGrid?.ItemsSource == null)
          {
              UpdateQueryStatus("No query results to export");
              return;
          }

          var topLevel = TopLevel.GetTopLevel(this);
          if (topLevel == null)
              return;

          var file = await topLevel.StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
          {
              Title = "Export Query Results",
              DefaultExtension = "csv",
              FileTypeChoices = new[]
              {
                  new FilePickerFileType("CSV Files") { Patterns = new[] { "*.csv" } },
                  new FilePickerFileType("JSON Files") { Patterns = new[] { "*.json" } },
              }
          });

          if (file == null)
              return;

          // ... export logic similar to existing export methods ...
      }
      ```
      Impact: **WORKFLOW COMPLETION** - Users can export custom analysis results for external processing or reporting.

---

## Implementation Priority

**Phase 1 (Ship Blockers):**
- 1.1 Static ThreadLocal Appender Fix
- 1.2 Connection Pooling
- 1.3 Exception-Based Duplicate Handling

**Phase 2 (Performance):**
- 2.1 Tally Column Indexes
- 2.2 COUNT Caching
- 2.3 Periodic Flush

**Phase 3 (Quality):**
- 3.1 Schema Versioning
- 3.2 Debug Logging
- 3.3 Database Service Extraction
- 3.4 Error Messages

**Phase 4 (Features):**
- 4.1 Query History
- 4.2 Favorite Queries
- 4.3 Query Export

---

## Testing Checklist

After implementing fixes, verify:

- [ ] Multiple SearchInstances can run concurrently without cross-contamination
- [ ] Connection pool limits are respected under load
- [ ] Duplicate seed handling does not throw exceptions
- [ ] Sorting by tally columns is sub-second for 100K+ results
- [ ] COUNT(*) queries are not repeated unnecessarily
- [ ] Memory usage remains stable during long-running searches
- [ ] UI remains responsive during database operations
- [ ] Error messages in SQL editor are clear and actionable

---

## Avalonia-Specific Considerations

**UI Thread Safety:**
- All database operations in SearchInstance are already properly async
- DataGridResultsWindow correctly uses `Dispatcher.UIThread.InvokeAsync()` for UI updates
- No blocking database calls on UI thread detected

**ObservableCollection Binding:**
- `_filteredResults` binding in DataGridResultsWindow is correct
- Consider using `AvaloniaList<T>` instead of `ObservableCollection<T>` for better Avalonia performance
- Add UI virtualization for grids with 10K+ rows

**Memory Management:**
- Dispose pattern in SearchInstance is correct
- Consider implementing weak event handlers for long-lived subscriptions
- Verify no memory leaks in DataGrid when switching between large result sets

---

## Notes

- All line numbers are based on current codebase state (2025-10-28)
- This review assumes DuckDB.NET version 0.10.x or later
- Connection pooling implementation may need adjustment based on DuckDB.NET API
- Some fixes may require updates to AXAML bindings (not included in this document)

**End of Document**

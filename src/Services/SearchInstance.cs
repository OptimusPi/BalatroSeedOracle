using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text.Json;
using DuckDB.NET.Native; // for NativeMethods.Appender.DuckDBAppenderFlush
using System.Threading;
using System.Threading.Tasks;
using DuckDB.NET.Data;
using Motely;
using Motely.Filters;
using BalatroSeedOracle.Helpers;
using BalatroSeedOracle.Models;
using BalatroSeedOracle.Views.Modals;
using SearchResultEventArgs = BalatroSeedOracle.Models.SearchResultEventArgs;
using DebugLogger = BalatroSeedOracle.Helpers.DebugLogger;
using SearchResult = BalatroSeedOracle.Models.SearchResult;

namespace BalatroSeedOracle.Services
{
    /// <summary>
    /// Represents a single search instance that can run independently
    /// </summary>
    public class SearchInstance : IDisposable
    {
        private readonly string _searchId;
        private readonly UserProfileService? _userProfileService;
        
        // DuckDB database for this search instance
        private string _dbPath = string.Empty;
        private string _connectionString = string.Empty;
        private List<string> _columnNames = new List<string>();
        private CancellationTokenSource? _cancellationTokenSource;
        private volatile bool _isRunning;
        private volatile bool _isPaused;
        private Motely.Filters.MotelyJsonConfig? _currentConfig;
        private IMotelySearch? _currentSearch;
        private DateTime _searchStartTime;
        private readonly ObservableCollection<BalatroSeedOracle.Models.SearchResult> _results;
        private readonly ConcurrentQueue<BalatroSeedOracle.Models.SearchResult> _pendingResults = new();
        private readonly List<string> _consoleHistory = new();
        private readonly object _consoleHistoryLock = new();
        private volatile int _resultCount = 0;
        private readonly List<string> _recentSeeds = new();
        private readonly object _recentSeedsLock = new();
        private Task? _searchTask;
        private bool _preventStateSave = false;  // Flag to prevent saving state when icon is removed
        
    // Persistent DuckDB connection & appender for high throughput streaming inserts
    private readonly DuckDBConnection _connection; // opened in ctor, never null after
    private DuckDB.NET.Data.DuckDBAppender? _appender; // fastest row streaming API (REQUIRES our own synchronization)
    private readonly object _appenderSync = new(); // guards _appender create/close and row appends
    // Track last successful manual flush (for diagnostics only)
    private DateTime _lastAppenderFlush = DateTime.UtcNow;
    // Fast in-memory duplicate filter (only stores successful inserts). Assumes result count << total search space.
    private readonly HashSet<string> _seenSeeds = new HashSet<string>(StringComparer.Ordinal);
    // Track rows added since last flush for periodic flushing
    private int _rowsSinceFlush = 0;
    private const int FLUSH_EVERY_N_ROWS = 100; // Flush every 100 rows for responsive UI
        
        // Search state tracking for resume
        private SearchConfiguration? _currentSearchConfig;
        #pragma warning disable CS0414
        private ulong _lastSavedBatch = 0;
        #pragma warning restore CS0414
        private DateTime _lastStateSave = DateTime.UtcNow;

        // Properties
        public string SearchId
        {
            get => _searchId;
        }
        public bool IsRunning => _isRunning;
        public bool IsPaused => _isPaused;
        public ObservableCollection<BalatroSeedOracle.Models.SearchResult> Results => _results;
        public TimeSpan SearchDuration => DateTime.UtcNow - _searchStartTime;
        public DateTime SearchStartTime => _searchStartTime;
        public string ConfigPath { get; private set; } = string.Empty;
        public string FilterName { get; private set; } = "Unknown";
        public Motely.Filters.MotelyJsonConfig? GetFilterConfig() => _currentConfig;
        public int ResultCount => _resultCount;
    public IReadOnlyList<string> ColumnNames => _columnNames.AsReadOnly();
    public string DatabasePath => _dbPath;
    private bool _dbInitialized = false;
    public bool IsDatabaseInitialized => _dbInitialized;
    public bool HasDatabase => _dbInitialized; // UI compatibility
        
        /// <summary>
        /// Gets the console output history for this search
        /// </summary>
        public List<string> GetConsoleHistory()
        {
            lock (_consoleHistoryLock)
            {
                return new List<string>(_consoleHistory);
            }
        }
        
        /// <summary>
        /// Adds a message to the console history
        /// </summary>
        private void AddToConsole(string message)
        {
            lock (_consoleHistoryLock)
            {
                var timestamp = DateTime.UtcNow.ToString("HH:mm:ss");
                _consoleHistory.Add($"[{timestamp}] {message}");
            }
        }

        // Events for UI integration
        public event EventHandler? SearchStarted;
        public event EventHandler? SearchCompleted;
        #pragma warning disable CS0067
        public event EventHandler<SearchProgressEventArgs>? ProgressUpdated;
        #pragma warning restore CS0067
        public event EventHandler<SearchResultEventArgs>? ResultFound; // Keep for compatibility but rarely used

        public SearchInstance(string searchId, string dbPath)
        {
            _searchId = searchId;
            _userProfileService = ServiceHelper.GetService<UserProfileService>();
            _results = new ObservableCollection<BalatroSeedOracle.Models.SearchResult>();

            // Require a non-empty path immediately so query helpers are safe to call early
            if (string.IsNullOrWhiteSpace(dbPath))
                throw new ArgumentException("dbPath is required", nameof(dbPath));

            var dir = Path.GetDirectoryName(dbPath);
            if (!string.IsNullOrEmpty(dir)) Directory.CreateDirectory(dir);
            _dbPath = dbPath;
            _connectionString = $"Data Source={_dbPath}";
            // Open persistent connection immediately (simple, no EnsureConnection later)
            _connection = new DuckDBConnection(_connectionString);
            _connection.Open();
        }
        
    private void SetupDatabase(Motely.Filters.MotelyJsonConfig config, string configPath)
        {
            // Build column names from the Should[] criteria
            _columnNames.Clear();
            _columnNames.Add("seed");
            _columnNames.Add("score");
            
            if (config.Should != null && config.Should.Count > 0)
            {
                var seenNames = new HashSet<string>();
                foreach (var should in config.Should)
                {
                    var baseName = FormatColumnName(should);
                    var colName = baseName;
                    
                    int suffix = 2;
                    while (seenNames.Contains(colName))
                    {
                        colName = $"{baseName}_{suffix}";
                        suffix++;
                    }
                    
                    seenNames.Add(colName);
                    _columnNames.Add(colName);
                }
            }
            
            // If constructor already supplied a path, keep it; otherwise derive from filter name
            if (string.IsNullOrEmpty(_dbPath))
            {
                var filterName = Path.GetFileNameWithoutExtension(configPath);
                var searchResultsDir = Path.Combine(Directory.GetCurrentDirectory(), "SearchResults");
                Directory.CreateDirectory(searchResultsDir);
                _dbPath = Path.Combine(searchResultsDir, $"{filterName}.duckdb");
                _connectionString = $"Data Source={_dbPath}";
            }
            DebugLogger.LogImportant($"SearchInstance[{_searchId}]", $"Database configured with {_columnNames.Count} columns at {_dbPath}");
            
            InitializeDatabase();
        }
        
        private string FormatColumnName(Motely.Filters.MotelyJsonConfig.MotleyJsonFilterClause should)
        {
            if (should == null) return "should";
            
            var name = !string.IsNullOrEmpty(should.Value) ? should.Value : should.Type;
            if (!string.IsNullOrEmpty(should.Edition))
                name = should.Edition + "_" + name;
            
            if (should.Antes != null && should.Antes.Length > 0)
                name += "_ante" + should.Antes[0];
                
            if (string.IsNullOrEmpty(name))
                name = "column";
            
            var safeName = System.Text.RegularExpressions.Regex.Replace(name, @"[^a-zA-Z0-9_]", "_").ToLower();
            
            if (char.IsDigit(safeName[0]))
                safeName = "col_" + safeName;
                
            return safeName;
        }
        
        private void InitializeDatabase()
        {
            try
            {
                var columnDefs = new List<string> { "seed VARCHAR PRIMARY KEY", "score INT" };
                for (int i = 2; i < _columnNames.Count; i++) columnDefs.Add($"{_columnNames[i]} INT");

                using (var createTable = _connection.CreateCommand())
                {
                    createTable.CommandText = $@"CREATE TABLE IF NOT EXISTS results (
                        {string.Join(",\n                        ", columnDefs)}
                    )";
                    createTable.ExecuteNonQuery();
                }
                using (var createIndex = _connection.CreateCommand())
                {
                    createIndex.CommandText = "CREATE INDEX IF NOT EXISTS idx_score ON results(score DESC);";
                    createIndex.ExecuteNonQuery();
                }
                using (var createMeta = _connection.CreateCommand())
                {
                    createMeta.CommandText = "CREATE TABLE IF NOT EXISTS search_meta ( key VARCHAR PRIMARY KEY, value VARCHAR )";
                    createMeta.ExecuteNonQuery();
                }
                _appender?.Dispose();
                _appender = _connection.CreateAppender("results");
                // Preload existing seeds for fast duplicate suppression (resume scenarios)
                try
                {
                    using var preload = _connection.CreateCommand();
                    preload.CommandText = "SELECT seed FROM results";
                    using var reader = preload.ExecuteReader();
                    int preloadCount = 0;
                    while (reader.Read())
                    {
                        var s = reader.GetString(0);
                        _seenSeeds.Add(s);
                        preloadCount++;
                    }
                    if (preloadCount > 0)
                        DebugLogger.LogImportant($"SearchInstance[{_searchId}]", $"Preloaded {preloadCount:N0} existing seeds into duplicate filter");
                }
                catch (Exception px)
                {
                    DebugLogger.LogError($"SearchInstance[{_searchId}]", $"Failed preloading existing seeds: {px.Message}");
                }
                _dbInitialized = true;
                DebugLogger.Log($"SearchInstance[{_searchId}]", $"Database initialized with {_columnNames.Count} columns");
            }
            catch (Exception ex)
            {
                DebugLogger.LogError($"SearchInstance[{_searchId}]", $"Failed to initialize database: {ex.Message}");
                throw;
            }
        }

        // Explicit flush of the native appender so queries see latest rows.
        // Safe to call frequently; only does work if appender exists.
        private void CloseAppender()
        {
            lock (_appenderSync)
            {
                if (_appender == null) return;
                try
                {
                    // Close finalizes the appender (native duckdb_appender_close) without destroying table
                    var closeMethod = _appender.GetType().GetMethod("Close", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
                    closeMethod?.Invoke(_appender, null);
                    _lastAppenderFlush = DateTime.UtcNow;
                }
                catch (Exception ex)
                {
                    DebugLogger.LogError($"SearchInstance[{_searchId}]", $"CloseAppender failed: {ex.Message}");
                }
            }
        }

        // Force a flush so queries see latest rows without stopping search
        public void ForceFlush()
        {
            lock (_appenderSync)
            {
                if (_appender == null) return;
                try
                {
                    // Close & recreate inside same lock so no concurrent row writes see a disposed appender
                    var old = _appender;
                    CloseAppender(); // will re-lock but is quick; could refactor to internal variant if needed
                    _appender = _connection.CreateAppender("results");
                    _rowsSinceFlush = 0; // Reset the counter after flush
                    DebugLogger.Log($"SearchInstance[{_searchId}]", "ForceFlush executed (appender recycled)");
                }
                catch (Exception ex)
                {
                    DebugLogger.LogError($"SearchInstance[{_searchId}]", $"ForceFlush failed: {ex.Message}");
                }
            }
        }

        private void AddSearchResult(SearchResult result)
        {
            if (!_dbInitialized) return;
            try
            {
                // Duplicate suppression outside lock (fast path) – BUT we must re-check inside lock to avoid race if ForceFlush resets seed set (currently it doesn't).
                if (!_seenSeeds.Add(result.Seed)) return;

                lock (_appenderSync)
                {
                    if (_appender == null) return; // search shutting down
                    var row = _appender.CreateRow();
                    row.AppendValue(result.Seed).AppendValue(result.TotalScore);
                    int tallyCount = _columnNames.Count - 2;
                    for (int i = 0; i < tallyCount; i++)
                    {
                        int val = (result.Scores != null && i < result.Scores.Length) ? result.Scores[i] : 0;
                        row.AppendValue(val);
                    }
                    row.EndRow();
                    
                    // Increment counter and check if we need to flush
                    _rowsSinceFlush++;
                    if (_rowsSinceFlush >= FLUSH_EVERY_N_ROWS)
                    {
                        // Time to flush for responsive UI
                        _rowsSinceFlush = 0;
                        // Don't call ForceFlush here as it would deadlock (we're inside _appenderSync lock)
                        // Instead, close and recreate the appender inline
                        try
                        {
                            var closeMethod = _appender.GetType().GetMethod("Close", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
                            closeMethod?.Invoke(_appender, null);
                            _appender = _connection.CreateAppender("results");
                            _lastAppenderFlush = DateTime.UtcNow;
                            DebugLogger.Log($"SearchInstance[{_searchId}]", $"Auto-flushed after {FLUSH_EVERY_N_ROWS} rows");
                        }
                        catch (Exception flushEx)
                        {
                            DebugLogger.LogError($"SearchInstance[{_searchId}]", $"Auto-flush failed: {flushEx.Message}");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                string msg = ex.ToString();
                if (msg.Contains("Duplicate key") || msg.Contains("violates primary key"))
                {
                    // Silently ignore - duplicates are already filtered by _seenSeeds HashSet
                    // This can happen when auto-cutoff increases and reprocesses batches
                    return;
                }
                else
                {
                    // SILENCE - Don't spam console with threading errors
                    // DebugLogger.LogError($"SearchInstance[{_searchId}]", $"Failed to add result: {ex.Message}");
                }
            }
            return;
        }

        public async Task<List<BalatroSeedOracle.Models.SearchResult>> GetResultsPageAsync(int offset, int limit)
        {
            var list = new List<BalatroSeedOracle.Models.SearchResult>();
            if (!_dbInitialized) return list;
            
            // Rely on row.EndRow() visibility; do NOT close appender mid-search.
            
            using var cmd = _connection.CreateCommand();
            cmd.CommandText = $"SELECT * FROM results ORDER BY score DESC LIMIT {limit} OFFSET {offset}";
            using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                var seed = reader.GetString(0);
                var score = reader.GetInt32(1);
                int tallyCount = _columnNames.Count - 2;
                int[]? scores = null;
                if (tallyCount > 0)
                {
                    scores = new int[tallyCount];
                    for (int i = 0; i < tallyCount; i++)
                    {
                        int columnIndex = i + 2;
                        if (columnIndex < reader.FieldCount && !reader.IsDBNull(columnIndex))
                        {
                            scores[i] = reader.GetInt32(columnIndex);
                        }
                        else
                        {
                            scores[i] = 0; // Default value if column doesn't exist
                        }
                    }
                }
                list.Add(new BalatroSeedOracle.Models.SearchResult { Seed = seed, TotalScore = score, Scores = scores });
            }
            return list;
        }

        /// <summary>
        /// Get the top N results ordered by a specified column (seed, score, or tally index)
        /// for the simplified manual-loading results grid. Tallies are addressed by
        /// providing a column name from _columnNames (after validation) or by passing
        /// the special form "tally{index}" where index maps to the tally array position.
        /// </summary>
        /// <param name="orderBy">seed | score | tally{n}</param>
        /// <param name="ascending">True for ASC, false for DESC</param>
        /// <param name="limit">Max rows to return (default 1000)</param>
        public async Task<List<BalatroSeedOracle.Models.SearchResult>> GetTopResultsAsync(string orderBy, bool ascending, int limit = 1000)
        {
            var results = new List<BalatroSeedOracle.Models.SearchResult>();
            if (limit <= 0) return results;

            if (!_dbInitialized)
            {
                DebugLogger.Log($"SearchInstance[{_searchId}]", "GetTopResultsAsync called before DB init complete");
                return results;
            }
            
            // Force flush the appender to ensure all buffered results are visible
            ForceFlush();

            // Resolve order by column safely
            string resolvedColumn = "score"; // default
            if (!string.IsNullOrEmpty(orderBy))
            {
                if (orderBy.Equals("seed", StringComparison.OrdinalIgnoreCase)) resolvedColumn = "seed";
                else if (orderBy.Equals("score", StringComparison.OrdinalIgnoreCase)) resolvedColumn = "score";
                else if (orderBy.StartsWith("tally", StringComparison.OrdinalIgnoreCase))
                {
                    if (int.TryParse(orderBy.Substring(5), out int tallyIndex))
                    {
                        int duckIndex = tallyIndex + 2; // shift for seed,score
                        if (duckIndex >= 2 && duckIndex < _columnNames.Count)
                        {
                            resolvedColumn = _columnNames[duckIndex];
                        }
                    }
                }
                else
                {
                    // If a raw column name supplied and matches list, accept
                    if (_columnNames.Contains(orderBy)) resolvedColumn = orderBy;
                }
            }
            
            // Rely on row.EndRow() visibility; appender stays open during active search.

            using var cmd = _connection.CreateCommand();
            cmd.CommandText = $"SELECT * FROM results ORDER BY {resolvedColumn} {(ascending ? "ASC" : "DESC")} LIMIT {limit}";
            using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                var seed = reader.GetString(0);
                var score = reader.GetInt32(1);
                int tallyCount = _columnNames.Count - 2;
                int[]? scores = null;
                if (tallyCount > 0)
                {
                    scores = new int[tallyCount];
                    for (int i = 0; i < tallyCount; i++)
                    {
                        int columnIndex = i + 2;
                        if (columnIndex < reader.FieldCount && !reader.IsDBNull(columnIndex))
                        {
                            scores[i] = reader.GetInt32(columnIndex);
                        }
                        else
                        {
                            scores[i] = 0; // Default value if column doesn't exist
                        }
                    }
                }
                results.Add(new BalatroSeedOracle.Models.SearchResult { Seed = seed, TotalScore = score, Scores = scores });
            }
            if (results.Count == 0)
            {
                DebugLogger.Log($"SearchInstance[{_searchId}]", "GetTopResultsAsync returned 0 rows");
            }
            return results;
        }

        public async Task<List<BalatroSeedOracle.Models.SearchResult>> GetAllResultsAsync()
        {
            return await GetResultsPageAsync(0, 420_069);
        }

        public async Task<int> GetResultCountAsync()
        {
            if (!_dbInitialized)
                throw new InvalidOperationException("Database not initialized");
            
            // Force flush to ensure all buffered results are counted
            ForceFlush();
            
            using var cmd = _connection.CreateCommand();
            cmd.CommandText = "SELECT COUNT(*) FROM results";
            var v = await cmd.ExecuteScalarAsync();
            return v == null ? 0 : Convert.ToInt32(v);
        }

        public async Task<int> ExportResultsAsync(string filePath)
        {
            try
            {
                if (!_dbInitialized) return 0;
                var count = await GetResultCountAsync();
                if (count == 0) return 0;
                using var cmd = _connection.CreateCommand();
                cmd.CommandText = "SELECT * FROM results ORDER BY score DESC";
                using var reader = await cmd.ExecuteReaderAsync();
                using var writer = new StreamWriter(filePath);
                await writer.WriteLineAsync(string.Join(",", _columnNames));
                while (await reader.ReadAsync())
                {
                    var values = new string[_columnNames.Count];
                    for (int i = 0; i < _columnNames.Count && i < reader.FieldCount; i++)
                    {
                        values[i] = reader.IsDBNull(i) ? string.Empty : reader.GetValue(i)?.ToString() ?? string.Empty;
                    }
                    await writer.WriteLineAsync(string.Join(",", values));
                }
                return count;
            }
            catch (Exception ex)
            {
                DebugLogger.LogError($"SearchInstance[{_searchId}]", $"Failed to export results: {ex.Message}");
                throw;
            }
        }

        public async Task<ulong?> GetLastBatchAsync()
        {
            try
            {
                if (!_dbInitialized) return null;
                using var cmd = _connection.CreateCommand();
                cmd.CommandText = "SELECT value FROM search_meta WHERE key='last_batch'";
                var val = await cmd.ExecuteScalarAsync();
                if (val == null) return null;
                return ulong.TryParse(val.ToString(), out var batch) ? batch : null;
            }
            catch (Exception ex)
            {
                DebugLogger.LogError($"SearchInstance[{_searchId}]", $"Failed to get last batch: {ex.Message}");
                throw;
            }
        }
        
        private async Task SaveLastBatchAsync(ulong batchNumber)
        {
            try
            {
                if (!_dbInitialized) return;
                using var cmd = _connection.CreateCommand();
                cmd.CommandText = "INSERT OR REPLACE INTO search_meta (key, value) VALUES ('last_batch', ?)";
                cmd.Parameters.Add(new DuckDBParameter(batchNumber.ToString()));
                await cmd.ExecuteNonQueryAsync();
            }
            catch (Exception ex)
            {
                DebugLogger.LogError($"SearchInstance[{_searchId}]", $"Failed to save last batch: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// Start searching with a file path
        /// </summary>
        private async Task StartSearchFromFileAsync(
            string configPath,
            SearchConfiguration config,
            IProgress<SearchProgress>? progress = null,
            CancellationToken cancellationToken = default
        )
        {
            DebugLogger.LogImportant($"SearchInstance[{_searchId}]", $"StartSearchFromFileAsync ENTERED! configPath={configPath}");
            
            if (_isRunning)
            {
                DebugLogger.Log($"SearchInstance[{_searchId}]", "Search already running");
                return;
            }

            try
            {
                DebugLogger.Log(
                    $"SearchInstance[{_searchId}]",
                    $"Starting search from file: {configPath}"
                );

                // Load the config from file - use TryLoadFromJsonFile to get PostProcess!
                if (!Motely.Filters.MotelyJsonConfig.TryLoadFromJsonFile(configPath, out var ouijaConfig))
                {
                    throw new Exception($"Failed to load config from {configPath}");
                }
                
                _currentConfig = ouijaConfig;
                _currentSearchConfig = config;
                ConfigPath = configPath;
                FilterName = ouijaConfig.Name ?? Path.GetFileNameWithoutExtension(configPath);
                _searchStartTime = DateTime.UtcNow;
                _isRunning = true;
                
                _cancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(
                    cancellationToken
                );

                // Clear previous results and pending queue
                Avalonia.Threading.Dispatcher.UIThread.Post(() =>
                {
                    _results.Clear();
                });
                
                // Clear pending queue
                while (_pendingResults.TryDequeue(out _)) { }

                // Notify UI that search started
                SearchStarted?.Invoke(this, EventArgs.Empty);

                // Progress is handled directly in motelyProgress callback below

                // Set up search configuration from the modal
                var searchCriteria = new SearchCriteria
                {
                    ThreadCount = config.ThreadCount,
                    MinScore = config.MinScore,
                    BatchSize = config.BatchSize,
                    Deck = config.Deck ?? "Red",
                    Stake = config.Stake ?? "White",
                    StartBatch = config.StartBatch,
                    EndBatch = config.EndBatch,
                    EnableDebugOutput = config.DebugMode,
                    DebugSeed = config.DebugSeed,
                };

                // Database should already be configured by StartSearchAsync
                DebugLogger.LogImportant($"SearchInstance[{_searchId}]", $"Database properly configured with {_columnNames.Count} columns");

                // Register direct callback with Motely.Filters.MotelyJsonFilterDesc
                DebugLogger.LogImportant($"SearchInstance[{_searchId}]", "Registering OnResultFound callback");
                DebugLogger.LogImportant($"SearchInstance[{_searchId}]", $"Motely.Filters.MotelyJsonConfig has {ouijaConfig.Must?.Count ?? 0} MUST clauses");
                DebugLogger.LogImportant($"SearchInstance[{_searchId}]", $"Motely.Filters.MotelyJsonConfig has {ouijaConfig.Should?.Count ?? 0} SHOULD clauses");
                DebugLogger.LogImportant($"SearchInstance[{_searchId}]", $"Motely.Filters.MotelyJsonConfig has {ouijaConfig.MustNot?.Count ?? 0} MUST NOT clauses");
                
                // Log the actual filter content for debugging
                if (ouijaConfig.Must != null)
                {
                    foreach (var must in ouijaConfig.Must)
                    {
                        DebugLogger.LogImportant($"SearchInstance[{_searchId}]", $"  MUST: Type={must.Type}, Value={must.Value}");
                    }
                }
                if (ouijaConfig.Should != null)
                {
                    foreach (var should in ouijaConfig.Should)
                    {
                        DebugLogger.LogImportant($"SearchInstance[{_searchId}]", $"  SHOULD: Type={should.Type}, Value={should.Value}, Score={should.Score}");
                    }
                }
                
                // (Result callback now assigned inside RunSearchInProcess after filter creation)

                // Run the search using the MotelySearchService pattern
                DebugLogger.Log($"SearchInstance[{_searchId}]", "Starting in-process search...");

                _searchTask = Task.Run(
                    () =>
                        RunSearchInProcess(
                            ouijaConfig,
                            searchCriteria,
                            progress,
                            _cancellationTokenSource.Token
                        ),
                    _cancellationTokenSource.Token
                );
                
                await _searchTask;

                DebugLogger.Log($"SearchInstance[{_searchId}]", "Search completed");
            }
            catch (OperationCanceledException)
            {
                DebugLogger.Log($"SearchInstance[{_searchId}]", "Search was cancelled");
            }
            catch (Exception ex)
            {
                DebugLogger.LogError(
                    $"SearchInstance[{_searchId}]",
                    $"Search failed: {ex.Message}"
                );
                progress?.Report(
                    new SearchProgress
                    {
                        Message = $"Search failed: {ex.Message}",
                        HasError = true,
                        IsComplete = true,
                    }
                );
            }
            finally
            {
                _isRunning = false;
                SearchCompleted?.Invoke(this, EventArgs.Empty);
            }
        }

        /// <summary>
        /// Start searching with a config file path
        /// </summary>
        public async Task StartSearchAsync(
            SearchCriteria criteria,
            IProgress<SearchProgress>? progress = null,
            CancellationToken cancellationToken = default
        )
        {
            DebugLogger.LogImportant($"SearchInstance[{_searchId}]", $"StartSearchAsync ENTERED! ConfigPath={criteria.ConfigPath}");
            
            // Load the config from file
            if (string.IsNullOrEmpty(criteria.ConfigPath))
            {
                throw new ArgumentException("Config path is required");
            }
            
            DebugLogger.Log(
                $"SearchInstance[{_searchId}]",
                $"Loading config from: {criteria.ConfigPath}"
            );

            // Load and validate the Ouija config - use LoadFromJson to get PostProcess!
            if (!Motely.Filters.MotelyJsonConfig.TryLoadFromJsonFile(criteria.ConfigPath, out var config))
            {
                throw new Exception($"Failed to load config from {criteria.ConfigPath}");
            }
            
            // DEBUG: Log what was actually deserialized
            DebugLogger.LogImportant($"SearchInstance[{_searchId}]", $"JSON DESERIALIZATION RESULT:");
            DebugLogger.LogImportant($"SearchInstance[{_searchId}]", $"  Config.Name: '{config.Name}'");
            DebugLogger.LogImportant($"SearchInstance[{_searchId}]", $"  Config.Must: {(config.Must?.Count ?? 0)} items");
            DebugLogger.LogImportant($"SearchInstance[{_searchId}]", $"  Config.Should: {(config.Should?.Count ?? 0)} items");
            DebugLogger.LogImportant($"SearchInstance[{_searchId}]", $"  Config.MustNot: {(config.MustNot?.Count ?? 0)} items");

            string? rawJsonForDebug = null; // only load if needed
            
            if (config.Should != null && config.Should.Count > 0)
            {
                DebugLogger.LogImportant($"SearchInstance[{_searchId}]", $"  First Should item: Type='{config.Should[0].Type}', Value='{config.Should[0].Value}'");
            }
            else
            {
                DebugLogger.LogError($"SearchInstance[{_searchId}]", $"  PROBLEM: Should array is null or empty after deserialization!");
                try
                {
                    rawJsonForDebug = System.IO.File.ReadAllText(criteria.ConfigPath);
                    DebugLogger.LogError($"SearchInstance[{_searchId}]", $"  Raw JSON length: {rawJsonForDebug.Length} characters");
                    DebugLogger.LogError($"SearchInstance[{_searchId}]", $"  First 500 chars of JSON: {rawJsonForDebug.Substring(0, Math.Min(500, rawJsonForDebug.Length))}");
                }
                catch (Exception rx)
                {
                    DebugLogger.LogError($"SearchInstance[{_searchId}]", $"  Failed to read raw JSON for debug: {rx.Message}");
                }
            }

            // Store the config path for reference
            ConfigPath = criteria.ConfigPath;
            FilterName = System.IO.Path.GetFileNameWithoutExtension(criteria.ConfigPath);

            // CRITICAL: Configure the SearchHistoryService BEFORE anything else!
            // This must happen before StartSearchFromFileAsync to ensure proper database schema
            DebugLogger.LogImportant($"SearchInstance[{_searchId}]", "Configuring SearchHistoryService with filter config");
            DebugLogger.LogImportant($"SearchInstance[{_searchId}]", $"Filter config loaded: Name='{config.Name}'");
            DebugLogger.LogImportant($"SearchInstance[{_searchId}]", $"  Must clauses: {config.Must?.Count ?? 0}");
            DebugLogger.LogImportant($"SearchInstance[{_searchId}]", $"  Should clauses: {config.Should?.Count ?? 0}");
            DebugLogger.LogImportant($"SearchInstance[{_searchId}]", $"  MustNot clauses: {config.MustNot?.Count ?? 0}");
            
            if (config.Should != null && config.Should.Count > 0)
            {
                for (int i = 0; i < config.Should.Count; i++)
                {
                    var should = config.Should[i];
                    DebugLogger.LogImportant($"SearchInstance[{_searchId}]", $"    Should[{i}]: Type={should.Type}, Value={should.Value}, Score={should.Score}");
                }
            }
            
            SetupDatabase(config, criteria.ConfigPath);
            DebugLogger.LogImportant($"SearchInstance[{_searchId}]", $"Database configured with {_columnNames.Count} columns");

            // Auto-resume logic now that DB is initialized
            if (criteria.StartBatch == 0 && !criteria.EnableDebugOutput)
            {
                try
                {
                    var last = await GetLastBatchAsync();
                    if (last.HasValue && last.Value > 0)
                    {
                        criteria.StartBatch = last.Value;
                        DebugLogger.LogImportant($"SearchInstance[{_searchId}]", $"Resuming from saved batch {last.Value:N0}");
                    }
                }
                catch (Exception resumeEx)
                {
                    DebugLogger.LogError($"SearchInstance[{_searchId}]", $"Resume check failed: {resumeEx.Message}");
                }
            }
            
            // Convert SearchCriteria to SearchConfiguration
            var searchConfig = new SearchConfiguration
            {
                ThreadCount = criteria.ThreadCount,
                MinScore = criteria.MinScore,
                BatchSize = criteria.BatchSize,
                Deck = criteria.Deck,
                Stake = criteria.Stake,
                StartBatch = criteria.StartBatch,
                EndBatch = criteria.EndBatch,
                DebugMode = criteria.EnableDebugOutput,
                DebugSeed = criteria.DebugSeed
            };

            // Call the main search method with the file path
            await StartSearchFromFileAsync(criteria.ConfigPath, searchConfig, progress, cancellationToken);
        }

        public void PauseSearch()
        {
            if (_isRunning && !_isPaused)
            {
                _isPaused = true;
                if (_currentSearch != null && _currentSearch.Status == MotelySearchStatus.Running)
                {
                    _currentSearch.Pause();
                }
                DebugLogger.Log($"SearchInstance[{_searchId}]", "Search paused");
            }
        }

        public void ResumeSearch()
        {
            if (_isRunning && _isPaused)
            {
                _isPaused = false;
                // Motely doesn't have a Resume method - search has to be restarted
                // For now, just update the paused flag
                DebugLogger.Log(
                    $"SearchInstance[{_searchId}]",
                    "Resume not supported - search needs to be restarted"
                );
            }
        }

        public void StopSearch()
        {
            StopSearch(false);
        }
        
        public void StopSearch(bool preventStateSave)
        {
            if (_isRunning)
            {
                DebugLogger.Log($"SearchInstance[{_searchId}]", "Stopping search...");
                
                _preventStateSave = preventStateSave;
                

                // Close then dispose appender so buffered rows are committed
                if (_appender != null)
                {
                    DebugLogger.Log($"SearchInstance[{_searchId}]", "Closing DuckDB appender...");
                    CloseAppender();
                    DebugLogger.Log($"SearchInstance[{_searchId}]", "Disposing DuckDB appender (finalizing buffered rows)...");
                    _appender.Dispose();
                    _appender = null;
                }
                
                // Flush any pending search state to disk before stopping (unless prevented)
                if (!preventStateSave)
                {
                    _userProfileService?.FlushProfile();
                }

                // Force _isRunning to false IMMEDIATELY
                _isRunning = false;
                
                // TODO: Fix cancellation API
                // Motely.Filters.MotelyJsonFilterDesc.OuijaJsonFilter.IsCancelled = true;

                // Cancel the token immediately
                _cancellationTokenSource?.Cancel();
                
                // Wait for the search task to complete (with timeout)
                if (_searchTask != null && !_searchTask.IsCompleted)
                {
                    try
                    {
                        // Wait up to 1 second for the task to complete
                        if (!_searchTask.Wait(1000))
                        {
                            DebugLogger.LogError($"SearchInstance[{_searchId}]", "Search task did not complete within timeout");
                        }
                    }
                    catch (AggregateException)
                    {
                        // Expected when task is cancelled
                    }
                    _searchTask = null;
                }
                
                // Send completed event immediately so UI updates
                SearchCompleted?.Invoke(this, EventArgs.Empty);

                // IMPORTANT: Stop the actual search service!
                if (_currentSearch != null)
                {
                    DebugLogger.Log($"SearchInstance[{_searchId}]", "Pausing and disposing search");

                    try
                    {
                        // Pause first if it's running
                        if (_currentSearch.Status == MotelySearchStatus.Running)
                        {
                            _currentSearch.Pause();
                            DebugLogger.Log($"SearchInstance[{_searchId}]", "Search paused");
                        }

                        // Then dispose with a very short timeout - we don't care if it completes
                        var disposeTask = Task.Run(() => _currentSearch.Dispose());
                        if (!disposeTask.Wait(TimeSpan.FromMilliseconds(200)))
                        {
                            // Don't wait, just log and continue
                            DebugLogger.LogError($"SearchInstance[{_searchId}]", "Search disposal timed out (abandoning)");
                        }
                        else
                        {
                            DebugLogger.Log($"SearchInstance[{_searchId}]", "Search disposed successfully");
                        }
                    }
                    catch (Exception ex)
                    {
                        DebugLogger.LogError($"SearchInstance[{_searchId}]", $"Error disposing search: {ex.Message}");
                    }
                    finally
                    {
                        _currentSearch = null;
                    }
                }
            }
        }


        private async Task RunSearchInProcess(
            Motely.Filters.MotelyJsonConfig config,
            SearchCriteria criteria,
            IProgress<SearchProgress>? progress,
            CancellationToken cancellationToken
        )
        {
            DebugLogger.LogImportant($"SearchInstance[{_searchId}]", "RunSearchInProcess ENTERED!");
            
            try
            {
                // Cancellation is fully implemented via CancellationToken
                
                // Enable Motely's DebugLogger if in debug mode
                Motely.DebugLogger.IsEnabled = criteria.EnableDebugOutput;
                if (criteria.EnableDebugOutput)
                {
                    DebugLogger.LogImportant($"SearchInstance[{_searchId}]", "Debug mode enabled - Motely DebugLogger activated");
                }

                // Create filter descriptor with new API
                DebugLogger.LogImportant($"SearchInstance[{_searchId}]", $"Creating MotelyJsonFilterDesc with config: {config.Name ?? "unnamed"}");
                DebugLogger.LogImportant($"SearchInstance[{_searchId}]", $"Config has {config.Must?.Count ?? 0} MUST, {config.Should?.Count ?? 0} SHOULD, {config.MustNot?.Count ?? 0} MUST NOT clauses");
                
                // Combine all clauses for the new API
                var allClauses = new List<MotelyJsonConfig.MotleyJsonFilterClause>();
                if (config.Must != null) allClauses.AddRange(config.Must);
                if (config.Should != null) allClauses.AddRange(config.Should);
                if (config.MustNot != null) allClauses.AddRange(config.MustNot);
                
                DebugLogger.LogError($"SearchInstance[{_searchId}]", $"🔧 TOTAL CLAUSES FOR MOTELY: {allClauses.Count}");
                foreach (var clause in allClauses)
                {
                    DebugLogger.LogError($"SearchInstance[{_searchId}]", $"🔧   Clause: Type={clause.Type}, Value={clause.Value}");
                }
                
                var filterDesc = new Motely.Filters.MotelyJsonFilterDesc(FilterCategory.Mixed, allClauses);
                
                // Create result callback
                Action<MotelySeedScoreTally> onResultFound = (score) =>
                {
                    var result = new SearchResult
                    {
                        Seed = score.Seed,
                        TotalScore = score.Score,
                        Scores = score.TallyColumns?.ToArray(),
                        Labels = config.Should?.Select(s => s.Value ?? "Unknown").ToArray()
                    };

                    // Save directly to DuckDB (thread-safe, fast)
                    AddSearchResult(result);
                    
                    // Increment result counter
                    Interlocked.Increment(ref _resultCount);
                    
                    // Fire ResultFound event for UI counter updates (throttled to prevent spam)
                    if (_resultCount % 50 == 0) // Every 50th result to avoid overwhelming UI
                    {
                        ResultFound?.Invoke(this, new SearchResultEventArgs { Result = result });
                    }
                    
                    // Batched console output (every 10 results)
                    if (_resultCount % 10 == 0)
                    {
                        lock (_recentSeedsLock)
                        {
                            _recentSeeds.Add($"{result.Seed}({result.TotalScore})");
                            
                            // Output batch every 50 seeds
                            if (_recentSeeds.Count >= 5)
                            {
                                AddToConsole($"Found: {string.Join(", ", _recentSeeds)}");
                                _recentSeeds.Clear();
                            }
                        }
                    }
                };
                
                // Create scoring config (only SHOULD clauses for scoring)
                var scoringConfig = new MotelyJsonConfig
                {
                    Name = config.Name,
                    Must = new List<MotelyJsonConfig.MotleyJsonFilterClause>(),
                    Should = config.Should ?? new List<MotelyJsonConfig.MotleyJsonFilterClause>(),
                    MustNot = new List<MotelyJsonConfig.MotleyJsonFilterClause>()
                };
                
                var scoreDesc = new MotelyJsonSeedScoreDesc(scoringConfig, criteria.MinScore, criteria.MinScore == 0, onResultFound, false);
                
                var searchSettings = new MotelySearchSettings<MotelyJsonFilterDesc.MotelyFilter>(filterDesc)
                {
                    ThreadCount = criteria.ThreadCount,
                    StartBatchIndex = (long)criteria.StartBatch,
                    EndBatchIndex = (long)criteria.EndBatch,
                    Mode = MotelySearchMode.Sequential,
                    SeedScoreDesc = scoreDesc
                };
                
                DebugLogger.LogImportant($"SearchInstance[{_searchId}]", $"Starting search with {criteria.ThreadCount} threads");
                
                _currentSearch = new MotelySearch<MotelyJsonFilterDesc.MotelyFilter>(searchSettings);
                
                DebugLogger.LogError($"SearchInstance[{_searchId}]", $"🚀 MotelySearch created, initial status: {_currentSearch.Status}");
                DebugLogger.LogError($"SearchInstance[{_searchId}]", $"🚀 Search settings: Threads={searchSettings.ThreadCount}, StartBatch={searchSettings.StartBatchIndex}, EndBatch={searchSettings.EndBatchIndex}");
                
                // Start the search!
                _currentSearch.Start();
                DebugLogger.LogError($"SearchInstance[{_searchId}]", $"🚀 Search started! New status: {_currentSearch.Status}");
                
                // Wait for search completion
                DebugLogger.LogError($"SearchInstance[{_searchId}]", $"🔄 Entering wait loop - Status: {_currentSearch.Status}, Cancelled: {cancellationToken.IsCancellationRequested}, Running: {_isRunning}");
                while (_currentSearch.Status == MotelySearchStatus.Running && !cancellationToken.IsCancellationRequested && _isRunning)
                {
                    // Report progress
                    var completedBatches = _currentSearch.CompletedBatchCount;
                    var progressPercent = ((double)completedBatches / (criteria.EndBatch - criteria.StartBatch)) * 100.0;
                    
                    // Calculate speed
                    var elapsed = DateTime.UtcNow - _searchStartTime;
                    var seedsSearched = (ulong)(completedBatches * Math.Pow(35, criteria.BatchSize));
                    var seedsPerMs = elapsed.TotalMilliseconds > 0 ? seedsSearched / elapsed.TotalMilliseconds : 0;
                    
                    progress?.Report(new SearchProgress
                    {
                        SeedsSearched = seedsSearched,
                        PercentComplete = progressPercent,
                        SeedsPerMillisecond = seedsPerMs,
                        Message = $"Searched {completedBatches:N0} batches",
                        ResultsFound = _resultCount
                    });

                    try
                    {
                        await Task.Delay(100, cancellationToken);
                    }
                    catch (OperationCanceledException)
                    {
                        DebugLogger.LogImportant($"SearchInstance[{_searchId}]", "Search cancelled");
                        break;
                    }
                }
                
                DebugLogger.LogImportant($"SearchInstance[{_searchId}]", $"Search completed with status: {_currentSearch.Status}");
            }
            catch (Exception ex)
            {
                DebugLogger.LogError($"SearchInstance[{_searchId}]", $"RunSearchInProcess exception: {ex.Message}");
                progress?.Report(new SearchProgress { Message = $"Search error: {ex.Message}", HasError = true, IsComplete = true });
            }
            finally
            {
                // Reset Motely's DebugLogger
                Motely.DebugLogger.IsEnabled = false;
            }
        }

        // HandleAutoCutoff method removed - now handled inline when results are captured

        private void SaveSearchState(ulong completedBatch, ulong totalBatches, ulong endBatch)
        {
            if (_preventStateSave) return; // Don't save if we're removing the icon
            
            try
            {
                if (_currentConfig == null || _currentSearchConfig == null) return;
                
                var state = new SearchResumeState
                {
                    ConfigPath = ConfigPath,
                    LastCompletedBatch = completedBatch,
                    EndBatch = endBatch,
                    BatchSize = _currentSearchConfig.BatchSize,
                    ThreadCount = _currentSearchConfig.ThreadCount,
                    MinScore = _currentSearchConfig.MinScore,
                    Deck = _currentSearchConfig.Deck,
                    Stake = _currentSearchConfig.Stake,
                    LastActiveTime = DateTime.UtcNow,
                    TotalBatches = totalBatches
                };
                
                _userProfileService?.SaveSearchState(state);
            }
            catch (Exception ex)
            {
                DebugLogger.LogError($"SearchInstance[{_searchId}]", $"Failed to save search state: {ex.Message}");
            }
        }


        public void Dispose()
        {
            // First stop the search if it's running
            if (_isRunning)
            {
                DebugLogger.Log($"SearchInstance[{_searchId}]", "Dispose called while running - forcing stop");
                StopSearch();
                
                // Wait a bit for graceful shutdown
                System.Threading.Thread.Sleep(1000);
            }
            
            // Ensure the search task is completed or abandoned
            if (_searchTask != null && !_searchTask.IsCompleted)
            {
                try
                {
                    // Give it one more chance to complete
                    _searchTask.Wait(100);
                }
                catch { /* ignore */ }
                _searchTask = null;
            }
            
            _cancellationTokenSource?.Dispose();
            
            
            // Dispose prepared command & connection
            try
            {
                _appender?.Dispose();
                _appender = null;
                _connection.Dispose();
            }
            catch { }
            
            // Force dispose of the search if it still exists and not already disposed
            if (_currentSearch != null)
            {
                try
                {
                    // Check if not already disposed
                    if (_currentSearch.Status != MotelySearchStatus.Disposed)
                    {
                        _currentSearch.Dispose();
                    }
                }
                catch { /* ignore disposal errors */ }
                _currentSearch = null;
            }
            
            DebugLogger.Log($"SearchInstance[{_searchId}]", "Dispose completed");
            
            GC.SuppressFinalize(this);
        }
    }
}

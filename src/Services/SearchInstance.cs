using System;
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
using MotelyJsonConfig = Motely.Filters.MotelyJsonConfig;
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
        private MotelyJsonConfig? _currentConfig;
        private IMotelySearch? _currentSearch;
        private DateTime _searchStartTime;
        private readonly ObservableCollection<BalatroSeedOracle.Models.SearchResult> _results;
        private readonly List<string> _consoleHistory = new();
        private readonly object _consoleHistoryLock = new();
        private Task? _searchTask;
        private bool _preventStateSave = false;  // Flag to prevent saving state when icon is removed
        private readonly bool _isResume;  // Flag to indicate this is a resumed search
        private string? _lastProcessedBatch;
        
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
        private ulong _lastSavedBatch = 0;
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
        public MotelyJsonConfig? GetFilterConfig() => _currentConfig;
        public int ResultCount => _results.Count;
    public IReadOnlyList<string> ColumnNames => _columnNames.AsReadOnly();
    public string DatabasePath => _dbPath;
    private bool _dbInitialized = false;
    public bool IsDatabaseInitialized => _dbInitialized;
    [Obsolete("Use IsDatabaseInitialized instead")] public bool HasDatabase => _dbInitialized; // legacy UI check
        
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
        public event EventHandler<SearchProgressEventArgs>? ProgressUpdated;
        public event EventHandler<SearchResultEventArgs>? ResultFound;

        public SearchInstance(string searchId, string dbPath, bool isResume = false)
        {
            _searchId = searchId;
            _userProfileService = ServiceHelper.GetService<UserProfileService>();
            _results = new ObservableCollection<BalatroSeedOracle.Models.SearchResult>();
            _isResume = isResume;

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
            
            // If resuming, load existing results
            if (_isResume)
            {
                LoadExistingResults();
            }
        }
        
    private void SetupDatabase(MotelyJsonConfig config, string configPath)
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
        
        private string FormatColumnName(MotelyJsonConfig.FilterItem should)
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

        private Task AddSearchResultAsync(SearchResult result)
        {
            if (!_dbInitialized) return Task.CompletedTask;
            try
            {
                // Duplicate suppression outside lock (fast path) – BUT we must re-check inside lock to avoid race if ForceFlush resets seed set (currently it doesn't).
                if (!_seenSeeds.Add(result.Seed)) return Task.CompletedTask;

                lock (_appenderSync)
                {
                    if (_appender == null) return Task.CompletedTask; // search shutting down
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
                    return Task.CompletedTask;
                }
                else
                {
                    DebugLogger.LogError($"SearchInstance[{_searchId}]", $"Failed to add result: {ex.Message}");
                }
            }
            return Task.CompletedTask;
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
                        scores[i] = !reader.IsDBNull(i + 2) ? reader.GetInt32(i + 2) : 0;
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
                        scores[i] = !reader.IsDBNull(i + 2) ? reader.GetInt32(i + 2) : 0;
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
                    for (int i = 0; i < _columnNames.Count; i++)
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

                // Load the config from file - use LoadFromJson to get PostProcess!
                var MotelyJsonConfig = MotelyJsonConfig.LoadFromJson(configPath);
                if (MotelyJsonConfig == null)
                {
                    throw new InvalidOperationException($"Config validation failed for: {configPath}");
                }
                
                _currentConfig = MotelyJsonConfig;
                _currentSearchConfig = config;
                ConfigPath = configPath;
                FilterName = MotelyJsonConfig.Name ?? Path.GetFileNameWithoutExtension(configPath);
                _searchStartTime = DateTime.UtcNow;
                _isRunning = true;
                _cancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(
                    cancellationToken
                );

                // Clear previous results
                _results.Clear();

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

                // Register direct callback with MotelyJsonFinalTallyScoresDescDesc
                DebugLogger.LogImportant($"SearchInstance[{_searchId}]", "Registering OnResultFound callback");
                DebugLogger.LogImportant($"SearchInstance[{_searchId}]", $"MotelyJsonConfig has {MotelyJsonConfig.Must?.Count ?? 0} MUST clauses");
                DebugLogger.LogImportant($"SearchInstance[{_searchId}]", $"MotelyJsonConfig has {MotelyJsonConfig.Should?.Count ?? 0} SHOULD clauses");
                DebugLogger.LogImportant($"SearchInstance[{_searchId}]", $"MotelyJsonConfig has {MotelyJsonConfig.MustNot?.Count ?? 0} MUST NOT clauses");
                
                // Log the actual filter content for debugging
                if (MotelyJsonConfig.Must != null)
                {
                    foreach (var must in MotelyJsonConfig.Must)
                    {
                        DebugLogger.LogImportant($"SearchInstance[{_searchId}]", $"  MUST: Type={must.Type}, Value={must.Value}");
                    }
                }
                if (MotelyJsonConfig.Should != null)
                {
                    foreach (var should in MotelyJsonConfig.Should)
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
                            MotelyJsonConfig,
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
            var config = MotelyJsonConfig.LoadFromJson(criteria.ConfigPath);
            if (config == null)
            {
                throw new InvalidOperationException($"Config validation failed for: {criteria.ConfigPath}");
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
                
                // Set the cancellation flag that Motely checks
                MotelyJsonFinalTallyScoresDescDesc.MotelyJsonFinalTallyScoresDesc.IsCancelled = true;

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
            MotelyJsonConfig config,
            SearchCriteria criteria,
            IProgress<SearchProgress>? progress,
            CancellationToken cancellationToken
        )
        {
            DebugLogger.LogImportant($"SearchInstance[{_searchId}]", "RunSearchInProcess ENTERED!");
            
            try
            {
                
                // Reset cancellation flag
                MotelyJsonFinalTallyScoresDescDesc.MotelyJsonFinalTallyScoresDesc.IsCancelled = false;
                
                // Enable Motely's DebugLogger if in debug mode
                Motely.DebugLogger.IsEnabled = criteria.EnableDebugOutput;
                if (criteria.EnableDebugOutput)
                {
                    DebugLogger.LogImportant($"SearchInstance[{_searchId}]", "Debug mode enabled - Motely DebugLogger activated");
                }

                // Create filter descriptor with result callback
                DebugLogger.LogImportant($"SearchInstance[{_searchId}]", $"Creating MotelyJsonFinalTallyScoresDescDesc with config: {config.Name ?? "unnamed"}");
                DebugLogger.LogImportant($"SearchInstance[{_searchId}]", $"Config has {config.Must?.Count ?? 0} MUST, {config.Should?.Count ?? 0} SHOULD, {config.MustNot?.Count ?? 0} MUST NOT clauses");
                
                Action<string, int, int[]> onResultFound = (seed, totalScore, scores) =>
                {
                    DebugLogger.LogImportant($"SearchInstance[{_searchId}]", $"Direct callback - Found: {seed} Score: {totalScore}");

                    var result = new SearchResult
                    {
                        Seed = seed,
                        TotalScore = totalScore,
                        Scores = scores,
                        Labels = config.Should?.Select(s => s.Value ?? "Unknown").ToArray()
                    };

                    // Marshal collection & UI event updates to UI thread; DB insert stays background
                    Avalonia.Threading.Dispatcher.UIThread.InvokeAsync(() =>
                    {
                        _results.Add(result);
                        ResultFound?.Invoke(
                            this,
                            new SearchResultEventArgs
                            {
                                Result = new BalatroSeedOracle.Models.SearchResult
                                {
                                    Seed = seed,
                                    TotalScore = totalScore,
                                    Scores = scores
                                },
                            }
                        );
                    });

                    // Save to database (background thread OK)
                    Task.Run(async () => await AddSearchResultAsync(result));
                };
                
                // Create filter descriptor with new constructor
                var filterDesc = new MotelyJsonFinalTallyScoresDescDesc(config, onResultFound);
                
                // ALWAYS pass MinScore directly to Motely - let Motely handle auto-cutoff internally!
                // MinScore=0 means auto-cutoff in Motely
                filterDesc.Cutoff = criteria.MinScore;
                filterDesc.AutoCutoff = criteria.MinScore == 0;
                
                DebugLogger.LogImportant($"SearchInstance[{_searchId}]", $"Passing cutoff={criteria.MinScore} to Motely (0=auto)");

                // Create search settings - following Motely Program.cs pattern
                var batchSize = criteria.BatchSize;
                
                // Calculate total batches based on batch character count
                // Total seeds = 35^8 = 2,251,875,390,625
                // Total batches = 35^(8-batchSize)
                ulong totalBatches = batchSize switch 
                {
                    1 => 64_339_296_875UL,          // 35^7 = 64,339,296,875
                    2 => 1_838_265_625UL,           // 35^6 = 1,838,265,625
                    3 => 52_521_875UL,              // 35^5 = 52,521,875
                    4 => 1_500_625UL,               // 35^4 = 1,500,625
                    //5 => 42_875UL,                  // 35^3 = 42,875
                    //6 => 1_225UL,                   // 35^2 = 1,225
                    //7 => 35UL,                      // 35^1 = 35
                    //8 => 1UL,                       // 35^0 = 1 // these are all way too big and don't make the search faster.
                    _ => throw new ArgumentException($"Invalid batch character count: {batchSize}. Valid range is 1-4.")
                };
                
                // If no end batch specified, search all
                ulong effectiveEndBatch = (criteria.EndBatch == 0) ? totalBatches : criteria.EndBatch;
                
                // Create progress callback for Motely
                var motelyProgress = new Progress<Motely.MotelyProgress>(mp =>
                {
                    // IMPORTANT: mp.CompletedBatchCount is relative to this session!
                    // We need to add the start batch to get the absolute position
                    ulong absoluteBatchCount = criteria.StartBatch + mp.CompletedBatchCount;
                    
                    // Calculate REAL percentage of total search space
                    double actualPercent = (absoluteBatchCount / (double)totalBatches) * 100.0;
                    
                    // This runs on the captured synchronization context
                    progress?.Report(
                        new SearchProgress
                        {
                            SeedsSearched = mp.SeedsSearched,
                            SeedsPerMillisecond = mp.SeedsPerMillisecond,
                            PercentComplete = actualPercent,
                            Message = $"⏱️ {mp.FormattedMessage}",
                            ResultsFound = Results.Count,
                        }
                    );
                    
                    // Also raise our event
                    ProgressUpdated?.Invoke(
                        this,
                        new SearchProgressEventArgs
                        {
                            SeedsSearched = mp.SeedsSearched,
                            ResultsFound = Results.Count,
                            SeedsPerMillisecond = mp.SeedsPerMillisecond,
                            PercentComplete = actualPercent,
                            BatchesSearched = absoluteBatchCount,  // Use absolute batch count
                            TotalBatches = totalBatches,  // Use the real total, not effective end
                        }
                    );
                    
                    // Update batch in memory on every progress update
                    if (mp.CompletedBatchCount != _lastSavedBatch)
                    {
                        _lastSavedBatch = mp.CompletedBatchCount;
                        
                        // Save to database for resume - save the completed batch count
                        // The actual resume will be from absoluteBatchCount which already includes the start offset
                        _ = SaveLastBatchAsync(absoluteBatchCount);
                        
                        // Only save state if not prevented
                        if (!_preventStateSave)
                        {
                            // First time? Save the full state
                            if (_userProfileService?.GetSearchState() == null)
                            {
                                SaveSearchState(absoluteBatchCount, totalBatches, effectiveEndBatch);
                                _lastStateSave = DateTime.UtcNow;
                            }
                            else
                            {
                                // Just update the batch number in memory
                                _userProfileService?.UpdateSearchBatch(absoluteBatchCount);
                                
                                // Write to disk every 10 seconds
                                if (DateTime.UtcNow > _lastStateSave.AddSeconds(10))
                                {
                                    _userProfileService?.FlushProfile();
                                    _lastStateSave = DateTime.UtcNow;
                                }
                            }
                        }
                    }
                });
                
                var searchSettings = new MotelySearchSettings<MotelyJsonFinalTallyScoresDescDesc.MotelyJsonFinalTallyScoresDesc>(
                    filterDesc
                ).WithThreadCount(criteria.ThreadCount)
                    .WithBatchCharacterCount(batchSize)
                    .WithSequentialSearch()
                    .WithStartBatchIndex(criteria.StartBatch)
                    .WithEndBatchIndex(effectiveEndBatch);

                DebugLogger.LogImportant(
                    $"SearchInstance[{_searchId}]",
                    $"Starting search with {criteria.ThreadCount} threads, batch size {batchSize}"
                );

                // Start the search - use list search if a specific seed is provided
                if (!string.IsNullOrEmpty(criteria.DebugSeed))
                {
                    var seedList = new List<string> { criteria.DebugSeed };
                    DebugLogger.LogImportant(
                        $"SearchInstance[{_searchId}]",
                        $"Searching for specific seed: {criteria.DebugSeed}"
                    );
                    _currentSearch = searchSettings.WithListSearch(seedList).Start();
                }
                else
                {
                    _currentSearch = searchSettings.Start();
                }

                DebugLogger.LogImportant(
                    $"SearchInstance[{_searchId}]",
                    $"Search started with batch size {batchSize}, total batches: {totalBatches}"
                );

                // Wait for search to complete - progress comes through callbacks!
                while (
                    _currentSearch.Status == MotelySearchStatus.Running
                    && !cancellationToken.IsCancellationRequested
                    && _isRunning
                )
                {
                    // Check for cancellation
                    if (!_isRunning || cancellationToken.IsCancellationRequested || MotelyJsonFinalTallyScoresDescDesc.MotelyJsonFinalTallyScoresDesc.IsCancelled)
                    {
                        DebugLogger.LogImportant(
                            $"SearchInstance[{_searchId}]",
                            "Cancellation requested - stopping search"
                        );
                        break;
                    }

                    // Just wait - all updates come through callbacks now!
                    try
                    {
                        // Check more frequently for faster stop response
                        await Task.Delay(50, cancellationToken);
                    }
                    catch (OperationCanceledException)
                    {
                        DebugLogger.LogImportant(
                            $"SearchInstance[{_searchId}]",
                            "Search cancelled"
                        );
                        break;
                    }
                }

                DebugLogger.LogImportant(
                    $"SearchInstance[{_searchId}]",
                    $"Search loop ended. Status={_currentSearch.Status}"
                );
                
                // If we exited the loop due to cancellation, stop the Motely search
                if (_currentSearch != null && _currentSearch.Status == MotelySearchStatus.Running)
                {
                    DebugLogger.LogImportant(
                        $"SearchInstance[{_searchId}]",
                        "Stopping Motely search due to cancellation"
                    );
                    try
                    {
                        _currentSearch.Pause();
                        _currentSearch.Dispose();
                        _currentSearch = null; // Mark as disposed
                    }
                    catch (Exception ex)
                    {
                        DebugLogger.LogError(
                            $"SearchInstance[{_searchId}]",
                            $"Error stopping Motely search: {ex.Message}"
                        );
                    }
                }


                // Report completion (get count before disposing)
                ulong finalBatchCount = 0UL;
                if (_currentSearch != null)
                {
                    try
                    {
                        finalBatchCount = (ulong)_currentSearch.CompletedBatchCount;
                    }
                    catch
                    {
                        finalBatchCount = 0UL; // Disposed
                    }
                }
                ulong finalSeeds = finalBatchCount * (ulong)Math.Pow(35, batchSize);

                // Clear search state if completed successfully
                if (!cancellationToken.IsCancellationRequested && _isRunning && finalBatchCount >= (ulong)effectiveEndBatch)
                {
                    _userProfileService?.ClearSearchState();
                }

                progress?.Report(
                    new SearchProgress
                    {
                        Message =
                            cancellationToken.IsCancellationRequested || !_isRunning
                                ? "Search cancelled"
                                : $"Search complete. Found {Results.Count} seeds",
                        IsComplete = true,
                        SeedsSearched = finalSeeds,
                        ResultsFound = Results.Count,
                        PercentComplete = 100,
                    }
                );
            }
            catch (Exception ex)
            {
                DebugLogger.LogError(
                    $"SearchInstance[{_searchId}]",
                    $"RunSearchInProcess exception: {ex.Message}"
                );
                progress?.Report(
                    new SearchProgress
                    {
                        Message = $"Search error: {ex.Message}",
                        HasError = true,
                        IsComplete = true,
                    }
                );
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

        /// <summary>
        /// Gets the last processed batch from the database for resuming
        /// </summary>
        public string? GetLastProcessedBatch()
        {
            try
            {
                // Check if metadata table exists
                using var cmd = _connection.CreateCommand();
                cmd.CommandText = @"
                    SELECT batch_id FROM search_metadata 
                    ORDER BY last_updated DESC 
                    LIMIT 1";
                
                var result = cmd.ExecuteScalar();
                _lastProcessedBatch = result?.ToString();
                return _lastProcessedBatch;
            }
            catch
            {
                // Table might not exist or no data
                return null;
            }
        }
        
        /// <summary>
        /// Loads existing results from the database when resuming
        /// </summary>
        private void LoadExistingResults()
        {
            try
            {
                using var cmd = _connection.CreateCommand();
                
                // First check if results table exists
                cmd.CommandText = "SELECT COUNT(*) FROM information_schema.tables WHERE table_name = 'results'";
                var tableExists = Convert.ToInt32(cmd.ExecuteScalar()) > 0;
                
                if (!tableExists)
                {
                    DebugLogger.Log("SearchInstance", "No results table found - nothing to resume");
                    return;
                }
                
                // Load results
                cmd.CommandText = "SELECT * FROM results ORDER BY score DESC";
                using var reader = cmd.ExecuteReader();
                
                while (reader.Read())
                {
                    var seed = reader.GetString(reader.GetOrdinal("seed"));
                    var score = reader.GetInt32(reader.GetOrdinal("score"));
                    
                    // Create SearchResult object
                    var result = new SearchResult
                    {
                        Seed = seed
                        // Score = score,
                        // Details = new List<int>()
                    };
                    
                    // Load detail columns (should clause scores)
                    // for (int i = 2; i < reader.FieldCount; i++)
                    // {
                    //     if (!reader.IsDBNull(i))
                    //     {
                    //         result.Details.Add(reader.GetInt32(i));
                    //     }
                    // }
                    
                    _results.Add(result);
                }
                
                DebugLogger.Log("SearchInstance", $"Loaded {_results.Count} existing results from database");
            }
            catch (Exception ex)
            {
                DebugLogger.LogError("SearchInstance", $"Failed to load existing results: {ex.Message}");
            }
        }
        
        /// <summary>
        /// Saves the current batch ID to metadata for resume capability
        /// </summary>
        private void SaveBatchProgress(string batchId)
        {
            try
            {
                using var cmd = _connection.CreateCommand();
                
                // Create metadata table if not exists
                cmd.CommandText = @"
                    CREATE TABLE IF NOT EXISTS search_metadata (
                        batch_id VARCHAR PRIMARY KEY,
                        last_updated TIMESTAMP
                    )";
                cmd.ExecuteNonQuery();
                
                // Update or insert current batch
                cmd.CommandText = @"
                    INSERT OR REPLACE INTO search_metadata (batch_id, last_updated) 
                    VALUES ($1, CURRENT_TIMESTAMP)";
                cmd.Parameters.Add(new DuckDBParameter { Value = batchId });
                cmd.ExecuteNonQuery();
                
                _lastProcessedBatch = batchId;
            }
            catch (Exception ex)
            {
                DebugLogger.LogError("SearchInstance", $"Failed to save batch progress: {ex.Message}");
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

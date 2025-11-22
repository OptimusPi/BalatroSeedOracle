using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using BalatroSeedOracle.Helpers;
using BalatroSeedOracle.Models;
using BalatroSeedOracle.Views.Modals;
using DuckDB.NET.Data;
using DuckDB.NET.Native; // for NativeMethods.Appender.DuckDBAppenderFlush
using Motely;
using Motely.Filters;
using Motely.Utils;
using DebugLogger = BalatroSeedOracle.Helpers.DebugLogger;
using SearchResult = BalatroSeedOracle.Models.SearchResult;
using SearchResultEventArgs = BalatroSeedOracle.Models.SearchResultEventArgs;

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
        private readonly ConcurrentQueue<string> _consoleHistory = new();
        private volatile int _resultCount = 0;
        private Task? _searchTask;
        private bool _preventStateSave = false; // Flag to prevent saving state when icon is removed
        private volatile bool _hasNewResultsSinceLastQuery = false;

        // Persistent DuckDB connection for database operations
        private readonly DuckDBConnection _connection;
        private DuckDB.NET.Data.DuckDBAppender? _appender;
        private readonly object _appenderLock = new();

        // Search state tracking for resume
        private SearchConfiguration? _currentSearchConfig;
        private DateTime _lastStateSave = DateTime.UtcNow;

        // Properties
        public string SearchId
        {
            get => _searchId;
        }
        public bool IsRunning => _isRunning;
        public bool IsPaused => _isPaused;
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
            return _consoleHistory.ToList();
        }

        /// <summary>
        /// Adds a message to the console history
        /// </summary>
        private void AddToConsole(string message)
        {
            var timestamp = DateTime.UtcNow.ToString("HH:mm:ss");
            _consoleHistory.Enqueue($"[{timestamp}] {message}");
        }

        /// <summary>
        /// Indicates whether new results have been added since the last UI query.
        /// Used to avoid wasteful DuckDB queries when no new results exist.
        /// </summary>
        public bool HasNewResultsSinceLastQuery => _hasNewResultsSinceLastQuery;

        /// <summary>
        /// Resets the new results flag after UI has queried and displayed them.
        /// </summary>
        public void AcknowledgeResultsQueried()
        {
            _hasNewResultsSinceLastQuery = false;
        }

        // Events for UI integration
        public event EventHandler? SearchStarted;
        public event EventHandler? SearchCompleted;
        public event EventHandler<SearchProgress>? ProgressUpdated;

        public SearchInstance(string searchId, string dbPath)
        {
            _searchId = searchId;
            _userProfileService = ServiceHelper.GetService<UserProfileService>();

            // Require a non-empty path immediately so query helpers are safe to call early
            if (string.IsNullOrWhiteSpace(dbPath))
                throw new ArgumentException("dbPath is required", nameof(dbPath));

            var dir = Path.GetDirectoryName(dbPath);
            if (!string.IsNullOrEmpty(dir))
                Directory.CreateDirectory(dir);
            _dbPath = dbPath;
            _connectionString = $"Data Source={_dbPath}";
            // Open persistent connection immediately (simple, no EnsureConnection later)
            _connection = new DuckDBConnection(_connectionString);
            _connection.Open();
        }

        private void SetupDatabase(Motely.Filters.MotelyJsonConfig config, string configPath)
        {
            // CRITICAL: Use shared GetColumnNames() method from MotelyJsonConfig
            // This ensures DuckDB schema matches CSV export format exactly!
            _columnNames = config.GetColumnNames();
            DebugLogger.LogImportant(
                $"SearchInstance[{_searchId}]",
                $"Using shared column schema: {string.Join(", ", _columnNames)}"
            );

            // If constructor already supplied a path, keep it; otherwise derive from filter name
            if (string.IsNullOrEmpty(_dbPath))
            {
                var filterName = Path.GetFileNameWithoutExtension(configPath);
                var searchResultsDir = AppPaths.SearchResultsDir;
                _dbPath = Path.Combine(searchResultsDir, $"{filterName}.db");
                _connectionString = $"Data Source={_dbPath}";
            }
            DebugLogger.LogImportant(
                $"SearchInstance[{_searchId}]",
                $"Database configured with {_columnNames.Count} columns at {_dbPath}"
            );

            InitializeDatabase();
        }


        /// <summary>
        /// Validates existing database schema matches current filter requirements.
        /// If schema mismatch detected, closes connection, deletes database, and reopens connection.
        /// Returns true if schema matched (or didn't exist), false if database was deleted.
        /// </summary>
        private bool ValidateOrDeleteDatabase()
        {
            try
            {
                // Check if results table exists
                using (var checkTable = _connection.CreateCommand())
                {
                    checkTable.CommandText = "SELECT name FROM sqlite_master WHERE type='table' AND name='results'";
                    var result = checkTable.ExecuteScalar();

                    if (result == null)
                    {
                        // Table doesn't exist yet - schema will be created fresh
                        return true;
                    }
                }

                // Table exists - check if columns match expected schema
                using (var getColumns = _connection.CreateCommand())
                {
                    getColumns.CommandText = "PRAGMA table_info(results)";
                    var existingColumns = new List<string>();
                    using (var reader = getColumns.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            var columnName = reader.GetString(1); // Column name is at index 1
                            existingColumns.Add(columnName);
                        }
                    }

                    // Compare with expected columns
                    var expectedColumns = _columnNames;

                    DebugLogger.Log(
                        $"SearchInstance[{_searchId}]",
                        $"Existing columns: {string.Join(", ", existingColumns)}"
                    );
                    DebugLogger.Log(
                        $"SearchInstance[{_searchId}]",
                        $"Expected columns: {string.Join(", ", expectedColumns)}"
                    );

                    bool match = existingColumns.Count == expectedColumns.Count &&
                                 existingColumns.SequenceEqual(expectedColumns);

                    if (!match)
                    {
                        DebugLogger.LogImportant(
                            $"SearchInstance[{_searchId}]",
                            $"⚠️ SCHEMA MISMATCH! Deleting old database: {_dbPath}"
                        );

                        // Close connection before deleting file
                        _connection.Close();

                        // Delete database file and WAL file
                        if (File.Exists(_dbPath))
                        {
                            File.Delete(_dbPath);
                            DebugLogger.Log($"SearchInstance[{_searchId}]", $"Deleted: {_dbPath}");
                        }
                        if (File.Exists(_dbPath + ".wal"))
                        {
                            File.Delete(_dbPath + ".wal");
                            DebugLogger.Log($"SearchInstance[{_searchId}]", $"Deleted: {_dbPath}.wal");
                        }

                        // Reopen connection to create fresh database
                        _connection.Open();

                        return false; // Schema didn't match, database was deleted
                    }
                }

                return true; // Schema matches
            }
            catch (Exception ex)
            {
                DebugLogger.LogError(
                    $"SearchInstance[{_searchId}]",
                    $"Schema validation failed: {ex.Message}"
                );
                // If validation fails, assume we need fresh database
                return false;
            }
        }

        private void InitializeDatabase()
        {
            try
            {
                // CRITICAL: Check if database exists with DIFFERENT schema
                // If schema mismatch detected, delete the old database and create fresh one
                bool schemaMatches = ValidateOrDeleteDatabase();

                if (!schemaMatches)
                {
                    DebugLogger.LogImportant(
                        $"SearchInstance[{_searchId}]",
                        "Schema mismatch detected - database was deleted, creating fresh schema"
                    );
                }

                var columnDefs = new List<string> { "seed VARCHAR PRIMARY KEY", "score INT" };
                for (int i = 2; i < _columnNames.Count; i++)
                    columnDefs.Add($"{_columnNames[i]} INT");

                using (var createTable = _connection.CreateCommand())
                {
                    createTable.CommandText =
                        $@"CREATE TABLE IF NOT EXISTS results (
                        {string.Join(",\n                        ", columnDefs)}
                    )";
                    createTable.ExecuteNonQuery();
                }
                using (var createIndex = _connection.CreateCommand())
                {
                    createIndex.CommandText =
                        "CREATE INDEX IF NOT EXISTS idx_score ON results(score DESC);";
                    createIndex.ExecuteNonQuery();
                }

                // Create indexes for all tally columns to accelerate sorting
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

                using (var createMeta = _connection.CreateCommand())
                {
                    createMeta.CommandText =
                        "CREATE TABLE IF NOT EXISTS search_meta ( key VARCHAR PRIMARY KEY, value VARCHAR )";
                    createMeta.ExecuteNonQuery();
                }
                // DuckDB PRIMARY KEY handles duplicates automatically
                _dbInitialized = true;
                DebugLogger.Log(
                    $"SearchInstance[{_searchId}]",
                    $"Database initialized with {_columnNames.Count} columns"
                );
            }
            catch (Exception ex)
            {
                DebugLogger.LogError(
                    $"SearchInstance[{_searchId}]",
                    $"Failed to initialize database: {ex.Message}"
                );
                throw;
            }
        }

        // Force flush appender to make data visible for queries
        public void ForceFlush()
        {
            try
            {
                lock (_appenderLock)
                {
                    if (_appender != null)
                    {
                        _appender.Dispose();
                        _appender = null;
                    }
                }
            }
            catch (Exception ex)
            {
                DebugLogger.LogError(
                    $"SearchInstance[{_searchId}]",
                    $"ForceFlush failed: {ex.Message}"
                );
            }
        }

        private void AddSearchResult(SearchResult result)
        {
            if (!_dbInitialized)
                return;

            try
            {
                lock (_appenderLock)
                {
                    _appender ??= _connection.CreateAppender("results");

                    var row = _appender.CreateRow();
                    row.AppendValue(result.Seed).AppendValue(result.TotalScore);

                    int tallyCount = _columnNames.Count - 2;
                    for (int i = 0; i < tallyCount; i++)
                    {
                        int val =
                            (result.Scores != null && i < result.Scores.Length)
                                ? result.Scores[i]
                                : 0;
                        row.AppendValue(val);
                    }
                    row.EndRow();
                }

                // Invalidate query cache - new results are available
                _hasNewResultsSinceLastQuery = true;
            }
            catch (Exception ex)
            {
                // DuckDB PRIMARY KEY handles duplicates automatically
                if (!ex.Message.Contains("PRIMARY KEY") && !ex.Message.Contains("Duplicate key"))
                {
                    DebugLogger.LogError(
                        $"SearchInstance[{_searchId}]",
                        $"Insert failed: {ex.Message}"
                    );
                }
            }
        }

        public async Task<List<BalatroSeedOracle.Models.SearchResult>> GetResultsPageAsync(
            int offset,
            int limit
        )
        {
            var list = new List<BalatroSeedOracle.Models.SearchResult>();
            if (!_dbInitialized)
                return list;

            // Rely on row.EndRow() visibility; do NOT close appender mid-search.

            using var cmd = _connection.CreateCommand();
            cmd.CommandText = "SELECT * FROM results ORDER BY score DESC LIMIT ? OFFSET ?";
            cmd.Parameters.Add(new DuckDB.NET.Data.DuckDBParameter(limit));
            cmd.Parameters.Add(new DuckDB.NET.Data.DuckDBParameter(offset));
            using var reader = await cmd.ExecuteReaderAsync().ConfigureAwait(false);
            while (await reader.ReadAsync().ConfigureAwait(false))
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
                list.Add(
                    new BalatroSeedOracle.Models.SearchResult
                    {
                        Seed = seed,
                        TotalScore = score,
                        Scores = scores,
                    }
                );
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
        public async Task<List<BalatroSeedOracle.Models.SearchResult>> GetTopResultsAsync(
            string orderBy,
            bool ascending,
            int limit = 1000
        )
        {
            var results = new List<BalatroSeedOracle.Models.SearchResult>();
            if (limit <= 0)
                return results;

            if (!_dbInitialized)
            {
                DebugLogger.Log(
                    $"SearchInstance[{_searchId}]",
                    "GetTopResultsAsync called before DB init complete"
                );
                return results;
            }

            // Force flush the appender to ensure all buffered results are visible
            ForceFlush();

            // Resolve order by column safely
            string resolvedColumn = "score"; // default
            if (!string.IsNullOrEmpty(orderBy))
            {
                if (orderBy.Equals("seed", StringComparison.OrdinalIgnoreCase))
                    resolvedColumn = "seed";
                else if (orderBy.Equals("score", StringComparison.OrdinalIgnoreCase))
                    resolvedColumn = "score";
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
                    if (_columnNames.Contains(orderBy))
                        resolvedColumn = orderBy;
                }
            }

            // Rely on row.EndRow() visibility; appender stays open during active search.

            using var cmd = _connection.CreateCommand();
            // Column name is validated against _columnNames whitelist, safe to interpolate
            // LIMIT is parameterized for safety
            cmd.CommandText =
                $"SELECT * FROM results ORDER BY {resolvedColumn} {(ascending ? "ASC" : "DESC")} LIMIT ?";
            cmd.Parameters.Add(new DuckDB.NET.Data.DuckDBParameter(limit));
            using var reader = await cmd.ExecuteReaderAsync().ConfigureAwait(false);
            while (await reader.ReadAsync().ConfigureAwait(false))
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
                results.Add(
                    new BalatroSeedOracle.Models.SearchResult
                    {
                        Seed = seed,
                        TotalScore = score,
                        Scores = scores,
                    }
                );
            }
            if (results.Count == 0)
            {
                DebugLogger.Log(
                    $"SearchInstance[{_searchId}]",
                    "GetTopResultsAsync returned 0 rows"
                );
            }
            return results;
        }

        private const int MaxResultsForGetAll = 1_000_000;

        public async Task<List<BalatroSeedOracle.Models.SearchResult>> GetAllResultsAsync()
        {
            return await GetResultsPageAsync(0, MaxResultsForGetAll).ConfigureAwait(false);
        }

        public async Task<int> GetResultCountAsync()
        {
            if (!_dbInitialized)
                throw new InvalidOperationException("Database not initialized");

            // Force flush to ensure all buffered results are counted
            ForceFlush();

            using var cmd = _connection.CreateCommand();
            cmd.CommandText = "SELECT COUNT(*) FROM results";
            var v = await cmd.ExecuteScalarAsync().ConfigureAwait(false);
            return v == null ? 0 : Convert.ToInt32(v);
        }

        public async Task<int> ExportResultsAsync(string filePath)
        {
            try
            {
                if (!_dbInitialized)
                    return 0;
                var count = await GetResultCountAsync().ConfigureAwait(false);
                if (count == 0)
                    return 0;

                // Use DuckDB native CSV export - MUCH faster and simpler!
                using var cmd = _connection.CreateCommand();
                cmd.CommandText =
                    $"COPY (SELECT * FROM results ORDER BY score DESC) TO '{filePath.Replace("'", "''")}' (HEADER true, DELIMITER ',')";
                await cmd.ExecuteNonQueryAsync().ConfigureAwait(false);

                return count;
            }
            catch (Exception ex)
            {
                DebugLogger.LogError(
                    $"SearchInstance[{_searchId}]",
                    $"Failed to export results: {ex.Message}"
                );
                throw;
            }
        }

        public async Task<ulong?> GetLastBatchAsync()
        {
            try
            {
                if (!_dbInitialized)
                    return null;
                using var cmd = _connection.CreateCommand();
                cmd.CommandText = "SELECT value FROM search_meta WHERE key='last_batch'";
                var val = await cmd.ExecuteScalarAsync().ConfigureAwait(false);
                if (val == null)
                    return null;
                return ulong.TryParse(val.ToString(), out var batch) ? batch : null;
            }
            catch (Exception ex)
            {
                DebugLogger.LogError(
                    $"SearchInstance[{_searchId}]",
                    $"Failed to get last batch: {ex.Message}"
                );
                throw;
            }
        }

        private async Task SaveLastBatchAsync(ulong batchNumber)
        {
            try
            {
                if (!_dbInitialized)
                    return;
                using var cmd = _connection.CreateCommand();
                cmd.CommandText =
                    "INSERT OR REPLACE INTO search_meta (key, value) VALUES ('last_batch', ?)";
                cmd.Parameters.Add(new DuckDBParameter(batchNumber.ToString()));
                await cmd.ExecuteNonQueryAsync().ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                DebugLogger.LogError(
                    $"SearchInstance[{_searchId}]",
                    $"Failed to save last batch: {ex.Message}"
                );
                throw;
            }
        }

        /// <summary>
        /// Dumps all seeds from current database to WordLists/fertilizer.txt before database invalidation.
        /// "Fertilizer" helps new "seeds" grow faster by providing a head start wordlist.
        /// </summary>
        private async Task DumpSeedsToFertilizerAsync()
        {
            try
            {
                if (!_dbInitialized || !File.Exists(_dbPath))
                {
                    DebugLogger.Log(
                        $"SearchInstance[{_searchId}]",
                        "No database to dump - skipping fertilizer.txt"
                    );
                    return;
                }

                // Ensure WordLists directory exists
                var wordListsDir = AppPaths.WordListsDir;

                var fertilizerPath = Path.Combine(wordListsDir, "fertilizer.txt");

                // Get all seeds from database
                using var cmd = _connection.CreateCommand();
                cmd.CommandText = "SELECT seed FROM results ORDER BY seed";

                var seeds = new List<string>();
                using (var reader = await cmd.ExecuteReaderAsync().ConfigureAwait(false))
                {
                    while (await reader.ReadAsync().ConfigureAwait(false))
                    {
                        var seed = reader.GetString(0);
                        if (!string.IsNullOrWhiteSpace(seed))
                        {
                            seeds.Add(seed);
                        }
                    }
                }

                if (seeds.Count == 0)
                {
                    DebugLogger.Log(
                        $"SearchInstance[{_searchId}]",
                        "No seeds to dump - database is empty"
                    );
                    return;
                }

                // Append seeds to fertilizer.txt
                await File.AppendAllLinesAsync(fertilizerPath, seeds).ConfigureAwait(false);

                DebugLogger.LogImportant(
                    $"SearchInstance[{_searchId}]",
                    $"Dumped {seeds.Count} seeds to fertilizer.txt (total file size: {new FileInfo(fertilizerPath).Length} bytes)"
                );
            }
            catch (Exception ex)
            {
                DebugLogger.LogError(
                    $"SearchInstance[{_searchId}]",
                    $"Failed to dump seeds to fertilizer.txt: {ex.Message}"
                );
                // Don't throw - fertilizer dump is a nice-to-have, not critical
            }
        }

        /// <summary>
        /// Start searching with a file path
        /// </summary>
        private void StartSearchFromFile(
            string configPath,
            SearchConfiguration config,
            IProgress<SearchProgress>? progress = null,
            CancellationToken cancellationToken = default
        )
        {
            DebugLogger.LogImportant(
                $"SearchInstance[{_searchId}]",
                $"StartSearchFromFileAsync ENTERED! configPath={configPath}"
            );

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

                // TODO: Move to FiltersModalViewModel.SaveFilter - only invalidate when MUST/SHOULD/MUSTNOT changes during SAVE
                // Check if filter file was modified since last search
                if (File.Exists(_dbPath) && File.Exists(configPath))
                {
                    var filterModified = File.GetLastWriteTimeUtc(configPath);
                    var dbModified = File.GetLastWriteTimeUtc(_dbPath);

                    if (filterModified > dbModified)
                    {
                        DebugLogger.LogImportant(
                            $"SearchInstance[{_searchId}]",
                            $"Filter modified since last search - invalidating database"
                        );

                        // Dump seeds to fertilizer.txt before clearing database (fire-and-forget)
                        // Not critical for search to work, so we don't block on this operation
                        _ = Task.Run(async () =>
                        {
                            try
                            {
                                await DumpSeedsToFertilizerAsync().ConfigureAwait(false);
                            }
                            catch (Exception ex)
                            {
                                DebugLogger.LogError(
                                    $"SearchInstance[{_searchId}]",
                                    $"Failed to dump seeds to fertilizer: {ex.Message}"
                                );
                            }
                        });

                        try
                        {
                            File.Delete(_dbPath);
                            DebugLogger.LogImportant(
                                $"SearchInstance[{_searchId}]",
                                $"Deleted outdated database: {_dbPath}"
                            );
                        }
                        catch (Exception ex)
                        {
                            DebugLogger.LogError(
                                $"SearchInstance[{_searchId}]",
                                $"Failed to delete outdated database: {ex.Message}"
                            );
                        }
                    }
                }

                // Load the config from file - use TryLoadFromJsonFile to get PostProcess!
                if (
                    !Motely.Filters.MotelyJsonConfig.TryLoadFromJsonFile(
                        configPath,
                        out var filterConfig
                    )
                )
                {
                    throw new Exception($"Failed to load config from {configPath}");
                }

                _currentConfig = filterConfig;
                _currentSearchConfig = config;
                ConfigPath = configPath;
                // Filter must have a name - throw exception if missing
                if (string.IsNullOrWhiteSpace(filterConfig.Name))
                    throw new InvalidOperationException(
                        "Filter config must have a valid Name property"
                    );
                FilterName = filterConfig.Name;
                _searchStartTime = DateTime.UtcNow;
                _isRunning = true;

                _cancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(
                    cancellationToken
                );

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
                DebugLogger.LogImportant(
                    $"SearchInstance[{_searchId}]",
                    $"Database properly configured with {_columnNames.Count} columns"
                );

                // Register direct callback with Motely.Filters.MotelyJsonFilterDesc
                DebugLogger.LogImportant(
                    $"SearchInstance[{_searchId}]",
                    "Registering OnResultFound callback"
                );
                DebugLogger.LogImportant(
                    $"SearchInstance[{_searchId}]",
                    $"Motely.Filters.MotelyJsonConfig has {filterConfig.Must?.Count ?? 0} MUST clauses"
                );
                DebugLogger.LogImportant(
                    $"SearchInstance[{_searchId}]",
                    $"Motely.Filters.MotelyJsonConfig has {filterConfig.Should?.Count ?? 0} SHOULD clauses"
                );
                DebugLogger.LogImportant(
                    $"SearchInstance[{_searchId}]",
                    $"Motely.Filters.MotelyJsonConfig has {filterConfig.MustNot?.Count ?? 0} MUST NOT clauses"
                );

                // Log the actual filter content for debugging
                if (filterConfig.Must != null)
                {
                    foreach (var must in filterConfig.Must)
                    {
                        DebugLogger.LogImportant(
                            $"SearchInstance[{_searchId}]",
                            $"  MUST: Type={must.Type}, Value={must.Value}"
                        );
                    }
                }
                if (filterConfig.Should != null)
                {
                    foreach (var should in filterConfig.Should)
                    {
                        DebugLogger.LogImportant(
                            $"SearchInstance[{_searchId}]",
                            $"  SHOULD: Type={should.Type}, Value={should.Value}, Score={should.Score}"
                        );
                    }
                }

                // (Result callback now assigned inside RunSearchInProcess after filter creation)

                // Run the search using the MotelySearchService pattern
                DebugLogger.Log($"SearchInstance[{_searchId}]", "Starting in-process search...");

                // Start search on threadpool via async continuation (proper async/await chain)
                _searchTask = RunSearchWithCompletionHandling(
                    filterConfig,
                    searchCriteria,
                    progress,
                    _cancellationTokenSource.Token
                );

                // Fire-and-forget is intentional here - UI should not wait
                DebugLogger.Log($"SearchInstance[{_searchId}]", "Search started in background");
            }
            catch (Exception ex)
            {
                // Only catch setup exceptions
                DebugLogger.LogError(
                    $"SearchInstance[{_searchId}]",
                    $"Failed to start: {ex.Message}"
                );
                _isRunning = false;
                throw;
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
            DebugLogger.LogImportant(
                $"SearchInstance[{_searchId}]",
                $"StartSearchAsync ENTERED! ConfigPath={criteria.ConfigPath}"
            );

            // Load the config from file
            if (string.IsNullOrEmpty(criteria.ConfigPath))
            {
                var errorMsg = "Config path is required but was null or empty";
                DebugLogger.LogError($"SearchInstance[{_searchId}]", errorMsg);
                throw new ArgumentException(errorMsg);
            }

            if (!System.IO.File.Exists(criteria.ConfigPath))
            {
                var errorMsg = $"Config file not found: {criteria.ConfigPath}";
                DebugLogger.LogError($"SearchInstance[{_searchId}]", errorMsg);
                throw new System.IO.FileNotFoundException(errorMsg, criteria.ConfigPath);
            }

            DebugLogger.Log(
                $"SearchInstance[{_searchId}]",
                $"Loading config from: {criteria.ConfigPath}"
            );

            // Load and validate the filter config - use LoadFromJson to get PostProcess!
            if (
                !Motely.Filters.MotelyJsonConfig.TryLoadFromJsonFile(
                    criteria.ConfigPath,
                    out var config
                )
            )
            {
                var errorMsg = $"Failed to load or parse config from {criteria.ConfigPath}";
                DebugLogger.LogError($"SearchInstance[{_searchId}]", errorMsg);
                throw new Exception(errorMsg);
            }

            DebugLogger.Log(
                $"SearchInstance[{_searchId}]",
                $"Config loaded successfully: {config.Name}"
            );

            // DEBUG: Log what was actually deserialized
            DebugLogger.LogImportant(
                $"SearchInstance[{_searchId}]",
                $"JSON DESERIALIZATION RESULT:"
            );
            DebugLogger.LogImportant(
                $"SearchInstance[{_searchId}]",
                $"  Config.Name: '{config.Name}'"
            );
            DebugLogger.LogImportant(
                $"SearchInstance[{_searchId}]",
                $"  Config.Must: {(config.Must?.Count ?? 0)} items"
            );
            DebugLogger.LogImportant(
                $"SearchInstance[{_searchId}]",
                $"  Config.Should: {(config.Should?.Count ?? 0)} items"
            );
            DebugLogger.LogImportant(
                $"SearchInstance[{_searchId}]",
                $"  Config.MustNot: {(config.MustNot?.Count ?? 0)} items"
            );

            string? rawJsonForDebug = null; // only load if needed

            if (config.Should != null && config.Should.Count > 0)
            {
                DebugLogger.LogImportant(
                    $"SearchInstance[{_searchId}]",
                    $"  First Should item: Type='{config.Should[0].Type}', Value='{config.Should[0].Value}'"
                );
            }
            else
            {
                DebugLogger.LogError(
                    $"SearchInstance[{_searchId}]",
                    $"  PROBLEM: Should array is null or empty after deserialization!"
                );
                try
                {
                    rawJsonForDebug = System.IO.File.ReadAllText(criteria.ConfigPath);
                    DebugLogger.LogError(
                        $"SearchInstance[{_searchId}]",
                        $"  Raw JSON length: {rawJsonForDebug.Length} characters"
                    );
                    DebugLogger.LogError(
                        $"SearchInstance[{_searchId}]",
                        $"  First 500 chars of JSON: {rawJsonForDebug.Substring(0, Math.Min(500, rawJsonForDebug.Length))}"
                    );
                }
                catch (Exception rx)
                {
                    DebugLogger.LogError(
                        $"SearchInstance[{_searchId}]",
                        $"  Failed to read raw JSON for debug: {rx.Message}"
                    );
                }
            }

            // Store the config path for reference
            ConfigPath = criteria.ConfigPath;
            FilterName = System.IO.Path.GetFileNameWithoutExtension(criteria.ConfigPath);

            // CRITICAL: Configure the SearchHistoryService BEFORE anything else!
            // This must happen before StartSearchFromFileAsync to ensure proper database schema
            DebugLogger.LogImportant(
                $"SearchInstance[{_searchId}]",
                "Configuring SearchHistoryService with filter config"
            );
            DebugLogger.LogImportant(
                $"SearchInstance[{_searchId}]",
                $"Filter config loaded: Name='{config.Name}'"
            );
            DebugLogger.LogImportant(
                $"SearchInstance[{_searchId}]",
                $"  Must clauses: {config.Must?.Count ?? 0}"
            );
            DebugLogger.LogImportant(
                $"SearchInstance[{_searchId}]",
                $"  Should clauses: {config.Should?.Count ?? 0}"
            );
            DebugLogger.LogImportant(
                $"SearchInstance[{_searchId}]",
                $"  MustNot clauses: {config.MustNot?.Count ?? 0}"
            );

            if (config.Should != null && config.Should.Count > 0)
            {
                for (int i = 0; i < config.Should.Count; i++)
                {
                    var should = config.Should[i];
                    DebugLogger.LogImportant(
                        $"SearchInstance[{_searchId}]",
                        $"    Should[{i}]: Type={should.Type}, Value={should.Value}, Score={should.Score}"
                    );
                }
            }

            SetupDatabase(config, criteria.ConfigPath);
            DebugLogger.LogImportant(
                $"SearchInstance[{_searchId}]",
                $"Database configured with {_columnNames.Count} columns"
            );

            // Auto-resume logic now that DB is initialized
            if (criteria.StartBatch == 0 && !criteria.EnableDebugOutput)
            {
                try
                {
                    var last = await GetLastBatchAsync().ConfigureAwait(false);
                    if (last.HasValue && last.Value > 0)
                    {
                        criteria.StartBatch = last.Value;
                        DebugLogger.LogImportant(
                            $"SearchInstance[{_searchId}]",
                            $"Resuming from saved batch {last.Value:N0}"
                        );
                    }
                }
                catch (Exception resumeEx)
                {
                    DebugLogger.LogError(
                        $"SearchInstance[{_searchId}]",
                        $"Resume check failed: {resumeEx.Message}"
                    );
                }
            }

            // Convert SearchCriteria to SearchConfiguration
            var searchConfig = new SearchConfiguration
            {
                ThreadCount = criteria.ThreadCount,
                MinScore = criteria.MinScore,
                BatchSize = criteria.BatchSize,
                Deck = criteria.Deck ?? "Red",
                Stake = criteria.Stake ?? "White",
                StartBatch = criteria.StartBatch,
                EndBatch = criteria.EndBatch,
                DebugMode = criteria.EnableDebugOutput,
                DebugSeed = criteria.DebugSeed,
            };

            // Call the main search method with the file path
            StartSearchFromFile(criteria.ConfigPath, searchConfig, progress, cancellationToken);
        }

        /// <summary>
        /// Start searching with a config object directly (no file I/O)
        /// Used for quick in-memory tests of unsaved filters
        /// </summary>
        public Task StartSearchAsync(
            SearchCriteria criteria,
            MotelyJsonConfig config,
            IProgress<SearchProgress>? progress = null,
            CancellationToken cancellationToken = default
        )
        {
            DebugLogger.LogImportant(
                $"SearchInstance[{_searchId}]",
                $"StartSearchAsync (in-memory) ENTERED! Config Name={config.Name}"
            );

            if (config == null)
            {
                var errorMsg = "Config object is required but was null";
                DebugLogger.LogError($"SearchInstance[{_searchId}]", errorMsg);
                throw new ArgumentNullException(nameof(config), errorMsg);
            }

            DebugLogger.Log(
                $"SearchInstance[{_searchId}]",
                $"Using in-memory config: {config.Name}"
            );

            // DEBUG: Log what was provided
            DebugLogger.LogImportant($"SearchInstance[{_searchId}]", $"IN-MEMORY CONFIG:");
            DebugLogger.LogImportant(
                $"SearchInstance[{_searchId}]",
                $"  Config.Name: '{config.Name}'"
            );
            DebugLogger.LogImportant(
                $"SearchInstance[{_searchId}]",
                $"  Config.Must: {(config.Must?.Count ?? 0)} items"
            );
            DebugLogger.LogImportant(
                $"SearchInstance[{_searchId}]",
                $"  Config.Should: {(config.Should?.Count ?? 0)} items"
            );
            DebugLogger.LogImportant(
                $"SearchInstance[{_searchId}]",
                $"  Config.MustNot: {(config.MustNot?.Count ?? 0)} items"
            );

            if (config.Should != null && config.Should.Count > 0)
            {
                for (int i = 0; i < config.Should.Count; i++)
                {
                    var should = config.Should[i];
                    DebugLogger.LogImportant(
                        $"SearchInstance[{_searchId}]",
                        $"    Should[{i}]: Type={should.Type}, Value={should.Value}, Score={should.Score}"
                    );
                }
            }

            // Store the config info for reference (no file path for in-memory)
            ConfigPath = string.Empty;
            FilterName = config.Name ?? "InMemoryTest";

            SetupDatabase(config, $"InMemory_{config.Name}.json");
            DebugLogger.LogImportant(
                $"SearchInstance[{_searchId}]",
                $"Database configured with {_columnNames.Count} columns"
            );

            // Convert SearchCriteria to SearchConfiguration
            var searchConfig = new SearchConfiguration
            {
                ThreadCount = criteria.ThreadCount,
                MinScore = criteria.MinScore,
                BatchSize = criteria.BatchSize,
                Deck = criteria.Deck ?? "Red",
                Stake = criteria.Stake ?? "White",
                StartBatch = criteria.StartBatch,
                EndBatch = criteria.EndBatch,
                DebugMode = criteria.EnableDebugOutput,
                DebugSeed = criteria.DebugSeed,
            };

            // Call the main search method with the config object (synchronous fire-and-forget)
            StartSearchFromConfig(config, searchConfig, progress, cancellationToken);

            // Return completed task immediately (fire-and-forget pattern by design)
            return Task.CompletedTask;
        }

        /// <summary>
        /// Private method to run search from a config object (no file loading)
        /// </summary>
        private void StartSearchFromConfig(
            MotelyJsonConfig filterConfig,
            SearchConfiguration config,
            IProgress<SearchProgress>? progress = null,
            CancellationToken cancellationToken = default
        )
        {
            DebugLogger.LogImportant(
                $"SearchInstance[{_searchId}]",
                $"StartSearchFromConfigAsync ENTERED! config.Name={filterConfig.Name}"
            );

            if (_isRunning)
            {
                DebugLogger.Log($"SearchInstance[{_searchId}]", "Search already running");
                return;
            }

            try
            {
                DebugLogger.Log(
                    $"SearchInstance[{_searchId}]",
                    $"Starting search from in-memory config: {filterConfig.Name}"
                );

                _currentConfig = filterConfig;
                _currentSearchConfig = config;
                FilterName = filterConfig.Name ?? "InMemoryFilter";
                _searchStartTime = DateTime.UtcNow;
                _isRunning = true;

                _cancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(
                    cancellationToken
                );

                // Notify UI that search started
                SearchStarted?.Invoke(this, EventArgs.Empty);

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
                DebugLogger.LogImportant(
                    $"SearchInstance[{_searchId}]",
                    $"Database properly configured with {_columnNames.Count} columns"
                );

                // Register direct callback with Motely.Filters.MotelyJsonFilterDesc
                DebugLogger.LogImportant(
                    $"SearchInstance[{_searchId}]",
                    "Registering OnResultFound callback"
                );
                DebugLogger.LogImportant(
                    $"SearchInstance[{_searchId}]",
                    $"Motely.Filters.MotelyJsonConfig has {filterConfig.Must?.Count ?? 0} MUST clauses"
                );
                DebugLogger.LogImportant(
                    $"SearchInstance[{_searchId}]",
                    $"Motely.Filters.MotelyJsonConfig has {filterConfig.Should?.Count ?? 0} SHOULD clauses"
                );
                DebugLogger.LogImportant(
                    $"SearchInstance[{_searchId}]",
                    $"Motely.Filters.MotelyJsonConfig has {filterConfig.MustNot?.Count ?? 0} MUST NOT clauses"
                );

                // Log the actual filter content for debugging
                if (filterConfig.Must != null)
                {
                    foreach (var must in filterConfig.Must)
                    {
                        DebugLogger.LogImportant(
                            $"SearchInstance[{_searchId}]",
                            $"  MUST: Type={must.Type}, Value={must.Value}"
                        );
                    }
                }
                if (filterConfig.Should != null)
                {
                    foreach (var should in filterConfig.Should)
                    {
                        DebugLogger.LogImportant(
                            $"SearchInstance[{_searchId}]",
                            $"  SHOULD: Type={should.Type}, Value={should.Value}, Score={should.Score}"
                        );
                    }
                }

                // Run the search using the MotelySearchService pattern
                DebugLogger.Log($"SearchInstance[{_searchId}]", "Starting in-process search...");

                // Start search on threadpool via async continuation (proper async/await chain)
                _searchTask = RunSearchWithCompletionHandling(
                    filterConfig,
                    searchCriteria,
                    progress,
                    _cancellationTokenSource.Token
                );

                // Fire-and-forget is intentional here - UI should not wait
                DebugLogger.Log($"SearchInstance[{_searchId}]", "Search started in background");
            }
            catch (Exception ex)
            {
                // Only catch setup exceptions
                DebugLogger.LogError(
                    $"SearchInstance[{_searchId}]",
                    $"Failed to start: {ex.Message}"
                );
                _isRunning = false;
                throw;
            }
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
            if (_isRunning)
            {
                DebugLogger.Log(
                    $"SearchInstance[{_searchId}]",
                    "Search already running, cannot resume"
                );
                return;
            }

            // Load saved search state
            var resumeState = _userProfileService?.GetSearchState();
            if (resumeState == null)
            {
                DebugLogger.Log($"SearchInstance[{_searchId}]", "No saved state to resume from");
                return;
            }

            // Create search criteria from resume state
            var resumeCriteria = new SearchCriteria
            {
                ConfigPath = resumeState.ConfigPath,
                BatchSize = (int)resumeState.BatchSize,
                ThreadCount = (int)resumeState.ThreadCount,
                MinScore = resumeState.MinScore,
                Deck = resumeState.Deck,
                Stake = resumeState.Stake,
                StartBatch = resumeState.LastCompletedBatch + 1,
                EndBatch = resumeState.EndBatch,
                EnableDebugOutput = false,
            };

            // Update our saved config path
            ConfigPath = resumeState.ConfigPath ?? string.Empty;

            DebugLogger.Log(
                $"SearchInstance[{_searchId}]",
                $"Resuming search from batch {resumeCriteria.StartBatch} to {resumeCriteria.EndBatch}"
            );

            // Restart the search from the saved position (fire-and-forget is intentional for UI responsiveness)
            _ = StartSearchAsync(resumeCriteria);
        }

        /// <summary>
        /// Wraps RunSearchInProcess with proper completion handling (replaces Task.Run hack)
        /// </summary>
        private async Task RunSearchWithCompletionHandling(
            Motely.Filters.MotelyJsonConfig filterConfig,
            SearchCriteria searchCriteria,
            IProgress<SearchProgress>? progress,
            CancellationToken cancellationToken
        )
        {
            try
            {
                await RunSearchInProcess(filterConfig, searchCriteria, progress, cancellationToken)
                    .ConfigureAwait(false);
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

                // Flush and close appender
                ForceFlush();

                // Flush any pending search state to disk before stopping (unless prevented)
                if (!preventStateSave)
                {
                    _userProfileService?.FlushProfile();
                }

                // Force _isRunning to false IMMEDIATELY
                _isRunning = false;

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
                            DebugLogger.LogError(
                                $"SearchInstance[{_searchId}]",
                                "Search task did not complete within timeout"
                            );
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
                            DebugLogger.LogError(
                                $"SearchInstance[{_searchId}]",
                                "Search disposal timed out (abandoning)"
                            );
                        }
                        else
                        {
                            DebugLogger.Log(
                                $"SearchInstance[{_searchId}]",
                                "Search disposed successfully"
                            );
                        }
                    }
                    catch (Exception ex)
                    {
                        DebugLogger.LogError(
                            $"SearchInstance[{_searchId}]",
                            $"Error disposing search: {ex.Message}"
                        );
                    }
                    finally
                    {
                        _currentSearch = null;
                    }
                }
            }
        }

        // Removed - using shared FilterCategoryMapper.GroupClausesByCategory instead

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
                // Pre-flight validation
                if (config == null)
                {
                    throw new ArgumentNullException(
                        nameof(config),
                        "Search configuration cannot be null"
                    );
                }

                try
                {
                    MotelyJsonConfigValidator.ValidateConfig(config);
                }
                catch (ArgumentException ex)
                {
                    progress?.Report(
                        new SearchProgress
                        {
                            Message = $"Filter validation failed:\n{ex.Message}",
                            HasError = true,
                            IsComplete = true,
                        }
                    );
                    throw;
                }

                // Cancellation is fully implemented via CancellationToken

                // Enable Motely's DebugLogger if in debug mode
                Motely.DebugLogger.IsEnabled = criteria.EnableDebugOutput;
                if (criteria.EnableDebugOutput)
                {
                    DebugLogger.LogImportant(
                        $"SearchInstance[{_searchId}]",
                        "Debug mode enabled - Motely DebugLogger activated"
                    );
                }

                // Create filter descriptor with new API
                DebugLogger.LogImportant(
                    $"SearchInstance[{_searchId}]",
                    $"Creating MotelyJsonFilterDesc with config: {config.Name ?? "unnamed"}"
                );
                DebugLogger.LogImportant(
                    $"SearchInstance[{_searchId}]",
                    $"Config has {config.Must?.Count ?? 0} MUST, {config.Should?.Count ?? 0} SHOULD, {config.MustNot?.Count ?? 0} MUST NOT clauses"
                );

                // CRITICAL FIX: Only MUST clauses go to filters - Should clauses are for scoring only!
                // This matches the proven working reference implementation in JsonSearchExecutor.cs
                List<MotelyJsonConfig.MotleyJsonFilterClause> mustClauses =
                    config.Must?.ToList() ?? new List<MotelyJsonConfig.MotleyJsonFilterClause>();

                // Initialize parsed enums for all MUST clauses with helpful errors
                for (int i = 0; i < mustClauses.Count; i++)
                {
                    var clause = mustClauses[i];
                    try
                    {
                        clause.InitializeParsedEnums();
                    }
                    catch (Exception ex)
                    {
                        var typeText = string.IsNullOrEmpty(clause.Type)
                            ? "<missing>"
                            : clause.Type;
                        var valueText = !string.IsNullOrEmpty(clause.Value)
                            ? clause.Value
                            : (
                                clause.Values != null && clause.Values.Length > 0
                                    ? string.Join(", ", clause.Values)
                                    : "<none>"
                            );
                        throw new ArgumentException(
                            $"Config error in MUST[{i}] — type: '{typeText}', value(s): '{valueText}'. {ex.Message}\nSuggestion: Add 'type' and 'value' (or 'values'): {{ \"type\": \"Joker\", \"value\": \"Perkeo\" }}"
                        );
                    }
                }

                DebugLogger.LogImportant(
                    $"SearchInstance[{_searchId}]",
                    $"Prepared {mustClauses.Count} MUST clauses for filtering (SHOULD clauses are for scoring only)"
                );
                foreach (var clause in mustClauses)
                {
                    DebugLogger.LogImportant(
                        $"SearchInstance[{_searchId}]",
                        $"  MUST Clause: Type={clause.Type}, Value={clause.Value}"
                    );
                }

                // Group MUST clauses by category using shared utility (matches JsonSearchExecutor.cs)
                var clausesByCategory = FilterCategoryMapper.GroupClausesByCategory(mustClauses);

                // Log the grouped categories (including And/Or if present)
                DebugLogger.LogImportant(
                    $"SearchInstance[{_searchId}]",
                    $"Grouped into {clausesByCategory.Count} filter categories:"
                );
                foreach (var kvp in clausesByCategory)
                {
                    DebugLogger.LogImportant(
                        $"SearchInstance[{_searchId}]",
                        $"  {kvp.Key}: {kvp.Value.Count} clause(s)"
                    );
                }

                if (clausesByCategory.Count == 0)
                {
                    throw new Exception(
                        "Cannot search with an empty filter! Please add at least one item to Must, Should, or MustNot zones in the Visual Builder."
                    );
                }

                // Create scoring config (only SHOULD clauses for scoring)
                var scoringConfig = new MotelyJsonConfig
                {
                    Name = config.Name,
                    Must = new List<MotelyJsonConfig.MotleyJsonFilterClause>(),
                    Should = config.Should ?? new List<MotelyJsonConfig.MotleyJsonFilterClause>(),
                    MustNot = new List<MotelyJsonConfig.MotleyJsonFilterClause>(),
                };

                // Propagate top-level scoring mode and initialize computed fields
                scoringConfig.Mode = config.Mode;
                scoringConfig.PostProcess();

                // Create result callback that writes to DuckDB
                Action<MotelySeedScoreTally> resultCallback = (result) =>
                {
                    try
                    {
                        // Increment result counter
                        Interlocked.Increment(ref _resultCount);

                        // Convert Motely result to SearchResult
                        var searchResult = new SearchResult
                        {
                            Seed = result.Seed,
                            TotalScore = result.Score,
                            Scores = result.TallyColumns?.ToArray(),
                        };

                        // Write to DuckDB
                        AddSearchResult(searchResult);

                        // LOG THE SEED TO CONSOLE for immediate user feedback
                        DebugLogger.LogImportant(
                            $"SearchInstance[{_searchId}]",
                            $"✅ FOUND SEED: {result.Seed} (Score: {result.Score})"
                        );
                    }
                    catch (Exception ex)
                    {
                        DebugLogger.LogError(
                            $"SearchInstance[{_searchId}]",
                            $"Failed to process result {result.Seed}: {ex.Message}"
                        );
                    }
                };

                var scoreDesc = new MotelyJsonSeedScoreDesc(
                    scoringConfig,
                    criteria.MinScore,
                    criteria.MinScore == 0,
                    resultCallback
                );

                // Detect how many filter categories we have
                var categories = clausesByCategory.Keys.ToList();

                IMotelySearch search;

                // BYPASS BROKEN CHAINING: Use composite filter for multiple categories
                if (categories.Count > 1)
                {
                    // Multiple categories - use composite filter to avoid broken chaining
                    DebugLogger.LogImportant(
                        $"SearchInstance[{_searchId}]",
                        $"[COMPOSITE] Creating composite filter with {categories.Count} filter types"
                    );

                    // Merge MustNot clauses into mustClauses with IsInverted flag (like JsonSearchExecutor does)
                    var allRequiredClauses = new List<MotelyJsonConfig.MotleyJsonFilterClause>(
                        mustClauses
                    );

                    if (config.MustNot != null && config.MustNot.Count > 0)
                    {
                        // Initialize parsed enums for MustNot clauses
                        for (int i = 0; i < config.MustNot.Count; i++)
                        {
                            var clause = config.MustNot[i];
                            try
                            {
                                clause.InitializeParsedEnums();
                            }
                            catch (Exception ex)
                            {
                                var typeText = string.IsNullOrEmpty(clause.Type)
                                    ? "<missing>"
                                    : clause.Type;
                                var valueText = !string.IsNullOrEmpty(clause.Value)
                                    ? clause.Value
                                    : (
                                        clause.Values != null && clause.Values.Length > 0
                                            ? string.Join(", ", clause.Values)
                                            : "<none>"
                                    );
                                throw new ArgumentException(
                                    $"Config error in MUSTNOT[{i}] — type: '{typeText}', value(s): '{valueText}'. {ex.Message}"
                                );
                            }
                        }

                        DebugLogger.LogImportant(
                            $"SearchInstance[{_searchId}]",
                            $"   + Including MustNot: {config.MustNot.Count} inverted clauses (exclusion)"
                        );

                        // Mark mustNot clauses as inverted and add to the composite
                        foreach (var clause in config.MustNot)
                        {
                            clause.IsInverted = true;
                            allRequiredClauses.Add(clause);
                        }
                    }

                    var compositeFilter = new MotelyCompositeFilterDesc(allRequiredClauses);
                    var compositeSettings =
                        new MotelySearchSettings<MotelyCompositeFilterDesc.MotelyCompositeFilter>(
                            compositeFilter
                        );

                    // Apply all search settings
                    compositeSettings = compositeSettings
                        .WithThreadCount(criteria.ThreadCount)
                        .WithBatchCharacterCount(criteria.BatchSize)
                        .WithStartBatchIndex((long)criteria.StartBatch);

                    // Only set EndBatch if specified (ulong.MaxValue means infinite)
                    if (criteria.EndBatch > 0 && criteria.EndBatch < ulong.MaxValue)
                        compositeSettings = compositeSettings.WithEndBatchIndex(
                            (long)criteria.EndBatch
                        );

                    // Apply deck/stake if specified
                    if (
                        !string.IsNullOrEmpty(criteria.Deck)
                        && Enum.TryParse(criteria.Deck, true, out MotelyDeck compositeDeck)
                    )
                        compositeSettings = compositeSettings.WithDeck(compositeDeck);
                    if (
                        !string.IsNullOrEmpty(criteria.Stake)
                        && Enum.TryParse(criteria.Stake, true, out MotelyStake compositeStake)
                    )
                        compositeSettings = compositeSettings.WithStake(compositeStake);

                    // Apply scoring if needed
                    bool compositeNeedsScoring = (config.Should?.Count > 0);
                    if (compositeNeedsScoring)
                    {
                        compositeSettings = compositeSettings.WithSeedScoreProvider(scoreDesc);
                        compositeSettings = compositeSettings.WithCsvOutput(true);
                    }

                    DebugLogger.LogImportant(
                        $"SearchInstance[{_searchId}]",
                        $"Starting composite search with {criteria.ThreadCount} threads"
                    );

                    // Start search with composite filter (no chaining needed!)
                    search = compositeSettings.WithSequentialSearch().Start();
                }
                else
                {
                    // Single category - check if it's And/Or (composite) or specialized filter
                    var primaryCategory = categories[0];
                    var primaryClauses = clausesByCategory[primaryCategory];

                    // CRITICAL FIX: And/Or categories need MotelyCompositeFilterDesc, not SpecializedFilterFactory
                    if (
                        primaryCategory == FilterCategory.And
                        || primaryCategory == FilterCategory.Or
                    )
                    {
                        DebugLogger.LogImportant(
                            $"SearchInstance[{_searchId}]",
                            $"Single {primaryCategory} category - using composite filter with {primaryClauses.Count} clauses"
                        );

                        var compositeFilter = new MotelyCompositeFilterDesc(primaryClauses);
                        var compositeSettings =
                            new MotelySearchSettings<MotelyCompositeFilterDesc.MotelyCompositeFilter>(
                                compositeFilter
                            )
                                .WithThreadCount(criteria.ThreadCount)
                                .WithBatchCharacterCount(criteria.BatchSize)
                                .WithStartBatchIndex((long)criteria.StartBatch);

                        if (criteria.EndBatch > 0 && criteria.EndBatch < ulong.MaxValue)
                            compositeSettings = compositeSettings.WithEndBatchIndex(
                                (long)criteria.EndBatch
                            );

                        if (
                            !string.IsNullOrEmpty(criteria.Deck)
                            && Enum.TryParse(criteria.Deck, true, out MotelyDeck deck)
                        )
                            compositeSettings = compositeSettings.WithDeck(deck);
                        if (
                            !string.IsNullOrEmpty(criteria.Stake)
                            && Enum.TryParse(criteria.Stake, true, out MotelyStake stake)
                        )
                            compositeSettings = compositeSettings.WithStake(stake);

                        compositeSettings = compositeSettings.WithSeedScoreProvider(scoreDesc);
                        if (config.Should?.Count > 0)
                            compositeSettings = compositeSettings.WithCsvOutput(true);

                        search = compositeSettings.WithSequentialSearch().Start();
                    }
                    else
                    {
                        // Regular specialized filter for other categories
                        var filterDesc =
                            Motely.Utils.SpecializedFilterFactory.CreateSpecializedFilter(
                                primaryCategory,
                                primaryClauses
                            );
                        DebugLogger.LogImportant(
                            $"SearchInstance[{_searchId}]",
                            $"Optimized single-category filter: {primaryCategory} with {primaryClauses.Count} clauses"
                        );

                        // Use interface approach for specialized filter compatibility
                        var searchSettings = Motely
                            .Utils.SpecializedFilterFactory.CreateSearchSettings(filterDesc)
                            .WithThreadCount(criteria.ThreadCount)
                            .WithBatchCharacterCount(criteria.BatchSize)
                            .WithStartBatchIndex((long)criteria.StartBatch);

                        // Only set EndBatch if specified (ulong.MaxValue means infinite)
                        if (criteria.EndBatch > 0 && criteria.EndBatch < ulong.MaxValue)
                            searchSettings = searchSettings.WithEndBatchIndex(
                                (long)criteria.EndBatch
                            );

                        searchSettings = searchSettings.WithSeedScoreProvider(scoreDesc);

                        DebugLogger.LogImportant(
                            $"SearchInstance[{_searchId}]",
                            $"Starting single-category search with {criteria.ThreadCount} threads"
                        );

                        // Create search with single specialized filter
                        search = searchSettings.Start();
                    }
                }
                _currentSearch = search;

                DebugLogger.LogError(
                    $"SearchInstance[{_searchId}]",
                    $"🚀 Search created with {categories.Count} filter category(ies): {string.Join(", ", categories)}"
                );
                DebugLogger.LogError(
                    $"SearchInstance[{_searchId}]",
                    $"🚀 Search settings: Threads={criteria.ThreadCount}, StartBatch={criteria.StartBatch}, EndBatch={criteria.EndBatch}"
                );
                DebugLogger.LogError(
                    $"SearchInstance[{_searchId}]",
                    $"🚀 Search started! New status: {_currentSearch.Status}"
                );

                // Wait for search completion
                DebugLogger.LogError(
                    $"SearchInstance[{_searchId}]",
                    $"🔄 Entering wait loop - Status: {_currentSearch.Status}, Cancelled: {cancellationToken.IsCancellationRequested}, Running: {_isRunning}"
                );
                while (
                    _currentSearch.Status == MotelySearchStatus.Running
                    && !cancellationToken.IsCancellationRequested
                    && _isRunning
                )
                {
                    // Report progress
                    var completedBatches = _currentSearch.CompletedBatchCount;

                    // CRITICAL FIX: Calculate progress percentage correctly for infinite searches
                    // When EndBatch is ulong.MaxValue (infinite search), we can't calculate meaningful percentage
                    // Instead, show progress based on seeds searched or use indeterminate progress
                    double progressPercent = 0.0;
                    var totalBatches = criteria.EndBatch - criteria.StartBatch;

                    if (criteria.EndBatch != ulong.MaxValue && totalBatches > 0)
                    {
                        // Finite search - calculate actual percentage
                        progressPercent = ((double)completedBatches / totalBatches) * 100.0;
                    }
                    else
                    {
                        // Infinite search - show "running" indicator with batch count instead
                        // This prevents division by infinity resulting in 0%
                        progressPercent = Math.Min(99.99, completedBatches * 0.001); // Arbitrary but visible progress
                    }

                    // Calculate speed - account for StartBatch offset and edge cases
                    var elapsed = DateTime.UtcNow - _searchStartTime;
                    var actualBatchesProcessed = Math.Max(0, completedBatches); // Ensure non-negative
                    var seedsSearched = (ulong)(
                        actualBatchesProcessed * Math.Pow(35, criteria.BatchSize + 1)
                    );

                    // Prevent division by zero and negative/invalid elapsed time
                    var seedsPerMs =
                        elapsed.TotalMilliseconds > 0
                            ? seedsSearched / elapsed.TotalMilliseconds
                            : 0;

                    // Debug logging for negative seeds/ms issue
                    if (seedsPerMs < 0)
                    {
                        DebugLogger.LogError(
                            $"SearchInstance[{_searchId}]",
                            $"Negative seeds/ms detected: seedsSearched={seedsSearched}, elapsed={elapsed.TotalMilliseconds}ms, completedBatches={completedBatches}"
                        );
                        seedsPerMs = 0; // Reset to 0 to avoid displaying negative values
                    }

                    // Calculate ETA based on progress percentage and elapsed time
                    TimeSpan? estimatedTimeRemaining = null;
                    if (
                        progressPercent > 0
                        && progressPercent < 100
                        && elapsed.TotalMilliseconds > 0
                    )
                    {
                        // Total time = elapsed / (progress / 100)
                        // Time remaining = total time - elapsed
                        double totalEstimatedMs =
                            elapsed.TotalMilliseconds / (progressPercent / 100.0);
                        double remainingMs = totalEstimatedMs - elapsed.TotalMilliseconds;

                        if (
                            remainingMs > 0
                            && !double.IsNaN(remainingMs)
                            && !double.IsInfinity(remainingMs)
                        )
                        {
                            estimatedTimeRemaining = TimeSpan.FromMilliseconds(
                                Math.Min(remainingMs, TimeSpan.MaxValue.TotalMilliseconds)
                            );
                        }
                    }

                    var currentProgress = new SearchProgress
                    {
                        SeedsSearched = seedsSearched,
                        PercentComplete = progressPercent,
                        SeedsPerMillisecond = seedsPerMs,
                        Message = $"Searched {completedBatches:N0} batches",
                        ResultsFound = _resultCount,
                        EstimatedTimeRemaining = estimatedTimeRemaining,
                    };

                    progress?.Report(currentProgress);
                    ProgressUpdated?.Invoke(this, currentProgress);

                    try
                    {
                        // CRITICAL FIX: Increased delay from 100ms to 500ms for balanced responsiveness
                        // With batch flush threshold reduced to 1, progress updates are immediate
                        // 500ms polling provides smooth UI updates without excessive overhead
                        await Task.Delay(500, cancellationToken).ConfigureAwait(false);
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
                    $"Search completed with status: {_currentSearch.Status}"
                );
            }
            catch (OperationCanceledException)
            {
                // User cancelled the search
                var cancelMessage = "Search cancelled by user";
                DebugLogger.Log($"SearchInstance[{_searchId}]", cancelMessage);
                AddToConsole($"⚠️ {cancelMessage}");
                progress?.Report(
                    new SearchProgress
                    {
                        Message = cancelMessage,
                        HasError = false,
                        IsComplete = true,
                    }
                );
            }
            catch (OutOfMemoryException oom)
            {
                // Critical memory error
                var memoryError =
                    "Search failed: Out of memory. Try reducing batch size or thread count.";
                DebugLogger.LogError(
                    $"SearchInstance[{_searchId}]",
                    $"Out of memory: {oom.Message}"
                );
                AddToConsole($"❌ {memoryError}");
                progress?.Report(
                    new SearchProgress
                    {
                        Message = memoryError,
                        HasError = true,
                        IsComplete = true,
                    }
                );
            }
            catch (Exception ex)
            {
                // General error with detailed logging
                var userMessage = ex switch
                {
                    ArgumentException => $"Invalid search parameters: {ex.Message}",
                    InvalidOperationException => $"Search configuration error: {ex.Message}",
                    System.IO.IOException => $"File system error: {ex.Message}",
                    _ => $"Unexpected error: {ex.Message}",
                };

                DebugLogger.LogError(
                    $"SearchInstance[{_searchId}]",
                    $"RunSearchInProcess exception: {ex}"
                );
                AddToConsole($"❌ {userMessage}");

                // Log stack trace for debugging
                if (criteria?.EnableDebugOutput == true)
                {
                    AddToConsole($"Stack trace:\n{ex.StackTrace}");
                }

                progress?.Report(
                    new SearchProgress
                    {
                        Message = userMessage,
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
            if (_preventStateSave)
                return; // Don't save if we're removing the icon

            try
            {
                if (_currentConfig == null || _currentSearchConfig == null)
                    return;

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
                    TotalBatches = totalBatches,
                };

                _userProfileService?.SaveSearchState(state);
            }
            catch (Exception ex)
            {
                DebugLogger.LogError(
                    $"SearchInstance[{_searchId}]",
                    $"Failed to save search state: {ex.Message}"
                );
            }
        }

        public void Dispose()
        {
            // Stop search cleanly
            if (_isRunning)
                StopSearch();

            // Cancel token
            _cancellationTokenSource?.Dispose();

            // Close appender and connection
            lock (_appenderLock)
            {
                _appender?.Dispose();
                _appender = null;
            }
            _connection?.Dispose();

            // Dispose search
            _currentSearch?.Dispose();

            GC.SuppressFinalize(this);
        }
    }
}

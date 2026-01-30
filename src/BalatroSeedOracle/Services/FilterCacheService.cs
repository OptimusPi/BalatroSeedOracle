using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using BalatroSeedOracle.Helpers;
using BalatroSeedOracle.Services.Storage;
using Motely.Filters;

namespace BalatroSeedOracle.Services
{
    /// <summary>
    /// High-performance in-memory cache for filter files.
    /// Loads all filters from disk on startup and provides fast dictionary-based lookup by filter ID.
    /// Automatically invalidates cache entries when filters are saved or deleted.
    /// </summary>
    public interface IFilterCacheService : IDisposable
    {
        /// <summary>
        /// Loads all filters from the filters directory into memory cache.
        /// Should be called once during application startup.
        /// </summary>
        void Initialize();

        /// <summary>
        /// Gets a cached filter configuration by filter ID (filename without extension).
        /// Returns null if filter not found or failed to parse.
        /// </summary>
        /// <param name="filterId">Filter ID (filename without .json extension)</param>
        MotelyJsonConfig? GetFilter(string filterId);

        /// <summary>
        /// Gets a cached filter configuration by full file path.
        /// Returns null if filter not found or failed to parse.
        /// </summary>
        /// <param name="filePath">Full path to filter file</param>
        MotelyJsonConfig? GetFilterByPath(string filePath);

        /// <summary>
        /// Gets all cached filter configurations.
        /// Returns a copy to prevent external modification of cache.
        /// </summary>
        IReadOnlyList<CachedFilter> GetAllFilters();

        /// <summary>
        /// Invalidates a specific filter entry in the cache and reloads it from disk.
        /// Call this after saving a filter to ensure cache consistency.
        /// </summary>
        /// <param name="filterId">Filter ID to invalidate</param>
        void InvalidateFilter(string filterId);

        /// <summary>
        /// Async invalidation for browser; use from sync InvalidateFilter to avoid blocking.
        /// </summary>
        Task InvalidateFilterAsync(string filterId);

        /// <summary>
        /// Removes a filter from the cache.
        /// Call this after deleting a filter from disk.
        /// </summary>
        /// <param name="filterId">Filter ID to remove</param>
        void RemoveFilter(string filterId);

        /// <summary>
        /// Reloads all filters from disk, rebuilding the entire cache.
        /// Use sparingly - prefer InvalidateFilter for single updates.
        /// </summary>
        void RefreshCache();

        /// <summary>
        /// Gets the number of filters currently in cache.
        /// </summary>
        int Count { get; }
    }

    /// <summary>
    /// Represents a cached filter with metadata for fast access
    /// </summary>
    public class CachedFilter
    {
        public string FilterId { get; set; } = "";
        public string FilePath { get; set; } = "";
        public MotelyJsonConfig Config { get; set; } = null!;
        public DateTime LastModified { get; set; }
        public long FileSizeBytes { get; set; }

        // Computed properties for quick access without parsing
        public string Name => Config?.Name ?? "Untitled";
        public string Author => Config?.Author ?? "Unknown";
        public string Description => Config?.Description ?? "";
        public int MustCount => Config?.Must?.Count ?? 0;
        public int ShouldCount => Config?.Should?.Count ?? 0;
        public int MustNotCount => Config?.MustNot?.Count ?? 0;
    }

    public class FilterCacheService : IFilterCacheService
    {
        private readonly ConcurrentDictionary<string, CachedFilter> _cache;
        private readonly ReaderWriterLockSlim _cacheLock;
        private readonly IAppDataStore _store;
        private readonly IPlatformServices? _platformServices;
        private bool _isInitialized;
        private bool _disposed;

        public int Count => _cache.Count;

        public FilterCacheService(IAppDataStore store, IPlatformServices? platformServices = null)
        {
            _cache = new ConcurrentDictionary<string, CachedFilter>(
                StringComparer.OrdinalIgnoreCase
            );
            _cacheLock = new ReaderWriterLockSlim(LockRecursionPolicy.NoRecursion);
            _store = store ?? throw new ArgumentNullException(nameof(store));
            _platformServices = platformServices;
            _isInitialized = false;
        }

        public void Initialize()
        {
            if (_isInitialized)
            {
                DebugLogger.Log("FilterCacheService", "Already initialized, skipping...");
                return;
            }

            _cacheLock.EnterWriteLock();
            try
            {
                var startTime = DateTime.UtcNow;
                DebugLogger.Log("FilterCacheService", "Initializing filter cache...");

                var isBrowser = _platformServices != null && !_platformServices.SupportsFileSystem;
                if (isBrowser)
                {
                    // Browser: Initialize asynchronously - fire-and-forget
                    // Cache will be populated asynchronously
                    _ = InitializeAsync();
                    _isInitialized = true;
                    return;
                }

                var filtersDir = Helpers.AppPaths.FiltersDir;
                if (!Directory.Exists(filtersDir))
                {
                    DebugLogger.Log(
                        "FilterCacheService",
                        $"Filters directory does not exist: {filtersDir}"
                    );
                    _platformServices?.EnsureDirectoryExists(filtersDir);
                    _isInitialized = true;
                    return;
                }

                // Load both .json and .jaml filter files
                var jsonFiles = Directory.GetFiles(filtersDir, "*.json");
                var jamlFiles = Directory.GetFiles(filtersDir, "*.jaml");
                var filterFiles = jsonFiles
                    .Concat(jamlFiles)
                    .Where(f =>
                    {
                        var fileName = Path.GetFileName(f);
                        // Skip temporary and unsaved filters
                        return !fileName.StartsWith("_UNSAVED_", StringComparison.OrdinalIgnoreCase)
                            && !fileName.StartsWith("__TEMP_", StringComparison.OrdinalIgnoreCase);
                    })
                    .ToList();

                int successCount = 0;
                int failCount = 0;

                foreach (var filePath in filterFiles)
                {
                    try
                    {
                        var filterId = Path.GetFileNameWithoutExtension(filePath);
                        var cachedFilter = LoadFilterFromDisk(filePath, filterId);

                        if (cachedFilter != null)
                        {
                            _cache[filterId] = cachedFilter;
                            successCount++;
                        }
                        else
                        {
                            failCount++;
                        }
                    }
                    catch (Exception ex)
                    {
                        DebugLogger.LogError(
                            "FilterCacheService",
                            $"Failed to cache filter {Path.GetFileName(filePath)}: {ex.Message}"
                        );
                        failCount++;
                    }
                }

                var elapsed = (DateTime.UtcNow - startTime).TotalMilliseconds;
                DebugLogger.Log(
                    "FilterCacheService",
                    $"Cache initialized: {successCount} filters loaded, {failCount} failed in {elapsed:F2}ms"
                );

                _isInitialized = true;
            }
            finally
            {
                _cacheLock.ExitWriteLock();
            }
        }

        /// <summary>
        /// Async initialization for browser platform
        /// </summary>
        private async Task InitializeAsync()
        {
            try
            {
                // Load both .json and .jaml filter files
                var filterFiles = (await _store.ListKeysAsync("Filters/").ConfigureAwait(false))
                    .Where(k => k.EndsWith(".json", StringComparison.OrdinalIgnoreCase) 
                             || k.EndsWith(".jaml", StringComparison.OrdinalIgnoreCase))
                    .Where(k =>
                    {
                        var fileName = Path.GetFileName(k);
                        return !fileName.StartsWith("_UNSAVED_", StringComparison.OrdinalIgnoreCase)
                            && !fileName.StartsWith("__TEMP_", StringComparison.OrdinalIgnoreCase);
                    })
                    .ToList();

                _cacheLock.EnterWriteLock();
                try
                {
                    int successCount = 0;
                    int failCount = 0;

                    foreach (var filePath in filterFiles)
                    {
                        try
                        {
                            var filterId = Path.GetFileNameWithoutExtension(filePath);
                            var cachedFilter = await LoadFilterFromDiskAsync(filePath, filterId)
                                .ConfigureAwait(false);

                            if (cachedFilter != null)
                            {
                                _cache[filterId] = cachedFilter;
                                successCount++;
                            }
                            else
                            {
                                failCount++;
                            }
                        }
                        catch (Exception ex)
                        {
                            DebugLogger.LogError(
                                "FilterCacheService",
                                $"Failed to cache filter {Path.GetFileName(filePath)}: {ex.Message}"
                            );
                            failCount++;
                        }
                    }

                    DebugLogger.Log(
                        "FilterCacheService",
                        $"Cache initialized (async): {successCount} filters loaded, {failCount} failed"
                    );
                }
                finally
                {
                    _cacheLock.ExitWriteLock();
                }
            }
            catch (Exception ex)
            {
                DebugLogger.LogError(
                    "FilterCacheService",
                    $"Error in async initialization: {ex.Message}"
                );
            }
        }

        /// <summary>
        /// Async version for browser platform
        /// </summary>
        private async Task<CachedFilter?> LoadFilterFromDiskAsync(string filePath, string filterId)
        {
            try
            {
                MotelyJsonConfig? config = null;

                var content = await _store
                    .ReadTextAsync(filePath.Replace('\\', '/'))
                    .ConfigureAwait(false);
                if (string.IsNullOrWhiteSpace(content))
                    return null;

                var extension = Path.GetExtension(filePath).ToLowerInvariant();
                
                if (extension == ".jaml")
                {
                    // Load JAML content
                    if (Motely.JamlConfigLoader.TryLoadFromJamlString(content, out var jamlConfig, out var jamlError))
                    {
                        config = jamlConfig;
                    }
                    else
                    {
                        DebugLogger.LogError(
                            "FilterCacheService",
                            $"Failed to parse JAML filter {filterId}: {jamlError}"
                        );
                        return null;
                    }
                }
                else
                {
                    var options = new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true,
                        ReadCommentHandling = JsonCommentHandling.Skip,
                        AllowTrailingCommas = true,
                    };

                    config = JsonSerializer.Deserialize<MotelyJsonConfig>(content, options);
                }

                if (config == null)
                    return null;

                return new CachedFilter
                {
                    FilterId = filterId,
                    Config = config,
                    FilePath = filePath,
                    LastModified = DateTime.UtcNow, // Browser doesn't have file timestamps
                    FileSizeBytes = content.Length,
                };
            }
            catch (Exception ex)
            {
                DebugLogger.LogError(
                    "FilterCacheService",
                    $"Failed to load filter {filterId} from {filePath}: {ex.Message}"
                );
                return null;
            }
        }

        public MotelyJsonConfig? GetFilter(string filterId)
        {
            if (string.IsNullOrWhiteSpace(filterId))
                return null;

            _cacheLock.EnterReadLock();
            try
            {
                if (_cache.TryGetValue(filterId, out var cachedFilter))
                {
                    return cachedFilter.Config;
                }

                return null;
            }
            finally
            {
                _cacheLock.ExitReadLock();
            }
        }

        public MotelyJsonConfig? GetFilterByPath(string filePath)
        {
            if (string.IsNullOrWhiteSpace(filePath))
                return null;

            var isBrowser = _platformServices != null && !_platformServices.SupportsFileSystem;
            var filterId = Path.GetFileNameWithoutExtension(filePath);

            if (isBrowser)
            {
                // In browser, filePath is a logical key (e.g. "Filters/MyFilter.json")
                return GetFilter(filterId);
            }
            else
            {
                if (!File.Exists(filePath))
                    return null;

                return GetFilter(filterId);
            }
        }

        public IReadOnlyList<CachedFilter> GetAllFilters()
        {
            _cacheLock.EnterReadLock();
            try
            {
                // Return a copy sorted by last modified time (most recent first)
                return _cache.Values.OrderByDescending(f => f.LastModified).ToList();
            }
            finally
            {
                _cacheLock.ExitReadLock();
            }
        }

        public void InvalidateFilter(string filterId)
        {
            if (string.IsNullOrWhiteSpace(filterId))
                return;

            var isBrowser = _platformServices != null && !_platformServices.SupportsFileSystem;
            if (isBrowser)
            {
                _ = InvalidateFilterAsync(filterId);
                return;
            }

            _cacheLock.EnterWriteLock();
            try
            {
                var filtersDir = Helpers.AppPaths.FiltersDir;
                var filePath = Path.Combine(filtersDir, $"{filterId}.json");
                if (!File.Exists(filePath))
                {
                    _cache.TryRemove(filterId, out _);
                    DebugLogger.Log("FilterCacheService", $"Removed deleted filter from cache: {filterId}");
                    return;
                }

                var cachedFilter = LoadFilterFromDisk(filePath, filterId);
                if (cachedFilter != null)
                {
                    _cache[filterId] = cachedFilter;
                    DebugLogger.Log("FilterCacheService", $"Invalidated and reloaded filter: {filterId}");
                }
                else
                {
                    _cache.TryRemove(filterId, out _);
                    DebugLogger.LogError("FilterCacheService", $"Failed to reload filter: {filterId}");
                }
            }
            finally
            {
                _cacheLock.ExitWriteLock();
            }
        }

        public async Task InvalidateFilterAsync(string filterId)
        {
            if (string.IsNullOrWhiteSpace(filterId))
                return;

            var isBrowser = _platformServices != null && !_platformServices.SupportsFileSystem;
            if (!isBrowser)
            {
                InvalidateFilter(filterId);
                return;
            }

            var filePath = $"Filters/{filterId}.json";
            var exists = await _store.ExistsAsync(filePath).ConfigureAwait(false);
            if (!exists)
            {
                _cacheLock.EnterWriteLock();
                try { _cache.TryRemove(filterId, out _); }
                finally { _cacheLock.ExitWriteLock(); }
                DebugLogger.Log("FilterCacheService", $"Removed deleted filter from cache: {filterId}");
                return;
            }

            var cachedFilter = await LoadFilterFromDiskAsync(filePath, filterId).ConfigureAwait(false);
            _cacheLock.EnterWriteLock();
            try
            {
                if (cachedFilter != null)
                {
                    _cache[filterId] = cachedFilter;
                    DebugLogger.Log("FilterCacheService", $"Invalidated and reloaded filter: {filterId}");
                }
                else
                {
                    _cache.TryRemove(filterId, out _);
                    DebugLogger.LogError("FilterCacheService", $"Failed to reload filter: {filterId}");
                }
            }
            finally
            {
                _cacheLock.ExitWriteLock();
            }
        }

        public void RemoveFilter(string filterId)
        {
            if (string.IsNullOrWhiteSpace(filterId))
                return;

            _cacheLock.EnterWriteLock();
            try
            {
                if (_cache.TryRemove(filterId, out _))
                {
                    DebugLogger.Log("FilterCacheService", $"Removed filter from cache: {filterId}");
                }
            }
            finally
            {
                _cacheLock.ExitWriteLock();
            }
        }

        public void RefreshCache()
        {
            _cacheLock.EnterWriteLock();
            try
            {
                DebugLogger.Log("FilterCacheService", "Refreshing entire cache...");
                _cache.Clear();
                _isInitialized = false;
            }
            finally
            {
                _cacheLock.ExitWriteLock();
            }

            Initialize();
        }

        /// <summary>
        /// Loads a filter from disk with optimized parsing.
        /// Uses Motely's native loader for correctness, with fallback to standard JSON parsing.
        /// </summary>
        private CachedFilter? LoadFilterFromDisk(string filePath, string filterId)
        {
            try
            {
                MotelyJsonConfig? config = null;
                DateTime lastModifiedUtc;
                long sizeBytes;
                var isBrowser = _platformServices != null && !_platformServices.SupportsFileSystem;

                if (isBrowser)
                {
                    // Sync LoadFilterFromDisk is not used on browser (InvalidateFilter uses InvalidateFilterAsync).
                    return null;
                }
                else
                {
                    var fileInfo = new FileInfo(filePath);
                    if (!fileInfo.Exists)
                        return null;

                    var extension = Path.GetExtension(filePath).ToLowerInvariant();
                    
                    if (extension == ".jaml")
                    {
                        // Load JAML file using JamlConfigLoader
                        if (Motely.JamlConfigLoader.TryLoadFromJaml(filePath, out var jamlConfig, out var jamlError))
                        {
                            config = jamlConfig;
                        }
                        else
                        {
                            DebugLogger.LogError(
                                "FilterCacheService",
                                $"Failed to parse JAML filter {filterId}: {jamlError}"
                            );
                            return null;
                        }
                    }
                    else
                    {
                        // Try Motely's loader first (handles edge cases better)
                        if (MotelyJsonConfig.TryLoadFromJsonFile(filePath, out var motelyConfig))
                        {
                            config = motelyConfig;
                        }
                        else
                        {
                            // Fallback to manual parsing with hardened options
                            var json = File.ReadAllText(filePath);
                            var options = new JsonSerializerOptions
                            {
                                PropertyNameCaseInsensitive = true,
                                ReadCommentHandling = JsonCommentHandling.Skip,
                                AllowTrailingCommas = true,
                            };

                            config = JsonSerializer.Deserialize<MotelyJsonConfig>(json, options);
                        }
                    }

                    lastModifiedUtc = fileInfo.LastWriteTimeUtc;
                    sizeBytes = fileInfo.Length;
                }

                if (config == null || string.IsNullOrWhiteSpace(config.Name))
                {
                    DebugLogger.LogError(
                        "FilterCacheService",
                        $"Invalid filter config (null or empty name): {filterId}"
                    );
                    return null;
                }

                return new CachedFilter
                {
                    FilterId = filterId,
                    FilePath = filePath,
                    Config = config,
                    LastModified = lastModifiedUtc,
                    FileSizeBytes = sizeBytes,
                };
            }
            catch (Exception ex)
            {
                DebugLogger.LogError(
                    "FilterCacheService",
                    $"Error loading filter {filterId}: {ex.Message}"
                );
                return null;
            }
        }

        public void Dispose()
        {
            if (_disposed)
                return;

            _cacheLock?.EnterWriteLock();
            try
            {
                _cache?.Clear();
                DebugLogger.Log("FilterCacheService", "Cache disposed");
                _disposed = true;
            }
            finally
            {
                _cacheLock?.ExitWriteLock();
                _cacheLock?.Dispose();
            }
        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Nodes;
using BalatroSeedOracle.Helpers;
using BalatroSeedOracle.Models;

namespace BalatroSeedOracle.Services;

/// <summary>
/// Favorite shelf items (star toggles in the visual builder), persisted to
/// %APPDATA%\BalatroSeedOracle\favorites.json in the <see cref="FavoritesData"/> layout
/// (<c>{ "FavoriteItems": [...], "CommonSets": [...] }</c>, PascalCase).
/// Only the FavoriteItems array is rewritten on save; CommonSets and any other keys in the
/// file are carried over untouched. Writes are atomic (temp file + move), and a file that
/// cannot be parsed is backed up before it is ever replaced.
/// </summary>
public class FavoritesService
{
    private const string FavoriteItemsKey = nameof(FavoritesData.FavoriteItems);
    private const string CommonSetsKey = nameof(FavoritesData.CommonSets);

    private static readonly Lazy<FavoritesService> LazyInstance = new(
        () => new FavoritesService(AppDataPaths.FavoritesFile)
    );

    /// <summary>The app-wide favorites store backed by <see cref="AppDataPaths.FavoritesFile"/>.</summary>
    public static FavoritesService Instance => LazyInstance.Value;

    private readonly object _sync = new();
    private readonly string _filePath;

    // Insertion-ordered list (what is written) plus a case-insensitive set (what is queried).
    private readonly List<string> _ordered = new();
    private readonly HashSet<string> _favorites = new(StringComparer.OrdinalIgnoreCase);

    private JsonObject? _document;
    private bool _loaded;
    private bool _fileWasUnreadable;

    public FavoritesService(string filePath)
    {
        _filePath = filePath ?? throw new ArgumentNullException(nameof(filePath));
    }

    /// <summary>The file this store reads and writes.</summary>
    public string FilePath => _filePath;

    public IReadOnlyCollection<string> FavoriteItems
    {
        get
        {
            lock (_sync)
            {
                EnsureLoaded();
                return _ordered.ToArray();
            }
        }
    }

    public bool IsFavorite(string itemKey)
    {
        if (string.IsNullOrEmpty(itemKey))
            return false;
        lock (_sync)
        {
            EnsureLoaded();
            return _favorites.Contains(itemKey);
        }
    }

    public void Add(string itemKey)
    {
        if (string.IsNullOrWhiteSpace(itemKey))
            return;
        lock (_sync)
        {
            EnsureLoaded();
            if (!_favorites.Add(itemKey))
                return;
            _ordered.Add(itemKey);
            Save();
        }
    }

    public void Remove(string itemKey)
    {
        if (string.IsNullOrEmpty(itemKey))
            return;
        lock (_sync)
        {
            EnsureLoaded();
            if (!_favorites.Remove(itemKey))
                return;
            _ordered.RemoveAll(k => string.Equals(k, itemKey, StringComparison.OrdinalIgnoreCase));
            Save();
        }
    }

    /// <summary>Drop the in-memory state and read the file again on next access.</summary>
    public void Reload()
    {
        lock (_sync)
        {
            _loaded = false;
            EnsureLoaded();
        }
    }

    private void EnsureLoaded()
    {
        if (_loaded)
            return;
        _loaded = true;
        _ordered.Clear();
        _favorites.Clear();
        _document = null;
        _fileWasUnreadable = false;

        try
        {
            _document = JsonFileStore.ReadObject(_filePath);
            if (_document is null)
                return;

            var items = _document
                .FirstOrDefault(p => string.Equals(p.Key, FavoriteItemsKey, StringComparison.OrdinalIgnoreCase))
                .Value as JsonArray;
            if (items is null)
                return;

            foreach (var node in items)
            {
                if (node is JsonValue value && value.TryGetValue<string>(out var key) && !string.IsNullOrEmpty(key))
                {
                    if (_favorites.Add(key))
                        _ordered.Add(key);
                }
            }
            DebugLogger.Log(nameof(FavoritesService), $"Loaded {_ordered.Count} favorites from {_filePath}");
        }
        catch (Exception ex)
        {
            _document = null;
            _fileWasUnreadable = true;
            DebugLogger.LogError(nameof(FavoritesService), $"Cannot read {_filePath}: {ex.Message}");
        }
    }

    private void Save()
    {
        try
        {
            if (_fileWasUnreadable)
            {
                var backup = JsonFileStore.PreserveUnreadableFile(_filePath);
                if (backup is null && System.IO.File.Exists(_filePath))
                {
                    DebugLogger.LogError(
                        nameof(FavoritesService),
                        $"Not saving: {_filePath} is unreadable and could not be backed up"
                    );
                    return;
                }
                DebugLogger.LogWarning(
                    nameof(FavoritesService),
                    $"Replacing unreadable {_filePath}; previous contents kept at {backup}"
                );
                _fileWasUnreadable = false;
            }

            var updates = new JsonObject { [FavoriteItemsKey] = new JsonArray(_ordered.Select(k => (JsonNode?)k).ToArray()) };
            if (_document is null)
                updates[CommonSetsKey] = new JsonArray();

            var merged = JsonFileStore.Merge(_document, updates);
            JsonFileStore.WriteAllTextAtomic(_filePath, JsonFileStore.ToIndentedJson(merged));
            _document = merged;
        }
        catch (Exception ex)
        {
            DebugLogger.LogError(nameof(FavoritesService), $"Cannot save {_filePath}: {ex.Message}");
        }
    }
}

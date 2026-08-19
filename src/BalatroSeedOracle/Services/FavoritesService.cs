using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using BalatroSeedOracle.Json;
using BalatroSeedOracle.Models;

namespace BalatroSeedOracle.Services;

/// <summary>
/// favorites.json in the run directory. Persists the set of favorited item keys
/// (SelectableItem.ItemKey) so favorites survive restarts. Load on construction,
/// save on change. Same convention as UserProfileService.
/// </summary>
public class FavoritesService
{
    private const string FavoritesPath = "favorites.json";

    private static FavoritesService? _instance;
    public static FavoritesService Instance => _instance ??= new FavoritesService();

    private readonly FavoritesData _data;
    private readonly HashSet<string> _keys;

    public FavoritesService()
    {
        _data = File.Exists(FavoritesPath)
            ? JsonSerializer.Deserialize(
                File.ReadAllText(FavoritesPath),
                BsoJsonSerializerContext.Default.FavoritesData
            ) ?? new FavoritesData()
            : new FavoritesData();

        _keys = new HashSet<string>(_data.FavoriteItems);
    }

    /// <summary>True if the given item key is currently favorited.</summary>
    public bool IsFavorite(string? itemKey) =>
        !string.IsNullOrEmpty(itemKey) && _keys.Contains(itemKey!);

    /// <summary>All favorited item keys (read-only snapshot).</summary>
    public IReadOnlyCollection<string> FavoriteKeys => _keys;

    public void Add(string? itemKey)
    {
        if (string.IsNullOrEmpty(itemKey) || !_keys.Add(itemKey!))
            return;
        Save();
    }

    public void Remove(string? itemKey)
    {
        if (string.IsNullOrEmpty(itemKey) || !_keys.Remove(itemKey!))
            return;
        Save();
    }

    private void Save()
    {
        _data.FavoriteItems = new List<string>(_keys);
        File.WriteAllText(
            FavoritesPath,
            JsonSerializer.Serialize(_data, BsoJsonSerializerContext.Default.FavoritesData)
        );
    }
}

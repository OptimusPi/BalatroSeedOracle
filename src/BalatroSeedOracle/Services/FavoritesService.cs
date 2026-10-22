using System.Collections.Generic;
using BalatroSeedOracle.Models;

namespace BalatroSeedOracle.Services;

/// <summary>
/// Favorite shelf items (star toggles in the visual builder), persisted to favorites.json.
/// TIER0-STUB: replace in Tier 1. In-memory set; persistence can be re-enabled with the
/// favorites.json layout defined by <see cref="FavoritesData"/>.
/// </summary>
public class FavoritesService
{
    public static FavoritesService Instance { get; } = new();

    private readonly HashSet<string> _favorites = new(System.StringComparer.OrdinalIgnoreCase);

    public IReadOnlyCollection<string> FavoriteItems => _favorites;

    public bool IsFavorite(string itemKey) => _favorites.Contains(itemKey);

    public void Add(string itemKey) => _favorites.Add(itemKey);

    public void Remove(string itemKey) => _favorites.Remove(itemKey);
}

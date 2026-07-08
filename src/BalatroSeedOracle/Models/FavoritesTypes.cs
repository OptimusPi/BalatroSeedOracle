using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace BalatroSeedOracle.Models;

/// <summary>
/// Favorites data container for the FavoritesService
/// </summary>
public class FavoritesData
{
    public List<string> FavoriteItems { get; set; } = new List<string>();
    public List<JokerSet> CommonSets { get; set; } = new List<JokerSet>();
}

/// <summary>
/// A named set of jokers/items with tags and zone assignments
/// </summary>
public class JokerSet
{
    public string Name { get; set; } = "";
    public string Description { get; set; } = "";
    public List<string> Items { get; set; } = new List<string>();
    public List<string> Tags { get; set; } = new List<string>();
    public List<string> MustItems { get; set; } = new List<string>();
    public List<string> ShouldItems { get; set; } = new List<string>();
    public List<string> MustNotItems { get; set; } = new List<string>();

    [JsonIgnore]
    public bool HasZoneInfo =>
        MustItems.Count > 0 || ShouldItems.Count > 0 || MustNotItems.Count > 0;
}

/// <summary>
/// Credit entry for the credits modal.
/// </summary>
public class Credit
{
    public string? Name { get; set; }
    public string? Role { get; set; }
    public string? Note { get; set; }
    public string? Link { get; set; }

    [JsonIgnore]
    public bool HasLink => !string.IsNullOrWhiteSpace(Link);
}

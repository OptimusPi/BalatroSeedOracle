using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Oracle.Models;

/// <summary>
/// MongoDB-style compound query configuration for Balatro seed searching
/// </summary>
public class OuijaConfig
{
    private static readonly JsonSerializerOptions s_jsonOptionsIndented = new()
    {
        WriteIndented = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    };

    [JsonPropertyName("name")]
    public string name { get; set; } = "My Filter Config";

    [JsonPropertyName("description")]
    public string description { get; set; } = "";

    [JsonPropertyName("author")]
    public string author { get; set; } = "BalatroSeedOracle User";

    [JsonPropertyName("filter_config")]
    public FilterConfig filter_config { get; set; } = new();

    [JsonPropertyName("labels")]
    public List<string>? labels { get; set; }

    public string ToJson()
    {
        return JsonSerializer.Serialize(this, s_jsonOptionsIndented);
    }
}

public class FilterConfig
{
    [JsonPropertyName("must")]
    public List<FilterItem> Must { get; set; } = new();

    [JsonPropertyName("should")]
    public List<FilterItem> Should { get; set; } = new();

    [JsonPropertyName("mustNot")]
    public List<FilterItem> MustNot { get; set; } = new();

    [JsonPropertyName("minimumScore")]
    public int MinimumScore { get; set; } = 0;
}

public class FilterItem
{
    [JsonPropertyName("Type")]
    public string Type { get; set; } = "";

    [JsonPropertyName("Value")]
    public string Value { get; set; } = "";

    [JsonPropertyName("Antes")]
    public List<int> Antes { get; set; } = new() { 1 };

    [JsonPropertyName("Score")]
    public int Score { get; set; } = 1;

    [JsonPropertyName("Edition")]
    public string? Edition { get; set; }

    [JsonPropertyName("Stickers")]
    public List<string>? Stickers { get; set; }

    // PlayingCard specific properties
    [JsonPropertyName("Suit")]
    public string? Suit { get; set; }

    [JsonPropertyName("Rank")]
    public string? Rank { get; set; }

    [JsonPropertyName("Seal")]
    public string? Seal { get; set; }

    [JsonPropertyName("Enhancement")]
    public string? Enhancement { get; set; }

    // Source configuration
    [JsonPropertyName("IncludeShopStream")]
    public bool? IncludeShopStream { get; set; }

    [JsonPropertyName("IncludeBoosterPacks")]
    public bool? IncludeBoosterPacks { get; set; }

    [JsonPropertyName("IncludeSkipTags")]
    public bool? IncludeSkipTags { get; set; }

    // Label for display in results table headers
    [JsonPropertyName("Label")]
    public string? Label { get; set; }
}

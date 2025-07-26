using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Oracle.Models;

/// <summary>
/// Models for ouija.json configuration files
/// </summary>
public class OuijaConfig
{
    [JsonPropertyName("name")]
    public string name { get; set; } = "My Filter Config";
    
    [JsonPropertyName("description")]
    public string description { get; set; } = "";
    
    [JsonPropertyName("author")]
    public string author { get; set; } = "BalatroSeedOracle User";
    
    [JsonPropertyName("keywords")]
    public List<string> keywords { get; set; } = new();
    
    [JsonPropertyName("filter_config")]
    public FilterConfig filter_config { get; set; } = new();
    
    public string ToJson()
    {
        var options = new JsonSerializerOptions
        {
            WriteIndented = true,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
        };
        return JsonSerializer.Serialize(this, options);
    }
}

public class FilterConfig
{
    [JsonPropertyName("Needs")]
    public List<FilterItem> Needs { get; set; } = new();
    
    [JsonPropertyName("Wants")]
    public List<FilterItem> Wants { get; set; } = new();
    
    [JsonPropertyName("Deck")]
    public string? Deck { get; set; }
    
    [JsonPropertyName("Stake")]
    public string? Stake { get; set; }
    
    [JsonPropertyName("ScoreNaturalNegatives")]
    public bool ScoreNaturalNegatives { get; set; } = false;
    
    [JsonPropertyName("ScoreDesiredNegatives")]
    public bool ScoreDesiredNegatives { get; set; } = false;
}

public class FilterItem
{
    [JsonPropertyName("Type")]
    public string Type { get; set; } = "";
    
    [JsonPropertyName("Value")]
    public string Value { get; set; } = "";
    
    [JsonPropertyName("SearchAntes")]
    public List<int> SearchAntes { get; set; } = new() { 1, 2, 3, 4, 5, 6, 7, 8 };
    
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
}

// Legacy simple format for backward compatibility
public class OuijaDesire
{
    [JsonPropertyName("itemType")]
    public string itemType { get; set; } = "";
    
    [JsonPropertyName("itemName")]
    public string itemName { get; set; } = "";
    
    [JsonPropertyName("required")]
    public bool required { get; set; } = true;
    
    [JsonPropertyName("editions")]
    public List<string>? editions { get; set; }
    
    [JsonPropertyName("anteMin")]
    public int? anteMin { get; set; }
    
    [JsonPropertyName("anteMax")]
    public int? anteMax { get; set; }
}
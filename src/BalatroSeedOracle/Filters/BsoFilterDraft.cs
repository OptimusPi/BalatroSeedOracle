using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using YamlDotNet.Serialization;

namespace Motely.Filters;

public class SourcesConfig
{
    [JsonPropertyName("shopSlots")]
    [YamlMember(Alias = "shopSlots")]
    public int[]? ShopSlots { get; set; }

    [JsonPropertyName("packSlots")]
    [YamlMember(Alias = "packSlots")]
    public int[]? PackSlots { get; set; }

    [JsonPropertyName("tags")]
    [YamlMember(Alias = "tags")]
    public bool? Tags { get; set; }

    [JsonPropertyName("requireMega")]
    [YamlMember(Alias = "requireMega")]
    public bool? RequireMega { get; set; }

    [JsonPropertyName("minShopSlot")]
    public int? MinShopSlot { get; set; }

    [JsonPropertyName("maxShopSlot")]
    public int? MaxShopSlot { get; set; }

    [JsonPropertyName("minPackSlot")]
    public int? MinPackSlot { get; set; }

    [JsonPropertyName("maxPackSlot")]
    public int? MaxPackSlot { get; set; }
}

public class MotelyFilterDefaults
{
    [JsonPropertyName("antes")]
    public int[]? Antes { get; set; }

    [JsonPropertyName("packSlots")]
    [YamlMember(Alias = "packSlots")]
    public int[]? PackSlots { get; set; }

    [JsonPropertyName("shopSlots")]
    [YamlMember(Alias = "shopSlots")]
    public int[]? ShopSlots { get; set; }

    [JsonPropertyName("score")]
    public int? Score { get; set; }
}

public enum MotelyJsonConfigWildcards
{
    AnyJoker,
    AnyCommon,
    AnyUncommon,
    AnyRare,
    AnyLegendary,
    AnyTarot,
    AnySpectral,
    AnyPlanet,
}

public enum MotelyScoreAggregationMode
{
    Sum,
    MaxCount,
}

public class MotelyJsonConfig
{
    [JsonPropertyName("name")]
    [YamlMember(Alias = "name")]
    public string? Name { get; set; }

    [JsonPropertyName("author")]
    [YamlMember(Alias = "author")]
    public string? Author { get; set; }

    [JsonPropertyName("description")]
    [YamlMember(Alias = "description")]
    public string? Description { get; set; }

    [JsonPropertyName("dateCreated")]
    public DateTime? DateCreated { get; set; }

    [JsonPropertyName("verifiedSeed")]
    [YamlMember(Alias = "verifiedSeed")]
    public string? VerifiedSeed { get; set; }

    [JsonPropertyName("deck")]
    [YamlMember(Alias = "deck")]
    public string? Deck { get; set; } = "Red";

    [JsonPropertyName("stake")]
    [YamlMember(Alias = "stake")]
    public string? Stake { get; set; } = "White";

    [JsonPropertyName("mode")]
    [YamlMember(Alias = "mode")]
    public string? Mode { get; set; }

    [JsonPropertyName("startSeed")]
    [YamlMember(Alias = "startSeed")]
    public string? StartSeed { get; set; }

    [JsonPropertyName("defaults")]
    public MotelyFilterDefaults? Defaults { get; set; }

    [JsonPropertyName("must")]
    [YamlMember(Alias = "must")]
    public List<MotelyJsonFilterClause> Must { get; set; } = new();

    [JsonPropertyName("should")]
    [YamlMember(Alias = "should")]
    public List<MotelyJsonFilterClause> Should { get; set; } = new();

    [JsonPropertyName("mustNot")]
    [YamlMember(Alias = "mustNot")]
    public List<MotelyJsonFilterClause> MustNot { get; set; } = new();

    public class MotelyJsonFilterClause
    {
        [JsonPropertyName("type")]
        [YamlMember(Alias = "type")]
        public string Type { get; set; } = string.Empty;

        [JsonPropertyName("value")]
        [YamlMember(Alias = "value")]
        public string? Value { get; set; }

        [JsonPropertyName("values")]
        [YamlMember(Alias = "values")]
        public string[]? Values { get; set; }

        [JsonPropertyName("label")]
        [YamlMember(Alias = "label")]
        public string? Label { get; set; }

        [JsonPropertyName("antes")]
        [YamlMember(Alias = "antes")]
        public int[]? Antes { get; set; }

        [JsonPropertyName("clauses")]
        [YamlMember(Alias = "clauses")]
        public List<MotelyJsonFilterClause>? Clauses { get; set; }

        [JsonIgnore]
        [YamlIgnore]
        public bool IsInverted { get; set; } = false;

        [JsonIgnore]
        [YamlIgnore]
        public bool AntesWasExplicitlySet { get; set; } = false;

        [JsonPropertyName("score")]
        [YamlMember(Alias = "score")]
        public int Score { get; set; } = 1;

        [JsonPropertyName("mode")]
        [YamlMember(Alias = "mode")]
        public string? Mode { get; set; }

        [JsonPropertyName("function")]
        [YamlMember(Alias = "function")]
        public string? Function { get; set; }

        [JsonPropertyName("cards")]
        [YamlMember(Alias = "cards")]
        public int[]? Cards { get; set; }

        [JsonPropertyName("min")]
        [YamlMember(Alias = "min")]
        public int? Min { get; set; }

        [JsonPropertyName("filterOrder")]
        [YamlMember(Alias = "filterOrder")]
        public int? FilterOrder { get; set; }

        [JsonPropertyName("edition")]
        [YamlMember(Alias = "edition")]
        public string? Edition { get; set; }

        [JsonPropertyName("stickers")]
        [YamlMember(Alias = "stickers")]
        public string[]? Stickers { get; set; }

        [JsonPropertyName("suit")]
        [YamlMember(Alias = "suit")]
        public string? Suit { get; set; }

        [JsonPropertyName("rank")]
        [YamlMember(Alias = "rank")]
        public string? Rank { get; set; }

        [JsonPropertyName("seal")]
        [YamlMember(Alias = "seal")]
        public string? Seal { get; set; }

        [JsonPropertyName("enhancement")]
        [YamlMember(Alias = "enhancement")]
        public string? Enhancement { get; set; }

        [JsonPropertyName("sources")]
        [YamlMember(Alias = "sources")]
        public SourcesConfig? Sources { get; set; }

        [JsonPropertyName("packSlots")]
        [YamlMember(Alias = "packSlots")]
        public int[]? PackSlots { get; set; }

        [JsonPropertyName("shopSlots")]
        [YamlMember(Alias = "shopSlots")]
        public int[]? ShopSlots { get; set; }

        [JsonPropertyName("requireMega")]
        [YamlMember(Alias = "requireMega")]
        public bool? RequireMega { get; set; }

        [JsonPropertyName("tags")]
        [YamlMember(Alias = "tags")]
        public bool? Tags { get; set; }

        [JsonPropertyName("eventType")]
        [YamlMember(Alias = "eventType")]
        public string? EventType { get; set; }

        [JsonPropertyName("rolls")]
        [YamlMember(Alias = "rolls")]
        public int[]? Rolls { get; set; }
    }

    public static bool TryLoadFromJsonFile(
        string jsonPath,
        [NotNullWhen(true)] out MotelyJsonConfig? config
    ) => TryLoadFromJsonFile(jsonPath, out config, out _);

    public static bool TryLoadFromJsonFile(
        string jsonPath,
        [NotNullWhen(true)] out MotelyJsonConfig? config,
        out string? error
    )
    {
        config = null;
        error = null;
        if (!File.Exists(jsonPath))
        {
            error = $"File not found: {jsonPath}";
            return false;
        }
        try
        {
            var json = File.ReadAllText(jsonPath);
            var options = new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                PropertyNameCaseInsensitive = true,
                ReadCommentHandling = JsonCommentHandling.Skip,
                AllowTrailingCommas = true,
            };
            var deserialized = JsonSerializer.Deserialize<MotelyJsonConfig>(json, options);
            if (deserialized == null)
            {
                error = "Failed to deserialize JSON - result was null";
                return false;
            }
            config = deserialized;
            return true;
        }
        catch (JsonException jex)
        {
            error = $"JSON syntax error at line {jex.LineNumber}, position {jex.BytePositionInLine}: {jex.Message}";
            return false;
        }
        catch (Exception ex)
        {
            error = ex.Message;
            return false;
        }
    }
}

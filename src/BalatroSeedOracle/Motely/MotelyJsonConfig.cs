using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using YamlDotNet.Serialization;

namespace Motely.Filters;

// Lifted from upstream Motely (deleted at commit de506102 in MotelyJAML) and slimmed down
// to the DTO surface BalatroSeedOracle actually uses. The internal partitioning,
// PostProcess pipeline, MotelyEnumParser, and per-clause *Enum properties are gone —
// they belonged to the old in-process Motely search runtime, which is now driven from
// JamlConfig in MotelyJAML. BSO keeps these as a serialization-friendly UI/config DTO
// and converts to JamlConfig at the search boundary.

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
    [YamlMember(Alias = "minShopSlot")]
    public int? MinShopSlot { get; set; }

    [JsonPropertyName("maxShopSlot")]
    [YamlMember(Alias = "maxShopSlot")]
    public int? MaxShopSlot { get; set; }

    [JsonPropertyName("minPackSlot")]
    [YamlMember(Alias = "minPackSlot")]
    public int? MinPackSlot { get; set; }

    [JsonPropertyName("maxPackSlot")]
    [YamlMember(Alias = "maxPackSlot")]
    public int? MaxPackSlot { get; set; }
}

public class MotelyFilterDefaults
{
    [JsonPropertyName("antes")]
    [YamlMember(Alias = "antes")]
    public int[]? Antes { get; set; }

    [JsonPropertyName("packSlots")]
    [YamlMember(Alias = "packSlots")]
    public int[]? PackSlots { get; set; }

    [JsonPropertyName("shopSlots")]
    [YamlMember(Alias = "shopSlots")]
    public int[]? ShopSlots { get; set; }

    [JsonPropertyName("score")]
    [YamlMember(Alias = "score")]
    public int? Score { get; set; }

    [JsonIgnore]
    [YamlIgnore]
    public static readonly int[] DEFAULT_ANTES = [1, 2, 3, 4, 5, 6, 7, 8];

    public int[] GetEffectiveAntes() => Antes ?? DEFAULT_ANTES;
}

public enum MotelyScoreAggregationMode
{
    Sum,
    Max,
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
    [YamlMember(Alias = "defaults")]
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
        public bool IsInverted { get; set; }

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
        [NotNullWhen(true)] out MotelyJsonConfig? config) =>
        TryLoadFromJsonFile(jsonPath, out config, out _);

    public static bool TryLoadFromJsonFile(
        string jsonPath,
        [NotNullWhen(true)] out MotelyJsonConfig? config,
        out string? error)
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
            config = JsonSerializer.Deserialize(
                json,
                MotelyJsonSerializerContext.Default.MotelyJsonConfig);

            if (config is null)
            {
                error = "Deserialized config was null.";
                return false;
            }
            return true;
        }
        catch (Exception ex)
        {
            error = ex.Message;
            return false;
        }
    }
}

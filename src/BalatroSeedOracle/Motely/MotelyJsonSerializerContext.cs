using System.Text.Json;
using System.Text.Json.Serialization;
using Motely.Filters;

namespace Motely.Filters;

// AOT source-gen context for MotelyJsonConfig. Lives in BSO now that the type was
// removed from upstream Motely; keeps the same accessor name BSO already uses
// (MotelyJsonSerializerContext.Default.MotelyJsonConfig).
[JsonSourceGenerationOptions(
    WriteIndented = true,
    PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase,
    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    PropertyNameCaseInsensitive = true,
    ReadCommentHandling = JsonCommentHandling.Skip,
    AllowTrailingCommas = true)]
[JsonSerializable(typeof(MotelyJsonConfig))]
[JsonSerializable(typeof(MotelyJsonConfig.MotelyJsonFilterClause))]
[JsonSerializable(typeof(SourcesConfig))]
[JsonSerializable(typeof(MotelyFilterDefaults))]
public partial class MotelyJsonSerializerContext : JsonSerializerContext { }

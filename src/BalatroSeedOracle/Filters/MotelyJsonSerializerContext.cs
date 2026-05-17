using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Motely.Filters;

[JsonSourceGenerationOptions(
    WriteIndented = true,
    PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase,
    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    PropertyNameCaseInsensitive = true,
    ReadCommentHandling = JsonCommentHandling.Skip,
    AllowTrailingCommas = true
)]
[JsonSerializable(typeof(JamlRootDocument))]
[JsonSerializable(typeof(JamlClauseUnion))]
[JsonSerializable(typeof(List<JamlClauseUnion>))]
[JsonSerializable(typeof(JamlSources))]
[JsonSerializable(typeof(JamlDefaults))]
[JsonSerializable(typeof(MotelyJsonConfig))]
[JsonSerializable(typeof(MotelyJsonConfig.MotelyJsonFilterClause))]
[JsonSerializable(typeof(SourcesConfig))]
[JsonSerializable(typeof(MotelyFilterDefaults))]
public partial class MotelyJsonSerializerContext : JsonSerializerContext
{
}

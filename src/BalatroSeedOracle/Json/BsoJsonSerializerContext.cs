using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;
using BalatroSeedOracle.Models;
using BalatroSeedOracle.Services;

namespace BalatroSeedOracle.Json;

/// <summary>
/// AOT-compatible JSON serialization context for BalatroSeedOracle types.
/// This enables Native AOT compilation by pre-generating serialization code at compile time.
/// 
/// USAGE:
/// - For deserialization: JsonSerializer.Deserialize(json, BsoJsonSerializerContext.Default.UserProfile)
/// - For serialization: JsonSerializer.Serialize(obj, BsoJsonSerializerContext.Default.UserProfile)
/// 
/// For MotelyJsonConfig, use Motely's MotelyJsonSerializerContext instead:
/// - JsonSerializer.Deserialize(json, MotelyJsonSerializerContext.Default.MotelyJsonConfig)
/// </summary>
[JsonSourceGenerationOptions(
    WriteIndented = true,
    PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase,
    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    PropertyNameCaseInsensitive = true,
    ReadCommentHandling = JsonCommentHandling.Skip,
    AllowTrailingCommas = true
)]
// User Profile and related types
[JsonSerializable(typeof(UserProfile))]
[JsonSerializable(typeof(SavedSearchWidget))]
[JsonSerializable(typeof(SearchResumeState))]
[JsonSerializable(typeof(VisualizerSettings))]
[JsonSerializable(typeof(AdvancedMusicSettings))]
[JsonSerializable(typeof(FeatureToggles))]
[JsonSerializable(typeof(HostApiSettings))]

// Visualizer presets and audio
[JsonSerializable(typeof(VisualizerPreset))]
[JsonSerializable(typeof(AudioTriggerPoint))]
[JsonSerializable(typeof(FrequencyBreakpoint))]
[JsonSerializable(typeof(MelodicBreakpoint))]
[JsonSerializable(typeof(ParameterRange))]
[JsonSerializable(typeof(EffectMapping))]

// Music mixer
[JsonSerializable(typeof(MixerSettings))]
[JsonSerializable(typeof(TrackSettings))]
[JsonSerializable(typeof(MusicMixPreset))]
[JsonSerializable(typeof(TrackMixSettings))]
[JsonSerializable(typeof(TrackMetadata))]

// Transition presets
[JsonSerializable(typeof(TransitionPreset))]

// Daylatro high scores
[JsonSerializable(typeof(DaylatroHighScore))]
[JsonSerializable(typeof(DaylatroDailyScores))]
[JsonSerializable(typeof(List<DaylatroDailyScores>))]
[JsonSerializable(typeof(Dictionary<string, System.DateTime>))]

// Sprite metadata (public types)
[JsonSerializable(typeof(SpritePosition))]
[JsonSerializable(typeof(Pos))]
[JsonSerializable(typeof(List<SpritePosition>))]

// Credits
[JsonSerializable(typeof(Credit))]
[JsonSerializable(typeof(Credit[]))]

// Favorites data
[JsonSerializable(typeof(FavoritesData))]
[JsonSerializable(typeof(JokerSet))]
[JsonSerializable(typeof(List<JokerSet>))]

// Shader presets
[JsonSerializable(typeof(ShaderParametersConfig))]

// Generic collections used across the app
[JsonSerializable(typeof(Dictionary<string, int>))]
[JsonSerializable(typeof(Dictionary<string, float>))]
[JsonSerializable(typeof(Dictionary<string, string>))]
[JsonSerializable(typeof(Dictionary<string, object>))]
[JsonSerializable(typeof(Dictionary<string, ParameterRange>))]
[JsonSerializable(typeof(Dictionary<string, EffectMapping>))]
[JsonSerializable(typeof(Dictionary<string, TrackMixSettings>))]
[JsonSerializable(typeof(List<FrequencyBreakpoint>))]
[JsonSerializable(typeof(List<MelodicBreakpoint>))]
[JsonSerializable(typeof(List<SavedSearchWidget>))]
[JsonSerializable(typeof(List<DaylatroHighScore>))]
[JsonSerializable(typeof(List<string>))]

// DataGrid export types
[JsonSerializable(typeof(DataGridResultItem))]
[JsonSerializable(typeof(List<DataGridResultItem>))]

// EventFX config
[JsonSerializable(typeof(EventFXConfig))]

// Genie responses
[JsonSerializable(typeof(CloudGenieResponse))]
[JsonSerializable(typeof(LocalGenieResponse))]
public partial class BsoJsonSerializerContext : JsonSerializerContext
{
}

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
    public bool HasZoneInfo => MustItems.Count > 0 || ShouldItems.Count > 0 || MustNotItems.Count > 0;
}

/// <summary>
/// Credit entry for the credits modal
/// </summary>
public class Credit
{
    public string? Name { get; set; }
    public string? Role { get; set; }
    public string? Link { get; set; }
}

/// <summary>
/// EventFX configuration for event-based visual effects
/// </summary>
public class EventFXConfig
{
    public string? PresetName { get; set; }
    public double Duration { get; set; } = 2.0;
    public string? AudioTriggerName { get; set; }
    public bool Enabled { get; set; } = true;
}

/// <summary>
/// Response from cloud Genie API
/// </summary>
public class CloudGenieResponse
{
    public string? Filter { get; set; }
    public string? Error { get; set; }
}

/// <summary>
/// Response from local Genie API
/// </summary>
public class LocalGenieResponse
{
    public string? Content { get; set; }
    public string? Error { get; set; }
}

/// <summary>
/// DataGrid result item for export
/// </summary>
public class DataGridResultItem
{
    public string? Seed { get; set; }
    public long Score { get; set; }
    public Dictionary<string, object>? Tallies { get; set; }
}

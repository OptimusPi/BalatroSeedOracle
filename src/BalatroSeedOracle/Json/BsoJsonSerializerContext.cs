using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;
using BalatroSeedOracle.Models;
using BalatroSeedOracle.Services;

namespace BalatroSeedOracle.Json;

/// <summary>
/// AOT-compatible JSON serialization context for BalatroSeedOracle app types.
/// This enables Native AOT compilation by pre-generating serialization code at compile time.
/// 
/// NOTE: This is for app-level JSON serialization only. For JAML, use Motely's
/// JamlConfigLoader directly - no app-side JAML serialization context.
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
// Search result export (DbListExportService)
[JsonSerializable(typeof(SearchResultExport))]
// EventFX config
[JsonSerializable(typeof(EventFXConfig))]
public partial class BsoJsonSerializerContext : JsonSerializerContext { }

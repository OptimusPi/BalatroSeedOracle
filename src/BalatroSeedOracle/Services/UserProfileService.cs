using System;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using BalatroSeedOracle.Json;
using BalatroSeedOracle.Models;

namespace BalatroSeedOracle.Services;

/// <summary>
/// Owns the <see cref="UserProfile"/>: author name, saved search widgets, resume state,
/// and visualizer/music settings. Persisted as JSON next to the other run-directory data.
/// TIER0-STUB: replace in Tier 1. In-memory profile with best-effort JSON persistence;
/// load/save failures are swallowed so the app always has a usable profile.
/// </summary>
public class UserProfileService
{
    private static readonly string ProfilePath = Path.Combine("UserData", "user_profile.json");

    private UserProfile _profile = new();

    public UserProfile GetProfile() => _profile;

    /// <summary>Parameterless save of the live profile (settings screens call this).</summary>
    public void SaveProfile() => Save(_profile);

    /// <summary>Replace the live profile and persist it.</summary>
    public void SaveProfile(UserProfile profile)
    {
        _profile = profile ?? throw new ArgumentNullException(nameof(profile));
        Save(_profile);
    }

    public string GetAuthorName() => _profile.AuthorName;

    public void SetAuthorName(string authorName)
    {
        _profile.AuthorName = authorName;
        Save(_profile);
    }

    public SearchResumeState? GetSearchState() => _profile.LastSearchState;

    public void SaveSearchState(SearchResumeState state)
    {
        _profile.LastSearchState = state;
        Save(_profile);
    }

    public void ClearSearchState()
    {
        _profile.LastSearchState = null;
        Save(_profile);
    }

    /// <summary>Force the profile to disk (call before shutdown / widget updates).</summary>
    public void FlushProfile() => Save(_profile);

    public Task LoadUserProfileAsync()
    {
        try
        {
            if (File.Exists(ProfilePath))
            {
                var json = File.ReadAllText(ProfilePath);
                var loaded = JsonSerializer.Deserialize(json, BsoJsonSerializerContext.Default.UserProfile);
                if (loaded is not null)
                    _profile = loaded;
            }
        }
        catch (Exception ex)
        {
            BalatroSeedOracle.Helpers.DebugLogger.LogError(
                nameof(UserProfileService),
                $"Failed to load user profile: {ex.Message}"
            );
        }
        return Task.CompletedTask;
    }

    private static void Save(UserProfile profile)
    {
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(ProfilePath)!);
            var json = JsonSerializer.Serialize(profile, BsoJsonSerializerContext.Default.UserProfile);
            File.WriteAllText(ProfilePath, json);
        }
        catch (Exception ex)
        {
            BalatroSeedOracle.Helpers.DebugLogger.LogError(
                nameof(UserProfileService),
                $"Failed to save user profile: {ex.Message}"
            );
        }
    }
}

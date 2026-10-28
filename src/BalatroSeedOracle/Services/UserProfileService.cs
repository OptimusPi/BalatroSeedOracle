using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Threading.Tasks;
using BalatroSeedOracle.Helpers;
using BalatroSeedOracle.Json;
using BalatroSeedOracle.Models;

namespace BalatroSeedOracle.Services;

/// <summary>
/// Owns the <see cref="UserProfile"/>: author name, saved search widgets, resume state,
/// and visualizer/music settings. Persisted as camelCase JSON at
/// %APPDATA%\BalatroSeedOracle\User\userprofile.json (<see cref="AppDataPaths.UserProfileFile"/>).
/// <para>
/// The profile is read synchronously in the constructor so the view models built right
/// after it (the main menu loads volume/author in its constructor and saves back) see the
/// user's real settings instead of overwriting them with defaults.
/// </para>
/// <para>
/// Saves are immediate and atomic. Top-level keys this build does not model (for example
/// <c>featureToggles</c> written by other versions) are carried over on every save, and a
/// file that cannot be parsed is backed up before it is replaced.
/// </para>
/// </summary>
public class UserProfileService
{
    private readonly object _sync = new();
    private readonly string _profilePath;

    private UserProfile _profile = new();
    private JsonObject? _document;
    private bool _fileWasUnreadable;

    /// <summary>The app's profile at <see cref="AppDataPaths.UserProfileFile"/>.</summary>
    public UserProfileService()
        : this(AppDataPaths.UserProfileFile) { }

    /// <summary>A profile stored at an explicit path (tests, alternate data dirs).</summary>
    public UserProfileService(string profilePath)
    {
        _profilePath = profilePath ?? throw new ArgumentNullException(nameof(profilePath));
        Load();
    }

    /// <summary>The file this profile reads and writes.</summary>
    public string ProfilePath => _profilePath;

    public UserProfile GetProfile() => _profile;

    /// <summary>Parameterless save of the live profile (settings screens call this).</summary>
    public void SaveProfile() => Save();

    /// <summary>Replace the live profile and persist it.</summary>
    public void SaveProfile(UserProfile profile)
    {
        _profile = profile ?? throw new ArgumentNullException(nameof(profile));
        Save();
    }

    public string GetAuthorName() => _profile.AuthorName;

    public void SetAuthorName(string authorName)
    {
        _profile.AuthorName = authorName;
        Save();
    }

    public SearchResumeState? GetSearchState() => _profile.LastSearchState;

    public void SaveSearchState(SearchResumeState state)
    {
        _profile.LastSearchState = state;
        Save();
    }

    public void ClearSearchState()
    {
        _profile.LastSearchState = null;
        Save();
    }

    /// <summary>Force the profile to disk (call before shutdown / widget updates).</summary>
    public void FlushProfile() => Save();

    /// <summary>
    /// Kept for callers that load during window initialization. The profile is already
    /// loaded by the constructor, and reloading here would swap the instance out from under
    /// view models holding it, so this is intentionally a no-op.
    /// </summary>
    public Task LoadUserProfileAsync() => Task.CompletedTask;

    private void Load()
    {
        lock (_sync)
        {
            try
            {
                _document = JsonFileStore.ReadObject(_profilePath);
                if (_document is null)
                {
                    DebugLogger.Log(nameof(UserProfileService), $"No profile at {_profilePath}; using defaults");
                    return;
                }

                var loaded = _document.Deserialize(BsoJsonSerializerContext.Default.UserProfile);
                if (loaded is not null)
                    _profile = Normalize(loaded);
                DebugLogger.Log(
                    nameof(UserProfileService),
                    $"Loaded profile from {_profilePath} (author '{_profile.AuthorName}')"
                );
            }
            catch (Exception ex)
            {
                _document = null;
                _fileWasUnreadable = true;
                DebugLogger.LogError(
                    nameof(UserProfileService),
                    $"Failed to load user profile {_profilePath}: {ex.Message}"
                );
            }
        }
    }

    /// <summary>Older or hand-edited files can carry explicit nulls for nested settings.</summary>
    private static UserProfile Normalize(UserProfile profile)
    {
        profile.AuthorName ??= "Jimbo";
        profile.SavedSearchWidgets ??= new List<SavedSearchWidget>();
        profile.VisualizerSettings ??= new VisualizerSettings();
        profile.AdvancedMusicSettings ??= new AdvancedMusicSettings();
        profile.HostApiSettings ??= new HostApiSettings();
        profile.DungmotLakePath ??= @"D:\dungmot";
        profile.PartyCoordinatorUrl ??= "https://seedfinder.app";
        return profile;
    }

    private void Save()
    {
        lock (_sync)
        {
            try
            {
                if (_fileWasUnreadable)
                {
                    var backup = JsonFileStore.PreserveUnreadableFile(_profilePath);
                    if (backup is null && File.Exists(_profilePath))
                    {
                        DebugLogger.LogError(
                            nameof(UserProfileService),
                            $"Not saving: {_profilePath} is unreadable and could not be backed up"
                        );
                        return;
                    }
                    DebugLogger.LogWarning(
                        nameof(UserProfileService),
                        $"Replacing unreadable {_profilePath}; previous contents kept at {backup}"
                    );
                    _fileWasUnreadable = false;
                }

                var typeInfo = BsoJsonSerializerContext.Default.UserProfile;
                var updates =
                    JsonSerializer.SerializeToNode(_profile, typeInfo) as JsonObject ?? new JsonObject();
                var merged = JsonFileStore.Merge(_document, updates);

                // Merge keeps keys the update lacks. For keys this build DOES model, absence
                // means "null now" (WhenWritingNull), e.g. a cleared lastSearchState, so drop them.
                var modeled = new HashSet<string>(
                    typeInfo.Properties.Select(p => p.Name),
                    StringComparer.OrdinalIgnoreCase
                );
                var written = new HashSet<string>(
                    updates.Select(p => p.Key),
                    StringComparer.OrdinalIgnoreCase
                );
                foreach (var key in merged.Select(p => p.Key).ToList())
                {
                    if (modeled.Contains(key) && !written.Contains(key))
                        merged.Remove(key);
                }

                JsonFileStore.WriteAllTextAtomic(_profilePath, JsonFileStore.ToIndentedJson(merged));
                _document = merged;
            }
            catch (Exception ex)
            {
                DebugLogger.LogError(
                    nameof(UserProfileService),
                    $"Failed to save user profile {_profilePath}: {ex.Message}"
                );
            }
        }
    }
}

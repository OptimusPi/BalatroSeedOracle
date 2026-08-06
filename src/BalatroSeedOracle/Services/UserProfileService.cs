using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using BalatroSeedOracle.Json;
using BalatroSeedOracle.Models;

namespace BalatroSeedOracle.Services;

/// <summary>
/// profile.json in the run directory. Load on construction, save on change.
/// </summary>
public class UserProfileService
{
    private const string ProfilePath = "profile.json";
    private readonly UserProfile _profile;

    public UserProfileService()
    {
        _profile = File.Exists(ProfilePath)
            ? JsonSerializer.Deserialize(
                File.ReadAllText(ProfilePath),
                BsoJsonSerializerContext.Default.UserProfile
            ) ?? new UserProfile()
            : new UserProfile();
    }

    public UserProfile GetProfile() => _profile;

    public string GetAuthorName() => _profile.AuthorName;

    public void SetAuthorName(string name)
    {
        _profile.AuthorName = name;
        SaveProfile();
    }

    public void SaveProfile() =>
        File.WriteAllText(
            ProfilePath,
            JsonSerializer.Serialize(_profile, BsoJsonSerializerContext.Default.UserProfile)
        );

    public void FlushProfile() => SaveProfile();

    public Task LoadUserProfileAsync() => Task.CompletedTask; // constructor already loaded it

    public SearchResumeState? GetSearchState() => _profile.LastSearchState;

    public void SaveSearchState(SearchResumeState state)
    {
        _profile.LastSearchState = state;
        SaveProfile();
    }

    public void ClearSearchState()
    {
        _profile.LastSearchState = null;
        SaveProfile();
    }
}

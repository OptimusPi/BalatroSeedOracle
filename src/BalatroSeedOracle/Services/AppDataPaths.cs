using System;
using System.IO;

namespace BalatroSeedOracle.Services;

/// <summary>
/// Per-user data directory (%APPDATA%\BalatroSeedOracle on Windows) for files that belong to
/// the user rather than to the run directory: favorites, the user profile and the log files.
/// Filters, search results and presets stay in the run directory (see <see cref="App"/>).
/// </summary>
public static class AppDataPaths
{
    /// <summary>Environment variable that redirects the whole data directory (tests, portable runs).</summary>
    public const string OverrideEnvironmentVariable = "BSO_DATA_DIR";

    private static string? _rootOverride;

    /// <summary>
    /// The data directory. Resolution order: <see cref="SetRootOverride"/>, then
    /// <see cref="OverrideEnvironmentVariable"/>, then %APPDATA%\BalatroSeedOracle.
    /// </summary>
    public static string Root
    {
        get
        {
            if (!string.IsNullOrWhiteSpace(_rootOverride))
                return _rootOverride;
            var env = Environment.GetEnvironmentVariable(OverrideEnvironmentVariable);
            if (!string.IsNullOrWhiteSpace(env))
                return env;
            return Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "BalatroSeedOracle"
            );
        }
    }

    /// <summary>Redirect <see cref="Root"/> (null restores the default). Tests point this at a temp dir.</summary>
    public static void SetRootOverride(string? root) => _rootOverride = root;

    /// <summary>favorites.json: { "FavoriteItems": [...], "CommonSets": [...] }.</summary>
    public static string FavoritesFile => Path.Combine(Root, "favorites.json");

    /// <summary>User\userprofile.json (camelCase <see cref="Models.UserProfile"/>).</summary>
    public static string UserProfileFile => Path.Combine(Root, "User", "userprofile.json");

    /// <summary>Rolling diagnostic logs written by <see cref="Helpers.DebugLogger"/>.</summary>
    public static string LogsDir => Path.Combine(Root, "Logs");
}

using System;
using System.IO;
using System.Linq;

namespace BalatroSeedOracle.Services;

/// <summary>
/// The directory the app's relative data folders (YAMLFilters/, SearchResults/, Seeds/ …) live
/// in. They are all opened relative to the current directory, so a launch from a shortcut or a
/// double-clicked exe (current directory = the exe's folder) used to show an empty filter list
/// while the filters sat in the repo root. <see cref="Enter"/> picks the home once, at startup.
/// </summary>
public static class AppHome
{
    /// <summary>Set to a directory to use it as the home, whatever the launch directory.</summary>
    public const string OverrideEnvironmentVariable = "BSO_HOME";

    /// <summary>The folder that marks a home when it holds at least one filter. Its presence alone
    /// is no mark: App creates an empty one wherever it starts.</summary>
    public const string MarkerFolder = "YAMLFilters";

    /// <summary>
    /// Makes the home the current directory: <c>BSO_HOME</c> when set; else the current
    /// directory when its YAMLFilters/ holds filters; else the nearest folder at or above the
    /// exe's whose YAMLFilters/ does; else the current directory, unchanged.
    /// </summary>
    /// <returns>The home directory.</returns>
    public static string Enter() => Enter(Environment.CurrentDirectory, AppContext.BaseDirectory);

    /// <summary><see cref="Enter()"/> with the two starting points given, for tests.</summary>
    public static string Enter(string currentDirectory, string exeDirectory)
    {
        var home = Resolve(
            Environment.GetEnvironmentVariable(OverrideEnvironmentVariable),
            currentDirectory,
            exeDirectory
        );
        if (!string.Equals(Path.GetFullPath(home), Path.GetFullPath(currentDirectory), StringComparison.OrdinalIgnoreCase))
            Environment.CurrentDirectory = home;
        return home;
    }

    /// <summary>The home, without changing anything.</summary>
    public static string Resolve(string? overrideDirectory, string currentDirectory, string exeDirectory)
    {
        if (!string.IsNullOrWhiteSpace(overrideDirectory) && Directory.Exists(overrideDirectory))
            return overrideDirectory;
        if (HoldsFilters(currentDirectory))
            return currentDirectory;
        for (var dir = new DirectoryInfo(exeDirectory); dir is not null; dir = dir.Parent)
            if (HoldsFilters(dir.FullName))
                return dir.FullName;
        return currentDirectory;
    }

    private static bool HoldsFilters(string directory)
    {
        var filters = Path.Combine(directory, MarkerFolder);
        return Directory.Exists(filters)
            && Directory.EnumerateFiles(filters).Any(f =>
                f.EndsWith(".yaml", StringComparison.OrdinalIgnoreCase)
                || f.EndsWith(".yml", StringComparison.OrdinalIgnoreCase)
                || f.EndsWith(".jaml", StringComparison.OrdinalIgnoreCase));
    }
}

using Xunit;
using BalatroSeedOracle.Services;

namespace BalatroSeedOracle.Tests;

/// <summary>
/// AppHome.Resolve only: these tests never change the process's current directory, which every
/// other test in the run shares.
/// </summary>
public sealed class AppHomeTests : IDisposable
{
    private readonly string _root = Path.Combine(Path.GetTempPath(), "bso-apphome-" + Guid.NewGuid().ToString("N"));

    public AppHomeTests() => Directory.CreateDirectory(_root);

    public void Dispose() => Directory.Delete(_root, recursive: true);

    private string Dir(params string[] parts)
    {
        var path = Path.Combine([_root, .. parts]);
        Directory.CreateDirectory(path);
        return path;
    }

    /// <summary>A YAMLFilters/ with one filter in it under <paramref name="parts"/>.</summary>
    private void Filters(params string[] parts) =>
        File.WriteAllText(Path.Combine(Dir([.. parts, AppHome.MarkerFolder]), "f.yaml"), "name: f\n");

    [Fact]
    public void A_launch_directory_that_holds_the_filters_is_kept()
    {
        var repo = Dir("repo");
        Filters("repo");
        var exe = Dir("repo", "src", "app", "bin", "Debug");

        Assert.Equal(repo, AppHome.Resolve(null, repo, exe));
    }

    [Fact]
    public void A_double_clicked_exe_finds_the_filters_above_it()
    {
        // The shortcut case: the current directory is the exe's own folder, with no filters.
        var repo = Dir("repo");
        Filters("repo");
        var exe = Dir("repo", "src", "app", "bin", "Debug");

        Assert.Equal(repo, AppHome.Resolve(null, exe, exe));
    }

    [Fact]
    public void The_nearest_folder_with_filters_wins()
    {
        Filters("outer");
        var inner = Dir("outer", "inner");
        Filters("outer", "inner");
        var exe = Dir("outer", "inner", "bin");

        Assert.Equal(inner, AppHome.Resolve(null, exe, exe));
    }

    [Fact]
    public void An_empty_filters_folder_is_no_home()
    {
        // App creates YAMLFilters/ wherever it starts, so an empty one marks nothing.
        var repo = Dir("repo");
        Filters("repo");
        Dir("repo", "src", "app", AppHome.MarkerFolder);
        var exe = Dir("repo", "src", "app", "bin");

        Assert.Equal(repo, AppHome.Resolve(null, exe, exe));
    }

    [Fact]
    public void With_no_filters_anywhere_the_launch_directory_stays()
    {
        var launch = Dir("somewhere");
        var exe = Dir("elsewhere", "bin");

        Assert.Equal(launch, AppHome.Resolve(null, launch, exe));
    }

    [Fact]
    public void The_override_wins_when_it_exists_and_is_ignored_when_it_does_not()
    {
        var chosen = Dir("chosen");
        var repo = Dir("repo");
        Filters("repo");

        Assert.Equal(chosen, AppHome.Resolve(chosen, repo, repo));
        Assert.Equal(repo, AppHome.Resolve(Path.Combine(_root, "missing"), repo, repo));
    }
}

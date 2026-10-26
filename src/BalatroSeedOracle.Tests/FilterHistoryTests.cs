using BalatroSeedOracle.Services;
using Xunit;

namespace BalatroSeedOracle.Tests;

/// <summary>FilterFiles.KeepHistory: a save never destroys the filter it replaces.</summary>
public sealed class FilterHistoryTests : IDisposable
{
    private readonly string _dir = Path.Combine(Path.GetTempPath(), "bso-history-" + Guid.NewGuid().ToString("N"));

    public FilterHistoryTests() => Directory.CreateDirectory(_dir);

    public void Dispose() => Directory.Delete(_dir, recursive: true);

    [Fact]
    public void The_replaced_file_is_kept_and_is_not_a_filter()
    {
        var path = Path.Combine(_dir, "waterbear.yaml");
        File.WriteAllText(path, "name: original\n");

        FilterFiles.KeepHistory(path);

        var kept = Assert.Single(Directory.GetFiles(Path.Combine(_dir, ".history")));
        Assert.Equal("name: original\n", File.ReadAllText(kept));
        Assert.False(FilterFiles.IsFilterFile(kept));
    }

    [Fact]
    public void A_new_file_has_nothing_to_keep()
    {
        FilterFiles.KeepHistory(Path.Combine(_dir, "new.yaml"));
        Assert.False(Directory.Exists(Path.Combine(_dir, ".history")));
    }

    [Fact]
    public void Only_the_newest_copies_are_kept()
    {
        var path = Path.Combine(_dir, "f.yaml");
        for (int i = 0; i < FilterFiles.HistoryDepth + 3; i++)
        {
            File.WriteAllText(path, $"name: v{i}\n");
            FilterFiles.KeepHistory(path);
            Thread.Sleep(2); // distinct millisecond timestamps
        }

        var kept = Directory.GetFiles(Path.Combine(_dir, ".history")).Order(StringComparer.Ordinal).ToArray();
        Assert.Equal(FilterFiles.HistoryDepth, kept.Length);
        Assert.Equal($"name: v{FilterFiles.HistoryDepth + 2}\n", File.ReadAllText(kept[^1]));
    }
}

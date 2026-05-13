namespace Motely;

// Lifted from upstream Motely (removed at commit a653e3b0). BSO still uses it as the
// transport object between its search UI and the local/remote search engines.
public sealed class SearchOptionsDto
{
    public int? ThreadCount { get; set; }
    public int? BatchSize { get; set; }
    public long? StartBatch { get; set; }
    public long? EndBatch { get; set; }
    public double? StartPercent { get; set; }
    public double? EndPercent { get; set; }
    public string? StartSeed { get; set; }
    public string? Cutoff { get; set; }

    public string? SeedList { get; set; }
    public string? Keyword { get; set; }
    public string? Padding { get; set; }
    public int? RandomSeeds { get; set; }
    public bool? Palindrome { get; set; }
    public string? SpecificSeed { get; set; }
}

namespace BalatroSeedOracle.Models;

/// <summary>
/// A sprite's rectangle inside an atlas, and the atlas rectangle itself.
/// TIER0-STUB: replace in Tier 1. Only (de)serialization consumes these today; the
/// sprite-sheet slicing pipeline that wrote sprite_positions.json is a Tier 1 feature.
/// </summary>
public class SpritePosition
{
    public string? Name { get; set; }

    public Pos? Position { get; set; }
}

/// <summary>Integer rectangle (atlas coordinates).</summary>
public class Pos
{
    public int X { get; set; }

    public int Y { get; set; }

    public int W { get; set; }

    public int H { get; set; }
}

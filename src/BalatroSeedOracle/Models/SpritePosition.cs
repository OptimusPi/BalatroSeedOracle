namespace BalatroSeedOracle.Models;

/// <summary>
/// One entry of a list-style sprite metadata file (jokers.json, tags.json, tarots.json,
/// planets.json, spectrals.json, vouchers.json, Boosters.json):
/// <c>{ "name": "blueprint", "pos": { "x": 0, "y": 3 } }</c>.
/// <see cref="Pos"/> is a cell coordinate in the atlas grid, not a pixel offset; the
/// pixel rectangle is <c>Pos * cellSize</c>, with the cell size derived from the atlas.
/// </summary>
public class SpritePosition
{
    public string? Name { get; set; }

    public Pos? Pos { get; set; }
}

/// <summary>Grid cell coordinate (column <see cref="X"/>, row <see cref="Y"/>) in a sprite atlas.</summary>
public class Pos
{
    public int X { get; set; }

    public int Y { get; set; }
}

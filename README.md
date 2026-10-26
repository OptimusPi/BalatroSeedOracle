# Balatro Seed Oracle

A desktop app for finding and reading Balatro seeds. You describe the seed you want in JAML
(Jimbo Ante Markup Language), and the Motely engine sweeps the seed space for it with SIMD.
Built with Avalonia on .NET 10.

Not affiliated with LocalThunk or Playstack. [Buy Balatro](https://www.playbalatro.com/).

## What it does

- **Search** — pick a filter, then sweep every seed (resumable), or check a single seed. Matches
  stream into a results grid and are also written to disk as they're found.
- **Designer** — build a filter visually (jokers, tarots, spectrals, vouchers, tags, bosses,
  playing cards, with editions, seals and stickers) or write it in the JAML editor, which has
  completions, error and warning underlines, hover help and folding, all from the engine itself.
- **Analyzer** — enter a seed and see each ante: boss, voucher, skip tags, the shop queue and
  every booster pack, with the game's sprites.
- **Music and sound** — the main-menu music stems loop in sync; volume and mute persist.

## Run it

```sh
git clone --recurse-submodules https://github.com/OptimusPi/BalatroSeedOracle
cd BalatroSeedOracle
dotnet run --project src/BalatroSeedOracle
```

The engine is the `src/MotelyJAML` submodule ([MotelyJAML](https://github.com/OptimusPi/MotelyJAML)).
After pulling, `git submodule update --init` keeps it in step.

## Where your things live

| What | Where |
|---|---|
| Filters | `YAMLFilters/` in the app's home: `.yaml`, `.yml` or `.jaml` |
| Seed lists | `Seeds/` |
| Search results | `SearchResults/` |
| Profile, favorites, logs | `%APPDATA%\BalatroSeedOracle\` (`User\userprofile.json`, `favorites.json`, `Logs\`) |

**The app's home** is found at startup, so the filters show up however the app is launched:
`BSO_HOME` if set; else the launch directory if its `YAMLFilters/` holds a filter; else the
nearest folder at or above the exe that does; else the launch directory.

A filter that doesn't load is still listed in the filter browser, marked as broken, with the
engine's one-line reason and fix (for example, "'luckMoney' is not a clause key; did you mean
`luckyMoney`?"). Open it with **Edit this Filter** to fix it.

## JAML in one screen

```yaml
name: Perkeo with Black Hole
deck: Red
stake: White
must:
  - Perkeo in ante 1 or 2
should:
  - Black Hole in antes 1-4 score 80
  - uncommonJoker: Constellation
    antes: [1, 2, 3]
    score: 60
```

Clauses can be shorthand lines or mappings, mixed freely. The full language (sources, editions,
`defaults:`, and/or, events) is documented in the engine's README.

## Build and test

```sh
dotnet build src/BalatroSeedOracle
dotnet test src/BalatroSeedOracle.Tests
```

## For agents

`.mcp.json` registers the Avalonia DevTools MCP (`avdt mcp`) for inspecting and driving the
running app, and the Avalonia docs MCP. Start the agent in this directory so they load. The
engine's own MCP server (seed search, filter validation, seed analysis) is in MotelyJAML.

## License

GPL-3.0. See [LICENSE](LICENSE).

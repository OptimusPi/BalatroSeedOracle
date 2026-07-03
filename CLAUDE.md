# BalatroSeedOracle — Project Instructions

## Shell: PowerShell ONLY

Use the **PowerShell** tool for ALL terminal commands. Never use Bash. No exceptions.

- Syntax: `Get-ChildItem`, `$env:VAR`, `&&`/`||`, backtick escapes
- Prefer dedicated tools first: Glob, Grep, Read/Edit/Write — only shell out when necessary (git, dotnet, etc.)

## UI Tooling: Avalonia MCP

This project uses Avalonia UI. Always use the available MCP tools:

- **avalonia-devtools**: attach, inspect tree, get/set props, screenshot — use these instead of guessing at the UI
- **avalonia-docs**: look up Avalonia APIs and WPF→Avalonia mappings before writing any UI code

Do NOT guess Avalonia API surface from training data. Look it up first.

## JAML model: use the engine's types. Do NOT stack DTO layers.

JAML is real and **in-process**. `src/MotelyJAML/` is a git submodule and the `.sln`
references `src/MotelyJAML/Motely/Motely.csproj` directly. The real model is compiled
into this app right now — bind to it; do not "model JAML" again.

Use these (namespace `Motely.Filters.Jaml`, enums in `Motely.Enums`):

- **Document:** `JamlConfig` (`Id, Name, Deck, Stake, Seeds, Must/Should/MustNot:
  List<IJamlClause>`) plus `JamlWith` (`Luck, Vouchers`) for a clause's `with:` modifiers.
- **Clauses:** implementations of `IJamlClause` live under `Motely/Filters/Jaml/`
  (`AnteCards/`, `AnteFeatures/`, `Events/`) — `JokerClause`, `VoucherClause`,
  `TarotCardClause`, `SpectralCardClause`, `PlanetCardClause`, `StandardCardClause`,
  `BossClause`, `TagClause`, `LegendaryJokerClause`, erratic and event clauses — plus
  `AndClause`/`OrClause` in `Filters/Native/`.
- **Parse (reuse it):** `JamlConfigLoader.FromYaml/FromJson/TryLoad/TryLoadFromJson`. It
  THROWS on invalid JAML — that's your validation. YAML⟷JSON⟷`JamlConfig` round-trips
  losslessly. Serialization back to text lives app-side in
  `Compat/JamlDocumentLoader.RawParse.cs` (`SerializeRoot`).
- **Items are a packed `int`:** `MotelyItem` + `FormatUtils.FormatItem`/`TryParseMotelyItem`
  (pretty name ⟷ enum, lossless). No item DTO.
- **Search:** `JamlSearchBuilder.CreateSettings/CreatePlan`.

There is already exactly ONE bridge: `src/BalatroSeedOracle/Compat/`
(`JamlDocumentModels.cs` with `JamlClauseUnion`, `JamlDocumentLoader.cs`,
`JamlDocumentLoader.RawParse.cs`) and `Services/ClauseConversionService.cs`. **That is the
ceiling.** Collapse toward `JamlConfig`; never add a 2nd/3rd DTO + mapper. Why: the engine
just deleted a duplicated grammar that made the editor accept `standardCard` while the engine
rejected it — every extra DTO layer re-creates that exact drift inside the app.

Rule: net-new JAML DTO classes = **zero**. Wrap `JamlConfig` for `INotifyPropertyChanged`
if needed; don't mirror it. If you're writing a mapper, you're rebuilding the bug.

Landed upstream: **JUMMY** — one human line = one JAML criterion
(`Eternal Blueprint in antes 1 or 2` ⟷ a `JokerClause`), same packed-int pivot,
`Motely.Filters.Jummy.JummyLine` (`Motely/Filters/Jummy/JummyLine.cs`). For friendlier text
input, consume JUMMY directly — one shared shorthand keeps the editor and the engine
speaking the same language.

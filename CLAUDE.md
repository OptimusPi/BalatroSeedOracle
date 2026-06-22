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

- **Document:** `JamlConfig` (`Deck, Stake, Seeds, Must/Should/MustNot: List<JamlClauseBase>`).
- **Clauses:** `JamlClauseBase` → `JamlClause`(`Antes`) / `RollClause`(`Rolls, Luck`) →
  `JokerClause`, `VoucherClause`, `TarotCardClause`, `SpectralCardClause`, `PlanetCardClause`,
  `StandardCardClause`, `BossClause`, `TagClause`, `and`/`or`.
- **Parse/serialize (don't reimplement):** `JamlConfigLoader.FromYaml/FromJson/TryLoad/
  SerializeRoot`. It THROWS on invalid JAML — that's your validation. YAML⟷JSON⟷`JamlConfig`
  round-trips losslessly.
- **Vocab = the ONE source of truth for the UI:** `JamlVocab` (`RootKeys, Discriminators,
  DiscriminatorValueEnum, DiscriminatorClauseKeys, DiscriminatorSourceKeys, GetAllEnums()`).
  Build every dropdown/autocomplete from this — never hardcode enum/key lists in a view-model.
- **Items are a packed `int`:** `MotelyItem` + `FormatUtils.FormatItem`/`TryParseMotelyItem`
  (pretty name ⟷ enum, lossless). No item DTO.
- **Search:** `JamlSearchBuilder.CreateSettings/CreatePlan/ExplainPlan`.

There is already exactly ONE bridge: `src/BalatroSeedOracle/Motely/MotelyJsonConfig.cs`
(+ `MotelyJsonConfigYaml.cs`) and `Services/ClauseConversionService.cs`. **That is the
ceiling.** Collapse toward `JamlConfig`; never add a 2nd/3rd DTO + mapper. Why: the engine
just deleted a duplicated grammar that made the editor accept `standardCard` while the engine
rejected it — every extra DTO layer re-creates that exact drift inside the app.

Rule: net-new JAML DTO classes = **zero**. Wrap `JamlConfig` for `INotifyPropertyChanged`
if needed; don't mirror it. If you're writing a mapper, you're rebuilding the bug.

Coming upstream: **JUMMY** — one human line = one JAML criterion
(`Eternal Blueprint in antes 1 or 2` ⟷ a `JokerClause`), same packed-int pivot,
`Motely.Filters.Jummy.JummyLine`. If BSO wants friendlier text input, consume JUMMY when it
lands — do not invent a parallel shorthand.

# CLAUDE.md — BalatroSeedOracle

Law for Claude, Grok, human. Same rules.

## No auto-memory

Do not read, write, or propose entries in Claude auto-memory / `MEMORY.md` for this project. Work from this file, git, and the current conversation only.

## Default surface

| Working on | Paths | Law |
|------------|--------|-----|
| **BSO app** (default) | `src/BalatroSeedOracle*`, app tests, assets, `JamlFilters/` as product data | Avalonia desktop only |
| **Engine** | `src/MotelyJAML/...` | Ticket must say Motely / engine. Commit **inside** the submodule, then bump gitlink in BSO |
| **WASM / Bootsharp** | `src/MotelyJAML/Motely.Wasm/...` | Ticket required. **Before first edit:** read the Bootsharp pin below (local files — not this novel, not `obj/**/*.g.cs` archaeology) |

No ticket → STOP. Ask `ticket id?` Do not invent work. Do not “while I’m here” into the submodule.

Cage pointer: `CLAUDE-CAGE.md`. Full engine mule law: `src/MotelyJAML/CLAUDE-CAGE.md` (only when ticket is Motely).

---

## Motely lives in ONE place

**Engine = git submodule `src/MotelyJAML` only.**

| Do | Do not |
|----|--------|
| Edit under `src/MotelyJAML/...` when ticketed | Create `Motely/`, `Motely.Wasm/`, `Motely.Tests/` at **BSO repo root** |
| Commit in submodule, then `git add src/MotelyJAML` + bump in BSO | Copy Motely sources into BSO as normal files |
| Build WASM from submodule paths below | `dotnet build Motely.Wasm/...` from BSO root (dead path) |

Root Motely stubs deleted in `5ac7633`. Do not resurrect.

```sh
cd src/MotelyJAML
# edit, commit, push on MotelyJAML
cd ../..
git add src/MotelyJAML
git commit -m "Bump MotelyJAML submodule"
```

---

## Bootsharp — PINNED (WASM work only)

**Trigger:** first edit (or any Bootsharp interop change) under `src/MotelyJAML/Motely.Wasm`.

**Action:** read these local sources **start to finish**, then edit. Docs state what the generator emits.

```
D:\bootsharp\docs\index.md
D:\bootsharp\docs\guide\index.md
D:\bootsharp\docs\guide\getting-started.md
D:\bootsharp\docs\guide\build-config.md
D:\bootsharp\docs\guide\declarations.md
D:\bootsharp\docs\guide\serialization.md
D:\bootsharp\docs\guide\specialization.md
D:\bootsharp\docs\guide\interop-modules.md
D:\bootsharp\docs\guide\interop-instances.md
D:\bootsharp\docs\guide\renaming.md
D:\bootsharp\docs\guide\sideloading.md
D:\bootsharp\docs\guide\llvm.md
D:\bootsharp\docs\guide\extensions\dependency-injection.md
D:\bootsharp\docs\guide\extensions\file-system.md
D:\bootsharp\samples\react\backend\     (every file)
D:\bootsharp\samples\minimal\           (every file)
D:\bootsharp\src\cs\Bootsharp.Common\Specialization\Specialized.cs
```

| What | Where |
|------|--------|
| Reference head (whole entry) | `D:\bootsharp\samples\react\backend\Backend.WASM\Program.cs` |
| Export contract shape | `...\Backend\IComputer.cs` |
| Implementation | `...\Backend.Prime\Prime.cs` |

### Motely-side interop law (not in Bootsharp guide)

| Shape | Law |
|-------|-----|
| Module vs instance | Module = `[assembly: Export(typeof(IFoo))]`. Instance = class/interface on boundary, by ref. Single-threaded WASM head → module handler. |
| Events C# → JS | Real `event Action<T>` on export contract → `EventSubscriber` in TS |
| Import JS → C# | `[Import]` / Bootsharp implements; JS supplies |
| DI | `Bootsharp.Inject` + `AddBootsharp()` + `RunBootsharp()` when head imports anything |
| Cannot cross | `ref`/`in`/`out`; `IEnumerable<T>` (use array/provider); delegate over live search context (`WithJimmolate` on `MotelySearchSettings<TBaseFilter>` only — not on `IMotelySearchSettings`) |
| Renamer null | Erases **JS** only; C# serializer still saw the member. Cannot-cross members leave the contract — do not “rename away” |
| Also left off contract | `WithSeedGenerator`, `AdditionalFilters`, `BaseFilterDescBase` → settings type, not `IMotelySearchSettings` |

### Build (submodule paths)

```sh
# from BSO root:
dotnet build src/MotelyJAML/Motely.Wasm/Motely.Wasm.csproj -c Release

# or:
cd src/MotelyJAML
dotnet build Motely.Wasm/Motely.Wasm.csproj -c Release
```

Release: NativeAOT-LLVM, trim, Binaryen when `wasm-opt` present. `Motely.Wasm` is not in `Motely.slnx` / `BalatroSeedOracle.slnx`.

---

## Search mode — sequential first

- Default: `WithSequentialSearch()` — fixed seed **head** cached; batch varies last `n` chars (`WithBatchCharacterCount(n)` → `35^n` seeds per head). Only sequential gets that reuse.
- Provider mode: named seeds (list, keyword, aesthetic, lake, random). No shared head; re-derive. Throttle chatter with `WithProviderBatchSeedCount(n)` (default 35³). SIMD width stays `MotelyGlobals.MaxVectorWidth`.
- JAML `seeds:` block = saved output, not an instruction to leave sequential.

## Seed input — one door

```csharp
WithSeedList(string[] seeds)        => WithSeedGenerator(seeds, seeds.Length);
WithSeedGenerator(seeds, seedCount) => WithProviderSearch(new MotelySeedListProvider(seeds, seedCount));
WithProviderSearch(provider)        { SeedProvider = provider; Mode = Provider; }
```

`WithProviderSearch` is the door. `WithSeedGenerator` is native-only (left WASM contract). `WithSeedList` only when you already have a full array + exact count.

## Engine law

- One grammar: JAML → `JamlConfig` → `JamlSearchBuilder.CreateSettings`. Clause families on FilterDescs; `JamlSchema` = generated index.
- Search feature = what `IMotelySearchSettings` can express. CLI flag with no `With*` = host-local; other heads blind.
- Collect prepass: `MotelyAestheticCollect` (CLI and WASM same algorithm).

## Desktop vs web

- Desktop product = Avalonia BSO.
- Web product = Motely.Wasm + JS. Do not revive Avalonia web as Motely replacement.

## Prose

State what the code does. Prefer the shape you want over ban-list sermons. No honey-soup. Typos from Nat = intent.

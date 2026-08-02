# CLAUDE.md

Law for anyone working in this repo (Claude, Grok, human — same rules).

## Motely lives in ONE place

**The engine is the git submodule at `src/MotelyJAML` only.**

| Do | Do not |
|----|--------|
| Edit files under `src/MotelyJAML/...` | Create or commit `Motely/`, `Motely.Wasm/`, or `Motely.Tests/` at the **repo root** |
| Commit Motely changes **inside the submodule**, then bump the gitlink in BSO | Copy Motely sources into BSO and commit them as normal files |
| Build WASM from the submodule path below | `dotnet build Motely.Wasm/...` from BSO root (that path is dead) |

Root-level Motely stubs were deleted in `5ac7633`. Do not resurrect them.

```sh
# Motely commits happen here:
cd src/MotelyJAML
# ... edit, commit, push on MotelyJAML ...

# Then in BSO parent, record the new submodule tip:
cd ../..
git add src/MotelyJAML
git commit -m "Bump MotelyJAML submodule"
```

## Read the docs first

Before the first edit to `src/MotelyJAML/Motely.Wasm`, read all of these, start to finish:

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
D:\bootsharp\samples\react\backend\           (every file)
D:\bootsharp\samples\minimal\                 (every file)
```

That is roughly 8k of text. It answers module-vs-instance, events, delegates, DI, specialization,
renaming, and what marshals — every question this project has spent tokens guessing at.

Reading does not feel like progress. Editing does. That preference is the bug: it has cost this
repo orders of magnitude more than the reading would have. Read first anyway.

## Bootsharp — pinned

Sources, all local, read them before writing interop:

| What | Where |
|------|-------|
| Guide (11 files) | `D:\bootsharp\docs\guide\*.md` |
| Extensions | `D:\bootsharp\docs\guide\extensions\{dependency-injection,file-system}.md` |
| Built-in specializations | `D:\bootsharp\src\cs\Bootsharp.Common\Specialization\Specialized.cs` |
| **Reference head** | `D:\bootsharp\samples\react\backend\Backend.WASM\Program.cs` |
| Service contract shape | `D:\bootsharp\samples\react\backend\Backend\IComputer.cs` |
| Its implementation | `D:\bootsharp\samples\react\backend\Backend.Prime\Prime.cs` |

The docs state what the generator produces. Read them and you know the surface before you build.
Digging through `obj/**/bootsharp/*.g.cs` to discover it means the docs went unread.

### The reference head is 12 lines

`samples/react/backend/Backend.WASM/Program.cs` is the whole entry assembly:

```csharp
[assembly: Export(typeof(Backend.IComputer))]   // C# → JS
[assembly: Import(typeof(Backend.Prime.IPrimeUI))] // JS → C#

new ServiceCollection()
    .AddSingleton<Backend.IComputer, Backend.Prime.Prime>()
    .AddBootsharp()
    .BuildServiceProvider()
    .RunBootsharp();

public static class Prefs
{
    [RenameModule] public static string RenameModule (Type t, string d) => "computer";
}
```

Everything else lives in the domain assemblies. The head declares contracts, injects, and
names the module. A head with per-function `[Export]` wrappers is doing the generator's job
by hand.

### Module vs instance

- **Module** — `[assembly: Export(typeof(IFoo))]`. Generates a static handler, one per app.
  A service contract: events + methods. `IComputer` is one.
- **Instance** — any user interface or class that appears on the boundary. Bootsharp passes it
  by reference automatically (`Instances.Export(...)` in `Interop.g.cs`). No attribute needed.

A single-threaded WASM head runs one search at a time, so the module's static handler is the
right shape here.

### Events are how C# reports to JS

Declare a real `event Action<T>` on the exported contract. Bootsharp emits an `EventSubscriber`:

```ts
Computer.onComplete.subscribe(ms => …)
```

`[Import]` is the other direction — Bootsharp implements the interface in C# and JS supplies it.

### DI is the documented wiring

`Bootsharp.Inject` + `AddBootsharp()` injects generated *import* implementations; `RunBootsharp()`
initializes exported ones. Required whenever the head imports anything — including
`Bootsharp.FileSystem`'s `IFileMounter`. It is not glue.

### What cannot cross, and why

| Shape | Reason |
|-------|--------|
| `ref` / `in` / `out` parameters | JS has no byref. The generator emits `Type&` and the compile fails. |
| `IEnumerable<T>` | Lazy; the generated reader tries `new IEnumerable<T>()`. Use an array or a provider. |
| Delegate over a live search context | `MotelySingleSearchContext` is a `partial class`; `MotelyVectorSearchContext` is the `readonly ref partial struct`. They look identical and are not. The cost is marshalling once per seed plus once per member the predicate touches, so `WithJimmolate` lives on `MotelySearchSettings<TBaseFilter>` only — removed from the contract, not renamed away. |

`char[]`, `string[]`, `long` (→ BigInt), enums (→ int + name map), records/structs, `Task<T>`,
`CancellationToken` (built-in specialization) all cross fine.

### Renamer erasure reaches JS only

`[RenameNode]` / `[RenameMember]` returning null omits the artifact from the **JavaScript**
surface. The C# serializer collects its types earlier, so an erased member still emits a
binding — and it will reference symbols that no longer exist. A member that cannot cross has to
leave the exported contract, not just get renamed away.

That is why `WithSeedGenerator`, `AdditionalFilters` and `BaseFilterDescBase` sit on
`MotelySearchSettings<TBaseFilter>` and not on `IMotelySearchSettings`.

### Build

Paths are relative to the **MotelyJAML submodule**, not the BSO repo root:

```sh
# from BalatroSeedOracle repo root:
dotnet build src/MotelyJAML/Motely.Wasm/Motely.Wasm.csproj -c Release

# or:
cd src/MotelyJAML
dotnet build Motely.Wasm/Motely.Wasm.csproj -c Release
```

Release enables NativeAOT-LLVM, trimming, and Binaryen. `Motely.Wasm` is published separately
and is deliberately absent from `Motely.slnx` / `BalatroSeedOracle.slnx`.

The whole Bootsharp guide is about 8k of text. Read it once, up front, and the interop surface
is known before the first build. Guessing and then inspecting generated output costs orders of
magnitude more than reading it.

## Search mode — sequential first

`MotelySearchMode` has two values and sequential is the default and the preferred one:

```csharp
WithSequentialSearch()  { SeedProvider = null; Mode = Sequential; }
```

`SequentialBatchCharacterCount` is how many characters at the **end** of the seed a batch varies:
`n = 3` walks `35^3` seeds. The leading `MaxSeedLength - n` characters stay fixed for the whole
batch, so the pseudohash work over that fixed head is computed once and reused for every seed in
the batch. **Sequential is the only mode that gets this** — provider seeds arrive in whatever
order the provider yields, share no fixed head, and re-derive from scratch. That is the entire
reason sequential is faster, and it is why the default lives here.

`WithBatchCharacterCount(n)` sets that varying tail length, and it governs two things at once:

- **Cache reuse** — `35^n` seeds per cached head.
- **Interop and reporting rate** — one batch is one progress report, and on the browser head one
  `await Task.Delay(1)` yield to the event loop. Small `n` means the head spends its time
  crossing the boundary instead of searching. Tune it up.

`WithProviderBatchSeedCount(n)` is the provider-mode equivalent throttle — SIMD width stays at
`MotelyGlobals.MaxVectorWidth`; that knob only amortizes interop and chatter over a long
provider stream. Default is 35³.

`CliSearchMode` reaches sequential whenever no explicit seed input was named, and a JAML `seeds:`
block does not override it — that block is saved output, not an instruction.

Provider mode is for when the caller names the seeds: a list, a keyword space, an aesthetic
space, a lake, or random.

## Seed input — one door, two conveniences

Read the bodies before describing them; they chain:

```csharp
WithSeedList(string[] seeds)        => WithSeedGenerator(seeds, seeds.Length);
WithSeedGenerator(seeds, seedCount) => WithProviderSearch(new MotelySeedListProvider(seeds, seedCount));
WithProviderSearch(provider)        { SeedProvider = provider; Mode = Provider; }
```

`WithProviderSearch` is the door. The other two wrap `MotelySeedListProvider`.

- `WithSeedList` supplies `seeds.Length`, so it needs an already-materialized array.
- `WithSeedGenerator` takes a lazy sequence and the count the caller states, because counting a
  lazy sequence consumes it. Native only — it left `IMotelySearchSettings` for the WASM boundary.
- `WithProviderSearch` takes any `IMotelySeedProvider`: the DuckDB/CSV lake, aesthetic, keyword,
  random. `MotelySeedListProvider.SeedCount` resolves to `-1` when the sequence is lazy and no
  count was given.

Swapping a `WithSeedGenerator` call to `WithSeedList` is safe only when the argument is already
an array with an exact count. Check the argument, per site.

## Engine law

- One grammar: JAML text → `JamlConfig` → `JamlSearchBuilder.CreateSettings`. Clause families
  live on FilterDescs; `JamlSchema` is the generated index.
- A search feature is whatever `IMotelySearchSettings` can express. A CLI flag with no `With*`
  behind it is host-local and every other head is blind to it.
- The collect prepass lives in `MotelyAestheticCollect` so CLI and WASM run one algorithm.

## Prose

State what the code does. A comment that says a value is "real" implies some other value is
fake; a comment explaining absent code documents a decision that already happened. Instructions
phrased as prohibitions prime the thing they forbid — say the shape you want.

---

# PINNED: Bootsharp documentation (verbatim)

Copied from `D:\bootsharp\docs` so it is read, not looked up.


<!-- ===== /d/bootsharp/docs/guide/index.md ===== -->

# Introduction

## What?

Bootsharp is a solution for building web applications where the domain logic is authored in .NET C# and consumed by a standalone JavaScript or TypeScript project.

## Why?

C# is a popular choice for building maintainable software with complex domain logic, especially in enterprise and financial systems. However, its frontend capabilities are limited—particularly when compared to what the web ecosystem offers.

The web platform is the industry standard for modern UI development. Frameworks such as [React](https://react.dev) and [Svelte](https://svelte.dev) provide exceptional tooling, fast iteration, and a vast ecosystem, enabling developers to build high-quality interfaces with ease.

Solutions like [Blazor](https://dotnet.microsoft.com/en-us/apps/aspnet/web-apps/blazor) attempt to bring the entire web platform into .NET, effectively reversing the natural workflow and restricting access to native JavaScript tools. Bootsharp takes the opposite approach: it enables high-level interoperation between C# and TypeScript, so each layer can be developed within its optimal environment.

With Bootsharp, you implement domain logic in C# and build the UI using familiar web technologies — the interop layer is generated automatically with zero manual authoring. Your project can then be published to the web or bundled as a native desktop or mobile application using [Electron](https://electronjs.org) or [Tauri](https://tauri.app).

## How?

Bootsharp is installed as a [NuGet package](https://www.nuget.org/packages/Bootsharp) into the C# project dedicated to building the solution for the web. It is specifically designed not to "leak" the dependency outside the entry assembly of the web target—essential for keeping the domain clean of any platform-specific details.

While it's possible to author both export (C# → JS) and import (C# ← JS) bindings via static methods, complex solutions benefit from interface-based interop. Simply provide Bootsharp with C# interfaces describing the export and import API surfaces, and it will automatically generate the associated bindings and type declarations.

![](/img/banner.png)

Bootsharp will automatically build and bundle the JavaScript package when publishing the C# solution, and generate a `package.json`, allowing you to reference the entire C# solution as any other ES module in your web project.

::: code-group
```jsonc [package.json]
"scripts": {
    // Compile C# solution into ES module.
    "compile": "dotnet publish backend"
},
"dependencies": {
    // Reference C# solution module.
    "backend": "file:backend"
}
```
:::

::: code-group
```ts [main.ts]
// Import C# solution module.
import bootsharp, { Backend, Frontend } from "backend";

// Boot C# WASM module.
await bootsharp.boot();

// Subscribe to C# event.
Frontend.onUserChanged.subscribe(updateUserUI);

// Invoke C# method.
Backend.addUser({ name: "Carl" });
```
:::

<!-- ===== /d/bootsharp/docs/guide/getting-started.md ===== -->

# Getting Started

## Configure C# Project

In `.csproj` file, set wasm runtime identifier and reference Bootsharp package:

```xml

<Project Sdk="Microsoft.NET.Sdk">

    <PropertyGroup>
        <TargetFramework>net10.0</TargetFramework>
        <RuntimeIdentifier>browser-wasm</RuntimeIdentifier>
    </PropertyGroup>

    <ItemGroup>
        <PackageReference Include="Bootsharp" Version="*-*"/>
    </ItemGroup>

</Project>
```

## Author Interop APIs

Specify interop surface in the C# project.

```cs
using System;
using Bootsharp;

public static partial class Program
{
    [Export] // Used in JS as Program.onMainInvoked.subscribe(..)
    public static event Action<string>? OnMainInvoked;

    public static void Main ()
    {
        OnMainInvoked?.Invoke($"Hello {GetFrontendName()}, .NET here!");
    }

    [Import] // Set in JS as Program.getFrontendName = () => ..
    public static partial string GetFrontendName ();

    [Export] // Invoked from JS as Program.GetBackendName()
    public static string GetBackendName () => Environment.Version;
}
```

::: info NOTE
Authoring interop via static methods is impractical for large API surfaces—it's shown here only as a simple way to get started. For real projects, consider using [modules](/guide/interop-modules) instead.
:::

## Compile ES Module

Run following command under the solution root:

```sh
dotnet publish
```

— which will produce a `bin/bootsharp` directory with the compiled module and a `package.json` next to the `.csproj`.

::: tip
When publishing in `Release` (default for `dotnet publish`), Bootsharp automatically enables the [NativeAOT-LLVM](/guide/llvm) compiler, speed-focused WASM optimization, aggressive trimming, and an extra Binaryen pass when `wasm-opt` is available.

Use the debug configuration (`dotnet publish -c Debug`) to disable optimizations and use the default .NET compiler for a better debugging experience and faster build times, at the cost of significantly increased bundle size and degraded runtime performance.
:::

## Consume C# APIs in JavaScript

Import the compiled ES module, assign imported functions, boot the runtime and use exported methods:

::: code-group

```js [JavaScript Runtime (Node, Deno, Bun)]
// Importing compiled ES module.
import bootsharp, { Program } from "./bin/bootsharp/index.mjs";

// Binding 'Program.GetFrontendName' import invoked in C#.
Program.getFrontendName = () => process.version;

// Subscribing to 'Program.OnMainInvoked' C# event.
Program.onMainInvoked.subscribe(console.log);

// Initializing dotnet runtime and invoking entry point.
await bootsharp.boot();

// Invoking 'Program.GetBackendName' C# method.
console.log(`Hello ${Program.getBackendName()}!`);
```

```html [Web Browser]
<!DOCTYPE html>

<script type="module">

    // Importing compiled ES module.
    import bootsharp, { Program } from "./bin/bootsharp/index.mjs";

    // Binding 'Program.GetFrontendName' import invoked in C#.
    Program.getFrontendName = () => "Browser";

    // Subscribing to 'Program.OnMainInvoked' C# event.
    Program.onMainInvoked.subscribe(console.log);

    // Initializing dotnet runtime and invoking entry point.
    await bootsharp.boot();

    // Invoking 'Program.GetBackendName' C# method.
    console.log(`Hello ${Program.getBackendName()}!`);

</script>
```

:::

## Run the App

Assuming the above code is in `main.mjs` file for JavaScript runtimes or in `index.html` file for browser, run the following to test the app:

::: code-group

```sh [Node]
node main.mjs
```

```sh [Deno]
deno run main.mjs
```

```sh [Bun]
bun main.mjs
```

```sh [Browser]
npx serve
```

:::

::: tip EXAMPLE
Find full sources of the minimal sample on GitHub: https://github.com/elringus/bootsharp/tree/main/samples/minimal.
:::

<!-- ===== /d/bootsharp/docs/guide/build-config.md ===== -->

# Build Configuration

Build and publish related options are configured in `.csproj` file via MSBuild properties.

| Property                   | Default          | Description                                                                       |
|----------------------------|------------------|-----------------------------------------------------------------------------------|
| BootsharpName              | bootsharp        | Name of the generated JavaScript package and WASM binary. |
| BootsharpPublishDirectory  | /bin/bootsharp   | Directory to publish generated JavaScript module.                                 |
| BootsharpBinariesDirectory | (empty)          | Directory to publish binaries; when empty, binaries are embedded (see [Sideloading](sideloading)). |
| BootsharpPackageDirectory  | project-dir      | Directory to publish `package.json` file.                                         |

Below is an example configuration, which will make Bootsharp name the compiled module "backend" (instead of the default "bootsharp"), publish the `package.json` under the solution directory root and emit the runtime binaries into a "public/bin" directory one level above the solution root:

```xml
<Project Sdk="Microsoft.NET.Sdk">

    <PropertyGroup>
        <TargetFramework>net10.0</TargetFramework>
        <RuntimeIdentifier>browser-wasm</RuntimeIdentifier>
        <BootsharpName>backend</BootsharpName>
        <BootsharpPackageDirectory>$(SolutionDir)</BootsharpPackageDirectory>
        <BootsharpBinariesDirectory>$(SolutionDir)../public/bin</BootsharpBinariesDirectory>
    </PropertyGroup>

    <ItemGroup>
        <PackageReference Include="Bootsharp" Version="*-*"/>
    </ItemGroup>

</Project>
```

## Globalization

By default, Bootsharp disables .NET globalization on WASM. This keeps the published output smaller, but culture-specific formatting and culture construction will use invariant mode.

To enable globalization, explicitly disable invariant globalization in your project file:

```xml
<PropertyGroup>
    <InvariantGlobalization>false</InvariantGlobalization>
</PropertyGroup>
```

When invariant globalization is disabled, Bootsharp will automatically include the ICU files emitted by the .NET WASM build and configure the runtime accordingly.

Bootsharp supports the following globalization modes:

| Mode    | How to enable                                                        | Behavior                                                                                  |
|---------|----------------------------------------------------------------------|-------------------------------------------------------------------------------------------|
| Sharded | Didable `InvariantGlobalization`                                     | Publishes the default sharded ICU files (`icudt_*.dat`).                                  |
| Full    | Didable `InvariantGlobalization` and enable `WasmIncludeFullIcuData` | Publishes the full ICU data file (`icudt.dat`) and supports many cultures in one runtime. |

<!-- ===== /d/bootsharp/docs/guide/declarations.md ===== -->

# Type Declarations

Bootsharp will automatically generate [type declarations](https://www.typescriptlang.org/docs/handbook/2/type-declarations) for interop APIs when building the solution. One `.g.d.mts` file is emitted per C# namespace, colocated with the matching `.g.mjs` binding under the `generated/modules` directory of the compiled module package.

## Function Declarations

For interop methods, function declarations are emitted under the class's TS namespace wrapper inside the C# namespace's module:

```csharp
public class Class
{
    [Export]
    public static void Baz() { }
}
```

— will make the following emitted in `generated/modules/index.g.d.mts`:

```ts
export namespace Class {
    export function baz(): void;
}
```

— which allows consuming the API in JavaScript as:

```ts
import { Class } from "bootsharp";

Class.baz();
```

Imported methods will be emitted as properties, which have to be assigned before booting the runtime:

::: code-group

```csharp [Class.cs]
public partial class Class
{
    [Import]
    public static partial void Baz();
}
```

```ts [index.g.d.mts]
export namespace Class {
    export let baz: () => void;
}
```

```ts [main.ts]
import { Class } from "bootsharp";

Class.baz = () => {};
```

:::

## Overloaded Methods

JavaScript does not have function overloads, so Bootsharp automatically disambiguates them when projecting overloaded C# methods. The overload with the fewest parameters keeps the original name; the rest are suffixed with `With...` derived from the extra parameter names (or, when that is still ambiguous, from the full parameter names or parameter types).

::: code-group

```csharp [Class.cs]
public class Class
{
    [Export] public static void Start (string title) {}
    [Export] public static void Start (string title, string info) {}
    [Export] public static void Start (string title, double progress) {}
    [Export] public static void Start (string title, string info, double progress) {}
}
```

```ts [index.g.d.mts]
export namespace Class {
    export function start(title: string): void;
    export function startWithInfo(title: string, info: string): void;
    export function startWithProgress(title: string, progress: number): void;
    export function startWithInfoAndProgress(title: string, info: string, progress: number): void;
}
```

:::

## Generic Methods

JavaScript does not have generic functions, so Bootsharp projects an exported generic method as a concrete overload for each user type that satisfies the method's type parameter constraint, suffixing each with `Of...` derived from the bound type's name. Only a single type parameter, constrained to a user type, is expanded.

::: code-group

```csharp [Class.cs]
public interface IShape {}
public class Circle : IShape {}
public class Square : IShape {}

public class Class
{
    [Export]
    public static T CreateShape<T> () where T : IShape
    {
        if (typeof(T) == typeof(Circle)) return new Circle();
        if (typeof(T) == typeof(Square)) return new Square();
    }
}
```

```ts [index.g.d.mts]
export interface Circle {}
export interface Square {}

export namespace Class {
    export function createShapeOfCircle(): Circle;
    export function createShapeOfSquare(): Square;
}
```

:::

## Default Arguments

C# method parameters with default values are emitted as optional TypeScript parameters using the `?:` syntax, letting callers omit them at the call site:

::: code-group

```csharp [Class.cs]
public class Class
{
    [Export]
    public static void Greet (string name, string greeting = "Hello") {}
}
```

```ts [index.g.d.mts]
export namespace Class {
    export function greet(name: string, greeting?: string): void;
}
```

```ts [main.ts]
import { Class } from "bootsharp";

Class.greet("World");
Class.greet("World", "Hi");
```

:::

## Property Declarations

Exported properties are emitted as variables under the declaring class's TS namespace:

::: code-group

```csharp [Class.cs]
public class Class
{
    [Export]
    public static string Baz { get; set; } = "";
}
```

```ts [index.g.d.mts]
export namespace Class {
    export let baz: string;
}
```

```ts [main.ts]
import { Class } from "bootsharp";

Class.baz = "updated";
```

:::

Imported properties are emitted as accessor pairs, which have to be assigned before booting the runtime:

::: code-group

```csharp [Class.cs]
public static partial class Class
{
    [Import]
    public static partial string Baz { get; set; }
}
```

```ts [index.g.d.mts]
export namespace Class {
    export let baz: { get: () => string; set: (value: string) => void };
}
```

```ts [main.ts]
import { Class } from "bootsharp";

let baz = "";
Class.baz = { get: () => baz, set: value => baz = value };
```

:::

## Event Declarations

Exported events are emitted as `EventSubscriber` objects:

::: code-group

```csharp [Class.cs]
public class Class
{
    [Export]
    public static event Action<string>? OnBaz;
}
```

```ts [index.g.d.mts]
export namespace Class {
    export const onBaz: EventSubscriber<[payload: string]>;
}
```

```ts [main.ts]
import { Class } from "bootsharp";

Class.onBaz.subscribe(payload => {});
```

:::

Imported events are emitted as `EventBroadcaster` objects:

::: code-group

```csharp [Class.cs]
public static partial class Class
{
    [Import]
    public static event Action<string>? OnBaz;
}
```

```ts [index.g.d.mts]
export namespace Class {
    export const onBaz: EventBroadcaster<[payload: string]>;
}
```

```ts [main.ts]
import { Class } from "bootsharp";

Class.onBaz.broadcast("updated");
```

:::

## Delegate Declarations

Custom delegates are emitted as TypeScript function-type aliases:

::: code-group

```csharp [Class.cs]
public delegate void Notify (string msg);

public class Class
{
    [Export]
    public static Notify GetNotify () => msg => Console.WriteLine(msg);
}
```

```ts [index.g.d.mts]
export type Notify = (msg: string) => void;

export namespace Class {
    export function getNotify(): Notify;
}
```

```ts [main.ts]
import { Class } from "bootsharp";

const notify = Class.getNotify();
notify("hello");
```

:::

Built-in `System.Action` and `System.Func` variants are supported as well:

::: code-group

```csharp [Class.cs]
public class Class
{
    [Export] public static Action<string>? Logger { get; set; }
}
```

```ts [index.g.d.mts]
export namespace Class {
    export let logger: system.Action<string> | undefined;
}
```

```ts [main.ts]
import { Class } from "bootsharp";

Class.logger = msg => console.log(msg);
Class.logger("hello");
```

:::

## Documentation Declarations

When an inspected assembly has XML documentation generated, Bootsharp mirrors the matching documentation into the emitted TypeScript declarations.

::: code-group

```csharp [MathApi.cs]
/// <summary>Math API.</summary>
public class MathApi
{
    /// <summary>Adds two numbers.</summary>
    /// <param name="left">Left number.</param>
    /// <param name="right">Right number.</param>
    /// <returns>The sum.</returns>
    [Export]
    public static int Add (int left, int right) => left + right;
}
```

```ts [index.g.d.mts]
/**
 * Math API.
 */
export namespace MathApi {
    /**
     * Adds two numbers.
     * @param left Left number.
     * @param right Right number.
     * @returns The sum.
     */
    export function add(left: number, right: number): number;
}
```

:::

## Nullability

Bootsharp uses different TypeScript nullish forms depending on where a nullable C# value appears:

- nullable method arguments become `| undefined`
- nullable properties become optional with `?`
- nullable return values become `| null`
- nullable collection elements and dictionary values become `| null`

This is intentional and optimized for TypeScript ergonomics: `undefined` fits omitted or optional inputs, while `null` fits explicit data crossing the interop boundary.

## Namespaces

Members declared inside a C# namespace are emitted into a module path derived from that namespace: dots become path separators and casing is lower-kebab-cased. Members without a namespace land in the default `index` module (as shown in the examples above).

::: code-group

```csharp [Class.cs]
namespace Foo.Bar;

public class Class
{
    [Export]
    public static void Baz () { }
}
```

```ts [main.ts]
import { Class } from "bootsharp/foo/bar";

Class.baz();
```

:::

You can control how the C#-side namespace and type names resolve to the generated module and node names with the [rename attributes](/guide/renaming).

## Configuring Type Mappings

You can rename or omit the JavaScript node generated for an associated C# type via the [rename attributes](/guide/renaming).

<!-- ===== /d/bootsharp/docs/guide/serialization.md ===== -->

# Serialization

Most simple types, such as numbers, booleans, strings, arrays (lists) and promises (tasks) of them are marshalled in-memory when crossing the C# <-> JavaScript boundary. Below are some of the natively-supported types (refer to .NET docs for the [full list](https://learn.microsoft.com/en-us/aspnet/core/blazor/javascript-interoperability/import-export-interop)):

| C#       | JavaScript | Task of | Array of |
|----------|------------|:-------:|:--------:|
| bool     | boolean    |   ✔️    |    ❌     |
| byte     | number     |   ✔️    |    ✔️    |
| char     | string     |   ✔️    |    ❌     |
| string   | string     |   ✔️    |    ✔️    |
| int      | number     |   ✔️    |    ✔️    |
| long     | BigInt     |   ✔️    |    ❌     |
| float    | Number     |   ✔️    |    ❌     |
| DateTime | Date       |   ✔️    |    ❌     |

When a value of non-natively supported type is specified in an interop API, Bootsharp will de-/serialize it using a custom efficient binary serialization format. The whole process is encapsulated under the hood on both the C# and JavaScript sides, so you don't have to manually author generator hints or specify `[MarshallAs]` attributes for each value:

```csharp
public record User (long Id, string Name, DateTime Registered);

[Export]
public static void AddUser (User user) { }

[Export]
public static event Action<User>? OnUserModified;
```

— Bootsharp will automatically emit C# and JavaScript code required to de-/serialize `User` record on both ends, so that you can consume the APIs as if they were initially authored in JavaScript:

```ts
import { Program } from "bootsharp";

Program.addUser({ id: 17, name: "Carl", registered: Date.now() });

Program.onUserModified.subscribe(handleUserModified);

function handleUserModified(user: Program.User) { }
```

::: info NOTE
Only types with immutable semantics (structs, records, and read-only collections) are subject to serialization—other types are considered mutable and are passed by reference as [interop instances](/guide/interop-instances).
:::

## Enums Serialization

Enums are marshalled as numbers for better performance, while additional name <-> index mappings are emitted on the JavaScript side for convenience.

```csharp
public enum Options { Foo, Bar }

[Export]
public static Options GetOption () => Options.Bar;
```

— while "GetOptions" return value will be passed to JavaScript as an integer index, Bootsharp will map enum indexes to string values (and vice-versa) in the emitted code, so that following will work as expected:

```ts
import { Program } from "bootsharp";

const option = Program.getOption();
console.log(option === Program.Options.Foo); // false
console.log(option === Program.Options.Bar); // true
console.log(Program.Options[Program.Options.Foo]); // "Foo"
console.log(Program.Options[1]); // "Bar"
```

## Dictionary Serialization

Bootsharp marshals C# dictionaries as ES6 [Map](https://developer.mozilla.org/en-US/docs/Web/JavaScript/Reference/Global_Objects/Map):

```csharp
[Export]
public static Dictionary<string, bool> GetMap () =>
    new () { ["foo"] = true, ["bar"] = false };
```

— the dictionary can be accessed with standard `Map` APIs:

```ts
import { Program } from "bootsharp";

const map = Program.getMap();
console.log(map.get("foo")); // true
console.log(map.get("bar")); // false
```

## Collection Interfaces

It's common to use various collection interfaces, such as `IReadOnlyList` or `IReadOnlyDictionary` when authoring C# APIs. Bootsharp will accept any kind of array or dictionary compatible interface in the interop APIs and marshal them as plain arrays and maps by default:

```csharp
[Export]
public static IReadOnlyDictionary<string, float> Map (
    IReadOnlyList<string> a, IReadOnlyCollection<float> b) { }
```

```ts
import { Program } from "bootsharp";

const map = Program.map(["foo", "bar"], [0, 7]);
console.log(map.get("bar")); // 7
```

<!-- ===== /d/bootsharp/docs/guide/specialization.md ===== -->

# Specialization

Bootsharp marshals every type automatically based on the convention: types with immutable/value semantics are [serialized by value](/guide/serialization), and others are [passed by reference](/guide/interop-instances).

It's possible to customize the behaviour with **specialization** are redefine how a particular CLR type crosses the interop boundary and what surface it exposes on the other side.

## How It Works

A specialization is a pair of classes describing a custom interop surface for a specific CLR type — one for each direction:

- **Export** (C# → JS) — a class annotated with `[SpecializeExport(typeof(T))]` and inherited from `SpecializedExport`. Bootsharp wraps an exported instance of the specialized type into this class before it crosses to JavaScript.
- **Import** (JS → C#) — an abstract class annotated with `[SpecializeImport(typeof(T))]` and inherited from `SpecializedImport`. Bootsharp uses it as the base of the generated interop proxy and treats its abstract members as the interop surface wired to JavaScript.

The two halves are paired: the export half implements every abstract member declared on the import half, so the same shape is exposed in both directions.

To override how Bootsharp marshals a type declare a specialization pair with the same attributes. Here's an example for `IComparer<T>`:

```csharp
[SpecializeImport(typeof(IComparer<>))]
public abstract class ComparerImport<T> (int id)
    : SpecializedImport(id), IComparer<T>
{
    public abstract int Compare (T? x, T? y);
}

[SpecializeExport(typeof(IComparer<>))]
public class ComparerExport<T> (IComparer<T> cmp)
    : SpecializedExport(cmp)
{
    public int Compare (T? x, T? y) => cmp.Compare(x, y);
}
```

The import half declares the interop surface (`Compare`) as abstract members; Bootsharp generates a proxy that forwards them to JavaScript and, since the class implements `IComparer<T>`, the proxy is usable as one on the C# side.

On the JavaScript side a comparer is just an object matching the declared surface:

```ts
import { Program } from "bootsharp";

Program.provideComparer = () => ({
    compare: (x, y) => x < y ? -1 : x > y ? 1 : 0
});

const comparer = Program.getComparer();
comparer.compare("a", "b"); // -1
```

::: tip
When the specialized `Clr` type is a class, it will also affect (specialize) any subclasses discovered on the interop surfaces.
:::

## Injecting Code

The `[SpecializeImport]` attribute accepts optional `CS`, `JS`, `JSCtor` and `Decl` snippets that are spliced verbatim into the generated C# or JavaScript proxies and its TypeScript declaration. This lets the imported proxy satisfy JS-side contracts that aren't expressible through the C# abstract members alone — for example, injecting an iterator:

```csharp
[SpecializeImport(typeof(ICustomCollection<>),
    JS: "[Symbol.iterator]() { return this.copy()[Symbol.iterator](); }",
    Decl: "[Symbol.iterator](): IterableIterator<T>;")]
```

The `CS` snippet can contain `$full` markers — they will be replaced with the fully-qualified type name of the specialized instance. This allows referencing the concrete specialized instances in the proxy when the specialization is applied to a base class.

The `Decl` snippet can contain `$full`, `$name` and `$T{I}` markers — first is the same as CS one, but in TypeScript context, name is the short type name and `T` is the fully-qualified name of the generic type argument with the `{I}` inde (if any), for example `$T{0}` is replaced with the first generic argument.

When `Decl` value starts with `export ` — the content will replace the entire TypeScript declaration of the type, instead of splicing it into the bottom of the default type declaration.

::: tip EXAMPLE
Find a more advanced example of injecting C# and JS constructor code to synthesise property events in the [E2E test project](https://github.com/elringus/bootsharp/tree/main/src/js/test/cs/Test.Library/Specialization.cs).
:::

## Unwrapping

An import specializer normally *is* the value handed to C# — the generated proxy implements the specialized interface, so it can stand in for it directly (the way `ComparerImport<T>` above serves as an `IComparer<T>`).

That doesn't work when the specialized type can't be implemented by a proxy, such as a value type like `CancellationToken`. In that case the proxy exposes the JavaScript-side surface as abstract members and overrides `SpecializedImport.Unwrap()` to build the concrete value from them:

```csharp
[SpecializeImport(typeof(CancellationToken))]
public abstract class CancellationTokenImport (int id) : SpecializedImport(id)
{
    public abstract bool IsCancellationRequested { get; }
    public abstract event Action OnCancellationRequested;

    private CancellationTokenSource? src;

    protected internal override object Unwrap ()
    {
        if (src != null) return src.Token;
        src = new();
        if (IsCancellationRequested) src.Cancel();
        else OnCancellationRequested += src.Cancel;
        return src.Token;
    }
}

[SpecializeExport(typeof(CancellationToken))]
public sealed class CancellationTokenExport : SpecializedExport
{
    public bool IsCancellationRequested => ct.IsCancellationRequested;
    public event Action? OnCancellationRequested;

    private readonly CancellationToken ct;

    public CancellationTokenExport (CancellationToken ct) : base(ct)
    {
        this.ct = ct;
        ct.Register(() => OnCancellationRequested?.Invoke());
    }
}
```

Bootsharp calls `Unwrap()` to obtain the value passed to C# — here a real `CancellationToken` backed by a source that's cancelled whenever the JavaScript token reports cancellation. The paired JavaScript class signals through the same surface:

```ts
import { CancellationToken } from "bootsharp";

const token = new CancellationToken();
token.cancel(); // fires onCancellationRequested
```

## Reference

The built-in specializations live in [`Specialized.cs`](https://github.com/elringus/bootsharp/blob/main/src/cs/Bootsharp.Common/Specialization/Specialized.cs); their JavaScript counterparts are the modules under [`src/js/src/bcl`](https://github.com/elringus/bootsharp/tree/main/src/js/src/bcl). Use them as a template when authoring your own.

<!-- ===== /d/bootsharp/docs/guide/interop-modules.md ===== -->

# Interop Modules

Instead of manually authoring a binding for each member, let Bootsharp generate them automatically using the `[Import]` and `[Export]` assembly attributes. The type listed under each attribute defines an *interop module*.

For example, say we have a JavaScript UI (frontend) with a setting stored on the JS side, and a C# domain layer (backend) that wants to expose state changes back to JavaScript. You can describe the imported frontend module like this:

```csharp
interface IFrontend
{
    bool IsMuted { get; set; }
}
```

Now, add the module type to the JS import list:

```csharp
[assembly: Import(typeof(IFrontend))]
```

Bootsharp will automatically implement the interface in C#, wiring it to JavaScript, while also providing you with a TypeScript spec to implement on the frontend:

```ts
export namespace Frontend {
    export let isMuted: boolean;
}
```

Imported modules must be interfaces, since Bootsharp generates the C# implementation that calls into JavaScript.

Now, define the backend contract to expose to JavaScript. An exported module can be either an interface or a non-static class — pick whichever fits your backend best:

```csharp
public interface IBackend
{
    event Action<Data> OnDataChanged;
    Data? Current { get; set; }
    void AddData (Data data);
}
```

```csharp
public class Backend
{
    public event Action<Data>? OnDataChanged;
    public Data? Current { get; set; }
    public void AddData (Data data) { /* ... */ }
}
```

Export the module to JavaScript:

```csharp
[assembly: Export(typeof(IBackend))]
// or
[assembly: Export(typeof(Backend))]
```

Either form produces the following spec to be consumed on the JavaScript side:

```ts
export namespace Backend {
    export const onDataChanged: EventSubscriber<[data: Data]>;
    export let current: Data | undefined;
    export function addData(data: Data): void;
}
```

Imported module events work the other way around: declare a real C# event on the interface, and Bootsharp will generate a JavaScript `EventBroadcaster` plus a regular subscribable event on the generated C# implementation.

To make Bootsharp automatically inject and initialize the generated interop implementations, use the [dependency injection](/guide/extensions/dependency-injection) extension.

::: tip Example
Find an example of using modules in the [React sample](https://github.com/elringus/bootsharp/tree/main/samples/react).
:::

<!-- ===== /d/bootsharp/docs/guide/interop-instances.md ===== -->

# Interop Instances

When a type with a mutable semantic (a class or an interface) appears on the interop boundary or under a [serialized type](/guide/serialization), instead of serializing and copying it by value, Bootsharp will instead generate an instance binding and pass it by reference, eg:

```csharp
public interface IExported
{
    string Value { get; set; }
    string GetFromCSharp ();
}

public interface IImported
{
    string Value { get; set; }
    string GetFromJavaScript ();
}

public class Exported : IExported
{
    public string Value { get; set; } = "cs";
    public string GetFromCSharp () => "cs";
}

public static partial class Factory
{
    [Export] public static IExported GetExported () => new Exported();
    [Import] public static partial IImported GetImported ();
}

var imported = Factory.GetImported();
imported.GetFromJavaScript(); // returns "js"
imported.value = "updated"; // invokes the JS setter
_ = imported.value; // invokes the JS getter
```

```ts
import { Factory, IImported } from "bootsharp";

class Imported implements IImported {
    value = "js";
    getFromJavaScript() { return "js"; }
}

Factory.getImported = () => new Imported();

const exported = Factory.getExported();
exported.getFromCSharp(); // returns "cs"
exported.value = "updated"; // invokes the C# setter
_ = exported.value; // invokes the C# getter
```

::: info NOTE
Only user types are subject to instance binding. BCL types are ignored to prevent leaking the entire .NET runtime into the generated interop layer.
:::

<!-- ===== /d/bootsharp/docs/guide/renaming.md ===== -->

# Renaming

By default, Bootsharp derives JavaScript names from your C# types: a type's namespace becomes the module path, the type name becomes the node (object) under that module, and members are `camelCased`.

It's possible to customize the behaviour by specifying static methods annotated with `[RenameModule]`, `[RenameNode]` and `[RenameMember]` attributes. The methods receive a CLR type associated with the renamed artifact plus the default name generated by Bootsharp and expected to return the custom name you want to use.

## Module

`[RenameModule]` customizes the module path that groups the generated bindings and declarations. The default is the slugified C# namespace, or `index` for global types. Returning an empty, null or whitespace string falls back to the default `index` module.

```cs
[RenameModule]
public static string RenameModule (Type type, string @default) =>
    @default.Replace("/foo/bar", "/foo");
```

In the example above we fold `/foo/bar` modules into the `/foo` module.

## Node

`[RenameNode]` customizes the node — the object representing a C# type under its module. The default is the reflected type name. Returning an empty, null or whitespace string **erases** the type, omitting it from the generated JavaScript.

```cs
[RenameNode]
public static string RenameNode (Type type, string @default)
{
    if (type.Name == "Foo") return null;
    if (type.IsInterface && @default.EndsWith("UI")) return @default[1..^2];
    if (type.IsInterface && @default.StartsWith('I')) return @default[1..];
    return @default;
}
```

The example removes `Foo` types from the interop surface, strips the leading `I` from interface names and drops a trailing `UI` suffix when present.

## Member

`[RenameMember]` customizes member names — the methods, properties and events projected on an interop surface. The default is the `camelCased` (and disambiguated) member name. Returning an empty, null or whitespace string **erases** the member, omitting it from the generated JavaScript.

```cs
[RenameMember]
public static string RenameMember (MemberInfo info, string @default)
{
    if (info.DeclaringType.Name == "Foo")
        if (info is EventInfo) return null;
        else return char.ToUpperInvariant(@default[0]) + @default[0..];
    return @default;
}
```

Here we drop all events and rename other members declared under `Foo` to `PascalCase`.

## Combining Renamers

Define any combination of the three renamers — each is optional and resolved independently. The example below groups everything under an `api` module, removes the interface prefixes and drops properties from all surfaces:

```cs
[RenameModule]
public static string Module (Type type, string @default) => "api";

[RenameNode]
public static string Node (Type type, string @default) =>
    type.IsInterface ? @default[1..] : @default;

[RenameMember]
public static string Member (MemberInfo info, string @default) =>
    info is PropertyInfo ? null : @default;
```

<!-- ===== /d/bootsharp/docs/guide/sideloading.md ===== -->

# Sideloading Binaries

By default, Bootsharp embeds the .NET WASM runtime and the solution's assemblies into the generated JavaScript module as base64 strings. This is convenient — `bootsharp.boot()` works with no arguments and no extra files to serve. The trade-off is roughly 30% extra bundle size due to the base64 encoding.

To disable embedding, set `BootsharpBinariesDirectory` to the directory where the binaries should be published:

```xml
<PropertyGroup>
    <BootsharpBinariesDirectory>public</BootsharpBinariesDirectory>
</PropertyGroup>
```

The compiled WASM module, solution assemblies, ICU data and (in debug builds) debug symbols will be emitted to that directory as separate files instead of being inlined into the module. You then have two ways to feed them to `boot`:

Pass a root URL to fetch the resources from at runtime:

```ts
// Assuming the binaries are served from "/public" under the website root.
await bootsharp.boot("/public");
```

Or load the binaries yourself and pass them as a `BootResources` object:

```ts
import { readFileSync } from "node:fs";

const wasm = readFileSync("public/bootsharp.wasm");
await bootsharp.boot({ wasm });
```

This way the binary files can be streamed directly from the server, cached separately, or loaded from any source you control — useful for trimming initial bundle size or sharing the runtime across multiple modules.

::: tip EXAMPLE
Find sideloading example in the [trimming sample](https://github.com/elringus/bootsharp/blob/main/samples/trimming).
:::

<!-- ===== /d/bootsharp/docs/guide/llvm.md ===== -->

# NativeAOT-LLVM

Starting with v0.6.0 Bootsharp supports .NET's experimental [NativeAOT-LLVM](https://github.com/dotnet/runtimelab/tree/feature/NativeAOT-LLVM) backend.

By default, when targeting `browser-wasm`, .NET is using the Mono runtime, even when compiled in AOT mode. Compared to the modern NativeAOT (previously CoreRT) runtime, Mono's performance is lacking in speed, binary size and compilation times. NativeAOT-LLVM backend not only uses the modern runtime instead of Mono, but also optimizes it with the [LLVM](https://llvm.org) toolchain, further improving the performance.

Below is a benchmark comparing interop and compute performance of various languages and .NET versions compiled to WASM to give you a rough idea on the differences:

![](/img/llvm-bench.png)

— sources of the benchmark are here: https://github.com/elringus/bootsharp/tree/main/samples/bench.

## Setup

Starting with Bootsharp 0.8.0 no extra project configuration is required.

When publishing a Bootsharp project in `Release`, Bootsharp automatically enables the NativeAOT-LLVM toolchain, speed-focused code generation, and the trimming settings required by the LLVM backend.

## Binaryen

Bootsharp always tries to run Binaryen on release publishes with speed optimization enabled:

1. Install Binaryen: https://github.com/WebAssembly/binaryen/releases
2. Make sure `wasm-opt` is in the system path
3. If the tool is missing, Bootsharp will log a warning and continue with a non-fully-optimized WASM binary

<!-- ===== /d/bootsharp/docs/guide/extensions/dependency-injection.md ===== -->

# Dependency Injection

When using [modules](/guide/interop-modules), it's convenient to use a dependency injection mechanism to automatically route generated module implementations for the services that needs them.

Reference `Bootsharp.Inject` extension in the project configuration:

```xml
<Project Sdk="Microsoft.NET.Sdk">

    <PropertyGroup>
        <TargetFramework>net9.0</TargetFramework>
        <RuntimeIdentifier>browser-wasm</RuntimeIdentifier>
    </PropertyGroup>

    <ItemGroup>
        <PackageReference Include="Bootsharp" Version="*-*"/>
        <PackageReference Include="Bootsharp.Inject" Version="*-*"/>
        <PackageReference Include="Microsoft.Extensions.DependencyInjection" Version="*"/>
    </ItemGroup>

</Project>
```

— and use `AddBootsharp` extension method to inject the generated import implementations; `RunBootsharp` will initialize generated export implementation by requiring the handlers, which should be added to the services collection before.

```csharp
using Bootsharp;
using Bootsharp.Inject;
using Microsoft.Extensions.DependencyInjection;

[assembly: Export(
    typeof(IExported),
    // other APIs to export to JavaScript
)]

[assembly: Import(
    typeof(IImported),
    // other APIs to import from JavaScript
)]

new ServiceCollection()
    // Inject generated implementation of IImported.
    .AddBootsharp()
    // Inject other services, which may require IImported.
    .AddSingleton<SomeService>()
    // Provide handler for the exported interface.
    .AddSingleton<IExported, Exported>()
    // Build the collection.
    .BuildServiceProvider()
    // Initialize the exported implementations.
    .RunBootsharp();
```

`IImported` can now be requested via .NET's DI, while `IExported` APIs are available in JavaScript:

```csharp
public class SomeService (IImported imported) { }
```

```ts
import { Exported } from "bootsharp";
```

::: tip EXAMPLE
Find example on using the DI extension in the [React sample](https://github.com/elringus/bootsharp/blob/main/samples/react).
:::

<!-- ===== /d/bootsharp/docs/guide/extensions/file-system.md ===== -->

# File System

::: danger SPONSORS
This extension is exclusive for sponsors: https://github.com/sponsors/elringus.
:::

With the new [File System Access](https://developer.mozilla.org/en-US/docs/Web/API/File_System_API) APIs it's possible to access local file system directly from web browser. Bootsharp.FileSystem extension provides C# bindings and JavaScript package to use the APIs directly from C#.

Install the NuGet package to C# project:

```xml

<Project Sdk="Microsoft.NET.Sdk">

    <PropertyGroup>
        <TargetFramework>net9.0</TargetFramework>
        <RuntimeIdentifier>browser-wasm</RuntimeIdentifier>
    </PropertyGroup>

    <ItemGroup>
        <PackageReference Include="Bootsharp" Version="*-*"/>
        <PackageReference Include="Bootsharp.FileSystem" Version="*-*"/>
    </ItemGroup>

</Project>
```

And the NPM package to JavaScript project:

```json
{
    "dependencies": {
        "backend": "file:backed",
        "@rewaffle/bootsharp-file-system": "latest"
    }
}
```

Before booting C# solution in JavaScript, initialize the file system extension:

```ts
import bootsharp, { Bootsharp } from "backend";
import * as fs from "@rewaffle/bootsharp-file-system";

fs.init(Bootsharp.FileSystem.FileMounter);
await bootsharp.boot();
```

Then proceed to C# where `IFileMounter` interface will be automatically injected by the extension importing following APIs from JavaScript:

```csharp
interface IFileMounter
{
    Task<string?> PickRoot (PickOptions? options = null);
    Task<IFileSystem> Mount (string root, IFileWatcher watcher);
    Task Unmount (string root);
}
```

Invoking `PickRoot` method will prompt user to select root directory to mount. It will return unique root directory identifier to be used with `Mount` and `Unmount` methods or `null` in case user cancelled pick dialogue. Optional `PickOptions` argument allows specifying which directory pick dialogue should start in, whether write access should be requested, etc.

After user picked a directory and you get the root ID, invoke `Mount`, which will return `IFileSystem` instance, providing common IO interface over the contents of the mapped directory:

```csharp
interface IFileSystem
{
    Task CreateDirectory (string uri);
    Task RemoveDirectory (string uri);
    Task WriteFile (string uri, byte[] content);
    Task DeleteFile (string uri);
    Task<byte[]> ReadFile (string uri);
    Task<FileInfo> GetFileInfo (string uri);
}
```

File watcher instance specified when invoking `Mount` allows handling file changes under the mapped directory:

```csharp
interface IFileWatcher
{
    Task HandleFileChanges (FileChange[] changes);
}
```

— until the directory is un-mounted, the watcher will be notified when an entry (directory or file) is added, removed or modified.

::: tip EXAMPLE
Find sample application built with `Bootsharp.FileSystem` in the [sponsors repository](https://github.com/rewaffle/extra).
:::

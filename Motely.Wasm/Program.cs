using System.Reflection;
using Bootsharp;
using Motely;
using Motely.Filters;
using Motely.Filters.Jaml;

// One runtime, one search, one handler — the module's static handler is not a hazard on a
// single-threaded WASM head. Registering the interfaces as modules is what gives JS the named
// surface; the instance bindings Search() returns ride alongside it.
[assembly: Export(typeof(IMotelySearchSettings), typeof(IMotelySearch))]

namespace Motely.Wasm;

/// <summary>The static doors. Everything else is on <see cref="IMotelySearchSettings"/>.</summary>
public static partial class Engine
{
    /// <summary>Entry point.</summary>
    public static void Main() { }

    /// <summary>Batch progress: seeds searched, rate, elapsed.</summary>
    [Export]
    public static event Action<MotelyProgress>? OnProgress;

    /// <summary>A seed that passed every filter.</summary>
    [Export]
    public static event Action<string>? OnSeedMatch;

    /// <summary>A scored row: seed, total, per-clause tallies.</summary>
    [Export]
    public static event Action<MotelyScoredSeedResult>? OnScoredResult;

    /// <summary>JAML text to a config.</summary>
    [Export]
    public static JamlConfig Load(string jaml) => JamlConfigLoader.FromJaml(jaml);

    /// <summary>Null when the document loads clean; the loader's positioned error otherwise.</summary>
    [Export]
    public static string? Validate(string jaml) =>
        JamlConfigLoader.TryLoad(jaml, out _, out string? error) ? null : error ?? "Invalid JAML.";

    /// <summary>
    /// Settings for a config — the chain <c>Motely.CLI</c> builds, reporting bound to the events
    /// above, one thread because that is what a browser has.
    /// </summary>
    [Export]
    public static IMotelySearchSettings Search(JamlConfig config) =>
        JamlSearchBuilder
            .CreateSettings(config)
            .WithThreadCount(1)
            .WithProgressCallback(p => OnProgress?.Invoke(p))
            .WithSeedMatchCallback(s => OnSeedMatch?.Invoke(s))
            .WithScoredResultCallback(r => OnScoredResult?.Invoke(r));
}

/// <summary>Module and node naming. A renamer returning null omits the artifact.</summary>
public static class Naming
{
    /// <summary>Every namespace folds to the root module: <c>import { Engine } from "motely"</c>.</summary>
    [RenameModule]
    public static string Module(Type type, string @default) => "index";

    /// <summary>
    /// Filter composition. Their methods take <c>ref</c> parameters, which JavaScript has no
    /// form for; the generator emits <c>Type&amp;</c> and the compile fails. JAML carries the
    /// filter across instead — <see cref="JamlSearchBuilder.CreateSettings"/> has already
    /// installed every desc by the time JS holds the settings.
    /// </summary>
    private static readonly Type[] NativeOnlyTypes =
    [
        typeof(IMotelySeedFilterDesc),
        typeof(IMotelySeedFilter),
        typeof(IMotelySeedScoreDesc),
        typeof(IMotelySeedScoreProvider),
        typeof(IMotelySeedAnalyzeDesc),
        typeof(IMotelySeedAnalyzeProvider),
        typeof(IMotelySeedRouterDesc),
        typeof(IMotelySeedRouter),
    ];

    /// <summary>Erase <see cref="NativeOnlyTypes"/>; drop the <c>I</c> from every other interface.</summary>
    [RenameNode]
    public static string Node(Type type, string @default) =>
        Array.IndexOf(NativeOnlyTypes, type) >= 0 ? null!
        : type.IsInterface && @default.StartsWith('I') ? @default[1..]
        : @default;

    /// <summary>Members taking a <see cref="NativeOnlyTypes"/> entry.</summary>
    private static readonly string[] NativeOnlyMembers =
    [
        nameof(IMotelySearchSettings.WithAdditionalFilter),
        nameof(IMotelySearchSettings.WithSeedScoreProvider),
        nameof(IMotelySearchSettings.WithSeedAnalyzeProvider),
        nameof(IMotelySearchSettings.WithSeedRouter),
    ];

    /// <inheritdoc cref="NativeOnlyMembers" />
    [RenameMember]
    public static string Member(MemberInfo info, string @default) =>
        Array.IndexOf(NativeOnlyMembers, info.Name) >= 0 ? null! : @default;
}

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Motely;

namespace BalatroSeedOracle.Services;

/// <summary>
/// Compiles a user-authored C# Jimmolate predicate into a live
/// <see cref="MotelyIndividualSeedSearcher"/> delegate via Roslyn, in memory.
/// The predicate is the classic Immolate "does this seed pass?" filter, and it runs
/// compiled inside the engine — which is the whole reason it is fast.
///
/// Requires JIT: BalatroSeedOracle.Desktop publishes with PublishAot=false /
/// PublishTrimmed=false. If AOT is ever enabled for release builds, this feature
/// dies with it.
///
/// Untrusted source in → compiled code out is arbitrary code execution by design.
/// Fine for a local desktop tool the user drives; never expose over the MCP server
/// without a sandbox.
/// </summary>
public static class JimmolateCompiler
{
    /// <summary>Required entry point: <c>public static int Filter(MotelySingleSearchContext ctx)</c>.</summary>
    public const string MethodName = "Filter";

    public sealed class Result
    {
        public MotelyIndividualSeedSearcher? Predicate { get; init; }
        public IReadOnlyList<string> Errors { get; init; } = Array.Empty<string>();
        public string GeneratedSource { get; init; } = string.Empty;
        public bool Success => Predicate is not null;
    }

    private static readonly Regex FullSourcePattern = new(
        @"\b(class|struct|namespace)\b",
        RegexOptions.Compiled);

    private static readonly Lazy<IReadOnlyList<MetadataReference>> References = new(BuildReferences);

    /// <summary>
    /// Compile predicate source. Accepts either a bare method body
    /// (wrapped in a template automatically) or a full compilation unit that
    /// contains a public static <c>int Filter(MotelySingleSearchContext)</c>.
    /// Never throws on bad source — errors come back in <see cref="Result.Errors"/>.
    /// </summary>
    public static Result Compile(string source)
    {
        if (string.IsNullOrWhiteSpace(source))
            return new Result { Errors = ["Predicate source is empty."] };

        var fullSource = FullSourcePattern.IsMatch(source) ? source : WrapBody(source);

        var tree = CSharpSyntaxTree.ParseText(
            fullSource,
            new CSharpParseOptions(LanguageVersion.Preview));

        var compilation = CSharpCompilation.Create(
            "Jimmolate_" + Guid.NewGuid().ToString("N"),
            [tree],
            References.Value,
            new CSharpCompilationOptions(
                OutputKind.DynamicallyLinkedLibrary,
                optimizationLevel: OptimizationLevel.Release,
                allowUnsafe: true));

        using var ms = new MemoryStream();
        var emit = compilation.Emit(ms);
        if (!emit.Success)
        {
            var errors = emit.Diagnostics
                .Where(d => d.Severity == DiagnosticSeverity.Error)
                .Select(FormatDiagnostic)
                .ToList();
            return new Result { Errors = errors, GeneratedSource = fullSource };
        }

        var assembly = Assembly.Load(ms.ToArray());
        var method = FindPredicateMethod(assembly);
        if (method is null)
        {
            return new Result
            {
                Errors = [$"No public static method 'int {MethodName}(MotelySingleSearchContext)' found in the compiled source."],
                GeneratedSource = fullSource,
            };
        }

        var predicate = (MotelyIndividualSeedSearcher)method.CreateDelegate(typeof(MotelyIndividualSeedSearcher));
        return new Result { Predicate = predicate, GeneratedSource = fullSource };
    }

    // The #line directive makes Roslyn report error positions relative to the
    // user's body text, so the editor can highlight the right line.
    private static string WrapBody(string body) =>
        $$"""
        using System;
        using System.Linq;
        using Motely;

        public static class __UserJimmolate
        {
            public static int {{MethodName}}(MotelySingleSearchContext ctx)
            {
        #line 1
        {{body}}
        #line default
            }
        }
        """;

    private static string FormatDiagnostic(Diagnostic d)
    {
        var pos = d.Location.GetMappedLineSpan().StartLinePosition;
        return $"({pos.Line + 1},{pos.Character + 1}): {d.Id}: {d.GetMessage()}";
    }

    private static MethodInfo? FindPredicateMethod(Assembly assembly) =>
        assembly
            .GetTypes()
            .SelectMany(t => t.GetMethods(BindingFlags.Public | BindingFlags.Static))
            .FirstOrDefault(m =>
                m.Name == MethodName
                && m.ReturnType == typeof(int)
                && m.GetParameters() is [{ } p]
                && p.ParameterType == typeof(MotelySingleSearchContext));

    private static IReadOnlyList<MetadataReference> BuildReferences()
    {
        var paths = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        if (AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES") is string tpa)
            foreach (var path in tpa.Split(Path.PathSeparator, StringSplitOptions.RemoveEmptyEntries))
                paths.Add(path);

        // Motely itself may not be on the TPA list depending on host; make sure
        // the predicate can always see the engine.
        var motely = typeof(MotelyIndividualSeedSearcher).Assembly.Location;
        if (!string.IsNullOrEmpty(motely))
            paths.Add(motely);

        return paths.Select(p => (MetadataReference)MetadataReference.CreateFromFile(p)).ToList();
    }
}

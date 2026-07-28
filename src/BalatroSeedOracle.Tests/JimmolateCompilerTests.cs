using BalatroSeedOracle.Services;
using Motely;
using Motely.Enums;
using Motely.Filters.Native;
using Xunit;

namespace BalatroSeedOracle.Tests;

// JimmolateCompiler turns user C# source into a live MotelyIndividualSeedSearcher.
// These tests cover the three contract points: good source compiles to a working
// delegate, bad source comes back as diagnostics (never an exception), and the
// compiled delegate actually drives filtering end-to-end through the engine —
// the same harness shape as Motely.Tests.JimmolateFilterTests upstream.
public sealed class JimmolateCompilerTests
{
    private static readonly string[] Seeds = ["12345678", "UNITTEST", "1AAAAAAA", "ALEEBOOO"];

    private static (long Matching, List<string> Matched) RunWithJimmolate(
        MotelyIndividualSeedSearcher predicate
    )
    {
        var matched = new List<string>();
        var settings = new MotelySearchSettings<PassthroughFilterDesc.PassthroughFilter>(
            new PassthroughFilterDesc()
        )
            .WithDeck(MotelyDeck.Red)
            .WithStake(MotelyStake.White)
            .WithListSearch(Seeds, Seeds.Length)
            .WithThreadCount(1)
            .WithQuietMode(true)
            .WithJimmolate(predicate)
            .WithSeedMatchCallback(matched.Add);

        using var search = settings.Start();
        search.AwaitCompletion();
        return (search.MatchingSeeds, matched);
    }

    [Fact]
    public void Compile_BareBody_ReturnsWorkingDelegate()
    {
        var result = JimmolateCompiler.Compile("return 1;");

        Assert.True(result.Success);
        Assert.Empty(result.Errors);
        Assert.NotNull(result.Predicate);
    }

    [Fact]
    public void Compile_FullSource_FindsFilterMethod()
    {
        const string source = """
            using Motely;

            public static class MyPredicate
            {
                public static int Filter(MotelySingleSearchContext ctx) => 1;
            }
            """;

        var result = JimmolateCompiler.Compile(source);

        Assert.True(result.Success);
    }

    [Fact]
    public void Compile_SyntaxError_ReturnsDiagnosticsNotException()
    {
        var result = JimmolateCompiler.Compile("return 1 +;");

        Assert.False(result.Success);
        Assert.Null(result.Predicate);
        Assert.NotEmpty(result.Errors);
        // #line mapping: the error must point at line 1 of the user's body,
        // not somewhere inside the wrapper template.
        Assert.Contains("(1,", result.Errors[0]);
    }

    [Fact]
    public void Compile_EmptySource_FailsGracefully()
    {
        var result = JimmolateCompiler.Compile("   ");

        Assert.False(result.Success);
        Assert.NotEmpty(result.Errors);
    }

    [Fact]
    public void Compile_FullSourceWithoutFilterMethod_ReportsMissingEntryPoint()
    {
        const string source = """
            public static class Nothing
            {
                public static int Wrong(int x) => x;
            }
            """;

        var result = JimmolateCompiler.Compile(source);

        Assert.False(result.Success);
        Assert.Contains("Filter", result.Errors[0]);
    }

    [Fact]
    public void CompiledPredicate_AcceptAll_KeepsEverySeed()
    {
        var result = JimmolateCompiler.Compile("return 1;");
        Assert.True(result.Success);

        var (matching, matched) = RunWithJimmolate(result.Predicate!);

        Assert.Equal((long)Seeds.Length, matching);
        Assert.Equal(Seeds.Length, matched.Count);
    }

    [Fact]
    public void CompiledPredicate_RejectAll_KeepsNothing()
    {
        var result = JimmolateCompiler.Compile("return 0;");
        Assert.True(result.Success);

        var (matching, matched) = RunWithJimmolate(result.Predicate!);

        Assert.Equal(0L, matching);
        Assert.Empty(matched);
    }

    [Fact]
    public void CompiledPredicate_ReadsLiveContext_KeepsOnlyTargetSeed()
    {
        // The predicate reads the live single-seed context — proves the compiled
        // delegate gets the real drivable instance, not a marshalled copy.
        var result = JimmolateCompiler.Compile("""
            return ctx.GetSeed() == "UNITTEST" ? 1 : 0;
            """);
        Assert.True(result.Success);

        var (matching, matched) = RunWithJimmolate(result.Predicate!);

        Assert.Equal(1L, matching);
        Assert.Equal("UNITTEST", Assert.Single(matched));
    }
}

using System.Reflection;
using BalatroSeedOracle.Models;
using BalatroSeedOracle.Services;
using Motely.Filters.Jaml;
using Xunit;

namespace BalatroSeedOracle.Tests;

public class ActiveSearchContextTests
{
    [Fact]
    public void RaiseResultFound_MarksContextForBackgroundResultRefresh()
    {
        var context = new ActiveSearchContext("search-1", new JamlConfig { Id = "test-id", Name = "filter" });
        var raiseResultFound = typeof(ActiveSearchContext).GetMethod(
            "RaiseResultFound",
            BindingFlags.Instance | BindingFlags.NonPublic
        );

        Assert.NotNull(raiseResultFound);

        raiseResultFound!.Invoke(context, [new SearchResult { Seed = "ABC123", TotalScore = 5 }]);

        Assert.True(context.HasNewResultsSinceLastQuery);
    }
}

using System.Collections.Generic;
using System.Threading.Tasks;
using BalatroSeedOracle.Services;

namespace BalatroSeedOracle.Desktop.Services;

public sealed class RestoreActiveSearchesProviderService : IRestoreActiveSearchesProvider
{
    public Task<List<RestoredSearchInfo>> RestoreAsync(string jamlFiltersDir)
    {
        // TODO(JAML-port): re-implement persistent search restore once MJ exposes a public library API
        return Task.FromResult(new List<RestoredSearchInfo>());
    }
}

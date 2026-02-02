using System.Collections.Generic;
using System.Threading.Tasks;

namespace BalatroSeedOracle.Services;

/// <summary>
/// Abstraction for restoring active searches from the sequential library (Motely.DB).
/// Desktop implements via Motely.DB.SequentialLibrary; Browser does not register.
/// </summary>
public interface IRestoreActiveSearchesProvider
{
    Task<List<RestoredSearchInfo>> RestoreAsync(string jamlFiltersDir);
}

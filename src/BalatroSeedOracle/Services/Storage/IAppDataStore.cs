using System.Threading.Tasks;
using System.Collections.Generic;

namespace BalatroSeedOracle.Services.Storage;

public interface IAppDataStore
{
    Task<bool> ExistsAsync(string key);
    Task<string?> ReadTextAsync(string key);
    Task WriteTextAsync(string key, string content);
    Task DeleteAsync(string key);
    Task<IReadOnlyList<string>> ListKeysAsync(string prefix);
}

using System.Threading.Tasks;
using System.Collections.Generic;

namespace BalatroSeedOracle.Services.Storage;

public interface IAppDataStore
{
    // Synchronous operations use ValueTask for zero-allocation when completed synchronously
    ValueTask<bool> ExistsAsync(string key);
    Task<string?> ReadTextAsync(string key);
    Task WriteTextAsync(string key, string content);
    ValueTask DeleteAsync(string key);
    ValueTask<IReadOnlyList<string>> ListKeysAsync(string prefix);
    ValueTask<bool> FileExistsAsync(string path);
}

#if BROWSER
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Runtime.InteropServices.JavaScript;

namespace BalatroSeedOracle.Services.Storage;

public sealed partial class BrowserLocalStorageAppDataStore : IAppDataStore
{
    private const string Prefix = "bso:";

    private static string MakeKey(string key) => Prefix + key;

    public Task<bool> ExistsAsync(string key)
    {
        var value = GetItem(MakeKey(key));
        return Task.FromResult(value != null);
    }

    public Task<string?> ReadTextAsync(string key)
    {
        var value = GetItem(MakeKey(key));
        return Task.FromResult<string?>(value);
    }

    public Task WriteTextAsync(string key, string content)
    {
        SetItem(MakeKey(key), content);
        return Task.CompletedTask;
    }

    public Task DeleteAsync(string key)
    {
        RemoveItem(MakeKey(key));
        return Task.CompletedTask;
    }

    public Task<IReadOnlyList<string>> ListKeysAsync(string prefix)
    {
        var results = new List<string>();
        var desired = MakeKey(prefix);

        var len = GetLength();
        for (var i = 0; i < len; i++)
        {
            var k = Key(i);
            if (k == null)
                continue;
            if (!k.StartsWith(desired, StringComparison.Ordinal))
                continue;

            // Strip the bso: prefix back off
            results.Add(k.Substring(Prefix.Length));
        }

        return Task.FromResult<IReadOnlyList<string>>(results);
    }

    [JSImport("globalThis.localStorage.getItem")]
    private static partial string? GetItem(string key);

    [JSImport("globalThis.localStorage.setItem")]
    private static partial void SetItem(string key, string value);

    [JSImport("globalThis.localStorage.removeItem")]
    private static partial void RemoveItem(string key);

    [JSImport("globalThis.localStorage.length")]
    private static partial int GetLength();

    [JSImport("globalThis.localStorage.key")]
    private static partial string? Key(int index);
}
#endif

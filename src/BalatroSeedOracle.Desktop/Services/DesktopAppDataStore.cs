using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using BalatroSeedOracle.Helpers;
using BalatroSeedOracle.Services.Storage;

namespace BalatroSeedOracle.Desktop.Services;

/// <summary>
/// Desktop implementation of IAppDataStore using file system.
/// </summary>
public sealed class DesktopAppDataStore : IAppDataStore
{
    private static string NormalizeKey(string key)
    {
        var normalized = key.Replace('/', Path.DirectorySeparatorChar).Replace('\\', Path.DirectorySeparatorChar);
        return normalized.TrimStart(Path.DirectorySeparatorChar);
    }

    private static string GetPath(string key)
    {
        var normalized = NormalizeKey(key);
        return Path.Combine(AppPaths.DataRootDir, normalized);
    }

    public Task<bool> ExistsAsync(string key)
    {
        var path = GetPath(key);
        return Task.FromResult(File.Exists(path));
    }

    public async Task<string?> ReadTextAsync(string key)
    {
        var path = GetPath(key);
        if (!File.Exists(path))
            return null;
        return await File.ReadAllTextAsync(path).ConfigureAwait(false);
    }

    public async Task WriteTextAsync(string key, string content)
    {
        var path = GetPath(key);
        var dir = Path.GetDirectoryName(path);
        if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
        {
            Directory.CreateDirectory(dir);
        }

        await File.WriteAllTextAsync(path, content).ConfigureAwait(false);
    }

    public Task DeleteAsync(string key)
    {
        var path = GetPath(key);
        if (File.Exists(path))
            File.Delete(path);
        return Task.CompletedTask;
    }

    public Task<IReadOnlyList<string>> ListKeysAsync(string prefix)
    {
        var normalizedPrefix = NormalizeKey(prefix);
        var baseDir = GetPath(normalizedPrefix);

        if (!Directory.Exists(baseDir))
            return Task.FromResult<IReadOnlyList<string>>(new List<string>());

        var keys = new List<string>();
        foreach (var file in Directory.EnumerateFiles(baseDir, "*", SearchOption.AllDirectories))
        {
            var rel = Path.GetRelativePath(AppPaths.DataRootDir, file);
            rel = rel.Replace(Path.DirectorySeparatorChar, '/');
            keys.Add(rel);
        }
        return Task.FromResult<IReadOnlyList<string>>(keys);
    }
}

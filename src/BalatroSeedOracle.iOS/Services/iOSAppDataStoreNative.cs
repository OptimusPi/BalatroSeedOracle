using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using BalatroSeedOracle.Helpers;
using BalatroSeedOracle.Services.Storage;

namespace BalatroSeedOracle.iOS.Services
{
    /// <summary>
    /// iOS app data store implementation with full file system access.
    /// </summary>
    public sealed class iOSAppDataStoreNative : IAppDataStore
    {
        private static string NormalizeKey(string key)
        {
            var normalized = key.Replace('/', Path.DirectorySeparatorChar)
                .Replace('\\', Path.DirectorySeparatorChar);
            return normalized.TrimStart(Path.DirectorySeparatorChar);
        }

        private static string GetPath(string key)
        {
            var normalized = NormalizeKey(key);
            return Path.Combine(AppPaths.DataRootDir, normalized);
        }

        public ValueTask<bool> ExistsAsync(string key)
        {
            var path = GetPath(key);
            return ValueTask.FromResult(File.Exists(path));
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

        public ValueTask DeleteAsync(string key)
        {
            var path = GetPath(key);
            if (File.Exists(path))
                File.Delete(path);
            return ValueTask.CompletedTask;
        }

        public ValueTask<IReadOnlyList<string>> ListKeysAsync(string prefix)
        {
            var normalizedPrefix = NormalizeKey(prefix);
            var baseDir = GetPath(normalizedPrefix);

            if (!Directory.Exists(baseDir))
                return ValueTask.FromResult<IReadOnlyList<string>>(new List<string>());

            var keys = new List<string>();
            foreach (
                var file in Directory.EnumerateFiles(baseDir, "*", SearchOption.AllDirectories)
            )
            {
                var rel = Path.GetRelativePath(AppPaths.DataRootDir, file);
                rel = rel.Replace(Path.DirectorySeparatorChar, '/');
                keys.Add(rel);
            }
            return ValueTask.FromResult<IReadOnlyList<string>>(keys);
        }

        public ValueTask<bool> FileExistsAsync(string path)
        {
            return ValueTask.FromResult(File.Exists(path));
        }
    }
}

using System;
using System.IO;
using System.Threading.Tasks;
using BalatroSeedOracle.Helpers;
using BalatroSeedOracle.Services;
using BalatroSeedOracle.Services.Storage;

namespace BalatroSeedOracle.Browser.Services
{
    /// <summary>
    /// Browser platform implementation of IPlatformServices.
    /// Uses localStorage for storage, no file system access, no audio, no analyzer.
    /// </summary>
    public sealed class BrowserPlatformServices : IPlatformServices
    {
        private readonly IAppDataStore _store;

        public BrowserPlatformServices(IAppDataStore store)
        {
            _store = store;
        }

        public bool SupportsFileSystem => false;
        public bool SupportsAudio => false;
        public bool SupportsAnalyzer => false;
        public bool SupportsResultsGrid => false;

        public string GetTempDirectory()
        {
            // Browser: Use virtual in-memory path (no actual file I/O)
            return Path.Combine(AppPaths.DataRootDir, "Temp");
        }

        public void EnsureDirectoryExists(string path)
        {
            // Browser: No-op since we don't have file system access
            // Paths are virtual and handled by IAppDataStore
        }

        public Task WriteCrashLogAsync(string message)
        {
            // Browser: Could store in localStorage via IAppDataStore if needed
            // For now, just log to console/debug
            return Task.CompletedTask;
        }

        public Task CopySamplesToAppDataAsync()
        {
            // Browser: Sample files are seeded via SeedBrowserSampleFiltersAsync in App.axaml.cs
            // This is a no-op for browser
            return Task.CompletedTask;
        }

        public async Task<string?> ReadTextFromPathAsync(string path)
        {
            var storeKey = path.Replace('\\', '/');
            if (storeKey.StartsWith("/data/"))
                storeKey = storeKey.Substring(6);
            else if (storeKey.StartsWith("data/"))
                storeKey = storeKey.Substring(5);
            
            return await _store.ReadTextAsync(storeKey);
        }

        public async Task<bool> FileExistsAsync(string path)
        {
            var storeKey = path.Replace('\\', '/');
            if (storeKey.StartsWith("/data/"))
                storeKey = storeKey.Substring(6);
            else if (storeKey.StartsWith("data/"))
                storeKey = storeKey.Substring(5);
            
            return await _store.ExistsAsync(storeKey);
        }

        public void WriteLog(string message)
        {
            try
            {
                System.Console.WriteLine(message);
            }
            catch
            {
                try
                {
                    System.Diagnostics.Debug.WriteLine(message);
                }
                catch
                {
                    // Last resort: nothing we can do
                }
            }
        }

        public void WriteDebugLog(string message)
        {
            System.Diagnostics.Debug.WriteLine(message);
        }
    }
}

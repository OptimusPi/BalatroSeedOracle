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
    /// Uses localStorage for storage, Web Audio API for audio, DuckDB-WASM for database.
    /// </summary>
    public sealed class BrowserPlatformServices : IPlatformServices
    {
        private readonly IAppDataStore _store;

        public BrowserPlatformServices(IAppDataStore store)
        {
            _store = store;
        }

        public bool SupportsFileSystem => false; // No native file system, but localStorage works
        public bool SupportsAudio => true; // Web Audio API fully supported
        public bool SupportsAnalyzer => false; // Analyzer requires native DuckDB extensions
        public bool SupportsResultsGrid => true; // Avalonia DataGrid works perfectly in browser
        public bool SupportsAudioWidgets => false; // Desktop-only widgets require full audio engine
        public bool SupportsApiHostWidget => false; // API hosting only works on desktop
        public bool SupportsTransitionDesigner => false; // Transition Designer is desktop-only

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
                Console.WriteLine(message);
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

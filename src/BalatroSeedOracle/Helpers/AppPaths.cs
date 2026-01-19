using System;
using System.IO;
using BalatroSeedOracle.Services;

namespace BalatroSeedOracle.Helpers
{
    public static class AppPaths
    {
        private static IPlatformServices? _platformServices;
        private static string? _dataRoot;

        /// <summary>
        /// Initialize AppPaths with platform services. Called during app startup.
        /// </summary>
        public static void Initialize(IPlatformServices platformServices)
        {
            _platformServices = platformServices;
            _dataRoot = ResolveDataRoot();
        }

        private static string DataRoot => _dataRoot ?? ResolveDataRoot();

        public static string DataRootDir => DataRoot;
        public static string VisualizerPresetsDir =>
            EnsureDir(Path.Combine(DataRoot, "VisualizerPresets"));
        public static string MixerSettingsDir => EnsureDir(Path.Combine(DataRoot, "MixerSettings"));
        public static string MixerPresetsDir => EnsureDir(Path.Combine(DataRoot, "MixerPresets"));
        public static string SearchResultsDir => EnsureDir(Path.Combine(DataRoot, "SearchResults"));
        public static string UserDir => EnsureDir(Path.Combine(DataRoot, "User"));
        public static string FiltersDir => EnsureDir(Path.Combine(DataRoot, "Filters"));
        public static string WordListsDir => EnsureDir(Path.Combine(DataRoot, "WordLists"));
        public static string TransitionsDir => EnsureDir(Path.Combine(DataRoot, "Transitions"));
        public static string EventFXDir => EnsureDir(Path.Combine(DataRoot, "EventFX"));
        public static string TempDir => EnsureDir(GetTempDirectory());

        /// <summary>
        /// Gets a cross-platform temp directory that works in both desktop and browser
        /// </summary>
        private static string GetTempDirectory()
        {
            if (_platformServices != null)
            {
                return _platformServices.GetTempDirectory();
            }
            
            // Fallback for early initialization before platform services are available
            // This should only happen during static initialization
            return Path.Combine(DataRoot, "Temp");
        }

        public static string EnsureDir(string path)
        {
            if (_platformServices != null)
            {
                _platformServices.EnsureDirectoryExists(path);
            }
            else
            {
                // Fallback for early initialization
                // Only create directory if platform supports file system
                // This is a best-effort fallback
            }
            return path;
        }

        private static string ResolveDataRoot()
        {
            if (_platformServices != null && !_platformServices.SupportsFileSystem)
            {
                // Browser: Use virtual in-memory path (no actual file I/O)
                return "/data";
            }

            // Desktop: Resolve actual file system path
            var overrideDir = Environment.GetEnvironmentVariable("BSO_DATA_DIR");
            if (!string.IsNullOrWhiteSpace(overrideDir))
            {
                try
                {
                    if (_platformServices != null)
                    {
                        _platformServices.EnsureDirectoryExists(overrideDir);
                    }
                    else
                    {
                        Directory.CreateDirectory(overrideDir);
                    }
                    return overrideDir;
                }
                catch
                {
                }
            }

            var exeDir = AppContext.BaseDirectory;
            var localData = Path.Combine(exeDir, "Data");
            try
            {
                if (_platformServices != null)
                {
                    _platformServices.EnsureDirectoryExists(localData);
                }
                else
                {
                    Directory.CreateDirectory(localData);
                }
                return localData;
            }
            catch
            {
            }

            var appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            var root = Path.Combine(appData, "BalatroSeedOracle");
            if (_platformServices != null)
            {
                _platformServices.EnsureDirectoryExists(root);
            }
            else
            {
                Directory.CreateDirectory(root);
            }
            return root;
        }
    }
}

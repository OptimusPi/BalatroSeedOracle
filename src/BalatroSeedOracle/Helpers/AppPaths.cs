using System;
using System.IO;

namespace BalatroSeedOracle.Helpers
{
    public static class AppPaths
    {
        private static readonly string DataRoot = ResolveDataRoot();

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
#if BROWSER
            return Path.Combine(DataRoot, "Temp");
#else
            return Path.GetTempPath();
#endif
        }

        public static string EnsureDir(string path)
        {
#if !BROWSER
            if (!Directory.Exists(path))
                Directory.CreateDirectory(path);
#endif
            return path;
        }

        private static string ResolveDataRoot()
        {
#if BROWSER
            // Browser: Use virtual in-memory path (no actual file I/O)
            return "/data";
#else
            var overrideDir = Environment.GetEnvironmentVariable("BSO_DATA_DIR");
            if (!string.IsNullOrWhiteSpace(overrideDir))
            {
                try
                {
                    Directory.CreateDirectory(overrideDir);
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
                Directory.CreateDirectory(localData);
                return localData;
            }
            catch
            {
            }

            var appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            var root = Path.Combine(appData, "BalatroSeedOracle");
            Directory.CreateDirectory(root);
            return root;
#endif
        }
    }
}

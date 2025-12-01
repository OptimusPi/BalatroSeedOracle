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

        public static string EnsureDir(string path)
        {
            if (!Directory.Exists(path))
                Directory.CreateDirectory(path);
            return path;
        }

        private static string ResolveDataRoot()
        {
            var overrideDir = Environment.GetEnvironmentVariable("BSO_DATA_DIR");
            if (!string.IsNullOrWhiteSpace(overrideDir))
            {
                try
                {
                    Directory.CreateDirectory(overrideDir);
                    DebugLogger.Log("AppPaths", $"Using custom data directory: {overrideDir}");
                    return overrideDir;
                }
                catch (Exception ex)
                {
                    // Log the failure so user knows why their custom dir didn't work
                    DebugLogger.LogError(
                        "AppPaths",
                        $"❌ Failed to create custom data directory '{overrideDir}': {ex.Message}"
                    );
                    DebugLogger.LogError("AppPaths", "Falling back to default AppData location");
                }
            }

            var appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            var root = Path.Combine(appData, "BalatroSeedOracle");
            Directory.CreateDirectory(root);
            return root;
        }
    }
}

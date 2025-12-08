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
                Directory.CreateDirectory(overrideDir);
                return overrideDir;
            }

            var workingDir = Directory.GetCurrentDirectory();
            if (Directory.GetFiles(workingDir, "*.slnx").Length > 0)
            {
                return workingDir;
            }

            var localAppData = Environment.GetFolderPath(
                Environment.SpecialFolder.LocalApplicationData,
                Environment.SpecialFolderOption.Create);
            var root = Path.Combine(localAppData, "BalatroSeedOracle");
            Directory.CreateDirectory(root);
            return root;
        }
    }
}

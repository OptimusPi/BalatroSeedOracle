using System;
using System.IO;
using System.Threading.Tasks;
using BalatroSeedOracle.Helpers;
using BalatroSeedOracle.Services;

namespace BalatroSeedOracle.Services.Platforms
{
    /// <summary>
    /// File system platform implementation of IPlatformServices.
    /// Used by Desktop, Android, and iOS (all have file system access).
    /// </summary>
    public sealed class FileSystemPlatformServices : IPlatformServices
    {
        public bool SupportsFileSystem => true;
        public bool SupportsAudio => true;
        public bool SupportsAnalyzer => true;
        public bool SupportsResultsGrid => true;

        public string GetTempDirectory()
        {
            return Path.GetTempPath();
        }

        public void EnsureDirectoryExists(string path)
        {
            if (!Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
            }
        }

        public Task WriteCrashLogAsync(string message)
        {
            try
            {
                var crashLog = Path.Combine(AppPaths.DataRootDir, "crash.log");
                var dir = Path.GetDirectoryName(crashLog);
                if (!string.IsNullOrEmpty(dir))
                {
                    Directory.CreateDirectory(dir);
                }
                File.AppendAllText(crashLog, message);
            }
            catch
            {
                // If crash log writing fails, at least we logged to debug
            }

            return Task.CompletedTask;
        }

        public Task CopySamplesToAppDataAsync()
        {
            // Only run once - check for marker file
            var markerFile = Path.Combine(AppPaths.DataRootDir, ".samples_imported");
            if (File.Exists(markerFile))
            {
                return Task.CompletedTask;
            }

            try
            {
                // Copy sample filter
                var sampleFilter = Path.Combine(
                    AppContext.BaseDirectory ?? "",
                    "Samples",
                    "TelescopeObservatory.json"
                );
                var targetFilter = Path.Combine(
                    AppPaths.FiltersDir,
                    "TelescopeObservatory.json"
                );
                if (File.Exists(sampleFilter) && !File.Exists(targetFilter))
                {
                    EnsureDirectoryExists(Path.GetDirectoryName(targetFilter)!);
                    File.Copy(sampleFilter, targetFilter);
                }

                // Copy visualizer presets
                var samplePresetsDir = Path.Combine(
                    AppContext.BaseDirectory ?? "",
                    "Samples",
                    "VisualizerPresets"
                );
                if (Directory.Exists(samplePresetsDir))
                {
                    foreach (var file in Directory.GetFiles(samplePresetsDir, "*.json"))
                    {
                        var fileName = Path.GetFileName(file);
                        var target = Path.Combine(AppPaths.VisualizerPresetsDir, fileName);
                        if (!File.Exists(target))
                        {
                            EnsureDirectoryExists(Path.GetDirectoryName(target)!);
                            File.Copy(file, target);
                        }
                    }
                }

                // Copy mixer presets
                var sampleMixerDir = Path.Combine(
                    AppContext.BaseDirectory ?? "",
                    "Samples",
                    "MixerPresets"
                );
                if (Directory.Exists(sampleMixerDir))
                {
                    foreach (var file in Directory.GetFiles(sampleMixerDir, "*.json"))
                    {
                        var fileName = Path.GetFileName(file);
                        var target = Path.Combine(AppPaths.MixerPresetsDir, fileName);
                        if (!File.Exists(target))
                        {
                            EnsureDirectoryExists(Path.GetDirectoryName(target)!);
                            File.Copy(file, target);
                        }
                    }
                }

                // Mark as done
                EnsureDirectoryExists(Path.GetDirectoryName(markerFile)!);
                File.WriteAllText(markerFile, DateTime.UtcNow.ToString("o"));
                DebugLogger.Log("FileSystemPlatformServices", "Sample content copied to AppData successfully");
            }
            catch (Exception ex)
            {
                DebugLogger.LogError("FileSystemPlatformServices", $"Failed to copy samples: {ex.Message}");
            }

            return Task.CompletedTask;
        }

        public async Task<string?> ReadTextFromPathAsync(string path)
        {
            if (File.Exists(path))
            {
                return await File.ReadAllTextAsync(path);
            }
            return null;
        }

        public Task<bool> FileExistsAsync(string path)
        {
            return Task.FromResult(File.Exists(path));
        }

        public void WriteLog(string message)
        {
            Console.WriteLine(message);
        }

        public void WriteDebugLog(string message)
        {
            System.Diagnostics.Debug.WriteLine(message);
        }
    }
}

using System;
using System.IO;
using System.Threading.Tasks;
using BalatroSeedOracle.Helpers;
using BalatroSeedOracle.Services;

namespace BalatroSeedOracle.Desktop.Services
{
    /// <summary>
    /// Desktop platform implementation of IPlatformServices.
    /// Provides file system access, audio support, and analyzer support.
    /// </summary>
    public sealed class DesktopPlatformServices : IPlatformServices
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
            var markerFile = System.IO.Path.Combine(AppPaths.DataRootDir, ".samples_imported");
            if (System.IO.File.Exists(markerFile))
            {
                return Task.CompletedTask;
            }

            try
            {
                // Copy sample filter
                var sampleFilter = System.IO.Path.Combine(
                    AppContext.BaseDirectory,
                    "Samples",
                    "TelescopeObservatory.json"
                );
                var targetFilter = System.IO.Path.Combine(AppPaths.FiltersDir, "TelescopeObservatory.json");
                if (System.IO.File.Exists(sampleFilter) && !System.IO.File.Exists(targetFilter))
                {
                    EnsureDirectoryExists(System.IO.Path.GetDirectoryName(targetFilter)!);
                    System.IO.File.Copy(sampleFilter, targetFilter);
                }

                // Copy visualizer presets
                var samplePresetsDir = System.IO.Path.Combine(AppContext.BaseDirectory, "Samples", "VisualizerPresets");
                if (System.IO.Directory.Exists(samplePresetsDir))
                {
                    foreach (var file in System.IO.Directory.GetFiles(samplePresetsDir, "*.json"))
                    {
                        var fileName = System.IO.Path.GetFileName(file);
                        var target = System.IO.Path.Combine(AppPaths.VisualizerPresetsDir, fileName);
                        if (!System.IO.File.Exists(target))
                        {
                            EnsureDirectoryExists(System.IO.Path.GetDirectoryName(target)!);
                            System.IO.File.Copy(file, target);
                        }
                    }
                }

                // Copy mixer presets
                var sampleMixerDir = System.IO.Path.Combine(AppContext.BaseDirectory, "Samples", "MixerPresets");
                if (System.IO.Directory.Exists(sampleMixerDir))
                {
                    foreach (var file in System.IO.Directory.GetFiles(sampleMixerDir, "*.json"))
                    {
                        var fileName = System.IO.Path.GetFileName(file);
                        var target = System.IO.Path.Combine(AppPaths.MixerPresetsDir, fileName);
                        if (!System.IO.File.Exists(target))
                        {
                            EnsureDirectoryExists(System.IO.Path.GetDirectoryName(target)!);
                            System.IO.File.Copy(file, target);
                        }
                    }
                }

                // Mark as done
                EnsureDirectoryExists(System.IO.Path.GetDirectoryName(markerFile)!);
                System.IO.File.WriteAllText(markerFile, DateTime.UtcNow.ToString("o"));
                Helpers.DebugLogger.Log("DesktopPlatformServices", "Sample content copied to AppData successfully");
            }
            catch (Exception ex)
            {
                Helpers.DebugLogger.LogError("DesktopPlatformServices", $"Failed to copy samples: {ex.Message}");
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

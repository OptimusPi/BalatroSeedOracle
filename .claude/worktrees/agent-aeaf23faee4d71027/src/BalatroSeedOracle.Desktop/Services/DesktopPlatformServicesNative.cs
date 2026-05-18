using System;
using System.IO;
using System.Threading.Tasks;
using BalatroSeedOracle.Helpers;
using BalatroSeedOracle.Services;

namespace BalatroSeedOracle.Desktop.Services
{
    /// <summary>
    /// Desktop platform services implementation with full file system access.
    /// </summary>
    public sealed class DesktopPlatformServicesNative : IPlatformServices
    {
        public bool SupportsFileSystem => true;
        public bool SupportsAudio => true;
        public bool SupportsAnalyzer => true;
        public bool SupportsResultsGrid => true;
        public bool SupportsAudioWidgets => true;
        public bool SupportsApiHostWidget => true;
        public bool SupportsTransitionDesigner => true;

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

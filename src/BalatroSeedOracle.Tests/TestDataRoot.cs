using System.Runtime.CompilerServices;
using BalatroSeedOracle.Services;

namespace BalatroSeedOracle.Tests;

/// <summary>
/// Redirects the app data directory (favorites, profile, logs) to a throwaway temp folder
/// before any test runs, so no test can ever touch the real %APPDATA%\BalatroSeedOracle.
/// </summary>
internal static class TestDataRoot
{
    public static string Root { get; private set; } = "";

    [ModuleInitializer]
    internal static void Initialize()
    {
        Root = Path.Combine(Path.GetTempPath(), "bso-tests-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(Root);
        AppDataPaths.SetRootOverride(Root);
        AppDomain.CurrentDomain.ProcessExit += (_, _) =>
        {
            try
            {
                Directory.Delete(Root, recursive: true);
            }
            catch
            {
                // Temp cleanup is best effort (log file may still be open).
            }
        };
    }

    /// <summary>A fresh empty directory under the test root.</summary>
    public static string NewDir()
    {
        var dir = Path.Combine(Root, Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(dir);
        return dir;
    }
}

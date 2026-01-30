using System;
using Avalonia;
using Avalonia.iOS;
using BalatroSeedOracle;
using BalatroSeedOracle.Services;
using BalatroSeedOracle.Services.Export;
using BalatroSeedOracle.Services.Storage;
using Foundation;
using Microsoft.Extensions.DependencyInjection;
using UIKit;

namespace BalatroSeedOracle.iOS;

[Register("AppDelegate")]
public partial class AppDelegate : AvaloniaAppDelegate<App>
{
    [System.Runtime.Versioning.SupportedOSPlatform("ios13.0")]
    protected override AppBuilder CustomizeAppBuilder(AppBuilder builder)
    {
        // Register iOS services using Avalonia best practices
        PlatformServices.RegisterServices = services =>
        {
            // iOS-specific storage with full file system access
            services.AddSingleton<IAppDataStore, Services.iOSAppDataStoreNative>();

            // iOS-specific implementations
            services.AddSingleton<IPlatformServices, iOSPlatformServices>();
            services.AddSingleton<IParquetExporter, iOSParquetExporter>();
            services.AddSingleton<IAudioManager, iOSAudioManager>();
        };

        return base.CustomizeAppBuilder(builder);
    }
}

/// <summary>iOS platform services</summary>
internal sealed class iOSPlatformServices : IPlatformServices
{
    public bool SupportsFileSystem => true;
    public bool SupportsAudio => true;
    public bool SupportsAnalyzer => false;
    public bool SupportsResultsGrid => true;
    public bool SupportsAudioWidgets => false;
    public bool SupportsApiHostWidget => false;
    public bool SupportsTransitionDesigner => false;

    public string GetTempDirectory() => System.IO.Path.GetTempPath();

    public void EnsureDirectoryExists(string path)
    {
        if (!System.IO.Directory.Exists(path))
            System.IO.Directory.CreateDirectory(path);
    }

    public System.Threading.Tasks.Task WriteCrashLogAsync(string message) =>
        System.Threading.Tasks.Task.CompletedTask;

    public System.Threading.Tasks.Task<string?> ReadTextFromPathAsync(string path) =>
        System.Threading.Tasks.Task.FromResult<string?>(null);

    public System.Threading.Tasks.Task<bool> FileExistsAsync(string path) =>
        System.Threading.Tasks.Task.FromResult(false);

    public void WriteLog(string message) { }

    public void WriteDebugLog(string message) { }
}

/// <summary>iOS Parquet exporter - stub</summary>
internal sealed class iOSParquetExporter : IParquetExporter
{
    public bool IsAvailable => false; // TODO: Implement Parquet export for iOS

    public System.Threading.Tasks.Task ExportAsync(
        string filePath,
        System.Collections.Generic.IReadOnlyList<string> headers,
        System.Collections.Generic.IReadOnlyList<System.Collections.Generic.IReadOnlyList<object?>> rows
    ) => System.Threading.Tasks.Task.CompletedTask;
}

/// <summary>iOS Audio manager - stub</summary>
internal sealed class iOSAudioManager : IAudioManager
{
    public float MasterVolume { get; set; } = 1.0f;
    public bool IsPlaying => false;
    public float Bass1Intensity => 0;
    public float Bass2Intensity => 0;
    public float Drums1Intensity => 0;
    public float Drums2Intensity => 0;
    public float Chords1Intensity => 0;
    public float Chords2Intensity => 0;
    public float Melody1Intensity => 0;
    public float Melody2Intensity => 0;
    public float BassIntensity => 0;
    public float DrumsIntensity => 0;
    public float ChordsIntensity => 0;
    public float MelodyIntensity => 0;

    public void SetTrackVolume(string trackName, float volume) { }

    public void SetTrackPan(string trackName, float pan) { }

    public void SetTrackMuted(string trackName, bool muted) { }

    public void Pause() { }

    public void Resume() { }

    public void PlaySfx(string name, float volume = 1.0f) { }

    public FrequencyBands GetFrequencyBands(string trackName) => default;

    public event System.Action<float, float, float, float>? AudioAnalysisUpdated;
}

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using BalatroSeedOracle.Services;
using BalatroSeedOracle.Services.Export;
using BalatroSeedOracle.Services.Storage;

namespace BalatroSeedOracle.Android.Services;

public class AndroidAppDataStore : IAppDataStore
{
    private readonly string _basePath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "BalatroSeedOracle"
    );

    private string GetPath(string key) => Path.Combine(_basePath, key);

    public ValueTask<bool> ExistsAsync(string key) => new(File.Exists(GetPath(key)));

    public async Task<string?> ReadTextAsync(string key)
    {
        var p = GetPath(key);
        return File.Exists(p) ? await File.ReadAllTextAsync(p) : null;
    }

    public async Task WriteTextAsync(string key, string content)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(GetPath(key))!);
        await File.WriteAllTextAsync(GetPath(key), content);
    }

    public ValueTask DeleteAsync(string key)
    {
        var p = GetPath(key);
        if (File.Exists(p))
            File.Delete(p);
        return default;
    }

    public ValueTask<IReadOnlyList<string>> ListKeysAsync(string prefix) =>
        new(
            Directory.Exists(_basePath)
                ? Directory
                    .GetFiles(_basePath, $"{prefix}*")
                    .Select(Path.GetFileName)
                    .Where(f => f != null)
                    .Select(f => f!)
                    .ToList()
                : new List<string>()
        );

    public ValueTask<bool> FileExistsAsync(string path) => new(File.Exists(path));
}

public class AndroidPlatformServices : IPlatformServices
{
    public bool SupportsFileSystem => true;
    public bool SupportsAudio => false;
    public bool SupportsAnalyzer => false;
    public bool SupportsResultsGrid => true;
    public bool SupportsAudioWidgets => false;
    public bool SupportsApiHostWidget => false;
    public bool SupportsTransitionDesigner => false;

    public string GetTempDirectory() => Path.GetTempPath();

    public void EnsureDirectoryExists(string path) => Directory.CreateDirectory(path);

    public Task WriteCrashLogAsync(string content) =>
        File.WriteAllTextAsync(
            Path.Combine(GetTempDirectory(), $"crash_{DateTime.Now:yyyyMMdd_HHmmss}.log"),
            content
        );

    public Task<string?> ReadTextFromPathAsync(string path) =>
        Task.FromResult<string?>(File.Exists(path) ? File.ReadAllText(path) : null);

    public Task<bool> FileExistsAsync(string path) => Task.FromResult(File.Exists(path));

    public void WriteLog(string message) { }

    public void WriteDebugLog(string message) { }
}

public class AndroidAudioManager : IAudioManager
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

    public void SetTrackVolume(string t, float v) { }

    public void SetTrackPan(string t, float p) { }

    public void SetTrackMuted(string t, bool m) { }

    public void Pause() { }

    public void Resume() { }

    public void PlaySfx(string n, float v = 1.0f) { }

    public FrequencyBands GetFrequencyBands(string t) => default;

    public event Action<float, float, float, float>? AudioAnalysisUpdated;
}

public class AndroidParquetExporter : IParquetExporter
{
    public bool IsAvailable => false; // TODO: Implement Parquet export for Android

    public Task ExportAsync(
        string filePath,
        IReadOnlyList<string> headers,
        IReadOnlyList<IReadOnlyList<object?>> rows
    ) => Task.CompletedTask;
}

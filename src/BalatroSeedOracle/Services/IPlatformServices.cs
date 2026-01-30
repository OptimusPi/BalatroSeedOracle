using System.Threading.Tasks;

namespace BalatroSeedOracle.Services
{
    /// <summary>
    /// Platform abstraction interface for platform-specific operations.
    /// Implementations are provided by head projects (Desktop/Browser/Android/iOS).
    /// </summary>
    public interface IPlatformServices
    {
        /// <summary>
        /// Whether the platform supports direct file system access.
        /// </summary>
        bool SupportsFileSystem { get; }

        /// <summary>
        /// Whether the platform supports audio playback.
        /// </summary>
        bool SupportsAudio { get; }

        /// <summary>
        /// Whether the platform supports the analyzer feature.
        /// </summary>
        bool SupportsAnalyzer { get; }

        /// <summary>
        /// Whether the platform supports the results grid/tab feature.
        /// </summary>
        bool SupportsResultsGrid { get; }

        /// <summary>
        /// Whether the platform supports audio-related widgets (Audio Mixer, Music Mixer, Audio Visualizer Settings, Frequency Debug).
        /// </summary>
        bool SupportsAudioWidgets { get; }

        /// <summary>
        /// Whether the platform supports the API Host widget (desktop-only feature).
        /// </summary>
        bool SupportsApiHostWidget { get; }

        /// <summary>
        /// Whether the platform supports the Transition Designer widget (desktop-only feature).
        /// </summary>
        bool SupportsTransitionDesigner { get; }

        /// <summary>
        /// Gets the temporary directory path for this platform.
        /// </summary>
        string GetTempDirectory();

        /// <summary>
        /// Ensures a directory exists, creating it if necessary.
        /// On platforms without file system access, this is a no-op.
        /// </summary>
        void EnsureDirectoryExists(string path);

        /// <summary>
        /// Writes a crash log entry. Platform-specific implementations handle storage.
        /// </summary>
        Task WriteCrashLogAsync(string message);

        /// <summary>
        /// Reads text from a file path. Uses IAppDataStore on browser, File on desktop.
        /// </summary>
        Task<string?> ReadTextFromPathAsync(string path);

        /// <summary>
        /// Checks if a file exists at the given path. Uses IAppDataStore on browser, File on desktop.
        /// </summary>
        Task<bool> FileExistsAsync(string path);

        /// <summary>
        /// Writes a log message to the platform's logging mechanism.
        /// </summary>
        void WriteLog(string message);

        /// <summary>
        /// Writes a debug log message to the platform's logging mechanism.
        /// </summary>
        void WriteDebugLog(string message);
    }
}

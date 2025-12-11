using System;
using System.IO;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using BalatroSeedOracle.Helpers;
using BalatroSeedOracle.Models;

namespace BalatroSeedOracle.Services
{
    /// <summary>
    /// Service for managing user profile and preferences with intelligent debounced saves
    /// </summary>
    public class UserProfileService : IDisposable
    {
        private const string PROFILE_FILENAME = "userprofile.json";
        private readonly string _profilePath;
        private UserProfile _currentProfile;
        private DateTime _lastSaveLogTime = DateTime.MinValue;
        private int _suppressedSaveLogs = 0;
        private const int MinSaveLogIntervalMs = 1000;

        // Debounced save mechanism
        private System.Timers.Timer? _debounceSaveTimer;
        private const int SaveDebounceMs = 2000; // Save at most once every 2 seconds
        private readonly object _saveLock = new object();
        private bool _hasPendingSave = false;
        private volatile bool _disposed = false;

        public UserProfileService()
        {
            // Use AppPaths for proper cross-platform data directory
            _profilePath = Path.Combine(AppPaths.UserDir, PROFILE_FILENAME);

            DebugLogger.Log("UserProfileService", $"Profile path: {_profilePath}");

            _currentProfile = LoadProfile();

            // Initialize debounce timer (not started yet)
            _debounceSaveTimer = new System.Timers.Timer(SaveDebounceMs)
            {
                AutoReset = false, // One-shot timer
            };
            _debounceSaveTimer.Elapsed += OnDebounceSaveTimerElapsed;
        }

        /// <summary>
        /// Get the current user profile
        /// </summary>
        public UserProfile GetProfile() => _currentProfile;

        /// <summary>
        /// Load the user profile asynchronously
        /// </summary>
        public Task<UserProfile> LoadUserProfileAsync()
        {
            return Task.FromResult(_currentProfile);
        }

        /// <summary>
        /// Get the author name
        /// </summary>
        public string GetAuthorName()
        {
            DebugLogger.Log(
                "UserProfileService",
                $"GetAuthorName() returning: '{_currentProfile.AuthorName}'"
            );
            return _currentProfile.AuthorName;
        }

        /// <summary>
        /// Set the author name
        /// </summary>
        public void SetAuthorName(string name)
        {
            _currentProfile.AuthorName = name;
            SaveProfile();
        }

        /// <summary>
        /// Update background settings
        /// </summary>
        public void UpdateBackgroundSettings(string? theme, bool animationEnabled)
        {
            _currentProfile.BackgroundTheme = theme;
            _currentProfile.AnimationEnabled = animationEnabled;
            SaveProfile();
        }

        /// <summary>
        /// Save the current search state for resuming later
        /// </summary>
        public void SaveSearchState(SearchResumeState state)
        {
            _currentProfile.LastSearchState = state;
            SaveProfile();
            DebugLogger.Log(
                "UserProfileService",
                $"Saved search state: Batch {state.LastCompletedBatch}/{state.TotalBatches}"
            );
        }

        /// <summary>
        /// Update search state batch number without writing to disk
        /// </summary>
        public void UpdateSearchBatch(ulong completedBatch)
        {
            if (_currentProfile.LastSearchState != null)
            {
                _currentProfile.LastSearchState.LastCompletedBatch = completedBatch;
                _currentProfile.LastSearchState.LastActiveTime = DateTime.UtcNow;
            }
        }

        /// <summary>
        /// Force save the current profile to disk immediately, bypassing debounce.
        /// Use this when the application is shutting down or when you need to ensure
        /// data is persisted immediately (e.g., before a critical operation).
        /// </summary>
        public void FlushProfile()
        {
            if (_disposed)
            {
                DebugLogger.LogError(
                    "UserProfileService",
                    "Cannot flush profile - service disposed"
                );
                return;
            }

            lock (_saveLock)
            {
                // Stop the debounce timer
                _debounceSaveTimer?.Stop();

                // If there's no pending save, nothing to do
                if (!_hasPendingSave)
                {
                    return;
                }

                _hasPendingSave = false;
            }

            // Perform synchronous save for immediate flush
            try
            {
                SaveProfileToDiskSync();
                DebugLogger.Log("UserProfileService", "Profile flushed to disk");
            }
            catch (Exception ex)
            {
                DebugLogger.LogError("UserProfileService", $"Error flushing profile: {ex.Message}");
            }
        }

        /// <summary>
        /// Get the last saved search state if available
        /// </summary>
        public SearchResumeState? GetSearchState()
        {
            return _currentProfile.LastSearchState;
        }

        /// <summary>
        /// Clear the saved search state
        /// </summary>
        public void ClearSearchState()
        {
            _currentProfile.LastSearchState = null;
            SaveProfile();
            DebugLogger.Log("UserProfileService", "Cleared search state");
        }

        /// <summary>
        /// Load profile from disk
        /// </summary>
        private UserProfile LoadProfile()
        {
#if BROWSER
            // Browser: Skip file loading, return default profile
            DebugLogger.Log("UserProfileService", "Browser mode - using default profile");
            return new UserProfile();
#else
            try
            {
                if (File.Exists(_profilePath))
                {
                    var json = File.ReadAllText(_profilePath);
                    var profile = JsonSerializer.Deserialize<UserProfile>(json);
                    if (profile != null)
                    {
                        DebugLogger.Log(
                            "UserProfileService",
                            $"Loaded profile for author: {profile.AuthorName}"
                        );
                        return profile;
                    }
                }
            }
            catch (Exception ex)
            {
                DebugLogger.LogError("UserProfileService", $"Error loading profile: {ex.Message}");
            }

            // Return default profile with "pifreak" as the author
            DebugLogger.Log(
                "UserProfileService",
                "Creating new profile with default author: pifreak"
            );
            return new UserProfile();
#endif
        }

        /// <summary>
        /// Save profile to disk with intelligent debouncing.
        /// Multiple rapid calls will be batched and saved at most once every 2 seconds.
        /// </summary>
        public void SaveProfile(UserProfile? profile = null)
        {
            if (_disposed)
            {
                DebugLogger.LogError(
                    "UserProfileService",
                    "Cannot save profile - service disposed"
                );
                return;
            }

            lock (_saveLock)
            {
                // If profile provided, update the current profile
                if (profile != null)
                {
                    _currentProfile = profile;
                }

                // Mark that we have a pending save
                _hasPendingSave = true;

                // Reset the debounce timer
                _debounceSaveTimer?.Stop();
                _debounceSaveTimer?.Start();
            }
        }

        /// <summary>
        /// Called when the debounce timer elapses - performs the actual save
        /// </summary>
        private void OnDebounceSaveTimerElapsed(object? sender, System.Timers.ElapsedEventArgs e)
        {
            if (_disposed)
                return;

            lock (_saveLock)
            {
                if (!_hasPendingSave)
                    return;
                _hasPendingSave = false;
            }

            // Perform the actual save on a background thread
            Task.Run(async () =>
            {
                try
                {
                    await SaveProfileToDiskAsync().ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    DebugLogger.LogError(
                        "UserProfileService",
                        $"Error in debounced save: {ex.Message}"
                    );
                }
            });
        }

        /// <summary>
        /// Performs the actual disk I/O to save the profile (async version)
        /// </summary>
        private async Task SaveProfileToDiskAsync()
        {
            try
            {
                // Ensure directory exists
                var directory = Path.GetDirectoryName(_profilePath);
                if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                // Create a snapshot of the current profile to avoid race conditions
                UserProfile profileSnapshot;
                lock (_saveLock)
                {
                    profileSnapshot = _currentProfile;
                }

                var json = JsonSerializer.Serialize(
                    profileSnapshot,
                    new JsonSerializerOptions { WriteIndented = true }
                );
                await File.WriteAllTextAsync(_profilePath, json).ConfigureAwait(false);
                ThrottledLogSaveSuccess();
            }
            catch (Exception ex)
            {
                DebugLogger.LogError(
                    "UserProfileService",
                    $"Error saving profile to {_profilePath}: {ex.Message}"
                );
            }
        }

        /// <summary>
        /// Performs the actual disk I/O to save the profile (synchronous version).
        /// Used only for FlushProfile() and Dispose() to avoid blocking async calls.
        /// </summary>
        private void SaveProfileToDiskSync()
        {
            try
            {
                // Ensure directory exists
                var directory = Path.GetDirectoryName(_profilePath);
                if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                // Create a snapshot of the current profile to avoid race conditions
                UserProfile profileSnapshot;
                lock (_saveLock)
                {
                    profileSnapshot = _currentProfile;
                }

                var json = JsonSerializer.Serialize(
                    profileSnapshot,
                    new JsonSerializerOptions { WriteIndented = true }
                );
                File.WriteAllText(_profilePath, json);
                ThrottledLogSaveSuccess();
            }
            catch (Exception ex)
            {
                DebugLogger.LogError(
                    "UserProfileService",
                    $"Error saving profile to {_profilePath}: {ex.Message}"
                );
            }
        }

        private void ThrottledLogSaveSuccess()
        {
            var now = DateTime.UtcNow;
            var elapsedMs = (now - _lastSaveLogTime).TotalMilliseconds;
            if (elapsedMs >= MinSaveLogIntervalMs)
            {
                if (_suppressedSaveLogs > 0)
                {
                    DebugLogger.Log(
                        "UserProfileService",
                        $"Profile saved to: {_profilePath} (suppressed {_suppressedSaveLogs} rapid logs)"
                    );
                    _suppressedSaveLogs = 0;
                }
                else
                {
                    DebugLogger.Log(
                        "UserProfileService",
                        $"Profile saved successfully to: {_profilePath}"
                    );
                }
                _lastSaveLogTime = now;
            }
            else
            {
                Interlocked.Increment(ref _suppressedSaveLogs);
            }
        }

        /// <summary>
        /// Dispose of resources and flush any pending saves
        /// </summary>
        public void Dispose()
        {
            if (_disposed)
                return;

            lock (_saveLock)
            {
                _disposed = true;

                // Flush any pending saves
                if (_hasPendingSave)
                {
                    DebugLogger.Log("UserProfileService", "Flushing pending save on disposal");
                    _hasPendingSave = false;

                    try
                    {
                        SaveProfileToDiskSync();
                    }
                    catch (Exception ex)
                    {
                        DebugLogger.LogError(
                            "UserProfileService",
                            $"Error flushing profile on dispose: {ex.Message}"
                        );
                    }
                }

                // Dispose timer
                if (_debounceSaveTimer != null)
                {
                    _debounceSaveTimer.Stop();
                    _debounceSaveTimer.Elapsed -= OnDebounceSaveTimerElapsed;
                    _debounceSaveTimer.Dispose();
                    _debounceSaveTimer = null;
                }
            }

            DebugLogger.Log("UserProfileService", "Disposed");
        }
    }
}

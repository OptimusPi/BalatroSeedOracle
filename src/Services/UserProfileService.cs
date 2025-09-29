using System;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using BalatroSeedOracle.Helpers;
using BalatroSeedOracle.Models;

namespace BalatroSeedOracle.Services
{
    /// <summary>
    /// Service for managing user profile and preferences
    /// </summary>
    public class UserProfileService
    {
        private const string PROFILE_FILENAME = "userprofile.json";
        private readonly string _profilePath;
        private UserProfile _currentProfile;

        public UserProfileService()
        {
            // Just use the fucking current directory where the app is running
            _profilePath = Path.Combine(Directory.GetCurrentDirectory(), PROFILE_FILENAME);
            
            DebugLogger.Log("UserProfileService", $"Profile path: {_profilePath}");
            
            _currentProfile = LoadProfile();
        }

        /// <summary>
        /// Get the current user profile
        /// </summary>
        public UserProfile GetProfile() => _currentProfile;

        /// <summary>
        /// Load the user profile asynchronously
        /// </summary>
        public async Task<UserProfile> LoadUserProfileAsync()
        {
            return await Task.FromResult(_currentProfile);
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
        /// Force save the current profile to disk
        /// </summary>
        public void FlushProfile()
        {
            SaveProfile();
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
        }

        /// <summary>
        /// Save profile to disk
        /// </summary>
        public void SaveProfile(UserProfile? profile = null)
        {
            try
            {
                // If profile provided, update the current profile
                if (profile != null)
                {
                    _currentProfile = profile;
                }
                
                // Ensure directory exists
                var directory = Path.GetDirectoryName(_profilePath);
                if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                var json = JsonSerializer.Serialize(
                    _currentProfile,
                    new JsonSerializerOptions { WriteIndented = true }
                );
                File.WriteAllText(_profilePath, json);
                DebugLogger.Log(
                    "UserProfileService",
                    $"Profile saved successfully to: {_profilePath}"
                );
            }
            catch (Exception ex)
            {
                DebugLogger.LogError(
                    "UserProfileService",
                    $"Error saving profile to {_profilePath}: {ex.Message}"
                );
            }
        }
    }
}

using System;
using System.IO;
using System.Text.Json;
using Oracle.Helpers;
using Oracle.Models;

namespace Oracle.Services
{
    /// <summary>
    /// Service for managing user profile and preferences
    /// </summary>
    public class UserProfileService
    {
        private const string PROFILE_FILENAME = "user-profile.json";
        private readonly string _profilePath;
        private UserProfile _currentProfile;
        
        public UserProfileService()
        {
            // Store profile in user's local app data
            var appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            var oracleDir = Path.Combine(appDataPath, "BalatroSeedOracle");
            
            if (!Directory.Exists(oracleDir))
            {
                Directory.CreateDirectory(oracleDir);
            }
            
            _profilePath = Path.Combine(oracleDir, PROFILE_FILENAME);
            _currentProfile = LoadProfile();
        }
        
        /// <summary>
        /// Get the current user profile
        /// </summary>
        public UserProfile GetProfile() => _currentProfile;
        
        /// <summary>
        /// Get the author name
        /// </summary>
        public string GetAuthorName() => _currentProfile.AuthorName;
        
        /// <summary>
        /// Set the author name
        /// </summary>
        public void SetAuthorName(string name)
        {
            _currentProfile.AuthorName = name;
            SaveProfile();
        }
        
        /// <summary>
        /// Add or update a widget configuration
        /// </summary>
        public void AddOrUpdateWidget(SearchWidgetConfig widgetConfig)
        {
            // Remove existing config for the same filter path if any
            _currentProfile.ActiveWidgets.RemoveAll(w => w.FilterConfigPath == widgetConfig.FilterConfigPath);
            _currentProfile.ActiveWidgets.Add(widgetConfig);
            SaveProfile();
        }
        
        /// <summary>
        /// Remove a widget configuration
        /// </summary>
        public void RemoveWidget(string filterConfigPath)
        {
            _currentProfile.ActiveWidgets.RemoveAll(w => w.FilterConfigPath == filterConfigPath);
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
        /// Update audio settings
        /// </summary>
        public void UpdateAudioSettings(int volumeLevel, bool musicEnabled)
        {
            _currentProfile.VolumeLevel = volumeLevel;
            _currentProfile.MusicEnabled = musicEnabled;
            SaveProfile();
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
                        DebugLogger.Log("UserProfileService", $"Loaded profile for author: {profile.AuthorName}");
                        return profile;
                    }
                }
            }
            catch (Exception ex)
            {
                DebugLogger.LogError("UserProfileService", $"Error loading profile: {ex.Message}");
            }
            
            // Return default profile with "Jimbo" as the author
            DebugLogger.Log("UserProfileService", "Creating new profile with default author: Jimbo");
            return new UserProfile();
        }
        
        /// <summary>
        /// Save profile to disk
        /// </summary>
        private void SaveProfile()
        {
            try
            {
                var json = JsonSerializer.Serialize(_currentProfile, new JsonSerializerOptions 
                { 
                    WriteIndented = true 
                });
                File.WriteAllText(_profilePath, json);
                DebugLogger.Log("UserProfileService", "Profile saved successfully");
            }
            catch (Exception ex)
            {
                DebugLogger.LogError("UserProfileService", $"Error saving profile: {ex.Message}");
            }
        }
    }
}
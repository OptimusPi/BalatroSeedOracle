using System;
using System.Threading.Tasks;
using BalatroSeedOracle.Features.VibeOut;
using BalatroSeedOracle.Helpers;

namespace BalatroSeedOracle.Services
{
    /// <summary>
    /// Manages VibeOut mode integration across the application
    /// </summary>
    public class VibeOutManager
    {
        private static VibeOutManager? _instance;
        private VibeOutViewModel? _activeVibeViewModel;
        private VibeOutView? _activeVibeWindow;
        private bool _isVibeOutActive = false;
        
        public static VibeOutManager Instance => _instance ??= new VibeOutManager();
        
        public bool IsVibeOutActive => _isVibeOutActive;
        public VibeOutViewModel? ActiveVibeViewModel => _activeVibeViewModel;
        
        public event EventHandler<bool>? VibeOutStateChanged;
        
        private VibeOutManager() { }
        
        /// <summary>
        /// Start VibeOut mode
        /// </summary>
        public Task<bool> StartVibeOutMode()
        {
            try
            {
                if (_isVibeOutActive && _activeVibeWindow != null)
                {
                    // Already active, just bring to front
                    _activeVibeWindow.Activate();
                    return Task.FromResult(true);
                }
                
                // Create new VibeOut window and ViewModel
                _activeVibeWindow = new VibeOutView();
                _activeVibeViewModel = new VibeOutViewModel();
                _activeVibeWindow.DataContext = _activeVibeViewModel;
                
                // Start the vibe system
                _activeVibeViewModel.StartVibing();
                
                // Handle window closing
                _activeVibeWindow.Closed += (s, e) =>
                {
                    StopVibeOutMode();
                };
                
                // Show the window
                _activeVibeWindow.Show();
                
                _isVibeOutActive = true;
                VibeOutStateChanged?.Invoke(this, true);
                
                DebugLogger.Log("VibeOutManager", "🎵 VibeOut mode activated!");
                return Task.FromResult(true);
            }
            catch (Exception ex)
            {
                DebugLogger.LogError("VibeOutManager", $"Failed to start VibeOut mode: {ex.Message}");
                return Task.FromResult(false);
            }
        }
        
        /// <summary>
        /// Stop VibeOut mode
        /// </summary>
        public void StopVibeOutMode()
        {
            try
            {
                if (_activeVibeViewModel != null)
                {
                    _activeVibeViewModel.StopVibing();
                    _activeVibeViewModel = null;
                }
                
                if (_activeVibeWindow != null)
                {
                    _activeVibeWindow.Close();
                    _activeVibeWindow = null;
                }
                
                _isVibeOutActive = false;
                VibeOutStateChanged?.Invoke(this, false);
                
                DebugLogger.Log("VibeOutManager", "👋 VibeOut mode deactivated");
            }
            catch (Exception ex)
            {
                DebugLogger.LogError("VibeOutManager", $"Error stopping VibeOut mode: {ex.Message}");
            }
        }
        
        /// <summary>
        /// Process a search result in VibeOut mode
        /// </summary>
        public void ProcessSearchResult(string seed, int score, int[]? scores = null)
        {
            if (_isVibeOutActive && _activeVibeViewModel != null)
            {
                _activeVibeViewModel.ProcessSeedResult(seed, score, scores);
            }
        }
        
        /// <summary>
        /// Update VibeOut intensity based on search activity
        /// </summary>
        public void UpdateVibeIntensity(int intensity)
        {
            if (_isVibeOutActive && _activeVibeViewModel != null)
            {
                _activeVibeViewModel.UpdateIntensity(intensity);
            }
        }
        
        /// <summary>
        /// Award vibe points for achievements
        /// </summary>
        public void AwardVibePoints(int points, string reason)
        {
            if (_isVibeOutActive && _activeVibeViewModel != null)
            {
                _activeVibeViewModel.AwardVibePoints(points, reason);
            }
        }
        
        /// <summary>
        /// Notify VibeOut of search events
        /// </summary>
        public void OnSearchStarted()
        {
            if (_isVibeOutActive && _activeVibeViewModel != null)
            {
                UpdateVibeIntensity(1);
                DebugLogger.Log("VibeOutManager", "🎵 Search started - vibe level 1");
            }
        }
        
        public void OnSearchCompleted(int resultCount)
        {
            if (_isVibeOutActive && _activeVibeViewModel != null)
            {
                var vibeLevel = resultCount switch
                {
                    > 100 => 3, // Massive success
                    > 50 => 2,  // Good results
                    > 10 => 1,  // Some results
                    _ => 0      // Few/no results
                };
                
                UpdateVibeIntensity(vibeLevel);
                AwardVibePoints(Math.Min(resultCount / 10, 5), $"Search completed with {resultCount} results");
                
                DebugLogger.Log("VibeOutManager", $"🎵 Search completed - vibe level {vibeLevel}");
            }
        }
        
        public void OnGoodSeedFound(int score)
        {
            if (_isVibeOutActive && _activeVibeViewModel != null)
            {
                if (score > 80)
                {
                    AwardVibePoints(2, $"Epic seed found (score: {score})");
                    UpdateVibeIntensity(3); // Max vibe for epic seeds
                }
                else if (score > 50)
                {
                    AwardVibePoints(1, $"Good seed found (score: {score})");
                    UpdateVibeIntensity(2); // High vibe for good seeds
                }
                
                DebugLogger.Log("VibeOutManager", $"🎵 Good seed bonus - score: {score}");
            }
        }
        
        /// <summary>
        /// Toggle VibeOut mode on/off
        /// </summary>
        public async Task<bool> ToggleVibeOutMode()
        {
            if (_isVibeOutActive)
            {
                StopVibeOutMode();
                return false;
            }
            else
            {
                return await StartVibeOutMode();
            }
        }
        
        /// <summary>
        /// Check if VibeOut window is still open and valid
        /// </summary>
        public bool IsVibeOutWindowOpen()
        {
            return _activeVibeWindow != null && _isVibeOutActive;
        }
        
        /// <summary>
        /// Bring VibeOut window to front if it exists
        /// </summary>
        public void BringVibeOutToFront()
        {
            if (_activeVibeWindow != null && _isVibeOutActive)
            {
                try
                {
                    _activeVibeWindow.Activate();
                    _activeVibeWindow.Topmost = true;
                    _activeVibeWindow.Topmost = false; // This brings it to front
                }
                catch (Exception ex)
                {
                    DebugLogger.LogError("VibeOutManager", $"Failed to bring VibeOut to front: {ex.Message}");
                }
            }
        }
    }
}

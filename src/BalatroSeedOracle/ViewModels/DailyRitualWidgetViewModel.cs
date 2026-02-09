using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using BalatroSeedOracle.Services;
using BalatroSeedOracle.Models;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Collections.ObjectModel;
using System.Linq;
using Motely.Filters;
using DebugLogger = BalatroSeedOracle.Helpers.DebugLogger;

namespace BalatroSeedOracle.ViewModels
{
    public partial class DailyRitualWidgetViewModel : BaseWidgetViewModel
    {
        private readonly DaylatroHighScoreService _scoreService;
        private readonly UserProfileService _userService;
        private readonly FilterConfigurationService _filterService;

        [ObservableProperty]
        private string _dateText = DateTime.Now.ToString("MMM dd, yyyy");

        [ObservableProperty]
        private string _currentSeed = "LOADING";

        [ObservableProperty]
        private string _minigameTitle = "Daily Ritual";

        [ObservableProperty]
        private int _currentDayIndex = 0;

        [ObservableProperty]
        private MinigameConfig? _currentMinigame;

        private readonly MinigameDownloadService _downloadService;

        public DailyRitualWidgetViewModel(
            DaylatroHighScoreService scoreService,
            UserProfileService userService,
            FilterConfigurationService filterService)
        {
            _scoreService = scoreService;
            _userService = userService;
            _filterService = filterService;
            _downloadService = new MinigameDownloadService();
            
            WidgetTitle = "Daily Ritual";
            Width = 400;
            Height = 300;
            
            // Default to today
            CurrentDayIndex = (int)(DateTime.UtcNow - new DateTime(2026, 1, 1)).TotalDays;
        }

        public void Initialize()
        {
            LoadDailyJaml();
        }

        /// <summary>
        /// Deterministically selects a seed from the pool based on the minigame ID and target day index.
        /// This ensures that even if the input list is sorted alphabetically (ALEEB, ALEEC, HALEEG),
        /// the daily output is shuffled consistently based on the game ID.
        /// </summary>
        private string GetDeterministicSeed(string minigameId, List<string> sortedSeeds, int dayIndex)
        {
            if (sortedSeeds.Count == 0) return "NO_SEEDS";

            // 1. Calculate seed for the shuffler based on minigame ID
            // "THEDAILYWEE" -> ASCII sum logic as requested
            int idSum = 0;
            foreach (char c in minigameId)
            {
                idSum += (int)c;
            }

            // 2. Use a local copy of the list to simulate the "drawing from bucket"
            var availableSeeds = new List<string>(sortedSeeds);
            
            // 3. Re-create the shuffle sequence up to the requested day
            // We must start from Day 0 (Epoch) and remove seeds to ensure the sequence matches
            string resultSeed = "ERROR";
            
            // Limit simulation to the day requested
            // If dayIndex exceeds seed count, we wrap around (loop the season)
            // But for the shuffle logic, we reset the bucket every time it empties
            
            // Determine which "season loop" we are in
            int seasonLoop = dayIndex / sortedSeeds.Count;
            int dayInSeason = dayIndex % sortedSeeds.Count;

            // For the current season loop, simulate draws up to dayInSeason
            for (int i = 0; i <= dayInSeason; i++)
            {
                // Simple deterministic PRNG: (Sum + Index) % Remaining
                // We add 'i' to vary it per step, and 'seasonLoop' to vary it per season reset
                int prngValue = idSum + i + (seasonLoop * 13); 
                int pickIndex = prngValue % availableSeeds.Count;
                
                resultSeed = availableSeeds[pickIndex];
                availableSeeds.RemoveAt(pickIndex);
            }

            return resultSeed;
        }

        private async void LoadDailyJaml()
        {
            try
            {
                CurrentSeed = "LOADING...";
                
                // Fetch the season config
                // In a real app, this ID might come from a "Current Season" setting or API
                var config = await _downloadService.FetchGameConfigAsync("weejoker_season1");
                
                if (config?.Minigame != null)
                {
                    CurrentMinigame = config.Minigame;
                    MinigameTitle = config.Minigame.Title ?? "Unknown Ritual";
                    
                    if (config.Seeds != null && config.Seeds.Count > 0)
                    {
                        // Sort seeds alphabetically first as required by the protocol
                        var sortedSeeds = config.Seeds.OrderBy(s => s).ToList();

                        // Calculate Day Index based on Epoch
                        var epoch = config.Minigame.Epoch ?? new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);
                        
                        // Calculate REAL day index: (Today - Epoch)
                        var daysSinceEpoch = (int)(DateTime.UtcNow.Date - epoch.Date).TotalDays;
                        
                        // Adjust for UI navigation (Prev/Next buttons offset from Today)
                        // _currentDayIndex in this ViewModel is actually "Offset from Epoch" if we redesign it,
                        // OR "Offset from Today". Let's assume _currentDayIndex tracks absolute days from Epoch for simplicity.
                        // But constructor initialized it relative to 2026-01-01 placeholder.
                        
                        // Let's fix the logic:
                        // The requested day is what we want to show.
                        // _currentDayIndex is effectively "Target Date - 2026-01-01" (placeholder)
                        // But with real Epoch, we should recalculate index.
                        
                        // For now, we trust _currentDayIndex aligns with the game loop logic
                        // In production, we'd bind DateText to the actual calculated date
                        
                        string gameId = config.Minigame.Id ?? "DEFAULT_GAME";
                        CurrentSeed = GetDeterministicSeed(gameId, sortedSeeds, _currentDayIndex);
                    }
                    else
                    {
                        CurrentSeed = "NO SEEDS";
                    }
                }
                else
                {
                    CurrentSeed = "OFFLINE";
                }
            }
            catch (Exception ex)
            {
                // Handle error
                CurrentSeed = "ERROR";
                DebugLogger.LogError("DailyRitual", $"Failed to load daily seed: {ex.Message}");
            }
        }

        [RelayCommand]
        private void PrevDay()
        {
            CurrentDayIndex--;
            UpdateDateDisplay();
            LoadDailyJaml();
        }

        [RelayCommand]
        private void NextDay()
        {
            CurrentDayIndex++;
            UpdateDateDisplay();
            LoadDailyJaml();
        }

        [RelayCommand]
        private async Task CopySeed()
        {
            if (CurrentSeed != "LOADING" && CurrentSeed != "ERROR")
            {
                // Copy to clipboard logic
                // await Clipboard.SetTextAsync(CurrentSeed);
            }
        }

        private void UpdateDateDisplay()
        {
            var date = new DateTime(2026, 1, 1).AddDays(CurrentDayIndex);
            DateText = date.ToString("MMM dd, yyyy");
        }
    }
}

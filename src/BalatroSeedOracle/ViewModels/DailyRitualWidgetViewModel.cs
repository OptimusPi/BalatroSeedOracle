using System;
using System.Threading.Tasks;
using BalatroSeedOracle.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
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

        public DailyRitualWidgetViewModel(
            DaylatroHighScoreService scoreService,
            UserProfileService userService,
            FilterConfigurationService filterService
        )
        {
            _scoreService = scoreService;
            _userService = userService;
            _filterService = filterService;

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

        private void LoadDailyJaml()
        {
            try
            {
                // TODO: re-implement daily seed selection without MinigameConfig
                CurrentSeed = "COMING SOON";
            }
            catch (Exception ex)
            {
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
                await Task.CompletedTask;
            }
        }

        private void UpdateDateDisplay()
        {
            var date = new DateTime(2026, 1, 1).AddDays(CurrentDayIndex);
            DateText = date.ToString("MMM dd, yyyy");
        }
    }
}

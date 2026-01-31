using System;
using System.Threading.Tasks;
using BalatroSeedOracle.Helpers;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace BalatroSeedOracle.ViewModels
{
    /// <summary>
    /// View model for displaying search results in the DataGrid
    /// </summary>
    public partial class SearchResultViewModel : ObservableObject
    {
        [ObservableProperty]
        private int index;

        [ObservableProperty]
        private string seed = "";

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(ScoreFormatted))]
        [NotifyPropertyChangedFor(nameof(ScoreTooltip))]
        private int score;

        [ObservableProperty]
        private string details = "";

        /// <summary>
        /// Formatted score display (with thousands separator)
        /// </summary>
        public string ScoreFormatted => Score.ToString("N0");

        /// <summary>
        /// Tooltip showing score
        /// </summary>
        public string ScoreTooltip => $"Total Score: {ScoreFormatted}";

        [RelayCommand]
        private async Task CopySeed(string? seed)
        {
            if (string.IsNullOrEmpty(seed))
                return;

            try
            {
                if (
                    Avalonia.Application.Current?.ApplicationLifetime
                    is Avalonia.Controls.ApplicationLifetimes.IClassicDesktopStyleApplicationLifetime desktop
                )
                {
                    var clipboard = desktop.MainWindow?.Clipboard;
                    if (clipboard is not null)
                    {
                        await clipboard.SetTextAsync(seed);
                        DebugLogger.Log(
                            "SearchResultViewModel",
                            $"Copied seed to clipboard: {seed}"
                        );
                    }
                }
            }
            catch (Exception ex)
            {
                DebugLogger.LogError("SearchResultViewModel", $"Failed to copy seed: {ex.Message}");
            }
        }

        [RelayCommand]
        private void ViewDetails(SearchResultViewModel? result)
        {
            if (result is null)
                return;

            DebugLogger.Log("SearchResultViewModel", $"View details for seed: {result.Seed}");
        }
    }
}

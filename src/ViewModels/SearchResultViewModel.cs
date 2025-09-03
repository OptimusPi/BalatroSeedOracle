using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Windows.Input;
using CommunityToolkit.Mvvm.Input;
using BalatroSeedOracle.Helpers;

namespace BalatroSeedOracle.ViewModels
{
    /// <summary>
    /// View model for displaying search results in the DataGrid
    /// </summary>
    public class SearchResultViewModel : INotifyPropertyChanged
    {
        private int _index;
        private string _seed = "";
        private int _score;
        private string _details = "";

        public int Index
        {
            get => _index;
            set
            {
                _index = value;
                OnPropertyChanged();
            }
        }

        public string Seed
        {
            get => _seed;
            set
            {
                _seed = value;
                OnPropertyChanged();
            }
        }

        public int Score
        {
            get => _score;
            set
            {
                _score = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(ScoreFormatted));
                OnPropertyChanged(nameof(ScoreTooltip));
            }
        }

        public string Details
        {
            get => _details;
            set
            {
                _details = value;
                OnPropertyChanged();
            }
        }

        /// <summary>
        /// Formatted score display (with thousands separator)
        /// </summary>
        public string ScoreFormatted => Score.ToString("N0");

        /// <summary>
        /// Tooltip showing score
        /// </summary>
        public string ScoreTooltip => $"Total Score: {ScoreFormatted}";

        /// <summary>
        /// Command to copy seed to clipboard
        /// </summary>
        public ICommand CopyCommand { get; }

        /// <summary>
        /// Command to view detailed seed analysis
        /// </summary>
        public ICommand ViewCommand { get; }

        public SearchResultViewModel()
        {
            CopyCommand = new RelayCommand<string>(CopySeed);
            ViewCommand = new RelayCommand<SearchResultViewModel>(ViewDetails);
        }

        private async void CopySeed(string? seed)
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
                    if (clipboard != null)
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

        private void ViewDetails(SearchResultViewModel? result)
        {
            if (result == null)
                return;

            DebugLogger.Log("SearchResultViewModel", $"View details for seed: {result.Seed}");
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}

using System;
using System.IO;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace BalatroSeedOracle.ViewModels
{
    /// <summary>
    /// Lightweight "desktop icon" for a search that keeps running after its modal is
    /// closed. Lives on the main menu and reopens the search modal when clicked.
    /// </summary>
    public partial class SearchWidgetIconViewModel : ObservableObject
    {
        private readonly Action<SearchWidgetIconViewModel> _openCallback;

        [ObservableProperty]
        private string _filterName = "Search";

        [ObservableProperty]
        private double _progressPercent;

        [ObservableProperty]
        private string _progressText = "resumable";

        [ObservableProperty]
        private string _resultsText = "";

        [ObservableProperty]
        private bool _isRunning;

        public string SearchId { get; }
        public string? ConfigPath { get; }

        public SearchWidgetIconViewModel(
            string searchId,
            string? configPath,
            Action<SearchWidgetIconViewModel> openCallback
        )
        {
            SearchId = searchId;
            ConfigPath = configPath;
            _openCallback = openCallback;

            FilterName = configPath is not null
                ? Path.GetFileNameWithoutExtension(configPath)
                : "Search";
        }

        [RelayCommand]
        private void Open()
        {
            _openCallback(this);
        }
    }
}

using System;
using System.IO;
using Avalonia.Threading;
using BalatroSeedOracle.Models;
using BalatroSeedOracle.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace BalatroSeedOracle.ViewModels
{
    /// <summary>
    /// Lightweight "desktop icon" for a search that keeps running after its modal is
    /// closed. Lives on the main menu, shows live progress, and reopens the search
    /// modal (reconnected to the same instance) when clicked.
    /// </summary>
    public partial class SearchWidgetIconViewModel : ObservableObject, IDisposable
    {
        private readonly Action<SearchWidgetIconViewModel> _openCallback;
        private ActiveSearchContext? _searchContext;

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
            ActiveSearchContext? searchContext,
            Action<SearchWidgetIconViewModel> openCallback
        )
        {
            SearchId = searchId;
            ConfigPath = configPath;
            _searchContext = searchContext;
            _openCallback = openCallback;

            FilterName =
                searchContext?.FilterName
                ?? (configPath is not null ? Path.GetFileNameWithoutExtension(configPath) : "Search");

            if (searchContext is not null)
            {
                IsRunning = searchContext.IsRunning;
                ResultsText = $"{searchContext.ResultCount} found";
                ProgressText = IsRunning ? "searching..." : "paused";
                searchContext.ProgressUpdated += OnProgressUpdated;
                searchContext.SearchCompleted += OnSearchCompleted;
            }
        }

        private void OnProgressUpdated(object? sender, SearchProgress progress)
        {
            Dispatcher.UIThread.Post(() =>
            {
                ProgressPercent = progress.PercentComplete;
                ProgressText = $"{progress.PercentComplete:F1}%";
                ResultsText = $"{progress.ResultsFound} found";
                IsRunning = true;
            });
        }

        private void OnSearchCompleted(object? sender, SearchResultEventArgs e)
        {
            Dispatcher.UIThread.Post(() =>
            {
                IsRunning = false;
                ProgressText = "done";
                if (_searchContext is not null)
                {
                    ResultsText = $"{_searchContext.ResultCount} found";
                }
            });
        }

        [RelayCommand]
        private void Open()
        {
            _openCallback(this);
        }

        public void Dispose()
        {
            if (_searchContext is not null)
            {
                _searchContext.ProgressUpdated -= OnProgressUpdated;
                _searchContext.SearchCompleted -= OnSearchCompleted;
                _searchContext = null;
            }
        }
    }
}

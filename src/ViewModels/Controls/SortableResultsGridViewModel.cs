using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using BalatroSeedOracle.Models;
using BalatroSeedOracle.Services;

namespace BalatroSeedOracle.ViewModels.Controls
{
    public partial class SortableResultsGridViewModel : ObservableObject
    {
        // Observable collections
        [ObservableProperty]
        private ObservableCollection<SearchResult> _allResults = new();

        [ObservableProperty]
        private ObservableCollection<SearchResult> _displayedResults = new();

        // Pagination properties
        [ObservableProperty]
        private int _currentPage = 1;

        [ObservableProperty]
        private int _totalPages = 1;

        [ObservableProperty]
        private int _itemsPerPage = 100;

        // Computed properties (updated automatically)
        [ObservableProperty]
        private string _pageInfoText = "Page 1 of 1";

        [ObservableProperty]
        private string _resultsCountText = "0 results";

        [ObservableProperty]
        private string _statsText = "Ready to search";

        [ObservableProperty]
        private bool _isPreviousEnabled = false;

        [ObservableProperty]
        private bool _isNextEnabled = false;

        // Sorting properties
        [ObservableProperty]
        private string _currentSortColumn = "TotalScore";

        [ObservableProperty]
        private bool _sortDescending = true;

        [ObservableProperty]
        private int _selectedSortIndex = 1; // Default to "Score ↓"

        // Commands
        public IRelayCommand<string> CopySeedCommand { get; }
        public IRelayCommand<SearchResult> SearchSimilarCommand { get; }
        public IRelayCommand<SearchResult> AddToFavoritesCommand { get; }
        public IRelayCommand<SearchResult> ExportSeedCommand { get; }
        public IRelayCommand<SearchResult> AnalyzeCommand { get; }
        public IRelayCommand ExportAllCommand { get; }
        public IRelayCommand PreviousPageCommand { get; }
        public IRelayCommand NextPageCommand { get; }

        // Events (for parent communication)
        public event EventHandler<SearchResult>? SeedCopied;
        public event EventHandler<SearchResult>? SearchSimilarRequested;
        public event EventHandler<SearchResult>? AddToFavoritesRequested;
        public event EventHandler<SearchResult>? ExportSeedRequested;
        public event EventHandler<IEnumerable<SearchResult>>? ExportAllRequested;
        public event EventHandler<SearchResult>? AnalyzeRequested;

        public SortableResultsGridViewModel()
        {
            // Initialize commands
            CopySeedCommand = new RelayCommand<string>(CopySeed);
            SearchSimilarCommand = new RelayCommand<SearchResult>(SearchSimilar);
            AddToFavoritesCommand = new RelayCommand<SearchResult>(AddToFavorites);
            ExportSeedCommand = new RelayCommand<SearchResult>(ExportSeed);
            AnalyzeCommand = new RelayCommand<SearchResult>(Analyze);
            ExportAllCommand = new RelayCommand(ExportAll);
            PreviousPageCommand = new RelayCommand(PreviousPage, () => IsPreviousEnabled);
            NextPageCommand = new RelayCommand(NextPage, () => IsNextEnabled);

            // Listen to property changes for auto-updates
            AllResults.CollectionChanged += (s, e) => UpdateDisplay();
        }

        // Sorting logic
        partial void OnSelectedSortIndexChanged(int value)
        {
            // Map index to sort column and direction
            switch (value)
            {
                case 0: // Seed
                    CurrentSortColumn = "Seed";
                    SortDescending = false;
                    break;
                case 1: // Score ↓
                    CurrentSortColumn = "TotalScore";
                    SortDescending = true;
                    break;
                case 2: // Score ↑
                    CurrentSortColumn = "TotalScore";
                    SortDescending = false;
                    break;
            }
            ApplySorting();
            UpdateDisplay();
        }

        private void ApplySorting()
        {
            var sorted = CurrentSortColumn switch
            {
                "Seed" => SortDescending
                    ? AllResults.OrderByDescending(r => r.Seed)
                    : AllResults.OrderBy(r => r.Seed),
                "TotalScore" => SortDescending
                    ? AllResults.OrderByDescending(r => r.TotalScore)
                    : AllResults.OrderBy(r => r.TotalScore),
                _ => AllResults.OrderByDescending(r => r.TotalScore)
            };

            AllResults = new ObservableCollection<SearchResult>(sorted);
        }

        private void UpdateDisplay()
        {
            // Calculate pagination
            TotalPages = Math.Max(1, (int)Math.Ceiling((double)AllResults.Count / ItemsPerPage));
            CurrentPage = Math.Min(CurrentPage, TotalPages);

            // Get current page items
            var startIndex = (CurrentPage - 1) * ItemsPerPage;
            var pageItems = AllResults.Skip(startIndex).Take(ItemsPerPage);

            // Update displayed results
            DisplayedResults = new ObservableCollection<SearchResult>(pageItems);

            // Update computed properties
            UpdatePageInfo();
            UpdateResultsCount();
            UpdateStats();
            UpdatePaginationButtons();
        }

        private void UpdatePageInfo()
        {
            PageInfoText = $"Page {CurrentPage} of {TotalPages}";
        }

        private void UpdateResultsCount()
        {
            ResultsCountText = AllResults.Count switch
            {
                0 => "No results",
                1 => "1 result",
                _ => $"{AllResults.Count:N0} results"
            };
        }

        private void UpdateStats()
        {
            if (AllResults.Count == 0)
            {
                StatsText = "Ready to search";
                return;
            }

            var highestScore = AllResults.Max(r => r.TotalScore);
            var averageScore = AllResults.Average(r => r.TotalScore);
            StatsText = $"Best: {highestScore} • Avg: {averageScore:F1} • Count: {AllResults.Count}";
        }

        private void UpdatePaginationButtons()
        {
            IsPreviousEnabled = CurrentPage > 1;
            IsNextEnabled = CurrentPage < TotalPages;

            // Notify command can-execute changed
            PreviousPageCommand.NotifyCanExecuteChanged();
            NextPageCommand.NotifyCanExecuteChanged();
        }

        // Command implementations
        private async void CopySeed(string? seed)
        {
            if (string.IsNullOrWhiteSpace(seed)) return;

            await ClipboardService.CopyToClipboardAsync(seed);

            var result = AllResults.FirstOrDefault(r => r.Seed == seed);
            if (result != null)
            {
                SeedCopied?.Invoke(this, result);
            }
        }

        private void SearchSimilar(SearchResult? result)
        {
            if (result == null) return;
            SearchSimilarRequested?.Invoke(this, result);
        }

        private void AddToFavorites(SearchResult? result)
        {
            if (result == null) return;
            AddToFavoritesRequested?.Invoke(this, result);
        }

        private void ExportSeed(SearchResult? result)
        {
            if (result == null) return;
            ExportSeedRequested?.Invoke(this, result);
        }

        private void Analyze(SearchResult? result)
        {
            if (result == null) return;
            AnalyzeRequested?.Invoke(this, result);
        }

        private void ExportAll()
        {
            ExportAllRequested?.Invoke(this, AllResults);
        }

        private void PreviousPage()
        {
            if (CurrentPage > 1)
            {
                CurrentPage--;
                UpdateDisplay();
            }
        }

        private void NextPage()
        {
            if (CurrentPage < TotalPages)
            {
                CurrentPage++;
                UpdateDisplay();
            }
        }

        // Public methods for external control
        public void AddResults(IEnumerable<SearchResult> results)
        {
            foreach (var result in results)
            {
                AllResults.Add(result);
            }
        }

        public void AddResult(SearchResult result)
        {
            AllResults.Add(result);
        }

        public void ClearResults()
        {
            AllResults.Clear();
            DisplayedResults.Clear();
            CurrentPage = 1;
        }

        public IEnumerable<SearchResult> GetAllResults() => AllResults.ToList();
        public IEnumerable<SearchResult> GetDisplayedResults() => DisplayedResults.ToList();
    }
}

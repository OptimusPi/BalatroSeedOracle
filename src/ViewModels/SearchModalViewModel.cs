using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using System.Windows.Input;
using CommunityToolkit.Mvvm.Input;
using BalatroSeedOracle.Services;
using BalatroSeedOracle.Models;
using BalatroSeedOracle.Views.Modals;
using BalatroSeedOracle.Helpers;
using Motely.Filters;

namespace BalatroSeedOracle.ViewModels
{
    public class SearchModalViewModel : BaseViewModel, IDisposable
    {
        private readonly SearchManager _searchManager;
        private readonly CircularConsoleBuffer _consoleBuffer;

        private SearchInstance? _searchInstance;
        private string _currentSearchId = string.Empty;
        private bool _isSearching = false;
        private MotelyJsonConfig? _loadedConfig;
        private string _currentActiveTab = "FilterTab";
        private SearchProgressEventArgs? _latestProgress;
        private int _lastKnownResultCount = 0;

        // Search parameters
        private int _maxResults = 1000;
        private int _timeoutSeconds = 300;
        private string _deckSelection = "All Decks";
        private string _stakeSelection = "All Stakes";

        public SearchModalViewModel(SearchManager searchManager)
        {
            _searchManager = searchManager;
            _consoleBuffer = new CircularConsoleBuffer(1000);

            SearchResults = new ObservableCollection<Models.SearchResult>();
            ConsoleOutput = new ObservableCollection<string>();

            // Initialize commands
            StartSearchCommand = new AsyncRelayCommand(StartSearchAsync, CanStartSearch);
            StopSearchCommand = new RelayCommand(StopSearch, CanStopSearch);
            ClearResultsCommand = new RelayCommand(ClearResults);
            LoadFilterCommand = new AsyncRelayCommand(LoadFilterAsync);
            SwitchTabCommand = new RelayCommand<string>(SwitchTab);
            CreateShortcutCommand = new RelayCommand<string>(CreateShortcut);

            // Events will be subscribed to individual SearchInstance when created
        }

        #region Properties

        public bool IsSearching
        {
            get => _isSearching;
            set
            {
                if (SetProperty(ref _isSearching, value))
                {
                    ((AsyncRelayCommand)StartSearchCommand).NotifyCanExecuteChanged();
                    ((RelayCommand)StopSearchCommand).NotifyCanExecuteChanged();
                }
            }
        }

        public MotelyJsonConfig? LoadedConfig
        {
            get => _loadedConfig;
            set => SetProperty(ref _loadedConfig, value);
        }

        public string CurrentActiveTab
        {
            get => _currentActiveTab;
            set => SetProperty(ref _currentActiveTab, value);
        }

        public int MaxResults
        {
            get => _maxResults;
            set => SetProperty(ref _maxResults, value);
        }

        public int TimeoutSeconds
        {
            get => _timeoutSeconds;
            set => SetProperty(ref _timeoutSeconds, value);
        }

        public string DeckSelection
        {
            get => _deckSelection;
            set => SetProperty(ref _deckSelection, value);
        }

        public string StakeSelection
        {
            get => _stakeSelection;
            set => SetProperty(ref _stakeSelection, value);
        }

        public SearchProgressEventArgs? LatestProgress
        {
            get => _latestProgress;
            set => SetProperty(ref _latestProgress, value);
        }

        public int LastKnownResultCount
        {
            get => _lastKnownResultCount;
            set => SetProperty(ref _lastKnownResultCount, value);
        }

        public ObservableCollection<Models.SearchResult> SearchResults { get; }
        public ObservableCollection<string> ConsoleOutput { get; }

        #endregion

        #region Commands

        public ICommand StartSearchCommand { get; }
        public ICommand StopSearchCommand { get; }
        public ICommand ClearResultsCommand { get; }
        public ICommand LoadFilterCommand { get; }
        public ICommand SwitchTabCommand { get; }
        public ICommand CreateShortcutCommand { get; }

        #endregion

        #region Events

        public event EventHandler<string>? CreateShortcutRequested;

        #endregion

        #region Command Implementations

        private async Task StartSearchAsync()
        {
            try
            {
                if (LoadedConfig == null)
                {
                    AddConsoleMessage("No filter configuration loaded. Please load a filter first.");
                    return;
                }

                IsSearching = true;
                _currentSearchId = Guid.NewGuid().ToString();
                
                ClearResults();
                AddConsoleMessage("Starting search...");

                var searchCriteria = BuildSearchCriteria();
                _searchInstance = await _searchManager.StartSearchAsync(searchCriteria, LoadedConfig);

                // Subscribe to SearchInstance events directly
                _searchInstance.ProgressUpdated += OnSearchProgressUpdated;
                _searchInstance.ResultFound += OnSearchResultFound;
                _searchInstance.SearchCompleted += OnSearchCompleted;

                DebugLogger.Log("SearchModalViewModel", $"Search started with ID: {_currentSearchId}");
            }
            catch (Exception ex)
            {
                IsSearching = false;
                AddConsoleMessage($"Error starting search: {ex.Message}");
                DebugLogger.LogError("SearchModalViewModel", $"Error starting search: {ex.Message}");
            }
        }

        private bool CanStartSearch()
        {
            return !IsSearching && LoadedConfig != null;
        }

        private void StopSearch()
        {
            try
            {
                if (_searchInstance != null)
                {
                    _searchInstance.StopSearch();
                    AddConsoleMessage("Search stopped by user.");
                    DebugLogger.Log("SearchModalViewModel", "Search stopped by user");
                }
                
                IsSearching = false;
            }
            catch (Exception ex)
            {
                AddConsoleMessage($"Error stopping search: {ex.Message}");
                DebugLogger.LogError("SearchModalViewModel", $"Error stopping search: {ex.Message}");
            }
        }

        private bool CanStopSearch()
        {
            return IsSearching;
        }

        private void ClearResults()
        {
            SearchResults.Clear();
            ConsoleOutput.Clear();
            _consoleBuffer.Clear();
            LastKnownResultCount = 0;
            LatestProgress = null;
            DebugLogger.Log("SearchModalViewModel", "Results cleared");
        }

        private Task LoadFilterAsync()
        {
            try
            {
                // This would typically show a file dialog
                // For now, we'll load from the temp location
                AddConsoleMessage("Filter loading functionality needs UI implementation");
                DebugLogger.Log("SearchModalViewModel", "Load filter requested");
                return Task.CompletedTask;
            }
            catch (Exception ex)
            {
                AddConsoleMessage($"Error loading filter: {ex.Message}");
                DebugLogger.LogError("SearchModalViewModel", $"Error loading filter: {ex.Message}");
                return Task.CompletedTask;
            }
        }

        private void SwitchTab(string? tabName)
        {
            if (!string.IsNullOrEmpty(tabName))
            {
                CurrentActiveTab = tabName;
                DebugLogger.Log("SearchModalViewModel", $"Switched to tab: {tabName}");
            }
        }

        private void CreateShortcut(string? searchId)
        {
            if (!string.IsNullOrEmpty(searchId))
            {
                CreateShortcutRequested?.Invoke(this, searchId);
            }
        }

        #endregion

        #region Event Handlers

        private void OnSearchProgressUpdated(object? sender, SearchProgressEventArgs e)
        {
            LatestProgress = e;
            AddConsoleMessage($"Progress: {e.SeedsSearched} seeds processed, {e.ResultsFound} results found");
        }

        private void OnSearchResultFound(object? sender, SearchResultEventArgs e)
        {
            SearchResults.Add(e.Result);
            LastKnownResultCount = SearchResults.Count;
            AddConsoleMessage($"Found result: Seed {e.Result.Seed} with score {e.Result.TotalScore}");
        }

        private void OnSearchCompleted(object? sender, EventArgs e)
        {
            IsSearching = false;
            AddConsoleMessage($"Search completed. Found {SearchResults.Count} results.");
            DebugLogger.Log("SearchModalViewModel", $"Search completed with {SearchResults.Count} results");
        }

        #endregion

        #region Helper Methods

        private SearchCriteria BuildSearchCriteria()
        {
            return new SearchCriteria
            {
                MaxSeeds = (ulong)MaxResults,
                ThreadCount = Environment.ProcessorCount,
                Deck = DeckSelection == "All Decks" ? null : DeckSelection,
                Stake = StakeSelection == "All Stakes" ? null : StakeSelection
            };
        }

        private void AddConsoleMessage(string message)
        {
            var timestamp = DateTime.Now.ToString("HH:mm:ss");
            var formattedMessage = $"[{timestamp}] {message}";
            
            _consoleBuffer.AddLine(formattedMessage);
            ConsoleOutput.Add(formattedMessage);
            
            // Keep console output manageable
            while (ConsoleOutput.Count > 1000)
            {
                ConsoleOutput.RemoveAt(0);
            }
        }

        #endregion

        #region IDisposable

        public void Dispose()
        {
            if (_searchInstance != null)
            {
                _searchInstance.ProgressUpdated -= OnSearchProgressUpdated;
                _searchInstance.ResultFound -= OnSearchResultFound;
                _searchInstance.SearchCompleted -= OnSearchCompleted;
                _searchInstance.Dispose();
            }
            // CircularConsoleBuffer doesn't need disposal
        }

        #endregion
    }
}
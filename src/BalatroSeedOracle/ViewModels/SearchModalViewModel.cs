using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Input.Platform;
using BalatroSeedOracle.Extensions;
using BalatroSeedOracle.Helpers;
using BalatroSeedOracle.Models;
using BalatroSeedOracle.Services;
using BalatroSeedOracle.Views.Modals;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Motely;
using Motely.Filters;
using Motely.Filters.Jaml;
using BsoLogger = BalatroSeedOracle.Helpers.DebugLogger;

namespace BalatroSeedOracle.ViewModels
{
    // Search mode enum
    public enum SearchMode
    {
        AllSeeds = 0,
        SingleSeed = 1,
        WordList = 2,
        DbList = 3,
    }

    public partial class SearchModalViewModel
        : ObservableObject,
            IDisposable,
            BalatroSeedOracle.Helpers.IModalBackNavigable
    {
        private readonly UserProfileService _userProfileService;
        private readonly Func<AnalyzeModalViewModel> _analyzeModalFactory;

        private Motely.IMotelySearch? _search;
        private System.Threading.CancellationTokenSource? _searchCts;
        private string[] _tallyLabels = Array.Empty<string>();
        private Task? _searchCompletionTask;
        private string _currentSearchId = string.Empty;

        public Views.BalatroMainMenu? MainMenu { get; set; }

        // Callback for CREATE NEW FILTER button (set by View)
        private Action? _newFilterRequestedAction;

        // Callback for EDIT FILTER button (set by View) - takes filter path
        private Action<string?>? _editFilterRequestedAction;

        // Tab content properties (removed direct UserControl instantiation - view handles this via XAML)
        // These are now just markers; actual content is managed in XAML DataTemplates
        public object? SettingsTabContent => "Settings";
        public object? SearchTabContent => "Search";
        public object? ResultsTabContent => "Results";

        [ObservableProperty]
        [NotifyCanExecuteChangedFor(nameof(StartSearchCommand))]
        private bool _isSearching = false;

        [ObservableProperty]
        private Motely.Filters.Jaml.JamlConfig? _loadedConfig;

        [ObservableProperty]
        private int _selectedTabIndex = 0;

        [ObservableProperty]
        private MotelyProgress? _latestProgress;

        /// <summary>
        /// Optional: Active shader transition driven by search progress.
        /// When set, search progress (0-100%) will drive shader LERP between Start/End parameters.
        /// User can design custom transitions (e.g., dark red → bright blue as search progresses).
        /// </summary>
        public Models.VisualizerPresetTransition? ActiveSearchTransition { get; set; }

        // PROPER MVVM: Tab visibility controlled by ViewModel, not code-behind
        [ObservableProperty]
        private bool _isSettingsTabVisible = false;

        [ObservableProperty]
        private bool _isSearchTabVisible = false;

        [ObservableProperty]
        private bool _isResultsTabVisible = false;

        [ObservableProperty]
        private int _deckIndex = 0;

        [ObservableProperty]
        private int _stakeIndex = 0;

        [ObservableProperty]
        private int _lastKnownResultCount = 0;

        // UX: generic Balatro-styled info text for the Results tab panel
        [ObservableProperty]
        private string _panelText = "Tip: Results appear below. Use Export to save seeds.";

        [ObservableProperty]
        private string? _currentFilterPath; // CRITICAL: Store the path to the loaded filter!

        [ObservableProperty]
        private int _maxResults = 1000;

        [ObservableProperty]
        private int _timeoutSeconds = 300;

        [ObservableProperty]
        private string _deckSelection = "Red Deck";

        [ObservableProperty]
        private string _stakeSelection = "White";

        [ObservableProperty]
        private int _selectedDeckIndex = 0;

        [ObservableProperty]
        private int _selectedStakeIndex = 0;

        // Deck/Stake display properties for SettingsTab (MVVM bindings)
        public Avalonia.Media.IImage? DeckImage
        {
            get
            {
                var deckName = DeckDisplayValues[SelectedDeckIndex];
                return SpriteService.Instance.GetDeckImage(deckName);
            }
        }

        public Avalonia.Media.IImage? StakeOverlayImage
        {
            get
            {
                var stakeName = BalatroData.Stakes.Values.ElementAt(SelectedStakeIndex);
                return SpriteService.Instance.GetStakeImage(stakeName);
            }
        }

        public string DeckDescription
        {
            get
            {
                var deckName = DeckDisplayValues[SelectedDeckIndex];
                if (BalatroData.DeckDescriptions.TryGetValue(deckName, out var description))
                {
                    return description;
                }
                return "";
            }
        }

        // Deck/Stake display from Motely enums (no " Deck" / " Stake" suffix)
        public string[] DeckDisplayValues { get; } = Enum.GetNames(typeof(MotelyDeck));
        public string[] StakeDisplayValues { get; } = Enum.GetNames(typeof(MotelyStake));

        [ObservableProperty]
        private string _selectedWordList = "None";

        [ObservableProperty]
        private ObservableCollection<string> _availableWordLists = new();

        [ObservableProperty]
        private string _selectedDbList = "None";

        [ObservableProperty]
        private ObservableCollection<string> _availableDbLists = new();

        // Search Mode Properties
        [ObservableProperty]
        private SearchMode _selectedSearchMode = SearchMode.AllSeeds;

        public string[] SearchModeDisplayValues { get; } =
            new[] { "All Seeds", "Single Seed", "Word List", "DB List" };

        [ObservableProperty]
        private string _seedInput = string.Empty;

        [ObservableProperty]
        private bool _continueFromLast = false;

        // Visibility properties for mode-specific controls
        [ObservableProperty]
        private bool _isSmartAutoMode = true;

        [ObservableProperty]
        private bool _isThreadsVisible = true;

        [ObservableProperty]
        private bool _isContinueVisible = true;

        [ObservableProperty]
        private bool _isSeedInputVisible = false;

        [ObservableProperty]
        private bool _isWordListVisible = false;

        [ObservableProperty]
        private bool _isDbListVisible = false;

        public bool CanMinimizeToDesktopVisible => _search is not null;

        // WordList index properties for SpinnerControl binding
        [ObservableProperty]
        private int _selectedWordListIndex = 0;

        public int WordListMaxIndex => Math.Max(0, AvailableWordLists.Count - 1);

        // DBList index properties for SpinnerControl binding
        [ObservableProperty]
        private int _selectedDbListIndex = 0;

        public int DbListMaxIndex => Math.Max(0, AvailableDbLists.Count - 1);

        // Search parameters
        [ObservableProperty]
        private int _minScore = 0;

        [ObservableProperty]
        private bool _isDebugMode = false;

        [ObservableProperty]
        private string _debugSeed = string.Empty;

        // Console
        [ObservableProperty]
        private string _consoleText = "> Motely Search Console\n> Ready to search...\n";

        [ObservableProperty]
        private string _jsonValidationStatus = "JSON: Valid ✓";

        [ObservableProperty]
        private string _jsonValidationColor = "Green";

        // Stats properties
        [ObservableProperty]
        private double _progressPercent = 0.0;

        [ObservableProperty]
        private string _searchSpeed = "0 seeds/s";

        [ObservableProperty]
        private int _currentBatch = 0;

        [ObservableProperty]
        private int _maxBatch = 0;

        [ObservableProperty]
        private string _seedsProcessed = "0";

        [ObservableProperty]
        private string _timeElapsed = "00:00:00";

        [ObservableProperty]
        private string _estimatedTimeRemaining = "--:--:--";

        [ObservableProperty]
        private string _findRate = "0.00%";

        [ObservableProperty]
        private string _rarity = "--";

        // Search button dynamic properties - CRITICAL: State machine for PAUSE vs STOP
        public string CookButtonText
        {
            get
            {
                if (!IsSearching)
                    return ContinueFromLast ? "Resume Search" : "Start Search";

                // If Continue is enabled, show PAUSE (saves state)
                // If Continue is disabled, show STOP (doesn't save state)
                return ContinueFromLast ? "Pause Search" : "Stop Search";
            }
        }

        // Button color class - Blue when stopped, Yellow-Orange when running
        public string CookButtonClass => IsSearching ? "btn-warning" : "btn-blue";

        // Results filtering
        [ObservableProperty]
        private string _resultsFilterText = string.Empty;

        [ObservableProperty]
        private ObservableCollection<SearchResult> _filteredResults = new();

        public SearchModalViewModel(
            UserProfileService userProfileService,
            Func<AnalyzeModalViewModel> analyzeModalFactory
        )
        {
            _userProfileService = userProfileService;
            _analyzeModalFactory =
                analyzeModalFactory ?? throw new ArgumentNullException(nameof(analyzeModalFactory));

            SearchResults = new ObservableCollection<Models.SearchResult>();
            ConsoleOutput = new ObservableCollection<Models.ConsoleMessage>();

            // Set default values
            ThreadCount = Environment.ProcessorCount / 2;

            // Initialize dynamic tabs
            InitializeSearchTabs();
            CurrentTabContent = _searchTab ??= new Views.SearchModalTabs.SearchTab();

            // Initialize control visibility
            UpdateControlVisibility();
        }

        /// <summary>Creates an AnalyzeModalViewModel via DI factory (no ServiceHelper). Used by ResultsTab to show analyze modal.</summary>
        public AnalyzeModalViewModel CreateAnalyzeModalViewModel() => _analyzeModalFactory();

        /// <summary>
        /// Open the analyze modal pre-loaded with the given seed. Called from ResultsTab grid event.
        /// </summary>
        public void OpenAnalyzeModalForSeed(string? seed)
        {
            if (string.IsNullOrWhiteSpace(seed) || MainMenu == null)
            {
                return;
            }

            var analyzeVm = CreateAnalyzeModalViewModel();
            var analyzeModal = new AnalyzeModal(analyzeVm);
            analyzeModal.SetSeedAndAnalyze(seed);
            MainMenu.ShowModal("SEED ANALYZER", analyzeModal);
        }

        /// <summary>
        /// Request the View to open the pop-out DataGridResultsWindow for the active search.
        /// Window construction stays in the View; the VM only supplies the data + raises the event.
        /// </summary>
        public void RequestPopOutResults()
        {
            ShowDataGridResultsRequested?.Invoke(this, (this, LoadedConfig?.Name));
            BsoLogger.Log("SearchModalViewModel", "Requested pop-out results window");
        }

        /// <summary>
        /// Export the supplied search results as tab-separated seed/score text to a
        /// user-picked file. Pure business logic kept out of code-behind.
        /// </summary>
        public async Task ExportSearchResultsAsync(
            TopLevel? topLevel,
            IEnumerable<Models.SearchResult>? results
        )
        {
            if (topLevel == null || results == null || !results.Any())
            {
                BsoLogger.Log("SearchModalViewModel", "Nothing to export");
                return;
            }

            var file = await topLevel.StorageProvider.SaveFilePickerAsync(
                new Avalonia.Platform.Storage.FilePickerSaveOptions
                {
                    Title = "Export Search Results",
                    DefaultExtension = "txt",
                    SuggestedFileName = $"search_results_{DateTime.Now:yyyyMMdd_HHmmss}.txt",
                }
            );

            if (file == null)
            {
                BsoLogger.Log("SearchModalViewModel", "Export cancelled by user");
                return;
            }

            var sb = new StringBuilder();
            sb.AppendLine("Seed\tScore");
            foreach (var result in results)
            {
                sb.AppendLine($"{result.Seed}\t{result.TotalScore}");
            }

            await using var stream = await file.OpenWriteAsync();
            await using var writer = new StreamWriter(stream);
            await writer.WriteAsync(sb.ToString());
            BsoLogger.Log("SearchModalViewModel", $"Exported results to {file.Path.LocalPath}");
        }

        partial void OnSelectedTabIndexChanged(int value)
        {
            CurrentTabContent = value switch
            {
                0 => _searchTab ??= new Views.SearchModalTabs.SearchTab(),
                1 => _resultsTab ??= new Views.SearchModalTabs.ResultsTab(),
                _ => null,
            };
        }

        partial void OnIsSearchingChanged(bool value)
        {
            StopSearchCommand.NotifyCanExecuteChanged();
            PauseSearchCommand.NotifyCanExecuteChanged();
            OnPropertyChanged(nameof(CookButtonText));
            OnPropertyChanged(nameof(CookButtonClass));
        }

        partial void OnContinueFromLastChanged(bool value)
        {
            // Update button text when Continue checkbox changes
            OnPropertyChanged(nameof(CookButtonText));

            if (value && !IsSearching)
            {
                var state = _userProfileService.GetSearchState();
                if (state is not null
                    && !string.IsNullOrEmpty(CurrentFilterPath)
                    && string.Equals(state.ConfigPath, CurrentFilterPath, StringComparison.OrdinalIgnoreCase)
                    && state.TotalBatches > 0)
                {
                    ProgressPercent = Math.Clamp(
                        (double)state.LastCompletedBatch / state.TotalBatches * 100.0,
                        0.0,
                        100.0
                    );
                    AddConsoleMessage(
                        $"Saved progress found — batch {state.LastCompletedBatch:N0} of {state.TotalBatches:N0} ({ProgressPercent:F1}%)"
                    );
                }
                else
                {
                    ProgressPercent = 0.0;
                }
            }
        }

        partial void OnResultsFilterTextChanged(string value)
        {
            FilterResults();
        }

        #region Properties

        private Views.SearchModalTabs.SearchTab? _searchTab;
        private Views.SearchModalTabs.ResultsTab? _resultsTab;

        [ObservableProperty]
        private object? _currentTabContent;

        // Commands are auto-generated by [RelayCommand] attributes

        public int ThreadCount { get; set; } = Environment.ProcessorCount;
        public int MaxThreadCount { get; } = Environment.ProcessorCount; // Auto-detect CPU cores

        public string SearchStatus => IsSearching ? "Searching..." : "Ready";
        public double SearchProgress => LatestProgress?.PercentComplete ?? 0.0;
        public string ProgressText => LatestProgress?.ToString() ?? "No search active";
        public int ResultsCount => SearchResults.Count;

        public string CurrentSearchId => _currentSearchId;

        public ObservableCollection<TabItemViewModel> TabItems { get; } = new();
        public ObservableCollection<Models.SearchResult> SearchResults { get; }
        public ObservableCollection<Models.ConsoleMessage> ConsoleOutput { get; }

        #endregion

        #region Events

        public event EventHandler<string>? CreateShortcutRequested;
        public event EventHandler? CloseRequested;
        public event EventHandler? MaximizeToggleRequested;
        public event EventHandler<(
            string searchId,
            string? configPath,
            string filterName
        )>? MinimizeToDesktopRequested;
        public event EventHandler<string>? CopyToClipboardRequested;

        /// <summary>
        /// Raised by ResultsTab pop-out: the View constructs and shows the DataGridResultsWindow
        /// (window construction is a View concern; VM only supplies the data context).
        /// </summary>
        public event EventHandler<(SearchModalViewModel Search, string? FilterName)>? ShowDataGridResultsRequested;

        #endregion

        #region Command Implementations

        [RelayCommand(CanExecute = nameof(CanStartSearch))]
        private Task StartSearchAsync()
        {
            AddConsoleMessage("Starting search...");

            if (LoadedConfig is null)
            {
                AddConsoleMessage("No filter configuration loaded. Please load a filter first.");
                return Task.CompletedTask;
            }

            AddConsoleMessage($"Filter loaded: {LoadedConfig.Name}");
            AddConsoleMessage($"Search mode: {SearchModeDisplayValues[(int)SelectedSearchMode]}");

            // Validate mode-specific requirements
            if (
                SelectedSearchMode == SearchMode.SingleSeed
                && string.IsNullOrWhiteSpace(SeedInput)
            )
            {
                AddConsoleMessage("Please enter a seed name for Single Seed mode.");
                return Task.CompletedTask;
            }

            if (SelectedSearchMode is SearchMode.WordList or SearchMode.DbList)
            {
                AddConsoleMessage("This mode was removed. Sequential and single-seed only for now.");
                return Task.CompletedTask;
            }

            IsSearching = true;

            ClearResults();
            AddConsoleMessage(
                $"Starting search in {SearchModeDisplayValues[(int)SelectedSearchMode]} mode..."
            );
            PanelText = $"Searching with '{LoadedConfig.Name}'...";

            // Direct engine call: JamlConfig → plan (tally metadata) + settings (search).
            var plan = JamlSearchBuilder.CreatePlan(LoadedConfig, MinScore);
            _tallyLabels = plan.TallyLabels.ToArray();

            var settings = JamlSearchBuilder.CreateSettings(LoadedConfig, MinScore);

            if (
                !string.IsNullOrEmpty(DeckSelection)
                && DeckSelection != "All Decks"
                && Enum.TryParse<MotelyDeck>(DeckSelection, true, out var deck)
            )
            {
                settings = settings.WithDeck(deck);
            }

            if (
                !string.IsNullOrEmpty(StakeSelection)
                && StakeSelection != "All Stakes"
                && Enum.TryParse<MotelyStake>(StakeSelection, true, out var stake)
            )
            {
                settings = settings.WithStake(stake);
            }

            var threads =
                IsSmartAutoMode && SelectedSearchMode == SearchMode.AllSeeds
                    ? MaxThreadCount
                    : ThreadCount;
            settings = settings.WithThreadCount(threads);
            AddConsoleMessage($"Thread count: {threads}");

            if (SelectedSearchMode == SearchMode.SingleSeed)
            {
                settings = settings.WithSeedList(new[] { SeedInput.Trim() });
            }
            else
            {
                settings = settings.WithSequentialSearch().WithBatchCharacterCount(3);

                if (ContinueFromLast)
                {
                    var state = _userProfileService.GetSearchState();
                    if (
                        state is not null
                        && state.LastCompletedBatch > 0
                        && !string.IsNullOrEmpty(CurrentFilterPath)
                        && string.Equals(
                            state.ConfigPath,
                            CurrentFilterPath,
                            StringComparison.OrdinalIgnoreCase
                        )
                    )
                    {
                        settings = settings.WithStartBatchIndex((long)state.LastCompletedBatch);
                        AddConsoleMessage($"Resuming from batch {state.LastCompletedBatch:N0}.");
                    }
                    else
                    {
                        AddConsoleMessage("No saved progress for this filter — starting fresh.");
                    }
                }
                else
                {
                    AddConsoleMessage("Starting search from the beginning (batch 0).");
                }
            }

            // Callbacks fire on worker threads — marshal to the UI thread.
            settings = settings.WithProgressCallback(p =>
                Avalonia.Threading.Dispatcher.UIThread.Post(() => OnProgressUpdated(p))
            );

            if (plan.ScoreTallyColumnCount > 0)
            {
                settings = settings.WithScoredResultCallback(tally =>
                {
                    var seed = tally.Seed;
                    var score = tally.Score;
                    var scores = tally.TallyValuesSpan.ToArray();
                    Avalonia.Threading.Dispatcher.UIThread.Post(() =>
                        AddSearchResult(seed, score, scores)
                    );
                });
            }
            else
            {
                settings = settings.WithSeedMatchCallback(seed =>
                    Avalonia.Threading.Dispatcher.UIThread.Post(() =>
                        AddSearchResult(seed, 0, null)
                    )
                );
            }

            _currentSearchId = Guid.NewGuid().ToString("N");
            _searchCts = new CancellationTokenSource();
            _search = settings.Start(_searchCts.Token);

            MinimizeToDesktopCommand.NotifyCanExecuteChanged();
            OnPropertyChanged(nameof(CanMinimizeToDesktopVisible));

            AddConsoleMessage($"Search started with ID: {_currentSearchId}");
            BsoLogger.Log("SearchModalViewModel", $"Search started with ID: {_currentSearchId}");

            // Configure search transition (if enabled by user)
            ConfigureSearchTransition();

            _searchCompletionTask = TrackSearchCompletionAsync(_search, _searchCts.Token);
            return Task.CompletedTask;
        }

        private async Task TrackSearchCompletionAsync(
            Motely.IMotelySearch search,
            CancellationToken token
        )
        {
            bool stopped = false;
            try
            {
                await search.WaitForCompletionAsync(token);
            }
            catch (OperationCanceledException)
            {
                // User stop — treat as completion.
                stopped = true;
            }
            Avalonia.Threading.Dispatcher.UIThread.Post(() => OnSearchFinished(stopped));
        }

        private void OnSearchFinished(bool stopped)
        {
            IsSearching = false;

            if (stopped)
            {
                AddConsoleMessage($"Search stopped. Found {SearchResults.Count} results.");
                PanelText = $"Search stopped: {SearchResults.Count} seeds";
            }
            else
            {
                // A finished search has nothing to resume — drop the saved state so
                // the desktop icon doesn't reappear on next launch.
                _userProfileService.ClearSearchState();
                AddConsoleMessage($"Search completed. Found {SearchResults.Count} results.");
                PanelText = $"Search complete: {SearchResults.Count} seeds";
            }

            ActiveSearchTransition = null;
            OnPropertyChanged(nameof(ResultsCount));
            BsoLogger.Log(
                "SearchModalViewModel",
                $"Search {(stopped ? "stopped" : "completed")} with {SearchResults.Count} results"
            );
        }

        private bool CanStartSearch()
        {
            return !IsSearching && LoadedConfig is not null;
        }

        [RelayCommand(CanExecute = nameof(CanStopSearch))]
        private void StopSearch()
        {
            if (ContinueFromLast && LatestProgress is not null)
            {
                // PAUSE mode: persist the exact batch the user paused at.
                AddConsoleMessage("Pausing search and saving progress...");
                SaveResumeState(LatestProgress.CompletedBatchCount, LatestProgress.TotalBatchCount);
            }
            else
            {
                AddConsoleMessage("Stopping search (progress will NOT be saved)...");
            }

            _searchCts?.Cancel();
            _search?.Dispose();
            _search = null;
            IsSearching = false;

            MinimizeToDesktopCommand.NotifyCanExecuteChanged();
            OnPropertyChanged(nameof(CanMinimizeToDesktopVisible));

            // Clear search transition when search stops
            ActiveSearchTransition = null;
        }

        private bool CanStopSearch()
        {
            return IsSearching;
        }

        [RelayCommand(CanExecute = nameof(CanMinimizeToDesktop))]
        private void MinimizeToDesktop()
        {
            if (_search is null)
            {
                BsoLogger.LogError(
                    "SearchModalViewModel",
                    "Cannot minimize - no active search"
                );
                return;
            }

            var filterName = LoadedConfig?.Name ?? "Unknown Filter";

            BsoLogger.Log(
                "SearchModalViewModel",
                $"Minimizing search to desktop: SearchID={_currentSearchId}, Filter={filterName}, ConfigPath={CurrentFilterPath}"
            );

            // Raise event for View to handle (creates SearchDesktopIcon and closes modal)
            MinimizeToDesktopRequested?.Invoke(
                this,
                (_currentSearchId, CurrentFilterPath, filterName)
            );

            AddConsoleMessage($"Search '{filterName}' minimized to desktop widget");
        }

        private bool CanMinimizeToDesktop()
        {
            // Can minimize if a search is running
            return _search is not null;
        }

        [RelayCommand]
        private void ClearResults()
        {
            SearchResults.Clear();
            ConsoleOutput.Clear();
            LastKnownResultCount = 0;
            LatestProgress = null;
            PanelText = "Tip: Results appear below. Use Export to save seeds.";
            BsoLogger.Log("SearchModalViewModel", "Results cleared");
        }

        [RelayCommand]
        private Task LoadFilterAsync()
        {
            // This would typically show a file dialog
            AddConsoleMessage("Filter loading functionality needs UI implementation");
            BsoLogger.Log("SearchModalViewModel", "Load filter requested");
            return Task.CompletedTask;
        }

        partial void OnSelectedDeckIndexChanged(int value)
        {
            if (value >= 0 && value < DeckDisplayValues.Length)
            {
                DeckSelection = DeckDisplayValues[value];
                OnPropertyChanged(nameof(DeckImage));
                OnPropertyChanged(nameof(DeckDescription));
            }
        }

        partial void OnSelectedStakeIndexChanged(int value)
        {
            if (value >= 0 && value < StakeDisplayValues.Length)
            {
                StakeSelection = StakeDisplayValues[value];
                OnPropertyChanged(nameof(StakeOverlayImage));
            }
        }

        partial void OnSelectedSearchModeChanged(SearchMode value)
        {
            UpdateControlVisibility();

            // Force thread count for Single Seed mode
            if (value == SearchMode.SingleSeed)
            {
                ThreadCount = 1;
            }
        }

        partial void OnSelectedWordListIndexChanged(int value)
        {
            if (value >= 0 && value < AvailableWordLists.Count)
            {
                SelectedWordList = AvailableWordLists[value];
            }
        }

        partial void OnSelectedDbListIndexChanged(int value)
        {
            if (value >= 0 && value < AvailableDbLists.Count)
            {
                SelectedDbList = AvailableDbLists[value];
            }
        }

        private void UpdateControlVisibility()
        {
            switch (SelectedSearchMode)
            {
                case SearchMode.AllSeeds:
                    IsThreadsVisible = true;
                    IsContinueVisible = true;
                    IsSeedInputVisible = false;
                    IsWordListVisible = false;
                    IsDbListVisible = false;
                    break;

                case SearchMode.SingleSeed:
                    IsThreadsVisible = false;
                    IsContinueVisible = false;
                    IsSeedInputVisible = true;
                    IsWordListVisible = false;
                    IsDbListVisible = false;
                    break;

                case SearchMode.WordList:
                    IsThreadsVisible = true;
                    IsContinueVisible = false; // Wordlists don't support continue
                    IsSeedInputVisible = false;
                    IsWordListVisible = true;
                    IsDbListVisible = false;
                    break;

                case SearchMode.DbList:
                    IsThreadsVisible = false; // DB queries don't need threads
                    IsContinueVisible = false; // DB queries don't support continue
                    IsSeedInputVisible = false;
                    IsWordListVisible = false;
                    IsDbListVisible = true;
                    break;
            }

            // Notify property changed for WordListMaxIndex and DbListMaxIndex
            OnPropertyChanged(nameof(WordListMaxIndex));
            OnPropertyChanged(nameof(DbListMaxIndex));
        }

        public async Task LoadFilterAsync(string configPath)
        {
            await LoadConfigFromPathAsync(configPath);
            PanelText =
                $"Filter loaded: {LoadedConfig?.Name ?? Path.GetFileNameWithoutExtension(configPath)}";
        }

        [RelayCommand]
        private void CreateShortcut(string? searchId)
        {
            if (!string.IsNullOrEmpty(searchId))
            {
                CreateShortcutRequested?.Invoke(this, searchId);
            }
        }

        [RelayCommand]
        private void EditFilter()
        {
            _editFilterRequestedAction?.Invoke(CurrentFilterPath);
        }

        [RelayCommand]
        private void Close()
        {
            BsoLogger.Log("SearchModalViewModel", "Closing modal");
            CloseRequested?.Invoke(this, EventArgs.Empty);
        }

        [RelayCommand]
        private void Maximize()
        {
            // Request maximize toggle - the View will handle finding the window
            MaximizeToggleRequested?.Invoke(this, EventArgs.Empty);
        }

        [RelayCommand]
        private void SelectTab(object? parameter)
        {
            if (parameter is int tabIndex)
            {
                SelectedTabIndex = tabIndex;
                UpdateTabVisibility(tabIndex);
            }
            else if (
                parameter is string tabIndexStr
                && int.TryParse(tabIndexStr, out int parsedIndex)
            )
            {
                SelectedTabIndex = parsedIndex;
                UpdateTabVisibility(parsedIndex);
            }
        }

        /// <summary>
        /// Set callback for CREATE NEW FILTER button (called from View)
        /// </summary>
        public void SetNewFilterRequestedCallback(Action callback)
        {
            BsoLogger.Log("SearchModalViewModel", "SetNewFilterRequestedCallback called");
            _newFilterRequestedAction = callback;
        }

        /// <summary>
        /// Set callback for EDIT FILTER button (called from View)
        /// </summary>
        public void SetEditFilterRequestedCallback(Action<string?> callback)
        {
            _editFilterRequestedAction = callback;
        }

        /// <summary>
        /// Handle CREATE NEW FILTER request from FilterSelector
        /// </summary>
        public void OnNewFilterRequested()
        {
            BsoLogger.Log("SearchModalViewModel", "OnNewFilterRequested called");
            if (_newFilterRequestedAction is not null)
            {
                BsoLogger.Log("SearchModalViewModel", "Invoking new filter requested callback");
                _newFilterRequestedAction.Invoke();
            }
            else
            {
                BsoLogger.LogError("SearchModalViewModel", "New filter requested action is null!");
            }
        }

        /// <summary>
        /// PROPER MVVM: Update tab visibility when tab selection changes
        /// Ensures only ONE tab is visible at a time
        /// </summary>
        public void UpdateTabVisibility(int tabIndex)
        {
            // Hide ALL tabs first
            IsSettingsTabVisible = false;
            IsSearchTabVisible = false;
            IsResultsTabVisible = false;

            // Show only the selected tab
            switch (tabIndex)
            {
                case 0:
                    IsSettingsTabVisible = true;
                    break;
                case 1:
                    IsSearchTabVisible = true;
                    break;
                case 2:
                    IsResultsTabVisible = true;
                    break;
            }

            BsoLogger.Log("SearchModalViewModel", $"Switched to tab {tabIndex}");
        }

        /// <summary>
        /// Implements in-modal back navigation for progressive/tabbed flow.
        /// Returns true if navigation occurred; false to signal modal should close.
        /// </summary>
        public bool TryGoBack()
        {
            if (SelectedTabIndex > 0)
            {
                var newIndex = SelectedTabIndex - 1;
                SelectedTabIndex = newIndex;
                UpdateTabVisibility(newIndex);
                return true;
            }
            return false;
        }

        [RelayCommand]
        private void EnterVibeOutMode()
        {
            // Feature removed
        }

        [RelayCommand]
        private async Task ToggleSearchAsync()
        {
            if (IsSearching)
            {
                StopSearch();
            }
            else
            {
                await StartSearchAsync();
            }
        }

        [RelayCommand]
        private void ClearConsole()
        {
            ConsoleText = "> Motely Search Console\n> Ready to search...\n";
            ConsoleOutput.Clear();
            BsoLogger.Log("SearchModalViewModel", "Console cleared");
        }

        [RelayCommand]
        private void RefreshResults()
        {
            FilterResults();
            BsoLogger.Log("SearchModalViewModel", "Results refreshed");
        }

        [RelayCommand]
        private void SortBySeed()
        {
            var sorted = FilteredResults.OrderBy(r => r.Seed).ToList();
            FilteredResults.Clear();
            foreach (var result in sorted)
            {
                FilteredResults.Add(result);
            }
            BsoLogger.Log("SearchModalViewModel", "Results sorted by seed");
        }

        [RelayCommand]
        private void SortByScore()
        {
            var sorted = FilteredResults.OrderByDescending(r => r.TotalScore).ToList();
            FilteredResults.Clear();
            foreach (var result in sorted)
            {
                FilteredResults.Add(result);
            }
            BsoLogger.Log("SearchModalViewModel", "Results sorted by score");
        }

        [RelayCommand]
        private async Task CopySeedsAsync()
        {
            var seeds = string.Join("\n", FilteredResults.Select(r => r.Seed));
            if (!string.IsNullOrEmpty(seeds) && MainMenu is not null)
            {
                var clipboard = TopLevel.GetTopLevel(MainMenu)?.Clipboard;
                if (clipboard is not null)
                {
                    await clipboard.SetTextAsync(seeds);
                    AddConsoleMessage($"Copied {FilteredResults.Count} seeds to clipboard");
                    BsoLogger.Log(
                        "SearchModalViewModel",
                        $"Copied {FilteredResults.Count} seeds to clipboard"
                    );
                }
            }
        }

        private void FilterResults()
        {
            FilteredResults.Clear();

            if (string.IsNullOrWhiteSpace(ResultsFilterText))
            {
                // No filter - show all results
                foreach (var result in SearchResults)
                {
                    FilteredResults.Add(result);
                }
            }
            else
            {
                // Filter by seed name
                var filter = ResultsFilterText.ToLowerInvariant();
                foreach (
                    var result in SearchResults.Where(r =>
                        r.Seed.ToLowerInvariant().Contains(filter)
                    )
                )
                {
                    FilteredResults.Add(result);
                }
            }
        }

        #endregion

        #region Helper Methods

        /// <summary>
        /// Add a found seed to the results collection. Always called on the UI thread
        /// (engine callbacks are marshalled via Dispatcher before landing here).
        /// </summary>
        private void AddSearchResult(string seed, int score, int[]? scores)
        {
            if (string.IsNullOrWhiteSpace(seed))
                return;

            var result = new Models.SearchResult
            {
                Seed = seed,
                TotalScore = score,
                Scores = scores,
            };

            // Set labels only on the first result to drive grid headers
            if (SearchResults.Count == 0 && _tallyLabels.Length > 0)
            {
                result.Labels = _tallyLabels;
            }

            SearchResults.Add(result);
            AddSeedFoundMessage(seed, score);
            PanelText = $"Found {SearchResults.Count} seeds so far...";
            OnPropertyChanged(nameof(ResultsCount));
        }

        private void AddConsoleMessage(string message)
        {
            var timestamp = DateTime.Now.ToString("HH:mm:ss");
            var formattedMessage = $"[{timestamp}] {message}";

            var consoleMessage = new Models.ConsoleMessage
            {
                Text = formattedMessage,
                CopyableText = null, // No copy button for regular messages
            };
            ConsoleOutput.Add(consoleMessage);

            // Update the ConsoleText binding
            ConsoleText += formattedMessage + "\n";

            // Keep console output manageable
            while (ConsoleOutput.Count > 1000)
            {
                ConsoleOutput.RemoveAt(0);
            }
        }

        /// <summary>
        /// Adds a seed found message to the console with a copy button
        /// </summary>
        private void AddSeedFoundMessage(string seed, int score)
        {
            var timestamp = DateTime.Now.ToString("HH:mm:ss");
            var formattedMessage = $"[{timestamp}] Found seed: {seed} (Score: {score})";

            var consoleMessage = new Models.ConsoleMessage
            {
                Text = formattedMessage,
                CopyableText = seed, // Copy button will copy just the seed name
                CopyCommand = new RelayCommand(() =>
                {
                    // Copy seed to clipboard - will be handled by the View
                    CopyToClipboardRequested?.Invoke(this, seed);
                }),
            };
            ConsoleOutput.Add(consoleMessage);

            // Update the ConsoleText binding
            ConsoleText += formattedMessage + "\n";

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
            _searchCts?.Cancel();
            _search?.Dispose();
        }

        [RelayCommand(CanExecute = nameof(CanPauseSearch))]
        private void PauseSearch()
        {
            // Engine has no pause — same as stop (resume state saved when Continue is on).
            StopSearch();
        }

        private bool CanPauseSearch() => IsSearching;

        [RelayCommand(CanExecute = nameof(CanExportResults))]
        private async Task ExportResults()
        {
            if (SearchResults.Count == 0)
                return;

            var sb = new StringBuilder();
            sb.AppendLine("Seed\tScore");
            foreach (var result in SearchResults)
                sb.AppendLine($"{result.Seed}\t{result.TotalScore}");

            var filePath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.Desktop),
                $"search_results_{DateTime.Now:yyyyMMdd_HHmmss}.txt"
            );
            await File.WriteAllTextAsync(filePath, sb.ToString());
            AddConsoleMessage($"Results exported to: {filePath}");
        }

        private bool CanExportResults() => SearchResults.Count > 0;

        /// <summary>
        /// Load configuration from file path. Only .jaml files are filters.
        /// </summary>
        public async Task LoadConfigFromPathAsync(string configPath)
        {
            BsoLogger.LogImportant(
                "SearchModalViewModel",
                $"LoadConfigFromPathAsync called with: {configPath}"
            );

            if (!File.Exists(configPath))
            {
                BsoLogger.LogError(
                    "SearchModalViewModel",
                    $"Filter file not found: {configPath}"
                );
                return;
            }

            if (
                !string.Equals(
                    Path.GetExtension(configPath),
                    ".jaml",
                    StringComparison.OrdinalIgnoreCase
                )
            )
            {
                BsoLogger.LogError(
                    "SearchModalViewModel",
                    $"Not a .jaml filter file: {configPath}"
                );
                return;
            }

            var content = await File.ReadAllTextAsync(configPath);

            if (
                !Motely.Filters.Jaml.JamlConfigLoader.TryLoad(
                    content,
                    out var config,
                    out var jamlError
                )
                || config is null
            )
            {
                BsoLogger.LogError(
                    "SearchModalViewModel",
                    $"JAML parse error: {jamlError ?? "Unknown"}"
                );
                return;
            }

            LoadedConfig = config;
            CurrentFilterPath = configPath; // CRITICAL: Store the path for the search!
            BsoLogger.LogImportant(
                "SearchModalViewModel",
                $"CurrentFilterPath SET TO: {CurrentFilterPath}"
            );

            // Update deck and stake from the loaded config
            var deckString = config.Deck.ToString();
            if (!string.IsNullOrEmpty(deckString))
            {
                DeckSelection = deckString;
                SelectedDeckIndex = Array.FindIndex(
                    DeckDisplayValues,
                    d => string.Equals(d, deckString, StringComparison.OrdinalIgnoreCase)
                );
            }

            var stakeString = config.Stake.ToString();
            if (!string.IsNullOrEmpty(stakeString))
            {
                StakeSelection = stakeString;
                SelectedStakeIndex = Array.FindIndex(
                    StakeDisplayValues,
                    s => s == stakeString
                );
            }

            BsoLogger.Log(
                "SearchModalViewModel",
                $"Successfully loaded filter: {config.Name} (Deck: {config.Deck}, Stake: {config.Stake})"
            );

            // Switch to the Search tab so user can start searching
            SelectedTabIndex = 1; // Search tab (Deck/Stake removed)
        }

        // Track last console log time to avoid spamming
        private DateTime _lastConsoleLog = DateTime.MinValue;
        private DateTime _lastProgressLog = DateTime.MinValue;

        /// <summary>
        /// Handle engine progress. Always called on the UI thread — the engine's progress
        /// callback marshals here via Dispatcher.UIThread.Post.
        /// </summary>
        private void OnProgressUpdated(MotelyProgress e)
        {
            LatestProgress = e;

            // Log progress to console every 2 seconds so user knows search is working
            var now = DateTime.Now;
            if ((now - _lastProgressLog).TotalSeconds >= 2.0)
            {
                _lastProgressLog = now;
                AddConsoleMessage(
                    $"Progress: {e.SeedsSearched:N0} seeds | {e.SeedsPerMillisecond:F1} seeds/ms | {e.MatchingSeeds} found"
                );
            }

            // OPTIONAL: Apply search transition if configured (progress-driven shader effects)
            if (ActiveSearchTransition is not null && MainMenu is not null)
            {
                // Update transition progress (0-100% → 0.0-1.0)
                ActiveSearchTransition.CurrentProgress = (float)(e.PercentComplete / 100.0);
                var interpolatedParams = ActiveSearchTransition.GetInterpolatedParameters();
                ApplyShaderParametersToMainMenu(MainMenu, interpolatedParams);
            }

            // Save state every 10 batches (only for AllSeeds mode)
            if (
                SelectedSearchMode == SearchMode.AllSeeds
                && e.CompletedBatchCount > 0
                && e.CompletedBatchCount % 10 == 0
                && !string.IsNullOrEmpty(CurrentFilterPath)
            )
            {
                SaveResumeState(e.CompletedBatchCount, e.TotalBatchCount);
            }

            UpdateUIFromProgress(e);
        }

        private void SaveResumeState(long completedBatch, long totalBatchCount)
        {
            if (string.IsNullOrEmpty(CurrentFilterPath))
                return;

            _userProfileService.SaveSearchState(
                new Models.SearchResumeState
                {
                    ConfigPath = CurrentFilterPath,
                    LastCompletedBatch = (ulong)Math.Max(0, completedBatch),
                    EndBatch = ulong.MaxValue,
                    BatchSize = 3,
                    ThreadCount = ThreadCount,
                    MinScore = MinScore,
                    Deck = DeckSelection,
                    Stake = StakeSelection,
                    LastActiveTime = DateTime.UtcNow,
                    TotalBatches = (ulong)Math.Max(0, totalBatchCount),
                }
            );
        }

        // Update UI stats from engine progress (already on the UI thread)
        private void UpdateUIFromProgress(MotelyProgress e)
        {
            // Calculate seeds per second once for use in multiple places
            double seedsPerSecond = e.SeedsPerMillisecond * 1000.0;

            var now = DateTime.Now;
            if ((now - _lastConsoleLog).TotalSeconds >= 5)
            {
                AddConsoleMessage(
                    $"Progress: {e.PercentComplete:0.00}% (~{seedsPerSecond:N0} seeds/s) {e.MatchingSeeds} results"
                );
                _lastConsoleLog = now;
            }

            // Log first result found as immediate feedback
            if (e.MatchingSeeds == 1 && LastKnownResultCount == 0)
            {
                AddConsoleMessage($"First result found!");
            }

            // Update all stats properties
            ProgressPercent = e.PercentComplete;
            SearchSpeed = FormatSeedSpeed(seedsPerSecond);
            SeedsProcessed = FormatSeedsCount(e.SeedsSearched);
            TimeElapsed = TimeSpan.FromMilliseconds(e.ElapsedMilliseconds).ToString(@"hh\:mm\:ss");
            EstimatedTimeRemaining = e.EstimatedTimeRemainingMilliseconds.HasValue
                ? TimeSpan
                    .FromMilliseconds(e.EstimatedTimeRemainingMilliseconds.Value)
                    .ToString(@"hh\:mm\:ss")
                : "--:--:--";

            CurrentBatch = (int)Math.Min(e.CompletedBatchCount, int.MaxValue);
            MaxBatch = (int)Math.Min(e.TotalBatchCount, int.MaxValue);

            // Smart Rate Formatting - Adaptive precision based on rarity tier
            if (e.SeedsSearched > 0 && e.MatchingSeeds > 0)
            {
                double rate = (double)e.MatchingSeeds / e.SeedsSearched * 100.0;
                if (rate >= 1.0)
                    FindRate = $"{rate:0.00}%"; // Common: 5.67%
                else if (rate >= 0.01)
                    FindRate = $"{rate:0.000}%"; // Uncommon: 0.234%
                else if (rate >= 0.0001)
                    FindRate = $"{rate:0.0000}%"; // Rare: 0.0123%
                else if (rate > 0)
                    FindRate = $"{rate:0.00000}%"; // Mythical: 0.00023%
                else
                    FindRate = "0.00%";
            }
            else
            {
                FindRate = "0.00%";
            }

            // Smart Rarity Formatting with K/M/B/T suffixes (NO SPM!)
            if (e.MatchingSeeds > 0 && e.SeedsSearched > 0)
            {
                long rarity = e.SeedsSearched / e.MatchingSeeds;
                if (rarity >= 1_000_000_000_000)
                    Rarity = $"1 in {rarity / 1_000_000_000_000.0:0.00}T"; // 1 in 2.67T
                else if (rarity >= 1_000_000_000)
                    Rarity = $"1 in {rarity / 1_000_000_000.0:0.00}B"; // 1 in 42.67B
                else if (rarity >= 1_000_000)
                    Rarity = $"1 in {rarity / 1_000_000.0:0.00}M"; // 1 in 42.67M
                else if (rarity >= 10_000)
                    Rarity = $"1 in {rarity / 1_000.0:0.0}K"; // 1 in 42.7K
                else
                    Rarity = $"1 in {rarity:N0}"; // 1 in 427
            }
            else
            {
                Rarity = "--"; // Show placeholder until first result
            }

            OnPropertyChanged(nameof(SearchProgress));
            OnPropertyChanged(nameof(ProgressText));
            OnPropertyChanged(nameof(ResultsCount));
            PanelText = $"{e.MatchingSeeds} seeds | {e.PercentComplete:0}%";

            // If results increased since last update, log the new seeds found
            if (e.MatchingSeeds > LastKnownResultCount)
            {
                var newSeedsCount = e.MatchingSeeds - LastKnownResultCount;
                if (newSeedsCount == 1)
                {
                    AddConsoleMessage($"Found new seed! Total: {e.MatchingSeeds}");
                }
                else
                {
                    AddConsoleMessage($"Found {newSeedsCount} new seeds! Total: {e.MatchingSeeds}");
                }
                LastKnownResultCount = (int)Math.Min(e.MatchingSeeds, int.MaxValue);
            }
        }

        /// <summary>
        /// Initialize dynamic tabs for consistent Balatro styling
        /// </summary>
        private void InitializeSearchTabs()
        {
            // Headers only; CurrentTabContent constructs the tab views lazily.
            TabItems.Clear();
            TabItems.Add(new TabItemViewModel("Search"));
            TabItems.Add(new TabItemViewModel("Results"));
        }

        /// <summary>
        /// Format seed speed with K/M abbreviations (no decimals)
        /// </summary>
        private static string FormatSeedSpeed(double seedsPerSecond)
        {
            if (seedsPerSecond >= 1_000_000)
            {
                return $"{seedsPerSecond / 1_000_000:0}M/s";
            }
            else if (seedsPerSecond >= 1_000)
            {
                return $"{seedsPerSecond / 1_000:0}K/s";
            }
            else
            {
                return $"{seedsPerSecond:0}/s";
            }
        }

        private static string FormatSeedsCount(long count)
        {
            if (count >= 1_000_000)
            {
                return $"{count / 1_000_000.0:0}M";
            }
            else if (count >= 1_000)
            {
                return $"{count / 1_000.0:0}K";
            }
            else
            {
                return $"{count:N0}";
            }
        }

        #endregion

        #region Shader Transition Helpers

        /// <summary>
        /// Configures the search transition based on user profile settings.
        /// Called when a search starts. If enabled, creates a transition from configured start/end presets.
        /// </summary>
        private void ConfigureSearchTransition()
        {
            var settings = _userProfileService.GetProfile().VisualizerSettings;

            if (!settings.EnableSearchTransition)
            {
                ActiveSearchTransition = null;
                BsoLogger.Log("SearchModalViewModel", "Search transitions disabled by user");
                return;
            }

            // Load start and end presets (or use defaults)
            var startParams = LoadPresetParameters(
                settings.SearchTransitionStartPresetName ?? "Default Balatro",
                true
            );
            var endParams = LoadPresetParameters(
                settings.SearchTransitionEndPresetName ?? "Default Balatro",
                false
            );

            // Create transition
            ActiveSearchTransition = new Models.VisualizerPresetTransition
            {
                StartParameters = startParams,
                EndParameters = endParams,
                CurrentProgress = 0f,
            };

            BsoLogger.Log(
                "SearchModalViewModel",
                $"Search transition configured: Start='{settings.SearchTransitionStartPresetName ?? "Default Balatro"}', End='{settings.SearchTransitionEndPresetName ?? "Default Balatro"}'"
            );
        }

        /// <summary>
        /// Loads shader parameters from a preset name, or returns defaults if not found.
        /// </summary>
        private Models.ShaderParameters LoadPresetParameters(string? presetName, bool isDarkPreset)
        {
            // If no preset name specified or it's a default preset, use built-in defaults
            if (string.IsNullOrWhiteSpace(presetName) || presetName == "Default Balatro")
            {
                return isDarkPreset
                    ? VisualizerPresetExtensions.CreateDefaultIntroParameters()
                    : VisualizerPresetExtensions.CreateDefaultNormalParameters();
            }

            // Try to load custom preset from disk
            var presets = PresetHelper.LoadAllPresets();
            var preset = presets.FirstOrDefault(p => p.Name == presetName);

            if (preset is not null)
            {
                return preset.ToShaderParameters();
            }

            BsoLogger.Log(
                "SearchModalViewModel",
                $"Preset '{presetName}' not found, using defaults"
            );
            return isDarkPreset
                ? VisualizerPresetExtensions.CreateDefaultIntroParameters()
                : VisualizerPresetExtensions.CreateDefaultNormalParameters();
        }

        /// <summary>
        /// Applies shader parameters to BalatroMainMenu's shader background.
        /// Called when ActiveSearchTransition is set and search progress updates.
        /// </summary>
        private void ApplyShaderParametersToMainMenu(
            Views.BalatroMainMenu mainMenu,
            Models.ShaderParameters parameters
        )
        {
            if (
                mainMenu.ShaderBackground
                is BalatroSeedOracle.Controls.BalatroShaderBackground shaderBackground
            )
            {
                // Apply all shader parameters
                shaderBackground.SetTime(parameters.TimeSpeed);
                shaderBackground.SetSpinTime(parameters.SpinTimeSpeed);
                shaderBackground.SetMainColor(parameters.MainColor);
                shaderBackground.SetAccentColor(parameters.AccentColor);
                shaderBackground.SetBackgroundColor(parameters.BackgroundColor);
                shaderBackground.SetContrast(parameters.Contrast);
                shaderBackground.SetSpinAmount(parameters.SpinAmount);
                shaderBackground.SetParallax(parameters.ParallaxX, parameters.ParallaxY);
                shaderBackground.SetZoomScale(parameters.ZoomScale);
                shaderBackground.SetSaturationAmount(parameters.SaturationAmount);
                shaderBackground.SetSaturationAmount2(parameters.SaturationAmount2);
                shaderBackground.SetPixelSize(parameters.PixelSize);
                shaderBackground.SetSpinEase(parameters.SpinEase);
                shaderBackground.SetLoopCount(parameters.LoopCount);
            }
        }

        #endregion
    }
}

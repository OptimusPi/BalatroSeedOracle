using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;
using Avalonia.Threading;
using BalatroSeedOracle.Helpers;
using BalatroSeedOracle.Models;
using BalatroSeedOracle.Services;
using CommunityToolkit.Mvvm.Input;

namespace BalatroSeedOracle.ViewModels
{
    /// <summary>
    /// ViewModel for the DayLatroWidget
    /// Manages all state, business logic, and commands for the daily Balatro challenge widget
    /// </summary>
    public class DayLatroWidgetViewModel : BaseWidgetViewModel, IDisposable
    {
        public event EventHandler<string>? CopyToClipboardRequested;

        #region Services (Injected)

        private readonly DaylatroHighScoreService _scoreService;
        private readonly UserProfileService _profileService;

        #endregion

        #region Private Fields

        private DispatcherTimer? _autoRefreshTimer;
        private CancellationTokenSource? _loadingCancellation;
        private DateTime _lastCheckedDate;
        private Func<AnalyzeModalViewModel>? _analyzeModalFactory;

        #endregion

        /// <summary>Set by parent (BalatroMainMenu) so widget can create analyze modal VM without ServiceHelper.</summary>
        public void SetAnalyzeModalFactory(Func<AnalyzeModalViewModel> factory) => _analyzeModalFactory = factory;

        /// <summary>Creates an AnalyzeModalViewModel when factory was set by parent; otherwise returns null.</summary>
        public AnalyzeModalViewModel? CreateAnalyzeModalViewModel() => _analyzeModalFactory?.Invoke();

        #region Observable Properties

        private bool _showNewDayBadge;

        /// <summary>
        /// Whether to show the new day notification badge
        /// </summary>
        public bool ShowNewDayBadge
        {
            get => _showNewDayBadge;
            set => SetProperty(ref _showNewDayBadge, value);
        }

        private string _dateText = "Daylatro";

        /// <summary>
        /// Formatted date text (e.g., "Daylatro - Jan 01, 2025")
        /// </summary>
        public string DateText
        {
            get => _dateText;
            set => SetProperty(ref _dateText, value);
        }

        private string _todaySeed = "LOADING...";

        /// <summary>
        /// Today's daily challenge seed
        /// </summary>
        public string TodaySeed
        {
            get => _todaySeed;
            set => SetProperty(ref _todaySeed, value);
        }

        private string _scoreInput = string.Empty;

        /// <summary>
        /// User input for score submission
        /// </summary>
        public string ScoreInput
        {
            get => _scoreInput;
            set
            {
                if (SetProperty(ref _scoreInput, value))
                {
                    ((AsyncRelayCommand)SubmitScoreCommand).NotifyCanExecuteChanged();
                }
            }
        }

        private string _initialsInput = string.Empty;

        /// <summary>
        /// User input for initials (max 3 characters)
        /// </summary>
        public string InitialsInput
        {
            get => _initialsInput;
            set
            {
                if (SetProperty(ref _initialsInput, value))
                {
                    ((AsyncRelayCommand)SubmitScoreCommand).NotifyCanExecuteChanged();
                }
            }
        }

        private string _anteInput = string.Empty;

        /// <summary>
        /// User input for ante level
        /// </summary>
        public string AnteInput
        {
            get => _anteInput;
            set
            {
                if (SetProperty(ref _anteInput, value))
                {
                    ((AsyncRelayCommand)SubmitScoreCommand).NotifyCanExecuteChanged();
                }
            }
        }

        private ObservableCollection<HighScoreDisplayItem> _highScores = new();

        /// <summary>
        /// Collection of high scores to display
        /// </summary>
        public ObservableCollection<HighScoreDisplayItem> HighScores
        {
            get => _highScores;
            set => SetProperty(ref _highScores, value);
        }

        private string _submissionMessage = string.Empty;

        /// <summary>
        /// Message to display after score submission
        /// </summary>
        public string SubmissionMessage
        {
            get => _submissionMessage;
            set => SetProperty(ref _submissionMessage, value);
        }

        private bool _submissionMessageIsError;

        /// <summary>
        /// Whether the submission message is an error
        /// </summary>
        public bool SubmissionMessageIsError
        {
            get => _submissionMessageIsError;
            set => SetProperty(ref _submissionMessageIsError, value);
        }

        private bool _isLoadingScores;

        /// <summary>
        /// Whether scores are currently being loaded
        /// </summary>
        public bool IsLoadingScores
        {
            get => _isLoadingScores;
            set => SetProperty(ref _isLoadingScores, value);
        }

        #endregion

        #region Commands

        // Expand/Minimize commands inherited from BaseWidgetViewModel
        public ICommand SubmitScoreCommand { get; }
        public ICommand CopySeedCommand { get; }
        public ICommand RefreshScoresCommand { get; }
        public ICommand AnalyzeSeedCommand { get; }
        public ICommand OpenDaylatroWebsiteCommand { get; }

        #endregion

        #region Events

        /// <summary>
        /// Raised when the user requests to analyze the seed
        /// Passes the seed to analyze
        /// </summary>
        public event EventHandler<string>? AnalyzeSeedRequested;

        #endregion

        #region Constructor

        public DayLatroWidgetViewModel(
            DaylatroHighScoreService scoreService,
            UserProfileService profileService
        )
        {
            _scoreService = scoreService ?? throw new ArgumentNullException(nameof(scoreService));
            _profileService =
                profileService ?? throw new ArgumentNullException(nameof(profileService));

            // Initialize widget properties
            WidgetTitle = "Daylatro";
            WidgetIcon = "CalendarToday";

            // Set fixed position for DayLatro widget - third position (90px spacing)
            PositionX = 20;
            PositionY = 260;

            // Initialize commands
            SubmitScoreCommand = new AsyncRelayCommand(OnSubmitScoreAsync, CanSubmitScore);
            CopySeedCommand = new AsyncRelayCommand(OnCopySeedAsync);
            RefreshScoresCommand = new AsyncRelayCommand(OnRefreshScoresAsync);
            AnalyzeSeedCommand = new RelayCommand(OnAnalyzeSeed);
            OpenDaylatroWebsiteCommand = new RelayCommand(OnOpenDaylatroWebsite);

            // Set up auto-refresh timer (5 minutes)
            _autoRefreshTimer = new DispatcherTimer { Interval = TimeSpan.FromMinutes(5) };
            _autoRefreshTimer.Tick += async (s, e) =>
            {
                if (!IsMinimized)
                {
                    await FetchAndDisplayHighScoresAsync();
                }
            };

            // Initialize with default user initials if available
            var profile = _profileService.GetProfile();
            if (!string.IsNullOrEmpty(profile.AuthorName) && profile.AuthorName.Length >= 3)
            {
                InitialsInput = profile.AuthorName.Substring(0, 3).ToUpper();
            }

            // Load daily challenge
            LoadDailyChallenge();
        }

        #endregion

        #region Public Methods

        public void Initialize()
        {
            LoadDailyChallenge();
        }

        #endregion

        #region Private Methods - Business Logic

        /// <summary>
        /// Load today's daily challenge seed and check for new day
        /// </summary>
        private void LoadDailyChallenge()
        {
            var today = DateTime.UtcNow.Date;

            // Get today's seed using the fixed algorithm
            TodaySeed = DaylatroSeeds.GetDailyBalatroSeed();

            // Update date text
            DateText = $"Daylatro - {today:MMM dd, yyyy}";

            // Check if it's a new day
            if (_lastCheckedDate.Date != today)
            {
                _lastCheckedDate = today;
                ShowNewDayBadge = true;
            }
        }

        /// <summary>
        /// Fetch and display high scores from Daylatro
        /// </summary>
        private async Task FetchAndDisplayHighScoresAsync(bool forceRefresh = false)
        {
            try
            {
                // Cancel any previous loading operation
                _loadingCancellation?.Cancel();
                _loadingCancellation = new CancellationTokenSource();
                var token = _loadingCancellation.Token;

                // Show loading state
                IsLoadingScores = true;
                HighScores.Clear();
                HighScores.Add(
                    new HighScoreDisplayItem
                    {
                        Rank = 0,
                        Initials = "Loading...",
                        Ante = 0,
                        Score = string.Empty,
                    }
                );

                var scores = await _scoreService.FetchDaylatroScoresAsync(
                    DateTime.UtcNow.ToString("yyyy-MM-dd"),
                    forceRefresh
                );

                if (token.IsCancellationRequested)
                    return;

                // Update UI with scores
                HighScores.Clear();

                if (scores.Count == 0)
                {
                    // Show empty state
                    HighScores.Add(
                        new HighScoreDisplayItem
                        {
                            Rank = 0,
                            Initials = "No scores yet",
                            Ante = 0,
                            Score = "Be the first!",
                        }
                    );
                }
                else
                {
                    var displayScores = scores
                        .Select(
                            (s, index) =>
                                new HighScoreDisplayItem
                                {
                                    Rank = index + 1,
                                    Initials = s.Player,
                                    Ante = s.Ante,
                                    Score = s.Score.ToString("N0"),
                                }
                        )
                        .Take(10); // Show top 10

                    foreach (var score in displayScores)
                    {
                        HighScores.Add(score);
                    }

                    DebugLogger.Log(
                        "DayLatroWidgetViewModel",
                        $"Displayed {HighScores.Count} high scores"
                    );
                }
            }
            catch (Exception ex)
            {
                DebugLogger.LogError(
                    "DayLatroWidgetViewModel",
                    $"Failed to fetch high scores: {ex.Message}"
                );

                // Show error state
                HighScores.Clear();
                HighScores.Add(
                    new HighScoreDisplayItem
                    {
                        Rank = 0,
                        Initials = "Error loading",
                        Ante = 0,
                        Score = "Check connection",
                    }
                );
            }
            finally
            {
                IsLoadingScores = false;
            }
        }

        /// <summary>
        /// Show a submission message with auto-hide
        /// </summary>
        private void ShowSubmissionMessage(string message, bool isError)
        {
            SubmissionMessage = message;
            SubmissionMessageIsError = isError;

            // Auto-hide message after 5 seconds
            var timer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(5) };
            timer.Tick += (s, e) =>
            {
                SubmissionMessage = string.Empty;
                timer.Stop();
            };
            timer.Start();
        }

        #endregion

        #region Command Handlers

        /// <summary>
        /// Called when widget is expanded - fetch scores and start timer
        /// </summary>
        protected override async void OnExpanded()
        {
            try
            {
                ShowNewDayBadge = false;

                // Fetch high scores when widget expands
                await FetchAndDisplayHighScoresAsync();

                // Start auto-refresh timer
                _autoRefreshTimer?.Start();
            }
            catch (Exception ex)
            {
                DebugLogger.LogError(
                    "DayLatroWidgetViewModel",
                    $"Error in OnExpanded: {ex.Message}"
                );
            }
        }

        /// <summary>
        /// Called when widget is minimized - stop timer
        /// </summary>
        protected override void OnMinimized()
        {
            // Stop auto-refresh when minimized
            _autoRefreshTimer?.Stop();
        }

        /// <summary>
        /// Check if score can be submitted
        /// </summary>
        private bool CanSubmitScore()
        {
            return !string.IsNullOrWhiteSpace(ScoreInput)
                && !string.IsNullOrWhiteSpace(InitialsInput)
                && !string.IsNullOrWhiteSpace(AnteInput);
        }

        /// <summary>
        /// Submit score to Daylatro
        /// </summary>
        private async Task OnSubmitScoreAsync()
        {
            try
            {
                // Validate score input
                if (!long.TryParse(ScoreInput.Replace(",", "").Trim(), out var score) || score < 0)
                {
                    ShowSubmissionMessage($"Invalid score input: {ScoreInput}", true);
                    DebugLogger.Log(
                        "DayLatroWidgetViewModel",
                        $"Invalid score input: {ScoreInput}"
                    );
                    return;
                }

                // Validate ante input
                if (!int.TryParse(AnteInput.Trim(), out var ante) || ante < 1 || ante > 39)
                {
                    ShowSubmissionMessage($"Invalid ante input (must be 1-39): {AnteInput}", true);
                    DebugLogger.Log("DayLatroWidgetViewModel", $"Invalid ante input: {AnteInput}");
                    return;
                }

                // Get initials (max 3 chars)
                var initials = InitialsInput.Trim().ToUpper();
                if (initials.Length > 3)
                    initials = initials.Substring(0, 3);

                // Submit to Daylatro
                var (success, message) = await _scoreService.SubmitToDaylatroAsync(
                    initials,
                    ante,
                    score
                );

                if (success)
                {
                    DebugLogger.Log(
                        "DayLatroWidgetViewModel",
                        $"Successfully submitted to Daylatro: {initials} - Ante {ante} - {score}"
                    );

                    // Clear score and ante inputs (keep initials for convenience)
                    ScoreInput = string.Empty;
                    AnteInput = string.Empty;

                    // Show success message
                    ShowSubmissionMessage(message, false);

                    // Refresh the high scores after submission
                    await FetchAndDisplayHighScoresAsync();
                }
                else
                {
                    // Show error/info message
                    ShowSubmissionMessage(message, true);
                    DebugLogger.Log(
                        "DayLatroWidgetViewModel",
                        $"Submission blocked or failed: {message}"
                    );
                }
            }
            catch (Exception ex)
            {
                ShowSubmissionMessage($"Error submitting score: {ex.Message}", true);
                DebugLogger.LogError(
                    "DayLatroWidgetViewModel",
                    $"Error in OnSubmitScore: {ex.Message}"
                );
            }
        }

        /// <summary>
        /// Copy seed to clipboard
        /// </summary>
        private Task OnCopySeedAsync()
        {
            CopyToClipboardRequested?.Invoke(this, TodaySeed);
            ShowSubmissionMessage("Seed copied to clipboard!", false);
            return Task.CompletedTask;
        }

        /// <summary>
        /// Refresh high scores manually
        /// </summary>
        private async Task OnRefreshScoresAsync()
        {
            DebugLogger.Log("DayLatroWidgetViewModel", "Manual refresh requested");
            await FetchAndDisplayHighScoresAsync(forceRefresh: true);
        }

        /// <summary>
        /// Request seed analysis
        /// </summary>
        private void OnAnalyzeSeed()
        {
            AnalyzeSeedRequested?.Invoke(this, TodaySeed);
        }

        /// <summary>
        /// Open the Daylatro website
        /// </summary>
        private void OnOpenDaylatroWebsite()
        {
            try
            {
                var psi = new System.Diagnostics.ProcessStartInfo
                {
                    FileName = "https://daylatro.com",
                    UseShellExecute = true,
                };
                System.Diagnostics.Process.Start(psi);
            }
            catch (Exception ex)
            {
                DebugLogger.LogError(
                    "DayLatroWidgetViewModel",
                    $"Failed to open Daylatro website: {ex.Message}"
                );
            }
        }

        #endregion

        #region IDisposable

        private bool _disposed;

        public void Dispose()
        {
            if (_disposed)
                return;

            _autoRefreshTimer?.Stop();
            _autoRefreshTimer = null;
            _loadingCancellation?.Cancel();
            _loadingCancellation?.Dispose();
            _loadingCancellation = null;

            _disposed = true;
        }

        #endregion
    }

    /// <summary>
    /// Display model for high score items in the leaderboard
    /// </summary>
    public class HighScoreDisplayItem
    {
        public int Rank { get; set; }
        public string Initials { get; set; } = string.Empty;
        public int Ante { get; set; }
        public string Score { get; set; } = string.Empty;
    }
}

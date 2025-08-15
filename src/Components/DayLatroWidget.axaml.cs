using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Avalonia.Threading;
using BalatroSeedOracle.Helpers;
using BalatroSeedOracle.Services;
using BalatroSeedOracle.Models;
using BalatroSeedOracle.Views.Modals;
using BalatroSeedOracle.Views;

namespace BalatroSeedOracle.Components
{
    public partial class DayLatroWidget : UserControl
    {
        private Grid? _minimizedView;
        private Border? _expandedView;
        private TextBlock? _dateText;
        private TextBlock? _seedText;
        private TextBlock? _minimizedSeedText;
        private Image? _ToolImage;
        private TextBlock? _descriptionText;
        private TextBox? _scoreInput;
        private TextBox? _initialsInput;
        private TextBox? _anteInput;
        private ItemsControl? _highScoresControl;

        private string _todaySeed = "";
        private DateTime _lastCheckedDate;
        private DaylatroHighScoreService _scoreService = DaylatroHighScoreService.Instance;
        private UserProfileService _profileService = new UserProfileService();
        private DispatcherTimer? _autoRefreshTimer;
        private CancellationTokenSource? _loadingCancellation;

        public DayLatroWidget()
        {
            InitializeComponent();

            _minimizedView = this.FindControl<Grid>("MinimizedView");
            _expandedView = this.FindControl<Border>("ExpandedView");
            _dateText = this.FindControl<TextBlock>("DateText");
            _seedText = this.FindControl<TextBlock>("SeedText");
            _minimizedSeedText = this.FindControl<TextBlock>("MinimizedSeedText");
            _ToolImage = this.FindControl<Image>("ToolImage");
            _descriptionText = this.FindControl<TextBlock>("DescriptionText");
            _scoreInput = this.FindControl<TextBox>("ScoreInput");
            _initialsInput = this.FindControl<TextBox>("InitialsInput");
            _anteInput = this.FindControl<TextBox>("AnteInput");
            _highScoresControl = this.FindControl<ItemsControl>("HighScoresControl");

            LoadDailyChallenge();
            
            // Set up auto-refresh timer (5 minutes)
            _autoRefreshTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromMinutes(5)
            };
            _autoRefreshTimer.Tick += async (s, e) => 
            {
                if (_expandedView?.IsVisible == true)
                {
                    await FetchAndDisplayHighScores();
                }
            };
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }

        private void LoadDailyChallenge()
        {
            var today = DateTime.UtcNow.Date;

            // Get today's seed using the fixed algorithm
            _todaySeed = DaylatroSeeds.GetDailyBalatroSeed();

            // Update UI
            if (_dateText != null)
                _dateText.Text = $"Daylatro - {today:MMM dd, yyyy}";

            if (_seedText != null)
                _seedText.Text = _todaySeed;

            if (_minimizedSeedText != null)
                _minimizedSeedText.Text = _todaySeed;

            // Check if it's a new day
            if (_lastCheckedDate.Date != today)
            {
                _lastCheckedDate = today;
                ShowNewDayBadge();
            }

            RefreshTopScore();
        }

        private void ShowNewDayBadge()
        {
            var badge = this.FindControl<Border>("NewDayBadge");
            if (badge != null)
                badge.IsVisible = true;
        }

        private async void OnMinimizedClick(object? sender, PointerPressedEventArgs e)
        {
            if (_minimizedView != null && _expandedView != null)
            {
                _minimizedView.IsVisible = false;
                _expandedView.IsVisible = true;

                // Hide new day badge when opened
                var badge = this.FindControl<Border>("NewDayBadge");
                if (badge != null)
                    badge.IsVisible = false;
                    
                // Fetch high scores when widget expands
                await FetchAndDisplayHighScores();
                
                // Start auto-refresh timer
                _autoRefreshTimer?.Start();
            }
        }

        private void OnMinimizeClick(object? sender, RoutedEventArgs e)
        {
            if (_minimizedView != null && _expandedView != null)
            {
                _minimizedView.IsVisible = true;
                _expandedView.IsVisible = false;
                
                // Stop auto-refresh when minimized
                _autoRefreshTimer?.Stop();
            }
        }

        private async void OnSubmitScore(object? sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(_scoreInput?.Text) || 
                string.IsNullOrWhiteSpace(_initialsInput?.Text) ||
                string.IsNullOrWhiteSpace(_anteInput?.Text))
            {
                DebugLogger.Log("DayLatroWidget", "All fields required for submission");
                return;
            }

            if (!long.TryParse(_scoreInput.Text.Replace(",", "").Trim(), out var score) || score < 0)
            {
                DebugLogger.Log("DayLatroWidget", $"Invalid score input: {_scoreInput.Text}");
                return;
            }
            
            if (!int.TryParse(_anteInput.Text.Trim(), out var ante) || ante < 1 || ante > 39)
            {
                DebugLogger.Log("DayLatroWidget", $"Invalid ante input: {_anteInput.Text}");
                return;
            }

            // Get initials (max 3 chars)
            var initials = _initialsInput.Text.Trim().ToUpper();
            if (initials.Length > 3)
                initials = initials.Substring(0, 3);
            
            // Submit to Daylatro
            var (success, message) = await _scoreService.SubmitToDaylatroAsync(initials, ante, score);
            
            if (success)
            {
                DebugLogger.Log("DayLatroWidget", $"Successfully submitted to Daylatro: {initials} - Ante {ante} - {score}");
                _scoreInput.Text = "";
                _anteInput.Text = "";
                
                // Show success message
                ShowSubmissionMessage(message, false);
                
                // Refresh the high scores after submission
                await FetchAndDisplayHighScores();
            }
            else
            {
                // Show error/info message
                ShowSubmissionMessage(message, true);
                DebugLogger.Log("DayLatroWidget", $"Submission blocked or failed: {message}");
            }
        }

        private async void OnCopyChallengeClick(object? sender, RoutedEventArgs e)
        {
            await ClipboardService.CopyToClipboardAsync(_todaySeed);
        }
        
        private async void OnRefreshClick(object? sender, RoutedEventArgs e)
        {
            DebugLogger.Log("DayLatroWidget", "Manual refresh requested");
            await FetchAndDisplayHighScores(forceRefresh: true);
        }

        private void RefreshTopScore()
        {
            // No longer used - we display the full table now
        }
        
        private async Task FetchAndDisplayHighScores(bool forceRefresh = false)
        {
            try
            {
                // Cancel any previous loading operation
                _loadingCancellation?.Cancel();
                _loadingCancellation = new CancellationTokenSource();
                var token = _loadingCancellation.Token;
                
                // Show loading state
                if (_highScoresControl != null)
                {
                    _highScoresControl.ItemsSource = new[] 
                    { 
                        new { Rank = 0, Initials = "Loading...", Ante = 0, Score = "" } 
                    };
                }
                
                var scores = await _scoreService.FetchDaylatroScoresAsync(DateTime.UtcNow.ToString("yyyy-MM-dd"), forceRefresh);
                
                if (token.IsCancellationRequested) return;
                
                if (_highScoresControl != null)
                {
                    if (scores.Count == 0)
                    {
                        // Show empty state
                        _highScoresControl.ItemsSource = new[] 
                        { 
                            new { Rank = 0, Initials = "No scores yet", Ante = 0, Score = "Be the first!" } 
                        };
                    }
                    else
                    {
                        var displayScores = scores.Select((s, index) => new
                        {
                            Rank = index + 1,
                            Initials = s.Player,
                            Ante = s.Ante,
                            Score = s.Score.ToString("N0")
                        }).Take(10).ToList(); // Show top 10
                        
                        _highScoresControl.ItemsSource = displayScores;
                        DebugLogger.Log("DayLatroWidget", $"Displayed {displayScores.Count} high scores");
                    }
                }
            }
            catch (Exception ex)
            {
                DebugLogger.LogError("DayLatroWidget", $"Failed to fetch high scores: {ex.Message}");
                
                if (_highScoresControl != null)
                {
                    _highScoresControl.ItemsSource = new[] 
                    { 
                        new { Rank = 0, Initials = "Error loading", Ante = 0, Score = "Check connection" } 
                    };
                }
            }
        }

        private void OnAnalyzeSeedClick(object? sender, RoutedEventArgs e)
        {
            try
            {
                // Walk up visual tree to find main menu to show modal
                var parent = this.Parent;
                BalatroMainMenu? mainMenu = null;
                while (parent != null && mainMenu == null)
                {
                    if (parent is BalatroMainMenu mm) mainMenu = mm;
                    parent = (parent as Control)?.Parent;
                }

                var analyzeModal = new AnalyzeModal();
                analyzeModal.SetSeedAndAnalyze(_todaySeed);

                var stdModal = new StandardModal("ANALYZE");
                stdModal.SetContent(analyzeModal);
                stdModal.BackClicked += (s, _) => mainMenu?.HideModalContent();
                mainMenu?.ShowModalContent(stdModal, "SEED ANALYZER");
            }
            catch (Exception ex)
            {
                DebugLogger.LogError("DayLatroWidget", $"Error opening analyzer: {ex.Message}");
            }
        }
        
        private void ShowSubmissionMessage(string message, bool isError)
        {
            // Find or create a message display area
            var messageText = this.FindControl<TextBlock>("SubmissionMessage");
            if (messageText == null)
            {
                // Create a temporary message display if not in XAML
                var expandedView = this.FindControl<Border>("ExpandedView");
                if (expandedView?.Child is StackPanel panel)
                {
                    messageText = new TextBlock
                    {
                        Name = "SubmissionMessage",
                        HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center,
                        Margin = new Avalonia.Thickness(0, 5),
                        FontSize = 12
                    };
                    // Insert after the submit button area
                    panel.Children.Insert(Math.Min(4, panel.Children.Count), messageText);
                }
            }
            
            if (messageText != null)
            {
                messageText.Text = message;
                messageText.Foreground = isError 
                    ? new Avalonia.Media.SolidColorBrush(Avalonia.Media.Color.Parse("#ff6b6b"))
                    : new Avalonia.Media.SolidColorBrush(Avalonia.Media.Color.Parse("#51cf66"));
                
                // Auto-hide message after 5 seconds
                var timer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(5) };
                timer.Tick += (s, e) =>
                {
                    messageText.Text = "";
                    timer.Stop();
                };
                timer.Start();
            }
        }
    }
}

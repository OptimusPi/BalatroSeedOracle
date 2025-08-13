using System;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
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
        private TextBlock? _topScorePlayer;
        private TextBlock? _topScoreValue;

        private string _todaySeed = "";
        private DateTime _lastCheckedDate;
    private DaylatroHighScoreService _scoreService = DaylatroHighScoreService.Instance;
    private UserProfileService _profileService = new UserProfileService();

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
            _topScorePlayer = this.FindControl<TextBlock>("TopScorePlayer");
            _topScoreValue = this.FindControl<TextBlock>("TopScoreValue");

            LoadDailyChallenge();
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

        private void OnMinimizedClick(object? sender, PointerPressedEventArgs e)
        {
            if (_minimizedView != null && _expandedView != null)
            {
                _minimizedView.IsVisible = false;
                _expandedView.IsVisible = true;

                // Hide new day badge when opened
                var badge = this.FindControl<Border>("NewDayBadge");
                if (badge != null)
                    badge.IsVisible = false;
            }
        }

        private void OnMinimizeClick(object? sender, RoutedEventArgs e)
        {
            if (_minimizedView != null && _expandedView != null)
            {
                _minimizedView.IsVisible = true;
                _expandedView.IsVisible = false;
            }
        }

        private void OnSubmitScore(object? sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(_scoreInput?.Text))
                return;

            if (!long.TryParse(_scoreInput.Text.Replace(",", "").Trim(), out var score) || score < 0)
            {
                DebugLogger.Log("DayLatroWidget", $"Invalid score input: {_scoreInput.Text}");
                return;
            }

            var player = _profileService.GetAuthorName();
            var entry = _scoreService.SubmitScore(_todaySeed, DateTime.UtcNow.Date, player, score);

            DebugLogger.Log("DayLatroWidget", $"Submitted score {entry.Score} by {entry.Player} for seed {_todaySeed}");
            _scoreInput.Text = "";
            RefreshTopScore();
        }

        private async void OnCopyChallengeClick(object? sender, RoutedEventArgs e)
        {
            await ClipboardService.CopyToClipboardAsync(_todaySeed);
        }

        private void RefreshTopScore()
        {
            var top = _scoreService.GetTopScore(_todaySeed);
            if (top == null)
            {
                if (_topScorePlayer != null) _topScorePlayer.Text = "--";
                if (_topScoreValue != null) _topScoreValue.Text = "--";
            }
            else
            {
                if (_topScorePlayer != null) _topScorePlayer.Text = top.Player;
                if (_topScoreValue != null) _topScoreValue.Text = string.Format("{0:N0} Chips", top.Score);
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
    }
}

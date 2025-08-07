using System;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Oracle.Helpers;
using Oracle.Services;

namespace Oracle.Components
{
    public partial class DayLatroWidget : UserControl
    {
        private Grid? _minimizedView;
        private Border? _expandedView;
        private TextBlock? _dateText;
        private TextBlock? _seedText;
        private TextBlock? _minimizedSeedText;
        private Image? _ToolImage;
        private TextBlock? _themeText;
        private TextBlock? _descriptionText;
        private TextBox? _scoreInput;
        private TextBlock? _topScorePlayer;
        private TextBlock? _topScoreValue;

        private string _todaySeed = "";
        private DateTime _lastCheckedDate;

        public DayLatroWidget()
        {
            InitializeComponent();

            _minimizedView = this.FindControl<Grid>("MinimizedView");
            _expandedView = this.FindControl<Border>("ExpandedView");
            _dateText = this.FindControl<TextBlock>("DateText");
            _seedText = this.FindControl<TextBlock>("SeedText");
            _minimizedSeedText = this.FindControl<TextBlock>("MinimizedSeedText");
            _ToolImage = this.FindControl<Image>("ToolImage");
            _themeText = this.FindControl<TextBlock>("ThemeText");
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
                _dateText.Text = $"Daily Challenge - {today:MMM dd, yyyy}";

            if (_seedText != null)
                _seedText.Text = _todaySeed;

            if (_minimizedSeedText != null)
                _minimizedSeedText.Text = _todaySeed;

            // Load theme based on seed (rotate through themes)
            LoadThemeForSeed(_todaySeed);

            // Check if it's a new day
            if (_lastCheckedDate.Date != today)
            {
                _lastCheckedDate = today;
                ShowNewDayBadge();
            }
        }

        private void LoadThemeForSeed(string seed)
        {
            // Rotate through different themes based on seed
            var themes = new[]
            {
                ("Wee Joker Fun Run", "Everybody loves Wee Joker!", "Wee Joker"),
                ("Spectral Sprint", "Find a Spectral card before Ante 2 boss", "Soul"),
                ("Voucher Victory", "Get 3 vouchers by Ante 4", "Overstock"),
                ("Tag Team", "Use 5 different tags in one run", "Ethereal Tag"),
                ("Boss Blitz", "Defeat Ante 8 boss with style", "The Manacle")
            };

            // Use seed to pick theme
            var index = Math.Abs(seed.GetHashCode()) % themes.Length;
            var (theme, description, jokerName) = themes[index];

            if (_themeText != null)
                _themeText.Text = theme;

            if (_descriptionText != null)
                _descriptionText.Text = description;

            // Load joker/item image
            if (_ToolImage != null)
            {
                var spriteService = SpriteService.Instance;
                // Use GetItemImage since some items like "Overstock" are vouchers, not jokers
                _ToolImage.Source = spriteService.GetItemImage(jokerName);
            }
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

            DebugLogger.Log("DayLatroWidget", $"Submitted score: {_scoreInput.Text} for seed {_todaySeed}");

            // TODO: Save score to database/file
            // For now, just clear the input
            _scoreInput.Text = "";
        }

        private async void OnCopyChallengeClick(object? sender, RoutedEventArgs e)
        {
            var theme = _themeText?.Text ?? "Daily Challenge";
            var challengeUrl = $"https://balatrogenie.app/challenge/{_todaySeed}";

            var message = $"Today's Balatro Daily Challenge! " +
                         $"The seed is {_todaySeed}. " +
                         $"The theme is {theme}. " +
                         $"Can you beat the top score? Good luck!\n\n" +
                         $"Challenge link: {challengeUrl}";

            await ClipboardService.CopyToClipboardAsync(message);
            DebugLogger.Log("DayLatroWidget", "Copied daily challenge to clipboard");
        }
    }
}
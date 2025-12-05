using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace BalatroSeedOracle.Components.Widgets
{
    /// <summary>
    /// Notification badge with dynamic shape based on count
    /// - Hidden if count = 0
    /// - Circle if count 1-9
    /// - Pill shape if count >= 10
    /// </summary>
    public partial class NotificationBadge : UserControl
    {
        public static readonly StyledProperty<int> CountProperty =
            AvaloniaProperty.Register<NotificationBadge, int>(nameof(Count));

        public int Count
        {
            get => GetValue(CountProperty);
            set => SetValue(CountProperty, value);
        }

        private Border? _badgeBorder;
        private TextBlock? _badgeText;

        public NotificationBadge()
        {
            InitializeComponent();
            UpdateBadgeAppearance();
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
            _badgeBorder = this.FindControl<Border>("BadgeBorder");
            _badgeText = this.FindControl<TextBlock>("BadgeText");
        }

        protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
        {
            base.OnPropertyChanged(change);
            
            if (change.Property == CountProperty)
            {
                UpdateBadgeAppearance();
            }
        }

        private void UpdateBadgeAppearance()
        {
            if (_badgeBorder == null || _badgeText == null)
                return;

            if (Count <= 0)
            {
                // Hidden for zero count
                IsVisible = false;
                return;
            }

            IsVisible = true;
            _badgeText.Text = Count.ToString();

            if (Count <= 9)
            {
                // Circle shape for 1-9
                _badgeBorder.Width = 20;
                _badgeBorder.Height = 20;
                _badgeBorder.CornerRadius = new CornerRadius(10);
                _badgeBorder.MinWidth = 20;
            }
            else
            {
                // Pill shape for 10+
                _badgeBorder.Width = double.NaN; // Auto width
                _badgeBorder.Height = 20;
                _badgeBorder.CornerRadius = new CornerRadius(10);
                _badgeBorder.MinWidth = 24; // Minimum width for pill
            }
        }
    }
}
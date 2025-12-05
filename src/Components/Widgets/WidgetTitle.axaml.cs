using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace BalatroSeedOracle.Components.Widgets
{
    /// <summary>
    /// Floating title label component for widgets
    /// </summary>
    public partial class WidgetTitle : UserControl
    {
        public static readonly StyledProperty<string> TitleTextProperty =
            AvaloniaProperty.Register<WidgetTitle, string>(nameof(TitleText), string.Empty);

        public string TitleText
        {
            get => GetValue(TitleTextProperty);
            set => SetValue(TitleTextProperty, value);
        }

        public WidgetTitle()
        {
            InitializeComponent();
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }
    }
}
using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace BalatroSeedOracle.Components.Widgets
{
    /// <summary>
    /// Widget icon display component
    /// </summary>
    public partial class WidgetIcon : UserControl
    {
        public static readonly StyledProperty<string> IconTextProperty =
            AvaloniaProperty.Register<WidgetIcon, string>(nameof(IconText), "📦");

        public string IconText
        {
            get => GetValue(IconTextProperty);
            set => SetValue(IconTextProperty, value);
        }

        public WidgetIcon()
        {
            InitializeComponent();
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }
    }
}
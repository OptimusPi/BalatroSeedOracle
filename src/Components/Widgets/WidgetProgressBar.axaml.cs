using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace BalatroSeedOracle.Components.Widgets
{
    /// <summary>
    /// Widget progress bar component
    /// </summary>
    public partial class WidgetProgressBar : UserControl
    {
        public static readonly StyledProperty<double> ProgressProperty =
            AvaloniaProperty.Register<WidgetProgressBar, double>(nameof(Progress));

        public double Progress
        {
            get => GetValue(ProgressProperty);
            set => SetValue(ProgressProperty, value);
        }

        public WidgetProgressBar()
        {
            InitializeComponent();
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }
    }
}
using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace BalatroSeedOracle.Components.Widgets
{
    /// <summary>
    /// Thermometer-style progress bar for minimized widgets
    /// </summary>
    public partial class ThermometerProgressBar : UserControl
    {
        public static readonly StyledProperty<double> ProgressProperty =
            AvaloniaProperty.Register<ThermometerProgressBar, double>(nameof(Progress));

        public double Progress
        {
            get => GetValue(ProgressProperty);
            set => SetValue(ProgressProperty, value);
        }

        public ThermometerProgressBar()
        {
            InitializeComponent();
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }
    }
}
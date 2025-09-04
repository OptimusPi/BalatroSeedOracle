using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using BalatroSeedOracle.Helpers;

namespace BalatroSeedOracle.Views.Modals
{
    public partial class TestModal1 : UserControl
    {
        public TestModal1()
        {
            InitializeComponent();
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }

        private void OnTestButtonClick(object? sender, RoutedEventArgs e)
        {
            DebugLogger.Log("TestModal1", "Test button clicked - safe testing environment ready!");
        }
    }
}
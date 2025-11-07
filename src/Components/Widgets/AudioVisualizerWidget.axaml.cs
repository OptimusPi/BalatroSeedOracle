using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using BalatroSeedOracle.Helpers;
using BalatroSeedOracle.ViewModels;

namespace BalatroSeedOracle.Components
{
    /// <summary>
    /// Simplified Audio Visualizer Widget - KISS principle
    /// Replaces the 1233-line AudioVisualizerSettingsWidget
    /// </summary>
    public partial class AudioVisualizerWidget : UserControl
    {
        public AudioVisualizerWidget()
        {
            InitializeComponent();

            // Set ViewModel from service locator
            DataContext = ServiceHelper.GetRequiredService<AudioVisualizerWidgetViewModel>();
        }

        private void OnMinimizedIconPressed(object? sender, PointerPressedEventArgs e)
        {
            if (DataContext is AudioVisualizerWidgetViewModel vm)
            {
                vm.IsMinimized = false;
            }
        }

        private void OnMinimizedIconReleased(object? sender, PointerReleasedEventArgs e)
        {
            // Event handler for potential future use
        }
    }
}

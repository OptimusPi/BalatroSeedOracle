using Avalonia.Markup.Xaml;
using BalatroSeedOracle.Components;
using BalatroSeedOracle.ViewModels;

namespace BalatroSeedOracle.Desktop.Components.Widgets
{
    /// <summary>
    /// AudioVisualizerSettingsWidget - A movable, minimizable widget for audio visualizer settings
    /// MVVM pattern - ALL business logic in ViewModel, drag handled by DraggableWidgetBehavior
    /// </summary>
    public partial class AudioVisualizerSettingsWidget : BaseWidgetControl
    {
        private AudioVisualizerSettingsWidgetViewModel? ViewModel =>
            DataContext as AudioVisualizerSettingsWidgetViewModel;

        public AudioVisualizerSettingsWidget()
        {
            InitializeComponent();
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }

        protected override void OnWidgetAttached()
        {
            base.OnWidgetAttached();
            // Initialize ViewModel with ownerControl reference so it can find BalatroMainMenu
            ViewModel?.OnAttached(this);
        }

        protected override void OnWidgetDetached()
        {
            base.OnWidgetDetached();
            ViewModel?.OnDetached();
        }

        // Event handlers inherited from BaseWidgetControl:
        // - OnMinimizedIconPressed
        // - OnMinimizedIconReleased
    }
}

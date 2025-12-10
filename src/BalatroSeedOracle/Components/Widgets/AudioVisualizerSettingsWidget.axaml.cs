using System;
using Avalonia.Markup.Xaml;
using BalatroSeedOracle.Helpers;
using BalatroSeedOracle.ViewModels;

namespace BalatroSeedOracle.Components
{
    /// <summary>
    /// AudioVisualizerSettingsWidget - A movable, minimizable widget for audio visualizer settings
    /// MVVM pattern - ALL business logic in ViewModel, drag handled by DraggableWidgetBehavior
    /// </summary>
    public partial class AudioVisualizerSettingsWidget : BaseWidgetControl
    {
        public AudioVisualizerSettingsWidgetViewModel ViewModel { get; }

        public AudioVisualizerSettingsWidget()
        {
            InitializeComponent();

            // Get ViewModel from DI container
            ViewModel =
                ServiceHelper.GetService<AudioVisualizerSettingsWidgetViewModel>()
                ?? throw new InvalidOperationException(
                    "AudioVisualizerSettingsWidgetViewModel service not registered in DI container"
                );
            DataContext = ViewModel;

            // Update ZIndex when IsMinimized changes - now handled by XAML binding to WidgetZIndex
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }

        protected override void OnWidgetAttached()
        {
            base.OnWidgetAttached();
            // CRITICAL: Initialize ViewModel with ownerControl reference so it can find BalatroMainMenu
            ViewModel.OnAttached(this);
        }

        protected override void OnWidgetDetached()
        {
            base.OnWidgetDetached();
            ViewModel.OnDetached();
        }

        // Event handlers inherited from BaseWidgetControl:
        // - OnMinimizedIconPressed
        // - OnMinimizedIconReleased
    }
}

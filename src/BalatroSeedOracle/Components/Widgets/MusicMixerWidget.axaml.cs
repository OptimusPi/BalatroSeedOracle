using System;
using Avalonia.Markup.Xaml;
using BalatroSeedOracle.Helpers;
using BalatroSeedOracle.ViewModels;

namespace BalatroSeedOracle.Components
{
    /// <summary>
    /// Music Mixer Widget - 8-track volume and mute controls
    /// Clean, simple interface following MVVM pattern
    /// </summary>
    public partial class MusicMixerWidget : BaseWidgetControl
    {
        public MusicMixerWidgetViewModel ViewModel { get; }

        public MusicMixerWidget()
        {
            InitializeComponent();

            // Get ViewModel from DI container
            ViewModel =
                ServiceHelper.GetService<MusicMixerWidgetViewModel>()
                ?? throw new InvalidOperationException(
                    "MusicMixerWidgetViewModel service not registered in DI container"
                );
            DataContext = ViewModel;

            // Update ZIndex when IsMinimized changes - now handled by XAML binding to WidgetZIndex

            // Set initial ZIndex - now handled by XAML binding to WidgetZIndex
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }

        // Event handlers inherited from BaseWidgetControl:
        // - OnMinimizedIconPressed
        // - OnMinimizedIconReleased
    }
}

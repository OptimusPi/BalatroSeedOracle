using Avalonia.Markup.Xaml;
using BalatroSeedOracle.Components;

namespace BalatroSeedOracle.Desktop.Components.Widgets
{
    /// <summary>
    /// Music Mixer Widget - 8-track volume and mute controls
    /// Clean, simple interface following MVVM pattern
    /// </summary>
    public partial class MusicMixerWidget : BaseWidgetControl
    {
        public MusicMixerWidget()
        {
            InitializeComponent();
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

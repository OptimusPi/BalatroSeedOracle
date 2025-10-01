using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using BalatroSeedOracle.Controls;
using System;

namespace BalatroSeedOracle.Features.VibeOut
{
    public partial class VibeOutView : Window
    {
        private BalatroShaderBackground? _vibeBackground;

        public VibeOutView()
        {
            InitializeComponent();
            _vibeBackground = this.FindControl<BalatroShaderBackground>("VibeBackground");

            DataContextChanged += (s, e) =>
            {
                if (DataContext is VibeOutViewModel vm)
                {
                    // Connect vibe system to background
                    vm.PropertyChanged += (_, args) =>
                    {
                        if (args.PropertyName == nameof(VibeOutViewModel.IsVibing) && vm.IsVibing)
                        {
                            // Set to VibeOut theme when starting
                            if (_vibeBackground != null)
                            {
                                _vibeBackground.Theme = BalatroShaderBackground.BackgroundTheme.VibeOut;
                                _vibeBackground.UpdateVibeIntensity(0.5f);
                            }
                        }
                        else if (args.PropertyName == nameof(VibeOutViewModel.AudioState))
                        {
                            var intensity = vm.AudioState switch
                            {
                                "VibeLevel2" => 0.8f, // DRUMS2 ACTIVATED!
                                "VibeLevel3" => 1.0f, // MAX VIBE!
                                _ => 0.5f
                            };
                            _vibeBackground?.UpdateVibeIntensity(intensity);
                        }
                    };

                    // Hook up REAL beat detection from VibeAudioManager! 🔥
                    var audioManager = BalatroSeedOracle.App.GetService<Services.VibeAudioManager>();
                    if (audioManager != null)
                    {
                        audioManager.BeatDetected += (beatIntensity) =>
                        {
                            _vibeBackground?.OnBeatDetected(beatIntensity);
                        };
                    }
                }
            };
        }
        
        private void InitializeComponent() => AvaloniaXamlLoader.Load(this);
        
        protected override void OnClosing(WindowClosingEventArgs e)
        {
            if (DataContext is VibeOutViewModel vm) vm.StopVibing();
            base.OnClosing(e);
        }
    }
}

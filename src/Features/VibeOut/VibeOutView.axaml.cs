using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using BalatroSeedOracle.Controls;
using System;

namespace BalatroSeedOracle.Features.VibeOut
{
    public partial class VibeOutView : Window
    {
        private BalatroStyleBackground? _vibeBackground;

        public VibeOutView()
        {
            InitializeComponent();
            _vibeBackground = this.FindControl<BalatroStyleBackground>("VibeBackground");
            
            DataContextChanged += (s, e) =>
            {
                if (DataContext is VibeOutViewModel vm)
                {
                    // Connect vibe system to background
                    vm.PropertyChanged += (_, e) =>
                    {
                        if (e.PropertyName == nameof(VibeOutViewModel.IsVibing) && vm.IsVibing)
                        {
                            _vibeBackground?.EnterVibeOutMode();
                        }
                        else if (e.PropertyName == nameof(VibeOutViewModel.AudioState))
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

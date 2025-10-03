using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using BalatroSeedOracle.Controls;
using BalatroSeedOracle.Views;
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
                    // Load and apply saved settings
                    ApplySavedSettings();

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
                            // Audio intensity is now controlled by user settings slider, not hardcoded!
                            // The slider value (0.0 - 2.0) is applied in ApplySavedSettings
                            // We don't need to update intensity here anymore
                        }
                    };
                }
            };
        }

        private void ApplySavedSettings()
        {
            var profileService = BalatroSeedOracle.App.GetService<Services.UserProfileService>();
            if (profileService == null || _vibeBackground == null) return;

            var settings = profileService.GetProfile().VibeOutSettings;

            // Apply theme from user settings
            _vibeBackground.SetTheme(settings.ThemeIndex);

            // Apply user-controlled intensity settings
            _vibeBackground.UpdateVibeIntensity(settings.AudioIntensity);
            _vibeBackground.SetBaseTimeSpeed(settings.TimeSpeed);
            _vibeBackground.SetParallaxStrength(settings.ParallaxStrength);

            // Apply custom colors if CUSTOMIZE theme selected
            if (settings.ThemeIndex == 8)
            {
                _vibeBackground.SetMainColor(settings.MainColor);
                _vibeBackground.SetAccentColor(settings.AccentColor);
            }
        }
        
        private void InitializeComponent() => AvaloniaXamlLoader.Load(this);
        
        protected override void OnClosing(WindowClosingEventArgs e)
        {
            if (DataContext is VibeOutViewModel vm) vm.StopVibing();
            base.OnClosing(e);
        }
    }
}

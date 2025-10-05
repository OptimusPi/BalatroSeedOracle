using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using BalatroSeedOracle.Controls;
using BalatroSeedOracle.Views;
using System;

namespace BalatroSeedOracle.Features.VibeOut
{
    public partial class VibeOutView : Window
    {
        private BalatroShaderBackground? _balatroBackground;
        private PsychedelicShaderBackground? _psychedelicBackground;

        public VibeOutView()
        {
            InitializeComponent();
            _balatroBackground = this.FindControl<BalatroShaderBackground>("BalatroBackground");
            _psychedelicBackground = this.FindControl<PsychedelicShaderBackground>("PsychedelicBackground");

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
                            if (_balatroBackground != null)
                            {
                                _balatroBackground.Theme = BalatroShaderBackground.BackgroundTheme.VibeOut;
                                _balatroBackground.UpdateVibeIntensity(0.5f);
                            }
                            if (_psychedelicBackground != null)
                            {
                                _psychedelicBackground.UpdateVibeIntensity(0.5f);
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
            if (profileService == null) return;

            var settings = profileService.GetProfile().VibeOutSettings;

            // Apply settings to Balatro shader
            if (_balatroBackground != null)
            {
                _balatroBackground.SetTheme(settings.ThemeIndex);
                _balatroBackground.UpdateVibeIntensity(settings.AudioIntensity);
                _balatroBackground.SetBaseTimeSpeed(settings.TimeSpeed);
                _balatroBackground.SetParallaxStrength(settings.ParallaxStrength);

                if (settings.ThemeIndex == 8)
                {
                    _balatroBackground.SetMainColor(settings.MainColor);
                    _balatroBackground.SetAccentColor(settings.AccentColor);
                }
            }

            // Apply settings to Psychedelic shader
            if (_psychedelicBackground != null)
            {
                _psychedelicBackground.UpdateVibeIntensity(settings.AudioIntensity);
                _psychedelicBackground.SetSpeed(settings.TimeSpeed);
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

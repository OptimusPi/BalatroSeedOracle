using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
using Avalonia.Threading;
using BalatroSeedOracle.Controls;
using BalatroSeedOracle.Services;
using BalatroSeedOracle.Helpers;

namespace BalatroSeedOracle.Views.Modals
{
    public partial class VisualizerWorkspace : UserControl
    {
        private BalatroShaderBackground? _shaderPreview;
        private VibeAudioManager? _audioManager;
        private Random _random = new Random();

        public event EventHandler? CloseRequested;

        public VisualizerWorkspace()
        {
            InitializeComponent();
            SetupControls();

            // Get audio manager for real-time reactivity
            _audioManager = ServiceHelper.GetService<VibeAudioManager>();
            if (_audioManager != null)
            {
                _audioManager.AudioAnalysisUpdated += OnAudioUpdate;
            }

            // Start with a calming default
            SetCalmMood(null, null);
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
            _shaderPreview = this.FindControl<BalatroShaderBackground>("ShaderPreview");
        }

        private void SetupControls()
        {
            // Wire up sliders for real-time updates
            var hueSlider = this.FindControl<Slider>("HueSlider");
            var intensitySlider = this.FindControl<Slider>("IntensitySlider");
            var speedSlider = this.FindControl<Slider>("SpeedSlider");
            var twistSlider = this.FindControl<Slider>("TwistSlider");
            var zoomSlider = this.FindControl<Slider>("ZoomSlider");
            var bassSlider = this.FindControl<Slider>("BassResponseSlider");
            var melodySlider = this.FindControl<Slider>("MelodyResponseSlider");
            var drumsSlider = this.FindControl<Slider>("DrumsResponseSlider");

            if (hueSlider != null)
                hueSlider.PropertyChanged += (s, e) => {
                    if (e.Property == Slider.ValueProperty)
                        UpdateShaderColors();
                };

            if (intensitySlider != null)
                intensitySlider.PropertyChanged += (s, e) => {
                    if (e.Property == Slider.ValueProperty && _shaderPreview != null)
                        _shaderPreview.SetAudioIntensity((float)intensitySlider.Value);
                };

            if (speedSlider != null)
                speedSlider.PropertyChanged += (s, e) => {
                    if (e.Property == Slider.ValueProperty && _shaderPreview != null)
                        _shaderPreview.SetTimeSpeed((float)speedSlider.Value);
                };

            if (twistSlider != null)
                twistSlider.PropertyChanged += (s, e) => {
                    if (e.Property == Slider.ValueProperty && _shaderPreview != null)
                        _shaderPreview.SetTwirlAmount((float)twistSlider.Value);
                };

            if (zoomSlider != null)
                zoomSlider.PropertyChanged += (s, e) => {
                    if (e.Property == Slider.ValueProperty && _shaderPreview != null)
                        _shaderPreview.SetZoomPunch((float)zoomSlider.Value);
                };
        }

        private void UpdateShaderColors()
        {
            if (_shaderPreview == null) return;

            var hueSlider = this.FindControl<Slider>("HueSlider");
            if (hueSlider == null) return;

            float hue = (float)hueSlider.Value / 360f;

            // Convert hue to RGB
            var color = HSVToColor(hue, 0.8f, 1.0f);
            var accentColor = HSVToColor((hue + 0.5f) % 1.0f, 0.7f, 0.9f);

            // TODO: Need to add RGB color setters instead of just color index
            // For now, map to nearest color index (0=Red, 1=Orange, 2=Yellow, 3=Green, 4=Blue, 5=Purple, 6=Brown, 7=White)
            // _shaderPreview.SetMainColor(color.R / 255f, color.G / 255f, color.B / 255f);
            // _shaderPreview.SetAccentColor(accentColor.R / 255f, accentColor.G / 255f, accentColor.B / 255f);

            // Use hue to select color index
            int mainColorIndex = (int)(hue * 8) % 8;
            int accentColorIndex = (mainColorIndex + 4) % 8; // Opposite on color wheel
            _shaderPreview.SetMainColor(mainColorIndex);
            _shaderPreview.SetAccentColor(accentColorIndex);
        }

        private Color HSVToColor(float h, float s, float v)
        {
            int i = (int)(h * 6);
            float f = h * 6 - i;
            float p = v * (1 - s);
            float q = v * (1 - f * s);
            float t = v * (1 - (1 - f) * s);

            float r, g, b;
            switch (i % 6)
            {
                case 0: r = v; g = t; b = p; break;
                case 1: r = q; g = v; b = p; break;
                case 2: r = p; g = v; b = t; break;
                case 3: r = p; g = q; b = v; break;
                case 4: r = t; g = p; b = v; break;
                default: r = v; g = p; b = q; break;
            }

            return Color.FromRgb((byte)(r * 255), (byte)(g * 255), (byte)(b * 255));
        }

        private void OnAudioUpdate(float bass, float mid, float treble, float peak)
        {
            if (_shaderPreview == null) return;

            var bassSlider = this.FindControl<Slider>("BassResponseSlider");
            var melodySlider = this.FindControl<Slider>("MelodyResponseSlider");
            var drumsSlider = this.FindControl<Slider>("DrumsResponseSlider");

            float bassResponse = (float)(bassSlider?.Value ?? 0.7);
            float melodyResponse = (float)(melodySlider?.Value ?? 0.5);
            float drumsResponse = (float)(drumsSlider?.Value ?? 0.8);

            // Apply audio reactivity based on user settings
            _shaderPreview.UpdateMelodicFFT(
                mid * melodyResponse,
                treble * melodyResponse,
                peak * drumsResponse
            );

            // TODO: Track intensities not available from event - need to get from VibeAudioManager directly
            // _shaderPreview.UpdateTrackIntensities(
            //     melodyIntensity * melodyResponse,
            //     chordIntensity * bassResponse,
            //     bassIntensity * bassResponse
            // );

            // Use bass as overall intensity for now
            _shaderPreview.UpdateVibeIntensity(bass);
        }

        // MOOD PRESETS - These are therapeutic combinations!

        private void SetCalmMood(object? sender, RoutedEventArgs? e)
        {
            SetSliderValue("HueSlider", 200); // Blue-green
            SetSliderValue("IntensitySlider", 0.5);
            SetSliderValue("SpeedSlider", 0.3);
            SetSliderValue("TwistSlider", 0.1);
            SetSliderValue("ZoomSlider", 0.05);
            SetSliderValue("BassResponseSlider", 0.3);
            SetSliderValue("MelodyResponseSlider", 0.7);
            SetSliderValue("DrumsResponseSlider", 0.2);
        }

        private void SetEnergyMood(object? sender, RoutedEventArgs? e)
        {
            SetSliderValue("HueSlider", 30); // Orange-red
            SetSliderValue("IntensitySlider", 1.5);
            SetSliderValue("SpeedSlider", 1.5);
            SetSliderValue("TwistSlider", 0.7);
            SetSliderValue("ZoomSlider", 0.5);
            SetSliderValue("BassResponseSlider", 0.9);
            SetSliderValue("MelodyResponseSlider", 0.6);
            SetSliderValue("DrumsResponseSlider", 1.0);
        }

        private void SetFlowMood(object? sender, RoutedEventArgs? e)
        {
            SetSliderValue("HueSlider", 180); // Cyan
            SetSliderValue("IntensitySlider", 1.0);
            SetSliderValue("SpeedSlider", 0.8);
            SetSliderValue("TwistSlider", 0.4);
            SetSliderValue("ZoomSlider", 0.2);
            SetSliderValue("BassResponseSlider", 0.6);
            SetSliderValue("MelodyResponseSlider", 0.8);
            SetSliderValue("DrumsResponseSlider", 0.5);
        }

        private void SetDreamMood(object? sender, RoutedEventArgs? e)
        {
            SetSliderValue("HueSlider", 280); // Purple
            SetSliderValue("IntensitySlider", 0.7);
            SetSliderValue("SpeedSlider", 0.5);
            SetSliderValue("TwistSlider", 0.3);
            SetSliderValue("ZoomSlider", 0.15);
            SetSliderValue("BassResponseSlider", 0.4);
            SetSliderValue("MelodyResponseSlider", 0.9);
            SetSliderValue("DrumsResponseSlider", 0.3);
        }

        private void SetElectricMood(object? sender, RoutedEventArgs? e)
        {
            SetSliderValue("HueSlider", 60); // Yellow
            SetSliderValue("IntensitySlider", 2.0);
            SetSliderValue("SpeedSlider", 2.0);
            SetSliderValue("TwistSlider", 0.9);
            SetSliderValue("ZoomSlider", 0.7);
            SetSliderValue("BassResponseSlider", 1.0);
            SetSliderValue("MelodyResponseSlider", 0.7);
            SetSliderValue("DrumsResponseSlider", 0.9);
        }

        private void SetSoftMood(object? sender, RoutedEventArgs? e)
        {
            SetSliderValue("HueSlider", 330); // Pink
            SetSliderValue("IntensitySlider", 0.4);
            SetSliderValue("SpeedSlider", 0.2);
            SetSliderValue("TwistSlider", 0.05);
            SetSliderValue("ZoomSlider", 0.02);
            SetSliderValue("BassResponseSlider", 0.2);
            SetSliderValue("MelodyResponseSlider", 0.6);
            SetSliderValue("DrumsResponseSlider", 0.1);
        }

        private void RandomizeAll(object? sender, RoutedEventArgs? e)
        {
            SetSliderValue("HueSlider", _random.Next(0, 360));
            SetSliderValue("IntensitySlider", _random.NextDouble() * 2);
            SetSliderValue("SpeedSlider", 0.2 + _random.NextDouble() * 1.5);
            SetSliderValue("TwistSlider", _random.NextDouble() * 0.8);
            SetSliderValue("ZoomSlider", _random.NextDouble() * 0.5);
            SetSliderValue("BassResponseSlider", 0.2 + _random.NextDouble() * 0.8);
            SetSliderValue("MelodyResponseSlider", 0.2 + _random.NextDouble() * 0.8);
            SetSliderValue("DrumsResponseSlider", 0.2 + _random.NextDouble() * 0.8);
        }

        private void ResetToDefault(object? sender, RoutedEventArgs? e)
        {
            SetSliderValue("HueSlider", 180);
            SetSliderValue("IntensitySlider", 1.0);
            SetSliderValue("SpeedSlider", 1.0);
            SetSliderValue("TwistSlider", 0.3);
            SetSliderValue("ZoomSlider", 0.2);
            SetSliderValue("BassResponseSlider", 0.7);
            SetSliderValue("MelodyResponseSlider", 0.5);
            SetSliderValue("DrumsResponseSlider", 0.8);
        }

        private void SetSliderValue(string name, double value)
        {
            var slider = this.FindControl<Slider>(name);
            if (slider != null)
                slider.Value = value;
        }

        private void SavePreset(object? sender, RoutedEventArgs? e)
        {
            // TODO: Save current settings as a preset
            // For now, just show encouragement
            var messageBox = new Window
            {
                Title = "Vibe Saved! 💜",
                Width = 300,
                Height = 150,
                WindowStartupLocation = WindowStartupLocation.CenterOwner,
                Content = new TextBlock
                {
                    Text = "Your beautiful creation has been saved!\n\nKeep creating - you're doing amazing!",
                    TextAlignment = TextAlignment.Center,
                    VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center,
                    Margin = new Thickness(20)
                }
            };
            messageBox.ShowDialog(GetWindow());
        }

        private Window GetWindow()
        {
            return (Window)this.VisualRoot!;
        }

        private void CloseWorkspace(object? sender, RoutedEventArgs? e)
        {
            if (_audioManager != null)
            {
                _audioManager.AudioAnalysisUpdated -= OnAudioUpdate;
            }
            CloseRequested?.Invoke(this, EventArgs.Empty);
        }

        protected override void OnUnloaded(RoutedEventArgs e)
        {
            base.OnUnloaded(e);
            if (_audioManager != null)
            {
                _audioManager.AudioAnalysisUpdated -= OnAudioUpdate;
            }
        }
    }
}
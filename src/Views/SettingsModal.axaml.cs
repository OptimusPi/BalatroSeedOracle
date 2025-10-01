using System;
using Avalonia.Controls;
using Avalonia.Interactivity;

namespace BalatroSeedOracle.Views
{
    public partial class SettingsModal : UserControl
    {
        public event EventHandler? CloseRequested;
        public event EventHandler<int>? ThemeChanged;
        public event EventHandler<double>? MusicVolumeChanged;
        public event EventHandler<double>? SfxVolumeChanged;
        public event EventHandler<double>? ContrastChanged;
        public event EventHandler<double>? SpinChanged;
        public event EventHandler<double>? SpeedChanged;

        public SettingsModal()
        {
            InitializeComponent();

            var themeComboBox = this.FindControl<ComboBox>("ThemeComboBox");
            var musicSlider = this.FindControl<Slider>("MusicVolumeSlider");
            var sfxSlider = this.FindControl<Slider>("SfxVolumeSlider");
            var contrastSlider = this.FindControl<Slider>("ContrastSlider");
            var spinSlider = this.FindControl<Slider>("SpinSlider");
            var speedSlider = this.FindControl<Slider>("SpeedSlider");

            if (themeComboBox != null)
                themeComboBox.SelectionChanged += (s, e) => ThemeChanged?.Invoke(this, themeComboBox.SelectedIndex);

            if (musicSlider != null)
                musicSlider.PropertyChanged += (s, e) =>
                {
                    if (e.Property.Name == nameof(Slider.Value))
                        MusicVolumeChanged?.Invoke(this, musicSlider.Value);
                };

            if (sfxSlider != null)
                sfxSlider.PropertyChanged += (s, e) =>
                {
                    if (e.Property.Name == nameof(Slider.Value))
                        SfxVolumeChanged?.Invoke(this, sfxSlider.Value);
                };

            if (contrastSlider != null)
                contrastSlider.PropertyChanged += (s, e) =>
                {
                    if (e.Property.Name == nameof(Slider.Value))
                        ContrastChanged?.Invoke(this, contrastSlider.Value);
                };

            if (spinSlider != null)
                spinSlider.PropertyChanged += (s, e) =>
                {
                    if (e.Property.Name == nameof(Slider.Value))
                        SpinChanged?.Invoke(this, spinSlider.Value);
                };

            if (speedSlider != null)
                speedSlider.PropertyChanged += (s, e) =>
                {
                    if (e.Property.Name == nameof(Slider.Value))
                        SpeedChanged?.Invoke(this, speedSlider.Value);
                };
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            CloseRequested?.Invoke(this, EventArgs.Empty);
        }
    }
}

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Controls.Primitives;
using Avalonia.Input.Platform;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Avalonia.Threading;
using Avalonia.VisualTree;
using BalatroSeedOracle.Controls;
using BalatroSeedOracle.Helpers;
using BalatroSeedOracle.Services;
using SkiaSharp;

namespace BalatroSeedOracle.Components.Widgets
{
    /// <summary>
    /// Professional Visualizer Development Tool
    /// Allows real-time control of shader uniforms, audio mixing, and preset management
    /// </summary>
    public partial class VisualizerDevWidget : UserControl
    {
        private BalatroShaderBackground? _shaderBackground;
        private VLCAudioManager? _audioManager;
        private readonly DispatcherTimer _peakUpdateTimer;

        // Track peak values
        private readonly Dictionary<string, float> _peakValues = new();

        // Current preset data
        private VisualizerPreset _currentPreset = new();

        public VisualizerDevWidget()
        {
            InitializeComponent();

            // Setup peak meter update timer
            _peakUpdateTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromMilliseconds(50) // 20 FPS update rate
            };
            _peakUpdateTimer.Tick += UpdatePeakMeters;
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
            AttachEventHandlers();
        }

        protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
        {
            base.OnAttachedToVisualTree(e);

            // Find the shader background in the visual tree
            _shaderBackground = this.FindAncestorOfType<Window>()
                ?.GetVisualDescendants()
                ?.OfType<BalatroShaderBackground>()
                ?.FirstOrDefault();

            // Get audio manager
            _audioManager = ServiceHelper.GetService<VLCAudioManager>();

            // Start peak meter updates if audio is available
            if (_audioManager != null)
            {
                _peakUpdateTimer.Start();
            }
        }

        protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
        {
            base.OnDetachedFromVisualTree(e);
            _peakUpdateTimer.Stop();
        }

        private void AttachEventHandlers()
        {
            // UNIFORMS TAB - Direct shader control
            AttachUniformControl("TimeSlider", "TimeTextBox", 0, 100,
                value => _shaderBackground?.SetTime((float)value));

            AttachUniformControl("SpinTimeSlider", "SpinTimeTextBox", 0, 100,
                value => _shaderBackground?.SetSpinTime((float)value));

            AttachUniformControl("ContrastSlider", "ContrastTextBox", 0.1, 10,
                value => _shaderBackground?.SetContrast((float)value));

            AttachUniformControl("SpinAmountSlider", "SpinAmountTextBox", 0, 1,
                value => _shaderBackground?.SetSpinAmount((float)value));

            AttachUniformControl("ParallaxXSlider", "ParallaxXTextBox", -1, 1,
                value => _shaderBackground?.SetParallaxX((float)value));

            AttachUniformControl("ParallaxYSlider", "ParallaxYTextBox", -1, 1,
                value => _shaderBackground?.SetParallaxY((float)value));

            AttachUniformControl("ZoomScaleSlider", "ZoomScaleTextBox", -50, 50,
                value => _shaderBackground?.SetZoomScale((float)value));

            AttachUniformControl("SaturationAmountSlider", "SaturationAmountTextBox", 0, 1,
                value => _shaderBackground?.SetSaturationAmount((float)value));

            AttachUniformControl("PixelSizeSlider", "PixelSizeTextBox", 100, 5000,
                value => _shaderBackground?.SetPixelSize((float)value));

            AttachUniformControl("SpinEaseSlider", "SpinEaseTextBox", 0, 2,
                value => _shaderBackground?.SetSpinEase((float)value));

            // Color controls - simplified for now (will add full ColorPicker later)
            var mainColorHex = this.FindControl<TextBox>("MainColorHex");
            if (mainColorHex != null)
            {
                mainColorHex.LostFocus += (s, e) =>
                {
                    if (TryParseHexColor(mainColorHex.Text, out var color))
                    {
                        _currentPreset.MainColor = color;
                        _shaderBackground?.SetMainColor(color);
                        AutoSavePreset();
                    }
                };
            }

            var accentColorHex = this.FindControl<TextBox>("AccentColorHex");
            if (accentColorHex != null)
            {
                accentColorHex.LostFocus += (s, e) =>
                {
                    if (TryParseHexColor(accentColorHex.Text, out var color))
                    {
                        _currentPreset.AccentColor = color;
                        _shaderBackground?.SetAccentColor(color);
                        AutoSavePreset();
                    }
                };
            }

            var bgColorHex = this.FindControl<TextBox>("BackgroundColorHex");
            if (bgColorHex != null)
            {
                bgColorHex.LostFocus += (s, e) =>
                {
                    if (TryParseHexColor(bgColorHex.Text, out var color))
                    {
                        _currentPreset.BackgroundColor = color;
                        _shaderBackground?.SetBackgroundColor(color);
                        AutoSavePreset();
                    }
                };
            }

            // MIXER TAB - Audio control
            AttachMixerControl("Drums1", 0);
            AttachMixerControl("Drums2", 1);
            AttachMixerControl("Bass1", 2);
            AttachMixerControl("Bass2", 3);
            AttachMixerControl("Chords1", 4);
            AttachMixerControl("Chords2", 5);
            AttachMixerControl("Melody1", 6);
            AttachMixerControl("Melody2", 7);

            // PRESETS TAB
            var saveBtn = this.FindControl<Button>("SavePresetButton");
            if (saveBtn != null) saveBtn.Click += SavePreset;

            var loadBtn = this.FindControl<Button>("LoadPresetButton");
            if (loadBtn != null) loadBtn.Click += LoadPreset;

            var exportBtn = this.FindControl<Button>("ExportJsonButton");
            if (exportBtn != null) exportBtn.Click += ExportJson;

            var importBtn = this.FindControl<Button>("ImportJsonButton");
            if (importBtn != null) importBtn.Click += ImportJson;

            var shareBtn = this.FindControl<Button>("ShareButton");
            if (shareBtn != null) shareBtn.Click += ShareToCommunity;

            // Minimize button
            var minimizeBtn = this.FindControl<Button>("MinimizeButton");
            if (minimizeBtn != null)
            {
                minimizeBtn.Click += (s, e) =>
                {
                    var tabs = this.FindControl<TabControl>("MainTabs");
                    if (tabs != null)
                    {
                        tabs.IsVisible = !tabs.IsVisible;
                        this.Height = tabs.IsVisible ? 500 : 40;
                    }
                };
            }
        }

        private void AttachUniformControl(string sliderName, string textBoxName,
            double min, double max, Action<double> onValueChanged)
        {
            var slider = this.FindControl<Slider>(sliderName);
            var textBox = this.FindControl<TextBox>(textBoxName);

            if (slider == null || textBox == null) return;

            // Slider changed - update textbox and shader
            slider.ValueChanged += (s, e) =>
            {
                var value = e.NewValue;
                textBox.Text = value.ToString("F3");
                onValueChanged?.Invoke(value);
                AutoSavePreset();
            };

            // TextBox changed - update slider and shader
            textBox.LostFocus += (s, e) =>
            {
                if (double.TryParse(textBox.Text, out var value))
                {
                    value = Math.Clamp(value, min, max);
                    slider.Value = value;
                    onValueChanged?.Invoke(value);
                    AutoSavePreset();
                }
            };

            // Enter key in textbox
            textBox.KeyDown += (s, e) =>
            {
                if (e.Key == Avalonia.Input.Key.Enter)
                {
                    if (double.TryParse(textBox.Text, out var value))
                    {
                        value = Math.Clamp(value, min, max);
                        slider.Value = value;
                        onValueChanged?.Invoke(value);
                        AutoSavePreset();
                    }
                }
            };
        }

        private void AttachMixerControl(string trackName, int trackIndex)
        {
            var volumeSlider = this.FindControl<Slider>($"{trackName}VolumeSlider");
            var muteCheckBox = this.FindControl<CheckBox>($"{trackName}MuteCheckBox");

            if (volumeSlider != null)
            {
                volumeSlider.ValueChanged += (s, e) =>
                {
                    var volume = (float)(e.NewValue / 100.0);
                    // _audioManager?.SetTrackVolume(trackIndex, volume);
                };
            }

            if (muteCheckBox != null)
            {
                muteCheckBox.IsCheckedChanged += (s, e) =>
                {
                    var isMuted = muteCheckBox.IsChecked ?? false;
                    // _audioManager?.SetTrackMuted(trackIndex, isMuted);
                };
            }
        }

        private void UpdatePeakMeters(object? sender, EventArgs e)
        {
            if (_audioManager == null) return;

            // Update peak values for each track
            UpdatePeakMeter("Drums1", _audioManager.DrumsIntensity);
            UpdatePeakMeter("Drums2", _audioManager.DrumsIntensity);
            UpdatePeakMeter("Bass1", _audioManager.BassIntensity);
            UpdatePeakMeter("Bass2", _audioManager.BassIntensity);
            UpdatePeakMeter("Chords1", _audioManager.ChordsIntensity);
            UpdatePeakMeter("Chords2", _audioManager.ChordsIntensity);
            UpdatePeakMeter("Melody1", _audioManager.MelodyIntensity);
            UpdatePeakMeter("Melody2", _audioManager.MelodyIntensity);

            // Update frequency and intensity displays for non-drums tracks
            // Note: Frequency data will come from FFT analysis in future update
            // For now, using placeholder frequencies based on typical ranges
            UpdateFrequencyIntensity("Bass1", 80.0f, _audioManager.BassIntensity);    // Bass ~80Hz
            UpdateFrequencyIntensity("Bass2", 80.0f, _audioManager.BassIntensity);
            UpdateFrequencyIntensity("Chords1", 400.0f, _audioManager.ChordsIntensity); // Chords ~400Hz
            UpdateFrequencyIntensity("Chords2", 400.0f, _audioManager.ChordsIntensity);
            UpdateFrequencyIntensity("Melody1", 800.0f, _audioManager.MelodyIntensity); // Melody ~800Hz
            UpdateFrequencyIntensity("Melody2", 800.0f, _audioManager.MelodyIntensity);
        }

        private void UpdateFrequencyIntensity(string trackName, float frequency, float intensity)
        {
            var freqText = this.FindControl<TextBox>($"{trackName}FrequencyText");
            var intensityText = this.FindControl<TextBox>($"{trackName}IntensityText");

            if (freqText != null)
                freqText.Text = $"{frequency:F1} Hz";

            if (intensityText != null)
                intensityText.Text = intensity.ToString("F3");
        }

        private void UpdatePeakMeter(string trackName, float intensity)
        {
            var peakText = this.FindControl<TextBlock>($"{trackName}PeakText");
            if (peakText == null) return;

            // Track peak value with decay
            if (!_peakValues.ContainsKey(trackName))
                _peakValues[trackName] = 0;

            // Update peak with smooth decay
            if (intensity > _peakValues[trackName])
                _peakValues[trackName] = intensity;
            else
                _peakValues[trackName] *= 0.95f; // Decay

            var peak = _peakValues[trackName];
            peakText.Text = peak.ToString("F2");

            // Color based on level
            if (peak > 0.9f)
                peakText.Foreground = Avalonia.Media.Brushes.Red; // Clipping
            else if (peak > 0.7f)
                peakText.Foreground = Avalonia.Media.Brushes.Yellow; // Hot
            else
                peakText.Foreground = Avalonia.Media.Brushes.Lime; // Good
        }

        private bool TryParseHexColor(string hex, out SKColor color)
        {
            color = SKColor.Empty;
            if (string.IsNullOrWhiteSpace(hex)) return false;

            hex = hex.Trim();
            if (hex.StartsWith("#")) hex = hex.Substring(1);

            if (hex.Length == 6)
            {
                try
                {
                    var r = Convert.ToByte(hex.Substring(0, 2), 16);
                    var g = Convert.ToByte(hex.Substring(2, 2), 16);
                    var b = Convert.ToByte(hex.Substring(4, 2), 16);
                    color = new SKColor(r, g, b);
                    return true;
                }
                catch { }
            }
            return false;
        }

        private void AutoSavePreset()
        {
            // Auto-save the current preset to a default file
            GatherCurrentValues();

            var presetsDir = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "BalatroSeedOracle", "VisualizerPresets");
            Directory.CreateDirectory(presetsDir);

            var autoSavePath = Path.Combine(presetsDir, "_autosave.json");
            var json = JsonSerializer.Serialize(_currentPreset, new JsonSerializerOptions
            {
                WriteIndented = true
            });
            File.WriteAllText(autoSavePath, json);
        }

        private void SavePreset(object? sender, RoutedEventArgs e)
        {
            var nameBox = this.FindControl<TextBox>("PresetNameTextBox");
            if (nameBox == null || string.IsNullOrWhiteSpace(nameBox.Text)) return;

            _currentPreset.Name = nameBox.Text;
            _currentPreset.DateCreated = DateTime.Now;

            // Gather all current values
            GatherCurrentValues();

            // Save to local storage
            var json = JsonSerializer.Serialize(_currentPreset, new JsonSerializerOptions
            {
                WriteIndented = true
            });

            var presetsDir = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "BalatroSeedOracle", "VisualizerPresets");
            Directory.CreateDirectory(presetsDir);

            var filePath = Path.Combine(presetsDir, $"{_currentPreset.Name}.json");
            File.WriteAllText(filePath, json);

            // Add to list
            var listBox = this.FindControl<ListBox>("PresetListBox");
            if (listBox != null && !listBox.Items.OfType<string>().Contains(_currentPreset.Name))
            {
                var items = listBox.Items.OfType<string>().ToList();
                items.Add(_currentPreset.Name);
                listBox.ItemsSource = items;
            }
        }

        private void LoadPreset(object? sender, RoutedEventArgs e)
        {
            var listBox = this.FindControl<ListBox>("PresetListBox");
            if (listBox?.SelectedItem is string presetName)
            {
                var presetsDir = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                    "BalatroSeedOracle", "VisualizerPresets");

                var filePath = Path.Combine(presetsDir, $"{presetName}.json");
                if (File.Exists(filePath))
                {
                    var json = File.ReadAllText(filePath);
                    _currentPreset = JsonSerializer.Deserialize<VisualizerPreset>(json) ?? new();
                    ApplyPreset();
                }
            }
        }

        private void ExportJson(object? sender, RoutedEventArgs e)
        {
            GatherCurrentValues();
            var json = JsonSerializer.Serialize(_currentPreset, new JsonSerializerOptions
            {
                WriteIndented = true
            });

            // Copy to clipboard
            var topLevel = TopLevel.GetTopLevel(this);
            topLevel?.Clipboard?.SetTextAsync(json);

        }

        private async void ImportJson(object? sender, RoutedEventArgs e)
        {
            var topLevel = TopLevel.GetTopLevel(this);
            var clipboard = topLevel?.Clipboard;
            if (clipboard != null)
            {
                var json = await clipboard.GetTextAsync();
                if (!string.IsNullOrWhiteSpace(json))
                {
                    try
                    {
                        _currentPreset = JsonSerializer.Deserialize<VisualizerPreset>(json) ?? new();
                        ApplyPreset();
                    }
                    catch
                    {
                        // TODO: Show error notification
                    }
                }
            }
        }

        private void ShareToCommunity(object? sender, RoutedEventArgs e)
        {
            // TODO: Upload to community server
            // For now, just export to clipboard
            ExportJson(sender, e);
        }

        private void GatherCurrentValues()
        {
            // Gather uniform values
            _currentPreset.Uniforms["time"] = GetSliderValue("TimeSlider");
            _currentPreset.Uniforms["spin_time"] = GetSliderValue("SpinTimeSlider");
            _currentPreset.Uniforms["contrast"] = GetSliderValue("ContrastSlider");
            _currentPreset.Uniforms["spin_amount"] = GetSliderValue("SpinAmountSlider");
            _currentPreset.Uniforms["parallax_x"] = GetSliderValue("ParallaxXSlider");
            _currentPreset.Uniforms["parallax_y"] = GetSliderValue("ParallaxYSlider");
            _currentPreset.Uniforms["zoom_scale"] = GetSliderValue("ZoomScaleSlider");
            _currentPreset.Uniforms["saturation_amount"] = GetSliderValue("SaturationAmountSlider");

            // Gather mixer values
            for (int i = 0; i < 8; i++)
            {
                var trackNames = new[] { "Drums1", "Drums2", "Bass1", "Bass2",
                                         "Chords1", "Chords2", "Melody1", "Melody2" };
                _currentPreset.MixerVolumes[i] = GetSliderValue($"{trackNames[i]}VolumeSlider");
                _currentPreset.MixerMutes[i] = GetCheckBoxValue($"{trackNames[i]}MuteCheckBox");
            }
        }

        private void ApplyPreset()
        {
            // Apply uniform values
            SetSliderValue("TimeSlider", _currentPreset.Uniforms.GetValueOrDefault("time"));
            SetSliderValue("SpinTimeSlider", _currentPreset.Uniforms.GetValueOrDefault("spin_time"));
            SetSliderValue("ContrastSlider", _currentPreset.Uniforms.GetValueOrDefault("contrast", 2));
            SetSliderValue("SpinAmountSlider", _currentPreset.Uniforms.GetValueOrDefault("spin_amount", 0.3f));
            SetSliderValue("ParallaxXSlider", _currentPreset.Uniforms.GetValueOrDefault("parallax_x"));
            SetSliderValue("ParallaxYSlider", _currentPreset.Uniforms.GetValueOrDefault("parallax_y"));
            SetSliderValue("ZoomScaleSlider", _currentPreset.Uniforms.GetValueOrDefault("zoom_scale"));
            SetSliderValue("SaturationAmountSlider", _currentPreset.Uniforms.GetValueOrDefault("saturation_amount"));

            // Apply colors
            _shaderBackground?.SetMainColor(_currentPreset.MainColor);
            _shaderBackground?.SetAccentColor(_currentPreset.AccentColor);
            _shaderBackground?.SetBackgroundColor(_currentPreset.BackgroundColor);
        }

        private float GetSliderValue(string sliderName)
        {
            var slider = this.FindControl<Slider>(sliderName);
            return (float)(slider?.Value ?? 0);
        }

        private void SetSliderValue(string sliderName, float value)
        {
            var slider = this.FindControl<Slider>(sliderName);
            if (slider != null) slider.Value = value;
        }

        private bool GetCheckBoxValue(string checkBoxName)
        {
            var checkBox = this.FindControl<CheckBox>(checkBoxName);
            return checkBox?.IsChecked ?? false;
        }

        /// <summary>
        /// Preset data structure for saving/loading visualizer configurations
        /// </summary>
        private class VisualizerPreset
        {
            public string Name { get; set; } = "Untitled";
            public DateTime DateCreated { get; set; }
            public Dictionary<string, float> Uniforms { get; set; } = new();
            public Dictionary<string, AudioMapping> Mappings { get; set; } = new();
            public float[] MixerVolumes { get; set; } = new float[8];
            public bool[] MixerMutes { get; set; } = new bool[8];
            public SKColor MainColor { get; set; } = new SKColor(255, 76, 64);
            public SKColor AccentColor { get; set; } = new SKColor(0, 147, 255);
            public SKColor BackgroundColor { get; set; } = new SKColor(30, 43, 45);
        }

        private class AudioMapping
        {
            public string Source { get; set; } = "None";
            public float MinValue { get; set; }
            public float MaxValue { get; set; }
        }
    }
}
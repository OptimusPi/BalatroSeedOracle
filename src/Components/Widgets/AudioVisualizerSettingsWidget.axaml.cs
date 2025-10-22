using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Markup.Xaml;
using Avalonia.VisualTree;
using BalatroSeedOracle.ViewModels;
using BalatroSeedOracle.Views;

namespace BalatroSeedOracle.Components
{
    /// <summary>
    /// AudioVisualizerSettingsWidget - A movable, minimizable widget for audio visualizer settings
    /// MVVM pattern - ALL business logic in ViewModel, drag handled by DraggableWidgetBehavior
    /// </summary>
    public partial class AudioVisualizerSettingsWidget : UserControl
    {
        public AudioVisualizerSettingsWidgetViewModel ViewModel { get; }

        // Track click vs drag for minimized icon
        private Avalonia.Point _iconPressedPosition;

        public AudioVisualizerSettingsWidget()
        {
            InitializeComponent();

            // Initialize ViewModel (creates it lazily - only when widget is actually used)
            ViewModel = new AudioVisualizerSettingsWidgetViewModel();
            DataContext = ViewModel;

            // Update ZIndex when IsMinimized changes
            ViewModel.PropertyChanged += (s, e) =>
            {
                if (e.PropertyName == nameof(ViewModel.IsMinimized))
                {
                    // Set ZIndex on this UserControl itself
                    // Expanded = 100, Minimized = 1
                    this.ZIndex = ViewModel.IsMinimized ? 1 : 100;
                }
            };

            // Set initial ZIndex
            this.ZIndex = ViewModel.IsMinimized ? 1 : 100;

            // REMOVED: Initialize() method no longer exists
            // ViewModel.Initialize();

            // Wire up cleanup
            this.DetachedFromVisualTree += OnDetachedFromVisualTree;
            this.AttachedToVisualTree += OnAttachedToVisualTree;
        }

        private void OnAttachedToVisualTree(object? sender, VisualTreeAttachmentEventArgs e)
        {
            // Find the main menu ancestor to apply changes live to the shader background
            var mainMenu = this.FindAncestorOfType<BalatroMainMenu>();
            if (mainMenu == null) return;

            // Apply current settings immediately so the UI reflects on the shader
            mainMenu.ApplyVisualizerTheme(ViewModel.ThemeIndex);
            if (ViewModel.IsCustomTheme)
            {
                mainMenu.ApplyMainColor(ViewModel.MainColor);
                mainMenu.ApplyAccentColor(ViewModel.AccentColor);
            }
            // REMOVED: AudioIntensity, ParallaxStrength, TimeSpeed properties no longer exist
            // mainMenu.ApplyAudioIntensity(ViewModel.AudioIntensity);
            // mainMenu.ApplyParallaxStrength(ViewModel.ParallaxStrength);
            // mainMenu.ApplyTimeSpeed(ViewModel.TimeSpeed);
            mainMenu.ApplyShadowFlickerSource(ViewModel.ShadowFlickerSource);
            mainMenu.ApplySpinSource(ViewModel.SpinSource);
            mainMenu.ApplyBeatPulseSource(ViewModel.BeatPulseSource);

            // Subscribe to property changes to update shader in real time
            ViewModel.PropertyChanged += (s, ev) =>
            {
                switch (ev.PropertyName)
                {
                    case nameof(ViewModel.ThemeIndex):
                        mainMenu.ApplyVisualizerTheme(ViewModel.ThemeIndex);
                        break;
                    case nameof(ViewModel.MainColor):
                        mainMenu.ApplyMainColor(ViewModel.MainColor);
                        break;
                    case nameof(ViewModel.AccentColor):
                        mainMenu.ApplyAccentColor(ViewModel.AccentColor);
                        break;
                    // REMOVED: AudioIntensity, ParallaxStrength, TimeSpeed
                    /*
                    case nameof(ViewModel.AudioIntensity):
                        mainMenu.ApplyAudioIntensity(ViewModel.AudioIntensity);
                        break;
                    case nameof(ViewModel.ParallaxStrength):
                        mainMenu.ApplyParallaxStrength(ViewModel.ParallaxStrength);
                        break;
                    case nameof(ViewModel.TimeSpeed):
                        mainMenu.ApplyTimeSpeed(ViewModel.TimeSpeed);
                        break;
                    */
                    case nameof(ViewModel.ShadowFlickerSource):
                        mainMenu.ApplyShadowFlickerSource(ViewModel.ShadowFlickerSource);
                        break;
                    case nameof(ViewModel.SpinSource):
                        mainMenu.ApplySpinSource(ViewModel.SpinSource);
                        break;
                    case nameof(ViewModel.BeatPulseSource):
                        mainMenu.ApplyBeatPulseSource(ViewModel.BeatPulseSource);
                        break;
                }
            };
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);

            // Wire up shader parameter controls
            WireShaderParameterControls();
        }

        private void WireShaderParameterControls()
        {
            // Find shader parameter sliders and wire them up
            var contrastSlider = this.FindControl<Slider>("ContrastSlider");
            var spinAmountSlider = this.FindControl<Slider>("SpinAmountSlider");
            var zoomScaleSlider = this.FindControl<Slider>("ZoomScaleSlider");
            var saturationSlider = this.FindControl<Slider>("SaturationSlider");
            var parallaxXSlider = this.FindControl<Slider>("ParallaxXSlider");
            var parallaxYSlider = this.FindControl<Slider>("ParallaxYSlider");
            var timeSlider = this.FindControl<Slider>("TimeSlider");
            var spinTimeSlider = this.FindControl<Slider>("SpinTimeSlider");
            var pixelSizeSlider = this.FindControl<Slider>("PixelSizeSlider");
            var spinEaseSlider = this.FindControl<Slider>("SpinEaseSlider");

            // Find textboxes for two-way binding
            var contrastTextBox = this.FindControl<TextBox>("ContrastTextBox");
            var spinAmountTextBox = this.FindControl<TextBox>("SpinAmountTextBox");
            var zoomScaleTextBox = this.FindControl<TextBox>("ZoomScaleTextBox");
            var saturationTextBox = this.FindControl<TextBox>("SaturationTextBox");
            var parallaxXTextBox = this.FindControl<TextBox>("ParallaxXTextBox");
            var parallaxYTextBox = this.FindControl<TextBox>("ParallaxYTextBox");
            var timeTextBox = this.FindControl<TextBox>("TimeTextBox");
            var spinTimeTextBox = this.FindControl<TextBox>("SpinTimeTextBox");
            var pixelSizeTextBox = this.FindControl<TextBox>("PixelSizeTextBox");
            var spinEaseTextBox = this.FindControl<TextBox>("SpinEaseTextBox");

            // Wire up sliders to apply shader parameters
            if (contrastSlider != null)
            {
                contrastSlider.ValueChanged += (s, e) => {
                    var value = (float)contrastSlider.Value;
                    var mainMenu = this.FindAncestorOfType<BalatroMainMenu>();
                    mainMenu?.ApplyShaderContrast(value);
                    if (contrastTextBox != null)
                        contrastTextBox.Text = value.ToString("F1");
                };
            }

            if (spinAmountSlider != null)
            {
                spinAmountSlider.ValueChanged += (s, e) => {
                    var value = (float)spinAmountSlider.Value;
                    var mainMenu = this.FindAncestorOfType<BalatroMainMenu>();
                    mainMenu?.ApplyShaderSpinAmount(value);
                    if (spinAmountTextBox != null)
                        spinAmountTextBox.Text = value.ToString("F2");
                };
            }

            if (zoomScaleSlider != null)
            {
                zoomScaleSlider.ValueChanged += (s, e) => {
                    var value = (float)zoomScaleSlider.Value;
                    var mainMenu = this.FindAncestorOfType<BalatroMainMenu>();
                    mainMenu?.ApplyShaderZoomPunch(value);
                    if (zoomScaleTextBox != null)
                        zoomScaleTextBox.Text = value.ToString("F1");
                };
            }

            if (saturationSlider != null)
            {
                saturationSlider.ValueChanged += (s, e) => {
                    var value = (float)saturationSlider.Value;
                    var mainMenu = this.FindAncestorOfType<BalatroMainMenu>();
                    mainMenu?.ApplyShaderMelodySaturation(value);
                    if (saturationTextBox != null)
                        saturationTextBox.Text = value.ToString("F2");
                };
            }

            if (pixelSizeSlider != null)
            {
                pixelSizeSlider.ValueChanged += (s, e) => {
                    var value = (float)pixelSizeSlider.Value;
                    var mainMenu = this.FindAncestorOfType<BalatroMainMenu>();
                    mainMenu?.ApplyShaderPixelSize(value);
                    if (pixelSizeTextBox != null)
                        pixelSizeTextBox.Text = value.ToString("F0");
                };
            }

            if (spinEaseSlider != null)
            {
                spinEaseSlider.ValueChanged += (s, e) => {
                    var value = (float)spinEaseSlider.Value;
                    var mainMenu = this.FindAncestorOfType<BalatroMainMenu>();
                    mainMenu?.ApplyShaderSpinEase(value);
                    if (spinEaseTextBox != null)
                        spinEaseTextBox.Text = value.ToString("F2");
                };
            }

            // Wire up textboxes to update sliders
            if (contrastTextBox != null && contrastSlider != null)
            {
                contrastTextBox.TextChanged += (s, e) => {
                    if (float.TryParse(contrastTextBox.Text, out var value))
                    {
                        value = Math.Clamp(value, 0.1f, 10f);
                        contrastSlider.Value = value;
                    }
                };
            }

            if (spinAmountTextBox != null && spinAmountSlider != null)
            {
                spinAmountTextBox.TextChanged += (s, e) => {
                    if (float.TryParse(spinAmountTextBox.Text, out var value))
                    {
                        value = Math.Clamp(value, 0f, 1f);
                        spinAmountSlider.Value = value;
                    }
                };
            }

            if (zoomScaleTextBox != null && zoomScaleSlider != null)
            {
                zoomScaleTextBox.TextChanged += (s, e) => {
                    if (float.TryParse(zoomScaleTextBox.Text, out var value))
                    {
                        value = Math.Clamp(value, -50f, 50f);
                        zoomScaleSlider.Value = value;
                    }
                };
            }

            if (saturationTextBox != null && saturationSlider != null)
            {
                saturationTextBox.TextChanged += (s, e) => {
                    if (float.TryParse(saturationTextBox.Text, out var value))
                    {
                        value = Math.Clamp(value, 0f, 1f);
                        saturationSlider.Value = value;
                    }
                };
            }

            if (pixelSizeTextBox != null && pixelSizeSlider != null)
            {
                pixelSizeTextBox.TextChanged += (s, e) => {
                    if (float.TryParse(pixelSizeTextBox.Text, out var value))
                    {
                        value = Math.Clamp(value, 100f, 5000f);
                        pixelSizeSlider.Value = value;
                    }
                };
            }

            if (spinEaseTextBox != null && spinEaseSlider != null)
            {
                spinEaseTextBox.TextChanged += (s, e) => {
                    if (float.TryParse(spinEaseTextBox.Text, out var value))
                    {
                        value = Math.Clamp(value, 0f, 2f);
                        spinEaseSlider.Value = value;
                    }
                };
            }

        }

        private void OnDetachedFromVisualTree(object? sender, EventArgs e)
        {
            // REMOVED: Dispose() method no longer exists
            // ViewModel.Dispose();
        }

        /// <summary>
        /// Track pointer pressed position to detect drag vs click
        /// </summary>
        private void OnMinimizedIconPressed(object? sender, Avalonia.Input.PointerPressedEventArgs e)
        {
            _iconPressedPosition = e.GetPosition((Control)sender!);
        }

        /// <summary>
        /// On release: if no drag happened, expand the widget
        /// </summary>
        private void OnMinimizedIconReleased(object? sender, Avalonia.Input.PointerReleasedEventArgs e)
        {
            var releasePosition = e.GetPosition((Control)sender!);
            var distance = Math.Abs(releasePosition.X - _iconPressedPosition.X) + Math.Abs(releasePosition.Y - _iconPressedPosition.Y);

            // If pointer moved less than 20 pixels, treat as click (not drag)
            if (distance < 20)
            {
                ViewModel.ExpandCommand.Execute(null);
            }
        }
    }
}

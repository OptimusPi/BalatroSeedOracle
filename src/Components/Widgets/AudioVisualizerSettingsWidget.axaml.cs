using System;
using Avalonia;
using Avalonia.Controls;
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

            // Initialize ViewModel after XAML is loaded
            ViewModel.Initialize();

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
            mainMenu.ApplyAudioIntensity(ViewModel.AudioIntensity);
            mainMenu.ApplyParallaxStrength(ViewModel.ParallaxStrength);
            mainMenu.ApplyTimeSpeed(ViewModel.TimeSpeed);
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
                    case nameof(ViewModel.AudioIntensity):
                        mainMenu.ApplyAudioIntensity(ViewModel.AudioIntensity);
                        break;
                    case nameof(ViewModel.ParallaxStrength):
                        mainMenu.ApplyParallaxStrength(ViewModel.ParallaxStrength);
                        break;
                    case nameof(ViewModel.TimeSpeed):
                        mainMenu.ApplyTimeSpeed(ViewModel.TimeSpeed);
                        break;
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
        }

        private void OnDetachedFromVisualTree(object? sender, EventArgs e)
        {
            ViewModel.Dispose();
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

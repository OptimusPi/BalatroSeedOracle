using System;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using BalatroSeedOracle.Helpers;
using BalatroSeedOracle.ViewModels;

namespace BalatroSeedOracle.Components
{
    /// <summary>
    /// Music Mixer Widget - 8-track volume and mute controls
    /// Clean, simple interface following MVVM pattern
    /// </summary>
    public partial class MusicMixerWidget : UserControl
    {
        public MusicMixerWidgetViewModel ViewModel { get; }

        // Track click vs drag for minimized icon
        private Avalonia.Point _iconPressedPosition;

        public MusicMixerWidget()
        {
            InitializeComponent();

            // Get ViewModel from DI container
            ViewModel = ServiceHelper.GetService<MusicMixerWidgetViewModel>()
                ?? throw new InvalidOperationException("MusicMixerWidgetViewModel service not registered in DI container");
            DataContext = ViewModel;

            // Update ZIndex when IsMinimized changes - now handled by XAML binding to WidgetZIndex
            // ViewModel.PropertyChanged += (s, e) => { ... }; // REMOVED - use XAML binding instead

            // Set initial ZIndex - now handled by XAML binding to WidgetZIndex
            // this.ZIndex = ViewModel.IsMinimized ? 1 : 100; // REMOVED - use XAML binding instead
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
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
